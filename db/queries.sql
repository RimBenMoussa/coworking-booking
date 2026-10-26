-- ============================================================================
-- Requêtes de référence. Elles servent de spécification pour l'API et pour les
-- métriques métier. Les paramètres sont des variables psql (-v nom=valeur).
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. Recherche d'espaces libres (US-06)
-- Paramètres : :'start_ts', :'end_ts' (timestamptz), :min_capacity (int),
--              :'amenity_ids' (ex. '{1,2}' ; '{}' = aucun équipement exigé),
--              :'site_tz' (fuseau du site, ex. 'Africa/Casablanca').
-- Un espace est proposé s'il est actif, assez grand, ouvert sur toute la plage
-- (une réservation ne peut pas traverser minuit), sans fermeture ni réservation
-- active qui chevauche, et muni de tous les équipements demandés.
-- ----------------------------------------------------------------------------
SELECT s.id, s.name, s.type, s.capacity, s.hourly_price
FROM spaces s
WHERE s.is_active
  AND s.capacity >= :min_capacity
  AND EXISTS (
        SELECT 1 FROM opening_hours oh
        WHERE oh.space_id = s.id
          AND oh.weekday    = EXTRACT(ISODOW FROM (:'start_ts'::timestamptz AT TIME ZONE :'site_tz'))
          AND oh.open_time  <= (:'start_ts'::timestamptz AT TIME ZONE :'site_tz')::time
          AND oh.close_time >= (:'end_ts'::timestamptz   AT TIME ZONE :'site_tz')::time
          AND (:'start_ts'::timestamptz AT TIME ZONE :'site_tz')::date
            = (:'end_ts'::timestamptz   AT TIME ZONE :'site_tz')::date
      )
  AND NOT EXISTS (
        SELECT 1 FROM closures c
        WHERE c.space_id = s.id
          AND tstzrange(c.starts_at, c.ends_at, '[)') && tstzrange(:'start_ts'::timestamptz, :'end_ts'::timestamptz, '[)')
      )
  AND NOT EXISTS (
        SELECT 1 FROM bookings b
        WHERE b.space_id = s.id
          AND b.status IN ('pending', 'confirmed', 'used')
          AND tstzrange(b.start_at, b.end_at, '[)') && tstzrange(:'start_ts'::timestamptz, :'end_ts'::timestamptz, '[)')
      )
  AND NOT EXISTS (
        SELECT 1 FROM unnest(:'amenity_ids'::bigint[]) AS wanted(amenity_id)
        WHERE NOT EXISTS (
            SELECT 1 FROM space_amenities sa WHERE sa.space_id = s.id AND sa.amenity_id = wanted.amenity_id
        )
      )
ORDER BY s.capacity, s.hourly_price;

-- ----------------------------------------------------------------------------
-- 2. Quota d'heures du mois d'un membre abonné
-- Paramètres : :'member_email', :'month_start' (date, 1er jour du mois).
-- ----------------------------------------------------------------------------
SELECT m.email,
       p.name                                                    AS plan,
       p.included_hours,
       COALESCE(round(SUM(EXTRACT(EPOCH FROM (b.end_at - b.start_at)) / 3600)::numeric, 2), 0) AS hours_booked,
       p.included_hours
         - COALESCE(round(SUM(EXTRACT(EPOCH FROM (b.end_at - b.start_at)) / 3600)::numeric, 2), 0) AS hours_left
FROM members m
JOIN subscriptions sub ON sub.member_id = m.id AND sub.status = 'active'
JOIN subscription_plans p ON p.id = sub.plan_id
LEFT JOIN bookings b ON b.member_id = m.id
     AND b.status IN ('confirmed', 'used')
     AND b.start_at >= :'month_start'::date
     AND b.start_at <  (:'month_start'::date + interval '1 month')
WHERE m.email = :'member_email'
GROUP BY m.email, p.name, p.included_hours;

-- ----------------------------------------------------------------------------
-- 3. Planning du jour par espace (gestionnaire, US-13)
-- Paramètres : :'day_start' (timestamptz, début de journée locale).
-- ----------------------------------------------------------------------------
SELECT s.name AS space, b.start_at, b.end_at, m.full_name AS member, b.status
FROM bookings b
JOIN spaces  s ON s.id = b.space_id
JOIN members m ON m.id = b.member_id
WHERE b.start_at >= :'day_start'::timestamptz
  AND b.start_at <  :'day_start'::timestamptz + interval '1 day'
  AND b.status IN ('pending', 'confirmed', 'used')
ORDER BY s.name, b.start_at;

-- ----------------------------------------------------------------------------
-- 4. Heures réservées par espace sur une période (taux d'occupation, US-22)
-- Paramètres : :'period_start', :'period_end' (timestamptz).
-- Le taux se calcule en divisant par les heures d'ouverture de la période.
-- ----------------------------------------------------------------------------
SELECT s.name,
       COALESCE(round(SUM(EXTRACT(EPOCH FROM (b.end_at - b.start_at)) / 3600)::numeric, 2), 0) AS hours_booked,
       COUNT(b.id) FILTER (WHERE b.status = 'no_show')   AS no_shows,
       COUNT(b.id) FILTER (WHERE b.status = 'cancelled') AS cancellations
FROM spaces s
LEFT JOIN bookings b ON b.space_id = s.id
     AND b.start_at >= :'period_start'::timestamptz
     AND b.start_at <  :'period_end'::timestamptz
GROUP BY s.name
ORDER BY hours_booked DESC;

-- ----------------------------------------------------------------------------
-- 5. Rappels à envoyer (job planifié, US-14)
-- Paramètre : :'reminder_delay' (ex. '24 hours').
-- Réservations confirmées qui commencent dans le délai et sans rappel encore créé.
-- ----------------------------------------------------------------------------
SELECT b.id AS booking_id, m.email, s.name AS space, b.start_at
FROM bookings b
JOIN members m ON m.id = b.member_id
JOIN spaces  s ON s.id = b.space_id
WHERE b.status = 'confirmed'
  AND b.start_at BETWEEN now() AND now() + :'reminder_delay'::interval
  AND NOT EXISTS (
        SELECT 1 FROM notifications n
        WHERE n.booking_id = b.id AND n.type = 'reminder' AND n.channel = 'email'
      );

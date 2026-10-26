-- ============================================================================
-- Tests des contraintes du schéma. À lancer sur une base contenant schema.sql
-- et seed.sql :  psql -v ON_ERROR_STOP=1 -f db/tests.sql
-- Chaque test échoue bruyamment (RAISE EXCEPTION) si la contrainte ne réagit pas.
-- Tout est annulé en fin de script (ROLLBACK) : la base reste inchangée.
-- ============================================================================
BEGIN;

CREATE FUNCTION pg_temp.expect_error(sql text, expected_state text, label text) RETURNS void AS $$
BEGIN
    EXECUTE sql;
    RAISE EXCEPTION 'TEST ÉCHOUÉ (aucune erreur levée) : %', label;
EXCEPTION WHEN OTHERS THEN
    IF SQLSTATE = 'P0001' AND SQLERRM LIKE 'TEST ÉCHOUÉ%' THEN
        RAISE;
    ELSIF SQLSTATE <> expected_state THEN
        RAISE EXCEPTION 'TEST ÉCHOUÉ (%) : attendu %, obtenu % (%)', label, expected_state, SQLSTATE, SQLERRM;
    END IF;
    RAISE NOTICE 'OK  %', label;
END;
$$ LANGUAGE plpgsql;

-- 1. Double réservation : même espace, plage qui chevauche -> 23P01 (exclusion_violation)
SELECT pg_temp.expect_error($$
    INSERT INTO bookings (member_id, space_id, start_at, end_at, status)
    SELECT (SELECT id FROM members WHERE email = 'lina@coworking.test'), s.id,
           b.start_at + interval '30 minutes', b.end_at + interval '30 minutes', 'confirmed'
    FROM bookings b JOIN spaces s ON s.id = b.space_id
    WHERE s.name = 'Salle Atlas' AND b.status = 'confirmed' ORDER BY b.start_at LIMIT 1
$$, '23P01', 'chevauchement sur le même espace refusé');

-- 2. Réservations adjacentes (fin = début suivant) : autorisées
INSERT INTO bookings (member_id, space_id, start_at, end_at, status)
SELECT (SELECT id FROM members WHERE email = 'lina@coworking.test'), s.id, b.end_at, b.end_at + interval '1 hour', 'confirmed'
FROM bookings b JOIN spaces s ON s.id = b.space_id
WHERE s.name = 'Salle Atlas' AND b.status = 'confirmed' ORDER BY b.start_at LIMIT 1;
DO $$ BEGIN RAISE NOTICE 'OK  réservations adjacentes acceptées'; END $$;

-- 3. Même créneau sur un autre espace : autorisé
INSERT INTO bookings (member_id, space_id, start_at, end_at, status)
SELECT (SELECT id FROM members WHERE email = 'lina@coworking.test'),
       (SELECT id FROM spaces WHERE name = 'Salle Rif'), b.start_at, b.end_at, 'confirmed'
FROM bookings b JOIN spaces s ON s.id = b.space_id
WHERE s.name = 'Salle Atlas' AND b.status = 'confirmed' ORDER BY b.start_at LIMIT 1;
DO $$ BEGIN RAISE NOTICE 'OK  même créneau sur un autre espace accepté'; END $$;

-- 4. Une réservation annulée ne bloque plus le créneau
INSERT INTO bookings (member_id, space_id, start_at, end_at, status)
SELECT (SELECT id FROM members WHERE email = 'amine@coworking.test'), b.space_id, b.start_at, b.end_at, 'confirmed'
FROM bookings b WHERE b.status = 'cancelled' LIMIT 1;
DO $$ BEGIN RAISE NOTICE 'OK  créneau d''une réservation annulée réutilisable'; END $$;

-- 5. Plage invalide -> 23514 (check_violation)
SELECT pg_temp.expect_error($$
    INSERT INTO bookings (member_id, space_id, start_at, end_at)
    VALUES (1, 1, now() + interval '5 days', now() + interval '5 days' - interval '1 hour')
$$, '23514', 'fin avant début refusée');

-- 6. Horaires d'ouverture incohérents -> 23514
SELECT pg_temp.expect_error($$
    INSERT INTO opening_hours (space_id, weekday, open_time, close_time) VALUES (1, 7, '18:00', '09:00')
$$, '23514', 'fermeture avant ouverture refusée');

SELECT pg_temp.expect_error($$
    INSERT INTO opening_hours (space_id, weekday, open_time, close_time) VALUES (1, 8, '09:00', '18:00')
$$, '23514', 'jour de semaine hors 1-7 refusé');

-- 7. Email unique sans tenir compte de la casse -> 23505 (unique_violation)
SELECT pg_temp.expect_error($$
    INSERT INTO members (email, password_hash, full_name) VALUES ('AMINE@coworking.test', 'x', 'Doublon')
$$, '23505', 'email en double (casse différente) refusé');

-- 8. Deux abonnements actifs qui se chevauchent -> 23P01
SELECT pg_temp.expect_error($$
    INSERT INTO subscriptions (member_id, plan_id, starts_on, status)
    SELECT m.id, (SELECT id FROM subscription_plans WHERE name = 'Équipe'), current_date, 'active'
    FROM members m WHERE m.email = 'amine@coworking.test'
$$, '23P01', 'deux abonnements actifs simultanés refusés');

-- 9. Capacité nulle -> 23514
SELECT pg_temp.expect_error($$
    INSERT INTO spaces (name, type, capacity, hourly_price) VALUES ('Salle fantôme', 'meeting_room', 0, 10)
$$, '23514', 'capacité nulle refusée');

-- 10. Le trigger met à jour updated_at
DO $$
DECLARE before_ts timestamptz; after_ts timestamptz;
BEGIN
    SELECT updated_at INTO before_ts FROM bookings ORDER BY id LIMIT 1;
    PERFORM pg_sleep(0.05);
    UPDATE bookings SET price = price WHERE id = (SELECT min(id) FROM bookings);
    SELECT updated_at INTO after_ts FROM bookings WHERE id = (SELECT min(id) FROM bookings);
    IF after_ts <= before_ts THEN RAISE EXCEPTION 'TEST ÉCHOUÉ : updated_at non mis à jour'; END IF;
    RAISE NOTICE 'OK  updated_at mis à jour par le trigger';
END $$;

ROLLBACK;

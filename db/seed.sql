-- ============================================================================
-- Données de démonstration (développement uniquement, jamais en production).
-- Les hachages ci-dessous sont des valeurs factices : aucun mot de passe réel.
-- Exécution : psql -v ON_ERROR_STOP=1 -f db/seed.sql  (après db/schema.sql)
-- ============================================================================

INSERT INTO members (email, password_hash, full_name, phone, role) VALUES
    ('admin@coworking.test',    'seed-placeholder-hash', 'Admin Coworking',  NULL,            'admin'),
    ('manager@coworking.test',  'seed-placeholder-hash', 'Sara Gestion',     '+212600000001', 'manager'),
    ('amine@coworking.test',    'seed-placeholder-hash', 'Amine Alaoui',     '+212600000002', 'member'),
    ('lina@coworking.test',     'seed-placeholder-hash', 'Lina Bennani',     '+212600000003', 'member');

INSERT INTO amenities (name) VALUES
    ('Écran'), ('Visioconférence'), ('Tableau blanc'), ('Prises multiples'), ('Climatisation'), ('Imprimante');

INSERT INTO spaces (name, type, capacity, hourly_price, description) VALUES
    ('Salle Atlas',   'meeting_room',   8, 25.00, 'Grande salle de réunion avec écran et visioconférence'),
    ('Salle Rif',     'meeting_room',   4, 15.00, 'Petite salle pour réunions d’équipe'),
    ('Bureau Sahara', 'private_office', 3, 20.00, 'Bureau privé fermé'),
    ('Poste Flex 1',  'flex_desk',      1,  3.00, 'Poste en open space'),
    ('Poste Flex 2',  'flex_desk',      1,  3.00, 'Poste en open space'),
    ('Poste Flex 3',  'flex_desk',      1,  3.00, 'Poste en open space');

-- Équipements par espace
INSERT INTO space_amenities (space_id, amenity_id)
SELECT s.id, a.id
FROM (VALUES
    ('Salle Atlas',   'Écran'), ('Salle Atlas',   'Visioconférence'), ('Salle Atlas',   'Tableau blanc'),
    ('Salle Atlas',   'Climatisation'), ('Salle Rif', 'Écran'), ('Salle Rif', 'Tableau blanc'),
    ('Bureau Sahara', 'Prises multiples'), ('Bureau Sahara', 'Climatisation'),
    ('Poste Flex 1',  'Prises multiples'), ('Poste Flex 2', 'Prises multiples'), ('Poste Flex 3', 'Prises multiples')
) AS v(space_name, amenity_name)
JOIN spaces s    ON s.name = v.space_name
JOIN amenities a ON a.name = v.amenity_name;

-- Ouvert du lundi au vendredi de 08h à 20h ; salles de réunion aussi le samedi de 09h à 13h.
INSERT INTO opening_hours (space_id, weekday, open_time, close_time)
SELECT s.id, d, '08:00', '20:00' FROM spaces s, generate_series(1, 5) AS d;
INSERT INTO opening_hours (space_id, weekday, open_time, close_time)
SELECT s.id, 6, '09:00', '13:00' FROM spaces s WHERE s.type = 'meeting_room';

-- Fermeture de maintenance pour le Bureau Sahara dans 10 jours (une journée).
INSERT INTO closures (space_id, starts_at, ends_at, reason)
SELECT id, date_trunc('day', now()) + interval '10 days', date_trunc('day', now()) + interval '11 days', 'Maintenance climatisation'
FROM spaces WHERE name = 'Bureau Sahara';

INSERT INTO subscription_plans (name, monthly_price, included_hours, discount_percent) VALUES
    ('Découverte', 0.00,    0,  0.00),
    ('Pro',        250.00, 20, 10.00),
    ('Équipe',     600.00, 60, 20.00);

INSERT INTO subscriptions (member_id, plan_id, starts_on, ends_on, status)
SELECT m.id, p.id, current_date - 15, NULL, 'active'
FROM members m, subscription_plans p
WHERE (m.email = 'amine@coworking.test' AND p.name = 'Pro')
   OR (m.email = 'lina@coworking.test'  AND p.name = 'Découverte');

-- Quelques réservations demain et après-demain (heures du serveur).
INSERT INTO bookings (member_id, space_id, start_at, end_at, status, price)
SELECT m.id, s.id,
       date_trunc('day', now()) + v.day_offset * interval '1 day' + v.start_h * interval '1 hour',
       date_trunc('day', now()) + v.day_offset * interval '1 day' + v.end_h   * interval '1 hour',
       v.status::booking_status, v.price
FROM (VALUES
    ('amine@coworking.test', 'Salle Atlas',  1, 10, 12, 'confirmed', 45.00),
    ('lina@coworking.test',  'Salle Atlas',  1, 14, 15, 'confirmed', 25.00),
    ('lina@coworking.test',  'Salle Rif',    1,  9, 10, 'cancelled', 15.00),
    ('amine@coworking.test', 'Poste Flex 1', 2,  9, 17, 'pending',   24.00)
) AS v(email, space_name, day_offset, start_h, end_h, status, price)
JOIN members m ON m.email = v.email
JOIN spaces  s ON s.name  = v.space_name;

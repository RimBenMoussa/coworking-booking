-- ============================================================================
-- Coworking booking : schéma PostgreSQL (version 1)
-- Cible : PostgreSQL 15 ou supérieur.
-- Exécution : psql -v ON_ERROR_STOP=1 -f db/schema.sql
-- ============================================================================

-- btree_gist permet de combiner l'égalité (space_id) et le chevauchement de
-- plages dans une même contrainte d'exclusion (voir bookings).
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- ----------------------------------------------------------------------------
-- Types énumérés
-- ----------------------------------------------------------------------------
CREATE TYPE member_role         AS ENUM ('member', 'manager', 'admin');
CREATE TYPE space_type          AS ENUM ('meeting_room', 'private_office', 'flex_desk');
CREATE TYPE booking_status      AS ENUM ('pending', 'confirmed', 'used', 'cancelled', 'no_show');
CREATE TYPE subscription_status AS ENUM ('active', 'expired', 'cancelled');
CREATE TYPE notification_type   AS ENUM ('confirmation', 'reminder', 'cancellation');
CREATE TYPE notification_status AS ENUM ('pending', 'sent', 'failed');

-- ----------------------------------------------------------------------------
-- members : comptes utilisateurs (membres, gestionnaires, administrateurs)
-- ----------------------------------------------------------------------------
CREATE TABLE members (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email          text        NOT NULL,
    password_hash  text        NOT NULL,
    full_name      text        NOT NULL,
    phone          text,
    role           member_role NOT NULL DEFAULT 'member',
    is_active      boolean     NOT NULL DEFAULT true,
    created_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT members_email_format CHECK (email LIKE '%_@_%')
);
-- Unicité de l'email sans tenir compte de la casse.
CREATE UNIQUE INDEX members_email_unique ON members (lower(email));

-- ----------------------------------------------------------------------------
-- spaces : espaces réservables
-- ----------------------------------------------------------------------------
CREATE TABLE spaces (
    id            bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name          text          NOT NULL UNIQUE,
    type          space_type    NOT NULL,
    capacity      integer       NOT NULL,
    hourly_price  numeric(8, 2) NOT NULL,
    description   text,
    is_active     boolean       NOT NULL DEFAULT true,
    created_at    timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT spaces_capacity_positive CHECK (capacity > 0),
    CONSTRAINT spaces_price_not_negative CHECK (hourly_price >= 0)
);
CREATE INDEX spaces_type_capacity_idx ON spaces (type, capacity) WHERE is_active;

-- ----------------------------------------------------------------------------
-- amenities / space_amenities : équipements (écran, visio, tableau blanc...)
-- ----------------------------------------------------------------------------
CREATE TABLE amenities (
    id    bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  text NOT NULL UNIQUE
);

CREATE TABLE space_amenities (
    space_id    bigint NOT NULL REFERENCES spaces (id)    ON DELETE CASCADE,
    amenity_id  bigint NOT NULL REFERENCES amenities (id) ON DELETE RESTRICT,
    PRIMARY KEY (space_id, amenity_id)
);
CREATE INDEX space_amenities_amenity_idx ON space_amenities (amenity_id);

-- ----------------------------------------------------------------------------
-- opening_hours : horaires d'ouverture hebdomadaires par espace
-- weekday suit la norme ISO : 1 = lundi ... 7 = dimanche.
-- Plusieurs plages par jour sont possibles (pause déjeuner).
-- ----------------------------------------------------------------------------
CREATE TABLE opening_hours (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    space_id    bigint   NOT NULL REFERENCES spaces (id) ON DELETE CASCADE,
    weekday     smallint NOT NULL,
    open_time   time     NOT NULL,
    close_time  time     NOT NULL,
    CONSTRAINT opening_hours_weekday_range CHECK (weekday BETWEEN 1 AND 7),
    CONSTRAINT opening_hours_order CHECK (close_time > open_time),
    CONSTRAINT opening_hours_unique UNIQUE (space_id, weekday, open_time)
);

-- ----------------------------------------------------------------------------
-- closures : fermetures exceptionnelles (maintenance, jour férié)
-- ----------------------------------------------------------------------------
CREATE TABLE closures (
    id         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    space_id   bigint      NOT NULL REFERENCES spaces (id) ON DELETE CASCADE,
    starts_at  timestamptz NOT NULL,
    ends_at    timestamptz NOT NULL,
    reason     text,
    CONSTRAINT closures_order CHECK (ends_at > starts_at)
);
CREATE INDEX closures_space_period_idx
    ON closures USING gist (space_id, tstzrange(starts_at, ends_at, '[)'));

-- ----------------------------------------------------------------------------
-- subscription_plans / subscriptions : abonnements des membres
-- Un plan définit un forfait mensuel d'heures incluses et une remise.
-- Le paiement n'est pas géré : ces tables servent uniquement à calculer le prix
-- d'une réservation et le quota d'heures restant.
-- ----------------------------------------------------------------------------
CREATE TABLE subscription_plans (
    id                    bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name                  text          NOT NULL UNIQUE,
    monthly_price         numeric(8, 2) NOT NULL,
    included_hours        integer       NOT NULL DEFAULT 0,
    discount_percent      numeric(5, 2) NOT NULL DEFAULT 0,
    is_active             boolean       NOT NULL DEFAULT true,
    CONSTRAINT plans_price_not_negative    CHECK (monthly_price >= 0),
    CONSTRAINT plans_hours_not_negative    CHECK (included_hours >= 0),
    CONSTRAINT plans_discount_range        CHECK (discount_percent BETWEEN 0 AND 100)
);

CREATE TABLE subscriptions (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    member_id   bigint              NOT NULL REFERENCES members (id)            ON DELETE CASCADE,
    plan_id     bigint              NOT NULL REFERENCES subscription_plans (id) ON DELETE RESTRICT,
    starts_on   date                NOT NULL,
    ends_on     date,
    status      subscription_status NOT NULL DEFAULT 'active',
    created_at  timestamptz         NOT NULL DEFAULT now(),
    CONSTRAINT subscriptions_period CHECK (ends_on IS NULL OR ends_on >= starts_on),
    -- Un membre ne peut pas avoir deux abonnements actifs sur des périodes qui se chevauchent.
    CONSTRAINT subscriptions_no_overlap EXCLUDE USING gist (
        member_id WITH =,
        daterange(starts_on, ends_on, '[]') WITH &&
    ) WHERE (status = 'active')
);
CREATE INDEX subscriptions_member_idx ON subscriptions (member_id);

-- ----------------------------------------------------------------------------
-- bookings : réservations
-- La contrainte bookings_no_overlap interdit physiquement la double réservation :
-- deux réservations actives du même espace ne peuvent pas avoir de plages qui se
-- chevauchent, même si deux requêtes arrivent exactement en même temps.
-- La plage est semi-ouverte [début, fin) : une réservation 10h-11h et une autre
-- 11h-12h sont donc compatibles.
-- ----------------------------------------------------------------------------
CREATE TABLE bookings (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    member_id   bigint         NOT NULL REFERENCES members (id) ON DELETE RESTRICT,
    space_id    bigint         NOT NULL REFERENCES spaces (id)  ON DELETE RESTRICT,
    start_at    timestamptz    NOT NULL,
    end_at      timestamptz    NOT NULL,
    status      booking_status NOT NULL DEFAULT 'confirmed',
    price       numeric(8, 2)  NOT NULL DEFAULT 0,
    version     integer        NOT NULL DEFAULT 1,
    created_at  timestamptz    NOT NULL DEFAULT now(),
    updated_at  timestamptz    NOT NULL DEFAULT now(),
    CONSTRAINT bookings_order CHECK (end_at > start_at),
    CONSTRAINT bookings_price_not_negative CHECK (price >= 0),
    CONSTRAINT bookings_no_overlap EXCLUDE USING gist (
        space_id WITH =,
        tstzrange(start_at, end_at, '[)') WITH &&
    ) WHERE (status IN ('pending', 'confirmed', 'used'))
);
CREATE FUNCTION touch_updated_at() RETURNS trigger AS $$
BEGIN
    NEW.updated_at := clock_timestamp();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER bookings_touch_updated_at
    BEFORE UPDATE ON bookings
    FOR EACH ROW EXECUTE FUNCTION touch_updated_at();

CREATE INDEX bookings_member_start_idx ON bookings (member_id, start_at DESC);
-- Sert au job de rappels : réservations confirmées à venir.
CREATE INDEX bookings_upcoming_confirmed_idx ON bookings (start_at) WHERE status = 'confirmed';

-- ----------------------------------------------------------------------------
-- notifications : confirmations, rappels et annulations envoyés aux membres
-- ----------------------------------------------------------------------------
CREATE TABLE notifications (
    id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_id  bigint              NOT NULL REFERENCES bookings (id) ON DELETE CASCADE,
    type        notification_type   NOT NULL,
    channel     text                NOT NULL DEFAULT 'email',
    status      notification_status NOT NULL DEFAULT 'pending',
    scheduled_at timestamptz        NOT NULL DEFAULT now(),
    sent_at     timestamptz,
    error       text,
    -- Un seul rappel (ou une seule confirmation) par réservation et par canal.
    CONSTRAINT notifications_unique UNIQUE (booking_id, type, channel)
);
CREATE INDEX notifications_pending_idx ON notifications (scheduled_at) WHERE status = 'pending';

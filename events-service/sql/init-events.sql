CREATE TABLE IF NOT EXISTS users (
    id          uuid PRIMARY KEY,
    user_name   varchar(50) NOT NULL UNIQUE,
    avatar      varchar(255),
    city        varchar(100),
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS events (
    id               bigserial PRIMARY KEY,
    owner_id         uuid         NOT NULL,
    title            varchar(150) NOT NULL,
    description      text,
    type             varchar(20)  NOT NULL,
    city             varchar(100) NOT NULL,
    address          varchar(255) NOT NULL,
    visibility       varchar(20)  NOT NULL,
    status           varchar(20)  NOT NULL,
    max_members      integer,
    members_count    integer      NOT NULL DEFAULT 0,
    price            integer,
    payment          varchar(20),
    event_start_at   timestamptz  NOT NULL,
    event_end_at     timestamptz,
    post_date        timestamptz  NOT NULL DEFAULT now(),
    is_recurring     boolean      NOT NULL DEFAULT false,
    recurrence_rule  varchar(255),
    call_link        varchar(255),
    likes_count      integer      NOT NULL DEFAULT 0,
    view_count       integer      NOT NULL DEFAULT 0,
    created_at       timestamptz  NOT NULL DEFAULT now(),
    updated_at       timestamptz  NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_events_city_status_start_at
    ON events (city, status, event_start_at);

CREATE TABLE IF NOT EXISTS event_members (
    id          bigserial PRIMARY KEY,
    event_id    bigint  NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    user_id     uuid    NOT NULL,
    status      varchar(20) NOT NULL,
    role        varchar(20) NOT NULL,
    joined_at   timestamptz NOT NULL DEFAULT now(),
    UNIQUE (event_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_event_members_user_status
    ON event_members (user_id, status);

CREATE TABLE IF NOT EXISTS liked_posts (
    id          bigserial PRIMARY KEY,
    user_id     uuid   NOT NULL,
    event_id    bigint NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    created_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (user_id, event_id)
);

CREATE TABLE IF NOT EXISTS saved_posts (
    id          bigserial PRIMARY KEY,
    user_id     uuid   NOT NULL,
    event_id    bigint NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    created_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (user_id, event_id)
);

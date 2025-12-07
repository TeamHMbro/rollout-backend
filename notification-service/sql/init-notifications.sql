CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS notifications (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         uuid        NOT NULL,
    type            varchar(50) NOT NULL,
    title           varchar(200) NOT NULL,
    body            text        NOT NULL,
    payload         jsonb       NULL,
    status          varchar(20) NOT NULL DEFAULT 'pending',
    created_at      timestamptz NOT NULL DEFAULT now(),
    sent_at         timestamptz NULL,
    read_at         timestamptz NULL
);

CREATE INDEX IF NOT EXISTS ix_notifications_user_status_created_at
    ON notifications (user_id, status, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_notifications_status_created_at
    ON notifications (status, created_at DESC);

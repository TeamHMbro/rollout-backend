docker exec -i rollout-postgres psql -U "rollout" -d notifications_db <<'SQL'
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

DROP TABLE IF EXISTS public.notifications CASCADE;

CREATE TABLE public.notifications (
    id          BIGSERIAL PRIMARY KEY,
    user_id     UUID NOT NULL,
    type        VARCHAR(50) NOT NULL,
    payload     JSONB NOT NULL,
    is_read     BOOLEAN NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    read_at     TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_notifications_user_is_read_created_at
    ON public.notifications (user_id, is_read, created_at DESC);
SQL
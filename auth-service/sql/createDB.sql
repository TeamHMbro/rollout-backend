CREATE ROLE auth_user WITH LOGIN PASSWORD 'CHANGE_ME_STRONG_PASSWORD';

CREATE DATABASE auth_db OWNER auth_user;

GRANT ALL PRIVILEGES ON DATABASE auth_db TO auth_user;

\connect auth_db

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE auth_users (
    id             uuid PRIMARY KEY,
    email          varchar(255) UNIQUE,
    phone          varchar(20) UNIQUE,
    password_hash  varchar(255) NOT NULL,
    status         varchar(20)  NOT NULL DEFAULT 'active',
    created_at     timestamptz  NOT NULL DEFAULT now(),
    updated_at     timestamptz  NOT NULL DEFAULT now(),
    CHECK (email IS NOT NULL OR phone IS NOT NULL)
);

CREATE TABLE auth_providers (
    id           bigserial PRIMARY KEY,
    user_id      uuid         NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
    provider     varchar(50)  NOT NULL,
    provider_id  varchar(255) NOT NULL,
    created_at   timestamptz  NOT NULL DEFAULT now(),
    UNIQUE (provider, provider_id)
);

CREATE TABLE auth_refresh_tokens (
    id          bigserial PRIMARY KEY,
    user_id     uuid         NOT NULL REFERENCES auth_users(id) ON DELETE CASCADE,
    token       varchar(255) NOT NULL UNIQUE,
    expires_at  timestamptz  NOT NULL,
    created_at  timestamptz  NOT NULL DEFAULT now(),
    revoked_at  timestamptz
);

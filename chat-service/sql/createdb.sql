    CREATE DATABASE chat_db
        WITH ENCODING='UTF8'
            LC_COLLATE='en_US.utf8'
            LC_CTYPE='en_US.utf8'
            TEMPLATE=template0;

CREATE TABLE event_messages (
    id          bigserial PRIMARY KEY,
    event_id    bigint      NOT NULL,
    user_id     uuid        NOT NULL,
    content     text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    edited_at   timestamptz,
    is_deleted  boolean     NOT NULL DEFAULT false
);

CREATE INDEX ix_event_messages_event_id_created_at
    ON event_messages (event_id, created_at);

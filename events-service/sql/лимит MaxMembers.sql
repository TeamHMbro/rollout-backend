
ALTER TABLE events
    ADD CONSTRAINT events_members_count_nonnegative
        CHECK (members_count >= 0);

ALTER TABLE events
    ADD CONSTRAINT events_members_not_exceed_max
        CHECK (max_members IS NULL OR members_count <= max_members);

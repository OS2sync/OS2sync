ALTER TABLE queue_users ADD COLUMN person_uuid VARCHAR(36) NULL;
ALTER TABLE success_users ADD COLUMN person_uuid VARCHAR(36) NULL;
ALTER TABLE failure_users ADD COLUMN person_uuid VARCHAR(36) NULL;

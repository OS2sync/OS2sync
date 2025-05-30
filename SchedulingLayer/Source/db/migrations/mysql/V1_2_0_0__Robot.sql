ALTER TABLE queue_users ADD COLUMN is_robot BOOLEAN NOT NULL DEFAULT FALSE AFTER person_uuid;
ALTER TABLE success_users ADD COLUMN is_robot BOOLEAN NULL AFTER person_uuid;
ALTER TABLE failure_users ADD COLUMN is_robot BOOLEAN NULL AFTER person_uuid;

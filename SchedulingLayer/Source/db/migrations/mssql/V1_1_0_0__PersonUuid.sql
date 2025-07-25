ALTER TABLE queue_users ADD person_uuid VARCHAR(36) NULL;
ALTER TABLE success_users ADD person_uuid VARCHAR(36) NULL;
ALTER TABLE failure_users ADD person_uuid VARCHAR(36) NULL;

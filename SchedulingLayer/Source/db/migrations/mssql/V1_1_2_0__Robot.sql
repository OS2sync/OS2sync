ALTER TABLE queue_users ADD COLUMN is_robot BIT NOT NULL DEFAULT 0;
ALTER TABLE success_users ADD COLUMN is_robot BIT NULL;
ALTER TABLE failure_users ADD COLUMN is_robot BIT NULL;

ALTER TABLE queue_users ADD is_robot BIT NOT NULL DEFAULT 0;
ALTER TABLE success_users ADD is_robot BIT NULL;
ALTER TABLE failure_users ADD is_robot BIT NULL;

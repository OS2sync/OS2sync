ALTER TABLE failure_users ADD COLUMN landline VARCHAR(200) AFTER racfid;
ALTER TABLE success_users ADD COLUMN landline VARCHAR(200);
ALTER TABLE queue_users ADD COLUMN landline VARCHAR(200);

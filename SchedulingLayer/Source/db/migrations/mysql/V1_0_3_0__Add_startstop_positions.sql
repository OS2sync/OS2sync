ALTER TABLE queue_user_positions ADD COLUMN start_date VARCHAR(10);
ALTER TABLE queue_user_positions ADD COLUMN stop_date VARCHAR(10);

ALTER TABLE failure_user_positions ADD COLUMN start_date VARCHAR(10);
ALTER TABLE failure_user_positions ADD COLUMN stop_date VARCHAR(10);

ALTER TABLE success_user_positions ADD COLUMN start_date VARCHAR(10);
ALTER TABLE success_user_positions ADD COLUMN stop_date VARCHAR(10);

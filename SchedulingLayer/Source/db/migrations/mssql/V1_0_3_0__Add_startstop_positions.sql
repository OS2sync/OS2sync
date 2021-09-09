ALTER TABLE queue_user_positions ADD start_date NVARCHAR(10);
ALTER TABLE queue_user_positions ADD stop_date NVARCHAR(10);

ALTER TABLE failure_user_positions ADD start_date NVARCHAR(10);
ALTER TABLE failure_user_positions ADD stop_date NVARCHAR(10);

ALTER TABLE success_user_positions ADD start_date NVARCHAR(10);
ALTER TABLE success_user_positions ADD stop_date NVARCHAR(10);

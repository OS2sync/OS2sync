ALTER TABLE failure_users ADD fmk_id NVARCHAR(200);
ALTER TABLE success_users ADD fmk_id NVARCHAR(200);
ALTER TABLE queue_users ADD fmk_id NVARCHAR(200);

ALTER TABLE queue_orgunits ADD foa NVARCHAR(200);
ALTER TABLE queue_orgunits ADD pnr NVARCHAR(200);
ALTER TABLE queue_orgunits ADD sor NVARCHAR(200);

ALTER TABLE failure_orgunits ADD foa NVARCHAR(200);
ALTER TABLE failure_orgunits ADD pnr NVARCHAR(200);
ALTER TABLE failure_orgunits ADD sor NVARCHAR(200);

ALTER TABLE success_orgunits ADD foa NVARCHAR(200);
ALTER TABLE success_orgunits ADD pnr NVARCHAR(200);
ALTER TABLE success_orgunits ADD sor NVARCHAR(200);

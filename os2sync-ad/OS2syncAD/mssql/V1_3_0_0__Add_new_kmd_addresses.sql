ALTER TABLE queue_orgunits ADD losid NVARCHAR(200);
ALTER TABLE success_orgunits ADD losid NVARCHAR(200);
ALTER TABLE failure_orgunits ADD losid NVARCHAR(200);
ALTER TABLE queue_users ADD racfid NVARCHAR(200);
ALTER TABLE success_users ADD racfid NVARCHAR(200);
ALTER TABLE failure_users ADD racfid NVARCHAR(200);

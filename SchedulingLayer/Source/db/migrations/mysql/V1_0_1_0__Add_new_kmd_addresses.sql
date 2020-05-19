ALTER TABLE queue_orgunits ADD COLUMN losid VARCHAR(200);
ALTER TABLE success_orgunits ADD COLUMN losid VARCHAR(200);
ALTER TABLE failure_orgunits ADD COLUMN losid VARCHAR(200);
ALTER TABLE queue_users ADD COLUMN racfid VARCHAR(200);
ALTER TABLE success_users ADD COLUMN racfid VARCHAR(200);
ALTER TABLE failure_users ADD COLUMN racfid VARCHAR(200);

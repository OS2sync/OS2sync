CREATE TABLE queue_orgunits_it_systems (
	id						BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
	unit_id					BIGINT NOT NULL,
	it_system_uuid			NVARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id)	REFERENCES queue_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE success_orgunits_it_systems (
	id						BIGINT,
	unit_id					BIGINT,
	it_system_uuid			NVARCHAR(36)
);

CREATE TABLE failure_orgunits_it_systems (
	id						BIGINT,
	unit_id					BIGINT,
	it_system_uuid			NVARCHAR(36)
);

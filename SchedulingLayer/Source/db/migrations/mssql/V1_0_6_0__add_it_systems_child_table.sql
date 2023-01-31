CREATE TABLE queue_orgunits_it_systems (
	id						BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	unit_id					BIGINT NOT NULL FOREIGN KEY REFERENCES queue_orgunits(id) ON DELETE CASCADE,
	it_system_uuid			NVARCHAR(36) NOT NULL
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

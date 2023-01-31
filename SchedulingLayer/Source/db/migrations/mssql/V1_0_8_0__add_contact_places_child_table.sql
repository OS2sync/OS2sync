CREATE TABLE queue_orgunits_contact_places (
	id						BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	unit_id					BIGINT NOT NULL FOREIGN KEY REFERENCES queue_orgunits(id) ON DELETE CASCADE,
	contact_place_uuid		NVARCHAR(36) NOT NULL
);

CREATE TABLE success_orgunits_contact_places (
	id						BIGINT,
	unit_id					BIGINT,
	contact_place_uuid		NVARCHAR(36)
);

CREATE TABLE failure_orgunits_contact_places (
	id						BIGINT,
	unit_id					BIGINT,
	contact_place_uuid		NVARCHAR(36)
);

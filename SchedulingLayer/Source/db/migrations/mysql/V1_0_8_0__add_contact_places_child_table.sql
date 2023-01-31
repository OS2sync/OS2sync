CREATE TABLE queue_orgunits_contact_places (
	id						BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
	unit_id					BIGINT NOT NULL,
	contact_place_uuid		NVARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id)	REFERENCES queue_orgunits(id) ON DELETE CASCADE
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

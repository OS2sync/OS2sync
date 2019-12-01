CREATE TABLE queue_orgunits (
	id                          BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
	timestamp                   DATETIME NOT NULL DEFAULT NOW(),
	uuid                        NVARCHAR(36) NOT NULL,
	shortkey                    NVARCHAR(50),
	name                        NVARCHAR(200),
	parent_ou_uuid              NVARCHAR(36),
	payout_ou_uuid              NVARCHAR(36),
	manager_uuid                NVARCHAR(36),
	orgunit_type                NVARCHAR(64),
	los_shortname               NVARCHAR(200),
	phone_number                NVARCHAR(200),
	email                       NVARCHAR(200),
	location                    NVARCHAR(200),
	ean                         NVARCHAR(200),
	url                         NVARCHAR(200),
	landline                    NVARCHAR(200),
	post_address                NVARCHAR(200),
	contact_open_hours          NVARCHAR(200),
	email_remarks               NVARCHAR(200),
	contact                     NVARCHAR(200),
	post_return                 NVARCHAR(200),
	phone_open_hours            NVARCHAR(200),
	cvr                         NVARCHAR(8) NOT NULL,
	operation                   NVARCHAR(16) NOT NULL
);

CREATE TABLE queue_orgunits_tasks (
	id						BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
	unit_id					BIGINT NOT NULL,
	task					NVARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id)	REFERENCES queue_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE queue_orgunits_contact_for_tasks (
	id						BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
	unit_id					BIGINT NOT NULL,
	task					NVARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id)	REFERENCES queue_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE queue_users (
	id                      BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
	timestamp               DATETIME NOT NULL DEFAULT NOW(),
	uuid                    NVARCHAR(36) NOT NULL,
	shortkey                NVARCHAR(50),
	user_id                 NVARCHAR(200),
	phone_number            NVARCHAR(200),
	email                   NVARCHAR(200),
	location                NVARCHAR(200),
	name                    NVARCHAR(100),
	cpr                     NVARCHAR(10),
	cvr                     NVARCHAR(8) NOT NULL,
	operation               NVARCHAR(16) NOT NULL
);

CREATE TABLE queue_user_positions (
	id                      BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
	user_id                 BIGINT NOT NULL,
	name                    NVARCHAR(200) NOT NULL,
	orgunit_uuid            NVARCHAR(36) NOT NULL,

	FOREIGN KEY (user_id)	REFERENCES queue_users(id) ON DELETE CASCADE
);

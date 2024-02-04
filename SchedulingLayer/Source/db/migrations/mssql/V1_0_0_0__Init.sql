CREATE TABLE queue_orgunits (
	id                          BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	timestamp                   DATETIME2 NOT NULL DEFAULT GETDATE(),
	uuid                        VARCHAR(36) NOT NULL,
	priority                    BIGINT NOT NULL DEFAULT 10,
	shortkey                    VARCHAR(50),
	name                        VARCHAR(255),
	parent_ou_uuid              VARCHAR(36),
	payout_ou_uuid              VARCHAR(36),
	manager_uuid                VARCHAR(36),
	orgunit_type                VARCHAR(64),
	los_shortname               VARCHAR(255),
	phone_number                VARCHAR(255),
	email                       VARCHAR(255),
	location                    VARCHAR(255),
	ean                         VARCHAR(255),
	url                         VARCHAR(255),
	landline                    VARCHAR(255),
	post_address                VARCHAR(255),
	post_address_secondary      VARCHAR(255),
	contact_open_hours          VARCHAR(255),
	email_remarks               VARCHAR(255),
	contact                     VARCHAR(255),
	post_return                 VARCHAR(255),
	phone_open_hours            VARCHAR(255),
	losid                       VARCHAR(255),
	dtr_id                      VARCHAR(255),
	fmk_id                      VARCHAR(255),
	foa                         VARCHAR(255),
	pnr                         VARCHAR(255),
	sor                         VARCHAR(255),
	bypass_cache                BIT NOT NULL DEFAULT 0,
	cvr                         VARCHAR(8) NOT NULL,
	operation                   VARCHAR(16) NOT NULL,

    INDEX IDX_queue_orgunits_priority (priority)
);

CREATE TABLE queue_orgunits_contact_places (
	id						BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	unit_id					BIGINT NOT NULL,
	contact_place_uuid		VARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id) REFERENCES queue_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE queue_orgunits_tasks (
	id						BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	unit_id					BIGINT NOT NULL,
	task					VARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id)	REFERENCES queue_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE queue_orgunits_contact_for_tasks (
	id						BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	unit_id					BIGINT NOT NULL,
	task					VARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id)	REFERENCES queue_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE queue_orgunits_it_systems (
	id						BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	unit_id					BIGINT NOT NULL,
	it_system_uuid			VARCHAR(36) NOT NULL,

	FOREIGN KEY (unit_id) REFERENCES queue_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE success_orgunits (
	id                          BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	timestamp                   DATETIME2,
	uuid                        VARCHAR(36),
	shortkey                    VARCHAR(50),
	name                        VARCHAR(255),
	parent_ou_uuid              VARCHAR(36),
	payout_ou_uuid              VARCHAR(36),
	manager_uuid                VARCHAR(36),
	orgunit_type                VARCHAR(64),
	los_shortname               VARCHAR(255),
	phone_number                VARCHAR(255),
	email                       VARCHAR(255),
	location                    VARCHAR(255),
	ean                         VARCHAR(255),
	url                         VARCHAR(255),
	landline                    VARCHAR(255),
	post_address                VARCHAR(255),
	post_address_secondary      VARCHAR(255),
	contact_open_hours          VARCHAR(255),
	email_remarks               VARCHAR(255),
	contact                     VARCHAR(255),
	post_return                 VARCHAR(255),
	phone_open_hours            VARCHAR(255),
	losid                       VARCHAR(255),
    dtr_id                      VARCHAR(255),
	fmk_id                      VARCHAR(255),
	foa                         VARCHAR(255),
	pnr                         VARCHAR(255),
	sor                         VARCHAR(255),
	cvr                         VARCHAR(8),
	operation                   VARCHAR(16),
    skipped                     BIT NOT NULL DEFAULT 0,

	transfered_timestamp        DATETIME2 NOT NULL DEFAULT GETDATE(),

	INDEX IDX_success_orgunits_uuid (uuid)
);

CREATE TABLE success_orgunits_it_systems (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	it_system_uuid			VARCHAR(36),

    FOREIGN KEY (unit_id) REFERENCES success_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE success_orgunits_contact_places (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	contact_place_uuid		VARCHAR(36),

	FOREIGN KEY (unit_id) REFERENCES success_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE success_orgunits_tasks (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	task					VARCHAR(36),

    FOREIGN KEY (unit_id) REFERENCES success_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE success_orgunits_contact_for_tasks (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	task					VARCHAR(36),

    FOREIGN KEY (unit_id) REFERENCES success_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE failure_orgunits (
	id                          BIGINT NOT NULL PRIMARY KEY,
	timestamp                   DATETIME2,
	uuid                        VARCHAR(36),
	shortkey                    VARCHAR(50),
	name                        VARCHAR(255),
	parent_ou_uuid              VARCHAR(36),
	payout_ou_uuid              VARCHAR(36),
	manager_uuid                VARCHAR(36),
	orgunit_type                VARCHAR(64),
	los_shortname               VARCHAR(255),
	phone_number                VARCHAR(255),
	email                       VARCHAR(255),
	location                    VARCHAR(255),
	ean                         VARCHAR(255),
	url                         VARCHAR(255),
	landline                    VARCHAR(255),
	post_address                VARCHAR(255),
	post_address_secondary      VARCHAR(255),
	contact_open_hours          VARCHAR(255),
	email_remarks               VARCHAR(255),
	contact                     VARCHAR(255),
	post_return                 VARCHAR(255),
	phone_open_hours            VARCHAR(255),
	losid                       VARCHAR(255),
	dtr_id                      VARCHAR(255),
	fmk_id                      VARCHAR(255),
	foa                         VARCHAR(255),
	pnr                         VARCHAR(255),
	sor                         VARCHAR(255),
	cvr                         VARCHAR(8),
	operation                   VARCHAR(16),

	failed_timestamp            DATETIME2 NOT NULL DEFAULT GETDATE(),
	error						TEXT
);

CREATE TABLE failure_orgunits_it_systems (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	it_system_uuid			VARCHAR(36),

    FOREIGN KEY (unit_id) REFERENCES failure_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE failure_orgunits_contact_places (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	contact_place_uuid		VARCHAR(36),

    FOREIGN KEY (unit_id) REFERENCES failure_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE failure_orgunits_tasks (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	task					VARCHAR(36),

    FOREIGN KEY (unit_id) REFERENCES failure_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE failure_orgunits_contact_for_tasks (
	id						BIGINT PRIMARY KEY,
	unit_id					BIGINT,
	task					VARCHAR(36),

    FOREIGN KEY (unit_id) REFERENCES failure_orgunits(id) ON DELETE CASCADE
);

CREATE TABLE queue_users (
	id                      BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	timestamp               DATETIME2 NOT NULL DEFAULT GETDATE(),
	uuid                    VARCHAR(36) NOT NULL,
	priority                BIGINT NOT NULL DEFAULT 10,
	shortkey                VARCHAR(50),
	user_id                 VARCHAR(255),
	phone_number            VARCHAR(255),
	email                   VARCHAR(255),
	location                VARCHAR(255),
	racfid                  VARCHAR(255),
	landline                VARCHAR(255),
	name                    VARCHAR(255),
	cpr                     VARCHAR(10),
    bypass_cache            BIT NOT NULL DEFAULT 0,
	cvr                     VARCHAR(8) NOT NULL,
	operation               VARCHAR(16) NOT NULL,

    INDEX IDX_queue_orgunits_priority (priority)
);

CREATE TABLE queue_user_positions (
	id                      BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	user_id                 BIGINT NOT NULL,
	name                    VARCHAR(255) NOT NULL,
	orgunit_uuid            VARCHAR(36) NOT NULL,
	start_date              VARCHAR(10),
	stop_date               VARCHAR(10),

	FOREIGN KEY (user_id)	REFERENCES queue_users(id) ON DELETE CASCADE
);

CREATE TABLE success_users (
	id                      BIGINT NOT NULL PRIMARY KEY,
	timestamp               DATETIME2,
	uuid                    VARCHAR(36),
	shortkey                VARCHAR(50),
	user_id                 VARCHAR(255),
	phone_number            VARCHAR(255),
	email                   VARCHAR(255),
	location                VARCHAR(255),
	racfid                  VARCHAR(255),
	landline                VARCHAR(255),
	name                    VARCHAR(255),
	cpr                     VARCHAR(10),
	cvr                     VARCHAR(8),
	operation               VARCHAR(16),
    skipped                 BIT NOT NULL DEFAULT 0,

	transfered_timestamp    DATETIME2 NOT NULL DEFAULT GETDATE(),

	INDEX IDX_success_users_uuid (uuid)
);

CREATE TABLE success_user_positions (
	id                      BIGINT PRIMARY KEY,
	user_id                 BIGINT,
	name                    VARCHAR(200),
	orgunit_uuid            VARCHAR(36),
	start_date              VARCHAR(10),
	stop_date               VARCHAR(10),

	FOREIGN KEY (user_id) REFERENCES success_users(id) ON DELETE CASCADE
);

CREATE TABLE failure_users (
	id                      BIGINT NOT NULL PRIMARY KEY,
	timestamp               DATETIME2,
	uuid                    VARCHAR(36),
	shortkey                VARCHAR(50),
	user_id                 VARCHAR(255),
	phone_number            VARCHAR(255),
	email                   VARCHAR(255),
	location                VARCHAR(255),
	racfid                  VARCHAR(255),
	landline                VARCHAR(255),
	name                    VARCHAR(255),
	cpr                     VARCHAR(10),
	cvr                     VARCHAR(8),
	operation               VARCHAR(16),

	transfered_timestamp    DATETIME2 NOT NULL DEFAULT GETDATE(),
	error					TEXT
);

CREATE TABLE failure_user_positions (
	id                      BIGINT PRIMARY KEY,
	user_id                 BIGINT,
	name                    VARCHAR(200),
	orgunit_uuid            VARCHAR(36),
	start_date              VARCHAR(10),
	stop_date               VARCHAR(10),

	FOREIGN KEY (user_id) REFERENCES failure_users(id) ON DELETE CASCADE
);

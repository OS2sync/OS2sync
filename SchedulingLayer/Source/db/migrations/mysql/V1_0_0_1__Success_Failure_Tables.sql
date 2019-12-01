CREATE TABLE success_orgunits (
	id                          BIGINT,
	timestamp                   DATETIME,
	uuid                        NVARCHAR(36),
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
	cvr                         NVARCHAR(8),
	operation                   NVARCHAR(16)
);

CREATE TABLE success_orgunits_tasks (
	id						BIGINT,
	unit_id					BIGINT,
	task					NVARCHAR(36)
);

CREATE TABLE success_orgunits_contact_for_tasks (
	id						BIGINT,
	unit_id					BIGINT,
	task					NVARCHAR(36)
);

CREATE TABLE success_users (
	id                      BIGINT,
	timestamp               DATETIME,
	uuid                    NVARCHAR(36),
	shortkey                NVARCHAR(50),
	user_id                 NVARCHAR(200),
	phone_number            NVARCHAR(200),
	email                   NVARCHAR(200),
	location                NVARCHAR(200),
	name                    NVARCHAR(100),
	cpr                     NVARCHAR(10),
	cvr                     NVARCHAR(8),
	operation               NVARCHAR(16)
);

CREATE TABLE success_user_positions (
	id                      BIGINT,
	user_id                 BIGINT,
	name                    NVARCHAR(200),
	orgunit_uuid            NVARCHAR(36)
);

CREATE TABLE failure_orgunits (
	id                          BIGINT,
	timestamp                   DATETIME,
	uuid                        NVARCHAR(36),
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
	cvr                         NVARCHAR(8),
	operation                   NVARCHAR(16),
	error						TEXT
);

CREATE TABLE failure_orgunits_tasks (
	id						BIGINT,
	unit_id					BIGINT,
	task					NVARCHAR(36)
);

CREATE TABLE failure_orgunits_contact_for_tasks (
	id						BIGINT,
	unit_id					BIGINT,
	task					NVARCHAR(36)
);

CREATE TABLE failure_users (
	id                      BIGINT,
	timestamp               DATETIME,
	uuid                    NVARCHAR(36),
	shortkey                NVARCHAR(50),
	user_id                 NVARCHAR(200),
	phone_number            NVARCHAR(200),
	email                   NVARCHAR(200),
	location                NVARCHAR(200),
	name                    NVARCHAR(200),
	cpr                     NVARCHAR(10),
	cvr                     NVARCHAR(8),
	operation               NVARCHAR(16),
	error					TEXT
);

CREATE TABLE failure_user_positions (
	id                      BIGINT,
	user_id                 BIGINT,
	name                    NVARCHAR(200),
	orgunit_uuid            NVARCHAR(36)
);
DROP TABLE success_orgunits;
CREATE TABLE success_orgunits (
	id                          BIGINT,
	timestamp                   DATETIME2,
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
    losid                       NVARCHAR(200)
);

DROP TABLE success_users;
CREATE TABLE success_users (
	id                      BIGINT,
	timestamp               DATETIME2,
	uuid                    NVARCHAR(36),
	shortkey                NVARCHAR(50),
	user_id                 NVARCHAR(200),
	phone_number            NVARCHAR(200),
	email                   NVARCHAR(200),
	location                NVARCHAR(200),
	name                    NVARCHAR(100),
	cpr                     NVARCHAR(10),
	cvr                     NVARCHAR(8),
	operation               NVARCHAR(16),
    racfid                  NVARCHAR(200)
);

DROP TABLE failure_orgunits;
CREATE TABLE failure_orgunits (
	id                          BIGINT,
	timestamp                   DATETIME2,
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
    losid                       NVARCHAR(200),
	error						TEXT
);

DROP TABLE failure_users;
CREATE TABLE failure_users (
	id                      BIGINT,
	timestamp               DATETIME2,
	uuid                    NVARCHAR(36),
	shortkey                NVARCHAR(50),
	user_id                 NVARCHAR(200),
	phone_number            NVARCHAR(200),
	email                   NVARCHAR(200),
	location                NVARCHAR(200),
	name                    NVARCHAR(100),
	cpr                     NVARCHAR(10),
	cvr                     NVARCHAR(8),
	operation               NVARCHAR(16),
    racfid                  NVARCHAR(200),
	error					TEXT
);

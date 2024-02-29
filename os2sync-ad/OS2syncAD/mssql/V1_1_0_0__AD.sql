CREATE TABLE records (
  id                    BIGINT NOT NULL PRIMARY KEY IDENTITY(1, 1),
  record_key            NVARCHAR(128) NOT NULL,
  record_value          NVARCHAR(4000) NOT NULL,
  record_timestamp      DATETIME2 NOT NULL DEFAULT GETDATE()
);

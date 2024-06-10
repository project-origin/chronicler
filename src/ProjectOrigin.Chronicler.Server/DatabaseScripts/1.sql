CREATE TABLE claim_intents (
    id uuid NOT NULL PRIMARY KEY,
    registry_name VARCHAR(64) NOT NULL,
    certificate_id uuid NOT NULL,
    commitment bytea NOT NULL,
    quantity bigint NOT NULL,
    random_r bytea NOT NULL
);

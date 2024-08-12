CREATE TABLE claim_intents (
    id uuid NOT NULL PRIMARY KEY,
    registry_name VARCHAR(64) NOT NULL,
    certificate_id uuid NOT NULL,
    commitment_hash bytea NOT NULL,
    quantity bigint NOT NULL,
    random_r bytea NOT NULL
);

CREATE TABLE read_blocks (
    registry_name  VARCHAR(64) NOT NULL PRIMARY KEY,
    block_height int NOT NULL,
    read_at timestamp with time zone NOT NULL
);

CREATE OR REPLACE FUNCTION insert_read_block()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM 1 FROM read_blocks WHERE registry_name = NEW.registry_name;

    IF NOT FOUND THEN
        INSERT INTO read_blocks (registry_name, block_height, read_at)
        VALUES (NEW.registry_name, -1, NOW());
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER insert_read_block_trigger
AFTER INSERT ON claim_intents
FOR EACH ROW
EXECUTE FUNCTION insert_read_block();

CREATE TABLE certificate_infos (
    registry_name VARCHAR(64) NOT NULL,
    certificate_id uuid NOT NULL,
    start_time timestamp with time zone NOT NULL,
    end_time timestamp with time zone NOT NULL,
    grid_area VARCHAR(64) NOT NULL,
    PRIMARY KEY (registry_name, certificate_id)
);

CREATE TABLE claim_allocations  (
    id uuid NOT NULL PRIMARY KEY,
    claim_intent_id uuid NOT NULL,
    registry_name VARCHAR(64) NOT NULL,
    certificate_id uuid NOT NULL,
    allocation_id uuid NOT NULL
);

CREATE TABLE claim_records (
    id uuid NOT NULL PRIMARY KEY,
    registry_name VARCHAR(64) NOT NULL,
    certificate_id uuid NOT NULL,
    quantity bigint NOT NULL,
    random_r bytea NOT NULL
);

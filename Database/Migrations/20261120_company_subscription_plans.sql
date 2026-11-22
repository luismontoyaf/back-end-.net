BEGIN;

ALTER TABLE companies
    ADD COLUMN subscription_plan VARCHAR(20) NOT NULL DEFAULT 'plus';

ALTER TABLE companies
    ALTER COLUMN subscription_plan SET DEFAULT 'standard';

ALTER TABLE companies
    ADD CONSTRAINT ck_companies_subscription_plan
    CHECK (subscription_plan IN ('standard', 'plus'));

COMMIT;

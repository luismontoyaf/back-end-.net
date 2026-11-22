BEGIN;

ALTER TABLE company_plan_upgrade_requests
    DROP CONSTRAINT ck_company_plan_upgrade_requests_status;

ALTER TABLE company_plan_upgrade_requests
    ADD CONSTRAINT ck_company_plan_upgrade_requests_status
    CHECK (status IN ('pending', 'approved', 'rejected'));

COMMIT;

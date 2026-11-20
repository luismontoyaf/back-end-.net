BEGIN;

CREATE TABLE company_plan_upgrade_requests (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    requested_by_email VARCHAR(255) NOT NULL,
    requested_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    decided_at TIMESTAMP WITH TIME ZONE NULL,
    decided_by_email VARCHAR(255) NULL,
    CONSTRAINT ck_company_plan_upgrade_requests_status
        CHECK (status IN ('pending', 'approved'))
);

CREATE UNIQUE INDEX ux_company_plan_upgrade_requests_pending
    ON company_plan_upgrade_requests(company_id)
    WHERE status = 'pending';

COMMIT;

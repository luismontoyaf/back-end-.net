BEGIN;

CREATE TABLE companies (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    nit VARCHAR(50) NOT NULL DEFAULT '',
    email VARCHAR(255) NOT NULL DEFAULT '',
    phone VARCHAR(50) NOT NULL DEFAULT '',
    address VARCHAR(500) NOT NULL DEFAULT '',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    suspended_for_overdue BOOLEAN NOT NULL DEFAULT FALSE,
    subscription_start_date DATE NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

ALTER TABLE tenants
    ADD COLUMN company_id INTEGER NULL,
    ADD COLUMN is_headquarters BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE companies ADD COLUMN source_tenant_id INTEGER NULL;

WITH inserted_companies AS (
    INSERT INTO companies (
        name, nit, email, phone, address, is_active, suspended_for_overdue,
        subscription_start_date, created_at, source_tenant_id
    )
    SELECT
        nombre, COALESCE(nit, ''), COALESCE(correo, ''), COALESCE(celular, ''),
        COALESCE(direccion, ''), estado, bloqueado_por_mora,
        fecha_inicio_suscripcion, COALESCE(fecha_creacion, NOW()), id
    FROM tenants
    WHERE company_id IS NULL
    RETURNING id, source_tenant_id
)
UPDATE tenants AS tenant
SET company_id = inserted_companies.id,
    is_headquarters = TRUE
FROM inserted_companies
WHERE tenant.id = inserted_companies.source_tenant_id;

ALTER TABLE companies DROP COLUMN source_tenant_id;
ALTER TABLE tenants ALTER COLUMN company_id SET NOT NULL;
ALTER TABLE tenants
    ADD CONSTRAINT fk_tenants_company
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE CASCADE;
CREATE INDEX ix_tenants_company_id ON tenants(company_id);
CREATE UNIQUE INDEX ux_tenants_company_headquarters
    ON tenants(company_id) WHERE is_headquarters = TRUE;

ALTER TABLE tenant_subscription_periods ADD COLUMN company_id INTEGER NULL;
UPDATE tenant_subscription_periods AS period
SET company_id = tenant.company_id
FROM tenants AS tenant
WHERE period.tenant_id = tenant.id;
ALTER TABLE tenant_subscription_periods ALTER COLUMN company_id SET NOT NULL;
ALTER TABLE tenant_subscription_periods
    ADD CONSTRAINT fk_tenant_subscription_periods_company
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE CASCADE;
CREATE UNIQUE INDEX ux_subscription_periods_company_cycle_month
    ON tenant_subscription_periods(company_id, cycle_number, month_number);

ALTER TABLE user_refresh_tokens ADD COLUMN active_tenant_id INTEGER NULL;
UPDATE user_refresh_tokens SET active_tenant_id = tenant_id;
ALTER TABLE user_refresh_tokens ALTER COLUMN active_tenant_id SET NOT NULL;

COMMIT;

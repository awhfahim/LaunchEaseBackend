-- Enhanced Permission System for Multi-Tenant Application
-- This extends the existing ACM tables with a robust permission system

-- Master Claims/Permissions table - stores all available permissions in the system
CREATE TABLE IF NOT EXISTS master_claims (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    claim_type VARCHAR(50) NOT NULL DEFAULT 'permission',
    claim_value VARCHAR(100) NOT NULL UNIQUE, -- e.g., 'users.view', 'roles.create'
    display_name VARCHAR(100) NOT NULL, -- Human-readable name
    description TEXT, -- Description of what this permission allows
    category VARCHAR(50) NOT NULL, -- e.g., 'users', 'roles', 'tenants', 'system'
    is_tenant_scoped BOOLEAN NOT NULL DEFAULT TRUE, -- Whether this permission is tenant-specific
    is_system_permission BOOLEAN NOT NULL DEFAULT FALSE, -- Whether this is a system-level permission
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE
);

-- Add indexes for performance
CREATE INDEX IF NOT EXISTS idx_master_claims_category ON master_claims(category);
CREATE INDEX IF NOT EXISTS idx_master_claims_tenant_scoped ON master_claims(is_tenant_scoped);
CREATE INDEX IF NOT EXISTS idx_master_claims_system_permission ON master_claims(is_system_permission);
CREATE INDEX IF NOT EXISTS idx_master_claims_claim_value ON master_claims(claim_value);

insert into public.master_claims_category (id, name, created_at, updated_at)
values  ('c601bdf7-72ea-4d1e-a93f-7e533159c17f', 'system', '2025-07-04 04:47:07.731087', null),
        ('b987b75f-ef41-4068-a805-0b9ac7b2e372', 'auth', '2025-07-04 04:47:07.731087', null),
        ('1e8db075-d02c-41f0-9db7-b0a5e2f478ca', 'tenant', '2025-07-04 04:47:07.731087', null),
        ('551f5d8e-ba6e-43fe-898e-ae5d1092bce7', 'audit', '2025-07-04 04:47:07.731087', null);

insert into public.master_claims (id, claim_type, claim_value, display_name, description, category_id, is_tenant_scoped, is_system_permission, created_at, updated_at)
values  ('0a7020c4-aaa5-4da3-ac38-37597a4363f6', 'permission', 'business.owner', 'Business Owner', 'Full access to all tenants and system operations', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('68e7eebb-b4ae-403d-9a6c-97d9787bb3ec', 'permission', 'system.admin', 'System Administrator', 'System-wide administrative access', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('2fe0a361-0e18-4596-8591-5614f977bb6c', 'permission', 'system.dashboard.view', 'System Dashboard View', 'View system-wide dashboard and metrics', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('d6ffc2ec-0168-4f20-870d-0cff688ed70b', 'permission', 'cross.tenant.access', 'Cross-Tenant Access', 'Access operations across multiple tenants', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('6b6dba11-2532-43b8-946b-2eacfe7a23d0', 'permission', 'tenant.settings.view', 'View Tenant Settings', 'View tenant configuration and settings', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('6fb66b4d-41eb-49ed-bf0a-7d100d94610e', 'permission', 'tenant.settings.edit', 'Edit Tenant Settings', 'Edit tenant configuration and settings', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('0e0ea183-4012-487f-badf-ece4f0f7b23e', 'permission', 'authentication.view', 'View Authentication Settings', 'View authentication configuration', 'b987b75f-ef41-4068-a805-0b9ac7b2e372', true, false, '2025-07-04 04:27:31.097111', null),
        ('5c81f0e0-bc44-4a53-9f1c-d7552cd38e7f', 'permission', 'authentication.edit', 'Edit Authentication Settings', 'Configure authentication settings', 'b987b75f-ef41-4068-a805-0b9ac7b2e372', true, false, '2025-07-04 04:27:31.097111', null),
        ('11cbe7c3-15d4-471b-b453-5ef2930848ca', 'permission', 'authorization.view', 'View Authorization Settings', 'View authorization and permission settings', 'b987b75f-ef41-4068-a805-0b9ac7b2e372', true, false, '2025-07-04 04:27:31.097111', null),
        ('8494dbcd-8d11-42d1-a24c-4d566c26944a', 'permission', 'authorization.edit', 'Edit Authorization Settings', 'Configure authorization and permissions', 'b987b75f-ef41-4068-a805-0b9ac7b2e372', true, false, '2025-07-04 04:27:31.097111', null),
        ('13e742ac-86dc-45be-8426-d80429197847', 'permission', 'audit.view', 'View Audit Logs', 'View audit logs and activity history', '551f5d8e-ba6e-43fe-898e-ae5d1092bce7', true, false, '2025-07-04 04:27:31.097111', null),
        ('fdca541d-cb11-428d-acce-fcd15246e394', 'permission', 'logs.view', 'View System Logs', 'View application logs', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('12fa6762-c6ed-4587-9290-23f7473270cc', 'permission', 'roles.delete', 'Delete Roles', 'Delete roles from the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('515d7b4a-ba9f-4e4a-a16b-80969c9aa264', 'permission', 'users.invite', 'Invite Users', 'Invite new users to the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('da8fc1ed-58f8-4c5d-a71e-b037d7b1519d', 'permission', 'global.tenants.edit', 'Global Tenants Edit', 'Edit any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('982f1c73-b655-49fa-9ae7-670f68315083', 'permission', 'users.view', 'View Users', 'View users within the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('dc1601e1-450c-4f8a-89d1-a102833bf850', 'permission', 'global.roles.create', 'Global Roles Create', 'Create roles in any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('c0d5a1e2-9fa1-4ab0-b4d7-5341200687c6', 'permission', 'global.roles.delete', 'Global Roles Delete', 'Delete any role from any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('c6cd6565-a350-46e8-8d97-fe7cadef37d6', 'permission', 'users.edit', 'Edit Users', 'Edit users within the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('8ad1273a-1de3-4ad7-9dad-8fcbb59d9392', 'permission', 'global.users.edit', 'Global Users Edit', 'Edit any user in any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('28730a92-874d-4395-8fcd-839a70bed184', 'permission', 'global.roles.view', 'Global Roles View', 'View all roles across all tenants', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('2f0d1449-1279-4fc0-85ab-79927121cda4', 'permission', 'roles.view', 'View Roles', 'View roles within the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('0197d3b2-7ecc-76a1-9e7c-583f54570a66', 'permission', 'system.configuration.edit', 'SYSTEM CONFIGURATION EDIT', 'Permission for system configuration edit', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 10:29:39.851883', null),
        ('f67b2ebb-8415-4724-afe3-8a8f498fe993', 'permission', 'global.tenants.delete', 'Global Tenants Delete', 'Delete any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('2f129c35-e718-4973-ac29-6eaeec26f25f', 'permission', 'users.create', 'Create Users', 'Create new users in the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('7e783ee4-5f8b-4386-9c2b-17c6c2c7a6d1', 'permission', 'users.delete', 'Delete Users', 'Delete users from the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('b84c9116-5e8d-491a-9955-3eddab91635e', 'permission', 'roles.edit', 'Edit Roles', 'Edit roles within the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('09646d09-9241-446b-9cce-b096ed96581f', 'permission', 'reports.view', 'View Reports', 'View tenant reports and analytics', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('43545ebb-b7a5-4b31-b58f-9b897376063c', 'permission', 'global.users.delete', 'Global Users Delete', 'Delete any user from any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('d6272e52-0433-4d4d-8c38-84798896b1be', 'permission', 'global.tenants.create', 'Global Tenants Create', 'Create new tenants', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('42f19abe-97ba-4511-ab6c-1c89addd778f', 'permission', 'users.manage.roles', 'Manage User Roles', 'Assign/remove roles from users', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('0f483439-59cd-4ecd-9120-c11df95ff97d', 'permission', 'global.users.view', 'Global Users View', 'View all users across all tenants', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('d662353f-c3c8-430b-9a22-7a836ab05351', 'permission', 'global.roles.edit', 'Global Roles Edit', 'Edit any role in any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('a9499b95-4eb1-4723-b30b-ea12365865ec', 'permission', 'roles.manage.permissions', 'Manage Role Permissions', 'Assign/remove permissions from roles', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('871eabac-3688-4cf9-b25e-0dd230378d82', 'permission', 'dashboard.view', 'View Dashboard', 'Access tenant dashboard', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('ee8af9c4-c421-476e-adfe-960ce66d08f8', 'permission', 'global.users.create', 'Global Users Create', 'Create users in any tenant', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null),
        ('4d31bf20-8224-4853-83f1-cf4b82e2a3fa', 'permission', 'roles.create', 'Create Roles', 'Create new roles in the tenant', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('910ebae5-f479-42da-800a-4c64c9de4f20', 'permission', 'reports.export', 'Export Reports', 'Export reports and data', '1e8db075-d02c-41f0-9db7-b0a5e2f478ca', true, false, '2025-07-04 04:27:31.097111', null),
        ('0197d3b2-7ea4-7064-a253-09a546476af0', 'permission', 'system.logs.view', 'SYSTEM LOGS VIEW', 'Permission for system logs view', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', true, false, '2025-07-04 10:29:39.851797', null),
        ('c56d71b9-a603-4618-9621-bc44a878d329', 'permission', 'global.tenants.view', 'Global Tenants View', 'View all tenants in the system', 'c601bdf7-72ea-4d1e-a93f-7e533159c17f', false, true, '2025-07-04 04:27:31.097111', null);

ON CONFLICT (claim_value) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    description = EXCLUDED.description,
    category = EXCLUDED.category,
    is_tenant_scoped = EXCLUDED.is_tenant_scoped,
    is_system_permission = EXCLUDED.is_system_permission,
    updated_at = NOW();

-- Create a view for easy permission browsing
CREATE OR REPLACE VIEW v_permissions_by_category AS
SELECT 
    category,
    COUNT(*) as permission_count,
    ARRAY_AGG(
        JSON_BUILD_OBJECT(
            'claim_value', claim_value,
            'display_name', display_name,
            'description', description,
            'is_tenant_scoped', is_tenant_scoped,
            'is_system_permission', is_system_permission
        ) ORDER BY display_name
    ) as permissions
FROM master_claims 
GROUP BY category
ORDER BY category;

-- Create a function to get all permissions for a role
CREATE OR REPLACE FUNCTION get_role_permissions(role_uuid UUID)
RETURNS TABLE (
    claim_value VARCHAR(100),
    display_name VARCHAR(100),
    description TEXT,
    category VARCHAR(50)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        mc.claim_value,
        mc.display_name,
        mc.description,
        mc.category
    FROM role_claims rc
    JOIN master_claims mc ON rc.claim_value = mc.claim_value
    WHERE rc.role_id = role_uuid
    ORDER BY mc.category, mc.display_name;
END;
$$ LANGUAGE plpgsql;

-- Create a function to get all permissions for a user in a specific tenant
CREATE OR REPLACE FUNCTION get_user_tenant_permissions(user_uuid UUID, tenant_uuid UUID)
RETURNS TABLE (
    claim_value VARCHAR(100),
    display_name VARCHAR(100),
    description TEXT,
    category VARCHAR(50),
    source VARCHAR(20) -- 'role' or 'direct'
) AS $$
BEGIN
    RETURN QUERY
    -- Permissions from roles
    SELECT DISTINCT
        mc.claim_value,
        mc.display_name,
        mc.description,
        mc.category,
        'role'::VARCHAR(20) as source
    FROM user_roles ur
    JOIN role_claims rc ON ur.role_id = rc.role_id
    JOIN master_claims mc ON rc.claim_value = mc.claim_value
    WHERE ur.user_id = user_uuid AND ur.tenant_id = tenant_uuid
    
    UNION
    
    -- Direct permissions assigned to user
    SELECT DISTINCT
        mc.claim_value,
        mc.display_name,
        mc.description,
        mc.category,
        'direct'::VARCHAR(20) as source
    FROM user_claims uc
    JOIN master_claims mc ON uc.claim_value = mc.claim_value
    WHERE uc.user_id = user_uuid AND uc.tenant_id = tenant_uuid
    
    ORDER BY category, display_name;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- ALTER EXISTING TABLES FOR DATA INTEGRITY
-- ========================================

-- First, clean up any orphaned claims that don't exist in master_claims
-- This ensures we don't have referential integrity issues when adding foreign keys

-- Clean up role_claims that don't have corresponding master_claims
DELETE FROM role_claims 
WHERE claim_value NOT IN (SELECT claim_value FROM master_claims);

-- Clean up user_claims that don't have corresponding master_claims  
DELETE FROM user_claims 
WHERE claim_value NOT IN (SELECT claim_value FROM master_claims);

-- Add foreign key constraint to role_claims table
-- This ensures only valid permissions from master_claims can be assigned to roles
ALTER TABLE role_claims 
ADD CONSTRAINT fk_role_claims_master_claims 
FOREIGN KEY (claim_value) REFERENCES master_claims(claim_value) 
ON DELETE CASCADE ON UPDATE CASCADE;

-- Add foreign key constraint to user_claims table
-- This ensures only valid permissions from master_claims can be assigned to users
ALTER TABLE user_claims 
ADD CONSTRAINT fk_user_claims_master_claims 
FOREIGN KEY (claim_value) REFERENCES master_claims(claim_value) 
ON DELETE CASCADE ON UPDATE CASCADE;

-- Add unique constraint to prevent duplicate role-permission assignments
ALTER TABLE role_claims 
ADD CONSTRAINT uk_role_claims_role_claim 
UNIQUE (role_id, claim_value);

-- Add unique constraint to prevent duplicate user-permission assignments
ALTER TABLE user_claims 
ADD CONSTRAINT uk_user_claims_user_tenant_claim 
UNIQUE (user_id, tenant_id, claim_value);

-- ========================================
-- ENHANCED VALIDATION FUNCTIONS
-- ========================================

-- Function to validate if a claim exists before assignment
CREATE OR REPLACE FUNCTION validate_claim_exists(claim_val VARCHAR(100))
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (SELECT 1 FROM master_claims WHERE claim_value = claim_val);
END;
$$ LANGUAGE plpgsql;

-- Function to get valid tenant-scoped claims for assignment
CREATE OR REPLACE FUNCTION get_tenant_assignable_claims()
RETURNS TABLE (
    claim_value VARCHAR(100),
    display_name VARCHAR(100),
    description TEXT,
    category VARCHAR(50)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        mc.claim_value,
        mc.display_name,
        mc.description,
        mc.category
    FROM master_claims mc
    WHERE mc.is_tenant_scoped = TRUE
    ORDER BY mc.category, mc.display_name;
END;
$$ LANGUAGE plpgsql;

-- Function to get valid system-level claims (for business owners/system admins)
CREATE OR REPLACE FUNCTION get_system_assignable_claims()
RETURNS TABLE (
    claim_value VARCHAR(100),
    display_name VARCHAR(100),
    description TEXT,
    category VARCHAR(50)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        mc.claim_value,
        mc.display_name,
        mc.description,
        mc.category
    FROM master_claims mc
    WHERE mc.is_system_permission = TRUE
    ORDER BY mc.category, mc.display_name;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- TRIGGERS FOR ADDITIONAL VALIDATION
-- ========================================

-- Trigger function to validate tenant-scoped permissions
CREATE OR REPLACE FUNCTION validate_tenant_permission_assignment()
RETURNS TRIGGER AS $$
BEGIN
    -- Check if the claim being assigned is valid for tenant context
    IF NOT EXISTS (
        SELECT 1 FROM master_claims mc 
        WHERE mc.claim_value = NEW.claim_value 
        AND (mc.is_tenant_scoped = TRUE OR mc.is_system_permission = TRUE)
    ) THEN
        RAISE EXCEPTION 'Invalid permission: % is not assignable in tenant context', NEW.claim_value;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to user_claims table
CREATE OR REPLACE TRIGGER trg_validate_user_claims
    BEFORE INSERT OR UPDATE ON user_claims
    FOR EACH ROW EXECUTE FUNCTION validate_tenant_permission_assignment();

-- Trigger function to validate role permission assignment
CREATE OR REPLACE FUNCTION validate_role_permission_assignment()
RETURNS TRIGGER AS $$
DECLARE
    role_tenant_id UUID;
BEGIN
    -- Get the tenant_id for this role
    SELECT tenant_id INTO role_tenant_id 
    FROM roles 
    WHERE id = NEW.role_id;
    
    -- Check if the claim being assigned is valid
    IF NOT EXISTS (
        SELECT 1 FROM master_claims mc 
        WHERE mc.claim_value = NEW.claim_value
    ) THEN
        RAISE EXCEPTION 'Invalid permission: % does not exist in master_claims', NEW.claim_value;
    END IF;
    
    -- Additional validation: system permissions should only be assigned to special roles
    IF EXISTS (
        SELECT 1 FROM master_claims mc 
        WHERE mc.claim_value = NEW.claim_value 
        AND mc.is_system_permission = TRUE
    ) THEN
        -- Log warning or add additional checks here if needed
        -- For now, we'll allow it but could add restrictions
        NULL;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to role_claims table
CREATE OR REPLACE TRIGGER trg_validate_role_claims
    BEFORE INSERT OR UPDATE ON role_claims
    FOR EACH ROW EXECUTE FUNCTION validate_role_permission_assignment();

-- ========================================
-- UTILITY FUNCTIONS FOR PERMISSION MANAGEMENT
-- ========================================

-- Function to safely assign permission to role (with validation)
CREATE OR REPLACE FUNCTION assign_permission_to_role(
    p_role_id UUID,
    p_claim_value VARCHAR(100)
)
RETURNS BOOLEAN AS $$
DECLARE
    claim_exists BOOLEAN;
BEGIN
    -- Check if claim exists
    SELECT EXISTS(SELECT 1 FROM master_claims WHERE claim_value = p_claim_value) INTO claim_exists;
    
    IF NOT claim_exists THEN
        RAISE EXCEPTION 'Permission % does not exist in master_claims', p_claim_value;
    END IF;
    
    -- Insert if not already exists
    INSERT INTO role_claims (id, role_id, claim_type, claim_value)
    VALUES (gen_random_uuid(), p_role_id, 'permission', p_claim_value)
    ON CONFLICT (role_id, claim_value) DO NOTHING;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- Function to safely assign permission to user (with validation)
CREATE OR REPLACE FUNCTION assign_permission_to_user(
    p_user_id UUID,
    p_tenant_id UUID,
    p_claim_value VARCHAR(100)
)
RETURNS BOOLEAN AS $$
DECLARE
    claim_exists BOOLEAN;
    is_tenant_valid BOOLEAN;
BEGIN
    -- Check if claim exists
    SELECT EXISTS(SELECT 1 FROM master_claims WHERE claim_value = p_claim_value) INTO claim_exists;
    
    IF NOT claim_exists THEN
        RAISE EXCEPTION 'Permission % does not exist in master_claims', p_claim_value;
    END IF;
    
    -- Check if it's appropriate for tenant assignment
    SELECT EXISTS(
        SELECT 1 FROM master_claims 
        WHERE claim_value = p_claim_value 
        AND (is_tenant_scoped = TRUE OR is_system_permission = TRUE)
    ) INTO is_tenant_valid;
    
    IF NOT is_tenant_valid THEN
        RAISE EXCEPTION 'Permission % is not valid for tenant assignment', p_claim_value;
    END IF;
    
    -- Insert if not already exists
    INSERT INTO user_claims (id, user_id, tenant_id, claim_type, claim_value)
    VALUES (gen_random_uuid(), p_user_id, p_tenant_id, 'permission', p_claim_value)
    ON CONFLICT (user_id, tenant_id, claim_value) DO NOTHING;
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- MIGRATION HELPER FUNCTIONS
-- ========================================

-- Function to migrate existing hardcoded claims to master_claims references
CREATE OR REPLACE FUNCTION migrate_existing_claims()
RETURNS TEXT AS $$
DECLARE
    orphaned_role_claims INT;
    orphaned_user_claims INT;
    result_text TEXT;
BEGIN
    -- Count orphaned claims before cleanup
    SELECT COUNT(*) INTO orphaned_role_claims
    FROM role_claims rc
    WHERE NOT EXISTS (SELECT 1 FROM master_claims mc WHERE mc.claim_value = rc.claim_value);
    
    SELECT COUNT(*) INTO orphaned_user_claims
    FROM user_claims uc
    WHERE NOT EXISTS (SELECT 1 FROM master_claims mc WHERE mc.claim_value = uc.claim_value);
    
    result_text := format('Migration Summary:
- Orphaned role claims found: %s
- Orphaned user claims found: %s
- These will be removed when foreign key constraints are applied.
- Run the main schema script to complete migration.', 
    orphaned_role_claims, orphaned_user_claims);
    
    RETURN result_text;
END;
$$ LANGUAGE plpgsql;

-- Run migration check
SELECT migrate_existing_claims();

-- ========================================
-- PERFORMANCE OPTIMIZATIONS
-- ========================================

-- Additional indexes for foreign key performance
CREATE INDEX IF NOT EXISTS idx_role_claims_claim_value ON role_claims(claim_value);
CREATE INDEX IF NOT EXISTS idx_user_claims_claim_value ON user_claims(claim_value);

-- Composite indexes for common queries
CREATE INDEX IF NOT EXISTS idx_role_claims_role_claim ON role_claims(role_id, claim_value);
CREATE INDEX IF NOT EXISTS idx_user_claims_user_tenant_claim ON user_claims(user_id, tenant_id, claim_value);

-- Index for permission validation queries
CREATE INDEX IF NOT EXISTS idx_master_claims_assignable ON master_claims(is_tenant_scoped, is_system_permission, claim_value);

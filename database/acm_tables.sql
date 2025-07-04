-- Database tables for Access Control Management - Multi-Tenant Support
-- This script should be run against your PostgreSQL database

-- Tenants table (unchanged)
CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(50) UNIQUE NOT NULL,
    logo_url TEXT,
    contact_email VARCHAR(255),
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE
);

-- Users table (modified for global users)
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL, -- Global unique email
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    password_hash TEXT NOT NULL,
    security_stamp VARCHAR(255) NOT NULL,
    is_email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    is_globally_locked BOOLEAN NOT NULL DEFAULT FALSE, -- Global lockout
    global_lockout_end TIMESTAMP WITHOUT TIME ZONE,
    global_access_failed_count INTEGER NOT NULL DEFAULT 0,
    last_login_at TIMESTAMP WITHOUT TIME ZONE,
    phone_number VARCHAR(20),
    is_phone_number_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE
);

-- User-Tenant relationships (many-to-many)
CREATE TABLE IF NOT EXISTS user_tenants (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    joined_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    left_at TIMESTAMP WITHOUT TIME ZONE,
    invited_by VARCHAR(255), -- Email of the inviting user
    UNIQUE(user_id, tenant_id)
);

-- Roles table (unchanged)
CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE,
    UNIQUE(tenant_id, name)
);

-- User roles junction table (modified to include tenant context)
CREATE TABLE IF NOT EXISTS user_roles (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE, -- Explicit tenant context
    UNIQUE(user_id, role_id, tenant_id)
);

-- User claims table (modified to include tenant context)
CREATE TABLE IF NOT EXISTS user_claims (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE, -- Tenant-specific claims
    claim_type VARCHAR(255) NOT NULL,
    claim_value VARCHAR(255) NOT NULL
);

-- Role claims table (unchanged)
CREATE TABLE IF NOT EXISTS role_claims (
    id UUID PRIMARY KEY,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    claim_type VARCHAR(255) NOT NULL,
    claim_value VARCHAR(255) NOT NULL
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_user_tenants_user_id ON user_tenants(user_id);
CREATE INDEX IF NOT EXISTS idx_user_tenants_tenant_id ON user_tenants(tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_tenants_active ON user_tenants(is_active);
CREATE INDEX IF NOT EXISTS idx_roles_tenant_id ON roles(tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON user_roles(role_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_tenant_id ON user_roles(tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_claims_user_id ON user_claims(user_id);
CREATE INDEX IF NOT EXISTS idx_user_claims_tenant_id ON user_claims(tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_claims_type_value ON user_claims(claim_type, claim_value);
CREATE INDEX IF NOT EXISTS idx_role_claims_role_id ON role_claims(role_id);
CREATE INDEX IF NOT EXISTS idx_role_claims_type_value ON role_claims(claim_type, claim_value);

-- Migration script to update existing data (if you have existing data)
-- Run this after creating the new tables

-- Add default user_tenant entries for existing users (if any)
-- INSERT INTO user_tenants (id, user_id, tenant_id, is_active, joined_at)
-- SELECT gen_random_uuid(), u.id, u.tenant_id, true, u.created_at
-- FROM users u
-- WHERE NOT EXISTS (
--     SELECT 1 FROM user_tenants ut 
--     WHERE ut.user_id = u.id AND ut.tenant_id = u.tenant_id
-- );

-- Update user_roles to include tenant_id (if you have existing data)
-- UPDATE user_roles 
-- SET tenant_id = (
--     SELECT r.tenant_id 
--     FROM roles r 
--     WHERE r.id = user_roles.role_id
-- )
-- WHERE tenant_id IS NULL;

-- Update user_claims to include tenant_id (if you have existing data)
-- UPDATE user_claims 
-- SET tenant_id = (
--     SELECT u.tenant_id 
--     FROM users u 
--     WHERE u.id = user_claims.user_id
-- )
-- WHERE tenant_id IS NULL;


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
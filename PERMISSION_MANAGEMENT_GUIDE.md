# LaunchEase Permission Management System

## Overview
The permission system is designed with a hierarchical structure to support multi-tenant access control where:
- **Tenant users** can only access data within their own tenant
- **Business owners** can access and manage all tenants' data

## Permission Hierarchy (High to Low)

### 1. Business Owner (`business.owner`)
- **Highest Level**: Can access ALL tenant data and perform ANY operation
- Has unrestricted access across all tenants
- Can perform all operations without tenant restrictions

### 2. System Administrator (`system.admin`)
- Can access system-wide operations
- Can view global configurations, logs, and manage system settings
- Has elevated privileges but focused on system administration

### 3. Cross-Tenant Access (`cross.tenant.access`)
- Can perform operations across multiple tenants
- Limited to specific cross-tenant operations

### 4. Tenant-Scoped Permissions (Regular Users)
- **Users**: `users.view`, `users.create`, `users.edit`, `users.delete`
- **Roles**: `roles.view`, `roles.create`, `roles.edit`, `roles.delete`
- **Tenant Settings**: `tenant.settings.view`, `tenant.settings.edit`
- **Dashboard**: `dashboard.view`
- **Auth**: `authentication.view`, `authentication.edit`, `authorization.view`, `authorization.edit`

## How It Works

### For Tenant Users:
- They get tenant-scoped permissions like `users.view`, `roles.create`, etc.
- These permissions only work within their own tenant
- They cannot access other tenants' data
- The system automatically enforces tenant isolation

### For Business Owners:
- They get the `business.owner` permission
- This permission bypasses all tenant restrictions
- They can access any tenant's data and perform any operation
- No additional tenant-scoped permissions needed

### Global Operations:
- **Global Tenant Management**: `global.tenants.view`, `global.tenants.create`, etc.
- **Global User Management**: `global.users.view`, `global.users.create`, etc.
- **Global Role Management**: `global.roles.view`, `global.roles.create`, etc.
- **System Operations**: `system.admin`, `system.dashboard.view`, etc.

## Controller Permission Usage

### TenantsController:
- `GET /api/tenants/{slug}` → `tenant.settings.view` (tenant users can view their own tenant)
- `GET /api/tenants` → `global.tenants.view` (only business owners/system admins)

### UsersController:
- All endpoints use tenant-scoped permissions (`users.view`, `users.create`, etc.)
- Tenant users can only manage users within their tenant
- Business owners can manage users across all tenants

### RolesController:
- All endpoints use tenant-scoped permissions (`roles.view`, `roles.create`, etc.)
- Tenant users can only manage roles within their tenant
- Business owners can manage roles across all tenants

## Security Implementation

### Permission Validation:
1. Check if user has `business.owner` → Allow everything
2. Check if user has `system.admin` → Allow system/global operations
3. Check if user has `cross.tenant.access` → Allow limited cross-tenant operations
4. Check specific tenant-scoped permission → Allow only within user's tenant

### Tenant Isolation:
- Controllers automatically enforce tenant isolation
- Users can only access data belonging to their tenant
- Business owners bypass tenant isolation
- System validates tenant access on every request

## Example Scenarios

### Scenario 1: Tenant User
- User belongs to "Acme Corp" tenant
- Has permissions: `users.view`, `users.create`, `roles.view`
- Can view and create users only within "Acme Corp"
- Cannot access "Other Company" tenant data
- Cannot list all tenants globally

### Scenario 2: Business Owner
- Has permission: `business.owner`
- Can access "Acme Corp", "Other Company", and all other tenants
- Can view global tenant list
- Can manage users/roles across all tenants
- Has unrestricted access

This design ensures that:
- Regular tenant users have isolated access to their own tenant data
- Business owners have complete control over all tenant data
- The system is secure and scalable for multi-tenant SaaS applications

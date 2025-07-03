using Acm.Application.DataTransferObjects;

namespace Acm.Application.Services;

public interface ITenantService 
{
    Task<TenantResponse?> RegisterTenantAsync(
        RegisterTenantRequest request, CancellationToken cancellationToken = default);
}
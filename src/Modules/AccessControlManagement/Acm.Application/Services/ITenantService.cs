using Acm.Application.DataTransferObjects;
using Acm.Application.DataTransferObjects.Request;
using Acm.Application.DataTransferObjects.Response;

namespace Acm.Application.Services;

public interface ITenantService 
{
    Task<TenantResponse?> RegisterTenantAsync(
        RegisterTenantRequest request, CancellationToken cancellationToken = default);

    Task DeleteTenantAsync(Guid id, CancellationToken ct);
}
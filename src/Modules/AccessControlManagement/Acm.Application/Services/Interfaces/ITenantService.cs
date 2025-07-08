using Acm.Application.DataTransferObjects.Request;
using Acm.Application.DataTransferObjects.Response;

namespace Acm.Application.Services.Interfaces;

public interface ITenantService 
{
    Task<TenantResponse?> RegisterTenantAsync(
        RegisterTenantRequest request, CancellationToken cancellationToken = default);

    Task DeleteTenantAsync(Guid id, CancellationToken ct);
}
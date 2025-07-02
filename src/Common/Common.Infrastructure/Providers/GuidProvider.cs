using Common.Application.Providers;

namespace Common.Infrastructure.Providers;

public class GuidProvider : IGuidProvider
{
    public Guid SortableGuid() => Guid.CreateVersion7();

    public Guid RandomGuid() => Guid.NewGuid();
}
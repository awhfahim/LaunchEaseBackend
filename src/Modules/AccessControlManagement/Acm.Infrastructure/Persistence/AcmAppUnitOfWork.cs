using Acm.Application;
using Common.Infrastructure.Persistence;

namespace Acm.Infrastructure.Persistence;

public class AcmAppUnitOfWork : UnitOfWorkBase, IAcmAppUnitOfWork
{
    public AcmAppUnitOfWork(AcmDbContext dbContext) : base(dbContext)
    {
    }
}

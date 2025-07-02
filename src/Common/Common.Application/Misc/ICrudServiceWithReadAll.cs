using Common.Domain.DataTransferObjects.Response;

namespace Common.Application.Misc;

public interface ICrudServiceWithReadAll<TEntity, TKey, in TCreateOrUpdateDto, in TQueryDto,
    TFailedOutcome> : ICrudService<TEntity, TKey, TCreateOrUpdateDto, TFailedOutcome>
    where TEntity : notnull
    where TFailedOutcome : notnull
    where TKey : notnull
{
    Task<PagedData<TEntity>> ReadAllAsync(TQueryDto dto);
}

using SharpOutcome;

namespace Common.Application.Misc;

public interface ICrudService<TEntity, TKey, in TCreateOrUpdateDto, TFailedOutcome>
    where TEntity : notnull
    where TFailedOutcome : notnull
    where TKey : notnull
{
    Task<ValueOutcome<TKey, TFailedOutcome>> CreateAsync(TCreateOrUpdateDto dto);
    Task<TEntity?> ReadAsync(TKey id, CancellationToken ct = default);
    Task<ValueOutcome<TKey, TFailedOutcome>> UpdateAsync(TKey id, TCreateOrUpdateDto dto);
    Task<bool> DeleteAsync(TKey id);
}

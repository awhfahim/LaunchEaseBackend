using SharpOutcome;

namespace Common.Application.Misc;

public interface IManipulateDataService<TEntity, in TKey, in TCreateOrUpdateDto, TFailedOutcome>
    where TEntity : notnull
    where TFailedOutcome : notnull
{
    Task<ValueOutcome<TEntity, TFailedOutcome>> CreateAsync(TCreateOrUpdateDto dto);
    Task<ValueOutcome<TEntity, TFailedOutcome>> UpdateAsync(TKey id, TCreateOrUpdateDto dto);
    Task<bool> DeleteAsync(TKey id);
}
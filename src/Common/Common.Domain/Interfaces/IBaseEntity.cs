namespace Common.Domain.Interfaces;

public interface IBaseEntity<TKey>
    where TKey : IEquatable<TKey>, IComparable
{
    public TKey Id { get; init; }
}
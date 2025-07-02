namespace Common.Domain.Interfaces;

public interface IEntity<TKey> where TKey : IEquatable<TKey>, IComparable
{
    public TKey Id { get; }
}

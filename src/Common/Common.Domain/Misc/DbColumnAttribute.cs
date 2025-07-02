namespace Common.Domain.Misc;

[AttributeUsage(AttributeTargets.Property)]
public class DbColumnAttribute : Attribute
{
    public string Name { get; }

    public DbColumnAttribute(string name)
    {
        Name = name;
    }
}

namespace Common.Infrastructure;

public static class SharedInfrastructureConstants
{
    public const string VarcharPgOperator = "varchar_pattern_ops";
    public static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(10);
}
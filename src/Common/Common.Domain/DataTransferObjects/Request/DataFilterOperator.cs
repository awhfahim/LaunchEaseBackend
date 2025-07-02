namespace Common.Domain.DataTransferObjects.Request;

public enum DataFilterOperator : byte
{
    Equals = 1,
    Contains,
    GreaterThan,
    GreaterThanEquals,
    LessThan,
    LessThanEquals,
    StartsWith,
    EndsWith
}
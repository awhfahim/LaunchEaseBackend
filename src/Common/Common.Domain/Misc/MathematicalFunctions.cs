namespace Common.Domain.Misc;

public static class MathematicalFunctions
{
    public static int CalculatePage(int page, int limit)
    {
        if (page <= 0)
        {
            page = 1;
        }

        if (limit <= 0)
        {
            limit = 1;
        }

        return (page - 1) * limit;
    }
}

namespace MixServer.Domain.Utilities;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime CreateUtcTime(DateTime utcTime);
    DateTime? CreateUtcTime(DateTime? utcTime);
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => CreateUtcTime(DateTime.UtcNow);

    public DateTime CreateUtcTime(DateTime utcTime) => DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

    public DateTime? CreateUtcTime(DateTime? utcTime)
    {
        return utcTime.HasValue
            ? CreateUtcTime(utcTime.Value)
            : null;
    }
}

using Common;
using System;
public class TimeManager : Singleton<TimeManager>
{
    public TimeSpan serverTimeModifier = TimeSpan.FromHours(8);

    public void SetServerTime(TimeSpan timeSpan)
    {
        serverTimeModifier = timeSpan;
    }

    public long GetCurrentUnixtimestamp()
    {
        return ((DateTimeOffset)DateTime.UtcNow.Add(serverTimeModifier)).ToUnixTimeSeconds();
    }

    public DateTime GetCurrentDatetime()
    {
        return DateTime.UtcNow.Add(serverTimeModifier);
    }

    public static DateTime UnixTimeStamp2DateTime(long unixTimeStamp)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }

    public static int DiffDayWithToday(long timestamp)
    {

        return UnixTimeStamp2DateTime(timestamp).Subtract(DateTime.MinValue).Days - DateTime.Now.Subtract(DateTime.MinValue).Days;
    }

    public static bool IsToday(long timestamp)
    {
        return DiffDayWithToday(timestamp) == 0;
    }

    public static bool IsPass(long timestamp)
    {
        return DiffDayWithToday(timestamp) < 0;
    }

    public static bool IsFuture(long timestamp)
    {
        return DiffDayWithToday(timestamp) > 0;
    }

    public bool IsWithinTimeRange(long startTime, long endTime)
    {
        return GetCurrentUnixtimestamp() >= startTime && GetCurrentUnixtimestamp() <= endTime;
    }
}
using Common;
using System;

public class TimeManager : Singleton<TimeManager>
{
    public TimeSpan serverTimeModifier;
    public TimeManager()
    {
        serverTimeModifier = TimeSpan.Zero;
    }

    public void SetServerTime(TimeSpan timeSpan)
    {
        serverTimeModifier = timeSpan;
    }

    public long GetServerTime()
    {
        DateTime currentTime = DateTime.UtcNow;
        currentTime.Add(serverTimeModifier);
        return ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
    }
}
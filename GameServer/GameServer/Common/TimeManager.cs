using Common;
using System;
using System.Timers;
using System.Collections;
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
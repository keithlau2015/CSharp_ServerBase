# EventScheduler Documentation

## Overview
The EventScheduler is a comprehensive job scheduling system for your game server that supports:
- ✅ One-time events
- ✅ Recurring events (seconds, minutes, hours, daily, weekly, monthly)
- ✅ Priority-based execution
- ✅ Async and sync operations
- ✅ Thread-safe operations
- ✅ Graceful shutdown handling

## Basic Usage

### 1. Initialize the Scheduler
```csharp
var scheduler = EventScheduler.Instance;
scheduler.StartScheduleThread();
```

### 2. Schedule One-Time Events
```csharp
// Execute in 5 minutes
var eventId = scheduler.ScheduleOneTimeEvent(
    "PlayerReward",
    () => GivePlayerReward(),
    DateTime.Now.AddMinutes(5),
    EventPriority.Normal
);

// Async version
var asyncId = scheduler.ScheduleOneTimeEventAsync(
    "DatabaseCleanup",
    async () => await CleanupDatabaseAsync(),
    DateTime.Now.AddHours(1),
    EventPriority.High
);
```

### 3. Schedule Recurring Events
```csharp
// Every 30 seconds
scheduler.ScheduleRecurringEvent(
    "Heartbeat",
    SendHeartbeat,
    RecurrenceType.Seconds,
    30,
    EventPriority.Low
);

// Every 5 minutes
scheduler.ScheduleRecurringEvent(
    "AutoSave",
    SavePlayerData,
    RecurrenceType.Minutes,
    5,
    EventPriority.Normal
);

// Every 6 hours
scheduler.ScheduleRecurringEvent(
    "Backup",
    CreateBackup,
    RecurrenceType.Hours,
    6,
    EventPriority.High
);
```

### 4. Schedule Daily Events
```csharp
// Daily at 3:00 AM
scheduler.ScheduleDailyEvent(
    "DailyMaintenance",
    PerformMaintenance,
    new TimeSpan(3, 0, 0), // 3:00 AM
    EventPriority.High
);

// Daily reset at midnight
scheduler.ScheduleDailyEvent(
    "DailyReset",
    ResetDailyContent,
    TimeSpan.Zero, // Midnight
    EventPriority.Critical
);
```

### 5. Schedule Weekly Events
```csharp
// Every Sunday at 4:00 AM
scheduler.ScheduleWeeklyEvent(
    "WeeklyMaintenance",
    PerformWeeklyMaintenance,
    DayOfWeek.Sunday,
    new TimeSpan(4, 0, 0),
    EventPriority.Critical
);

// Every Friday at 8:00 PM
scheduler.ScheduleWeeklyEvent(
    "WeekendEvent",
    StartWeekendEvent,
    DayOfWeek.Friday,
    new TimeSpan(20, 0, 0),
    EventPriority.Normal
);
```

### 6. Execute Immediately
```csharp
// Add to immediate execution queue
scheduler.ExecuteImmediately(
    "EmergencyTask",
    HandleEmergency,
    EventPriority.Critical
);
```

## Event Management

### Remove Events
```csharp
bool removed = scheduler.RemoveScheduledEvent(eventId);
```

### Enable/Disable Events
```csharp
scheduler.DisableEvent(eventId);
scheduler.EnableEvent(eventId);
```

### Get Event Information
```csharp
var eventInfo = scheduler.GetScheduledEvent(eventId);
var allEvents = scheduler.GetAllScheduledEvents();
var criticalEvents = scheduler.GetEventsByPriority(EventPriority.Critical);
```

### Check Scheduler Status
```csharp
bool isRunning = scheduler.IsRunning;
int eventCount = scheduler.ScheduledEventCount;
int pendingCount = scheduler.PendingImmediateEvents;
```

## Priority Levels

| Priority | Use Case | Execution Order |
|----------|----------|----------------|
| `Critical` | Server shutdown, emergency tasks | 1st |
| `High` | Maintenance, backups, important notifications | 2nd |
| `Normal` | Regular game events, player actions | 3rd |
| `Low` | Monitoring, statistics, non-critical tasks | 4th |

## Recurrence Types

| Type | Description | Example |
|------|-------------|---------|
| `None` | One-time execution | Event reminders |
| `Seconds` | Every X seconds | Heartbeat (30s) |
| `Minutes` | Every X minutes | Auto-save (5min) |
| `Hours` | Every X hours | Backup (6h) |
| `Daily` | Daily at specific time | Maintenance (3 AM) |
| `Weekly` | Weekly on specific day/time | Weekly reset (Sunday 4 AM) |
| `Monthly` | Monthly on specific day | Monthly statistics |

## Best Practices

### 1. Use Appropriate Priorities
```csharp
// ❌ Wrong - using Critical for non-critical tasks
scheduler.ScheduleRecurringEvent("Stats", UpdateStats, RecurrenceType.Minutes, 1, EventPriority.Critical);

// ✅ Correct - using appropriate priority
scheduler.ScheduleRecurringEvent("Stats", UpdateStats, RecurrenceType.Minutes, 1, EventPriority.Low);
```

### 2. Handle Exceptions in Event Actions
```csharp
// ✅ Good practice - wrap in try-catch
scheduler.ScheduleRecurringEvent("RiskyTask", () => {
    try 
    {
        PerformRiskyOperation();
    }
    catch (Exception ex)
    {
        Debug.DebugUtility.ErrorLog($"RiskyTask failed: {ex.Message}");
    }
}, RecurrenceType.Minutes, 5, EventPriority.Normal);
```

### 3. Use Meaningful Names
```csharp
// ❌ Poor naming
scheduler.ScheduleOneTimeEvent("Event1", DoSomething, DateTime.Now.AddMinutes(5));

// ✅ Clear naming
scheduler.ScheduleOneTimeEvent("PlayerLoginBonus", GiveLoginBonus, DateTime.Now.AddMinutes(5));
```

### 4. Clean Up Resources
```csharp
// Always stop and dispose when shutting down
public void Shutdown()
{
    scheduler.StopScheduleThread();
    scheduler.Dispose();
}
```

## Game Server Integration

### Server Startup
```csharp
public void StartServer()
{
    // 1. Initialize scheduler
    var scheduler = EventScheduler.Instance;
    scheduler.StartScheduleThread();
    
    // 2. Schedule essential events
    ScheduleMaintenanceEvents();
    ScheduleGameEvents();
    ScheduleMonitoring();
}
```

### Server Shutdown
```csharp
public void ShutdownServer()
{
    // 1. Stop accepting new events
    scheduler.StopScheduleThread();
    
    // 2. Save all data
    SaveAllServerData();
    
    // 3. Dispose scheduler
    scheduler.Dispose();
}
```

## Common Game Server Events

### Maintenance Events
```csharp
// Daily maintenance at 3 AM
scheduler.ScheduleDailyEvent("DailyMaintenance", PerformDailyMaintenance, new TimeSpan(3, 0, 0), EventPriority.High);

// Database backup every 6 hours
scheduler.ScheduleRecurringEvent("DatabaseBackup", BackupDatabase, RecurrenceType.Hours, 6, EventPriority.High);
```

### Player Data Events
```csharp
// Auto-save every 5 minutes
scheduler.ScheduleRecurringEvent("AutoSave", SaveAllPlayers, RecurrenceType.Minutes, 5, EventPriority.Normal);

// Clear inactive sessions every 30 minutes
scheduler.ScheduleRecurringEvent("ClearSessions", ClearInactiveSessions, RecurrenceType.Minutes, 30, EventPriority.Normal);
```

### Game Content Events
```csharp
// Daily reset at 6 AM
scheduler.ScheduleDailyEvent("DailyReset", ResetDailyContent, new TimeSpan(6, 0, 0), EventPriority.Critical);

// World boss spawn every 2 hours
scheduler.ScheduleRecurringEvent("WorldBoss", SpawnWorldBoss, RecurrenceType.Hours, 2, EventPriority.Normal);
```

### Monitoring Events
```csharp
// Performance monitoring every minute
scheduler.ScheduleRecurringEvent("Monitor", MonitorPerformance, RecurrenceType.Minutes, 1, EventPriority.Low);

// Server heartbeat every 30 seconds
scheduler.ScheduleRecurringEvent("Heartbeat", SendHeartbeat, RecurrenceType.Seconds, 30, EventPriority.Low);
```

## Error Handling

The EventScheduler automatically handles and logs errors, but you should still handle exceptions in your event actions:

```csharp
scheduler.ScheduleRecurringEvent("DatabaseOperation", () => {
    try 
    {
        PerformDatabaseOperation();
    }
    catch (DatabaseException ex)
    {
        Debug.DebugUtility.ErrorLog($"Database operation failed: {ex.Message}");
        // Maybe schedule a retry or switch to backup database
    }
    catch (Exception ex)
    {
        Debug.DebugUtility.ErrorLog($"Unexpected error: {ex.Message}");
    }
}, RecurrenceType.Minutes, 5, EventPriority.Normal);
```

## Performance Considerations

1. **Event Frequency**: Don't schedule too many high-frequency events
2. **Event Duration**: Keep event actions short to avoid blocking
3. **Memory Usage**: Remove completed one-time events automatically
4. **Thread Safety**: All operations are thread-safe, but your event actions should be too

## Troubleshooting

### Events Not Executing
- Check if scheduler is started: `scheduler.StartScheduleThread()`
- Verify event is enabled: `scheduler.EnableEvent(eventId)`
- Check execution time: `scheduler.GetScheduledEvent(eventId).NextExecutionTime`

### High CPU Usage
- Reduce frequency of recurring events
- Make event actions more efficient
- Use async operations for I/O bound tasks

### Memory Leaks
- Ensure proper disposal: `scheduler.Dispose()`
- Remove unused events: `scheduler.RemoveScheduledEvent(eventId)`
- Clear all events if needed: `scheduler.ClearAllEvents()` 
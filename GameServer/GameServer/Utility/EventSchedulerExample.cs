using System;
using System.Threading.Tasks;
using Utility;

namespace GameServer.Examples
{
    /// <summary>
    /// Example usage of the EventScheduler for common game server scenarios
    /// </summary>
    public class EventSchedulerExample
    {
        private readonly EventScheduler _scheduler;

        public EventSchedulerExample()
        {
            _scheduler = EventScheduler.Instance;
        }

        public void InitializeServerEvents()
        {
            // Start the scheduler
            _scheduler.StartScheduleThread();

            // Setup maintenance events
            SetupMaintenanceEvents();
            
            // Setup game events
            SetupGameEvents();

            Debug.DebugUtility.DebugLog("All server events have been scheduled");
        }

        #region Maintenance Events

        private void SetupMaintenanceEvents()
        {
            // Daily server maintenance at 3:00 AM
            _scheduler.ScheduleDailyEvent(
                "DailyMaintenance",
                PerformDailyMaintenance,
                new TimeSpan(3, 0, 0), // 3:00 AM
                EventPriority.High
            );

            // Database backup every 6 hours
            _scheduler.ScheduleRecurringEvent(
                "DatabaseBackup",
                PerformDatabaseBackup,
                RecurrenceType.Hours,
                6,
                EventPriority.High
            );

            // Auto-save player data every 5 minutes
            _scheduler.ScheduleRecurringEvent(
                "AutoSavePlayerData",
                SaveAllPlayerData,
                RecurrenceType.Minutes,
                5,
                EventPriority.Normal
            );
        }

        private void PerformDailyMaintenance()
        {
            Debug.DebugUtility.DebugLog("Starting daily maintenance...");
            // Cleanup temporary files, optimize database, etc.
            Debug.DebugUtility.DebugLog("Daily maintenance completed");
        }

        private void PerformDatabaseBackup()
        {
            Debug.DebugUtility.DebugLog("Performing database backup...");
            // Trigger database backup
            Debug.DebugUtility.DebugLog("Database backup completed");
        }

        #endregion

        #region Game Events

        private void SetupGameEvents()
        {
            // Daily server reset at 6:00 AM
            _scheduler.ScheduleDailyEvent(
                "DailyServerReset",
                PerformDailyReset,
                new TimeSpan(6, 0, 0),
                EventPriority.Critical
            );

            // Weekend special event every Friday at 8 PM
            _scheduler.ScheduleWeeklyEvent(
                "WeekendSpecialEvent",
                StartWeekendSpecialEvent,
                DayOfWeek.Friday,
                new TimeSpan(20, 0, 0),
                EventPriority.Normal
            );

            // Spawn world bosses every 2 hours
            _scheduler.ScheduleRecurringEvent(
                "WorldBossSpawn",
                SpawnWorldBoss,
                RecurrenceType.Hours,
                2,
                EventPriority.Normal
            );
        }

        private void PerformDailyReset()
        {
            Debug.DebugUtility.DebugLog("Performing daily server reset...");
            // Reset daily content, clear statistics, etc.
            Debug.DebugUtility.DebugLog("Daily server reset completed");
        }

        private void StartWeekendSpecialEvent()
        {
            Debug.DebugUtility.DebugLog("Starting weekend special event!");
            // Activate special content, increase drop rates, etc.
        }

        private void SpawnWorldBoss()
        {
            Debug.DebugUtility.DebugLog("Spawning world boss...");
            // Spawn a world boss in a random location
        }

        #endregion

        #region Helper Methods

        private void SaveAllPlayerData()
        {
            Debug.DebugUtility.DebugLog("Auto-saving player data...");
            // Save all connected players' data
            Debug.DebugUtility.DebugLog("Player data auto-save completed");
        }

        #endregion

        #region Event Management Examples

        public void DemonstrateEventManagement()
        {
            // Schedule a test event
            var eventId = _scheduler.ScheduleOneTimeEvent(
                "TestEvent",
                () => Debug.DebugUtility.DebugLog("Test event executed!"),
                DateTime.Now.AddMinutes(5)
            );

            Debug.DebugUtility.DebugLog($"Scheduled test event with ID: {eventId}");

            // Execute something immediately
            _scheduler.ExecuteImmediately(
                "ImmediateTask",
                () => Debug.DebugUtility.DebugLog("This executed immediately!"),
                EventPriority.High
            );

            // Check scheduler status
            Debug.DebugUtility.DebugLog($"Scheduler running: {_scheduler.IsRunning}");
            Debug.DebugUtility.DebugLog($"Scheduled events: {_scheduler.ScheduledEventCount}");
        }

        #endregion

        public void Shutdown()
        {
            Debug.DebugUtility.DebugLog("Shutting down EventScheduler...");
            _scheduler.StopScheduleThread();
            _scheduler.Dispose();
        }
    }
} 
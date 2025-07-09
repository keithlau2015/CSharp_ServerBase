using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Utility
{
    public enum EventPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public enum RecurrenceType
    {
        None,           // One-time event
        Seconds,        // Every X seconds
        Minutes,        // Every X minutes
        Hours,          // Every X hours
        Daily,          // Daily at specific time
        Weekly,         // Weekly on specific day
        Monthly,        // Monthly on specific day
        Cron            // Cron expression
    }

    public class ScheduledEvent
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Action EventAction { get; set; }
        public Func<Task> AsyncEventAction { get; set; }
        public DateTime NextExecutionTime { get; set; }
        public RecurrenceType RecurrenceType { get; set; }
        public int RecurrenceInterval { get; set; } // For Seconds, Minutes, Hours
        public TimeSpan? DailyTime { get; set; } // For Daily events (time of day)
        public DayOfWeek? WeeklyDay { get; set; } // For Weekly events
        public int? MonthlyDay { get; set; } // For Monthly events (day of month)
        public string CronExpression { get; set; } // For Cron events
        public EventPriority Priority { get; set; }
        public bool IsEnabled { get; set; }
        public int MaxExecutions { get; set; } // -1 for unlimited
        public int ExecutionCount { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? LastExecutionTime { get; set; }
        public bool IsAsync { get; set; }

        public ScheduledEvent()
        {
            Id = Guid.NewGuid().ToString();
            Priority = EventPriority.Normal;
            IsEnabled = true;
            MaxExecutions = -1;
            ExecutionCount = 0;
            CreatedTime = TimeManager.Instance.GetCurrentDatetime();
            IsAsync = false;
        }
    }

    public class EventScheduler : Singleton<EventScheduler>
    {
        private readonly ConcurrentDictionary<string, ScheduledEvent> _scheduledEvents;
        private readonly ConcurrentQueue<ScheduledEvent> _immediateExecutionQueue;
        private readonly Timer _schedulerTimer;
        private readonly Timer _immediateExecutionTimer;
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private Thread _schedulerThread;
        private readonly object _lockObject = new object();

        public bool IsRunning => _isRunning;
        public int ScheduledEventCount => _scheduledEvents.Count;
        public int PendingImmediateEvents => _immediateExecutionQueue.Count;

        public EventScheduler()
        {
            _scheduledEvents = new ConcurrentDictionary<string, ScheduledEvent>();
            _immediateExecutionQueue = new ConcurrentQueue<ScheduledEvent>();
            _executionSemaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
            _cancellationTokenSource = new CancellationTokenSource();

            // Timer to check scheduled events every second
            _schedulerTimer = new Timer(CheckScheduledEvents, null, Timeout.Infinite, Timeout.Infinite);
            
            // Timer to process immediate execution queue every 100ms
            _immediateExecutionTimer = new Timer(ProcessImmediateQueue, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartScheduleThread()
        {
            lock (_lockObject)
            {
                if (_isRunning)
                {
                    Debug.DebugUtility.WarningLog("EventScheduler is already running");
                    return;
                }

                _isRunning = true;
                _schedulerTimer.Change(0, 1000); // Check every second
                _immediateExecutionTimer.Change(0, 100); // Process immediate queue every 100ms

                Debug.DebugUtility.DebugLog("EventScheduler started successfully");
            }
        }

        public void StopScheduleThread()
        {
            lock (_lockObject)
            {
                if (!_isRunning)
                {
                    Debug.DebugUtility.WarningLog("EventScheduler is not running");
                    return;
                }

                _isRunning = false;
                _schedulerTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _immediateExecutionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _cancellationTokenSource.Cancel();

                Debug.DebugUtility.DebugLog("EventScheduler stopped");
            }
        }

        #region Event Registration Methods

        /// <summary>
        /// Schedule a one-time event to execute at a specific time
        /// </summary>
        public string ScheduleOneTimeEvent(string name, Action action, DateTime executeTime, EventPriority priority = EventPriority.Normal)
        {
            var scheduledEvent = new ScheduledEvent
            {
                Name = name,
                EventAction = action,
                NextExecutionTime = executeTime,
                RecurrenceType = RecurrenceType.None,
                Priority = priority,
                MaxExecutions = 1
            };

            return AddScheduledEvent(scheduledEvent);
        }

        /// <summary>
        /// Schedule a one-time async event to execute at a specific time
        /// </summary>
        public string ScheduleOneTimeEventAsync(string name, Func<Task> asyncAction, DateTime executeTime, EventPriority priority = EventPriority.Normal)
        {
            var scheduledEvent = new ScheduledEvent
            {
                Name = name,
                AsyncEventAction = asyncAction,
                NextExecutionTime = executeTime,
                RecurrenceType = RecurrenceType.None,
                Priority = priority,
                MaxExecutions = 1,
                IsAsync = true
            };

            return AddScheduledEvent(scheduledEvent);
        }

        /// <summary>
        /// Schedule a recurring event (every X seconds/minutes/hours)
        /// </summary>
        public string ScheduleRecurringEvent(string name, Action action, RecurrenceType recurrenceType, int interval, EventPriority priority = EventPriority.Normal)
        {
            var scheduledEvent = new ScheduledEvent
            {
                Name = name,
                EventAction = action,
                RecurrenceType = recurrenceType,
                RecurrenceInterval = interval,
                Priority = priority,
                NextExecutionTime = CalculateNextExecution(recurrenceType, interval)
            };

            return AddScheduledEvent(scheduledEvent);
        }

        /// <summary>
        /// Schedule a daily event at a specific time
        /// </summary>
        public string ScheduleDailyEvent(string name, Action action, TimeSpan timeOfDay, EventPriority priority = EventPriority.Normal)
        {
            var scheduledEvent = new ScheduledEvent
            {
                Name = name,
                EventAction = action,
                RecurrenceType = RecurrenceType.Daily,
                DailyTime = timeOfDay,
                Priority = priority,
                NextExecutionTime = CalculateNextDailyExecution(timeOfDay)
            };

            return AddScheduledEvent(scheduledEvent);
        }

        /// <summary>
        /// Schedule a weekly event on a specific day and time
        /// </summary>
        public string ScheduleWeeklyEvent(string name, Action action, DayOfWeek dayOfWeek, TimeSpan timeOfDay, EventPriority priority = EventPriority.Normal)
        {
            var scheduledEvent = new ScheduledEvent
            {
                Name = name,
                EventAction = action,
                RecurrenceType = RecurrenceType.Weekly,
                WeeklyDay = dayOfWeek,
                DailyTime = timeOfDay,
                Priority = priority,
                NextExecutionTime = CalculateNextWeeklyExecution(dayOfWeek, timeOfDay)
            };

            return AddScheduledEvent(scheduledEvent);
        }

        /// <summary>
        /// Execute an event immediately (added to immediate execution queue)
        /// </summary>
        public void ExecuteImmediately(string name, Action action, EventPriority priority = EventPriority.Normal)
        {
            var immediateEvent = new ScheduledEvent
            {
                Name = name,
                EventAction = action,
                Priority = priority,
                NextExecutionTime = DateTime.MinValue // Indicates immediate execution
            };

            _immediateExecutionQueue.Enqueue(immediateEvent);
        }

        #endregion

        #region Event Management Methods

        public bool RemoveScheduledEvent(string eventId)
        {
            if (_scheduledEvents.TryRemove(eventId, out var removedEvent))
            {
                Debug.DebugUtility.DebugLog($"Removed scheduled event: {removedEvent.Name} (ID: {eventId})");
                return true;
            }
            return false;
        }

        public bool EnableEvent(string eventId)
        {
            if (_scheduledEvents.TryGetValue(eventId, out var scheduledEvent))
            {
                scheduledEvent.IsEnabled = true;
                Debug.DebugUtility.DebugLog($"Enabled event: {scheduledEvent.Name} (ID: {eventId})");
                return true;
            }
            return false;
        }

        public bool DisableEvent(string eventId)
        {
            if (_scheduledEvents.TryGetValue(eventId, out var scheduledEvent))
            {
                scheduledEvent.IsEnabled = false;
                Debug.DebugUtility.DebugLog($"Disabled event: {scheduledEvent.Name} (ID: {eventId})");
                return true;
            }
            return false;
        }

        public ScheduledEvent GetScheduledEvent(string eventId)
        {
            _scheduledEvents.TryGetValue(eventId, out var scheduledEvent);
            return scheduledEvent;
        }

        public List<ScheduledEvent> GetAllScheduledEvents()
        {
            return _scheduledEvents.Values.ToList();
        }

        public List<ScheduledEvent> GetEventsByPriority(EventPriority priority)
        {
            return _scheduledEvents.Values.Where(e => e.Priority == priority).ToList();
        }

        public void ClearAllEvents()
        {
            _scheduledEvents.Clear();
            Debug.DebugUtility.DebugLog("Cleared all scheduled events");
        }

        #endregion

        #region Private Methods

        private string AddScheduledEvent(ScheduledEvent scheduledEvent)
        {
            _scheduledEvents[scheduledEvent.Id] = scheduledEvent;
            Debug.DebugUtility.DebugLog($"Scheduled event: {scheduledEvent.Name} (ID: {scheduledEvent.Id}) - Next execution: {scheduledEvent.NextExecutionTime}");
            return scheduledEvent.Id;
        }

        private async void CheckScheduledEvents(object state)
        {
            if (!_isRunning || _cancellationTokenSource.Token.IsCancellationRequested)
                return;

            try
            {
                var currentTime = TimeManager.Instance.GetCurrentDatetime();
                var eventsToExecute = new List<ScheduledEvent>();

                // Find events that need to be executed
                foreach (var kvp in _scheduledEvents)
                {
                    var scheduledEvent = kvp.Value;
                    
                    if (!scheduledEvent.IsEnabled)
                        continue;

                    if (scheduledEvent.MaxExecutions > 0 && scheduledEvent.ExecutionCount >= scheduledEvent.MaxExecutions)
                    {
                        // Remove events that have reached their maximum execution count
                        _scheduledEvents.TryRemove(kvp.Key, out _);
                        continue;
                    }

                    if (currentTime >= scheduledEvent.NextExecutionTime)
                    {
                        eventsToExecute.Add(scheduledEvent);
                    }
                }

                // Sort by priority (Critical -> High -> Normal -> Low)
                eventsToExecute.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                // Execute events
                foreach (var eventToExecute in eventsToExecute)
                {
                    await ExecuteEvent(eventToExecute);
                    ScheduleNextExecution(eventToExecute);
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Error in CheckScheduledEvents: {ex.Message}");
            }
        }

        private async void ProcessImmediateQueue(object state)
        {
            if (!_isRunning || _cancellationTokenSource.Token.IsCancellationRequested)
                return;

            try
            {
                var eventsToExecute = new List<ScheduledEvent>();

                // Dequeue all immediate events
                while (_immediateExecutionQueue.TryDequeue(out var immediateEvent))
                {
                    eventsToExecute.Add(immediateEvent);
                }

                if (eventsToExecute.Count == 0)
                    return;

                // Sort by priority
                eventsToExecute.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                // Execute immediate events
                foreach (var eventToExecute in eventsToExecute)
                {
                    await ExecuteEvent(eventToExecute);
                }
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Error in ProcessImmediateQueue: {ex.Message}");
            }
        }

        private async Task ExecuteEvent(ScheduledEvent scheduledEvent)
        {
            await _executionSemaphore.WaitAsync(_cancellationTokenSource.Token);

            try
            {
                Debug.DebugUtility.DebugLog($"Executing event: {scheduledEvent.Name} (Priority: {scheduledEvent.Priority})");

                scheduledEvent.LastExecutionTime = TimeManager.Instance.GetCurrentDatetime();
                scheduledEvent.ExecutionCount++;

                if (scheduledEvent.IsAsync && scheduledEvent.AsyncEventAction != null)
                {
                    await scheduledEvent.AsyncEventAction();
                }
                else if (scheduledEvent.EventAction != null)
                {
                    await Task.Run(scheduledEvent.EventAction, _cancellationTokenSource.Token);
                }

                Debug.DebugUtility.DebugLog($"Successfully executed event: {scheduledEvent.Name}");
            }
            catch (Exception ex)
            {
                Debug.DebugUtility.ErrorLog($"Error executing event {scheduledEvent.Name}: {ex.Message}");
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }

        private void ScheduleNextExecution(ScheduledEvent scheduledEvent)
        {
            if (scheduledEvent.RecurrenceType == RecurrenceType.None)
            {
                // One-time event, remove it
                _scheduledEvents.TryRemove(scheduledEvent.Id, out _);
                return;
            }

            if (scheduledEvent.MaxExecutions > 0 && scheduledEvent.ExecutionCount >= scheduledEvent.MaxExecutions)
            {
                // Reached maximum executions, remove it
                _scheduledEvents.TryRemove(scheduledEvent.Id, out _);
                return;
            }

            // Calculate next execution time based on recurrence type
            switch (scheduledEvent.RecurrenceType)
            {
                case RecurrenceType.Seconds:
                    scheduledEvent.NextExecutionTime = scheduledEvent.NextExecutionTime.AddSeconds(scheduledEvent.RecurrenceInterval);
                    break;
                
                case RecurrenceType.Minutes:
                    scheduledEvent.NextExecutionTime = scheduledEvent.NextExecutionTime.AddMinutes(scheduledEvent.RecurrenceInterval);
                    break;
                
                case RecurrenceType.Hours:
                    scheduledEvent.NextExecutionTime = scheduledEvent.NextExecutionTime.AddHours(scheduledEvent.RecurrenceInterval);
                    break;
                
                case RecurrenceType.Daily:
                    scheduledEvent.NextExecutionTime = CalculateNextDailyExecution(scheduledEvent.DailyTime.Value);
                    break;
                
                case RecurrenceType.Weekly:
                    scheduledEvent.NextExecutionTime = CalculateNextWeeklyExecution(scheduledEvent.WeeklyDay.Value, scheduledEvent.DailyTime.Value);
                    break;
                
                case RecurrenceType.Monthly:
                    scheduledEvent.NextExecutionTime = CalculateNextMonthlyExecution(scheduledEvent.MonthlyDay.Value, scheduledEvent.DailyTime ?? TimeSpan.Zero);
                    break;
            }
        }

        private DateTime CalculateNextExecution(RecurrenceType recurrenceType, int interval)
        {
            var currentTime = TimeManager.Instance.GetCurrentDatetime();
            
            return recurrenceType switch
            {
                RecurrenceType.Seconds => currentTime.AddSeconds(interval),
                RecurrenceType.Minutes => currentTime.AddMinutes(interval),
                RecurrenceType.Hours => currentTime.AddHours(interval),
                _ => currentTime.AddMinutes(1) // Default fallback
            };
        }

        private DateTime CalculateNextDailyExecution(TimeSpan timeOfDay)
        {
            var currentTime = TimeManager.Instance.GetCurrentDatetime();
            var nextExecution = currentTime.Date.Add(timeOfDay);
            
            if (nextExecution <= currentTime)
            {
                nextExecution = nextExecution.AddDays(1);
            }
            
            return nextExecution;
        }

        private DateTime CalculateNextWeeklyExecution(DayOfWeek dayOfWeek, TimeSpan timeOfDay)
        {
            var currentTime = TimeManager.Instance.GetCurrentDatetime();
            var daysUntilTarget = ((int)dayOfWeek - (int)currentTime.DayOfWeek + 7) % 7;
            
            if (daysUntilTarget == 0 && currentTime.TimeOfDay > timeOfDay)
            {
                daysUntilTarget = 7; // Next week
            }
            
            return currentTime.Date.AddDays(daysUntilTarget).Add(timeOfDay);
        }

        private DateTime CalculateNextMonthlyExecution(int dayOfMonth, TimeSpan timeOfDay)
        {
            var currentTime = TimeManager.Instance.GetCurrentDatetime();
            var nextExecution = new DateTime(currentTime.Year, currentTime.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(currentTime.Year, currentTime.Month))).Add(timeOfDay);
            
            if (nextExecution <= currentTime)
            {
                var nextMonth = currentTime.AddMonths(1);
                nextExecution = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(dayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month))).Add(timeOfDay);
            }
            
            return nextExecution;
        }

        #endregion

        #region Dispose Pattern

        public void Dispose()
        {
            StopScheduleThread();
            _schedulerTimer?.Dispose();
            _immediateExecutionTimer?.Dispose();
            _executionSemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        ~EventScheduler()
        {
            Dispose();
        }

        #endregion
    }
}
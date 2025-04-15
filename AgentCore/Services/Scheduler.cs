using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AgentCore
{
    /// <summary>
    /// Interface for the task scheduler
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Schedule a recurring task
        /// </summary>
        /// <param name="taskId">Unique identifier for the task</param>
        /// <param name="interval">Time interval between executions</param>
        /// <param name="action">Action to execute</param>
        void ScheduleRecurringTask(string taskId, TimeSpan interval, Func<CancellationToken, Task> action);
        
        /// <summary>
        /// Schedule a one-time task
        /// </summary>
        /// <param name="taskId">Unique identifier for the task</param>
        /// <param name="delay">Delay before execution</param>
        /// <param name="action">Action to execute</param>
        void ScheduleOneTimeTask(string taskId, TimeSpan delay, Func<CancellationToken, Task> action);
        
        /// <summary>
        /// Cancel a scheduled task
        /// </summary>
        /// <param name="taskId">ID of the task to cancel</param>
        /// <returns>True if the task was cancelled, false if it doesn't exist</returns>
        bool CancelTask(string taskId);
        
        /// <summary>
        /// Stop all scheduled tasks
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Implements a task scheduler for recurring and one-time tasks
    /// </summary>
    public class Scheduler : IScheduler, IDisposable
    {
        private readonly ILogger<Scheduler> _logger;
        private readonly ConcurrentDictionary<string, TaskInfo> _scheduledTasks = new ConcurrentDictionary<string, TaskInfo>();
        private readonly CancellationTokenSource _shutdownCts = new CancellationTokenSource();
        private bool _disposed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Scheduler(ILogger<Scheduler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Schedule a recurring task
        /// </summary>
        public void ScheduleRecurringTask(string taskId, TimeSpan interval, Func<CancellationToken, Task> action)
        {
            _logger.LogDebug("Scheduling recurring task {TaskId} with interval {Interval}", taskId, interval);

            var taskInfo = new TaskInfo
            {
                TaskId = taskId,
                IsRecurring = true,
                Interval = interval,
                Action = action,
                CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token),
                NextExecutionTime = DateTime.UtcNow.Add(interval)
            };

            if (_scheduledTasks.TryGetValue(taskId, out var existingTask))
            {
                _logger.LogWarning("Task {TaskId} already exists. Cancelling and replacing.", taskId);
                existingTask.CancellationTokenSource.Cancel();
            }

            _scheduledTasks[taskId] = taskInfo;
            _ = RunRecurringTaskAsync(taskInfo);
        }

        /// <summary>
        /// Schedule a one-time task
        /// </summary>
        public void ScheduleOneTimeTask(string taskId, TimeSpan delay, Func<CancellationToken, Task> action)
        {
            _logger.LogDebug("Scheduling one-time task {TaskId} with delay {Delay}", taskId, delay);

            var taskInfo = new TaskInfo
            {
                TaskId = taskId,
                IsRecurring = false,
                Interval = delay,
                Action = action,
                CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_shutdownCts.Token),
                NextExecutionTime = DateTime.UtcNow.Add(delay)
            };

            if (_scheduledTasks.TryGetValue(taskId, out var existingTask))
            {
                _logger.LogWarning("Task {TaskId} already exists. Cancelling and replacing.", taskId);
                existingTask.CancellationTokenSource.Cancel();
            }

            _scheduledTasks[taskId] = taskInfo;
            _ = RunOneTimeTaskAsync(taskInfo);
        }

        /// <summary>
        /// Cancel a scheduled task
        /// </summary>
        public bool CancelTask(string taskId)
        {
            if (_scheduledTasks.TryRemove(taskId, out var taskInfo))
            {
                _logger.LogDebug("Cancelling task {TaskId}", taskId);
                taskInfo.CancellationTokenSource.Cancel();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stop all scheduled tasks
        /// </summary>
        public void Stop()
        {
            _logger.LogInformation("Stopping all scheduled tasks");
            _shutdownCts.Cancel();

            foreach (var task in _scheduledTasks.Values)
            {
                _logger.LogDebug("Cancelling task {TaskId}", task.TaskId);
                task.CancellationTokenSource.Cancel();
            }

            _scheduledTasks.Clear();
        }

        /// <summary>
        /// Run a recurring task
        /// </summary>
        private async Task RunRecurringTaskAsync(TaskInfo taskInfo)
        {
            var token = taskInfo.CancellationTokenSource.Token;
            
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var delay = taskInfo.NextExecutionTime - DateTime.UtcNow;
                    
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, token);
                    }

                    if (token.IsCancellationRequested)
                        break;

                    try
                    {
                        _logger.LogDebug("Executing recurring task {TaskId}", taskInfo.TaskId);
                        await taskInfo.Action(token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing recurring task {TaskId}", taskInfo.TaskId);
                    }
                    
                    // Calculate next execution time
                    taskInfo.NextExecutionTime = DateTime.UtcNow.Add(taskInfo.Interval);
                }
            }
            catch (OperationCanceledException)
            {
                // Task was cancelled, remove it
                _scheduledTasks.TryRemove(taskInfo.TaskId, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recurring task {TaskId}", taskInfo.TaskId);
                _scheduledTasks.TryRemove(taskInfo.TaskId, out _);
            }
        }

        /// <summary>
        /// Run a one-time task
        /// </summary>
        private async Task RunOneTimeTaskAsync(TaskInfo taskInfo)
        {
            var token = taskInfo.CancellationTokenSource.Token;
            
            try
            {
                var delay = taskInfo.NextExecutionTime - DateTime.UtcNow;
                
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, token);
                }

                if (token.IsCancellationRequested)
                    return;

                _logger.LogDebug("Executing one-time task {TaskId}", taskInfo.TaskId);
                await taskInfo.Action(token);
            }
            catch (OperationCanceledException)
            {
                // Task was cancelled, just remove it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in one-time task {TaskId}", taskInfo.TaskId);
            }
            finally
            {
                // Always remove one-time tasks after execution or failure
                _scheduledTasks.TryRemove(taskInfo.TaskId, out _);
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            
            if (disposing)
            {
                Stop();
                _shutdownCts.Dispose();
            }
            
            _disposed = true;
        }

        /// <summary>
        /// Information about a scheduled task
        /// </summary>
        private class TaskInfo
        {
            public string TaskId { get; set; }
            public bool IsRecurring { get; set; }
            public TimeSpan Interval { get; set; }
            public Func<CancellationToken, Task> Action { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public DateTime NextExecutionTime { get; set; }
        }
    }
}
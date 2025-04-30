using Ez.Generic.DataSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Service for managing and synchronizing airline notifications
    /// </summary>
    public class AirlineNotificationService : GenericSyncService<AirlineNotification>
    {
        private readonly List<string> _serviceLogEntries = new();
        private readonly int _maxRetryDelayMs = 30000; // 30 seconds max delay between retries
        private readonly int _baseRetryDelayMs = 1000; // 1 second base delay
        private bool _forceDelivery = false;
        private readonly NetworkSimulator _networkSimulator;
        private readonly GenericRepository<AirlineNotification> _repository;
        
        /// <summary>
        /// Gets the log entries for the notification service
        /// </summary>
        public IReadOnlyList<string> ServiceLogEntries => _serviceLogEntries.AsReadOnly();
        
        /// <summary>
        /// Gets the current network quality
        /// </summary>
        public NetworkQuality NetworkQuality => _networkSimulator.CurrentQuality;
        
        /// <summary>
        /// Initialize a new instance of the AirlineNotificationService
        /// </summary>
        /// <param name="syncService">The underlying sync service</param>
        /// <param name="repository">The repository for notifications</param>
        /// <param name="networkSimulator">The network simulator for testing</param>
        public AirlineNotificationService(
            ISyncService syncService,
            IRepository<SyncableEntityWrapper<AirlineNotification>> repository,
            NetworkSimulator networkSimulator) 
            : base(syncService, repository)
        {
            _networkSimulator = networkSimulator;
            _repository = GetRepository();
            
            // Configure more aggressive sync policy for critical notifications
            SetNetworkPolicy(new NetworkPolicy
            {
                MaxRetries = 20, // Higher retry count
                TimeoutSeconds = 60, // Longer timeout
                RequireWifi = false, // Can use any network
                AutoSyncOnNetworkAvailable = true // Automatically sync when network is available
            });
            
            // Log initial setup
            Log("AirlineNotificationService initialized with enhanced reliability settings");
        }
        
        /// <summary>
        /// Helper method to get an airline notification by ID
        /// </summary>
        /// <param name="id">Notification ID</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>The airline notification</returns>
        public async Task<AirlineNotification?> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            // Use the repository directly
            return await _repository.GetItemAsync(id, cancellationToken);
        }
        
        /// <summary>
        /// Helper method to get all airline notifications
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Collection of airline notifications</returns>
        public async Task<IEnumerable<AirlineNotification>> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            // Use the repository directly
            return await _repository.GetItemsAsync(cancellationToken);
        }
        
        /// <summary>
        /// Helper method to add an airline notification
        /// </summary>
        /// <param name="notification">The notification to add</param>
        /// <param name="id">Optional ID to use</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public async Task AddItemAsync(
            AirlineNotification notification, 
            string? id = null,
            CancellationToken cancellationToken = default)
        {
            // Use the repository directly
            await _repository.AddItemAsync(notification, id, cancellationToken);
        }
        
        /// <summary>
        /// Helper method to update an airline notification
        /// </summary>
        /// <param name="notification">The notification to update</param>
        /// <param name="id">Optional ID to use</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public async Task UpdateItemAsync(
            AirlineNotification notification, 
            string? id = null,
            CancellationToken cancellationToken = default)
        {
            // Use the repository directly
            await _repository.UpdateItemAsync(notification, id, cancellationToken);
        }
        
        /// <summary>
        /// Creates and sends a new notification
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="priority">Notification priority</param>
        /// <param name="category">Notification category</param>
        /// <param name="requiresAcknowledgment">Whether acknowledgment is required</param>
        /// <param name="expiresIn">Optional expiration timespan</param>
        /// <returns>The created notification ID</returns>
        public async Task<string> SendNotificationAsync(
            string title,
            string message,
            NotificationPriority priority = NotificationPriority.Normal,
            NotificationCategory category = NotificationCategory.General,
            bool requiresAcknowledgment = false,
            TimeSpan? expiresIn = null)
        {
            var notification = new AirlineNotification
            {
                Title = title,
                Message = message,
                Priority = priority,
                Category = category,
                RequiresAcknowledgment = requiresAcknowledgment,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresIn.HasValue ? DateTime.UtcNow.Add(expiresIn.Value) : null
            };
            
            // Higher priority notifications get more delivery attempts
            notification.MaxDeliveryAttempts = priority switch
            {
                NotificationPriority.Emergency => 50,
                NotificationPriority.Critical => 30,
                NotificationPriority.High => 20,
                NotificationPriority.Normal => 10,
                _ => 5
            };
            
            string id = Guid.NewGuid().ToString();
            
            // Set the ID for the notification
            notification.Id = id;
            
            // Add to repository
            await AddItemAsync(notification, id);
            
            Log($"Created notification [{notification.Priority}] '{notification.Title}' with ID: {id}");
            
            // Attempt immediate delivery
            _ = DeliverNotificationAsync(id, CancellationToken.None);
            
            return id;
        }
        
        /// <summary>
        /// Attempt to deliver a notification, with exponential backoff retry
        /// </summary>
        /// <param name="notificationId">The notification ID to deliver</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if delivered successfully</returns>
        public async Task<bool> DeliverNotificationAsync(string notificationId, CancellationToken cancellationToken)
        {
            var notification = await GetItemAsync(notificationId);
            if (notification == null)
            {
                Log($"Notification {notificationId} not found for delivery");
                return false;
            }
            
            if (notification.IsDelivered)
            {
                Log($"Notification {notificationId} already delivered");
                return true;
            }
            
            if (!notification.IsValid)
            {
                Log($"Notification {notificationId} has expired and will not be delivered");
                return false;
            }
            
            Log($"Attempting to deliver notification {notificationId} ({notification.Title})");
            
            // Emergency and Critical notifications force delivery even in poor network conditions
            _forceDelivery = notification.Priority >= NotificationPriority.Critical;
            
            // Keep trying until delivered or max attempts reached
            while (notification.DeliveryAttempts < notification.MaxDeliveryAttempts && !notification.IsDelivered)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Log($"Delivery of notification {notificationId} was cancelled");
                    return false;
                }
                
                notification.DeliveryAttempts++;
                
                try
                {
                    // Simulate network delay based on quality
                    await _networkSimulator.SimulateNetworkDelayAsync();
                    
                    if (_networkSimulator.CurrentQuality == NetworkQuality.Offline)
                    {
                        Log($"Delivery attempt {notification.DeliveryAttempts}/{notification.MaxDeliveryAttempts} " +
                            $"for notification {notificationId} failed: Network is offline");
                        
                        // Update notification in repository with attempt info
                        await UpdateItemAsync(notification, notificationId);
                        
                        // Exponential backoff with jitter for retries
                        int delayMs = Math.Min(
                            _maxRetryDelayMs, 
                            _baseRetryDelayMs * (int)Math.Pow(2, notification.DeliveryAttempts - 1));
                        
                        // Add jitter (±20% randomization)
                        Random random = new Random();
                        delayMs = (int)(delayMs * (0.8 + (random.NextDouble() * 0.4)));
                        
                        Log($"Waiting {delayMs}ms before next delivery attempt for notification {notificationId}");
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }
                    
                    // Success - deliver the notification
                    notification.IsDelivered = true;
                    notification.DeliveredAt = DateTime.UtcNow;
                    await UpdateItemAsync(notification, notificationId);
                    
                    Log($"Successfully delivered notification {notificationId} on attempt {notification.DeliveryAttempts}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"Error during delivery attempt for notification {notificationId}: {ex.Message}");
                    
                    // Only wait if we're going to retry
                    if (notification.DeliveryAttempts < notification.MaxDeliveryAttempts)
                    {
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            
            // Final update for max attempts reached
            if (!notification.IsDelivered)
            {
                Log($"Failed to deliver notification {notificationId} after {notification.DeliveryAttempts} attempts");
                await UpdateItemAsync(notification, notificationId);
            }
            
            return notification.IsDelivered;
        }
        
        /// <summary>
        /// Acknowledge receipt of a notification
        /// </summary>
        /// <param name="notificationId">ID of the notification to acknowledge</param>
        /// <returns>True if acknowledgment was successful</returns>
        public async Task<bool> AcknowledgeNotificationAsync(string notificationId)
        {
            var notification = await GetItemAsync(notificationId);
            if (notification == null)
            {
                Log($"Notification {notificationId} not found for acknowledgment");
                return false;
            }
            
            if (!notification.RequiresAcknowledgment)
            {
                Log($"Notification {notificationId} does not require acknowledgment");
                return true;
            }
            
            if (notification.AcknowledgedAt.HasValue)
            {
                Log($"Notification {notificationId} already acknowledged at {notification.AcknowledgedAt}");
                return true;
            }
            
            notification.AcknowledgedAt = DateTime.UtcNow;
            await UpdateItemAsync(notification, notificationId);
            
            Log($"Notification {notificationId} acknowledged successfully");
            
            // Sync the acknowledgment to server
            await SyncItemAsync(notificationId);
            
            return true;
        }
        
        /// <summary>
        /// Gets all pending notifications (delivered but not acknowledged)
        /// </summary>
        /// <returns>List of pending notifications</returns>
        public async Task<List<AirlineNotification>> GetPendingNotificationsAsync()
        {
            var allNotifications = await GetItemsAsync();
            return allNotifications
                .Where(n => n.IsDelivered && n.RequiresAcknowledgment && !n.AcknowledgedAt.HasValue)
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.DeliveredAt)
                .ToList();
        }
        
        /// <summary>
        /// Gets all undelivered notifications
        /// </summary>
        /// <returns>List of undelivered notifications</returns>
        public async Task<List<AirlineNotification>> GetUndeliveredNotificationsAsync()
        {
            var allNotifications = await GetItemsAsync();
            return allNotifications
                .Where(n => !n.IsDelivered && n.IsValid)
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.CreatedAt)
                .ToList();
        }
        
        /// <summary>
        /// Override of the SyncItemAsync method to handle forced delivery of critical notifications
        /// </summary>
        public override async Task<SyncResult> SyncItemAsync(string id, CancellationToken token = default)
        {
            if (_forceDelivery)
            {
                // For critical notifications, temporarily boost network quality if needed
                var originalQuality = _networkSimulator.CurrentQuality;
                if ((int)originalQuality > (int)NetworkQuality.Fair)
                {
                    Log($"Temporarily boosting network quality for critical notification delivery");
                    _networkSimulator.CurrentQuality = NetworkQuality.Fair;
                }
                
                try
                {
                    return await base.SyncItemAsync(id, token);
                }
                finally
                {
                    // Restore original network quality
                    if ((int)originalQuality > (int)NetworkQuality.Fair)
                    {
                        _networkSimulator.CurrentQuality = originalQuality;
                    }
                    _forceDelivery = false;
                }
            }
            else
            {
                return await base.SyncItemAsync(id, token);
            }
        }
        
        /// <summary>
        /// Adds a log entry
        /// </summary>
        /// <param name="message">Log message</param>
        private void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] {message}";
            _serviceLogEntries.Add(logEntry);
            Console.WriteLine(logEntry);
        }
    }
}

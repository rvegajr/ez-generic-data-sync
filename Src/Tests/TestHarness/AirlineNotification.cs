using System;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Represents an airline notification that needs to be reliably delivered to pilots
    /// </summary>
    public class AirlineNotification
    {
        /// <summary>
        /// Gets or sets the unique identifier for this notification
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Gets or sets the notification title
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the notification message content
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the notification priority
        /// </summary>
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        
        /// <summary>
        /// Gets or sets the notification category
        /// </summary>
        public NotificationCategory Category { get; set; } = NotificationCategory.General;
        
        /// <summary>
        /// Gets or sets the timestamp when the notification was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets whether the notification requires acknowledgment
        /// </summary>
        public bool RequiresAcknowledgment { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the notification was acknowledged (if applicable)
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }
        
        /// <summary>
        /// Gets or sets whether the notification has been delivered
        /// </summary>
        public bool IsDelivered { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the notification was delivered (if applicable)
        /// </summary>
        public DateTime? DeliveredAt { get; set; }
        
        /// <summary>
        /// Gets or sets the retry count for delivery attempts
        /// </summary>
        public int DeliveryAttempts { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum allowed delivery attempts
        /// </summary>
        public int MaxDeliveryAttempts { get; set; } = 10;
        
        /// <summary>
        /// Gets or sets the expiration time for the notification
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether the notification is still valid (not expired)
        /// </summary>
        public bool IsValid => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
        
        /// <summary>
        /// Gets a string representation of the notification
        /// </summary>
        public override string ToString()
        {
            return $"[{Priority}] {Title}" +
                   (IsDelivered ? " ✓" : "") +
                   (RequiresAcknowledgment && AcknowledgedAt.HasValue ? " (Acknowledged)" : "");
        }
    }
    
    /// <summary>
    /// Notification priority levels
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// Low priority notification
        /// </summary>
        Low,
        
        /// <summary>
        /// Normal priority notification
        /// </summary>
        Normal,
        
        /// <summary>
        /// High priority notification
        /// </summary>
        High,
        
        /// <summary>
        /// Critical priority notification
        /// </summary>
        Critical,
        
        /// <summary>
        /// Emergency priority notification
        /// </summary>
        Emergency
    }
    
    /// <summary>
    /// Notification categories
    /// </summary>
    public enum NotificationCategory
    {
        /// <summary>
        /// General information
        /// </summary>
        General,
        
        /// <summary>
        /// Weather related notification
        /// </summary>
        Weather,
        
        /// <summary>
        /// Flight plan change
        /// </summary>
        FlightPlanChange,
        
        /// <summary>
        /// Schedule change
        /// </summary>
        ScheduleChange,
        
        /// <summary>
        /// Maintenance alert
        /// </summary>
        MaintenanceAlert,
        
        /// <summary>
        /// Security notification
        /// </summary>
        Security,
        
        /// <summary>
        /// Emergency notification
        /// </summary>
        Emergency
    }
}

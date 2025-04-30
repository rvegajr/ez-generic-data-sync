using System;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Simple model class for testing
    /// </summary>
    public class TodoItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the todo item
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the title of the todo item
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the description of the todo item
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the due date of the todo item
        /// </summary>
        public DateTime? DueDate { get; set; }
        
        /// <summary>
        /// Gets or sets whether the todo item is completed
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// Gets or sets the priority of the todo item
        /// </summary>
        public Priority Priority { get; set; } = Priority.Normal;
        
        /// <summary>
        /// Gets or sets the creation date of the todo item
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Returns a string representation of the todo item
        /// </summary>
        public override string ToString()
        {
            return $"{Title} [{Priority}] - {(IsCompleted ? "Completed" : "Pending")}" + 
                   (DueDate.HasValue ? $" Due: {DueDate:yyyy-MM-dd}" : "");
        }
    }
    
    /// <summary>
    /// Priority level enum
    /// </summary>
    public enum Priority
    {
        /// <summary>
        /// Low priority
        /// </summary>
        Low,
        
        /// <summary>
        /// Normal priority
        /// </summary>
        Normal,
        
        /// <summary>
        /// High priority
        /// </summary>
        High,
        
        /// <summary>
        /// Urgent priority
        /// </summary>
        Urgent
    }
}

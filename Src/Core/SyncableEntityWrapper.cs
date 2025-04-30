using System;

namespace Ez.Generic.DataSync.Core
{
    /// <summary>
    /// Implements a generic wrapper for any class that needs to be synchronized via Datasync
    /// </summary>
    /// <typeparam name="T">The type of object being wrapped</typeparam>
    public class SyncableEntityWrapper<T> : ISyncableEntity<T> where T : class
    {
        /// <summary>
        /// Gets the wrapped domain object
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// Gets or sets the unique identifier for this entity
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this entity was last updated
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entity has been deleted
        /// </summary>
        public bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the version identifier for this entity
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncableEntityWrapper{T}"/> class.
        /// </summary>
        /// <param name="data">The domain object to wrap</param>
        /// <param name="id">Optional custom ID (generates a new ID if not provided)</param>
        public SyncableEntityWrapper(T data, string? id = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Id = id ?? Guid.NewGuid().ToString();
            UpdatedAt = DateTimeOffset.UtcNow;
            Deleted = false;
        }

        /// <summary>
        /// Updates the wrapped domain object with data from the provided source
        /// </summary>
        /// <param name="source">The source object containing updated data</param>
        public void UpdateFrom(T source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // Update the wrapped data
            Data = source;
            
            // Update the timestamp
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Marks this entity as deleted
        /// </summary>
        public void MarkAsDeleted()
        {
            Deleted = true;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}

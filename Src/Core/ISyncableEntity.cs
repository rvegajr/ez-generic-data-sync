using CommunityToolkit.Datasync.Client;

namespace Ez.Generic.DataSync.Core
{
    /// <summary>
    /// Generic interface for syncable entities that wrap a domain object of type T
    /// </summary>
    /// <typeparam name="T">The type of domain object being wrapped</typeparam>
    public interface ISyncableEntity<T> where T : class
    {
        /// <summary>
        /// Gets the wrapped domain object
        /// </summary>
        T Data { get; }
        
        /// <summary>
        /// Gets or sets the unique identifier for this entity
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this entity was last updated
        /// </summary>
        DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this entity has been deleted
        /// </summary>
        bool Deleted { get; set; }

        /// <summary>
        /// Gets or sets the version identifier for this entity
        /// </summary>
        string Version { get; set; }
        
        /// <summary>
        /// Updates the wrapped domain object with data from the provided source
        /// </summary>
        /// <param name="source">The source object containing updated data</param>
        void UpdateFrom(T source);
    }
}

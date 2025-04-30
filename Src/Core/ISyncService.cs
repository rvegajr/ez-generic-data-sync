using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.Core
{
    /// <summary>
    /// Enumeration of possible synchronization statuses
    /// </summary>
    public enum SyncStatus
    {
        /// <summary>
        /// The synchronization has not started yet
        /// </summary>
        NotStarted,
        
        /// <summary>
        /// The synchronization is in progress
        /// </summary>
        InProgress,
        
        /// <summary>
        /// The synchronization completed successfully
        /// </summary>
        Completed,
        
        /// <summary>
        /// The synchronization failed
        /// </summary>
        Failed,
        
        /// <summary>
        /// The synchronization was cancelled
        /// </summary>
        Cancelled,
        
        /// <summary>
        /// The synchronization encountered conflicts that need resolution
        /// </summary>
        Conflict
    }
    
    /// <summary>
    /// Result of a synchronization operation
    /// </summary>
    public class SyncResult
    {
        /// <summary>
        /// Gets or sets the status of the synchronization
        /// </summary>
        public SyncStatus Status { get; set; } = SyncStatus.NotStarted;
        
        /// <summary>
        /// Gets or sets the number of items processed during synchronization
        /// </summary>
        public int ItemCount { get; set; }
        
        /// <summary>
        /// Gets or sets an error message if the synchronization failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// Network policy configuration for sync operations
    /// </summary>
    public record NetworkPolicy
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts
        /// </summary>
        public int MaxRetries { get; init; } = 3;
        
        /// <summary>
        /// Gets or sets the timeout in seconds for sync operations
        /// </summary>
        public int TimeoutSeconds { get; init; } = 30;
        
        /// <summary>
        /// Gets or sets whether to sync only if on WiFi
        /// </summary>
        public bool RequireWifi { get; init; } = false;
        
        /// <summary>
        /// Gets or sets whether to sync automatically when network becomes available
        /// </summary>
        public bool AutoSyncOnNetworkAvailable { get; init; } = true;
        
        /// <summary>
        /// Gets the default network policy
        /// </summary>
        public static NetworkPolicy Default => new NetworkPolicy();
    }
    
    /// <summary>
    /// Interface for sync service operations
    /// </summary>
    public interface ISyncService
    {
        /// <summary>
        /// Pulls changes from the server
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        Task<SyncResult> PullAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Pushes local changes to the server
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        Task<SyncResult> PushAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Synchronizes a specific item by ID
        /// </summary>
        /// <param name="id">The ID of the item to synchronize</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        Task<SyncResult> SyncItemAsync(string id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sets the network policy for sync operations
        /// </summary>
        /// <param name="policy">The network policy to apply</param>
        void SetNetworkPolicy(NetworkPolicy policy);
    }
}

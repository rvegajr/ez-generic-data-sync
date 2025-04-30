using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.Core
{
    /// <summary>
    /// Generic sync service that provides type-safe synchronization operations
    /// </summary>
    /// <typeparam name="T">The type of domain object to synchronize</typeparam>
    public class GenericSyncService<T> where T : class
    {
        private readonly ISyncService _innerService;
        private readonly IRepository<SyncableEntityWrapper<T>> _innerRepository;
        private GenericRepository<T>? _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSyncService{T}"/> class.
        /// </summary>
        /// <param name="innerService">The underlying sync service</param>
        /// <param name="innerRepository">The underlying repository for wrapped entities</param>
        public GenericSyncService(ISyncService innerService, IRepository<SyncableEntityWrapper<T>> innerRepository)
        {
            _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        /// <summary>
        /// Gets a type-safe repository for the domain objects
        /// </summary>
        public GenericRepository<T> Repository => _repository ??= GetRepository();

        /// <summary>
        /// Creates a generic repository for the domain objects
        /// </summary>
        /// <returns>A generic repository</returns>
        public virtual GenericRepository<T> GetRepository()
        {
            return new GenericRepository<T>(_innerRepository);
        }

        /// <summary>
        /// Pulls changes from the server
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        public virtual Task<SyncResult> PullAsync(CancellationToken cancellationToken = default)
        {
            return _innerService.PullAsync(cancellationToken);
        }

        /// <summary>
        /// Pushes local changes to the server
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        public virtual Task<SyncResult> PushAsync(CancellationToken cancellationToken = default)
        {
            return _innerService.PushAsync(cancellationToken);
        }

        /// <summary>
        /// Synchronizes a specific item by ID
        /// </summary>
        /// <param name="id">The ID of the item to synchronize</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        public virtual Task<SyncResult> SyncItemAsync(string id, CancellationToken cancellationToken = default)
        {
            return _innerService.SyncItemAsync(id, cancellationToken);
        }

        /// <summary>
        /// Sets the network policy for sync operations
        /// </summary>
        /// <param name="policy">The network policy to apply</param>
        public virtual void SetNetworkPolicy(NetworkPolicy policy)
        {
            _innerService.SetNetworkPolicy(policy);
        }
    }
}

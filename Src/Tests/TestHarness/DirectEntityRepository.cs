using Ez.Generic.DataSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Repository adapter that allows direct entity usage without SyncableEntityWrapper for testing
    /// </summary>
    /// <typeparam name="T">The type of entity in the repository</typeparam>
    public class DirectEntityRepository<T> : IRepository<SyncableEntityWrapper<T>> where T : class
    {
        private readonly IRepository<T> _innerRepository;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectEntityRepository{T}"/> class.
        /// </summary>
        /// <param name="innerRepository">The inner repository that stores direct entities</param>
        public DirectEntityRepository(IRepository<T> innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }
        
        /// <summary>
        /// Gets an item by its identifier
        /// </summary>
        public async Task<SyncableEntityWrapper<T>?> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _innerRepository.GetItemAsync(id, cancellationToken);
            return entity != null ? new SyncableEntityWrapper<T>(entity, id) : null;
        }
        
        /// <summary>
        /// Gets all items in the repository
        /// </summary>
        public async Task<IEnumerable<SyncableEntityWrapper<T>>> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _innerRepository.GetItemsAsync(cancellationToken);
            
            // Use reflection to get the ID for each entity if possible
            return entities.Select(entity => {
                string? entityId = null;
                var idProperty = entity.GetType().GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(string))
                {
                    entityId = idProperty.GetValue(entity) as string;
                }
                
                return new SyncableEntityWrapper<T>(entity, entityId);
            });
        }
        
        /// <summary>
        /// Adds a new item to the repository
        /// </summary>
        public async Task AddItemAsync(SyncableEntityWrapper<T> item, string? id = null, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            await _innerRepository.AddItemAsync(item.Data, id ?? item.Id, cancellationToken);
        }
        
        /// <summary>
        /// Updates an existing item in the repository
        /// </summary>
        public async Task UpdateItemAsync(SyncableEntityWrapper<T> item, string? id = null, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            await _innerRepository.UpdateItemAsync(item.Data, id ?? item.Id, cancellationToken);
        }
        
        /// <summary>
        /// Deletes an item from the repository
        /// </summary>
        public async Task DeleteItemAsync(string id, CancellationToken cancellationToken = default)
        {
            await _innerRepository.DeleteItemAsync(id, cancellationToken);
        }
    }
}

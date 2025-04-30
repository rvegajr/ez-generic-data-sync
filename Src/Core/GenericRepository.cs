using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.Core
{
    /// <summary>
    /// Generic repository implementation that wraps domain objects with SyncableEntityWrapper
    /// </summary>
    /// <typeparam name="T">The type of domain object managed by the repository</typeparam>
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        private readonly IRepository<SyncableEntityWrapper<T>> _innerRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericRepository{T}"/> class.
        /// </summary>
        /// <param name="innerRepository">The underlying repository that stores wrapped entities</param>
        public GenericRepository(IRepository<SyncableEntityWrapper<T>> innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        /// <summary>
        /// Gets an item by its identifier
        /// </summary>
        /// <param name="id">The unique identifier of the item</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The unwrapped domain object if found; otherwise, null</returns>
        public async Task<T?> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            var wrapper = await _innerRepository.GetItemAsync(id, cancellationToken);
            return wrapper?.Deleted == false ? wrapper.Data : null;
        }

        /// <summary>
        /// Gets all non-deleted items in the repository
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A collection of unwrapped domain objects</returns>
        public async Task<IEnumerable<T>> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            var wrappers = await _innerRepository.GetItemsAsync(cancellationToken);
            return wrappers
                .Where(w => !w.Deleted)
                .Select(w => w.Data);
        }

        /// <summary>
        /// Adds a new item to the repository
        /// </summary>
        /// <param name="item">The domain object to add</param>
        /// <param name="id">Optional custom ID for the item</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        public async Task AddItemAsync(T item, string? id = null, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var wrapper = new SyncableEntityWrapper<T>(item, id);
            await _innerRepository.AddItemAsync(wrapper, id, cancellationToken);
        }

        /// <summary>
        /// Updates an existing item in the repository
        /// </summary>
        /// <param name="item">The updated domain object</param>
        /// <param name="id">Optional identifier for the item</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        public async Task UpdateItemAsync(T item, string? id = null, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // Try to determine the ID if not provided
            string itemId = id ?? GetItemId(item);
            if (string.IsNullOrEmpty(itemId))
                throw new ArgumentException("Item ID must be provided or item must have an ID property", nameof(id));

            var existingWrapper = await _innerRepository.GetItemAsync(itemId, cancellationToken);
            if (existingWrapper == null)
                throw new KeyNotFoundException($"Item with ID '{itemId}' not found.");

            existingWrapper.UpdateFrom(item);
            await _innerRepository.UpdateItemAsync(existingWrapper, itemId, cancellationToken);
        }

        /// <summary>
        /// Deletes an item from the repository (marks it as deleted)
        /// </summary>
        /// <param name="id">The unique identifier of the item to delete</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        public async Task DeleteItemAsync(string id, CancellationToken cancellationToken = default)
        {
            var existingWrapper = await _innerRepository.GetItemAsync(id, cancellationToken);
            if (existingWrapper == null)
                throw new KeyNotFoundException($"Item with ID '{id}' not found.");

            existingWrapper.MarkAsDeleted();
            await _innerRepository.UpdateItemAsync(existingWrapper, id, cancellationToken);
        }

        /// <summary>
        /// Attempts to get the ID of an item using reflection
        /// </summary>
        /// <param name="item">The item to get the ID for</param>
        /// <returns>The ID if found; otherwise, an empty string</returns>
        private string GetItemId(T item)
        {
            // Try to get the ID using reflection
            var idProperty = item.GetType().GetProperty("Id");
            if (idProperty != null && idProperty.PropertyType == typeof(string))
            {
                return idProperty.GetValue(item) as string ?? string.Empty;
            }
            
            return string.Empty;
        }
    }
}

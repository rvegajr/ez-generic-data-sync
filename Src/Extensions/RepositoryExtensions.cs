using Ez.Generic.DataSync.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.Extensions
{
    /// <summary>
    /// Extension methods for repositories to provide syntactic sugar
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Gets a strongly-typed item by its ID
        /// </summary>
        /// <typeparam name="T">The type of item to retrieve</typeparam>
        /// <param name="repository">The repository</param>
        /// <param name="id">The item's unique ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The item, or null if not found</returns>
        public static Task<T?> GetItemAsync<T>(
            this IRepository<SyncableEntityWrapper<T>> repository, 
            string id,
            CancellationToken cancellationToken = default) where T : class
        {
            var genRepository = new GenericRepository<T>(repository);
            return genRepository.GetItemAsync(id, cancellationToken);
        }
        
        /// <summary>
        /// Gets all non-deleted items from the repository
        /// </summary>
        /// <typeparam name="T">The type of items to retrieve</typeparam>
        /// <param name="repository">The repository</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of items</returns>
        public static Task<IEnumerable<T>> GetItemsAsync<T>(
            this IRepository<SyncableEntityWrapper<T>> repository,
            CancellationToken cancellationToken = default) where T : class
        {
            var genRepository = new GenericRepository<T>(repository);
            return genRepository.GetItemsAsync(cancellationToken);
        }
        
        /// <summary>
        /// Adds a new item to the repository
        /// </summary>
        /// <typeparam name="T">The type of item to add</typeparam>
        /// <param name="repository">The repository</param>
        /// <param name="item">The item to add</param>
        /// <param name="id">Optional custom ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task AddItemAsync<T>(
            this IRepository<SyncableEntityWrapper<T>> repository,
            T item,
            string? id = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var genRepository = new GenericRepository<T>(repository);
            return genRepository.AddItemAsync(item, id, cancellationToken);
        }
        
        /// <summary>
        /// Updates an existing item in the repository
        /// </summary>
        /// <typeparam name="T">The type of item to update</typeparam>
        /// <param name="repository">The repository</param>
        /// <param name="item">The updated item</param>
        /// <param name="id">Optional ID of the item to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task UpdateItemAsync<T>(
            this IRepository<SyncableEntityWrapper<T>> repository,
            T item,
            string? id = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var genRepository = new GenericRepository<T>(repository);
            return genRepository.UpdateItemAsync(item, id, cancellationToken);
        }
        
        /// <summary>
        /// Deletes an item from the repository (marks it as deleted)
        /// </summary>
        /// <typeparam name="T">The type of item to delete</typeparam>
        /// <param name="repository">The repository</param>
        /// <param name="id">The ID of the item to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static Task DeleteItemAsync<T>(
            this IRepository<SyncableEntityWrapper<T>> repository,
            string id,
            CancellationToken cancellationToken = default) where T : class
        {
            var genRepository = new GenericRepository<T>(repository);
            return genRepository.DeleteItemAsync(id, cancellationToken);
        }
    }
}

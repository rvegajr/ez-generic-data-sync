using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.Core
{
    /// <summary>
    /// Generic repository interface for data operations
    /// </summary>
    /// <typeparam name="T">The type of entity managed by the repository</typeparam>
    public interface IRepository<T>
    {
        /// <summary>
        /// Gets an item by its identifier
        /// </summary>
        /// <param name="id">The unique identifier of the item</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>The item if found; otherwise, null</returns>
        Task<T> GetItemAsync(string id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets all items in the repository
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>A collection of items</returns>
        Task<IEnumerable<T>> GetItemsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Adds a new item to the repository
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="id">Optional identifier for the item; if null, the item should have its own ID property</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        Task AddItemAsync(T item, string id = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Updates an existing item in the repository
        /// </summary>
        /// <param name="item">The item to update</param>
        /// <param name="id">Optional identifier for the item; if null, the item should have its own ID property</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        Task UpdateItemAsync(T item, string id = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Deletes an item from the repository
        /// </summary>
        /// <param name="id">The unique identifier of the item to delete</param>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        Task DeleteItemAsync(string id, CancellationToken cancellationToken = default);
    }
}

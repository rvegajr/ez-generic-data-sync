using Ez.Generic.DataSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Mock in-memory repository for testing
    /// </summary>
    /// <typeparam name="T">The type of entity in the repository</typeparam>
    public class MockRepository<T> : IRepository<T> where T : class
    {
        private readonly Dictionary<string, T> _items = new();
        
        /// <summary>
        /// Gets an item by its identifier
        /// </summary>
        public Task<T> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            if (_items.TryGetValue(id, out var item))
            {
                return Task.FromResult(item);
            }
            
            return Task.FromResult<T>(null);
        }

        /// <summary>
        /// Gets all items in the repository
        /// </summary>
        public Task<IEnumerable<T>> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<T>>(_items.Values.ToList());
        }

        /// <summary>
        /// Adds a new item to the repository
        /// </summary>
        public Task AddItemAsync(T item, string id = null, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            // For AirlineNotification, use the Id property
            string itemId = id;
            if (item is AirlineNotification notification && string.IsNullOrEmpty(itemId))
            {
                itemId = notification.Id;
            }
            
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = Guid.NewGuid().ToString();
                
                // If item has an Id property, set it via reflection
                var idProperty = item.GetType().GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(string))
                {
                    idProperty.SetValue(item, itemId);
                }
            }
            
            if (_items.ContainsKey(itemId))
            {
                throw new InvalidOperationException($"Item with ID '{itemId}' already exists in the repository.");
            }
            
            _items[itemId] = item;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates an existing item in the repository
        /// </summary>
        public Task UpdateItemAsync(T item, string id = null, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            // For AirlineNotification, use the Id property
            string itemId = id;
            if (item is AirlineNotification notification && string.IsNullOrEmpty(itemId))
            {
                itemId = notification.Id;
            }
            
            if (string.IsNullOrEmpty(itemId))
            {
                // If item has an Id property, get it via reflection
                var idProperty = item.GetType().GetProperty("Id");
                if (idProperty != null && idProperty.PropertyType == typeof(string))
                {
                    itemId = idProperty.GetValue(item) as string;
                }
            }
            
            if (string.IsNullOrEmpty(itemId) || !_items.ContainsKey(itemId))
            {
                throw new KeyNotFoundException($"Item with ID '{itemId}' not found in the repository.");
            }
            
            _items[itemId] = item;
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Deletes an item from the repository
        /// </summary>
        public Task DeleteItemAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id) || !_items.ContainsKey(id))
            {
                throw new KeyNotFoundException($"Item with ID '{id}' not found in the repository.");
            }
            
            _items.Remove(id);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Clears all items from the repository
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }
        
        /// <summary>
        /// Gets the total number of items in the repository
        /// </summary>
        public int Count => _items.Count;
        
        /// <summary>
        /// Gets the number of non-deleted items in the repository
        /// </summary>
        public int ActiveCount => _items.Count;
    }
}

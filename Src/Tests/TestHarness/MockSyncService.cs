using Ez.Generic.DataSync.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Mock implementation of ISyncService for testing
    /// </summary>
    public class MockSyncService : ISyncService
    {
        private NetworkPolicy _networkPolicy = NetworkPolicy.Default;
        private readonly Random _random = new Random();
        private readonly NetworkSimulator _networkSimulator;
        private readonly List<string> _syncedItems = new();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MockSyncService"/> class.
        /// </summary>
        /// <param name="networkSimulator">Network simulator to use</param>
        public MockSyncService(NetworkSimulator networkSimulator)
        {
            _networkSimulator = networkSimulator ?? throw new ArgumentNullException(nameof(networkSimulator));
        }
        
        /// <summary>
        /// Gets the list of synced item IDs
        /// </summary>
        public IReadOnlyList<string> SyncedItems => _syncedItems;
        
        /// <summary>
        /// Gets or sets whether to simulate conflicts during sync
        /// </summary>
        public bool SimulateConflicts { get; set; }
        
        /// <summary>
        /// Gets or sets whether to simulate random failures
        /// </summary>
        public bool SimulateRandomFailures { get; set; }

        /// <summary>
        /// Pulls changes from the server
        /// </summary>
        public async Task<SyncResult> PullAsync(CancellationToken cancellationToken = default)
        {
            // Check if we should simulate a failure
            if (SimulateRandomFailures && _random.Next(10) < 3)
            {
                return new SyncResult 
                { 
                    Status = SyncStatus.Failed, 
                    ErrorMessage = "Simulated random failure during pull"
                };
            }
            
            // Check if network is offline
            if (_networkSimulator.CurrentQuality == NetworkQuality.Offline)
            {
                return new SyncResult 
                { 
                    Status = SyncStatus.Failed,
                    ErrorMessage = "Network is offline"
                };
            }
            
            // Check if we should simulate a conflict
            if (SimulateConflicts && _random.Next(10) < 3)
            {
                return new SyncResult
                {
                    Status = SyncStatus.Conflict,
                    ItemCount = _random.Next(1, 4),
                    ErrorMessage = "Conflict detected during pull operation"
                };
            }
            
            // Simulate successful pull with random number of items
            var itemCount = _random.Next(1, 10);
            await Task.Delay(50 + _random.Next(100)); // Add some randomness to the operation time
            
            for (int i = 0; i < itemCount; i++)
            {
                var id = $"pulled-item-{Guid.NewGuid()}";
                _syncedItems.Add(id);
            }
            
            return new SyncResult
            {
                Status = SyncStatus.Completed,
                ItemCount = itemCount
            };
        }

        /// <summary>
        /// Pushes local changes to the server
        /// </summary>
        public async Task<SyncResult> PushAsync(CancellationToken cancellationToken = default)
        {
            // Check if we should simulate a failure
            if (SimulateRandomFailures && _random.Next(10) < 3)
            {
                return new SyncResult 
                { 
                    Status = SyncStatus.Failed, 
                    ErrorMessage = "Simulated random failure during push"
                };
            }
            
            // Check if network is offline
            if (_networkSimulator.CurrentQuality == NetworkQuality.Offline)
            {
                return new SyncResult 
                { 
                    Status = SyncStatus.Failed,
                    ErrorMessage = "Network is offline"
                };
            }
            
            // Check if we should simulate a conflict
            if (SimulateConflicts && _random.Next(10) < 3)
            {
                return new SyncResult
                {
                    Status = SyncStatus.Conflict,
                    ItemCount = _random.Next(1, 4),
                    ErrorMessage = "Conflict detected during push operation"
                };
            }
            
            // Simulate successful push with random number of items
            var itemCount = _random.Next(1, 5);
            await Task.Delay(50 + _random.Next(150)); // Add some randomness to the operation time
            
            for (int i = 0; i < itemCount; i++)
            {
                var id = $"pushed-item-{Guid.NewGuid()}";
                _syncedItems.Add(id);
            }
            
            return new SyncResult
            {
                Status = SyncStatus.Completed,
                ItemCount = itemCount
            };
        }

        /// <summary>
        /// Synchronizes a specific item by ID
        /// </summary>
        public async Task<SyncResult> SyncItemAsync(string id, CancellationToken cancellationToken = default)
        {
            // Check if we should simulate a failure
            if (SimulateRandomFailures && _random.Next(10) < 3)
            {
                return new SyncResult 
                { 
                    Status = SyncStatus.Failed, 
                    ErrorMessage = $"Simulated random failure syncing item {id}"
                };
            }
            
            // Check if network is offline
            if (_networkSimulator.CurrentQuality == NetworkQuality.Offline)
            {
                return new SyncResult 
                { 
                    Status = SyncStatus.Failed,
                    ErrorMessage = "Network is offline"
                };
            }
            
            // Check if we should simulate a conflict
            if (SimulateConflicts && _random.Next(10) < 3)
            {
                return new SyncResult
                {
                    Status = SyncStatus.Conflict,
                    ItemCount = 1,
                    ErrorMessage = $"Conflict detected syncing item {id}"
                };
            }
            
            await Task.Delay(50 + _random.Next(100)); // Add some randomness to the operation time
            _syncedItems.Add(id);
            
            return new SyncResult
            {
                Status = SyncStatus.Completed,
                ItemCount = 1
            };
        }

        /// <summary>
        /// Sets the network policy for sync operations
        /// </summary>
        public void SetNetworkPolicy(NetworkPolicy policy)
        {
            _networkPolicy = policy ?? throw new ArgumentNullException(nameof(policy));
        }
    }
}

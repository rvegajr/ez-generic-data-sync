using Ez.Generic.DataSync.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Test harness that extends the production sync service
    /// Following the prime directive of extending actual production code for testing
    /// </summary>
    /// <typeparam name="T">The type of entity to synchronize</typeparam>
    public class SyncServiceTester<T> : GenericSyncService<T> where T : class
    {
        private readonly List<string> _logEntries = new();
        private NetworkPolicy _currentPolicy = NetworkPolicy.Default;
        private NetworkSimulator _networkSimulator;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SyncServiceTester{T}"/> class.
        /// </summary>
        /// <param name="innerService">The inner sync service</param>
        /// <param name="innerRepository">The inner repository</param>
        /// <param name="networkSimulator">The network simulator for testing</param>
        public SyncServiceTester(
            ISyncService innerService, 
            IRepository<SyncableEntityWrapper<T>> innerRepository,
            NetworkSimulator networkSimulator) 
            : base(innerService, innerRepository)
        {
            _networkSimulator = networkSimulator ?? throw new ArgumentNullException(nameof(networkSimulator));
        }

        /// <summary>
        /// Gets log entries from sync operations
        /// </summary>
        public IReadOnlyList<string> LogEntries => _logEntries;

        /// <summary>
        /// Gets the current network quality
        /// </summary>
        public NetworkQuality NetworkQuality => _networkSimulator.CurrentQuality;

        /// <summary>
        /// Sets the network quality for testing
        /// </summary>
        /// <param name="quality">The network quality to simulate</param>
        public void SetNetworkQuality(NetworkQuality quality)
        {
            _networkSimulator.CurrentQuality = quality;
            LogOperation($"Network quality set to {quality}");
        }

        /// <summary>
        /// Pulls changes from the server with instrumentation
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        public override async Task<SyncResult> PullAsync(CancellationToken cancellationToken = default)
        {
            LogOperation($"Starting Pull operation for {typeof(T).Name} with {_networkSimulator.CurrentQuality} network");
            
            try
            {
                // Simulate network delay based on quality
                await _networkSimulator.SimulateNetworkDelayAsync();
                
                if (_networkSimulator.CurrentQuality == NetworkQuality.Offline)
                {
                    LogOperation("Pull failed - network is offline");
                    return new SyncResult { Status = SyncStatus.Failed, ErrorMessage = "Network is offline" };
                }
                
                var result = await base.PullAsync(cancellationToken);
                LogOperation($"Pull completed with status {result.Status}, items: {result.ItemCount}");
                return result;
            }
            catch (Exception ex)
            {
                LogOperation($"Pull failed with error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pushes local changes to the server with instrumentation
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation</param>
        /// <returns>Result of the sync operation</returns>
        public override async Task<SyncResult> PushAsync(CancellationToken cancellationToken = default)
        {
            LogOperation($"Starting Push operation for {typeof(T).Name} with {_networkSimulator.CurrentQuality} network");
            
            try
            {
                // Simulate network delay based on quality
                await _networkSimulator.SimulateNetworkDelayAsync();
                
                if (_networkSimulator.CurrentQuality == NetworkQuality.Offline)
                {
                    LogOperation("Push failed - network is offline");
                    return new SyncResult { Status = SyncStatus.Failed, ErrorMessage = "Network is offline" };
                }
                
                var result = await base.PushAsync(cancellationToken);
                LogOperation($"Push completed with status {result.Status}, items: {result.ItemCount}");
                return result;
            }
            catch (Exception ex)
            {
                LogOperation($"Push failed with error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sets the network policy with instrumentation
        /// </summary>
        /// <param name="policy">The policy to set</param>
        public override void SetNetworkPolicy(NetworkPolicy policy)
        {
            _currentPolicy = policy;
            LogOperation($"Network policy updated - MaxRetries: {policy.MaxRetries}, Timeout: {policy.TimeoutSeconds}s");
            base.SetNetworkPolicy(policy);
        }

        private void LogOperation(string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            _logEntries.Add(logEntry);
            Console.WriteLine(logEntry);
        }
    }
    
    /// <summary>
    /// Simulates different network conditions for testing
    /// </summary>
    public class NetworkSimulator
    {
        private static readonly Dictionary<NetworkQuality, int> DelayMilliseconds = new()
        {
            { NetworkQuality.Excellent, 10 },
            { NetworkQuality.Good, 100 },
            { NetworkQuality.Fair, 300 },
            { NetworkQuality.Poor, 800 },
            { NetworkQuality.Terrible, 2000 },
            { NetworkQuality.Offline, 0 }  // No delay for offline, just fails immediately
        };
        
        /// <summary>
        /// Gets or sets the current network quality
        /// </summary>
        public NetworkQuality CurrentQuality { get; set; } = NetworkQuality.Good;
        
        /// <summary>
        /// Simulates network delay based on the current quality
        /// </summary>
        public async Task SimulateNetworkDelayAsync()
        {
            if (CurrentQuality != NetworkQuality.Offline)
            {
                await Task.Delay(DelayMilliseconds[CurrentQuality]);
            }
        }
        
        /// <summary>
        /// Simulates a network dropout
        /// </summary>
        /// <param name="durationMs">Duration of the dropout in milliseconds</param>
        public async Task SimulateNetworkDropoutAsync(int durationMs)
        {
            var previousQuality = CurrentQuality;
            CurrentQuality = NetworkQuality.Offline;
            await Task.Delay(durationMs);
            CurrentQuality = previousQuality;
        }
    }
    
    /// <summary>
    /// Network quality enum for simulation
    /// </summary>
    public enum NetworkQuality
    {
        /// <summary>
        /// Excellent network quality (~10ms latency)
        /// </summary>
        Excellent,
        
        /// <summary>
        /// Good network quality (~100ms latency)
        /// </summary>
        Good,
        
        /// <summary>
        /// Fair network quality (~300ms latency)
        /// </summary>
        Fair,
        
        /// <summary>
        /// Poor network quality (~800ms latency)
        /// </summary>
        Poor,
        
        /// <summary>
        /// Terrible network quality (~2000ms latency)
        /// </summary>
        Terrible,
        
        /// <summary>
        /// Network is offline (operations will fail)
        /// </summary>
        Offline
    }
}

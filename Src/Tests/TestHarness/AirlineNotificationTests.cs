using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ez.Generic.DataSync.Core;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// Functional tests for airline notification delivery in challenging network conditions
    /// </summary>
    public class AirlineNotificationTests
    {
        private readonly AirlineNotificationService _notificationService;
        private readonly MockRepository<AirlineNotification> _repository;
        private readonly NetworkSimulator _networkSimulator;
        private readonly List<string> _testResults = new();
        
        /// <summary>
        /// Initialize a new instance of the AirlineNotificationTests
        /// </summary>
        public AirlineNotificationTests()
        {
            _networkSimulator = new NetworkSimulator();
            var mockSyncService = new MockSyncService(_networkSimulator);
            _repository = new MockRepository<AirlineNotification>();
            
            // Create adapter to work with our custom IRepository implementation
            var repositoryAdapter = new DirectEntityRepository<AirlineNotification>(_repository);
            
            _notificationService = new AirlineNotificationService(mockSyncService, repositoryAdapter, _networkSimulator);
        }
        
        /// <summary>
        /// Run all tests and get test results
        /// </summary>
        /// <returns>List of test results</returns>
        public async Task<List<string>> RunAllTestsAsync()
        {
            _testResults.Clear();
            
            LogTestHeader("AIRLINE NOTIFICATION FUNCTIONAL TESTS");
            
            try
            {
                await SimulateExcellentConnectionTest();
                await SimulateGoGoWifiTest();
                await SimulateIntermittentConnectionTest();
                await SimulateOfflineToOnlineTransitionTest();
                await SimulatePriorityNotificationTest();
                await SimulateMultiDayFlightTest();
                
                LogTestHeader("TEST SUMMARY");
                LogTest($"All tests completed at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                LogTest($"TEST FAILURE: {ex.Message}");
                LogTest(ex.StackTrace ?? string.Empty);
            }
            
            return _testResults;
        }
        
        /// <summary>
        /// Test notification delivery in excellent network conditions
        /// </summary>
        private async Task SimulateExcellentConnectionTest()
        {
            LogTestHeader("TEST: Notification Delivery in Excellent Network Conditions");
            
            // Set network to excellent
            _networkSimulator.CurrentQuality = NetworkQuality.Excellent;
            LogTest($"Network quality set to {_networkSimulator.CurrentQuality}");
            
            // Send a standard notification
            string notificationId = await _notificationService.SendNotificationAsync(
                "Departure Time Update",
                "Your departure time has been moved forward by 15 minutes.",
                NotificationPriority.Normal,
                NotificationCategory.ScheduleChange,
                requiresAcknowledgment: true);
            
            // Wait for delivery
            var delivered = await _notificationService.DeliverNotificationAsync(notificationId, CancellationToken.None);
            
            // Verify results
            var notification = await _repository.GetItemAsync(notificationId);
            
            if (notification != null)
            {
                LogTest($"Notification delivered: {delivered}");
                LogTest($"Delivery attempts: {notification.DeliveryAttempts}");
                LogTest($"Expected result: Successful first-attempt delivery with minimal latency");
                LogTest($"Actual result: {(delivered ? "SUCCESS" : "FAILURE")} in {notification.DeliveryAttempts} attempt(s)");
            }
            else
            {
                LogTest("ERROR: Notification not found in repository");
            }
        }
        
        /// <summary>
        /// Test notification delivery in poor Go-Go in-flight WiFi conditions
        /// </summary>
        private async Task SimulateGoGoWifiTest()
        {
            LogTestHeader("TEST: Notification Delivery via Go-Go In-Flight WiFi");
            
            // Set network to poor (typical of in-flight WiFi)
            _networkSimulator.CurrentQuality = NetworkQuality.Poor;
            LogTest($"Network quality set to {_networkSimulator.CurrentQuality} (simulating Go-Go WiFi)");
            
            // Send a critical notification
            string notificationId = await _notificationService.SendNotificationAsync(
                "Weather Alert",
                "Severe turbulence reported ahead. Consider altitude change.",
                NotificationPriority.High,
                NotificationCategory.Weather,
                requiresAcknowledgment: true);
            
            // Wait for delivery (with timeout)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var delivered = await _notificationService.DeliverNotificationAsync(notificationId, cts.Token);
            
            // Verify results
            var notification = await _repository.GetItemAsync(notificationId);
            
            if (notification != null)
            {
                LogTest($"Notification delivered: {delivered}");
                LogTest($"Delivery attempts: {notification.DeliveryAttempts}");
                LogTest($"Expected result: Eventually successful delivery with multiple retries");
                LogTest($"Actual result: {(delivered ? "SUCCESS" : "FAILURE")} in {notification.DeliveryAttempts} attempt(s)");
                
                // Test acknowledgment
                if (delivered)
                {
                    var acknowledged = await _notificationService.AcknowledgeNotificationAsync(notificationId);
                    LogTest($"Acknowledgment successful: {acknowledged}");
                }
            }
            else
            {
                LogTest("ERROR: Notification not found in repository");
            }
        }
        
        /// <summary>
        /// Test notification delivery with an intermittent connection
        /// </summary>
        private async Task SimulateIntermittentConnectionTest()
        {
            LogTestHeader("TEST: Notification Delivery with Intermittent Connection");
            
            // Create a notification with normal priority
            string notificationId = await _notificationService.SendNotificationAsync(
                "Runway Assignment",
                "You have been assigned Runway 27R for landing.",
                NotificationPriority.Normal,
                NotificationCategory.FlightPlanChange,
                requiresAcknowledgment: false);
            
            // Schedule network quality changes
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            
            // Start a background task to change network quality randomly
            _ = Task.Run(async () =>
            {
                var random = new Random();
                while (!token.IsCancellationRequested)
                {
                    // Randomly switch between terrible, poor, fair and offline
                    int quality = random.Next(0, 4);
                    NetworkQuality networkQuality = quality switch
                    {
                        0 => NetworkQuality.Terrible,
                        1 => NetworkQuality.Poor,
                        2 => NetworkQuality.Fair,
                        _ => NetworkQuality.Offline
                    };
                    
                    _networkSimulator.CurrentQuality = networkQuality;
                    LogTest($"Network quality changed to {networkQuality}");
                    
                    // Wait between 2-5 seconds before changing again
                    await Task.Delay(TimeSpan.FromSeconds(random.Next(2, 6)), token);
                }
            }, token);
            
            // Try to deliver the notification
            using var deliveryTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var delivered = await _notificationService.DeliverNotificationAsync(notificationId, deliveryTimeoutCts.Token);
            
            // Stop the network changing task
            tokenSource.Cancel();
            
            // Verify results
            var notification = await _repository.GetItemAsync(notificationId);
            
            if (notification != null)
            {
                LogTest($"Notification delivered: {delivered}");
                LogTest($"Delivery attempts: {notification.DeliveryAttempts}");
                LogTest($"Expected result: Eventually successful delivery despite network fluctuations");
                LogTest($"Actual result: {(delivered ? "SUCCESS" : "FAILURE")} in {notification.DeliveryAttempts} attempt(s)");
            }
            else
            {
                LogTest("ERROR: Notification not found in repository");
            }
        }
        
        /// <summary>
        /// Test handling of offline to online transition
        /// </summary>
        private async Task SimulateOfflineToOnlineTransitionTest()
        {
            LogTestHeader("TEST: Offline to Online Transition Handling");
            
            // Set network to offline
            _networkSimulator.CurrentQuality = NetworkQuality.Offline;
            LogTest($"Network quality set to {_networkSimulator.CurrentQuality}");
            
            // Send an emergency notification
            string notificationId = await _notificationService.SendNotificationAsync(
                "Emergency Protocol Alpha",
                "Immediate landing required at nearest suitable airport.",
                NotificationPriority.Emergency,
                NotificationCategory.Emergency,
                requiresAcknowledgment: true);
            
            // Start delivery attempt (which should fail)
            var deliveryTask = _notificationService.DeliverNotificationAsync(notificationId, CancellationToken.None);
            
            // Wait briefly to allow some failed attempts
            await Task.Delay(5000);
            
            // Now transition to good network quality
            _networkSimulator.CurrentQuality = NetworkQuality.Good;
            LogTest($"Network quality changed to {_networkSimulator.CurrentQuality}");
            
            // Wait for delivery to complete
            var delivered = await deliveryTask;
            
            // Verify results
            var notification = await _repository.GetItemAsync(notificationId);
            
            if (notification != null)
            {
                LogTest($"Notification delivered: {delivered}");
                LogTest($"Delivery attempts: {notification.DeliveryAttempts}");
                LogTest($"Expected result: Successful delivery after network restoration");
                LogTest($"Actual result: {(delivered ? "SUCCESS" : "FAILURE")} in {notification.DeliveryAttempts} attempt(s)");
            }
            else
            {
                LogTest("ERROR: Notification not found in repository");
            }
        }
        
        /// <summary>
        /// Test that high priority notifications get special handling
        /// </summary>
        private async Task SimulatePriorityNotificationTest()
        {
            LogTestHeader("TEST: Priority Notification Handling");
            
            // Set network to terrible
            _networkSimulator.CurrentQuality = NetworkQuality.Terrible;
            LogTest($"Network quality set to {_networkSimulator.CurrentQuality}");
            
            // First send a low priority notification
            string lowPriorityId = await _notificationService.SendNotificationAsync(
                "Catering Update",
                "Special meal requests have been confirmed.",
                NotificationPriority.Low,
                NotificationCategory.General);
            
            // Then send a critical priority notification
            string criticalPriorityId = await _notificationService.SendNotificationAsync(
                "Security Alert",
                "Security protocol Delta in effect. Cockpit security measures required.",
                NotificationPriority.Critical,
                NotificationCategory.Security,
                requiresAcknowledgment: true);
            
            // Try to deliver both
            var lowPriorityTask = _notificationService.DeliverNotificationAsync(lowPriorityId, CancellationToken.None);
            var criticalPriorityTask = _notificationService.DeliverNotificationAsync(criticalPriorityId, CancellationToken.None);
            
            // Wait for both to complete (with timeout)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await Task.WhenAny(
                Task.WhenAll(lowPriorityTask, criticalPriorityTask),
                Task.Delay(TimeSpan.FromSeconds(60), cts.Token)
            );
            
            // Verify results
            var lowPriorityNotification = await _repository.GetItemAsync(lowPriorityId);
            var criticalPriorityNotification = await _repository.GetItemAsync(criticalPriorityId);
            
            if (lowPriorityNotification != null && criticalPriorityNotification != null)
            {
                LogTest($"Low priority notification delivered: {lowPriorityNotification.IsDelivered}");
                LogTest($"Critical priority notification delivered: {criticalPriorityNotification.IsDelivered}");
                LogTest($"Low priority delivery attempts: {lowPriorityNotification.DeliveryAttempts}");
                LogTest($"Critical priority delivery attempts: {criticalPriorityNotification.DeliveryAttempts}");
                LogTest($"Expected result: Critical notification delivered with priority, possibly before low priority");
                LogTest($"Actual result: Critical={criticalPriorityNotification.IsDelivered}, Low={lowPriorityNotification.IsDelivered}");
            }
            else
            {
                LogTest("ERROR: One or both notifications not found in repository");
            }
        }
        
        /// <summary>
        /// Test notification reliability on multi-day flights with extended offline periods
        /// </summary>
        private async Task SimulateMultiDayFlightTest()
        {
            LogTestHeader("TEST: Multi-Day Flight with Extended Offline Periods");
            
            // Create a list to track notifications
            var notificationIds = new List<string>();
            
            // Create several notifications of varying priorities
            notificationIds.Add(await _notificationService.SendNotificationAsync(
                "Duty Time Extension", 
                "Your duty time has been extended by 4 hours for flight continuity.",
                NotificationPriority.High,
                NotificationCategory.ScheduleChange,
                requiresAcknowledgment: true));
                
            notificationIds.Add(await _notificationService.SendNotificationAsync(
                "Maintenance Check Required",
                "Pre-flight maintenance check required before next takeoff.",
                NotificationPriority.Normal,
                NotificationCategory.MaintenanceAlert,
                requiresAcknowledgment: true));
                
            notificationIds.Add(await _notificationService.SendNotificationAsync(
                "New Weather Charts",
                "Updated weather charts available for download.",
                NotificationPriority.Low,
                NotificationCategory.Weather,
                requiresAcknowledgment: false));
            
            // Simulate the offline -> online patterns of a multi-day flight
            var networkPatterns = new[]
            {
                (NetworkQuality.Offline, TimeSpan.FromSeconds(10)), // Initial offline
                (NetworkQuality.Poor, TimeSpan.FromSeconds(5)),     // Brief connectivity
                (NetworkQuality.Offline, TimeSpan.FromSeconds(15)), // Back to offline 
                (NetworkQuality.Fair, TimeSpan.FromSeconds(8)),     // Better connectivity
                (NetworkQuality.Offline, TimeSpan.FromSeconds(20)), // Extended offline
                (NetworkQuality.Good, TimeSpan.FromSeconds(10))     // Final good connection
            };
            
            // Start delivery tasks for all notifications
            var deliveryTasks = new List<Task<bool>>();
            foreach (var id in notificationIds)
            {
                deliveryTasks.Add(_notificationService.DeliverNotificationAsync(id, CancellationToken.None));
            }
            
            // Run through the network pattern simulation
            foreach (var (quality, duration) in networkPatterns)
            {
                _networkSimulator.CurrentQuality = quality;
                LogTest($"Network changed to {quality} for {duration.TotalSeconds} seconds");
                await Task.Delay(duration);
            }
            
            // Wait for all delivery tasks to complete or timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await Task.WhenAny(
                Task.WhenAll(deliveryTasks),
                Task.Delay(TimeSpan.FromSeconds(30), cts.Token)
            );
            
            // Check results
            LogTest("Multi-day flight test results:");
            for (int i = 0; i < notificationIds.Count; i++)
            {
                var notification = await _repository.GetItemAsync(notificationIds[i]);
                
                if (notification != null)
                {
                    LogTest($"Notification {i+1} ({notification.Priority}): " +
                            $"Delivered={notification.IsDelivered}, " +
                            $"Attempts={notification.DeliveryAttempts}");
                }
                else
                {
                    LogTest($"ERROR: Notification {i+1} not found in repository");
                }
            }
            
            LogTest("Expected result: Higher priority notifications delivered first when network becomes available");
            LogTest("Expected result: All notifications eventually delivered despite extended offline periods");
        }
        
        /// <summary>
        /// Log a test header
        /// </summary>
        private void LogTestHeader(string headerText)
        {
            var header = $"\n=== {headerText} ===\n";
            _testResults.Add(header);
            Console.WriteLine(header);
        }
        
        /// <summary>
        /// Log a test message
        /// </summary>
        private void LogTest(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}";
            _testResults.Add(logEntry);
            Console.WriteLine(logEntry);
        }
    }
}

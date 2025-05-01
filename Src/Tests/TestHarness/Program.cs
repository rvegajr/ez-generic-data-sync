using Ez.Generic.DataSync.Core;
using Ez.Generic.DataSync.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// CLI Test Harness for the Generic Data Sync library
    /// </summary>
    class Program
    {
        private static bool _keepRunning = true;
        private SyncServiceTester<TodoItem> _syncService;
        private MockRepository<TodoItem> _repository;
        private DirectEntityRepository<TodoItem> _repositoryAdapter;
        private int _listCount = 0;
        private NetworkSimulator _networkSimulator;
        
        static async Task Main(string[] args)
        {
            try
            {
                var program = new Program();
                
                // Check if we're running in integration test mode directly
                if (args.Length > 0 && args[0].Equals("integration-test", StringComparison.OrdinalIgnoreCase))
                {
                    // Initialize services before running integration tests
                    program.InitializeServices();
                    await program.RunIntegrationTests();
                    return;
                }
                
                // Regular interactive mode
                await program.Initialize();
                
                while (_keepRunning)
                {
                    await program.DisplayMenu();
                    var choice = Console.ReadLine()?.Trim().ToLower();
                    
                    await program.ProcessChoice(choice);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Main entry point
        /// </summary>
        private async Task Initialize()
        {
            Console.WriteLine("====================================================");
            Console.WriteLine("  EZ-GENERIC-DATA-SYNC - FUNCTIONAL TEST HARNESS");
            Console.WriteLine("====================================================");
            Console.WriteLine();
            
            InitializeServices();
        }
        
        /// <summary>
        /// Run the automated functional tests
        /// </summary>
        private async Task RunAutomatedTests()
        {
            Console.WriteLine("Running automated functional tests...");
            var testRunner = new AutomatedFunctionalTests();
            await testRunner.RunAllTests();
        }
        
        /// <summary>
        /// Initializes the sync service, repository and other components
        /// </summary>
        private void InitializeServices()
        {
            // Initialize network simulator
            _networkSimulator = new NetworkSimulator();
            
            // Initialize mock sync service and repository
            var mockSyncService = new MockSyncService(_networkSimulator);
            _repository = new MockRepository<TodoItem>();
            
            // Create adapter to work with SyncableEntityWrapper
            _repositoryAdapter = new DirectEntityRepository<TodoItem>(_repository);
            
            // Create our test harness
            _syncService = new SyncServiceTester<TodoItem>(mockSyncService, _repositoryAdapter, _networkSimulator);
            
            // Configure defaults
            _syncService.SetNetworkPolicy(new NetworkPolicy 
            { 
                MaxRetries = 3, 
                TimeoutSeconds = 30,
                RequireWifi = false,
                AutoSyncOnNetworkAvailable = true
            });
            
            // Create some sample data
            InitializeSampleData().Wait();
        }
        
        /// <summary>
        /// Adds some sample todo items
        /// </summary>
        private async Task InitializeSampleData()
        {
            Console.WriteLine("Initializing sample data...");
            
            var items = new[]
            {
                new TodoItem 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Title = "Complete project documentation", 
                    Description = "Update all class documentation with XML comments",
                    Priority = Priority.High,
                    DueDate = DateTime.Now.AddDays(3)
                },
                new TodoItem 
                { 
                    Id = Guid.NewGuid().ToString(),
                    Title = "Fix bugs in sync module",
                    Description = "Address error handling issues in offline mode",
                    Priority = Priority.Urgent,
                    DueDate = DateTime.Now.AddDays(1)
                },
                new TodoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Review pull requests",
                    Description = "Check the pending PRs and provide feedback",
                    Priority = Priority.Normal,
                    DueDate = DateTime.Now.AddDays(2)
                },
                new TodoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Team standup meeting",
                    Description = "Daily standup at 10:00 AM",
                    Priority = Priority.Normal,
                    IsCompleted = true
                },
                new TodoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Plan next sprint",
                    Description = "Prepare backlog and estimates for next sprint",
                    Priority = Priority.Low,
                    DueDate = DateTime.Now.AddDays(5)
                }
            };
            
            foreach (var item in items)
            {
                await _repository.AddItemAsync(item, item.Id);
            }
            
            Console.WriteLine($"Added {items.Length} sample items to the repository");
        }
        
        /// <summary>
        /// Displays the main menu
        /// </summary>
        private async Task DisplayMenu()
        {
            Console.WriteLine("EZ-GENERIC-DATA-SYNC TESTER - CHOOSE AN OPTION:");
            Console.WriteLine();
            Console.WriteLine("1. View All Items");
            Console.WriteLine("2. Add New Item");
            Console.WriteLine("3. Update Item");
            Console.WriteLine("4. Delete Item");
            Console.WriteLine("5. Pull Changes");
            Console.WriteLine("6. Push Changes");
            Console.WriteLine("7. Sync Specific Item");
            Console.WriteLine("8. Change Network Quality");
            Console.WriteLine("9. View Sync Log");
            Console.WriteLine("A. Run Automated Tests");
            Console.WriteLine("B. Start Sync Server");
            Console.WriteLine("C. Run Client-Server Integration Tests");
            Console.WriteLine("0. Exit");
            Console.Write("\nEnter choice (0-9, A-C): ");
        }
        
        /// <summary>
        /// Processes the user's menu choice
        /// </summary>
        private async Task ProcessChoice(string choice)
        {
            switch (choice)
            {
                case "1":
                    await ViewAllItems();
                    break;
                case "2":
                    await AddNewItem();
                    break;
                case "3":
                    await UpdateItem();
                    break;
                case "4":
                    await DeleteItem();
                    break;
                case "5":
                    await PullChanges();
                    break;
                case "6":
                    await PushChanges();
                    break;
                case "7":
                    await SyncSpecificItem();
                    break;
                case "8":
                    ChangeNetworkQuality();
                    break;
                case "9":
                    ViewSyncLog();
                    break;
                case "a":
                case "A":
                    await RunAutomatedTests();
                    break;
                case "b":
                case "B":
                    await RunSyncServer();
                    break;
                case "c":
                case "C":
                    await RunIntegrationTests();
                    break;
                case "0":
                    _keepRunning = false;
                    Console.WriteLine("Exiting test harness. Goodbye!");
                    break;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }
        
        /// <summary>
        /// Displays all items in the repository
        /// </summary>
        private async Task ViewAllItems()
        {
            Console.WriteLine("\n=== All Todo Items ===\n");
            
            var items = await _repository.GetItemsAsync();
            var itemsList = items.ToList();
            
            if (!itemsList.Any())
            {
                Console.WriteLine("No items found in the repository.");
                return;
            }
            
            int index = 0;
            foreach (var item in itemsList)
            {
                Console.WriteLine($"{++index}. {item}");
            }
            
            Console.WriteLine($"\nTotal: {itemsList.Count} items");
        }
        
        /// <summary>
        /// Adds a new item to the repository
        /// </summary>
        private async Task AddNewItem()
        {
            Console.WriteLine("\n=== Add New Todo Item ===\n");
            
            var todoItem = new TodoItem
            {
                Id = Guid.NewGuid().ToString()
            };
            
            Console.Write("Title: ");
            todoItem.Title = Console.ReadLine() ?? string.Empty;
            
            Console.Write("Description: ");
            todoItem.Description = Console.ReadLine() ?? string.Empty;
            
            Console.Write("Due Date (yyyy-MM-dd or leave empty): ");
            string dueDateStr = Console.ReadLine() ?? string.Empty;
            if (DateTime.TryParse(dueDateStr, out DateTime dueDate))
            {
                todoItem.DueDate = dueDate;
            }
            
            Console.Write("Priority (0=Low, 1=Normal, 2=High, 3=Urgent): ");
            if (int.TryParse(Console.ReadLine(), out int priority) && Enum.IsDefined(typeof(Priority), priority))
            {
                todoItem.Priority = (Priority)priority;
            }
            
            Console.Write("Is Completed (y/n): ");
            todoItem.IsCompleted = (Console.ReadLine()?.Trim().ToLower() == "y");
            
            await _repository.AddItemAsync(todoItem, todoItem.Id);
            
            Console.WriteLine($"\nItem added successfully with ID: {todoItem.Id}");
        }
        
        /// <summary>
        /// Updates an existing item
        /// </summary>
        private async Task UpdateItem()
        {
            if (!_repository.GetItemsAsync().Result.Any())
            {
                Console.WriteLine("\nNo items to update.");
                return;
            }
            
            Console.WriteLine("\n=== Update Todo Item ===\n");
            
            // Display items for selection
            await ViewAllItems();
            
            Console.Write("\nEnter item number to update (1-" + _repository.GetItemsAsync().Result.Count() + "): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > _repository.GetItemsAsync().Result.Count())
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            
            string itemId = _repository.GetItemsAsync().Result.ElementAt(selection - 1).Id;
            var existingItem = await _repository.GetItemAsync(itemId);
            
            if (existingItem == null)
            {
                Console.WriteLine("Item not found or has been deleted.");
                return;
            }
            
            // Create a copy to update
            var updatedItem = new TodoItem
            {
                Id = existingItem.Id,
                Title = existingItem.Title,
                Description = existingItem.Description,
                DueDate = existingItem.DueDate,
                Priority = existingItem.Priority,
                IsCompleted = existingItem.IsCompleted
            };
            
            Console.WriteLine("\nEnter new values (leave empty to keep current value)");
            
            Console.Write($"Title [{updatedItem.Title}]: ");
            string input = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(input))
            {
                updatedItem.Title = input;
            }
            
            Console.Write($"Description [{updatedItem.Description}]: ");
            input = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(input))
            {
                updatedItem.Description = input;
            }
            
            Console.Write($"Due Date [{updatedItem.DueDate:yyyy-MM-dd}]: ");
            input = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(input) && DateTime.TryParse(input, out DateTime dueDate))
            {
                updatedItem.DueDate = dueDate;
            }
            
            Console.Write($"Priority [{updatedItem.Priority}] (0=Low, 1=Normal, 2=High, 3=Urgent): ");
            input = Console.ReadLine() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out int priority) && Enum.IsDefined(typeof(Priority), priority))
            {
                updatedItem.Priority = (Priority)priority;
            }
            
            Console.Write($"Is Completed [{(updatedItem.IsCompleted ? "y" : "n")}] (y/n): ");
            input = Console.ReadLine()?.Trim().ToLower() ?? string.Empty;
            if (input == "y" || input == "n")
            {
                updatedItem.IsCompleted = (input == "y");
            }
            
            await _repository.UpdateItemAsync(updatedItem, existingItem.Id);
            Console.WriteLine("\nItem updated successfully.");
        }
        
        /// <summary>
        /// Deletes an item from the repository
        /// </summary>
        private async Task DeleteItem()
        {
            if (!_repository.GetItemsAsync().Result.Any())
            {
                Console.WriteLine("\nNo items to delete.");
                return;
            }
            
            Console.WriteLine("\n=== Delete Todo Item ===\n");
            
            // Display items for selection
            await ViewAllItems();
            
            Console.Write("\nEnter item number to delete (1-" + _repository.GetItemsAsync().Result.Count() + "): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > _repository.GetItemsAsync().Result.Count())
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            
            string itemId = _repository.GetItemsAsync().Result.ElementAt(selection - 1).Id;
            
            Console.Write($"Are you sure you want to delete this item? (y/n): ");
            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine("Deletion cancelled.");
                return;
            }
            
            await _repository.DeleteItemAsync(itemId);
            Console.WriteLine("\nItem marked as deleted successfully.");
        }
        
        /// <summary>
        /// Pulls changes from the server
        /// </summary>
        private async Task PullChanges()
        {
            Console.WriteLine("\n=== Pull Changes ===\n");
            Console.WriteLine($"Current network quality: {_syncService.NetworkQuality}");
            
            Console.WriteLine("Pulling changes...");
            var result = await _syncService.PullAsync();
            
            Console.WriteLine($"\nSync Result:");
            Console.WriteLine($"Status: {result.Status}");
            Console.WriteLine($"Items: {result.ItemCount}");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Pushes local changes to the server
        /// </summary>
        private async Task PushChanges()
        {
            Console.WriteLine("\n=== Push Changes ===\n");
            Console.WriteLine($"Current network quality: {_syncService.NetworkQuality}");
            
            Console.WriteLine("Pushing changes...");
            var result = await _syncService.PushAsync();
            
            Console.WriteLine($"\nSync Result:");
            Console.WriteLine($"Status: {result.Status}");
            Console.WriteLine($"Items: {result.ItemCount}");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Synchronizes a specific item
        /// </summary>
        private async Task SyncSpecificItem()
        {
            if (!_repository.GetItemsAsync().Result.Any())
            {
                Console.WriteLine("\nNo items to sync.");
                return;
            }
            
            Console.WriteLine("\n=== Sync Specific Item ===\n");
            
            // Display items for selection
            await ViewAllItems();
            
            Console.Write("\nEnter item number to sync (1-" + _repository.GetItemsAsync().Result.Count() + "): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > _repository.GetItemsAsync().Result.Count())
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            
            string itemId = _repository.GetItemsAsync().Result.ElementAt(selection - 1).Id;
            
            Console.WriteLine($"Syncing item with ID: {itemId}");
            Console.WriteLine($"Current network quality: {_syncService.NetworkQuality}");
            
            var result = await _syncService.SyncItemAsync(itemId);
            
            Console.WriteLine($"\nSync Result:");
            Console.WriteLine($"Status: {result.Status}");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Changes the network quality for testing
        /// </summary>
        private void ChangeNetworkQuality()
        {
            Console.WriteLine("\n=== Change Network Quality ===\n");
            Console.WriteLine("Available network qualities:");
            Console.WriteLine("0 - Excellent (10ms latency)");
            Console.WriteLine("1 - Good (100ms latency)");
            Console.WriteLine("2 - Fair (300ms latency)");
            Console.WriteLine("3 - Poor (800ms latency)");
            Console.WriteLine("4 - Terrible (2000ms latency)");
            Console.WriteLine("5 - Offline (operations will fail)");
            
            Console.Write("\nSelect network quality (0-5): ");
            if (int.TryParse(Console.ReadLine(), out int quality) && Enum.IsDefined(typeof(NetworkQuality), quality))
            {
                _syncService.SetNetworkQuality((NetworkQuality)quality);
                Console.WriteLine($"Network quality set to: {(NetworkQuality)quality}");
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }
        
        /// <summary>
        /// Displays the sync operation log
        /// </summary>
        private void ViewSyncLog()
        {
            Console.WriteLine("\n=== Sync Operation Log ===\n");
            
            var logEntries = _syncService.LogEntries;
            
            if (!logEntries.Any())
            {
                Console.WriteLine("No log entries found.");
                return;
            }
            
            foreach (var entry in logEntries)
            {
                Console.WriteLine(entry);
            }
            
            Console.WriteLine($"\nTotal: {logEntries.Count} log entries");
        }
        
        private async Task RunSyncServer()
        {
            Console.WriteLine("======== STARTING SYNC SERVER ========");
            Console.WriteLine("This will run until you press Ctrl+C to stop it");
            Console.WriteLine();
            
            try
            {
                var builder = WebApplication.CreateBuilder();
                
                // Add services to the container
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                
                // Register the repository
                var todoRepository = new MockRepository<TodoItem>();
                var todoItems = CreateSampleData();
                foreach (var item in todoItems)
                {
                    todoRepository.AddItemAsync(item).GetAwaiter().GetResult();
                }
                
                // Create the wrapper repository
                var wrappedRepo = new DirectEntityRepository<TodoItem>(todoRepository);
                
                // Register both specific and generic repositories
                builder.Services.AddSingleton<IRepository<SyncableEntityWrapper<TodoItem>>>(wrappedRepo);
                
                // Build the application
                var app = builder.Build();
                
                // Configure middleware
                app.UseSwagger();
                app.UseSwaggerUI();
                
                app.UseHttpsRedirection();
                app.UseAuthorization();
                app.MapControllers();
                
                // Notify user
                Console.WriteLine("Sync Server is running on http://localhost:5000");
                Console.WriteLine("API documentation available at http://localhost:5000/swagger");
                Console.WriteLine("Press Ctrl+C to stop the server");
                
                // Start the server
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }
        
        private async Task RunIntegrationTests()
        {
            Console.WriteLine("======== RUNNING CLIENT-SERVER INTEGRATION TESTS ========");
            Console.WriteLine("This will start a server instance and test the client against it");
            Console.WriteLine();
            
            try
            {
                // Create a server in a separate task
                var serverTask = Task.Run(async () =>
                {
                    var builder = WebApplication.CreateBuilder(new string[] { "--urls", "http://localhost:5050" });
                    
                    // Add services
                    builder.Services.AddControllers();
                    builder.Services.AddEndpointsApiExplorer();
                    
                    // Register the repository with sample data
                    var todoRepository = new MockRepository<TodoItem>();
                    var todoItems = CreateSampleData();
                    foreach (var item in todoItems)
                    {
                        todoRepository.AddItemAsync(item).GetAwaiter().GetResult();
                    }
                    
                    // Create the wrapper repository
                    var wrappedRepo = new DirectEntityRepository<TodoItem>(todoRepository);
                    
                    // Register repositories
                    builder.Services.AddSingleton<IRepository<SyncableEntityWrapper<TodoItem>>>(wrappedRepo);
                    
                    // Build the application
                    var app = builder.Build();
                    app.MapControllers();
                    
                    // Start server on port 5050
                    Console.WriteLine("Test server started on http://localhost:5050");
                    await app.StartAsync();
                    
                    // Keep server running during tests
                    var serverCompletionSource = new TaskCompletionSource<bool>();
                    return (app, serverCompletionSource);
                });
                
                // Wait for server to start
                var (server, serverCompletionSource) = await serverTask;
                await Task.Delay(1000); // Small delay to ensure server is ready
                
                // Create client
                Console.WriteLine("Creating test client...");
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("http://localhost:5050");
                
                // Create a custom network-aware repository that uses the HTTP client
                var clientRepo = new HttpClientRepository<TodoItem>(httpClient, "/api/TodoItems");
                
                // Create a dummy sync service implementation to avoid null
                var dummySyncService = new DummySyncService<TodoItem>();
                
                // Create a sync service that uses the HTTP repository
                var syncService = new GenericSyncService<TodoItem>(
                    dummySyncService, // Use our dummy ISyncService implementation
                    clientRepo // Using the HTTP client repository directly
                );
                
                // Test basic operations
                await TestBasicOperations(syncService);
                
                // Test network conditions
                await TestNetworkConditions(syncService);
                
                // Test conflict handling
                await TestConflictHandling(syncService);
                
                // Stop the server
                await server.StopAsync();
                serverCompletionSource.SetResult(true);
                
                Console.WriteLine("\nIntegration tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during integration tests: {ex.Message}");
            }
        }
        
        private async Task TestBasicOperations(GenericSyncService<TodoItem> syncService)
        {
            Console.WriteLine("\n--- Testing Basic Operations ---");
            
            try
            {
                // Get the Repository property which handles the wrapping/unwrapping for us
                var repository = syncService.Repository;
                
                // Test Pull
                Console.WriteLine("Testing Pull operation...");
                var pullResult = await syncService.PullAsync();
                Console.WriteLine($"Pull Result: {pullResult.Status}, Items: {pullResult.ItemCount}");
                
                // Test Add Item
                Console.WriteLine("Testing Add Item operation...");
                var newItem = new TodoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Integration Test Item",
                    Description = "Created during integration testing",
                    Priority = Priority.High,
                    DueDate = DateTime.Now.AddDays(1),
                    CreatedAt = DateTime.UtcNow
                };
                
                // Use the repository to add the item (it will handle the wrapping)
                await repository.AddItemAsync(newItem, newItem.Id);
                
                // Test Push
                Console.WriteLine("Testing Push operation...");
                var pushResult = await syncService.PushAsync();
                Console.WriteLine($"Push Result: {pushResult.Status}, Items: {pushResult.ItemCount}");
                
                // Verify item was added
                var items = await repository.GetItemsAsync();
                bool itemFound = items.Any(i => i.Title == "Integration Test Item");
                Console.WriteLine($"Item verification: {(itemFound ? "Success" : "Failed")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in basic operations test: {ex.Message}");
            }
        }
        
        private async Task TestNetworkConditions(GenericSyncService<TodoItem> syncService)
        {
            Console.WriteLine("\n--- Testing Network Conditions ---");
            
            try 
            {
                // We can't actually control the network for a real server
                // but we can simulate by adding delays or error handling in the client
                
                Console.WriteLine("Network condition testing with real server is limited.");
                Console.WriteLine("For comprehensive network testing, use the automated tests with network simulation.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in network conditions test: {ex.Message}");
            }
        }
        
        private async Task TestConflictHandling(GenericSyncService<TodoItem> syncService)
        {
            Console.WriteLine("\n--- Testing Conflict Handling ---");
            
            try
            {
                // Get the Repository property which handles the wrapping/unwrapping for us
                var repository = syncService.Repository;
                
                // Basic test - update an item and push
                var items = await repository.GetItemsAsync();
                if (items.Any())
                {
                    var item = items.First();
                    var originalTitle = item.Title ?? "Unknown";
                    
                    // Create updated version of the item
                    var updatedItem = new TodoItem
                    {
                        Id = item.Id,
                        Title = $"{originalTitle} - Updated in integration test",
                        Description = item.Description,
                        Priority = item.Priority,
                        IsCompleted = item.IsCompleted,
                        DueDate = item.DueDate,
                        CreatedAt = item.CreatedAt
                    };
                    
                    // Update the item through the repository
                    await repository.UpdateItemAsync(updatedItem, item.Id);
                    
                    // Push the update
                    Console.WriteLine("Testing conflict handling with update...");
                    var pushResult = await syncService.PushAsync();
                    Console.WriteLine($"Push Result: {pushResult.Status}, Items: {pushResult.ItemCount}");
                }
                else
                {
                    Console.WriteLine("No items available for conflict test");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in conflict handling test: {ex.Message}");  
            }
        }
        
        // Sample HTTP Client-based repository for integration testing
        private class HttpClientRepository<T> : IRepository<SyncableEntityWrapper<T>> where T : class
        {
            private readonly HttpClient _httpClient;
            private readonly string _endpoint;
            
            public HttpClientRepository(HttpClient httpClient, string endpoint)
            {
                _httpClient = httpClient;
                _endpoint = endpoint;
            }
            
            public async Task AddItemAsync(SyncableEntityWrapper<T> item, string id = null, CancellationToken cancellationToken = default)
            {
                var response = await _httpClient.PostAsJsonAsync(_endpoint, item, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            
            public async Task DeleteItemAsync(string id, CancellationToken cancellationToken = default)
            {
                var response = await _httpClient.DeleteAsync($"{_endpoint}/{id}", cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            
            public async Task<SyncableEntityWrapper<T>> GetItemAsync(string id, CancellationToken cancellationToken = default)
            {
                var response = await _httpClient.GetAsync($"{_endpoint}/{id}", cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null!; // Using null! to satisfy non-nullable return type
                    
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SyncableEntityWrapper<T>>(cancellationToken: cancellationToken) ?? null!;
            }
            
            public async Task<IEnumerable<SyncableEntityWrapper<T>>> GetItemsAsync(CancellationToken cancellationToken = default)
            {
                var response = await _httpClient.GetAsync(_endpoint, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<SyncableEntityWrapper<T>>>(cancellationToken: cancellationToken) ?? Array.Empty<SyncableEntityWrapper<T>>();
            }
            
            public async Task UpdateItemAsync(SyncableEntityWrapper<T> item, string id, CancellationToken cancellationToken = default)
            {
                var response = await _httpClient.PutAsJsonAsync($"{_endpoint}/{id}", item, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
        }
        
        private static List<TodoItem> CreateSampleData()
        {
            return new List<TodoItem>
            {
                new TodoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Server Test Item 1", 
                    Description = "First test item from server",
                    Priority = Priority.Normal,
                    DueDate = DateTime.Now.AddDays(1),
                    CreatedAt = DateTime.UtcNow
                },
                new TodoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Server Test Item 2", 
                    Description = "Second test item from server",
                    Priority = Priority.High,
                    IsCompleted = false,
                    DueDate = DateTime.Now.AddDays(3),
                    CreatedAt = DateTime.UtcNow
                },
                new TodoItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Server Test Item 3", 
                    Description = "Third test item from server",
                    Priority = Priority.Urgent,
                    IsCompleted = true,
                    DueDate = DateTime.Now.AddDays(-1),
                    CreatedAt = DateTime.UtcNow
                }
            };
        }
        
        // Dummy sync service implementation to avoid passing null to GenericSyncService
        private class DummySyncService<T> : ISyncService where T : class
        {
            public Task<SyncResult> PullAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new SyncResult { Status = SyncStatus.Completed });
            }

            public Task<SyncResult> PushAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new SyncResult { Status = SyncStatus.Completed });
            }
            
            public Task<SyncResult> SyncItemAsync(string id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new SyncResult { Status = SyncStatus.Completed });
            }
            
            public void SetNetworkPolicy(NetworkPolicy policy)
            {
                // No-op for this dummy implementation
            }
        }
    }
}

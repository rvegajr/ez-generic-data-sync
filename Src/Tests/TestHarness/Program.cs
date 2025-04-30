using Ez.Generic.DataSync.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.TestHarness
{
    /// <summary>
    /// CLI Test Harness for the Generic Data Sync library
    /// </summary>
    class Program
    {
        private static SyncServiceTester<TodoItem> _syncService;
        private static MockRepository<TodoItem> _repository;
        private static DirectEntityRepository<TodoItem> _repositoryAdapter;
        private static List<string> _itemIds = new();
        private static NetworkSimulator _networkSimulator;
        private static bool _keepRunning = true;

        /// <summary>
        /// Main entry point
        /// </summary>
        static async Task Main(string[] args)
        {
            Console.WriteLine("====================================================");
            Console.WriteLine("  EZ-GENERIC-DATA-SYNC - FUNCTIONAL TEST HARNESS");
            Console.WriteLine("====================================================");
            Console.WriteLine();
            
            // Check for automated test argument
            if (args.Length > 0 && args[0].ToLower() == "--auto-test")
            {
                await RunAutomatedTests();
                return;
            }
            
            InitializeServices();
            
            while (_keepRunning)
            {
                DisplayMenu();
                var choice = Console.ReadLine()?.Trim().ToLower();
                
                await ProcessChoice(choice);
                
                Console.WriteLine("\nPress Enter to continue...");
                Console.ReadLine();
                Console.Clear();
            }
        }
        
        /// <summary>
        /// Run the automated functional tests
        /// </summary>
        private static async Task RunAutomatedTests()
        {
            Console.WriteLine("Running automated functional tests...");
            var testRunner = new AutomatedFunctionalTests();
            await testRunner.RunAllTests();
        }
        
        /// <summary>
        /// Initializes the sync service, repository and other components
        /// </summary>
        private static void InitializeServices()
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
        private static async Task InitializeSampleData()
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
                _itemIds.Add(item.Id);
            }
            
            Console.WriteLine($"Added {items.Length} sample items to the repository");
        }
        
        /// <summary>
        /// Displays the main menu
        /// </summary>
        private static void DisplayMenu()
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
            Console.WriteLine("0. Exit");
            Console.Write("\nEnter choice (0-9, A): ");
        }
        
        /// <summary>
        /// Processes the user's menu choice
        /// </summary>
        private static async Task ProcessChoice(string choice)
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
        private static async Task ViewAllItems()
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
        private static async Task AddNewItem()
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
            _itemIds.Add(todoItem.Id);
            
            Console.WriteLine($"\nItem added successfully with ID: {todoItem.Id}");
        }
        
        /// <summary>
        /// Updates an existing item
        /// </summary>
        private static async Task UpdateItem()
        {
            if (!_itemIds.Any())
            {
                Console.WriteLine("\nNo items to update.");
                return;
            }
            
            Console.WriteLine("\n=== Update Todo Item ===\n");
            
            // Display items for selection
            await ViewAllItems();
            
            Console.Write("\nEnter item number to update (1-" + _itemIds.Count + "): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > _itemIds.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            
            string itemId = _itemIds[selection - 1];
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
        private static async Task DeleteItem()
        {
            if (!_itemIds.Any())
            {
                Console.WriteLine("\nNo items to delete.");
                return;
            }
            
            Console.WriteLine("\n=== Delete Todo Item ===\n");
            
            // Display items for selection
            await ViewAllItems();
            
            Console.Write("\nEnter item number to delete (1-" + _itemIds.Count + "): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > _itemIds.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            
            string itemId = _itemIds[selection - 1];
            
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
        private static async Task PullChanges()
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
        private static async Task PushChanges()
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
        private static async Task SyncSpecificItem()
        {
            if (!_itemIds.Any())
            {
                Console.WriteLine("\nNo items to sync.");
                return;
            }
            
            Console.WriteLine("\n=== Sync Specific Item ===\n");
            
            // Display items for selection
            await ViewAllItems();
            
            Console.Write("\nEnter item number to sync (1-" + _itemIds.Count + "): ");
            if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > _itemIds.Count)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }
            
            string itemId = _itemIds[selection - 1];
            
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
        private static void ChangeNetworkQuality()
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
        private static void ViewSyncLog()
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
    }
}

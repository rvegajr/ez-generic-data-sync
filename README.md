# ez-generic-data-sync

A generic wrapper for the [CommunityToolkit/Datasync](https://github.com/CommunityToolkit/Datasync/) library that adds strongly-typed object support.

## What It Does

Ez-Generic-Data-Sync solves a key limitation in the original Datasync toolkit – working with strongly-typed objects throughout your application while maintaining the powerful sync capabilities of the underlying library.

### Problems It Solves

- **Type Safety**: Eliminates runtime errors by providing compile-time type checking
- **Developer Experience**: Work directly with your domain models instead of manual conversion
- **Code Maintainability**: Refactoring is safer with strong typing (rename properties, move classes)
- **Complexity Reduction**: Removes boilerplate code for type conversion and error handling

## How It Works

The library uses a layered approach to wrap the original Datasync toolkit:

```
┌───────────────────────────────────────────────────────┐
│ Your Application Code                                 │
│                                                       │
│ var customer = await syncService.GetItemAsync<Customer>("id-123"); │
└───────────────┬───────────────────────────────────────┘
                │
┌───────────────▼───────────────────────────────────────┐
│ Ez-Generic-Data-Sync                                  │
│                                                       │
│ ┌─────────────────────┐    ┌───────────────────────┐  │
│ │ GenericSyncService<T>│    │ SyncableEntityWrapper<T>│  │
│ └─────────────────────┘    └───────────────────────┘  │
└───────────────┬───────────────────────────────────────┘
                │
┌───────────────▼───────────────────────────────────────┐
│ Community Toolkit / Datasync                          │
│                                                       │
│ ┌─────────────┐  ┌──────────────┐  ┌────────────────┐ │
│ │ SyncService │  │ ISyncableEntity│  │ IRepository    │ │
│ └─────────────┘  └──────────────┘  └────────────────┘ │
└───────────────────────────────────────────────────────┘
```

### Key Components

1. **Generic Entity Wrapper**: `SyncableEntityWrapper<T>` encapsulates your domain object within a standard ISyncableEntity
   
   ```csharp
   // Your domain class - no special requirements
   public class Customer 
   { 
       public string Name { get; set; }
       public string Email { get; set; }
   }
   
   // Automatically wrapped for synchronization
   var wrappedCustomer = new SyncableEntityWrapper<Customer>(customer);
   ```

2. **Generic Repository**: Provides type-safe CRUD operations while handling the mapping between your types and the Datasync entities

3. **Generic Sync Service**: Coordinates synchronization operations with proper type handling and error management

### Client-Server Architecture

Ez-Generic-Data-Sync works within a client-server architecture:

- **Client-Side**: The library primarily enhances the client experience with generic typing
- **Server-Side**: Uses standard Datasync server endpoints (ASP.NET Core)

## Features

- [x] Generic wrapper for any C# class
- [x] Type-safe repository operations
- [x] Automatic conflict resolution
- [x] Offline support
- [x] Network condition resilience
- [x] Comprehensive test suite

## Project Status

**Version 1.0.0 Released!** All tests are passing and the library is ready for production use.

See the [Architecture Checklist](./_Resources/ARCHITECTURE_CHECKLIST.md) for implementation details.

## Getting Started

For detailed implementation instructions, see our [Usage Guide](./USAGE_GUIDE.md) which covers:

- Basic setup and configuration
- Repository implementations
- Working with sync services
- Handling challenging network conditions
- Conflict resolution strategies
- Testing methodologies
- Advanced scenarios for airline notification delivery

### Installation

```bash
# Install the core package
dotnet add package Ez.Generic.DataSync.Core

# Optional extensions package with additional functionality
dotnet add package Ez.Generic.DataSync.Extensions
```

### Basic Usage

Here's a complete example showing how to set up and use Ez-Generic-DataSync in a typical application:

```csharp
// 1. Define your domain model (any POCO class)
public class Customer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime LastUpdated { get; set; }
}

// 2. Register services in your DI container
public void ConfigureServices(IServiceCollection services)
{
    // Register the generic data sync services
    services.AddGenericDataSync<Customer>(options => 
    {
        options.ApiUrl = "https://your-datasync-server.com/api";
        options.OfflineBehavior = OfflineBehavior.CacheAndSync;
        options.ConflictResolution = ConflictResolutionStrategy.ClientWins;
    });
    
    // Register your application services that will use the sync service
    services.AddScoped<ICustomerService, CustomerService>();
}

// 3. Use in your service class
public class CustomerService : ICustomerService
{
    private readonly GenericSyncService<Customer> _syncService;
    
    public CustomerService(GenericSyncService<Customer> syncService)
    {
        _syncService = syncService;
    }
    
    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        // Try to pull latest from server first
        try
        {
            await _syncService.PullAsync();
        }
        catch (NetworkException)
        {
            // Continue with cached data if network is unavailable
            Console.WriteLine("Working with cached data - network unavailable");
        }
        
        // Get all customers from the repository
        return await _syncService.Repository.GetItemsAsync();
    }
    
    public async Task AddCustomerAsync(Customer customer)
    {
        // Generate a unique ID if not provided
        if (string.IsNullOrEmpty(customer.Id))
        {
            customer.Id = Guid.NewGuid().ToString();
        }
        
        customer.LastUpdated = DateTime.UtcNow;
        
        // Add to repository (works offline)
        await _syncService.Repository.AddItemAsync(customer, customer.Id);
        
        // Try to push changes to server
        try
        {
            var pushResult = await _syncService.PushAsync();
            if (pushResult.Status == SyncStatus.Conflict)
            {
                // Handle conflicts if needed
                HandleConflicts(pushResult.Conflicts);
            }
        }
        catch (NetworkException)
        {
            // Changes will be pushed later when network is available
            Console.WriteLine("Changes saved locally and will sync when network is available");
        }
    }
    
    public async Task UpdateCustomerAsync(Customer customer)
    {
        customer.LastUpdated = DateTime.UtcNow;
        
        // Update in repository
        await _syncService.Repository.UpdateItemAsync(customer, customer.Id);
        
        // Try to push changes
        try
        {
            await _syncService.PushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Changes saved locally but sync failed: {ex.Message}");
        }
    }
    
    public async Task DeleteCustomerAsync(string customerId)
    {
        // Delete from repository
        await _syncService.Repository.DeleteItemAsync(customerId);
        
        // Push the deletion to server
        await _syncService.PushAsync();
    }
    
    private void HandleConflicts(IEnumerable<SyncConflict> conflicts)
    {
        foreach (var conflict in conflicts)
        {
            Console.WriteLine($"Conflict detected for item {conflict.ItemId}");
            // Apply custom conflict resolution logic if needed
        }
    }
}
```

### Advanced Usage Patterns

#### Working with Multiple Entity Types

You can register multiple entity types for synchronization:

```csharp
// Register multiple entity types
services.AddGenericDataSync<Customer>(options => { options.ApiUrl = "https://api.example.com/customers"; });
services.AddGenericDataSync<Order>(options => { options.ApiUrl = "https://api.example.com/orders"; });
services.AddGenericDataSync<Product>(options => { options.ApiUrl = "https://api.example.com/products"; });

// Inject the specific sync service you need
public class OrderService
{
    private readonly GenericSyncService<Order> _orderSyncService;
    
    public OrderService(GenericSyncService<Order> orderSyncService)
    {
        _orderSyncService = orderSyncService;
    }
    
    // Use the order sync service
}
```

#### Custom Conflict Resolution

The library supports custom conflict resolution strategies:

```csharp
services.AddGenericDataSync<Customer>(options => 
{
    options.ApiUrl = "https://your-api.com/customers";
    options.ConflictResolution = ConflictResolutionStrategy.Custom;
    options.CustomConflictResolver = async (clientItem, serverItem) => 
    {
        // Custom logic to resolve conflicts
        var clientCustomer = clientItem.GetEntity<Customer>();
        var serverCustomer = serverItem.GetEntity<Customer>();
        
        // Example: Use the most recently updated version
        if (clientCustomer.LastUpdated > serverCustomer.LastUpdated)
        {
            return clientItem; // Client wins
        }
        else
        {
            return serverItem; // Server wins
        }
    };
});
```

#### Handling Network Conditions

The library is designed to handle various network conditions gracefully:

```csharp
public async Task SyncWithNetworkAwareness()
{
    try
    {
        // Try to sync
        var result = await _syncService.PushAsync();
        
        // Check the result
        switch (result.Status)
        {
            case SyncStatus.Completed:
                Console.WriteLine($"Sync completed successfully. Items: {result.Count}");
                break;
                
            case SyncStatus.Conflict:
                Console.WriteLine($"Sync completed with conflicts. Conflicts: {result.Conflicts.Count()}");
                break;
                
            case SyncStatus.Failed:
                Console.WriteLine($"Sync failed: {result.Error}");
                break;
        }
    }
    catch (NetworkException ex)
    {
        // Handle offline scenario
        Console.WriteLine($"Network unavailable: {ex.Message}");
        
        // Queue for later sync when network is available
        _syncService.QueueForSync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during sync: {ex.Message}");
    }
}
```

#### Background Synchronization

For applications that need periodic background synchronization:

```csharp
public class BackgroundSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    
    public BackgroundSyncService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Get all sync services that need periodic syncing
                    var customerSyncService = scope.ServiceProvider.GetRequiredService<GenericSyncService<Customer>>();
                    var orderSyncService = scope.ServiceProvider.GetRequiredService<GenericSyncService<Order>>();
                    
                    // Sync each service
                    await customerSyncService.PullAsync();
                    await customerSyncService.PushAsync();
                    
                    await orderSyncService.PullAsync();
                    await orderSyncService.PushAsync();
                    
                    Console.WriteLine("Background sync completed successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background sync error: {ex.Message}");
            }
            
            // Wait before next sync
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}

// Register in Startup.cs
services.AddHostedService<BackgroundSyncService>();
```

### Server Setup

The server uses the standard Datasync server template:

```bash
# Install template
dotnet new -i CommunityToolkit.Datasync.Server.Template.CSharp

# Create new server project
dotnet new datasync-server

# Run server (defaults to port 5000)
dotnet run
```

See the [Server Configuration Guide](./docs/server-config.md) for customization options.

## Testing

Ez-Generic-Data-Sync includes comprehensive testing tools to ensure reliability and correctness:

### Test Runner Script

A versatile testing script is provided in the `_Resources/scripts` directory to simplify running tests:

```bash
# Run all unit tests
./_Resources/scripts/run-tests.sh --unit

# Run all functional tests
./_Resources/scripts/run-tests.sh --functional

# Run both unit and functional tests
./_Resources/scripts/run-tests.sh --all

# Run tests with specific filters
./_Resources/scripts/run-tests.sh --unit --test SyncService

# Display detailed test output
./_Resources/scripts/run-tests.sh --all --verbose
```

### Test Categories

The test suite includes two main categories:

1. **Unit Tests**: Located in `Src/Tests/UnitTests`, these tests verify individual components in isolation.
   
2. **Functional Tests**: Located in `Src/Tests/TestHarness`, this interactive harness tests the library's functionality in realistic scenarios including:
   - Basic CRUD operations
   - Network condition simulation
   - Conflict resolution strategies
   - Offline capabilities
   - Performance under various conditions

### Test Harness

The functional test harness provides a CLI environment for testing the sync functionality with simulated network conditions. Run it directly or through the test script:

```bash
# Run via test script
./_Resources/scripts/run-tests.sh --functional

# Or run directly 
dotnet run --project Src/Tests/TestHarness integration-test
```

For more details on testing methodologies, see the [Usage Guide](./USAGE_GUIDE.md#testing-your-implementation).

## Performance Considerations

Based on our test results, here are some performance metrics to consider:

| Network Condition | Average Response Time |
|-------------------|------------------------|
| Excellent         | ~110-160ms             |
| Good              | ~230-250ms             |
| Fair              | ~380-430ms             |
| Poor              | ~920-970ms             |
| Terrible          | ~2000ms                |

The library handles even terrible network conditions reliably, with successful sync operations completing in all network scenarios except when completely offline (where operations are properly queued for later sync).

## Release Notes

### Version 1.0.0 (May 1, 2025)
- Initial stable release
- Full implementation of generic wrapper for CommunityToolkit.Datasync
- Comprehensive test suite with all tests passing
- Complete documentation and usage examples

## Contributing

Contributions are welcome! Please see our [Contribution Guidelines](./CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

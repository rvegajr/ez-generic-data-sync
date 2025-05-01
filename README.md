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

For development and testing, a lightweight server can be included, but production deployments would use a full Datasync server implementation.

## Features

- [x] Generic wrapper for any C# class
- [x] Type-safe repository operations
- [x] Automatic conflict resolution
- [x] Offline support
- [x] Network condition resilience
- [x] Comprehensive test suite

## Project Status

This project is currently in development. See the [Architecture Checklist](./_Resources/ARCHITECTURE_CHECKLIST.md) for implementation details and progress.

## Getting Started

For detailed implementation instructions, see our [Usage Guide](./USAGE_GUIDE.md) which covers:

- Basic setup and configuration
- Repository implementations
- Working with sync services
- Handling challenging network conditions
- Conflict resolution strategies
- Testing methodologies
- Advanced scenarios for airline notification delivery

### Quick Start

```csharp
// 1. Install NuGet package
// dotnet add package Ez.Generic.DataSync

// 2. Register services
services.AddGenericDataSync<Customer>(options => {
    options.ApiUrl = "https://your-datasync-server.com/api";
});

// 3. Use in your service
public class CustomerService
{
    private readonly GenericSyncService<Customer> _syncService;
    
    public CustomerService(GenericSyncService<Customer> syncService)
    {
        _syncService = syncService;
    }
    
    public async Task SyncCustomers()
    {
        // Pull latest from server
        await _syncService.PullAsync();
        
        // Work with strongly-typed data
        var customers = await _syncService.Repository.GetItemsAsync();
        foreach (var customer in customers)
        {
            Console.WriteLine($"Customer: {customer.Name}");
        }
        
        // Add new customer
        await _syncService.Repository.AddItemAsync(new Customer { 
            Name = "New Customer", 
            Email = "customer@example.com" 
        });
        
        // Push changes to server
        await _syncService.PushAsync();
    }
}
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

## Contributing

Contributions are welcome! Please see our [Contribution Guidelines](./CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

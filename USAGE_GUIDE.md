# Ez-Generic-Data-Sync Usage Guide

This guide provides a comprehensive overview of how to implement and utilize the Ez-Generic-Data-Sync library in your applications, especially in challenging network environments like those faced by airline pilots.

## What is Ez-Generic-Data-Sync?

Ez-Generic-Data-Sync is a lightweight, flexible library designed to handle data synchronization between client applications and backend servers in challenging network conditions. It provides reliable data transfer with:

- **Robust offline handling** - Continue working without network connectivity
- **Automatic conflict resolution** - Intelligently merge changes from different sources
- **Flexible retry mechanisms** - Custom retry policies with exponential backoff
- **Network-aware operations** - Adaptive behavior based on network quality
- **Priority-based delivery** - Critical data gets transmitted first when network is limited
- **Type-safe repositories** - Generic repositories to work with any data model

Originally developed to solve the challenge of delivering flight notifications to airline pilots who frequently operate in areas with poor or intermittent network connectivity, the library ensures that critical data reaches its destination even under challenging conditions. Its architecture allows for extensive customization while providing sensible defaults that work well in most scenarios.

## Table of Contents

1. [Installation](#installation)
2. [Basic Setup](#basic-setup)
3. [Core Concepts](#core-concepts)
4. [Repository Implementation](#repository-implementation)
5. [Working with Sync Services](#working-with-sync-services)
6. [Handling Network Conditions](#handling-network-conditions)
7. [Conflict Resolution](#conflict-resolution)
8. [Testing Your Implementation](#testing-your-implementation)
9. [Advanced Scenarios](#advanced-scenarios)
10. [Troubleshooting](#troubleshooting)

## Installation

### NuGet Package (Recommended)

```
dotnet add package Ez.Generic.DataSync
```

### Direct Reference

```
dotnet add reference path/to/Ez.Generic.DataSync.Core.csproj
dotnet add reference path/to/Ez.Generic.DataSync.Extensions.csproj
```

## Basic Setup

### 1. Create a Model Class

Your model class should implement `ISyncableEntity` or you can use the `SyncableEntityWrapper<T>` to wrap existing models:

```csharp
public class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 2. Create a Repository

Implement `IRepository<SyncableEntityWrapper<T>>` or use one of the provided implementations:

```csharp
// Use the provided GenericRepository with a custom data store
var repository = new GenericRepository<Customer>(myCustomDataStore);

// Or create a DirectEntityRepository adapter for an existing repository
var existingRepo = new MyCustomerRepository();
var repository = new DirectEntityRepository<Customer>(existingRepo);
```

### 3. Configure the Sync Service

```csharp
// Set up the sync service
var syncService = new GenericSyncService<Customer>(repository);

// Configure network policies
syncService.SetNetworkPolicy(new NetworkPolicy 
{ 
    MaxRetries = 3,
    TimeoutSeconds = 30,
    RequireWifi = false,
    AutoSyncOnNetworkAvailable = true
});
```

### 4. Basic Operations

```csharp
// Pull data from server
await syncService.PullAsync();

// Get all customers
var customers = await syncService.Repository.GetItemsAsync();

// Add a new customer
await syncService.Repository.AddItemAsync(new Customer 
{ 
    Id = Guid.NewGuid().ToString(),
    Name = "New Customer",
    Email = "customer@example.com" 
});

// Update a customer
var customer = customers.First();
customer.Name = "Updated Name";
await syncService.Repository.UpdateItemAsync(customer, customer.Id);

// Delete a customer
await syncService.Repository.DeleteItemAsync(customerId);

// Push changes to server
await syncService.PushAsync();
```

## Core Concepts

### SyncableEntityWrapper<T>

The `SyncableEntityWrapper<T>` class wraps your domain objects and adds synchronization-specific properties:

```csharp
// Access the wrapped entity
var customer = wrapper.Data;

// Access sync-specific properties
bool isDeleted = wrapper.Deleted;
string id = wrapper.Id;
DateTime lastModified = wrapper.LastModified;
```

### Repository Pattern

The library implements the Repository pattern for data access, with these key operations:

- `GetItemAsync(id)` - Get a specific item
- `GetItemsAsync()` - Get all items
- `AddItemAsync(item, id)` - Add a new item
- `UpdateItemAsync(item, id)` - Update an existing item
- `DeleteItemAsync(id)` - Mark an item for deletion (soft delete)

### Network Policies

Configure how the sync service handles network challenges:

```csharp
var policy = new NetworkPolicy
{
    // Maximum number of retry attempts
    MaxRetries = 5,
    
    // Timeout in seconds
    TimeoutSeconds = 30,
    
    // Whether WiFi is required for sync
    RequireWifi = false,
    
    // Auto-sync when network becomes available
    AutoSyncOnNetworkAvailable = true,
    
    // Use exponential backoff for retries
    UseExponentialBackoff = true
};

syncService.SetNetworkPolicy(policy);
```

## Repository Implementation

### Creating a Custom Repository

```csharp
public class AzureTableRepository<T> : IRepository<SyncableEntityWrapper<T>> where T : class
{
    private readonly CloudTable _table;
    
    public AzureTableRepository(string connectionString, string tableName)
    {
        var storageAccount = CloudStorageAccount.Parse(connectionString);
        var tableClient = storageAccount.CreateCloudTableClient();
        _table = tableClient.GetTableReference(tableName);
        _table.CreateIfNotExists();
    }
    
    public async Task<SyncableEntityWrapper<T>> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        // Implementation for Azure Table Storage
        // ...
    }
    
    // Implement other methods from IRepository
    // ...
}
```

### Using Direct Entity Repository Adapter

The `DirectEntityRepository<T>` adapter lets you bridge between repositories that work directly with entities and the `SyncableEntityWrapper<T>` repositories expected by the sync framework:

```csharp
// Your existing repository that works with Customer directly
public class CustomerRepository : IRepository<Customer>
{
    // Implementation...
}

// Create an adapter
var directRepo = new CustomerRepository();
var syncRepo = new DirectEntityRepository<Customer>(directRepo);

// Now you can use syncRepo with GenericSyncService
var syncService = new GenericSyncService<Customer>(syncRepo);
```

## Working with Sync Services

### GenericSyncService<T>

The primary class for handling synchronization:

```csharp
// Basic operations
await syncService.PullAsync(); // Pull from server
await syncService.PushAsync(); // Push to server
await syncService.SyncAsync(); // Push and pull

// Get sync status
var syncStatus = syncService.GetSyncStatus();
if (syncStatus.HasPendingChanges)
{
    Console.WriteLine($"Changes to sync: {syncStatus.PendingChangesCount}");
}
```

### Extending the Sync Service for Custom Logic

```csharp
public class AirlineNotificationService : GenericSyncService<AirlineNotification>
{
    public AirlineNotificationService(IRepository<SyncableEntityWrapper<AirlineNotification>> repository) 
        : base(repository)
    {
    }
    
    // Add specialized notification delivery methods
    public async Task<bool> DeliverNotificationAsync(string notificationId, int maxAttempts = 3)
    {
        // Custom delivery logic with retry support
        // ...
    }
    
    // Helper methods for accessing items through the repository
    public async Task<AirlineNotification> GetNotificationAsync(string id)
    {
        var wrapper = await GetRepository().GetItemAsync(id);
        return wrapper?.Data;
    }
    
    // More custom methods...
}
```

## Handling Network Conditions

### Network Quality Simulation

For testing, the library includes a `NetworkSimulator` that can simulate various network conditions:

```csharp
// In your test code
var networkSimulator = new NetworkSimulator();
networkSimulator.SetNetworkQuality(NetworkQuality.Poor);

// Use with sync service
var syncService = new SyncServiceTester<Customer>(mockSyncService, repository, networkSimulator);
```

### Automatic Retries

The library handles automatic retries with exponential backoff:

```csharp
// Configure retry policy
syncService.SetNetworkPolicy(new NetworkPolicy
{
    MaxRetries = 5,
    UseExponentialBackoff = true,
    BackoffMultiplier = 1.5, // Each retry waits 1.5x longer
    InitialBackoffSeconds = 1 // Start with 1 second
});

// The service will automatically retry failed operations
await syncService.PushAsync();
```

### Offline Mode

```csharp
// Check if offline
if (syncService.IsOffline)
{
    // Perform offline-only operations
    // Changes will be queued for later sync
}

// Force offline mode
syncService.SetOfflineMode(true);

// Operations will be stored locally until online again
await syncService.Repository.AddItemAsync(newCustomer);
```

## Conflict Resolution

### Default Conflict Handling

By default, the library uses a "server wins" strategy:

```csharp
// This will detect conflicts and report them in the SyncResult
var result = await syncService.PushAsync();

if (result.Status == SyncStatus.Conflict)
{
    Console.WriteLine($"Conflicts detected: {result.ConflictCount}");
}
```

### Custom Conflict Resolution

```csharp
// Create a custom conflict resolver
public class CustomerConflictResolver : IConflictResolver<Customer>
{
    public SyncableEntityWrapper<Customer> ResolveConflict(
        SyncableEntityWrapper<Customer> localVersion,
        SyncableEntityWrapper<Customer> serverVersion)
    {
        // Custom logic to merge changes
        var resolved = new Customer
        {
            Id = localVersion.Data.Id,
            // Take server name if it exists, otherwise local
            Name = !string.IsNullOrEmpty(serverVersion.Data.Name) 
                ? serverVersion.Data.Name 
                : localVersion.Data.Name,
            // Always take local email
            Email = localVersion.Data.Email
        };
        
        return new SyncableEntityWrapper<Customer>(resolved, localVersion.Id);
    }
}

// Register your resolver with the sync service
syncService.SetConflictResolver(new CustomerConflictResolver());
```

## Testing Your Implementation

### Automated Testing

The library includes an automated test harness for validating your implementation:

```csharp
// Create automated test runner
var tests = new AutomatedFunctionalTests();

// Run all tests
await tests.RunAllTests();

// Run specific test categories
await tests.RunNetworkConditionTests();
await tests.RunCrudOperationTests();
await tests.RunConflictResolutionTests();
```

### Manual Testing with the Test Harness

```csharp
// Initialize test harness
var testHarness = new Program();

// Run interactive tests
await testHarness.Run();
```

### Testing Network Scenarios

The test harness includes comprehensive network scenario testing:

```csharp
// Test under different network conditions
_networkSimulator.SetNetworkQuality(NetworkQuality.Excellent);
var excellentResult = await _syncService.PullAsync();

_networkSimulator.SetNetworkQuality(NetworkQuality.Poor);
var poorResult = await _syncService.PullAsync();

_networkSimulator.SetNetworkQuality(NetworkQuality.Offline);
var offlineResult = await _syncService.PullAsync();
```

## Advanced Scenarios

### Prioritized Notifications

For critical data like airline pilot notifications:

```csharp
// Create high-priority notification
var criticalNotification = new AirlineNotification
{
    Id = Guid.NewGuid().ToString(),
    Title = "Weather Alert",
    Message = "Turbulence reported ahead",
    Priority = NotificationPriority.High,
    ExpiresAt = DateTime.UtcNow.AddHours(2)
};

// Add to sync service
await airlineService.AddNotificationAsync(criticalNotification);

// Custom delivery with priority handling
await airlineService.DeliverAllNotificationsAsync();
```

### Multi-Device Sync

```csharp
// For sync across multiple devices, use consistent IDs
var sharedItem = new SharedDocument
{
    Id = "shared-doc-123", // Same ID across devices
    Title = "Flight Manual",
    Content = "Updated procedures for emergency landing",
    Version = 2
};

await syncService.Repository.UpdateItemAsync(sharedItem, sharedItem.Id);
await syncService.PushAsync();
```

### Long-Duration Flight Mode

For extended offline operations:

```csharp
// Before flight
syncService.SetNetworkPolicy(new NetworkPolicy
{
    MaxRetries = 10,
    MaxStorageDays = 7, // Keep data for 7 days
    PrioritizeDelivery = true // Send high-priority items first when reconnecting
});

// During flight
syncService.SetOfflineMode(true);

// Record changes during flight
await syncService.Repository.AddItemAsync(flightLog);

// After landing, re-enable network and sync
syncService.SetOfflineMode(false);
await syncService.PushAsync();
```

## Troubleshooting

### Common Issues

1. **Synchronization Fails**: Check network connectivity and verify server endpoint
2. **Conflict Resolution Errors**: Ensure your custom resolver handles all edge cases
3. **Performance Issues**: Consider batch operations for large datasets

### Logging

Enable detailed logging to troubleshoot issues:

```csharp
syncService.EnableLogging(LogLevel.Verbose);
syncService.SetLogHandler((level, message) => {
    Console.WriteLine($"[{level}] {message}");
});
```

### Diagnostics

```csharp
// Get diagnostic information
var diagnostics = syncService.GetDiagnostics();
Console.WriteLine($"Total sync attempts: {diagnostics.TotalSyncAttempts}");
Console.WriteLine($"Failed attempts: {diagnostics.FailedAttempts}");
Console.WriteLine($"Average sync time: {diagnostics.AverageSyncTimeMs}ms");
```

## Conclusion

The Ez-Generic-Data-Sync library provides a robust framework for handling data synchronization in challenging network environments. By following this guide, you should be able to implement reliable sync functionality in your applications, especially for scenarios requiring high reliability like airline pilot notifications.

For additional examples and detailed API documentation, see the [API Reference](./docs/api-reference.md).

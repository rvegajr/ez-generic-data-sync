# ez-generic-data-sync
Utilizing the awesome CommunityToolkit/Datasync library but adding the ability to use typed objects with it.

## Development Workflow

**PRIME DIRECTIVE**: For each feature implementation, follow these two steps in sequence:

1. **Begin with Test-Driven Design (TDD)**
   - Write unit tests that define the expected behavior
   - Implement only the code necessary to pass those tests
   - Refactor for clean design while maintaining test coverage

2. **End with Test Harness Implementation**
   - After unit tests pass, implement a CLI-based test harness
   - Use inheritance to extend production code (not duplicate it)
   - The harness must validate real-world scenarios with actual dependencies
   - Document test results in the standard format

This approach ensures both correct implementation and practical resilience under varied conditions.

## Implementation Checklist

### 0️⃣ Testing Approach
- [ ] Implement dual testing strategy:
  - **Unit Tests** - Drive the design of each component (TDD)
  - **Functional Test Harness** - Verify real-world behavior after implementation
- [ ] Setup test projects:
  ```
  /Src
    /Tests
      /UnitTests - xUnit tests for TDD
      /TestHarness - CLI-based functional tests
  ```
- [ ] Unit Testing Requirements (Test-Driven Development):
  - [ ] Create unit tests **before** implementing each feature
  - [ ] Follow the "AAA" pattern (Arrange, Act, Assert)
  - [ ] Use mocking frameworks to isolate components under test
  - [ ] Test edge cases and error conditions
  - [ ] Target 90%+ code coverage for core components
  - [ ] Create interface specifications through tests first

- [ ] Functional Test Harness Requirements:
  - [ ] Implement only **after** unit-tested features are complete
  - [ ] Create CLI-based harness that tests end-to-end workflows
  - [ ] Use inheritance to extend production classes for testability
  - [ ] Implement network condition simulation
  - [ ] Test actual persistence with real repositories
  - [ ] Provide detailed reporting in `TEST_RESULTS.md`
  - [ ] Include performance benchmarking under various loads

### 1️⃣ Setup Project Structure
- [ ] Install NuGet packages:
  ```
  CommunityToolkit.Datasync.Client
  CommunityToolkit.Datasync.Server (for test harness)
  ```
- [ ] Create core project structure:
  ```
  /Src
    /Core - Core interfaces and base implementation
    /Extensions - Extension methods for easy integration
    /Adapters - Database and network adapters
    /Tests - CLI-based test harness
  ```

### 2️⃣ Implement Generic Entity Wrapper
- [ ] Create `ISyncableEntity<T>` interface:
  ```csharp
  public interface ISyncableEntity<T> : ISyncableEntity where T : class
  {
      T Data { get; }
      void UpdateFrom(T source);
  }
  ```
- [ ] Implement `SyncableEntityWrapper<T>`:
  ```csharp
  public class SyncableEntityWrapper<T> : ISyncableEntity<T> where T : class
  {
      public string Id { get; set; }
      public DateTimeOffset UpdatedAt { get; set; }
      public bool Deleted { get; set; }
      public string Version { get; set; }
      public T Data { get; private set; }
      
      // Implementation methods...
  }
  ```

### 3️⃣ Create Generic Repository
- [ ] Implement `GenericRepository<T>`:
  ```csharp
  public class GenericRepository<T> : IRepository<SyncableEntityWrapper<T>> where T : class
  {
      private readonly IRepository<ISyncableEntity> _innerRepository;
      
      // CRUD operations that wrap the underlying repository
  }
  ```
- [ ] Add extension methods for syntactic sugar:
  ```csharp
  public static class RepositoryExtensions
  {
      public static Task<T> GetItemAsync<T>(this IRepository<SyncableEntityWrapper<T>> repository, string id)
          where T : class
      // More helper methods...
  }
  ```

### 4️⃣ Create Generic SyncService
- [ ] Implement wrapper around DataSync's SyncService:
  ```csharp
  public class GenericSyncService<T> where T : class
  {
      private readonly SyncService _innerService;
      private readonly IRepository<SyncableEntityWrapper<T>> _repository;
      
      // Methods for PullAsync, PushAsync, etc.
  }
  ```
- [ ] Add automatic type conversion and conflict resolution handlers

### 5️⃣ Setup Testing Harness
- [ ] Implement unit tests (TDD approach):
  ```csharp
  [Fact]
  public async Task GenericSyncService_PullAsync_ShouldRetrieveItems()
  {
      // Arrange - Setup mock repositories and services
      // Act - Call the method under test
      // Assert - Verify expected behavior
  }
  ```

- [ ] Create CLI test harness that inherits from production code:
  ```csharp
  // Follow the in-place testing with inheritance pattern
  public class SyncServiceTester<T> : GenericSyncService<T> where T : class
  {
      // Override key methods to inject test conditions & instrumentation
      public override async Task<SyncResult> PullAsync(CancellationToken token = default)
      {
          // Instrument the method for testing
          Console.WriteLine($"TEST: Starting Pull operation for {typeof(T).Name}");
          var result = await base.PullAsync(token);
          Console.WriteLine($"TEST: Pull completed with {result.Status}");
          return result;
      }
  }
  ```

- [ ] Implement network simulation with configurable conditions:
  ```csharp
  public enum NetworkQuality { Excellent, Good, Fair, Poor, Terrible, Offline }
  
  public class NetworkConditionAdapter : INetworkAdapter
  {
      public NetworkQuality CurrentQuality { get; set; } = NetworkQuality.Good;
      
      // Methods to simulate delays, dropouts, etc. based on quality setting
      public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
      {
          // Apply network condition effects before forwarding request
      }
  }
  ```

- [ ] Create comprehensive test scenarios covering:
  - [ ] Basic CRUD with different entity types
  - [ ] Sync conflicts and resolution strategies
  - [ ] Offline and reconnection scenarios
  - [ ] Performance under varied network conditions
  - [ ] Edge cases (very large datasets, malformed data)
  - [ ] Concurrent operations

### 6️⃣ Add DI Container Registration
- [ ] Create extension methods for registering services:
  ```csharp
  public static class ServiceCollectionExtensions
  {
      public static IServiceCollection AddGenericDataSync<T>(
          this IServiceCollection services, Action<SyncOptions> configureOptions = null)
          where T : class
      {
          // Register all required services
      }
  }
  ```

### 7️⃣ Create Sample Implementation
- [ ] Provide a working end-to-end example:
  ```csharp
  // Setup
  services.AddGenericDataSync<Customer>();
  
  // Usage
  var syncService = serviceProvider.GetService<GenericSyncService<Customer>>();
  await syncService.PullAsync();
  
  // Add new item
  await syncService.Repository.AddItemAsync(new Customer { Name = "Test" });
  
  // Push changes
  await syncService.PushAsync();
  ```

### 8️⃣ Document Usage & Extension Points
- [ ] Create comprehensive XML documentation
- [ ] Provide extension examples:
  - Custom conflict resolution
  - Alternative storage providers
  - Integration with UI frameworks

### 9️⃣ Implement Test Results Dashboard
- [ ] Create `TEST_RESULTS.md` template:
  ```markdown
  # Generic Data Sync Test Results

  ## Summary
  - **Last Run**: {datetime}
  - **Status**: {pass/fail}
  - **Total Tests**: {count}
  - **Passed**: {count}
  - **Failed**: {count}
  - **Performance**: {metrics}

  ## Test Categories
  
  ### Basic Operations
  | Test | Status | Duration | Notes |
  |------|--------|----------|-------|
  | Create Entity | ✅ | 45ms | |
  | Retrieve Entity | ✅ | 12ms | |
  
  ### Sync Operations
  ...
  ```

- [ ] Automate test result generation:
  ```csharp
  public class TestReporter
  {
      private readonly StringBuilder _report = new();
      private readonly List<TestResult> _results = new();
      
      public void RecordResult(string testName, bool passed, TimeSpan duration, string notes = "")
      {
          _results.Add(new TestResult(testName, passed, duration, notes));
      }
      
      public async Task GenerateReport(string path)
      {
          // Generate markdown report and write to TEST_RESULTS.md
      }
  }
  ```

- [ ] Implement performance benchmarking:
  ```csharp
  public class PerformanceBenchmark
  {
      public async Task<BenchmarkResult> MeasureOperation(Func<Task> operation,
          int iterations = 100,
          NetworkQuality quality = NetworkQuality.Good)
      {
          // Measure operation performance under specified conditions
      }
  }
  ```

### 🔟 Architecture Documentation

- [ ] Create architecture diagram showing:
  - [ ] Component dependencies and communication
  - [ ] Generic wrapper relationship to CommunityToolkit/Datasync
  - [ ] Extension points for customization
  
- [ ] Document design principles:
  - [ ] Separation of concerns
  - [ ] Generics usage
  - [ ] Error handling strategy
  - [ ] Performance considerations
  
- [ ] Create decision log for key architectural choices:
  ```markdown
  | Decision | Alternatives | Reasoning |
  |----------|-------------|-----------|
  | Use wrapper pattern vs fork library | Direct modification, new library | Maintains upgradability while adding strong typing |
  ```

### 11️⃣ Continuous Integration / Continuous Deployment
- [ ] Set up GitHub Actions (or Azure DevOps) workflow:
  - Build on push & PR
  - Run unit tests and CLI functional tests
  - Collect code coverage and upload to Badges
  - Execute static analyzers (dotnet analyzers, ReSharper CLT `jb inspectcode`)
  - Publish NuGet package on tagged release (semantic version)
- [ ] Include status and coverage badges at top of README

### 12️⃣ Security & Secrets Management
- [ ] Centralize secrets using `dotnet user-secrets` (dev) or Azure Key Vault (prod)
- [ ] Ensure HTTPS is enforced for all endpoints
- [ ] Add dependency scanning (GitHub Dependabot)
- [ ] Document secure coding guidelines and OWASP considerations

### 13️⃣ Logging & Telemetry
- [ ] Integrate `Microsoft.Extensions.Logging` + OpenTelemetry
- [ ] Emit correlation IDs per sync operation for traceability
- [ ] Provide sample Grafana/Prometheus dashboard JSON

### 14️⃣ Versioning & Release Management
- [ ] Adopt [Semantic Versioning 2.0.0](https://semver.org)
- [ ] Maintain `CHANGELOG.md` with unreleased, added, changed, fixed sections
- [ ] Automate changelog update in release workflow

### 15️⃣ Coding Standards & Contribution Workflow
- [ ] Enforce `.editorconfig` & StyleCop analyzers
- [ ] Provide PR template outlining test evidence requirements
- [ ] Use `git flow` or trunk-based branching (decide and document)
- [ ] Code reviews require two approvals and green CI

### 16️⃣ Developer Environment Setup
- [ ] Required SDK: .NET 8.0+ (`dotnet --version`)
- [ ] Recommended IDEs: Visual Studio 2022, JetBrains Rider, VS Code
- [ ] Pre-commit hooks:
  ```bash
  dotnet format
  dotnet test --no-build
  ```
- [ ] Local scripts:
  ```bash
  ./scripts/restore.sh
  ./scripts/build.sh
  ./scripts/test.sh
  ```

## Looking Ahead

Future enhancements that could be considered:

1. **UI Integration Adapters** - Ready-made adapters for common UI frameworks:
   - WPF (ObservableCollection integration)
   - Blazor (StateHasChanged notifications)
   - MAUI (UI thread synchronization)

2. **Advanced Conflict Resolution** - Specialized handlers for different entity types:
   - Field-level merging
   - Business rule validation during merges
   - User-prompted resolution

3. **Offline-First Optimizations** - Improvements for offline scenarios:
   - Predictive pre-syncing
   - Background sync scheduling
   - Delta compression

## Getting Started

To use this library:

1. Install the NuGet package (when published)
2. Add to your services:
   ```csharp
   services.AddGenericDataSync<YourType>(options => {
       options.ApiUrl = "https://your-server-url/api";
   });
   ```
3. Inject and use the sync service in your code:
   ```csharp
   public class YourService
   {
       private readonly GenericSyncService<YourType> _syncService;
       
       public YourService(GenericSyncService<YourType> syncService)
       {
           _syncService = syncService;
       }
       
       public async Task SyncData()
       {
           await _syncService.PullAsync();
           // Work with data...
           await _syncService.PushAsync();
       }
   }
   ```

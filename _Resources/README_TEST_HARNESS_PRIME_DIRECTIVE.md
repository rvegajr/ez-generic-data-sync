# Universal CLI Test Harness Example

A flexible command-line testing harness for validating any project's core business logic without needing the full application or UI.

> **Disclaimer**: The following document is a collection of **examples only**.  
> It illustrates how one *might* design a CLI-based test harness.  
> Feel free to copy, adapt, or ignore any part of it—none of the approaches here are required for your project.

### Sample Guideline: Separation of Concerns ("Prime Directive")

Testing modules "in-place" means executing the very same classes that run in production, but within an isolated CLI environment.  
To achieve deep coverage without code duplication we extend production classes via inheritance and override only the seams that need instrumentation or stubbing (for example, network or database adapters). This approach can help to:

- Zero drift between test and production logic  
- Fine-grained CLI control over inputs, timing, and network conditions  
- The ability to probe every protected or internal method without weakening encapsulation  

Example:

```swift
// Production class
class PaymentProcessor { /* ... */ }

// Inheriting test double
final class PaymentProcessorTests: PaymentProcessor {
    override func sendToGateway(_ txn: Transaction) -> Result {
        // Inject mock gateway
        return .success
    }
}
```

> You might derive your test classes from the production counterpart instead of re-implementing logic.

## Overview

The test harness provides a controlled environment for testing the new architecture, which leverages native Azure Service Bus calls for near real-time data synchronization. It includes simulated network conditions, offline storage capabilities, and detailed reporting.

## Components

The test harness is organized into several modules:

- **AzureSyncTester**: Core test implementation for basic CRUD operations and sync functionality
- **SimpleAzureTester**: Streamlined version for quick validation with detailed timing information
- **MVVMTests**: Test implementation for MVVM architecture components, focusing on view models and controllers
- **TreatmentSessionSyncTester**: Tests specifically for treatment session synchronization
- **ServerPendingTester**: Specialized tests for server pending changes, state transitions, and network performance

## Recent Additions

### Example: Azure Sync Scenario
**Note:** The following Azure-specific example is provided for illustration. Adapt or skip if your project does not involve Azure Service Bus synchronization.

#### Sample Module: ServerPendingTester (Azure Sync)

The ServerPendingTester module provides comprehensive testing for:

1. **Server Pending Changes**: Tests the correct handling of server-to-client downloads and state transitions
2. **Record State Transitions**: Verifies that records transition correctly between sync states
3. **Network Latency Impact**: Quantifies the impact of different network conditions on sync performance
4. **Batch Processing**: Tests the system's ability to handle batch downloads efficiently
5. **Multi-User Sync Performance**: Measures sync performance across multiple users (5 records × 8 users)
6. **Sync Icon and Connection States**: Validates the business logic determining which icons and connection states to display

Key findings from these tests:

- Poor network conditions result in ~5x slower sync performance than good conditions
- Batch processing is ~2x more efficient than individual user syncs
- Multi-user scaling shows consistent performance across users

### Test Results

Detailed test results are now stored in the [TEST_RESULTS.md](./TEST_RESULTS.md) file for easy reference and tracking of system quality over time.

## Example: Aligning CLI Harness with an iOS App

This example shows how a CLI harness can align with an iOS application to ensure consistent behavior and testing:

### Shared Components

- **ServiceLocator**: Used for dependency injection in both environments
- **TreatmentSessionSyncManager**: Handles sync operations for treatment sessions
- **SyncManager**: Base synchronization functionality

### Prime Directive Implementation

The fundamental architecture follows the prime directive:

> "If we're going to test, we need to test with code that's going to be used in the UI."

To implement this principle:

1. **Core Business Logic**: Lives in the iOS app as the source of truth
2. **Adapter Pattern**: The test harness uses thin adapters that connect to the actual iOS app code
3. **No Duplicate Implementations**: We avoid creating separate test-only implementations of business logic

### Adapter Architecture

![Adapter Pattern](https://mermaid.ink/img/pako:eNp1kD1PwzAQhv-KdROgQD-SpkltCQwdUKOKAfnD9UAWjl35nJaq-t_xJUWwMNy9z73P3VlOwlOTklqZ5j12LY7CtpuqJuBKO3wDV52yDkODG-QE0MCeNLAgYZQjFVJyLvlSUnGk3Hx3VT-ZVlgc1U-FawP5hCeaxhlmX-8t_WcJtlXXHHoZXHMQzf3Nk12D8_w5o4FfdgO6DPSs_FQYi3cxPOdY7pP3e9IHgxE8BaedswNssdCw2-GNiXvnkEsW4cPE8Zt5Y1pMdpJCFxLZ42_RWMX-RKtxpFVDxmCbxZm-mdN27a3bnhlnLnRRaavT0YxsJMsYz7OlLIvVKs_XOc_LbM2kTFmSzK7zjSlGSVZS5nMhZlmy5CzNU_UD3WF9Ng)

This approach ensures:
- **Exact Code Testing**: We test the exact code that will run in production
- **Single Source of Truth**: Business logic is only defined once
- **Realistic Test Coverage**: Tests reflect real-world usage scenarios

### Import Compatibility

To maintain alignment between the test harness and iOS app:

1. The iOS app has implementations of all necessary components used in tests
2. The test harness contains its own test-specific implementations
3. Both environments follow the same architectural patterns

This alignment ensures that tests in the CLI environment accurately represent the behavior in the iOS app.

## Example: MVVM Test Harness

The following sample demonstrates how to test MVVM view models in isolation from UI components. Use or adapt as needed for projects using the MVVM pattern:

### MVVM Components

The MVVM Test Harness includes the following components:

- **TreatmentSessionsScreen**: A module containing the view model and tests for the Treatment Sessions screen
- **TreatmentSessionsViewModel**: The view model for the Treatment Sessions screen, containing all business logic
- **TreatmentSessionsViewModelTester**: A tester for the TreatmentSessionsViewModel

### MVVM Tests

The MVVM Test Harness includes the following tests:

1. **Initial Session Loading**
   - Tests loading sessions from local storage
   - Verifies correct loading state transitions
   - Confirms sessions are displayed in the correct order

2. **Session Creation**
   - Tests creating a new therapy session
   - Verifies session is added to the view model's state
   - Confirms pending sync count is updated

3. **Session Sync**
   - Tests manual synchronization of sessions
   - Verifies sync state transitions
   - Confirms sync completion

4. **Network State Changes**
   - Tests behavior when network conditions change
   - Verifies sync behavior under poor network conditions
   - Confirms network state is correctly exposed to the UI

5. **Offline Recovery**
   - Tests behavior when offline
   - Verifies sessions remain pending when offline
   - Confirms recovery when network is restored

### Running MVVM Tests

To run the MVVM tests, use the `run_mvvm_tests.sh` script:

```bash
./run_mvvm_tests.sh
```

The script will build and run the test harness, and display the results in the console.

### Creating New MVVM Tests

To create tests for a new screen:

1. Create a new folder under `Sources/MVVMTests` for the screen
2. Create a view model that contains all business logic
3. Create a tester that tests the view model in isolation
4. Update the Package.swift file to include the new module
5. Update the main.swift file to run the new tests

This approach ensures that all business logic is thoroughly tested before integration with the UI, adhering to the prime directive of separating business logic from UI components.

## Features

- **Mock Implementations**: Simulated Azure Service Bus, offline storage, and other core components
- **Network Simulation**: Tests under various network conditions (excellent, good, fair, poor, terrible, offline)
- **CRUD Operations**: Comprehensive testing of Create, Read, Update, and Delete operations
- **Sync Testing**: Validation of synchronization mechanisms including conflict handling
- **Performance Metrics**: Detailed timing and throughput information for operations
- **Offline Capabilities**: Testing of offline operation and recovery scenarios
- **UI State Simulation**: Testing UI state transitions and representation without requiring actual UI
- **Bulk Session Data Operations**: Creation of realistic session data with varied parameters, support for multiple environments, configurable session count for load testing, sample data clearing capabilities, and detailed session metrics and usage patterns
- **Conflict Resolution**: Testing various conflict resolution strategies (server-wins, client-wins, last-modified-wins, merge)
- **Dead Letter Queue Management**: Testing message failure handling, retry mechanisms, recovery strategies, and analytics
- **HIPAA Compliance Validation**: Testing data encryption, access controls, audit logging, and breach detection
- **Authentication Security**: Testing Azure authentication flows, token refresh, multi-tenant access, and connection security
- **Multi-Device Synchronization**: Testing data consistency across devices, concurrent edits, and priority-based resolution
- **Session Completion Test**: Simulates completing a therapy session with real therapy data and tests sync behavior under different network conditions

## Sample Implementation Checklist

When **you choose** to implement new features, you *could* follow a process like this:

1. First develop and test the core logic in the test harness
2. Ensure comprehensive test coverage of edge cases and failure scenarios
3. Only after thorough testing, integrate the tested components with UI elements
4. UI components should primarily bind to properties and call methods on tested components
5. Any UI-specific logic should be minimal and focused on presentation concerns only

Following a checklist like this can help keep an application robust, testable, and maintainable.

## Example Architecture Overview

The test harness implements simplified versions of the core Azure sync components:

- **EnhancedMockAzureServiceBus**: Simulates Azure Service Bus connectivity with configurable network conditions
- **MockOfflineStorage**: Provides persistent storage capabilities for offline operation
- **NetworkSimulator**: Controls simulated network conditions and latency
- **TestPatient**: Sample implementation of SyncableEntity for testing purposes
- **UIStateSimulator**: Manages UI state representation and state transitions
- **SessionDataGenerator**: Creates varied, realistic sample session data for testing
- **ConflictResolver**: Implements various conflict resolution strategies for sync conflicts
- **DeadLetterQueueManager**: Manages message failure handling and recovery strategies
- **MockServer**: Simulates server-side storage with different security levels for testing
- **MockAuthService**: Simulates Azure authentication services for token-based authentication
- **DeviceSimulator**: Simulates multiple devices syncing with the same backend
- **TreatmentSessionSyncManager**: The component under test

## Getting Started

### Prerequisites

- Swift 5.7+
- macOS 12.0+

### Building and Running

#### Standard Test Suite

```bash
# Execute the test harness
./build_and_run.sh
```

#### Simple Test Suite

```bash
# Run the simplified test with timing information
./run_simple_test.sh
```

#### Network Performance Tests

```bash
# Run dedicated network performance tests
./run_network_test.sh
```

#### UI State Tests

```bash
# Run UI state representation tests
./run_ui_state_test.sh
```

#### Session Data Loading

```bash
# Generate sample session data (local testing)
./load_session_data.sh --env local --count 150

# Generate data for TestFlight with connection string
./load_session_data.sh --env testflight --connection "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=your-policy;SharedAccessKey=your-key" --count 150

# Clear existing data in Production
./load_session_data.sh --env production --connection "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=your-policy;SharedAccessKey=your-key" --clear
```

#### Advanced Testing Modules

```bash
# Run all test modules in sequence
./run_all_tests.sh

# Run individual advanced test modules
./.build/debug/ConflictResolutionTester
./.build/debug/DeadLetterQueueTester
./.build/debug/HIPAAComplianceTester
./.build/debug/AuthenticationTester
./.build/debug/MultiDeviceSyncTester
./.build/debug/TreatmentSessionSyncManagerTester
```

#### Running Conflict Resolution Tests

The ConflictResolutionTester implements comprehensive testing for data conflict scenarios that occur during synchronization:

```bash
# Run the conflict resolution tests
./.build/debug/ConflictResolutionTester

# Run with timing information
time ./.build/debug/ConflictResolutionTester

# Run with specific focus on merge conflicts only (with script)
./run_conflict_test.sh --focus=merge
```

The conflict resolution tests validate:
- Last-write-wins resolution when timestamps differ
- Field-level merging for non-conflicting field updates
- Detection of scenarios requiring manual intervention
- Proper UI state updates during conflict resolution
- Multi-device conflict resolution scenarios

After running the tests, review the TEST_RESULTS.md file for detailed performance metrics and test outcomes.

## Test Categories

### General Sync Tests

- Basic synchronization operations
- Offline operation handling
- Network recovery testing
- Conflict resolution
- Batch processing

### CRUD Operations

- Individual Create, Read, Update, Delete operations
- Batch operations
- Error handling

### Azure Service Bus Tests

- Message sending and receiving
- Error handling
- Network failure recovery
- High volume processing

### Network Scenario Tests

- Performance under varying network qualities
- Intermittent connectivity handling
- Extended disconnection with delayed retry
- Progressive network degradation

### UI State Test Suite

- Record sync state representation (synced, syncing, pending upload, etc.)
- Connection state visualization (connected, connecting, disconnected, etc.)
- Sync progress representation (idle, in progress, completed, failed)
- State transition testing under different network conditions
- Visual element mapping to sync and connection states

### Conflict Resolution Test Suite

The ConflictResolutionTester provides comprehensive testing for data conflict scenarios that occur during synchronization between local storage and server. Key test areas include:

- **Last-Write-Wins Resolution**: Validates timestamp-based conflict resolution where newer version takes precedence
- **Field-Level Merge**: Tests the ability to combine changes to different fields from local and server versions
- **Concurrent Edit Handling**: Verifies proper resolution when simultaneous changes occur from multiple sources
- **Manual Resolution Detection**: Confirms scenarios requiring human intervention are correctly identified
- **Multi-Device Conflict Scenarios**: Tests conflicts across complex multi-device sync patterns

Each test validates both the technical correctness of the resolution and the appropriate UI state updates, ensuring users receive accurate indications of conflict detection and resolution status. The test suite averages 2.061s execution time across all scenarios.

### Dead Letter Queue Test Suite

- Message failure handling and categorization
- Automatic retry mechanisms
- Custom recovery strategies
- Queue pattern analysis and recommendations
- Error distribution reporting
- UI state representation for message processing

### HIPAA Compliance Test Suite

- Data encryption at rest and in transit
- Role-based access control validation
- Audit logging completeness and integrity
- Data integrity during synchronization
- Unusual access pattern detection
- Data breach detection mechanisms

### Authentication Test Suite

- Token-based authentication
- Automatic token refresh
- Multi-tenant authentication
- Connection security parameters
- Certificate validation
- Invalid credential handling

### Multi-Device Synchronization Test Suite

- Basic synchronization across devices
- Concurrent edit handling
- Priority-based conflict resolution
- Offline operation with later synchronization
- Sync status tracking accuracy

### Treatment Session Sync Manager Test Suite

- Auto Sync Test
- Manual Sync Test
- Session Completion Test
- Network Disconnection Tests
- Authentication Tests

## Test Results and Reporting

The test harness generates detailed reports including:

- Test success rates by category
- Performance metrics for each operation
- Comparative analysis of online vs. offline operations
- Network state comparison
- Throughput calculations

Example performance data for network conditions:

| Network Quality | Operations/second | Avg. Latency (ms) |
|-----------------|------------------|-------------------|
| Excellent       | 31.4             | 28.4              |
| Good            | 10.4             | 104.8             |
| Fair            | 9.6              | 230.4             |
| Poor            | 9.5              | 396.8             |
| Terrible        | 9.6              | 733.4             |

## Usage Scenarios

### Basic Validation

```bash
./run_simple_test.sh
```

For quick validation of sync functionality with pass/fail reporting.

### Performance Benchmarking

```bash
./run_network_test.sh
```

For detailed timing information under various network conditions.

### Comprehensive Testing

```bash
./build_and_run.sh
```

For full test suite execution with detailed error reporting.

## Integration with iOS Project

The test harness is designed to be run independently from the iOS application, but it implements the same core interfaces and protocols. To integrate with actual iOS components:

1. Update `Package.swift` to include your main project as a dependency
2. Replace the mock implementations with real implementations from your iOS project
3. Configure the environment variables for Azure Service Bus connection

## Custom Test Development

To create new test scenarios:

1. Create a new Swift file in the appropriate Sources directory
2. Implement your test logic following the existing patterns
3. Update the run scripts to include your new test

## Sample Code

Example of a simple network test:

```swift
// Test excellent network conditions
do {
    azureServiceBus.setNetworkCondition(.excellent)
    let startTime = Date()
    
    var successCount = 0
    for patient in testPatients {
        let (success, _) = azureServiceBus.sendMessage(patient)
        if success {
            successCount += 1
        }
    }
    
    let endTime = Date()
    let duration = endTime.timeIntervalSince(startTime)
    
    if successCount == testPatients.count {
        networkTestCounter.recordSuccess(name: "Excellent Network Batch", duration: duration)
    } else {
        throw NSError(domain: "TestError", code: 101)
    }
} catch {
    networkTestCounter.recordFailure(name: "Excellent Network Batch", error: error.localizedDescription, duration: 0)
}
```

## Related Documentation

- [Azure Sync Architecture](../auris-ios/AZURE_SYNC_ARCHITECTURE.md)
- [Main Project README](../README.md)

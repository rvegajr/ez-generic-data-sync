using Ez.Generic.DataSync.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.UnitTests
{
    // Simple test model
    public class Order
    {
        public string Number { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
    
    public class GenericSyncServiceTests
    {
        private readonly Mock<ISyncService> _mockInnerSyncService;
        private readonly Mock<IRepository<SyncableEntityWrapper<Order>>> _mockRepository;
        private readonly GenericSyncService<Order> _syncService;
        
        public GenericSyncServiceTests()
        {
            _mockInnerSyncService = new Mock<ISyncService>();
            _mockRepository = new Mock<IRepository<SyncableEntityWrapper<Order>>>();
            _syncService = new GenericSyncService<Order>(
                _mockInnerSyncService.Object,
                _mockRepository.Object
            );
        }
        
        [Fact]
        public async Task PullAsync_ShouldDelegateToPullFromInnerService()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var syncResult = new SyncResult { ItemCount = 5, Status = SyncStatus.Completed };
            
            _mockInnerSyncService
                .Setup(s => s.PullAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(syncResult);
                
            // Act
            var result = await _syncService.PullAsync(cancellationToken);
            
            // Assert
            _mockInnerSyncService.Verify(s => s.PullAsync(cancellationToken), Times.Once);
            Assert.Equal(syncResult.Status, result.Status);
            Assert.Equal(syncResult.ItemCount, result.ItemCount);
        }
        
        [Fact]
        public async Task PushAsync_ShouldDelegateToPushFromInnerService()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var syncResult = new SyncResult { ItemCount = 3, Status = SyncStatus.Completed };
            
            _mockInnerSyncService
                .Setup(s => s.PushAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(syncResult);
                
            // Act
            var result = await _syncService.PushAsync(cancellationToken);
            
            // Assert
            _mockInnerSyncService.Verify(s => s.PushAsync(cancellationToken), Times.Once);
            Assert.Equal(syncResult.Status, result.Status);
            Assert.Equal(syncResult.ItemCount, result.ItemCount);
        }
        
        [Fact]
        public void Repository_ShouldReturnGenericRepository()
        {
            // Arrange
            var genRepository = new GenericRepository<Order>(_mockRepository.Object);
            
            var mockServiceWithRepo = new Mock<GenericSyncService<Order>>(
                _mockInnerSyncService.Object,
                _mockRepository.Object
            ) { CallBase = true };
            
            mockServiceWithRepo
                .Setup(s => s.GetRepository())
                .Returns(genRepository);
                
            // Act
            var repository = mockServiceWithRepo.Object.Repository;
            
            // Assert
            Assert.NotNull(repository);
            Assert.IsType<GenericRepository<Order>>(repository);
        }
        
        [Fact]
        public async Task SyncItemAsync_ShouldPushSpecificItem()
        {
            // Arrange
            var orderId = "order-123";
            var cancellationToken = new CancellationToken();
            var syncResult = new SyncResult { ItemCount = 1, Status = SyncStatus.Completed };
            
            _mockInnerSyncService
                .Setup(s => s.SyncItemAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(syncResult);
                
            // Act
            var result = await _syncService.SyncItemAsync(orderId, cancellationToken);
            
            // Assert
            _mockInnerSyncService.Verify(s => s.SyncItemAsync(orderId, cancellationToken), Times.Once);
            Assert.Equal(syncResult.Status, result.Status);
            Assert.Equal(syncResult.ItemCount, result.ItemCount);
        }
        
        [Fact]
        public void SetNetworkPolicy_ShouldConfigureNetworkPolicy()
        {
            // Arrange
            var policy = NetworkPolicy.Default with { MaxRetries = 5 };
            
            // Act
            _syncService.SetNetworkPolicy(policy);
            
            // Assert
            _mockInnerSyncService.Verify(s => s.SetNetworkPolicy(policy), Times.Once);
        }
    }
}

using Ez.Generic.DataSync.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.UnitTests
{
    // Test model class - Must be public for Moq to access it
    public class Product
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class GenericRepositoryTests
    {
        private readonly Mock<IRepository<SyncableEntityWrapper<Product>>> _mockInnerRepository;
        private readonly GenericRepository<Product> _repository;
        private readonly Product _testProduct;
        private readonly SyncableEntityWrapper<Product> _wrappedProduct;

        public GenericRepositoryTests()
        {
            _mockInnerRepository = new Mock<IRepository<SyncableEntityWrapper<Product>>>();
            _repository = new GenericRepository<Product>(_mockInnerRepository.Object);
            
            _testProduct = new Product { Name = "Test Product", Price = 99.99m };
            _wrappedProduct = new SyncableEntityWrapper<Product>(_testProduct, "test-id-123");
        }

        [Fact]
        public async Task GetItemAsync_WithValidId_ShouldReturnItem()
        {
            // Arrange
            _mockInnerRepository
                .Setup(r => r.GetItemAsync("test-id-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(_wrappedProduct);

            // Act
            var result = await _repository.GetItemAsync("test-id-123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testProduct.Name, result.Name);
            Assert.Equal(_testProduct.Price, result.Price);
        }

        [Fact]
        public async Task GetItemAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            _mockInnerRepository
                .Setup(r => r.GetItemAsync("non-existent-id", It.IsAny<CancellationToken>()))
                .ReturnsAsync((SyncableEntityWrapper<Product>)null);

            // Act
            var result = await _repository.GetItemAsync("non-existent-id");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetItemsAsync_ShouldReturnAllNonDeletedItems()
        {
            // Arrange
            var product1 = new Product { Name = "Product 1", Price = 10.99m };
            var product2 = new Product { Name = "Product 2", Price = 20.99m };
            var product3 = new Product { Name = "Product 3", Price = 30.99m }; // This one will be deleted
            
            var wrapper1 = new SyncableEntityWrapper<Product>(product1, "id-1");
            var wrapper2 = new SyncableEntityWrapper<Product>(product2, "id-2");
            var wrapper3 = new SyncableEntityWrapper<Product>(product3, "id-3");
            wrapper3.MarkAsDeleted();
            
            var items = new List<SyncableEntityWrapper<Product>> { wrapper1, wrapper2, wrapper3 };
            
            _mockInnerRepository
                .Setup(r => r.GetItemsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);

            // Act
            var results = await _repository.GetItemsAsync();

            // Assert
            Assert.Equal(2, results.Count()); // Should only return non-deleted items
            Assert.Contains(results, p => p.Name == "Product 1");
            Assert.Contains(results, p => p.Name == "Product 2");
            Assert.DoesNotContain(results, p => p.Name == "Product 3");
        }

        [Fact]
        public async Task AddItemAsync_ShouldWrapAndAddItem()
        {
            // Arrange
            var newProduct = new Product { Name = "New Product", Price = 49.99m };
            SyncableEntityWrapper<Product> capturedWrapper = null;
            string capturedId = null;
            
            _mockInnerRepository
                .Setup(r => r.AddItemAsync(It.IsAny<SyncableEntityWrapper<Product>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<SyncableEntityWrapper<Product>, string, CancellationToken>((w, id, _) => 
                {
                    capturedWrapper = w;
                    capturedId = id;
                })
                .Returns(Task.CompletedTask);

            // Act
            await _repository.AddItemAsync(newProduct);

            // Assert
            _mockInnerRepository.Verify(r => r.AddItemAsync(It.IsAny<SyncableEntityWrapper<Product>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(capturedWrapper);
            Assert.Equal(newProduct, capturedWrapper.Data);
            Assert.Equal("New Product", capturedWrapper.Data.Name);
            Assert.Equal(49.99m, capturedWrapper.Data.Price);
            Assert.False(capturedWrapper.Deleted);
        }

        [Fact]
        public async Task UpdateItemAsync_ShouldWrapAndUpdateItem()
        {
            // Arrange
            var existingId = "existing-id";
            var updatedProduct = new Product { Name = "Updated Product", Price = 79.99m };
            var existingWrapper = new SyncableEntityWrapper<Product>(
                new Product { Name = "Original Product", Price = 59.99m }, 
                existingId
            );
            
            SyncableEntityWrapper<Product> capturedWrapper = null;
            string capturedId = null;
            
            _mockInnerRepository
                .Setup(r => r.GetItemAsync(existingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWrapper);
                
            _mockInnerRepository
                .Setup(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Product>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<SyncableEntityWrapper<Product>, string, CancellationToken>((w, id, _) => 
                {
                    capturedWrapper = w;
                    capturedId = id;
                })
                .Returns(Task.CompletedTask);

            // Act
            await _repository.UpdateItemAsync(updatedProduct, existingId);

            // Assert
            _mockInnerRepository.Verify(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Product>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(capturedWrapper);
            Assert.Equal(existingId, capturedId);
            Assert.Equal("Updated Product", capturedWrapper.Data.Name);
            Assert.Equal(79.99m, capturedWrapper.Data.Price);
        }

        [Fact]
        public async Task DeleteItemAsync_ShouldMarkItemAsDeleted()
        {
            // Arrange
            var existingId = "existing-id";
            var existingProduct = new Product { Name = "Product to Delete", Price = 39.99m };
            var existingWrapper = new SyncableEntityWrapper<Product>(existingProduct, existingId);
            
            SyncableEntityWrapper<Product> capturedWrapper = null;
            string capturedId = null;
            
            _mockInnerRepository
                .Setup(r => r.GetItemAsync(existingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWrapper);
                
            _mockInnerRepository
                .Setup(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Product>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<SyncableEntityWrapper<Product>, string, CancellationToken>((w, id, _) => 
                {
                    capturedWrapper = w;
                    capturedId = id;
                })
                .Returns(Task.CompletedTask);

            // Act
            await _repository.DeleteItemAsync(existingId);

            // Assert
            _mockInnerRepository.Verify(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Product>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(capturedWrapper);
            Assert.Equal(existingId, capturedId);
            Assert.True(capturedWrapper.Deleted);
        }
    }
}

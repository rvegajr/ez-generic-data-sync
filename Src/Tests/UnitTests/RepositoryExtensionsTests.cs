using Ez.Generic.DataSync.Core;
using Ez.Generic.DataSync.Extensions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.UnitTests
{
    public class RepositoryExtensionsTests
    {
        // Test model
        public class Contact
        {
            public string Name { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }
        
        private readonly Mock<IRepository<SyncableEntityWrapper<Contact>>> _mockRepository;
        private readonly Contact _testContact;
        private readonly SyncableEntityWrapper<Contact> _wrappedContact;
        
        public RepositoryExtensionsTests()
        {
            _mockRepository = new Mock<IRepository<SyncableEntityWrapper<Contact>>>();
            _testContact = new Contact { Name = "Test Contact", Phone = "555-1234" };
            _wrappedContact = new SyncableEntityWrapper<Contact>(_testContact, "contact-123");
        }
        
        [Fact]
        public async Task GetItemAsync_ShouldReturnTypedItem()
        {
            // Arrange
            _mockRepository
                .Setup(r => r.GetItemAsync("contact-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(_wrappedContact);
                
            // Act
            var result = await _mockRepository.Object.GetItemAsync<Contact>("contact-123");
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testContact.Name, result.Name);
            Assert.Equal(_testContact.Phone, result.Phone);
        }
        
        [Fact]
        public async Task GetItemsAsync_ShouldReturnTypedItems()
        {
            // Arrange
            var contact1 = new Contact { Name = "Contact 1", Phone = "111-1111" };
            var contact2 = new Contact { Name = "Contact 2", Phone = "222-2222" };
            
            var wrapper1 = new SyncableEntityWrapper<Contact>(contact1, "id-1");
            var wrapper2 = new SyncableEntityWrapper<Contact>(contact2, "id-2");
            
            var items = new List<SyncableEntityWrapper<Contact>> { wrapper1, wrapper2 };
            
            _mockRepository
                .Setup(r => r.GetItemsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(items);
                
            // Act
            var results = await _mockRepository.Object.GetItemsAsync<Contact>();
            
            // Assert
            Assert.Equal(2, results.Count());
            Assert.Contains(results, c => c.Name == "Contact 1");
            Assert.Contains(results, c => c.Name == "Contact 2");
        }
        
        [Fact]
        public async Task AddItemAsync_ShouldAddItem()
        {
            // Arrange
            var newContact = new Contact { Name = "New Contact", Phone = "333-3333" };
            SyncableEntityWrapper<Contact> capturedWrapper = null;
            string capturedId = null;
            
            _mockRepository
                .Setup(r => r.AddItemAsync(It.IsAny<SyncableEntityWrapper<Contact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<SyncableEntityWrapper<Contact>, string, CancellationToken>((w, id, _) => 
                {
                    capturedWrapper = w;
                    capturedId = id;
                })
                .Returns(Task.CompletedTask);
                
            // Act
            await _mockRepository.Object.AddItemAsync(newContact);
            
            // Assert
            _mockRepository.Verify(r => r.AddItemAsync(It.IsAny<SyncableEntityWrapper<Contact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(capturedWrapper);
            Assert.Equal(newContact, capturedWrapper.Data);
        }
        
        [Fact]
        public async Task UpdateItemAsync_ShouldUpdateItem()
        {
            // Arrange
            var existingId = "existing-id";
            var updatedContact = new Contact { Name = "Updated Contact", Phone = "444-4444" };
            var existingWrapper = new SyncableEntityWrapper<Contact>(
                new Contact { Name = "Original Contact", Phone = "000-0000" }, 
                existingId
            );
            
            SyncableEntityWrapper<Contact> capturedWrapper = null;
            string capturedId = null;
            
            _mockRepository
                .Setup(r => r.GetItemAsync(existingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWrapper);
                
            _mockRepository
                .Setup(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Contact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<SyncableEntityWrapper<Contact>, string, CancellationToken>((w, id, _) => 
                {
                    capturedWrapper = w;
                    capturedId = id;
                })
                .Returns(Task.CompletedTask);
                
            // Act
            await _mockRepository.Object.UpdateItemAsync(updatedContact, existingId);
            
            // Assert
            _mockRepository.Verify(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Contact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(capturedWrapper);
            Assert.Equal(existingId, capturedId);
            Assert.Equal("Updated Contact", capturedWrapper.Data.Name);
        }
        
        [Fact]
        public async Task DeleteItemAsync_ShouldMarkItemAsDeleted()
        {
            // Arrange
            var existingId = "existing-id";
            var existingContact = new Contact { Name = "Contact to Delete", Phone = "999-9999" };
            var existingWrapper = new SyncableEntityWrapper<Contact>(existingContact, existingId);
            
            SyncableEntityWrapper<Contact> capturedWrapper = null;
            string capturedId = null;
            
            _mockRepository
                .Setup(r => r.GetItemAsync(existingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWrapper);
                
            _mockRepository
                .Setup(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Contact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<SyncableEntityWrapper<Contact>, string, CancellationToken>((w, id, _) => 
                {
                    capturedWrapper = w;
                    capturedId = id;
                })
                .Returns(Task.CompletedTask);
                
            // Act
            await _mockRepository.Object.DeleteItemAsync(existingId);
            
            // Assert
            _mockRepository.Verify(r => r.UpdateItemAsync(It.IsAny<SyncableEntityWrapper<Contact>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(capturedWrapper);
            Assert.Equal(existingId, capturedId);
            Assert.True(capturedWrapper.Deleted);
        }
    }
}

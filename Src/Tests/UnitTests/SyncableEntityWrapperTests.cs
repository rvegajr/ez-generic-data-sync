using CommunityToolkit.Datasync.Client;
using System;
using System.Threading.Tasks;

namespace Ez.Generic.DataSync.UnitTests
{
    public class SyncableEntityWrapperTests
    {
        // Test model class
        private class Customer
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        [Fact]
        public void Constructor_WithValidData_ShouldInitializeProperties()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Test Customer",
                Email = "test@example.com"
            };
            
            // Act
            var wrapper = new Core.SyncableEntityWrapper<Customer>(customer);
            
            // Assert
            Assert.NotNull(wrapper);
            Assert.Equal(customer, wrapper.Data);
            Assert.NotEmpty(wrapper.Id);
            Assert.False(wrapper.Deleted);
            Assert.NotEqual(default, wrapper.UpdatedAt);
        }
        
        [Fact]
        public void Constructor_WithCustomId_ShouldUseProvidedId()
        {
            // Arrange
            var customer = new Customer();
            var customId = "custom-id-123";
            
            // Act
            var wrapper = new Core.SyncableEntityWrapper<Customer>(customer, customId);
            
            // Assert
            Assert.Equal(customId, wrapper.Id);
        }
        
        [Fact]
        public async Task UpdateFrom_WithNewData_ShouldUpdateDataAndTimestamp()
        {
            // Arrange
            var originalCustomer = new Customer { Name = "Original", Email = "original@example.com" };
            var wrapper = new Core.SyncableEntityWrapper<Customer>(originalCustomer);
            var originalTimestamp = wrapper.UpdatedAt;
            
            // Wait a bit to ensure timestamp changes
            await Task.Delay(10);
            
            var newCustomer = new Customer { Name = "Updated", Email = "updated@example.com" };
            
            // Act
            wrapper.UpdateFrom(newCustomer);
            
            // Assert
            Assert.Equal("Updated", wrapper.Data.Name);
            Assert.Equal("updated@example.com", wrapper.Data.Email);
            Assert.True(wrapper.UpdatedAt > originalTimestamp);
        }
        
        [Fact]
        public async Task MarkAsDeleted_ShouldSetDeletedFlagAndUpdateTimestamp()
        {
            // Arrange
            var customer = new Customer();
            var wrapper = new Core.SyncableEntityWrapper<Customer>(customer);
            var originalTimestamp = wrapper.UpdatedAt;
            
            // Wait a bit to ensure timestamp changes
            await Task.Delay(10);
            
            // Act
            wrapper.MarkAsDeleted();
            
            // Assert
            Assert.True(wrapper.Deleted);
            Assert.True(wrapper.UpdatedAt > originalTimestamp);
        }
    }
}

using GameControllerForZwift.Gamepad.USB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Gamepad.Tests.USB
{
    public class DeviceLookupTests
    {
        // Test for constructor and LoadDeviceMap
        [Fact]
        public void LoadDeviceMap_ShouldDeserializeJsonCorrectly()
        {
            // Arrange
            var json = "{\"5678-1234\": \"Gamepad1\", \"efgh-abcd\": \"Gamepad2\"}";
            var deviceLookup = new DeviceLookup(json);

            // Act
            var deviceMap = deviceLookup.LoadDeviceMap(json);

            // Assert
            Assert.Equal(2, deviceMap.Count);
            Assert.Equal("Gamepad1", deviceMap["5678-1234"]);
            Assert.Equal("Gamepad2", deviceMap["efgh-abcd"]);
        }

        [Fact]
        public void LoadDeviceMap_ShouldReturnEmptyDictionary_WhenJsonIsNull()
        {
            // Arrange
            var json = (string)null;
            var deviceLookup = new DeviceLookup(json);

            // Act
            var deviceMap = deviceLookup.LoadDeviceMap(json);

            // Assert
            Assert.Empty(deviceMap);
        }

        [Fact]
        public void LoadDeviceMap_ShouldReturnEmptyDictionary_WhenJsonIsMalformed()
        {
            // Arrange
            var json = "{ invalid json }";
            var deviceLookup = new DeviceLookup(json);

            // Act
            var deviceMap = deviceLookup.LoadDeviceMap(json);

            // Assert
            Assert.Empty(deviceMap);
        }

        // Test for GetDeviceName using vendorId and productId
        [Fact]
        public void GetDeviceName_ShouldReturnCorrectName_WhenDeviceExists()
        {
            // Arrange
            var json = "{\"5678-1234\": \"Gamepad1\", \"efgh-abcd\": \"Gamepad2\"}";
            var deviceLookup = new DeviceLookup(json);

            // Act
            var deviceName = deviceLookup.GetDeviceName("5678", "1234");

            // Assert
            Assert.Equal("Gamepad1", deviceName);
        }

        [Fact]
        public void GetDeviceName_ShouldReturnUnknownDevice_WhenDeviceDoesNotExist()
        {
            // Arrange
            var json = "{\"5678-1234\": \"Gamepad1\"}";
            var deviceLookup = new DeviceLookup(json);

            // Act
            var deviceName = deviceLookup.GetDeviceName("abcd", "efgh");

            // Assert
            Assert.Equal("Unknown Device", deviceName);
        }

        // Test for GetDeviceName using Guid
        [Fact]
        public void GetDeviceName_ShouldReturnCorrectName_WhenGuidMatches()
        {
            // Arrange
            var json = "{\"5678-1234\": \"Gamepad1\", \"efgh-abcd\": \"Gamepad2\"}";
            var deviceLookup = new DeviceLookup(json);
            var guid = new Guid("12345678-1234-5678-1234-567812345678"); // Example GUID

            // Act
            var deviceName = deviceLookup.GetDeviceName(guid);

            // Assert
            Assert.Equal("Gamepad1", deviceName);
        }

        [Fact]
        public void GetDeviceName_ShouldReturnUnknownDevice_WhenGuidDoesNotMatch()
        {
            // Arrange
            var json = "{\"5678-1234\": \"Gamepad1\"}";
            var deviceLookup = new DeviceLookup(json);
            var guid = new Guid("abcdabcd-abcd-abcd-abcd-abcdabcdabcd"); // Example GUID

            // Act
            var deviceName = deviceLookup.GetDeviceName(guid);

            // Assert
            Assert.Equal("Unknown Device", deviceName);
        }

        // Test for GetDeviceName with Unknown VendorId/ProductId
        [Fact]
        public void GetDeviceName_ShouldReturnUnknownDevice_WhenVendorIdAndProductIdNotFound()
        {
            // Arrange
            var json = "{\"5678-1234\": \"Gamepad1\"}";
            var deviceLookup = new DeviceLookup(json);

            // Act
            var deviceName = deviceLookup.GetDeviceName("abcd", "efgh");

            // Assert
            Assert.Equal("Unknown Device", deviceName);
        }
    }
}

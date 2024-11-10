﻿using System.Text.Json;

namespace GameControllerForZwift.Gamepad.USB
{
    public class DeviceLookup : IDeviceLookup
    {
        private const string UnknownDevice = "Unknown Device";
        private Dictionary<string, string> _deviceMap;

        public DeviceLookup(string jsonContent)
        {
            _deviceMap = LoadDeviceMap(jsonContent);
        }

        public Dictionary<string, string> LoadDeviceMap(string jsonContent)
        {
            var deserializeMap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

            return deserializeMap ?? new Dictionary<string, string>();
        }

        public string GetDeviceName(string vendorId, string productId)
        {
            string key = $"{vendorId}-{productId}";
            return _deviceMap.TryGetValue(key, out var name) ? name : UnknownDevice;
        }

        public string GetDeviceName(Guid productGuid)
        {
            // Extract the Product ID and Vendor ID from the GUID
            string guidString = productGuid.ToString();

            // Product ID is the first 4 characters after the hyphen
            string productId = guidString.Substring(0, 4);

            // Vendor ID is the next 4 characters after the hyphen
            string vendorId = guidString.Substring(4, 4);

            // Create the key using Vendor ID and Product ID
            string key = $"{vendorId}-{productId}".ToUpper();

            // Look up the device name from the dictionary
            return _deviceMap.TryGetValue(key, out var name) ? name : UnknownDevice;
        }
    }
}
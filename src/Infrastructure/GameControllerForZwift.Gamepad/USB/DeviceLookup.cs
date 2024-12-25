using GameControllerForZwift.Core.FileSystem;
using System.Text.Json;

namespace GameControllerForZwift.Gamepad.USB
{
    public class DeviceLookup : IDeviceLookup
    {
        #region Fields
        private const string UnknownDevice = "Unknown Device";
        private Dictionary<string, string> _deviceMap;
        #endregion

        #region Constructors
        public DeviceLookup(string jsonContent)
        {
            _deviceMap = LoadDeviceMap(jsonContent ?? string.Empty);
        }
        public DeviceLookup(IFileService fileService, string filePath) : this(fileService.ReadFileContent(filePath)) { }
        #endregion

        #region Methods

        public Dictionary<string, string> LoadDeviceMap(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
                return new Dictionary<string, string>();
            
            try
            {
                var deserializeMap = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                return deserializeMap ?? new Dictionary<string, string>();
            }
            catch (JsonException)
            {
                // return an empty dictionary in case of malformed JSON
                return new Dictionary<string, string>();
            }
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
        #endregion
    }
}
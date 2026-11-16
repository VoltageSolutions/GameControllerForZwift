using GameControllerForZwift.Core.Controller;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Reads and writes the configuration file format (version 1). See docs/spec/saved-configurations.md.
    /// </summary>
    public static class ConfigurationSerializer
    {
        /// <summary>
        /// The newest format version this app understands (CFG-007).
        /// </summary>
        public const int CurrentFormatVersion = 1;

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        private static readonly JsonSerializerOptions InputOptions = new() { Converters = { new ControllerInputJsonConverter() } };
        private static readonly JsonSerializerOptions FunctionOptions = new() { Converters = { new ZwiftFunctionJsonConverter() } };

        /// <summary>
        /// Writes a configuration as JSON. Every input is written, in enum order.
        /// </summary>
        public static string Serialize(SavedConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var file = new ConfigurationFile
            {
                FormatVersion = CurrentFormatVersion,
                Id = configuration.Id,
                Name = configuration.Name,
                Controller = new ControllerFile
                {
                    VendorId = configuration.Controller.VendorIdHex,
                    ProductId = configuration.Controller.ProductIdHex,
                    Name = configuration.Controller.Name,
                },
                Mappings = ConfigurationMappings.Normalize(configuration.Mappings)
                    .OrderBy(kv => kv.Key)
                    .Select(kv => (MappingFile?)new MappingFile
                    {
                        Input = JsonSerializer.SerializeToElement(kv.Key, InputOptions),
                        Function = JsonSerializer.SerializeToElement(kv.Value, FunctionOptions),
                    })
                    .ToList(),
                Modified = configuration.Modified.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            };
            return JsonSerializer.Serialize(file, WriteOptions);
        }

        /// <summary>
        /// Reads a configuration from JSON. Does not depend on the file name (CFG-003). Unknown functions load as None
        /// (CFG-009) and missing inputs load as None (CFG-010); entries with an unknown input are ignored.
        /// </summary>
        /// <exception cref="ConfigurationFormatException">The JSON is not a valid configuration, or its format is newer than this app understands.</exception>
        public static SavedConfiguration Deserialize(string json, string? filePath = null)
        {
            ConfigurationFile? file;
            try
            {
                file = JsonSerializer.Deserialize<ConfigurationFile>(json ?? string.Empty, ReadOptions);
            }
            catch (JsonException ex)
            {
                throw new ConfigurationFormatException("The file is not a valid configuration (it could not be read as JSON).", ex);
            }

            if (file is null)
                throw new ConfigurationFormatException("The file is not a valid configuration (it is empty).");
            if (file.FormatVersion is not int version || version < 1)
                throw new ConfigurationFormatException("The file is not a valid configuration (formatVersion is missing).");
            if (version > CurrentFormatVersion)
            {
                throw new ConfigurationFormatException(
                    string.Create(CultureInfo.InvariantCulture,
                        $"This configuration was made by a newer version of Game Controller for Zwift (format version {version}). Update the app to use it."),
                    isNewerFormat: true);
            }
            if (file.Id is not Guid id || id == Guid.Empty)
                throw new ConfigurationFormatException("The file is not a valid configuration (id is missing).");
            var name = ConfigurationNames.Normalize(file.Name);
            if (name.Length == 0)
                throw new ConfigurationFormatException("The file is not a valid configuration (name is missing).");
            if (file.Controller is null)
                throw new ConfigurationFormatException("The file is not a valid configuration (controller is missing).");

            // An unreadable ID is treated like an unknown one, so matching falls back to the controller name (CFG-014).
            var vendorId = ControllerModel.TryParseId(file.Controller.VendorId, out var vid) ? vid : (ushort)0;
            var productId = ControllerModel.TryParseId(file.Controller.ProductId, out var pid) ? pid : (ushort)0;
            var controller = new ControllerModel(vendorId, productId, file.Controller.Name?.Trim() ?? string.Empty);

            var mappings = new List<KeyValuePair<ControllerInput, ZwiftFunction>>();
            foreach (var mapping in file.Mappings ?? [])
            {
                if (mapping is null || !TryReadInput(mapping.Input, out var input))
                    continue;
                mappings.Add(new(input, ReadFunction(mapping.Function)));
            }

            var modified = DateTimeOffset.TryParse(file.Modified, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTimeOffset.MinValue;

            return new SavedConfiguration(id, name, controller, ConfigurationMappings.Normalize(mappings), modified, filePath);
        }

        private static bool TryReadInput(JsonElement? element, out ControllerInput input)
        {
            input = default;
            if (element is not JsonElement value)
                return false;
            try
            {
                input = value.Deserialize<ControllerInput>(InputOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static ZwiftFunction ReadFunction(JsonElement? element)
        {
            if (element is not JsonElement value)
                return ZwiftFunction.None;
            try
            {
                return value.Deserialize<ZwiftFunction>(FunctionOptions);
            }
            catch (JsonException)
            {
                return ZwiftFunction.None;
            }
        }

        private sealed class ConfigurationFile
        {
            public int? FormatVersion { get; set; }
            public Guid? Id { get; set; }
            public string? Name { get; set; }
            public ControllerFile? Controller { get; set; }
            public List<MappingFile?>? Mappings { get; set; }
            public string? Modified { get; set; }
        }

        private sealed class ControllerFile
        {
            public string? VendorId { get; set; }
            public string? ProductId { get; set; }
            public string? Name { get; set; }
        }

        private sealed class MappingFile
        {
            [JsonPropertyName("input")]
            public JsonElement? Input { get; set; }

            [JsonPropertyName("function")]
            public JsonElement? Function { get; set; }
        }
    }
}

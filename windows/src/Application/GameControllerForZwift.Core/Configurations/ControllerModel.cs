using GameControllerForZwift.Core.Controller;
using System.Globalization;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// A controller model: the USB vendor and product ID that SDL reports, plus the controller name (CFG-011).
    /// It is not tied to one physical unit, so every controller of the same model shares its configurations.
    /// </summary>
    /// <param name="VendorId">The USB vendor ID, or 0 if SDL does not know it.</param>
    /// <param name="ProductId">The USB product ID, or 0 if SDL does not know it.</param>
    /// <param name="Name">The controller name, used for matching when the IDs are unknown (CFG-014).</param>
    public sealed record ControllerModel(ushort VendorId, ushort ProductId, string Name)
    {
        /// <summary>
        /// Gets a value indicating whether SDL reported both a vendor and a product ID.
        /// </summary>
        public bool HasUsbIds => VendorId != 0 && ProductId != 0;

        /// <summary>
        /// Gets the vendor ID as four uppercase hex digits, as stored in configuration files.
        /// </summary>
        public string VendorIdHex => FormatId(VendorId);

        /// <summary>
        /// Gets the product ID as four uppercase hex digits, as stored in configuration files.
        /// </summary>
        public string ProductIdHex => FormatId(ProductId);

        /// <summary>
        /// Gets a name to show to the user.
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"controller {VendorIdHex}:{ProductIdHex}" : Name.Trim();

        /// <summary>
        /// Creates the model of a connected controller.
        /// </summary>
        public static ControllerModel From(IController controller)
        {
            ArgumentNullException.ThrowIfNull(controller);
            return new ControllerModel(controller.VendorId, controller.ProductId, controller.Name ?? string.Empty);
        }

        /// <summary>
        /// Formats an ID as four uppercase hex digits.
        /// </summary>
        public static string FormatId(ushort id) => id.ToString("X4", CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses an ID written as hex digits, with or without a 0x prefix.
        /// </summary>
        public static bool TryParseId(string? text, out ushort id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var span = text.AsSpan().Trim();
            if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                span = span[2..];
            return span.Length is > 0 and <= 4
                && ushort.TryParse(span, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out id);
        }
    }

    /// <summary>
    /// A USB vendor and product ID pair.
    /// </summary>
    public readonly record struct UsbId(ushort VendorId, ushort ProductId);
}

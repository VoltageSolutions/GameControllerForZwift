using System.Globalization;
using System.Text;

namespace GameControllerForZwift.Gamepad.SDL2
{
    /// <summary>
    /// A snapshot of what SDL reports about one attached joystick device, taken before it is opened.
    /// </summary>
    /// <param name="Index">The SDL device index.</param>
    /// <param name="Name">The device name, or null if SDL has none.</param>
    /// <param name="Guid">The raw SDL GUID bytes, or empty if unavailable.</param>
    /// <param name="Vendor">The USB vendor id.</param>
    /// <param name="Product">The USB product id.</param>
    /// <param name="JoystickType">The SDL joystick type, as text.</param>
    /// <param name="IsGameController">Whether SDL has a game controller mapping for the device.</param>
    internal sealed record SdlDeviceInfo(
        int Index,
        string? Name,
        byte[] Guid,
        ushort Vendor,
        ushort Product,
        string JoystickType,
        bool IsGameController);

    /// <summary>
    /// Pure helpers that decide whether a device is listed and describe it for the log.
    /// </summary>
    internal static class SdlDeviceDiagnostics
    {
        /// <summary>
        /// Decides whether the device should be opened as a game controller.
        /// </summary>
        /// <param name="info">The device snapshot.</param>
        /// <param name="skipReason">The reason the device is skipped, or null if it is included.</param>
        /// <returns><c>true</c> if the device should be opened.</returns>
        public static bool ShouldInclude(SdlDeviceInfo info, out string? skipReason)
        {
            ArgumentNullException.ThrowIfNull(info);

            if (!info.IsGameController)
            {
                skipReason = "SDL has no game controller mapping for this device (plain joystick)";
                return false;
            }

            skipReason = null;
            return true;
        }

        /// <summary>
        /// Formats a one-line description of the device for diagnostics.
        /// </summary>
        /// <param name="info">The device snapshot.</param>
        /// <param name="skipReason">The skip reason, or null if the device is included.</param>
        /// <returns>The diagnostic line.</returns>
        public static string Format(SdlDeviceInfo info, string? skipReason)
        {
            ArgumentNullException.ThrowIfNull(info);

            string name = info.Name ?? "(none)";
            string status = skipReason is null ? "included" : $"skipped ({skipReason})";

            return FormattableString.Invariant(
                $"Device {info.Index}: name='{name}', guid={FormatGuid(info.Guid)}, vid={info.Vendor:X4}, pid={info.Product:X4}, type={info.JoystickType}, isGameController={info.IsGameController}, {status}");
        }

        /// <summary>
        /// Formats raw SDL GUID bytes as lowercase hex, matching SDL's own GUID string.
        /// </summary>
        /// <param name="guid">The GUID bytes.</param>
        /// <returns>The hex string, or "(none)" if empty.</returns>
        public static string FormatGuid(byte[]? guid)
        {
            if (guid is null || guid.Length == 0)
            {
                return "(none)";
            }

            var sb = new StringBuilder(guid.Length * 2);
            foreach (byte b in guid)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }
    }
}

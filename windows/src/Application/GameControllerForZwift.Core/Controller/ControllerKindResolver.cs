namespace GameControllerForZwift.Core.Controller
{
    /// <summary>Picks the <see cref="ControllerKind"/> for a controller from what SDL reports (CTRL-012 to CTRL-015).</summary>
    public static class ControllerKindResolver
    {
        private const int SonyVendorId = 0x054C;
        private const int NintendoVendorId = 0x057E;
        private const int MicrosoftVendorId = 0x045E;

        /// <summary>
        /// The name rules, tried in order, so put the most specific first. A rule matches when the SDL controller name contains
        /// <see cref="NameRule.Fragment"/>, ignoring case, and, if <see cref="NameRule.RequiredVendorId"/> is set, the vendor matches.
        /// To support a new name, add a row here.
        /// </summary>
        private static readonly NameRule[] NameRules =
        [
            new("(L/R)", ControllerKind.JoyConPair),
            new("Joy-Con (L)", ControllerKind.JoyConLeft),
            new("Joy-Con L", ControllerKind.JoyConLeft),
            new("Joy-Con (R)", ControllerKind.JoyConRight),
            new("Joy-Con R", ControllerKind.JoyConRight),
            new("Pro Controller", ControllerKind.SwitchPro),
            new("DualSense", ControllerKind.PlayStationDualSense),
            new("PS5", ControllerKind.PlayStationDualSense),
            new("DualShock 4", ControllerKind.PlayStationDualShock4),
            new("PS4", ControllerKind.PlayStationDualShock4),
            new("Wireless Controller", ControllerKind.PlayStationDualShock4, SonyVendorId),
            new("Xbox", ControllerKind.Xbox),
            new("X-Box", ControllerKind.Xbox),
            new("XInput", ControllerKind.Xbox),
        ];

        /// <summary>The SDL controller type names (the part after <c>SDL_CONTROLLER_TYPE_</c>), compared ignoring case.</summary>
        private static readonly Dictionary<string, ControllerKind> TypeKinds = new(StringComparer.OrdinalIgnoreCase)
        {
            ["PS4"] = ControllerKind.PlayStationDualShock4,
            ["PS5"] = ControllerKind.PlayStationDualSense,
            ["XBOXONE"] = ControllerKind.Xbox,
            ["XBOX360"] = ControllerKind.Xbox,
            ["NINTENDO_SWITCH_PRO"] = ControllerKind.SwitchPro,
            ["NINTENDO_SWITCH_JOYCON_LEFT"] = ControllerKind.JoyConLeft,
            ["NINTENDO_SWITCH_JOYCON_RIGHT"] = ControllerKind.JoyConRight,
            ["NINTENDO_SWITCH_JOYCON_PAIR"] = ControllerKind.JoyConPair,
        };

        private const string TypePrefix = "SDL_CONTROLLER_TYPE_";

        /// <summary>
        /// Resolves the kind from the SDL name, then the SDL controller type, then the vendor and product IDs.
        /// The first step that gives a kind wins; otherwise the result is <see cref="ControllerKind.Generic"/>.
        /// </summary>
        /// <param name="sdlName">The SDL controller name, if any.</param>
        /// <param name="sdlControllerType">The SDL controller type, such as <c>PS4</c> or <c>NINTENDO_SWITCH_PRO</c>, if any.</param>
        /// <param name="vendorId">The USB vendor ID, if known.</param>
        /// <param name="productId">The USB product ID, if known.</param>
        public static ControllerKind Resolve(string? sdlName, string? sdlControllerType, int? vendorId, int? productId)
        {
            return FromName(sdlName, vendorId)
                ?? FromType(sdlControllerType)
                ?? FromIds(vendorId, productId)
                ?? ControllerKind.Generic;
        }

        private static ControllerKind? FromName(string? name, int? vendorId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            foreach (NameRule rule in NameRules)
            {
                if (rule.RequiredVendorId is { } required && required != vendorId)
                {
                    continue;
                }

                if (name.Contains(rule.Fragment, StringComparison.OrdinalIgnoreCase))
                {
                    return rule.Kind;
                }
            }

            return null;
        }

        private static ControllerKind? FromType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return null;
            }

            string key = type.Trim();
            if (key.StartsWith(TypePrefix, StringComparison.OrdinalIgnoreCase))
            {
                key = key[TypePrefix.Length..];
            }

            return TypeKinds.TryGetValue(key, out ControllerKind kind) ? kind : null;
        }

        private static ControllerKind? FromIds(int? vendorId, int? productId)
        {
            return (vendorId, productId) switch
            {
                (SonyVendorId, 0x05C4 or 0x09CC) => ControllerKind.PlayStationDualShock4,
                (SonyVendorId, 0x0CE6 or 0x0DF2) => ControllerKind.PlayStationDualSense,
                (NintendoVendorId, 0x2009) => ControllerKind.SwitchPro,
                (NintendoVendorId, 0x2006) => ControllerKind.JoyConLeft,
                (NintendoVendorId, 0x2007) => ControllerKind.JoyConRight,
                (NintendoVendorId, 0x2008) => ControllerKind.JoyConPair,
                (MicrosoftVendorId, _) => ControllerKind.Xbox,
                _ => null,
            };
        }

        private readonly record struct NameRule(string Fragment, ControllerKind Kind, int? RequiredVendorId = null);
    }
}

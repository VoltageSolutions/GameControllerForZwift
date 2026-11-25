using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.Core.Tests.Controller
{
    /// <summary>
    /// Working out the controller kind from the SDL name, type and IDs (CTRL-011 to CTRL-015).
    /// </summary>
    public class ControllerKindResolverTests
    {
        [Theory]
        [InlineData("Xbox Series X Controller", ControllerKind.Xbox)]
        [InlineData("Xbox 360 Controller", ControllerKind.Xbox)]
        [InlineData("X-Box One pad", ControllerKind.Xbox)]
        [InlineData("XInput Controller", ControllerKind.Xbox)]
        [InlineData("PS4 Controller", ControllerKind.PlayStationDualShock4)]
        [InlineData("DualShock 4 Wireless Controller", ControllerKind.PlayStationDualShock4)]
        [InlineData("PS5 Controller", ControllerKind.PlayStationDualSense)]
        [InlineData("DualSense Wireless Controller", ControllerKind.PlayStationDualSense)]
        [InlineData("Nintendo Switch Pro Controller", ControllerKind.SwitchPro)]
        [InlineData("Nintendo Switch Joy-Con (L)", ControllerKind.JoyConLeft)]
        [InlineData("Nintendo Switch Joy-Con (R)", ControllerKind.JoyConRight)]
        [InlineData("Nintendo Switch Joy-Con (L/R)", ControllerKind.JoyConPair)]
        public void NameIdentifiesKind(string name, ControllerKind expected)
        {
            // CTRL-011, CTRL-012
            Assert.Equal(expected, ControllerKindResolver.Resolve(name, null, null, null));
        }

        [Theory]
        [InlineData("xbox series x controller")]
        [InlineData("XBOX SERIES X CONTROLLER")]
        public void NameMatchingIgnoresCase(string name)
        {
            // CTRL-014
            Assert.Equal(ControllerKind.Xbox, ControllerKindResolver.Resolve(name, null, null, null));
        }

        [Fact]
        public void ThirdPartyControllerNamedXbox_IsXbox()
        {
            // CTRL-015
            Assert.Equal(ControllerKind.Xbox, ControllerKindResolver.Resolve("Xbox 360 Controller for Windows (3rd party)", null, 0x0E6F, 0x0213));
        }

        [Fact]
        public void WirelessControllerWithSonyVendor_IsDualShock4()
        {
            // CTRL-012
            Assert.Equal(ControllerKind.PlayStationDualShock4, ControllerKindResolver.Resolve("Wireless Controller", null, 0x054C, 0x05C4));
        }

        [Fact]
        public void WirelessControllerWithOtherVendor_IsNotDualShock4()
        {
            // CTRL-012, CTRL-013
            Assert.Equal(ControllerKind.Generic, ControllerKindResolver.Resolve("Wireless Controller", null, 0x1234, 0x0001));
        }

        [Fact]
        public void MoreSpecificNameRuleWins()
        {
            // CTRL-014
            Assert.Equal(ControllerKind.JoyConPair, ControllerKindResolver.Resolve("Joy-Con (L/R)", null, null, null));
        }

        [Theory]
        [InlineData("PS4", ControllerKind.PlayStationDualShock4)]
        [InlineData("PS5", ControllerKind.PlayStationDualSense)]
        [InlineData("XBOXONE", ControllerKind.Xbox)]
        [InlineData("XBOX360", ControllerKind.Xbox)]
        [InlineData("NINTENDO_SWITCH_PRO", ControllerKind.SwitchPro)]
        [InlineData("NINTENDO_SWITCH_JOYCON_LEFT", ControllerKind.JoyConLeft)]
        [InlineData("NINTENDO_SWITCH_JOYCON_RIGHT", ControllerKind.JoyConRight)]
        [InlineData("NINTENDO_SWITCH_JOYCON_PAIR", ControllerKind.JoyConPair)]
        [InlineData("nintendo_switch_pro", ControllerKind.SwitchPro)]
        [InlineData("SDL_CONTROLLER_TYPE_PS5", ControllerKind.PlayStationDualSense)]
        public void TypeIdentifiesKindWhenNameDoesNot(string type, ControllerKind expected)
        {
            // CTRL-012
            Assert.Equal(expected, ControllerKindResolver.Resolve("Some Gamepad", type, null, null));
        }

        [Fact]
        public void NameWinsOverTypeAndIds()
        {
            // CTRL-012
            Assert.Equal(ControllerKind.Xbox, ControllerKindResolver.Resolve("Xbox Controller", "PS5", 0x054C, 0x0CE6));
        }

        [Fact]
        public void TypeWinsOverIds()
        {
            // CTRL-012
            Assert.Equal(ControllerKind.SwitchPro, ControllerKindResolver.Resolve("Gamepad", "NINTENDO_SWITCH_PRO", 0x054C, 0x0CE6));
        }

        [Theory]
        [InlineData(0x054C, 0x05C4, ControllerKind.PlayStationDualShock4)]
        [InlineData(0x054C, 0x09CC, ControllerKind.PlayStationDualShock4)]
        [InlineData(0x054C, 0x0CE6, ControllerKind.PlayStationDualSense)]
        [InlineData(0x054C, 0x0DF2, ControllerKind.PlayStationDualSense)]
        [InlineData(0x057E, 0x2009, ControllerKind.SwitchPro)]
        [InlineData(0x057E, 0x2006, ControllerKind.JoyConLeft)]
        [InlineData(0x057E, 0x2007, ControllerKind.JoyConRight)]
        [InlineData(0x057E, 0x2008, ControllerKind.JoyConPair)]
        [InlineData(0x045E, 0x028E, ControllerKind.Xbox)]
        [InlineData(0x045E, 0x0B12, ControllerKind.Xbox)]
        public void VendorAndProductIdentifyKindWhenNameAndTypeDoNot(int vendor, int product, ControllerKind expected)
        {
            // CTRL-012
            Assert.Equal(expected, ControllerKindResolver.Resolve("Gamepad", "UNKNOWN", vendor, product));
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "", 0, 0)]
        [InlineData("Generic USB Joystick", "UNKNOWN", 0x1234, 0x5678)]
        [InlineData("Generic USB Joystick", null, 0x054C, 0x9999)]
        public void UnknownController_IsGeneric(string? name, string? type, int? vendor, int? product)
        {
            // CTRL-013
            Assert.Equal(ControllerKind.Generic, ControllerKindResolver.Resolve(name, type, vendor, product));
        }
    }
}

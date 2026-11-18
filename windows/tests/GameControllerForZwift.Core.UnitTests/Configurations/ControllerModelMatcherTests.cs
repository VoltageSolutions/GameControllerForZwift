using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;
using NSubstitute;

namespace GameControllerForZwift.Core.Tests.Configurations
{
    public class ControllerModelMatcherTests
    {
        [Fact]
        public void CFG011_CFG012_SameVidPidMatchesRegardlessOfUnitOrName()
        {
            var matcher = new ControllerModelMatcher();
            var unitOne = new ControllerModel(0x045E, 0x0B13, "Xbox Series X Controller");
            var unitTwo = new ControllerModel(0x045E, 0x0B13, "Xbox Wireless Controller");

            Assert.True(matcher.Matches(unitOne, unitTwo));
            Assert.Equal(matcher.GetKey(unitOne), matcher.GetKey(unitTwo));
        }

        [Fact]
        public void CFG011_DifferentPidDoesNotMatch()
        {
            var matcher = new ControllerModelMatcher();

            Assert.False(matcher.Matches(new ControllerModel(0x045E, 0x0B13, "Pad"), new ControllerModel(0x045E, 0x0B12, "Pad")));
        }

        [Fact]
        public void CFG013_AliasesMatchEachOther()
        {
            var matcher = new ControllerModelMatcher([[new UsbId(0x057E, 0x2009), new UsbId(0x057E, 0x2017)]]);
            var usb = new ControllerModel(0x057E, 0x2017, "Pro Controller");
            var bluetooth = new ControllerModel(0x057E, 0x2009, "Pro Controller");

            Assert.True(matcher.Matches(usb, bluetooth));
            Assert.Equal("057E:2009", matcher.GetKey(usb));
            Assert.False(new ControllerModelMatcher().Matches(usb, bluetooth));
        }

        [Fact]
        public void CFG013_KnownAliasesStartEmpty()
        {
            Assert.Empty(ControllerModelMatcher.KnownAliases);
        }

        [Fact]
        public void CFG014_WithoutIdsFallsBackToName()
        {
            var matcher = new ControllerModelMatcher();
            var noIds = new ControllerModel(0, 0, "Generic Pad");

            Assert.True(matcher.Matches(noIds, new ControllerModel(0, 0, " generic pad ")));
            Assert.False(matcher.Matches(noIds, new ControllerModel(0, 0, "Other Pad")));
            Assert.Equal("NAME:GENERIC PAD", matcher.GetKey(noIds));
        }

        [Fact]
        public void CFG014_OneSideWithoutIdsComparesByName()
        {
            var matcher = new ControllerModelMatcher();

            Assert.True(matcher.Matches(new ControllerModel(0x1234, 0, "Generic Pad"), new ControllerModel(0, 0, "Generic Pad")));
            Assert.False(matcher.Matches(new ControllerModel(0x045E, 0x0B13, "Generic Pad"), new ControllerModel(0, 0, "Generic Pad")));
        }

        [Fact]
        public void From_ReadsIdsAndNameFromController()
        {
            var controller = Substitute.For<IController>();
            controller.Name.Returns("Pad");
            controller.VendorId.Returns((ushort)0x045E);
            controller.ProductId.Returns((ushort)0x0B13);

            Assert.Equal(new ControllerModel(0x045E, 0x0B13, "Pad"), ControllerModel.From(controller));
        }

        [Theory]
        [InlineData("045E", true, 0x045E)]
        [InlineData("0x045e", true, 0x045E)]
        [InlineData("b13", true, 0x0B13)]
        [InlineData("12345", false, 0)]
        [InlineData("xyz", false, 0)]
        [InlineData("", false, 0)]
        [InlineData(null, false, 0)]
        public void TryParseId_ParsesHex(string? text, bool expected, int value)
        {
            Assert.Equal(expected, ControllerModel.TryParseId(text, out var id));
            Assert.Equal(value, id);
        }
    }
}

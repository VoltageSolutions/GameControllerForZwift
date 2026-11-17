using GameControllerForZwift.Core.Controller;
using NSubstitute;

namespace GameControllerForZwift.Core.Tests.Controller
{
    /// <summary>
    /// Comparing the listed controllers with the connected ones (CTRL-001, CTRL-003, CTRL-009).
    /// </summary>
    public class ControllerListChangeTests
    {
        private static IController Controller(ushort vendorId, ushort productId, string name)
        {
            var controller = Substitute.For<IController>();
            controller.VendorId.Returns(vendorId);
            controller.ProductId.Returns(productId);
            controller.Name.Returns(name);
            return controller;
        }

        [Fact]
        public void Between_SameVendorDifferentProducts_ListsBoth()
        {
            // An 8BitDo SN30 Pro+ in X-input mode reports as an Xbox One S controller (045E:02E0) next to an Xbox Series X (045E:0B13).
            var seriesX = Controller(0x045E, 0x0B13, "Xbox Series X Controller");
            var oneS = Controller(0x045E, 0x02E0, "Xbox One S Controller");

            var change = ControllerListChange.Between([], [seriesX, oneS]);

            Assert.Equal([seriesX, oneS], change.Added);
            Assert.Equal([seriesX, oneS], change.Result);
            Assert.Empty(change.Removed);
        }

        [Fact]
        public void Between_TwoControllersOfTheSameModel_ListsBoth()
        {
            var first = Controller(0x045E, 0x0B13, "Xbox Series X Controller");
            var second = Controller(0x045E, 0x0B13, "Xbox Series X Controller");

            var change = ControllerListChange.Between([first], [first, second]);

            Assert.Equal([second], change.Added);
            Assert.Equal([first, second], change.Result);
        }

        [Fact]
        public void Between_SameInstances_HasNoChanges()
        {
            var a = Controller(1, 1, "A");
            var b = Controller(1, 2, "B");

            var change = ControllerListChange.Between([a, b], [b, a]);

            Assert.False(change.HasChanges);
            Assert.Equal([a, b], change.Result);
        }

        [Fact]
        public void Between_DisconnectedAndConnected_KeepsOrderAndAppendsNewOnes()
        {
            var a = Controller(1, 1, "A");
            var b = Controller(1, 2, "B");
            var c = Controller(1, 3, "C");
            var d = Controller(1, 4, "D");

            var change = ControllerListChange.Between([a, b, c], [d, c, a]);

            Assert.True(change.HasChanges);
            Assert.Equal([b], change.Removed);
            Assert.Equal([d], change.Added);
            Assert.Equal([a, c, d], change.Result);
        }

        [Fact]
        public void Between_IgnoresNullsAndDuplicateInstances()
        {
            var a = Controller(1, 1, "A");

            var change = ControllerListChange.Between([], [a, null!, a]);

            Assert.Equal([a], change.Added);
        }

        [Fact]
        public void Between_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => ControllerListChange.Between(null!, []));
            Assert.Throws<ArgumentNullException>(() => ControllerListChange.Between([], null!));
        }

        [Fact]
        public void Contains_MatchesTheInstanceOnly()
        {
            var a = Controller(1, 1, "A");
            var sameModel = Controller(1, 1, "A");

            Assert.True(ControllerListChange.Contains([a], a));
            Assert.False(ControllerListChange.Contains([a], sameModel));
            Assert.False(ControllerListChange.Contains([a], null));
            Assert.Throws<ArgumentNullException>(() => ControllerListChange.Contains(null!, a));
        }
    }
}

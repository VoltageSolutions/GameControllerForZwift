using GameControllerForZwift.Core.Controller;
using NSubstitute;

namespace GameControllerForZwift.Core.Tests.Controller
{
    /// <summary>
    /// Selection after controllers connect or disconnect (CTRL-004 to CTRL-007).
    /// </summary>
    public class ControllerSelectionTests
    {
        private readonly IController _a = Substitute.For<IController>();
        private readonly IController _b = Substitute.For<IController>();
        private readonly IController _c = Substitute.For<IController>();

        [Fact]
        public void OtherControllerConnects_SelectionStays()
        {
            // CTRL-004, CTRL-007
            var change = ControllerListChange.Between([_a], [_a, _b]);

            Assert.Same(_a, ControllerSelection.Choose(_a, change));
        }

        [Fact]
        public void OtherControllerDisconnects_SelectionStays()
        {
            // CTRL-004
            var change = ControllerListChange.Between([_a, _b], [_a]);

            Assert.Same(_a, ControllerSelection.Choose(_a, change));
        }

        [Fact]
        public void SelectedDisconnects_FirstRemainingControllerIsSelected()
        {
            // CTRL-005
            var change = ControllerListChange.Between([_a, _b, _c], [_c, _b]);

            Assert.Same(_b, ControllerSelection.Choose(_a, change));
        }

        [Fact]
        public void SelectedDisconnects_AndAnotherConnects_FirstInTheNewListIsSelected()
        {
            // CTRL-005, CTRL-009: the controllers that stay come first, new ones are appended.
            var change = ControllerListChange.Between([_a, _b], [_b, _c]);

            Assert.Same(_b, ControllerSelection.Choose(_a, change));
        }

        [Fact]
        public void SelectedDisconnects_AndNoneRemain_NothingIsSelected()
        {
            // CTRL-006
            var change = ControllerListChange.Between([_a], []);

            Assert.Null(ControllerSelection.Choose(_a, change));
        }

        [Fact]
        public void NothingSelected_ControllerConnects_ItIsSelected()
        {
            // CTRL-007
            var change = ControllerListChange.Between([_a], [_a, _b]);

            Assert.Same(_b, ControllerSelection.Choose(null, change));
        }

        [Fact]
        public void NothingSelected_ControllerDisconnects_NothingIsSelected()
        {
            var change = ControllerListChange.Between([_a, _b], [_a]);

            Assert.Null(ControllerSelection.Choose(null, change));
        }

        [Fact]
        public void NullChange_Throws()
            => Assert.Throws<ArgumentNullException>(() => ControllerSelection.Choose(_a, null!));
    }
}

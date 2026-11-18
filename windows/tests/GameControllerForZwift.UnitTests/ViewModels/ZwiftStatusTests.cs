using GameControllerForZwift.Core;
using GameControllerForZwift.UI.WPF.ViewModels;
using NSubstitute;
using System.Windows.Threading;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    public class ZwiftStatusTests
    {
        private readonly IZwiftConnection _connection = Substitute.For<IZwiftConnection>();

        private void Raise(bool connected) =>
            _connection.ConnectionChanged += NSubstitute.Raise.Event<EventHandler<bool>>(_connection, connected);

        [Fact]
        public void SCRCONN004_StartsFromTheConnectionAndSaysItInWords()
        {
            _connection.IsZwiftConnected.Returns(true);

            using var status = new ZwiftStatus(_connection);

            Assert.True(status.IsConnected);
            Assert.Equal("Zwift can see your controller", status.StatusText);
        }

        [Fact]
        public void SCRCONN004_WithoutAConnection_ZwiftIsNotConnected()
        {
            using var status = new ZwiftStatus();

            Assert.False(status.IsConnected);
            Assert.Equal("Zwift isn't connected yet", status.StatusText);
        }

        [Fact]
        public void SCRCONN005_FollowsConnectionChanges()
        {
            using var status = new ZwiftStatus(_connection);
            var changed = new List<string?>();
            status.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            Raise(true);

            Assert.True(status.IsConnected);
            Assert.Contains(nameof(ZwiftStatus.StatusText), changed);

            Raise(false);
            Assert.False(status.IsConnected);
        }

        [Fact]
        public void ChangesFromAnotherThreadArePostedToTheThreadThatCreatedIt()
        {
            using var status = new ZwiftStatus(_connection);

            var network = new Thread(() => Raise(true));
            network.Start();
            network.Join();
            Assert.False(status.IsConnected);

            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
            Dispatcher.PushFrame(frame);
            Assert.True(status.IsConnected);
        }

        [Fact]
        public void Dispose_StopsFollowing()
        {
            var status = new ZwiftStatus(_connection);
            status.Dispose();

            Raise(true);

            Assert.False(status.IsConnected);
            new ZwiftStatus().Dispose();
        }
    }
}

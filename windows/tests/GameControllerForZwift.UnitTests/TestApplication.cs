using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace GameControllerForZwift.UnitTests
{
    /// <summary>
    /// Tests that create WPF windows share one <see cref="Application"/> and run one at a time, because an
    /// <see cref="Application"/> exists once per process and its resources belong to the thread that created it.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class WpfWindows
    {
        public const string Name = "WPF windows";
    }

    internal static class TestApplication
    {
        private static readonly Lazy<Dispatcher> _dispatcher = new(StartApplicationThread);

        /// <summary>Runs the action on the thread that owns the shared application, and rethrows what it throws.</summary>
        public static void Run(Action action)
        {
            Exception? failure = null;
            _dispatcher.Value.Invoke(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            if (failure is not null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static Dispatcher StartApplicationThread()
        {
            using var started = new ManualResetEventSlim();
            Dispatcher? dispatcher = null;
            var thread = new Thread(() =>
            {
                var app = new App();
                app.InitializeComponent();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                dispatcher = Dispatcher.CurrentDispatcher;
                started.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "Test application thread",
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            started.Wait();
            return dispatcher!;
        }
    }
}

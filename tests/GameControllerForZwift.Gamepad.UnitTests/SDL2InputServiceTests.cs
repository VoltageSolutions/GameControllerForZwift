using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Gamepad.SDL2;
using Microsoft.Extensions.Logging;

namespace GameControllerForZwift.Gamepad.UnitTests;

/// <summary>
/// Exercises the real <see cref="SDL2InputService"/> against the SDL2 native library. SDL state is process-global,
/// so these tests must not run in parallel with each other. They make no assumption about attached controllers.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SdlGlobalStateDefinition
{
    public const string Name = "SDL global state";
}

[Collection(SdlGlobalStateDefinition.Name)]
public class SDL2InputServiceTests
{
    [Fact]
    public void Constructor_NullLogger_Throws()
        => Assert.Throws<ArgumentNullException>(() => new SDL2InputService(null!));

    [Fact]
    public void Constructor_InitializesSdlAndLogsLinkedVersion()
    {
        var logger = new CapturingLogger();

        using var service = new SDL2InputService(logger);

        Assert.Contains(logger.Messages, m => m.Contains("SDL2 initialized successfully", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, m => m.Contains("SDL2 linked version 2.", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, m => m.Contains("compiled against", StringComparison.Ordinal));
    }

    [Fact]
    public void GetControllers_WithoutAssumingDevices_ReturnsCollectionAndLogsEnumeration()
    {
        var logger = new CapturingLogger();
        using var service = new SDL2InputService(logger);

        IEnumerable<IController> controllers = service.GetControllers();

        Assert.NotNull(controllers);
        Assert.Contains(logger.Messages, m =>
            m.Contains("No game controllers found", StringComparison.Ordinal) ||
            m.Contains("joystick device(s)", StringComparison.Ordinal));
        foreach (IController controller in controllers)
        {
            Assert.False(string.IsNullOrEmpty(controller.Name));
        }
    }

    [Fact]
    public void GetControllers_CalledTwice_RefreshesWithoutThrowing()
    {
        using var service = new SDL2InputService(new CapturingLogger());

        IEnumerable<IController> first = service.GetControllers();
        IEnumerable<IController> second = service.GetControllers();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Count(), second.Count());
    }

    [Fact]
    public void GetControllers_ReturnsSnapshotThatLaterRefreshDoesNotMutate()
    {
        using var service = new SDL2InputService(new CapturingLogger());

        IController[] first = service.GetControllers().ToArray();
        int countBefore = first.Length;
        _ = service.GetControllers();

        Assert.Equal(countBefore, first.Length);
    }

    [Fact]
    public void ReadData_NullController_ThrowsAndLogsError()
    {
        var logger = new CapturingLogger();
        using var service = new SDL2InputService(logger);

        Assert.Throws<ArgumentNullException>(() => service.ReadData(null!));
        Assert.Contains(logger.Messages, m => m.Contains("null controller", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadData_DelegatesToController()
    {
        using var service = new SDL2InputService(new CapturingLogger());
        var expected = new ControllerData();
        var controller = new StubController(expected);

        Assert.Same(expected, service.ReadData(controller));
    }

    [Fact]
    public void Dispose_IsIdempotentAndLogsOnce()
    {
        var logger = new CapturingLogger();
        var service = new SDL2InputService(logger);

        service.Dispose();
        service.Dispose();

        Assert.Equal(1, logger.Messages.Count(m => m.Contains("SDL2InputService disposed", StringComparison.Ordinal)));
    }

    [Fact]
    public void Service_CanBeRecreatedAfterDispose()
    {
        new SDL2InputService(new CapturingLogger()).Dispose();

        using var service = new SDL2InputService(new CapturingLogger());

        Assert.NotNull(service.GetControllers());
    }

    [Fact]
    public void DescribeDevice_OutOfRangeIndex_ReturnsUnpopulatedSnapshotWithoutThrowing()
    {
        using var service = new SDL2InputService(new CapturingLogger());

        SdlDeviceInfo info = SDL2InputService.DescribeDevice(int.MaxValue);

        Assert.Equal(int.MaxValue, info.Index);
        Assert.NotNull(info.Guid);
        Assert.False(info.IsGameController);
        Assert.False(string.IsNullOrEmpty(info.JoystickType));
        Assert.False(SdlDeviceDiagnostics.ShouldInclude(info, out string? reason));
        Assert.NotNull(reason);
    }

    private sealed class StubController(ControllerData? data) : IController
    {
        public string Name => "Stub";

        public ControllerData? ReadData() => data;

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger : ILogger<SDL2InputService>
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages
        {
            get
            {
                lock (_messages)
                {
                    return [.. _messages];
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_messages)
            {
                _messages.Add(formatter(state, exception));
            }
        }
    }
}

using GameControllerForZwift.Gamepad.SDL2;

namespace GameControllerForZwift.Gamepad.UnitTests;

/// <summary>
/// Change detection by SDL instance id (CTRL-003): only devices that connected or disconnected are opened or closed.
/// </summary>
public class SdlDeviceSetTests
{
    [Fact]
    public void Diff_SameDevices_HasNoChanges()
    {
        SdlDeviceChange change = SdlDeviceSet.Diff([3, 7], [7, 3]);

        Assert.False(change.HasChanges);
        Assert.Empty(change.Added);
        Assert.Empty(change.Removed);
    }

    [Fact]
    public void Diff_NewDevice_IsAddedInEnumerationOrder()
    {
        SdlDeviceChange change = SdlDeviceSet.Diff([3], [9, 3, 4]);

        Assert.True(change.HasChanges);
        Assert.Equal([9, 4], change.Added);
        Assert.Empty(change.Removed);
    }

    [Fact]
    public void Diff_DisconnectedDevice_IsRemoved()
    {
        SdlDeviceChange change = SdlDeviceSet.Diff([3, 4], [4]);

        Assert.Empty(change.Added);
        Assert.Equal([3], change.Removed);
    }

    [Fact]
    public void Diff_ReconnectedDevice_GetsANewInstanceId_SoItIsRemovedAndAdded()
    {
        SdlDeviceChange change = SdlDeviceSet.Diff([3], [5]);

        Assert.Equal([5], change.Added);
        Assert.Equal([3], change.Removed);
    }

    [Fact]
    public void Diff_FromNothing_AddsEveryDevice()
    {
        SdlDeviceChange change = SdlDeviceSet.Diff([], [0, 1]);

        Assert.Equal([0, 1], change.Added);
    }

    [Fact]
    public void Diff_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => SdlDeviceSet.Diff(null!, []));
        Assert.Throws<ArgumentNullException>(() => SdlDeviceSet.Diff([], null!));
    }
}

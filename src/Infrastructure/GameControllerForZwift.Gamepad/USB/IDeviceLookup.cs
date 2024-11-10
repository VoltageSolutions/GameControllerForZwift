namespace GameControllerForZwift.Gamepad.USB
{
    public interface IDeviceLookup
    {
        string GetDeviceName(string vendorId, string productId);
        string GetDeviceName(Guid productGuid);
    }
}

namespace GameControllerForZwift.Core.Controller
{
    public interface IController : IDisposable
    {
        public string Name { get; }

        /// <summary>
        /// Gets the USB vendor ID that SDL reports, or 0 if it is unknown.
        /// </summary>
        public ushort VendorId { get; }

        /// <summary>
        /// Gets the USB product ID that SDL reports, or 0 if it is unknown.
        /// </summary>
        public ushort ProductId { get; }

        public ControllerData? ReadData();
    }
}

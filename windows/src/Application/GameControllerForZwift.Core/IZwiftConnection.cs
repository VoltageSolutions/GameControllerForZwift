namespace GameControllerForZwift.Core
{
    /// <summary>Reports whether Zwift is connected to the app's controller.</summary>
    public interface IZwiftConnection
    {
        /// <summary>True while Zwift is connected.</summary>
        bool IsZwiftConnected { get; }

        /// <summary>Raised with the new value of <see cref="IsZwiftConnected"/>. It can be raised on any thread.</summary>
        event EventHandler<bool>? ConnectionChanged;
    }
}

namespace GameControllerForZwift.Core
{
    /// <summary>
    /// A destination for the mapped controller state. Implementations decide how the state is delivered
    /// (for example over the network); the application core stays transport-agnostic.
    /// </summary>
    public interface IOutputService
    {
        /// <summary>Pushes the current output state. Must be cheap and non-blocking.</summary>
        Task UpdateAsync(OutputState state, CancellationToken ct = default);
    }
}

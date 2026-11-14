namespace GameControllerForZwift.UITests
{
    // xUnit collection definitions are conventionally named "...Collection"; renaming would obscure their purpose.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit collection definition type; the Collection suffix is the xUnit convention.")]
    [CollectionDefinition("Application collection")]
    public class AppCollection : ICollectionFixture<AppFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}

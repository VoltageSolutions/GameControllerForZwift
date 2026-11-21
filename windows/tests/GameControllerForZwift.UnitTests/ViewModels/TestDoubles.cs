using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.UI.WPF.Dialogs;
using NSubstitute;

namespace GameControllerForZwift.UI.WPF.Tests.ViewModels
{
    /// <summary>
    /// A unique folder under the temp path, deleted on dispose. Tests never touch the real %LOCALAPPDATA%.
    /// </summary>
    internal sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Join(System.IO.Path.GetTempPath(), "GameControllerForZwift.UnitTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Combine(params string[] parts) => System.IO.Path.Join([Path, .. parts]);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best effort: a leftover temp folder does not affect other tests.
            }
        }
    }

    /// <summary>
    /// Answers dialogs from queues set up by the test and records what was shown.
    /// </summary>
    internal sealed class FakeDialogService : IDialogService
    {
        public Queue<bool> ConfirmAnswers { get; } = new();
        public Queue<UnsavedChangesChoice> UnsavedChangesAnswers { get; } = new();
        public Queue<string?> NameAnswers { get; } = new();
        public Queue<string?> PickedFiles { get; } = new();

        public List<string> Confirmations { get; } = [];
        public List<string> ConfirmMessages { get; } = [];
        public int UnsavedChangesPrompts { get; private set; }
        public List<(string InitialName, Func<string, string?> Validate)> NamePrompts { get; } = [];
        public List<string> Errors { get; } = [];
        public List<string> Notices { get; } = [];
        public List<string> OpenedFolders { get; } = [];

        /// <summary>
        /// Gets or sets what runs while a dialog is open, for example a nested message loop like a modal window has.
        /// </summary>
        public Action? WhileOpen { get; set; }

        public bool Confirm(string title, string message, string confirmText)
        {
            WhileOpen?.Invoke();
            Confirmations.Add(title);
            ConfirmMessages.Add(message);
            return ConfirmAnswers.Count > 0 && ConfirmAnswers.Dequeue();
        }

        public UnsavedChangesChoice AskUnsavedChanges(string configurationName)
        {
            UnsavedChangesPrompts++;
            WhileOpen?.Invoke();
            return UnsavedChangesAnswers.Count > 0 ? UnsavedChangesAnswers.Dequeue() : UnsavedChangesChoice.Cancel;
        }

        public string? PromptForName(string title, string message, string initialName, string confirmText, Func<string, string?> validate)
        {
            NamePrompts.Add((initialName, validate));
            WhileOpen?.Invoke();
            return NameAnswers.Count > 0 ? NameAnswers.Dequeue() : null;
        }

        public int FilePickPrompts { get; private set; }

        public string? PickConfigurationFile()
        {
            FilePickPrompts++;
            return PickedFiles.Count > 0 ? PickedFiles.Dequeue() : null;
        }

        public void ShowError(string title, string message) => Errors.Add(title + ": " + message);

        public void ShowNotice(string title, string message) => Notices.Add(title + ": " + message);

        public void OpenFolder(string folderPath) => OpenedFolders.Add(folderPath);
    }

    internal static class TestControllers
    {
        public static IController Create(ushort vendorId, ushort productId, string name)
        {
            var controller = Substitute.For<IController>();
            controller.VendorId.Returns(vendorId);
            controller.ProductId.Returns(productId);
            controller.Name.Returns(name);
            return controller;
        }
    }
}

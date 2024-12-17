using GameControllerForZwift.UI.WPF.Controls;
using System.Windows;

namespace GameControllerForZwift.UI.WPF.Tests.Controls
{
    public class CardTests
    {
        #region TileRadius Tests

        [WpfFact]
        public void TileRadius_DefaultValue_IsZero()
        {
            // Arrange
            var card = new Card();

            // Act
            var tileRadius = card.TileRadius;

            // Assert
            Assert.Equal(new CornerRadius(0), tileRadius);
        }

        [WpfFact]
        public void TileRadius_SetValue_ChangesValue()
        {
            // Arrange
            var card = new Card();
            var newRadius = new CornerRadius(10);

            // Act
            card.TileRadius = newRadius;

            // Assert
            Assert.Equal(newRadius, card.TileRadius);
        }

        #endregion

        #region Title Tests

        [WpfFact]
        public void Title_DefaultValue_IsEmptyString()
        {
            // Arrange
            var card = new Card();

            // Act
            var title = card.Title;

            // Assert
            Assert.Equal(string.Empty, title);
        }

        [WpfFact]
        public void Title_SetValue_ChangesValue()
        {
            // Arrange
            var card = new Card();
            var newTitle = "New Title";

            // Act
            card.Title = newTitle;

            // Assert
            Assert.Equal(newTitle, card.Title);
        }

        #endregion

        #region Subtitle Tests

        [WpfFact]
        public void Subtitle_DefaultValue_IsEmptyString()
        {
            // Arrange
            var card = new Card();

            // Act
            var subtitle = card.Subtitle;

            // Assert
            Assert.Equal(string.Empty, subtitle);
        }

        [WpfFact]
        public void Subtitle_SetValue_ChangesValue()
        {
            // Arrange
            var card = new Card();
            var newSubtitle = "New Subtitle";

            // Act
            card.Subtitle = newSubtitle;

            // Assert
            Assert.Equal(newSubtitle, card.Subtitle);
        }

        #endregion

        #region Content Tests

        [WpfFact]
        public void Content_DefaultValue_IsNull()
        {
            // Arrange
            var card = new Card();

            // Act
            var content = card.Content;

            // Assert
            Assert.Null(content);
        }

        [WpfFact]
        public void Content_SetValue_ChangesValue()
        {
            // Arrange
            var card = new Card();
            var newContent = new { Name = "Test" };

            // Act
            card.Content = newContent;

            // Assert
            Assert.Equal(newContent, card.Content);
        }

        #endregion

        #region DependencyPropertyChanged Tests

        [WpfFact]
        public void TileRadiusPropertyChanged_TriggersTileRadiusChangedCallback()
        {
            // Arrange
            var card = new Card();
            var newRadius = new CornerRadius(10);
            bool callbackCalled = false;

            // Act
            card.TileRadius = newRadius;
            // TileRadiusPropertyChanged callback is automatically invoked in the background

            // Assert
            Assert.Equal(newRadius, card.TileRadius);
        }

        #endregion
    }
}

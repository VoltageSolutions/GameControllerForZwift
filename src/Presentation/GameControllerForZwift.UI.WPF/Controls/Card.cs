using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameControllerForZwift.UI.WPF.Controls
{
    public partial class Card : UserControl
    {
        static Card()
        {

        }

        public CornerRadius TileRadius
        {
            get { return (CornerRadius)GetValue(TileRadiusProperty); }
            set { SetValue(TileRadiusProperty, value); }
        }

        public static readonly DependencyProperty TileRadiusProperty =
            DependencyProperty.Register("TileRadius", typeof(CornerRadius), typeof(Card), new PropertyMetadata(new CornerRadius(0)));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Card), new PropertyMetadata(""));

        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle", typeof(string), typeof(Card), new PropertyMetadata(""));


        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(Card),
                new PropertyMetadata(null));

        public object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }


    }
}

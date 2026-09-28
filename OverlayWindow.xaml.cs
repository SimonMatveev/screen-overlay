using System.Windows;
using System.Windows.Media;

namespace ScreenOverlayApp
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow(double left, double top, double width, double height)
        {
            InitializeComponent();

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            ShowInTaskbar = false;
            Background = System.Windows.Media.Brushes.Black;

            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using HeyClicky.ComputerControl;

namespace HeyClicky.Overlay
{
    public partial class HeyClickyOverlay : Window
    {
        public HeyClickyOverlay()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Make the window click-through
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            
            // Add WS_EX_TRANSPARENT (Click-through) and WS_EX_TOOLWINDOW (Hide from ALT+TAB)
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, extendedStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW);
        }

        public void UpdateStatus(string status, System.Windows.Media.Color dotColor)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
                StatusDot.Fill = new System.Windows.Media.SolidColorBrush(dotColor);
            });
        }

        public void ShowTargetMarker(int x, int y)
        {
            Dispatcher.Invoke(() =>
            {
                // Note: In WPF, logical pixels might not perfectly match physical pixels on high DPI displays.
                // For V1, we assume 100% scaling. To make it perfect, we'd multiply by the DPI ratio.
                TargetMarker.Visibility = Visibility.Visible;
                
                // Center the marker on the coordinates
                System.Windows.Controls.Canvas.SetLeft(TargetMarker, x - (TargetMarker.Width / 2));
                System.Windows.Controls.Canvas.SetTop(TargetMarker, y - (TargetMarker.Height / 2));

                // Quick pop-in animation
                DoubleAnimation anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                TargetMarker.BeginAnimation(UIElement.OpacityProperty, anim);
            });
        }

        public void HideTargetMarker()
        {
            Dispatcher.Invoke(() =>
            {
                TargetMarker.Visibility = Visibility.Collapsed;
            });
        }
    }
}

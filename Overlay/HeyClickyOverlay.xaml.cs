using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
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
                var brush = new SolidColorBrush(dotColor);
                
                // Update glowing orb halo and status dot
                OrbGlowRing.Stroke = brush;
                OrbDropShadow.Color = dotColor;
                OrbAura.Fill = brush;
                OrbStatusDot.Fill = brush;
                OrbStatusDotGlow.Fill = brush;
                
                // Quick pulse on state change
                DoubleAnimation pulse = new DoubleAnimation(0.8, 0.25, TimeSpan.FromMilliseconds(400))
                {
                    AutoReverse = true
                };
                OrbAura.BeginAnimation(UIElement.OpacityProperty, pulse);
            });
        }

        public void ShowTargetMarker(int x, int y)
        {
            Dispatcher.Invoke(() =>
            {
                TargetMarkerGroup.Visibility = Visibility.Visible;
                
                // Center the 60x60 marker group on the coordinates
                System.Windows.Controls.Canvas.SetLeft(TargetMarkerGroup, x - 30);
                System.Windows.Controls.Canvas.SetTop(TargetMarkerGroup, y - 30);

                // Ripple animation
                DoubleAnimation fadeAnim = new DoubleAnimation(0.9, 0.0, TimeSpan.FromMilliseconds(500));
                TargetRipple.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
            });
        }

        public void HideTargetMarker()
        {
            Dispatcher.Invoke(() =>
            {
                TargetMarkerGroup.Visibility = Visibility.Collapsed;
            });
        }
    }
}

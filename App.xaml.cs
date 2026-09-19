using System.Windows;
using HeyClicky.Overlay;

namespace HeyClicky
{
    public partial class App : System.Windows.Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Create and show the transparent overlay
            HeyClickyOverlay overlay = new HeyClickyOverlay();
            overlay.Show();

            // Create and show the main control window
            MainWindow mainWindow = new MainWindow();
            mainWindow.InitializeAgent(overlay);
            mainWindow.Show();
        }
    }
}

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using HeyClicky.Core;
using HeyClicky.Overlay;
using HeyClicky.Vision;

namespace HeyClicky
{
    public partial class MainWindow : Window
    {
        private HeyClickyOverlay _overlay;
        private AgentController _agent;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void InitializeAgent(HeyClickyOverlay overlay)
        {
            _overlay = overlay;
            
            // For V1 testing without an API key, use MockVisionProvider.
            // Replace with GeminiVisionProvider when ready to test live vision.
           // IVisionProvider visionProvider = new MockVisionProvider();
            IVisionProvider visionProvider = new GeminiVisionProvider("YOUR_API_KEY_HERE");

            _agent = new AgentController(visionProvider);
            
            _agent.OnStateChanged += (state, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogText.Text = message;
                    
                    System.Windows.Media.Color dotColor = Colors.Cyan;
                    switch (state)
                    {
                        case AgentState.Idle: dotColor = Colors.Gray; break;
                        case AgentState.Observing: dotColor = Colors.Yellow; break;
                        case AgentState.Reasoning: dotColor = Colors.Orange; break;
                        case AgentState.Acting: dotColor = Colors.Red; break;
                        case AgentState.Done: dotColor = Colors.LimeGreen; break;
                        case AgentState.Error: dotColor = Colors.DarkRed; break;
                    }
                    
                    _overlay.UpdateStatus(message, dotColor);
                });
            };

            _agent.OnTargetIdentified += (x, y) =>
            {
                _overlay.ShowTargetMarker(x, y);
            };
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandTextBox.Text;
            if (string.IsNullOrWhiteSpace(command)) return;

            RunButton.IsEnabled = false;
            _overlay.HideTargetMarker();
            
            await Task.Run(() => _agent.ExecuteCommandAsync(command));
            
            Dispatcher.Invoke(() =>
            {
                RunButton.IsEnabled = true;
                _overlay.UpdateStatus("Idle", Colors.Gray);
            });
        }
    }
}

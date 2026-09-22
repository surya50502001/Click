using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HeyClicky.Agent;
using HeyClicky.Core;
using HeyClicky.Overlay;
using HeyClicky.Tools;

namespace HeyClicky
{
    public partial class MainWindow : Window
    {
        private const string LocalKeyFile = "local_api_key.txt";
        private HeyClickyOverlay _overlay;
        private ToolRegistry _registry;
        private AgentLoop _agentLoop;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();
            LoadApiKey();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadApiKey()
        {
            try
            {
                if (File.Exists(LocalKeyFile))
                {
                    string saved = File.ReadAllText(LocalKeyFile).Trim();
                    if (!string.IsNullOrEmpty(saved))
                    {
                        ApiKeyBox.Password = saved;
                        return;
                    }
                }

                string envKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (!string.IsNullOrEmpty(envKey))
                {
                    ApiKeyBox.Password = envKey;
                }
            }
            catch { }
        }

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                string key = ApiKeyBox.Password.Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    File.WriteAllText(LocalKeyFile, key);
                }
            }
            catch { }
        }

        public void InitializeAgent(HeyClickyOverlay overlay)
        {
            _overlay = overlay;
            
            // 1. Setup Tools
            _registry = new ToolRegistry();
            _registry.RegisterTool(new MouseClickTool());
            _registry.RegisterTool(new MouseDragTool());
            _registry.RegisterTool(new DrawCircleTool());
            _registry.RegisterTool(new TypeTextTool());
            _registry.RegisterTool(new PressKeyTool());
            _registry.RegisterTool(new FocusWindowTool());
            _registry.RegisterTool(new LaunchAppTool());
            _registry.RegisterTool(new HotkeyTool());
            _registry.RegisterTool(new WaitTool());
            _registry.RegisterTool(new DoneTool());
            _registry.RegisterTool(new FailTool());
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandTextBox.Text;
            if (string.IsNullOrWhiteSpace(command)) return;

            string apiKey = ApiKeyBox.Password.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                LogText.Text = "Please enter your Gemini API Key in the field above.";
                LogText.Foreground = System.Windows.Media.Brushes.Salmon;
                return;
            }

            RunButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            LogText.Foreground = System.Windows.Media.Brushes.White;
            _overlay.HideTargetMarker();

            // Setup fresh Agent Model and Loop
            IAgentModel model = new GeminiAgentModel(apiKey, _registry);
            _agentLoop = new AgentLoop(model, _registry);

            _agentLoop.OnStateChanged += (state, message) =>
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

                    var brush = new SolidColorBrush(dotColor);
                    StatusDot.Fill = brush;
                    StatusDotGlow.Fill = brush;
                    _overlay.UpdateStatus(message, dotColor);
                });
            };

            _agentLoop.OnTargetIdentified += (x, y) =>
            {
                _overlay.ShowTargetMarker(x, y);
                Task.Delay(1000).ContinueWith(_ => _overlay.HideTargetMarker());
            };
            
            _cts = new CancellationTokenSource();

            try
            {
                await Task.Run(() => _agentLoop.RunAsync(command, _cts.Token));
            }
            catch (OperationCanceledException)
            {
                LogText.Text = "Agent stopped by user.";
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    RunButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                    _overlay.UpdateStatus("Idle", Colors.Gray);
                });
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                LogText.Text = "Stopping agent immediately...";
                LogText.Foreground = System.Windows.Media.Brushes.Salmon;
                StopButton.IsEnabled = false;
            }
        }
    }
}

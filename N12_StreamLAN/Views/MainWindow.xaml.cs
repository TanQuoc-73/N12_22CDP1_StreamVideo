using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using OpenCvSharp;
using Server_StreamLAN.Services;
using Server_StreamLAN.Utils;

namespace Server_StreamLAN.Views
{

    public partial class MainWindow : System.Windows.Window
    {

        private UdpReceiver _receiver;
        private CancellationTokenSource _cts;
        private DiscoveryService _discovery;
        private FaceDetectionService _faceDetection;
        private RecordingService _recording;
        private DispatcherTimer _recordingTimer;
        private DateTime _recordingStartUtc;
        private volatile bool _faceDetectionEnabled;
        private static readonly SolidColorBrush RecordActiveBrush = new(Color.FromRgb(0xCD, 0x5C, 0x5C));

        public MainWindow()
        {
            InitializeComponent();

            // Tự động thêm rule firewall (UAC một lần) để stream LAN không cần mở port thủ công
            FirewallHelper.EnsureFirewallRules();

            txtLocalIp.Text = NetworkInfo.GetLocalIPv4() ?? "Khong xac dinh duoc IPv4";

            _receiver = new UdpReceiver(9000);
            _cts = new CancellationTokenSource();

            _discovery = new DiscoveryService();
            _discovery.Start();

            _faceDetection = new FaceDetectionService();
            _recording = new RecordingService();

            _recordingTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
            _recordingTimer.Tick += RecordingTimer_Tick;

            StartReceiveLoop();
        }

        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        byte[] data = await _receiver.ReceiveAsync();

                        using Mat frame = Cv2.ImDecode(data, ImreadModes.Color);

                        if (!frame.Empty())
                        {
                            if (_faceDetectionEnabled)
                                _faceDetection.DetectAndDraw(frame);

                            if (_recording.IsRecording)
                                _recording.WriteFrame(frame);

                            var bitmap = ImgConverter.ToBitmapSource(frame);

                            if (bitmap != null)
                            {
                                bitmap.Freeze();
                                Dispatcher.Invoke(() =>
                                {
                                    imgCamera.Source = bitmap;
                                });
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }

        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_recording.IsRecording)
            {
                _recording.Stop();
                _recordingTimer.Stop();
                btnRecordIcon.Text = "\uE1C4"; // Record icon
                btnRecordText.Text = "Record";
                btnRecord.Background = (Brush)FindResource("AutumnAccentBrush");
                txtRecordingStatus.Text = "Idle";
            }
            else
            {
                _recording.Start();
                _recordingStartUtc = DateTime.UtcNow;
                _recordingTimer.Start();
                btnRecordIcon.Text = "\uE15A"; // Stop icon
                btnRecordText.Text = "Stop";
                btnRecord.Background = RecordActiveBrush;
                UpdateRecordingStatusText();
            }
        }

        private void RecordingTimer_Tick(object? sender, EventArgs e)
        {
            UpdateRecordingStatusText();
        }

        private void UpdateRecordingStatusText()
        {
            if (!_recording.IsRecording) return;
            var elapsed = DateTime.UtcNow - _recordingStartUtc;
            txtRecordingStatus.Text = $"Recording {elapsed:mm\\:ss}";
        }

        private void BtnFaceDetection_Changed(object sender, RoutedEventArgs e)
        {
            _faceDetectionEnabled = btnFaceDetection.IsChecked == true;
        }
        
        protected override void OnClosed(EventArgs e)
        {
            _recordingTimer?.Stop();
            _recording?.Stop();
            _faceDetection?.Dispose();
            _cts?.Cancel();
            base.OnClosed(e);
        }

        // Window control handlers
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to maximize/restore
                MaximizeRestoreWindow();
            }
            else
            {
                // Single click to drag
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MaximizeRestoreWindow();
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeRestoreWindow()
        {
            if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Maximized;
            }
        }
    }
}

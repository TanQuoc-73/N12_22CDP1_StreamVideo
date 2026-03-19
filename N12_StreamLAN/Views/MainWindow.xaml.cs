using System;
using System.Linq;
using System.Net;
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
        // ── Services ───────────────────────────────────────────────────────
        private UdpReceiver         _receiver;
        private CancellationTokenSource _cts;
        private DiscoveryService    _discovery;
        private FaceDetectionService _faceDetection;
        private RecordingService    _recording;
        private AudioPlaybackService _audioPlayback;
        private UdpAudioReceiver     _audioReceiver;

        // ── State ──────────────────────────────────────────────────────────
        private volatile bool _faceDetectionEnabled;
        private IPEndPoint?   _activeClient;   // which client stream to display

        // ── Timers ─────────────────────────────────────────────────────────
        private DispatcherTimer _recordingTimer;
        private DispatcherTimer _statsTimer;
        private DateTime        _recordingStartUtc;
        private int             _frameCount;
        private DateTime        _fpsTimer = DateTime.UtcNow;

        private static readonly SolidColorBrush RecordActiveBrush =
            new(Color.FromRgb(0xCD, 0x5C, 0x5C));

        // ──────────────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();

            FirewallHelper.EnsureFirewallRules();
            txtLocalIp.Text = NetworkInfo.GetLocalIPv4() ?? "Không xác định";

            _receiver      = new UdpReceiver(9000);
            _cts           = new CancellationTokenSource();
            _discovery     = new DiscoveryService();
            _discovery.Start();
            _faceDetection = new FaceDetectionService();
            _recording     = new RecordingService();

            // Audio playback
            _audioPlayback = new AudioPlaybackService();
            _audioReceiver = new UdpAudioReceiver(_audioPlayback);
            _audioPlayback.Start();
            _audioReceiver.Start();

            // Recording status timer
            _recordingTimer = new DispatcherTimer(DispatcherPriority.Background)
                { Interval = TimeSpan.FromSeconds(1) };
            _recordingTimer.Tick += (_, _) => UpdateRecordingStatusText();

            // Stats update timer (FPS, clients, packet loss)
            _statsTimer = new DispatcherTimer(DispatcherPriority.Background)
                { Interval = TimeSpan.FromSeconds(1) };
            _statsTimer.Tick += StatsTimer_Tick;
            _statsTimer.Start();

            StartReceiveLoop();
        }

        // ── Receive loop ───────────────────────────────────────────────────
        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var (ep, jpeg, seqNo, flags) = await _receiver.ReceiveAsync();

                        // Auto-select first client
                        if (_activeClient == null) _activeClient = ep;

                        // Only render from active client
                        if (!ep.Equals(_activeClient)) continue;

                        using Mat frame = Cv2.ImDecode(jpeg, ImreadModes.Color);
                        if (frame.Empty()) continue;

                        if (_faceDetectionEnabled) _faceDetection.DetectAndDraw(frame);
                        if (_recording.IsRecording)  _recording.WriteFrame(frame);

                        _frameCount++;

                        var bitmap = ImgConverter.ToBitmapSource(frame);
                        if (bitmap != null)
                        {
                            bitmap.Freeze();
                            Dispatcher.InvokeAsync(() => imgCamera.Source = bitmap);
                        }
                    }
                    catch (Exception) { /* keep loop alive */ }
                }
            });
        }

        // ── Stats timer ────────────────────────────────────────────────────
        private void StatsTimer_Tick(object? sender, EventArgs e)
        {
            double elapsed = (DateTime.UtcNow - _fpsTimer).TotalSeconds;
            int fps = elapsed > 0 ? (int)(_frameCount / elapsed) : 0;
            _frameCount = 0;
            _fpsTimer   = DateTime.UtcNow;

            var sessions = _receiver.Clients.ToList();
            int count    = _receiver.ClientCount;

            // Packet loss from the currently active client
            var activeSession = sessions.FirstOrDefault(c => c.EndPoint.Equals(_activeClient));
            double loss = activeSession?.PacketLossPercent ?? 0;

            txtClients.Text  = $"Clients: {count}";
            txtStatsFps.Text = $"FPS: {fps}";
            txtLoss.Text     = $"Loss: {loss:F1}%";

            // Refresh client list in sidebar
            lbClients.ItemsSource = null;
            lbClients.ItemsSource = sessions;
        }

        // ── Client list selection ──────────────────────────────────────────
        private void LbClients_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lbClients.SelectedItem is ClientSession s)
                _activeClient = s.EndPoint;
        }

        // ── Record ─────────────────────────────────────────────────────────
        private void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (_recording.IsRecording)
            {
                _recording.Stop();
                _recordingTimer.Stop();
                btnRecordIcon.Text = "\uE1C4";
                btnRecordText.Text = "Record";
                btnRecord.Background = (Brush)FindResource("AutumnAccentBrush");
                txtRecordingStatus.Text = "Idle";
            }
            else
            {
                _recording.Start();
                _recordingStartUtc = DateTime.UtcNow;
                _recordingTimer.Start();
                btnRecordIcon.Text = "\uE15A";
                btnRecordText.Text = "Stop";
                btnRecord.Background = RecordActiveBrush;
                UpdateRecordingStatusText();
            }
        }

        private void UpdateRecordingStatusText()
        {
            if (!_recording.IsRecording) return;
            var elapsed = DateTime.UtcNow - _recordingStartUtc;
            txtRecordingStatus.Text = $"Recording {elapsed:mm\\:ss}";
        }

        // ── Face Detection ─────────────────────────────────────────────────
        private void BtnFaceDetection_Changed(object sender, RoutedEventArgs e)
            => _faceDetectionEnabled = btnFaceDetection.IsChecked == true;

        // ── Audio controls ─────────────────────────────────────────────────
        private void BtnMuteAudio_Changed(object sender, RoutedEventArgs e)
        {
            bool muted = btnMuteAudio.IsChecked == true;
            _audioPlayback.IsMuted = muted;
            txtMuteLabel.Text = muted ? "Muted" : "Audio";
        }

        private void SliderVolume_ValueChanged(object sender,
            System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (_audioPlayback != null)
                _audioPlayback.Volume = (float)(sliderVolume.Value / 100.0);
        }

        // ── Cleanup ────────────────────────────────────────────────────────
        protected override void OnClosed(EventArgs e)
        {
            _statsTimer?.Stop();
            _recordingTimer?.Stop();
            _recording?.Stop();
            _faceDetection?.Dispose();
            _audioReceiver?.Dispose();
            _audioPlayback?.Dispose();
            _receiver?.Dispose();
            _cts?.Cancel();
            base.OnClosed(e);
        }

        // ── Window chrome ──────────────────────────────────────────────────
        private void TitleBar_MouseLeftButtonDown(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) MaximizeRestoreWindow();
            else DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
            => MaximizeRestoreWindow();

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close();

        private void MaximizeRestoreWindow()
            => WindowState = WindowState == WindowState.Maximized
               ? WindowState.Normal : WindowState.Maximized;
    }
}

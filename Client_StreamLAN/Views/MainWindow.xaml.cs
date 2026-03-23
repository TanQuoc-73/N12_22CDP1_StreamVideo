using Client_StreamLAN.Models;
using Client_StreamLAN.Services;
using Client_StreamLAN.Utils;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client_StreamLAN.Views
{
    public partial class MainWindow : System.Windows.Window
    {
        private readonly CameraService          _camera     = new();
        private readonly StreamController       _controller = new();
        private readonly AdaptiveBitrateController _adaptive = new();
        private UdpSender?                      _sender;
        private readonly AudioCaptureService    _audioCapture = new();

        private CancellationTokenSource? _cts;
        private uint  _seqNo;
        private int   _frameCount;
        private DateTime _fpsTimer = DateTime.UtcNow;

        private int  _sendFailCount;
        private bool _reconnecting;
        private const int MaxFails = 5;

        private static readonly Dictionary<string, OpenCvSharp.Size> Resolutions = new()
        {
            { "320×240",  new OpenCvSharp.Size(320,  240)  },
            { "640×480",  new OpenCvSharp.Size(640,  480)  },
            { "960×540",  new OpenCvSharp.Size(960,  540)  },
            { "1280×720", new OpenCvSharp.Size(1280, 720)  },
        };

        public MainWindow()
        {
            InitializeComponent();

            _controller.StateChanged += OnStateChanged;

            txtLocalIp.Text    = NetworkInfo.GetLocalIPv4() ?? "Unavailable";
            txtUserEmail.Text  = "User: TEST_MODE";

            cbResolution.ItemsSource   = Resolutions.Keys.ToList();
            cbResolution.SelectedIndex = 1;

            int camCount = CameraService.GetCameraCount();
            cbCamera.ItemsSource   = Enumerable.Range(0, camCount).Select(i => $"Camera {i}").ToList();
            cbCamera.SelectedIndex = 0;

            try { _camera.Start(0); }
            catch {}
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_sender == null)
            {
                MessageBox.Show("Please connect to a server before streaming.");
                return;
            }
            _seqNo = 0;
            _adaptive.Reset();
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _controller.Start();
            _ = Task.Run(() => StreamLoopAsync(_cts.Token));

            if (chkMicrophone.IsChecked == true && _sender != null)
            {
                try { _audioCapture.Start(_sender.ServerIp); }
                catch { }
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _controller.Stop();
            _audioCapture.Stop();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            if (_controller.IsRunning)
            {
                _controller.Pause();
                _audioCapture.Enabled = false;
            }
            else if (_controller.IsPaused)
            {
                _controller.Resume();
                _audioCapture.Enabled = chkMicrophone.IsChecked == true;
            }
        }

        private void OnStateChanged(StreamState state)
        {
            Dispatcher.Invoke(() =>
            {
                txtStreamState.Text = state switch
                {
                    StreamState.Running      => "🟢 Streaming",
                    StreamState.Paused       => "⏸ Paused",
                    StreamState.Reconnecting => "🔄 Reconnecting...",
                    _                        => "⏹ Stopped"
                };

                btnStart.IsEnabled = state == StreamState.Stopped;
                btnStop.IsEnabled  = state != StreamState.Stopped;
                btnPause.IsEnabled = state == StreamState.Running || state == StreamState.Paused;
                btnPause.Content   = state == StreamState.Paused ? "▶ Resume" : "⏸ Pause";

                ellipseStatus.Fill = state == StreamState.Running
                    ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
                    : new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            });
        }

        private async Task StreamLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (_controller.IsPaused || _reconnecting)
                    {
                        await Task.Delay(100, ct);
                        continue;
                    }

                    using Mat? raw = _camera.GetFrame();
                    if (raw == null || raw.Empty()) { await Task.Delay(30, ct); continue; }

                    using Mat processed = _controller.ApplyControls(raw);
                    using var resized   = new Mat();
                    Cv2.Resize(processed, resized, _controller.Resolution);

                    int quality = _controller.UseAdaptive ? _adaptive.Quality : _controller.ManualQuality;
                    Cv2.ImEncode(".jpg", resized, out byte[] jpeg,
                                 new[] { (int)ImwriteFlags.JpegQuality, quality });

                    byte[] packet = PacketProtocol.Pack(_seqNo++, PacketProtocol.FlagKeyFrame, jpeg);

                    if (_sender != null && packet.Length < 65_000)
                    {
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            _sender.Send(packet);
                            sw.Stop();
                            _adaptive.Feedback(packet.Length, sw.ElapsedMilliseconds);
                            _sendFailCount = 0;
                        }
                        catch
                        {
                            sw.Stop();
                            if (++_sendFailCount >= MaxFails)
                                _ = Task.Run(() => ReconnectAsync(ct));
                        }
                    }

                    var bitmap = resized.ToBitmapSource();
                    bitmap?.Freeze();
                    Dispatcher.InvokeAsync(() => imgCamera.Source = bitmap);

                    _frameCount++;
                    double elapsed = (DateTime.UtcNow - _fpsTimer).TotalSeconds;
                    if (elapsed >= 1.0)
                    {
                        int fps = (int)(_frameCount / elapsed);
                        _frameCount = 0;
                        _fpsTimer   = DateTime.UtcNow;
                        Dispatcher.InvokeAsync(() =>
                        {
                            txtFps.Text     = $"FPS: {fps}";
                            txtQuality.Text = $"Q: {quality}%";
                        });
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* swallow per-frame errors */ }

                await Task.Delay(_controller.FrameDelayMs, ct).ConfigureAwait(false);
            }
        }

        private async Task ReconnectAsync(CancellationToken ct)
        {
            _reconnecting = true;
            _controller.SetReconnecting();
            string ip   = _sender?.ServerIp   ?? "127.0.0.1";
            int    port = _sender?.ServerPort  ?? 9000;

            while (!ct.IsCancellationRequested && _sendFailCount >= MaxFails)
            {
                await Task.Delay(2000, ct);
                try
                {
                    _sender?.Dispose();
                    _sender = new UdpSender(ip, port);
                    _sendFailCount = 0;
                    _reconnecting  = false;
                    _controller.Resume();
                    break;
                }
                catch { }
            }
        }

        private void CbCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { _camera.SwitchCamera(cbCamera.SelectedIndex); }
            catch { }
        }

        private void CbResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbResolution.SelectedItem is string key && Resolutions.TryGetValue(key, out var size))
                _controller.Resolution = size;
        }

        private void SliderQuality_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _controller.ManualQuality = (int)sliderQuality.Value;
            if (txtManualQuality != null) txtManualQuality.Text = $"{_controller.ManualQuality}%";
        }

        private void ChkAdaptive_Changed(object sender, RoutedEventArgs e)
        {
            _controller.UseAdaptive  = chkAdaptive.IsChecked == true;
            if (sliderQuality != null)
                sliderQuality.IsEnabled  = !_controller.UseAdaptive;
        }

        private void SliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => _controller.Brightness = sliderBrightness.Value;

        private void SliderContrast_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => _controller.Contrast = sliderContrast.Value;

        private void ChkFlipH_Changed(object sender, RoutedEventArgs e)
            => _controller.FlipH = chkFlipH.IsChecked == true;

        private void ChkFlipV_Changed(object sender, RoutedEventArgs e)
            => _controller.FlipV = chkFlipV.IsChecked == true;

        private void ChkMicrophone_Changed(object sender, RoutedEventArgs e)
        {
            bool enabled = chkMicrophone.IsChecked == true;
            if (_audioCapture.IsCapturing)
                _audioCapture.Enabled = enabled;
        }

        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnDiscover.IsEnabled = false;
                var servers = await new ServerDiscovery().DiscoverAsync();
                cbServers.ItemsSource = servers;
                if (servers.Count == 0)
                    txtServerStatus.Text = "No server found";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error discovering servers: {ex.Message}");
            }
            finally
            {
                btnDiscover.IsEnabled = true;
            }
        }

        private void CbServers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbServers.SelectedItem is ServerInfo s && !string.IsNullOrEmpty(s.Ip))
            {
                txtManualIp.Text = s.Ip;
                ConnectTo(s.Ip, s.Port);
            }
        }

        private void BtnManualConnect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtManualIp.Text.Trim();
            if (string.IsNullOrEmpty(ip)) { MessageBox.Show("Nhập IP server hợp lệ."); return; }
            ConnectTo(ip, 9000);
        }

        private void ConnectTo(string ip, int port)
        {
            try
            {
                _sender?.Dispose();
                _sender        = new UdpSender(ip, port);
                _sendFailCount = 0;
                _reconnecting  = false;
                txtServerStatus.Text = $"Connected: {ip}:{port}";
                btnStart.IsEnabled   = true;
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi kết nối: {ex.Message}"); }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts?.Cancel();
            _camera.Stop();
            _audioCapture.Dispose();
            _sender?.Dispose();
            base.OnClosed(e);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) MaximizeRestoreWindow();
            else DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, RoutedEventArgs e) => MaximizeRestoreWindow();
        private void CloseButton_Click(object sender, RoutedEventArgs e)    => Close();

        private void MaximizeRestoreWindow()
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}

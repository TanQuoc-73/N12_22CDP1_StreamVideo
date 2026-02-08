using Client_StreamLAN.Services;
using Client_StreamLAN.Utils;
using OpenCvSharp;
using OpenCvSharp.Internal.Vectors;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;



namespace Client_StreamLAN.Views
{
    public class ServerInfo
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }

        public override string ToString() => $"{Name} - {Ip}:{Port}";
    }
    public partial class MainWindow : System.Windows.Window
    {
        private CameraService _camera;
        private CancellationTokenSource _cts;
        private UdpSender _sender;


        public MainWindow()
        {
            InitializeComponent();

            // TEST :
            // if (!UserSession.IsLoggedIn)
            // {
            //     MessageBox.Show("Chua dang nhap");
            //     this.Close();
            //     return;
            // }


            txtLocalIp.Text = NetworkInfo.GetLocalIPv4() ?? "Khong xac dinh duoc IPv4";

            txtUserEmail.Text = $"User: TEST_MODE";

            try
            {
                _camera = new CameraService();
                _camera.Start();
                MessageBox.Show("Camera da khoi dong", "Debug Info");
                
                _sender = new UdpSender("127.0.0.1", 9000);
                MessageBox.Show("UDP Sender duoc tao (port 9000)", "Debug Info");

                _cts = new CancellationTokenSource();
                StartCameraLoop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi khoi dong:\n{ex.Message}", "Error");
                this.Close();
            }


        }

        private void StartCameraLoop()
        {
            Task.Run(async () =>
            {
                int frameCount = 0;
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        Mat frame = _camera.GetFrame();
                        if (frame != null && !frame.Empty())
                        {
                            frameCount++;
                            
                            using var resized = new Mat();
                            Cv2.Resize(frame, resized, new OpenCvSharp.Size(640, 480));
                            
                            //JPEG QUALITY < 60KB
                            var encodeParams = new[] { (int)ImwriteFlags.JpegQuality, 50 };
                            Cv2.ImEncode(".jpg", resized, out byte[] buffer, encodeParams);
                            
                            if (frameCount == 1)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show($"Frame dau tien: {buffer.Length} bytes (max 60KB)", "Debug Info");
                                });
                            }
                            
                            // Chỉ gửi nếu JPEG QUALITY< 60KB
                            if (buffer.Length < 60000)
                            {
                                _sender.Send(buffer);
                            }
                            else
                            {
                                if (frameCount == 1)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        MessageBox.Show($"Frame qua lon: {buffer.Length} bytes, giam quality", "Warning");
                                    });
                                }
                            }

                            var bitmap = ImgConverter.ToBitmapSource(resized);
                            
                            if (bitmap != null)
                            {
                                bitmap.Freeze();
                                Dispatcher.Invoke(() =>
                                {
                                    imgCamera.Source = bitmap;
                                    
                                    if (frameCount == 1)
                                    {
                                        MessageBox.Show($"Bitmap da set vao UI: {imgCamera.ActualWidth}x{imgCamera.ActualHeight}", "Debug Info");
                                    }
                                });
                            }
                            else
                            {
                                if (frameCount == 1)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        MessageBox.Show("Bitmap NULL!", "Error");
                                    });
                                }
                            }
                            
                            frame.Dispose();
                        }
                        else
                        {
                            if (frameCount == 0)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show("Frame NULL hoac EMPTY!", "Error");
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Loi loop:\n{ex.Message}", "Error");
                        });
                        break;
                    }

                    await Task.Delay(30);
                }
            });
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _cts?.Cancel();
            _camera?.Stop();
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

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            MaximizeRestoreWindow();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MaximizeRestoreWindow()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private async void BtnDiscover_Click(object sender, RoutedEventArgs e)
        {
            var discovery = new ServerDiscovery();
            var servers = await discovery.DiscoverAsync();

            cbServers.ItemsSource = servers;
        }

        

        private void CbServers_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbServers.SelectedItem is ServerInfo s)
            {
                _sender = new UdpSender(s.Ip, s.Port);
                MessageBox.Show($"Da chon server: {s.Ip}:{s.Port}");
            }
        }


    }
}

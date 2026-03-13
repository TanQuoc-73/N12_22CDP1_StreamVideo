using Client_StreamLAN.Models;
using Client_StreamLAN.Services;
using Client_StreamLAN.Utils;
using OpenCvSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Client_StreamLAN.Views
{
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

                
                _sender = new UdpSender("127.0.0.1", 9000);


                _cts = new CancellationTokenSource();
                StartCameraLoop();
            }
            catch (Exception)
            {
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
                            

                            
                            // Chỉ gửi nếu JPEG QUALITY< 60KB
                            if (buffer.Length < 60000)
                            {
                                _sender.Send(buffer);
                            }


                            var bitmap = ImgConverter.ToBitmapSource(resized);
                            
                            if (bitmap != null)
                            {
                                bitmap.Freeze();
                                Dispatcher.Invoke(() =>
                                {
                                    imgCamera.Source = bitmap;
                                    

                                });
                            }

                            
                            frame.Dispose();
                        }

                    }
                    catch (Exception)
                    {
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
            if (cbServers.SelectedItem is ServerInfo s && !string.IsNullOrEmpty(s.Ip))
            {
                _sender = new UdpSender(s.Ip, s.Port);
            }
        }


    }
}

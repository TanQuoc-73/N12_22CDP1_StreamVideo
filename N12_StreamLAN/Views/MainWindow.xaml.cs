using System;
using System.Threading;
using System.Threading.Tasks;
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

        public MainWindow()
        {
            

            InitializeComponent();


            txtLocalIp.Text = NetworkInfo.GetLocalIPv4() ?? "Khong xac dinh duoc IPv4";


            _receiver = new UdpReceiver(9000);
            _cts = new CancellationTokenSource();

            _discovery = new DiscoveryService();
            _discovery.Start();

            System.Windows.MessageBox.Show("Server khoi dong o port 9000...", "Debug Info");
            
            StartReceiveLoop();
        }

        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                int frameCount = 0;
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        byte[] data = await _receiver.ReceiveAsync();
                        
                        frameCount++;
                        
                        if (frameCount == 1)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                System.Windows.MessageBox.Show($"Frame dau tien nhan: {data.Length} bytes", "Debug Info");
                            });
                        }
                        
                        using Mat frame = Cv2.ImDecode(data, ImreadModes.Color);
                        
                        if (!frame.Empty())
                        {
                            var bitmap = ImgConverter.ToBitmapSource(frame);
                            
                            if (bitmap != null)
                            {
                                bitmap.Freeze();
                                Dispatcher.Invoke(() =>
                                {
                                    imgCamera.Source = bitmap;
                                    
                                    if (frameCount == 1)
                                    {
                                        System.Windows.MessageBox.Show($"Bitmap set vao UI: {imgCamera.ActualWidth}x{imgCamera.ActualHeight}", "Debug Info");
                                    }
                                });
                            }
                        }
                        else
                        {
                            if (frameCount == 1)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    System.Windows.MessageBox.Show("Decode frame FAILED!", "Error");
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show($"Loi nhan frame:\n{ex.Message}", "Error");
                        });
                    }
                }
            });
        }
        
        protected override void OnClosed(EventArgs e)
        {
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

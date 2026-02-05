using Client_StreamLAN.Services;
using Client_StreamLAN.Utils;
using OpenCvSharp;
using System.Drawing;
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

            if (!UserSession.IsLoggedIn)
            {
                MessageBox.Show("Chưa đăng nhập");
                this.Close();
                return;
            }

            txtUserEmail.Text = $"User: {UserSession.UserEmail}";

            _camera = new CameraService();
            _camera.Start();
            _sender = new UdpSender("127.0.0.1", 9000);

            _cts = new CancellationTokenSource();
            StartCameraLoop();
        }

        private void StartCameraLoop()
        {
            Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    Mat frame = _camera.GetFrame();
                    if (frame != null)
                    {
                        Cv2.ImEncode(".jpg", frame, out byte[] buffer);
                        _sender.Send(buffer);

                        var bitmap = ImgConverter.ToBitmapSource(frame);
                        Dispatcher.Invoke(() =>
                        {
                            imgCamera.Source = bitmap;
                        });
                    }

                    await Task.Delay(30);
                }
            });
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _cts.Cancel();
            _camera.Stop();
            base.OnClosed(e);
        }

    }
}

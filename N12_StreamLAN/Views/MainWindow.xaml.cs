using System.Threading.Tasks;
using OpenCvSharp;
using Server_StreamLAN.Services;
using Server_StreamLAN.Utils;

namespace Server_StreamLAN.Views
{

    public partial class MainWindow : System.Windows.Window
    {

        private UdpReceiver _receiver;  
        public MainWindow()
        {
            InitializeComponent();
            _receiver = new UdpReceiver(9000);
            StartReceiveLoop();
        }

        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    byte[] data = await _receiver.ReceiveAsync();
                    Mat frame = Cv2.ImDecode(data, ImreadModes.Color);

                    var bitmap = ImgConverter.ToBitmapSource(frame);
                    Dispatcher.Invoke(() =>
                    {
                        imgCamera.Source = bitmap;
                    });

                }
            });
        }
        
    }
}

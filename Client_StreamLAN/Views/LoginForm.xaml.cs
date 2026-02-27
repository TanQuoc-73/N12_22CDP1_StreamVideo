using System.Threading.Tasks;
using System.Windows;
using Client_StreamLAN.Services;
using Client_StreamLAN.Views;

namespace Client_StreamLAN.Views
{

    public partial class LoginForm : Window
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void Login_Btn_Click(object sender, RoutedEventArgs e)
        {
            string email = txt_Email.Text.Trim();
            string password = txt_Password.Password;

            if (email == "test@test.com" && password == "123456")
            {
                UserSession.AccessToken = "test-token";
                UserSession.UserEmail = email;

                MessageBox.Show("Dang nhap thanh cong");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Sai tai khoan hoac mat khau.\nTai khoan test: test@test.com / 123456");
            }
        }

        // Window control handlers
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

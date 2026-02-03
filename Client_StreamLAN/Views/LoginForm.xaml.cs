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

        private async void Login_Btn_Click(object sender, RoutedEventArgs e)
        {
            var auth = new SupabaseAuthService();

            bool ok = await auth.LoginAsync(
                txt_Email.Text,
                txt_Password.Password
                );

            if (ok)
            {
                MessageBox.Show("Dang nhap thanh cong");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Dang nhap that bai");

            }
        }


    }
}

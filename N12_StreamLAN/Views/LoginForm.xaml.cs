using System.Windows;
using Server_StreamLAN.Services;

namespace Server_StreamLAN.Views
{
    public partial class LoginForm : Window
    {
        public LoginForm()
        {
            InitializeComponent();
            btnSignIn.Click += Login_Btn_Click;
            btnGoogle.Click += BtnGoogle_Click;
        }

        private void Login_Btn_Click(object sender, RoutedEventArgs e)
        {
            string user = txt_Username.Text.Trim();
            string pwd  = txt_Password.Password;

            var auth = new ServerAuthService();
            bool ok = auth.Authenticate(user, pwd);

            if (ok)
            {
                ServerSession.Username = user;
                ServerSession.AccessToken = "server-local-token";

                MessageBox.Show("Đăng nhập server thành công");
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Sai tài khoản hoặc mật khẩu.");
            }
        }

        private async void BtnGoogle_Click(object? sender, RoutedEventArgs e)
        {
            // Reused button: perform Supabase email/password auth using the fields
            try
            {
                string user = txt_Username.Text.Trim();
                string pwd  = txt_Password.Password;

                var auth = new ServerAuthService();
                var (ok, email) = await auth.AuthenticateWithSupabaseAsync(user, pwd);
                if (ok)
                {
                    ServerSession.Username = email ?? user;
                    ServerSession.AccessToken = "server-supabase-token";
                    MessageBox.Show("Đăng nhập bằng Supabase thành công");
                    MainWindow main = new MainWindow();
                    main.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Đăng nhập Supabase thất bại.");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi đăng nhập: {ex.Message}");
            }
        }
    }
}

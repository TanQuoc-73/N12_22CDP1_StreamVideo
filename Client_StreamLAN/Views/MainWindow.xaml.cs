using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Client_StreamLAN.Services;

namespace Client_StreamLAN.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (!UserSession.IsLoggedIn)
            {
                MessageBox.Show("Chua dang nhap");
                this.Close();
                return;
            }

            txtUserEmail.Text = $"User: {UserSession.UserEmail}";
        }
    }
}

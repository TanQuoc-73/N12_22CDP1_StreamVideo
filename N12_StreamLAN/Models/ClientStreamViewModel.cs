using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Server_StreamLAN.Models
{
    public class ClientStreamViewModel : INotifyPropertyChanged
    {
        private BitmapSource? _currentFrame;

        public IPEndPoint Endpoint { get; }

        public string DisplayName { get; }

        public BitmapSource? CurrentFrame
        {
            get => _currentFrame;
            set
            {
                if (!Equals(_currentFrame, value))
                {
                    _currentFrame = value;
                    OnPropertyChanged();
                }
            }
        }

        public ClientStreamViewModel(IPEndPoint endpoint)
        {
            Endpoint = endpoint;
            DisplayName = $"Client - {endpoint.Address}:{endpoint.Port}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


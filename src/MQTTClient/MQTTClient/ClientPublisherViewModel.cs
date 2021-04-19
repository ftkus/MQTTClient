using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTClient
{
    public class ClientPublisherViewModel : INotifyPropertyChanged
    {
        public class MessageChangedEventArgs : EventArgs
        {
            public MessageChangedEventArgs(string topic, string message)
            {
                Topic = topic;
                Message = message;
            }

            public string Topic { get; }

            public string Message { get; }
        }

        private string topic;
        private string message;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<MessageChangedEventArgs> MessageChanged;

        public string Topic
        {
            get => topic;
            set
            {
                if (Equals(topic, value)) { return; }

                topic = value;

                NotifyPropertyChanged(nameof(Topic));
            }
        }

        public string Message
        { 
            get => message;
            set
            {
                if (Equals(message, value)) { return; }

                message = value;

                NotifyPropertyChanged(nameof(Message));

                if (!string.IsNullOrWhiteSpace(Topic))
                {
                    MessageChanged?.Invoke(this, new MessageChangedEventArgs(Topic, value));
                }
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

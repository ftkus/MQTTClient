using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTClient
{
    public class ClientSubscriberViewModel : INotifyPropertyChanged
    {
        public class TopicChangedEventArgs : EventArgs
        {
            public TopicChangedEventArgs(string oldTopic, string newTopic)
            {
                OldTopic = oldTopic;
                NewTopic = newTopic;
            }

            public string OldTopic { get; }
            public string NewTopic { get; }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<TopicChangedEventArgs> TopicChanged;

        private string topic;
        private string message;

        public string Topic
        {
            get => topic;
            set
            {
                if (Equals(topic, value)) { return; }

                string old = topic;

                topic = value;

                NotifyPropertyChanged(nameof(Topic));

                TopicChanged?.Invoke(this, new TopicChangedEventArgs(old, topic));
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
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

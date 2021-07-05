using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTClient.Data
{
    public class TagValueViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string tag;

        private DateTime timestamp;

        private double value;

        public TagValueViewModel(string tag, string topic)
        {
            Tag = tag;
            Topic = topic;
            Display = System.IO.Path.Combine(topic, tag);
        }

        public string Tag
        {
            get
            {
                return tag;
            }
            set
            {
                if (Equals(tag, value)) { return; }

                tag = value;

                NotifyPropertyChanged(nameof(Tag));
            }
        }

        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                if (Equals(this.value, value)) { return; }

                this.value = value;

                NotifyPropertyChanged(nameof(Value));
            }
        }

        public DateTime Timestamp
        {
            get
            {
                return timestamp;
            }
            set
            {
                if (Equals(timestamp, value)) { return; }

                timestamp = value;

                NotifyPropertyChanged(nameof(Timestamp));
            }
        }

        public string Display { get; }

        public string Topic { get; set; }

        public void Update(double value, DateTime timestamp)
        {
            Timestamp = timestamp;
            Value = value;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

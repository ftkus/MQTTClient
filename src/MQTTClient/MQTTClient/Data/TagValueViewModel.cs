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

        public DataSet DataSet { get; } = new DataSet();

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

        public string Topic { get; set; }

        public void Update(double value, DateTime timestamp)
        {
            DataSet.Points.Add(new DataPoint(timestamp, value));

            if (DataSet.Points.Count > 500)
            {
                DataSet.Points.RemoveAt(0);
            }

            Timestamp = timestamp;
            Value = value;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

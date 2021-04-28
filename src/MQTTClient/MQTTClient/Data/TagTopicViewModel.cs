using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTClient.Data
{
    public class TagTopicViewModel
    {
        public TagTopicViewModel(string topic, string tag)
        {
            Tag = tag;
            Topic = topic;

            Display = System.IO.Path.Combine(topic, tag);
        }

        public string Tag { get; }

        public string Topic { get; }

        public string Display { get; }
    }
}

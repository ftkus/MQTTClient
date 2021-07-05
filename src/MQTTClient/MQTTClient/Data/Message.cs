using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MQTTClient.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Message
    {
        [JsonProperty("connected")]
        public bool IsConnected { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string,double> Tags { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}

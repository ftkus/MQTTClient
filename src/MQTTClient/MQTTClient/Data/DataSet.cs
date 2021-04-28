using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTClient.Data
{
    public class DataSet
    {
        private List<DataPoint> points = new List<DataPoint>();

        public List<DataPoint> Points => points;
    }
}

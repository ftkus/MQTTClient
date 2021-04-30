using System;
using System.Net.Http.Headers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace MQTTClient
{
    public class DataBase
    {
        private InfluxDBClient client;
        private readonly string url;

        public DataBase(string address, int port, string token)
        {
            url = $"http://{address}:{port}";
            client = InfluxDBClientFactory.Create(url, token.ToCharArray());
        }

        public PointData GetPointData(string measurement, DateTime timestamp)
        {
            PointData retval = PointData.Measurement(measurement).Timestamp(timestamp.ToUniversalTime(), WritePrecision.Ns);

            return retval;
        }

        public PointData AppendFieldValue(PointData pointData, string field, double value)
        {
            return pointData.Field(field, value);
        }

        public void Write(string bucket, string org, PointData point)
        {
            using (var writeApi = client.GetWriteApi())
            {
                writeApi.WritePoint(bucket, org, point);
            }
        }

        public void Diconnect()
        {
            client.Dispose();
            client = null;
        }
    }
}
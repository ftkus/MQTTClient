using System;
using System.Net.Http.Headers;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;

namespace MQTTClient
{
    public class DataBase
    {
        public class QueryCompleteEventArgs : EventArgs
        {
            public QueryCompleteEventArgs(FluxRecord result)
            {
                Result = result;
            }

            public FluxRecord Result { get; }
        }

        public class QueryExceptionEventArgs : EventArgs
        {
            public QueryExceptionEventArgs(Exception exception)
            {
                Exception = exception;
            }

            public Exception Exception { get; }
        }

        public class QuerySuccessEventArgs : EventArgs { }
        

        public event EventHandler<QueryCompleteEventArgs> QueryComplete;
        public event EventHandler<QueryExceptionEventArgs> QueryException;
        public event EventHandler<QuerySuccessEventArgs> QuerySuccess;

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

        public async void Query(string bucket, string org)
        {
            string flux = $"from(bucket:\"{bucket}\") |> range(start: 0)";

            var queryApi = client.GetQueryApi();

            await queryApi.QueryAsync(flux, org, (cancellable, record) =>
                {
                    QueryComplete?.Invoke(this, new QueryCompleteEventArgs(record));
                },
                exception =>
                {
                    QueryException?.Invoke(this, new QueryExceptionEventArgs(exception));
                },
                () =>
                {
                    QuerySuccess?.Invoke(this, new QuerySuccessEventArgs());
                });
        }

        public void Diconnect()
        {
            client.Dispose();
            client = null;
        }
    }
}
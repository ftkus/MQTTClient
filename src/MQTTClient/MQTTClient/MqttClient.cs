using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using MQTTClient.Data;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;

namespace MQTTClient
{
    public class MqttClient
    {
        public class MessageReceivedEventArgs : EventArgs
        {
            public MessageReceivedEventArgs(string topic, string payload)
            {
                Topic = topic;
                Payload = payload;
            }

            public string Topic { get; }

            public string Payload { get; }
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private IManagedMqttClient subscriber;
        private string name;
        private Log log;
        private string server;
        private int port;

        private MqttClientTlsOptions tlsOptions;

        private string username = "username";
        private string password = "password";

        public MqttClient(string name, string server, int port, Log log)
        {
            this.name = name;
            this.server = server;
            this.port = port;
            this.log = log;

            tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true,
            };
        }

        public void UseTls(List<X509Certificate> certificates)
        {
            tlsOptions.UseTls = true;
            tlsOptions.Certificates = certificates;
        }

        public void UseAuthentication(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public async void Start()
        {
            MqttFactory factory = new MqttFactory();

            var options = new MqttClientOptions
            {
                ClientId = name,
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = server,
                    Port = port,
                    TlsOptions = tlsOptions
                }
            };

            if (options.ChannelOptions == null)
            {
                throw new InvalidOperationException();
            }

            options.Credentials = new MqttClientCredentials
            {
                Username = username,
                Password = Encoding.UTF8.GetBytes(password)
            };

            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(5);

            subscriber = factory.CreateManagedMqttClient();
            subscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(Client_OnSubscriberConnected);
            subscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(Client_OnSubscriberDisconnected);
            subscriber.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(Client_OnSubscriberMessageReceived);
            await subscriber.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = options
                });

            if (subscriber is null)
            {
                throw new InvalidOperationException();
            }
        }

        public async void Stop()
        {
            try
            {
                await subscriber?.StopAsync();
            }
            catch (NullReferenceException)
            {
                //Do nothing
            }
        }

        public async void Subscribe(string topic)
        {
            try
            {
                await subscriber?.SubscribeAsync(topic);
            }
            catch (NullReferenceException)
            {
                //Do nothing
            }
        }

        public async void Unsubscribe(string topic)
        {
            try
            {
                await subscriber?.UnsubscribeAsync(topic);
            }
            catch (NullReferenceException)
            {
                //Do nothing
            }
        }

        private void Client_OnSubscriberConnected(MqttClientConnectedEventArgs e)
        {
            log.Add($"{name} Connected");
        }

        private void Client_OnSubscriberDisconnected(MqttClientDisconnectedEventArgs e)
        {
            log.Add($"{name} Disconnected");
        }

        private void Client_OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString()));
        }
    }
}
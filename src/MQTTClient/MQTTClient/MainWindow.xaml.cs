using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;

using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Management;
using System.Collections.ObjectModel;
using System.Timers;

using System.Security.Cryptography.X509Certificates;

using MQTTClient.Data;
using Newtonsoft.Json;

namespace MQTTClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isConnected;

        private string server;

        private int port;

        private bool useTls;

        private bool useAuth;

        private string username;

        private string certPath;

        private IManagedMqttClient clientSubscriber;
        private IManagedMqttClient clientPublisher;

        private Log log;

        private string logContent;

        private string clientName;

        public MainWindow()
        {
            ClientName = GetClientName();

            log = new Log();
            log.Updated += Log_Updated;

            Server = Properties.Settings.Default.Server;
            Port = Properties.Settings.Default.Port;
            CertPath = Properties.Settings.Default.CertPath;
            UseTls = Properties.Settings.Default.UseTls;
            UseAuth = Properties.Settings.Default.UseAuth;
            Username = Properties.Settings.Default.Username;

            Message msg = new Message()
            {
                IsConnected = false,
                Timestamp = DateTime.Now,
                Tags = new Dictionary<string, double>() { { "Sine", 3.452 }, { "Step", 1.234 }, { "Ramp", 2.974 } }
            };

            string output = JsonConvert.SerializeObject(msg);

            log.Add(output);

            InitializeComponent();
        }

        public string LogContent
        {
            get
            {
                return logContent;
            }
            set
            {
                if (Equals(logContent, value)) { return; }

                logContent = value;

                NotifyPropertyChanged(nameof(LogContent));
            }
        }

        public string ClientName
        {
            get
            {
                return clientName;
            }
            set
            {
                if (Equals(clientName, value)) { return; }

                clientName = value;

                NotifyPropertyChanged(nameof(ClientName));
            }
        }
        
        public string Server
        {
            get
            {
                return server;
            }
            set
            {
                if (Equals(value, server)) { return; }

                server = value;

                NotifyPropertyChanged(nameof(Server));
            }
        }

        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                if (Equals(value, port)) { return; }

                port = value;

                NotifyPropertyChanged(nameof(Port));
            }
        }

        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                if (Equals(value, isConnected)) { return; }

                isConnected = value;

                NotifyPropertyChanged(nameof(IsConnected));
            }
        }

        public bool UseTls
        {
            get
            {
                return useTls;
            }
            set
            {
                if (Equals(useTls, value)) { return; }

                useTls = value;

                NotifyPropertyChanged(nameof(UseTls));
            }
        }

        public bool UseAuth
        {
            get
            {
                return useAuth;
            }
            set
            {
                if (Equals(useAuth, value)) { return; }

                useAuth = value;

                NotifyPropertyChanged(nameof(UseAuth));
            }
        }

        public string Username
        { 
            get
            {
                return username;
            }
            set
            {
                if (Equals(username, value)) { return; }

                username = value;

                NotifyPropertyChanged(nameof(Username));
            }
        }

        public string CertPath
        {
            get
            {
                return certPath;
            }
            set
            {
                if (Equals(certPath, value)) { return; }

                certPath = value;

                NotifyPropertyChanged(nameof(CertPath));
            }
        }

        public string ClientSub => $"{ClientName}_sub";

        private string GetClientName()
        {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                cpuInfo = mo.Properties["processorID"].Value.ToString();
                break;
            }

            return cpuInfo;
        }

        private List<X509Certificate> GetCerts()
        {
            List<X509Certificate> certs = new List<X509Certificate>
            {
                new X509Certificate2(CertPath),
            };

            return certs;
        }

        private void UpdateTopic(string topic, string paylod)
        {
           
        }

        private bool CompareTopics(string localTopic, string receivedTopic)
        {
            const char TopicLevelSeparator = '/';
            const string Wildcard = "#";

            string[] localTopicArray = localTopic.Split(TopicLevelSeparator);
            string[] receivedTopicArray = receivedTopic.Split(TopicLevelSeparator);

            for(int i = 0; i < receivedTopicArray.Length; i++)
            {
                if (localTopicArray.Length <= i) { return false; }

                if (string.Equals(localTopicArray[i], Wildcard)) { return true; }

                if (string.Equals(localTopicArray[i], receivedTopicArray[i]))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButConnect_OnClick(object sender, RoutedEventArgs e)
        {
            var mqttFactory = new MqttFactory();

            clientSubscriber = StartClientSubscriber(mqttFactory, Server, Port).Result;

            IsConnected = true;

            Properties.Settings.Default.Port = Port;
            Properties.Settings.Default.Server = Server;
            Properties.Settings.Default.UseAuth = UseAuth;
            Properties.Settings.Default.Username = Username;
            Properties.Settings.Default.UseTls = UseTls;
            Properties.Settings.Default.CertPath = CertPath;
            Properties.Settings.Default.Save();
        }

        private async void ButDisconnect_OnClick(object sender, RoutedEventArgs e)
        {
            await clientSubscriber?.StopAsync();
            await clientPublisher?.StopAsync();

            clientSubscriber = null;
            clientPublisher = null;

            IsConnected = false;
        }

        private async Task<IManagedMqttClient> StartClientSubscriber(MqttFactory factory, string server, int port)
        {
            var certs = GetCerts();

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = this.UseTls,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true,
                Certificates = certs,
            };

            var options = new MqttClientOptions
            {
                ClientId = ClientSub,
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
                Username = UseAuth ? Username : "username",
                Password = UseAuth ? Encoding.UTF8.GetBytes(pwBox.Password) : Encoding.UTF8.GetBytes("password")
            };

            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(5);


            IManagedMqttClient managedMqttClientSubscriber = factory.CreateManagedMqttClient();
            managedMqttClientSubscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(Client_OnSubscriberConnected);
            managedMqttClientSubscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(Client_OnSubscriberDisconnected);
            managedMqttClientSubscriber.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(Client_OnSubscriberMessageReceived);
            await managedMqttClientSubscriber.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = options
                });

            return managedMqttClientSubscriber;
        }

        private void Client_OnSubscriberConnected(MqttClientConnectedEventArgs e)
        {
           log.Add($"{ClientSub} Connected");
        }

        private void Client_OnSubscriberDisconnected(MqttClientDisconnectedEventArgs e)
        {
            log.Add($"{ClientSub} Disconnected");
        }

        private void Client_OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {e.ApplicationMessage.Topic} | Payload: {e.ApplicationMessage.ConvertPayloadToString()} | QoS: {e.ApplicationMessage.QualityOfServiceLevel}";

            log.Add(item);

            UpdateTopic(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
        }

        private void Log_Updated(object sender, Log.LogEventArgs e)
        {
            LogContent = e.Log.ToString();
        }

        private void ButBrowseCert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.FileName = CertPath;

            var result = ofd.ShowDialog();

            if (result == true)
            {
                CertPath = ofd.FileName;
            }
        }
    }
}

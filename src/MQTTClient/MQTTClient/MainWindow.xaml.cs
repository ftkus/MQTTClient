﻿using System;
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

        private ObservableCollection<ClientPublisherViewModel> clientPublisherViewModels;

        private ObservableCollection<ClientSubscriberViewModel> clientSubscriberViewModels;

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


            ClientPublisherViewModels = new ObservableCollection<ClientPublisherViewModel>();
            ClientSubscriberViewModels = new ObservableCollection<ClientSubscriberViewModel>();

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

        public string ClientSub => $"{ClientName}.sub";

        public string ClientPub => $"{ClientName}.pub";

        public ObservableCollection<ClientPublisherViewModel> ClientPublisherViewModels
        {
            get => clientPublisherViewModels;
            set
            {
                if (Equals(clientPublisherViewModels, value))
                {
                    return;
                }

                clientPublisherViewModels = value;

                NotifyPropertyChanged(nameof(ClientPublisherViewModels));
            }
        }

        public ObservableCollection<ClientSubscriberViewModel> ClientSubscriberViewModels
        {
            get => clientSubscriberViewModels;
            set
            {
                if (Equals(clientSubscriberViewModels, value))
                {
                    return;
                }

                clientSubscriberViewModels = value;

                NotifyPropertyChanged(nameof(clientSubscriberViewModels));
            }
        }

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
            foreach (var vm in ClientSubscriberViewModels)
            {
                if (CompareTopics(vm.Topic, topic))
                {
                    vm.Message = paylod;
                }
            }
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
            clientPublisher = StartClientPublisher(mqttFactory, Server, Port).Result;

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

        private async Task<IManagedMqttClient> StartClientPublisher(MqttFactory factory, string server, int port)
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
                ClientId = ClientPub,
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


            IManagedMqttClient managedMqttClientPublisher = factory.CreateManagedMqttClient();
            managedMqttClientPublisher.UseApplicationMessageReceivedHandler(Client_HandleReceivedApplicationMessage);
            managedMqttClientPublisher.ConnectedHandler = new MqttClientConnectedHandlerDelegate(Client_OnPublisherConnected);
            managedMqttClientPublisher.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(Client_OnPublisherDisconnected);
            await managedMqttClientPublisher.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = options
                });

            return managedMqttClientPublisher;
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

        private void Client_OnPublisherConnected(MqttClientConnectedEventArgs e)
        {
            log.Add($"{ClientPub} Connected");
        }

        private void Client_OnPublisherDisconnected(MqttClientDisconnectedEventArgs e)
        {
            log.Add($"{ClientPub} Disconnected");
        }

        private void Client_HandleReceivedApplicationMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {e.ApplicationMessage.Topic} | Payload: {e.ApplicationMessage.ConvertPayloadToString()} | QoS: {e.ApplicationMessage.QualityOfServiceLevel}";

            log.Add(item);
        }

        private void Log_Updated(object sender, Log.LogEventArgs e)
        {
            LogContent = e.Log.ToString();
        }

        private void MiPublisherAdd_Click(object sender, RoutedEventArgs e)
        {
            ClientPublisherViewModel newVM = new ClientPublisherViewModel();
            newVM.MessageChanged += PublisherVM_MessageChanged;

            ClientPublisherViewModels.Add(newVM);
        }

        private void PublisherVM_MessageChanged(object sender, ClientPublisherViewModel.MessageChangedEventArgs e)
        {
            if (clientPublisher is null) { return; }

            if (string.IsNullOrWhiteSpace(e.Topic)) { return; }
            if (string.IsNullOrWhiteSpace(e.Message)) { return; }

            clientPublisher?.PublishAsync(e.Topic, e.Message);
        }

        private void MiSubscriberAdd_Click(object sender, RoutedEventArgs e)
        {
            ClientSubscriberViewModel newVM = new ClientSubscriberViewModel();
            newVM.TopicChanged += SubscriberVM_TopicChanged;

            ClientSubscriberViewModels.Add(newVM);
        }

        private void SubscriberVM_TopicChanged(object sender, ClientSubscriberViewModel.TopicChangedEventArgs e)
        {
            if (clientSubscriber is null) { return; }

            //Unsubscribe old
            if (!string.IsNullOrWhiteSpace(e.OldTopic))
            {
                clientSubscriber.UnsubscribeAsync(e.OldTopic);
            }

            //Subscribe new
            if (!string.IsNullOrWhiteSpace(e.NewTopic))
            {
                clientSubscriber.SubscribeAsync(e.NewTopic);
            }
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

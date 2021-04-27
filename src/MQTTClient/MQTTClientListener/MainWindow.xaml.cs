﻿using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
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

namespace MQTTClientListener
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private IManagedMqttClient client;
        private IManagedMqttClient clientPublisher;

        private bool isConnected;

        private string server;

        private int port;

        private string clientName;

        private ObservableCollection<string> messages;

        public MainWindow()
        {
            Port = Properties.Settings.Default.Port;
            Server = Properties.Settings.Default.Server;

            Messages = new ObservableCollection<string>();

            clientName = GetClientName();

            InitializeComponent();
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
                if (Equals(isConnected, value)) { return; }

                isConnected = value;

                NotifyPropertyChanged(nameof(IsConnected));
            }
        }

        public ObservableCollection<string> Messages
        {
            get
            {
                return messages;
            }
            set
            {
                if (Equals(messages, value)) { return; }

                messages = value;

                NotifyPropertyChanged(nameof(Messages));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private async Task<IManagedMqttClient> StartClientSubscriber(MqttFactory factory, string server, int port, string name)
        {
            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = false,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true
            };

            var options = new MqttClientOptions
            {
                ClientId = $"{name}.listener",
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
                Username = "username",
                Password = Encoding.UTF8.GetBytes("password")
            };

            options.CleanSession = true;
            options.KeepAlivePeriod = TimeSpan.FromSeconds(5);


            IManagedMqttClient managedMqttClientSubscriber = factory.CreateManagedMqttClient();
            managedMqttClientSubscriber.ConnectedHandler = new MqttClientConnectedHandlerDelegate(Client_OnClientConnected);
            managedMqttClientSubscriber.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(Client_OnClientDisconnected);
            managedMqttClientSubscriber.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(Client_OnSubscriberMessageReceived);
            await managedMqttClientSubscriber.StartAsync(
                new ManagedMqttClientOptions
                {
                    ClientOptions = options
                });

            return managedMqttClientSubscriber;
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            var mqttFactory = new MqttFactory();

            client = StartClientSubscriber(mqttFactory, Server, Port, clientName).Result;

            IsConnected = true;

            Properties.Settings.Default.Port = Port;
            Properties.Settings.Default.Server = Server;
            Properties.Settings.Default.Save();

            await client.SubscribeAsync("#");
        }

        private async void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            await client?.StopAsync();

            client = null;

            IsConnected = false;
        }

        private void Client_OnClientConnected(MqttClientConnectedEventArgs e)
        {
            Dispatcher.Invoke(() => Messages.Add($"{clientName} Connected"));
        }

        private void Client_OnClientDisconnected(MqttClientDisconnectedEventArgs e)
        {
            Dispatcher.Invoke(() => Messages.Add($"{clientName} Disconnected"));

        }

        private void Client_OnSubscriberMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var item = $"Timestamp: {DateTime.Now:O} | Topic: {e.ApplicationMessage.Topic} | Payload: {e.ApplicationMessage.ConvertPayloadToString()} | QoS: {e.ApplicationMessage.QualityOfServiceLevel}";

            Dispatcher.Invoke(() => Messages.Add(item));
        }

        private void MiCopy_OnClick(object sender, RoutedEventArgs e)
        {
            var msgs = Messages.ToArray();

            if (!msgs.Any())
            {
                return;
            }

            using (StringWriter sw = new StringWriter())
            {
                foreach (var msg in msgs)
                {
                    sw.WriteLine(msg);
                }

                try
                {
                    Clipboard.Clear();
                    Clipboard.SetText(sw.ToString().Trim());
                }
                catch (Exception)
                {
                    //Clipboard exception
                    //Do nothing
                }
            }
        }

        private void MiClear_OnClick(object sender, RoutedEventArgs e)
        {
            Messages.Clear();
        }
    }
}

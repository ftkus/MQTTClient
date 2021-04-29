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

using OxyPlot;
using OxyPlot.Series;
using DataPoint = OxyPlot.DataPoint;

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

        private Log log;

        private string logContent;

        private string clientName;

        private string searchTopic;

        private ObservableCollection<TagTopicViewModel> tags;

        private ObservableCollection<TagValueViewModel> tagValueViewModels;

        private TagTopicViewModel selectedTag;

        private DateTime? firstTimeStamp = null;

        public MainWindow()
        {
            ClientName = GetClientName();

            log = new Log();
            log.Updated += Log_Updated;

            Tags = new ObservableCollection<TagTopicViewModel>();

            TagValueViewModels = new ObservableCollection<TagValueViewModel>();

            Server = Properties.Settings.Default.Server;
            Port = Properties.Settings.Default.Port;
            CertPath = Properties.Settings.Default.CertPath;
            UseTls = Properties.Settings.Default.UseTls;
            UseAuth = Properties.Settings.Default.UseAuth;
            Username = Properties.Settings.Default.Username;

            MyModel = new PlotModel { Title = "Data Preview" };

            InitializeComponent();

            Timer chartTimer = new Timer();
            chartTimer.Elapsed += ChartTimer_OnElapsed;
            chartTimer.Interval = 5000;
            chartTimer.Start();
        }

        private void ChartTimer_OnElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher?.Invoke(() => plot.InvalidatePlot());
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

        public string SearchTopic
        {
            get
            {
                return searchTopic;
            }
            set
            {
                if (Equals(value, searchTopic)) { return; }

                string oldTopic = searchTopic;

                searchTopic = value;

                NotifyPropertyChanged(nameof(SearchTopic));

                SearchTopicChanged(oldTopic);
            }
        }

        public ObservableCollection<TagValueViewModel> TagValueViewModels
        {
            get
            {
                return tagValueViewModels;
            }
            set
            {
                if (Equals(tagValueViewModels, value)) { return; }

                tagValueViewModels = value;

                NotifyPropertyChanged(nameof(TagValueViewModels));
            }
        }

        public TagTopicViewModel SelectedTag
        {
            get
            {
                return selectedTag;
            }
            set
            {
                if (Equals(value, selectedTag)) { return; }

                selectedTag = value;

                NotifyPropertyChanged(nameof(SelectedTag));
            }
        }

        public ObservableCollection<TagTopicViewModel> Tags
        {
            get
            {
                return tags;
            }
            set
            {
                if (Equals(value, tags)) { return; }

                tags = value;

                NotifyPropertyChanged(nameof(Tags));
            }
        }

        public string ClientSub => $"{ClientName}_sub";

        public PlotModel MyModel { get; private set; }

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

        private void UpdateTopicTags(string topic, string payload)
        {
            object deserializedObject = JsonConvert.DeserializeObject<Message>(payload);

            if (deserializedObject is Message msg)
            {
                foreach(var m in msg.Tags)
                {
                    if (Tags.Any(t => t.Topic == topic && t.Tag == m.Key))
                    {
                        continue;
                    }

                    Dispatcher.Invoke(() => Tags.Add(new TagTopicViewModel(topic, m.Key)));
                }
            }
        }

        private bool CompareTopics(string localTopic, string receivedTopic)
        {
            const char TopicLevelSeparator = '/';
            const string Wildcard = "#";

            string[] localTopicArray = localTopic.Split(TopicLevelSeparator);
            string[] receivedTopicArray = receivedTopic.Split(TopicLevelSeparator);

            for (int i = 0; i < receivedTopicArray.Length; i++)
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

        private void UpdateData(string topic, string payload)
        {
            object deserializedObject = JsonConvert.DeserializeObject<Message>(payload);

            if (deserializedObject is Message msg)
            {
                foreach (var tvvm in TagValueViewModels)
                {
                    if (tvvm.Topic == topic)
                    {
                        foreach (var tag in msg.Tags)
                        {
                            if (tvvm.Tag == tag.Key)
                            {
                                if (firstTimeStamp is null)
                                {
                                    firstTimeStamp = msg.Timestamp;
                                }

                                tvvm.Update(tag.Value, msg.Timestamp);

                                foreach (var s in MyModel.Series)
                                {
                                    if (s.Tag == tvvm)
                                    {
                                        ((LineSeries) s).Points.Add(new DataPoint((msg.Timestamp - firstTimeStamp.Value).TotalSeconds, tag.Value));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private async void SearchTopicChanged(string oldTopic)
        {
            if (!string.IsNullOrWhiteSpace(oldTopic))
            {
                if (!TagValueViewModels.Any(t => t.Topic == oldTopic))
                {
                    await clientSubscriber?.UnsubscribeAsync(oldTopic);

                    log.Add($"{DateTime.Now}: Unsubscribed from topic {oldTopic}");
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchTopic))
            {
                if (!TagValueViewModels.Any(t => t.Topic == SearchTopic))
                {
                    await clientSubscriber?.SubscribeAsync(SearchTopic);

                    log.Add($"{DateTime.Now}: Subscribed to topic {SearchTopic}");
                }
            }

            Tags = new ObservableCollection<TagTopicViewModel>();
        }

        private void AddTag(string topic, string tag)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (TagValueViewModels.Any(t => t.Tag == tag && t.Topic == topic)) { return; }

            var tvvm = new TagValueViewModel()
            {
                Tag = tag,
                Topic = topic,
            };

            var ls = new LineSeries();
            ls.Tag = tvvm;
            ls.Title = tag;

            MyModel.Series.Add(ls);

            TagValueViewModels.Add(tvvm);

            log.Add($"{DateTime.Now}: Subscribed to item {tag} on topic {topic}");
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButConnect_OnClick(object sender, RoutedEventArgs e)
        {
            var mqttFactory = new MqttFactory();

            clientSubscriber = StartClientSubscriber(mqttFactory, Server, Port).Result;

            if (clientSubscriber is null)
            {
                return;
            }

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

            clientSubscriber = null;

            IsConnected = false;
        }

        private async Task<IManagedMqttClient> StartClientSubscriber(MqttFactory factory, string server, int port)
        {

            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = this.UseTls,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
                AllowUntrustedCertificates = true,
            };

            try
            {
                if (UseTls)
                {
                    var certs = GetCerts();
                    tlsOptions.Certificates = certs;
                }
            }
            catch (Exception ex)
            {
                log.Add($"{DateTime.Now}: Invalid certificate: {ex.Message}");

                return null;
            }

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
            if (CompareTopics(SearchTopic, e.ApplicationMessage.Topic))
            {
                UpdateTopicTags(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
            }

            UpdateData(e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString());
        }

        private void Log_Updated(object sender, Log.LogEventArgs e)
        {
            LogContent = e.Log.ToString();
            Dispatcher.Invoke(() => logListBox.ScrollToEnd());
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

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedTag is null) { return; }

            AddTag(SelectedTag.Topic, SelectedTag.Tag);
        }
    }
}

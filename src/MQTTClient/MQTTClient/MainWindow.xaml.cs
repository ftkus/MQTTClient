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
using MQTTClient.Properties;
using Newtonsoft.Json;

using OxyPlot;
using OxyPlot.Series;
using DataPoint = OxyPlot.DataPoint;
using MessageBox = System.Windows.Forms.MessageBox;

namespace MQTTClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isConnected;
        private bool isDBConnected;
        private bool useTls;
        private bool useAuth;
        private int port;
        private int dbPort;
        private string username;
        private string certPath;
        private string server;
        private string logContent;
        private string clientName;
        private string searchTopic;
        private string dbAddress;
        private string dbOrg;
        private string dbBucket;
        private string dbToken;
        private MqttClient client;
        private Log log;

        private ObservableCollection<TagTopicViewModel> tags;
        private ObservableCollection<TagValueViewModel> tagValueViewModels;

        private TagTopicViewModel selectedTag;

        private DateTime? firstTimeStamp = null;

        private DataBase influxDB;

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
            DBAddress = Properties.Settings.Default.DBAddress;
            DBPort = Properties.Settings.Default.DBPort;
            DBOrg = Properties.Settings.Default.DBOrg;
            DBBucket = Properties.Settings.Default.DBBucket;
            DBToken = Properties.Settings.Default.DBToken;

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

        public string DBAddress
        {
            get
            {
                return dbAddress;
            }
            set
            {
                if (Equals(value, dbAddress)) { return; }

                dbAddress = value;

                NotifyPropertyChanged(nameof(DBAddress));
            }
        }

        
        public int DBPort
        {
            get
            {
                return dbPort;
            }
            set
            {
                if (Equals(value, dbPort)) { return; }

                dbPort = value;

                NotifyPropertyChanged(nameof(DBPort));
            }
        }

        public string DBToken
        {
            get
            {
                return dbToken;
            }
            set
            {
                if (Equals(value, dbToken)) { return; }

                dbToken = value;

                NotifyPropertyChanged(nameof(DBToken));
            }
        }

        public string DBOrg
        {
            get
            {
                return dbOrg;
            }
            set
            {
                if (Equals(value, dbOrg)) { return; }

                dbOrg = value;

                NotifyPropertyChanged(nameof(DBOrg));
            }
        }

        public string DBBucket
        {
            get
            {
                return dbBucket;
            }
            set
            {
                if (Equals(value, dbBucket)) { return; }

                dbBucket = value;

                NotifyPropertyChanged(nameof(DBBucket));
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

        public bool IsDBConnected
        {
            get
            {
                return isDBConnected;
            }
            set
            {
                if (Equals(value, isDBConnected)) { return; }

                isDBConnected = value;

                NotifyPropertyChanged(nameof(IsDBConnected));
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
                            }
                        }
                    }
                }

                if (IsDBConnected)
                {
                    var pointData = influxDB.GetPointData(topic, msg.Timestamp);

                    foreach (var tag in msg.Tags)
                    {
                        pointData = influxDB.AppendFieldValue(pointData, tag.Key, tag.Value);
                    }

                    influxDB.Write(DBBucket, DBOrg, pointData);
                }
            }
        }

        private async void SearchTopicChanged(string oldTopic)
        {
            if (!string.IsNullOrWhiteSpace(oldTopic))
            {
                if (!TagValueViewModels.Any(t => t.Topic == oldTopic))
                {
                    client.Unsubscribe(oldTopic);

                    log.Add($"{DateTime.Now}: Unsubscribed from topic {oldTopic}");
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchTopic))
            {
                if (!TagValueViewModels.Any(t => t.Topic == SearchTopic))
                {
                    client.Subscribe(SearchTopic);

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

            var tvvm = new TagValueViewModel(tag, topic);

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

            if (string.IsNullOrWhiteSpace(server))
            {
                MessageBox.Show("Invalid Mqtt Server");
                return;
            }

            if (port < 1024 || port > 65535)
            {
                MessageBox.Show("Invalid Mqtt Port");
                return;
            }

            client = new MqttClient(ClientSub, server, port, log);

            if (UseAuth)
            {
                client.UseAuthentication(Username, pwBox.Password);
            }

            if (UseTls)
            {
                try
                {
                    client.UseTls(GetCerts());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid certificate {ex.Message}");
                    return;
                }
            }

            client.MessageReceived += (o, args) =>
            {
                if (CompareTopics(SearchTopic, args.Topic))
                {
                    UpdateTopicTags(args.Topic, args.Payload);
                }

                UpdateData(args.Topic, args.Payload);
            };

            try
            {
                client.Start();
            }
            catch (InvalidOperationException)
            {
                log.Add($"{DateTime.Now}: Unable to start client {ClientSub}");
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
            client.Stop();

            client = null;

            IsConnected = false;
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

        private void ButDBConnect_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DBAddress))
            {
                MessageBox.Show("Invalid InfluxDB parameters");
                return;
            }

            if (string.IsNullOrWhiteSpace(DBOrg))
            {
                MessageBox.Show("Invalid InfluxDB parameters");
                return;
            }

            if (string.IsNullOrWhiteSpace(DBBucket))
            {
                MessageBox.Show("Invalid InfluxDB parameters");
                return;
            }

            if (string.IsNullOrWhiteSpace(DBToken))
            {
                MessageBox.Show("Invalid InfluxDB parameters");
                return;
            }

            if (DBPort < 1024 || DBPort > 65535)
            {
                MessageBox.Show("Invalid InfluxDB parameters");
                return;
            }

            influxDB = new DataBase(DBAddress, DBPort, DBToken);

            Properties.Settings.Default.DBAddress = DBAddress;
            Properties.Settings.Default.DBPort = DBPort;
            Properties.Settings.Default.DBBucket = DBBucket;
            Properties.Settings.Default.DBOrg = DBOrg;
            Properties.Settings.Default.DBToken = DBToken;
            Properties.Settings.Default.Save();

            IsDBConnected = true;
        }

        private void ButDBDisconnect_OnClick(object sender, RoutedEventArgs e)
        {
            influxDB?.Diconnect();
            influxDB = null;

            IsDBConnected = false;
        }
    }
}

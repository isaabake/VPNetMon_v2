using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using System.Xml.Serialization;

namespace VPNetMon_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            DataContext = this;
            VPNConnected = false;

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    List<string> ipaddresses = GetAllIPAdresses();
                    this.Dispatcher.Invoke(() =>
                    {
                        CurrentIPAddresses.Clear();
                        foreach (string s in ipaddresses)
                        {
                            CurrentIPAddresses.Add(s);
                        }
                    });
                    CheckVPNIPInAddresses();
                    Thread.Sleep(_selectedRefreshRate * 1000);
                }
            });

            LoadPrograms();
            InitializeComponent();
            VPNStatus = "Disconnected";
        }

        private void CheckVPNIPInAddresses()
        {
            if (VPNIPAddress.Contains('.') && CurrentIPAddresses.Any(ip => ip.StartsWith(VPNIPAddress)))
            {
                VPNConnected = true;
            }
            else
            {
                VPNConnected = false;
            }
        }

        private List<string> GetAllIPAdresses()
        {
            List<string> IPAddresses = new List<string>();

            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netif.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = netif.GetIPProperties();

                    foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                    {
                        if (!unicast.Address.IsIPv6Teredo && !unicast.Address.IsIPv6LinkLocal && !unicast.Address.IsIPv6Multicast && !unicast.Address.ToString().Contains("::1") && !unicast.Address.ToString().Contains("127.0.0.1"))
                        {
                            IPAddresses.Add(unicast.Address.ToString());
                        }

                    }
                }
            }

            return IPAddresses;
        }

        private void KillProcesses()
        {
            foreach (string program in VPNPrograms)
            {
                Process[] p = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(program));
                foreach (Process proc in p)
                {
                    proc.Kill();
                }
            }
        }

        private void StartProcesses()
        {
            foreach (string program in VPNPrograms)
            {
                Process.Start(program);
            }
        }

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private ObservableCollection<string> _VPNPrograms = new ObservableCollection<string>();
        public ObservableCollection<string> VPNPrograms
        {
            get { return _VPNPrograms; }
            set { _VPNPrograms = value; }
        }


        private bool _VPNConnected;
        public bool VPNConnected
        {
            get { return _VPNConnected; }
            set
            {
                if (value == false && VPNConnected == true)
                {
                    KillProcesses();
                    this.Dispatcher.Invoke(() => VPNStatus = "Disconnected");
                }
                else if (value == true && VPNConnected == false)
                {
                    StartProcesses();
                    this.Dispatcher.Invoke(() => VPNStatus = "Connected");
                }
                _VPNConnected = value;
                this.OnPropertyChanged("VPNConnected");
            }
        }

        private string _VPNStatus;
        public string VPNStatus
        {
            get { return _VPNStatus; }
            set
            {
                _VPNStatus = value;
                BindingOperations.GetBindingExpressionBase(this.StatusLabel, Label.ContentProperty).UpdateTarget();
            }
        }


        private ObservableCollection<string> _refreshRates = new ObservableCollection<string>() { "1", "2", "5", "10", "30" };
        public ObservableCollection<string> RefreshRates
        {
            get { return _refreshRates; }
            set { _refreshRates = value; }
        }

        private int _selectedRefreshRate = 2;
        public string SelectedRefreshRate
        {
            get { return _selectedRefreshRate.ToString(); }
            set
            {
                int.TryParse(value, out _selectedRefreshRate);
            }
        }

        private string _VPNIPAddress = string.Empty;
        public string VPNIPAddress
        {
            get { return _VPNIPAddress; }
            set { _VPNIPAddress = value; }
        }

        private ObservableCollection<string> _currentIPAddresses = new ObservableCollection<string>();
        public ObservableCollection<string> CurrentIPAddresses
        {
            get { return _currentIPAddresses; }
            set
            {
                _currentIPAddresses = value;
            }
        }

        private string _selectedProgram;
        public string SelectedProgram
        {
            get { return _selectedProgram; }
            set { _selectedProgram = value; }
        }


        private void AddProgram_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;

            bool? userClickedOK = openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                VPNPrograms.Add(openFileDialog.FileName);
                SavePrograms();
            }

        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            CheckVPNIPInAddresses();
            //TextBox tBox = (TextBox)sender;
            //DependencyProperty prop = TextBox.TextProperty;

            //BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
            //if (binding != null)
            //{
            //    binding.UpdateSource();
            //}
        }

        private void RemoveProgram_Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProgram != null)
            {
                VPNPrograms.Remove(SelectedProgram);
                SavePrograms();
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            KillProcesses();
        }

        private void SavePrograms()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
            using (TextWriter writer = new StreamWriter("VPNetMonProgramsList.xml"))
            {
                serializer.Serialize(writer, VPNPrograms.ToList());
                writer.Close();
            }
        }

        private void LoadPrograms()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
            try
            {
                using (TextReader reader = new StreamReader("VPNetMonProgramsList.xml"))
                {
                    foreach (string prog in (List<string>)serializer.Deserialize(reader))
                    {
                        VPNPrograms.Add(prog);
                    }
                    reader.Close();
                }
            }
            catch (Exception ex) { }
        }



    }
}

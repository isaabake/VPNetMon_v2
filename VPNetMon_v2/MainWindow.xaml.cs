using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace VPNetMon_v2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            VPNConnected = false;
            KillWatchedProcesses();

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        CurrentIPAddresses.Clear();
                        GetAllIPAdresses();
                    });
                    if (CurrentIPAddresses.Contains(VPNIPAddress))
                    {
                        VPNConnected = true;
                    }
                    else
                    {
                        VPNConnected = false;
                    }
                    Thread.Sleep(_selectedRefreshRate * 1000);
                }
            });

        }

        private void GetAllIPAdresses()
        {
            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties properties = netif.GetIPProperties();

                foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (!unicast.Address.IsIPv6Teredo && !unicast.Address.IsIPv6LinkLocal && !unicast.Address.IsIPv6Multicast && !unicast.Address.ToString().Contains("::1") && !unicast.Address.ToString().Contains("127.0.0.1"))
                    {
                        CurrentIPAddresses.Add(unicast.Address.ToString());
                    }

                }

            }
        }

        private void KillWatchedProcesses()
        {
            foreach (string programname in VPNPrograms)
            {
                Process[] p = Process.GetProcessesByName(programname);
                foreach (Process proc in p)
                {
                    proc.Kill();
                }
            }

        }

        private ObservableCollection<string> _VPNPrograms = new ObservableCollection<string>() {"mstsc"};

        public ObservableCollection<string> VPNPrograms
        {
            get { return _VPNPrograms; }
            set { _VPNPrograms = value; }
        }


        private bool _VPNConnected;
        public bool VPNConnected
        {
            get { return _VPNConnected; }
            set { _VPNConnected = value; }
        }


        private ObservableCollection<string> _refreshRates = new ObservableCollection<string>() { "1", "2", "5", "10", "30" };
        public ObservableCollection<string> RefreshRates
        {
            get { return _refreshRates; }
            set { _refreshRates = value; }
        }

        private int _selectedRefreshRate = 5;
        public string SelectedRefreshRate
        {
            get { return _selectedRefreshRate.ToString(); }
            set
            {
                int.TryParse(value, out _selectedRefreshRate);
            }
        }

        private string _VPNIPAddress;
        public string VPNIPAddress
        {
            get { return _VPNIPAddress; }
            set { _VPNIPAddress = value; }
        }

        private ObservableCollection<string> _currentIPAddresses = new ObservableCollection<string>();
        public ObservableCollection<string> CurrentIPAddresses
        {
            get { return _currentIPAddresses; }
            set { _currentIPAddresses = value; }
        }


    }
}

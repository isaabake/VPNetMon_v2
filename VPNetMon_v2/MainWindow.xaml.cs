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
                    //Kill
                    KillProcesses();
                }
                else if (value == true && VPNConnected == false)
                {
                    //Start
                    StartProcesses();
                }
                _VPNConnected = value;
            }
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
            set 
            {
                _currentIPAddresses = value; 
            }
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
            }

        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null)
                {
                    binding.UpdateSource();
                }
            }
        }



    }
}

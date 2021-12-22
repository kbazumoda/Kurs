using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NetworkClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int destIp, int srcIP, byte[] macAddr, ref uint physicalAddrLen);

        List<TableHost> _host = new List<TableHost>();
        IPHostEntry entry;
        string[] ipToString = new string[4];


        public MainWindow()
        {
            InitializeComponent();

            String host = Dns.GetHostName();
            // Получение ip-адреса.
            //IPAddress ip = Dns.GetHostByName(host).AddressList[0];
            for(int i=0;i< Dns.GetHostByName(host).AddressList.Length; i++)
            {
                ComboBoxIP.Items.Add(Dns.GetHostByName(host).AddressList[i]);
                
            }
            if (ComboBoxIP.Items.Count > 0)
            {
                ComboBoxIP.SelectedIndex = 0;
            }
            //IPAddress ip = comboBoxIP.;
            // Показ адреса в label'е.
            label7.Content ="Your IP - " +ComboBoxIP.SelectedItem;
            ipToString = ComboBoxIP.SelectedItem.ToString().Split('.');
            ADd();
        }

        string[] ipadressText;
        string[] hostnameText;
        string[] macaddressText;

        private void ADd()
        {
            string parth = Environment.CurrentDirectory + "\\IPMAC.txt";

            try
            {
                string[] str = File.ReadAllLines(parth);

                ipadressText = new string[str.Length];
                hostnameText = new string[str.Length];
                macaddressText = new string[str.Length];
                for (int i = 0; i < str.Length; i++)
                {
                    string[] s = str[i].Split('#');
                    ipadressText[i] = s[0];
                    hostnameText[i] = s[1];
                    macaddressText[i] = s[2];
                    comboBox1.Items.Add(s[1]);
                }
            }
            catch
            {
               // File.Create(parth);
            }

        }

        private void WakeFunction(string MAC_ADDRESS)
        {
            WOLClass client = new WOLClass();
            client.Connect(new IPAddress(0xffffffff), 0x2fff);
            client.SetClientToBrodcastMode();
            int counter = 0;
            //буффер для отправки
            byte[] bytes = new byte[1024];
            //Первые 6 бит 0xFF
            for (int y = 0; y < 6; y++)
                bytes[counter++] = 0xFF;
            //Повторим MAC адрес 16 раз
            for (int y = 0; y < 16; y++)
            {
                int i = 0;
                for (int z = 0; z < 6; z++)
                {
                    bytes[counter++] = byte.Parse(MAC_ADDRESS.Substring(i, 2), NumberStyles.HexNumber);
                    i += 2;
                }
            }

            //Отправляем полученый магический пакет
            int reterned_value = client.Send(bytes, 1024);
        }
        //private async void fill_ListView  ()
        //{
            
        //}


        private void GetInform(string textName)
        {
            string IP_Address = "";
            string HostName = "";
            string MacAddress = "";

              try
            {
                //Проверяем существует ли IP
                entry = Dns.GetHostEntry(textName);
                foreach (IPAddress a in entry.AddressList)
                {
                    IP_Address = a.ToString();
                    break;
                }

                //Получаем HostName
                HostName = entry.HostName;

                //Получаем Mac-address
                IPAddress dst = IPAddress.Parse(textName);

                byte[] macAddr = new byte[6];
                uint macAddrLen = (uint)macAddr.Length;

                if (SendARP(BitConverter.ToInt32(dst.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
                    throw new InvalidOperationException("SendARP failed.");

                string[] str = new string[(int)macAddrLen];
                for (int i = 0; i < macAddrLen; i++)
                    str[i] = macAddr[i].ToString("x2");

                MacAddress = string.Join(":", str);

                //Далее, если всё успешно, добавляем все данные в список, после чего выводим всё в ListView
                Dispatcher.Invoke(new Action(() =>
                {

                    _host.Add(new TableHost() { ipAdress = IP_Address, nameComputer = HostName, MacAdress = MacAddress });
                    listView1.ItemsSource = null;
                    listView1.ItemsSource = _host;
                }));
            }
            catch { }

        }


        private void button2_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo psiOpt = new ProcessStartInfo(@"cmd.exe", @"/C arp -a");
            psiOpt.WindowStyle = ProcessWindowStyle.Hidden;
            psiOpt.RedirectStandardOutput = true;
            psiOpt.UseShellExecute = false;
            psiOpt.StandardOutputEncoding = Encoding.UTF7;
            psiOpt.CreateNoWindow = true;
            Process procCommand = Process.Start(psiOpt);
            StreamReader srIncoming = procCommand.StandardOutput;
            //  MessageBox.Show(srIncoming.ReadToEnd());
            string output;
            while ((output = srIncoming.ReadLine()) != null)
            {

                string[] mas = output.Split(new char[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    IPAddress ipA = IPAddress.Parse(string.Format("{0}.{1}.{2}.{3}", mas[0], mas[1], mas[2], mas[3]));
                    GetInform(ipA.ToString());
                }
                catch { }
            }


            procCommand.WaitForExit();

        }

        private void PowerOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WakeFunction(_host[listView1.SelectedIndex].MacAdress.ToString().Replace(":", ""));
                MessageBox.Show("Операция выполнена успешно!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("Запрос некорретный!", "Внимание!Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            _host.Clear();
            listView1.Items.Refresh();
        }

        private void copyIP_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_host[listView1.SelectedIndex].ipAdress.ToString());
        }

        private void copyName_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_host[listView1.SelectedIndex].nameComputer.ToString());
        }

        private void copyMacAddress_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_host[listView1.SelectedIndex].MacAdress.ToString());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StreamWriter write = new StreamWriter(@"IPMAC.txt", true);

            for (int index = 0; index < _host.Count; index++)
            {

                if (!macaddressText.Contains(_host[index].MacAdress))
                    write.WriteLine(_host[index].ipAdress + "#" + _host[index].nameComputer + "#" + _host[index].MacAdress);
            }
            write.Close();
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            textBox2.Text = ipadressText[comboBox1.SelectedIndex];
            textBox3.Text = hostnameText[comboBox1.SelectedIndex];
            textBox4.Text = macaddressText[comboBox1.SelectedIndex];
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WakeFunction(textBox4.Text.Replace(":", ""));
                MessageBox.Show("Операция выполнена успешно!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("Запрос некорретный!", "Внимание!Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void label1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("http://vk.com/gor_pp");
        }
    }

    class TableHost
    {
        public string ipAdress { get; set; }
        public string nameComputer { get; set; }
        public string MacAdress { get; set; }
    }

    public class WOLClass : UdpClient
    {
        public WOLClass() : base() { }
        public void SetClientToBrodcastMode()
        {
            if (this.Active)
                this.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PortScanner
{
    public partial class Form1 : Form
    {
        private int countThreads = 1;
        private List<IPAddress> ipAdress = new List<IPAddress>();
        private List<int> ports = new List<int>();
        private List<IPEndPoint> iPEndPoints = new List<IPEndPoint>();
        private readonly object listLock = new object();
        private ulong countCheckPoints;
        private Stopwatch stopWatch = new Stopwatch();
        private bool isProcess = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!isProcess)
            {
                isProcess = true;
                //listView1.Clear();
                stopWatch.Reset();
                stopWatch.Start();
            }
            else
            {
                MessageBox.Show("Сканер уже работает");
                return;
            }
            GetPorts();
            GetListIp();
            CreateStackEndPoints();
            Task.Factory.StartNew(DrawProgress);
            countThreads = Convert.ToInt32(numericUpDown3.Value);
            StartScanning();
        }

        private void StartScanning()
        {
            for (int i = 0; i < countThreads; i++)
            {
                if (iPEndPoints.Count > 0)
                {
                    IPEndPoint endPoint = ExtractNextEndPoint();
                    new Thread(new ThreadStart(() => Scan(endPoint.Address.ToString(), endPoint.Port))).Start();
                }
            }
        }

        private void CreateStackEndPoints()
        {
            iPEndPoints.Clear();
            countCheckPoints = 0;
            foreach (IPAddress ip in ipAdress)
            {
                foreach (int port in ports)
                {
                    iPEndPoints.Add(new IPEndPoint(ip,port));
                    countCheckPoints++;
                }
            }
        }

        private IPEndPoint ExtractNextEndPoint()
        {
            lock (listLock)
            {
                IPEndPoint result = iPEndPoints[0];
                iPEndPoints.RemoveAt(0);
                return result;
            }
            
        }

        private void GetPorts()
        {
            ports.Clear();
            int startPort = Convert.ToInt32(numericUpDown1.Value);
            int endPort = Convert.ToInt32(numericUpDown2.Value);
            if (endPort < startPort) throw new Exception("Неверно введен диапазон портов");
            for(int i =startPort; i<= endPort;i++)
            {
                ports.Add(i);
            }
        }

        private void GetListIp()
        {
            ipAdress.Clear();      
            ipAdress.Add(IPAddress.Parse(textBox1.Text));
        }

        private void Scan(string ip, int port)
        {
            System.Net.Sockets.TcpClient TcpScan = new System.Net.Sockets.TcpClient();
            try
            {
                TcpScan.Connect(ip, port);
                PortOpen(ip,port);
            }
            catch
            {
                PortClose(ip,port);
            }    
            finally
            {
                if (iPEndPoints.Count > 0)
                {
                    IPEndPoint endPoint = ExtractNextEndPoint();
                    Scan(endPoint.Address.ToString(), endPoint.Port);
                }
                else
                {
                    if (isProcess)
                    {
                        isProcess = false;
                        stopWatch.Stop();
                        MessageBox.Show("Scan Sucsess: " + stopWatch.Elapsed.ToString("hh\\:mm\\:ss"));
                        stopWatch.Stop();
                    }                   
                }
            }
        }

        public void PortOpen(string ip,int port)
        {
            try
            {
                if (InvokeRequired)
                    BeginInvoke(new MethodInvoker(delegate
                    {
                        listView1.Items.Add(new ListViewItem(new string[] {ip ,port.ToString()}));
                    }));
                else
                {
                    listView1.Items.Add(new ListViewItem(new string[] { ip, port.ToString() }));
                }
            }
            catch (Exception) { }
        }

        public void PortClose(string ip, int port)
        {
        }

        private void DrawProgress()
        {
            try
            {
                if (iPEndPoints.Count > 0)
                {
                    ulong test = Convert.ToUInt64(iPEndPoints.Count);
                    progressBar1.BeginInvoke(new Action(() => progressBar1.Value = 100 - (int)(((float)test / countCheckPoints) * 100f)));
                    progressBar1.BeginInvoke(new Action(() => label1.Text = stopWatch.Elapsed.ToString("hh\\:mm\\:ss")));
                }
                else
                {
                    progressBar1.BeginInvoke(new Action(() => progressBar1.Value = 100));
                    progressBar1.BeginInvoke(new Action(() => label1.Text = stopWatch.Elapsed.ToString("hh\\:mm\\:ss")));
                }
                if (isProcess)
                {
                    Thread.Sleep(10);
                    DrawProgress();
                }
            }
            catch { }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}

using Microsoft.Win32;
using Org.BouncyCastle.Asn1.Cms;
using PDC_System.Services;
using System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using XamlAnimatedGif;



namespace PDC_System
{
    /// <summary>
    /// Interaction logic for Fingerprint.xaml
    /// </summary>
    public partial class Fingerprint : Window
    {
        private readonly HikvisionService _hikvision;

        public Fingerprint()
        {
            InitializeComponent();
            StartGifAnimation();

            _hikvision = new HikvisionService(
          "192.168.1.15",
                    "admin",
                    "priyanthaD@8");

            Loaded += Fingerprint_Loaded;

        }

        private void StartGifAnimation()
        {
            AnimationBehavior.SetSourceUri(MyGifImage, null);
            AnimationBehavior.SetSourceUri(
                MyGifImage,
                new Uri(System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    "fingerprintss.gif")));
        }



        private async void Fingerprint_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDeviceInfo();
        }

        private async Task LoadDeviceInfo()
        {
            try
            {
                XDocument? doc = await _hikvision.DeviceInfo();

                if (doc == null)
                    return;

                XNamespace ns = "http://www.isapi.org/ver20/XMLSchema";

                txtDeviceName.Text =
                    doc.Root?.Element(ns + "deviceName")?.Value ?? "-";

                txtModel.Text =
                    doc.Root?.Element(ns + "model")?.Value ?? "-";

                txtFirmware.Text =
                    doc.Root?.Element(ns + "firmwareVersion")?.Value ?? "-";

                bool online = await _hikvision.TestConnection();

                txtStatus.Text = online ? "ONLINE" : "OFFLINE";
                txtStatus.Foreground = online
                    ? Brushes.LimeGreen
                    : Brushes.Red;
            }
            catch
            {
                txtStatus.Text = "OFFLINE";
                txtStatus.Foreground = Brushes.Red;
            }
        }




    }
}

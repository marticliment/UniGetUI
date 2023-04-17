using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Capture;

namespace WingetUIShareComponent
{
    public partial class Form1 : Form
    {

        string name = "PackageName";
        string link = "https://marticliment.com/wingetui/share?pname=WingetUI&pid=SomePythonThings.WingetUIStore";

        public Form1()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();

            // args[0]: executable file
            // args[1]: Application share title
            // args[2]: Share link
            // args[3]: Desired geometry

            if (args.Length < 4)
            {
                System.Environment.Exit(2);
            }

            name = args[1];
            link = args[2];


            IntPtr hwnd = this.Handle;
            var dtm = DataTransferManagerHelper.GetForWindow(hwnd);
            dtm.DataRequested += OnDataRequested;
            dtm.TargetApplicationChosen += (DataTransferManager sender, TargetApplicationChosenEventArgs arg) => { this.Close(); };
            DataTransferManagerHelper.ShowShareUIForWindow(hwnd);

        }

        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args) {             
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetWebLink(new System.Uri(this.link));
            dataPackage.Properties.Title = "Sharing "+this.name;
            dataPackage.Properties.Description = "View and install "+this.name+" from WingetUI";
            args.Request.Data = dataPackage;

        }
    }

    static class DataTransferManagerHelper
    {
        static readonly Guid _dtm_iid = new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        static IDataTransferManagerInterop DataTransferManagerInterop
        {
            get
            {
                return (IDataTransferManagerInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(DataTransferManager));
            }
        }

        public static DataTransferManager GetForWindow(IntPtr hwnd)
        {
            return DataTransferManagerInterop.GetForWindow(hwnd, _dtm_iid);
        }

        public static void ShowShareUIForWindow(IntPtr hwnd)
        {
            DataTransferManagerInterop.ShowShareUIForWindow(hwnd);
        }

        [ComImport, Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDataTransferManagerInterop
        {
            DataTransferManager GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);
            void ShowShareUIForWindow(IntPtr appWindow);
        }
    }
}

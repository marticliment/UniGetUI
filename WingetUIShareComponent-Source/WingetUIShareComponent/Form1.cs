using System;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

namespace WingetUIShareComponent
{
    public partial class Form1 : Form
    {

        string name = "PackageName";
        string link = "https://marticliment.com/wingetui/share?pname=WingetUI&pid=SomePythonThings.WingetUIStore";
        string receivedGeometry = "";
        int x = 0;
        int y = 0;
        int width = 800;
        int height = 600;

        public Form1()
        {
            InitializeComponent();

            string[] args = Environment.GetCommandLineArgs();

            // args[0]: executable file (default argument)
            // args[1]: Application share title
            // args[2]: Share link
            // args[3]: Desired geometry

            if (args.Length < 3)
            {
                System.Environment.Exit(2);
            }

            name = args[1];
            link = args[2];

            try
            {
                receivedGeometry = args[3];
                string[] geometryElements = receivedGeometry.Split(',');
                if (geometryElements.Length != 4)
                {
                    throw new ArgumentException("The geomery string must be given in the following scheme: {left},{top},{width},{height}");
                }
                width = int.Parse(geometryElements[2]);
                height = int.Parse(geometryElements[3]);
                x = int.Parse(geometryElements[0]);
                y = int.Parse(geometryElements[1]);
                if (width < 100 || height < 100)
                {
                    throw new ArgumentException("Width and Height values must not be smaller than 100px each");
                }
            } catch (Exception ex)
            {
                Screen screen = Screen.PrimaryScreen;
                Rectangle rect = screen.Bounds;
                width = rect.Width;
                height = rect.Height;
                x = rect.Left;
                y = rect.Top;
                Debug.WriteLine(rect.ToString());  
                Debug.WriteLine(ex.ToString());
            }

            Height = height-1;
            Width = width-1; // +-1 offsets to prevent the window from being detected as a fullscreen window
            Left = x+1;
            Top = y+1;
            Text = "Sharing " + this.name;

            Debug.WriteLine(this.Size);

            Activate();

            IntPtr hwnd = this.Handle;
            var dtm = DataTransferManagerHelper.GetForWindow(hwnd);
            dtm.DataRequested += OnDataRequested;
            dtm.TargetApplicationChosen += (DataTransferManager sender, TargetApplicationChosenEventArgs arg) => { this.Close(); };
            DataTransferManagerHelper.ShowShareUIForWindow(hwnd);
            dtm.ShareProvidersRequested += (DataTransferManager sender, ShareProvidersRequestedEventArgs arg) =>
            {
                LostFocus += (self, arg2) => { Close(); };
                GotFocus += (self, arg2) => { Close(); }; // When the shareUI is loaded, detech window state changes and close root window
            };
        }

        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args) {             
            DataPackage dataPackage = new DataPackage();
            dataPackage.SetWebLink(new System.Uri(this.link));
            dataPackage.Properties.Title = "Sharing "+this.name;
            dataPackage.Properties.Description = "View and install "+this.name+" from WingetUI";
            args.Request.Data = dataPackage;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

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

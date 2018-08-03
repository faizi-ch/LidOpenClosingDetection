using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LidOpenClosingDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification",
            CallingConvention = CallingConvention.StdCall)]

        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid,
            Int32 Flags);

        internal struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        Guid GUID_LIDSWITCH_STATE_CHANGE = new Guid(0xBA3E0F4D, 0xB817, 0x4094, 0xA2, 0xD1, 0xD5, 0x63, 0x79, 0xE6, 0xA0,
            0xF3);

        const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        const int WM_POWERBROADCAST = 0x0218;
        const int PBT_POWERSETTINGCHANGE = 0x8013;

        private bool? _previousLidState = null;

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += Window_SourceInitialized;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            RegisterForPowerNotifications();
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
        }

        private void RegisterForPowerNotifications()
        {
            IntPtr handle = new WindowInteropHelper(Application.Current.Windows[0]).Handle;
            IntPtr hLIDSWITCHSTATECHANGE = RegisterPowerSettingNotification(handle,
                ref GUID_LIDSWITCH_STATE_CHANGE,
                DEVICE_NOTIFY_WINDOW_HANDLE);
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_POWERBROADCAST:
                    OnPowerBroadcast(wParam, lParam);
                    break;
                default:
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnPowerBroadcast(IntPtr wParam, IntPtr lParam)
        {
            if ((int) wParam == PBT_POWERSETTINGCHANGE)
            {
                POWERBROADCAST_SETTING ps =
                    (POWERBROADCAST_SETTING) Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING));
                IntPtr pData = (IntPtr) ((int) lParam + Marshal.SizeOf(ps));
                Int32 iData = (Int32) Marshal.PtrToStructure(pData, typeof(Int32));
                if (ps.PowerSetting == GUID_LIDSWITCH_STATE_CHANGE)
                {
                    bool isLidOpen = ps.Data != 0;

                    if (!isLidOpen == _previousLidState)
                    {
                        LidStatusChanged(isLidOpen);
                    }

                    _previousLidState = isLidOpen;
                }
            }
        }

        private void LidStatusChanged(bool isLidOpen)
        {
            if (isLidOpen)
            {
                //Do some action on lid open event
                TextBlock.Text += String.Format("{0}: Lid opened!\n\n", DateTime.Now);
                //MessageBox.Show("open");
                //Debug.WriteLine("{0}: Lid opened!", DateTime.Now);
            }
            else
            {
                //MessageBox.Show("close");
                //Do some action on lid close event
                TextBlock.Text += String.Format("{0}: Lid closed!\n\n", DateTime.Now);
                //Debug.WriteLine("{0}: Lid closed!", DateTime.Now);
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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

namespace GTA_Online_Solo_Session_Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static int pid;
        bool StartTasksEnabled;

        public MainWindow()
        {
            InitializeComponent();

            //Hides the texts from mainform
            Text.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;
            DoneText.Visibility = Visibility.Hidden;
            StartGTAText.Visibility = Visibility.Hidden;

            GetGTAVProcess();
        }

        void GetGTAVProcess()
        {
            Process[] processes = Process.GetProcessesByName("GTA5");

            //Do this if GTA V is not running
            if (processes.Length == 0 || processes == null)
            {
                Button.IsEnabled = false;
                StartTasksEnabled = false;
                StartGTAText.Visibility = Visibility.Visible;
                DoneText.Visibility = Visibility.Hidden; // Fix if the user wants to run it after it did a StartTasks() void and closed GTA V
            }
            else
            {
                // If runnung activate button enabled and StartTasks() void can run
                Button.IsEnabled = true;
                StartTasksEnabled = true;
            }


            foreach (var process in processes)
            {
                // Get GTA5.exe process id
                pid = process.Id;
            }
        }
        
        // Suspend and Resume process stuff
        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


        private static void SuspendProcess()
        {
            var process = Process.GetProcessById(pid); // throws exception if process does not exist

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess()
        {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GetGTAVProcess();
            StartTasks();
        }

        //The actual tasks that the program does
        async void StartTasks()
        {
            if (StartTasksEnabled == true)
            {
                Text.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Visible;
                DoneText.Visibility = Visibility.Hidden;

                Text.Text = "Suspending GTA5.exe process";
                SuspendProcess();
                await Task.Run(() => Wait()); // Wait 15 seconds to be able to read the text and give some time to GTA (Async/Await function to not freeze the UI)
                Text.Text = "Resuming GTA5.exe process";
                ResumeProcess();
                await Task.Run(() => Wait3Sec());
                DoneText.Visibility = Visibility.Visible;
                ProgressBar.Visibility = Visibility.Hidden;
                Text.Visibility = Visibility.Hidden;
                await Task.Run(() => Wait3Sec());
            }
        }

        void Wait()
        {
            System.Threading.Thread.Sleep(15000);
        }

        void Wait3Sec()
        {
            System.Threading.Thread.Sleep(3000);
        }

        // Hotkey stuff | Thank you CodeSwine <3 https://github.com/CodeSwine/GTA5Online-Private_Public_Lobby
        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
        [In] IntPtr hWnd,
        [In] int id,
        [In] uint fsModifiers,
        [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private HwndSource _source;
        private const int HOTKEY_ID = 9000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
            base.OnClosed(e);
        }

        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            const uint VK_F10 = 0x79;
            const uint MOD_CTRL = 0x0002;
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL, VK_F10))
            {

            }
        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            OnHotKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnHotKeyPressed()
        {
            GetGTAVProcess();
            StartTasks();
            System.Media.SystemSounds.Hand.Play();
        }
    }
}

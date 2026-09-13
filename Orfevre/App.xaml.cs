using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;
using Orfevre.PowerTerminal;

namespace Orfevre
{
    public partial class App : Application
    {
        private static Dictionary<string, (string Key, int ID, uint MOD, string Type, string Command, bool DisplayFlag)> Info = [];
        private static Dictionary<string, uint> key = [];
        private static string[] index = [];
        private static string configPath = "";
        private static string configDir = "";

        private Mutex? _mutex;
        private const string PipeName = "StayGoldPipe";
        private const string MutexName = "StayGoldMutex";

        private NotifyIcon? _notifyIcon;
        private MainWindow? _main;

        public App()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            configDir = Path.Combine(localAppData, "StayGold");
            configPath = Path.Combine(configDir, "config.ini");

            // AからZまでのホットキーを登録する
            uint k = 65;
            for(char c = 'A'; c <= 'Z'; c++) key[c.ToString()] = k++;


        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _mutex = new Mutex(false, MutexName,out bool createdNew);
            if(!createdNew)
            {
                Application.Current.Shutdown();
                return;
            }
            SetupNotifyIcon();
            Task.Run(StartNamedPipeServer);
            SetConfig();
            RegisterHotKey(IntPtr.Zero, 1, 6, (uint)70);

            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            for(int u = 1; u < 10000; u++) UnregisterHotKey(IntPtr.Zero, u);
            ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;

            if(_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            if(_mutex != null) _mutex.Dispose();
            base.OnExit(e);
        }

        private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg.message == WM_HOTKEY)
            {
                if (msg.wParam.ToInt32() == 1)
                {
                    var powerTerminal = new Orfevre.PowerTerminal.PowerTerminal();
                    powerTerminal.Show();
                }
                    foreach (var item in index)
                {
                    if (msg.wParam.ToInt32() == Info[item].ID)
                    {
                        Process.Start(Info[item].Command);
                        handled = true;
                    }
                }
            }
        }


        private async Task StartNamedPipeServer()
        {
            while (true)
            {
                // "StayGoldPipe" という名前の通信経路（パイプ）を開く
                using (var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                {
                    // クライアントからの接続を待つ（ここで待機状態になるわ）
                    await server.WaitForConnectionAsync();

                    using (var reader = new StreamReader(server))
                    {
                        // クライアントから送られてきたコマンドを受け取る
                        var command = await reader.ReadLineAsync();
                        
                        if(command == null) continue;
                        // 受信したコマンドをJson形式から配列に変換する
                        var commandArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(command);
                        if(commandArray == null) continue;

                        switch (commandArray[0])
                        {
                            case "Close":
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    Application.Current.Shutdown();
                                });
                                break;
                            case "CreateObject":
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var newObjectWindow = new CreateObject.NewObject(commandArray);
                                    newObjectWindow.Show();
                                });
                                break;
                            case "Config":
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (_main == null || !_main.IsLoaded)
                                    {
                                        _main = new MainWindow();
                                        _main.Show();
                                    }
                                    else
                                    {
                                        if (_main.WindowState == WindowState.Minimized)
                                        {
                                            _main.WindowState = WindowState.Normal;
                                        }
                                        _main.Activate();
                                    }
                                });
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        private void SetupNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();

            var iconUri = new Uri("pack://application:,,,/Orfevre;component/icon/images.bmp", UriKind.Absolute);
            using (var stream = System.Windows.Application.GetResourceStream(iconUri).Stream)
            using (var bitmap = new Bitmap(stream))
            {
                _notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
            }

            _notifyIcon.Text = "Orfevre - 待機中";
            _notifyIcon.Visible = true;

            // 右クリックしたときのメニューも作っておきましょうか
            var menu = new Forms.ContextMenuStrip();

            // 終了メニューの追加
            var exitItem = new Forms.ToolStripMenuItem("終了");
            exitItem.Click += (sender, args) =>
            {
                // ここで明示的にアプリを終了させるのよ
                Application.Current.Shutdown();
            };
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        public static void SetConfig()
        {
            if (!Directory.Exists(configDir)) { Directory.CreateDirectory(configDir); }
            if (!File.Exists(configPath) || new FileInfo(configPath).Length == 0) File.WriteAllText(configPath, @"X_CTRLSHIFT,9000,6,Exec,D:\ProgramData\pdfas\pdf_as.exe,true");

            index = File.ReadLines(configPath).Select(line => line.Split(',')[0]).ToArray();
            Info = File.ReadLines(configPath)
                .Select(line => line.Split(','))
                .ToDictionary(parts => parts[0], parts => (Key: parts[0], ID: int.Parse(parts[1]), MOD: uint.Parse(parts[2]), Type: parts[3], Command: parts[4], DisplayFlag: bool.Parse(parts[5])));

            for (int u = 2; u < 10000; u++) UnregisterHotKey(IntPtr.Zero, u);
            foreach (string i in index) RegisterHotKey(IntPtr.Zero, Info[i].ID, Info[i].MOD, key[Info[i].Key[0].ToString()]);
        }


        public static void ExitOrfevre()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }
    }

}

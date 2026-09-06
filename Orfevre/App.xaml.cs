using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;

namespace Orfevre
{
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int CSX_ID = 9000;
        private const int AltA_ID = 9002;

        // 修飾キー（CtrlやShiftなど）の定義
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private Mutex? _mutex;
        private const string PipeName = "StayGoldPipe";
        private const string MutexName = "StayGoldMutex";

        private NotifyIcon? _notifyIcon;
        private MainWindow? _main;

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

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(Key.X);
            RegisterHotKey(IntPtr.Zero, CSX_ID, MOD_CTRL | MOD_SHIFT, vk);
            uint vkAltA = (uint)KeyInterop.VirtualKeyFromKey(Key.A);
            RegisterHotKey(IntPtr.Zero, AltA_ID, MOD_ALT, vkAltA);
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            UnregisterHotKey(IntPtr.Zero, CSX_ID);
            UnregisterHotKey(IntPtr.Zero, AltA_ID);
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

            // 飛んできたメッセージが「ホットキー」で、かつ「私たちが登録したID」かチェックするわ
            if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == CSX_ID)
            {
                // ----------------------------------------------------
                // ここに、ショートカットキーが押された時の処理を書くのよ
                // ----------------------------------------------------

                // 試しにメッセージを出してみるわね（WPFのMessageBoxよ）
                System.Windows.MessageBox.Show("Ctrl + Shift + X が押されたわ！");

                // 「このメッセージは私が処理したから、もう他には流さないで」という合図よ
                handled = true;
            }
            else if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == AltA_ID)
            {
                // ----------------------------------------------------
                // ここに、Alt + Aキーが押された時の処理を書くのよ
                // ----------------------------------------------------

                // 試しにメッセージを出してみるわね（WPFのMessageBoxよ）
                System.Windows.MessageBox.Show("Alt + A が押されたわ！");

                // 「このメッセージは私が処理したから、もう他には流さないで」という合図よ
                handled = true;
            }
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();

            // 実行ファイル自身のアイコンを抽出してセットするわ
            if(Environment.ProcessPath != null) _notifyIcon.Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
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
                        //System.Windows.MessageBox.Show($"受信したコマンド: {command}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if(_main == null || !_main.IsLoaded)
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
                    }
                }
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Orfevre.PowerTerminal
{
    public partial class PowerTerminal : Window
    {
        private Runspace _sharedRunspace;
        public PowerTerminal()
        {
            InitializeComponent();
            _sharedRunspace = RunspaceFactory.CreateRunspace();
            _sharedRunspace.Open();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string input = InputTextBox.Text;
                OutputTextBox.AppendText($"> {input}\n");
                InputTextBox.Clear();
                ExecuteCommand(input);
            }
        }

        private async void ExecuteCommand(string command)
        {
            await Task.Run(async () =>
            {
                using (PowerShell ps = PowerShell.Create())
                {
                    ps.Runspace = _sharedRunspace;
                    ps.AddScript(command);
                    var results = await ps.InvokeAsync();
                    foreach (var result in results)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText(result.ToString() + "\n");
                        });
                    }
                    if (ps.Streams.Error.Count > 0)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                OutputTextBox.AppendText($"Error: {error.ToString()}\n");
                            });
                        }
                    }
                }
            });
        }
        public void Cleanup()
        {
            // アプリ終了時にはお部屋のお片付けも忘れずにね
            _sharedRunspace?.Dispose();
        }
    }
}

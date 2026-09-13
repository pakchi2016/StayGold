using System;
using System.IO;
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
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace Orfevre.CreateObject
{
    /// <summary>
    /// NewObject.xaml の相互作用ロジック
    /// </summary>
    public partial class NewObject : Window
    {
        private ComboBox CSBox;
        private ComboBox CSCreateBox;
        private ComboBox CSCreateTypeBox;
        private ComboBox CSBuildBox;
        private ComboBox NewBox;
        private TextBox InputBox;
        private BuildInfo buildInfo;

        private string[] args;
        public NewObject(string[] _args)
        {
            InitializeComponent();
            NewBox = new ComboBox();
            CSBox = new ComboBox();
            CSCreateBox = new ComboBox();
            CSCreateTypeBox = new ComboBox();
            CSBuildBox = new ComboBox();
            SetComboBox();
            InputBox = new TextBox();
            FormatNewBox();
            args = _args;
            buildInfo = new BuildInfo();

            ActionComboBox.SelectionChanged += ActionComboBox_SelectionChanged;
            CSBox.SelectionChanged += CSBox_SelectionChanged;

        }

        private void ActionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NewBox == null || CSBox == null) return;

            SecondPanel.Children.Clear();
            ThirdPanel.Children.Clear();
            FourthPanel.Children.Clear();
            FifthPanel.Children.Clear();
            switch (ActionComboBox.SelectedIndex)
            {
                case 0: // C#
                    SecondPanel.Children.Add(CSBox);
                    ThirdPanel.Children.Add(CSBuildBox);
                    CSBox.SelectedIndex = 1;
                    CSBuildBox.SelectedIndex = 0;
                    break;
                case 1: // 新規作成
                    FormatNewBox();
                    break;
            }

        }

        private void CSBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CSCreateBox == null) return;

            ThirdPanel.Children.Clear();
            FourthPanel.Children.Clear();
            FifthPanel.Children.Clear();

            switch (CSBox.SelectedIndex)
            {
                case 0: // Create
                    ThirdPanel.Children.Add(CSCreateBox);
                    FourthPanel.Children.Add(CSCreateTypeBox);
                    FifthPanel.Children.Add(InputBox);
                    CSCreateBox.SelectedIndex = 0;
                    CSCreateTypeBox.SelectedIndex = 0;
                    InputBox.Clear();
                    InputBox.AcceptsReturn = false;
                    break;
                case 1: // Build
                    ThirdPanel.Children.Add(CSBuildBox);
                    break;
            }
        }

        private void SetComboBox()
        {
            NewBox.Items.Add("ファイル作成");
            NewBox.Items.Add("フォルダ作成");
            CSBox.Items.Add("Create");
            CSBox.Items.Add("Build");
            CSCreateBox.Items.Add("Console");
            CSCreateBox.Items.Add("WPF");
            CSCreateBox.Items.Add("Windows Forms");
            CSCreateBox.Items.Add("Class Library");
            CSCreateTypeBox.Items.Add(".NET Core");
            CSCreateTypeBox.Items.Add(".NET Framework");
            CSBuildBox.Items.Add("Run");
            CSBuildBox.Items.Add("Release");
        }

        private void FormatNewBox()
        {
            SecondPanel.Children.Add(NewBox);
            ThirdPanel.Children.Add(InputBox);
            InputBox.AcceptsReturn = true;
            NewBox.SelectedIndex = 0;
            InputBox.Text = DateTime.Today.ToString("yyyyMMdd");
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            switch (ActionComboBox.SelectedIndex)
            {
                case 0:
                    switch(CSBox.SelectedIndex)
                    {
                        case 0:
                            if (InputBox.Text == "")
                            {
                                MessageBox.Show("プロジェクト名を指定してください");
                                return;
                            }
                            int type = CSCreateBox.SelectedIndex;
                            int ver = CSCreateTypeBox.SelectedIndex;
                            string Command = $"{buildInfo.Type[type]} {buildInfo.Version[ver]} -o {Path.Combine(args[1], InputBox.Text)}";
                            MessageBox.Show(Command);
                            System.Diagnostics.Process.Start("cmd.exe", $"/c {Command} && pause");
                            break;
                        case 1:
                            int buildType = CSBuildBox.SelectedIndex;
                            string buildCommand = buildType == 0 ? "dotnet run" : "dotnet publish -c Release";
                            System.Diagnostics.Process.Start("cmd.exe", $"/c cd {args[1]} && {buildCommand} && pause");
                            break;
                    }
                    break;
                case 1:
                    switch(NewBox.SelectedIndex)
                    {
                        case 0:
                            if (InputBox.Text == "")
                            {
                                MessageBox.Show("ファイル名を指定してください");
                                return;
                            }
                            foreach (string line in InputBox.Text.Split("\r\n"))
                            {
                                File.CreateText(Path.Combine(args[1], line)).Close();
                            }
                            break;
                        case 1:
                            if (InputBox.Text == "")
                            {
                                MessageBox.Show("フォルダ名を指定してください");
                                return;
                            }
                            foreach (string line in InputBox.Text.Split("\r\n"))
                            {
                                Directory.CreateDirectory(Path.Combine(args[1], line));
                            }
                            break;
                    }
                    break;
            }
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

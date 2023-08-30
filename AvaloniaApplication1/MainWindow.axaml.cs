using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using Porter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AvaloniaApplication1
{
    public partial class MainWindow : Window
    {
        Analyzer.Analyzer analyzer;
        Logger.Logger logger = new Logger.Logger();
        String File_path;
        String File_Path_Long;
        int currentfile = 0;
        private TextBox _logBox;
        Boolean isport = false;
        ProgressBar pb;
        Label pbLabel;
        public MainWindow()
        {
            InitializeComponent();
            // Grab and pass the textbox to the logger class
            _logBox = this.FindControl<TextBox>("LogTextBox");
            logger.LogTextBox = _logBox;
            pb = this.FindControl<ProgressBar>("PortProgressBar");
            pb.Minimum = 0;
            pb.Maximum = 100;
            pbLabel = this.Find<Label>("ProgressBarLabel");

        }


        // Handle Project Select Button Click
        public async void OnClickSelect(object sender, RoutedEventArgs e)
        {
            //TODO: only allow .csproj files
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filters.Add(new FileDialogFilter() { Name = "csproj files", Extensions = { "csproj" } });
            var result = ofd.ShowAsync(new Window());

            if (result.Result is not null)
            {
                pb.IsVisible = false;
                pb.Value = 0;
                pbLabel.IsVisible = false;
                pbLabel.Content = "Porting...";
                Debug.WriteLine(result);
                // They have selected a file
                PathTextBox.Text = result.Result[0];
                string temp = result.Result[0];
                File_Path_Long= temp;
                int projectLabel = temp.LastIndexOf("\\");
                File_path = temp.Substring(0, projectLabel);
                logger.appendMessage("Project selected: " + PathTextBox.Text, Logger.Logger.MessageType.Message);

                // Instantiate analyzer
                analyzer = new Analyzer.Analyzer(result.Result[0]);

                // Change the font back to normal and enable the inputs for step 2
                Label stepOneLabel = this.Find<Label>("StepOneLabel");
                stepOneLabel.Content = "Step 1: Select Project File";
                stepOneLabel.FontWeight = FontWeight.Normal;

                Label stepTwoLabel = this.Find<Label>("StepTwoLabel");
                stepTwoLabel.Content = "* Step 2: Select Backup Location";
                stepTwoLabel.Foreground = new SolidColorBrush(Colors.Black);
                stepTwoLabel.FontWeight = FontWeight.Bold;

                RadioButton projectPathRadioButton = this.FindControl<RadioButton>("Project");
                RadioButton customPathRadioButton = this.FindControl<RadioButton>("Custom");
                TextBox textbox = this.FindControl<TextBox>("BackupTextBox");
                Button selectButton = this.FindControl<Button>("SelectBackupButton");
                Button backupButton = this.FindControl<Button>("BackupButton");
                projectPathRadioButton.IsEnabled = true;
                customPathRadioButton.IsEnabled = true;
                projectPathRadioButton.IsChecked = true;
                textbox.IsEnabled = true;
                selectButton.IsEnabled = true;
                backupButton.IsEnabled = true;
            }

        }

        public void Textboxfill(int indice)
        {
            string[] filesindirectory = Directory.GetFiles(File_path);
            filesindirectory = filesindirectory.Where(file => file.EndsWith(".cs")).ToArray();
            foreach (string file in filesindirectory)
            {
                logger.appendMessage("Analyzing file: " + file, Logger.Logger.MessageType.Message);
            }
            var syntaxDefinition = HighlightingManager.Instance.GetDefinition("C#");

            OldCode.SyntaxHighlighting = syntaxDefinition;
            OldCode.Text = File.ReadAllText(filesindirectory[currentfile]);

            if (isport)
            {
                NewCode.SyntaxHighlighting = syntaxDefinition;
                NewCode.Text = File.ReadAllText(filesindirectory[currentfile]);
            }
        }

        public void PreviousFile(object sender, RoutedEventArgs e)
        {
            string[] filesindirectory = Directory.GetFiles(File_path);
            filesindirectory = filesindirectory.Where(file => file.EndsWith(".cs")).ToArray();
            if (currentfile == 0)
            {
                currentfile = filesindirectory.Length-1;

            }
            else
            {
                currentfile = currentfile-1;
            }
            Textboxfill(currentfile);
        }
        public void NextFile(object sender, RoutedEventArgs e)
        {
            string[] filesindirectory = Directory.GetFiles(File_path);
            filesindirectory = filesindirectory.Where(file => file.EndsWith(".cs")).ToArray();
            if (currentfile == filesindirectory.Length-1)
            {
                currentfile = 0;
            }
            else
            {
                currentfile = currentfile + 1;
            }
            Textboxfill(currentfile);
        }
        // Uses the project path or allows user to select a custom path
        // when creating a backup
        public void HandleChecked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;

            if (rb.Name.Equals("Project"))
            {
                // Grey out the textbox and select button, change textbox to project path

                // First we have to cut off the .csproj part to get root folder
                int count = 0;
                for (int i = PathTextBox.Text.Length - 1; i > 0; i--)
                {
                    if (PathTextBox.Text[i] == '\\')
                    {
                        BackupTextBox.Text = PathTextBox.Text.Substring(0, i);
                        count++;

                        if (count == 2)
                            break;
                    }
                }

                BackupTextBox.IsReadOnly = true;
                SelectBackupButton.IsEnabled = false;
            }
            else if (rb.Name.Equals("Custom"))
            {
                // Allow user to access textbox and select button
                SelectBackupButton.IsEnabled = true;
                BackupTextBox.IsReadOnly = false;
            }
        }

        public void OnClickSelectBackup(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFolderDialog();
            var result = ofd.ShowAsync(new Window());

            if (result != null)
            {
                BackupTextBox.Text = result.Result;
            }
        }
        public void OnClickBackup(object sender, RoutedEventArgs e)
        {
            string sourcePath = "";
            string outPath = BackupTextBox.Text;
            for (int i = PathTextBox.Text.Length - 1; i > 0; i--)
            {
                if (PathTextBox.Text[i] == '\\')
                {
                    sourcePath = PathTextBox.Text.Substring(0, i);
                    break;
                }
            }
            Debug.WriteLine("Backing up " + sourcePath + " to location " + outPath);
            Backer.Backer.CopyFilesRecursively(sourcePath, outPath);

            // Change the font back to normal and enable the inputs for step 3
            Label stepTwoLabel = this.Find<Label>("StepTwoLabel");
            stepTwoLabel.FontWeight = FontWeight.Normal;
            stepTwoLabel.Content = "Step 2: Backup Project";

            Label stepThreeLabel = this.Find<Label>("StepThreeLabel");
            stepThreeLabel.FontWeight = FontWeight.Bold;
            stepThreeLabel.Content = "* Step 3: Analyze";
            stepThreeLabel.Foreground = new SolidColorBrush(Colors.Black);

            Button analyzeButton = this.FindControl<Button>("AnalyzeButton");
            analyzeButton.IsEnabled = true;

            Label classesLabel = this.FindControl<Label>("ClassesLabel");
            Label methodsLabel = this.FindControl<Label>("MethodsLabel");
            Label referencesLabel = this.FindControl<Label>("ReferencesLabel");
            Label versionLabel = this.FindControl<Label>("VersionLabel");
            classesLabel.Foreground = new SolidColorBrush(Colors.Black);
            methodsLabel.Foreground = new SolidColorBrush(Colors.Black);
            referencesLabel.Foreground = new SolidColorBrush(Colors.Black);
            versionLabel.Foreground = new SolidColorBrush(Colors.Black);
        }

        // Handle Analyze Button Click
        public void OnAnalyzeClick(object sender, RoutedEventArgs e)
        {
            //logger.appendMessage("Analyzed Project", Logger.Logger.MessageType.Message);

            Textboxfill(0);

            //Initialize
            int[] data = analyzer.Analyze();
            var classesLabel = this.Find<Label>("ClassesLabel");
            var methodsLabel = this.Find<Label>("MethodsLabel");
            var referenceLabel = this.Find<Label>("ReferencesLabel");
            var versionLabel = this.Find<Label>("VersionLabel");

            //Set values
            referenceLabel.Content = "References: " + data[0];
            classesLabel.Content = "Classes: " + data[1];
            methodsLabel.Content = "Methods: " + data[2];
            versionLabel.Content = "Version: " + analyzer.getVersion();

            // Change the font back to normal and enable the inputs for step 4
            Label stepThreeLabel = this.Find<Label>("StepThreeLabel");
            stepThreeLabel.FontWeight = FontWeight.Normal;
            stepThreeLabel.Content = "Step 3: Analyze";

            Label stepFourLabel = this.Find<Label>("StepFourLabel");
            stepFourLabel.Content = "* Step 4: Port";
            stepFourLabel.FontWeight = FontWeight.Bold;
            stepFourLabel.Foreground = new SolidColorBrush(Colors.Black);

            Button portButton = this.FindControl<Button>("PortButton");
            Button winFormPortButton = this.FindControl<Button>("PortWinFormButton");
            RadioButton radioButton = this.FindControl<RadioButton>("LiveEdit");
            RadioButton radioButton1 = this.FindControl<RadioButton>("NoLiveEdit");
            portButton.IsEnabled = true;
            winFormPortButton.IsEnabled = true;
            radioButton.IsEnabled = true;
            radioButton1.IsEnabled = true;

        }

        // Handle Port Button Click
        public async void OnClickPort(object sender, RoutedEventArgs e)
        {
            pb.IsVisible = true;
            pbLabel.IsVisible = true;
            pb.Value = 0;
            pbLabel.Content = "Porting...";

            Porter.Porter port = new Porter.Porter(PathTextBox.Text);
            pb.Value = 0;
            port.FixProjectStyle();
            pb.Value = 40;
            await Task.Delay(2000);
            port.FixPackageStyle();
            pb.Value = 80;
            string[] filesindirectory = Directory.GetFiles(File_path);
            pb.Value = 90;
            filesindirectory = filesindirectory.Where(file => file.EndsWith(".cs")).ToArray();
            isport = true;
            Textboxfill(currentfile);
            pb.Value = 100;

            pbLabel.Content="Done porting";

        }

        public async void OnClickPortForm(object sender, RoutedEventArgs e)

        {
            // Grab the project name from the path specified by user
            string file = PathTextBox.Text;
            WinForms winForms = new WinForms(file);


            //show progress bar
            pb.IsVisible = true;
            pbLabel.IsVisible = true;

            // First step
            pb.Value=0;
            pbLabel.Content = "Porting...";
            winForms.AddToProjFile();
            pb.Value = 10;
            winForms.CreateProgramFile();
            pb.Value = 20;
            winForms.ConvertDesignerToAxaml();
            pb.Value = 30;
            winForms.MoveCSFiles();
            pb.Value = 40;
            winForms.DeleteFiles();
            pb.Value = 50;
            winForms.CreateAppXaml();
            pb.Value = 60;
            await Task.Delay(2000);
            winForms.CreateAppXamlCs();
            pb.Value = 70;
            winForms.CreateAppManifest();
            pb.Value = 80;
            winForms.CreateRoots();
            pb.Value = 90;

            pb.Value = 100;

            pbLabel.Content = "Done porting";

        }
    }
}   
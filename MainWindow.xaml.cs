using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DISM_Graphical_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void RunDism_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder arguments = new StringBuilder("/Online /loglevel:3");
            arguments.Append(" /Cleanup-Image /StartComponentCleanup");

            if (cbRestoreHealth.IsChecked == true)
            {
                arguments.Append(" /ScanHealth");
                await RunDismCommandAsync(arguments.ToString());

                arguments.Clear();
                LogMessage("Args= { " + arguments + " }");
                arguments.Append("/Online /Cleanup-Image /RestoreHealth");
                await RunDismCommandAsync(arguments.ToString());
                LogMessage("DISM Restore Health Task Completed");
            }
            else
            {
                await RunDismCommandAsync(arguments.ToString());
                LogMessage("DISM Scan and Repair Task Completed");

            }
        }


        private async Task RunDismCommandAsync(string arguments)
        {
            tbOutput.Clear();
            LogMessage("Starting DISM Cleanup operation\n");

            Process dismProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                }
            };

            dismProcess.Start();
            dismProcess.BeginOutputReadLine();
            dismProcess.BeginErrorReadLine();

            // Event handler for output data received
            dismProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // Check if the line contains a percentage
                    int percentageIndex = args.Data.IndexOf('%');
                    if (percentageIndex != -1)
                    {
                        // Extract the percentage value
                        string percentageString = args.Data.Substring(percentageIndex - 4, 4).Trim();
                        int percentage;
                        if (int.TryParse(percentageString, out percentage))
                        {
                            // Debug statement to verify parsed percentage
                            Debug.WriteLine("Parsed Percentage: " + percentage);

                            // Update the progress bar
                            Dispatcher.Invoke(() => dismprogressbar.Value = percentage);
                        }
                        else
                        {
                            LogMessage("Failed to parse progress");
                        }
                    }
                    else
                    {
                        // Output non-percentage lines to the log
                        LogMessage("Failed to parse progress");
                        Dispatcher.Invoke(() => tbOutput.AppendText(args.Data + "\n"));
                    }
                }
            };

            dismProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    Dispatcher.Invoke(() => tbOutput.AppendText("ERROR: " + args.Data + "\n"));
                }
            };



            await Task.Run(() => dismProcess.WaitForExit());
            LogMessage("DISM Restore Health Task Completed\n");
        }

        private async Task RunSfcCommandAsync()
        {
            tbOutput.Clear();
            LogMessage("Starting SFC scan...\n");

            Process sfcProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sfc.exe",
                    Arguments = "/scannow",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ErrorDialog = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    Verb = "runas"

                }
            };

            sfcProcess.Start();
            sfcProcess.BeginOutputReadLine();
            sfcProcess.BeginErrorReadLine();

            sfcProcess.OutputDataReceived += (sender, args) =>
            {
                LogMessage("SFC" + ": " + args.Data);
                Console.WriteLine("SFC: " + args.Data);
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // Check if the line contains a percentage
                    int percentageIndex = args.Data.IndexOf('%');
                    if (percentageIndex != -1)
                    {
                        // Extract the percentage value
                        string percentageString = args.Data.Substring(percentageIndex - 4, 4).Trim();
                        int percentage;
                        if (int.TryParse(percentageString, out percentage))
                        {
                            // Debug statement to verify parsed percentage
                            Debug.WriteLine("Parsed Percentage: " + percentage);

                            // Update the progress bar
                            Dispatcher.Invoke(() => dismprogressbar.Value = percentage);
                        }
                        else
                        {
                            LogMessage("Failed to parse progress\n");
                            Console.WriteLine("Failed to parse progess\n");
                        }
                    }
                    else
                    {
                        // Output non-percentage lines to the log
                        LogMessage("Failed to parse progress\n");
                        Console.WriteLine("Failed to parse progess\n");
                        Dispatcher.Invoke(() => tbOutput.AppendText(args.Data + "\n"));
                    }
                }
            };

            sfcProcess.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    Dispatcher.Invoke(() => tbOutput.AppendText("ERROR: " + args.Data + "\n"));
                }
            };

            await Task.Run(() => sfcProcess.WaitForExit());
        }

        private async void RunSFC_Click(object sender, RoutedEventArgs e)
        {

            await RunSfcCommandAsync();
            LogMessage("SFC Scan Complete\n");
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() => tbOutput.AppendText(message + "\n"));
        }


        private void dismprogressbar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void runFileClear(object sender, RoutedEventArgs e)
        {
            ClearWinUpdates clr = new ClearWinUpdates();
            clr.ClearSoftwareDistribution();
        }

    }

    public class ClearWinUpdates
    {
        // Method to stop Windows Update services
        private void StopUpdateServices()
        {
            ExecuteCommand("net stop wuauserv");
            ExecuteCommand("net stop bits");
            ExecuteCommand("net stop cryptSvc");
            ExecuteCommand("net stop msiserver");
        }

        // Method to start Windows Update services
        private void StartUpdateServices()
        {
            ExecuteCommand("net start wuauserv");
            ExecuteCommand("net start bits");
            ExecuteCommand("net start cryptSvc");
            ExecuteCommand("net start msiserver");
        }

        // Method to execute command in Command Prompt
        private void ExecuteCommand(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + command,
                Verb = "runas", // Run as administrator
                UseShellExecute = true
            };
            Process.Start(psi)?.WaitForExit();
        }

        // Method to delete contents of SoftwareDistribution folder
        private void ClearSoftwareDistributionFolder()
        {
            string softwareDistributionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution");
            DirectoryInfo di = new DirectoryInfo(softwareDistributionPath);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        // Method to perform the entire process of clearing SoftwareDistribution folder
        public void ClearSoftwareDistribution()
        {
            StopUpdateServices();
            ClearSoftwareDistributionFolder();
            StartUpdateServices();
        }
    }
}

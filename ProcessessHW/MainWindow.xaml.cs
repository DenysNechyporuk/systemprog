using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace _02_TaskManager;

// Task 1
public partial class MainWindow : Window
{
    DispatcherTimer timer = new DispatcherTimer();

    public MainWindow()
    {
        InitializeComponent();
        timer.Interval = TimeSpan.FromSeconds(2);
        timer.Tick += Timer_Tick;
        timer.Start();
        Refresh(null, null);
    }

    private void Refresh(object? sender, RoutedEventArgs? e)
    {
        int selectedId = -1;
        if (grid.SelectedItem is ProcessInfo selectedInfo)
        {
            selectedId = selectedInfo.Id;
        }

        List<ProcessInfo> processes = new List<ProcessInfo>();
        foreach (Process process in Process.GetProcesses())
        {
            processes.Add(new ProcessInfo(process));
        }
        grid.ItemsSource = processes;

        foreach (ProcessInfo info in processes)
        {
            if (info.Id == selectedId)
            {
                grid.SelectedItem = info;
                break;
            }
        }

        statusText.Text = "Processes count :: " + processes.Count;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        Refresh(null, null);
    }

    private void RadioButton_Click(object sender, RoutedEventArgs e)
    {
        int seconds = int.Parse(((RadioButton)sender).Content.ToString()!);
        timer.Interval = TimeSpan.FromSeconds(seconds);
        timer.Start();
    }

    private void Stop_Timer(object sender, RoutedEventArgs e)
    {
        timer.Stop();
        intervalOne.IsChecked = false;
        intervalTwo.IsChecked = false;
        intervalFive.IsChecked = false;
    }

    private void Kill_Process(object sender, RoutedEventArgs e)
    {
        if (grid.SelectedItem is not ProcessInfo selectedInfo)
        {
            MessageBox.Show("Select process first");
            return;
        }

        try
        {
            Process process = Process.GetProcessById(selectedInfo.Id);
            process.Kill();
            process.WaitForExit(1000);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

        Refresh(null, null);
    }

    private void Show_Details(object sender, RoutedEventArgs e)
    {
        if (grid.SelectedItem is not ProcessInfo selectedInfo)
        {
            MessageBox.Show("Select process first");
            return;
        }

        try
        {
            Process process = Process.GetProcessById(selectedInfo.Id);

            string path;
            try
            {
                path = process.MainModule!.FileName;
            }
            catch
            {
                path = "N/A";
            }

            string details = "Name :: " + process.ProcessName + "\n"
                + "PID :: " + process.Id + "\n"
                + "Path :: " + path + "\n"
                + "Start time :: " + selectedInfo.StartTime + "\n"
                + "Total processor time :: " + selectedInfo.TotalProcessorTime + "\n"
                + "Priority :: " + selectedInfo.Priority + "\n"
                + "State :: " + selectedInfo.State + "\n"
                + "Threads count :: " + process.Threads.Count + "\n"
                + "Memory (MB) :: " + process.WorkingSet64 / 1024 / 1024;

            MessageBox.Show(details, "Process details");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void Go_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(processPathTextBox.Text);
            Refresh(null, null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
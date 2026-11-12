using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

namespace HTTPHW;

public partial class MainWindow : Window
{
    ObservableCollection<DownloadInfo> downloads = new ObservableCollection<DownloadInfo>();

    public MainWindow()
    {
        InitializeComponent();
        downloadsListView.ItemsSource = downloads;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new SaveFileDialog();
        try
        {
            dialog.FileName = Path.GetFileName(new Uri(urlTextBox.Text).LocalPath);
        }
        catch (Exception)
        { }

        if (dialog.ShowDialog() == true)
            pathTextBox.Text = dialog.FileName;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(urlTextBox.Text) || string.IsNullOrWhiteSpace(pathTextBox.Text))
        {
            MessageBox.Show("Enter URL and choose where to save file!");
            return;
        }

        Uri address;
        try
        {
            address = new Uri(urlTextBox.Text.Trim());
        }
        catch (Exception)
        {
            MessageBox.Show("Wrong URL!");
            return;
        }

        DownloadInfo info = new DownloadInfo()
        {
            FileName = Path.GetFileName(pathTextBox.Text),
            FilePath = pathTextBox.Text,
            Status = "Waiting..."
        };
        downloads.Add(info);

        urlTextBox.Text = "";
        pathTextBox.Text = "";

        WebClient client = new WebClient();
        info.Client = client;
        client.DownloadProgressChanged += (s, args) =>
        {
            string status = $"Downloading... {args.ProgressPercentage}%";
            if (info.Status == status || info.Status == "Canceled")
                return;
            info.TotalBytes = args.TotalBytesToReceive;
            info.Status = status;
            downloadsListView.Items.Refresh();
        };

        try
        {
            await client.DownloadFileTaskAsync(address, info.FilePath);
            info.Status = "Completed";
            info.TotalBytes = new FileInfo(info.FilePath).Length;
        }
        catch (Exception ex)
        {
            if (info.Status == "Canceled")
            {
                if (File.Exists(info.FilePath))
                    File.Delete(info.FilePath);
            }
            else
            {
                info.Status = "Error : " + ex.Message;
            }
        }
        downloadsListView.Items.Refresh();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (downloadsListView.SelectedItem == null)
        {
            MessageBox.Show("Select download!");
            return;
        }

        DownloadInfo info = (DownloadInfo)downloadsListView.SelectedItem;
        if (info.Client != null && info.Client.IsBusy)
        {
            info.Status = "Canceled";
            info.Client.CancelAsync();
            downloadsListView.Items.Refresh();
        }
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (downloadsListView.SelectedItem == null)
        {
            MessageBox.Show("Select download!");
            return;
        }
        OpenFile((DownloadInfo)downloadsListView.SelectedItem);
    }

    private void DownloadsListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (downloadsListView.SelectedItem != null)
            OpenFile((DownloadInfo)downloadsListView.SelectedItem);
    }

    private void OpenFile(DownloadInfo info)
    {
        if (info.Status != "Completed")
        {
            MessageBox.Show("File is not downloaded yet!");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(info.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}

public class DownloadInfo
{
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public long TotalBytes { get; set; }
    public string Status { get; set; }
    public WebClient Client { get; set; }

    public string SizeText
    {
        get
        {
            if (TotalBytes <= 0)
                return "-";
            return $"{(float)TotalBytes / 1024 / 1024:F2} Mb";
        }
    }
}

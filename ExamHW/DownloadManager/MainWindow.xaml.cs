using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;

namespace DownloadManager;

public partial class MainWindow : Window
{
    static HttpClient client = new HttpClient();
    List<DownloadItem> downloads = new List<DownloadItem>();

    public MainWindow()
    {
        InitializeComponent();
        folderTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        ShowDownloads();
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
            folderTextBox.Text = dialog.FolderName;
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(urlTextBox.Text) || string.IsNullOrWhiteSpace(folderTextBox.Text))
        {
            MessageBox.Show("Enter URL and save folder!");
            return;
        }

        if (!Directory.Exists(folderTextBox.Text))
        {
            MessageBox.Show("Folder doesn't exist!");
            return;
        }

        Uri uri;
        try
        {
            uri = new Uri(urlTextBox.Text.Trim());
        }
        catch (Exception)
        {
            MessageBox.Show("Wrong URL!");
            return;
        }

        string fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "file";

        string filePath = Path.Combine(folderTextBox.Text, fileName);
        int number = 1;
        while (File.Exists(filePath) || downloads.Any(d => d.FilePath == filePath))
        {
            filePath = Path.Combine(folderTextBox.Text, $"{Path.GetFileNameWithoutExtension(fileName)}({number}){Path.GetExtension(fileName)}");
            number++;
        }

        DownloadItem item = new DownloadItem()
        {
            Url = uri.ToString(),
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            Tags = tagsTextBox.Text.Trim(),
            Threads = int.Parse(((ComboBoxItem)threadsComboBox.SelectedItem).Content.ToString()!),
            Status = "Starting...",
            State = "Active"
        };

        downloads.Add(item);
        urlTextBox.Text = "";
        tagsTextBox.Text = "";
        ShowDownloads();

        await StartDownload(item);
        ShowDownloads();
    }

    private async Task StartDownload(DownloadItem item)
    {
        try
        {
            long size = 0;
            bool canUseParts = false;
            try
            {
                var head = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, item.Url));
                if (head.IsSuccessStatusCode)
                {
                    size = head.Content.Headers.ContentLength ?? 0;
                    canUseParts = size > 0 && head.Headers.AcceptRanges.Contains("bytes");
                }
            }
            catch (Exception)
            { }

            if (!canUseParts)
                item.Threads = 1;
            item.Size = size;

            using (FileStream fs = new FileStream(item.FilePath, FileMode.Create))
            {
                if (size > 0)
                    fs.SetLength(size);
            }

            List<Task> tasks = new List<Task>();
            long partSize = size / item.Threads;
            for (int i = 0; i < item.Threads; i++)
            {
                long from = i * partSize;
                long to = i == item.Threads - 1 ? size - 1 : from + partSize - 1;
                tasks.Add(DownloadPart(item, from, to, canUseParts));
            }
            await Task.WhenAll(tasks);

            if (item.IsStopped)
            {
                item.Status = "Stopped";
                item.State = "Failed";
                if (File.Exists(item.FilePath))
                    File.Delete(item.FilePath);
            }
            else
            {
                if (item.Size == 0)
                    item.Size = item.Downloaded;
                item.Status = "Completed";
                item.State = "Completed";
            }
        }
        catch (Exception ex)
        {
            item.Status = "Error : " + ex.Message;
            item.State = "Failed";
            item.IsStopped = true;
            try
            {
                if (File.Exists(item.FilePath))
                    File.Delete(item.FilePath);
            }
            catch (Exception)
            { }
        }
    }

    private async Task DownloadPart(DownloadItem item, long from, long to, bool useRange)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, item.Url);
        if (useRange)
            request.Headers.Range = new RangeHeaderValue(from, to);

        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        if (useRange && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
            throw new Exception("Server doesn't support download in parts");

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using FileStream fs = new FileStream(item.FilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
        fs.Seek(useRange ? from : 0, SeekOrigin.Begin);

        byte[] buffer = new byte[65536];
        int count;
        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            while (item.IsPaused && !item.IsStopped)
                await Task.Delay(200);

            if (item.IsStopped)
                return;

            await fs.WriteAsync(buffer, 0, count);
            item.Downloaded += count;
            UpdateProgress(item);
        }
    }

    private void UpdateProgress(DownloadItem item)
    {
        if (item.IsPaused)
            return;

        int percent = item.Size > 0 ? (int)(item.Downloaded * 100 / item.Size) : 0;
        if (percent == item.LastPercent && item.Size > 0)
            return;

        item.LastPercent = percent;
        item.Status = item.Size > 0 ? $"Downloading {percent}%" : $"Downloading {item.SizeText}";
        downloadsListView.Items.Refresh();
    }

    private void ShowDownloads()
    {
        IEnumerable<DownloadItem> list = downloads;

        if (filterComboBox.SelectedIndex == 1)
            list = list.Where(d => d.State == "Active");
        else if (filterComboBox.SelectedIndex == 2)
            list = list.Where(d => d.State == "Completed");
        else if (filterComboBox.SelectedIndex == 3)
            list = list.Where(d => d.State == "Failed");

        if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
            list = list.Where(d => d.HasTag(searchTextBox.Text));

        downloadsListView.ItemsSource = list.ToList();
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (downloadsListView != null)
            ShowDownloads();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (downloadsListView != null)
            ShowDownloads();
    }

    private void DownloadsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (downloadsListView.SelectedItem != null)
            newNameTextBox.Text = ((DownloadItem)downloadsListView.SelectedItem).FileName;
    }

    private DownloadItem? GetSelected()
    {
        if (downloadsListView.SelectedItem == null)
        {
            MessageBox.Show("Select download!");
            return null;
        }
        return (DownloadItem)downloadsListView.SelectedItem;
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadItem? item = GetSelected();
        if (item == null || item.State != "Active")
            return;

        item.IsPaused = !item.IsPaused;
        if (item.IsPaused)
        {
            item.Status = "Paused";
        }
        else
        {
            item.LastPercent = -1;
            item.Status = "Downloading...";
        }
        downloadsListView.Items.Refresh();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadItem? item = GetSelected();
        if (item == null || item.State != "Active")
            return;

        item.IsStopped = true;
        item.Status = "Stopping...";
        downloadsListView.Items.Refresh();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadItem? item = GetSelected();
        if (item == null)
            return;

        if (MessageBox.Show($"Delete {item.FileName}?", "Delete", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        if (item.State == "Active")
        {
            item.IsStopped = true;
            while (item.State == "Active")
                await Task.Delay(100);
        }

        try
        {
            if (File.Exists(item.FilePath))
                File.Delete(item.FilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

        downloads.Remove(item);
        ShowDownloads();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadItem? item = GetSelected();
        if (item == null)
            return;

        if (item.State != "Completed")
        {
            MessageBox.Show("File is not downloaded!");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadItem? item = GetSelected();
        if (item == null)
            return;

        if (item.State != "Completed")
        {
            MessageBox.Show("Only downloaded files can be moved!");
            return;
        }

        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() != true)
            return;

        string newPath = Path.Combine(dialog.FolderName, item.FileName);
        try
        {
            File.Move(item.FilePath, newPath);
            item.FilePath = newPath;
            downloadsListView.Items.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        DownloadItem? item = GetSelected();
        if (item == null)
            return;

        if (item.State != "Completed")
        {
            MessageBox.Show("Only downloaded files can be renamed!");
            return;
        }

        if (string.IsNullOrWhiteSpace(newNameTextBox.Text))
        {
            MessageBox.Show("Enter new name!");
            return;
        }

        string newPath = Path.Combine(Path.GetDirectoryName(item.FilePath)!, newNameTextBox.Text.Trim());
        try
        {
            File.Move(item.FilePath, newPath);
            item.FilePath = newPath;
            item.FileName = Path.GetFileName(newPath);
            downloadsListView.Items.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}

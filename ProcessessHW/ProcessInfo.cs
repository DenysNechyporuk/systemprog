using System.Diagnostics;

namespace _02_TaskManager;

public class ProcessInfo
{
    public string ProcessName { get; set; } = string.Empty;
    public int Id { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string TotalProcessorTime { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    public ProcessInfo(Process process)
    {
        ProcessName = process.ProcessName;
        Id = process.Id;

        try
        {
            StartTime = process.StartTime.ToString("HH:mm:ss");
        }
        catch
        {
            StartTime = "N/A";
        }

        try
        {
            TotalProcessorTime = process.TotalProcessorTime.ToString(@"hh\:mm\:ss");
        }
        catch
        {
            TotalProcessorTime = "N/A";
        }

        try
        {
            Priority = process.PriorityClass.ToString();
        }
        catch
        {
            Priority = "N/A";
        }

        try
        {
            State = process.Responding ? "Running" : "Not Responding";
        }
        catch
        {
            State = "N/A";
        }
    }
}
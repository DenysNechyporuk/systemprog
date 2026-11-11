namespace DownloadManager
{
    public class DownloadItem
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Tags { get; set; }
        public int Threads { get; set; }
        public long Size { get; set; }
        public long Downloaded { get; set; }
        public string Status { get; set; }
        public string State { get; set; }
        public bool IsPaused { get; set; }
        public bool IsStopped { get; set; }
        public int LastPercent { get; set; } = -1;

        public string SizeText
        {
            get
            {
                long bytes = Size > 0 ? Size : Downloaded;
                if (bytes <= 0)
                    return "-";
                if (bytes < 1024 * 1024)
                    return $"{(float)bytes / 1024:F1} Kb";
                return $"{(float)bytes / 1024 / 1024:F2} Mb";
            }
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(Tags))
                return false;

            foreach (string item in Tags.Split(','))
            {
                if (item.Trim().ToLower().Contains(tag.Trim().ToLower()))
                    return true;
            }
            return false;
        }
    }
}

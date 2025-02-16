using System.Diagnostics;
using System.Text.Json.Serialization;

namespace GameShuffler
{
    public class GameInfo
    {
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
        [JsonIgnore]
        public Process? Process { get; set; }
        public bool Pause { get; set; }
        public bool MakeFullscreen { get; set; } = true;
        public GameInfo? ConnectedGame { get; set; }

        public GameInfo(string processName, string windowTitle, bool pause)
        {
            ProcessName = processName;
            WindowTitle = windowTitle;
            Pause = pause;
        }
    }
}

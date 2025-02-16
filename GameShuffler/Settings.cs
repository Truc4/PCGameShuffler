using System.Collections.Generic;

namespace GameShuffler
{
    public class Settings
    {
        public List<int> SelectedProcessIds { get; set; } = new List<int>();
        public List<GameInfo> DesiredGamesToShuffle { get; set; } = new List<GameInfo>();
        public int MinShuffleTime { get; set; }
        public int MaxShuffleTime { get; set; }
    }
}

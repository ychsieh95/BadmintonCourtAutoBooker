using System.Collections.Generic;

namespace BadmintonCourtAutoBooker
{
    internal class SportCenter
    {
        public string Name { get; set; }

        public string BaseUrl { get; set; }

        public string ModuleName { get; set; }

        public Dictionary<string, int> Courts { get; set; }

        public int DayDiff { get; set; }
    }
}

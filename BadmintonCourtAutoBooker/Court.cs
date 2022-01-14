using System;

namespace BadmintonCourtAutoBooker
{
    internal class Court
    {
        public DateTime Date { get; set; }

        public string Time { get; set; }

        public string Name { get; set; }

        public int Rent { get; set; }

        public bool Available { get; set; } = false;

        public int Id { get; set; }

        public int TimeCode { get; set; }
    }
}

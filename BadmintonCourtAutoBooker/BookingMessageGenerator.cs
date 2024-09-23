using System;

namespace BadmintonCourtAutoBooker
{
    internal class BookingMessageGenerator
    {
        public string SportCenterName { get; set; }

        public string CourtName { get; set; }

        public int CourtCode { get; set; }

        public DateTime DestDate { get; set; }

        public int DestTime { get; set; }

        public string Username { get; set; }

        public string GetMessage(bool bookingState) =>
            $"{(bookingState ? "Success" : "Failed")} to book the court {SportCenterName} {CourtName}({CourtCode}) (" +
            $"date_code={DestDate:yyyy-MM-dd}, " +
            $"time_code={DestTime}), " +
            $"account={Username.Substring(0, 5)}{new string('*', Username.Length - 5)})";
    }
}

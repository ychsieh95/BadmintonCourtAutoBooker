using System;
using System.Collections.Generic;

namespace BadmintonCourtAutoBooker
{
    internal class BackgroundWrokerArguments
    {
        #region Account settings

        public string Username { get; set; }

        public string Password { get; set; }

        #endregion

        #region Booking settings

        public string BaseUrl { get; set; }

        public string ModuleName { get; set; }

        public DateTime DestDate { get; set; }

        public List<int> BookingTimeCodes { get; set; }

        public List<int> BookingCourtCodes { get; set; }

        public bool BookingWithoutCheckOpenState { get; set; }

        #endregion

        #region Monitor settings

        public List<int> MonitorTimeCodes { get; set; }

        public List<int> MonitorCourtCodes { get; set; }

        public DateTime MonitorDateTime { get; set; }

        public int MonitorIntervalSeconds { get; set; }

        public bool MonitorTimeUnduplicated { get; set; }

        public bool MonitorBaseOnBookingSettings { get; set; }

        public bool MonitorBySingleThread { get; set; }

        #endregion

        public int Id { get; set; }
    }
}

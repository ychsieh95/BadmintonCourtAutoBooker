using System;
using System.Collections.Generic;

namespace BadmintonCourtAutoBooker
{
    internal class OrderListItem
    {
        #region In List

        public DateTime Date { get; set; }

        public string OrderId { get; set; }

        public string ReceiptId { get; set; }

        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public bool IsOrderDate { get; set; }

        public DateTime OrderDate { get; set; }

        public string OrderDateStr { get; set; }

        public bool IsOrderTime { get; set; }

        public int OrderTime { get; set; }

        public string OrderTimeStr { get; set; }

        public OrderStatus Status { get; set; }

        public string Remark { get; set; }

        public Dictionary<string, string> ViewOnClickDict { get; set; }

        public Dictionary<string, string> CancelOnClickDict { get; set; }

        #endregion

        #region Appendage

        public string CourtName { get; set; }

        #endregion
    }
}

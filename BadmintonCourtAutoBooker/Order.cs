using System;

namespace BadmintonCourtAutoBooker
{
    internal class Order
    {
        public DateTime Date { get; set; }

        public string OrderId { get; set; }

        public string SalesOrderId { get; set; }

        public string ReceiptId { get; set; }

        public string MemberName { get; set; }

        public string MemberId { get; set; }

        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public bool IsOrderDate { get; set; }

        public DateTime OrderDate { get; set; }

        public string OrderDateStr { get; set; }

        public bool IsOrderTime { get; set; }

        public int OrderTime { get; set; }

        public string OrderTimeStr { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public string DayOfWeekStr { get; set; }

        public OrderStatus Status { get; set; }

        public string CourtName { get; set; }

        public string PhoneNumber { get; set; }

        public string Lessee { get; set; }
    }

    public enum DayOfWeek
    {
        星期日,
        星期一,
        星期二,
        星期三,
        星期四,
        星期五,
        星期六
    }

    public enum OrderStatus
    {
        所有,
        繳費,
        未繳費,
        取消,
        退費
    }
}

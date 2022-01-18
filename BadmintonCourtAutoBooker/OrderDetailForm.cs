using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BadmintonCourtAutoBooker
{
    public partial class OrderDetailForm : Form
    {
        internal Order Order { get; set; }

        public OrderDetailForm() => InitializeComponent();

        private void OrderDetailForm_Load(object sender, EventArgs e)
        {
            orderListView.View = View.Details;
            orderListView.FullRowSelect = true;
            orderListView.MultiSelect = false;
            orderListView.Columns.Add("Item", 100);
            orderListView.Columns.Add("Content", 250);

            List<string[]> strList = new List<string[]>()
            {
                new string[] { "訂購日期", Order.Date.ToString("yyyy-MM-dd (ddd.)") },
                new string[] { "訂單編號", Order.OrderId },
                new string[] { "銷售單號", Order.SalesOrderId },
                new string[] { "發票號碼", Order.ReceiptId },
                new string[] { "會員姓名", Order.MemberName },
                new string[] { "身分證字號", Order.MemberId },
                new string[] { "產品名稱", Order.ProductName },
                new string[] { "金額", Order.Price.ToString() },
                new string[] { "租用日期", Order.IsOrderDate ? Order.OrderDate.ToString("yyyy-MM-dd (ddd.)") : Order.OrderDateStr },
                new string[] { "預約時段", Order.IsOrderTime ? $"{Order.OrderTime:00} ~ {Order.OrderTime + 1:00}" : Order.OrderTimeStr },
                new string[] { "預約場地", Order.CourtName },
                new string[] { "狀態", Order.Status.ToString() },
                new string[] { "聯絡電話", Order.PhoneNumber },
                new string[] { "租用人", Order.Lessee }
            };
            foreach (string[] str in strList)
            {
                ListViewItem listViewItem = new ListViewItem(str);
                if (str[0] == "狀態")
                {
                    listViewItem.UseItemStyleForSubItems = false;
                    
                    switch (Order.Status)
                    {
                        case OrderStatus.繳費:
                            listViewItem.SubItems[1].ForeColor = Color.Blue;
                            break;
                        case OrderStatus.退費:
                            listViewItem.SubItems[1].ForeColor = Color.Green;
                            break;
                        case OrderStatus.未繳費:
                        case OrderStatus.取消:
                            listViewItem.SubItems[1].ForeColor = Color.Red;
                            break;
                    }
                }
                orderListView.Items.Add(listViewItem);
            }

            this.SetAttributes(text: $"Order {Order.OrderId} Detail", icon: Properties.Resources.badminton_512x512);
        }
    }
}

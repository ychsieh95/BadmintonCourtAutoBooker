using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BadmintonCourtAutoBooker
{
    public partial class OrderListForm : Form
    {
        internal string Username { get; set; }

        internal string Password { get; set; }

        internal SportCenter SportCenter { get; set; }

        private List<OrderListItem> orderListItems;
        private BookingBot bookingBot;

        public OrderListForm() => InitializeComponent();

        private void OrderListForm_Load(object sender, EventArgs e)
        {
            splitContainer1.Cursor = Cursors.Default;

            viewDetailToolStripMenuItem.Image = Properties.Resources.View_Detail;
            cancelToolStripMenuItem.Image = Properties.Resources.Cancel;

            beginDateDateTimePicker.Format = endDateDateTimePicker.Format = DateTimePickerFormat.Custom;
            beginDateDateTimePicker.CustomFormat = endDateDateTimePicker.CustomFormat = "yyyy-MM-dd (dddd)";
            beginDateDateTimePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddMonths(-1);
            endDateDateTimePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

            orderBeginDateDateTimePicker.Format = orderEndDateDateTimePicker.Format = DateTimePickerFormat.Custom;
            orderBeginDateDateTimePicker.CustomFormat = orderEndDateDateTimePicker.CustomFormat = "yyyy-MM-dd (dddd)";
            orderBeginDateDateTimePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddMonths(-1);
            orderEndDateDateTimePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0).AddDays(7);

            orderListView.View = View.Details;
            orderListView.ContextMenuStrip = orderListItemContextMenuStrip;
            orderListView.FullRowSelect = true;
            orderListView.MultiSelect = false;
            orderListView.Columns.Add("#", 30, HorizontalAlignment.Center);
            orderListView.Columns.Add("Date", 100, HorizontalAlignment.Center);
            orderListView.Columns.Add("Order ID", 100, HorizontalAlignment.Center);
            orderListView.Columns.Add("Receipt ID", 125, HorizontalAlignment.Center);
            orderListView.Columns.Add("Product Name", 100);
            orderListView.Columns.Add("Court Name", 100);
            orderListView.Columns.Add("Price", 50, HorizontalAlignment.Center);
            orderListView.Columns.Add("Order Date", 100, HorizontalAlignment.Center);
            orderListView.Columns.Add("Order Time", 100, HorizontalAlignment.Center);
            orderListView.Columns.Add("Status", 75);
            orderListView.Columns.Add("Remark", 75);
            orderListView.Items.Clear();

            orderStatusComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (OrderStatus orderStatus in Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().ToList())
            {
                orderStatusComboBox.Items.Add(orderStatus);
            }
            orderStatusComboBox.SelectedIndex = 0;

            LoginForm loginForm = new LoginForm()
            {
                Username = Username,
                Password = Password,
                SportCenter = SportCenter
            };
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Username = loginForm.Username;
                Password = loginForm.Password;
                bookingBot = loginForm.BookingBot;
                this.SetAttributes(text: "Order List", icon: Properties.Resources.badminton_512x512, minimizeBox: true);
            }
            else
            {
                this.Close();
            }
        }

        private void OrderListForm_Shown(object sender, EventArgs e) => searchButton.PerformClick();

        private void searchButton_Click(object sender, EventArgs e)
        {
            orderListView.Items.Clear();
            SetFormControl(false);

            DateTime beginDate = beginDateDateTimePicker.Value;
            DateTime endDate = endDateDateTimePicker.Value;
            DateTime orderBeginDate = orderBeginDateDateTimePicker.Value;
            DateTime orderEndDate = orderEndDateDateTimePicker.Value;
            string orderId = orderIdTextBox.Text;
            OrderStatus orderStatus = (OrderStatus)Enum.Parse(typeof(OrderStatus), orderStatusComboBox.SelectedItem.ToString());
            new System.Threading.Thread(() =>
            {
                orderListItems = bookingBot.GetOrderList(beginDate: beginDate, endDate: endDate, orderId: orderId);
                orderListItems = orderListItems.FindAll(order => orderBeginDate <= order.OrderDate && order.OrderDate <= orderEndDate);
                if (orderStatus != OrderStatus.所有)
                {
                    orderListItems = orderListItems.FindAll(order => order.Status == orderStatus);
                }
                InsertListViewItems(orderListItems);
                SetFormControl(true);
            }).Start();
        }

        private void orderListItemContextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (orderListView.SelectedItems.Count == 0)
            {
                e.Cancel = true;
            }
            else
            {
                OrderListItem selectOrder = orderListItems.First(order => order.OrderId == orderListView.SelectedItems[0].SubItems[2].Text);
                switch (selectOrder.Status)
                {
                    case OrderStatus.繳費:
                        cancelToolStripMenuItem.Enabled = true;
                        break;
                    default:
                        cancelToolStripMenuItem.Enabled = false;
                        break;
                }
            }
        }

        private void orderListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var senderList = (ListView)sender;
            var clickedItem = senderList.HitTest(e.Location).Item;
            if (clickedItem != null)
            {
                OrderListItem selectOrder = orderListItems.First(order => order.OrderId == clickedItem.SubItems[2].Text);
                if (selectOrder.ViewOnClickDict == null)
                {
                    MessageBox.Show($"Cannot view the order {selectOrder.OrderId}.", "Order List", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Order order = bookingBot.GetOrder(id: selectOrder.ViewOnClickDict["ID"], orderId: selectOrder.OrderId);
                    clickedItem.SubItems[5].Text = order.CourtName;
                }
            }
        }

        private void viewDetailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (orderListView.SelectedItems.Count > 0)
            {
                OrderListItem selectOrder = orderListItems.First(order => order.OrderId == orderListView.SelectedItems[0].SubItems[2].Text);
                new OrderDetailForm()
                {
                    Order = bookingBot.GetOrder(id: selectOrder.ViewOnClickDict["ID"], orderId: selectOrder.OrderId)
                }.ShowDialog();
            }
        }

        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (orderListView.SelectedItems.Count > 0)
            {
                OrderListItem selectOrder = orderListItems.First(order => order.OrderId == orderListView.SelectedItems[0].SubItems[2].Text);
                if (bookingBot.CancelOrder(id: selectOrder.ViewOnClickDict["ID"], orderId: selectOrder.OrderId))
                {
                    MessageBox.Show($"Cancelled order {selectOrder.OrderId} successful.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    searchButton.PerformClick();
                }
                else
                {
                    MessageBox.Show($"Cancelled order {selectOrder.OrderId} failed.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void InsertListViewItems(List<OrderListItem> orderListItems)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        InsertListViewItems(orderListItems: orderListItems);
                    }));
                }
                catch (InvalidAsynchronousStateException) { }
            }
            else
            {

                for (int i = 0; i < orderListItems.Count; i++)
                {
                    OrderListItem order = orderListItems[i];
                    ListViewItem listViewItem = new ListViewItem(
                        new string[]
                        {
                            (i + 1).ToString(),
                            order.Date.ToString("yyyy-MM-dd"),
                            order.OrderId,
                            order.ReceiptId,
                            order.ProductName,
                            order.CourtName,
                            order.Price.ToString(),
                            order.IsOrderDate ? order.OrderDate.ToString("yyyy-MM-dd") : order.OrderDateStr,
                            order.IsOrderTime ? order.OrderTime.ToString("00") : order.OrderTimeStr,
                            order.Status.ToString(),
                            order.Remark
                        })
                    {
                        UseItemStyleForSubItems = false
                    };
                    switch (order.Status)
                    {
                        case OrderStatus.繳費:
                            listViewItem.SubItems[9].ForeColor = Color.Blue;
                            break;
                        case OrderStatus.退費:
                            listViewItem.SubItems[9].ForeColor = Color.Green;
                            break;
                        case OrderStatus.未繳費:
                        case OrderStatus.取消:
                            listViewItem.SubItems[9].ForeColor = Color.Red;
                            break;
                    }
                    orderListView.Items.Add(listViewItem);
                }
            }
        }

        private void SetFormControl(bool status = true)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        SetFormControl(status: status);
                    }));
                }
                catch (InvalidAsynchronousStateException) { }
            }
            else
            {
                groupBox1.Enabled =
                    panel1.Enabled =
                    status;
            }
        }
    }
}

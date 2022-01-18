namespace BadmintonCourtAutoBooker
{
    partial class OrderListForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.orderEndDateDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.label6 = new System.Windows.Forms.Label();
            this.orderBeginDateDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.label5 = new System.Windows.Forms.Label();
            this.orderStatusComboBox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.searchButton = new System.Windows.Forms.Button();
            this.orderIdTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.endDateDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.beginDateDateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.orderListView = new System.Windows.Forms.ListView();
            this.orderListItemContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.viewDetailToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.orderListItemContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.orderEndDateDateTimePicker);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.orderBeginDateDateTimePicker);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.orderStatusComboBox);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.searchButton);
            this.groupBox1.Controls.Add(this.orderIdTextBox);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.endDateDateTimePicker);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.beginDateDateTimePicker);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1003, 138);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Search Settings";
            // 
            // orderEndDateDateTimePicker
            // 
            this.orderEndDateDateTimePicker.Location = new System.Drawing.Point(359, 51);
            this.orderEndDateDateTimePicker.Name = "orderEndDateDateTimePicker";
            this.orderEndDateDateTimePicker.Size = new System.Drawing.Size(250, 23);
            this.orderEndDateDateTimePicker.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(338, 57);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(15, 15);
            this.label6.TabIndex = 6;
            this.label6.Text = "~";
            // 
            // orderBeginDateDateTimePicker
            // 
            this.orderBeginDateDateTimePicker.Location = new System.Drawing.Point(82, 51);
            this.orderBeginDateDateTimePicker.Name = "orderBeginDateDateTimePicker";
            this.orderBeginDateDateTimePicker.Size = new System.Drawing.Size(250, 23);
            this.orderBeginDateDateTimePicker.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 55);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 15);
            this.label5.TabIndex = 4;
            this.label5.Text = "Order Date:";
            // 
            // orderStatusComboBox
            // 
            this.orderStatusComboBox.FormattingEnabled = true;
            this.orderStatusComboBox.Location = new System.Drawing.Point(82, 109);
            this.orderStatusComboBox.Name = "orderStatusComboBox";
            this.orderStatusComboBox.Size = new System.Drawing.Size(250, 23);
            this.orderStatusComboBox.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 112);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 15);
            this.label4.TabIndex = 10;
            this.label4.Text = "Status:";
            // 
            // searchButton
            // 
            this.searchButton.Location = new System.Drawing.Point(534, 108);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(75, 23);
            this.searchButton.TabIndex = 12;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
            // 
            // orderIdTextBox
            // 
            this.orderIdTextBox.Location = new System.Drawing.Point(82, 80);
            this.orderIdTextBox.Name = "orderIdTextBox";
            this.orderIdTextBox.Size = new System.Drawing.Size(250, 23);
            this.orderIdTextBox.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 15);
            this.label3.TabIndex = 8;
            this.label3.Text = "Order ID:";
            // 
            // endDateDateTimePicker
            // 
            this.endDateDateTimePicker.Location = new System.Drawing.Point(359, 22);
            this.endDateDateTimePicker.Name = "endDateDateTimePicker";
            this.endDateDateTimePicker.Size = new System.Drawing.Size(250, 23);
            this.endDateDateTimePicker.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(338, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(15, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "~";
            // 
            // beginDateDateTimePicker
            // 
            this.beginDateDateTimePicker.Location = new System.Drawing.Point(82, 22);
            this.beginDateDateTimePicker.Name = "beginDateDateTimePicker";
            this.beginDateDateTimePicker.Size = new System.Drawing.Size(250, 23);
            this.beginDateDateTimePicker.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Date:";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel1);
            this.splitContainer1.Size = new System.Drawing.Size(1009, 779);
            this.splitContainer1.SplitterDistance = 144;
            this.splitContainer1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.orderListView);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1009, 631);
            this.panel1.TabIndex = 0;
            // 
            // orderListView
            // 
            this.orderListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.orderListView.HideSelection = false;
            this.orderListView.Location = new System.Drawing.Point(0, 0);
            this.orderListView.Name = "orderListView";
            this.orderListView.Size = new System.Drawing.Size(1009, 631);
            this.orderListView.TabIndex = 0;
            this.orderListView.UseCompatibleStateImageBehavior = false;
            this.orderListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.orderListView_MouseDoubleClick);
            // 
            // orderListItemContextMenuStrip
            // 
            this.orderListItemContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewDetailToolStripMenuItem,
            this.cancelToolStripMenuItem});
            this.orderListItemContextMenuStrip.Name = "orderListItemContextMenuStrip";
            this.orderListItemContextMenuStrip.Size = new System.Drawing.Size(181, 70);
            this.orderListItemContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.orderListItemContextMenuStrip_Opening);
            // 
            // viewDetailToolStripMenuItem
            // 
            this.viewDetailToolStripMenuItem.Name = "viewDetailToolStripMenuItem";
            this.viewDetailToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.viewDetailToolStripMenuItem.Text = "View Detail";
            this.viewDetailToolStripMenuItem.Click += new System.EventHandler(this.viewDetailToolStripMenuItem_Click);
            // 
            // cancelToolStripMenuItem
            // 
            this.cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            this.cancelToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.cancelToolStripMenuItem.Text = "Cancel";
            this.cancelToolStripMenuItem.Click += new System.EventHandler(this.cancelToolStripMenuItem_Click);
            // 
            // OrderListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1009, 779);
            this.Controls.Add(this.splitContainer1);
            this.Name = "OrderListForm";
            this.Text = "OrderListForm";
            this.Load += new System.EventHandler(this.OrderListForm_Load);
            this.Shown += new System.EventHandler(this.OrderListForm_Shown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.orderListItemContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.TextBox orderIdTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker endDateDateTimePicker;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker beginDateDateTimePicker;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListView orderListView;
        private System.Windows.Forms.ComboBox orderStatusComboBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker orderEndDateDateTimePicker;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DateTimePicker orderBeginDateDateTimePicker;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ContextMenuStrip orderListItemContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem viewDetailToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
    }
}
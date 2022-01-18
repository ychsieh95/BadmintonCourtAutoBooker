namespace BadmintonCourtAutoBooker
{
    partial class OrderDetailForm
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
            this.orderListView = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // orderListView
            // 
            this.orderListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.orderListView.HideSelection = false;
            this.orderListView.Location = new System.Drawing.Point(0, 0);
            this.orderListView.Name = "orderListView";
            this.orderListView.Size = new System.Drawing.Size(364, 461);
            this.orderListView.TabIndex = 0;
            this.orderListView.UseCompatibleStateImageBehavior = false;
            // 
            // OrderDetailForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(364, 461);
            this.Controls.Add(this.orderListView);
            this.Name = "OrderDetailForm";
            this.Text = "OrderDetailForm";
            this.Load += new System.EventHandler(this.OrderDetailForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView orderListView;
    }
}
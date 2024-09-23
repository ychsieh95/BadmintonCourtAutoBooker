using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using RestSharp;
using static BadmintonCourtAutoBooker.BookingBot;

namespace BadmintonCourtAutoBooker
{
    public partial class MainForm : Form
    {
        /* Share with other forms */
        internal static List<SportCenter> SportCenters;

        /* Timer for waiting to book and monitor */
        private readonly Timer bookingTimer = new Timer() { Interval = 500, Enabled = false };
        /* BackgroundWorker for booking */
        private List<BackgroundWorkerPair> backgroundWorkerPairs;
        private List<int> courtBookingRecord;
        private System.Threading.Mutex mutex;

        private readonly List<ToolStripStatusLabel> toolStripStatusLabels = new List<ToolStripStatusLabel>();

        private SportCenter currentSportCenter;
        private DateTime waitDateTime;
        private bool isWaitBeginTime = false;
        private bool isWaitServiceTime = false;

        private const string IniVersion = "1.0";
        private const string IniPath = "./BadmintonCourtAutoBooker.ini";
        private const int CryptoKeySize = 128;
        private readonly string cryptoKey = Environment.UserName.Repeat((CryptoKeySize / Environment.UserName.Length) + 1)[..CryptoKeySize];

        private const string LogDirPath = "./logs";

        private Size checkButtonImageSize = new Size(24, 24);
        private Size toolStripMenuItemImageSize;
        private Size toolStripStatusLabelImageSize;

        private bool cancelByStopButton = false;

        private delegate void VoidDelegate();

        public MainForm()
        {
            SportCenters = new List<SportCenter>()
            {
                new SportCenter()
                {
                    Name = "新竹市立竹光國民運動中心",
                    BaseUrl = "https://scr.cyc.org.tw",
                    ModuleName = "tp16.aspx",
                    Courts = new Dictionary<string, int>()
                    {
                        { "羽1", 1175 },
                        { "羽2", 1174 },
                        { "羽3", 1176 },
                        { "羽4", 1192 }
                    },
                    DayDiff = 6
                },
                new SportCenter()
                {
                    Name = "新竹縣立竹北國民運動中心",
                    BaseUrl = "https://fe.xuanen.com.tw",
                    ModuleName = "fe02.aspx",
                    Courts = new Dictionary<string, int>()
                    {
                        { "2F-1", 83 },
                        { "2F-2", 84 },
                        { "2F-3", 1074 },
                        { "2F-4", 1075 },
                        { "2F-5", 87 },
                        { "2F-6", 88 },
                        { "2F-7", 2115 },
                        { "2F-8", 2116 },
                        { "4F-B1", 2123 },
                        { "4F-B2", 2124 },
                        { "4F-B3", 2125 },
                        { "4F-B4", 2126 },
                        { "4F-B5", 2127 },
                        { "4F-B6", 2128 }
                    },
                    DayDiff = 7
                }
            };

            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            DateTime dtNext1Day = DateTime.Now.AddDays(1);
            DateTime dtNextSelectDay = DateTime.Now;

            /* Initial variables */
            toolStripMenuItemImageSize = new Size(viewToolStripMenuItem.Size.Height, viewToolStripMenuItem.Size.Height);
            toolStripStatusLabelImageSize = new Size(versionToolStripStatusLabel.Size.Height, versionToolStripStatusLabel.Size.Height);

            /* MenuStrip */
            checkWebsiteStatusOnFirstExecutionToolStripMenuItem.CheckOnClick = true;
            orderListToolStripMenuItem.Image = new Bitmap(Properties.Resources.Order_List, toolStripMenuItemImageSize);

            /* LogListViewContextMenuStrip */
            exportToolStripMenuItem.Image = Properties.Resources.Export;
            clearToolStripMenuItem.Image = Properties.Resources.Clear;

            /* SplitContainer */
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Cursor = Cursors.Default;

            /* GroupBox of booking settings */
            sportCenterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sportCenterComboBox.Items.Clear();
            foreach (SportCenter sportCenter in SportCenters)
            {
                sportCenterComboBox.Items.Add(sportCenter.Name);
            }

            timeCheckedListBox.Items.Clear();
            for (int i = 6; i <= 21; i++)
            {
                timeCheckedListBox.Items.Add(string.Format("{0:00}:00-{1:00}:00", i, i + 1));
            }

            courtCheckedListBox.Items.Clear();

            /* GroupBox of monitor settings */
            untilDateTimePicker.Format = DateTimePickerFormat.Custom;
            untilDateTimePicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            untilDateTimePicker.Value = new DateTime(dtNext1Day.Year, dtNext1Day.Month, dtNext1Day.Day, 0, 30, 0);

            intervalDateTimePicker.Format = DateTimePickerFormat.Custom;
            intervalDateTimePicker.CustomFormat = "HH:mm:ss";
            intervalDateTimePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 10);
            monitorCheckBox_CheckedChanged(null, null);

            bookingTimer.Tick += bookingTimer_Tick;

            /* GroupBox of notification settings */
            botTokenTextBox.UseSystemPasswordChar = true;
            channelIdTextBox.UseSystemPasswordChar = true;
            telegramNotifyCheckBox_CheckedChanged(null, null);

            /* GroupBox of actions */
            stopButton.Enabled = false;

            /* Log ListView */
            logListView.ContextMenuStrip = logListViewContextMenuStrip;
            logListView.View = View.Details;
            logListView.FullRowSelect = true;
            logListView.Columns.Add("#", 30, HorizontalAlignment.Center);
            logListView.Columns.Add("DateTime", 150, HorizontalAlignment.Center);
            logListView.Columns.Add("Type", 80, HorizontalAlignment.Center);
            logListView.Columns.Add("Message", 500);
            logListView.Items.Clear();

            /* statusStrip */
            statusStrip.ShowItemToolTips = true;
            statusStrip.AutoSize = false;

            versionToolStripStatusLabel.Text = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
            versionToolStripStatusLabel.Spring = true;

            toolStripStatusLabels.Add(new ToolStripStatusLabel()
            {
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                Image = new Bitmap(Properties.Resources.Circle_Gray, toolStripStatusLabelImageSize),
                ImageScaling = ToolStripItemImageScaling.None,
                Text = "https://ocr.holey.cc",
                ToolTipText = "OCR Website Status"
            });
            for (int i = 0; i < SportCenters.Count; i++)
            {
                toolStripStatusLabels.Add(new ToolStripStatusLabel()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Image = new Bitmap(Properties.Resources.Circle_Gray, toolStripStatusLabelImageSize),
                    ImageScaling = ToolStripItemImageScaling.None,
                    Text = $"{SportCenters[i].BaseUrl}/{SportCenters[i].ModuleName}",
                    ToolTipText = $"{SportCenters[i].Name} Status"
                });
            }
            for (int i = 0; i < toolStripStatusLabels.Count; i++)
            {
                toolStripStatusLabels[i].Click += ToolStripStatusLabel_Click;
                statusStrip.Items.Add(toolStripStatusLabels[i]);
            }

            usernameTextBox.SetWaterMark("[Username]");
            passwordTextBox.SetWaterMark("[Password]");
            this.SetAttributes(text: "BadmintonCourtAutoBooker", icon: Properties.Resources.badminton_512x512, minimizeBox: true);

            LoadConfigs();

            // After loading configs
            dtNextSelectDay = dtNextSelectDay.AddDays(currentSportCenter.DayDiff);
            dateDateTimePicker.Format = DateTimePickerFormat.Custom;
            dateDateTimePicker.CustomFormat = "yyyy-MM-dd (dddd)";
            dateDateTimePicker.Value = new DateTime(dtNextSelectDay.Year, dtNextSelectDay.Month, dtNextSelectDay.Day + 1, 0, 0, 0);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (!System.Diagnostics.Debugger.IsAttached &&
                checkWebsiteStatusOnFirstExecutionToolStripMenuItem.Checked)
            {
                for (int i = 0; i < toolStripStatusLabels.Count; i++)
                {
                    toolStripStatusLabels[i].PerformClick();
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _ = WriteLogsAsync();
            SaveConfigs();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e) => new System.Threading.Thread(() => CancelBackgroundWorkers()).Start();

        private void orderListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OrderListForm orderListForm = new OrderListForm()
            {
                Username = usernameTextBox.Text,
                Password = passwordTextBox.Text,
                SportCenter = currentSportCenter
            };
            orderListForm.Show();
        }

        private void usernameTextBox_DoubleClick(object sender, EventArgs e) =>
            usernameTextBox.UseSystemPasswordChar = !usernameTextBox.UseSystemPasswordChar;

        private void passwordTextBox_DoubleClick(object sender, EventArgs e) =>
            passwordTextBox.UseSystemPasswordChar = !passwordTextBox.UseSystemPasswordChar;


        #region GroupBox of Account Settings

        private void tryLoginButton_Click(object sender, EventArgs e)
        {
            SetFormControl(false);
            string username = usernameTextBox.Text, password = passwordTextBox.Text;
            new System.Threading.Thread(() =>
            {
                BookingBot bookingBot = new BookingBot(currentSportCenter.BaseUrl, currentSportCenter.ModuleName);
                LoginStatus loginStatus = bookingBot.Login(usernameTextBox.Text, passwordTextBox.Text);
                Log("Trying login");
                switch (loginStatus)
                {
                    case LoginStatus.Success:
                        MessageBox.Show("Login successful.", "Try Login Response", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Log("Login successful", logType: LogType.Okay);
                        break;
                    default:
                        MessageBox.Show($"Login failed ({loginStatus}).", "Try Login Response", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Log("Login failed", logType: LogType.Failed);
                        break;
                }
                SetFormControl(true);
            }).Start();
        }

        private void usernameTextBox_MouseDoubleClick(object sender, MouseEventArgs e) =>
            usernameTextBox.UseSystemPasswordChar = !usernameTextBox.UseSystemPasswordChar;

        private void passwordTextBox_MouseDoubleClick(object sender, MouseEventArgs e) =>
            passwordTextBox.UseSystemPasswordChar = !passwordTextBox.UseSystemPasswordChar;

        #endregion

        #region GroupBox of Booking Settings

        private void sportCenterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SportCenter recordSportCenter = currentSportCenter;
            courtCheckedListBox.Items.Clear();
            foreach (var court in SportCenters.First(sportcCenter => sportcCenter.Name == sportCenterComboBox.SelectedItem.ToString()).Courts)
            {
                courtCheckedListBox.Items.Add(court.Key);
                currentSportCenter = SportCenters.First(sportcCenter => sportcCenter.Name == sportCenterComboBox.SelectedItem.ToString());
            }
            if (recordSportCenter != null &&
                dateDateTimePicker.Value.Date == DateTime.Now.AddDays(recordSportCenter.DayDiff + 1).Date)
            {
                dateDateTimePicker.Value = DateTime.Now.AddDays(currentSportCenter.DayDiff + 1);
            }
        }

        private void timeSelectCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bool checkStatus = timeCheckedListBox.CheckedItems.Count == 0;
            for (int i = 0; i < timeCheckedListBox.Items.Count; i++)
            {
                timeCheckedListBox.SetItemChecked(i, checkStatus);
            }
        }

        private void courtSelectCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            bool checkStatus = courtCheckedListBox.CheckedItems.Count == 0;
            for (int i = 0; i < courtCheckedListBox.Items.Count; i++)
            {
                courtCheckedListBox.SetItemChecked(i, checkStatus);
            }
        }

        #endregion

        #region GroupBox of Monitor Settings

        private void monitorCheckBox_CheckedChanged(object sender, EventArgs e) =>
            untilDateTimePicker.Enabled =
                intervalDateTimePicker.Enabled =
                timeUnduplicatedCheckBox.Enabled =
                monitorBaseOnBookingSettingsCheckBox.Enabled =
                monitorBySingleThreadCheckBox.Enabled =
                useMonitorCheckBox.Checked;

        #endregion

        #region GroupBox of Notification Settings

        private void telegramNotifyCheckBox_CheckedChanged(object sender, EventArgs e) =>
            botTokenTextBox.Enabled =
                channelIdTextBox.Enabled =
                sendTestMessageButton.Enabled =
                useTelegramToNotifyCheckBox.Checked;

        private void sendTestMessageButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(botTokenTextBox.Text) || string.IsNullOrWhiteSpace(channelIdTextBox.Text))
            {
                MessageBox.Show("Bot Token and Channel ID cannot be empty.", "Send Test Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                TelegramBot telegramBot = new TelegramBot(botToken: botTokenTextBox.Text, channelId: channelIdTextBox.Text);
                new Task(() =>
                {
                    _ = telegramBot.SendMessageByTelegramBotAsync($"[BadmintonCourtAutoBooker] This test message is sent at {DateTime.Now:yyyy-MM-dd HH:mm:ss}.");
                    MessageBox.Show("Sent test message successful.", "Send Test Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }).Start();

            }
        }

        #endregion

        #region GroupBox of Actions

        private void startButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(usernameTextBox.Text) || string.IsNullOrEmpty(passwordTextBox.Text))
            {
                MessageBox.Show("[Username] or [Password] cannot be empty.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (timeCheckedListBox.CheckedItems.Count == 0)
            {
                MessageBox.Show("At least one [Time] must be selected.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (courtCheckedListBox.CheckedItems.Count == 0)
            {
                MessageBox.Show("At least one [Count] must be selected.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                SetFormControl(false);

                waitDateTime = new DateTime(dateDateTimePicker.Value.Year, dateDateTimePicker.Value.Month, dateDateTimePicker.Value.Day)
                    .AddDays(-currentSportCenter.DayDiff)
                    .AddMinutes(-1);
                bookingTimer.Enabled = true;
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            cancelByStopButton = true;
            bookingTimer.Enabled = false;
            new System.Threading.Thread(() => CancelBackgroundWorkers()).Start();
        }

        #endregion

        private void logListViewContextMenuStrip_Opening(object sender, CancelEventArgs e) =>
            exportToolStripMenuItem.Enabled = logListView.SelectedItems.Count > 0;

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                AddExtension = true,
                CheckPathExists = true,
                DefaultExt = ".log",
                FileName = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{logListView.Items.Count}ea.log",
                Filter = "Log files|*.log|CSV (Comma delimited)|*.csv|Text (Tab delimited)|*.txt",
                FilterIndex = 0,
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory,
                OverwritePrompt = true,
                RestoreDirectory = true,
                Title = "Save Log As"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string logMessage = "";
                char delimitChar = saveFileDialog.FilterIndex switch
                {
                    1 => ',',
                    _ => '\t',
                };
                for (int i = 0; i < logListView.Items.Count; i++)
                {
                    for (int j = 0; j < logListView.Items[i].SubItems.Count; j++)
                    {
                        logMessage += $"{logListView.Items[i].SubItems[j].Text}{delimitChar}";
                    }
                    logMessage = logMessage.TrimEnd(delimitChar) + "\n";
                }

                System.IO.File.WriteAllText(saveFileDialog.FileName, logMessage);

                switch (MessageBox.Show("Saved log successful. Do you want to open it now?\n\nPress [Yes] to open log file, [No] to open the folder, [Cancel] to do nothing.", this.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        try
                        {
                            System.Diagnostics.Process.Start(saveFileDialog.FileName);
                        }
                        catch (Win32Exception)
                        {
                            System.Diagnostics.Process.Start("notepad.exe", saveFileDialog.FileName);
                        }
                        break;
                    case DialogResult.No:
                        System.Diagnostics.Process.Start("explorer.exe", saveFileDialog.FileName[..(saveFileDialog.FileName.LastIndexOf('\\') + 1)]);
                        break;
                    case DialogResult.Cancel:
                        break;
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e) => logListView.Items.Clear();

        private void ToolStripStatusLabel_Click(object sender, EventArgs e)
        {
            ToolStripStatusLabel toolStripStatusLabel = (ToolStripStatusLabel)sender;
            toolStripStatusLabel.ToolTipText = toolStripStatusLabel.ToolTipText.Split('(')[0].Trim();
            toolStripStatusLabel.Image = new Bitmap(Properties.Resources.Circle_Gray, toolStripStatusLabelImageSize);
            Log($"Testing connection of {toolStripStatusLabel.ToolTipText[..toolStripStatusLabel.ToolTipText.IndexOf(" Status")]}");

            new System.Threading.Thread(() =>
            {
                ChangeToolStripStatusLabelStatus(
                    toolStripStatusLabel,
                    CheckWebsiteStatus(toolStripStatusLabel.Text));
            }).Start();
        }

        private void bookingTimer_Tick(object sender, EventArgs e)
        {
            /* Wait for begin time */
            if (DateTime.Now < waitDateTime)
            {
                if (!isWaitBeginTime)
                {
                    Log($"Preparing to auto-booking, begining after {waitDateTime}");
                    isWaitBeginTime = true;
                }
                return;
            }
            else
            {
                if (isWaitBeginTime)
                {
                    Log("Finished to wait", logType: LogType.Okay);
                }
                isWaitBeginTime = false;
            }

            /* Wait for service time */
            if (2 <= DateTime.Now.Hour && DateTime.Now.Hour <= 4)
            {
                if (!isWaitServiceTime)
                {
                    Log("Waiting for service time to be finished");
                    isWaitServiceTime = true;
                }
                return;
            }
            else
            {
                if (isWaitServiceTime)
                {
                    Log("Finished to wait", logType: LogType.Okay);
                }
                isWaitServiceTime = false;
            }

            /* Disable bookingTimer after finished all waiting */
            bookingTimer.Enabled = false;

            List<int> timeCodes = new List<int>();
            for (int i = 0; i < timeCheckedListBox.CheckedItems.Count; i++)
            {
                timeCodes.Add(int.Parse(timeCheckedListBox.CheckedItems[i].ToString().Split(':')[0].TrimStart('0')));
            }
            List<int> courtCodes = new List<int>();
            for (int i = 0; i < courtCheckedListBox.CheckedItems.Count; i++)
            {
                courtCodes.Add(currentSportCenter.Courts[courtCheckedListBox.CheckedItems[i].ToString()]);
            }

            /* Initial variables */
            backgroundWorkerPairs = new List<BackgroundWorkerPair>();
            courtBookingRecord = new List<int>();
            mutex = new System.Threading.Mutex();
            int bgWorkerId = 1;
            if (bookingByMultiThreadCheckBox.Checked)
            {
                foreach (int timeCode in timeCodes)
                {
                    foreach (int courtCode in courtCodes)
                    {
                        BackgroundWorker backgroundWorker = new BackgroundWorker() { WorkerSupportsCancellation = true };
                        backgroundWorker.DoWork += backgroundWorker_DoWork;
                        BackgroundWrokerArguments backgroundWrokerArguments = new BackgroundWrokerArguments()
                        {
                            /* Account settings */
                            Username = usernameTextBox.Text,
                            Password = passwordTextBox.Text,
                            /* Booking settings */
                            BaseUrl = currentSportCenter.BaseUrl,
                            ModuleName = currentSportCenter.ModuleName,
                            DestDate = dateDateTimePicker.Value,
                            BookingTimeCodes = new List<int>() { timeCode },
                            BookingCourtCodes = new List<int>() { courtCode },
                            BookingWithoutCheckOpenState = withoutCheckOpenStateCheckBox.Checked,
                            /* Monitor settings */
                            UseMonitor = useMonitorCheckBox.Checked,
                            MonitorTimeCodes = timeCodes,
                            MonitorCourtCodes = courtCodes,
                            MonitorDateTime = untilDateTimePicker.Value,
                            MonitorIntervalSeconds = (((intervalDateTimePicker.Value.Hour * 60) + intervalDateTimePicker.Value.Minute) * 60) + intervalDateTimePicker.Value.Second,
                            MonitorTimeUnduplicated = timeUnduplicatedCheckBox.Checked,
                            MonitorBaseOnBookingSettings = monitorBaseOnBookingSettingsCheckBox.Checked,
                            MonitorBySingleThread = monitorBySingleThreadCheckBox.Checked,
                            /* Notify settings */
                            UseTelegramToNotify = useTelegramToNotifyCheckBox.Checked,
                            TelegramBot = new TelegramBot(botToken: botTokenTextBox.Text, channelId: channelIdTextBox.Text),
                            /* BackgroundWorker settings */
                            Id = bgWorkerId
                        };
                        backgroundWorkerPairs.Add(new BackgroundWorkerPair()
                        {
                            BackgroundWorker = backgroundWorker,
                            BackgroundWrokerArguments = backgroundWrokerArguments
                        });
                        Log("Created BackgroundWorker", logType: LogType.Okay, id: bgWorkerId++);
                    }
                }
            }
            else
            {
                BackgroundWorker backgroundWorker = new BackgroundWorker() { WorkerSupportsCancellation = true };
                backgroundWorker.DoWork += backgroundWorker_DoWork;
                BackgroundWrokerArguments backgroundWrokerArguments = new BackgroundWrokerArguments()
                {
                    /* Account settings */
                    Username = usernameTextBox.Text,
                    Password = passwordTextBox.Text,
                    /* Booking settings */
                    BaseUrl = currentSportCenter.BaseUrl,
                    ModuleName = currentSportCenter.ModuleName,
                    DestDate = dateDateTimePicker.Value,
                    BookingTimeCodes = timeCodes,
                    BookingCourtCodes = courtCodes,
                    BookingWithoutCheckOpenState = withoutCheckOpenStateCheckBox.Checked,
                    /* Monitor settings */
                    UseMonitor = useMonitorCheckBox.Checked,
                    MonitorTimeCodes = timeCodes,
                    MonitorCourtCodes = courtCodes,
                    MonitorDateTime = untilDateTimePicker.Value,
                    MonitorIntervalSeconds = (((intervalDateTimePicker.Value.Hour * 60) + intervalDateTimePicker.Value.Minute) * 60) + intervalDateTimePicker.Value.Second,
                    MonitorTimeUnduplicated = timeUnduplicatedCheckBox.Checked,
                    MonitorBaseOnBookingSettings = monitorBaseOnBookingSettingsCheckBox.Checked,
                    MonitorBySingleThread = monitorBySingleThreadCheckBox.Checked,
                    /* Notify settings */
                    UseTelegramToNotify = useTelegramToNotifyCheckBox.Checked,
                    TelegramBot = new TelegramBot(botToken: botTokenTextBox.Text, channelId: channelIdTextBox.Text),
                    /* BackgroundWorker settings */
                    Id = bgWorkerId
                };
                backgroundWorkerPairs.Add(new BackgroundWorkerPair()
                {
                    BackgroundWorker = backgroundWorker,
                    BackgroundWrokerArguments = backgroundWrokerArguments
                });
                Log("Created BackgroundWorker", logType: LogType.Okay, id: bgWorkerId++);
            }

            courtBookingRecord.Clear();
            for (int i = 0; i < backgroundWorkerPairs.Count; i++)
            {
                backgroundWorkerPairs[i].BackgroundWorker.RunWorkerAsync(backgroundWorkerPairs[i].BackgroundWrokerArguments);
            }
        }

        #region Group of Notification Settings

        private void botTokenTextBox_MouseDoubleClick(object sender, MouseEventArgs e) =>
            botTokenTextBox.UseSystemPasswordChar = !botTokenTextBox.UseSystemPasswordChar;

        private void channelIdTextBox_MouseDoubleClick(object sender, MouseEventArgs e) =>
            channelIdTextBox.UseSystemPasswordChar = !channelIdTextBox.UseSystemPasswordChar;

        #endregion

        #region BackgroungWorker

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWrokerArguments args = (BackgroundWrokerArguments)e.Argument;

            BookingBot bookingBot = new BookingBot(args.BaseUrl, args.ModuleName);

            /* Login */
            while (true)
            {
                if (((BackgroundWorker)sender).CancellationPending)
                {
                    e.Cancel = true;
                    Log($"Cancelled BackgroundWorker", logType: LogType.Info, id: args.Id);
                    return;
                }

                LoginStatus loginStatus = bookingBot.Login(args.Username, args.Password);
                if (loginStatus == LoginStatus.Success)
                {
                    Log("Login successful", logType: LogType.Okay, id: args.Id);
                    break;
                }
                else
                {
                    Log($"Login failed ({loginStatus}), retrying after 3 seconds", logType: LogType.Failed, id: args.Id);
                    backgroundWorker_Sleep(sender, e, 3000, out bool cancellationPending);
                    if (cancellationPending)
                    {
                        return;
                    }
                }
            }

            if (args.BookingWithoutCheckOpenState)
            {
                /* Wait for dest date */
                while (true)
                {
                    if (((BackgroundWorker)sender).CancellationPending)
                    {
                        e.Cancel = true;
                        Log($"Cancelled BackgroundWorker", logType: LogType.Info, id: args.Id);
                        return;
                    }

                    if (DateTime.Now.Date >= args.DestDate.Date.AddDays(-currentSportCenter.DayDiff))
                    {
                        break;
                    }
                }
            }
            else
            {
                /* Checking the date status */
                while (true)
                {
                    if (((BackgroundWorker)sender).CancellationPending)
                    {
                        e.Cancel = true;
                        Log($"Cancelled BackgroundWorker", logType: LogType.Info, id: args.Id);
                        return;
                    }

                    if (bookingBot.CheckDataStatus(args.DestDate))
                    {
                        Log($"{args.DestDate:yyyy-MM-dd} booking status is opened", logType: LogType.Okay, id: args.Id);
                        break;
                    }
                    else
                    {
                        Log($"{args.DestDate:yyyy-MM-dd} booking status is not opened", logType: LogType.Warn, id: args.Id);
                    }
                }
            }

            if (((BackgroundWorker)sender).CancellationPending)
            {
                e.Cancel = true;
                Log($"Cancelled BackgroundWorker", logType: LogType.Info, id: args.Id);
                return;
            }

            /* Booking first */
            foreach (int timeCode in args.BookingTimeCodes)
            {
                foreach (int courtCode in args.BookingCourtCodes)
                {
                    string currentCourtName = currentSportCenter.Courts.First(court => court.Value == courtCode).Key;
                    BookingMessageGenerator bookingMessageGenerator = new BookingMessageGenerator()
                    {
                        SportCenterName = currentSportCenter.Name,
                        CourtName = currentCourtName,
                        CourtCode = courtCode,
                        DestDate = args.DestDate,
                        DestTime = timeCode,
                        Username = args.Username
                    };
                    if (bookingBot.BookCourt(courtCode, timeCode, args.DestDate))
                    {
                        Log(bookingMessageGenerator.GetMessage(bookingState: true), logType: LogType.Okay, id: args.Id);
                        if (args.UseTelegramToNotify)
                        {
                            new Task(() =>
                            {
                                _ = args.TelegramBot.SendMessageByTelegramBotAsync(bookingMessageGenerator.GetMessage(bookingState: true));
                            }).Start();
                        }
                    }
                    else
                    {
                        Log(bookingMessageGenerator.GetMessage(bookingState: false), logType: LogType.Failed, id: args.Id);
                    }
                }
            }

            /* 
             * Monitor
             *     If in the "Monitor By Single Thread" mode, must sure that
             *     only one BackgroungWorker can set the finished flag in the
             *     same time.
             */
            if (args.UseMonitor)
            {
                if (!args.MonitorBySingleThread)
                {
                    backgroundWorker_Monitor(sender, e, bookingBot);
                }
                else
                {
                    Log("Finished first booking", logType: LogType.Okay, id: args.Id);
                    if (!IsLastBackgroundWorker((BackgroundWorker)sender))
                    {
                        return;
                    }
                    else
                    {
                        Log("Monitor by current BackgroungWorker", id: args.Id);
                        backgroundWorker_Monitor(sender, e, bookingBot);
                    }
                }
            }

            /*
             * Dispose all BackgroundWorker after inished to auto-book courts
             */
            if (IsLastBackgroundWorker((BackgroundWorker)sender) && !cancelByStopButton)
            {
                Log($"Finished to auto-book courts", logType: LogType.Okay);
                this.Invoke((VoidDelegate)(() => { stopButton.PerformClick(); }));
            }
        }

        private void backgroundWorker_Monitor(object sender, DoWorkEventArgs e, BookingBot bookingBot)
        {
            BackgroundWrokerArguments args = (BackgroundWrokerArguments)e.Argument;

            /* Monitor */
            Log("Beginging to monitor", id: args.Id);
            (DateTime loginDateTime, DateTime monitorBegin) = (DateTime.Now, DateTime.Now);
            (List<int> monitorTimeCodes, List<int> monitorCourtCodes) = args.MonitorBySingleThread ? (args.MonitorTimeCodes, args.MonitorCourtCodes) : (args.BookingTimeCodes, args.BookingCourtCodes);
            bool serviceTimeRecord = false;
            bool cancellationPending = false;
            while (true)
            {
                if (((BackgroundWorker)sender).CancellationPending)
                {
                    e.Cancel = true;
                    Log($"Cancelled BackgroundWorker", logType: LogType.Info, id: args.Id);
                    return;
                }

                if (IsServiceTime(DateTime.Now))
                {
                    serviceTimeRecord = true;
                    continue;
                }
                else
                {
                    /* Re-login after finished service time */
                    if (serviceTimeRecord || (DateTime.Now - loginDateTime).TotalSeconds >= (2 * 60 * 60))
                    {
                        LoginStatus loginStatus = bookingBot.Login(args.Username, args.Password);
                        if (loginStatus == LoginStatus.Success)
                        {
                            Log("Login successful", logType: LogType.Okay, id: args.Id);
                            serviceTimeRecord = false;
                            loginDateTime = DateTime.Now;
                        }
                        else
                        {
                            Log($"Login failed ({loginStatus}), retrying after 3 seconds", logType: LogType.Failed, id: args.Id);
                            backgroundWorker_Sleep(sender, e, 3000,  out cancellationPending);
                            continue;
                        }
                    }

                    foreach (int timePartCode in monitorTimeCodes.ToTimePartCodes())
                    {
                        var courtStatusDict = bookingBot.CheckCourtStatus(args.DestDate, new List<int>() { timePartCode });
                        var availableCourts = courtStatusDict[timePartCode].FindAll(court => court.Available);

                        /*
                         * Time Unduplicated
                         *     Only booking the court which time code is not already be record.
                         */
                        if (args.MonitorTimeUnduplicated)
                        {
                            availableCourts = availableCourts.FindAll(availableCourt => !courtBookingRecord.Contains(availableCourt.TimeCode));
                        }

                        /*
                         * BaseOnBookingSettings
                         *     Inherit the setting of booking
                         */
                        if (args.MonitorBaseOnBookingSettings)
                        {
                            availableCourts = availableCourts.FindAll(availableCourt => monitorTimeCodes.Contains(availableCourt.TimeCode) && monitorCourtCodes.Contains(availableCourt.Id));
                        }

                        if (availableCourts.Count == 0)
                        {
                            Log($"Do not have any avaiable courts at date {args.DestDate:yyyy-MM-dd} timepart {timePartCode}", logType: LogType.Warn, id: args.Id);
                        }
                        else
                        {
                            foreach (Court court in availableCourts)
                            {
                                string currentCourtName = currentSportCenter.Courts.First(courtPair => courtPair.Value == court.Id).Key;
                                BookingMessageGenerator bookingMessageGenerator = new BookingMessageGenerator()
                                {
                                    SportCenterName = currentSportCenter.Name,
                                    CourtName = currentCourtName,
                                    CourtCode = court.Id,
                                    DestDate = court.Date,
                                    DestTime = court.TimeCode,
                                    Username = args.Username
                                };
                                if (bookingBot.BookCourt(court))
                                {
                                    Log(bookingMessageGenerator.GetMessage(bookingState: true), logType: LogType.Okay, id: args.Id);
                                    courtBookingRecord.Add(court.TimeCode);
                                    if (args.UseTelegramToNotify)
                                    {
                                        new Task(() =>
                                        {
                                            _ = args.TelegramBot.SendMessageByTelegramBotAsync(bookingMessageGenerator.GetMessage(bookingState: true));
                                        }).Start();
                                    }
                                }
                                else
                                {
                                    Log(bookingMessageGenerator.GetMessage(bookingState: false), logType: LogType.Failed, id: args.Id);
                                }
                            }
                        }
                    }
                }

                if (DateTime.Now >= args.MonitorDateTime)
                {

                    Log($"Finished to monitor courts", logType: LogType.Okay, id: args.Id);
                    break;
                }

                backgroundWorker_Sleep(sender, e, args.MonitorIntervalSeconds * 1000, out cancellationPending);
                if (cancellationPending)
                {
                    return;
                }
            }
        }

        private void backgroundWorker_Sleep(object sender, DoWorkEventArgs e, int millisecondsTimeout, out bool cancellationPending)
        {
            BackgroundWrokerArguments args = (BackgroundWrokerArguments)e.Argument;

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            long currentElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            while ((stopwatch.ElapsedMilliseconds - currentElapsedMilliseconds) < millisecondsTimeout)
            {
                if (((BackgroundWorker)sender).CancellationPending)
                {
                    e.Cancel = cancellationPending = true;
                    Log($"Cancelled BackgroundWorker", logType: LogType.Info, id: args.Id);
                    return;
                }
                System.Threading.Thread.SpinWait(10);
            }
            cancellationPending = false;
        }

        private bool IsLastBackgroundWorker(BackgroundWorker backgroundWorker)
        {
            mutex.WaitOne();
            backgroundWorkerPairs.First(backgroundWorkerPair => backgroundWorkerPair.BackgroundWorker == backgroundWorker).IsFinished = true;
            bool isLastBackgroundWorker = backgroundWorkerPairs.All(backgroundWorkerPair => backgroundWorkerPair.IsFinished);
            mutex.ReleaseMutex();

            return isLastBackgroundWorker;
        }

        #endregion

        private bool IsServiceTime(DateTime dateTime) => 2 <= dateTime.Hour && dateTime.Hour <= 4;

        private void CancelBackgroundWorkers()
        {
            if (backgroundWorkerPairs != null && backgroundWorkerPairs.Count > 0)
            {
                /* Cancel all BackgroundWorker */
                for (int i = 0; i < backgroundWorkerPairs.Count; i++)
                {
                    if (backgroundWorkerPairs[i].BackgroundWorker.IsBusy)
                    {
                        Log($"Cancelling BackgroundWorker", logType: LogType.Info, id: backgroundWorkerPairs[i].BackgroundWrokerArguments.Id);
                        backgroundWorkerPairs[i].BackgroundWorker.CancelAsync();
                    }
                }

                /* When all BackgroundWorkers is not busy, dispose all BackgroundWorkers */
                while (true)
                {
                    if (!backgroundWorkerPairs.Any(backgroundWorkerPair => backgroundWorkerPair.BackgroundWorker.IsBusy))
                    {
                        Log($"All BackgroundWorker is already be cancelled", logType: LogType.Info);
                        for (int i = 0; i < backgroundWorkerPairs.Count; i++)
                        {
                            Log($"Disposing BackgroundWorker", logType: LogType.Info, id: backgroundWorkerPairs[i].BackgroundWrokerArguments.Id);
                            backgroundWorkerPairs[i].BackgroundWorker.Dispose();
                            backgroundWorkerPairs[i].BackgroundWrokerArguments = null;
                        }
                        break;
                    }
                }

                Log($"All BackgroundWorker is already be disposed", logType: LogType.Info);
            }

            backgroundWorkerPairs = null;
            courtBookingRecord = null;
            if (mutex != null)
            {
                mutex.Close();
            }
            cancelByStopButton = false;
            SetFormControl(true);
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
                catch (ObjectDisposedException) { }
            }
            else
            {
                groupBox1.Enabled =
                    groupBox2.Enabled =
                    groupBox3.Enabled =
                    groupBox4.Enabled =
                    startButton.Enabled =
                    status;
                stopButton.Enabled = !status;
            }
        }

        private void Log(string message, LogType logType = LogType.Info, int id = 0, string oriDateTime = "")
        {
            if (logListView.InvokeRequired)
            {
                try
                {
                    logListView.Invoke(new Action(() =>
                    {
                        Log(
                            message: message,
                            logType: logType,
                            id: id,
                            oriDateTime: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }));
                }
                catch (InvalidAsynchronousStateException) { }
            }
            else
            {
                logListView.Items.Add(new ListViewItem(
                    new string[]
                    {
                        id.ToString(),
                        string.IsNullOrEmpty(oriDateTime) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : oriDateTime,
                        logType.ToString().ToUpper(),
                        message
                    }));
                logListView.Items[^1].EnsureVisible();
            }
        }

        private HttpStatusCode CheckWebsiteStatus(string url) => new RestClient(url).Get(new RestRequest()).StatusCode;

        private void ChangeToolStripStatusLabelStatus(ToolStripStatusLabel toolStripStatusLabel, HttpStatusCode httpStatusCode)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    ChangeToolStripStatusLabelStatus(toolStripStatusLabel: toolStripStatusLabel, httpStatusCode: httpStatusCode);
                }));
            }
            else
            {
                toolStripStatusLabel.Image = new Bitmap((httpStatusCode == HttpStatusCode.OK) ? Properties.Resources.Circle_Green : Properties.Resources.Circle_Red, toolStripStatusLabelImageSize);
                toolStripStatusLabel.ToolTipText += $" ({(int)httpStatusCode} {httpStatusCode}, {DateTime.Now:yyyy-MM-dd HH:mm:ss})";
                Log($"{(int)httpStatusCode} {httpStatusCode} from connection of {toolStripStatusLabel.ToolTipText[..toolStripStatusLabel.ToolTipText.IndexOf(" Status")]}", logType: httpStatusCode == HttpStatusCode.OK ? LogType.Okay : LogType.Failed);
            }
        }

        private async Task WriteLogsAsync()
        {
            string logMessage = "";
            char delimitChar = '\t';
            for (int i = 0; i < logListView.Items.Count; i++)
            {
                for (int j = 0; j < logListView.Items[i].SubItems.Count; j++)
                {
                    logMessage += $"{logListView.Items[i].SubItems[j].Text}{delimitChar}";
                }
                logMessage = logMessage.TrimEnd(delimitChar) + "\n";
            }

            string logPath = $"{LogDirPath}/{DateTime.Now:yyyyMMdd_HHmmss_fff}_{logListView.Items.Count}ea.log";
            if (!System.IO.Directory.Exists(LogDirPath))
            {
                System.IO.Directory.CreateDirectory(LogDirPath);
            }
            if (!string.IsNullOrEmpty(logMessage))
            {
                await System.IO.File.WriteAllTextAsync(logPath, logMessage);
            }
        }

        #region Save/Load Configs

        private void SaveConfigs()
        {
            IniManager iniManager = new IniManager(IniPath);
            /* IniInfo */
            iniManager.WriteIniFile("IniInfo", "Version", IniVersion);
            /* Settings */
            iniManager.WriteIniFile("Settings", "CheckWebsiteStatusOnFirstExecution", checkWebsiteStatusOnFirstExecutionToolStripMenuItem.Checked);
            /* Account */
            iniManager.WriteIniFile("Account", "Username", usernameTextBox.Text.Encrypt(cryptoKey) ?? "");
            iniManager.WriteIniFile("Account", "Password", passwordTextBox.Text.Encrypt(cryptoKey) ?? "");
            iniManager.WriteIniFile("Account", "UsernamePassChar", usernameTextBox.UseSystemPasswordChar);
            iniManager.WriteIniFile("Account", "PasswordPassChar", passwordTextBox.UseSystemPasswordChar);
            /* Booking */
            iniManager.WriteIniFile("Booking", "SportCenterIndex", sportCenterComboBox.SelectedIndex);
            iniManager.WriteIniFile("Booking", "MultiThread", bookingByMultiThreadCheckBox.Checked);
            iniManager.WriteIniFile("Booking", "Times", string.Join(',', timeCheckedListBox.GetCheckedItemIndexs()));
            iniManager.WriteIniFile("Booking", "Courts", string.Join(',', courtCheckedListBox.GetCheckedItemIndexs()));
            iniManager.WriteIniFile("Booking", "WithoutCheckOpenState", withoutCheckOpenStateCheckBox.Checked);
            /* Monitor */
            iniManager.WriteIniFile("Monitor", "UseMonitor", useMonitorCheckBox.Checked);
            iniManager.WriteIniFile("Monitor", "TimeUnduplicated", timeUnduplicatedCheckBox.Checked);
            iniManager.WriteIniFile("Monitor", "BasedOnBookingSettings", monitorBaseOnBookingSettingsCheckBox.Checked);
            iniManager.WriteIniFile("Monitor", "BySingleThread", monitorBySingleThreadCheckBox.Checked);
            /* Telegram Bot */
            iniManager.WriteIniFile("TelegramBot", "UseTelegramNotify", useTelegramToNotifyCheckBox.Checked);
            iniManager.WriteIniFile("TelegramBot", "BotToken", botTokenTextBox.Text.Encrypt(cryptoKey) ?? "");
            iniManager.WriteIniFile("TelegramBot", "ChannelId", channelIdTextBox.Text.Encrypt(cryptoKey) ?? "");
        }

        private void LoadConfigs()
        {
            IniManager iniManager = new IniManager(IniPath);
            /* Settings */
            checkWebsiteStatusOnFirstExecutionToolStripMenuItem.Checked = bool.Parse(iniManager.ReadIniFile("Settings", "CheckWebsiteStatusOnFirstExecution", "true"));
            /* Account */
            usernameTextBox.Text = iniManager.ReadIniFile("Account", "Username", "").Decrypt(cryptoKey) ?? "";
            passwordTextBox.Text = iniManager.ReadIniFile("Account", "Password", "").Decrypt(cryptoKey) ?? "";
            usernameTextBox.UseSystemPasswordChar = bool.Parse(iniManager.ReadIniFile("Account", "UsernamePassChar", "true"));
            passwordTextBox.UseSystemPasswordChar = bool.Parse(iniManager.ReadIniFile("Account", "PasswordPassChar", "true"));
            /* Booking */
            sportCenterComboBox.SelectedIndex = int.Parse(iniManager.ReadIniFile("Booking", "SportCenterIndex", "0"));
            bookingByMultiThreadCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Booking", "MultiThread", "true"));
            timeCheckedListBox.SetItemsChecked(iniManager.ReadIniFile("Booking", "Times", ""));
            courtCheckedListBox.SetItemsChecked(iniManager.ReadIniFile("Booking", "Courts", ""));
            withoutCheckOpenStateCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Booking", "WithoutCheckOpenState", "true"));
            /* Monitor */
            useMonitorCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "UseMonitor", "false"));
            timeUnduplicatedCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "TimeUnduplicated", "true"));
            monitorBaseOnBookingSettingsCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "BasedOnBookingSettings", "true"));
            monitorBySingleThreadCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "BySingleThread", "true"));
            /* Telegram Bot */
            useTelegramToNotifyCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("TelegramBot", "UseTelegramNotify", "false"));
            botTokenTextBox.Text = iniManager.ReadIniFile("TelegramBot", "BotToken", "").Decrypt(cryptoKey) ?? "";
            channelIdTextBox.Text = iniManager.ReadIniFile("TelegramBot", "ChannelId", "").Decrypt(cryptoKey) ?? "";

            /* Initialize related components */
            dateDateTimePicker.Value = DateTime.Now.AddDays(currentSportCenter.DayDiff + 1);
        }

        #endregion

        private enum LogType
        {
            Info,
            Okay,
            Warn,
            Failed
        }
    }
}

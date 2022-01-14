using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using RestSharp;
using Telegram.Bot;
using static BadmintonCourtAutoBooker.BookingBot;

namespace BadmintonCourtAutoBooker
{
    public partial class MainForm : Form
    {
        /* Timer for waiting to book and monitor */
        private readonly Timer bookingTimer = new Timer() { Interval = 500, Enabled = false };
        /* BackgroundWorker for booking */
        private readonly List<BackgroundWorkerPair> backgroundWorkerPairs = new List<BackgroundWorkerPair>();
        private readonly System.Threading.Mutex mutex = new System.Threading.Mutex();

        private readonly List<SportCenter> sportCenters = new List<SportCenter>();
        private readonly List<int> courtBookingRecord = new List<int>();
        private readonly List<ToolStripStatusLabel> toolStripStatusLabels = new List<ToolStripStatusLabel>();

        /* Telegram.Bot configs */
        private TelegramBot telegramBot;

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
        private Size circleImageSize;

        public MainForm()
        {
            sportCenters.Add(new SportCenter()
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
            });
            sportCenters.Add(new SportCenter()
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
            });

            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            /* Initial variables */
            circleImageSize = new Size(versionToolStripStatusLabel.Size.Height, versionToolStripStatusLabel.Size.Height);

            /* SplitContainer */
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Cursor = Cursors.Default;

            /* GroupBox of account settings */
            usernameTextBox.UseSystemPasswordChar = true;
            passwordTextBox.UseSystemPasswordChar = true;

            /* GroupBox of booking settings */
            sportCenterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sportCenterComboBox.Items.Clear();
            foreach (SportCenter sportCenter in sportCenters)
            {
                sportCenterComboBox.Items.Add(sportCenter.Name);
            }

            dateDateTimePicker.Format = DateTimePickerFormat.Custom;
            dateDateTimePicker.CustomFormat = "yyyy-MM-dd (dddd)";
            dateDateTimePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);

            timeCheckedListBox.Items.Clear();
            for (int i = 6; i <= 21; i++)
            {
                timeCheckedListBox.Items.Add(string.Format("{0:00}:00-{1:00}:00", i, i + 1));
            }
            timeSelectButton.Image = new Bitmap(Properties.Resources.checkbox_checked, checkButtonImageSize);

            courtCheckedListBox.Items.Clear();
            courtSelectButton.Image = new Bitmap(Properties.Resources.checkbox_checked, checkButtonImageSize);

            /* GroupBox of monitor settings */
            untilDateTimePicker.Format = DateTimePickerFormat.Custom;
            untilDateTimePicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            untilDateTimePicker.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, 0, 30, 0);

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
                Image = new Bitmap(Properties.Resources.circle_gray, circleImageSize),
                ImageScaling = ToolStripItemImageScaling.None,
                Text = "https://ocr.holey.cc",
                ToolTipText = "OCR Website Status"
            });
            for (int i = 0; i < sportCenters.Count; i++)
            {
                toolStripStatusLabels.Add(new ToolStripStatusLabel()
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    Image = new Bitmap(Properties.Resources.circle_gray, circleImageSize),
                    ImageScaling = ToolStripItemImageScaling.None,
                    Text = $"{sportCenters[i].BaseUrl}/{sportCenters[i].ModuleName}",
                    ToolTipText = $"{sportCenters[i].Name} Status"
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
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            for (int i = 0; i < toolStripStatusLabels.Count; i++)
            {
                toolStripStatusLabels[i].PerformClick();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _ = WriteLogsAsync();
            SaveConfigs();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e) => new System.Threading.Thread(() => CancelBackgroundWorkers()).Start();

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

        #endregion

        #region GroupBox of Booking Settings

        private void sportCenterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            SportCenter recordSportCenter = currentSportCenter;
            courtCheckedListBox.Items.Clear();
            foreach (var court in sportCenters.First(sportcCenter => sportcCenter.Name == sportCenterComboBox.SelectedItem.ToString()).Courts)
            {
                courtCheckedListBox.Items.Add(court.Key);
                currentSportCenter = sportCenters.First(sportcCenter => sportcCenter.Name == sportCenterComboBox.SelectedItem.ToString());
            }
            if (recordSportCenter != null &&
                dateDateTimePicker.Value.Date == DateTime.Now.AddDays(recordSportCenter.DayDiff + 1).Date)
            {
                dateDateTimePicker.Value = DateTime.Now.AddDays(currentSportCenter.DayDiff + 1);
            }
        }

        private void timeSelectButton_Click(object sender, EventArgs e)
        {
            bool checkStatus = timeCheckedListBox.CheckedItems.Count == 0;
            for (int i = 0; i < timeCheckedListBox.Items.Count; i++)
            {
                timeCheckedListBox.SetItemChecked(i, checkStatus);
            }
            SetTimeSelectButtonStatus(!checkStatus);
        }

        private void courtSelectButton_Click(object sender, EventArgs e)
        {
            bool checkStatus = courtCheckedListBox.CheckedItems.Count == 0;
            for (int i = 0; i < courtCheckedListBox.Items.Count; i++)
            {
                courtCheckedListBox.SetItemChecked(i, checkStatus);
            }
            SetCourtSelectButtonStatus(!checkStatus);
        }

        private void timeCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e) =>
            SetTimeSelectButtonStatus(timeCheckedListBox.CheckedItems.Count == 1 && e.NewValue == CheckState.Unchecked);

        private void courtCheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e) =>
            SetCourtSelectButtonStatus(courtCheckedListBox.CheckedItems.Count == 1 && e.NewValue == CheckState.Unchecked);

        #endregion

        #region GroupBox of Monitor Settings

        private void monitorCheckBox_CheckedChanged(object sender, EventArgs e) =>
            untilDateTimePicker.Enabled =
                intervalDateTimePicker.Enabled =
                timeUnduplicatedCheckBox.Enabled =
                monitorBaseOnBookingSettingsCheckBox.Enabled =
                monitorBySingleThreadCheckBox.Enabled =
                monitorCheckBox.Checked;

        #endregion

        #region GroupBox of Notification Settings

        private void telegramNotifyCheckBox_CheckedChanged(object sender, EventArgs e) =>
            botTokenTextBox.Enabled =
                channelIdTextBox.Enabled =
                sendTestMessageButton.Enabled =
                telegramNotifyCheckBox.Checked;

        private void sendTestMessageButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(botTokenTextBox.Text) || string.IsNullOrWhiteSpace(channelIdTextBox.Text))
            {
                MessageBox.Show("Bot Token and Channel ID cannot be empty.", "Send Test Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                telegramBot = new TelegramBot(
                    botToken: botTokenTextBox.Text,
                    channelId: channelIdTextBox.Text);
                new Task(() =>
                {
                    _ = telegramBot.SendMessageByTelegramBotAsync($"[BadmintonCourtAutoBooker] This test message is sent at {DateTime.Now:yyyy-MM-dd HH:mm:ss}.");
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
            bookingTimer.Enabled = false;
            new System.Threading.Thread(() => CancelBackgroundWorkers()).Start();
        }

        #endregion

        private void ToolStripStatusLabel_Click(object sender, EventArgs e)
        {
            ToolStripStatusLabel toolStripStatusLabel = (ToolStripStatusLabel)sender;
            toolStripStatusLabel.ToolTipText = toolStripStatusLabel.ToolTipText.Split('(')[0].Trim();
            toolStripStatusLabel.Image = new Bitmap(Properties.Resources.circle_gray, circleImageSize);
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
                            MonitorTimeCodes = timeCodes,
                            MonitorCourtCodes = courtCodes,
                            MonitorDateTime = untilDateTimePicker.Value,
                            MonitorIntervalSeconds = (((intervalDateTimePicker.Value.Hour * 60) + intervalDateTimePicker.Value.Minute) * 60) + intervalDateTimePicker.Value.Second,
                            MonitorTimeUnduplicated = timeUnduplicatedCheckBox.Checked,
                            MonitorBaseOnBookingSettings = monitorBaseOnBookingSettingsCheckBox.Checked,
                            MonitorBySingleThread = monitorBySingleThreadCheckBox.Checked,
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
                    MonitorTimeCodes = timeCodes,
                    MonitorCourtCodes = courtCodes,
                    MonitorDateTime = untilDateTimePicker.Value,
                    MonitorIntervalSeconds = (((intervalDateTimePicker.Value.Hour * 60) + intervalDateTimePicker.Value.Minute) * 60) + intervalDateTimePicker.Value.Second,
                    MonitorTimeUnduplicated = timeUnduplicatedCheckBox.Checked,
                    MonitorBaseOnBookingSettings = monitorBaseOnBookingSettingsCheckBox.Checked,
                    MonitorBySingleThread = monitorBySingleThreadCheckBox.Checked,
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
                    backgroundWorker_Sleep(sender, e, 3000);
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
                    if (bookingBot.BookCourt(courtCode, timeCode, args.DestDate))
                    {
                        Log($"Success to book court {currentCourtName}({courtCode}) (date_code={args.DestDate:yyyy-MM-dd}, time_code={timeCode})", logType: LogType.Okay, id: args.Id);
                        if (telegramNotifyCheckBox.Checked)
                        {
                            new Task(() =>
                            {
                                _ = telegramBot.SendMessageByTelegramBotAsync($"Success to book court {currentCourtName}({courtCode}) (date_code={args.DestDate:yyyy-MM-dd}, time_code={timeCode})");
                            }).Start();
                        }
                    }
                    else
                    {
                        Log($"Failed to book court {currentCourtName}({courtCode}) (date_code={args.DestDate:yyyy-MM-dd}, time_code={timeCode})", logType: LogType.Failed, id: args.Id);
                    }
                }
            }

            /* 
             * Monitor
             *     If in the "Monitor By Single Thread" mode, must sure that
             *     only one BackgroungWorker can set the finished flag in the
             *     same time.
             */
            if ((args.MonitorBySingleThread && mutex.WaitOne()) ||
                !args.MonitorBySingleThread)
            {
                backgroundWorker_Monitor(sender, e, bookingBot);
            }
        }

        private void backgroundWorker_Monitor(object sender, DoWorkEventArgs e, BookingBot bookingBot)
        {
            BackgroundWrokerArguments args = (BackgroundWrokerArguments)e.Argument;
            backgroundWorkerPairs.First(backgroundWorkerPair => backgroundWorkerPair.BackgroundWorker == (BackgroundWorker)sender).IsFinished = true;

            if (args.MonitorBySingleThread)
            {
                bool isReturned = false;
                if (!backgroundWorkerPairs.All(backgroundWorkerPair => backgroundWorkerPair.IsFinished))
                {
                    Log("Finished first booking", logType: LogType.Okay, id: args.Id);
                    isReturned = true;
                }
                else
                {
                    Log("Monitor by current BackgroungWorker", id: args.Id);
                }

                if (isReturned)
                {
                    mutex.ReleaseMutex();
                    return;
                }
            }

            /* Monitor */
            Log("Beginging to monitor", id: args.Id);
            (DateTime loginDateTime, DateTime monitorBegin) = (DateTime.Now, DateTime.Now);
            (List<int> monitorTimeCodes, List<int> monitorCourtCodes) = args.MonitorBySingleThread ? (args.MonitorTimeCodes, args.MonitorCourtCodes) : (args.BookingTimeCodes, args.BookingCourtCodes);
            bool serviceTimeRecord = false;
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
                            backgroundWorker_Sleep(sender, e, 3000);
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
                                if (bookingBot.BookCourt(court))
                                {
                                    Log($"Success to book court {currentCourtName}({court.Id}) (date_code={court.Date:yyyy-MM-dd}, time_code={court.TimeCode})", logType: LogType.Okay, id: args.Id);
                                    courtBookingRecord.Add(court.TimeCode);
                                    if (telegramNotifyCheckBox.Checked)
                                    {
                                        new Task(() =>
                                        {
                                            _ = telegramBot.SendMessageByTelegramBotAsync($"Success to book court {currentCourtName}({court.Id}) (date_code={court.Date:yyyy-MM-dd}, time_code={court.TimeCode})");
                                        }).Start();
                                    }
                                }
                                else
                                {
                                    Log($"Failed to book court {currentCourtName}({court.Id}) (date_code={court.Date:yyyy-MM-dd}, time_code={court.TimeCode})", logType: LogType.Failed, id: args.Id);
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

                backgroundWorker_Sleep(sender, e, args.MonitorIntervalSeconds * 1000);
            }
        }

        private void backgroundWorker_Sleep(object sender, DoWorkEventArgs e, int millisecondsTimeout)
        {
            BackgroundWrokerArguments args = (BackgroundWrokerArguments)e.Argument;

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            long currentElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            while ((stopwatch.ElapsedMilliseconds - currentElapsedMilliseconds) < millisecondsTimeout)
            {
                if (((BackgroundWorker)sender).CancellationPending)
                {
                    e.Cancel = true;
                    Log($"Cancelled BackgroundWorker", logType: LogType.Info, id: args.Id);
                    return;
                }
                System.Threading.Thread.SpinWait(10);
            }
        }

        #endregion

        private bool IsServiceTime(DateTime dateTime) => 2 <= dateTime.Hour && dateTime.Hour <= 4;

        private void CancelBackgroundWorkers()
        {
            if (backgroundWorkerPairs.Count > 0)
            {
                /* Cancel all BackgroundWorker */
                for (int i = 0; i < backgroundWorkerPairs.Count; i++)
                {
                    if (backgroundWorkerPairs[i].BackgroundWorker.IsBusy)
                    {
                        Log($"Cancelling BackgroundWorker", logType: LogType.Info, id: i);
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
                            Log($"Disposing BackgroundWorker", logType: LogType.Info, id: i);
                            backgroundWorkerPairs[i].BackgroundWorker.Dispose();
                            backgroundWorkerPairs[i].BackgroundWrokerArguments = null;
                        }
                        break;
                    }
                }

                Log($"All BackgroundWorker is already be disposed", logType: LogType.Info);
                backgroundWorkerPairs.Clear();
            }

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
            }
            else
            {
                groupBox1.Enabled =
                    groupBox2.Enabled =
                    groupBox3.Enabled =
                    startButton.Enabled =
                    status;
                stopButton.Enabled = !status;
            }
        }

        private void SetTimeSelectButtonStatus(bool status) =>
            timeSelectButton.Image = new Bitmap(status ? Properties.Resources.checkbox_checked : Properties.Resources.checkbox_unchecked, checkButtonImageSize);

        private void SetCourtSelectButtonStatus(bool status) =>
            courtSelectButton.Image = new Bitmap(status ? Properties.Resources.checkbox_checked : Properties.Resources.checkbox_unchecked, checkButtonImageSize);

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
                toolStripStatusLabel.Image = new Bitmap((httpStatusCode == HttpStatusCode.OK) ? Properties.Resources.circle_green : Properties.Resources.circle_red, circleImageSize);
                toolStripStatusLabel.ToolTipText += $" ({(int)httpStatusCode} {httpStatusCode}, {DateTime.Now:yyyy-MM-dd HH:mm:ss})";
                Log($"{(int)httpStatusCode} {httpStatusCode} from connection of {toolStripStatusLabel.ToolTipText[..toolStripStatusLabel.ToolTipText.IndexOf(" Status")]}", logType: httpStatusCode == HttpStatusCode.OK ? LogType.Okay : LogType.Failed);
            }
        }

        private async Task WriteLogsAsync()
        {
            string logMessage = "";
            for (int i = 0; i < logListView.Items.Count; i++)
            {
                for (int j = 0; j < logListView.Items[i].SubItems.Count; j++)
                {
                    logMessage += logListView.Items[i].SubItems[j].Text + "\t";
                }
                logMessage = logMessage.TrimEnd('\t') + "\n";
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
            /* Account */
            iniManager.WriteIniFile("Account", "Username", usernameTextBox.Text.Encrypt(cryptoKey) ?? "");
            iniManager.WriteIniFile("Account", "Password", passwordTextBox.Text.Encrypt(cryptoKey) ?? "");
            /* Booking */
            iniManager.WriteIniFile("Booking", "SportCenterIndex", sportCenterComboBox.SelectedIndex);
            iniManager.WriteIniFile("Booking", "MultiThread", bookingByMultiThreadCheckBox.Checked);
            iniManager.WriteIniFile("Booking", "Times", string.Join(',', timeCheckedListBox.GetCheckedItemIndexs()));
            iniManager.WriteIniFile("Booking", "Courts", string.Join(',', courtCheckedListBox.GetCheckedItemIndexs()));
            iniManager.WriteIniFile("Booking", "WithoutCheckOpenState", withoutCheckOpenStateCheckBox.Checked);
            /* Monitor */
            iniManager.WriteIniFile("Monitor", "UseMonitor", monitorCheckBox.Checked);
            iniManager.WriteIniFile("Monitor", "TimeUnduplicated", timeUnduplicatedCheckBox.Checked);
            iniManager.WriteIniFile("Monitor", "BasedOnBookingSettings", monitorBaseOnBookingSettingsCheckBox.Checked);
            iniManager.WriteIniFile("Monitor", "BySingleThread", monitorBySingleThreadCheckBox.Checked);
            /* Telegram Bot */
            iniManager.WriteIniFile("TelegramBot", "TelegramNotify", telegramNotifyCheckBox.Checked);
            iniManager.WriteIniFile("TelegramBot", "BotToken", botTokenTextBox.Text.Encrypt(cryptoKey) ?? "");
            iniManager.WriteIniFile("TelegramBot", "ChannelId", channelIdTextBox.Text.Encrypt(cryptoKey) ?? "");
        }

        private void LoadConfigs()
        {
            IniManager iniManager = new IniManager(IniPath);
            /* Account */
            usernameTextBox.Text = iniManager.ReadIniFile("Account", "Username", "").Decrypt(cryptoKey) ?? "";
            passwordTextBox.Text = iniManager.ReadIniFile("Account", "Password", "").Decrypt(cryptoKey) ?? "";
            /* Booking */
            sportCenterComboBox.SelectedIndex = int.Parse(iniManager.ReadIniFile("Booking", "SportCenterIndex", "0"));
            bookingByMultiThreadCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Booking", "MultiThread", "true"));
            timeCheckedListBox.SetItemsChecked(iniManager.ReadIniFile("Booking", "Times", ""));
            courtCheckedListBox.SetItemsChecked(iniManager.ReadIniFile("Booking", "Courts", ""));
            withoutCheckOpenStateCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Booking", "WithoutCheckOpenState", "true"));
            /* Monitor */
            monitorCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "UseMonitor", "false"));
            timeUnduplicatedCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "TimeUnduplicated", "true"));
            monitorBaseOnBookingSettingsCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "BasedOnBookingSettings", "true"));
            monitorBySingleThreadCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("Monitor", "BySingleThread", "true"));
            /* Telegram Bot */
            telegramNotifyCheckBox.Checked = bool.Parse(iniManager.ReadIniFile("TelegramBot", "TelegramNotify", "false"));
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

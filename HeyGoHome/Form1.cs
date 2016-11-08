#region

using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HeyGoHome.Properties;
using Microsoft.Win32;

#endregion

namespace HeyGoHome
{
    public partial class Form1 : Form
    {

        #region Fields

        private static string filename =  "time.txt";
        private readonly Timer mainTimer;
        private Timer flushingTimer;
        private bool _fromMenuClosing;
        private bool _firstTime = true;
        private int countOfSecTorepeat;
        private List<string> lines;
        private DateTime startTime;
        private string outputTimeFormat = @"hh\:mm\:ss";
        // скока работать
        public TimeSpan totalWorkingDay;

        #endregion

        #region managed functions
        
        private void SetWorkingDayFromSettings()
        {
            totalWorkingDay = DateTime.Now.DayOfWeek == DayOfWeek.Friday
                ? Settings.Default.AddQuarter ? new TimeSpan(0, 7, 15, 0) : new TimeSpan(0, 7, 0, 0)
                : Settings.Default.AddQuarter ? new TimeSpan(0, 8, 15, 0) : new TimeSpan(0, 8, 0, 0);
        }

        private void UpdateTimeFormatFromSettings()
        {
            outputTimeFormat = !Settings.Default.ShowMs ? @"hh\:mm\:ss" : String.Empty;
        }

        private void CheckPrevTime()
        {
            if (!File.Exists(filename))
            {
                startTime = DateTime.Now;
                WriteFile();
            }
            else
            {
                lines = File.ReadAllLines(filename, Encoding.UTF8).ToList();
                var lastTime = lines.LastOrDefault();

                if (string.IsNullOrEmpty(lastTime))
                    startTime = DateTime.Now;
                else
                {
                    if (!DateTime.TryParse(lastTime.Split(';').FirstOrDefault(), out startTime))
                        startTime = DateTime.Now;

                    if (startTime.Day != DateTime.Now.Day)
                    {
                        startTime = DateTime.Now;
                        WriteFile(true);
                    }
                    else
                    {
                        WriteFile(false);
                    }
                }
            }

            flushingTimer = new Timer { Interval = Settings.Default.FlushIntervalInMs };
            flushingTimer.Tick += (sender, args) =>
            {
                WriteFile(false);
            };
            flushingTimer.Start();
        }

        private void WriteFile(bool append = false)
        {
            GC.Collect();
            string newString = $@"{startTime};{DateTime.Now}";
            if (lines?.Any() == true)
            {
                if (append)
                    lines.Add(newString);
                else
                {
                    lines[lines.Count - 1] = newString;
                }
            }
            else
            {
                lines = new List<string> { newString };
            }

            File.WriteAllLines(filename, lines, Encoding.UTF8);
        }

        private void CloseApp()
        {
            _fromMenuClosing = true;
            Close();
        }
        
        private void SetStaticLabels()
        {
            arrivalTime.Text = startTime.ToShortTimeString();
            finishTime.Text = (startTime + totalWorkingDay).ToShortTimeString();
        }


        private void MainTimerOnTick(object sender, EventArgs eventArgs)
        {
            GC.Collect(2, GCCollectionMode.Forced);
            var timeToLeave = (startTime + totalWorkingDay - DateTime.Now).ToString(outputTimeFormat);

            timeLeft.Text = timeToLeave;
            notifyIcon1.Text = timeToLeave;

            var diff = (DateTime.Now - startTime).TotalMinutes;
            if (diff > totalWorkingDay.TotalMinutes)
            {
                if (countOfSecTorepeat < 60 && !_firstTime)
                {
                    countOfSecTorepeat++;
                    if (notifyIcon1.Icon != Resources.run)
                        notifyIcon1.Icon = Resources.run;
                }
                else
                {
                    
                    if (_firstTime)
                    {
                        _firstTime = false;
                        countOfSecTorepeat = 0;
                        MessageBox.Show(@"Пора домой!", @"Уходи", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                }
            }
            // если меньше часа
            else if (diff > (totalWorkingDay - TimeSpan.FromHours(1.0)).TotalMinutes)
            {
                if (notifyIcon1.Icon != Resources.soon)
                    notifyIcon1.Icon = Resources.soon;
            }
            else
            {
                if (notifyIcon1.Icon != Resources.logo)
                    notifyIcon1.Icon = Resources.logo;
            }
        }


        #endregion


        public Form1()
        {
            InitializeComponent();

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeyGoHome");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            filename = Path.Combine(dir, filename);

            CheckPrevTime();
            SetWorkingDayFromSettings();
            UpdateTimeFormatFromSettings();
            SetStaticLabels();

            mainTimer = new Timer {Interval = Settings.Default.Interval};
            mainTimer.Tick += MainTimerOnTick;
            mainTimer.Start();

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                var rkApp = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                var startPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) +
                                @"\Grigoriev\Igor\HeyGoHome.appref-ms";
                rkApp?.SetValue("YourProduct", startPath);

                VersionMenuItem.Text = $"Ver. {ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString(4)}";
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseApp();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !_fromMenuClosing;

            if (_fromMenuClosing)
            {
                notifyIcon1.Visible = false;
            }
            Hide();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (Visible)
                Hide();
            else
                Show();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseApp();
        }
        
        private void timeSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TimeSettings dlg = new TimeSettings();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                mainTimer.Interval = Settings.Default.Interval;
                flushingTimer.Interval = Settings.Default.FlushIntervalInMs;

                SetWorkingDayFromSettings();
                SetStaticLabels();
                UpdateTimeFormatFromSettings();
            }
        }

        private void startNewDayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (
                MessageBox.Show(@"Start new day and set " + DateTime.Now.ToShortTimeString() + @" as beginning?",
                    @"Restart", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                startTime = DateTime.Now;
                WriteFile(true);
                SetStaticLabels();
            }
        }

        private void whatsNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"Added Start new day function", "What's new?", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
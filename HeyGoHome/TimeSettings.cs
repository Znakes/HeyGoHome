using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HeyGoHome.Properties;

namespace HeyGoHome
{
    public partial class TimeSettings : Form
    {
        public TimeSettings()
        {
            InitializeComponent();
            FillForm();
        }

        private void FillForm()
        {
            addQuarter.Checked = Settings.Default.AddQuarter;
            showMs.Checked = Settings.Default.ShowMs;

            if (Settings.Default.Interval > tickInterval.Maximum)
            {
                Settings.Default.Interval = (int) tickInterval.Maximum;
                Settings.Default.Save();
            }

            if (Settings.Default.Interval < tickInterval.Minimum)
            {
                Settings.Default.Interval = (int) tickInterval.Minimum;
                Settings.Default.Save();
            }

            tickInterval.Value = Settings.Default.Interval;

            if (Settings.Default.FlushIntervalInMs > fileUpdate.Maximum)
            {
                Settings.Default.FlushIntervalInMs = (int)fileUpdate.Maximum;
                Settings.Default.Save();
            }

            if (Settings.Default.FlushIntervalInMs < fileUpdate.Minimum)
            {
                Settings.Default.FlushIntervalInMs = (int)fileUpdate.Minimum;
                Settings.Default.Save();
            }

            fileUpdate.Value = Settings.Default.FlushIntervalInMs;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;

            Settings.Default.FlushIntervalInMs = (int)fileUpdate.Value;
            Settings.Default.Interval = (int)tickInterval.Value;
            Settings.Default.AddQuarter = addQuarter.Checked;
            Settings.Default.ShowMs = showMs.Checked;

            Settings.Default.Save();

            Close();
        }
    }
}

using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading;
using System.Net.Mail;
using System.Configuration;
using System.Text.RegularExpressions;

namespace ServiceAvailabilityTest
{
    public partial class Main : Form
    {
        public int counter;
        public Main()
        {
            InitializeComponent();
            counter = 0;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if(txtTimer.Text != "" && openFileDialog.FileName != "")
            {
                if (!Regex.Match(txtTimer.Text, "^[0-9]*$").Success)
                {
                    // first name was incorrect  
                    MessageBox.Show("Invalid Time Interval", "Message", MessageBoxButtons.OK,  MessageBoxIcon.Error);
                    txtTimer.Focus();
                    return;
                }
                if (!timerInterval.Enabled)
                {
                    // First run
                    lblStatus.Text = "Starting TiP test";
                    lblStatus.Refresh();
                    Thread.Sleep(1000);
                    SendPing(sender,e);

                    // Get duration
                    int duration = Convert.ToInt32(txtTimer.Text) * 1000 * 60; //Convert timer in minutes to seconds

                    timerInterval.Interval = duration;
                    timerInterval.Start();
                    timerInterval.Tick += new EventHandler(SendPing);
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if(timerInterval.Enabled)
            {
                timerInterval.Stop();
                lblStatus.Text = "Status messages here";
                lblStatus.Refresh();
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();
        }

        public void SendPing(object sender, EventArgs e)
        {
            try
            {
                counter++;
                // Get URLs
                string textFile = openFileDialog.FileName;
                string[] lineItems = File.ReadAllLines(textFile);

                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();

                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;

                foreach (string url in lineItems)
                {
                    // Ping the address
                    lblStatus.Text = string.Format("TiP test for {0}", url);
                    lblStatus.Refresh();
                    Thread.Sleep(1000);

                    PingReply reply = pingSender.Send(url, timeout, buffer, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        lblStatus.Text += string.Format("\n\nAddress: {0}", reply.Address.ToString());
                        lblStatus.Text += string.Format("\nRoundTrip time: {0}", reply.RoundtripTime);
                        lblStatus.Text += string.Format("\nTime to live: {0}", reply.Options.Ttl);
                        lblStatus.Text += string.Format("\nPing status: Success");
                        lblStatus.Refresh();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        // Send mail to group in case of ping failure
                        string mailBody = string.Format("TiP test failure for URL: {0}", url);
                        SendEmail("v-skrishnasa@microsoft.com", null, "TiP Test failure from factory", mailBody, null);
                    }
                }
                lblStatus.Text = "Waiting for next schedule";
                lblStatus.Refresh();
                Console.WriteLine("Counter {0}", counter);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }

        private void SendEmail(string recipient, string cc, string subject, string body, Attachment attachment)
        {
            MailMessage msg = new MailMessage(ConfigurationManager.AppSettings["SendFrom"], recipient, subject, body);

            msg.IsBodyHtml = true;

            if (cc != null && cc.Length != 0)
            {
                msg.CC.Add(cc);
            }

            if (attachment != null)
            {
                msg.Attachments.Add(attachment);
            }

            SmtpClient smtpclient = new SmtpClient(ConfigurationManager.AppSettings["SmtpServer"]);
            smtpclient.UseDefaultCredentials = true;
            smtpclient.Send(msg);
        }
    }
}

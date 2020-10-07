using System;
using System.IO;
using System.Windows.Forms;

namespace AGrabber.WinForms
{
    class Utils
    {
        private static string DRIVER_LOG_FILE = "logs/driver_log.txt";
        public static void WriteLog(string text, Control textBox)
        {
            try
            {
                Form1.MainForm.Invoke((Action)(() =>
                {
                    (textBox as TextBox).AppendText(text + Environment.NewLine);
                }));
            }
            catch { }
        }

        public static void DriverWriteLog(string text)
            => WriteLog(text, Form1.LogControl);

        public static void DriverFileLog(string text)
            => File.AppendAllText(DRIVER_LOG_FILE, text + Environment.NewLine);
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AGrabber.WinForms
{
    public partial class Form1 : Form
    {
        public static TextBox LogControl;
        public static Form1 MainForm
        {
            get => mainForm;
            set { if (mainForm == null) mainForm = value; }
        }
        private static Form1 mainForm;

        public Form1()
        {
            InitializeComponent();
            LogControl = logBox;
            mainForm = this;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length <= 4 || textBox2.Text.Length <= 4)
            {
                Utils.WriteLog("Некорректно заполнено поле Почта или Пароль", logBox);
                return;
            }

            new Account(textBox1.Text, textBox2.Text);
            Utils.WriteLog($"Почта {textBox1.Text} успешно добавлена", logBox);
            listBox1.Items.Add(textBox1.Text);
            textBox1.Clear();
            textBox2.Clear();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(listBox1.Items.Count == 0)
            {
                Utils.WriteLog("Добавьте хотя бы один почтовый аккаунт", logBox);
                return;
            }

            ThreadPool.QueueUserWorkItem(Marionette.SendEmailRequests);
        }

        public void StartParseProccess()
        {
            int threads = 0;
            if (int.TryParse(textBox3.Text, out threads) == false)
                threads = 1;

            MailParser.THREADS_COUNT = threads;
            ThreadPool.QueueUserWorkItem(StarParseThread, new StartConfig(ParseType.All, accs: Account.Accounts));
        }

        private void StarParseThread(object obj)
        {
            StartConfig cfg = (StartConfig)obj;
            switch (cfg.PType)
            {
                case ParseType.All:
                    MailParser.BeginParse(cfg.Accounts);
                    break;
                case ParseType.Id:
                    MailParser.BeginParse(cfg.AccountID);
                    break;
                case ParseType.Login:
                    MailParser.BeginParse(cfg.AccountLogin);
                    break;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(textBox5.Text.Length <= 1)
            {
                Utils.WriteLog("Некорректно введен адрес сайта", logBox);
                return;
            }

            if(listBox2.Items.Contains(textBox5.Text))
            {
                Utils.WriteLog("Данный сайт уже есть в списке", logBox);
                return;
            }

            listBox2.Items.Add(textBox5.Text);
            Website.Add(textBox5.Text, true);

            textBox5.Clear();
        }

        // ТК МЕТОД БЕРЕТ ПЕРВЫЙ ЭЛЕМЕНТ ИЗ ЛИСТ БОКСА ЕСЛИ НЕ ВЫБРАНО ИНОГО - НУЖНО ПРОВЕРЯТЬ НАЛИЧИЕ ЭЛЕМЕНТОВ В ЛИСТБОКСЕ ПЕРЕД СТАРТОМ
        public string GetSelectedAccountString()
        {
            string selectedMail = string.Empty;
            this.Invoke((Action) (()=> {
                if (listBox1.SelectedIndex == -1)
                    selectedMail = listBox1.Items[0].ToString();
                else
                    selectedMail = listBox1.Items[listBox1.SelectedIndex].ToString();
            }));

            return selectedMail;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Website.LoadWebsitesFromFile();
            var websites = Website.Get();
            foreach (var w in websites) 
                listBox2.Items.Add(w.Address);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            Marionette.StopDrivers();
        }
    }

    enum ParseType
    {
        Id,
        Login,
        All
    }

    class StartConfig
    {
        public ParseType PType;
        public int AccountID;
        public string AccountLogin;
        public List<Account> Accounts;

        public StartConfig(ParseType pType, int id = -1, string str = "", List<Account> accs = null)
        {
            PType = pType;
            AccountID = id;
            AccountLogin = str;
            Accounts = accs;
        }
    }
}

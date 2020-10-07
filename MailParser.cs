using OpenPop.Mime;
using OpenPop.Pop3;
using OpenPop.Pop3.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace AGrabber.WinForms
{
    class MailParser
    {
        private static Thread[] Threads;
        public static int THREADS_COUNT = 1;
        private static int EmailCounter = 0;
        private static int MessagesParsed = 0; // Используется для подсчета количества спаршенных писем

        public static void BeginParse(List<Account> accs)
        {
            var res = GetMessages(accs);
            foreach(var kvp in res)
                SaveMessages(kvp.Key, kvp.Value);
        }
        public static void BeginParse(int accountID) => BeginParse(new List<Account>() { Account.Accounts[accountID] });
        public static void BeginParse(string accountLogin) => BeginParse(new List<Account>() { Account.GetAccount(accountLogin)});

        public static void ParseMailData(object data)
        {
            ParseData d = (ParseData) data;
            Pop3Client client = new Pop3Client();
            client.Connect("pop.mail.ru", 995, true);
            client.Authenticate(d.AccountLogin, d.AccountPass);
            Utils.WriteLog($"Клиент {d.ThreadID} подключился к {d.AccountLogin}", Form1.LogControl);

            for (int i = d.ThreadID; i < d.MessageCount; i += THREADS_COUNT)
            {
                try
                {
                    var msg = client.GetMessage(i + 1);
                    d.Messages.Add(msg);

                    Utils.WriteLog($"Parsing message # {++MessagesParsed} of {d.MessageCount} ...", Form1.LogControl); 
                }
                catch (Exception ex)
                {
                    Utils.WriteLog($"Couldn't download message №{i + 1}", Form1.LogControl);
                    Utils.WriteLog($"[Error] {ex.Message}", Form1.LogControl);
                }
            }

            client.Disconnect();
            client.Dispose();
            Utils.WriteLog($"Клиент {d.ThreadID} отключился от {d.AccountLogin}", Form1.LogControl);
            Threads[d.ThreadID].Abort();
        }

        static Dictionary<Account, List<Message>> GetMessages(List<Account> accs)
        {
            var result = new Dictionary<Account, List<Message>>();

            foreach (var acc in accs)
            {
                using (Pop3Client client = new Pop3Client())
                {
                    client.Connect("pop.mail.ru", 995, true);
                    try { client.Authenticate(acc.Login, acc.Password); } 
                    catch(Exception ex) 
                    {
                        if(ex is PopServerException || ex is InvalidLoginException || ex is InvalidUseException)
                        {
                            Utils.WriteLog($"Неудалось авторизоваться в почте {acc.Login}", Form1.LogControl);
                            continue;
                        }
                    }

                    int msgCount = client.GetMessageCount();
                    Utils.WriteLog($"Found {msgCount} messages at {acc.Login}", Form1.LogControl);
                    List<Message> messages = new List<Message>();

                    Threads = new Thread[THREADS_COUNT];
                    for(int i = 0; i < THREADS_COUNT; i++)
                    {
                        Threads[i] = new Thread(new ParameterizedThreadStart(ParseMailData));
                        Threads[i].Start(new ParseData(i, msgCount, messages, acc.Login, acc.Password));
                    }

                    while (AllThreadsFinished() == false)
                        Thread.Sleep(500);

                    result.Add(acc, messages);
                }
            }
            return result;
        }

        static bool AllThreadsFinished()
        {
            foreach(var t in Threads)
            {
                if (t.IsAlive)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Метод используется, если письмо получено в одно и тоже время, так как имя файла строится из этих параметров.
        /// Возвращает новое имя файла с порядковым номером для сохранения и дальнейшей обработки.
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <param name="tCounter">Номер попытки сохранения файла, при первичном запуске метода - параметр должен быть равен 1</param>
        /// <returns>Возвращает имя, которое еще не занято другим письмом</returns>
        static string NormalizeCloneName(string filePath, int tCounter = 1)
        {

            if (tCounter == 1) filePath += "_";

            if (File.Exists(filePath + tCounter + ".txt") == true || File.Exists(filePath + tCounter + ".html") || File.Exists(filePath + tCounter + ".xml")) 
                return NormalizeCloneName(filePath, ++tCounter);
            return filePath + tCounter;
        }

        static void SaveMessages(Account acc, List<Message> messages)
        {
            if (Directory.Exists("Mailbox") == false)
                Directory.CreateDirectory("Mailbox");


            foreach (var msg in messages)
            {
                string senderMail = CreateDirectory(msg, acc.Login);

                string filePath = $"Mailbox/{acc.Login}/{senderMail}/" +
                    msg.Headers.DateSent.Day + "_" +
                    msg.Headers.DateSent.Month + "_" +
                    msg.Headers.DateSent.Year + "-" +
                    msg.Headers.DateSent.Hour + "_" +
                    msg.Headers.DateSent.Minute + "_" +
                    msg.Headers.DateSent.Second;



                if (File.Exists(filePath + ".txt") == true || File.Exists(filePath + ".html") || File.Exists(filePath +".xml"))
                    filePath = NormalizeCloneName(filePath);

                MessagePart plainText = msg.FindFirstPlainTextVersion();
                if (plainText != null)
                {
                    plainText.Save(new FileInfo($"{filePath}.txt"));
                }

                MessagePart html = msg.FindFirstHtmlVersion();
                if (html != null)
                {
                    html.Save(new FileInfo($"{filePath}.html"));
                    continue;
                }

                MessagePart xml = msg.FindFirstMessagePartWithMediaType("text/xml");
                if (xml != null)
                {
                    string xmlString = xml.GetBodyAsText();
                    System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                    doc.LoadXml(xmlString);
                    doc.Save($"{filePath}.xml");
                    continue;
                }

                try
                {
                    File.WriteAllText($"{filePath}.txt", Encoding.ASCII.GetString(msg.RawMessage));
                }
                catch
                {
                    Utils.WriteLog($"Cannot save message with ID {msg.Headers.MessageId}", Form1.LogControl);
                }
            }

            Utils.WriteLog($"Сохранено {messages.Count} писем", Form1.LogControl);
        }

        static string CreateDirectory(Message msg, string accountLogin)
        {
            string address = string.Empty;
            if (msg.Headers.From.MailAddress == null || msg.Headers.From.Address == String.Empty || msg.Headers.From.Address.Length == 0)
            {
                string tmpAddress = msg.Headers.From.DisplayName;
                for (int i = 0; i < tmpAddress.Length; i++)
                {
                    if (tmpAddress[i] == '>' || tmpAddress[i] == '<' || tmpAddress[i] == '?' || tmpAddress[i] == ':' || tmpAddress[i] == '\\' ||
                        tmpAddress[i] == '/' || tmpAddress[i] == '\"' || tmpAddress[i] == '\'')
                        continue;
                    address += tmpAddress[i];
                }
            }
            else address = msg.Headers.From.Address;

            if (Directory.Exists($"Mailbox/{accountLogin}/{address}") == false)
            {
                Directory.CreateDirectory($"Mailbox/{accountLogin}/{address}");

                EmailCounter++;
                Utils.WriteLog($"Creating directory {address} // {EmailCounter}", Form1.LogControl);
            }

            return address;
        }
    }
}

class ParseData
{
    public string AccountLogin { get; }
    public string AccountPass { get; }

    public int ThreadID { get; }
    public int MessageCount { get; }
    public List<Message> Messages { get; }
    public ParseData(int tid, int mc, List<Message> msg, string login, string pass)
    {
        ThreadID = tid; MessageCount = mc; Messages = msg; AccountLogin = login; AccountPass = pass;
    }
}
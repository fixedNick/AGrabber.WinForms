using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGrabber.WinForms
{
    class Website
    {
        private static List<Website> Websites = new List<Website>();
        public string Address { get; private set; }

        public Website(string address)
        {
            Address = address;
        }

        public static bool Add(Website website, bool saveToFile = false)
        {
            foreach(var w in Websites)
            {
                if (w.Address.Equals(website.Address))
                    return false;
            }

            Websites.Add(website);
            if (saveToFile) File.AppendAllText("websites.txt", website.Address + Environment.NewLine);
            return true;
        }

        public static bool Add(string address, bool saveToFile = false)
        {
            foreach (var w in Websites)
            {
                if (w.Equals(address)) 
                    return false;
            }

            Websites.Add(new Website(address));
            if (saveToFile) File.AppendAllText("websites.txt", address + Environment.NewLine);
            return true;
        }

        public static List<Website> Get() => Websites;

        public static void LoadWebsitesFromFile()
        {
            if (File.Exists("websites.txt") == false) return;

            var websites = File.ReadAllLines("websites.txt");
            foreach(var w in websites)
                Add(w);
        }

    }
}

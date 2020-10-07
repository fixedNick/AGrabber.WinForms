using System;
using System.Collections.Generic;
using System.Linq;
using SeleniumDriver;
using OpenQA.Selenium;
using System.IO;
using System.Threading;

namespace AGrabber.WinForms
{
    class Marionette
    {
        private static Driver driver;

        private static string FILE_EMAIL_INPUT_SELECTOR = "selectors/email/email_input_selector.txt";
        private static string FILE_EMAIL_INPUT_CLASS = "selectors/email/email_input_class.txt";
        private static string FILE_EMAIL_INPUT_PLACEHOLDER = "selectors/email/email_input_placeholder.txt";

        private static string FILE_PHONE_INPUT_SELECTOR = "selectors/phone/phone_input_selector.txt";
        private static string FILE_PHONE_INPUT_CLASS = "selectors/phone/phone_input_class.txt";
        private static string FILE_PHONE_INPUT_PLACEHOLDER = "selectors/phone/phone_input_placeholder.txt";

        private static string FILE_ACTION_BUTTON_SELECTOR = "selectors/actionButton/action_button_selector.txt";
        private static string FILE_ACTION_BUTTON_CLASS = "selectors/actionButton/action_button_class.txt";
        private static string FILE_ACTION_BUTTON_TEXT = "selectors/actionButton/action_button_text.txt";

        private static string FILE_NAME_INPUT_PLACEHOLDER = "selectors/name_placeholders.txt";

        public static void SendEmailRequests(object obj)
        {
            Utils.DriverFileLog($"Запуск Marionette - {DateTime.Now.Day}/{DateTime.Now.Month}/{DateTime.Now.Year} | {DateTime.Now.Hour}:{DateTime.Now.Minute}");
            Utils.DriverFileLog("");

            driver = new Driver(Driver.DriverType.Chrome, Utils.DriverWriteLog, true, true, true, false);

            var websites = Website.Get();
            var account = Account.GetSelectedAccount();

            int counter = 0;
            foreach (var w in websites)
            {
                counter++;
                Utils.DriverWriteLog($"Переход к сайту номер {counter} из {websites.Count}");
                Utils.DriverFileLog($"Переход на сайт {w.Address}");
                driver.GoToUrl(w.Address);

                var actionButtons = new List<IWebElement>();

                Utils.DriverFileLog("Попытка найти поля формы");

                if (TryFindAndFillInputs(account, w) == false)
                {
                    // поиск кнопки
                    if (TryParseActionButton(ref actionButtons) && TryClickOnActionButton(actionButtons, w.Address))
                    {
                        Utils.DriverFileLog($"Найдено {actionButtons.Count} элементов под actionButton на сайте {w.Address}");
                        Utils.DriverFileLog("Начинаю поиск полей для заполнения формы");

                        if (TryFindAndFillInputs(account, w) == false)
                            Utils.DriverFileLog($"[!][ВАЖНО] Требуется дополнение параметров поиска для сайта {w.Address}");

                        continue;
                    }
                    continue;
                }

            }

            driver.StopDriver();
            Form1.MainForm.StartParseProccess();
        }
        private static Boolean TryClickOnActionButton(List<IWebElement> actionButtons, string address)
        {
            bool flag = false;
            foreach (var ab in actionButtons)
            {
                if (TrySendKeysToElement(ab, OpenQA.Selenium.Keys.Return))
                {
                    flag = true;
                    break;
                }
            }

            if (flag == false)
            {
                Utils.DriverFileLog($"Неудалось использовать найденную actionButton на сайте {address}");
                Utils.DriverFileLog($"[!][ВАЖНО] Требуется дополнение параметров поиска для сайта {address}");
            }
            return flag;
        }

        // метод поиска и заполнения компонентов
        private static bool TryFindAndFillInputs(Account account, Website w)
        {
            #region Temp Inputs
            var emailInputs = new List<IWebElement>();
            var phoneInputs = new List<IWebElement>();
            var namesInputs = new List<IWebElement>();
            var actionButtons = new List<IWebElement>();
            #endregion

            if (TryParseEmails(ref emailInputs) |
                TryParsePhone(ref phoneInputs) |
                TryParseName(ref namesInputs))
            {
                Utils.DriverFileLog($"Найдено {emailInputs.Count} элементов под email на сайте {w.Address}");
                Utils.DriverFileLog($"Найдено {phoneInputs.Count} элементов под phone на сайте {w.Address}");
                Utils.DriverFileLog($"Найдено {namesInputs.Count} элементов под names на сайте {w.Address}");


                if (FillInputs(new List<KeyValuePair<List<IWebElement>, string>>() {
                        new KeyValuePair<List<IWebElement>, string>(emailInputs, account.Login),
                        new KeyValuePair<List<IWebElement>, string>(phoneInputs, Account.Phone),
                        new KeyValuePair<List<IWebElement>, string>(namesInputs, Account.Name)
                    }) == false
                ) return false;

                var allInputs = new List<IWebElement>();
                allInputs.AddRange(emailInputs); allInputs.AddRange(phoneInputs); allInputs.AddRange(namesInputs);

                if (TrySendKeysToElements(allInputs, OpenQA.Selenium.Keys.Return) == false)
                {
                    // Данный код отрабатывает если у нас не получилось отправить форму с помощью Enter в одно из полей формы.
                    Utils.DriverFileLog($"Неудалось отправить форму с данными на сайте {w.Address} через поля ввода, требуется найти actionButton");
                    Utils.DriverFileLog($"Пытаюсь найти actionButton для отправки формы");

                    if (TryParseActionButton(ref actionButtons) && TryClickOnActionButton(actionButtons, w.Address))
                    {
                        Utils.DriverFileLog($"Отправка данных на сайт {w.Address} произведена успешно");
                        return true;
                    }

                    Utils.DriverFileLog($"[!][ВАЖНО] Неудалось отправить форму на сайт {w.Address}");
                    return true;
                }

                Thread.Sleep(1000);
                if (driver.GetCurrentUrl().Trim().ToLower().Equals(driver.NavigatedUrl.Trim().ToLower()) == true)
                    Thread.Sleep(3000);

                Utils.DriverFileLog($"Отправка данных на сайт {w.Address} произведена успешно");
                // Отправка данных прошла успешно
                return true;
            }

            Utils.DriverFileLog($"Неудалось найти поля для заполенния формы на сайте {w.Address}");
            return false;
        }


        // Возвращает FALSE, если заполняемые элементы не отображены на странице и сигнализирует о том, что нужно искать actionButton
        // Возвращает TRUE, если хотя бы один элемент был заполнен
        private static Boolean FillInputs(List<KeyValuePair<List<IWebElement>, string>> dict)
        {
            bool flag = false;
            foreach (var kp in dict)
            {
                if (kp.Key.Count == 0) continue;
                foreach (var element in kp.Key)
                {
                    if (TrySendKeysToElement(element, kp.Value))
                    {
                        flag = true;
                        break;
                    }
                }
            }
            return flag;
        }

        // Сводка по методам TryParseEmails, TryParseActionButton, TryParsePhone, TryParseName
        // Возвращают TRUE, если удалось найти хотя бы один IWebElement соответствующего компонента
        // Возвращают FALSE, если не удалось найти ни одного элемента на веб-странице
        // Список найденных элементов возвращается в параметре с оператором out

        // ?? TODO
        // Данные методы можно объеденить в один, уменьшив повторяющийся код и используя Enum
        private static Boolean TryParseEmails(ref List<IWebElement> emailInputs)
        {

            if (SearchElementsByClassNames(File.ReadAllLines(FILE_EMAIL_INPUT_CLASS).ToList(), ref emailInputs) |
                SearchElementsBySelectors(File.ReadAllLines(FILE_EMAIL_INPUT_SELECTOR).ToList(), ref emailInputs) |
                SearchElementsByPlaceholder(File.ReadAllLines(FILE_EMAIL_INPUT_PLACEHOLDER).ToList(), ref emailInputs))
                return true;

            return false;
        }
        private static Boolean TryParseActionButton(ref List<IWebElement> validActionButtons)
        {
            var actionButtons = new List<IWebElement>();

            if (SearchElementsByClassNames(File.ReadAllLines(FILE_ACTION_BUTTON_CLASS).ToList(), ref actionButtons) |
                SearchElementsBySelectors(File.ReadAllLines(FILE_ACTION_BUTTON_SELECTOR).ToList(), ref actionButtons))
            {
                // Проверяем найденные элементы на наличие нужного текста в них
                var actionButtonTextList = File.ReadAllLines(FILE_ACTION_BUTTON_TEXT).ToList();
                foreach (var ab in actionButtons)
                {
                    var abText = ab.Text;
                    foreach (var abTextFromList in actionButtonTextList)
                    {
                        if (abText.Trim().ToLower().Contains(abTextFromList.Trim().ToLower()))
                        {
                            validActionButtons.Add(ab);
                            break;
                        }
                    }
                }
            }

            if (validActionButtons.Count == 0)
                return false;

            return true;
        }
        private static Boolean TryParsePhone(ref List<IWebElement> phoneInputs)
        {
            if (SearchElementsByClassNames(File.ReadAllLines(FILE_PHONE_INPUT_CLASS).ToList(), ref phoneInputs) |
                SearchElementsBySelectors(File.ReadAllLines(FILE_PHONE_INPUT_SELECTOR).ToList(), ref phoneInputs) |
                SearchElementsByPlaceholder(File.ReadAllLines(FILE_PHONE_INPUT_PLACEHOLDER).ToList(), ref phoneInputs))
                return true;

            return false;
        }
        private static Boolean TryParseName(ref List<IWebElement> nameInputs)
        {
            if (SearchElementsByPlaceholder(File.ReadAllLines(FILE_NAME_INPUT_PLACEHOLDER).ToList(), ref nameInputs))
                return true;

            return false;
        }


        // Методы для заполнения полей и нажатий на кнопки
        private static Boolean TrySendKeysToElement(IWebElement element, string message, bool sendReturnKey = false)
        {
            try
            {
                driver.KeySend(element, message, sendReturnKey: sendReturnKey, allowException: true);
                return true;
            }
            catch { return false; }
        }
        private static Boolean TrySendKeysToElements(List<IWebElement> elements, string message)
        {
            bool flag = false;
            foreach (var e in elements)
            {
                try
                {
                    driver.KeySend(e, message, allowException: true);
                    flag = true;
                }
                catch { }
            }

            return flag;
        }

        // Сводка по трем методам  SearchElementsByClassNames, SearchElementsByPlaceholder, SearchElementsBySelectors
        // Возвращает TRUE, если найден хотя бы один IWebElement данного компонента
        // Возвращает FALSE, если не найдено ниодного элемента по начальным данным
        // Список элементов возвращаается с помощью оператора out в список foundElements
        private static Boolean SearchElementsByClassNames(List<string> names, ref List<IWebElement> foundElements)
        {
            var elementsWithClass = driver.FindCssList("[class]", isNullAcceptable: true);
            if (elementsWithClass == null || elementsWithClass.Count == 0)
                return false;
            int cnt = 0;
            foreach (var e in elementsWithClass)
            {
                cnt++;
                string classesString = string.Empty;
                try { classesString = e.GetAttribute("class"); }
                catch { continue; }
                var classes = classesString.Split(' ');

                foreach (var c in classes)
                {
                    foreach (var n in names)
                    {
                        if (c.ToLower().Trim().Equals(n.ToLower().Trim()))
                        {
                            foundElements.Add(e);
                            break;
                        }
                    }
                }
            }

            if (foundElements.Count == 0)
                return false;

            return true;
        }
        private static Boolean SearchElementsByPlaceholder(List<string> placeholders, ref List<IWebElement> foundElements)
        {

            var elementsWithPH = driver.FindCssList("[placeholder]", isNullAcceptable: true);
            if (elementsWithPH == null || elementsWithPH.Count == 0)
                return false;

            foreach (var ph in elementsWithPH)
            {
                foreach (var template in placeholders)
                {
                    if (ph.GetAttribute("placeholder").ToLower().Trim().Contains(template.ToLower().Trim()))
                    {
                        foundElements.Add(ph);
                        break;
                    }
                }
            }

            if (foundElements.Count == 0)
                return false;

            return true;
        }
        private static Boolean SearchElementsBySelectors(List<string> selectors, ref List<IWebElement> foundElements)
        {
            foreach (var selector in selectors)
            {
                var searchResult = driver.FindCssList(selector, isNullAcceptable: true);
                if (searchResult == null || searchResult.Count == 0)
                    continue;

                foundElements.AddRange(searchResult);
            }

            if (foundElements.Count == 0)
                return false;

            return true;
        }
    }
}

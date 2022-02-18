using System.IO;
using System.Xml;
using Spectre.Console;
using TheAirBlow.Skysmart.Library;

namespace TheAirBlow.Skysmart.Console
{
    internal class Program
    {
        /// <summary>
        /// Was made mostly for testing
        /// </summary>
        public static void Main(string[] args)
        {
            AnsiConsole.MarkupLine("[cyan]Добро пожаловать в SkySmart Solver от TheAirBlow![/]");
            if (!File.Exists("token.txt")) {
                AnsiConsole.MarkupLine("[yellow]Ты не вошел в свой аккаунт![/]");
                var login = AnsiConsole.Ask<string>("Телефон или Почта:");
                var password = AnsiConsole.Ask<string>("Пароль:");
                WebHelper.Authenticate(login, password);
                File.WriteAllText("token.txt", WebHelper.Token);
            } else {
                AnsiConsole.MarkupLine("[green]Обнаружен файл с токеном![/]");
                WebHelper.Token = File.ReadAllText("token.txt");
            }
            var code = AnsiConsole.Ask<string>("Код Задания:");
            AnsiConsole.MarkupLine("[yellow]Подождите, загрузка UUID заданий...[/]");
            var uuids = WebHelper.GetAnswerXmlsUuids(code);
            for (var i = 0; i < uuids.Length; i++) {
                var uuid = uuids[i];
                var xml = WebHelper.GetAnswerXml(uuid);
                var name = "(Unable to find)";
                var root = xml["div"];
                if (root.SelectSingleNode("vim-instruction") != null)
                    name = root["vim-instruction"]?.InnerText;
                else if (root.SelectSingleNode("vim-content-section-titl") != null)
                    name = root["vim-content-section-title"]?.InnerText;
                else if (root.SelectSingleNode("vim-text") != null)
                    name = root["vim-text"]?.InnerText;
                AnsiConsole.MarkupLine($"[green]Задание №{i}: {name}[/]");
                #region Test Question
                foreach (XmlNode sus in root.SelectNodes("//vim-test")) {
                    var select = sus?.FirstChild;
                    // ReSharper disable once PossibleNullReferenceException
                    foreach (XmlNode item in select["vim-test-answers"]?.ChildNodes) {
                        AnsiConsole.MarkupLine($"[cyan]Ответ на Тест: {item.InnerText}[/]");
                        break;
                    }
                }
                #endregion
                #region Input Question
                // ReSharper disable once PossibleNullReferenceException
                foreach (XmlNode vim in root.SelectNodes("//vim-input")) {
                    var select = vim?.FirstChild;
                    // ReSharper disable once PossibleNullReferenceException
                    foreach (XmlNode item in select?.ChildNodes) {
                        AnsiConsole.MarkupLine($"[cyan]Введи: {item.InnerText}[/]");
                        break;
                    }
                }
                #endregion
                #region Select Question
                // ReSharper disable once PossibleNullReferenceException
                foreach (XmlNode vim in root.SelectNodes("//vim-select")) {
                    var select = vim?.FirstChild;
                    // ReSharper disable once PossibleNullReferenceException
                    foreach (XmlNode item in select?.ChildNodes) {
                        if (item.Attributes?["correct"] != null &&
                            item.Attributes?["correct"].InnerText == "true") {
                            AnsiConsole.MarkupLine($"[cyan]Выбери: {item.FirstChild.InnerText}[/]");
                            break;
                        }
                    }
                }
                #endregion
            }
        }
    }
}
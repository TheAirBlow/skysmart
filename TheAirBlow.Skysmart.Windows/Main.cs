using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using TheAirBlow.Skysmart.Library;

namespace TheAirBlow.Skysmart.Windows
{
    public partial class Main : Form
    {
        private List<List<string>> Answers = new();

        public Main() => InitializeComponent();

        /// <summary>
        /// Logoff
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            new Login().Show();
            Close();
        }

        /// <summary>
        /// Solve the exercise
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            try {
                Answers.Clear();
                comboBox1.Items.Clear();
                checkedListBox1.Items.Clear();
                var uuids = WebHelper.GetAnswerXmlsUuids(textBox1.Text);
                for (var i = 0; i < uuids.Length; i++) {
                    var uuid = uuids[i];
                    var xml = WebHelper.GetAnswerXml(uuid);
                    var name = "(Не удалось найти название)";
                    var root = xml["div"];
                    if (root.SelectSingleNode("vim-instruction") != null)
                        name = root["vim-instruction"]?.InnerText;
                    else if (root.SelectSingleNode("vim-content-section-titl") != null)
                        name = root["vim-content-section-title"]?.InnerText;
                    else if (root.SelectSingleNode("vim-text") != null)
                        name = root["vim-text"]?.InnerText;
                    comboBox1.Items.Add($"Задание №{i}: {name}");
                    var ans = new List<string>();
                    #region Test Question
                    foreach (XmlNode sus in root.SelectNodes("//vim-test")) {
                        var select = sus?.FirstChild;
                        // ReSharper disable once PossibleNullReferenceException
                        foreach (XmlNode item in select["vim-test-answers"]?.ChildNodes) {
                            ans.Add($"Ответ на Тест: {item.InnerText}");
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
                            ans.Add($"Введи: {item.InnerText}");
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
                                ans.Add($"Выбери: {item.FirstChild.InnerText}");
                                break;
                            }
                        }
                    }
                    #endregion
                    Answers.Add(ans);
                }
            } catch (Exception ex) {
                MessageBox.Show("Не удалось решить задание.\nПроверьте код, " +
                                "который вы ввели, а также связь с Интернетом." +
                                $"\n{ex.Message}", 
                    "Ошибка во время решения!", MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Select an exercise
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            // ReSharper disable once CoVariantArrayConversion
            checkedListBox1.Items.AddRange(Answers[comboBox1.SelectedIndex].ToArray());
        }

        /// <summary>
        /// Verify bearer token
        /// </summary>
        private void Main_Load(object sender, EventArgs e)
        {
            try {
                var info = WebHelper.GetInformation();
                label3.Text = $"Добро пожаловать, {info.Name} {info.Surname}!";
            } catch {
                MessageBox.Show("Не удалось войти в аккаунт, используя токен.", 
                    "Ошибка во время входа!", MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                new Login().Show();
                Close();
            }
            
        }
    }
}
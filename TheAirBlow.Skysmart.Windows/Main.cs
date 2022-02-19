﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using TheAirBlow.Skysmart.Library;

namespace TheAirBlow.Skysmart.Windows
{
    public partial class Main : Form
    {
        private volatile List<List<string>> _answers = new();
        private volatile List<WebHelper.ExerciseXml> _xmls = new();
        private volatile List<string> _comboBoxItems = new();
        private Thread _thread;

        public Main() => InitializeComponent();

        /// <summary>
        /// Logoff
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            new Login().Show();
            Hide();
        }

        /// <summary>
        /// Solve the exercise
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            _xmls.Clear();
            _answers.Clear();
            comboBox1.Items.Clear();
            checkedListBox1.Items.Clear();

            _thread = new Thread(() => {
                try {
                    WebHelper.ExerciseMeta uuids;
                    try { uuids = WebHelper.GetAnswerXmlsUuids(textBox1.Text); } 
                    catch {
                        MessageBox.Show("Комнаты с таким ID не существует." +
                                        "\nПроверьте введенную вами информацию.", 
                            "Ошибка во время решения!", MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                        return;
                    }
                    textBox6.Text = uuids.Meta.Subject.Title;
                    textBox7.Text = $"{uuids.Meta.TeacherInformation.Surname} " +
                                    $"{uuids.Meta.TeacherInformation.Name}";
                    Invoke(() => {
                        panel1.Enabled = true;
                        panel1.Visible = true;
                    });
                    Invoke(() => {
                        progressBar1.Maximum = uuids.Meta.Uuids.Length + 1;
                        progressBar1.Style = ProgressBarStyle.Blocks;
                        progressBar1.Value = 0;
                    });
                    for (var i = 0; i < uuids.Meta.Uuids.Length; i++) {
                        Invoke(() => {
                            label2.Text = $"Решаем задание {i + 1}/{uuids.Meta.Uuids.Length}";
                            progressBar1.Increment(1);
                        });
                        var uuid = uuids.Meta.Uuids[i];
                        var xml = WebHelper.GetAnswerXml(uuid, uuids);
                        var root = xml.XmlContent["div"];
                        _comboBoxItems.Add($"Задание №{i + 1}: {xml.Title}");

                        var ans = new List<string>();
                        var list = root.SelectNodes("//*");
                        for (var b = 0; b < list.Count; b++) {
                            var sus = list[b];
                            switch (sus.Name) {
                                #region Math Question
                                case "vim-math":
                                    try {
                                        var id = sus.Attributes?["id"].InnerText;
                                        var input = root.SelectSingleNode($"//*[@id='{id.Replace("math", "MI")}']");
                                        if (input != null)
                                            ans.Add($"{sus.InnerText}{input.InnerText}");
                                    } catch (Exception ex) {
                                        MessageBox.Show("Не удалось решить математический пример.\n" +
                                                        $"UUID: {uuid}\nСообщите об этом разработчику.",
                                            "Ошибка во время решения!", MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                        File.WriteAllText($"{uuid}.xml", root.InnerXml);
                                    }
                                    break;
                                #endregion
                                #region Test Question
                                case "vim-test":
                                    try {
                                        var select = sus?.FirstChild;
                                        ans.Add($"Тест: {select["vim-test-answers"]?.ChildNodes?[0].InnerText}");
                                    } catch (Exception ex) {
                                        MessageBox.Show("Не удалось решить тест.\n" +
                                                        $"UUID: {uuid}\nСообщите об этом разработчику.",
                                            "Ошибка во время решения!", MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                        File.WriteAllText($"{uuid}.xml", root.InnerXml);
                                    }
                                    break;
                                #endregion
                                #region Input Question
                                case "vim-input":
                                    try {
                                        var select = sus?.FirstChild;
                                        foreach (XmlNode item in @select?.ChildNodes) {
                                            ans.Add($"Введи: {item.InnerText}");
                                            break;
                                        }
                                    } catch (Exception ex) {
                                        MessageBox.Show("Не удалось решить задания с вводом ответа.\n" +
                                                        $"UUID: {uuid}\nСообщите об этом разработчику.",
                                            "Ошибка во время решения!", MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                        File.WriteAllText($"{uuid}.xml", root.InnerXml);
                                    }

                                    break;
                                #endregion
                                #region Select Question
                                case "vim-select":
                                    try {
                                        var select = sus?.FirstChild;
                                        foreach (XmlNode item in @select?.ChildNodes) {
                                            if (item.Attributes?["correct"] != null &&
                                                item.Attributes?["correct"].InnerText == "true") {
                                                ans.Add($"Выбери: {item.FirstChild.InnerText}");
                                                break;
                                            }
                                        }
                                    } catch (Exception ex) {
                                        MessageBox.Show("Не удалось решить задания с выбором.\n" +
                                                        $"UUID: {uuid}\nСообщите об этом разработчику.",
                                            "Ошибка во время решения!", MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                        File.WriteAllText($"{uuid}.xml", root.InnerXml);
                                    }
                                    break;
                                #endregion
                                #region Drag&Drop Question
                                case "vim-dnd-group":
                                    try {
                                        var drags = sus["vim-dnd-group-drags"];
                                        foreach (XmlNode item in sus["vim-dnd-group-groups"]!) {
                                            var ids = item.Attributes?["drag-ids"].InnerText.Split(',');
                                            foreach (var id in ids)
                                                ans.Add(
                                                    $"Перетащи {drags?.SelectSingleNode($"//*[@answer-id='{id}']").InnerText}" +
                                                    $" в {item.FirstChild.InnerText}");
                                        }
                                    } catch (Exception ex) {
                                        MessageBox.Show("Не удалось решить Drag&Drop задание.\n" +
                                                        $"UUID: {uuid}\nСообщите об этом разработчику.",
                                            "Ошибка во время решения!", MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                        File.WriteAllText($"{uuid}.xml", root.InnerXml);
                                    }
                                    break;
                                #endregion
                                #region Text Drag&Drop Question
                                case "vim-dnd-text":
                                    try {
                                        var drags = sus["vim-dnd-text-drags"];
                                        var nodes = sus.SelectNodes("//vim-dnd-text-drop");
                                        for (var h = 0; h < nodes.Count; h++) {
                                            var item = nodes[h];
                                            var ids = item.Attributes?["drag-ids"].InnerText.Split(',');
                                            ans.Add($"Перетащи {drags?.SelectSingleNode($"//*[@answer-id='{ids[0]}']").InnerText}" +
                                                    $" в \"{item.ParentNode.InnerText}\"");
                                        }
                                    } catch (Exception ex) {
                                        MessageBox.Show("Не удалось решить Drag&Drop задание.\n" +
                                                        $"UUID: {uuid}\nСообщите об этом разработчику.",
                                            "Ошибка во время решения!", MessageBoxButtons.OK,
                                            MessageBoxIcon.Error);
                                        File.WriteAllText($"{uuid}.xml", root.InnerXml);
                                    }
                                    break;
                                #endregion
                            }
                        }

                        if (ans.Count == 0) {
                            MessageBox.Show("Не удалось решить одно из заданий.\n" +
                                            $"UUID: {uuid}\nСообщите об этом разработчику.", 
                                "Ошибка во время решения!", MessageBoxButtons.OK, 
                                MessageBoxIcon.Error);
                            File.WriteAllText($"{uuid}.xml", root.InnerXml);
                        }
                        _answers.Add(ans);
                        _xmls.Add(xml);
                    }
                    Invoke(() => {
                        panel1.Enabled = false;
                        panel1.Visible = false;
                    });
                } catch (Exception ex) {
                    Invoke(() => {
                        panel1.Enabled = true;
                        panel1.Visible = true;
                        label2.Text = $"Ошибка во время решения, ожидание команд...";
                        progressBar1.Style = ProgressBarStyle.Marquee;
                    });
                    MessageBox.Show("Не удалось решить задание.\nПроверьте код, " +
                                    "который вы ввели, а также связь с Интернетом." +
                                    $"\n{ex.Message}", 
                        "Ошибка во время решения!", MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
            });
            
            _thread.Start();
        }
        
        /// <summary>
        /// Select an exercise
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            // ReSharper disable once CoVariantArrayConversion
            checkedListBox1.Items.AddRange(_answers[comboBox1.SelectedIndex].ToArray());

            textBox2.Text = _xmls[comboBox1.SelectedIndex].Uuid;
            textBox3.Text = _xmls[comboBox1.SelectedIndex].IsRandom ? "Да" : "Нет";
            textBox4.Text = _xmls[comboBox1.SelectedIndex].ExerciseIdentifier.ToString();
            textBox5.Text = _xmls[comboBox1.SelectedIndex].IsInteractive ? "Да" : "Нет";
        }

        /// <summary>
        /// Verify bearer token
        /// </summary>
        private void Main_Load(object sender, EventArgs e)
        {
            timer1.Tick += (_, _) => {
                button1.Enabled = _thread is not { IsAlive: true };
                if (_comboBoxItems.Count == 0
                    || _thread is { IsAlive: true }) return;
                comboBox1.Items.Clear();
                // ReSharper disable once CoVariantArrayConversion
                comboBox1.Items.AddRange(_comboBoxItems.ToArray());
                _comboBoxItems.Clear();
            };
            Closed += (_, _) => Environment.Exit(0);
            timer1.Start();

            try {
                var info = WebHelper.GetInformation();
                label3.Text = $"Добро пожаловать, {info.Name} {info.Surname}!";
            } catch {
                MessageBox.Show("Не удалось войти в аккаунт, используя токен.", 
                    "Ошибка во время входа!", MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                new Login().Show();
                Hide();
            }
            
        }
    }
}
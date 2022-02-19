using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using TheAirBlow.Skysmart.Library;

namespace TheAirBlow.Skysmart.Windows
{
    public partial class Login : Form
    {
        public Login() => InitializeComponent();

        /// <summary>
        /// Exit
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
            => Environment.Exit(0);

        /// <summary>
        /// Login
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            var login = textBox2.Text;
            var pass = textBox1.Text;
            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            button1.Enabled = false;
            new Thread(() => {
                try {
                    WebHelper.Authenticate(login, pass);
                    File.WriteAllText("token.txt", WebHelper.Token);
                    new Main().Show();
                    Hide();
                } catch (WebException ex) {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                        MessageBox.Show("Неправильный логин или пароль." +
                                        "\nПроверьте введеную вами информацию.", 
                            "Ошибка во время входа!", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    else MessageBox.Show("Произошла неизвестная ошибка." +
                                         "\nПроверьте связь с Интернетом." +
                                         $"\n{ex.Message}", 
                        "Ошибка во время входа!", MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
                
                Invoke(() => {
                    textBox1.ReadOnly = false;
                    textBox2.ReadOnly = false;
                    button1.Enabled = true;
                });
            }).Start();
        }

        private void Login_Load(object sender, EventArgs e)
            => Closed += (_, _) => Environment.Exit(0);
    }
}
using System;
using System.IO;
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
            try {
                WebHelper.Authenticate(textBox2.Text, textBox1.Text);
                File.WriteAllText("token.txt", WebHelper.Token);
                new Main().Show();
                Close();
            } catch (Exception ex) {
                MessageBox.Show("Не удалось войти в аккаунт SkySmart.\nПроверьте данные, " +
                                "которые вы ввели, а также связь с Интернетом." +
                                $"\n{ex.Message}", 
                    "Ошибка во время входа!", MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void Login_Load(object sender, EventArgs e)
            => Closed += (_, _) => Environment.Exit(0);
    }
}
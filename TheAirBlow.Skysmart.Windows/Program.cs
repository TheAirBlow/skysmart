using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheAirBlow.Skysmart.Library;

namespace TheAirBlow.Skysmart.Windows
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!File.Exists("token.txt")) {
                new Login().Show();
                Application.Run();
            } else {
                WebHelper.Token = File.ReadAllText("token.txt");
                new Main().Show();
                Application.Run();
            }
            
        }
    }
}
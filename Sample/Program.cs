using System;
using System.Windows.Forms;

namespace Sample
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //MainWindow mw = new MainWindow();
            //mw.GetRequest(mw.currencyCodeUrl);
            //mw.GetCurrencyCodes();
            //mw.FillComboBoxes();
            Application.Run(new MainWindow());
        }
    }
}

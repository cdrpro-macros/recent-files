using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace RecentFiles
{
    public partial class wAbout : Window
    {
        public wAbout()
        {
            InitializeComponent();

            this.sName.Text = RFcontrol.mName;
            this.sInfo.Text = "Version: " + RFcontrol.mVer + "\n" +
                "Release date: " + RFcontrol.mDate + "\n" +
                "Copyright © Sanich, " + RFcontrol.mYear;
            this.sWeb.Text = RFcontrol.mWebSite;
            this.sEmail.Text = "e-mail: " + RFcontrol.mEmail;
        }

        private void cmClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void sWeb_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(RFcontrol.mWebSite);
            this.Close();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                MessageBox.Show(
                    "Number of documents in the base: " + RFcontrol.DocCount.ToString(CultureInfo.InvariantCulture) +
                    "\nCode of current UI language: " + RFcontrol.dApp.UILanguage.GetHashCode().ToString(CultureInfo.InvariantCulture),
                    RFcontrol.mName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
    }
}

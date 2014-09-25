using ServiceLibrary;
using System;
using System.Windows;

namespace LabRun
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private Service service = Service.getInstance();
        private MainWindow parent = null;

        public Login(MainWindow parent)
        {
            InitializeComponent();
            this.Owner = parent;
            this.parent = parent;
            txbName.Text = "";
            txbPass.Password = "";
            txbName.Focus();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            User user = null;
            try
            {
                user = service.Login(txbName.Text, txbPass.Password);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to server. Please contant your system administrator.\n\n" + ex.Message, "Timed out", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (user == null)
            {
                MessageBox.Show("Login Failed! Username and/or password was incorrect.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                //MessageBox.Show("Login Failed! Username and/or password was incorrect.", "Login Success", MessageBoxButton.OK, MessageBoxImage.Information);
                parent.SetLogin(user);
                service.InitProjects();
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

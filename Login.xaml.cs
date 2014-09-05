using ServiceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
            this.parent = parent;
            txbName.Text = "dtutinas";
            txbPass.Password = "QpbP9422";
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            User user = service.Login(txbName.Text, txbPass.Password);
            if (user == null)
            {
                MessageBox.Show("Login Failed! Username and/or password was incorrect.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                //MessageBox.Show("Login Failed! Username and/or password was incorrect.", "Login Success", MessageBoxButton.OK, MessageBoxImage.Information);
                parent.SetLogin(user);
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

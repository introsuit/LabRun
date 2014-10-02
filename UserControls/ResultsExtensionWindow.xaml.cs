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

namespace UserControls
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ResultsExtensionWindow : Window
    {
        CustomRun parent;
        List<StringValue> extensions = new List<StringValue>();
        public ResultsExtensionWindow(CustomRun parent, List<string> paramExtensions)
        {
            InitializeComponent();
            if (paramExtensions != null) { 
                foreach (string temp in paramExtensions)
                {
                    extensions.Add(new StringValue(temp));
                }
            }
            dgrExtensions.ItemsSource = extensions;
            this.parent = parent;
     
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (txtbxExtField.Text.Trim().Length != 0)
            {
                StringValue str = new StringValue(txtbxExtField.Text);
                extensions.Add(str);
                dgrExtensions.Items.Refresh();
            }
        }

        public class StringValue
        {
            public StringValue(string s)
            {
                _value = s;
            }
            public string Value { get { return _value; } set { _value = value; } }
            string _value;
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            extensions.Remove((StringValue)dgrExtensions.SelectedItem);
            dgrExtensions.Items.Refresh();
            
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            List<string> strExtensions = new List<string>();
            foreach (StringValue temp in extensions) {
                strExtensions.Add(temp.Value);
            }
            parent.extensions = strExtensions;
            this.Close();
        }
    }
}

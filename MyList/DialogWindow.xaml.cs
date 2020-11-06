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

namespace MyList
{
    /// <summary>
    /// Логика взаимодействия для DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window
    {
        public bool IsDontSee;
        int last = 5;
        bool wasmore = false;
        public DialogWindow(Window w, string label, bool dontsee)
        {
            InitializeComponent();
            this.Owner = w;
            this.Resources = w.Resources;
            this.tbMessage.Text = label.ToString();
            if(dontsee)
                this.chbDontSee.Visibility = Visibility.Visible;
            else
                this.chbDontSee.Visibility = Visibility.Hidden;
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void chbDontSee_Checked(object sender, RoutedEventArgs e)
        {
            IsDontSee = true;
        }

        private void chbDontSee_Unchecked(object sender, RoutedEventArgs e)
        {
            IsDontSee = false;
        }

        private void tbMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbMessage.LineCount > 5 && tbMessage.LineCount != last)
            {
                if (tbMessage.LineCount > 20 && !wasmore)
                {
                    this.Width += 90;
                    tbMessage.Width += 90;
                    wasmore = true;
                }

                tbMessage.Height += (tbMessage.LineCount - last) * 16;
                this.Height += (tbMessage.LineCount - last) * 16;
                last = tbMessage.LineCount;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
    /// Interaction logic for SupportForm.xaml
    /// </summary>
    public partial class SupportForm : Window
    {
        List<string> Pathes = new List<string>();
        private void SendMessage()
        {
            if (tbMail.Text != "")
            {
                try
                {
                    MailMessage mail = new MailMessage("andrey14scr@gmail.com", "andrey14scr@gmail.com");
                    mail.Subject = "MyList";
                    mail.Body = tbMail.Text;
                    if(tbEmail.Text != "")
                        mail.Body += "\nFeedback: " + tbEmail.Text;
                    SmtpClient client = new SmtpClient("smtp.gmail.com");
                    client.Port = 587;
                    client.Credentials = new System.Net.NetworkCredential("andrey14scr@gmail.com", "59645206gg14");
                    client.EnableSsl = true;
                    foreach (var item in Pathes)
                    {
                        mail.Attachments.Add(new Attachment(item));
                    }
                    client.Send(mail);
                    MessageBox.Show("Mail is sent successfully!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            DialogResult = true;
        }

        public SupportForm(MainWindow w)
        {
            this.Owner = w;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            InitializeComponent();
            rec.StrokeDashArray = new DoubleCollection(new List<double>() { 0, 0 });
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void tbMail_Drop(object sender, DragEventArgs e)
        {
            foreach (string file in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                Pathes.Add(file);
                Image im = new Image();
                im.Height = 32;
                im.Width = lbFiles.Width - 14;
                using (System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(file))
                {
                    im.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }

                string[] s = file.Split('\\');

                StackPanel sp = new StackPanel();
                
                sp.HorizontalAlignment = HorizontalAlignment.Center;
                TextBlock tb = new TextBlock() {Text = s[s.Length-1]};
                tb.TextWrapping = TextWrapping.Wrap;
                tb.Width = lbFiles.Width - 14;
                tb.TextAlignment = TextAlignment.Center;

                sp.Children.Add(im);
                sp.Children.Add(tb);

                lbFiles.Items.Add(sp);
                
            }

            tbMail.Foreground = Brushes.Black;
            rec.StrokeDashArray = new DoubleCollection(new List<double>() { 0, 0 });
            tbMail.BorderThickness = new Thickness(1);
        }

        private void tbMail_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            tbMail.Foreground = Brushes.LightGray;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;

            rec.StrokeDashArray = new DoubleCollection(new List<double>() { 5, 5 });
            tbMail.BorderThickness = new Thickness(0);
        }

        private void tbMail_PreviewDragLeave(object sender, DragEventArgs e)
        {
            tbMail.Foreground = Brushes.Black;
            rec.StrokeDashArray = new DoubleCollection(new List<double>() { 0, 0 });
            tbMail.BorderThickness = new Thickness(1);
        }

        private void ListBoxItem_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = Pathes[lbFiles.SelectedIndex];
            myProcess.Start();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            Pathes.Clear();
            lbFiles.Items.Clear();
        }
    }
}

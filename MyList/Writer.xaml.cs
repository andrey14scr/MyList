using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyList
{
    /// <summary>
    /// Логика взаимодействия для Writer.xaml
    /// </summary>
    public partial class Writer : Window
    {
        List<string> WeekDays;
        List<string> Monthes;

        bool allcheck = false;
        string inputplease;
        bool isend = false;
        public bool IsEdit = false;
        
        public int wrIntervalValue = -1;
        public int wrIntervalVars = -1;

        public int Kind = 0;
        public int wrRemindValueBefore = 0;
        public int wrRemindValueEvery = -1;

        private void FindVariants(UIElementCollection collection)
        {
            int i = 0;
            bool[] bools = new bool[32];
            for (int j = 0; j < bools.Length; j++)
            {
                bools[j] = false;
            }
            wrIntervalVars = -1;
            foreach (var item in collection)
            {
                if (item is CheckBox)
                {
                    if ((item as CheckBox).IsChecked == true)
                    {
                        if ((item as CheckBox).Content.ToString() == WeekDays[6] && i == 6)
                            i = -1;
                        bools[i+1] = true;
                    }
                    i++;
                }
            }
            wrIntervalVars = GlobalClass.BitArrayToInt(bools);
        }
        private bool Checkcbs(UIElementCollection collection)
        {
            foreach (var item in collection)
            {
                if ((item is CheckBox) && (item as CheckBox).IsChecked == true)
                {
                    return true;
                }
            }
            return false;
        }
        private bool CheckDate(int day)
        {
            if ((canvas.Children[3] as CheckBox).IsChecked == true)
                if (day > 31)
                    return false;
            if ((canvas.Children[4] as CheckBox).IsChecked == true)
                if (day > 28)
                    return false;
            if ((canvas.Children[5] as CheckBox).IsChecked == true)
                if (day > 31)
                    return false;
            if ((canvas.Children[6] as CheckBox).IsChecked == true)
                if (day > 30)
                    return false;
            if ((canvas.Children[7] as CheckBox).IsChecked == true)
                if (day > 31)
                    return false;
            if ((canvas.Children[8] as CheckBox).IsChecked == true)
                if (day > 30)
                    return false;
            if ((canvas.Children[9] as CheckBox).IsChecked == true)
                if (day > 31)
                    return false;
            if ((canvas.Children[10] as CheckBox).IsChecked == true)
                if (day > 31)
                    return false;
            if ((canvas.Children[11] as CheckBox).IsChecked == true)
                if (day > 30)
                    return false;
            if ((canvas.Children[12] as CheckBox).IsChecked == true)
                if (day > 31)
                    return false;
            if ((canvas.Children[13] as CheckBox).IsChecked == true)
                if (day > 30)
                    return false;
            if ((canvas.Children[14] as CheckBox).IsChecked == true)
                if (day > 31)
                    return false;
            return true;
        }
        public Writer(Window w)
        {
            this.Owner = w;
            this.Resources = w.Resources;
            InitializeComponent();

            WeekDays = new List<string>() { (string)this.FindResource("stringMonday"), (string)this.FindResource("stringTuesday"), (string)this.FindResource("stringWednesday"), (string)this.FindResource("stringThursday"), (string)this.FindResource("stringFriday"), (string)this.FindResource("stringSaturday"), (string)this.FindResource("stringSunday") };
            Monthes = new List<string>() { (string)this.FindResource("stringJanuary"), (string)this.FindResource("stringFebruary"), (string)this.FindResource("stringMarch"), (string)this.FindResource("stringApril"), (string)this.FindResource("stringMay"), (string)this.FindResource("stringJune"), (string)this.FindResource("stringJuly"), (string)this.FindResource("stringAugust"), (string)this.FindResource("stringSeptember"), (string)this.FindResource("stringOctober"), (string)this.FindResource("stringNovember"), (string)this.FindResource("stringDecember") };
            
            inputplease = (string)this.FindResource("stringWriteTextNote");
            isend = true;
            this.tbText.Text = inputplease;
            this.tbText.Foreground = new SolidColorBrush(Colors.Gray);
            for (int i = 0; i < 24; i++)
            {
                cbHour.Items.Add(String.Format("{0:d2}", i));
            }
            for (int i = 0; i < 60; i++)
            {
                cbMinute.Items.Add(String.Format("{0:d2}", i));
            }

            cbKind.SelectedIndex = (int)TypeNote.Usual;
            cbKindRemind.SelectedIndex = (int)KindRemind.Usual;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (cbKindRemind.SelectedIndex != (int)KindRemind.Usual) 
            {
                if (!(dpDate.SelectedDate != null && cbHour.Text != (string)this.FindResource("stringH") && cbMinute.Text != (string)this.FindResource("stringM")))
                {
                    MessageBox.Show("Напоминание только с датой и временем!");
                    return;
                }
            }
            
            switch (cbKindRemind.SelectedIndex)
            {
                case 1:
                    if(!Int32.TryParse((canvasrem.Children[1] as TextBox).Text, out wrRemindValueBefore) || wrRemindValueBefore < 0)
                    {
                        MessageBox.Show("Время - целое положительное число!");
                        return;
                    }
                    if ((canvasrem.Children[2] as ComboBox).SelectedIndex == (int)KindTime.Hour)
                        wrRemindValueBefore *= 60;
                    if ((canvasrem.Children[2] as ComboBox).SelectedIndex == (int)KindTime.Day)
                        wrRemindValueBefore *= 1440;
                    Kind = 1;
                    break;
                case 2:
                    if (!Int32.TryParse((canvasrem.Children[1] as TextBox).Text, out wrRemindValueBefore) || wrRemindValueBefore < 0)
                    {
                        MessageBox.Show("Время - целое положительное число!");
                        return;
                    }
                    if (!Int32.TryParse((canvasrem.Children[4] as TextBox).Text, out wrRemindValueEvery) || wrRemindValueEvery <= 0)
                    {
                        MessageBox.Show("Время - целое положительное число и период не равен 0!");
                        return;
                    }
                    if ((canvasrem.Children[2] as ComboBox).SelectedIndex == (int)KindTime.Hour)
                        wrRemindValueBefore *= 60;
                    if ((canvasrem.Children[2] as ComboBox).SelectedIndex == (int)KindTime.Day)
                        wrRemindValueBefore *= 1440;
                    if ((canvasrem.Children[5] as ComboBox).SelectedIndex == (int)KindTime.Hour)
                        wrRemindValueEvery *= 60;
                    if ((canvasrem.Children[5] as ComboBox).SelectedIndex == (int)KindTime.Day)
                        wrRemindValueEvery *= 1440;
                    Kind = 2;
                    break;
                default:
                    wrRemindValueBefore = 0;
                    wrRemindValueEvery = -1;
                    Kind = 0;
                    break;
            }
            

            int tempnum = -1;
            bool ok = false;
            if (Int32.TryParse(cbHour.Text, out tempnum) && tempnum >= 0 && tempnum < 24)
                ok = true;
            if (cbHour.Text == (string)this.FindResource("stringH"))
                ok = true;
            if (!ok)
            {
                cbMinute.Items.Add((string)this.FindResource("stringM"));
                cbMinute.Text = (string)this.FindResource("stringM");
                MessageBox.Show("Неверный формат времени(минуты)");
                return;
            }

            ok = false;
            if (Int32.TryParse(cbMinute.Text, out tempnum) && tempnum >= 0 && tempnum < 60)
                ok = true;
            if (cbMinute.Text == (string)this.FindResource("stringM"))
                ok = true;
            if (!ok) 
            {
                cbMinute.Items.Add((string)this.FindResource("stringM"));
                cbMinute.Text = (string)this.FindResource("stringM");
                MessageBox.Show("Неверный формат времени(минуты)");
                return;
            }



            if (tbText.Text == "" || tbText.Text == inputplease)
            {
                MessageBox.Show("Вы не ввели текст!");
            }
            else if (dpDate.SelectedDate == null)
            {
                MessageBox.Show("Вы не ввели первоначальную дату!");
            }
            else if (!IsEdit && dpDate.SelectedDate != null && dpDate.SelectedDate.Value.Date < DateTime.Now.Date)
            {
                MessageBox.Show("Дата не может быть прошедшей!");
            }
            else if ((cbHour.Text.ToString() == (string)this.FindResource("stringH") && cbMinute.Text.ToString() != (string)this.FindResource("stringM")) || (cbHour.Text.ToString() != (string)this.FindResource("stringH") && cbMinute.Text.ToString() == (string)this.FindResource("stringM")))
            {
                MessageBox.Show("Введите время полностью!");
            }
            else if (!IsEdit && dpDate.SelectedDate == DateTime.Now.Date && cbHour.Text.ToString() != (string)this.FindResource("stringH") && cbMinute.Text.ToString() != (string)this.FindResource("stringM") && (Int32.Parse(cbHour.Text.ToString())*60 + Int32.Parse(cbMinute.Text.ToString()) < DateTime.Now.Hour*60 + DateTime.Now.Minute) && dpDate.SelectedDate != null) 
            {
                MessageBox.Show("Время не может быть прошедшим!");
            }

            else if (cbKind.SelectedIndex == (int)(int)TypeNote.DaysPeriod && (!Int32.TryParse((canvas.Children[1] as TextBox).Text, out wrIntervalValue) || wrIntervalValue < 1 || wrIntervalValue > 1000))
            {
                MessageBox.Show("Количество дней должно быть целым положительным числом, не превосходящим 1000!");
            }
            else if (cbKind.SelectedIndex == (int)(int)TypeNote.WeekPeriod && !Checkcbs(canvas.Children))
            {
                MessageBox.Show("Должен быть выбран хотя бы один день!");
            }
            else if (cbKind.SelectedIndex == (int)(int)TypeNote.MonthPeriod && !Checkcbs(canvas.Children))
            {
                MessageBox.Show("Количество дней должно быть целым неотрицательным числом!\nДолжен быть выбран хотя бы один месяц!");
            }
            else if (cbKind.SelectedIndex == (int)(int)TypeNote.MonthPeriod && (!Int32.TryParse((canvas.Children[1] as TextBox).Text, out wrIntervalValue) || !CheckDate(wrIntervalValue) || wrIntervalValue < 1 || !Checkcbs(canvas.Children)))
            {
                MessageBox.Show("Номер дня должен быть целым положительным числом, присутствующим во всех месяцах!\nДолжен быть выбран хотя бы один месяц!");
            }
            else
            {
                if (cbKind.SelectedIndex != (int)(int)TypeNote.Usual)
                    FindVariants(canvas.Children);
                DialogResult = true;
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void tbText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this.tbText.Text == inputplease)
            {
                tbText.Clear();
                tbText.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void cbMinute_DropDownOpened(object sender, EventArgs e)
        {
            if(cbMinute.Items.Contains((string)this.FindResource("stringM")) && isend)
                cbMinute.Items.Remove((string)this.FindResource("stringM"));
        }
        private void cbHour_DropDownOpened(object sender, EventArgs e)
        {
            if (cbHour.Items.Contains((string)this.FindResource("stringH")) && isend)
                cbHour.Items.Remove((string)this.FindResource("stringH"));
        }
        private void cbMinute_DropDownClosed(object sender, EventArgs e)
        {
            if (cbMinute.SelectedItem == null)
            {
                cbMinute.Items.Add((string)this.FindResource("stringM"));
                cbMinute.Text = (string)this.FindResource("stringM");
            }
        }
        private void cbHour_DropDownClosed(object sender, EventArgs e)
        {
            if (cbHour.SelectedItem == null)
            {
                cbHour.Items.Add((string)this.FindResource("stringH"));
                cbHour.Text = (string)this.FindResource("stringH");
            }
        }
        private void cbHour_LostFocus(object sender, RoutedEventArgs e)
        {
            int temptime = -1;
            if (!Int32.TryParse(cbHour.Text, out temptime) || temptime < 0 || temptime > 23)
            {
                cbHour.Text = (string)this.FindResource("stringH");
            }
        }
        private void cbMinute_LostFocus(object sender, RoutedEventArgs e)
        {
            int temptime = -1;
            if (!Int32.TryParse(cbMinute.Text, out temptime) || temptime < 0 || temptime > 59)
            {
                cbMinute.Text = (string)this.FindResource("stringM");
            }
        }

        private void dpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime dt;
            if (!DateTime.TryParse(dpDate.Text, out dt))
            {
                dpDate.SelectedDate = null;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                this.Close();
            }
        }


        private void cbKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            allcheck = false;
            wrIntervalVars = -1;
            int hw = 314;
            int lmb = 177;
            int bm = 200;
            int bcm = 226;

            canvas.Children.Clear();

            int add = 0;
            int more = 0;
            if (cbKindRemind.SelectedIndex == (int)KindRemind.Once)
                more += 30;
            if (cbKindRemind.SelectedIndex == (int)KindRemind.Repeatable)
                more += 50;

            
            if (cbKind.SelectedIndex == (int)(int)TypeNote.Usual)
            {
                btnAllCheck.IsEnabled = false;
                btnAllCheck.Visibility = Visibility.Hidden;
                this.Height = hw + add + more;
                canvas.Height = 0;
            }
            if (cbKind.SelectedIndex == (int)(int)TypeNote.DaysPeriod)
            {
                btnAllCheck.IsEnabled = false;
                btnAllCheck.Visibility = Visibility.Hidden;

                add = 30;
                this.Height = hw + add + more;
                canvas.Height = add;

                Label lbl1 = new Label() { Content = (string)this.FindResource("stringRepeatEveryDay"), Height = 24, Width = 80, Padding = new Thickness(1, 0, 0 ,0) };
                Label lbl2 = new Label() { Content = (string)this.FindResource("stringTh") + " " + (string)this.FindResource("stringDay"), Height = 24, Width = 60, Padding = new Thickness(1, 0, 0, 0) };
                TextBox tb = new TextBox() { Text = "1", Width = 40, Height = 22 };

                Canvas.SetTop(lbl1, 5);
                Canvas.SetLeft(lbl1, 10);
                Canvas.SetTop(tb, 5);
                Canvas.SetLeft(tb, 110);
                Canvas.SetTop(lbl2, 5);
                Canvas.SetLeft(lbl2, 150);

                canvas.Children.Add(lbl1);
                canvas.Children.Add(tb);
                canvas.Children.Add(lbl2);
            }
            if (cbKind.SelectedIndex == (int)(int)TypeNote.WeekPeriod)
            {
                btnAllCheck.IsEnabled = true;
                btnAllCheck.Visibility = Visibility.Visible;
                btnAllCheck.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#178f45"));
                btnAllCheck.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a1ffc5"));

                add = 130;
                this.Height = hw + add + more;
                canvas.Height = add;

                Label lbl1 = new Label() { Content = (string)this.FindResource("stringChooseNecessaryDays") + ":", Height = 24, Width = 180, Padding = new Thickness(1, 0, 0, 0) };
                Canvas.SetTop(lbl1, 5);
                Canvas.SetLeft(lbl1, 10);
                canvas.Children.Add(lbl1);
                double h = 30, w = 15;
                for (int i = 0; i < 7; i++)
                {
                    CheckBox chb = new CheckBox() { Content = WeekDays[i], Height = 22, Width = 110, Padding = new Thickness(1, -2, 0, 0) };
                    Canvas.SetTop(chb, h);
                    Canvas.SetLeft(chb, w);
                    canvas.Children.Add(chb);
                    if (i == 4)
                    {
                        h = 10;
                        w += 120;
                    }
                    h += 20;
                }
            }
            if (cbKind.SelectedIndex == (int)(int)TypeNote.MonthPeriod)
            {
                btnAllCheck.IsEnabled = true;
                btnAllCheck.Visibility = Visibility.Visible;
                btnAllCheck.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#178f45"));
                btnAllCheck.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a1ffc5"));

                add = 110;
                this.Height = hw + add + more;
                canvas.Height = add;

                Label lbl1 = new Label() { Content = (string)this.FindResource("stringRepeatEveryMonth"), Height = 24, Width = 130, Padding = new Thickness(1, 0, 0, 0) };
                Label lbl2 = new Label() { Content = (string)this.FindResource("stringThDate"), Height = 24, Width = 210, Padding = new Thickness(1, 0, 0, 0) };
                TextBox tb = new TextBox() { Text = "1", Width = 30, Height = 22 };

                Canvas.SetTop(lbl1, 5);
                Canvas.SetLeft(lbl1, 10);
                Canvas.SetTop(tb, 5);
                Canvas.SetLeft(tb, 70);
                Canvas.SetTop(lbl2, 5);
                Canvas.SetLeft(lbl2, 100);

                canvas.Children.Add(lbl1);
                canvas.Children.Add(tb);
                canvas.Children.Add(lbl2);

                double h = 30, w = 15;
                for (int i = 0; i < 12; i++)
                {
                    CheckBox chb = new CheckBox() { Content = Monthes[i], Height = 22, Width = 110, Padding = new Thickness(1, -2, 0, 0) };
                    Canvas.SetTop(chb, h);
                    Canvas.SetLeft(chb, w);
                    canvas.Children.Add(chb);
                    if (i == 3 || i == 7)
                    {
                        h = 10;
                        w += 100;
                    }
                    h += 20;
                }
            }

            lblKindRemind.Margin = new Thickness(10, bm + add, 0, 0);
            cbKindRemind.Margin = new Thickness(lmb, bm + add, 0, 0);
            canvasrem.Margin = new Thickness(11, bcm + add, 0, 0);
        }
        private void cbKindRemind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            canvasrem.Children.Clear();

            int nh = 314;
            if (cbKind.SelectedIndex == (int)TypeNote.DaysPeriod)
                nh += 30;
            if (cbKind.SelectedIndex == (int)TypeNote.WeekPeriod)
                nh += 130;
            if (cbKind.SelectedIndex == (int)TypeNote.MonthPeriod)
                nh += 110;

            if (cbKindRemind.SelectedIndex == (int)KindRemind.Usual)
            {
                this.Height = nh;
                canvasrem.Height = 0;
            }
            if (cbKindRemind.SelectedIndex == (int)KindRemind.Once)
            {
                byte v = 0;
                this.Height = nh + 30;
                canvasrem.Height = 30;

                if (wrRemindValueBefore % 1440 == 0 && wrRemindValueBefore != 0)
                {
                    wrRemindValueBefore /= 1440;
                    v = 2;
                }
                else if (wrRemindValueBefore % 60 == 0 && wrRemindValueBefore != 0)
                {
                    wrRemindValueBefore /= 60;
                    v = 1;
                }

                Label lbl = new Label() { Content = (string)this.FindResource("stringBefore"), Height = 24, Width = 120, Padding = new Thickness(1, 1, 0, 0) };
                TextBox tb = new TextBox() { Text = wrRemindValueBefore.ToString(), Width = 40, Height = 22 };
                ComboBox cb = new ComboBox() { Width = 80, Height = 24 };
                cb.Items.Add((string)this.FindResource("stringMinutes"));
                cb.Items.Add((string)this.FindResource("stringHours"));
                cb.Items.Add((string)this.FindResource("stringOfDays"));
                cb.SelectedIndex = v;

                Canvas.SetTop(lbl, 6);
                Canvas.SetLeft(lbl, 10);
                Canvas.SetTop(tb, 6);
                Canvas.SetLeft(tb, 110);
                Canvas.SetTop(cb, 5);
                Canvas.SetLeft(cb, 160);

                canvasrem.Children.Add(lbl);
                canvasrem.Children.Add(tb);
                canvasrem.Children.Add(cb);
            }
            if (cbKindRemind.SelectedIndex == (int)KindRemind.Repeatable)
            {
                this.Height = nh + 60;
                canvasrem.Height = 55;

                byte ve = 0, vb = 0;
                if (wrRemindValueEvery % 1440 == 0)
                {
                    wrRemindValueEvery /= 1440;
                    ve = 2;
                }
                else if (wrRemindValueEvery % 60 == 0)
                {
                    wrRemindValueEvery /= 60;
                    ve = 1;
                }
                if (wrRemindValueBefore % 1440 == 0 && wrRemindValueBefore != 0)
                {
                    wrRemindValueBefore /= 1440;
                    vb = 2;
                }
                else if (wrRemindValueBefore % 60 == 0 && wrRemindValueBefore != 0)
                {
                    wrRemindValueBefore /= 60;
                    vb = 1;
                }


                Label lbl1 = new Label() { Content = (string)this.FindResource("stringBefore"), Height = 24, Width = 120, Padding = new Thickness(1, 1, 0, 0) };
                TextBox tb1 = new TextBox() { Text = wrRemindValueBefore.ToString(), Width = 40, Height = 22 };
                ComboBox cb1 = new ComboBox() { Width = 80, Height = 24 };

                Label lbl2 = new Label() { Content = (string)this.FindResource("stringInervalBetweenReminds"), Height = 24, Width = 120, Padding = new Thickness(1, 1, 0, 0) };
                TextBox tb2 = new TextBox() { Text = wrRemindValueEvery == -1 ? "1" : wrRemindValueEvery.ToString(), Width = 40, Height = 22 };
                ComboBox cb2 = new ComboBox() { Width = 80, Height = 24 };

                cb1.Items.Add((string)this.FindResource("stringMinutes"));
                cb1.Items.Add((string)this.FindResource("stringHours"));
                cb1.Items.Add((string)this.FindResource("stringOfDays"));
                cb1.SelectedIndex = vb;

                cb2.Items.Add((string)this.FindResource("stringMinutes"));
                cb2.Items.Add((string)this.FindResource("stringHours"));
                cb2.Items.Add((string)this.FindResource("stringOfDays"));
                cb2.SelectedIndex = ve;

                Canvas.SetTop(lbl1, 6);
                Canvas.SetLeft(lbl1, 10);
                Canvas.SetTop(tb1, 6);
                Canvas.SetLeft(tb1, 110);
                Canvas.SetTop(cb1, 5);
                Canvas.SetLeft(cb1, 160);

                Canvas.SetTop(lbl2, 36);
                Canvas.SetLeft(lbl2, 10);
                Canvas.SetTop(tb2, 36);
                Canvas.SetLeft(tb2, 110);
                Canvas.SetTop(cb2, 35);
                Canvas.SetLeft(cb2, 160);

                canvasrem.Children.Add(lbl1);
                canvasrem.Children.Add(tb1);
                canvasrem.Children.Add(cb1);
                canvasrem.Children.Add(lbl2);
                canvasrem.Children.Add(tb2);
                canvasrem.Children.Add(cb2);
            }
            
        }

        private void grWrGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void btnAllCheck_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in canvas.Children)
            {
                if (item is CheckBox)
                    (item as CheckBox).IsChecked = !allcheck;
            }

            allcheck = !allcheck;

            if (!allcheck)
            {
                btnAllCheck.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#178f45"));
                btnAllCheck.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a1ffc5"));
            }
            else
            {
                btnAllCheck.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#821010"));
                btnAllCheck.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffabab"));
            }
        }
    }
}

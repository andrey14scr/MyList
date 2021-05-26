using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
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
using System.Windows.Threading;

namespace MyList
{
    /// <summary>
    /// Логика взаимодействия для Reminder.xaml
    /// </summary>

    public partial class Reminder : Window
    {
        /*
        public class ListNotesEventArgs
        {
            public Note InnerItem { get;}

            public ListNotesEventArgs(Note o)
            {
                InnerItem = o;
            }
        }
        public class ListNotes<T> : List<T>
        {
            public delegate void ListNotesHandler(object sender, ListNotesEventArgs e);
            public event ListNotesHandler OnAdd;
            public ListNotes(Reminder w)
            {
                CurrentReminder = w;
            }
            public Reminder CurrentReminder { get; private set; }
            public new void Add(T item)
            {
                OnAdd?.Invoke(this, new ListNotesEventArgs(item as Note));
                base.Add(item);
            }
        }
        static void List_OnAdd(object sender, ListNotesEventArgs e)
        {        
            (sender as ListNotes<Note>).CurrentReminder.lbReminds.Items.Add(new ListBoxItem() {Content = "(" + e.InnerItem.Date.ToString() + ") " + e.InnerItem.Message });
        }
        */
        public bool IsFirstRemindExit;
        public List<Note> ListOfRemindNotes; 
        public bool NeedToClose = false;
        public MainWindow MWOwner;

        public void UpdateListBox()
        {
            lbReminds.Items.Clear();
            ListOfRemindNotes.Sort( (x, y) =>
                {
                    if (x.Date > y.Date)
                        return 1;
                    else if (x.Date < y.Date)
                        return -1;
                    else
                        return 0;
                });
            int k = 0;
            foreach (var item in ListOfRemindNotes)
            {
                ListBoxItem lbi = new ListBoxItem() { Content = "(" /*+ item.Date.ToShortDateString() + " "*/ + item.Date.ToShortTimeString() + ")  " + item.Message };
                if (item.Type != TypeNote.Usual)
                {
                    lbi.Content += "  (p)";
                }
                if (item.RemindEvery != -1)
                {
                    string temp = "";
                    if (item.RemindEvery % 1440 == 0)
                    {
                        temp = (item.RemindEvery / 1440).ToString() + " " + (string)this.FindResource("stringd");
                    }
                    else if (item.RemindEvery % 60 == 0)
                    {
                        temp = (item.RemindEvery / 60).ToString() + " " + (string)this.FindResource("stringh");
                    }
                    else
                    {
                        temp = item.RemindEvery.ToString() + " " + (string)this.FindResource("stringmin");
                    }
                    lbi.Content += "(" + temp + ")";
                }
                lbi.MouseDoubleClick += ListBoxItemDoubleClick;
                if (k % 2 == 1)
                    lbi.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f5f5f5"));
                lbReminds.Items.Add(lbi);
                k++;
            }
            cbKind.SelectedIndex = (int)TypeNote.Usual;
        }

        private void DeleteItem(int id)
        {
            lbReminds.Items.RemoveAt(id);
            ListOfRemindNotes.RemoveAt(id);
        }

        public Reminder()
        {
            GlobalClass.SetLang(this);
            InitializeComponent();

            ListOfRemindNotes = new List<Note>();
            //ListOfRemindNotes = new ListNotes<Note>(this);
            //ListOfRemindNotes.OnAdd += List_OnAdd;

            using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList"))
            {
                RegistryKey nKey;
                if (Key == null)
                    nKey = Registry.CurrentUser.CreateSubKey(@"Software\MyList");
                else
                    nKey = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true);

                if(Key.GetValue(Properties.Resources.regIsFirstRemindExit) == null)
                    nKey.SetValue(Properties.Resources.regIsFirstRemindExit, true);
                IsFirstRemindExit = Boolean.Parse(Key.GetValue(Properties.Resources.regIsFirstRemindExit).ToString());

                nKey.Close();
            }
        }

        private void lbReminds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbReminds.SelectedIndex != -1)
            {
                if (ListOfRemindNotes[lbReminds.SelectedIndex].RemindEvery != -1 || ListOfRemindNotes[lbReminds.SelectedIndex].Type != TypeNote.Usual)
                    btnStop.IsEnabled = true;
                else
                    btnStop.IsEnabled = false;
                btnClose.IsEnabled = true;
                btnComplete.IsEnabled = true;
                btnPutOff.IsEnabled = true;
                tbAmount.IsEnabled = true;
                cbKind.IsEnabled = true;
            }
            else
            {
                btnStop.IsEnabled = false;
                btnPutOff.IsEnabled = false;
                btnClose.IsEnabled = false;
                btnComplete.IsEnabled = false;
                tbAmount.IsEnabled = false;
                cbKind.IsEnabled = false;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (lbReminds.Items.Count == 0)
            {
                ClearListBox();
                this.WindowState = WindowState.Minimized;
                this.Hide();
            }
            if(!NeedToClose)
                e.Cancel = true;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            for (int i = lbReminds.Items.Count-1; i >= 0; i--)
            {
                if ((lbReminds.Items[i] as ListBoxItem).IsSelected)
                {
                    DeleteItem(i);
                }
            }
            btnClose.IsEnabled = false;
            btnComplete.IsEnabled = false;
            btnPutOff.IsEnabled = false;
            tbAmount.IsEnabled = false;
            cbKind.IsEnabled = false;
            if (lbReminds.Items.Count == 0)
            {
                this.Close();
            }
            MWOwner.BeginWaiting();
        }

        private void btnCloseAll_Click(object sender, RoutedEventArgs e)
        {
            ClearListBox();
            this.Close();
        }

        private void btnComplete_Click(object sender, RoutedEventArgs e)
        {
            for (int i = lbReminds.Items.Count - 1; i >= 0; i--)
            {
                if ((lbReminds.Items[i] as ListBoxItem).IsSelected)
                {
                    string sqlExpression = "UPDATE TableOfNotes SET IsDoneNote=@IsDoneNote WHERE Id=" + ListOfRemindNotes[i].Id.ToString();
                    using (SqlConnection connection = new SqlConnection(MWOwner.MainConnectionString))
                    {
                        try
                        {
                            connection.Open();
                            using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                            {
                                SqlParameter ParamIsDoneNote = new SqlParameter("@IsDoneNote", true);
                                command.Parameters.Add(ParamIsDoneNote);
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (SqlException ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }

                    MWOwner.SelectDataForDay(DateTime.Now);
                    DeleteItem(i);
                    break;
                }
            }
            if (lbReminds.Items.Count == 0)
            {
                this.Close();
            }
            btnClose.IsEnabled = false;
            btnPutOff.IsEnabled = false;
            btnComplete.IsEnabled = false;
            tbAmount.IsEnabled = false;
            cbKind.IsEnabled = false;
            MWOwner.BeginWaiting();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if (lbReminds.Items.Count > 0 && IsFirstRemindExit)
            {
                if (MWOwner.ShowDialogWindow(this, "Вы уверены, что хотите выйти?\nВсе напоминания будут закрыты.", (string)FindResource("stringYes"), (string)FindResource("stringNo"), true, out IsFirstRemindExit) == true)
                {
                    ClearListBox();
                    this.WindowState = WindowState.Minimized;
                    this.Hide();
                }
                using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true))
                {
                    RegistryKey nKey;
                    if (Key == null)
                        nKey = Registry.CurrentUser.CreateSubKey(@"Software\MyList");
                    else
                        nKey = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true);

                    nKey.SetValue(Properties.Resources.regIsFirstRemindExit, IsFirstRemindExit);
                    nKey.Close();
                }
            }
            else
            {
                ClearListBox();
                this.WindowState = WindowState.Minimized;
                this.Hide();
            }
        }

        private void ClearListBox()
        {
            ListOfRemindNotes.Clear();
            lbReminds.Items.Clear();
            btnClose.IsEnabled = false;
            btnComplete.IsEnabled = false;
            btnPutOff.IsEnabled = false;
            tbAmount.IsEnabled = false;
            cbKind.IsEnabled = false;
            MWOwner.BeginWaiting();
        }

        private void btnPutOff_Click(object sender, RoutedEventArgs e)
        {
            int temp = 0;
            if (!Int32.TryParse(tbAmount.Text, out temp) || temp <= 0 || temp > 1000)
            {
                MessageBox.Show("Время должно быть целым положительным числом, не превосходящим 1000!");
                return;
            }
            if (cbKind.SelectedIndex == (int)KindTime.Hour)
                temp *= 60;
            if (cbKind.SelectedIndex == (int)KindTime.Day)
                temp *= 1440;

            DateTime nextdt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
            nextdt = nextdt.AddMinutes(temp);

            GlobalClass.SetRemindDate(nextdt, ListOfRemindNotes[lbReminds.SelectedIndex].Id);
            DeleteItem(lbReminds.SelectedIndex);
            if (lbReminds.Items.Count == 0)
            {
                this.Close();
            }
            MWOwner.BeginWaiting();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < lbReminds.Items.Count; i++)
            {
                if ((lbReminds.Items[i] as ListBoxItem).IsSelected)
                {
                    if(DateTime.Now.TimeOfDay >= ListOfRemindNotes[i].Date.TimeOfDay)
                        ListOfRemindNotes[i].SetStopDate(DateTime.Now.Date);
                    DeleteItem(i);
                    btnStop.IsEnabled = false;
                    break;
                }
            }
            if (lbReminds.Items.Count == 0)
            {
                this.Close();
            }
        }

        private void ListBoxItemDoubleClick(object sender, EventArgs e)
        {
            Note item = ListOfRemindNotes[lbReminds.SelectedIndex];

            Writer wr = new Writer(this) { IsEdit = true };
            wr.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            wr.tbText.Text = item.Message;
            wr.tbText.Foreground = new SolidColorBrush(Colors.Black);
            wr.dpDate.SelectedDate = item.Date;

            if (item.Date.Second != 1)
            {
                wr.cbHour.SelectedItem = String.Format("{0:d2}", item.Date.Hour);
                wr.cbMinute.SelectedItem = String.Format("{0:d2}", item.Date.Minute);
            }

            wr.wrRemindValueBefore = item.RemindBefore;
            wr.wrRemindValueEvery = item.RemindEvery;

            int vars = item.IntervalVars;
            bool[] bools = GlobalClass.IntToBitArray(vars);
            
            wr.cbKind.SelectedIndex = (int)item.Type;
            switch (item.Type)
            {
                case TypeNote.DaysPeriod:
                    (wr.canvas.Children[1] as TextBox).Text = item.IntervalValue;
                    break;
                case TypeNote.WeekPeriod:
                    int k = 0;
                    foreach (var chb in wr.canvas.Children)
                    {
                        if (chb is CheckBox)
                        {
                            if (k != 6)
                                (chb as CheckBox).IsChecked = bools[k + 1];
                            else
                                (chb as CheckBox).IsChecked = bools[0];
                            k++;
                        }
                    }
                    break;
                case TypeNote.MonthPeriod:
                    (wr.canvas.Children[1] as TextBox).Text = item.IntervalValue;
                    int l = 0;
                    foreach (var chb in wr.canvas.Children)
                    {
                        if (chb is CheckBox)
                        {
                            (chb as CheckBox).IsChecked = bools[l+1];
                            l++;
                        }
                    }
                    break;
                default:
                    break;
            }

            wr.Kind = (int)item.KindRemind;
            wr.cbKindRemind.SelectedIndex = (int)item.KindRemind;

            if ((bool)wr.ShowDialog())
            {
                Note NewNote = MWOwner.MakeNewNote(wr, item.Id);
                MWOwner.UpdateDB(item.Id, NewNote);
                ListOfRemindNotes[lbReminds.SelectedIndex] = NewNote;
                UpdateListBox();
                MWOwner.SelectDataForDay(GlobalClass.CurrentDateDay);
            }
            wr.Close();
        }
    }
}

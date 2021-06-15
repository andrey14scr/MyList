using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Win32;

using System.Windows.Media.Animation;
using System.Windows.Media;

namespace MyList
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (!GlobalClass.IsRunning)
            {
                string musicpath = "ы";

                LogPath = GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + "Log.txt";
                DBPath = GlobalClass.FindDBPath();
                MainConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + DBPath + ";Integrated Security=True";
                this.MinHeight = constHeightWindow;
                this.MinWidth = constWidthWindow;
                InitializeComponent();

                /*
                using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run\", true))
                {
                    if (Key != null)
                    {
                        Key.SetValue("myList", "\"" + Assembly.GetExecutingAssembly().Location + "\"" + " " + GlobalClass.AutoTurn);
                    }
                    else
                    {
                        RegistryKey nKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                        nKey.SetValue("myList", Assembly.GetExecutingAssembly().Location);
                    }
                }
                */

                using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList"))
                {
                    RegistryKey nKey = FindRegistry(Key); ;

                    UpdateBoolFromRegKey(nKey, Properties.Resources.regIsFirstDelete, IsFirstDelete, out IsFirstDelete);
                    UpdateBoolFromRegKey(nKey, Properties.Resources.regIsFocusText, IsFocusText, out IsFocusText);
                    UpdateBoolFromRegKey(nKey, Properties.Resources.regIsFirstMain, IsFirstMain, out IsFirstMain);
                    UpdateBoolFromRegKey(nKey, Properties.Resources.regIsSound, GlobalClass.IsSound, out GlobalClass.IsSound);

                    if (nKey.GetValue(Properties.Resources.regPastWidth) == null)
                        nKey.SetValue(Properties.Resources.regPastWidth, constWidthWindow.ToString());
                    this.Width = Double.Parse(nKey.GetValue(Properties.Resources.regPastWidth).ToString());

                    if (nKey.GetValue(Properties.Resources.regPastHeight) == null)
                        nKey.SetValue(Properties.Resources.regPastHeight, constHeightWindow.ToString());
                    this.Height = Double.Parse(nKey.GetValue(Properties.Resources.regPastHeight).ToString());

                    if (nKey.GetValue(Properties.Resources.regLanguage) == null)
                        nKey.SetValue(Properties.Resources.regLanguage, "Eng");
                    switch (nKey.GetValue(Properties.Resources.regLanguage).ToString())
                    {
                        case "Eng":
                            GlobalClass.CurrentLanguage = MyList.Language.English;
                            break;
                        case "Rus":
                            GlobalClass.CurrentLanguage = MyList.Language.Russain;
                            break;
                        default:
                            GlobalClass.CurrentLanguage = MyList.Language.English;
                            break;
                    }

                    if (nKey.GetValue(Properties.Resources.regMusicFile) == null)
                        nKey.SetValue(Properties.Resources.regMusicFile, "BellsSound.wav");
                    musicpath = nKey.GetValue(Properties.Resources.regMusicFile).ToString();

                    nKey.Close();
                }

                LastID = FindNextId();

                GlobalClass.SetLang(this);
                ShortWeekDays = new List<string>() { (string)this.FindResource("stringSun"), (string)this.FindResource("stringMon"), (string)this.FindResource("stringTue"), (string)this.FindResource("stringWed"), (string)this.FindResource("stringThu"), (string)this.FindResource("stringFri"), (string)this.FindResource("stringSat") };
                GlobalClass.CurrentDateDay = DateTime.Now.Date;
                SelectDataForDay(GlobalClass.CurrentDateDay);
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.myNotifyIcon.Icon = MyList.Properties.Resources.note;         //  new System.Drawing.Icon(@"../../Resources/note.ico");
                MainCalendar.Visibility = Visibility.Hidden;
                MainCalendar.SelectedDate = DateTime.Now.Date;
                MainCalendar.SelectedDatesChanged += MainCalendaSelectedDatesChanged;
                MainCanvas.Children.Add(MainCalendar);
                WinReminder = new Reminder() { MWOwner = this };
                WinReminder.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                BeginWaiting();

                if (GlobalClass.Flag == GlobalClass.Flags.AutoTurn)
                {
                    this.Close();
                    GlobalClass.Flag = "";
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                }

                SetMusic(musicpath);
                if (!Directory.Exists(GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appMusFolderName))
                    Directory.CreateDirectory(GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appMusFolderName);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            HideList();
            CurrentPanel = null;
            CheckSizes();
        }

        private void TrayDoubleClick(object sender, EventArgs e)
        {
            OpenFromTray();
        }
        private void MenuItemOpenClick(object sender, EventArgs e)
        {
            OpenFromTray();
        }
        private void MenuItemExitClick(object sender, EventArgs e)
        {
            IsClose = true;
            this.Close();
        }


        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            MainCalendar.Visibility = Visibility.Hidden;
            HideList();
            CurrentPanel = null;
            Writer wr = new Writer(this, false);
            if (IsFocusText)
                wr.tbText.Focus();
            wr.dpDate.SelectedDate = GlobalClass.CurrentDateDay;
            wr.cbHour.SelectedIndex = DateTime.Now.Hour;
            wr.cbMinute.SelectedIndex = DateTime.Now.Minute;
            wr.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if ((bool)wr.ShowDialog())
            {
                LastID++;
                Note NewNote = MakeNewNote(wr, LastID);
                //AddToJson(1, NewNote);
                AddToDB(NewNote);

                SelectDataForDay(GlobalClass.CurrentDateDay);

                if (IsFirstMain && DebugBoolean)
                {
                    DialogWindow wwr = new DialogWindow(this, "Чтобы редактировать либо удалить\nзаметку, нажмите правой кнопкой мыши\nи выберите соответствующий пункт", true);
                    wwr.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    wwr.btnYes.Content = "Ок";
                    wwr.btnNo.Content = "Отмена";
                    wwr.ShowDialog();
                    if (wwr.IsDontSee && IsFirstMain)
                    {
                        IsFirstMain = false;
                        using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true))
                        {
                            Key.SetValue(Properties.Resources.regIsFirstMain, false);
                        }
                    }
                }
            }
            wr.Close();
            BeginWaiting();
        }
        private void btnRight_Click(object sender, RoutedEventArgs e)
        {
            MainCalendar.Visibility = Visibility.Hidden;
            HideList();
            CurrentPanel = null;
            GlobalClass.CurrentDateDay = GlobalClass.CurrentDateDay.AddDays(1);
            SelectDataForDay(GlobalClass.CurrentDateDay);
        }
        private void btnLeft_Click(object sender, RoutedEventArgs e)
        {
            MainCalendar.Visibility = Visibility.Hidden;
            HideList();
            CurrentPanel = null;
            GlobalClass.CurrentDateDay = GlobalClass.CurrentDateDay.AddDays(-1);
            SelectDataForDay(GlobalClass.CurrentDateDay);
        }


        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            HideList();
            CallWriter(true);
        }
        private void Delete_Click(object sender, EventArgs e)
        {
            if (IsFirstDelete && DebugBoolean)
            {
                DialogWindow dw = new DialogWindow(this, "Вы уверены, что хотите \nудалить эту заметку?", true);
                dw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (!(bool)dw.ShowDialog())
                    return;
                if (dw.chbDontSee.IsChecked == true && IsFirstDelete)
                {
                    IsFirstDelete = false;
                    using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true))
                    {
                        Key.SetValue(Properties.Resources.regIsFirstDelete, false);
                    }
                }
            }
            DeleteNote();

            HideList();
        }
        private void Revoke_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in MainGrid.Children)
            {
                if (CurrentPanel == (item as MessagePanel))
                {
                    ((item as MessagePanel).Children[1] as Button).Content = new Image() { Source = ImgLightCheck }; ;
                    (item as MessagePanel).InnerNote.IsDo = false;
                    (item as MessagePanel).SetUsualColor((item as MessagePanel).InnerNote);
                    (item as MessagePanel).InnerNote.SetIsDone(false);
                }
            }
            HideList();
            CurrentPanel = null;
        }


        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            HideList();
            CurrentPanel = null;
            ChangeMenuState();
            MainCalendar.Visibility = Visibility.Hidden;
        }
        private void lblGoto_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangeMenuState();
            MainCalendar.SelectedDate = GlobalClass.CurrentDateDay;
            MainCalendar.Visibility = Visibility.Visible;
            Canvas.SetLeft(MainCalendar, this.ActualWidth / 2 - MainCalendar.Width / 2 - 10);
            Canvas.SetTop(MainCalendar, 28);
        }
        private void lblToday_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangeMenuState();
            GlobalClass.CurrentDateDay = DateTime.Now.Date;
            SelectDataForDay(GlobalClass.CurrentDateDay);
        }
        private void lblArchive_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangeMenuState();
            if (!IsArchive)
            {
                IsArchive = true;
                lblArchive.Content = (string)this.FindResource("stringHideArchive");
            }
            else
            {
                IsArchive = false;
                lblArchive.Content = (string)this.FindResource("stringShowArchive");
            }
            SelectDataForDay(GlobalClass.CurrentDateDay);
        }
        private void lblToArchive_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangeMenuState();
            string sqlExpression = "UPDATE TableOfNotes SET IsArchiveNote=1 WHERE IsDoneNote = 1";
            using (SqlConnection connection = new SqlConnection(MainConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            SelectDataForDay(GlobalClass.CurrentDateDay);
        }
        private void lblSettings_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangeMenuState();
            Settings stn = new Settings() { Owner = this };
            stn.chbFocus.IsChecked = IsFocusText;
            stn.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if ((bool)stn.ShowDialog())
            {
                if (MainGrid.Children.Count > 0 && (MainGrid.Children[0] is TextBox) && (MainGrid.Children[0] as TextBox).Text == (string)this.FindResource("stringWriteHere"))
                    (MainGrid.Children[0] as TextBox).Text = "";
                GlobalClass.SetLang(this);
                GlobalClass.SetLang(WinReminder);
                Music = null;
                if (stn.MusicName != "" && GlobalClass.IsSound)
                    SetMusic(stn.MusicName);

                ShortWeekDays = new List<string>() { (string)this.FindResource("stringSun"), (string)this.FindResource("stringMon"), (string)this.FindResource("stringTue"), (string)this.FindResource("stringWed"), (string)this.FindResource("stringThu"), (string)this.FindResource("stringFri"), (string)this.FindResource("stringSat") };
                if (!IsArchive)
                    lblArchive.Content = (string)this.FindResource("stringShowArchive");
                else
                    lblArchive.Content = (string)this.FindResource("stringHideArchive");
                if (!IsBookMark)
                    SelectDataForDay(GlobalClass.CurrentDateDay);
                else
                {
                    lblData.Content = (string)this.FindResource("stringBookmark");
                    if ((MainGrid.Children[0] as TextBox).Text == "")
                        (MainGrid.Children[0] as TextBox).Text = (string)this.FindResource("stringWriteHere");
                }

                IsFocusText = (bool)stn.chbFocus.IsChecked;
                using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(Properties.Resources.appRegPath))
                {
                    RegistryKey nKey = FindRegistry(Key);
                    nKey.SetValue(Properties.Resources.regIsFocusText, IsFocusText);
                    nKey.Close();
                }
            }
        }
        private void lblSupport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ChangeMenuState();
            SupportForm sf = new SupportForm(this);
            GlobalClass.SetLang(sf);
            sf.ShowDialog();
            sf.Close();
        }
        private void lblExit_MouseUp(object sender, MouseButtonEventArgs e)
        {
            IsClose = true;
            this.Close();
        }

        private void lblMenuItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender != lblExit)
                (sender as Label).Background = System.Windows.Media.Brushes.LightBlue;
            else
                (sender as Label).Background = new SolidColorBrush(Color.FromRgb(255, 170, 170));
        }
        private void lblMenuItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender != lblExit)
                (sender as Label).Background = System.Windows.Media.Brushes.Transparent;
            else
                (sender as Label).Background = new SolidColorBrush(Color.FromRgb(255, 130, 130));
        }


        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            HideList();
            CurrentPanel = null;
            if (!lblData.IsMouseOver && !lblGoto.IsMouseOver)
                MainCalendar.Visibility = Visibility.Hidden;
        }
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool misclick = true;
            foreach (var item in MainGrid.Children)
            {
                if ((item as MessagePanel).IsMouseOver)
                {
                    misclick = false;
                    break;
                }
            }
            if (misclick)
            {
                HideList();
                CurrentPanel = null;
            }
            if (!lblData.IsMouseOver)
                MainCalendar.Visibility = Visibility.Hidden;
        }
        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            HideList();
            CurrentPanel = null;
            if (imgBookMark.IsMouseOver || IsBookMark)
                return;
            foreach (var item in MainGrid.Children)
            {
                if ((item as MessagePanel).IsMouseOver)
                    CurrentPanel = (item as MessagePanel);
            }
            CallWriter(true);
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            HideList();
            CurrentPanel = null;
            //MessageBox.Show("Нажато " + e.Key.ToString());

            if (!IsBookMark)
            {
                if (e.Key == Key.Left)
                {
                    GlobalClass.CurrentDateDay = GlobalClass.CurrentDateDay.AddDays(-1);
                    SelectDataForDay(GlobalClass.CurrentDateDay);
                }
                if (e.Key == Key.Right)
                {
                    GlobalClass.CurrentDateDay = GlobalClass.CurrentDateDay.AddDays(1);
                    SelectDataForDay(GlobalClass.CurrentDateDay);
                }
            }
            else
            {
                if (e.Key == Key.Escape)
                {
                    CloseBookmark();
                }
            }
        }


        private void MessagePanel_MouseEnter(object sender, MouseEventArgs e)
        {
            if ((sender as MessagePanel).InnerNote.IsDo)
                (sender as MessagePanel).Background = SelectedCompleteColor;
            else
                (sender as MessagePanel).Background = SelectedColor;
        }
        private void MessagePanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if ((sender as MessagePanel) != CurrentPanel)
            {
                if ((sender as MessagePanel).InnerNote.IsDo)
                    (sender as MessagePanel).Background = CompleteColor;
                else
                    (sender as MessagePanel).SetUsualColor((sender as MessagePanel).InnerNote);
            }
        }
        private void MessagePanel_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            HideList();

            CurrentPanel = (sender as MessagePanel);

            if (CurrentPanel.InnerNote.IsDo)
                CurrentPanel.Background = SelectedCompleteColor;
            else
                CurrentPanel.Background = SelectedColor;

            miEdit.IsEnabled = !CurrentPanel.InnerNote.IsDo;
            miRevoke.IsEnabled = CurrentPanel.InnerNote.IsDo;
            double X = e.GetPosition(null).X;
            if (e.GetPosition(null).X > this.ActualWidth - 121)
                X = e.GetPosition(null).X - MenuBox.Width;
            MenuBox.Margin = new Thickness(X + 1, e.GetPosition(null).Y + 1, 1, 1);
            MenuBox.IsEnabled = true;
            MenuBox.Visibility = Visibility.Visible;
        }


        private void MainCalendaSelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            //MessageBox.Show((sender as Calendar).SelectedDate.ToString());
            GlobalClass.CurrentDateDay = (DateTime)(sender as Calendar).SelectedDate;
            SelectDataForDay(GlobalClass.CurrentDateDay);
            MainCalendar.Visibility = Visibility.Hidden;
        }


        private void MPButton_Click(object sender, RoutedEventArgs e)//
        {
            HideList();
            foreach (var item in MainGrid.Children)
            {
                if ((item as MessagePanel).Children[1] == sender)
                {
                    if (!(item as MessagePanel).InnerNote.IsDo)
                    {
                        ((item as MessagePanel).Children[1] as Button).Content = new Image() { Source = ImgCross };
                        (item as MessagePanel).InnerNote.IsDo = true;
                        (item as MessagePanel).Background = CompleteColor;
                        (item as MessagePanel).InnerNote.SetIsDone(true);
                    }
                    else
                    {
                        if ((item as MessagePanel).InnerNote.IsArchive)
                        {
                            CurrentPanel = (sender as Button).Parent as MessagePanel;
                            DeleteNote();
                            break;
                        }
                        else
                        {
                            (item as MessagePanel).InnerNote.IsArchive = true;
                            UpdateDB((item as MessagePanel).InnerNote.Id, (item as MessagePanel).InnerNote);
                            SelectDataForDay(GlobalClass.CurrentDateDay);
                            CurrentPanel = (item as MessagePanel);
                            break;
                        }
                    }
                }
            }
        }
        private void BtnEnter(object sender, MouseEventArgs e)
        {
            if (((sender as Button).Parent as MessagePanel).InnerNote.IsDo || IsArchive)
                (sender as Button).Content = new Image() { Source = ImgCross };
        }
        private void BtnLeave(object sender, MouseEventArgs e)
        {
            if (((sender as Button).Parent as MessagePanel).InnerNote.IsDo)
                (sender as Button).Content = new Image() { Source = ImgCheck };
            else
                (sender as Button).Content = new Image() { Source = ImgLightCheck };
        }


        private void imgBookMark_MouseEnter(object sender, MouseEventArgs e)
        {
            imgBookMark.Margin = new Thickness(0, 0, 22, 0);
            imgBookMark.Height += 8;
            imgBookMark.Width += 4;
            this.Cursor = Cursors.Hand;
        }
        private void imgBookMark_MouseLeave(object sender, MouseEventArgs e)
        {
            imgBookMark.Margin = new Thickness(0, 0, 24, 0);
            imgBookMark.Height -= 8;
            imgBookMark.Width -= 4;
            this.Cursor = Cursors.Arrow;
        }
        private void imgBookMark_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HideList();
            CurrentPanel = null;
            if (!IsBookMark)
            {
                OpenBookmark();
            }
            else
            {
                CloseBookmark();
            }
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!GlobalClass.IsRunning)
            {
                HideList();
                CurrentPanel = null;
                using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true))
                {
                    RegistryKey nKey;
                    if (Key == null)
                        nKey = Registry.CurrentUser.CreateSubKey(@"Software\MyList");
                    else
                        nKey = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true);

                    nKey.SetValue(Properties.Resources.regPastWidth, this.ActualWidth.ToString());
                    nKey.SetValue(Properties.Resources.regPastHeight, this.ActualHeight.ToString());
                    switch (GlobalClass.CurrentLanguage)
                    {
                        case MyList.Language.English:
                            nKey.SetValue(Properties.Resources.regLanguage, "Eng");
                            break;
                        case MyList.Language.Russain:
                            nKey.SetValue(Properties.Resources.regLanguage, "Rus");
                            break;
                        default:
                            break;
                    }

                    nKey.Close();
                }
                this.Hide();
                this.WindowState = WindowState.Minimized;
                if (!IsClose)
                {
                    e.Cancel = true;
                }
                else
                {
                    WinReminder.NeedToClose = true;
                    WinReminder.Close();

                    this.myNotifyIcon.Dispose();

                    if (WaitingThread != null)
                    {
                        WaitingThread.Abort();
                        WaitingThread.Join();
                    }
                }
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {

        }


        private void lblData_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }
        private void lblData_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }
        private void lblData_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MainCalendar.Visibility == Visibility.Visible)
                MainCalendar.Visibility = Visibility.Hidden;
            else
            {
                HideList();
                CurrentPanel = null;
                MainCalendar.SelectedDate = GlobalClass.CurrentDateDay;
                MainCalendar.Visibility = Visibility.Visible;
                Canvas.SetLeft(MainCalendar, this.ActualWidth / 2 - MainCalendar.Width / 2 - 10);
                Canvas.SetTop(MainCalendar, 28);
            }
        }


        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MainSW_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            HideList();
            CurrentPanel = null;
        }
    }
}

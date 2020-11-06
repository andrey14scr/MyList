using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Data.SqlClient;
using Microsoft.Win32;
using System.Threading;
using System.Windows.Threading;
using System.Media;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Dropbox.Api;
using System.Text;
using Dropbox.Api.Files;

namespace MyList
{
    public partial class MainWindow : Window
    {
        static string mytoken = "sl.AkvCuPOTz9wacOL_zP_3l60MS_qsqFWJ4Hu4V0IF0IRU_1S87VSYAaWcebDsHnNwIZV_zxFlApTRRdlkaLAoEdvFtZOHFVFK_LwhPv0k64BLx5Hz7voFTP9KFLWuGTS541yZvjN11QI";
        static string JsonContent;

        private double CurrentHeight = 0;
        const double constMessagePanelHeight = 32;
        const double constTextSize = 14;
        const double constSizeButton = 30;
        const double constScrollWidth = 15;
        const double constWidthWindow = 350;
        const double constHeightWindow = 450;

        ///<summary>Last ID of notes(for creating a new one)</summary>
        private int LastID = 0;

        ///<summary>Is the first opening of the program</summary>
        public bool IsFirstMain = true;
        ///<summary>Is the first time deleting the note</summary>
        public bool IsFirstDelete = true;
        ///<summary>Is the program closed</summary>
        private bool IsClose = false;
        ///<summary>Is the bookmark opened</summary>
        private bool IsBookMark = false;
        ///<summary>Is the first time loading the program(for afterloaded actions)</summary>
        private bool IsFirst = true;
        ///<summary>Is the archive opened</summary>
        private bool IsArchive = false;
        ///<summary>Is the textbox autofocused after opening</summary>
        public bool IsFocusText = false;

        ///<summary>Is debug mode on</summary>
        private bool DebugBoolean = false;

        ///<summary>Path to database</summary>
        private string DBPath;
        ///<summary>Path to log file</summary>
        public string LogPath;
        ///<summary>Connection string to database</summary>
        public string MainConnectionString;

        ///<summary>Short names of weekdays</summary>
        private List<string> ShortWeekDays;
        ///<summary>IDs that are already in reminder</summary>
        private List<int> PastID = new List<int>();

        ///<summary>Thread that is waiting for reminds and shows them</summary>
        public Thread WaitingThread;
        ///<summary>Last clicked panel</summary>
        private MessagePanel CurrentPanel;
        ///<summary>Today's date</summary>
        public DateTime CurrentDateDay = DateTime.Now.Date;
        ///<summary>Reminder for this window</summary>
        private Reminder WinReminder;
        ///<summary>Navigational calendar in the top of main window</summary>
        Calendar MainCalendar = new Calendar() { Width = 186, Height = 170 };

        ///<summary>Colour for complete notes</summary>
        public static SolidColorBrush CompleteColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#bdffbd"));
        ///<summary>Colour for usual not completed notes</summary>
        public static SolidColorBrush UsualColor = new SolidColorBrush(Colors.White);
        ///<summary>Colour for usual notes that was till now</summary>
        public static SolidColorBrush UsualPastColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dbdbdb"));
        ///<summary>Colour for selection when cursor is over</summary>
        public static SolidColorBrush SelectedColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f5f1a4"));
        ///<summary>Colour for complete notes when cursor is over</summary>
        public static SolidColorBrush SelectedCompleteColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d5ff91"));

        ///<summary>Image of red cross</summary>
        BitmapSource ImgCross = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(MyList.Properties.Resources.Cross.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight((int)constSizeButton, (int)constSizeButton));
        ///<summary>Image of green check mark</summary>
        BitmapSource ImgCheck = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(MyList.Properties.Resources.check.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight((int)constSizeButton, (int)constSizeButton));
        ///<summary>Image of gray check mark</summary>
        BitmapSource ImgLightCheck = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(MyList.Properties.Resources.lightCheck.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight((int)constSizeButton, (int)constSizeButton));

        ///<summary>Player that plays sound while reminding</summary>
        SoundPlayer Music;


        private void AddToJson(int todo, Note note)
        {
            string path = GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appJsonFile + ".json";
            List<(int, Note)> list = new List<(int, Note)>();
            using (StreamReader file = File.OpenText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                list = (List<(int, Note)>)serializer.Deserialize(file, typeof(List<(int, Note)>));
            }

            list.Add((todo, note));

            using (StreamWriter file = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, list);
            }

            using (StreamReader file = File.OpenText(path))
            {
                string content = file.ReadToEnd();
            }
            //var task = Task.Run((Func<Task>)MainWindow.Run);

            File.WriteAllText(path, "");
        }

        static async Task Run()
        {
            using (var dbx = new DropboxClient(mytoken))
            {
                //var full = await dbx.Users.GetCurrentAccountAsync();
                //Console.WriteLine("{0} - {1}", full.Name.DisplayName, full.Email);
                await Upload(dbx, "", Properties.Resources.appJsonFile + ".json", JsonContent);
                //await ListRootFolder(dbx);
                //await Download(dbx, folderName, fileName);
            }
        }
        static async Task Upload(DropboxClient dbx, string folder, string file, string content)
        {
            using (var mem = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var updated = await dbx.Files.UploadAsync(folder + "/" + file, WriteMode.Overwrite.Instance, body: mem);
                //Console.WriteLine("Saved {0}/{1} rev {2}", folder, file, updated.Rev);
            }
        }

        private void LookJson()
        {
            List<(int, Note)> list;
            using (StreamReader file = File.OpenText(GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appGotJsonFile + ".json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                list = (List<(int, Note)>)serializer.Deserialize(file, typeof(List<(int, Note)>));
            }

            foreach (var item in list)
            {
                switch (item.Item1)
                {
                    case 0:
                        UpdateDB(item.Item2.Id, item.Item2);
                        break;
                    case 1:
                        AddToDB(item.Item2);
                        break;
                    case -1:
                        DeleteFromDB(item.Item2.Id);
                        break;
                    default:
                        break;
                }
            }
        }


        private void OpenFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            CurrentDateDay = DateTime.Now.Date;
            SelectDataForDay(CurrentDateDay);
        }

        /// <summary>Update selected bool variable from registry</summary>
        /// <param name="regkey">Registry key</param>
        /// <param name="path">File name of sound</param>
        /// <param name="b">File name of sound</param>
        /// <param name="ob">File name of sound</param>
        private void UpdateBoolFromRegKey(RegistryKey regkey, string path, bool b, out bool ob)
        {
            if (regkey.GetValue(path) == null)
                regkey.SetValue(path, b);
            ob = Boolean.Parse(regkey.GetValue(path).ToString());
        }
      
        /// <summary>Set music for sound player</summary>
        /// <param name="key">The key we want to find</param>
        /// <returns>Registry key(new if this is null)</returns>
        private RegistryKey FindRegistry(RegistryKey key)
        {
            RegistryKey nKey;
            if (key == null)
                nKey = Registry.CurrentUser.CreateSubKey(Properties.Resources.appRegPath);
            else
                nKey = Registry.CurrentUser.OpenSubKey(Properties.Resources.appRegPath, true);
            return nKey;
        }
      
        /// <summary>Check sixes of everything in the <seealso cref="MainWindow"/></summary>
        private void CheckSizes()
        {
            CurrentHeight = 0;
            double w = this.ActualWidth;
            btnAdd.Margin = new Thickness(w / 2 - 80, 7, w / 2 - 80, 7);
            btnRight.Margin = new Thickness(w / 2 + 57, 1, w / 2 - 57 - 34, 0);
            btnLeft.Margin = new Thickness(w / 2 - 52 - 17, 1, w / 2 + 45 - 17, 0);
            lblData.Margin = new Thickness(w / 2 - 49, 0, w / 2 - 73, 0);

            MainGrid.Height = this.ActualHeight - 121;
            MainGrid.Width = w - 16;
            double cur = 6;

            foreach (var item in MainGrid.Children)
            {
                (item as MessagePanel).Width = MainGrid.Width - 24;
                (item as MessagePanel).Margin = new Thickness(5, cur, 0, 0);
                ((item as MessagePanel).Children[0] as TextBlock).Width = MainGrid.Width - 80;
                ((item as MessagePanel).Children[0] as TextBlock).UpdateLayout();

                double tbheight = ((item as MessagePanel).Children[0] as TextBlock).ActualHeight;
                double spheight = (item as MessagePanel).Height;

                if (tbheight > spheight)
                    (item as MessagePanel).Height = tbheight;
                double btnh = ((item as MessagePanel).Children[1] as Button).Height;
                if (tbheight >= constMessagePanelHeight && tbheight < spheight)
                    (item as MessagePanel).Height = tbheight + 2;
                if (tbheight <= constMessagePanelHeight)
                {
                    (item as MessagePanel).Height = constMessagePanelHeight;
                    btnh = 3;
                }
                ((item as MessagePanel).Children[1] as Button).Margin = new Thickness(0, -(((item as MessagePanel).Height + btnh) / 2), 1, 0);
                cur += (item as MessagePanel).Height + 10;
            }
            if (cur > MainGrid.Height)
            {
                MainGrid.Height = cur;
                MainGrid.Margin = new Thickness(0, 6, 0, 0);
            }
            if (cur + 10 < MainGrid.Height && cur > this.ActualHeight - 116)
                MainGrid.Height = cur;
            CurrentHeight = cur;

            Canvas.SetLeft(MainCalendar, this.ActualWidth / 2 - MainCalendar.Width / 2 - 10);
            Canvas.SetTop(MainCalendar, 28);
        }
      
        /// <summary>Hide menu after right mouse click</summary>
        private void HideList()
        {
            MenuBox.Visibility = Visibility.Hidden;
            MenuBox.IsEnabled = false;
            if (CurrentPanel != null && !CurrentPanel.InnerNote.IsDo)
                CurrentPanel.SetUsualColor(CurrentPanel.InnerNote);
        }
     
        /// <summary>Make a new note</summary>
        /// <param name="writer">Writer where the note is creating</param>
        /// <param name="id">Id of a new note</param>
        /// <returns>A new note made in <seealso cref="Writer"/></returns>
        public Note MakeNewNote(Writer writer, int id)
        {
            Note newnote = new Note() { Message = writer.tbText.Text, IsDo = false, Id = id, Type = TypeNote.Usual, IsArchive = false, KindRemind = (KindRemind)writer.Kind };

            newnote.IntervalVars = writer.wrIntervalVars;
            newnote.IntervalValue = writer.wrIntervalValue.ToString();

            DateTime proba;
            bool Th = writer.cbHour.Text.ToString() != (string)this.FindResource("stringH");
            bool D = DateTime.TryParse(writer.dpDate.Text, out proba);

            if (D && !Th)
                newnote.Date = new DateTime(writer.dpDate.SelectedDate.Value.Year, writer.dpDate.SelectedDate.Value.Month, writer.dpDate.SelectedDate.Value.Day, 0, 0, 1);
            if (D && Th)
                newnote.Date = new DateTime(writer.dpDate.SelectedDate.Value.Year, writer.dpDate.SelectedDate.Value.Month, writer.dpDate.SelectedDate.Value.Day, Int32.Parse(writer.cbHour.SelectedItem.ToString()), Int32.Parse(writer.cbMinute.SelectedItem.ToString()), 0);

            switch (writer.cbKind.SelectedIndex)
            {
                case 1:
                    newnote.Type = TypeNote.DaysPeriod;
                    break;
                case 2:
                    newnote.Type = TypeNote.WeekPeriod;
                    break;
                case 3:
                    newnote.Type = TypeNote.MonthPeriod;
                    break;
                default:
                    break;
            }

            newnote.RemindEvery = writer.wrRemindValueEvery;
            newnote.RemindBefore = writer.wrRemindValueBefore;
            return newnote;
        }
       
        /// <summary>Create a new textblock for a note in the <seealso cref="MainWindow"/></summary>
        /// <param name="noteinit">Note the content of which will be showed</param>
        /// <returns>New textblock with information about note</returns>
        private TextBlock MakeNewtextBlock(Note noteinit)
        {
            TextBlock textblock = new TextBlock() { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(1, 0, 0, 0), Width = MainGrid.Width - 80, TextWrapping = TextWrapping.Wrap, FontSize = constTextSize };

            if (noteinit.Date.Second != 1 && noteinit.IntervalValue != "-1")
            {
                Run rTime = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = " " + noteinit.Date.ToShortTimeString() + " " };
                textblock.Inlines.Add(rTime);
                Run rPeriod = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = "(periodic)\n" };
                textblock.Inlines.Add(rPeriod);
            }
            else if (noteinit.Date.Second == 1 && noteinit.IntervalValue != "-1")
            {
                Run rPeriod = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = "(periodic)\n" };
                textblock.Inlines.Add(rPeriod);
            }
            else if (noteinit.Date.Second != 1 && noteinit.IntervalValue == "-1")
            {
                Run rTime = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = " " + noteinit.Date.ToShortTimeString() + "\n" };
                textblock.Inlines.Add(rTime);
            }

            Run rMessage = new Run() { Text = noteinit.Message };
            textblock.Inlines.Add(rMessage);

            return textblock;
        }
      
        /// <summary>Create a new textblock for a note in the <seealso cref="MainWindow"/></summary>
        /// <param name="tbl">New textblock with information about note</param>
        /// <param name="noteinit">Note the content of which will be showed</param>
        private void MakeNewtextBlock(TextBlock tbl, Note noteinit)
        {
            tbl.Inlines.Clear();

            if (noteinit.Date.Second != 1 && noteinit.IntervalValue != "-1")
            {
                Run rTime = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = " " + noteinit.Date.ToShortTimeString() + " " };
                tbl.Inlines.Add(rTime);
                Run rPeriod = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = "(periodic)\n" };
                tbl.Inlines.Add(rPeriod);
            }
            else if (noteinit.Date.Second == 1 && noteinit.IntervalValue != "-1")
            {
                Run rPeriod = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = "(periodic)\n" };
                tbl.Inlines.Add(rPeriod);
            }
            else if (noteinit.Date.Second != 1 && noteinit.IntervalValue == "-1")
            {
                Run rTime = new Run() { FontStyle = FontStyles.Italic, Foreground = new SolidColorBrush(Colors.Blue), Text = " " + noteinit.Date.ToShortTimeString() + "\n" };
                tbl.Inlines.Add(rTime);
            }

            Run rMessage = new Run() { Text = noteinit.Message };
            tbl.Inlines.Add(rMessage);
        }
      
        /// <summary>Draw a new note in the <seealso cref="MainWindow"/></summary>
        /// <param name="newnote">Note the content of which will be showed</param>
        private void DrawNote(Note newnote)
        {
            TextBlock tbl = MakeNewtextBlock(newnote);
            MessagePanel sp = new MessagePanel() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(5, CurrentHeight, 0, 0), Width = MainGrid.Width - 24, Height = constMessagePanelHeight, Background = UsualColor, InnerNote = newnote };
            sp.MouseEnter += MessagePanel_MouseEnter;
            sp.MouseLeave += MessagePanel_MouseLeave;
            sp.MouseRightButtonUp += MessagePanel_MouseRightButtonUp;
            Button btn = new Button() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Height = constSizeButton, Width = constSizeButton, IsTabStop = false };

            if (newnote.IsDo)
            {
                sp.Background = CompleteColor;
                btn.Content = new Image() { Source = ImgCheck };
            }
            else
            {
                sp.SetUsualColor(newnote);
                btn.Content = new Image() { Source = ImgLightCheck };
            }

            btn.Click += MPButton_Click;
            btn.MouseEnter += BtnEnter;
            btn.MouseLeave += BtnLeave;

            sp.Children.Add(tbl);
            sp.Children.Add(btn);
            MainGrid.Children.Add(sp);
            this.UpdateLayout();

            double btnh = 3;
            if (tbl.ActualHeight > sp.Height)
            {
                sp.Height = tbl.ActualHeight;
                btnh = btn.Height;
            }

            CurrentHeight += sp.Height + 10;
            if (CurrentHeight > MainGrid.Height + 5)
                MainGrid.Height = CurrentHeight;
            btn.Margin = new Thickness(1, -((sp.Height + btnh) / 2), 1, 1);
        }
      
        /// <summary>Select all notes that should be showed for the selected day in the <seealso cref="MainWindow"/></summary>
        /// <param name="day">Selected day</param>
        public void SelectDataForDay(DateTime day)
        {
            MainGrid.Children.Clear();
            this.UpdateLayout();
            CurrentHeight = 0;
            lblData.Content = CurrentDateDay.ToShortDateString();
            switch (day.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    lblData.Content += " (" + ShortWeekDays[0] + ")";
                    break;
                case DayOfWeek.Monday:
                    lblData.Content += " (" + ShortWeekDays[1] + ")";
                    break;
                case DayOfWeek.Tuesday:
                    lblData.Content += " (" + ShortWeekDays[2] + ")";
                    break;
                case DayOfWeek.Wednesday:
                    lblData.Content += " (" + ShortWeekDays[3] + ")";
                    break;
                case DayOfWeek.Thursday:
                    lblData.Content += " (" + ShortWeekDays[4] + ")";
                    break;
                case DayOfWeek.Friday:
                    lblData.Content += " (" + ShortWeekDays[5] + ")";
                    break;
                case DayOfWeek.Saturday:
                    lblData.Content += " (" + ShortWeekDays[6] + ")";
                    break;
                default:
                    break;
            }


            string sqlExpression = "SELECT * FROM TableOfNotes WHERE IsArchiveNote = " + (IsArchive ? "1" : "0") + " AND DATEPART(second, DateNote) = 0" + " ORDER BY CAST(DateNote AS TIME) ASC";
            using (SqlConnection connection = new SqlConnection(MainConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    TypeNote type = TypeNote.Usual;
                                    switch ((byte)reader["TypeNote"])
                                    {
                                        case 1:
                                            type = TypeNote.DaysPeriod;
                                            break;
                                        case 2:
                                            type = TypeNote.WeekPeriod;
                                            break;
                                        case 3:
                                            type = TypeNote.MonthPeriod;
                                            break;
                                        default:
                                            break;
                                    }
                                    if (((DateTime)reader["DateNote"]).Date == day.Date && type == TypeNote.Usual)
                                    {
                                        DrawNote(new Note() { Message = reader["MessageNote"].ToString(), Date = (DateTime)reader["DateNote"], IntervalValue = reader["IntervalValueNote"].ToString(), IntervalVars = (int)reader["IntervalVarsNote"], RemindBefore = (int)reader["RemindBeforeNote"], RemindEvery = (int)reader["RemindEveryNote"], IsDo = (bool)reader["IsDoneNote"], Type = type, Id = (int)reader["Id"], KindRemind = (KindRemind)((byte)reader["KindRemindNote"]), IsArchive = (bool)reader["IsArchiveNote"] });
                                    }
                                    else
                                    {
                                        bool isgood = false;
                                        int vars = (int)reader["IntervalVarsNote"];
                                        bool[] bools = GlobalClass.IntToBitArray(vars);
                                        switch (type)
                                        {
                                            case TypeNote.DaysPeriod:
                                                TimeSpan interval = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0) - new DateTime(((DateTime)reader["DateNote"]).Year, ((DateTime)reader["DateNote"]).Month, ((DateTime)reader["DateNote"]).Day, 0, 0, 0);
                                                int betweendays = interval.Days;
                                                if (betweendays % Int32.Parse(reader["IntervalValueNote"].ToString()) == 0 && day.Date >= ((DateTime)reader["DateNote"]).Date)
                                                    isgood = true;
                                                break;
                                            case TypeNote.MonthPeriod:
                                                if (reader["IntervalValueNote"].ToString() == day.Day.ToString() && bools[day.Month] && day.Date >= ((DateTime)reader["DateNote"]).Date)
                                                    isgood = true;
                                                break;
                                            case TypeNote.WeekPeriod:
                                                if (bools[(int)day.DayOfWeek] && day.Date >= ((DateTime)reader["DateNote"]).Date)
                                                    isgood = true;
                                                break;
                                            default:
                                                break;
                                        }
                                        if (isgood)
                                            DrawNote(new Note() { Message = reader["MessageNote"].ToString(), Date = (DateTime)reader["DateNote"], IntervalValue = reader["IntervalValueNote"].ToString(), IntervalVars = (int)reader["IntervalVarsNote"], RemindBefore = (int)reader["RemindBeforeNote"], RemindEvery = (int)reader["RemindEveryNote"], IsDo = (bool)reader["IsDoneNote"], Type = type, Id = (int)reader["Id"], KindRemind = (KindRemind)((byte)reader["KindRemindNote"]), IsArchive = (bool)reader["IsArchiveNote"] });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            sqlExpression = "SELECT * FROM TableOfNotes WHERE IsArchiveNote = " + (IsArchive ? "1" : "0") + " AND DATEPART(second, DateNote) = 1" + " ORDER BY DateNote ASC";
            using (SqlConnection connection = new SqlConnection(MainConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    TypeNote type = TypeNote.Usual;
                                    switch ((byte)reader["TypeNote"])
                                    {
                                        case 1:
                                            type = TypeNote.DaysPeriod;
                                            break;
                                        case 2:
                                            type = TypeNote.WeekPeriod;
                                            break;
                                        case 3:
                                            type = TypeNote.MonthPeriod;
                                            break;
                                        default:
                                            break;
                                    }
                                    if (((DateTime)reader["DateNote"]).Date == day.Date && type == TypeNote.Usual)
                                    {
                                        DrawNote(new Note() { Message = reader["MessageNote"].ToString(), Date = (DateTime)reader["DateNote"], IntervalValue = reader["IntervalValueNote"].ToString(), IntervalVars = (int)reader["IntervalVarsNote"], RemindBefore = (int)reader["RemindBeforeNote"], RemindEvery = (int)reader["RemindEveryNote"], IsDo = (bool)reader["IsDoneNote"], Type = type, Id = (int)reader["Id"], KindRemind = (KindRemind)((byte)reader["KindRemindNote"]), IsArchive = (bool)reader["IsArchiveNote"] });
                                    }
                                    else
                                    {
                                        bool isgood = false;
                                        int vars = (int)reader["IntervalVarsNote"];
                                        bool[] bools = GlobalClass.IntToBitArray(vars);
                                        switch (type)
                                        {
                                            case TypeNote.DaysPeriod:
                                                TimeSpan interval = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0) - new DateTime(((DateTime)reader["DateNote"]).Year, ((DateTime)reader["DateNote"]).Month, ((DateTime)reader["DateNote"]).Day, 0, 0, 0);
                                                int betweendays = interval.Days;
                                                if (betweendays % Int32.Parse(reader["IntervalValueNote"].ToString()) == 0 && day.Date >= ((DateTime)reader["DateNote"]).Date)
                                                    isgood = true;
                                                break;
                                            case TypeNote.MonthPeriod:
                                                if (reader["IntervalValueNote"].ToString() == day.Day.ToString() && bools[day.Month] && day.Date >= ((DateTime)reader["DateNote"]).Date)
                                                    isgood = true;
                                                break;
                                            case TypeNote.WeekPeriod:
                                                if (bools[(int)day.DayOfWeek] && day.Date >= ((DateTime)reader["DateNote"]).Date)
                                                    isgood = true;
                                                break;
                                            default:
                                                break;
                                        }
                                        if (isgood)
                                            DrawNote(new Note() { Message = reader["MessageNote"].ToString(), Date = (DateTime)reader["DateNote"], IntervalValue = reader["IntervalValueNote"].ToString(), IntervalVars = (int)reader["IntervalVarsNote"], RemindBefore = (int)reader["RemindBeforeNote"], RemindEvery = (int)reader["RemindEveryNote"], IsDo = (bool)reader["IsDoneNote"], Type = type, Id = (int)reader["Id"], KindRemind = (KindRemind)((byte)reader["KindRemindNote"]), IsArchive = (bool)reader["IsArchiveNote"] });
                                    }
                                }
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (!IsFirst)
                CheckSizes();
            else
                IsFirst = false;
        }
        
        /// <summary>Find the nearest notes to remind about them in the <seealso cref="Reminder"/></summary>
        /// <returns>Pair of time for waiting and list of notes</returns>
        private (double, List<Note>) FindNearest()
        {
            List<(double, Note)> tempanswer = new List<(double, Note)>();

            string sqlExpression = "SELECT * FROM TableOfNotes WHERE IsDoneNote = 0 AND KindRemindNote != 0 AND IsArchiveNote = 0";
            using (SqlConnection connection = new SqlConnection(MainConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                DateTime MainNowTime = DateTime.Now;
                                while (reader.Read())
                                {
                                    #region GetFata
                                    int id = (Int32)reader["Id"];

                                    if (reader["StopDateNote"] as DateTime? != null)  //checking is stop for today
                                    {
                                        if (((DateTime)reader["StopDateNote"]).Date == MainNowTime.Date)
                                            continue;
                                        else if (((DateTime)reader["StopDateNote"]).Date < MainNowTime.Date)
                                            GlobalClass.SetStopDate(null, id);
                                    }

                                    byte kind = (byte)reader["KindRemindNote"];
                                    int period = Int32.Parse(reader["RemindEveryNote"].ToString());
                                    int before = Int32.Parse(reader["RemindBeforeNote"].ToString());
                                    DateTime dateofnote = (DateTime)reader["DateNote"];
                                    DateTime remindtime = dateofnote.AddMinutes(-before);
                                    string mes = reader["MessageNote"].ToString();

                                    TypeNote type = TypeNote.Usual;
                                    switch ((byte)reader["TypeNote"])
                                    {
                                        case 1:
                                            type = TypeNote.DaysPeriod;
                                            break;
                                        case 2:
                                            type = TypeNote.WeekPeriod;
                                            break;
                                        case 3:
                                            type = TypeNote.MonthPeriod;
                                            break;
                                        default:
                                            break;
                                    }
                                    #endregion

                                    if (period != -1)  //till needed TIME (not day)
                                    {
                                        if (type == TypeNote.Usual)
                                        {
                                            if (remindtime.Date == MainNowTime.Date)
                                                while (remindtime < MainNowTime)
                                                    remindtime = remindtime.AddMinutes(period);
                                            else
                                                continue;
                                        }
                                        else if (MainNowTime.TimeOfDay >= remindtime.TimeOfDay)
                                        {
                                            while (remindtime < MainNowTime)
                                                remindtime = remindtime.AddMinutes(period);
                                        }
                                    }

                                    bool isgood = true;
                                    int intervalvars = (int)reader["IntervalVarsNote"];
                                    string intervalvalue = (reader["IntervalValueNote"]).ToString();
                                    bool[] bools = GlobalClass.IntToBitArray(intervalvars);
                                    switch (type)  //checking of validate date for every day/weekday/month reminds
                                    {
                                        case TypeNote.DaysPeriod:
                                            int betweendays = (new DateTime(MainNowTime.Year, MainNowTime.Month, MainNowTime.Day, 0, 0, 0) - new DateTime(dateofnote.Year, dateofnote.Month, dateofnote.Day, 0, 0, 0)).Days;
                                            if (!((double)betweendays % Double.Parse(intervalvalue) == 0))
                                                isgood = false;
                                            else
                                                remindtime = new DateTime(MainNowTime.Year, MainNowTime.Month, MainNowTime.Day, remindtime.Hour, remindtime.Minute, 0);
                                            break;

                                        case TypeNote.MonthPeriod:
                                            if (!(intervalvalue == MainNowTime.Day.ToString() && bools[MainNowTime.Month]))
                                                isgood = false;
                                            else
                                                remindtime = new DateTime(MainNowTime.Year, MainNowTime.Month, MainNowTime.Day, remindtime.Hour, remindtime.Minute, 0);
                                            break;

                                        case TypeNote.WeekPeriod:
                                            if (!bools[(int)MainNowTime.DayOfWeek])
                                                isgood = false;
                                            else
                                                remindtime = new DateTime(MainNowTime.Year, MainNowTime.Month, MainNowTime.Day, remindtime.Hour, remindtime.Minute, 0);
                                            break;

                                        default:
                                            break;
                                    }
                                    if (!isgood)
                                        continue;

                                    if (reader["RemindDateNote"] as DateTime? != null)
                                    {
                                        TimeSpan nextinterval = (DateTime)reader["RemindDateNote"] - MainNowTime;
                                        double sec = nextinterval.TotalSeconds;
                                        if (sec < 0)
                                            sec = 0;
                                        tempanswer.Add((sec, new Note() { Message = reader["MessageNote"].ToString(), Date = dateofnote, IntervalValue = reader["IntervalValueNote"].ToString(), IntervalVars = intervalvars, RemindEvery = period, RemindBefore = before, IsDo = (bool)reader["IsDoneNote"], Type = type, Id = id, KindRemind = (KindRemind)kind }));
                                        continue;
                                    }

                                    double res = 0;

                                    TimeSpan interval = remindtime - MainNowTime;  // remindtime is a date+time now
                                    res = interval.TotalSeconds;

                                    if (res < 0 && !PastID.Exists(x => x == id))
                                    {
                                        res = 0;
                                        PastID.Add(id);
                                    }
                                    else if (res < 0)
                                    {
                                        continue;
                                    }

                                    if (WinReminder.ListOfRemindNotes.Find(x => x.Id.ToString() == id.ToString()) == null)
                                        tempanswer.Add((res, new Note() { Message = reader["MessageNote"].ToString(), Date = dateofnote, IntervalValue = reader["IntervalValueNote"].ToString(), IntervalVars = intervalvars, RemindEvery = period, RemindBefore = before, IsDo = (bool)reader["IsDoneNote"], Type = type, Id = id, KindRemind = (KindRemind)kind }));
                                }
                            }
                        }
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (tempanswer.Count > 0)
            {
                tempanswer.Sort((a, b) =>
                {
                    if (a.Item1 < b.Item1)
                        return -1;
                    else if (a.Item1 > b.Item1)
                        return 1;
                    else
                        return 0;
                });
                List<Note> answer = new List<Note>();
                answer.Add(tempanswer[0].Item2);
                int i = 1;
                while (tempanswer.Count > i && tempanswer[i].Item1 == tempanswer[0].Item1)
                {
                    answer.Add(tempanswer[i].Item2);
                    i++;
                }
                return (tempanswer[0].Item1, answer);
            }
            else
                return (-1, null);

        }
        
        /// <summary>Start the thread <seealso cref="WaitingThread"/> that waits to remind about notes in the <seealso cref="Reminder"/></summary>
        public void BeginWaiting()
        {
            //return;
            if (WaitingThread != null)
            {
                WaitingThread.Abort();
                WaitingThread.Join();
            }

            WaitingThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    (double, List<Note>) putting = FindNearest();
                    //myNotifyIcon.ShowBalloonTip("Извещение", putting.Item1.ToString(), BalloonIcon.Info);
                    if (putting.Item1 < 0)
                    {
                        using (StreamWriter sw = new StreamWriter(GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + "Log.txt", true, System.Text.Encoding.Default))
                        {
                            sw.WriteLine(DateTime.Now.ToString() + ": CLOSED");
                        }
                        break;
                    }
                    using (StreamWriter sw = new StreamWriter(GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + "Log.txt", true, System.Text.Encoding.Default))
                    {
                        sw.WriteLine(DateTime.Now.ToString() + ": will wait " + putting.Item1.ToString() + " for:");
                        foreach (var item in putting.Item2)
                        {
                            sw.Write(item.Id.ToString() + " ");
                        }
                        sw.WriteLine("\n------\n");
                    }
                    Thread.Sleep((int)(putting.Item1 * 1000));
                    if (putting.Item2.Count > 0)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            foreach (var item in putting.Item2)
                            {
                                GlobalClass.SetRemindDate(null, item.Id);
                                WinReminder.ListOfRemindNotes.Add(item);
                            }
                            WinReminder.UpdateListBox();
                            WinReminder.Show();
                            WinReminder.WindowState = WindowState.Normal;
                            if (GlobalClass.IsSound && Music != null)
                                Music.Play();
                        });
                    }
                }
            }
            ))
            { IsBackground = true };

            WaitingThread.SetApartmentState(ApartmentState.STA);
            WaitingThread.Start();
        }
        
        /// <summary>Open a <seealso cref="Writer"/> to change some information about it</summary>
        private void CallWriter()
        {
            foreach (var item in MainGrid.Children)
            {
                if ((item as MessagePanel) == CurrentPanel)
                {
                    Writer wr = new Writer(this) { IsEdit = true };
                    wr.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                    wr.tbText.Text = (item as MessagePanel).InnerNote.Message;
                    wr.tbText.Foreground = new SolidColorBrush(Colors.Black);
                    wr.dpDate.SelectedDate = (item as MessagePanel).InnerNote.Date;

                    if ((item as MessagePanel).InnerNote.Date.Second != 1)
                    {
                        wr.cbHour.SelectedItem = String.Format("{0:d2}", (item as MessagePanel).InnerNote.Date.Hour);
                        wr.cbMinute.SelectedItem = String.Format("{0:d2}", (item as MessagePanel).InnerNote.Date.Minute);
                    }

                    wr.wrRemindValueBefore = (item as MessagePanel).InnerNote.RemindBefore;
                    wr.wrRemindValueEvery = (item as MessagePanel).InnerNote.RemindEvery;

                    int vars = (item as MessagePanel).InnerNote.IntervalVars;
                    bool[] bools = GlobalClass.IntToBitArray(vars);

                    wr.cbKind.SelectedIndex = (int)(item as MessagePanel).InnerNote.Type;
                    switch ((item as MessagePanel).InnerNote.Type)
                    {
                        case TypeNote.DaysPeriod:
                            (wr.canvas.Children[1] as TextBox).Text = (item as MessagePanel).InnerNote.IntervalValue;
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
                            (wr.canvas.Children[1] as TextBox).Text = (item as MessagePanel).InnerNote.IntervalValue;
                            int l = 0;
                            foreach (var chb in wr.canvas.Children)
                            {
                                if (chb is CheckBox)
                                {
                                    (chb as CheckBox).IsChecked = bools[l + 1];
                                    l++;
                                }
                            }
                            break;
                        default:
                            break;
                    }

                    wr.Kind = (int)(item as MessagePanel).InnerNote.KindRemind;
                    wr.cbKindRemind.SelectedIndex = (int)(item as MessagePanel).InnerNote.KindRemind;

                    if ((bool)wr.ShowDialog())
                    {
                        for (int i = WinReminder.lbReminds.Items.Count - 1; i >= 0; i--)
                        {
                            if (WinReminder.ListOfRemindNotes.Contains((item as MessagePanel).InnerNote))
                            {
                                WinReminder.ListOfRemindNotes.RemoveAt(i);
                                WinReminder.lbReminds.Items.RemoveAt(i);
                                break;
                            }
                        }
                        Note NewNote = MakeNewNote(wr, (item as MessagePanel).InnerNote.Id);
                        //AddToJson(0, NewNote);
                        UpdateDB((item as MessagePanel).InnerNote.Id, NewNote);
                        (item as MessagePanel).InnerNote = NewNote;
                        MakeNewtextBlock((item as MessagePanel).Children[0] as TextBlock, NewNote);
                    }
                    wr.Close();
                    break;
                }
            }
            CheckSizes();
            CurrentPanel = null;
            HideList();
            BeginWaiting();
        }
        
        /// <summary>Delete all notes from the <seealso cref="MainWindow"/> and the <seealso cref="Reminder"/></summary>
        private void DeleteNote()
        {
            foreach (var item in MainGrid.Children)
            {
                if (CurrentPanel == (item as MessagePanel))
                {
                    //AddToJson(-1, (item as MessagePanel).InnerNote);
                    DeleteFromDB((item as MessagePanel).InnerNote.Id);
                    for (int i = WinReminder.lbReminds.Items.Count - 1; i >= 0; i--)
                    {
                        if (WinReminder.ListOfRemindNotes.Contains((item as MessagePanel).InnerNote))
                        {
                            WinReminder.ListOfRemindNotes.RemoveAt(i);
                            WinReminder.lbReminds.Items.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            SelectDataForDay(CurrentDateDay);

            CurrentPanel = null;
            BeginWaiting();
        }
        
        /// <summary>Close opened bookmark in the <seealso cref="MainWindow"/></summary>
        private void CloseBookmark()
        {
            this.ResizeMode = ResizeMode.CanResize;
            lblData.MouseUp += lblData_MouseUp;
            lblData.MouseEnter += lblData_MouseEnter;
            lblData.MouseLeave += lblData_MouseLeave;

            if ((MainGrid.Children[0] as TextBox).Text == (string)this.FindResource("stringWriteHere"))
                (MainGrid.Children[0] as TextBox).Text = "";
            string path = GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appBMName + ".txt";
            if (File.Exists(path))
                File.WriteAllText(path, (MainGrid.Children[0] as TextBox).Text);
            else
            {
                File.Create(path);
                File.WriteAllText(path, (MainGrid.Children[0] as TextBox).Text);
            }

            MainSW.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            miSelectDate.IsEnabled = true;
            miToday.IsEnabled = true;
            miArchive.IsEnabled = true;
            miToArchive.IsEnabled = true;
            btnAdd.IsEnabled = true;
            btnLeft.IsEnabled = true;
            btnRight.IsEnabled = true;
            MainGrid.Children.Clear();
            SelectDataForDay(CurrentDateDay);
            IsBookMark = false;
        }
        
        /// <summary>Open bookmark in the <seealso cref="MainWindow"/></summary>
        private void OpenBookmark()
        {
            lblData.MouseUp -= lblData_MouseUp;
            lblData.MouseEnter -= lblData_MouseEnter;
            lblData.MouseLeave -= lblData_MouseLeave;

            this.ResizeMode = ResizeMode.CanMinimize;
            miSelectDate.IsEnabled = false;
            miToday.IsEnabled = false;
            miArchive.IsEnabled = false;
            miToArchive.IsEnabled = false;
            btnAdd.IsEnabled = false;
            btnLeft.IsEnabled = false;
            btnRight.IsEnabled = false;
            MainSW.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            MainGrid.Children.Clear();

            TextBox bookmark = new TextBox() { TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Left, Width = MainGrid.Width - 1, Height = MainGrid.Height, Background = new SolidColorBrush(Colors.Transparent), FontSize = 14, FontStyle = FontStyles.Italic, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, BorderThickness = new Thickness(0) };
            string path = GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appBMName + ".txt";
            if (File.Exists(path))
                bookmark.Text = File.ReadAllText(path);
            else
                File.Create(path);

            if (bookmark.Text == "")
                bookmark.Text = (string)this.FindResource("stringWriteHere");

            MainGrid.Children.Add(bookmark);
            lblData.Content = (string)this.FindResource("stringBookmark");

            IsBookMark = true;
        }
        
        /// <summary>Show <seealso cref="DialogWindow"/> with some information and some actions for a choice</summary>
        /// <param name="parentwindow">Parent window that called this DialogWindow</param>\
        /// <param name="message">Information that should be showed</param>
        /// <param name="yes">Content for a "yes" button-choice</param>
        /// <param name="no">Content for a "no" button-choice</param>
        /// <param name="showcheckbox">Is a checkbox necessary</param>
        /// <param name="waschecked">The result of communication with checkbox</param>
        /// <returns>New textblock with information about note</returns>
        public bool ShowDialogWindow(Window parentwindow, string message, string yes, string no, bool showcheckbox, out bool waschecked)
        {
            DialogWindow dw = new DialogWindow(parentwindow, message, showcheckbox) { WindowStartupLocation = WindowStartupLocation.CenterScreen };
            dw.btnNo.Content = no;
            dw.btnYes.Content = yes;
            dw.ShowDialog();
            waschecked = !dw.IsDontSee;
            return (bool)dw.DialogResult;
        }

        #region Methods of work with Database
        private void AddToDB(Note note)
        {
            using (SqlConnection connection = new SqlConnection(MainConnectionString))
            {
                try
                {
                    connection.Open();
                    string sqlExpression = "INSERT INTO TableOfNotes (Id, MessageNote, IsDoneNote, DateNote, TypeNote, IntervalValueNote, IntervalVarsNote, RemindBeforeNote, RemindEveryNote, KindRemindNote, StopDateNote, IsArchiveNote) VALUES (@Id, @MessageNote, @IsDoneNote,  @DateNote, @TypeNote, @IntervalValueNote, @IntervalVarsNote, @RemindBeforeNote, @RemindEveryNote, @KindRemindNote, @StopDateNote, @IsArchiveNote)";
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        SqlParameter ParamId = new SqlParameter("@Id", note.Id);
                        command.Parameters.Add(ParamId);
                        SqlParameter ParamMessage = new SqlParameter("@MessageNote", note.Message);
                        command.Parameters.Add(ParamMessage);
                        SqlParameter ParamIsDone = new SqlParameter("@IsDoneNote", note.IsDo);
                        command.Parameters.Add(ParamIsDone);
                        SqlParameter ParamDate = new SqlParameter("@DateNote", note.Date);
                        command.Parameters.Add(ParamDate);
                        SqlParameter ParamType = new SqlParameter("@TypeNote", (byte)note.Type);
                        command.Parameters.Add(ParamType);
                        SqlParameter ParamIntervalValue = new SqlParameter("@IntervalValueNote", note.IntervalValue);
                        command.Parameters.Add(ParamIntervalValue);
                        SqlParameter ParamIntervalVars = new SqlParameter("@IntervalVarsNote", note.IntervalVars);
                        command.Parameters.Add(ParamIntervalVars);
                        SqlParameter ParamRemindBefore = new SqlParameter("@RemindBeforeNote", note.RemindBefore);
                        command.Parameters.Add(ParamRemindBefore);
                        SqlParameter ParamRemindEvery = new SqlParameter("@RemindEveryNote", note.RemindEvery);
                        command.Parameters.Add(ParamRemindEvery);
                        SqlParameter ParamKindRemind = new SqlParameter("@KindRemindNote", note.KindRemind);
                        command.Parameters.Add(ParamKindRemind);
                        SqlParameter ParamStopDate = new SqlParameter("@StopDateNote", new DateTime(GlobalClass.constNullYear, 1, 1));
                        command.Parameters.Add(ParamStopDate);
                        SqlParameter ParamIsArchive = new SqlParameter("@IsArchiveNote", note.IsArchive);
                        command.Parameters.Add(ParamIsArchive);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        public void UpdateDB(int id, Note note)
        {
            string sqlExpression = "UPDATE TableOfNotes SET MessageNote=@MessageNote, IsDoneNote=@IsDoneNote, DateNote=@DateNote, TypeNote=@TypeNote, IntervalValueNote=@IntervalValueNote, IntervalVarsNote=@IntervalVarsNote, RemindBeforeNote=@RemindBeforeNote, RemindEveryNote=@RemindEveryNote, KindRemindNote=@KindRemindNote, IsArchiveNote=@IsArchiveNote WHERE Id=" + id.ToString();

            using (SqlConnection connection = new SqlConnection(MainConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        SqlParameter ParamMessage = new SqlParameter("@MessageNote", note.Message);
                        command.Parameters.Add(ParamMessage);
                        SqlParameter ParamIsDone = new SqlParameter("@IsDoneNote", note.IsDo);
                        command.Parameters.Add(ParamIsDone);
                        SqlParameter ParamDate = new SqlParameter("@DateNote", note.Date);
                        command.Parameters.Add(ParamDate);
                        SqlParameter ParamType = new SqlParameter("@TypeNote", (byte)note.Type);
                        command.Parameters.Add(ParamType);
                        SqlParameter ParamIntervalValue = new SqlParameter("@IntervalValueNote", note.IntervalValue);
                        command.Parameters.Add(ParamIntervalValue);
                        SqlParameter ParamIntervalVars = new SqlParameter("@IntervalVarsNote", note.IntervalVars);
                        command.Parameters.Add(ParamIntervalVars);
                        SqlParameter ParamRemindBefore = new SqlParameter("@RemindBeforeNote", note.RemindBefore);
                        command.Parameters.Add(ParamRemindBefore);
                        SqlParameter ParamRemindEvery = new SqlParameter("@RemindEveryNote", note.RemindEvery);
                        command.Parameters.Add(ParamRemindEvery);
                        SqlParameter ParamKindRemind = new SqlParameter("@KindRemindNote", note.KindRemind);
                        command.Parameters.Add(ParamKindRemind);
                        SqlParameter ParamIsArchive = new SqlParameter("@IsArchiveNote", note.IsArchive);
                        command.Parameters.Add(ParamIsArchive);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void DeleteFromDB(int id)
        {
            string sqlExpression = "DELETE FROM TableOfNotes WHERE Id=" + id.ToString();
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
        }
        private int FindNextId()
        {
            int res = 0;
            using (SqlConnection connection = new SqlConnection(MainConnectionString))
            {
                try
                {
                    connection.Open();
                    string sqlExpression = "SELECT Id FROM " + Properties.Resources.appDBTableName + " WHERE Id=(SELECT max(Id) FROM " + Properties.Resources.appDBTableName + ");";
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        command.ExecuteNonQuery();
                        object o = command.ExecuteScalar();
                        if (o != null)
                            res = (int)o;
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message + "\n Id value is damaged, do not change notes!");
                }
            }

            return res;
        }
        #endregion

        /// <summary>Set the sound for <seealso cref="Music"/></summary>
        /// <param name="music">File name of sound</param>
        private void SetMusic(string music)
        {
            string path = GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appMusFolderName + "\\" + music;
            if (File.Exists(path))
            {
                Music = new SoundPlayer(path);
            }
        }
    }
}

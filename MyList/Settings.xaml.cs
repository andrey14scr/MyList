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

namespace MyList
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public string MusicName = "";
        private bool CheckMusic(string file)
        {
            string res = "";
            int i = file.Length - 1;
            while (file[i] != '.' && i >= 0)
            {
                res = file[i] + res;
                i--;
            }

            if (res != "wav")
                return false;
            return true;
        }

        public Settings()
        {
            GlobalClass.SetLang(this);
            InitializeComponent();
            switch (GlobalClass.CurrentLanguage)
            {
                case MyList.Language.English:
                    cbLang.SelectedIndex = 0;
                    break;
                case MyList.Language.Russain:
                    cbLang.SelectedIndex = 1;
                    break;
                default:
                    break;
            }

            string currentmusic = "";
            using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList"))
            {
                if (Key != null && Key.GetValue(Properties.Resources.regMusicFile) != null)
                {
                    currentmusic = Key.GetValue(Properties.Resources.regMusicFile).ToString();
                }
            }

            string path = GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appMusFolderName;
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (var item in dir.GetFiles())
            {
                if (CheckMusic(item.Name))
                {
                    cbMusic.Items.Add(item.Name);
                    if (item.Name == currentmusic)
                        cbMusic.SelectedItem = item.Name;
                }
            }

            if (!GlobalClass.IsSound)
            {
                cbMusic.IsEnabled = false;
                btnStateMusic.Content = (string)FindResource("stringOff");
            }
            else
            {
                cbMusic.IsEnabled = true;
                btnStateMusic.Content = (string)FindResource("stringOnn");
            }
        }


        private void cbLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbLang.SelectedIndex)
            {
                case 0:
                    GlobalClass.CurrentLanguage = MyList.Language.English;
                    break;
                case 1:
                    GlobalClass.CurrentLanguage = MyList.Language.Russain;
                    break;
                default:
                    break;
            }
            GlobalClass.SetLang(this);
            if (!GlobalClass.IsSound)
                btnStateMusic.Content = (string)FindResource("stringOff");
            else
                btnStateMusic.Content = (string)FindResource("stringOnn");
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void btnClearDB_Click(object sender, RoutedEventArgs e)
        {
            List<int> IDs = new List<int>();
            string sqlExpressionSettingsFind = "SELECT * FROM TableOfNotes";
            using (SqlConnection connection = new SqlConnection((this.Owner as MainWindow).MainConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand commandFind = new SqlCommand(sqlExpressionSettingsFind, connection))
                    {
                        using (SqlDataReader reader = commandFind.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                DateTime MainNowTime = DateTime.Now;
                                while (reader.Read())
                                {
                                    IDs.Add((int)reader["Id"]);
                                }
                            }
                        }
                    }
                    foreach (var item in IDs)
                    {
                        string sqlExpressionSettingsDelete = "DELETE FROM TableOfNotes WHERE Id=" + item.ToString();
                        SqlCommand commandDelete = new SqlCommand(sqlExpressionSettingsDelete, connection);
                        commandDelete.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnSeeDB_Click(object sender, RoutedEventArgs e)
        {
            string path = GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + "db" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + ".txt";
            string result = "";

            string sqlExpressionSettingsFind = "SELECT * FROM TableOfNotes";
            using (SqlConnection connection = new SqlConnection((this.Owner as MainWindow).MainConnectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand commandFind = new SqlCommand(sqlExpressionSettingsFind, connection))
                    {
                        using (SqlDataReader reader = commandFind.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                DateTime MainNowTime = DateTime.Now;
                                while (reader.Read())
                                {
                                    result += "ID: " + ((int)reader["Id"]).ToString() + "\n"
                                            + (string)reader["MessageNote"] + "\n"
                                            + ((bool)reader["IsDoneNote"] ? "выполнена" : "не выполнена") + "\n"
                                            + "дата: " + (((DateTime)reader["DateNote"]).Year != GlobalClass.constNullYear ? ((DateTime)reader["DateNote"]).ToString() : (((DateTime)reader["DateNote"]).TimeOfDay).ToString()) + "\n"
                                            + ((bool)reader["IsArchiveNote"] ? "в архиве" : "не в архиве") + "\n";
                                    switch ((byte)reader["TypeNote"])
                                    {
                                        case 0:
                                            result += "без повторений\n";
                                            break;
                                        case 1:
                                            result += "повторение каждые " + (string)reader["IntervalValueNote"] + " дней\n";
                                            break;
                                        case 2:
                                            result += "повторение каждый " + (int)reader["IntervalVarsNote"] + "(день недели)\n";
                                            break;
                                        case 3:
                                            result += "повторение каждый " + (string)reader["IntervalValueNote"] + " день " + (int)reader["IntervalVarsNote"] + " месяцев\n";
                                            break;
                                        default:
                                            break;
                                    }
                                    switch ((byte)reader["KindRemindNote"])
                                    {
                                        case 0:
                                            result += "без напоминаний\n";
                                            break;
                                        case 1:
                                            result += "напомнить за " + ((int)reader["RemindBeforeNote"]).ToString() + " минут\n";
                                            break;
                                        case 2:
                                            result += "напомнить за " + ((int)reader["RemindBeforeNote"]).ToString() + " и повторять каждые " + ((int)reader["RemindEveryNote"]).ToString() + " минут\n";
                                            break;
                                        default:
                                            break;
                                    }
                                    if (reader["StopDateNote"] as DateTime? != null)
                                        result += "остановлен в течение " + (((DateTime)reader["StopDateNote"]).Date).ToString();


                                    result += "\n\n";
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

            File.WriteAllText(path, result);

            MessageBox.Show("готово");
        }

        private void cbMusic_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true))
            {
                if (Key != null)
                {
                    Key.SetValue(Properties.Resources.regMusicFile, cbMusic.SelectedItem.ToString());
                    MusicName = cbMusic.SelectedItem.ToString();
                }
            }
        }

        private void btnStateMusic_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalClass.IsSound)
            {
                cbMusic.IsEnabled = false;
                GlobalClass.IsSound = false;
                btnStateMusic.Content = (string)FindResource("stringOff");
            }
            else
            {
                cbMusic.IsEnabled = true;
                GlobalClass.IsSound = true;
                btnStateMusic.Content = (string)FindResource("stringOnn");
            }

            using (RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"Software\MyList", true))
            {
                if (Key != null)
                {
                    Key.SetValue(Properties.Resources.regIsSound, (GlobalClass.IsSound).ToString());
                }
            }
        }
    }
}

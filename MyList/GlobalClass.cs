using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MyList
{
    public enum Language
    {
        English,
        Russain,
    }

    public enum KindTime
    {
        Min,
        Hour,
        Day,
    }

    static class GlobalClass
    {
        public const int constNullYear = 1999;

        public static bool IsRunning = false;
        public static bool IsSound = false;

        /*
        public static List<int> StringToListOfInt(string str)
        {
            List<int> list = new List<int>();
            if (str != "")
            {
                string[] s = str.Split(' ');
                foreach (var item in s)
                {
                    if (item != "")
                        list.Add(Int32.Parse(item));
                }
            }
            return list;
        }
        */

        public static bool[] IntToBitArray(int number)
        {
            BitArray b = new BitArray(new int[] { number });
            return b.Cast<bool>().ToArray();
        }
        public static int BitArrayToInt(bool[] bits)
        {
            BitArray b = new BitArray(bits);
            int[] array = new int[1];
            b.CopyTo(array, 0);
            return array[0];
        }


        public static void SetRemindDate(object nexttime, int id)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + FindDBPath() + ";Integrated Security=True";
            string sqlExpression = "UPDATE TableOfNotes SET RemindDateNote=@RemindDateNote WHERE Id=" + id.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        SqlParameter ParamRemindDate;
                        if (nexttime != null)
                            ParamRemindDate = new SqlParameter("@RemindDateNote", (DateTime)nexttime);
                        else
                            ParamRemindDate = new SqlParameter("@RemindDateNote", DBNull.Value);
                        command.Parameters.Add(ParamRemindDate);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        public static void SetStopDate(object stopdate, int id)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + FindDBPath() + ";Integrated Security=True";
            string sqlExpression = "UPDATE TableOfNotes SET StopDateNote=@StopDateNote WHERE Id=" + id.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        SqlParameter ParamStopDate;
                        if (stopdate != null)
                            ParamStopDate = new SqlParameter("@StopDateNote", (DateTime)stopdate);
                        else
                            ParamStopDate = new SqlParameter("@StopDateNote", DBNull.Value);
                        command.Parameters.Add(ParamStopDate);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public static void ResetData(object nexttime, object stopdate, int id)
        {
            SetRemindDate(nexttime, id);
            SetStopDate(stopdate, id);
        }

        public static Language CurrentLanguage;
        public static void SetLang(Window w)
        {
            if (w != null) 
            {
                switch (CurrentLanguage)
                {
                    case MyList.Language.English:
                        w.Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/Eng.xaml") };
                        break;
                    case MyList.Language.Russain:
                        w.Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/Rus.xaml") };
                        break;
                    default:
                        w.Resources = new ResourceDictionary() { Source = new Uri("pack://application:,,,/Eng.xaml") };
                        break;
                }
            }
            else
            {
                throw new Exception("Null window(l)");
            }
        }

        public struct StructFlags
        {
            public string AutoTurn;
            public StructFlags(bool b)
            {
                AutoTurn = "-a";
            }
        }
        public static string Flag;
        //public static string AutoTurn = "-a";
        public static StructFlags Flags = new StructFlags(true);

        public static string FindPath()
        {
            string path = Assembly.GetExecutingAssembly().Location;
            path = path.Remove(path.Length - Properties.Resources.appName.Length - 5);
            return path;
        }

        public static string FindDBPath()
        {
            return GlobalClass.FindPath() + "\\" + Properties.Resources.appResFolderName + "\\" + Properties.Resources.appDBName + ".mdf";
        }

        /*
        public static string Aboutnote(int id)
        {
            string res = "";
            string connectionString = FindDBPath();
            string sqlExpressionSettingsFind = "SELECT * FROM TableOfNotes WHERE Id=" + id.ToString();
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
                SqlCommand commandFind = new SqlCommand(sqlExpressionSettingsFind, connection);
                SqlDataReader reader = commandFind.ExecuteReader();
                if (reader.HasRows)
                {
                    res = "ID: " + ((int)reader["Id"]).ToString() + "; "
                                + (string)reader["MessageNote"] + "; "
                                + ((bool)reader["IsDoneNote"] ? "выполнена" : "не выполнена") + "; "
                                + "дата: " + (((DateTime)reader["DateNote"]).Year != GlobalClass.constNullYear ? ((DateTime)reader["DateNote"]).ToString() : (((DateTime)reader["DateNote"]).TimeOfDay).ToString()) + "; "
                                + ((bool)reader["IsArchiveNote"] ? "в архиве" : "не в архиве") + "; ";
                    switch ((byte)reader["TypeNote"])
                    {
                        case 0:
                            res += "без повторений; ";
                            break;
                        case 1:
                            res += "повторение каждые " + (string)reader["IntervalValueNote"] + " дней; ";
                            break;
                        case 2:
                            res += "повторение каждый " + (string)reader["IntervalVarsNote"] + " день недели(0 - воскресенье, 1 - понедельник...); ";
                            break;
                        case 3:
                            res += "повторение каждый " + (string)reader["IntervalValueNote"] + " день " + (string)reader["IntervalVarsNote"] + " месяцев; ";
                            break;
                        default:
                            break;
                    }
                    switch ((byte)reader["KindRemindNote"])
                    {
                        case 0:
                            res += "без напоминаний; ";
                            break;
                        case 1:
                            res += "напомнить за " + ((int)reader["RemindBeforeNote"]).ToString() + " минут; ";
                            break;
                        case 2:
                            res += "напомнить за " + ((int)reader["RemindBeforeNote"]).ToString() + " и повторять каждые " + ((int)reader["RemindEveryNote"]).ToString() + " минут; ";
                            break;
                        default:
                            break;
                    }
                    if (reader["StopDateNote"] as DateTime? != null)
                        res += "остановлен в течение " + (((DateTime)reader["StopDateNote"]).Date).ToShortDateString();
                }
                reader.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return res;
        }
        */
        
    }
}

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MyList
{
    public class Note
    {
        public int Id { get; set; }//
        public string Message { get; set; }//
        public DateTime Date { get; set; }//
        public string IntervalValue { get; set; }//
        public int IntervalVars { get; set; }//
        public int RemindBefore { get; set; }//
        public int RemindEvery { get; set; }//
        public bool IsDo { get; set; }//
        public TypeNote Type { get; set; }//
        public KindRemind KindRemind { get; set; }//
        public bool IsArchive { get; set; }//

        public void SetIsDone(bool isdone)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + GlobalClass.FindDBPath() + ";Integrated Security=True";
            string sqlExpression = "UPDATE TableOfNotes SET IsDoneNote=@IsDoneNote WHERE Id=" + this.Id.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        SqlParameter ParamIsDone = new SqlParameter("@IsDoneNote", isdone);
                        command.Parameters.Add(ParamIsDone);
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        public void SetStopDate(DateTime stopdt)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + GlobalClass.FindDBPath() + ";Integrated Security=True";
            string sqlExpression = "UPDATE TableOfNotes SET StopDateNote=@StopDateNote WHERE Id=" + this.Id.ToString();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sqlExpression, connection))
                    {
                        SqlParameter ParamStopDate = new SqlParameter("@StopDateNote", stopdt);
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

    }

    public enum KindRemind
    {
        Usual,
        Once,
        Repeatable,
    }
    public enum TypeNote
    {
        Usual,

        DaysPeriod,
        WeekPeriod,
        MonthPeriod,
    }

    public class MessagePanel : StackPanel
    {
        public Note InnerNote { get; set; }
        public void SetUsualColor(Note n)
        {
            if (n.Date.AddMinutes(1).TimeOfDay < DateTime.Now.TimeOfDay)
                this.Background = MainWindow.UsualPastColor;
            else
                this.Background = MainWindow.UsualColor;
        }
    }
}

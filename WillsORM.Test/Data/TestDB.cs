using System.Configuration;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Xml;

namespace WillsORM.Test.Data
{
    public class TestDB
    {
        private string connString = ConfigurationManager.ConnectionStrings["TestDB"].ToString();

        public TestDB()
        {
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connString);
        }


        public DataTable GetBySQL(string sql)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                SqlCommand command = new SqlCommand(sql, conn);
                SqlDataReader reader = command.ExecuteReader();
                
                dt.Load(reader);

                conn.Close();
            }

            return dt;
        }

        public void RunSQL(string sql)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                SqlCommand command = new SqlCommand(sql, conn);
                command.ExecuteNonQuery();

                conn.Close();
            }

        }
    }
}

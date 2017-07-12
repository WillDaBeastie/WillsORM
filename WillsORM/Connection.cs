using System;
using System.Configuration;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace WillsORM
{
    public class Connection
    {
        public string _connectionStringName = string.Empty;
        public string ConnectionStringName
        {
            get
            {
                return _connectionStringName;
            }
            set
            {
                _connectionStringName = value;
            }
        }

        private string GetConnString()
        {
            if (!string.IsNullOrEmpty(ConnectionStringName))
            {
                return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
            }
            else
            {
                return string.Empty;
            }
        }

        public SqlConnection ConnObj
        {
            get
            {
                return new SqlConnection(GetConnString());
            }
        }
    }
}

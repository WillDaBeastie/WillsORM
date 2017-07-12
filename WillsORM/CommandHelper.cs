using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Configuration;

namespace WillsORM
{
    public class CommandHelper<T>
    {
        private string _baseTypeName = "DataObj";

        public PropertyInfo[] props { get; set; }

        public Dictionary<string, object> addedParameters { get; set; }

        private string _primaryKey;

        private bool? _hasOrderColumn = null;

        private string _sqlSchema = "dbo";

        public string SqlSchema
        {
            get
            {
                return _sqlSchema;
            }
            set
            {
                _sqlSchema = value;
            }
        }

        public SqlCommand CreateObjectParams(SqlCommand command, T obj, bool includePrimary)
        {
           // Type type = typeof(T);
            string primaryKey = FindPrimaryKey();
            int maxChars = 150;

            foreach (PropertyInfo param in props)
            {
                ValidationAttribute[] vals = (ValidationAttribute[])param.GetCustomAttributes(typeof(ValidationAttribute), false);

                if ((primaryKey != param.Name && !includePrimary || includePrimary) && !param.Name.StartsWith("FK_"))
                {
                    if (param.PropertyType.Namespace != "System.Collections.Generic")
                    {
                        if (param.PropertyType.BaseType.Name != _baseTypeName)
                        {
                            if (vals.Length > 0)
                            {
                                maxChars = vals[0].MaxChars;
                            }
                            object paramValue = param.GetValue(obj, null);

                            string typeName = param.PropertyType.Name;
                            if (Nullable.GetUnderlyingType(param.PropertyType) != null)
                            {
                                typeName = Nullable.GetUnderlyingType(param.PropertyType).Name;
                            }

                            addParameters(ref command, param.Name, typeName, paramValue, maxChars);
                        }
                    }
                }
            }

            if (addedParameters != null)
            {
                foreach (KeyValuePair<string, object> kvp in addedParameters)
                {
                    if (kvp.Value == null)
                    {
                        addParameters(ref command, kvp.Key, null, null, maxChars);
                    }
                    else
                    {
                        addParameters(ref command, kvp.Key, kvp.Value.GetType().Name, kvp.Value, maxChars);
                    }
                }
            }

            return command;
        }

        private void addParameters(ref SqlCommand command, string paramName, string propType, object value, int maxChars)
        {
            SqlDbType dbType = DataHelper.FindParamType(propType);
            SqlParameter myParam;
            if (propType == "String")
            {
                myParam = command.Parameters.Add("@" + paramName, dbType, maxChars);
            }
            else
            {
                myParam = command.Parameters.Add("@" + paramName, dbType);
            }

            myParam.Value = GetCleansedValue(value);
        }

        public SqlCommand CreateObjectParams(SqlCommand command, T obj, T original, bool includePrimary)
        {
            // Type type = typeof(T);
            string primaryKey = FindPrimaryKey();

            foreach (PropertyInfo param in props)
            {
                ValidationAttribute[] vals = (ValidationAttribute[])param.GetCustomAttributes(typeof(ValidationAttribute), false);

                if (param.PropertyType.Namespace != "System.Collections.Generic")
                {
                    if (param.PropertyType.BaseType.Name != _baseTypeName)
                    {
                        if ((primaryKey != param.Name && !includePrimary || includePrimary) && !param.Name.StartsWith("FK_"))
                        {
                            string typeName = param.PropertyType.Name;
                            if (Nullable.GetUnderlyingType(param.PropertyType) != null)
                            {
                                typeName = Nullable.GetUnderlyingType(param.PropertyType).Name;
                            }

                            SqlDbType dbType = DataHelper.FindParamType(typeName);
                            int maxChars = 150;

                            if (vals.Length > 0)
                            {
                                maxChars = vals[0].MaxChars;
                            }

                            SqlParameter myParam;
                            if (param.PropertyType.Name == "String")
                            {
                                myParam = command.Parameters.Add("@" + param.Name, dbType, maxChars);
                            }
                            else
                            {
                                myParam = command.Parameters.Add("@" + param.Name, dbType);
                            }

                            object paramValue = param.GetValue(obj, null);
                            myParam.Value = GetCleansedValue(paramValue);

                            bool isBigTextOrimage = false;
                            if (param.GetCustomAttributes(typeof(DBAttribute), false).Length > 0)
                            {
                                DBAttribute db = (DBAttribute)param.GetCustomAttributes(typeof(DBAttribute), false)[0];
                                if (db != null)
                                {
                                    isBigTextOrimage = db.IsBigTextOrImage;
                                }
                            }

                            if (primaryKey != param.Name && (isBigTextOrimage || param.PropertyType.Name == "String" || param.GetType().Name == "Boolean" || param.GetType().Name == "DateTime" || param.GetType().Name == "Int32"))
                            {
                                SqlParameter concurrParam;
                                if (param.PropertyType.Name == "String")
                                {
                                    concurrParam = command.Parameters.Add("@original_" + param.Name, dbType, maxChars);
                                }
                                else
                                {
                                    concurrParam = command.Parameters.Add("@original_" + param.Name, dbType);
                                }

                                object concurrValue = param.GetValue(original, null);
                                concurrParam.Value = GetCleansedValue(concurrValue);

                                SqlParameter concurrIsNullParam;
                                concurrIsNullParam = command.Parameters.Add("@original_" + param.Name + "_IsNull", SqlDbType.Bit);
                                concurrIsNullParam.Value = (concurrParam.Value == DBNull.Value ? 1 : 0);
                            }
                        }
                    }
                }
            }

            return command;
        }

        private object GetCleansedValue(object paramValue)
        {
            if (paramValue == null)
            {
                paramValue = DBNull.Value;
            }
            else if (paramValue.GetType().Name == "DateTime")
            {
                DateTime dt = DateTime.MinValue;
                DateTime.TryParse(paramValue.ToString(), out dt);
                paramValue = dt.ToLongDateString() + " " + dt.ToLongTimeString();
            }
            else if (paramValue.GetType().Name == "Boolean")
            {
                if ((bool)paramValue)
                {
                    paramValue = 1;
                }
                else
                {
                    paramValue = 0;
                }
            }

            return paramValue;
        }

        public string CreateForeignIDSQL(string foreignKey)
        {
            Type type = typeof(T);

            string tableName = type.Name;

            string primaryKey = FindPrimaryKey();

            string sql = "SELECT " + foreignKey + " FROM ["+ _sqlSchema +"].[" + tableName + "] WHERE  " + primaryKey + " = @" + primaryKey;

            return sql;
        }

        public string CreateInsertSQL()
        {
            Type type = typeof(T);

            string tableName = type.Name;

            if ((type.GetCustomAttributes(typeof(TableAttribute), false).Length) > 0)
            {
                TableAttribute db = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), false)[0];
                if (db.Name.Length > 0)
                {
                    tableName = db.Name;
                }
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder sbVals = new StringBuilder();

            sb.Append("INSERT INTO [" + _sqlSchema + "].[" + tableName + "] (");
            sbVals.Append(") VALUES (");

            int amnt = props.Length + addedParameters.Count;
            int cnt = 0;
            string primaryKey = FindPrimaryKey();

            foreach (PropertyInfo param in props)
            {
                if (param.PropertyType.Namespace != "System.Collections.Generic")
                {
                    string colName = param.Name;

                    DBAttribute db = DataHelper.FindAttribute<DBAttribute>(param);
                    if (db != null && db.ColumnName != null && db.ColumnName.Length > 0)
                    {
                        colName = db.ColumnName;
                    }

                    addSqlParams(primaryKey, colName, amnt, cnt, param.PropertyType.BaseType.Name, ref sb, ref sbVals);
                }
                cnt++;
            }

            foreach (KeyValuePair<string, object> kvp in addedParameters)
            {
                addSqlParams(primaryKey, kvp.Key, amnt, cnt, "", ref sb, ref sbVals);
                cnt++;
            }

            sbVals.Append(" )");

            sbVals.AppendFormat(" SELECT * FROM [{2}].[{0}] WHERE {1} = SCOPE_IDENTITY()", tableName, primaryKey, _sqlSchema);

            return sb.ToString() + sbVals.ToString();
        }

        public string CreateInsertManyManySQL(string parentKey, long parentId, long[] primaryIds, string tableName)
        {
            Type type = typeof(T);

            StringBuilder sb = new StringBuilder();

            string primaryKey = FindPrimaryKey();
            foreach (long id in primaryIds)
            {

                sb.AppendFormat(" MERGE [{0}] AS T", tableName);
                sb.AppendFormat("   USING (SELECT {0} AS [{1}], {2} AS [{3}] ) AS S", parentId.ToString(), parentKey, id.ToString(), primaryKey);
                sb.AppendFormat("   ON  T.[{0}] = S.[{0}]", primaryKey);
                sb.AppendFormat("    AND T.[{0}] = S.[{0}]", parentKey);
             //   sb.Append("      WHEN MATCHED THEN");
              //  sb.AppendFormat("       UPDATE SET T.[ORDER] = {0}", count);
                sb.Append("     WHEN NOT MATCHED THEN");
                sb.AppendFormat("       INSERT ([{0}], [{1}]) ", parentKey, primaryKey);
                sb.AppendFormat("        VALUES(S.[{0}], S.[{1}]);", parentKey, primaryKey);

            }
            
            return sb.ToString();
        }

        public string CreateInsertManyManyOrderSQL(string parentKey, long parentId, long[] primaryIds, string tableName)
        {
            Type type = typeof(T);

            StringBuilder sb = new StringBuilder();

            string primaryKey = FindPrimaryKey();

            int count = 1;
            foreach (long id in primaryIds)
            {

                sb.AppendFormat(" MERGE [{0}] AS T", tableName);
                sb.AppendFormat("   USING (SELECT {0} AS [{1}], {2} AS [{3}] ) AS S", parentId.ToString(), parentKey, id.ToString(), primaryKey);
                sb.AppendFormat("   ON  T.[{0}] = S.[{0}]", primaryKey);
                sb.AppendFormat("    AND T.[{0}] = S.[{0}]", parentKey);
                sb.Append("      WHEN MATCHED THEN");
                sb.AppendFormat("       UPDATE SET T.[ORDER] = {0}", count);
                sb.Append("     WHEN NOT MATCHED THEN");
                sb.AppendFormat("       INSERT ([{0}], [{1}], [Order]) ", parentKey, primaryKey);
                sb.AppendFormat("        VALUES(S.[{0}], S.[{1}], {2});", parentKey, primaryKey, count);

                count++;

            }

            return sb.ToString();
        }

        private void addSqlParams(string excludedPrimaryKey, string paramName, int amnt, int cnt, string BaseTypeName, ref StringBuilder sb, ref StringBuilder sbVals)
        {
            if (excludedPrimaryKey != paramName && BaseTypeName != _baseTypeName  && !paramName.StartsWith("FK_"))
            {
                if (cnt < amnt - 1)
                {
                    sb.AppendFormat("[{0}], ", paramName);
                    sbVals.AppendFormat("@{0}, ", paramName);
                }
                else
                {
                    sb.AppendFormat("[{0}]",  paramName);
                    sbVals.AppendFormat("@{0}", paramName);
                }
            }
        }

        private void addSqlParamsWithJoin(string tableAlias, string excludedPrimaryKey, string paramName, int amnt, int cnt, string BaseTypeName, ref StringBuilder sb, ref StringBuilder sbVals)
        {
            if (excludedPrimaryKey != paramName && BaseTypeName != "DataObj" && !paramName.StartsWith("FK_"))
            {
                if (cnt < amnt - 1)
                {
                    sb.AppendFormat("[{0}].[{1}], ", tableAlias, paramName);
                    sbVals.AppendFormat("@{0}, ", paramName);
                }
                else
                {
                    sb.AppendFormat("[{0}].[{1}]", tableAlias, paramName);
                    sbVals.AppendFormat("@{0}", paramName);
                }
            }
        }

        public string CreateUpdateSQL()
        {
            Type type = typeof(T);

            string tableName = type.Name;

            if ((type.GetCustomAttributes(typeof(TableAttribute), false).Length) > 0)
            {
                TableAttribute db = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), false)[0];
                if (db.Name.Length > 0)
                {
                    tableName = db.Name;
                }
            }
            StringBuilder sb = new StringBuilder();
            StringBuilder sbVals = new StringBuilder();
            sb.Append(string.Format("UPDATE [{0}].[{1}] SET ", _sqlSchema, tableName));

            int amnt = props.Length + addedParameters.Count;
            int cnt = 0;

            string primaryKey = FindPrimaryKey();

            foreach (PropertyInfo param in props)
            {
                if (param.PropertyType.Namespace != "System.Collections.Generic")
                {
                    if (primaryKey != param.Name && param.PropertyType.BaseType.Name != _baseTypeName && !param.Name.StartsWith("FK_"))
                    {
                        if (cnt < amnt - 1)
                        {
                            sb.Append("[" + param.Name + "] = @" + param.Name + ", ");
                        }
                        else
                        {
                            sb.Append("[" + param.Name + "]  = @" + param.Name);
                        }
                    }
                }
                cnt++;
            }

            foreach (KeyValuePair<string, object> kvp in addedParameters)
            {
                if (cnt < amnt - 1)
                {
                    sb.Append("[" + kvp.Key + "] = @" + kvp.Key + ", ");
                }
                else
                {
                    sb.Append("[" + kvp.Key + "] = @" + kvp.Key);
                }
                cnt++;
            }

            sb.Append(" WHERE [" + primaryKey + "] = @" + primaryKey);

            return sb.ToString();
        }

        public string CreateUpdateWConcurrencySQL(T original)
        {
            Type type = typeof(T);

            StringBuilder sb = new StringBuilder();
            sb.Append(CreateUpdateSQL() + " ");

          //  int amnt = props.Length;
            int cnt = 0;

            string primaryKey = FindPrimaryKey();
            foreach (PropertyInfo param in props)
            {
                if (param.PropertyType.Namespace != "System.Collections.Generic")
                {
                    if (primaryKey != param.Name && param.PropertyType.BaseType.Name != "DataObj" && !param.Name.StartsWith("FK_"))
                    {
                        bool isBigTextOrimage = false;
                        if (param.GetCustomAttributes(typeof(DBAttribute), false).Length > 0)
                        {
                            DBAttribute db = (DBAttribute)param.GetCustomAttributes(typeof(DBAttribute), false)[0];
                            if (db != null)
                            {
                                isBigTextOrimage = db.IsBigTextOrImage;
                            }
                        }

                        if (isBigTextOrimage)
                        {
                            sb.Append(" AND (Cast([" + param.Name + "] AS Varchar(max)) = Cast(@original_" + param.Name + " AS Varchar(max)) OR @original_" + param.Name + "_isNull =1 AND @original_" + param.Name + " IS null)");
                        }
                        else if (param.GetType().Name == "String" || param.GetType().Name == "Boolean" || param.GetType().Name == "DateTime" || param.GetType().Name == "Int32")
                        {
                            sb.Append(" AND ([" + param.Name + "] = @original_" + param.Name + " OR @original_" + param.Name + "_isNull =1 AND @original_" + param.Name + " IS null)");
                        }
                    }
                }
                cnt++;
            }

            return sb.ToString();
            
        }

        public string CreateSelectByIDSQL()
        {
            Type type = typeof(T);

            string tableName = type.Name;

            if ((type.GetCustomAttributes(typeof(TableAttribute), false).Length) > 0)
            {
                TableAttribute db = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), false)[0];
                if (db.Name.Length > 0)
                {
                    tableName = db.Name;
                }
            }
            StringBuilder sb = new StringBuilder();
            StringBuilder sbVals = new StringBuilder();
            sb.Append("SELECT ");

            int amnt = props.Length;
            int cnt = 0;

            string primaryKey = FindPrimaryKey();

            foreach (PropertyInfo param in props)
            {
                if (param.PropertyType.Namespace != "System.Collections.Generic")
                {
                    string colName = param.Name;

                    DBAttribute db = DataHelper.FindAttribute<DBAttribute>(param);
                    if (db != null && db.ColumnName != null && db.ColumnName.Length > 0)
                    {
                        colName = db.ColumnName;
                    }
                    addSqlParams(string.Empty, colName, amnt, cnt, param.PropertyType.BaseType.Name, ref sb, ref sbVals);
                }
                cnt++;
            }

            sb.Append(" FROM [" + _sqlSchema + "].[" + tableName + "] WHERE " + primaryKey + " = @" + primaryKey);

            return sb.ToString();
        }

        public string CreateSelectByParamsSQL(Dictionary<string, object> whereVals)
        {
            return CreateSelectByParamsSQL(whereVals, new Dictionary<string, bool>());
        }

        public string CreateSelectByParamsSQL(Dictionary<string, object> whereVals, Dictionary<string, bool> orderByList)
        {
            Type type = typeof(T);

            string tableName = type.Name;

            if((type.GetCustomAttributes(typeof(TableAttribute), false).Length)>0)
            {
                TableAttribute db = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), false)[0];
                if (db.Name.Length > 0)
                {
                    tableName = db.Name;
                }
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder sbVals = new StringBuilder();
            sb.Append("SELECT ");

            int amnt = props.Length;
            int cnt = 0;

           // string primaryKey = FindPrimaryKey();

            foreach (PropertyInfo param in props)
            {
                if (param.PropertyType.Namespace != "System.Collections.Generic")
                {
                    string colName = param.Name;

                    DBAttribute db = DataHelper.FindAttribute<DBAttribute>(param);
                    if (db != null && db.ColumnName != null && db.ColumnName.Length > 0)
                    {
                        colName = db.ColumnName;
                    }
                    addSqlParams(string.Empty, colName, amnt, cnt, param.PropertyType.BaseType.Name, ref sb, ref sbVals);
                }

                cnt++;
            }

            sb.Append(" FROM [" + _sqlSchema + "].[" + tableName + "] ");

            if (whereVals != null)
            {
                if (whereVals.Count > 0)
                {
                    sb.Append("WHERE ");
                }

                amnt = whereVals.Count;
                cnt = 0;

                foreach (var param in whereVals)
                {
                    string and = " AND ";
                    if (cnt == (amnt - 1))
                        and = "";

                    if (param.Value == DBNull.Value)
                    {

                        sb.AppendFormat("ISNull({0}, -1) = ISNull(@{0}, -1) {1}", param.Key, and);
                    }
                    else
                    {
                        sb.AppendFormat("{0} = @{0} {1}", param.Key, and);
                    }

                    cnt++;
                }
            }

            if (orderByList != null)
            {
                if (orderByList.Count > 0)
                {
                    sb.Append(" ORDER BY ");
                    amnt = orderByList.Count;
                    cnt = 0;

                    foreach (KeyValuePair<string, bool> param in orderByList)
                    {
                        string ascDesc = " ASC";
                        if (!param.Value)
                        {
                            ascDesc = " DESC";
                        }

                        if (cnt < amnt - 1)
                        {
                            sb.Append("["+param.Key+"]" + ascDesc + ", ");
                        }
                        else
                        {
                            sb.Append("[" + param.Key + "]" + ascDesc);
                        }
                        cnt++;
                    }
                }
            }

            return sb.ToString();
        }

        public string CreateSelectJoinByParamsSQL(string[] paramList, Dictionary<string, bool> orderByList, string joinTable)
        {
            Type type = typeof(T);

            string tableName = type.Name;

            if ((type.GetCustomAttributes(typeof(TableAttribute), false).Length) > 0)
            {
                TableAttribute db = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), false)[0];
                if (db.Name.Length > 0)
                {
                    tableName = db.Name;
                }
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder sbVals = new StringBuilder();
            sb.Append("SELECT ");

            int amnt = props.Length;
            int cnt = 0;

            string primaryKey = FindPrimaryKey();

            foreach (PropertyInfo param in props)
            {
                if (param.PropertyType.Namespace != "System.Collections.Generic")
                {
                    string colName = param.Name;

                    DBAttribute db = DataHelper.FindAttribute<DBAttribute>(param);
                    if (db != null && db.ColumnName != null && db.ColumnName.Length > 0)
                    {
                        colName = db.ColumnName;
                    }
                    addSqlParamsWithJoin("a", string.Empty, colName, amnt, cnt, param.PropertyType.BaseType.Name, ref sb, ref sbVals);
                }

                cnt++;
            }

            sb.AppendFormat(" FROM [{3}].[{0}] a LEFT JOIN [{3}].[{1}] b ON a.[{2}] = b.[{2}] ", tableName, joinTable, primaryKey, _sqlSchema);

            if (paramList.Length > 0)
            {
                sb.Append("WHERE ");
            }

            amnt = paramList.Length;
            cnt = 0;

            foreach (string param in paramList)
            {
                if (cnt < amnt - 1)
                {
                    sb.Append("b.[" + param + "] = @" + param + " AND ");
                }
                else
                {
                    sb.Append("b.[" + param + "] = @" + param);
                }

                cnt++;
            }

            if (orderByList != null)
            {
                if (orderByList.Count > 0)
                {
                    sb.Append(" ORDER BY ");
                    amnt = orderByList.Count;
                    cnt = 0;

                    foreach (KeyValuePair<string, bool> param in orderByList)
                    {
                        string ascDesc = " ASC";
                        if (!param.Value)
                        {
                            ascDesc = " DESC";
                        }

                        if (cnt < amnt - 1)
                        {
                            sb.Append("[" + param.Key + "]" + ascDesc + ", ");
                        }
                        else
                        {
                            sb.Append("[" + param.Key + "]" + ascDesc);
                        }
                        cnt++;
                    }
                }
            }
            else
            {
                sb.AppendFormat(" ORDER BY [{0}] ASC", tableName+"ID");
            }

            return sb.ToString();
        }

        public string CreateDeleteByIDSQL()
        {
            Type type = typeof(T);

            string tableName = type.Name;

            if ((type.GetCustomAttributes(typeof(TableAttribute), false).Length) > 0)
            {
                TableAttribute db = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), false)[0];
                if (db.Name.Length > 0)
                {
                    tableName = db.Name;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE");

            string primaryKey = FindPrimaryKey();

            sb.Append(" FROM ["+_sqlSchema +"].[" + tableName + "] WHERE [" + primaryKey + "] = @" + primaryKey);

            return sb.ToString();
        }

        public string FindPrimaryKey()
        {
            if (_primaryKey == null)
            {
                foreach (PropertyInfo param in props)
                {
                    foreach (DBAttribute db in param.GetCustomAttributes(typeof(DBAttribute), false))
                    {
                        if (db.IsPrimary == true)
                        {
                            _primaryKey = param.Name;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(_primaryKey))
                    {
                        break;
                    }
                }
            }

            return _primaryKey;
        }

        public bool HasOrder()
        {
            if (_hasOrderColumn == null)
            {
                _hasOrderColumn = false;
                foreach (PropertyInfo param in props)
                {
                    if (param.Name == "Order")
                    {
                        _hasOrderColumn = true;
                        break;
                    }
                }
            }

            return (bool)_hasOrderColumn;
        }

        public bool HasManyManyOrder(PropertyInfo param)
        {
            bool hasOrder = false;

            object[] dbAttrs = param.GetCustomAttributes(typeof(DBAttribute), false);

            foreach (DBAttribute db in dbAttrs)
            {
                if (db.HasOrder)
                {
                    hasOrder = true;
                    break;
                }
            }           

            return hasOrder;
        }

        public string DeleteOrphanSQL(string parentKey, long parentId, long[] primaryIds)
        {
            Type type = typeof(T);

            string tableName = type.Name;

            StringBuilder sb = new StringBuilder();

            string primaryKey = FindPrimaryKey();

            sb.AppendFormat("DELETE FROM [{3}].[{0}] WHERE {1} = {2} ", tableName, parentKey, parentId.ToString(), _sqlSchema);
            foreach (long id in primaryIds)
            {
                sb.AppendFormat("AND {0} != {1} ", tableName+"ID", id.ToString());
            }

            sb.Append(";");

            return sb.ToString();
        }

        public string DeleteManyManyOrphanSQL(string parentKey, long parentId, long[] primaryIds, string tableName, string primaryKey)
        {
            Type type = typeof(T);

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("DELETE FROM {0} WHERE {1} = {2} ", tableName, parentKey, parentId.ToString());
            foreach (long id in primaryIds)
            {
                sb.AppendFormat("AND {0} != {1} ", primaryKey, id.ToString());
            }

            sb.Append(";");

            return sb.ToString();
        }
    }
}

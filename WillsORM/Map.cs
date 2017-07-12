using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

namespace WillsORM
{
    public class Map<T>
    {
        private DataTable schemaTable = null;

        public IList<T> MapCollection(IDataReader reader, PropertyInfo[] props)
        {
            IList<T> collection = new List<T>();
            using (schemaTable = reader.GetSchemaTable())
            {
                while (reader.Read())
                {
                    collection.Add(MapRecord(reader, props));
                }
            }

            return collection;
        }

        public T MapRecord(IDataRecord record, PropertyInfo[] props)
        {
            Type type = typeof(T);

            T obj = (T)Activator.CreateInstance(type);

            string[] columnNames = new string[schemaTable.Rows.Count];

            int index = 0;
            foreach (DataRow dr in schemaTable.Rows)
            {
                columnNames[index] = dr["ColumnName"].ToString();
                index++;
            }

            foreach (PropertyInfo prop in props)
            {
                if (prop.PropertyType.Namespace != "System.Collections.Generic")
                {
                    if (prop.PropertyType.BaseType.Name != "DataObj")
                    {
                        if (Array.IndexOf(columnNames, prop.Name) > -1)
                        {
                            if (record[prop.Name] != DBNull.Value)
                            {
                                prop.SetValue(obj, record[prop.Name], null);
                            }
                        }
                    }
                }
            }

            return obj;
        }
    }
}

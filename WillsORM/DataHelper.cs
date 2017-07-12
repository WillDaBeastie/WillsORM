using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Reflection;
using System.Web;

namespace WillsORM
{
    public class DataHelper
    {
        //private static bool _useStoredProcs = false;// bool.Parse(new AppSettingsReader().GetValue("useSP", typeof(String)).ToString());

        public static SqlCommand CreateSelectParams(SqlCommand command, Dictionary<string, object> nameVals)
        {
            foreach (KeyValuePair<string, object> param in nameVals)
            {
                SqlDbType dbType = FindParamType(param.Value.GetType().Name);

                SqlParameter myParam = command.Parameters.Add("@" + param.Key, dbType);
                myParam.Value = param.Value;
            }

            return command;
        }

        public static PropertyInfo[] getCachedProps(Type type)
        {
            PropertyInfo[] props;

            if (HttpRuntime.Cache[type.Name + "-Props"] != null)
            {
                props = (PropertyInfo[])HttpRuntime.Cache[type.Name + "-Props"];
            }
            else
            {
                props = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
                if (typeof(WillsORM.DataObj).IsAssignableFrom(type.BaseType) && type.BaseType != typeof(WillsORM.DataObj))
                {
                    PropertyInfo[] newProps = getCachedProps(type.BaseType);
                    IList<PropertyInfo> combinedProps = new List<PropertyInfo>(props.Concat<PropertyInfo>(newProps));
                    props = combinedProps.ToArray();
                }
                HttpRuntime.Cache.Add(type.Name + "-Props", props, null, DateTime.Now.AddYears(1), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
            }

            return props;
        }

        public static SqlDbType FindParamType(string typeName)
        {
            SqlDbType dbType = SqlDbType.VarChar;

            switch (typeName)
            {
                case ("Boolean"):
                    dbType = SqlDbType.Bit;
                    break;
                case ("DateTime"):
                    dbType = SqlDbType.DateTime;
                    break;
                case ("Decimal"):
                    dbType = SqlDbType.Decimal;
                    break;
                case ("Double"):
                    dbType = SqlDbType.Float;
                    break;
                case ("Int32"):
                    dbType = SqlDbType.Int;
                    break;
                case ("Int64"):
                    dbType = SqlDbType.BigInt;
                    break;
                case ("String"):
                    dbType = SqlDbType.VarChar;
                    break;
                case ("Xml"):
                    dbType = SqlDbType.Xml;
                    break;
                default:
                    dbType = SqlDbType.BigInt;
                    break;
            }

            return dbType;
        }

        public static Attribute FindAttribute(Type attrType, Type thing) //typeof(TableAttribute) -- T = TableAttribute
        {
            Attribute output = null;

            object[] custAttribs = thing.GetCustomAttributes(false);

            if (custAttribs != null)
                output = ((Attribute)custAttribs.FirstOrDefault(x => x.GetType() == attrType));
            
            return output;
        }

        public static T FindAttribute<T>(PropertyInfo thing) 
        {
            T output = default(T);

            object[] custAttribs = thing.GetCustomAttributes(false);

            if (custAttribs != null)
                output = ((T)custAttribs.FirstOrDefault(x => x.GetType() == typeof(T)));

            return output;
        }
    }
}

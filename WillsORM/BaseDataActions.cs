using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;



namespace WillsORM
{
	public class BaseDataActions<T> : IData<T>
    {
        public static bool _useStoredProcs = false; //bool.Parse(new AppSettingsReader().GetValue("useSP", typeof(String)).ToString());
        private Connection _conn;
        private PropertyInfo[] _props;
        private CommandHelper<T> _comHelp;


		public BaseDataActions()
		{
		}

		public BaseDataActions(string connStringName)
		{
			conn.ConnectionStringName = connStringName;
		}


        protected CommandHelper<T> commHelper
        {
            get
            {
                if (_comHelp == null)
                {
                    _comHelp = new CommandHelper<T>();
                    _comHelp.props = props;
                    _comHelp.SqlSchema = _sqlSchema;
                }
                return _comHelp;
            }
        }

        private string _sqlSchema = "dbo";

        protected string SqlSchema
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

        public Connection conn
        {
            get
            {
                if (_conn == null)
                {
                    _conn = new Connection();
                }
                return _conn;
            }
        }

        protected PropertyInfo[] props
        {
            get 
            {
                if (_props == null)
                {
                    Type type = typeof(T);
                    _props = DataHelper.getCachedProps(type);
                }

                return _props;
            }
        }

        private Dictionary<string, object> _addedParameters;
        public Dictionary<string, object> AddedParameters
        {
            get 
            {
                if (_addedParameters == null)
                {
                    _addedParameters = new Dictionary<string, object>();
                }
                return _addedParameters;
            }
            set
            {
                _addedParameters = value;
            }
        }

        private int _selectLevels = 0;
        public int SelectLevels
        {
            get { return _selectLevels;  }
            set { _selectLevels = value;  }
        }

        public T Create(T obj) 
        {
            Type type = typeof(T);

            TableAttribute tAttr = (TableAttribute)DataHelper.FindAttribute(typeof(TableAttribute), type);

            bool isReadOnly = false;
            if (tAttr != null && tAttr.ReadOnly)
                isReadOnly = true;

            if (!isReadOnly)
            {

                ascendDataActionForChildProps(obj, true);

                commHelper.addedParameters = AddedParameters;

                
                // While deving use SQL. Stored Procs can be used later on after all params are solid. TODO:

                SqlCommand insertCMD = new SqlCommand();
                if (_useStoredProcs)
                {
                    insertCMD.CommandText = type.Name + "_Insert";
                    insertCMD.CommandType = CommandType.StoredProcedure;
                }
                else
                {
                    insertCMD.CommandText = commHelper.CreateInsertSQL();
                    insertCMD.CommandType = CommandType.Text;
                }
                IList<T> coll;
                using (insertCMD.Connection = conn.ConnObj)
                {

                    insertCMD = commHelper.CreateObjectParams(insertCMD, obj, false);

                    insertCMD.Connection.Open();

                    SqlDataReader reader = insertCMD.ExecuteReader();

                    Map<T> map = new Map<T>();

                    coll = map.MapCollection(reader, props);

                }

                T newObj = coll[0];

                string primaryKey = commHelper.FindPrimaryKey();
                object primaryVal = newObj.GetType().GetProperty(primaryKey).GetValue(newObj, null);
                obj.GetType().GetProperty(primaryKey).SetValue(obj, primaryVal, null);

                cascadeDataActionForChildProps(obj, "Create");
            }

            return obj;
        }

        private void ascendDataActionForChildProps(T obj, bool isUpdate)
        {
            foreach (PropertyInfo param in props)
            {
                object[] dbAttrs = param.GetCustomAttributes(typeof(DBAttribute), false);

                foreach (DBAttribute db in dbAttrs)
                {
                    if (db.RelationType == DBAttribute.RelationTypes.ManyToOne)
                    {
                        if (param.PropertyType.BaseType.Name == "DataObj")
                        {
                            Dictionary<string, object> foreign = getForeignKey(param, obj, isUpdate);
                            if (foreign["foreignKeyValue"] != null)
                            {
                                if (long.Parse(foreign["foreignKeyValue"].ToString()) > 0)
                                {
                                    AddedParameters[foreign["foreignKey"].ToString()] = (long)foreign["foreignKeyValue"];
                                }
                            }
                            else
                            {
                              //  AddedParameters.Remove((string)foreign["foreignKey"]); // Never lets a null value overwrite a non null value...
                                AddedParameters[foreign["foreignKey"].ToString()] = null;
                            }
                        }
                    }
                }
            }
        }
        
        private void cascadeDataActionForChildProps(T obj, string dataAction)
        {
            foreach (PropertyInfo param in props)
            {
                object[] dbAttrs = param.GetCustomAttributes(typeof(DBAttribute), false);

                foreach (DBAttribute db in dbAttrs)
                {
                    if (db.RelationType == DBAttribute.RelationTypes.OneToMany)
                    {
                        string primaryKey = commHelper.FindPrimaryKey();
                        object primaryKeyVal = obj.GetType().GetProperty(primaryKey).GetValue(obj, null);

                        if (param.PropertyType.IsGenericType && param.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                        {
                            // do a collection cascade
                            //  loop though collection. if exists do update if not insert

                            Type itemType = param.PropertyType.GetGenericArguments()[0];
                            string childName = itemType.Name;

                            object paramCol = param.GetValue(obj, null);

                            if (dataAction == "Delete")
                            {
                                ((IList)paramCol).Clear();
                            }

                            if (paramCol != null)
                            {
                                object dataObj = FindModelRepository(itemType);
                                PropertyInfo dataSelectLevels = dataObj.GetType().GetProperty("SelectLevels");
                                dataSelectLevels.SetValue(dataObj, _selectLevels - 1, null);

                                if (_selectLevels > 0)
                                {
                                    MethodInfo updateCollection = dataObj.GetType().GetMethod("UpdateCollection", BindingFlags.NonPublic | BindingFlags.Instance);
                                    object newObj = updateCollection.Invoke(dataObj, new[] { paramCol, obj.GetType().Name + "ID", primaryKeyVal });
                                }
                            }
                        }
                        else if (param.PropertyType.BaseType.Name == "DataObj" && dataAction != "Delete")
                        {
                            cascadeDataAction(obj, param, dataAction, null, primaryKey, long.Parse(primaryKeyVal.ToString()));
                        }
                    }
                    else if (db.RelationType == DBAttribute.RelationTypes.ManyToOne && dataAction != "Delete")
                    {
                       // string primaryKey = commHelper.FindPrimaryKey();
                       // object primaryKeyVal = obj.GetType().GetProperty(primaryKey).GetValue(obj, null);

                        if (param.PropertyType.BaseType.Name == "DataObj" && dataAction == "Insert")
                        {
                            cascadeDataAction(obj, param, dataAction, null, string.Empty, 0);
                        }
                    }
                    else if (db.RelationType == DBAttribute.RelationTypes.ManyToMany)
                    {
                        string tableName = string.Empty; 

                        foreach (DBAttribute dbAtr in dbAttrs)
                        {
                            if (dbAtr.TableName != null)
                            {
                                tableName = dbAtr.TableName;
                            }
                        }

                        string primaryKey = commHelper.FindPrimaryKey();
                        object primaryKeyVal = obj.GetType().GetProperty(primaryKey).GetValue(obj, null);

                        if (param.PropertyType.IsGenericType && param.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                        {
                            // do a collection cascade
                            //  loop though collection. if exists do update if not insert

                            Type itemType = param.PropertyType.GetGenericArguments()[0];

                            object paramCol = param.GetValue(obj, null);
                            if (dataAction == "Delete")
                            {
                                ((IList)paramCol).Clear();
                            }

                            object dataObj = FindModelRepository(itemType);
                            PropertyInfo dataSelectLevels = dataObj.GetType().GetProperty("SelectLevels");
                            dataSelectLevels.SetValue(dataObj, _selectLevels - 1, null);

                            if (_selectLevels > 0)
                            {
                                bool hasOrder = commHelper.HasManyManyOrder(param);
                                MethodInfo updateCollection = dataObj.GetType().GetMethod("UpdateCollectionManyMany", BindingFlags.NonPublic | BindingFlags.Instance);
                                object newObj = updateCollection.Invoke(dataObj, new[] { paramCol, obj.GetType().Name + "ID", primaryKeyVal, tableName, hasOrder});
                            }

                        }
                    }
                }
            }
        }

        protected void UpdateCollection(IList<T> collection, string parentKey, long parentId)
        {
            Dictionary<string, object> addedParam = new Dictionary<string, object>() { { parentKey, parentId } };

            AddedParameters = addedParam;
            
            string primaryKey = commHelper.FindPrimaryKey();

            long[] primaryIds = new long[collection.Count];
            int cnt = 0;

            bool hasOrder = commHelper.HasOrder();

            if (collection.Count > 0)
            { 
                Type thingType = collection[0].GetType();

                foreach (var thing in collection)
                {
                    object primaryKeyVal = thingType.GetProperty(primaryKey).GetValue(thing, null);

                    if (hasOrder)
                    {
                        thingType.GetProperty("Order").SetValue(thing, cnt + 1, null);
                    }

                    if (Int64.Parse(primaryKeyVal.ToString()) > 0)
                    {
                        Update(thing);
                        primaryIds[cnt] = long.Parse(primaryKeyVal.ToString());
                    }
                    else
                    {
                        object newObj = Create(thing);
                        primaryIds[cnt] = long.Parse(thingType.GetProperty(primaryKey).GetValue(newObj, null).ToString());
                    }
                    cnt++;
                }
            }

            //Delete orphaned children

            SqlCommand updateCMD = new SqlCommand();

            updateCMD.CommandText = commHelper.DeleteOrphanSQL(parentKey, parentId, primaryIds);
            updateCMD.CommandType = CommandType.Text;

            using (updateCMD.Connection = conn.ConnObj)
            {
                updateCMD.Connection.Open();
                updateCMD.ExecuteNonQuery();
            }

        }

        protected void UpdateCollectionManyMany(IList<T> collection, string parentKey, long parentId, string manyTableName, bool hasOrder)
        {
            string primaryKey = commHelper.FindPrimaryKey();

            long[] primaryIds = new long[collection.Count];
            int cnt = 0;

            foreach (var thing in collection)
            {
                object primaryKeyVal = thing.GetType().GetProperty(primaryKey).GetValue(thing, null);

                if (Int64.Parse(primaryKeyVal.ToString()) > 0)
                {
                    Update(thing); // Needed to delete or add to collections like imageVersions
                    primaryIds[cnt] = long.Parse(primaryKeyVal.ToString());                    
                }
                else
                {
                    object newObj = Create(thing);
                    primaryIds[cnt] = long.Parse(newObj.GetType().GetProperty(primaryKey).GetValue(newObj, null).ToString());
                }
                cnt++;
            }

            Type type = typeof(T);
            TableAttribute tAttr = (TableAttribute)DataHelper.FindAttribute(typeof(TableAttribute), type);

            bool isReadOnly = false;
            if (tAttr != null && tAttr.ReadOnly)
                isReadOnly = true;


            if (!isReadOnly)
            {
                if (primaryIds.Length > 0)
                {
                    SqlCommand updateCMD = new SqlCommand();

                    if (hasOrder)
                    {
                        updateCMD.CommandText = commHelper.CreateInsertManyManyOrderSQL(parentKey, parentId, primaryIds, manyTableName);
                    }
                    else
                    {
                        updateCMD.CommandText = commHelper.CreateInsertManyManySQL(parentKey, parentId, primaryIds, manyTableName);
                    }

                    updateCMD.CommandType = CommandType.Text;

                    using (updateCMD.Connection = conn.ConnObj)
                    {
                        updateCMD.Connection.Open();
                        updateCMD.ExecuteNonQuery();
                    }
                }


                //Delete orphaned children

                SqlCommand deleteCMD = new SqlCommand();

                deleteCMD.CommandText = commHelper.DeleteManyManyOrphanSQL(parentKey, parentId, primaryIds, manyTableName, primaryKey);
                deleteCMD.CommandType = CommandType.Text;

                using (deleteCMD.Connection = conn.ConnObj)
                {
                    deleteCMD.Connection.Open();
                    deleteCMD.ExecuteNonQuery();
                }
            }
        }

        private object cascadeDataAction(T obj, PropertyInfo param, string dataAction, object[] args, string foreignkey, long foreignID)
        {
     
            Type childType = param.PropertyType;

            object dataObj = FindModelRepository(childType);

            PropertyInfo dataSelectLevels = dataObj.GetType().GetProperty("SelectLevels");
            dataSelectLevels.SetValue(dataObj, _selectLevels - 1, null);

            if (!string.IsNullOrEmpty(foreignkey))
            {
                Dictionary<string, object> addedParams = new Dictionary<string, object>() { { foreignkey, foreignID } };

                dataObj.GetType().GetProperty("AddedParameters").SetValue(dataObj, addedParams, null);
            }

            object newObj = null;
            if (args != null)
            {
                newObj = dataObj.GetType().InvokeMember(dataAction, BindingFlags.InvokeMethod, null, dataObj, args);
            }
            else
            {
                object childObj = param.GetValue(obj, null);
                if (childObj == null) // was !=
                { 
                    newObj = dataObj.GetType().InvokeMember(dataAction, BindingFlags.InvokeMethod, null, dataObj, new[] { childObj });
                }
            }

            if (newObj != null && dataAction == "Insert")
            {
                string varName = childType.Name + "ID";
                object newId = childType.GetProperty(varName).GetValue(newObj, null);

                // add to name and value to private dictionary for cascading insert so the original can pick it up.

                AddedParameters.Add(varName, newId);
            }

            return newObj;
        }

        private object FindModelRepository(Type itemType)
        {
            object dataObj = null;

            string name = itemType.Name;

            foreach (Type t in Assembly.GetAssembly(itemType).GetTypes()) // was GetExecutingAss UPDATE
            {
                if(t.BaseType != null)
                {
                    if (t.BaseType.Name == "BaseDataActions`1")
                    {
                        foreach (var genParAtts in t.BaseType.GetGenericArguments())
                        {
							if (genParAtts.Name == name)
                            {
                                ConstructorInfo cInfo = t.GetConstructor(new Type[]{typeof(string)});
                                if (cInfo != null)
                                {
                                    dataObj = Activator.CreateInstance(t, _conn.ConnectionStringName);
                                }
                                else
                                {
                                    dataObj = Activator.CreateInstance(t);
                                }
                                break;
                            }
                        }
                        if (dataObj != null)
                        {
                            break;
                        }
                    }
                }
            }

            return dataObj;
        }


        public bool Update(T obj) 
        {
            bool ret = false;

            Type type = typeof(T);

            TableAttribute tAttr = (TableAttribute)DataHelper.FindAttribute(typeof(TableAttribute), type);

            bool isReadOnly = false;
            if (tAttr!= null && tAttr.ReadOnly)
                isReadOnly = true;
            

            if (!isReadOnly)
            {
                //AddedParameters.Clear();
                ascendDataActionForChildProps(obj, true);
                commHelper.addedParameters = AddedParameters;

                cascadeDataActionForChildProps(obj, "Update");


                // While deving use SQL. Stored Procs can be used later on after all params are solid.


                // TODO: if Object DB attribute CascadeWrites = false, don't update man. 

                SqlCommand updateCMD = new SqlCommand();
                if (_useStoredProcs)
                {
                    updateCMD.CommandText = type.Name + "_Update";
                    updateCMD.CommandType = CommandType.StoredProcedure;
                }
                else
                {
                    updateCMD.CommandText = commHelper.CreateUpdateSQL();
                    updateCMD.CommandType = CommandType.Text;
                }
                int rowsAffected = 0;
                using (updateCMD.Connection = conn.ConnObj)
                {

                    updateCMD = commHelper.CreateObjectParams(updateCMD, obj, true);

                    updateCMD.Connection.Open();

                    rowsAffected = updateCMD.ExecuteNonQuery();

                }

                if (rowsAffected > 0)
                {
                    ret = true;
                }
            }

            return ret;
        }


        public bool UpdateWConcurrency(T obj, T original)
        {
            ascendDataActionForChildProps(obj, true);
            commHelper.addedParameters = AddedParameters;

            bool ret = false;

            Type type = typeof(T);

            // While deving use SQL. Stored Procs can be used later on after all params are solid.

            SqlCommand updateCMD = new SqlCommand();
            if (_useStoredProcs)
            {
                updateCMD.CommandText = type.Name + "_UpdateWConcurrency";
                updateCMD.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                updateCMD.CommandText = commHelper.CreateUpdateWConcurrencySQL(original);
                updateCMD.CommandType = CommandType.Text;
            }

            int rowsAffected = 0;
            using (updateCMD.Connection = conn.ConnObj)
            {

                updateCMD = commHelper.CreateObjectParams(updateCMD, obj, original, true);

                updateCMD.Connection.Open();

                rowsAffected = updateCMD.ExecuteNonQuery();

            }

            if (rowsAffected > 0)
            {
                ret = true;
                cascadeDataActionForChildProps(obj, "Update");
            }

            return ret;
        }

        public T GetByID(long ID)
        {
            SqlDataReader reader;

            Type type = typeof(T);

            // While deving use SQL. Stored Procs can be used later on after all params are solid.

            SqlCommand selectCMD = new SqlCommand();
            if (_useStoredProcs)
            {
                selectCMD.CommandText = type.Name + "_SelectByID";
                selectCMD.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                selectCMD.CommandText = commHelper.CreateSelectByIDSQL();
                selectCMD.CommandType = CommandType.Text;
            }

            Map<T> map ;

            IList<T> coll;

            using (selectCMD.Connection = conn.ConnObj)
            {

                Dictionary<string, object> paramDict = new Dictionary<string, object>();
                paramDict.Add(commHelper.FindPrimaryKey(), ID);

                selectCMD = DataHelper.CreateSelectParams(selectCMD, paramDict);

                selectCMD.Connection.Open();

                using (reader = selectCMD.ExecuteReader())
                {
                    map = new Map<T>();
                    coll = map.MapCollection(reader, props);
                }

            }

            if (coll.Count > 0)
            {
                T returnobj = coll[0];

                if (_selectLevels > 0)
                {
                    attachChildren(ref returnobj);
                }

                return returnobj;
            }
            else
            {
                return (T)Activator.CreateInstance(typeof(T), null);
            }
        }

        private void attachChildren(ref T t)
        {
            string primaryKey = commHelper.FindPrimaryKey();
            object primaryKeyVal = t.GetType().GetProperty(primaryKey).GetValue(t, null);

            foreach(PropertyInfo prop in _props)
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    // do collection cascade
                    Type itemType = prop.PropertyType.GetGenericArguments()[0];

                    Dictionary<string, object> wheres = new Dictionary<string, object>();
                    wheres.Add(primaryKey, primaryKeyVal);

                    object dataObj = FindModelRepository(itemType);

                    PropertyInfo dataSelectLevels = dataObj.GetType().GetProperty("SelectLevels");
                    dataSelectLevels.SetValue(dataObj, _selectLevels - 1, null);

                    object[] dbAttrs = prop.GetCustomAttributes(typeof(DBAttribute), false);

                    foreach (DBAttribute db in dbAttrs)
                    {
                        if (db.RelationType == DBAttribute.RelationTypes.OneToMany)
                        {
                            object newObj = dataObj.GetType().InvokeMember("GetMany", BindingFlags.InvokeMethod, null, dataObj, new[] { wheres });
                            prop.SetValue(t, newObj, null);
                        }
                        else if (db.RelationType == DBAttribute.RelationTypes.ManyToMany)
                        {
                            string tableName = string.Empty;
                            foreach (DBAttribute db2 in dbAttrs)
                            {
                                if (db2.TableName != null)
                                {
                                    tableName = db2.TableName;
                                }
                            }

                            Dictionary<string, bool> orders = new Dictionary<string, bool>();

                            bool hasOrder = commHelper.HasManyManyOrder(prop);
                            if(hasOrder)
                            {
                                orders.Add("Order", true);
                            }

                            MethodInfo manyManyMethod = dataObj.GetType().GetMethod("GetManyMany", BindingFlags.NonPublic | BindingFlags.Instance);
                            object newObj = manyManyMethod.Invoke(dataObj, new object[] { wheres, orders, tableName });

                            prop.SetValue(t, newObj, null);
                        }
                    }
                }
                else if (prop.PropertyType.BaseType.Name == "DataObj")
                {
                    object[] dbAttrs = prop.GetCustomAttributes(typeof(DBAttribute), false);

                    foreach (DBAttribute db in dbAttrs)
                    {
                        if (db.RelationType == DBAttribute.RelationTypes.ManyToOne)
                        {
                            Dictionary<string, object> foreign = getForeignKey(prop, t, false);
                            if (foreign["foreignKeyValue"] != null)
                            {
                                if (long.Parse(foreign["foreignKeyValue"].ToString()) > 0)
                                {
                                    Dictionary<string, object> wheres = new Dictionary<string, object>();
                                    wheres.Add((string)foreign["foreignKey"], (long)foreign["foreignKeyValue"]);

                                    object newObj = cascadeDataAction(t, prop, "GetSingle", new[] { wheres }, null, 0);

                                    t.GetType().GetProperty(prop.Name).SetValue(t, newObj, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void attachChildren(ref T t, IDataReader reader)
        {
            string primaryKey = commHelper.FindPrimaryKey();
            object primaryKeyVal = t.GetType().GetProperty(primaryKey).GetValue(t, null);

            foreach (PropertyInfo prop in _props)
            {
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IList<>)
                    || prop.PropertyType.BaseType.Name == "DataObj")
                {
                    // do collection cascade
                    Type childType = prop.PropertyType.GetGenericArguments()[0];
                    
                    object map = typeof(Map<>).MakeGenericType(childType).GetConstructor(Type.EmptyTypes).Invoke(null);

                    MethodInfo mapCollection = map.GetType().GetMethod("MapCollection");
                    object newObj = mapCollection.Invoke(map, new object[] { reader, DataHelper.getCachedProps(childType) });

                    //object newObj = dataObj.GetType().InvokeMember("GetMany", BindingFlags.InvokeMethod, null, dataObj, new[] { wheres });
                    prop.SetValue(t, newObj, null);

                }
            }
        }

        public Dictionary<string, object> getForeignKey(PropertyInfo param, object obj, bool isUpdate)
        {
            Dictionary<string, object> returnVals = new Dictionary<string, object>();
            object foreignObj = param.GetValue(obj, null);
            Type foreignType = param.PropertyType;
            PropertyInfo[] foreignProps = DataHelper.getCachedProps(foreignType);
            string foreignKey = findForeignKey(foreignProps);
            returnVals.Add("foreignKey",foreignKey);
            returnVals.Add("foreignKeyValue", null);

            if (foreignObj == null && !isUpdate)
            {
                // Get foreign key value from db

                string primaryKey = commHelper.FindPrimaryKey();
                object primaryVal = obj.GetType().GetProperty(primaryKey).GetValue(obj, null);

                Dictionary<string, object> commVals = new Dictionary<string, object>();
                commVals.Add(primaryKey, primaryVal);

                SqlDataReader reader;
                string cmdSql = commHelper.CreateForeignIDSQL(foreignKey);

                SqlCommand com = new SqlCommand(cmdSql, conn.ConnObj);
                if (_useStoredProcs)
                {
                    com.CommandType = CommandType.StoredProcedure;
                }
                else
                {
                    com.CommandType = CommandType.Text;
                }
                    
                com = DataHelper.CreateSelectParams(com, commVals);

                using (com.Connection = conn.ConnObj)
                {
                    com.Connection.Open();

                    using (reader = com.ExecuteReader())
                    {
                        long forId = 0;
                        while (reader.Read())
                        {
                            if (reader[foreignKey] != DBNull.Value)
                            {
                                forId = long.Parse(reader[foreignKey].ToString());
                                returnVals["foreignKeyValue"] = forId;
                                break;
                            }
                        }
                    }
                }
            }
            else if (foreignObj != null)
            {
                foreach(PropertyInfo propInfo in foreignProps)
                {
                    if(propInfo.Name == foreignKey)
                    {
                        object foreignKeyVal = propInfo.GetValue(foreignObj, null);
                        if (foreignKeyVal != null)
                        {
                            long forId = long.Parse(foreignKeyVal.ToString());
                            if (forId > 0)
                            {
                                returnVals["foreignKeyValue"] = forId;
                            }
                        }
                        break;
                    }
                }
            }

            return returnVals;
        }

        private string findForeignKey(PropertyInfo[] props)
        {
            string foreignKey = string.Empty;

            foreach (PropertyInfo param in props)
            {
                foreach( DBAttribute db in param.GetCustomAttributes(typeof(DBAttribute), false))
                {
                    if (db.IsPrimary == true)
                    {
                        foreignKey = param.Name;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(foreignKey))
                {
                    break;
                }
            }
            return foreignKey;
        }

        public T GetByID(int ID)
        {
            return GetByID((long)ID);
        }

        public bool DeleteByID(long ID)
        {
            Type type = typeof(T);
            TableAttribute tAttr = (TableAttribute)DataHelper.FindAttribute(typeof(TableAttribute), type);

            bool isReadOnly = false;
            if (tAttr != null && tAttr.ReadOnly)
                isReadOnly = true;

            int rows = 0;
            if (!isReadOnly)
            {
                _selectLevels = 10;
                cascadeDataActionForChildProps(GetByID(ID), "Delete");

                SqlCommand selectCMD = new SqlCommand();
                if (_useStoredProcs)
                {
                    selectCMD.CommandText = type.Name + "_Delete";
                    selectCMD.CommandType = CommandType.StoredProcedure;
                }
                else
                {
                    selectCMD.CommandText = commHelper.CreateDeleteByIDSQL();
                    selectCMD.CommandType = CommandType.Text;
                }

                using (selectCMD.Connection = conn.ConnObj)
                {

                    Dictionary<string, object> paramDict = new Dictionary<string, object>();
                    paramDict.Add(commHelper.FindPrimaryKey(), ID);

                    selectCMD = DataHelper.CreateSelectParams(selectCMD, paramDict);

                    selectCMD.Connection.Open();

                    rows = selectCMD.ExecuteNonQuery();

                }

            }
            if (rows > 0){ return true; }
            else { return false; }
        }

        public bool DeleteByID(int ID)
        {
            return DeleteByID((long)ID);
        }

        public T GetSingle(Dictionary<string, object> whereVals)
        {
            IList<T> things = GetMany(whereVals, new Dictionary<string, bool>());

            if (things.Count>0)
            {
                return things[0];
            }
            else
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
        }

        public IList<T> GetMany(Dictionary<string, object> whereVals)
        {
            return GetMany(whereVals, new Dictionary<string, bool>());
        }

        public IList<T> GetMany(Dictionary<string, object> whereVals, Dictionary<string, bool> orderBy)
        {
            SqlDataReader reader;

            Type type = typeof(T);

            if (commHelper.HasOrder())
            {
                if (!orderBy.ContainsKey("Order"))
                {
                    orderBy.Add("Order", true);
                }
            }

            // While deving use SQL. Stored Procs can be used later on after all params are solid.

            SqlCommand selectCMD = new SqlCommand();
            if (_useStoredProcs)
            {
                selectCMD.CommandText = type.Name + "_SelectMany";
                selectCMD.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                selectCMD.CommandText = commHelper.CreateSelectByParamsSQL(whereVals, orderBy);
                selectCMD.CommandType = CommandType.Text;
            }

            IList<T> coll;

            using (selectCMD.Connection = conn.ConnObj)
            {

                Dictionary<string, object> vals = new Dictionary<string, object>();
                if (whereVals != null)
                { vals = whereVals; }

                selectCMD = DataHelper.CreateSelectParams(selectCMD, vals);

                selectCMD.Connection.Open();

                using (reader = selectCMD.ExecuteReader())
                {
                    Map<T> map = new Map<T>();

                    coll = map.MapCollection(reader, props);
                }

                for (int x = 0; x < coll.Count; x++)
                {
                    T thing = coll[x];
                    if (_selectLevels > 0)
                    {
                        attachChildren(ref thing);
                    }
                    coll[x] = thing;
                }

            }

            return coll;
        }

        protected IList<T> GetManyMany(Dictionary<string, object> whereVals, Dictionary<string, bool> orderBy, string tableName)
        {
            SqlDataReader reader;

            Type type = typeof(T);

            // While deving use SQL. Stored Procs can be used later on after all params are solid.

            SqlCommand selectCMD = new SqlCommand();
            if (_useStoredProcs)
            {
                selectCMD.CommandText = type.Name + "_SelectManyMany";
                selectCMD.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                int keysNum = 0;
                if (whereVals != null)
                {
                    keysNum = whereVals.Keys.Count;
                }

                string[] pars = new string[keysNum];
                int cnt = 0;
                if (keysNum > 0)
                {
                    foreach (string str in whereVals.Keys)
                    {
                        pars[cnt] = str;
                        cnt++;
                    }
                }

                selectCMD.CommandText = commHelper.CreateSelectJoinByParamsSQL(pars, orderBy, tableName);
                selectCMD.CommandType = CommandType.Text;
            }

            IList<T> coll;

            using (selectCMD.Connection = conn.ConnObj)
            {

                Dictionary<string, object> vals = new Dictionary<string, object>();
                if (whereVals != null)
                { vals = whereVals; }

                selectCMD = DataHelper.CreateSelectParams(selectCMD, vals);

                selectCMD.Connection.Open();

                using (reader = selectCMD.ExecuteReader()) 
                { 
                    Map<T> map = new Map<T>();

                    coll = map.MapCollection(reader, props);
                }

                for (int x = 0; x < coll.Count; x++)
                {
                    T thing = coll[x];
                    if (_selectLevels > 0)
                    {
                        attachChildren(ref thing);
                    }
                    coll[x] = thing;
                }

            }

            return coll;
        }

        public IList<T> GetMany(string procedure, SqlParameter[] parameters)
        {
            Type type = typeof(T);

            SqlCommand selectCMD = new SqlCommand();
            selectCMD.CommandText = procedure;
            selectCMD.CommandType = CommandType.StoredProcedure;
            selectCMD.Connection = conn.ConnObj;

            if (parameters != null)
            {
                selectCMD.Parameters.AddRange(parameters);
            }

            SqlDataReader reader = null;
            IList<T> coll = null;

            try
            {
                selectCMD.Connection.Open();

                using (reader = selectCMD.ExecuteReader())
                {

                    Map<T> map = new Map<T>();

                    coll = map.MapCollection(reader, props);
                }

                for (int x = 0; x < coll.Count; x++)
                {
                    T thing = coll[x];
                    if (_selectLevels > 0)
                    {
                        attachChildren(ref thing);
                    }
                    coll[x] = thing;
                }
            }
			catch(Exception ex)
			{
			// log
			}
            finally
            {
                if (reader != null) reader.Close();
                selectCMD.Connection.Close();
            }

            return coll;
        }

        public T GetSingle(string procedure, SqlParameter[] parameters)
        {
            Type type = typeof(T);

            SqlCommand selectCMD = new SqlCommand();
            selectCMD.CommandText = procedure;
            selectCMD.CommandType = CommandType.StoredProcedure;
            selectCMD.Connection = conn.ConnObj;

            if (parameters != null)
            {
                selectCMD.Parameters.AddRange(parameters);
            }

            T single = default(T);

            try
            {
                selectCMD.Connection.Open();

                using (SqlDataReader reader = selectCMD.ExecuteReader())
                {

                    Map<T> map = new Map<T>();

                    single = map.MapCollection(reader, props).SingleOrDefault();
               
                    if (_selectLevels > 0)
                    {
                        // Only works if the next result is the only next level GET for this object as it will blindly get the next collection/generic property.
                        if (reader.NextResult())
                            attachChildren(ref single, reader);
                        else
                            attachChildren(ref single);
                    }
                }
            }
			catch (Exception ex)
			{
				// log
			}
            finally
            {
               // if (reader != null) reader.Close();
                selectCMD.Connection.Close();
                selectCMD.Connection.Dispose();
            }

            return single;
        }

        public void Execute(string procedure, SqlParameter[] parameters)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = procedure;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn.ConnObj;

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            try
            {
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
            }
			catch (Exception ex)
			{
				//log
			}
            finally
            {
                cmd.Connection.Close();
            }
        }

        public object ExecuteScalar(string procedure, SqlParameter[] parameters)
        {
            object thing = null;
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = procedure;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn.ConnObj;

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            try
            {
                cmd.Connection.Open();
                thing = cmd.ExecuteScalar();
            }
			catch (Exception ex)
			{
			  // log
			}
            finally
            {
                cmd.Connection.Close();
            }

            return thing;
        }
    }
}

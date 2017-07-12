using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.Caching;

namespace WillsORM
{
	public class BaseDataActionsFake<T> : IData<T>
    {

        private MemoryCache _cacheStore;

        public BaseDataActionsFake()
        {
            _cacheStore = new MemoryCache("WillsORMFake");
        }

        protected List<T> CacheStore
        {
            get
            {
                Type type = typeof(T);
                if (_cacheStore[type.Name] == null)
                {
                    List<T> list = new List<T>();
                    _cacheStore.Add(type.Name, list, DateTimeOffset.Now.AddHours(1));
                }
                return (List<T>)_cacheStore[type.Name];
            }
        }

        private int _selectLevels = 0;
        public int SelectLevels
        {
            get { return _selectLevels; }
            set { _selectLevels = value; }
        }

        public T Create(T obj)
        {
            Type type = typeof(T);
            int primID = (int)type.GetProperty(type.Name + "ID").GetValue(obj, null);
            if (primID == 0)
            {
                type.GetProperty(type.Name + "ID").SetValue(obj, CreateRandomID(), null);

            }
            CacheStore.Add(obj);
            
            return obj;
        }

		private int CreateRandomID()
		{
			Random random = new Random();
			return random.Next(1000);
		}


		public bool DeleteByID(long ID)
        {
            CacheStore.Remove(GetByID(ID));

            return true;
        }

        public bool DeleteByID(int ID)
        {
            CacheStore.Remove(GetByID(ID));

            return true;
        }

        public T GetByID(long ID)
        {
            Type type = typeof(T);
            PropertyInfo[] props = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            T getObj = (T)Activator.CreateInstance(type);
            foreach (T obj in CacheStore)
            {
                long primaryVal = (long)obj.GetType().GetProperty(type.Name +"ID").GetValue(obj, null);
                if (primaryVal == ID)
                {
                    getObj = obj;
                }

                break;
            }

            return getObj;
        }

        public T GetByID(int ID)
        {
            Type type = typeof(T);
            PropertyInfo[] props = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            T getObj = (T)Activator.CreateInstance(type);
            foreach (T obj in CacheStore)
            {
                int primaryVal = (int)obj.GetType().GetProperty(type.Name + "ID").GetValue(obj, null);
                if (primaryVal == ID)
                {
                    getObj = obj;
                    break;
                }
            }

            return getObj;
        }

        public IList<T> GetMany(Dictionary<string, object> whereVals)
        {
            return CacheStore;
        }

        public IList<T> GetMany(Dictionary<string, object> whereVals, Dictionary<string, bool> orderBy)
        {
            return CacheStore;
        }

        public bool Update(T obj)
        {
            Type type = typeof(T);
            PropertyInfo[] props = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            int primaryVal = (int)obj.GetType().GetProperty(type.Name +"ID").GetValue(obj, null);

            DeleteByID(primaryVal);
            Create(obj);

            return true;

        }

        public bool UpdateWConcurrency(T obj, T original)
        {
            Update(obj);

            return true;
        }


        public T GetSingle(Dictionary<string, object> whereVals)
        {
            throw new NotImplementedException();
        }

        public T GetSingle(string procedure, SqlParameter[] parameters)
        {
            throw new NotImplementedException();
        }

        public void DeleteCache()
        {
            _cacheStore.Trim(100);
            _cacheStore.Dispose();
            _cacheStore = null;
        }
    }
}

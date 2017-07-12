using System;
using System.Collections.Generic;
using System.Data.SqlClient;
namespace WillsORM
{
    public interface IData<T>
    {
        T Create(T obj);
        bool DeleteByID(long ID);
        bool DeleteByID(int ID);
        T GetByID(long ID);
        T GetByID(int ID);
        System.Collections.Generic.IList<T> GetMany(System.Collections.Generic.Dictionary<string, object> whereVals);
        System.Collections.Generic.IList<T> GetMany(System.Collections.Generic.Dictionary<string, object> whereVals, System.Collections.Generic.Dictionary<string, bool> orderBy);
        T GetSingle(Dictionary<string, object> whereVals);
        T GetSingle(string procedure, SqlParameter[] parameters);
        int SelectLevels { get; set;}
        bool Update(T obj);
        bool UpdateWConcurrency(T obj, T original);
    }
}

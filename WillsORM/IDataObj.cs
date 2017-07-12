using System;
using System.Collections.Generic;
namespace WillsORM
{
    public interface IDataObj
    {
        void SetProperties(System.Collections.Generic.IDictionary<string, object> dict);

        System.Collections.Generic.Dictionary<string, object> Values { get; }
    }
}

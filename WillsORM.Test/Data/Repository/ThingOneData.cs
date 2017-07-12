using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WillsORM.Test.Data.Model;

namespace WillsORM.Test.Data.Repository
{
    public class ThingOneData : BaseDataActions<ThingOne>
    {
        public ThingOneData(string connStringName)
        {
            base.conn.ConnectionStringName = connStringName; 
        }
    }
}

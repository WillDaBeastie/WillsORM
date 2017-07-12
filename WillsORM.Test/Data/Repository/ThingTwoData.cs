using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WillsORM.Test.Data.Model;

namespace WillsORM.Test.Data.Repository
{
    public class ThingTwoData : BaseDataActions<ThingTwo>
    {
        public ThingTwoData(string connStringName)
        {
            base.conn.ConnectionStringName = connStringName;
        }
    }
}

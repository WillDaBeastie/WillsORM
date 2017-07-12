namespace WillsORM.Test.Data.Model
{
	public class ThingOne : DataObj
    {
        [DB(IsPrimary = true)]
        public int ThingOneID { get; set; }

        public string Name { get; set;  }

        public int Score { get; set; }
    }
}

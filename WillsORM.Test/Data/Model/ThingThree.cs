namespace WillsORM.Test.Data.Model
{
	public class ThingThree : DataObj
    {
        [DB(IsPrimary = true)]
        public int ThingThreeID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string SomethingElse { get; set; }
    }
}

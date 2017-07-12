using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Data;
using WillsORM.Test.Data;
using WillsORM.Test.Data.Model;
using WillsORM.Test.Data.Repository;

namespace WillsORM.Test
{
	[TestClass]
	public class CRUD_Test
    {
        private ThingOneData _thingOneData;
        private TestDB _testDB;
        private string connStringName = "TestDB";
        private string connString = ConfigurationManager.ConnectionStrings["TestDB"].ToString();

        private int testId = 1;


        public CRUD_Test()
        {
			Setup();
		}

		private void Setup()
		{
			_testDB = new TestDB();
			_testDB.RunSQL("DELETE FROM ThingOne; DBCC CHECKIDENT (ThingOne, reseed, 0); ");
			_thingOneData = new ThingOneData(connStringName);
		}

        [TestMethod]
        public void Create_Test()
        {
			Setup();

			string name = "Testing name";
            int rowsToInsert = 1;
            int rowsInserted = 0;

            ThingOne thing = new ThingOne()
            {
                Name = name,
                Score = 1
            };

            _thingOneData.Create(thing);

            DataTable dt = _testDB.GetBySQL("SELECT * FROM ThingOne WHERE Name = '" + name + "'");

            rowsInserted = dt.Rows.Count;

            Assert.AreEqual(rowsToInsert, rowsInserted);
        }

        [TestMethod]
        public void GetByID_Test()
        {
			Setup();

			_testDB.RunSQL("INSERT INTO ThingOne ( Name, Score) VALUES ('Testing name', 5) ");

            ThingOne thing = _thingOneData.GetByID(testId);

            DataTable dt = _testDB.GetBySQL("SELECT * FROM ThingOne WHERE ThingOneID = " + testId);

            Assert.AreEqual(dt.Rows[0]["Name"].ToString(), thing.Name);
        }

        [TestMethod]
        public void Update_Test()
        {
			Setup();

			_testDB.RunSQL("INSERT INTO ThingOne ( Name, Score) VALUES ('Testing name', 5) ");

            string newName = "Something else name";

            ThingOne thing = _thingOneData.GetByID(testId);
            thing.Name = newName;

            _thingOneData.Update(thing);

            DataTable dt = _testDB.GetBySQL("SELECT * FROM ThingOne WHERE ThingOneID = " + testId);

            Assert.AreEqual(newName, dt.Rows[0]["Name"].ToString());
        }

        [TestMethod]
        public void Delete_Test()
        {
			Setup();

			_testDB.RunSQL("INSERT INTO ThingOne ( Name, Score) VALUES ('Testing name', 5) ");

            _thingOneData.DeleteByID(testId);

            DataTable dt = _testDB.GetBySQL("SELECT * FROM ThingOne WHERE ThingOneID = " + testId);

            Assert.AreEqual(0, dt.Rows.Count);
        }
    }
}

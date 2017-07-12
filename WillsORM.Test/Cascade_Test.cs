using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using WillsORM.Test.Data;
using WillsORM.Test.Data.Model;
using WillsORM.Test.Data.Repository;

namespace WillsORM.Test
{
	[TestClass]
    public class Cascade_Test
    {
        private ThingOneData _thingOneData;
        private ThingTwoData _thingTwoData;
        private ThingThreeData _thingThreeData;
        private TestDB _testDB;
        private string connStringName = "TestDB";
        private string connString = ConfigurationManager.ConnectionStrings["TestDB"].ToString();

		
        public Cascade_Test()
        {
			Setup();
		}

		private void Setup()
		{
			_testDB = new TestDB();
			_testDB.RunSQL("DELETE FROM ThingOne; DBCC CHECKIDENT (ThingOne, reseed, 0); ");
			_testDB.RunSQL("DELETE FROM ThingTwo; DBCC CHECKIDENT (ThingTwo, reseed, 0); ");
		//	_testDB.RunSQL("DELETE FROM ThingThree; DBCC CHECKIDENT (ThingThree, reseed, 0); ");
		//	_testDB.RunSQL("DELETE FROM ThingTwo_ThingThree; DBCC CHECKIDENT (ThingTwo_ThingThree, reseed, 0); ");
			_thingOneData = new ThingOneData(connStringName);
			_thingTwoData = new ThingTwoData(connStringName);
			_thingThreeData = new ThingThreeData(connStringName);
		}


        [TestMethod]
        public void AddM21Child_Test()
        {
			Setup();

			_testDB.RunSQL("INSERT INTO ThingOne ( Name, Score) VALUES ('Top thing', 10) ;");
            _testDB.RunSQL("INSERT INTO ThingTwo ( Name, Description) VALUES ('Bob', 'Is a top thing') ;");

            ThingOne topThing = _thingOneData.GetByID(1);
            _thingTwoData.SelectLevels = 2;
            ThingTwo bob = _thingTwoData.GetByID(1);

            bob.ThingOne = topThing;

            _thingTwoData.Update(bob);

            DataTable dt = _testDB.GetBySQL("SELECT ThingTwoID, Name, Description, ThingOneID FROM dbo.[ThingTwo] WHERE ThingTwoID = 1");

            DataRow dr = dt.Rows[0];

            Assert.IsTrue(dr["ThingOneID"].ToString() == "1");

        }

        [TestMethod]
        public void RemoveM21Child_Test()
        {
			Setup();

			_testDB.RunSQL("INSERT INTO ThingOne ( Name, Score) VALUES ('Top thing', 10) ;");
            _testDB.RunSQL("INSERT INTO ThingTwo ( Name, Description, ThingOneID) VALUES ('Bob', 'Is a top thing', 1) ;");

            _thingTwoData.SelectLevels = 2;
            ThingTwo bob = _thingTwoData.GetByID(1);

            bob.ThingOne = null;

            _thingTwoData.Update(bob);

            DataTable dt = _testDB.GetBySQL("SELECT ThingTwoID, Name, Description, ThingOneID FROM dbo.[ThingTwo] WHERE ThingTwoID = 1");

            DataRow dr = dt.Rows[0];

            Assert.IsTrue(dr["ThingOneID"] == DBNull.Value);
        }
    }
}

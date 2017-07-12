using System.Collections.Generic;

namespace WillsORM.Test.Data.Model
{
	public class ThingTwo : DataObj
    {
        [DB(IsPrimary = true)]
        public int ThingTwoID { get; set; }

        public string Name { get; set; }


        [DB(RelationType= DBAttribute.RelationTypes.ManyToOne)]
        public ThingOne ThingOne { get; set; }


        [DB(RelationType = DBAttribute.RelationTypes.ManyToMany, HasOrder=true, TableName="ThingTwo_ThingThree")]
        public IList<ThingThree> ThingThrees { get; set; }

        public string Description { get; set; }
    }
}

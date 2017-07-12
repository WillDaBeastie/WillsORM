using System;

namespace WillsORM
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]

    public class ValidationAttribute : Attribute
    {
        private object _initVal = null;

        private int _maxChar = 150;

        private bool _mandatory = false;

        private string _regEx = string.Empty;

        private string _displayName = string.Empty;

        public object InitValue
        {
            get { return _initVal; }

            set { _initVal = value; }
        }

        public int MaxChars
        {
            get { return _maxChar; }

            set { _maxChar = value; }
        }

        public bool IsMandatory
        {
            get { return _mandatory; }

            set { _mandatory = value; }
        }

        public string RegEx
        {
            get { return _regEx; }

            set { _regEx = value; }
        }

        public string DisplayName
        {
            get { return _displayName; }

            set { _displayName = value; }
        }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]

    public class DBAttribute : Attribute
    {
        public enum RelationTypes
        {
            None,
            OneToMany,
            ManyToOne,
            ManyToMany
        }

        private bool _isPrimary = false;

        private bool _isBigTextOrImage = false;
        private bool _CascadeInsert = false;
        private RelationTypes _relationType = RelationTypes.None;

        private string _tableName;

        private string _columnName;

        private bool _hasOrder = false;

        public bool IsPrimary
        {
            get { return _isPrimary; }
            set { _isPrimary = value; }
        }

        public bool IsBigTextOrImage
        {
            get { return _isBigTextOrImage; }
            set { _isBigTextOrImage = value; }
        }

        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

        public string ColumnName
        {
            get { return _columnName; }
            set { _columnName = value; }
        }

        public bool CascadeWrites
        {
            get { return _CascadeInsert; }
            set { _CascadeInsert = value; }
        }

        public RelationTypes RelationType
        {
            get { return _relationType; }
            set { _relationType = value; }
        }

        public bool HasOrder
        {
            get { return _hasOrder; }

            set { _hasOrder = value; }
        }
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]

    public class TableAttribute : Attribute
    {
        private string _name = string.Empty;

        private bool _readonly = false;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public bool ReadOnly
        {
            get { return _readonly; }
            set { _readonly = value; }
        }
    }
}

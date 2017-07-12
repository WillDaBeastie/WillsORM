using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;



namespace WillsORM
{

	public abstract class DataObj : IDataObj
    {

		private Validation _validation;

        private Dictionary<string, object> _values;



        private PropertyInfo[] _props;

        private PropertyInfo[] _objProperties
        {
            get
            {
                if (_props == null)
                {
                    var type = this.GetType();
                    _props = DataHelper.getCachedProps(type);
                }
                return _props;
            }
        }



		public Validation Validation
		{
			get
			{
				return _validation;
			}
		}


        public virtual Dictionary<string, object> Values
        {
            get
            {
              //  if (_values == null) // Stop using stale data..
              //  {
                    _values = new Dictionary<string, object>();
                    var properties = _objProperties;

                    foreach (PropertyInfo property in properties)
                    {
                        object propValue = property.GetValue(this, null);
                        if (propValue is ICollection)
                        {
                            int countProp = ((ICollection)propValue).Count;
                            if (countProp > 0)
                            {
                                _values[property.Name] = property.GetValue(this, null);
                            }
                        }

                        else if (property.GetValue(this, null) != null)
                        {
                            _values[property.Name] = property.GetValue(this, null); // was .ToString()
                        }
                    }
              //  }
                return _values;
            }
        }

        /// <summary>
        /// To dynamically create the object with a dictionary of values.
        /// </summary>
        /// <param name="dict"></param>

        public DataObj(IDictionary<String, object> dict)
        {
            this.SetProperties(dict);
			_validation = new Validation(this, _objProperties);
        }

		public DataObj()
		{
			_validation = new Validation(this, _objProperties);
		}

        public virtual void SetProperties(IDictionary<String, object> dict)
        {
            Type type = this.GetType();
            foreach (PropertyInfo property in _objProperties)
            {
                if (dict.ContainsKey(property.Name))
                {
                    object newValue = dict[property.Name];

                    newValue = CleanUpFormVals(newValue, property.PropertyType);

					if (_validation.IsValidType(newValue, property.PropertyType))
                    {
                        string typeName = property.PropertyType.Name;
                        if (typeName.Contains("Nullable"))
                        {
                            typeName = Nullable.GetUnderlyingType(property.PropertyType).Name;
                        }

                        if (!string.IsNullOrEmpty(newValue.ToString()))
                        {
                            object niceNewValue = setProperPropertyValue(typeName, newValue);
                            property.SetValue(this, niceNewValue, null);
                        }
                    }
                    else
                    {
                        ValidationAttribute[] vals = (ValidationAttribute[])property.GetCustomAttributes(typeof(ValidationAttribute), false);

                        string displayName = property.Name;

                        if (vals.Length > 0)
                        {
                            ValidationAttribute valAl = vals[0];

                            if (!string.IsNullOrEmpty(valAl.DisplayName))
                            {
                                displayName = valAl.DisplayName;
                            }
                        }

						if (_validation.ValidationErrorReport.ContainsKey(displayName))
                        {
							_validation.ValidationErrorReport[displayName] += " Wrong type (" + property.PropertyType.Name + ").";
                        }
						else { _validation.ValidationErrorReport.Add(displayName, "Wrong type (" + property.PropertyType.Name + ")."); }

						if (_validation.ValidationErrorReport.ContainsKey(property.Name))
						{
							_validation.ValidationErrorReport[property.Name] += " Wrong type (" + property.PropertyType.Name + ").";
						}
						else { _validation.ValidationErrorReport.Add(property.Name, "Wrong type (" + property.PropertyType.Name + ")."); }
                    }
                }
                else if (property.PropertyType.BaseType != null)
                {
                    if (property.PropertyType.BaseType.Name == type.BaseType.Name) // Fill Data Type
                    {
                        object obj = Activator.CreateInstance(property.PropertyType);

                        Type childType = obj.GetType();

                        IDictionary<string, object> releventVals = getReleventValues(dict, childType.Name);

                        if (releventVals.Count > 0)
                        {
                            childType.InvokeMember("SetProperties", BindingFlags.InvokeMethod, null, obj, new object[] { releventVals });
                            property.SetValue(this, obj, null);
                        }
                        //else // Don't want to set a null value
                        //{
                        //    property.SetValue(this, obj, null); //Put back in to see what happens
                        //}
                    }
                }
            }
            _values = (Dictionary<String, object>)dict;
        }

        private object CleanUpFormVals(object newValue, Type type)
        { 
            switch (type.Name)
            {
                case "Boolean":
                    if (newValue.GetType().Name == "String")
                    { 
                        if ((string)newValue == "true,false")
                        {
                            newValue = true;
                        }
                    }
                    break;
            }

            return newValue;
        }

        private IDictionary<string, object> getReleventValues(IDictionary<string, object> vals, string prefix)
        {
            IDictionary<string, object> newVals = new Dictionary<string, object>();

            foreach(KeyValuePair<string, object> keyValPair in vals)
            {
                if(keyValPair.Key.StartsWith(prefix+"."))
                {
                    newVals[keyValPair.Key.Replace(prefix + ".", "")] = keyValPair.Value;
                }
            }

            return newVals;
        }

        private object setProperPropertyValue(string typeName, object newValue)
        {
            object output = null;
            switch (typeName)
            {
                case "String":
                    output = newValue.ToString();
                    break;
                case "Int32":
                    output = int.Parse(newValue.ToString());
                    break;
                case "Int64":
                    output = Int64.Parse(newValue.ToString());
                    break;
                case "Single":
                    output = float.Parse(newValue.ToString());
                    break;
                case "Double":
                    output = Double.Parse(newValue.ToString());
                    break;
                case "DateTime":
                    if (!string.IsNullOrEmpty(newValue.ToString()))
                    {
                        output = DateTime.Parse(newValue.ToString());
                    }
                    break;
                case "Decimal":
                    if (!string.IsNullOrEmpty(newValue.ToString()))
                    {
                        output = decimal.Parse(newValue.ToString());
                    }
                    break;
                case "Boolean":
                    output = bool.Parse(newValue.ToString());
                    break;
                default:
                    output = newValue;
                    break;
            }

            return output;
        }

   

    }

}
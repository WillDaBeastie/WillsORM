using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WillsORM
{
	public class Validation
	{
		public ICollection<IFieldValidation> _validationOverrides;

		public ICollection<IFieldValidation> ValidationOverrides
		{
			get
			{
				if(_validationOverrides == null)
				{
					_validationOverrides = new List<IFieldValidation>();
				}
				return _validationOverrides;
			}
			set
			{
				_validationOverrides = value;
			}
		}

		private string[] _validateList;

		public string[] ValidationList
		{
			get
			{
				return _validateList;
			}
			set
			{
				_validateList = value;
			}
		}

		private object _objType;

		private PropertyInfo[] _objProperties;

		private Dictionary<string, string> _validationReport;

		private Dictionary<string, string> _validationReport2;

		private string[] _mandatoryFields;

		/// <summary>
		/// Does some stuff to figure out the display name even if it's been overriden somewhere else. Flaky!
		/// </summary>
		public string[] MandatoryFields
		{
			get
			{
				if (_mandatoryFields == null)
				{
					_mandatoryFields = this.GetMandatoryFields();
				}
				return _mandatoryFields;
			}
			set
			{
				_mandatoryFields = value;
			}

		}

		private string[] _mandatoryFields2;

		/// <summary>
		/// Only uses the Form name for use with js etc.
		/// </summary>
		public string[] MandatoryFields2
		{
			get
			{
				if (_mandatoryFields2 == null)
				{
					_mandatoryFields2 = this.GetMandatoryFields2();
				}
				return _mandatoryFields2;
			}

		}


		public Validation(object objType, PropertyInfo[] objProperties)
		{
			_objType = objType;
			_objProperties = objProperties;
			_validationReport = new Dictionary<string, string>();
			_validationReport2 = new Dictionary<string, string>();
			
		}

		public Dictionary<string, string> ValidationErrorReport
		{

			get { return _validationReport; }

			set { _validationReport = value; }
			
		}

		public Dictionary<string, string> ValidationErrorReport2
		{

			get { return _validationReport2; }

			set { _validationReport2 = value; }

		}

		public string[] GetMandatoryFields()
		{
			IList<string> aList = new List<string>();
			foreach (PropertyInfo property in _objProperties)
			{
				ValidationAttribute[] vals = (ValidationAttribute[])property.GetCustomAttributes(typeof(ValidationAttribute), false);

				string displayName = AddSpacesToSentence(property.Name);

				if (vals.Length > 0)
				{
					foreach (ValidationAttribute valAl in vals)
					{
						if (!string.IsNullOrEmpty(valAl.DisplayName))
						{
							displayName = valAl.DisplayName;
						}
						if (valAl.IsMandatory)
						{
							aList.Add(displayName);
						}
					}
				}

				foreach (FieldValidation fVal in ValidationOverrides)
				{
					if (fVal.FieldName == property.Name)
					{
						string voDisplayName = displayName;
						if (!string.IsNullOrEmpty(fVal.DisplayName))
						{
							voDisplayName = fVal.DisplayName;
						}
						if (fVal.IsMandatory)
						{
							aList.Add(voDisplayName);
						}
						else
						{
							aList.Remove(voDisplayName);
						}
						if (fVal.ForceValid)
						{
							aList.Remove(voDisplayName);
						}
					}
				}
			}

			return aList.ToArray();
		}

		public string[] GetMandatoryFields2()
		{
			IList<string> aList = new List<string>();
			foreach (PropertyInfo property in _objProperties)
			{
				ValidationAttribute[] vals = (ValidationAttribute[])property.GetCustomAttributes(typeof(ValidationAttribute), false);

				if (vals.Length > 0)
				{
					foreach (ValidationAttribute valAl in vals)
					{
						if (valAl.IsMandatory)
						{
							aList.Add(property.Name);
						}
					}
				}

				foreach (FieldValidation fVal in ValidationOverrides)
				{
					if (fVal.FieldName == property.Name)
					{
						if (fVal.IsMandatory)
						{
							aList.Add(property.Name);
						}
						else
						{
							aList.Remove(property.Name);
						}
						if (fVal.ForceValid)
						{
							aList.Remove(property.Name);
						}
					}
				}
			}

			return aList.ToArray();
		}

		public bool IsValidType(object value, Type expectedType)
		{
			bool isValid = false;

			string typeName = expectedType.Name;
			if (typeName.Contains("Nullable"))
			{
				typeName = Nullable.GetUnderlyingType(expectedType).Name;
			}

			Type valueType = value.GetType();

			if (expectedType == valueType)
			{
				isValid = true;
			}
			else if (expectedType.IsGenericType && valueType.IsGenericType)
			{
				isValid = true;
			}
			else if (string.IsNullOrEmpty(value.ToString()))
			{
				isValid = true;
			}
			else
			{
				switch (typeName)
				{
					case "Boolean":
						Boolean outBool;
						if (Boolean.TryParse(value.ToString(), out outBool))
						{
							isValid = true;
						}
						break;
					case "DateTime":
						DateTime outdt;
						if (DateTime.TryParse(value.ToString(), out outdt))
						{
							isValid = true;
						}
						break;
					case "Decimal":
						Decimal outdec;
						if (Decimal.TryParse(value.ToString(), out outdec))
						{
							isValid = true;
						}
						break;
					case "Double":
						Double outdoubl;
						if (Double.TryParse(value.ToString(), out outdoubl))
						{
							isValid = true;
						}
						break;
					case "Int32":
						int outInt;
						if (Int32.TryParse(value.ToString(), out outInt))
						{
							isValid = true;
						}
						break;
					case "Int64":
						long outInt64;
						if (Int64.TryParse(value.ToString(), out outInt64))
						{
							isValid = true;
						}
						break;
					case "String":
						if (value.ToString().Length > -1)
						{
							isValid = true;
						}
						break;
				}
			}

			return isValid;
		}



		public bool IsValid()
		{
			return this.IsValid(false);
		}

		/// <summary>
		/// Validates a populated object against the validation attributes set in the DataObj class.
		/// </summary>
		/// 

		public bool IsValid(bool CheckChildren)
		{
			bool retVal = false;

			foreach (PropertyInfo property in _objProperties)
			{
				ValidationAttribute[] vals = (ValidationAttribute[])property.GetCustomAttributes(typeof(ValidationAttribute), false);

				object value = property.GetValue(_objType, null);
				string displayName = AddSpacesToSentence(property.Name);
				int maxChars = 255;

				if (vals.Length > 0)
				{
					ValidationAttribute valAl = vals[0];
					maxChars = valAl.MaxChars;

					string initVal = string.Empty;
					if (valAl.InitValue != null)
					{
						initVal = valAl.InitValue.ToString();
					}
					if (!string.IsNullOrEmpty(valAl.DisplayName))
					{
						displayName = valAl.DisplayName;
					}


					if (_validateList == null)
					{
						validateField(value, initVal, displayName, valAl.RegEx, valAl.IsMandatory, maxChars, property.PropertyType.Name);
					}
					else if (Array.IndexOf(_validateList, property.Name) > -1 && _validateList.Length > 0)
					{
						validateField(value, initVal, displayName, valAl.RegEx, valAl.IsMandatory, maxChars, property.PropertyType.Name);
					}
				}

				foreach (FieldValidation fVal in ValidationOverrides)
				{
					if (fVal.FieldName == property.Name)
					{
						string voDisplayName = displayName;
						if (!string.IsNullOrEmpty(fVal.DisplayName))
						{
							voDisplayName = fVal.DisplayName;
						}

						if (maxChars > fVal.MaxChars)
						{
							fVal.MaxChars = maxChars;
						}

						validateField(value, fVal.InitValue, voDisplayName, fVal.RegEx, fVal.IsMandatory, fVal.MaxChars, property.PropertyType.Name);

						string voErrorMessage = fVal.ErrorMessage;
						if (string.IsNullOrEmpty(fVal.ErrorMessage))
						{
							voErrorMessage = "is mandatory";
						}

						if (fVal.ForceValid)
						{
							if (_validationReport.ContainsKey(voDisplayName))
							{
								_validationReport.Remove(voDisplayName);
							}
						}
						else if (fVal.ForceInvalid)
						{
							ValidationReportSafeAdd(voDisplayName, voErrorMessage);
						}
					}
				}

				if (CheckChildren)
				{
					if (property.PropertyType.Namespace != "System.Collections.Generic")
					{
						if (property.PropertyType.BaseType.Name == "DataObj")
						{
							object obj = property.GetValue(_objType, null);

							if (obj != null)
							{
								Type dataObjType = obj.GetType();
								object validationObj = dataObjType.GetProperty("Validation").GetValue(_objType, null);
								Type validationType = validationObj.GetType();
								if (!(bool)validationType.InvokeMember("IsValid", BindingFlags.InvokeMethod, null, validationObj, null))
								{
									Dictionary<string, string> errorsToAdd = (Dictionary<string, string>)validationType.GetProperty("ValidationErrorReport").GetValue(validationObj, null);

									Dictionary<string, string> tempDict = new Dictionary<string, string>();
									tempDict.Concat(errorsToAdd);
									foreach (KeyValuePair<string, string> kvp in tempDict)
									{
										ValidationReportSafeAdd(kvp.Key, kvp.Value);
									}

									tempDict = null;
								}
							}
						}
					}
				}
			}

			if (_validationReport.Count == 0)
			{
				retVal = true;
			}
			return retVal;
		}


		private void validateField(object fieldValue, string initVal, string displayName, string regex, bool isMandatory, int maxChars, string propertyType)
		{
			if (isMandatory)
			{
				if ((fieldValue == null || fieldValue.ToString() == string.Empty || fieldValue.ToString() == initVal))
				{
					ValidationReportSafeAdd(displayName, "Is mandatory.");
				}
			}

			if (regex != null)
			{
				if (regex.Length > 0)
				{
					Regex regexObj = new Regex(regex);

					if (fieldValue != null && (string)fieldValue != string.Empty)
					{
						if (!regexObj.IsMatch(fieldValue.ToString()))
						{
							ValidationReportSafeAdd(displayName, "Is not valid.");
						}
					}
				}
			}

			switch (propertyType)
			{
				case "String":
					if (fieldValue == null)
					{
						break;
					}
					else if (fieldValue.ToString().Length > maxChars)
					{
						ValidationReportSafeAdd(displayName, "Too many characters.");
					}
					break;
			}
		}

		private void ValidationReportSafeAdd(string key, string value)
		{
			if (_validationReport.ContainsKey(key))
				_validationReport[key] += value;
			else
				_validationReport.Add(key, value); 
		}

		private static string AddSpacesToSentence(string text)
		{
			if (string.IsNullOrEmpty(text))
				return "";
			StringBuilder newText = new StringBuilder(text.Length * 2);
			newText.Append(text[0]);
			for (int i = 1; i < text.Length; i++)
			{
				if (char.IsUpper(text[i]) && text[i - 1] != ' ')
					newText.Append(' ');
				newText.Append(text[i]);
			}
			return newText.ToString();
		}

	}
}

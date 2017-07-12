using System;
namespace WillsORM
{
	public interface IFieldValidation
	{
		string DisplayName { get; set; }
		string ErrorMessage { get; set; }
		string FieldName { get; set; }
		bool ForceInvalid { get; set; }
		bool ForceValid { get; set; }
		string InitValue { get; set; }
		bool IsMandatory { get; set; }
		int MaxChars { get; set; }
		string RegEx { get; set; }
	}
}

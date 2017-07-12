using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WillsORM
{
	public class FieldValidation : IFieldValidation
    {
        private int _maxChars = 255;

        public FieldValidation(){ }

        public FieldValidation(string FieldName, bool ForceValid)
        {
            this.FieldName = FieldName;
            this.ForceValid = ForceValid;
        }

        public FieldValidation(string FieldName, bool ForceValid, bool IsMandatory)
        {
            this.FieldName = FieldName;
            this.ForceValid = ForceValid;
            this.IsMandatory = IsMandatory;
        }

        public FieldValidation(string FieldName, bool ForceValid, bool IsMandatory, int MaxChars)
        {
            this.FieldName = FieldName;
            this.ForceValid = ForceValid;
            this.IsMandatory = IsMandatory;
            this.MaxChars = MaxChars;
        }

        public FieldValidation(string FieldName, bool ForceValid, string ErrorMessage)
        {
            this.FieldName = FieldName;
            this.ForceValid = ForceValid;
            this.ErrorMessage = ErrorMessage;
        }

        public string FieldName { get; set; }

        public string DisplayName { get; set; }

        public string RegEx { get; set; }

        public string InitValue { get; set; }

        public bool ForceValid { get; set; }

        public bool ForceInvalid { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsMandatory { get; set; }

        public int MaxChars
        {
            get
            {
                return _maxChars;
            }

            set
            {
                _maxChars = value;
            }
        }
    }
}

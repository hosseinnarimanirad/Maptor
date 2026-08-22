using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.ValidationRules
{
    public class KodeMelliValidation : BaseValidationRule
    {

        public string InvalidKodeMelli
        {
            get { return IsFarsiMode ? "کد ملی نامعتبر" : "Invalid code"; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var stringValue = value?.ToString();

                string errorContent = string.Empty;

                if (string.IsNullOrWhiteSpace(stringValue) && IsRequired)
                {
                    errorContent = IsRequiredMessage;
                }
                else if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    if (stringValue.Length != 10 || stringValue.Any(c => !Char.IsDigit(c)))
                    {
                        errorContent = InvalidKodeMelli;
                    }
                }

                return new ValidationResult(errorContent.Length == 0, errorContent);
            }
            catch
            {
                return new ValidationResult(false, null);
            }
        }





    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IRI.Maptor.Jab.Common.ValidationRules
{
    public class EmailValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                var stringValue = value?.ToString();

                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    Regex regex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");

                    var isValid = regex.IsMatch(stringValue);

                    var errorContent = Localization.LocalizationManager.Instance["validation_msg_invalidEmail"];

                    return new ValidationResult(isValid, isValid ? null : errorContent);
                }

                var content = Localization.LocalizationManager.Instance["validation_msg_nullOrEmptyString"];

                return new ValidationResult(false, content);
            }
            catch
            {
                var error = Localization.LocalizationManager.Instance["validation_msg_unknownError"];

                return new ValidationResult(false, error);
            }


        }
    }
}

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

                    return new ValidationResult(regex.IsMatch(stringValue), null);
                }

                return new ValidationResult(false, "invalid");
            }
            catch
            {
                return new ValidationResult(false, "invalid");
            }


        }
    }
}

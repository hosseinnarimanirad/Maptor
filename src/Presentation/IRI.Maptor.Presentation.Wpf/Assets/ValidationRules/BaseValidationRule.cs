using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.ValidationRules
{
    public abstract class BaseValidationRule : ValidationRule
    {
        public virtual bool IsRequired { get; set; }

        public virtual bool IsFarsiMode { get; set; } = true;

        protected string CannotBeNullMessage
        {
            get { return IsFarsiMode ? "مقدار نمی‌تواند خالی باشد" : "Cannot be null"; }
        }

        protected string IsRequiredMessage
        {
            get { return IsFarsiMode ? "فیلد اجباری است" : "Required field"; }
        }


    }
}

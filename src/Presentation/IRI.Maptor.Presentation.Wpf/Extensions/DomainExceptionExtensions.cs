using IRI.Maptor.Presentation.Core.Localization;
using IRI.Maptor.Core.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Extensions;

public static class DomainExceptionExtensions
{
    public static string GetLocalizedMessage(this DomainException exception) => LocalizationManager.Instance[exception.MessageResourceKey];
}

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Abstractions;

public interface ILogger
{
    void Fatal(string message);

    void Error(string message);

    void Warn(string message);

    void Info(string message);

    void Debug(string message);

    void Trace(string message);


    void Fatal(string message, Exception ex);

    void Error(string message, Exception ex);

    void Warn(string message, object value);

    void Info(string message, object value);

    void Debug(string message, object value);

    void Trace(string message, object value);



    void Fatal(string methodName, string message, Exception ex = null);

    void Error(string methodName, string message, Exception ex = null);

    void Warn(string methodName, string message, object value = null);

    void Info(string methodName, string message, object value = null);

    void Debug(string methodName, string message, object value = null);

    void Trace(string methodName, string message, object value = null);



    void Fatal(string methodName, string followUpKey, string message, Exception ex = null);

    void Error(string methodName, string followUpKey, string message, Exception ex = null);

    void Warn(string methodName, string followUpKey, string message, object value = null);

    void Info(string methodName, string followUpKey, string message, object value = null);

    void Debug(string methodName, string followUpKey, string message, object value = null);

    void Trace(string methodName, string followUpKey, string message, object value = null);

}

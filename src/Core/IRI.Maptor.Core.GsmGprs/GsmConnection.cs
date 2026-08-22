using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace IRI.Maptor.Core.GsmGprs;

public class GsmConnection
{
    SerialPort port;

    public GsmConnection(string portName, int baudRate, int dataBits)
    {
        InitializePort(portName, baudRate, dataBits);
    }

    private void InitializePort(string portName, int baudRate, int dataBits)
    {
        this.port.PortName = portName;

        this.port.BaudRate = baudRate;

        this.port.DataBits = dataBits;

        this.port.ReadBufferSize = 10000;

        this.port.ReadTimeout = 1000;

        this.port.RtsEnable = true;

        this.port.WriteBufferSize = 10000;

        this.port.WriteTimeout = 10000;
    }

    public bool IsModemConnected()
    {
        throw new NotImplementedException();
    }
}

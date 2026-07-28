using System;
using System.Threading;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace IRI.Maptor.Ket.IO.Gsm;

public class GsmCommunication
{
    #region AT Commands

    //****** Initial setup AT commands ******//
    private const string IsModemWorkingString = "AT";           //Returns a "OK" to confirm that modem is working
    // AT+CPIN="xxxx"                                       //To enter the PIN for your SIM (if enabled)
    private const string IsConnectedString = "AT+CREG?";        //A 1,0 reply confirms your modem is connected 
    private const string GetSignalStrengthString = "AT+CSQ";      //Indicate the signal strength, 31.99 is maximum


    //****** Sending SMS using AT commands ******//
    private const string FormatAsTextString = "AT+CMGF=1";        // To format SMS as a TEXT message
    //  AT+CSCA="+xxxxx"                                    //Set your SMS center's number. Check with your provider
    //To send a SMS, the AT command to use is AT+CMGS ..
    //  AT+CMGS="+yyyyy" <Enter>
    //> Your SMS text message here <Ctrl-Z>
    //The "+yyyyy" is your recipient's mobile number. Next, we will look at receiving SMS via AT commands.


    //****** Receiving SMS using AT commands ******//
    //The GSM modem can be configured to response in different ways when it receives a SMS.

    //a) Immediate - when a SMS is received, the SMS's details are immediately sent to the host 
    //computer (DTE) via the +CMT command
    //AT+CMGF=1 	To format SMS as a TEXT message
    //AT+CNMI=1,2,0,0,0   	Set how the modem will response when a SMS is received
    //When a new SMS is received by the GSM modem, the DTE will receive the following ..
    //+CMT :  "+61xxxxxxxx" , , "04/08/30,23:20:00+40"
    //This the text SMS message sent to the modem
    //Your computer (DTE) will have to continuously monitor the COM serial port, read and parse 
    //the message.

    //b) Notification - when a SMS is received, the host computer ( DTE ) will be notified of 
    //the new message. The computer will then have to read the message from the indicated memory 
    //location and clear the memory location.

    //AT+CMGF=1 	To format SMS as a TEXT message
    //AT+CNMI=1,1,0,0,0   	Set how the modem will response when a SMS is received

    //When a new SMS is received by the GSM modem, the DTE will receive the following ..

    //+CMTI: "SM",3 	Notification sent to the computer. Location 3 in SIM memory
    //AT+CMGR=3 <Enter>  	AT command to send read the received SMS from modem

    //The modem will then send to the computer details of the received SMS from the specified 
    //memory location ( eg. 3 ) ..

    //+CMGR: "REC READ","+61xxxxxx",,"04/08/28,22:26:29+40"
    //This is the new SMS received by the GSM modem

    //After reading and parsing the new SMS message, the computer (DTE) should send a AT command 
    //to clear the memory location in the GSM modem ..

    //AT+CMGD=3 <Enter>   To clear the SMS receive memory location in the GSM modem

    //If the computer tries to read a empty/cleared memory location, a +CMS ERROR : 321 will be 
    //sent to the computer.
    #endregion

    private AutoResetEvent receiveNow;

    private SerialPort port;

    public string PortName
    {
        get { return this.port.PortName; }
    }

    public GsmCommunication(string portName, int baudRate, int dataBits)
        : this(portName, baudRate, dataBits, 300, 300) { }

    public GsmCommunication(string portName, int baudRate, int dataBits, int readTimeout, int writeTimeout)
    {
        receiveNow = new AutoResetEvent(false);

        this.port = new SerialPort();

        try
        {
            port.PortName = portName;               //COM1
            port.BaudRate = baudRate;               //9600
            port.DataBits = dataBits;               //8
            port.StopBits = StopBits.One;           //1
            port.Parity = Parity.None;              //None
            port.ReadTimeout = readTimeout;         //300
            port.WriteTimeout = writeTimeout;       //300
            port.Encoding = Encoding.GetEncoding("iso-8859-1");
            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            port.DtrEnable = true;
            port.RtsEnable = true;
        }
        catch (Exception ex)
        {
            throw ex;
        }

    }

    // //
    #region Private Methods

    private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (e.EventType == SerialData.Chars)
                receiveNow.Set();
        
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    private string ExecuteCommand(string command, int responseTimeout)
    {
        try
        {
            port.DiscardOutBuffer(); port.DiscardInBuffer();

            receiveNow.Reset();

            port.Write(command + "\r");

            string input;

            ReadResponse(port, responseTimeout, out input);

            if ((input.Length == 0) || ((!input.EndsWith("\r\n> ")) && (!input.EndsWith("\r\nOK\r\n"))))
                return "Error: No success message was received";

            return input;
        }
        catch (Exception ex)
        {
            return string.Format("Error: {0}", ex.Message);
        }
    }

    private bool ReadResponse(SerialPort port, int timeout, out string result)
    {
        result = string.Empty;
        try
        {
            do
            {
                if (receiveNow.WaitOne(timeout, false))
                {
                    string t = port.ReadExisting();

                    result += t;
                }
                else
                {
                    if (result.Length > 0)
                    {
                        result = "Response received is incomplete.";

                        return false;
                    }
                    else
                    {
                        result = "No data received from phone.";

                        return false;
                    }
                }
            }
            while (!result.EndsWith("\r\nOK\r\n") && !result.EndsWith("\r\n> ") && !result.EndsWith("\r\nERROR\r\n"));
        }
        catch (Exception ex)
        {
            result = ex.Message;

            return false;
        }

        return true;
    }

    private bool ParseMessages(string input, out ShortMessageCollection messages)
    {
        messages = new ShortMessageCollection();

        try
        {
            Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");

            Match m = r.Match(input);

            while (m.Success)
            {
                ShortMessage msg = new ShortMessage(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value,
                                                       m.Groups[4].Value, m.Groups[5].Value, m.Groups[6].Value);

                messages.Add(msg);

                m = m.NextMatch();
            }

        }
        catch (Exception ex)
        {
            messages = null;

            return false;
        }

        return true;
    }

    #endregion
    // //

    public bool Open()
    {
        try
        {
            this.port.Open();
        }
        catch (Exception ex)
        {
            return false;
        }

        return true;
    }

    public void Close()
    {
        this.port.Close();
    }

    public bool IsModemWorking()
    {
        string result = ExecuteCommand(IsModemWorkingString, 300);

        return result.Equals("AT\r\r\nOK\r\n");
    }

    public bool IsConnectedToGsmNetwork()
    {
        string result = ExecuteCommand(IsConnectedString, 300);

        //return result.Contains("0,1");

        return !(result.ToUpper()).Contains("ERROR");
    }

    public double GetSignalStrength()
    {
        string tempResult = ExecuteCommand(GetSignalStrengthString, 1000);

        if (tempResult.Contains("\n\r\nOK\r\n"))
        {
            int startIndex = tempResult.LastIndexOf("+CSQ:") + 5;

            int endIndex = tempResult.LastIndexOf("\r\n\r\nOK\r\n");

            string tempValue = tempResult.Substring(startIndex, endIndex - startIndex).Trim();

            double result;

            if (double.TryParse((tempValue.Split(','))[0] + '.' + (tempValue.Split(','))[1], out result))
            {
                return result;
            }
        }

        return double.NaN;
    }

    public bool SendTextMessage(string phoneNumber, string message)
    {
        bool isSend = false;

        if (IsModemWorking())
        {
            string outputCommandMessage;

            outputCommandMessage = ExecuteCommand(FormatAsTextString, 300); //, "Failed to set message format."

            String command = "AT+CMGS=\"" + phoneNumber + "\"";

            outputCommandMessage = ExecuteCommand(command, 3000);              //, "Failed to accept phoneNo"

            command = message + char.ConvertFromUtf32(26) +"\r";

            outputCommandMessage = ExecuteCommand(command, 10000);             //3 seconds , "Failed to send message"

            if (outputCommandMessage.EndsWith("\r\nOK\r\n"))
            {
                isSend = true;
            }
        }

        return isSend;

    }

    public bool ReadTextMessage(out ShortMessageCollection messages, bool readSIM)
    {
        messages = new ShortMessageCollection();

        try
        {
            if (IsModemWorking())
            {
                ExecuteCommand(FormatAsTextString, 300);                    //, "Failed to set message format."

                // Use character set "PCCP437"
                ExecuteCommand("AT+CSCS=\"PCCP437\"", 300);
                // Select SIM storage
                if (readSIM)
                {
                    ExecuteCommand("AT+CPMS=\"SM\"", 300);
                }
                else
                {
                    ExecuteCommand("AT+CPMS=\"ME\"", 300);
                }
                //, "Failed to select message storage.");
                // Read the messages
                string input = ExecuteCommand("AT+CMGL=\"ALL\"", 5000);   //, "Failed to read the messages.");

                if (!ParseMessages(input, out messages))
                {
                    return false;
                }

                return true;
            }
            else
                return false;
        }
        catch
        {
            messages = null;

            return false;
        }

    }

    public ShortMessageCollection CheckForNewMessage(bool readSIM)
    {
        ShortMessageCollection result = new ShortMessageCollection();

        ShortMessageCollection messages;

        if (ReadTextMessage(out messages, readSIM))
        {
            foreach (ShortMessage item in messages)
            {
                if (item.Status == "REC UNREAD")
                {
                    result.Add(item);
                }
            }
        }

        return result;
    }

}

using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.GsmGprs.Gsm;

public struct ShortMessage
{
    private string m_Index;

    private string m_Status;

    private string m_Sender;

    private string m_Alphabet;

    private string m_Sent;

    private string m_Message;

    public string Index
    {
        get { return m_Index; }
        //set { m_Index = value; }
    }

    public string Status
    {
        get { return m_Status; }
        //set { m_Status = value; }
    }

    public string Sender
    {
        get { return m_Sender; }
        //set { m_Sender = value; }
    }

    public string Alphabet
    {
        get { return m_Alphabet; }
        //set { m_Alphabet = value; }
    }

    public string Sent
    {
        get { return m_Sent; }
        //set { m_Sent = value; }
    }

    public string Message
    {
        get { return m_Message; }
        //set { m_Message = value; }
    }

    public ShortMessage(string index, string status, string sender,
                        string alphabet, string sent, string message)
    {
        this.m_Index = index; this.m_Status = status;
        
        this.m_Sender = sender; this.m_Alphabet = alphabet;
        
        this.m_Sent = sent; this.m_Message = message;
    }
}

public class ShortMessageCollection : List<ShortMessage>
{
}

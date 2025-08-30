# 📡 IRI.Maptor.Sta.GsmGprs



\[!\[.NET Standard](https://img.shields.io/badge/.NET-Standard2.1-blue.svg)](https://dotnet.microsoft.com/)

\[!\[License](https://img.shields.io/github/license/hosseinnarimanirad/Maptor)](LICENSE)



\*\*IRI.Maptor.Sta.GsmGprs\*\* is a .NET library for working with \*\*GSM/GPRS modules\*\* through serial communication.  

It provides high-level abstractions for \*\*AT commands\*\*, enabling developers to send SMS, manage USSD codes, and establish GPRS connections.



---



## ✨ Features



\- Serial port communication with GSM/GPRS modems

\- Send and receive \*\*SMS messages\*\*

\- Execute \*\*AT commands\*\* with response handling

\- Manage \*\*USSD requests\*\*

\- GPRS connectivity management

\- Event-driven architecture for receiving messages and modem events



---



## ⚙️ Installation



```bash

dotnet add package IRI.Maptor.Sta.GsmGprs

```



---



## 💻 Usage Examples



### Example 1 – Connecting to GSM Modem

```csharp

using IRI.Maptor.Sta.GsmGprs;



var gsm = new GsmModem("COM3", 9600);

gsm.Open();



Console.WriteLine("Connected to GSM modem.");

```



---



### Example 2 – Sending an SMS

```csharp

gsm.SendSms("+989123456789", "Hello from GSM Library!");



Console.WriteLine("SMS Sent successfully.");

```



---



### Example 3 – Receiving SMS

```csharp

gsm.SmsReceived += (sender, sms) =>

{

&nbsp;   Console.WriteLine($"New SMS from {sms.Sender}: {sms.Message}");

};

```



---



### Example 4 – Executing USSD Command

```csharp

var response = gsm.ExecuteUssd("\*140#");

Console.WriteLine("USSD Response: " + response);

```



---



### Example 5 – Sending Raw AT Command

```csharp

var atResponse = gsm.SendAtCommand("AT+CSQ"); // Signal Quality

Console.WriteLine("Signal Strength: " + atResponse);

```



---



## 📂 Project Structure

```

IRI.Maptor.Sta.GsmGprs/

│

├── Core/              # Base modem handling

├── Commands/          # AT command abstractions

├── Messaging/         # SMS and USSD management

├── Networking/        # GPRS connectivity features

└── README.md          # Documentation

```



---



## 🤝 Contributing

Contributions are welcome!  

Please include tests and documentation updates when submitting pull requests.



---



## 📜 License

This project is licensed under the \[MIT License](LICENSE).




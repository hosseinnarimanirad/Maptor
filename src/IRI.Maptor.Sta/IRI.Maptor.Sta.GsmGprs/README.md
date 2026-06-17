# IRI.Maptor.Sta.GsmGprs

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.GsmGprs.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.GsmGprs)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

A .NET Standard 2.1 library for working with **GSM/GPRS** communication — specifically SMS PDU encoding/decoding and modem interaction over a serial connection.

---

## Features

- **SMS PDU encoding** (`PduEncoder`) — build SMS-SUBMIT PDU byte streams ready to send via a GSM modem AT command
- **SMS PDU decoding** (`PduDecoder`) — parse incoming SMS-DELIVER PDU byte streams into structured objects
- **GSM modem connection** (`GsmConnection`) — open and manage a serial port connection to a GSM/GPRS modem
- **Structured SMS types**: `Sms`, `SmsDeliver` (received), `SmsSubmit` (to send)
- **TPDU parameters**: full coverage of TP-MTI, TP-DCS (Data Coding Scheme), TP-VP (Validity Period), TP-RD, TP-SRI, TP-UDHI, TP-RP, TP-MMS
- **Address field handling**: type-of-number (TON) and numbering-plan-identification (NPI) encoding

---

## Installation

```bash
dotnet add package IRI.Maptor.Sta.GsmGprs
```

---

## Project Structure

```
Sta.GsmGprs/
├── GsmConnection.cs          # Serial port modem connection
├── Sms.cs                    # Base SMS type
├── SmsDeliver.cs             # Incoming SMS (SMS-DELIVER)
├── SmsSubmit.cs              # Outgoing SMS (SMS-SUBMIT)
├── PduDecoder.cs             # PDU byte stream → SmsDeliver
├── PduEncoder.cs             # SmsSubmit → PDU byte stream
├── AddressField/             # TON, NPI and address encoding
└── TpduParameters/           # TP-MTI, TP-DCS, TP-VP, TP-RD, …
```

---

📦 **NuGet**: [IRI.Maptor.Sta.GsmGprs](https://www.nuget.org/packages/IRI.Maptor.Sta.GsmGprs)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)

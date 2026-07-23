# IRI.Maptor.Sta.GsmGprs

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.GsmGprs?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Sta.GsmGprs/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

GSM/GPRS communication primitives for the Maptor stack: SMS PDU encoding (building the hexadecimal SMS-SUBMIT/SMS-DELIVER byte streams sent to a GSM modem via AT commands) and a serial-port modem connection type.

## Installation

```bash
dotnet add package IRI.Maptor.Sta.GsmGprs
```

## Features

- SMS-SUBMIT PDU encoding: `SmsSubmit` builds the complete hex PDU (`PduCode`) for an outgoing message
- SMS-DELIVER PDU construction via `SmsDeliver`
- `PduEncoder` — service-center number encoding and UCS-2 user-data encoding
- TPDU parameter types: TP-MTI, TP-DCS, TP-PID, TP-VP/TP-VPF, TP-RD, TP-SRI/TP-SRR, TP-UDHI, TP-RP, TP-MMS
- Address field handling: type-of-number (TON) and numbering-plan-identification (NPI) via the `Address` struct
- `GsmConnection` — serial-port configuration (port name, baud rate, data bits) for a GSM/GPRS modem

## Usage

Encode an outgoing SMS as a PDU:

```csharp
using IRI.Maptor.Sta.GsmGprs;
using IRI.Maptor.Sta.GsmGprs.AddressField;

var destination = new Address(989121234567);

var sms = new SmsSubmit(smscNumber: 989120000000, destination, "Hello from Maptor");

string pdu = sms.PduCode;   // hex PDU, ready to send with AT+CMGS
```

## Limitations

- PDU decoding of incoming messages is not implemented (`PduDecoder` is an empty placeholder).
- Message text is encoded as UCS-2 only.
- `GsmConnection.IsModemConnected()` is not implemented yet.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Sta.GsmGprs/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Sta](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/README.md)

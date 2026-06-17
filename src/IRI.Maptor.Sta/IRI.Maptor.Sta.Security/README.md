# IRI.Maptor.Sta.Security

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Security.svg?style=flat-square)](https://www.nuget.org/packages/IRI.Maptor.Sta.Security)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

A .NET Standard 2.1 library of **cryptography and security primitives** used across the Maptor ecosystem — AES encryption, RSA public-key cryptography, hashing, and JWT signing.

---

## Features

### Symmetric Encryption (AES)
- `RijndaelHelper` — AES/Rijndael encrypt and decrypt with configurable key/IV

### Asymmetric Encryption (RSA)
- `CryptoRSAHelper` — RSA encrypt/decrypt and sign/verify
- `RSACryptoServiceProviderExtension` — helper extensions for `RSACryptoServiceProvider`
- `RsaKeys` — key pair container (public + private)
- `RsaMessage` — encrypted message envelope

### Hashing
- `HashAlgorithmHelper` — MD5, SHA-1, SHA-256, SHA-512 convenience wrappers

### JWT
- `SignedJsonWebToken` — create and validate signed JSON Web Tokens

### Helpers
- `CryptographyHelper` — general-purpose cryptography utilities
- `EncryptedMessage` — generic encrypted payload wrapper

---

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Security
```

---

## Project Structure

```
Sta.Security/
├── AES/
│   └── RijndaelHelper.cs
├── RSA/
│   ├── CryptoRSAHelper.cs
│   ├── RSACryptoServiceProviderExtension.cs
│   ├── RsaKeys.cs
│   └── RsaMessage.cs
├── Hashing/
│   └── HashAlgorithmHelper.cs
├── Services/
├── CryptographyHelper.cs
├── EncryptedMessage.cs
└── SignedJsonWebToken.cs
```

---

📦 **NuGet**: [IRI.Maptor.Sta.Security](https://www.nuget.org/packages/IRI.Maptor.Sta.Security)

🐞 **Issues**: [GitHub Issues](https://github.com/hosseinnarimanirad/Maptor/issues)

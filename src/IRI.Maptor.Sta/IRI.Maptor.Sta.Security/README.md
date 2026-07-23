# IRI.Maptor.Sta.Security

[![NuGet](https://img.shields.io/nuget/v/IRI.Maptor.Sta.Security?logo=nuget)](https://www.nuget.org/packages/IRI.Maptor.Sta.Security/)
[![Target](https://img.shields.io/badge/netstandard2.1-512BD4)](https://learn.microsoft.com/dotnet/standard/net-standard)

Cryptography and security primitives used across the Maptor ecosystem — AES encryption, RSA public-key encryption and key management, hashing, and HMAC-signed JSON Web Tokens. All types live in the `IRI.Maptor.Sta.Common.Security` namespace.

## Installation

```bash
dotnet add package IRI.Maptor.Sta.Security
```

## Features

- AES/Rijndael symmetric encryption: `RijndaelHelper.AesEncrypt`/`AesDecrypt` with explicit key/IV, plus password-based `Encrypt`/`Decrypt`
- RSA public-key cryptography: `CryptoRSAHelper` with `RsaEncrypt`/`RsaDecrypt`, key-pair generation in XML (`GenerateKeysInXml`) and PEM (`GenerateKeysInPem`), and key-format conversion helpers
- `RSACryptoServiceProviderExtension` — load public/private keys from DER blobs and PEM strings
- Key containers: `RsaKeys` (XML key pair) and `RsaMessage`/`EncryptedMessage` envelopes
- Hashing: `HashAlgorithmHelper` with MD5 and SHA-256 helpers (with optional salt) plus a generic `CalculateHash(input, HashAlgorithm)` for any algorithm
- JWT: `SignedJsonWebToken` — create (via `ToString()`) and validate (via `Parse`) HMAC-SHA256 signed tokens with claims, issuer, audience, and expiry checks
- `CryptographyHelper` — Base64/Base64URL utilities and random data generation
- Google OAuth service helper (`GoogleOAuthService`)

## Usage

Hashing:

```csharp
using IRI.Maptor.Sta.Common.Security;

string sha256 = HashAlgorithmHelper.CalculateSha256Hash("my-password");
string salted = HashAlgorithmHelper.GetMd5Hash("my-password", "my-salt");
```

RSA round trip:

```csharp
using IRI.Maptor.Sta.Common.Security;

var keys = CryptoRSAHelper.GenerateKeysInXml(keySize: 2048);

string cipher = CryptoRSAHelper.RsaEncrypt("secret message", keys.PublicKeyAsBase64Xml);
string plain  = CryptoRSAHelper.RsaDecrypt(cipher, keys.PrivateKeyAsBase64Xml);
```

AES with explicit key and IV:

```csharp
using IRI.Maptor.Sta.Common.Security;

string encrypted = RijndaelHelper.AesEncrypt("sensitive data", key, iv);
string decrypted = RijndaelHelper.AesDecrypt(encrypted, key, iv);
```

## Limitations

- RSA signing/verification helpers are not implemented — the RSA helpers cover encryption, decryption, and key management only.
- `SignedJsonWebToken` supports the HMAC-SHA256 (`HS256`) algorithm only.

---
[NuGet package](https://www.nuget.org/packages/IRI.Maptor.Sta.Security/) · [Report issues](https://github.com/hosseinnarimanirad/Maptor/issues) · [Back to IRI.Maptor.Sta](https://github.com/hosseinnarimanirad/Maptor/blob/master/src/IRI.Maptor.Sta/README.md)

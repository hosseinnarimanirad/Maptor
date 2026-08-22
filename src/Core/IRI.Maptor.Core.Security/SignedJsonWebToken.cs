using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using IRI.Maptor.Core.Common.Encodings;

namespace IRI.Maptor.Core.Security;

public class SignedJsonWebToken
{
    private const string TYPE_HEADER = "typ";
    private const string JSON_WEB_TOKEN = "JWT";
    private const string SIGNING_ALGORITHM_HEADER = "alg";
    private const string HMAC_SHA256 = "HS256";
    private const string EXPIRATION_TIME_CLAIM = "exp";
    private const string ISSUER_CLAIM = "iss";
    private const string AUDIENCE_CLAIM = "aud";
    private static readonly TimeSpan lifeTime = new TimeSpan(0, 2, 0);
    private static readonly DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
    private byte[] keyBytes = null;
    private Dictionary<string, string> claims = new Dictionary<string, string>();
    public SignedJsonWebToken()
    {
        TimeSpan ts = DateTime.UtcNow - epochStart + lifeTime;
        this.ExpiresOn = Convert.ToUInt64(ts.TotalSeconds);
    }
    [JsonPropertyName(TYPE_HEADER)]
    public string Type
    {
        get { return JSON_WEB_TOKEN; }
    }
    [JsonPropertyName(SIGNING_ALGORITHM_HEADER)]
    public string SignatureAlgorithm
    {
        get { return HMAC_SHA256; }
    }
    [JsonIgnore]
    public string SymmetricKey
    {
        get
        {
            return Convert.ToBase64String(keyBytes);
        }
        set
        {
            keyBytes = Convert.FromBase64String(value);
        }
    }
    [JsonIgnore]
    public IList<Claim> Claims
    {
        get
        {
            return this.claims.Keys.SelectMany(key =>
            this.claims[key].Split(',')
            .Select(value => new Claim(key, value))).ToList();
        }
    }
    [JsonIgnore]
    public ulong ExpiresOn
    {
        get
        {
            return UInt64.Parse(this.claims[EXPIRATION_TIME_CLAIM]);
        }
        private set
        {
            this.claims.Add(EXPIRATION_TIME_CLAIM, value.ToString());
        }
    }
    [JsonIgnore]
    public string Issuer
    {
        get
        {
            return this.claims.ContainsKey(ISSUER_CLAIM) ? this.claims[ISSUER_CLAIM] : String.Empty;
        }
        set
        {
            this.claims.Add(ISSUER_CLAIM, value);
        }
    }
    [JsonIgnore]
    public string Audience
    {
        get
        {
            return this.claims.ContainsKey(AUDIENCE_CLAIM) ?
            this.claims[AUDIENCE_CLAIM] :
            String.Empty;
        }
        set
        {
            this.claims.Add(AUDIENCE_CLAIM, value);
        }
    }
    public void AddClaim(string claimType, string value)
    {
        if (this.claims.ContainsKey(claimType))
            this.claims[claimType] = this.claims[claimType] + "," + value;
        else
            this.claims.Add(claimType, value);
    }
    // Class code not complete

    public override string ToString()
    {
        string header = JsonSerializer.Serialize(this).ToBase64UrlString();
        string claims = JsonSerializer.Serialize(this.claims).ToBase64UrlString();
        string signature = String.Empty;
        using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
        {
            string data = String.Format("{0}.{1}", header, claims);
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            signature = signatureBytes.ToBase64UrlString();
        }
        return String.Format("{0}.{1}.{2}", header, claims, signature);
    }

    public static SignedJsonWebToken Parse(string token, string secretKey)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            throw new SecurityException("Bad token");
        string header = Encoding.UTF8.GetString(parts[0].ToByteArray());
        string claims = Encoding.UTF8.GetString(parts[1].ToByteArray());
        byte[] incomingSignature = parts[2].ToByteArray();
        string computedSignature = String.Empty;
        var jwt = JsonSerializer.Deserialize<SignedJsonWebToken>(header);
        jwt.SymmetricKey = secretKey;
        jwt.claims = JsonSerializer.Deserialize<Dictionary<string, string>>(claims);
        using (HMACSHA256 hmac = new HMACSHA256(Convert.FromBase64String(secretKey)))
        {
            string data = String.Format("{0}.{1}", parts[0], parts[1]);
            byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            computedSignature = signatureBytes.ToBase64UrlString();
        }
        if (!computedSignature.Equals(incomingSignature.ToBase64UrlString(), StringComparison.Ordinal))
            throw new SecurityException("Signature is invalid");
        TimeSpan ts = DateTime.UtcNow - epochStart;
        if (jwt.ExpiresOn < Convert.ToUInt64(ts.TotalSeconds))
            throw new SecurityException("Token has expired");
        return jwt;
    }
}
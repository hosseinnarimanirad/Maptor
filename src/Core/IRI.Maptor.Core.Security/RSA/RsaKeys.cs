using IRI.Maptor.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Security;

public class RsaKeys
{
    public RsaKeys(string privateKeyXml, string publicKeyXml)
    {
        PrivateKeyXml = privateKeyXml;

        PublicKeyXml = publicKeyXml;
    }

    public string PrivateKeyXml { get; private set; }

    public string PublicKeyXml { get; private set; }

    public string PrivateKeyAsBase64Xml { get { return PrivateKeyXml.AsBase64String(); } }

    public string PublicKeyAsBase64Xml { get { return PublicKeyXml.AsBase64String(); } }
}

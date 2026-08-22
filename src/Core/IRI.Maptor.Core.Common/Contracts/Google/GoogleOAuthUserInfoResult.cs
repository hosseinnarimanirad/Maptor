
using System;
using System.Collections.Generic;
using System.Text;

namespace IRI.Maptor.Core.Common.Contracts.Google;

//[JsonObject]
public class GoogleOAuthUserInfoResult
{
    //[JsonProperty]
    public string sub { get; set; }

    //[JsonProperty]
    public string name { get; set; }

    //[JsonProperty]
    public string given_name { get; set; }

    //[JsonProperty]
    public string family_name { get; set; }

    //[JsonProperty]
    public string profile { get; set; }

    //[JsonProperty]
    public string picture { get; set; }

    //[JsonProperty]
    public string email { get; set; }

    //[JsonProperty]
    public bool email_verified { get; set; }

    //[JsonProperty]
    public string gender { get; set; }

    //[JsonProperty]
    public string locale { get; set; }
}

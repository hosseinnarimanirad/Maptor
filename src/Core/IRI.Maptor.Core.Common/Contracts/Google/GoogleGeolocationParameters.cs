using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace IRI.Maptor.Core.Common.Contracts.Google;

////[JsonObject]
public class GoogleGeolocationParameters
{

    //[DataMember]
    //[JsonProperty]
    public int? homeMobileCountryCode { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int? homeMobileNetworkCode { get; set; }

    //[DataMember]
    //[JsonProperty]
    public string radioType { get; set; }

    //[DataMember]
    //[JsonProperty]
    public string carrier { get; set; }

    //[DataMember]
    //[JsonProperty]
    public string considerIp { get; set; }

    //[DataMember]
    //[JsonProperty]
    public Celltower[] cellTowers { get; set; }

    //[DataMember]
    //[JsonProperty]
    public Wifiaccesspoint[] wifiAccessPoints { get; set; }
}

//[DataContract]
////[JsonObject]
public class Celltower
{

    //[DataMember]
    //[JsonProperty]
    public int cellId { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int locationAreaCode { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int mobileCountryCode { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int mobileNetworkCode { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int age { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int signalStrength { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int timingAdvance { get; set; }
}

//[DataContract]
////[JsonObject]
public class Wifiaccesspoint
{

    //[DataMember]
    //[JsonProperty]
    public string macAddress { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int signalStrength { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int age { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int channel { get; set; }

    //[DataMember]
    //[JsonProperty]
    public int signalToNoiseRatio { get; set; }
}

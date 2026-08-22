using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Spatial.Services.Waze;

 
public class WazeGeoRssResult
{
    public Alert[] alerts { get; set; }
    public long endTimeMillis { get; set; }
    public long startTimeMillis { get; set; }
    public string startTime { get; set; }
    public string endTime { get; set; }
    public Jam[] jams { get; set; }
    public User[] users { get; set; }
}

public class Alert
{
    public string reportBy { get; set; }
    public string country { get; set; }
    public int nThumbsUp { get; set; }
    public bool inscale { get; set; }
    public Comment[] comments { get; set; }
    public bool isJamUnifiedAlert { get; set; }
    public string city { get; set; }
    public int reportRating { get; set; }
    public int confidence { get; set; }
    public int reliability { get; set; }
    public int nImages { get; set; }
    public string type { get; set; }
    public string uuid { get; set; }
    public int speed { get; set; }
    public int reportMood { get; set; }
    public int magvar { get; set; }
    public bool showFacebookPic { get; set; }
    public string subtype { get; set; }
    public string wazeData { get; set; }
    public string reportDescription { get; set; }
    public Location location { get; set; }
    public string id { get; set; }
    public int nComments { get; set; }
    public long pubMillis { get; set; }
    public string street { get; set; }
    public int roadType { get; set; }
}

public class Location
{
    public float x { get; set; }
    public float y { get; set; }
}

public class Comment
{
    public string reportBy { get; set; }
    public long reportMillis { get; set; }
    public bool isThumbsUp { get; set; }
    public string text { get; set; }
}

public class Jam
{
    public string country { get; set; }
    public string city { get; set; }
    public Line[] line { get; set; }
    public float speedKMH { get; set; }
    public string type { get; set; }
    public int blockingAlertID { get; set; }
    public long blockExpiration { get; set; }
    public int uuid { get; set; }
    public string endNode { get; set; }
    public float speed { get; set; }
    public Segment[] segments { get; set; }
    public int id { get; set; }
    public long blockStartTime { get; set; }
    public long blockUpdate { get; set; }
    public int severity { get; set; }
    public int level { get; set; }
    public string blockType { get; set; }
    public int length { get; set; }
    public string turnType { get; set; }
    public string blockingAlertUuid { get; set; }
    public int roadType { get; set; }
    public int delay { get; set; }
    public string blockDescription { get; set; }
    public long updateMillis { get; set; }
    public long pubMillis { get; set; }
    public string street { get; set; }
    public Causealert causeAlert { get; set; }
}

public class Causealert
{
    public string country { get; set; }
    public int nThumbsUp { get; set; }
    public string city { get; set; }
    public int reportRating { get; set; }
    public int reliability { get; set; }
    public string type { get; set; }
    public string uuid { get; set; }
    public int speed { get; set; }
    public int reportMood { get; set; }
    public string subtype { get; set; }
    public string street { get; set; }
    public string id { get; set; }
    public int nComments { get; set; }
    public string reportBy { get; set; }
    public bool inscale { get; set; }
    public Comment[] comments { get; set; }
    public bool isJamUnifiedAlert { get; set; }
    public int confidence { get; set; }
    public int nImages { get; set; }
    public int magvar { get; set; }
    public bool showFacebookPic { get; set; }
    public string wazeData { get; set; }
    public string reportDescription { get; set; }
    public Location location { get; set; }
    public long pubMillis { get; set; }
}

 
public class Line
{
    public float x { get; set; }
    public float y { get; set; }
}

public class Segment
{
    public int fromNode { get; set; }
    public int ID { get; set; }
    public int toNode { get; set; }
    public bool isForward { get; set; }
}

public class User
{
    public string fleet { get; set; }
    public int magvar { get; set; }
    public bool inscale { get; set; }
    public int mood { get; set; }
    public int addon { get; set; }
    public int ping { get; set; }
    public Location location { get; set; }
    public string id { get; set; }
    public string userName { get; set; }
    public float speed { get; set; }
    public bool ingroup { get; set; }
}



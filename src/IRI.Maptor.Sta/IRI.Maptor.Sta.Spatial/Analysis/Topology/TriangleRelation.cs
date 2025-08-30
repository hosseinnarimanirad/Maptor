
namespace IRI.Maptor.Sta.Spatial.Topology;

//[Flags]
public enum TriangleRelation
{
    Nothing = 0,
    MeetAtFirst = 1,
    MeetAtSecond = 2,
    MeetAtThird = 4,
    //Meet = MeetAtFirst | MeetAtSecond | MeetAtThird,
    FirstSecondNeighbour = 3,
    SecondThirdNeighbour = 6,
    ThirdFirstNeighbour = 5,
    //Neighbour = FirstSecondNeighbour | SecondThirdNeighbour | ThirdFirstNeighbour,
    Equal =7,
}

using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.Common.Primitives;

namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class KdTreePointClusters<T> where T : IPoint
{
    public List<T> _allSingleMembers;
    //BalancedKdTree<T> _allSingleMembers;

    public BalancedKdTree<T> _all;

    public BalancedKdTree<Group<T>> Groups { get; private set; }
    //public List<Group<T>> Groups { get; private set; }



    private Group<T> _nilValue;

    private KdTreePointClusters()
    {

    }

    public KdTreePointClusters(List<T> points, Group<T> nilValue)
    {
        this._nilValue = nilValue;
        //this._allSingleMembers = new BalancedKdTree<T>(points.ToArray(), funcs.ToList(), nilValue);
        this._allSingleMembers = points;
    }

    public void GetClusters(Func<T, T, bool> groupLogic)
    {
        Func<Group<T>, Group<T>, int> xWise = (p1, p2) => p1.Center.X.CompareTo(p2.Center.X);
        Func<Group<T>, Group<T>, int> yWise = (p1, p2) => p1.Center.Y.CompareTo(p2.Center.Y);
        Func<Group<T>, Group<T>, int>[] funcs = { xWise, yWise };

        //this.Groups = new List<Group<T>>();

        if (_allSingleMembers?.Count > 0)
        {
            this.Groups = new BalancedKdTree<Group<T>>(new Group<T>[] { new Group<T>(_allSingleMembers[0]) }, funcs.ToList(), _nilValue, g => g.Center);

            //Groups.Add(new Group<T>(_allSingleMembers[0]));

            for (int i = 1; i < _allSingleMembers.Count; i++)
            {
                bool hasGroup = false;

                var tempGroup = new Group<T>(_allSingleMembers[i]);

                var nearestGroup = Groups.FindNearestNeighbour(tempGroup, ((g1, g2) => IRI.Maptor.Sta.Spatial.Analysis.SpatialUtility.GetEuclideanDistance(g1.Center, g2.Center)));

                if (groupLogic(nearestGroup.Center, _allSingleMembers[i]))
                {
                    nearestGroup.Add(_allSingleMembers[i]);

                    hasGroup = true;
                }

                //bool hasGroup = false;
                //for (int g = 0; g < Groups.Count; g++)
                //{
                //if (groupLogic(Groups[g].Center, _allSingleMembers[i]))
                //{
                //    Groups[g].Add(_allSingleMembers[i]);

                //    hasGroup = true;

                //    break;
                //}
                //}

                if (!hasGroup)
                {
                    MakeNewGroup(_allSingleMembers[i]);
                }
            }
        }
    }

    private void MakeNewGroup(T center)
    {
        //this.Groups.Add(new Group<T>(center));
        if (double.IsNaN(center.X + center.Y) || double.IsInfinity(center.X + center.Y))
        {


        }

        this.Groups.Insert(new Group<T>(center));
    }

    //public int GroupCount
    //{
    //    get { return this.Groups.Count; }
    //}

    public List<T> GetGroupCenters()
    {
        return this.Groups.GetAllValues().Select(i => i.Center).ToList();
    }


    public static List<T> GetClusterCenters(List<T> points, T nilValue, double radius)
    {
        try
        {

            //var result = new KdTreePointClusters<T>();
            Func<T, T, int> xWise = (p1, p2) => p1.X.CompareTo(p2.X);
            Func<T, T, int> yWise = (p1, p2) => p1.Y.CompareTo(p2.Y);
            Func<T, T, int>[] funcs = { xWise, yWise };

            HashSet<T> set = new HashSet<T>();

            var kdtree = new BalancedKdTree<T>(points, funcs.ToList(), nilValue, i => i);

            if (!(points?.Count > 0))
                return new List<T>();

            List<T> result = new List<T>();

            for (int i = 0; i < points.Count; i++)
            {
                try
                {

                    if (set.Contains(points[i]))
                    {
                        continue;
                    }

                    result.Add(points[i]);

                    var neighbours = kdtree.FindNeighbours(points[i], radius);

                    set.Add(points[i]);

                    set.UnionWith(neighbours);
                    //for (int j = 0; j < neighbours?.Count; j++)
                    //{
                    //    set.Add(neighbours[j]);
                    //}

                }
                catch (Exception)
                {
                    return new List<T>();
                }
            }

            return result;

        }
        catch (Exception)
        {
            return new List<T>();
        }
    }


}

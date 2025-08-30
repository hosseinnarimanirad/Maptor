namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class PointClusters<T>  
{
    public List<T> _allSingleMembers;

    public List<Group<T>> Groups { get; private set; }

    public PointClusters(List<T> points)
    {
        this._allSingleMembers = points;
    }

    public List<Group<T>> GetClusters(Func<T, T, bool> groupLogic)
    {
        this.Groups = new List<Group<T>>();

        if (_allSingleMembers.Count > 0)
        {
            Groups.Add(new Group<T>(_allSingleMembers[0]));

            for (int i = 1; i < _allSingleMembers.Count; i++)
            {
                bool hasGroup = false;

                for (int g = 0; g < Groups.Count; g++)
                {
                    if (groupLogic(Groups[g].Center, _allSingleMembers[i]))
                    {
                        Groups[g].Add(_allSingleMembers[i]);

                        hasGroup = true;

                        break;
                    }
                }

                if (!hasGroup)
                {
                    MakeNewGroup(_allSingleMembers[i]);
                }
            }
        }

        return Groups;
    }

    private void MakeNewGroup(T center)
    {
        this.Groups.Add(new Group<T>(center));
    }

    public int GroupCount
    {
        get { return this.Groups.Count; }
    }

    public List<T> GetGroupCenters()
    {
        return this.Groups.Select(i => i.Center).ToList();
    }


}

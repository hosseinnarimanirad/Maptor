using IRI.Maptor.Extensions;
using IRI.Maptor.Sta.Spatial.Analysis; 
using IRI.Maptor.Sta.Common.Abstrations;
using IRI.Maptor.Sta.DataStructures.Trees; 

namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class BalancedKdTree<T>
{
    private BalancedKdTreeNode<T> root;

    public BalancedKdTreeNode<T> Root
    {
        get { return this.root; }
        set
        {
            this.root = value;
            this.root.Parent = null;
        }
    }

    private static T _nilValue;

    private static Func<T, IPoint> _pointFunc;

    public static Func<T, IPoint> PointFunc
    {
        get
        {
            return _pointFunc;
        }
    }

    private static BalancedKdTreeNode<T> _nilNode;//= BalancedKdTreeNode<T>.nilNode;

    public static BalancedKdTreeNode<T> NilNode
    {
        get
        {
            if (_nilNode == null)
            {
                _nilNode = BalancedKdTreeNode<T>.CreateNilNode(_nilValue);
            }

            return _nilNode;
        }
    }

    List<Func<T, T, int>> comparers;

    public BalancedKdTree(IEnumerable<T> values, List<Func<T, T, int>> comparers, T nilValue, Func<T, IPoint> pointFunc)
    {
        if (values.IsNullOrEmpty() || comparers.IsNullOrEmpty())
        {
            return;
        }

        _pointFunc = pointFunc == null ? i => (IPoint)i : pointFunc;

        _nilValue = nilValue;
        //if (_nilNode == null)
        //{
        //    _nilNode = new BalancedKdTreeNode<T>(nilValue, NodeColor.Black);
        //}

        if (values?.Any() != true)
            return;

        this.Root = new BalancedKdTreeNode<T>(values.First(), NodeColor.Black);
        //this.Root.LeftChild = nilNode;

        //this.Root.RigthChild = nilNode;

        this.comparers = comparers;

        foreach (var item in values)
        {
            Insert(item);
        }

        //for (int i = 1; i < values.Length; i++)
        //{
        //    Insert(values[i]);
        //}
    }

    public void Insert(T value)
    {
        BalancedKdTreeNode<T> node = new BalancedKdTreeNode<T>(value, NodeColor.Red) { Parent = this.Root };
        //node.LeftChild = nilNode;
        //node.RigthChild = nilNode;

        Add(this.Root, node);

        InsertFixup(node);
    }

    private void Add(BalancedKdTreeNode<T> parent, BalancedKdTreeNode<T> childValue)
    {
        int functionIndex = GetNodeLevel(parent) % comparers.Count;

        if (this.comparers[functionIndex](parent.Point, childValue.Point) >= 0)
        {
            if (parent.LeftChild != NilNode)
            {
                Add(parent.LeftChild, childValue);
            }
            else
            {
                parent.LeftChild = childValue;
            }
        }
        else
        {
            if (parent.RightChild != NilNode)
            {
                Add(parent.RightChild, childValue);
            }
            else
            {
                parent.RightChild = childValue;
            }
        }

    }

    private void InsertFixup(BalancedKdTreeNode<T> node)
    {
        while (node.Parent.Color == NodeColor.Red)
        {
            if (object.ReferenceEquals(node.Parent, node.Parent.Parent.LeftChild))
            {
                BalancedKdTreeNode<T> y = node.Parent.Parent.RightChild;

                if (y.Color == NodeColor.Red)
                {
                    node.Parent.Color = NodeColor.Black;

                    y.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    node = node.Parent.Parent;
                }
                else if (object.ReferenceEquals(node, node.Parent.RightChild))
                {
                    node = node.Parent;

                    LeftRotate(node);
                }
                else
                {
                    node.Parent.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    RigthRotate(node.Parent.Parent);
                }
            }
            else if (object.ReferenceEquals(node.Parent, node.Parent.Parent.RightChild))
            {
                BalancedKdTreeNode<T> y = node.Parent.Parent.LeftChild;

                if (y.Color == NodeColor.Red)
                {
                    node.Parent.Color = NodeColor.Black;

                    y.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    node = node.Parent.Parent;
                }
                else if (object.ReferenceEquals(node, node.Parent.LeftChild))
                {
                    node = node.Parent;

                    RigthRotate(node);
                }
                else
                {
                    node.Parent.Color = NodeColor.Black;

                    node.Parent.Parent.Color = NodeColor.Red;

                    LeftRotate(node.Parent.Parent);
                }
            }
            if (node.Parent == null)
            {
                break;
            }
        }

        this.Root.Color = NodeColor.Black;
    }

    private void LeftRotate(BalancedKdTreeNode<T> node)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            throw new NotImplementedException();
        }

        BalancedKdTreeNode<T> rigthChild = node.RightChild;

        node.RightChild = rigthChild.LeftChild;

        if (node.Parent == null)
        {
            this.Root = rigthChild;
        }
        else if (object.ReferenceEquals(node, node.Parent.LeftChild))
        {
            node.Parent.LeftChild = rigthChild;
        }
        else
        {
            node.Parent.RightChild = rigthChild;
        }

        rigthChild.LeftChild = node;
    }

    private void RigthRotate(BalancedKdTreeNode<T> node)
    {
        if (node.LeftChild == null || node.RightChild == null)
        {
            throw new NotImplementedException();
        }

        BalancedKdTreeNode<T> leftChild = node.LeftChild;

        node.LeftChild = leftChild.RightChild;

        if (node.Parent == null)
        {
            this.Root = leftChild;
        }
        else if (object.ReferenceEquals(node, node.Parent.LeftChild))
        {
            node.Parent.LeftChild = leftChild;
        }
        else
        {
            node.Parent.RightChild = leftChild;
        }

        leftChild.RightChild = node;
    }

    private int GetNodeLevel(BalancedKdTreeNode<T> node)
    {
        if (object.Equals(null, node.Parent))
        {
            return 0;
        }
        else
        {
            return GetNodeLevel(node.Parent) + 1;
        }
    }

    //Nearest Neighbour
    public T FindNearestNeighbour(T point, Func<T, T, double> distanceFunc = null)
    {
        if (distanceFunc == null)
        {
            distanceFunc = (p1, p2) => SpatialUtility.GetEuclideanDistance(PointFunc(p1), PointFunc(p2));
        }

        var minDistance = distanceFunc(this.Root.Point, point);

        return FindNearestNeighbour(point, minDistance, Root, distanceFunc);
    }

    private T FindNearestNeighbour(T point, double distance, BalancedKdTreeNode<T> node, Func<T, T, double> distanceFunc)
    {
        double minDistance = Math.Min(distance, distanceFunc(point, node.Point));

        T result = node.Point;

        if (node.LeftChild != null &&
            !node.LeftChild.IsNilNode() &&
            SpatialUtility.CircleRectangleIntersects(PointFunc(point), minDistance, node.LeftChild.MinimumBoundingBox))
        {
            var newPoint = FindNearestNeighbour(point, minDistance, node.LeftChild, distanceFunc);

            if (distanceFunc(point, newPoint) < minDistance)
            {
                minDistance = distanceFunc(point, newPoint);

                result = newPoint;
            }
        }

        if (node.RightChild != null &&
            !node.RightChild.IsNilNode() &&
            SpatialUtility.CircleRectangleIntersects(PointFunc(point), minDistance, node.RightChild.MinimumBoundingBox))
        {
            var newPoint = FindNearestNeighbour(point, minDistance, node.RightChild, distanceFunc);

            if (distanceFunc(point, newPoint) < minDistance)
            {
                minDistance = distanceFunc(point, newPoint);

                result = newPoint;
            }
        }

        return result;
    }

    //Range
    public List<T> FindNeighbours(T point, double distance, Func<T, T, double> distanceFunc = null)
    {
        if (distanceFunc == null)
        {
            distanceFunc = (p1, p2) => SpatialUtility.GetEuclideanDistance(PointFunc(p1), PointFunc(p2));
        }

        return FindNeighbours(point, distance, Root, distanceFunc);
    }

    private List<T> FindNeighbours(T point, double radius, BalancedKdTreeNode<T> node, Func<T, T, double> distanceFunc = null)
    {
        var result = new List<T>();

        //if (distanceFunc(point, node.Point) <= radius)
        //{
        //    result.Add(node.Point);
        //}

        if (node.LeftChild != null && !node.LeftChild.IsNilNode())
        {
            var relation = SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(PointFunc(point), radius, node.LeftChild.MinimumBoundingBox);

            if (relation == IRI.Maptor.Sta.Common.Enums.SpatialRelation.Contained)
            {
                result.AddRange(GetAllValues(node.LeftChild));
            }
            else if (relation == IRI.Maptor.Sta.Common.Enums.SpatialRelation.Intersects)
            {
                result.AddRange(FindNeighbours(point, radius, node.LeftChild, distanceFunc));
            }
        }

        if (node.RightChild != null && !node.RightChild.IsNilNode())
        {
            var relation = SpatialUtility.CalculateAxisAlignedRectangleRelationToCircle(PointFunc(point), radius, node.RightChild.MinimumBoundingBox);

            if (relation == IRI.Maptor.Sta.Common.Enums.SpatialRelation.Contained)
            {
                result.AddRange(GetAllValues(node.RightChild));
            }
            else if (relation == IRI.Maptor.Sta.Common.Enums.SpatialRelation.Intersects)
            {
                result.AddRange(FindNeighbours(point, radius, node.RightChild, distanceFunc));
            }
        }

        return result;
    }

    public List<T> GetAllValues()
    {
        return GetAllValues(Root);
    }

    private List<T> GetAllValues(BalancedKdTreeNode<T> node)
    {
        var result = new List<T>();

        result.Add(node.Point);

        if (node.LeftChild != null && !node.LeftChild.IsNilNode())
        {
            result.AddRange(GetAllValues(node.LeftChild));
        }

        if (node.RightChild != null && !node.RightChild.IsNilNode())
        {
            result.AddRange(GetAllValues(node.RightChild));
        }

        return result;
    }
}

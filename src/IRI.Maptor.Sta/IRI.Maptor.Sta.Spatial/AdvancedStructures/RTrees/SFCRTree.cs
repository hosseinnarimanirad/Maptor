using IRI.Maptor.Sta.Common.Primitives;
using IRI.Maptor.Sta.Spatial.Analysis.SFC;

namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class SFCRTree
{
    public RTreeNode Root;

    private Func<Rectangle, Rectangle, Boundary, int> comparer;

    public static int HilbertComparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.HilbertComparer(firstPoint, secondPoint, boundary);
    }

    public static int NOrderingComparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.NOrderingComparer(firstPoint, secondPoint, boundary);
    }

    public static int GrayComparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.GrayComparer(firstPoint, secondPoint, boundary);
    }

    public static int ZOrderingComparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.ZOrderingComparer(firstPoint, secondPoint, boundary);
    }

    public static int DiagonalLebesgueComparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.DiagonalLebesgueComparer(firstPoint, secondPoint, boundary);
    }

    public static int UOrderOrLebesgueSquareComparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.UOrderOrLebesgueSquareComparer(firstPoint, secondPoint, boundary);
    }

    public static int PeanoComparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.PeanoComparer(firstPoint, secondPoint, boundary);
    }

    public static int Peano02Comparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.Peano02Comparer(firstPoint, secondPoint, boundary);
    }

    public static int Peano03Comparer(Rectangle first, Rectangle second, Boundary boundary)
    {
        Point firstPoint = new Point(first.CenterX, first.CenterY);

        Point secondPoint = new Point(second.CenterX, second.CenterY);

        return PointOrdering.Peano03Comparer(firstPoint, secondPoint, boundary);
    }


    private SFCRTree(Rectangle[] values, int minimumDegree)
        : this(values, HilbertComparer, minimumDegree) { }

    public SFCRTree(Rectangle[] values, Func<Rectangle, Rectangle, Boundary, int> comparer, int minimumDegree)
    {
        Root = new RTreeNode();

        this.minimumDegree = minimumDegree;

        this.comparer = comparer;

        Root.IsLeaf = true;

        foreach (Rectangle item in values)
        {
            Insert(item);
        }
    }

    int minimumDegree;

    public int MinimumDegree
    {
        get { return minimumDegree; }
    }

    private Boundary boundary;

    public Boundary Boundary
    {
        get
        {
            return boundary;
        }
    }

    //Untested
    public void Insert(Rectangle key)
    {
        //
        Rectangle newBoundary = Root.Boundary.Add(key);

        Point lowerLeft =
             new Point(newBoundary.minX, newBoundary.minY);

        Point upperRight =
           new Point(newBoundary.maxX, newBoundary.maxY);

        boundary = new Boundary(lowerLeft, upperRight);
        //

        RTreeNode r = Root;

        if (r.NumberOfKeys == 2 * MinimumDegree - 1)
        {
            RTreeNode s = new RTreeNode();

            Root = s;

            s.IsLeaf = false;

            s.AddPointer(r);

            SplitTheNode(s, 0);

            InsertNonFull(s, key);
        }
        else
        {
            InsertNonFull(r, key);
        }
    }

    //Untested
    public void SplitTheNode(RTreeNode parent, int childIndex)
    {
        RTreeNode y = parent.GetPointer(childIndex);

        int t = MinimumDegree;

        RTreeNode z = new RTreeNode();

        z.IsLeaf = y.IsLeaf;

        //z.AddRangeKeys(y.SkipKeys(t));

        if (y.IsLeaf)
        {
            z.AddRangeKeys(y.SkipKeys(t));

            //z.AddRangePointers(y.SkipPointers(t));

            y.RemoveRangeKeys(t, t - 1);
        }
        else
        {
            z.AddRangePointers(y.SkipPointers(t));

            y.RemoveRangePointers(t, t - 1);
        }

        parent.InsertPointer(childIndex + 1, z);
    }

    //Untested
    public void InsertNonFull(RTreeNode x, Rectangle k)
    {
        int i = x.NumberOfKeys - 1;

        if (x.IsLeaf)
        {
            while (i > 0 && comparer(k, x.GetKey(i), Boundary) < 0)
            {
                i--;
            }

            x.InsertKey(i + 1, k);
        }
        else
        {
            while (i > 0 && comparer(k, x.GetPointer(i).Boundary, Boundary) < 0)
            {
                i--;
            }

            if (x.GetPointer(i).NumberOfKeys == 2 * MinimumDegree - 1)
            {
                SplitTheNode(x, i);

                if (comparer(k, x.GetPointer(i).Boundary, Boundary) > 0)
                {
                    i++;
                }
            }

            InsertNonFull(x.GetPointer(i), k);

            x.FixBoundary();
        }
    }
}
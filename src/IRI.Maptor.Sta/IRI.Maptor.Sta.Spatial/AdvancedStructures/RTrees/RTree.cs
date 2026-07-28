using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IRI.Maptor.Sta.Spatial.AdvancedStructures;

public class RTree
{
    public RTreeNode Root;

    private Func<IEnumerable<Rectangle>, Rectangle, int> comparer;

    public static int FindTheBestRectangle(IEnumerable<Rectangle> rectangles, Rectangle rectangle)
    {
        int result = 0, i = 0;

        double tempValue = double.MaxValue;

        foreach (Rectangle item in rectangles)
        {
            double areaEnlargment = item.GetEnlargementArea(rectangle);// temp.GetArea() - rectangles[i].GetArea();

            if (areaEnlargment < tempValue)
            {
                result = i;

                tempValue = areaEnlargment;
            }
            i++;
        }

        return result;
    }

    public RTree(Rectangle[] values, int minimumDegree)
    {
        Root = new RTreeNode();

        this.minimumDegree = minimumDegree;

        comparer = FindTheBestRectangle;

        Root.IsLeaf = true;

        foreach (Rectangle item in values)
        {
            Insert(item);
        }
    }

    public RTree(RTreeNode root, int minimumDegree)
        : this(root, FindTheBestRectangle, minimumDegree) { }

    public RTree(RTreeNode root, Func<IEnumerable<Rectangle>, Rectangle, int> comparer, int minimumDegree)
    {
        if (minimumDegree < 2)
        {
            throw new NotImplementedException();
        }

        this.comparer = comparer;

        Root = root;

        Root.IsLeaf = true;

        this.minimumDegree = minimumDegree;
    }

    int minimumDegree;

    public int MinimumDegree
    {
        get { return minimumDegree; }
    }

    //Untested
    public void Insert(Rectangle key)
    {
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

        //this.Root.FixBoundary();
    }

    //Untested
    public void SplitTheNode(RTreeNode parent, int childIndex)
    {
        RTreeNode y = parent.GetPointer(childIndex);
        parent.RemovePointer(y);

        int t = MinimumDegree;

        int firstChild, secondChild;

        y.FindTheMostDistantRectangles(out firstChild, out secondChild);

        RTreeNode firstNode = new RTreeNode(); firstNode.IsLeaf = y.IsLeaf;

        RTreeNode secondNode = new RTreeNode(); secondNode.IsLeaf = y.IsLeaf;

        if (y.IsLeaf)
        {
            firstNode.AddKey(y.GetKey(firstChild)); secondNode.AddKey(y.GetKey(secondChild));

            for (int i = 0; i < y.NumberOfKeys; i++)
            {
                if (i == firstChild || i == secondChild)
                    continue;

                if (firstNode.NumberOfKeys == y.NumberOfKeys - t)
                {
                    secondNode.AddKey(y.GetKey(i));
                }
                else if (secondNode.NumberOfKeys == y.NumberOfKeys - t)
                {
                    firstNode.AddKey(y.GetKey(i));
                }
                else
                {
                    double firstEnlargement = firstNode.Boundary.GetEnlargementArea(y.GetKey(i));

                    double secondEnlargement = secondNode.Boundary.GetEnlargementArea(y.GetKey(i));

                    if (firstEnlargement < secondEnlargement)
                    {
                        firstNode.AddKey(y.GetKey(i));
                    }
                    else if (firstEnlargement > secondEnlargement)
                    {
                        secondNode.AddKey(y.GetKey(i));
                    }
                    else
                    {
                        if (firstNode.Boundary.GetArea() > secondNode.Boundary.GetArea())
                        {
                            secondNode.AddKey(y.GetKey(i));
                        }
                        else
                        {
                            firstNode.AddKey(y.GetKey(i));
                        }
                    }
                }
            }
        }
        else
        {
            firstNode.AddPointer(y.GetPointer(firstChild)); secondNode.AddPointer(y.GetPointer(secondChild));

            for (int i = 0; i < y.NumberOfKeys; i++)
            {
                if (i == firstChild || i == secondChild)
                    continue;

                if (firstNode.NumberOfKeys == y.NumberOfKeys - t)
                {
                    secondNode.AddPointer(y.GetPointer(i));
                }
                else if (secondNode.NumberOfKeys == y.NumberOfKeys - t)
                {
                    firstNode.AddPointer(y.GetPointer(i));
                }
                else
                {
                    double firstEnlargement = firstNode.Boundary.GetEnlargementArea(y.GetPointer(i).Boundary);

                    double secondEnlaregement = secondNode.Boundary.GetEnlargementArea(y.GetPointer(i).Boundary);

                    if (firstEnlargement < secondEnlaregement)
                    {
                        firstNode.AddPointer(y.GetPointer(i));
                    }
                    else if (firstEnlargement > secondEnlaregement)
                    {
                        secondNode.AddPointer(y.GetPointer(i));
                    }
                    else
                    {
                        if (firstNode.Boundary.GetArea() > secondNode.Boundary.GetArea())
                        {
                            secondNode.AddPointer(y.GetPointer(i));
                        }
                        else
                        {
                            firstNode.AddPointer(y.GetPointer(i));
                        }
                    }
                }
            }
        }

        parent.InsertPointer(childIndex, firstNode);

        parent.InsertPointer(childIndex + 1, secondNode);
    }

    //Untested
    public void InsertNonFull(RTreeNode x, Rectangle k)
    {
        if (x.IsLeaf)
        {
            x.AddKey(k);
        }
        else
        {
            int i = comparer(x.GetSubRectangles(), k);

            if (x.GetPointer(i).NumberOfKeys == 2 * MinimumDegree - 1)
            {
                SplitTheNode(x, i);
            }

            InsertNonFull(x.GetPointer(i), k);

            x.FixBoundary();
        }
    }
}
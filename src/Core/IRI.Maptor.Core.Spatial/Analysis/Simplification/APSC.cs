using IRI.Maptor.Core.Common.Abstractions;
using IRI.Maptor.Core.Common.Primitives;

namespace IRI.Maptor.Core.Spatial.Analysis;

public static class ApscSimplification<T> where T : IPoint, new()
{
    private static VertexRelation<T> InitVertexTree(List<T> pts)
    {
        var vertexRelation = new VertexRelation<T>();
        int n = pts.Count;

        // Initialize default values for each vertex
        for (int i = 0; i < n; i++)
        {
            vertexRelation.ID.Add(i);
            vertexRelation.XY.Add(pts[i]);
            vertexRelation.Error.Add(0);
            vertexRelation.Parent.Add(-1);
            vertexRelation.LC.Add(-1);
            vertexRelation.RC.Add(-1);
            vertexRelation.LS.Add(i - 1);
            vertexRelation.RS.Add(i + 1);
            vertexRelation.Current.Add(true);
        }

        // If not a closed loop, set start/end left/right siblings to -1
        vertexRelation.RS[n - 1] = -1;
        vertexRelation.LS[0] = -1;

        return vertexRelation;
    }

    private static void AddToPriorityList(
         int A,
         int B,
         int C,
         int D,
         VertexRelation<T> vtree,
         SortedSet<Tuple<double, int, int, int, int, T, int>> pl)
    {
        var XY = vtree.XY;

        // Make sure all vertices are current
        if (A != -1 && B != -1 && C != -1 && D != -1)
        {
            // Compute collapse point E and associated displacement error
            var result = GeomUtils<T>.CalculateNewPoint(XY[A], XY[B], XY[C], XY[D]);
            var pE = result.Item1;
            var displacement = result.Item2;
            var overlapEndpt = result.Item3;

            // Make sure collapse point is valid
            if (pE != null)
            {
                // Get index of overlapping endpoint
                if (overlapEndpt == 0)
                {
                    overlapEndpt = A;
                }
                if (overlapEndpt == 1)
                {
                    overlapEndpt = D;
                }

                // Add to priority list
                pl.Add(Tuple.Create(displacement, A, B, C, D, pE, overlapEndpt));
            }
        }
    }

    private static SortedSet<Tuple<double, int, int, int, int, T, int>> InitPriorityList(VertexRelation<T> vtree)
    {
        var ID = vtree.ID;
        var pl = new SortedSet<Tuple<double, int, int, int, int, T, int>>(new DisplacementComparer<T>());

        // Loop through collapsible line segments
        for (int i = 0; i < ID.Count - 3; i++)
        {
            AddToPriorityList(i, i + 1, i + 2, i + 3, vtree, pl);
        }

        return pl; // Return sorted priority list
    }

    private static void Collapse<T>(int B, int C, int E, T thisxy, double thiserror, VertexRelation<T> vtree) where T : IPoint
    {
        var ID = vtree.ID;
        var XY = vtree.XY;
        var error = vtree.Error;
        var parent = vtree.Parent;
        var LC = vtree.LC;
        var RC = vtree.RC;
        var LS = vtree.LS;
        var RS = vtree.RS;
        var current = vtree.Current;

        // add new row
        // id, xy, error, parent, LC, RC, LS, RS, current
        ID.Add(E);
        XY.Add(thisxy);
        error.Add((int)thiserror);
        parent.Add(-1);
        LC.Add(B);
        RC.Add(C);
        LS.Add(LS[B]);
        RS.Add(RS[C]);
        current.Add(true);

        // reset sibling relations to E
        if (LS[B] != -1)
        {
            RS[LS[B]] = E;
        }
        if (RS[C] != -1)
        {
            LS[RS[C]] = E;
        }

        // reset properties of B
        LS[B] = -1;
        RS[B] = C;
        parent[B] = E;
        current[B] = false;

        // reset properties of C
        LS[C] = B;
        RS[C] = -1;
        parent[C] = E;
        current[C] = false;
    }

    public static VertexRelation<T> SimplificationTable(List<T> pts)
    {
        // build vertex relations on initial points
        var vtree = InitVertexTree(pts);
        var ID = vtree.ID;
        var XY = vtree.XY;
        var E = vtree.Error;
        var parent = vtree.Parent;
        var LC = vtree.LC;
        var RC = vtree.RC;
        var LS = vtree.LS;
        var RS = vtree.RS;
        var current = vtree.Current;

        // build priority list of collapse events
        var pl = InitPriorityList(vtree);

        // build spatial index for topology check
        // segment indices are indices of first vertex
        // if (doTopoCheck)
        // {
        //     // create index of segments; use stream loading
        //     idx = new Index(SegmentBoundingBoxGenerator(XY, otherPtLists));
        // }

        // initialize values for loop
        int e = pts.Count - 1;
        double maxerror = 0;
        int collapsed = 0;
        int reportInterval = 2000;

        while (pl.Count > 0)
        {
            var item = pl.First();
            pl.Remove(item);

            var error = (double)item.Item1;
            var A = item.Item2;
            var B = item.Item3;
            var C = item.Item4;
            var D = item.Item5;
            var pE = item.Item6;
            var overlapEndpt = item.Item7;

            if (current[A] && current[B] && current[C] && current[D])
            {
                e += 1;
                maxerror = Math.Max(error, maxerror);


                // perform collapse
                Collapse(B, C, e, pE, maxerror, vtree);

                collapsed += 1;
                if (collapsed % reportInterval == 0)
                {
                    Console.WriteLine($"collapsed {collapsed} of {pts.Count}");
                }

                try
                {
                    AddToPriorityList(LS[LS[A]], LS[A], A, e, vtree, pl);
                    AddToPriorityList(LS[A], A, e, D, vtree, pl);
                    AddToPriorityList(A, e, D, RS[D], vtree, pl);
                    AddToPriorityList(e, D, RS[D], RS[RS[D]], vtree, pl);
                }
                catch
                {
                    Console.WriteLine($"Error at {e}");
                }

            }
        }

        return vtree;
    }


    public static List<T> SimplifiedLine<T>(VertexRelation<T> simpTable, double maxDisp = -1, int minPts = -1) where T : IPoint
    {
        var ID = simpTable.ID;
        var XY = simpTable.XY;
        var error = simpTable.Error;
        var parent = simpTable.Parent;
        var LC = simpTable.LC;
        var RC = simpTable.RC;
        var LS = simpTable.LS;
        var RS = simpTable.RS;
        var current = simpTable.Current;

        int maxID = ID.Count - 1;
        int origCount = 0;

        // determine original number of points,
        // maximum ID to allow based on displacement error criterion
        for (int i = 0; i < ID.Count; i++)
        {
            if (LC[i] == -1)
            {
                origCount++;
            }
            if (maxDisp >= 0 && error[i] <= maxDisp)
            {
                maxID = i;
            }
        }

        // determine max ID to allow based on minimum number of points
        if (minPts > -1)
        {
            int maxIDByMinPts = origCount - 1 + origCount - minPts;
            if (maxDisp > -1)
            {
                maxID = Math.Max(maxID, maxIDByMinPts);
            }
            else
            {
                maxID = maxIDByMinPts;
            }
        }

        // initialize output as empty list
        var outLine = new List<T>();
        int v = 0;

        // work to end
        while (v > -1)
        {
            if (v <= maxID)  // vertex is good
            {
                outLine.Add(XY[v]); // add to result

                // if we can't move right, move up
                while (v > -1 && RS[v] == -1)
                {
                    v = parent[v];
                }

                // move right if we're not at the end already
                if (v > -1)
                {
                    v = RS[v];
                }
            }
            else  // vertex needs to be expanded; move to left child
            {
                v = LC[v];
            }
        }

        // that's it!
        return outLine;
    }


    private class ApscBlock<T> where T : IPoint, new()
    {
        public T A { get; private set; }
        public T B { get; private set; }
        public T C { get; private set; }
        public T D { get; private set; }

        public T E { get; private set; }

        public double Epsilon { get; set; }

        private void UpdateMeasures()
        {
            var temp = GeomUtils<T>.CalculateNewPoint(A, B, C, D);

            this.E = temp.Item1;
            this.Epsilon = temp.Item2;
        }

        public void UpdateCoordinates(T a, T b, T c, T d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;

            UpdateMeasures();
        }

        private ApscBlock(T a, T b, T c, T d)
        {
            this.A = a;
            this.B = b;
            this.C = c;
            this.D = d;
        }

        public static ApscBlock<T> Create(T a, T b, T c, T d)
        {
            var result = new ApscBlock<T>(a, b, c, d);

            result.UpdateMeasures();

            return result;
        }

        public override string ToString()
        {
            return $"epsilon: {Epsilon}, A:{A}, B:{B}, C:{C}, D:{D}, E:{E}";
        }

        public bool HasNaNPoint()
        {
            return A.IsNaN() || B.IsNaN() || C.IsNaN() || D.IsNaN();
        }
    }

    public static List<T> SimplifiedLine<T>(List<T> list, double areaThreshold) where T : IPoint, new()
    {
        var workingList = new List<ApscBlock<T>>();

        for (int i = 0; i < list.Count - 3; i++)
        {
            workingList.Add(ApscBlock<T>.Create(list[i], list[i + 1], list[i + 2], list[i + 3]));
        }

        Func<T> createNaNPoint = () => new T() { X = double.NaN, Y = double.NaN };

        while (workingList.Count > 2)
        {
            var minValue = workingList.Where(w => !double.IsNaN(w.Epsilon)).Select(w => w.Epsilon).DefaultIfEmpty().Min();

            if (minValue > areaThreshold)
                break;

            var toBeCollapsed = workingList.Select((w, index) => (w, index)).FirstOrDefault(w => w.w.Epsilon == minValue);

            var currentIndex = toBeCollapsed.index;

            var currentBlock = toBeCollapsed.w;

            //var toBeRemoved = new List<ApscBlock<T>>();

            // update current block
            //if (currentIndex + 1 < workingList.Count)
            currentBlock.UpdateCoordinates(currentBlock.A, currentBlock.E, currentBlock.D, (currentIndex + 1 < workingList.Count) ? workingList[currentIndex + 1].D : createNaNPoint());

            //else
            //    toBeRemoved.Add(currentBlock);

            // update next block
            //if (currentIndex + 2 < workingList.Count)
            if (currentIndex + 1 < workingList.Count)
                //{
                workingList[currentIndex + 1].UpdateCoordinates(currentBlock.B, currentBlock.C, currentBlock.D, (currentIndex + 2 < workingList.Count) ? workingList[currentIndex + 2].D : createNaNPoint());
            //toBeRemoved.Add(workingList[currentIndex + 2]);
            //}
            //else if (currentIndex + 1 < workingList.Count)
            //    toBeRemoved.Add(workingList[currentIndex + 1]);

            // update previous block
            if (currentIndex - 1 >= 0)
                workingList[currentIndex - 1].UpdateCoordinates(workingList[currentIndex - 1].A, currentBlock.A, currentBlock.B, currentBlock.C);

            if (currentIndex - 2 >= 0)
                workingList[currentIndex - 2].UpdateCoordinates(workingList[currentIndex - 2].A, workingList[currentIndex - 2].B, currentBlock.A, currentBlock.B);

            //foreach (var item in toBeRemoved)
            //    workingList.Remove(item);

            if (currentIndex + 2 < workingList.Count)
                workingList.Remove(workingList[currentIndex + 2]);

            //if (currentIndex + 1 < workingList.Count && workingList[currentIndex + 1].HasNaNPoint())
            //    workingList.Remove(workingList[currentIndex + 1]);

            //if (currentBlock.HasNaNPoint())
            //    workingList.Remove(currentBlock);
            for (int i = workingList.Count - 1; i >= 0; i--)
            {
                if (workingList[i].HasNaNPoint())
                    workingList.RemoveAt(i);
            }

        }

        var result = workingList.Select(w => w.A).ToList();

        if (workingList.Count == 0)
        {
            return new List<T>() { list.First(), list.Last() };
        }

        result.Add(workingList[workingList.Count - 1].B);
        result.Add(workingList[workingList.Count - 1].C);
        result.Add(workingList[workingList.Count - 1].D);

        return result;
    }
}
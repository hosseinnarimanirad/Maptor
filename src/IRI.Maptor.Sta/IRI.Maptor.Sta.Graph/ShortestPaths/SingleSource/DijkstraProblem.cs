// BESMELLAHE RAHMANE RAHIM
// ALLAHOMMA AJJEL LE-VALIYEK AL-FARAJ

using System;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.DataStructures.CustomStructures;

namespace IRI.Maptor.Sta.Graph;

public class DijkstraProblem
{
    Matrix m_Adjacency;

    List<IndexValue<double>> labelWeight;

    List<int> intendedIndexes;

    //int m_firstNode;

    //public int FirstNode
    //{
    //    get { return this.m_firstNode; }
    //}

    public Matrix Adjacency
    {
        get { return this.m_Adjacency; }
    }

    public int NumberOfNodes
    {
        get { return this.Adjacency.NumberOfColumns; }
    }

    public DijkstraProblem(Matrix adjacency)
    {
        if (!adjacency.IsSquare())//|| adjacency.IsSingular())
        {
            throw new NotImplementedException();
        }

        this.m_Adjacency = adjacency;

    }

    public List<int> FindShortestPath(int firstNode, int secondNode)
    {
        // check wheather firstNode and secondNode are in the range!
        // check if it has not been calculated before

        Initialize(firstNode);

        while (intendedIndexes.Count > 0)
        {
            int currentIndex = GetMinimumWeightNode();

            for (int i = 0; i < this.NumberOfNodes; i++)
            {
                double tempDistance = labelWeight[currentIndex].Value + Adjacency[currentIndex, i];

                if (labelWeight[i].Value > tempDistance)
                {
                    labelWeight[i] = new IndexValue<double>(currentIndex, tempDistance);
                }
            }

            intendedIndexes.Remove(currentIndex);
        }

        return TracePath(firstNode, secondNode);

    }

    private List<int> TracePath(int firstNode, int secondNode)
    {

        List<int> result = [secondNode];

        int tempNode = secondNode;

        while (labelWeight[tempNode].Index != firstNode)
        {
            result.Add(labelWeight[tempNode].Index);

            tempNode = labelWeight[tempNode].Index;
        }

        result.Add(firstNode);

        result.Reverse();

        return result;
    }

    private void Initialize(int firstNode)
    {

        this.labelWeight = new List<IndexValue<double>>();

        this.intendedIndexes = new List<int>();

        for (int i = 0; i < this.NumberOfNodes; i++)
        {
            labelWeight.Add(new IndexValue<double>(firstNode, double.PositiveInfinity));

            intendedIndexes.Add(i);
        }

        labelWeight[firstNode] = new IndexValue<double>(firstNode, 0);
    }

    private int GetMinimumWeightNode()
    {
        if (this.intendedIndexes.Count == 0)
        {
            throw new NotImplementedException();
        }

        int resultIndex = intendedIndexes[0];

        for (int i = 1; i < intendedIndexes.Count; i++)
        {
            if (labelWeight[intendedIndexes[i]].Value < labelWeight[resultIndex].Value)
                resultIndex = intendedIndexes[i];
        }

        return resultIndex;

    }

}

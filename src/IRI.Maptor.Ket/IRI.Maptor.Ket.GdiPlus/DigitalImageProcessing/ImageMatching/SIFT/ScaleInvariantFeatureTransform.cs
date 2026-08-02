// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;
using IRI.Maptor.Sta.DataStructures;
using System.Xml.Serialization;

namespace IRI.Maptor.Ket.DigitalImageProcessing.ImageMatching;

public class ScaleInvariantFeatureTransform
{
    public Extrema extrema;

    //public Extrema exactExtrema;

    public List<KeyPoint> keypoints;

    //private List<double[]> descriptors;

    private Matrix originalImage;

    public List<Descriptor> descriptors;

    public List<Matrix> scaleSpace;

    public List<Matrix> doGs;

    public List<double> doGMax;

    public List<double> doGMin;

    public List<Vector> vectors;

    public int depthOfBlur, depthOfScale;

    public double originalStandardDeviation, edgeThreshold;

    //OK 29/11/2009
    public ScaleInvariantFeatureTransform(Matrix originalImage, int depthOfScale, int depthOfBlur, double originalStandardDeviation, double edgeThreshold)
    {
        scaleSpace = new List<Matrix>();

        this.originalImage = originalImage;

        ////scaleSpace.Add(originalImage);

        //n
        this.depthOfScale = depthOfScale;
        //s
        this.depthOfBlur = depthOfBlur;

        this.originalStandardDeviation = originalStandardDeviation;
        //r=10 Loe
        this.edgeThreshold = edgeThreshold;

        this.doGMax = new List<double>();

        this.doGMin = new List<double>();

        this.keypoints = new List<KeyPoint>();
    }

    //OK, 04/13/2009
    public void ConstructScaleSpace()
    {
        Matrix tempMatrix = IRI.Maptor.Ket.DigitalImageProcessing.GeometricEnhancement.DoubleTheSize(this.originalImage);

        tempMatrix = new GaussianConvolution(1.5).Convolve(tempMatrix);

        originalStandardDeviation *= 0.5;

        for (int n = 0; n < depthOfScale; n++)
        {
            //We need s + 3 images in each octave. [David Lowe 2004]
            for (int s = 0; s < depthOfBlur + 3; s++)
            {
                if (s == 0)
                {
                    //if (n != 0)
                    //{
                    scaleSpace.Add(tempMatrix);
                    //}

                    continue;
                }

                double standarDeviation = CalculateSigma(n, s);

                scaleSpace.Add(new GaussianConvolution(standarDeviation).Convolve(tempMatrix));
            }

            // index of image in the current octave (has the twice 
            // scale of first image in the current octave) to be
            // half, 
            int tempIndex = n * (this.depthOfBlur + 3) + depthOfBlur;

            tempMatrix = IRI.Maptor.Ket.DigitalImageProcessing.GeometricEnhancement.HalveTheSize(scaleSpace[tempIndex]);

        }
    }

    //OK, 04/13/2009
    public void CalculateDifferenceOfGaussians()
    {

        this.doGs = new List<Matrix>();

        for (int n = 0; n < depthOfScale; n++)
        {
            for (int s = 1; s < depthOfBlur + 3; s++)
            {
                // D(x,y,sigma) = L(x,y,k*sigma) - L(x,y,sigma) [David Lowe. 2004]
                doGs.Add(scaleSpace[GetImageIndex(n, s)] - scaleSpace[GetImageIndex(n, s - 1)]);
            }
        }

        foreach (Matrix values in doGs)
        {
            doGMax.Add(IRI.Maptor.Sta.Mathematics.Statistics.GetMax(values));

            doGMin.Add(IRI.Maptor.Sta.Mathematics.Statistics.GetMin(values));
        }
    }

    //OK, 04/13/2009
    public void FindExtrema()
    {
        this.extrema = new Extrema();

        for (int n = 0; n < depthOfScale; n++)
        {
            // depth of blur baraye DoG ha = depth of blur L ha - 1
            for (int s = 1; s < (depthOfBlur + 3 - 1) - 1; s++)
            {
                int index = GetDoGIndex(n, s);

                int tempColumn = doGs[index].NumberOfColumns - 15;

                int tempRow = doGs[index].NumberOfRows - 15;

                for (int row = 15; row < tempRow; row++)
                {
                    for (int col = 15; col < tempColumn; col++)
                    {
                        double[] tempValue = new double[]{  doGs[index-1][row-1,col-1],     doGs[index-1][row-1,col],   doGs[index-1][row-1,col+1],
                                                            doGs[index-1][row  ,col-1],     doGs[index-1][row  ,col],   doGs[index-1][row  ,col+1],
                                                            doGs[index-1][row+1,col-1],     doGs[index-1][row+1,col],   doGs[index-1][row+1,col+1],
                                                            
                                                            doGs[index][row-1,col-1],       doGs[index][row-1,col],     doGs[index][row-1,col+1],
                                                            doGs[index][row  ,col-1],       doGs[index][row  ,col],     doGs[index][row  ,col+1],
                                                            doGs[index][row+1,col-1],       doGs[index][row+1,col],     doGs[index][row+1,col+1],

                                                            doGs[index+1][row-1,col-1],     doGs[index+1][row-1,col],   doGs[index+1][row-1,col+1],
                                                            doGs[index+1][row  ,col-1],     doGs[index+1][row  ,col],   doGs[index+1][row  ,col+1],
                                                            doGs[index+1][row+1,col-1],     doGs[index+1][row+1,col],   doGs[index+1][row+1,col+1]};

                        double tempMax = FindMax(tempValue); double tempMin = FindMin(tempValue);

                        if (tempMax == tempMin)
                            continue;

                        if (doGs[index][row, col] == tempMax || doGs[index][row, col] == tempMin)
                        {
                            double standarDeviation = CalculateSigma(n, s);

                            this.extrema.Add(n, s, col, row, standarDeviation);
                        }
                    }
                }
            }
        }

    }

    //Checked 06/04/2010
    public void FindExactExtrema()
    {
        for (int t = extrema.Count - 1; t >= 0; t--)
        {
            //Note: depthOfBlur + 3 has been noticed
            int doGIndex = GetDoGIndex((int)extrema[t].ScaleLevel, (int)extrema[t].BlurLevel);

            int n = (int)extrema[t].ScaleLevel;

            //y-->row, x-->column
            int row = (int)extrema[t].Row; int column = (int)extrema[t].Column;

            double dx, dy, dsigma;

            int controlValue = 0;

            FindExtremumDisplacementAt(doGIndex, row, column, out dx, out dy, out dsigma);

            //check if the displacement is more than one pixel far
            if (dx * dx + dy * dy > 2.0)
            {
                this.extrema.Remove(t);

                continue;
            }

            while (Math.Abs(dx) > 0.5 || Math.Abs(dy) > 0.5 || Math.Abs(dsigma) > 0.5)
            {
                controlValue++;

                row += ((int)Math.Floor(Math.Abs(dy) / 0.5) * (dy > 0 ? 1 : -1));

                column += ((int)Math.Floor(Math.Abs(dx) / 0.5) * (dx > 0 ? 1 : -1));

                doGIndex += ((int)Math.Floor(Math.Abs(dsigma) / 0.5) * (dsigma > 0 ? 1 : -1));

                //jahate ta akse yeki be akhar va aval!
                if (doGIndex + 1 >= (this.depthOfBlur + 3 - 1) * (n + 1) || doGIndex - 1 < (this.depthOfBlur + 3 - 1) * n || column < 15 || row < 15
                        || column > doGs[doGIndex].NumberOfColumns - 15 || row > doGs[doGIndex].NumberOfRows - 15)
                {
                    controlValue = 100;

                    break;
                }
                if (controlValue > 4)
                {
                    break;
                }

                FindExtremumDisplacementAt(doGIndex, row, column, out dx, out dy, out dsigma);
            }
            if (controlValue != 100)
            {
                //int blurLevel = doGIndex - (this.depthOfBlur + 3 - 1) * (int)extrema[t].ScaleLevel;
                int blurLevel = doGIndex - (this.depthOfBlur + 3 - 1) * n;

                double standarDeviation = CalculateSigma(n, blurLevel);

                this.extrema[t] = new Extremum(n, blurLevel, column, row, standarDeviation);

                if (IsLowContrastExtremum(doGIndex, row, column, dx, dy, dsigma))
                {
                    this.extrema.Remove(t);
                }
            }
            else
            {
                this.extrema.Remove(t);
            }
        }
    }

    //Checked 06/04/2010
    private bool IsLowContrastExtremum(int doGIndex, int row, int column, double dx, double dy, double dsigma)
    {
        double max = doGMax[doGIndex];

        double min = doGMin[doGIndex];

        double range = max - min;

        double dX = 0.5 * ((doGs[doGIndex][row, column + 1] - min) / range - (doGs[doGIndex][row, column - 1] - min) / range);

        double dY = 0.5 * ((doGs[doGIndex][row + 1, column] - min) / range - (doGs[doGIndex][row - 1, column] - min) / range);

        double dS = 0.5 * ((doGs[doGIndex + 1][row, column] - min) / range - (doGs[doGIndex - 1][row, column] - min) / range);

        double value = doGs[doGIndex][row, column] + 0.5 * (dx * dX + dy * dY + dsigma * dS);

        if (Math.Abs(value) < 0.03)
        {
            return true;
        }

        return false;
    }

    //Modified 06/04/2010
    public void FindExtremumDisplacementAt(int doGIndex, int row, int column, out double dx, out double dy, out double dsigma)
    {
        //double dX = 0.5 * (doGs[doGIndex][row, column + 1] - doGs[doGIndex][row, column - 1]);

        //double dY = 0.5 * (doGs[doGIndex][row + 1, column] - doGs[doGIndex][row - 1, column]);

        //double dS = 0.5 * (doGs[doGIndex + 1][row, column] - doGs[doGIndex - 1][row, column]);

        //double dXX = doGs[doGIndex][row, column + 1] + doGs[doGIndex][row, column - 1] - 2 * doGs[doGIndex][row, column];

        //double dYY = doGs[doGIndex][row + 1, column] + doGs[doGIndex][row - 1, column] - 2 * doGs[doGIndex][row, column];

        //double dSS = doGs[doGIndex + 1][row, column] + doGs[doGIndex - 1][row, column] - 2 * doGs[doGIndex][row, column];

        //double dXY = 0.25 * (doGs[doGIndex][row + 1, column + 1] + doGs[doGIndex][row - 1, column - 1] -
        //                        doGs[doGIndex][row - 1, column + 1] - doGs[doGIndex][row + 1, column - 1]);

        //double dXS = 0.25 * (doGs[doGIndex + 1][row, column + 1] + doGs[doGIndex - 1][row, column - 1] -
        //                        doGs[doGIndex - 1][row, column + 1] - doGs[doGIndex + 1][row, column - 1]);

        //double dYS = 0.25 * (doGs[doGIndex + 1][row + 1, column] + doGs[doGIndex - 1][row - 1, column] -
        //                        doGs[doGIndex - 1][row + 1, column] - doGs[doGIndex + 1][row - 1, column]);
        double max = doGMax[doGIndex]; double min = doGMin[doGIndex]; double range = max - min;

        double dX = 0.5 * ((doGs[doGIndex][row, column + 1] - min) / range - (doGs[doGIndex][row, column - 1] - min) / range);

        double dY = 0.5 * ((doGs[doGIndex][row + 1, column] - min) / range - (doGs[doGIndex][row - 1, column] - min) / range);

        double dS = 0.5 * ((doGs[doGIndex + 1][row, column] - min) / range - (doGs[doGIndex - 1][row, column] - min) / range);

        double dXX = (doGs[doGIndex][row, column + 1] - min) / range + (doGs[doGIndex][row, column - 1] - min) / range - 2 * (doGs[doGIndex][row, column] - min) / range;

        double dYY = (doGs[doGIndex][row + 1, column] - min) / range + (doGs[doGIndex][row - 1, column] - min) / range - 2 * (doGs[doGIndex][row, column] - min) / range;

        double dSS = (doGs[doGIndex + 1][row, column] - min) / range + (doGs[doGIndex - 1][row, column] - min) / range - 2 * (doGs[doGIndex][row, column] - min) / range;

        double dXY = 0.25 * ((doGs[doGIndex][row + 1, column + 1] - min) / range + (doGs[doGIndex][row - 1, column - 1] - min) / range -
                                (doGs[doGIndex][row - 1, column + 1] - min) / range - (doGs[doGIndex][row + 1, column - 1] - min) / range);

        double dXS = 0.25 * ((doGs[doGIndex + 1][row, column + 1] - min) / range + (doGs[doGIndex - 1][row, column - 1] - min) / range -
                                (doGs[doGIndex - 1][row, column + 1] - min) / range - (doGs[doGIndex + 1][row, column - 1] - min) / range);

        double dYS = 0.25 * ((doGs[doGIndex + 1][row + 1, column] - min) / range + (doGs[doGIndex - 1][row - 1, column] - min) / range -
                                (doGs[doGIndex - 1][row + 1, column] - min) / range - (doGs[doGIndex + 1][row - 1, column] - min) / range);

        Matrix coef = new Matrix(new double[][] { 
                                        new double[] { dXX, dXY, dXS }, 
                                        new double[] { dXY, dYY, dYS }, 
                                        new double[] { dXS, dYS, dSS } });

        if (coef.Determinant == 0)
        {
            dx = dy = dsigma = 0;
        }
        else
        {
            Matrix values = new Matrix(new double[][] { new double[] { -dX, -dY, -dS } });

            Matrix result = coef.Inverse() * values;

            dx = result[0, 0]; dy = result[1, 0]; dsigma = result[2, 0];
        }
    }

    //Modified 06/04/2010: it has bug
    public void RemoveEdgeResponses()
    {
        double threshold = ((this.edgeThreshold + 1.0) * (this.edgeThreshold + 1.0)) / this.edgeThreshold;

        for (int t = this.extrema.Count - 1; t >= 0; t--)
        {
            int doGIndex = GetDoGIndex((int)extrema[t].ScaleLevel, (int)extrema[t].BlurLevel);

            int row = (int)extrema[t].Row; int column = (int)extrema[t].Column;

            double max = doGMax[doGIndex];

            double min = doGMin[doGIndex];

            double range = max - min;

            double dXX = doGs[doGIndex][row, column + 1] + doGs[doGIndex][row, column - 1] - 2 * doGs[doGIndex][row, column];

            double dYY = doGs[doGIndex][row + 1, column] + doGs[doGIndex][row - 1, column] - 2 * doGs[doGIndex][row, column];

            double dXY = 0.25 * (doGs[doGIndex][row + 1, column + 1] + doGs[doGIndex][row - 1, column - 1] -
                                    doGs[doGIndex][row - 1, column + 1] - doGs[doGIndex][row + 1, column - 1]);
            //double dXX = (doGs[doGIndex][row, column + 1] - min) / range + (doGs[doGIndex][row, column - 1] - min) / range - 2 * (doGs[doGIndex][row, column] - min) / range;

            //double dYY = (doGs[doGIndex][row + 1, column] - min) / range + (doGs[doGIndex][row - 1, column] - min) / range - 2 * (doGs[doGIndex][row, column] - min) / range;

            //double dXY = 0.25 * ((doGs[doGIndex][row + 1, column + 1] - min) / range + (doGs[doGIndex][row - 1, column - 1] - min) / range -
            //                        (doGs[doGIndex][row - 1, column + 1] - min) / range - (doGs[doGIndex][row + 1, column - 1] - min) / range);


            //
            //
            if ((dXX * dYY - dXY * dXY) < 0)
            {
                this.extrema.Remove(t);
            }
            //

            // false
            //else if (Math.Abs(((dXX + dYY) * (dXX + dYY)) / (dXX * dYY - dXY * dXY)) < threshold)
            // true
            else if (Math.Abs(((dXX + dYY) * (dXX + dYY)) / (dXX * dYY - dXY * dXY)) > threshold)
            {
                this.extrema.Remove(t);
            }
        }
    }

    //Checked 06/04/2010
    public void AssignOrientations()
    {
        this.keypoints = new List<KeyPoint>();

        for (int t = 0; t < extrema.Count; t++)
        {
            int scale = (int)extrema[t].ScaleLevel;

            int blur = (int)extrema[t].BlurLevel;

            int ImageIndex = GetImageIndex(scale, blur);

            int row = (int)extrema[t].Row;

            int column = (int)extrema[t].Column;

            //Matrix magnitude = new Matrix(17, 17);

            //Matrix angle = new Matrix(17, 17);

            double magnitude, angle;

            double[] histogram = new double[36];

            double sigma = extrema[t].Sigma * 1.5;

            double sigma2 = sigma * sigma;

            for (int i = row - 8; i < row + 9; i++)
            {
                for (int j = column - 8; j < column + 9; j++)
                {
                    magnitude = CalculateMagnitude(ImageIndex, j, i);

                    angle = CalculateAngle(ImageIndex, j, i);

                    //
                    double angleD = angle * 180 / Math.PI;

                    int binNumber = (int)Math.Floor(angle * (18.0 / Math.PI)) % 36;

                    //the sign is not important here
                    int tempX = column - j; int tempY = row - i;

                    histogram[binNumber] += magnitude * 1 / (2 * Math.PI * sigma2) * Math.Exp(-(tempX * tempX + tempY * tempY) / (2 * sigma2));
                }
            }

            List<int> tempIndexes = FindNearMaxIndexes(histogram, 0.8);

            foreach (int item in tempIndexes)
            {
                if (item > 0 && item < 35)
                {
                    double x1 = (item - 1) * 10 + 5; double x2 = (item) * 10 + 5; double x3 = (item + 1) * 10 + 5;

                    Matrix coef = new Matrix(new double[][] { new double[] { x1 * x1, x2 * x2, x3 * x3 }, new double[] { x1, x2, x3 }, new double[] { 1, 1, 1 } });

                    Matrix values = new Matrix(new double[][] { new double[] { histogram[item - 1], histogram[item], histogram[item + 1] } });

                    Matrix result = coef.Inverse() * values;

                    double resultAngle = -result[1, 0] / (2 * result[0, 0]);

                    double resultMagnitude = result[0, 0] * resultAngle * resultAngle + result[1, 0] * resultAngle + result[2, 0];

                    if (resultAngle < 0 || resultAngle > 2 * Math.PI)
                    {
                        resultAngle = (item) * 10 + 5;

                        resultMagnitude = histogram[item];
                    }

                    this.keypoints.Add(new KeyPoint(t, resultAngle * Math.PI / 180, resultMagnitude));
                }
                else
                {
                    this.keypoints.Add(new KeyPoint(t, (item * 10 + 5) * Math.PI / 180, histogram[item]));
                }
            }
        }
    }

    public void CreateDescriptors()
    {
        this.descriptors = new List<Descriptor>();

        for (int k = this.keypoints.Count - 1; k >= 0; k--)
        {
            int t = this.keypoints[k].ExtremumIndex;

            int scale = (int)extrema[t].ScaleLevel;

            int blur = (int)extrema[t].BlurLevel;

            int index = GetImageIndex(scale, blur);

            int row = (int)extrema[t].Row;

            int column = (int)extrema[t].Column;

            double rotationAngle = keypoints[k].Orientation;

            Matrix neighbour = GeometricEnhancement.GetImagePortion(this.scaleSpace[index], row, column, 9, rotationAngle);

            //Matrix angle = new Matrix(17, 17);

            List<double>[,] histogram = new List<double>[4, 4];

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    histogram[i, j] = new List<double>(new double[8]);
                }
            }

            double sigma = 16 * 0.5;

            double sigma2 = sigma * sigma;

            //

            for (int i = row - 8; i < row + 8; i++)
            {
                for (int j = column - 8; j < column + 8; j++)
                {

                    int xPrime = j - column; int yPrime = i - row;

                    double magnitude = CalculateMagnitude(neighbour, j - (column - 8) + 1, (i - (row - 8)) + 1);

                    double tempAngle = CalculateAngle(neighbour, j - (column - 8) + 1, (i - (row - 8)) + 1) - rotationAngle;


                    double angle = tempAngle >= 0 ? tempAngle : 2 * Math.PI + tempAngle;

                    //
                    double angleD = angle * 180 / Math.PI;
                    double rotationD = rotationAngle * 180 / Math.PI;

                    int temp = (int)Math.Floor(angle / (Math.PI / 4)) % 8; //8 bin

                    int tempI = (int)(Math.Floor((i - row + 8) / 4.0));

                    int tempJ = (int)(Math.Floor((j - column + 8) / 4.0));

                    histogram[tempI, tempJ][temp] += magnitude * 1 / (2 * Math.PI * sigma2) * Math.Exp(-(xPrime * xPrime + yPrime * yPrime) / (2 * sigma2));
                }
            }

            List<double> tempResult = new List<double>();

            foreach (List<double> item in histogram)
            {
                tempResult.AddRange(item.ToArray());
            }

            Descriptor descriptor = new Descriptor(this.extrema[t], this.keypoints[k], tempResult.ToArray());

            if (descriptor.Norm != 0)
            {
                this.descriptors.Add(descriptor);
            }
            else
            {
                this.keypoints.RemoveAt(k);

                //this.extrema.Remove(k);
            }
            //this.descriptors.Add();
        }
    }

    private Matrix GetImagePortion(int imageIndex, int row, int column, int quasiRadius, double angle)
    {

        Matrix result = new Matrix(quasiRadius + quasiRadius, quasiRadius + quasiRadius);

        for (int i = row - quasiRadius; i < row + quasiRadius; i++)
        {
            for (int j = column - quasiRadius; j < column + quasiRadius; j++)
            {
                double rotatedRow, rotatedColumn;

                Rotate(angle, j - column, i - row, out rotatedColumn, out rotatedRow);

                result[i - (row - quasiRadius), j - (column - quasiRadius)] = this.scaleSpace[imageIndex][(int)(row + rotatedRow), (int)(column + rotatedColumn)];
            }
        }

        return result;
    }

    //rotationAngle is in Radian
    public void Rotate(double rotationAngle, int originalX, int originalY, out double rotatedX, out double rotatedY)
    {
        rotatedX = originalX * Math.Cos(rotationAngle) - originalY * Math.Sin(rotationAngle);

        rotatedY = originalX * Math.Sin(rotationAngle) + originalY * Math.Cos(rotationAngle);
    }

    //direction is in Radian
    //private double CalculateMagnitude(int column, int row, int imageIndx, double direction)
    //{
    //    double dx1, dy1, dx2, dy2;

    //    Rotate(direction, 1, 0, out dx1, out dy1);

    //    Rotate(direction, -1, 0, out dx2, out dy2);

    //    //double dX = ((scaleSpace[imageIndx][row, column + 1] - scaleSpace[imageIndx][row, column - 1]));

    //    //double dY = ((scaleSpace[imageIndx][row + 1, column] - scaleSpace[imageIndx][row - 1, column]));

    //    double dX = ((scaleSpace[imageIndx][(int)(row + dy2), (int)(column + dx2)] - scaleSpace[imageIndx][(int)(row + dy1), (int)(column - dx1)]));

    //    Rotate(direction, 0, 1, out dx1, out dy1);

    //    Rotate(direction, 0, -1, out dx2, out dy2);

    //    double dY = ((scaleSpace[imageIndx][(int)(row + dy2), (int)(column + dx2)] - scaleSpace[imageIndx][(int)(row + dy1), (int)(column - dx1)]));

    //    return Math.Sqrt(dX * dX + dY * dY);
    //}

    private double CalculateMagnitude(Matrix image, int column, int row)
    {
        double dX = ((image[row, column + 1] - image[row, column - 1]));

        double dY = ((image[row + 1, column] - image[row - 1, column]));

        return Math.Sqrt(dX * dX + dY * dY);
    }

    private double CalculateMagnitude(int imageIndex, int column, int row)
    {
        return CalculateMagnitude(this.scaleSpace[imageIndex], column, row);
    }

    //direction is in Radian
    //private double CalculateAngle(int column, int row, int imageIndx, double direction)
    //{
    //    double dx1, dy1, dx2, dy2;

    //    Rotate(direction, 1, 0, out dx1, out dy1);

    //    Rotate(direction, -1, 0, out dx2, out dy2);

    //    //double dX = ((scaleSpace[imageIndx][row, column + 1] - scaleSpace[imageIndx][row, column - 1]));

    //    //double dY = ((scaleSpace[imageIndx][row + 1, column] - scaleSpace[imageIndx][row - 1, column]));

    //    double dX = ((scaleSpace[imageIndx][(int)(row + dy2), (int)(column + dx2)] - scaleSpace[imageIndx][(int)(row + dy1), (int)(column - dx1)]));

    //    Rotate(direction, 0, 1, out dx1, out dy1);

    //    Rotate(direction, 0, -1, out dx2, out dy2);

    //    double dY = ((scaleSpace[imageIndx][(int)(row + dy2), (int)(column + dx2)] - scaleSpace[imageIndx][(int)(row + dy1), (int)(column - dx1)]));

    //    double result = Math.Atan2(dY, dX);

    //    return (result >= 0) ? result : (2 * Math.PI + result);
    //}

    private double CalculateAngle(Matrix image, int column, int row)
    {
        double dX = ((image[row, column + 1] - image[row, column - 1]));

        double dY = ((image[row + 1, column] - image[row - 1, column]));

        double result = Math.Atan2(dY, dX);

        return (result >= 0) ? result : (2 * Math.PI + result);
    }

    private double CalculateAngle(int imageIndex, int column, int row)
    {
        return CalculateAngle(this.scaleSpace[imageIndex], column, row);
    }


    private double FindMax(double[] values)
    {
        double result = values[0];

        for (int i = 0; i < values.Length; i++)
        {
            if (result < values[i])
            {
                result = values[i];
            }
        }

        return result;
    }

    private double FindMin(double[] values)
    {
        double result = values[0];

        for (int i = 0; i < values.Length; i++)
        {
            if (result > values[i])
            {
                result = values[i];
            }
        }

        return result;
    }

    //percentage in sift is 0.8
    private List<int> FindNearMaxIndexes(double[] values, double percentage)
    {
        double threshold = FindMax(values) * percentage;

        List<int> result = new List<int>();

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] >= threshold)
            {
                result.Add(i);
            }
        }

        return result;
    }

    #region Private Methods

    //scaleLevel = octave number
    //OK, 03/12/2009
    private double CalculateSigma(int scaleLevel, int blurLevel)
    {
        //sigma0 * 2^k/s * 2^n
        return originalStandardDeviation * Math.Pow(2.0, (double)blurLevel / (double)depthOfBlur) * Math.Pow(2.0, scaleLevel);
    }

    //OK, 03/12/2009
    private int GetDoGIndex(int scaleLevel, int blurLevel)
    {
        return (depthOfBlur + 3 - 1) * scaleLevel + blurLevel;
    }

    //OK, 03/12/2009
    private int GetImageIndex(int scaleLevel, int blurLevel)
    {
        return (depthOfBlur + 3) * scaleLevel + blurLevel;
    }

    #endregion
}

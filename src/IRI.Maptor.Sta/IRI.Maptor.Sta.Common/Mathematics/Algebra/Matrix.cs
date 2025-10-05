// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Text;
using System.Globalization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using IRI.Maptor.Sta.Mathematics;

namespace IRI.Maptor.Sta.Mathematics;

[DataContract]
public class Matrix
{
    #region Fields&Properties

    protected double[][] Element = [];

    public double this[int rowNumber, int columNumber]
    {
        get { return this.GetValue(rowNumber, columNumber); }

        set { this.SetValue(rowNumber, columNumber, value); }
    }

    public int NumberOfRows => this.Element[0].Length;

    public int NumberOfColumns => this.Element.Length;

    public double Trace => CalculateTrace();

    public double Determinant => CalculateDeterminant();

    public double SumOfElements => CalculateSumOfElemets();

    #endregion

    #region Constructors

    public Matrix()
    {
        this.Element[0] = new double[1];
    }

    public Matrix(double[,] matrix)
    {

        int numberOfRows = matrix.GetUpperBound(0) + 1;

        int numberOfColumns = matrix.GetUpperBound(1) + 1;

        Array.Resize(ref this.Element, numberOfColumns);

        for (int i = 0; i < numberOfColumns; i++)
        {
            Array.Resize(ref this.Element[i], numberOfRows);
        }

        for (int i = 0; i < numberOfColumns; i++)
        {
            for (int j = 0; j < this.NumberOfRows; j++)
            {
                this.Element[i][j] = matrix[j, i];
            }
        }

    }

    public Matrix(double[][] matrix)
    {
        this.Element = matrix;
    }

    public Matrix(int size)
        : this(size, size)
    {
    }

    public Matrix(int row, int column)
    {
        //this.m_NumberOfRows = row;

        //this.m_NumberOfColumns = column;

        Array.Resize(ref this.Element, column);

        for (int i = 0; i < column; i++)
        {
            Array.Resize(ref this.Element[i], row);
        }

    }

    #endregion

    #region Methods

    public bool IsNull() => this.Element == null;

    public bool IsRowMatrix() => (this.NumberOfRows == 1);

    public bool IsColumnMatrix() => (this.NumberOfColumns == 1);

    public bool IsSquare() => (this.NumberOfColumns == this.NumberOfRows);

    public bool IsSymmetric() => (this == this.Transpose());

    public bool IsDiagonal()
    {
        if (!this.IsSquare())
            throw new NonSquareMatrixException();

        return ((this.Clone() - Matrix.DiagonalMatrix(this.DiagonalVector())) == Matrix.Zeros(this.NumberOfColumns));
    }

    public bool IsSingular() => (this.Determinant == 0);

    public bool IsNonsingular() => !(this.Determinant == 0);

    public bool IsOrthogonal()
    {
        if (!this.IsSquare())
            throw new NonSquareMatrixException();

        return (this * this.Transpose() == Matrix.Identity(this.NumberOfColumns));
    }

    public Matrix Clone()
    {
        Matrix resultMatrix = new Matrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {
                resultMatrix.SetValue(i, j, this.GetValue(i, j));
            }
        }

        return (resultMatrix);
    }

    public Matrix Negative() => -this;

    public Matrix SubMatrix(int startRow, int startColumn, int endRow, int endColumns)
    {
        if (startRow > endRow || startColumn > endColumns)
        {
            throw new IllegalInputException();
        }

        if (this.NumberOfRows < endRow || this.NumberOfColumns < endColumns)
        {
            throw new IllegalInputException();
        }

        Matrix result = new Matrix(endRow - startRow + 1, endColumns - startColumn + 1);

        for (int i = startRow; i <= endRow; i++)
        {
            for (int j = startColumn; j <= endColumns; j++)
            {
                result[i - startRow, j - startColumn] = this[i, j];
            }
        }

        return result;
    }

    public Matrix Transpose()
    {
        Matrix result = new Matrix(this.NumberOfColumns, this.NumberOfRows);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            result.SetColumn(i, this.GetRow(i));
        }

        return result;
    }
      
    public Matrix CofactorMatrixOf(int rowNumber, int columNumber)
    {
        Matrix tempMatrix = this.Clone();

        tempMatrix.RemoveRow(rowNumber);

        tempMatrix.RemoveColumn(columNumber);

        return tempMatrix;
    }

    public Matrix Adjoint()
    {
        if (!this.IsSquare())
            throw new NonSquareMatrixException();

        Matrix resultMatrix = new Matrix(this.NumberOfColumns, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfColumns; i++)
        {

            for (int j = 0; j < this.NumberOfColumns; j++)
            {

                resultMatrix[i, j] = this.CofactorOf(j, i);

            }

        }

        return resultMatrix;
    }

    // ? try with a matrix that has a infinity member
    public Matrix Inverse() => 1 / this.Determinant * this.Adjoint();

    public Matrix LeftInverse()
    {
        Matrix tempMatrix = this.Transpose();

        return (tempMatrix * this).Inverse() * tempMatrix;
    }

    public Matrix RightInverse()
    {
        Matrix tempMatrix = this.Transpose();

        return tempMatrix * (this * tempMatrix).Inverse();
    }

    public double[] DiagonalVector()
    {
        if (!this.IsSquare())
            throw new NonSquareMatrixException();

        double[] result = new double[this.NumberOfRows];

        for (int i = 0; i < this.NumberOfRows; i++)
        {

            result[i] = this[i, i];

        }

        return result;
    }

    public double[] GetRow(int row)
    {

        double[] result = new double[this.NumberOfColumns];

        for (int i = 0; i < this.NumberOfColumns; i++)
        {

            result[i] = this.Element[i][row];

        }

        return result;

    }

    public double[] GetColumn(int column)
    {

        return this.Element[column];

    }

    public void Reconstruct(Matrix matrix)
    {

        Array.Resize(ref this.Element, matrix.NumberOfColumns);

        for (int i = 0; i < matrix.NumberOfColumns; i++)
        {

            Element[i] = matrix.Element[i];

        }

    }

    public void SetRow(int row, double[] values)
    {

        if (values.Length != this.NumberOfColumns)
        {
            throw new NumberOfElementsException();
        }

        for (int i = 0; i < this.NumberOfColumns; i++)
        {
            this.Element[i][row] = values[i];
        }

    }

    public void SetColumn(int column, double[] values)
    {
        if (values.Length != this.NumberOfRows)
        {
            throw new NumberOfElementsException();
        }
        this.Element[column] = values;
    }

    public void RemoveRow(int row)
    {

        if (this.NumberOfRows <= row)
        {

            throw new OutOfBoundIndexException();

        }

        Matrix resultMatrix = new Matrix(this.NumberOfRows - 1, this.NumberOfColumns);

        int counter = 0;

        for (int i = 0; i < this.NumberOfRows; i++)
        {

            if (i == row) continue;

            resultMatrix.SetRow(counter, this.GetRow(i));

            counter += 1;

        }

        this.Reconstruct(resultMatrix);

    }

    public void RemoveColumn(int column)
    {

        if (this.NumberOfColumns <= column)
        {

            throw new OutOfBoundIndexException();

        }

        Matrix resultMatrix = new Matrix(this.NumberOfRows, this.NumberOfColumns - 1);

        int counter = 0;

        for (int i = 0; i < this.NumberOfColumns; i++)
        {

            if (i == column) continue;

            resultMatrix.SetColumn(counter, this.GetColumn(i));

            counter += 1;

        }

        this.Reconstruct(resultMatrix);

    }

    public void InsertRow(int row, double[] values)
    {

        if (this.NumberOfRows <= row)
        {

            throw new OutOfBoundIndexException();

        }

        Matrix resultMatrix = new Matrix(this.NumberOfRows + 1, this.NumberOfColumns);

        int counter = 0;

        for (int i = 0; i < this.NumberOfRows + 1; i++)
        {

            if (i == row)
            {

                resultMatrix.SetRow(i, values);

                continue;

            }

            resultMatrix.SetRow(i, this.GetRow(counter));

            counter += 1;

        }

        this.Reconstruct(resultMatrix);

    }

    public void InsertColumn(int column, double[] values)
    {

        if (this.NumberOfColumns <= column)
        {

            throw new OutOfBoundIndexException();

        }

        Matrix resultMatrix = new Matrix(this.NumberOfRows, this.NumberOfColumns + 1);

        int counter = 0;

        for (int i = 0; i < this.NumberOfColumns + 1; i++)
        {

            if (i == column)
            {

                resultMatrix.SetColumn(i, values);

                continue;

            }

            resultMatrix.SetColumn(i, this.GetColumn(counter));

            counter += 1;

        }

        this.Reconstruct(resultMatrix);

    }

    public void SwapRows(int index1, int index2)
    {

        double[] firstRow = this.GetRow(index1);

        double[] secondRow = this.GetRow(index2);

        this.SetRow(index1, secondRow);

        this.SetRow(index2, firstRow);

    }

    public void SwapColumns(int index1, int index2)
    {

        double[] firstColumns = this.GetColumn(index1);

        double[] secondColumns = this.GetColumn(index2);

        this.SetColumn(index1, firstColumns);

        this.SetColumn(index2, secondColumns);

    }

    private double CalculateTrace()
    {

        if (!this.IsSquare())
        {

            throw new NonSquareMatrixException();

        }

        double[] diagVector = this.DiagonalVector();

        double result = 0;

        for (int i = 0; i < this.NumberOfRows; i++)
        {

            result += diagVector[i];

        }

        return result;

    }

    public double MinorOf(int rowIndex, int columnIndex)
    {

        return ((this.CofactorMatrixOf(rowIndex, columnIndex)).Determinant);

    }

    public double CofactorOf(int rowIndex, int columnIndex)
    {

        return (Math.Pow(-1, rowIndex + columnIndex) * this.MinorOf(rowIndex, columnIndex));

    }

    public Matrix Subtract(Matrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new UnequalMatrixSizeException();
        }

        Matrix result = new Matrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {
                //result.SetValue(i, j, this.Element[j][i] - value.Element[j, i]);
                result[i, j] = this[i, j] - value[i, j];

            }
        }

        return result;
    }

    private double CalculateSumOfElemets()
    {
        double result = 0;

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {
                result += this[i, j];
            }
        }

        return result;
    }

    public double CalculateSumOfNonDiagonalElemets()
    {
        double result = 0;

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {
                if (i != j)
                {
                    result += this[i, j];
                }
            }
        }

        return result;
    }

    public double CalculateAbsoluteSumOfNonDiagonalElements()
    {
        double result = 0;

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {
                if (i != j)
                {
                    result += Math.Abs(this[i, j]);
                }
            }
        }

        return result;
    }

    public Matrix Negate()
    {

        for (int i = 0; i < this.NumberOfRows; i++)
        {

            for (int j = 0; j < this.NumberOfColumns; j++)
            {

                this[i, j] = -1 * this[i, j];

            }

        }

        return this;
    }

    public Matrix Multiply(Matrix value)
    {
        if (!CanBeMultiply(this, value))
        {
            throw new ImproperMatrixSizeForMultiplicationException();
        }

        int row1 = this.NumberOfRows;

        int column1 = this.NumberOfColumns;

        int column2 = value.NumberOfColumns;

        Matrix result = new Matrix(row1, column2);

        for (int i = 0; i < row1; i++)
        {
            for (int j = 0; j < column2; j++)
            {
                double tempValue = 0;

                for (int k = 0; k < column1; k++)
                {
                    tempValue += this[i, k] * value[k, j];
                }

                result[i, j] = tempValue;
            }
        }
        return result;
    }

    public Matrix DotMultiply(Matrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new UnequalMatrixSizeException();
        }

        Matrix result = new Matrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {

                result[i, j] = this[i, j] * value[i, j];

            }
        }
        return result;

    }

    public Matrix Multiply(double scalar)
    {
        int row = this.NumberOfRows;

        int column = this.NumberOfColumns;

        Matrix result = new Matrix(row, column);

        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < column; j++)
            {

                result[i, j] = scalar * this[i, j];

            }
        }
        return result;
    }

    public Matrix Add(Matrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new UnequalMatrixSizeException();
        }

        Matrix result = new Matrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {

                result[i, j] = this[i, j] + value[i, j];

            }
        }
        return result;
    }

    private double CalculateDeterminant()
    {

        if (!this.IsSquare())
        {

            throw new NonSquareMatrixException();

        }

        if (this.NumberOfColumns == 2)
        {

            return (this[0, 0] * this[1, 1] - this[0, 1] * this[1, 0]);

        }
        else
        {

            double result = 0;

            //Matrix tempMatrix = this.Clone();

            //tempMatrix.RemoveRow(0);

            //for (int i = 0; i < this.NumberOfColumns; i++)
            //{

            //    Matrix tempMatrix2 = tempMatrix.Clone();

            //    tempMatrix2.RemoveColumn(i);

            //    result += Math.Pow(-1, i) * tempMatrix2.CalculateDeterminant();

            //}

            for (int i = 0; i < this.NumberOfColumns; i++)
            {
                result += this[0, i] * this.CofactorOf(0, i);
            }
            return result;
        }
    }

    private double GetValue(int rowNumber, int columNumber)
    {
        if (rowNumber > this.NumberOfRows || columNumber > this.NumberOfColumns ||
                rowNumber < 0 || columNumber < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            return Element[columNumber][rowNumber];
        }
    }

    private void SetValue(int rowNumber, int columNumber, double value)
    {
        if (rowNumber > this.NumberOfRows || columNumber > this.NumberOfColumns ||
                rowNumber < 0 || columNumber < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            this.Element[columNumber][rowNumber] = value;
        }
    }

    private bool IsOutOfRange(int rowNumber, int columNumber)
    {
        return rowNumber >= this.NumberOfRows ||
                columNumber >= this.NumberOfColumns ||
                rowNumber < 0 ||
                columNumber < 0;
    }

    // single value means adjacent cells are all zero
    public bool AreAllAdjacentCellsZero(int rowNumber, int columNumber)
    {
        for (int i = rowNumber - 1; i <= rowNumber + 1; i++)
        {
            for (int j = columNumber - 1; j <= columNumber + 1; j++)
            {
                if (IsOutOfRange(i, j))
                    continue;

                if (i == rowNumber && j == columNumber)
                    continue;

                if (this[i, j] != 0)
                    return false;
            }
        }

        return true;
    }

    public int GetNumberOfCellsWithValue(double value)
    {
        int count = 0;

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {
                if (this[i, j] == value)
                    count++;
            }
        }

        return count;
    }

    //
    #region EigenvaluesEigenvectors

    public Matrix CalculateEigenvector()
    {
        EigenvaluesEigenvectors result = GetEigenvaluesEigenvectors();

        return result.EigenvectorMatrix;
    }

    public double[] CalculateEigenvalues()
    {
        //    Matrix eignVectors = CalculateEigenvector();

        //    double[] result = new double[eignVectors.NumberOfColumns];

        //    for (int i = 0; i < eignVectors.NumberOfColumns; i++)
        //    {
        //        Matrix tempEigenvector = new Matrix(NumberOfRows, 1);

        //        tempEigenvector.SetColumn(0, eignVectors.GetColumn(i));

        //        Matrix tempMultiplication = this * tempEigenvector;

        //        result[i] = tempMultiplication[0, 0] / tempEigenvector[0, 0];
        //    }

        //    return result;

        EigenvaluesEigenvectors result = GetEigenvaluesEigenvectors();

        return result.Eigenvlaues;
    }

    public EigenvaluesEigenvectors GetEigenvaluesEigenvectors()
    {
        if (!IsSymmetric())
        {
            throw new NotImplementedException();
        }

        //Q=wT * this * w
        Matrix Q = this.Clone();

        Matrix eigenvectors = Identity(this.NumberOfColumns); ;

        double threshold = Q.CalculateSumOfNonDiagonalElemets();

        do
        {
            DoSweep(ref Q, ref eigenvectors);

            threshold = Q.CalculateAbsoluteSumOfNonDiagonalElements();

        } while (threshold > 0.0000000001);

        //return W;
        double[] eigenvalues = new double[eigenvectors.NumberOfColumns];

        for (int i = 0; i < eigenvectors.NumberOfColumns; i++)
        {
            Matrix tempEigenvector = new Matrix(NumberOfRows, 1);

            tempEigenvector.SetColumn(0, eigenvectors.GetColumn(i));

            Matrix tempMultiplication = this * tempEigenvector;

            eigenvalues[i] = tempMultiplication[0, 0] / tempEigenvector[0, 0];
        }

        return new EigenvaluesEigenvectors(eigenvalues, eigenvectors);
    }

    private void DoSweep(ref Matrix Q, ref Matrix W)
    {
        for (int row = 0; row < this.NumberOfRows; row++)
        {
            for (int column = row + 1; column < this.NumberOfColumns; column++)
            {
                double rotationAngle = CalculateJacobiRotationAngle(Q[row, column], Q[column, column], Q[row, row]);

                Matrix P = CalculateJacobiRotationMatrix(row, column, rotationAngle);

                Q = P.Transpose() * Q * P;

                W = W * P;
            }
        }
    }

    //Angle is in Radian
    private double CalculateJacobiRotationAngle(double Qpq, double Qqq, double Qpp)
    {
        if (Qpp == Qqq)
        {
            return Math.PI / 2;
        }
        else
        {
            return -(1.0 / 2.0 * (Math.Atan2(2 * Qpq, Qqq - Qpp)));
        }
    }

    private Matrix CalculateJacobiRotationMatrix(int row, int column, double rotationAngle)
    {
        Matrix result = Identity(this.NumberOfColumns);

        result.SetValue(row, row, Math.Cos(rotationAngle));

        result.SetValue(row, column, -Math.Sin(rotationAngle));

        result.SetValue(column, row, Math.Sin(rotationAngle));

        result.SetValue(column, column, Math.Cos(rotationAngle));

        return result;
    }

    #endregion
    //

    #endregion

    #region StaticMembers

    public static bool AreCommutative(Matrix matrix1, Matrix matrix2)
    {

        if (!(matrix1.IsSquare() && matrix2.IsSquare()))
        {

            throw new NonSquareMatrixException();

        }

        return (matrix1 * matrix2 == matrix2 * matrix1);

    }

    public static bool AreTheSameSize(Matrix firstMatrix, Matrix secondMatrix)
    {
        return (firstMatrix.NumberOfRows == secondMatrix.NumberOfRows &&
                firstMatrix.NumberOfColumns == secondMatrix.NumberOfColumns);
    }

    public static bool CanBeMultiply(Matrix matrix1, Matrix matrix2)
    {

        return (matrix1.NumberOfColumns == matrix2.NumberOfRows);

    }

    public static Matrix Null()
    {
        return new Matrix();
    }

    public static Matrix Zeros(int size)
    {

        return new Matrix(size, size);

    }

    public static Matrix Zeros(int row, int column)
    {

        return new Matrix(row, column);

    }

    public static Matrix Ones(int size)
    {

        return Matrix.Ones(size, size);

    }

    public static Matrix Ones(int row, int column)
    {

        Matrix resultMatrix = new Matrix(row, column);

        double[] values = new double[column];

        for (int i = 0; i < column; i++)
        {

            values[i] = 1;

        }

        for (int i = 0; i < row; i++)
        {

            resultMatrix.SetRow(i, values);

        }

        return resultMatrix;

    }

    public static Matrix DiagonalMatrix(double[] values)
    {

        Matrix resultMatrix = new Matrix(values.Length);

        for (int i = 0; i < values.Length; i++)
        {

            resultMatrix[i, i] = values[i];

        }

        return resultMatrix;

    }

    public static Matrix Identity(int size)
    {

        Matrix resultMatrix = new Matrix(size, size);

        for (int i = 0; i < size; i++)
        {

            resultMatrix[i, i] = 1;

        }

        return resultMatrix;

    }

    public static void LU(Matrix matrix, out Matrix lowerMatrix, out Matrix upperMatrix)
    {

        if (!matrix.IsSquare())
        {
            throw new NonSquareMatrixException();
        }

        lowerMatrix = new Matrix(matrix.NumberOfRows, matrix.NumberOfColumns);

        lowerMatrix.SetColumn(0, matrix.GetColumn(0));

        upperMatrix = Matrix.Identity(matrix.NumberOfRows);

        for (int i = 0; i < matrix.NumberOfRows; i++)
        {

            for (int j = i + 1; j < matrix.NumberOfColumns; j++)
            {

                for (int k = 0; k <= i - 1; k++)
                {
                    upperMatrix[i, j] += lowerMatrix[i, k] * upperMatrix[k, j];
                }

                upperMatrix[i, j] = (matrix[i, j] - upperMatrix[i, j]) / lowerMatrix[i, i];

            }

            if (i == matrix.NumberOfColumns - 1)
            {
                continue;
            }

            for (int j = i + 1; j < matrix.NumberOfColumns; j++)
            {

                for (int k = 0; k <= i + 1 - 1; k++)
                {
                    lowerMatrix[j, i + 1] += lowerMatrix[j, k] * upperMatrix[k, i + 1];
                }

                lowerMatrix[j, i + 1] = matrix[j, i + 1] - lowerMatrix[j, i + 1];

            }

        }

    }

    public static Matrix ScalarMatrix(double value, int size)
    {

        return value * (new Matrix(size, size));

    }

    public static Matrix RotateAboutX(double theta)
    {
        //theta in radian
        double[][] result = new double[3][];

        result[0] = new double[] { 1, 0, 0 };
        result[1] = new double[] { 0, Math.Cos(theta), Math.Sin(theta) };
        result[2] = new double[] { 0, -Math.Sin(theta), Math.Cos(theta) };

        //double[,] result = new double[,]{
        //                                    {1,   0,                  0},
        //                                    {0,   Math.Cos(theta),    Math.Sin(theta)},
        //                                    {0,   -Math.Sin(theta),   Math.Cos(theta)}
        //                                };

        return new Matrix(result);

    }

    public static Matrix RotateAboutY(double theta)
    {

        //theta in radian
        double[][] result = new double[3][];

        result[0] = new double[] { Math.Cos(theta), 0, Math.Sin(theta) };
        result[1] = new double[] { 0, 1, 0 };
        result[2] = new double[] { -Math.Sin(theta), 0, Math.Cos(theta) };

        //double[,] result = new double[,]{
        //                                    {Math.Cos(theta), 0,  -Math.Sin(theta)},
        //                                    {0              , 1,  0},
        //                                    {Math.Sin(theta), 0,  Math.Cos(theta)}
        //                                };

        return new Matrix(result);

    }

    public static Matrix RotateAboutZ(double theta)
    {

        //theta in radian
        double[][] result = new double[3][];

        result[0] = new double[] { Math.Cos(theta), -Math.Sin(theta), 0 };
        result[1] = new double[] { Math.Sin(theta), Math.Cos(theta), 0 };
        result[2] = new double[] { 0, 0, 1 };

        //double[,] result = new double[,]{
        //                                    {Math.Cos(theta),     Math.Sin(theta),    0},
        //                                    {-Math.Sin(theta),    Math.Cos(theta),    0},
        //                                    {0,                   0,                  1}
        //                                };

        return new Matrix(result);

    }

    public static Matrix Rotate(double omega, double phi, double kappa)
    {
        //all in radian

        double[][] result = new double[3][];

        result[0] = new double[]{Math.Cos(phi) * Math.Cos(kappa),
                                    -Math.Cos(phi) * Math.Sin(kappa),
                                    Math.Sin(phi)};

        result[1] = new double[]{Math.Cos(omega) * Math.Sin(kappa) + Math.Sin(omega) * Math.Sin(phi) * Math.Cos(kappa),
                                    Math.Cos(omega) * Math.Cos(kappa) - Math.Sin(omega) * Math.Sin(phi) * Math.Sin(kappa),
                                    -Math.Sin(omega) * Math.Cos(phi)};

        result[2] = new double[]{Math.Sin(omega) * Math.Sin(kappa) - Math.Cos(omega) * Math.Sin(phi) * Math.Cos(kappa),
                                    Math.Sin(omega) * Math.Cos(kappa) + Math.Cos(omega) * Math.Sin(phi) * Math.Sin(kappa),
                                    Math.Cos(omega) * Math.Cos(phi)};

        return new Matrix(result);

    }

    public static Matrix CrossCorrelate(Matrix original, Matrix kernel)
    {
        return CrossCorrelate(original, kernel, true);
    }

    public static Matrix CrossCorrelate(Matrix original, Matrix kernel, bool keepOriginalSize)
    {
        int originalWidth = original.NumberOfColumns;

        int originalHeight = original.NumberOfRows;

        int kernelWidth = kernel.NumberOfColumns;

        int kernelHeight = kernel.NumberOfRows;

        int tempX = 0, tempY = 0;

        int width = originalWidth + kernelWidth - 1;

        int height = originalHeight + kernelHeight - 1;

        Matrix result;

        if (keepOriginalSize)
        {
            tempX = (int)Math.Ceiling(kernelWidth / 2.0) - kernelWidth % 2;

            tempY = (int)Math.Ceiling(kernelHeight / 2.0) - kernelHeight % 2;

            result = new Matrix(originalHeight, originalWidth);

            width = tempX + originalWidth;

            height = tempY + originalHeight;
        }
        else
        {
            result = new Matrix(height, width);
        }

        for (int x = tempX; x < width; x++)
        {
            for (int y = tempY; y < height; y++)
            {
                int startKernelX = (kernelWidth - x - 1 >= 0 ? kernelWidth - x - 1 : 0);

                int startKernelY = (kernelHeight - y - 1 >= 0 ? kernelHeight - y - 1 : 0);

                int endKernelX = (x - originalWidth + 1 < 0 ? kernelWidth - 1 : kernelWidth - (x - originalWidth + 1) - 1);

                int endKernelY = (y - originalHeight + 1 < 0 ? kernelHeight - 1 : kernelHeight - (y - originalHeight + 1) - 1);

                int startOriginalX = (x - kernelWidth < 0 ? 0 : x - kernelWidth + 1);

                int startOriginalY = (y - kernelHeight < 0 ? 0 : y - kernelHeight + 1);

                int endOriginalX = (x >= originalWidth ? originalWidth - 1 : x);

                int endOriginalY = (y >= originalHeight ? originalHeight - 1 : y);

                Matrix tempKernel = kernel.SubMatrix(startKernelY, startKernelX, endKernelY, endKernelX);

                Matrix tempOriginal = original.SubMatrix(startOriginalY, startOriginalX, endOriginalY, endOriginalX);

                result[y - tempY, x - tempX] = (tempKernel.DotMultiply(tempOriginal)).SumOfElements;
            }
        }

        return result;
    }

    //private static Matrix UsualCrossCorrelate(Matrix original, Matrix kernel)
    //{
    //    int originalWidth = original.NumberOfColumns;

    //    int originalHeight = original.NumberOfRows;

    //    int kernelWidth = kernel.NumberOfColumns;

    //    int kernelHeight = kernel.NumberOfRows;

    //    int tempKernelWidth = Math.Ceiling(kernelWidth / 2);

    //    int tempKernelHeight = Math.Ceiling(kernelHeight / 2);

    //    int width = originalWidth + kernelWidth - 1;

    //    int height = originalHeight + kernelHeight - 1;

    //    Matrix result = new Matrix(originalHeight, originalWidth);

    //    for (int x = 0; x < width; x++)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            int startKernelX = (tempKernelWidth - x - 1 >= 0 ? tempKernelWidth - x - 1 : 0);

    //            int startKernelY = (tempKernelHeight - y - 1 >= 0 ? tempKernelHeight - y - 1 : 0);

    //            int endKernelX = (x - originalWidth + 1 < 0 ? kernelWidth - 1 : kernelWidth - (x - originalWidth + 1) - 1);

    //            int endKernelY = (y - originalHeight + 1 < 0 ? kernelHeight - 1 : kernelHeight - (y - originalHeight + 1) - 1);

    //            int startOriginalX = (x - tempKernelWidth < 0 ? 0 : x - kernelWidth + 1);

    //            int startOriginalY = (y - kernelHeight < 0 ? 0 : y - kernelHeight + 1);

    //            int endOriginalX = (x >= originalWidth ? originalWidth - 1 : x);

    //            int endOriginalY = (y >= originalHeight ? originalHeight - 1 : y);

    //            Matrix tempKernel = kernel.SubMatrix(startKernelY, startKernelX, endKernelY, endKernelX);

    //            Matrix tempOriginal = original.SubMatrix(startOriginalY, startOriginalX, endOriginalY, endOriginalX);

    //            result[y, x] = (tempKernel.DotMultiply(tempOriginal)).SumOfElements;
    //        }
    //    }

    //    return result;
    //}

    public static Matrix Convolve(Matrix original, Matrix kernel)
    {
        return Convolve(original, kernel, true);
    }

    public static Matrix Convolve(Matrix original, Matrix kernel, bool keepOriginalSize)
    {
        Matrix tempKernel = new Matrix(kernel.NumberOfRows, kernel.NumberOfColumns);

        int width = kernel.NumberOfColumns;

        int height = kernel.NumberOfRows;

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                tempKernel[i, j] = kernel[height - i - 1, width - j - 1];
            }
        }

        return CrossCorrelate(original, tempKernel, keepOriginalSize);
    }

    #endregion

    #region Overrides

    public override bool Equals(object obj)
    {

        return (obj.ToString() == this.ToString());

    }

    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }

    public override string ToString()
    {

        string resultString = string.Empty;

        for (int i = 0; i < this.NumberOfRows; i++)
        {

            for (int j = 0; j < this.NumberOfColumns - 1; j++)
            {

                resultString += string.Format(CultureInfo.CurrentCulture, "{0},", this[i, j]);

            }

            resultString += string.Format(CultureInfo.CurrentCulture, "{0};", this[i, this.NumberOfColumns - 1]);

        }

        return resultString;

    }

    #endregion

    #region Operators

    public static Matrix operator *(double scalar, Matrix matrix)
    {
        return matrix.Multiply(scalar);
    }

    public static Matrix operator *(Matrix matrix1, Matrix matrix2)
    {
        return matrix1.Multiply(matrix2);
    }

    public static Matrix operator +(Matrix matrix1, Matrix matrix2)
    {
        return matrix1.Add(matrix2);
    }

    public static Matrix operator -(Matrix matrix1, Matrix matrix2)
    {
        return matrix1.Subtract(matrix2);
    }

    public static Matrix operator -(Matrix matrix)
    {
        return matrix.Negate();
    }

    public static bool operator ==(Matrix matrix1, Matrix matrix2)
    {
        if (object.ReferenceEquals(matrix1, null) && object.ReferenceEquals(matrix2, null))
            return true;

        if (object.ReferenceEquals(matrix1, null) || object.ReferenceEquals(matrix2, null))
            return false;


        int row1 = matrix1.NumberOfRows;

        int column1 = matrix1.NumberOfColumns;

        int row2 = matrix2.NumberOfRows;

        int column2 = matrix2.NumberOfColumns;

        if (column1 != column2 || row1 != row2)
        {
            return false;
        }

        for (int i = 0; i < row1; i++)
        {
            for (int j = 0; j < column2; j++)
            {
                if (Math.Round(matrix1[i, j], 10) != Math.Round(matrix2[i, j], 10))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool operator !=(Matrix matrix1, Matrix matrix2)
    {
        return !(matrix1 == matrix2);
    }

    #endregion

    #region Statistics

    public List<BasicStatisticsInfo> GetStatisticsByColumns()
    {
        List<BasicStatisticsInfo> result = new List<BasicStatisticsInfo>();

        for (int i = 0; i < NumberOfColumns; i++)
        {
            result.Add(new BasicStatisticsInfo(this.GetColumn(i)));
        }

        return result;
    }

    #endregion
}

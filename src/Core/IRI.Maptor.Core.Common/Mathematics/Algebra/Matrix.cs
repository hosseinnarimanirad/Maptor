// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj

using System;
using System.Text;
using System.Globalization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using IRI.Maptor.Core.Common.Mathematics;

namespace IRI.Maptor.Core.Common.Mathematics;

[DataContract]
public class Matrix
{
    #region Fields&Properties

    // column-major jagged storage: Element[column][row].
    // note: the double[][] constructor therefore takes COLUMNS as its inner
    // arrays, while the double[,] constructor takes rows ([row, column])
    protected double[][] Element = [];

    public double this[int rowNumber, int columnNumber]
    {
        get { return this.GetValue(rowNumber, columnNumber); }

        set { this.SetValue(rowNumber, columnNumber, value); }
    }

    public int NumberOfRows => this.Element[0].Length;

    public int NumberOfColumns => this.Element.Length;

    public double Trace => CalculateTrace();

    public double Determinant => CalculateDeterminant();

    public double SumOfElements => CalculateSumOfElements();

    #endregion

    #region Constructors

    public Matrix()
        : this(1, 1)
    {
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

    /// <summary>
    /// Determines whether the matrix equals its transpose:
    /// <c>this == this.Transpose()</c> (10-decimal rounding tolerance, same as
    /// operator==). Non-square matrices return false.
    /// </summary>
    public bool IsSymmetric()
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        if (rows != columns)
            return false;

        for (int i = 0; i < rows; i++)
        {
            for (int j = i + 1; j < columns; j++)
            {
                if (Math.Round(Element[j][i], 10) != Math.Round(Element[i][j], 10))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether every off-diagonal element is 0, equivalent to
    /// <c>(this - DiagonalMatrix(DiagonalVector())) == Zeros(n)</c>
    /// (10-decimal rounding tolerance, same as operator==).
    /// </summary>
    public bool IsDiagonal()
    {
        if (!this.IsSquare())
            throw new NonSquareMatrixException();

        int n = this.NumberOfColumns;

        for (int j = 0; j < n; j++)
        {
            double[] column = Element[j];

            for (int i = 0; i < n; i++)
            {
                if (i != j && Math.Round(column[i], 10) != 0)
                    return false;
            }
        }

        return true;
    }

    public bool IsSingular() => (this.Determinant == 0);

    public bool IsNonsingular() => !(this.Determinant == 0);

    /// <summary>
    /// Determines whether the matrix is orthogonal, equivalent to
    /// <c>this * this.Transpose() == Identity(n)</c>
    /// (10-decimal rounding tolerance, same as operator==).
    /// </summary>
    public bool IsOrthogonal()
    {
        if (!this.IsSquare())
            throw new NonSquareMatrixException();

        int n = this.NumberOfColumns;

        // entry [i,j] of this * Transpose() is the dot product of rows i and j;
        // k-ascending accumulation matches what the full product would compute
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                double dot = 0;

                for (int k = 0; k < n; k++)
                {
                    dot += Element[k][i] * Element[k][j];
                }

                if (Math.Round(dot, 10) != (i == j ? 1 : 0))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Deep copy: the result shares no storage with this matrix.
    /// </summary>
    public Matrix Clone()
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        Matrix result = new Matrix(rows, columns);

        for (int j = 0; j < columns; j++)
        {
            Array.Copy(Element[j], result.Element[j], rows);
        }

        return result;
    }

    public Matrix Negative() => -this;

    /// <summary>
    /// Copies the inclusive block
    /// <c>[startRow..endRow] x [startColumn..endColumns]</c> into a new matrix.
    /// </summary>
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

        for (int c = startColumn; c <= endColumns; c++)
        {
            double[] sourceColumn = Element[c];

            double[] resultColumn = result.Element[c - startColumn];

            for (int r = startRow; r <= endRow; r++)
            {
                resultColumn[r - startRow] = sourceColumn[r];
            }
        }

        return result;
    }

    /// <summary>
    /// Transpose: <c>result[j,i] = this[i,j]</c>.
    /// </summary>
    public Matrix Transpose()
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        Matrix result = new Matrix(columns, rows);

        for (int r = 0; r < rows; r++)
        {
            // row r of this becomes column r of the result
            double[] resultColumn = result.Element[r];

            for (int c = 0; c < columns; c++)
            {
                resultColumn[c] = Element[c][r];
            }
        }

        return result;
    }
      
    /// <summary>
    /// Returns the (n-1)x(n-1) submatrix obtained by removing row
    /// <paramref name="rowNumber"/> and column <paramref name="columnNumber"/>.
    /// The source matrix is not modified.
    /// </summary>
    public Matrix CofactorMatrixOf(int rowNumber, int columnNumber)
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        if (rows <= rowNumber || columns <= columnNumber)
        {
            throw new OutOfBoundIndexException();
        }

        Matrix result = new Matrix(rows - 1, columns - 1);

        int targetColumn = 0;

        for (int c = 0; c < columns; c++)
        {
            if (c == columnNumber) continue;

            double[] sourceColumn = Element[c];

            double[] resultColumn = result.Element[targetColumn];

            int targetRow = 0;

            for (int r = 0; r < rows; r++)
            {
                if (r == rowNumber) continue;

                resultColumn[targetRow] = sourceColumn[r];

                targetRow++;
            }

            targetColumn++;
        }

        return result;
    }

    /// <summary>
    /// Computes the adjugate (classical adjoint): the transpose of the cofactor
    /// matrix, <c>adj[i,j] = CofactorOf(j, i)</c>.
    /// </summary>
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

    /// <summary>
    /// Computes the inverse, defined as <c>1 / Determinant * Adjoint()</c>.
    /// Implemented as closed adjugate forms for n &lt;= 3 and an LU solve of
    /// <c>A·x_j = e_j</c> per column for n &gt;= 4. A singular matrix does not
    /// throw: it yields an Infinity/NaN-filled result, matching the definition.
    /// </summary>
    public Matrix Inverse()
    {
        if (!this.IsSquare())
            throw new NonSquareMatrixException();

        int n = this.NumberOfColumns;

        if (n == 1)
        {
            // legacy path, kept verbatim for 1x1 behavior compatibility
            return 1 / this.Determinant * this.Adjoint();
        }

        if (n == 2)
        {
            double[] c0 = Element[0]; double[] c1 = Element[1];

            double invDet = 1 / this.Determinant;

            Matrix result = new Matrix(2, 2);

            double[] r0 = result.Element[0]; double[] r1 = result.Element[1];

            // inv[i,j] = invDet * cofactor(j,i), stored column-major
            r0[0] = invDet * c1[1];
            r1[0] = invDet * -c1[0];
            r0[1] = invDet * -c0[1];
            r1[1] = invDet * c0[0];

            return result;
        }

        if (n == 3)
        {
            double[] c0 = Element[0]; double[] c1 = Element[1]; double[] c2 = Element[2];

            double invDet = 1 / this.Determinant;

            Matrix result = new Matrix(3, 3);

            // 2x2 minor of the columns (colA, colB) restricted to rows (r1, r2),
            // operand order matching the former CofactorMatrixOf(...).Determinant
            static double Minor(double[] colA, double[] colB, int r1, int r2)
                => colA[r1] * colB[r2] - colB[r1] * colA[r2];

            double[] r0 = result.Element[0]; double[] r1 = result.Element[1]; double[] r2 = result.Element[2];

            // inv[i,j] = invDet * cofactor(j,i), stored column-major: Element[j][i]
            r0[0] = invDet * Minor(c1, c2, 1, 2);
            r0[1] = invDet * -Minor(c0, c2, 1, 2);
            r0[2] = invDet * Minor(c0, c1, 1, 2);

            r1[0] = invDet * -Minor(c1, c2, 0, 2);
            r1[1] = invDet * Minor(c0, c2, 0, 2);
            r1[2] = invDet * -Minor(c0, c1, 0, 2);

            r2[0] = invDet * Minor(c1, c2, 0, 1);
            r2[1] = invDet * -Minor(c0, c2, 0, 1);
            r2[2] = invDet * Minor(c0, c1, 0, 1);

            return result;
        }

        // n >= 4: factor once, then solve for each unit vector
        {
            double[][] lu = CopyToRowMajor();

            int[] pivot = CreateIdentityPermutation(n);

            if (!TryFactorLu(lu, pivot, out _))
            {
                // singular: keep the legacy non-throwing Infinity/NaN result
                return 1 / this.Determinant * this.Adjoint();
            }

            Matrix result = new Matrix(n, n);

            for (int j = 0; j < n; j++)
            {
                // column j of the inverse, solved in place: A·x = e_j  =>  L·U·x = P·e_j
                double[] x = result.Element[j];

                // forward substitution with unit lower L (stored below the diagonal)
                for (int r = 0; r < n; r++)
                {
                    double value = pivot[r] == j ? 1.0 : 0.0;

                    double[] row = lu[r];

                    for (int c = 0; c < r; c++)
                    {
                        value -= row[c] * x[c];
                    }

                    x[r] = value;
                }

                // back substitution with U
                for (int r = n - 1; r >= 0; r--)
                {
                    double value = x[r];

                    double[] row = lu[r];

                    for (int c = r + 1; c < n; c++)
                    {
                        value -= row[c] * x[c];
                    }

                    x[r] = value / row[r];
                }
            }

            return result;
        }
    }

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
        int columns = this.NumberOfColumns;

        double[] result = new double[columns];

        for (int i = 0; i < columns; i++)
        {
            result[i] = Element[i][row];
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

        this.SetColumn(index1, secondColumns);

        this.SetColumn(index2, firstColumns);

    }

    // trace = sum of the diagonal: sum_i this[i,i]
    private double CalculateTrace()
    {

        if (!this.IsSquare())
        {

            throw new NonSquareMatrixException();

        }

        int n = this.NumberOfRows;

        double result = 0;

        for (int i = 0; i < n; i++)
        {
            result += Element[i][i];
        }

        return result;

    }

    /// <summary>
    /// Computes the minor: the determinant of the submatrix obtained by removing
    /// row <paramref name="rowIndex"/> and column <paramref name="columnIndex"/>.
    /// </summary>
    public double MinorOf(int rowIndex, int columnIndex)
    {

        return ((this.CofactorMatrixOf(rowIndex, columnIndex)).Determinant);

    }

    /// <summary>
    /// Computes the cofactor:
    /// <c>cofactor(i,j) = (-1)^(i+j) * MinorOf(i, j)</c>.
    /// </summary>
    public double CofactorOf(int rowIndex, int columnIndex)
    {
        double minor = this.MinorOf(rowIndex, columnIndex);

        return ((rowIndex + columnIndex) & 1) == 0 ? minor : -minor;
    }

    /// <summary>
    /// Element-wise difference: <c>result[i,j] = this[i,j] - value[i,j]</c>.
    /// </summary>
    public Matrix Subtract(Matrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new UnequalMatrixSizeException();
        }

        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        Matrix result = new Matrix(rows, columns);

        for (int j = 0; j < columns; j++)
        {
            double[] thisColumn = Element[j];

            double[] valueColumn = value.Element[j];

            double[] resultColumn = result.Element[j];

            for (int i = 0; i < rows; i++)
            {
                resultColumn[i] = thisColumn[i] - valueColumn[i];
            }
        }

        return result;
    }

    private double CalculateSumOfElements()
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        double result = 0;

        // row-major accumulation order kept for bit-stable sums
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                result += Element[j][i];
            }
        }

        return result;
    }

    public double CalculateSumOfNonDiagonalElements()
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        double result = 0;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (i != j)
                {
                    result += Element[j][i];
                }
            }
        }

        return result;
    }

    public double CalculateAbsoluteSumOfNonDiagonalElements()
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        double result = 0;

        // row-major accumulation order kept: this sum is the Jacobi convergence metric
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (i != j)
                {
                    result += Math.Abs(Element[j][i]);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Negates the matrix IN PLACE: <c>this[i,j] = -this[i,j]</c>, then returns
    /// <c>this</c>. For a non-mutating negation use the unary <c>-</c> operator
    /// or <see cref="Negative"/>.
    /// </summary>
    public Matrix Negate()
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        for (int j = 0; j < columns; j++)
        {
            double[] column = Element[j];

            for (int i = 0; i < rows; i++)
            {
                column[i] = -column[i];
            }
        }

        return this;
    }

    /// <summary>
    /// Matrix product: <c>result[i,j] = sum_k this[i,k] * value[k,j]</c>.
    /// (The loops run saxpy-style over the column-major storage for cache
    /// locality; each element accumulates in the same k-ascending order as the
    /// classic row-by-column dot product.)
    /// </summary>
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

        for (int j = 0; j < column2; j++)
        {
            double[] resultColumn = result.Element[j];

            double[] valueColumn = value.Element[j];

            // k ascending keeps each element's accumulation order identical
            // to the classic row-by-column dot product
            for (int k = 0; k < column1; k++)
            {
                double scalar = valueColumn[k];

                double[] thisColumn = Element[k];

                for (int i = 0; i < row1; i++)
                {
                    resultColumn[i] += thisColumn[i] * scalar;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Element-wise (Hadamard) product: <c>result[i,j] = this[i,j] * value[i,j]</c>.
    /// </summary>
    public Matrix DotMultiply(Matrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new UnequalMatrixSizeException();
        }

        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        Matrix result = new Matrix(rows, columns);

        for (int j = 0; j < columns; j++)
        {
            double[] thisColumn = Element[j];

            double[] valueColumn = value.Element[j];

            double[] resultColumn = result.Element[j];

            for (int i = 0; i < rows; i++)
            {
                resultColumn[i] = thisColumn[i] * valueColumn[i];
            }
        }
        return result;

    }

    /// <summary>
    /// Scalar product: <c>result[i,j] = scalar * this[i,j]</c>.
    /// </summary>
    public Matrix Multiply(double scalar)
    {
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        Matrix result = new Matrix(rows, columns);

        for (int j = 0; j < columns; j++)
        {
            double[] thisColumn = Element[j];

            double[] resultColumn = result.Element[j];

            for (int i = 0; i < rows; i++)
            {
                resultColumn[i] = scalar * thisColumn[i];
            }
        }
        return result;
    }

    /// <summary>
    /// Element-wise sum: <c>result[i,j] = this[i,j] + value[i,j]</c>.
    /// </summary>
    public Matrix Add(Matrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new UnequalMatrixSizeException();
        }

        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        Matrix result = new Matrix(rows, columns);

        for (int j = 0; j < columns; j++)
        {
            double[] thisColumn = Element[j];

            double[] valueColumn = value.Element[j];

            double[] resultColumn = result.Element[j];

            for (int i = 0; i < rows; i++)
            {
                resultColumn[i] = thisColumn[i] + valueColumn[i];
            }
        }
        return result;
    }

    // det(A) by cofactor expansion along the first row for n <= 3:
    //   det = sum_i a[0,i] * cofactor(0,i)
    // and by LU factorization for n >= 4:
    //   det = sign(P) * product of the U diagonal
    private double CalculateDeterminant()
    {

        if (!this.IsSquare())
        {

            throw new NonSquareMatrixException();

        }

        int n = this.NumberOfColumns;

        if (n == 1)
        {
            return Element[0][0];
        }
        else if (n == 2)
        {
            double[] c0 = Element[0]; double[] c1 = Element[1];

            return c0[0] * c1[1] - c1[0] * c0[1];
        }
        else if (n == 3)
        {
            double[] c0 = Element[0]; double[] c1 = Element[1]; double[] c2 = Element[2];

            // cofactor expansion along the first row; term and operand order match
            // the former recursive expansion bit-for-bit (sign-sensitive geometry
            // predicates rely on the exact 3x3 result)
            double result = c0[0] * (c1[1] * c2[2] - c2[1] * c1[2]);

            result += c1[0] * -(c0[1] * c2[2] - c2[1] * c0[2]);

            result += c2[0] * (c0[1] * c1[2] - c1[1] * c0[2]);

            return result;
        }
        else
        {
            // n >= 4: LU factorization with partial pivoting, O(n^3)
            double[][] lu = CopyToRowMajor();

            int[] pivot = CreateIdentityPermutation(n);

            if (!TryFactorLu(lu, pivot, out int sign))
            {
                return 0.0;
            }

            double result = sign;

            for (int i = 0; i < n; i++)
            {
                result *= lu[i][i];
            }

            return result;
        }
    }

    private double[][] CopyToRowMajor()
    {
        int n = this.NumberOfRows;

        double[][] result = new double[n][];

        for (int r = 0; r < n; r++)
        {
            double[] row = new double[n];

            for (int c = 0; c < n; c++)
            {
                row[c] = Element[c][r];
            }

            result[r] = row;
        }

        return result;
    }

    private static int[] CreateIdentityPermutation(int n)
    {
        int[] result = new int[n];

        for (int i = 0; i < n; i++)
        {
            result[i] = i;
        }

        return result;
    }

    // Doolittle LU factorization with partial pivoting, in place on a row-major
    // scratch copy: unit lower factors end up below the diagonal, U on and above it.
    // pivot[r] tracks which original row sits at position r; sign is the permutation
    // parity. Returns false when the matrix is singular.
    private static bool TryFactorLu(double[][] a, int[] pivot, out int sign)
    {
        int n = a.Length;

        sign = 1;

        for (int k = 0; k < n; k++)
        {
            int p = k;

            double max = Math.Abs(a[k][k]);

            for (int r = k + 1; r < n; r++)
            {
                double abs = Math.Abs(a[r][k]);

                if (abs > max)
                {
                    max = abs;
                    p = r;
                }
            }

            if (max == 0)
            {
                return false;
            }

            if (p != k)
            {
                (a[p], a[k]) = (a[k], a[p]);
                (pivot[p], pivot[k]) = (pivot[k], pivot[p]);
                sign = -sign;
            }

            double pivotValue = a[k][k];

            for (int r = k + 1; r < n; r++)
            {
                double factor = a[r][k] / pivotValue;

                a[r][k] = factor;

                if (factor == 0)
                {
                    continue;
                }

                double[] rowR = a[r]; double[] rowK = a[k];

                for (int c = k + 1; c < n; c++)
                {
                    rowR[c] -= factor * rowK[c];
                }
            }
        }

        return true;
    }

    private double GetValue(int rowNumber, int columnNumber)
    {
        if (rowNumber > this.NumberOfRows || columnNumber > this.NumberOfColumns ||
                rowNumber < 0 || columnNumber < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            return Element[columnNumber][rowNumber];
        }
    }

    private void SetValue(int rowNumber, int columnNumber, double value)
    {
        if (rowNumber > this.NumberOfRows || columnNumber > this.NumberOfColumns ||
                rowNumber < 0 || columnNumber < 0)
        {
            throw new OutOfBoundIndexException();
        }
        else
        {
            this.Element[columnNumber][rowNumber] = value;
        }
    }

    private bool IsOutOfRange(int rowNumber, int columnNumber)
    {
        return rowNumber >= this.NumberOfRows ||
                columnNumber >= this.NumberOfColumns ||
                rowNumber < 0 ||
                columnNumber < 0;
    }

    // single value means adjacent cells are all zero
    public bool AreAllAdjacentCellsZero(int rowNumber, int columnNumber)
    {
        for (int i = rowNumber - 1; i <= rowNumber + 1; i++)
        {
            for (int j = columnNumber - 1; j <= columnNumber + 1; j++)
            {
                if (IsOutOfRange(i, j))
                    continue;

                if (i == rowNumber && j == columnNumber)
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

        return result.Eigenvalues;
    }

    /// <summary>
    /// Cyclic Jacobi eigenvalue algorithm for SYMMETRIC matrices: Givens
    /// rotations drive <c>Q = W^T * A * W</c> to diagonal form. The diagonal of
    /// Q holds the eigenvalues; column i of W is the eigenvector paired with
    /// eigenvalue i. Iterates until the absolute off-diagonal sum falls below
    /// 1e-10, capped at 100 sweeps.
    /// </summary>
    public EigenvaluesEigenvectors GetEigenvaluesEigenvectors()
    {
        if (!IsSymmetric())
        {
            throw new NotImplementedException();
        }

        //Q=wT * this * w
        Matrix Q = this.Clone();

        Matrix eigenvectors = Identity(this.NumberOfColumns);

        // cap the sweeps so pathological input returns a near-converged result
        // instead of looping forever
        const int maxSweeps = 100;

        for (int sweep = 0; sweep < maxSweeps; sweep++)
        {
            DoSweep(Q, eigenvectors);

            if (Q.CalculateAbsoluteSumOfNonDiagonalElements() <= 0.0000000001)
                break;
        }

        //Q has converged to diagonal form; Q[i,i] is the eigenvalue of eigenvector column i
        double[] eigenvalues = Q.DiagonalVector();

        return new EigenvaluesEigenvectors(eigenvalues, eigenvectors);
    }

    // one Jacobi sweep, applied in place: for each plane (row, column) the update
    // Q <- P^T * Q * P and W <- W * P only touches rows/columns `row` and `column`
    // (P is the Givens rotation with P[row,row]=cos, P[row,column]=-sin,
    // P[column,row]=sin, P[column,column]=cos), so each rotation is O(n) instead
    // of three full O(n^3) matrix products
    private void DoSweep(Matrix Q, Matrix W)
    {
        int n = this.NumberOfRows;

        double[][] qElement = Q.Element;

        double[][] wElement = W.Element;

        for (int row = 0; row < n; row++)
        {
            for (int column = row + 1; column < n; column++)
            {
                double rotationAngle = CalculateJacobiRotationAngle(qElement[column][row], qElement[column][column], qElement[row][row]);

                // exact no-op rotation (cos 1, sin 0); do NOT skip merely on
                // Q[row,column] == 0 — Atan2(0, negative) yields a ±π/2 rotation
                // that swaps diagonal entries and must still be applied
                if (rotationAngle == 0.0)
                    continue;

                double cos = Math.Cos(rotationAngle);

                double sin = Math.Sin(rotationAngle);

                // Q <- P^T * Q : combine rows `row` and `column`
                for (int k = 0; k < n; k++)
                {
                    double[] columnK = qElement[k];

                    double qpk = columnK[row];

                    double qqk = columnK[column];

                    columnK[row] = cos * qpk + sin * qqk;

                    columnK[column] = -sin * qpk + cos * qqk;
                }

                // Q <- Q * P : combine columns `row` and `column` (contiguous arrays)
                double[] qColumnP = qElement[row];

                double[] qColumnQ = qElement[column];

                for (int k = 0; k < n; k++)
                {
                    double qkp = qColumnP[k];

                    double qkq = qColumnQ[k];

                    qColumnP[k] = cos * qkp + sin * qkq;

                    qColumnQ[k] = -sin * qkp + cos * qkq;
                }

                // W <- W * P
                double[] wColumnP = wElement[row];

                double[] wColumnQ = wElement[column];

                for (int k = 0; k < n; k++)
                {
                    double wkp = wColumnP[k];

                    double wkq = wColumnQ[k];

                    wColumnP[k] = cos * wkp + sin * wkq;

                    wColumnQ[k] = -sin * wkp + cos * wkq;
                }
            }
        }
    }

    //Angle is in Radian
    //when Qpp == Qqq, Atan2(2*Qpq, 0) gives ±π/2 so the angle is ∓π/4, which zeroes the off-diagonal element
    private double CalculateJacobiRotationAngle(double Qpq, double Qqq, double Qpp)
    {
        return -(1.0 / 2.0 * (Math.Atan2(2 * Qpq, Qqq - Qpp)));
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

        return value * Matrix.Identity(size);

    }

    public static Matrix RotateAboutX(double theta)
    {
        //theta in radian
        //each inner array is a column of the matrix
        double[][] result = new double[3][];

        result[0] = new double[] { 1, 0, 0 };
        result[1] = new double[] { 0, Math.Cos(theta), -Math.Sin(theta) };
        result[2] = new double[] { 0, Math.Sin(theta), Math.Cos(theta) };

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

    // intentionally kept as a ToString comparison: an element-wise rewrite would
    // silently change semantics (round-trip-format equality vs the 10-decimal
    // rounding used by operator==)
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
        int rows = this.NumberOfRows;

        int columns = this.NumberOfColumns;

        StringBuilder result = new StringBuilder();

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns - 1; j++)
            {
                result.AppendFormat(CultureInfo.CurrentCulture, "{0},", Element[j][i]);
            }

            result.AppendFormat(CultureInfo.CurrentCulture, "{0};", Element[columns - 1][i]);
        }

        return result.ToString();

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
        return matrix.Multiply(-1);
    }

    /// <summary>
    /// Element-wise equality with both sides rounded to 10 decimal places:
    /// <c>Math.Round(a[i,j], 10) == Math.Round(b[i,j], 10)</c> for every element.
    /// </summary>
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

        for (int j = 0; j < column1; j++)
        {
            double[] column1Values = matrix1.Element[j];

            double[] column2Values = matrix2.Element[j];

            for (int i = 0; i < row1; i++)
            {
                if (Math.Round(column1Values[i], 10) != Math.Round(column2Values[i], 10))
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

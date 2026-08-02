// besmellahe rahmane rahim
// Allahomma ajjel le-valiyek al-faraj
 
using System.Globalization; 

namespace IRI.Maptor.Ket.DigitalImageProcessing;

public class ImageMatrix
{

    #region Fields&Properties

    private byte[][] Element = new byte[0][];

    public byte this[int rowNumber, int columNumber]
    {
        get { return this.GetValue(rowNumber, columNumber); }

        set { this.SetValue(rowNumber, columNumber, value); }
    }

    public int NumberOfRows
    {
        get { return this.Element[0].Length; }
    }

    public int NumberOfColumns
    {
        get { return this.Element.Length; }
    }

    //public byte Trace
    //{
    //    get { return CalculateTrace(); }
    //}

    //public byte SumOfElements
    //{
    //    get { return CalculateSumOfElements(); }
    //}

    #endregion


    #region Constructors

    public ImageMatrix()
    {
    }

    public ImageMatrix(byte[][] ImageMatrix)
    {
        this.Element = ImageMatrix;
    }

    public ImageMatrix(int size)
        : this(size, size)
    {
    }

    public ImageMatrix(int row, int column)
    {

        Array.Resize(ref this.Element, column);

        for (int i = 0; i < column; i++)
        {
            Array.Resize(ref this.Element[i], row);
        }

    }

    #endregion


    #region Methods

    //public bool IsRowImageMatrix()
    //{

    //    return (this.NumberOfRows == 1);

    //}

    //public bool IsColumnImageMatrix()
    //{

    //    return (this.NumberOfColumns == 1);

    //}

    //public bool IsSquare()
    //{
    //    return (this.NumberOfColumns == this.NumberOfRows);
    //}

    //public bool IsSymmetric()
    //{

    //    return (this == this.Transpose());

    //}

    //public bool IsDiagonal()
    //{

    //    if (!this.IsSquare())
    //    {

    //        throw new NonSquareImageMatrixException();

    //    }

    //    return ((this.Clone() - ImageMatrix.DiagonalImageMatrix(this.DiagonalVector())) == ImageMatrix.Zeros(this.NumberOfColumns));

    //}

    //public bool IsOrthogonal()
    //{

    //    if (!this.IsSquare())
    //    {
    //        throw new NonSquareImageMatrixException();
    //    }

    //    return (this * this.Transpose() == ImageMatrix.Identity(this.NumberOfColumns));

    //}

    public ImageMatrix Clone()
    {

        ImageMatrix resultImageMatrix = new ImageMatrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfColumns; i++)
        {

            resultImageMatrix.SetColumn(i, this.GetColumn(i));

        }

        return (resultImageMatrix);

    }

    public ImageMatrix SubImageMatrix(int startRow, int startColumn, int endRow, int endColumns)
    {

        if (startRow > endRow || startColumn > endColumns)
        {

            throw new NotImplementedException();

        }

        if (this.NumberOfRows < endRow || this.NumberOfColumns < endColumns)
        {

            throw new NotImplementedException();

        }

        ImageMatrix result = new ImageMatrix(endRow - startRow + 1, endColumns - startColumn + 1);

        for (int i = 0; i < endRow - startRow + 1; i++)
        {

            for (int j = 0; j < endColumns - startColumn + 1; j++)
            {

                result[i, j] = this[i + startRow, j + startColumn];

            }

        }

        return result;
    }

    //public ImageMatrix Transpose()
    //{

    //    ImageMatrix result = new ImageMatrix(this.NumberOfColumns, this.NumberOfRows);

    //    for (int i = 0; i < this.NumberOfRows; i++)
    //    {

    //        result.SetColumn(i, this.GetRow(i));

    //    }

    //    return result;
    //}

    //public byte[] DiagonalVector()
    //{

    //    if (!this.IsSquare())
    //    {

    //        throw new NonSquareImageMatrixException();

    //    }

    //    byte[] result = new byte[this.NumberOfRows];

    //    for (int i = 0; i < this.NumberOfRows; i++)
    //    {

    //        result[i] = this[i, i];

    //    }

    //    return result;

    //}

    public byte[] GetRow(int row)
    {

        byte[] result = new byte[this.NumberOfColumns];

        for (int i = 0; i < this.NumberOfColumns; i++)
        {

            result[i] = this.Element[i][row];

        }

        return result;

    }

    public byte[] GetColumn(int column)
    {

        return this.Element[column];

    }

    public void Reconstruct(ImageMatrix ImageMatrix)
    {

        Array.Resize(ref this.Element, ImageMatrix.NumberOfColumns);

        for (int i = 0; i < ImageMatrix.NumberOfColumns; i++)
        {

            Element[i] = ImageMatrix.Element[i];

        }

    }

    public void SetRow(int row, byte[] values)
    {

        if (values.Length != this.NumberOfColumns)
        {
            throw new NotImplementedException();
        }

        for (int i = 0; i < this.NumberOfColumns; i++)
        {
            this.Element[i][row] = values[i];
        }

    }

    public void SetColumn(int column, byte[] values)
    {
        if (values.Length != this.NumberOfRows)
        {
            throw new NotImplementedException();
        }
        this.Element[column] = values;
    }

    //public void RemoveRow(int row)
    //{

    //    if (this.NumberOfRows <= row)
    //    {

    //        throw new NotImplementedException();

    //    }

    //    ImageMatrix resultImageMatrix = new ImageMatrix(this.NumberOfRows - 1, this.NumberOfColumns);

    //    int counter = 0;

    //    for (int i = 0; i < this.NumberOfRows; i++)
    //    {

    //        if (i == row) continue;

    //        resultImageMatrix.SetRow(counter, this.GetRow(i));

    //        counter += 1;

    //    }

    //    this.Reconstruct(resultImageMatrix);

    //}

    //public void RemoveColumn(int column)
    //{

    //    if (this.NumberOfColumns <= column)
    //    {

    //        throw new OutOfBoundIndexException();

    //    }

    //    ImageMatrix resultImageMatrix = new ImageMatrix(this.NumberOfRows, this.NumberOfColumns - 1);

    //    int counter = 0;

    //    for (int i = 0; i < this.NumberOfColumns; i++)
    //    {

    //        if (i == column) continue;

    //        resultImageMatrix.SetColumn(counter, this.GetColumn(i));

    //        counter += 1;

    //    }

    //    this.Reconstruct(resultImageMatrix);

    //}

    //public void InsertRow(int row, byte[] values)
    //{

    //    if (this.NumberOfRows <= row)
    //    {

    //        throw new OutOfBoundIndexException();

    //    }

    //    ImageMatrix resultImageMatrix = new ImageMatrix(this.NumberOfRows + 1, this.NumberOfColumns);

    //    int counter = 0;

    //    for (int i = 0; i < this.NumberOfRows + 1; i++)
    //    {

    //        if (i == row)
    //        {

    //            resultImageMatrix.SetRow(i, values);

    //            continue;

    //        }

    //        resultImageMatrix.SetRow(i, this.GetRow(counter));

    //        counter += 1;

    //    }

    //    this.Reconstruct(resultImageMatrix);

    //}

    //public void InsertColumn(int column, byte[] values)
    //{

    //    if (this.NumberOfColumns <= column)
    //    {

    //        throw new OutOfBoundIndexException();

    //    }

    //    ImageMatrix resultImageMatrix = new ImageMatrix(this.NumberOfRows, this.NumberOfColumns + 1);

    //    int counter = 0;

    //    for (int i = 0; i < this.NumberOfColumns + 1; i++)
    //    {

    //        if (i == column)
    //        {

    //            resultImageMatrix.SetColumn(i, values);

    //            continue;

    //        }

    //        resultImageMatrix.SetColumn(i, this.GetColumn(counter));

    //        counter += 1;

    //    }

    //    this.Reconstruct(resultImageMatrix);

    //}

    //public void SwapRows(int index1, int index2)
    //{

    //    byte[] firstRow = this.GetRow(index1);

    //    byte[] secondRow = this.GetRow(index2);

    //    this.SetRow(index1, secondRow);

    //    this.SetRow(index2, firstRow);

    //}

    //public void SwapColumns(int index1, int index2)
    //{

    //    byte[] firstColumns = this.GetColumn(index1);

    //    byte[] secondColumns = this.GetColumn(index2);

    //    this.SetColumn(index1, firstColumns);

    //    this.SetColumn(index2, secondColumns);

    //}

    //private byte CalculateTrace()
    //{

    //    if (!this.IsSquare())
    //    {

    //        throw new NonSquareImageMatrixException();

    //    }

    //    byte[] diagVector = this.DiagonalVector();

    //    byte result = 0;

    //    for (int i = 0; i < this.NumberOfRows; i++)
    //    {

    //        result += diagVector[i];

    //    }

    //    return result;

    //}

    //private byte CalculateSumOfElements()
    //{
    //    byte result = 0;

    //    for (int i = 0; i < this.NumberOfRows; i++)
    //    {
    //        for (int j = 0; j < this.NumberOfColumns; j++)
    //        {
    //            result += this[i, j];
    //        }
    //    }

    //    return result;
    //}

    public ImageMatrix Subtract(ImageMatrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new NotImplementedException();
        }

        ImageMatrix result = new ImageMatrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {

                result[i, j] = (byte)(this[i, j] - value[i, j]);

            }
        }

        return result;
    }

    public ImageMatrix Multiply(ImageMatrix value)
    {
        if (!CanBeMultiply(this, value))
        {
            throw new NotImplementedException();
        }

        int row1 = this.NumberOfRows;

        int column1 = this.NumberOfColumns;

        int column2 = value.NumberOfColumns;

        ImageMatrix result = new ImageMatrix(row1, column2);

        for (int i = 0; i < row1; i++)
        {
            for (int j = 0; j < column2; j++)
            {
                byte tempValue = 0;

                for (int k = 0; k < column1; k++)
                {
                    tempValue += (byte)(this[i, k] * value[k, j]);
                }
                result[i, j] = tempValue;
            }
        }
        return result;
    }

    public ImageMatrix DotMultiply(ImageMatrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new NotImplementedException();
        }

        ImageMatrix result = new ImageMatrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {

                result[i, j] = (byte)(this[i, j] * value[i, j]);

            }
        }
        return result;

    }

    public ImageMatrix Multiply(double scalar)
    {
        int row = this.NumberOfRows;

        int column = this.NumberOfColumns;

        ImageMatrix result = new ImageMatrix(row, column);

        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < column; j++)
            {

                result[i, j] = (byte)(scalar * this[i, j]);

            }
        }
        return result;
    }

    public ImageMatrix Add(ImageMatrix value)
    {
        if (!AreTheSameSize(this, value))
        {
            throw new NotImplementedException();
        }

        ImageMatrix result = new ImageMatrix(this.NumberOfRows, this.NumberOfColumns);

        for (int i = 0; i < this.NumberOfRows; i++)
        {
            for (int j = 0; j < this.NumberOfColumns; j++)
            {

                result[i, j] = (byte)(this[i, j] + value[i, j]);

            }
        }
        return result;
    }

    private byte GetValue(int rowNumber, int columNumber)
    {
        if (rowNumber > this.NumberOfRows || columNumber > this.NumberOfColumns ||
                rowNumber < 0 || columNumber < 0)
        {
            throw new NotImplementedException();
        }
        else
        {
            return Element[columNumber][rowNumber];
        }
    }

    private void SetValue(int rowNumber, int columNumber, byte value)
    {
        if (rowNumber > this.NumberOfRows || columNumber > this.NumberOfColumns ||
                rowNumber < 0 || columNumber < 0)
        {
            throw new NotImplementedException();
        }
        else
        {
            this.Element[columNumber][rowNumber] = value;
        }
    }
    
    #endregion


    #region StaticMembers

    //public static bool AreCommutative(ImageMatrix ImageMatrix1, ImageMatrix ImageMatrix2)
    //{

    //    if (!(ImageMatrix1.IsSquare() && ImageMatrix2.IsSquare()))
    //    {

    //        throw new NonSquareImageMatrixException();

    //    }

    //    return (ImageMatrix1 * ImageMatrix2 == ImageMatrix2 * ImageMatrix1);

    //}

    public static bool AreTheSameSize(ImageMatrix firstImageMatrix, ImageMatrix secondImageMatrix)
    {
        return (firstImageMatrix.NumberOfRows == secondImageMatrix.NumberOfRows &&
                firstImageMatrix.NumberOfColumns == secondImageMatrix.NumberOfColumns);
    }

    public static bool CanBeMultiply(ImageMatrix ImageMatrix1, ImageMatrix ImageMatrix2)
    {

        return (ImageMatrix1.NumberOfColumns == ImageMatrix2.NumberOfRows);

    }

    public static ImageMatrix Null()
    {
        return new ImageMatrix();
    }

    public static ImageMatrix Zeros(int size)
    {

        return new ImageMatrix(size, size);

    }

    public static ImageMatrix Zeros(int row, int column)
    {

        return new ImageMatrix(row, column);

    }

    public static ImageMatrix Ones(int size)
    {

        return ImageMatrix.Ones(size, size);

    }

    public static ImageMatrix Ones(int row, int column)
    {

        ImageMatrix resultImageMatrix = new ImageMatrix(row, column);

        byte[] values = new byte[column];

        for (int i = 0; i < column; i++)
        {

            values[i] = 1;

        }

        for (int i = 0; i < row; i++)
        {

            resultImageMatrix.SetRow(i, values);

        }

        return resultImageMatrix;

    }

    public static ImageMatrix DiagonalImageMatrix(byte[] values)
    {

        ImageMatrix resultImageMatrix = new ImageMatrix(values.Length);

        for (int i = 0; i < values.Length; i++)
        {

            resultImageMatrix[i, i] = values[i];

        }

        return resultImageMatrix;

    }

    public static ImageMatrix Identity(int size)
    {

        ImageMatrix resultImageMatrix = new ImageMatrix(size, size);

        for (int i = 0; i < size; i++)
        {

            resultImageMatrix[i, i] = 1;

        }

        return resultImageMatrix;

    }

    public static ImageMatrix ScalarImageMatrix(byte value, int size)
    {

        return value * (new ImageMatrix(size, size));

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

    public static ImageMatrix operator *(double scalar, ImageMatrix ImageMatrix)
    {
        return ImageMatrix.Multiply(scalar);
    }

    public static ImageMatrix operator *(ImageMatrix ImageMatrix1, ImageMatrix ImageMatrix2)
    {
        return ImageMatrix1.Multiply(ImageMatrix2);
    }

    public static ImageMatrix operator +(ImageMatrix ImageMatrix1, ImageMatrix ImageMatrix2)
    {
        return ImageMatrix1.Add(ImageMatrix2);
    }

    public static ImageMatrix operator -(ImageMatrix ImageMatrix1, ImageMatrix ImageMatrix2)
    {
        return ImageMatrix1.Subtract(ImageMatrix2);
    }

    public static bool operator ==(ImageMatrix ImageMatrix1, ImageMatrix ImageMatrix2)
    {
        int row1 = ImageMatrix1.NumberOfRows;

        int column1 = ImageMatrix1.NumberOfColumns;

        int row2 = ImageMatrix2.NumberOfRows;

        int column2 = ImageMatrix2.NumberOfColumns;

        if (column1 != column2 || row1 != row2)
        {
            return false;
        }

        for (int i = 0; i < row1; i++)
        {
            for (int j = 0; j < column2; j++)
            {
                if (ImageMatrix1[i, j] != ImageMatrix2[i, j])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool operator !=(ImageMatrix ImageMatrix1, ImageMatrix ImageMatrix2)
    {
        return !(ImageMatrix1 == ImageMatrix2);
    }

    #endregion

}

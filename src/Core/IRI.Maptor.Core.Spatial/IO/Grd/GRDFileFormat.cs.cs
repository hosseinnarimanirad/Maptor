namespace IRI.Maptor.Core.Spatial.IO;

public class GRDFileFormat
{
    public int NumberOfColumns;

    public int NumberOfRows;

    public double NoDataValue;

    public double LowerLeftX;

    public double LowerLeftY;

    public double CellSize;

    public string Path;

    public IRI.Maptor.Core.Common.Mathematics.Matrix Values;

    public GRDFileFormat(string path)
    {
        if (!File.Exists(path))
            throw new Exception("File does not exists!");

        StreamReader reader = new StreamReader(path);
        try
        {
            Path = path;

            NumberOfColumns = int.Parse(reader.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1]);

            NumberOfRows = int.Parse(reader.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1]);

            LowerLeftX = double.Parse(reader.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1]);

            LowerLeftY = double.Parse(reader.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1]);

            CellSize = double.Parse(reader.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1]);

            NoDataValue = double.Parse(reader.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)[1]);

            ReadToMatrix();
        }
        catch
        {
            throw new Exception("Error while opening grd file");
        }
        finally
        {
            reader.Close();
        }

    }

    private void ReadToMatrix()
    {

        StreamReader reader = new StreamReader(Path);

        Values = new IRI.Maptor.Core.Common.Mathematics.Matrix(NumberOfRows, NumberOfColumns);

        for (int i = 0; i < 6; i++)
            reader.ReadLine();

        for (int i = 0; i < NumberOfRows; i++)
        {
            string line = reader.ReadLine();

            string[] lineValues = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (lineValues.Length != NumberOfColumns)
                throw new Exception("File was corrupted");

            for (int j = 0; j < NumberOfColumns; j++)
            {
                double tempValue = double.Parse(lineValues[j]);

                if (tempValue == NoDataValue)
                    Values[i, j] = double.NaN;
                else
                    Values[i, j] = tempValue;
            }
        }

        reader.Close();
    }

    public double[] ReadToDouble()
    {
        StreamReader reader = new StreamReader(Path);

        double[] result = new double[NumberOfRows * NumberOfColumns];

        for (int i = 0; i < 6; i++)
            reader.ReadLine();

        for (int i = 6; i < NumberOfRows; i++)
        {
            string line = reader.ReadLine();

            string[] lineValues = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (lineValues.Length != NumberOfColumns)
                throw new Exception("File was corrupted");

            for (int j = 0; j < lineValues.Length; j++)
            {
                double tempValue = double.Parse(lineValues[j]);

                if (tempValue == NoDataValue)
                    result[(i - 6) * NumberOfColumns + j] = double.NaN;
                else
                    result[(i - 6) * NumberOfColumns + j] = tempValue;
            }

            GC.Collect();
        }

        reader.Close();

        return result;
    }

    public IRI.Maptor.Core.Common.Mathematics.Matrix ReadQuarter(QuarterPart part)
    {
        int midRow = NumberOfRows % 2 == 0 ? NumberOfRows / 2 - 1 : NumberOfRows / 2;

        int midColumn = NumberOfColumns % 2 == 0 ? NumberOfColumns / 2 - 1 : NumberOfColumns / 2;

        switch (part)
        {
            case QuarterPart.UpperLeft:
                return ReadRegion(0, 0, midRow, midColumn);

            case QuarterPart.UpperRight:
                return ReadRegion(0, midColumn + 1, midRow, NumberOfColumns - 1);

            case QuarterPart.LowerLeft:
                return ReadRegion(midRow + 1, 0, NumberOfRows - 1, midColumn);

            case QuarterPart.LowerRight:
                return ReadRegion(midRow + 1, midColumn + 1, NumberOfRows - 1, NumberOfColumns - 1);

            default:
                return null;
        }
    }

    public IRI.Maptor.Core.Common.Mathematics.Matrix ReadRegion(int startRow, int startColumn, int endRow, int endColumn)
    {
        return Values.SubMatrix(startRow, startColumn, endRow, endColumn);
    }

    public void SaveQuarter(string fileName, QuarterPart part)
    {
        int midRow = NumberOfRows % 2 == 0 ? NumberOfRows / 2 - 1 : NumberOfRows / 2;

        int midColumn = NumberOfColumns % 2 == 0 ? NumberOfColumns / 2 - 1 : NumberOfColumns / 2;

        IRI.Maptor.Core.Common.Mathematics.Matrix values;

        switch (part)
        {
            case QuarterPart.UpperLeft:

                values = ReadRegion(0, 0, midRow, midColumn);

                SaveAsGRD(fileName,
                            values,
                            0 + LowerLeftX,
                            (NumberOfRows - values.NumberOfRows) * CellSize + LowerLeftY,
                            CellSize,
                            NoDataValue);
                break;

            case QuarterPart.UpperRight:

                values = ReadRegion(0, midColumn + 1, midRow, NumberOfColumns - 1);

                SaveAsGRD(fileName,
                            values,
                            (NumberOfColumns - values.NumberOfColumns) * CellSize + LowerLeftX,
                            (NumberOfRows - values.NumberOfRows) * CellSize + LowerLeftY,
                            CellSize,
                            NoDataValue);

                break;

            case QuarterPart.LowerLeft:

                values = ReadRegion(midRow + 1, 0, NumberOfRows - 1, midColumn);


                SaveAsGRD(fileName,
                            values,
                            0 + LowerLeftX,
                            0 + LowerLeftY,
                            CellSize,
                            NoDataValue);

                break;

            case QuarterPart.LowerRight:

                values = ReadRegion(midRow + 1, midColumn + 1, NumberOfRows - 1, NumberOfColumns - 1);

                SaveAsGRD(fileName,
                            values,
                            (NumberOfColumns - values.NumberOfColumns) * CellSize + LowerLeftX,
                            0 + LowerLeftY,
                            CellSize,
                            NoDataValue);

                break;

            default:
                throw new NotImplementedException();
        }
    }

    public void SaveAsGRD(string path, double[,] values, double lowerLeftX, double lowerLeftY, double cellSize, double noDataValue)
    {
        StreamWriter writer = new StreamWriter(path);

        int numberOfColumns = values.GetLength(1);

        int numberOfRows = values.GetLength(0);

        writer.WriteLine(string.Format("ncols         {0}", numberOfColumns));

        writer.WriteLine(string.Format("nrows         {0}", numberOfRows));

        writer.WriteLine(string.Format("xllcorner     {0}", lowerLeftX));

        writer.WriteLine(string.Format("yllcorner     {0}", lowerLeftY));

        writer.WriteLine(string.Format("cellsize      {0}", cellSize));

        writer.WriteLine(string.Format("NODATA_value {0}", noDataValue));

        for (int i = 0; i < numberOfRows; i++)
        {
            for (int j = 0; j < numberOfColumns; j++)
            {
                writer.Write(string.Format("{0} ", values[i, j]));
            }

            writer.WriteLine();
        }

        writer.Close();
    }

    public void SaveAsGRD(string path, IRI.Maptor.Core.Common.Mathematics.Matrix values, double lowerLeftX, double lowerLeftY, double cellSize, double noDataValue)
    {
        StreamWriter writer = new StreamWriter(path);

        int numberOfColumns = values.NumberOfColumns;

        int numberOfRows = values.NumberOfRows;

        writer.WriteLine(string.Format("ncols         {0}", numberOfColumns));

        writer.WriteLine(string.Format("nrows         {0}", numberOfRows));

        writer.WriteLine(string.Format("xllcorner     {0}", lowerLeftX));

        writer.WriteLine(string.Format("yllcorner     {0}", lowerLeftY));

        writer.WriteLine(string.Format("cellsize      {0}", cellSize));

        writer.WriteLine(string.Format("NODATA_value {0}", noDataValue));

        for (int i = 0; i < numberOfRows; i++)
        {
            for (int j = 0; j < numberOfColumns; j++)
            {
                writer.Write(string.Format("{0} ", values[i, j]));
            }

            writer.WriteLine();
        }

        writer.Close();
    }

}
using System;
using System.Linq; 
using System.Collections.Generic;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace IRI.Maptor.Jab.Common.OfficeFormats;

public static class ExcelHelper
{
    public static void Write<T>(List<T> rows, List<string> headers, List<Func<T, string>> mapFuncs, string outputFileName, string sheetName)
    {
        var defaultTypes = headers.Select(h => CellValues.String).ToList();

        Write(rows, headers, defaultTypes, mapFuncs, outputFileName, sheetName);

        //if (headers.Count != mapFuncs.Count)
        //{
        //    throw new NotImplementedException();
        //}

        //using (SpreadsheetDocument document = SpreadsheetDocument.Create(outputFileName, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        //{
        //    WorkbookPart workbookPart = document.AddWorkbookPart();
        //    workbookPart.Workbook = new Workbook();

        //    WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        //    worksheetPart.Worksheet = new Worksheet();

        //    Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());

        //    Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = sheetName };

        //    sheets.Append(sheet);

        //    workbookPart.Workbook.Save();

        //    SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());

        //    // Constructing header
        //    Row row = new Row();

        //    List<Cell> headerCells = new List<Cell>();

        //    for (int i = 0; i < headers.Count; i++)
        //    {
        //        headerCells.Add(ConstructCell(headers[i], CellValues.String));
        //    }

        //    row.Append(headerCells);

        //    // Insert the header row to the Sheet Data
        //    sheetData.AppendChild(row);

        //    try
        //    {
        //        //var karevan = karevanha.Single(i => i.Id == item.KarevanId);
        //        foreach (var item in rows)
        //        {

        //            List<Cell> cells = new List<Cell>();

        //            for (int i = 0; i < headers.Count; i++)
        //            {
        //                cells.Add(ConstructCell(mapFuncs[i](item), CellValues.String));
        //            }

        //            //Header
        //            //Row temporaryRow = new Row(cells);

        //            //temporaryRow.Append(cells);

        //            //sheetData.AppendChild(temp);
        //            sheetData.AppendChild(new Row(cells));
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //    worksheetPart.Worksheet.Save();
        //}
    }

    public static void Write<T>(List<T> rows, List<string> headers, List<CellValues> types, List<Func<T, string>> mapFuncs, string outputFileName, string sheetName)
    {
        if (headers.Count != mapFuncs.Count || headers.Count != types.Count)
            throw new NotImplementedException();

        using (SpreadsheetDocument document = SpreadsheetDocument.Create(outputFileName, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet();

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());

            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = sheetName };

            sheets.Append(sheet);

            workbookPart.Workbook.Save();

            SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());

            // Constructing header
            Row row = new Row();

            List<Cell> headerCells = new List<Cell>();

            for (int i = 0; i < headers.Count; i++)
            {
                headerCells.Add(ConstructCell(headers[i], types[i]));
            }

            row.Append(headerCells);

            // Insert the header row to the Sheet Data
            sheetData.AppendChild(row);

            try
            {
                //var karevan = karevanha.Single(i => i.Id == item.KarevanId);
                foreach (var item in rows)
                {

                    List<Cell> cells = new List<Cell>();

                    for (int i = 0; i < headers.Count; i++)
                    {
                        cells.Add(ConstructCell(mapFuncs[i](item), types[i]));
                    }

                    //Header
                    //Row temporaryRow = new Row(cells);

                    //temporaryRow.Append(cells);

                    //sheetData.AppendChild(temp);
                    sheetData.AppendChild(new Row(cells));
                }
            }
            catch (Exception ex)
            {

            }

            worksheetPart.Worksheet.Save();
        }
    }

    public static void WriteDictionary(List<Dictionary<string, object>> rows, string outputFileName, string sheetName, List<CellValues> types = null, List<string> headers = null)
    {
        if (rows is null || rows.Count == 0)
            return;

        var numberOfColumns = rows.First().Keys.Count;

        types = types == null ? Enumerable.Range(0, numberOfColumns).Select(k => CellValues.String).ToList() : types;

        headers = headers == null ? rows.First().Select(r => r.Key).ToList() : headers;

        //if (  headers.Count != types.Count)
        //    throw new NotImplementedException();

        using (SpreadsheetDocument document = SpreadsheetDocument.Create(outputFileName, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet();

            Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());

            Sheet sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = sheetName };

            sheets.Append(sheet);

            workbookPart.Workbook.Save();

            SheetData sheetData = worksheetPart.Worksheet.AppendChild(new SheetData());

            // Constructing header
            Row row = new Row();

            List<Cell> headerCells = new List<Cell>();

            for (int i = 0; i < headers.Count; i++)
            {
                headerCells.Add(ConstructCell(headers[i], types[i]));
            }

            row.Append(headerCells);

            // Insert the header row to the Sheet Data
            sheetData.AppendChild(row);

            try
            {
                //var karevan = karevanha.Single(i => i.Id == item.KarevanId);
                foreach (var item in rows)
                {

                    List<Cell> cells = new List<Cell>();

                    for (int i = 0; i < headers.Count; i++)
                    {
                        cells.Add(ConstructCell(item.ElementAt(i).Value?.ToString(), types[i]));
                    }

                    //Header
                    //Row temporaryRow = new Row(cells);

                    //temporaryRow.Append(cells);

                    //sheetData.AppendChild(temp);
                    sheetData.AppendChild(new Row(cells));
                }
            }
            catch (Exception ex)
            {

            }

            worksheetPart.Worksheet.Save();
        }

    }


    #region Helper Methods

    ///<summary>returns an empty cell when a blank cell is encountered
    ///</summary>
    public static IEnumerable<Cell> GetRowCells(Row row)
    {
        int currentCount = 0;

        foreach (Cell cell in
            row.Descendants<Cell>())
        {
            string columnName = GetColumnName(cell.CellReference);

            int currentColumnIndex = ConvertColumnNameToNumber(columnName);

            for (; currentCount < currentColumnIndex; currentCount++)
            {
                yield return new Cell();
            }

            yield return cell;
            currentCount++;
        }
    }

    /// <summary>
    /// Given a cell name, parses the specified cell to get the column name.
    /// </summary>
    /// <param name="cellReference">Address of the cell (ie. B2)</param>
    /// <returns>Column Name (ie. B)</returns>
    public static string GetColumnName(string cellReference)
    {
        // Match the column name portion of the cell name.
        var regex = new System.Text.RegularExpressions.Regex("[A-Za-z]+");
        var match = regex.Match(cellReference);

        return match.Value;
    }

    /// <summary>
    /// Given just the column name (no row index),
    /// it will return the zero based column index.
    /// </summary>
    /// <param name="columnName">Column Name (ie. A or AB)</param>
    /// <returns>Zero based index if the conversion was successful</returns>
    /// <exception cref="ArgumentException">thrown if the given string
    /// contains characters other than uppercase letters</exception>
    public static int ConvertColumnNameToNumber(string columnName)
    {
        var alpha = new System.Text.RegularExpressions.Regex("^[A-Z]+$");
        if (!alpha.IsMatch(columnName)) throw new ArgumentException();

        char[] colLetters = columnName.ToCharArray();
        Array.Reverse(colLetters);

        int convertedValue = 0;
        for (int i = 0; i < colLetters.Length; i++)
        {
            char letter = colLetters[i];
            int current = i == 0 ? letter - 65 : letter - 64; // ASCII 'A' = 65
            convertedValue += current * (int)Math.Pow(26, i);
        }

        return convertedValue;
    }

    public static Cell ConstructCell(string value, CellValues dataType)
    {
        return new Cell()
        {
            CellValue = new CellValue(value),
            DataType = new DocumentFormat.OpenXml.EnumValue<CellValues>(dataType)
        };
    }

    #endregion
}

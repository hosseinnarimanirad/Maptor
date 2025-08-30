using System.IO;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace IRI.Maptor.Jab.Common.OfficeFormats;

public static class WordHelper
{
    public static Text GetCellText(Table table, int row, int column)
    {
        return table.Elements<TableRow>().ElementAt(row).Elements<TableCell>().ElementAt(column).Elements<Paragraph>().First().Elements<Run>().First().Elements<Text>().First();
    }

    public static void SetCellText(Table table, int row, int column, string text)
    {
        table.Elements<TableRow>().ElementAt(row).Elements<TableCell>().ElementAt(column).Elements<Paragraph>().First().Elements<Run>().First().Elements<Text>().First().Text = text;
    }

    public static Run CreateNewLine()
    {
        Run result = new Run();

        result.AppendChild(new Break());

        return result;
    }

    public static Run CreateRun(string value, bool isBold = false)
    {
        Run result = new Run();

        result.AppendChild(new Text()
        {
            Text = value ?? string.Empty,
            Space = SpaceProcessingModeValues.Preserve
        });

        if (isBold)
        {
            result.RunProperties = new RunProperties() { Bold = new Bold() };
        }

        return result;
    }

    public static Run CreateRunWithNewLine(string value, bool isBold = false)
    {
        Run result = CreateRun(value, isBold);

        result.AppendChild(new Break());

        return result;
    }

    public static Paragraph CreateRtlParagraph()
    {
        Paragraph paragraph = new Paragraph();

        paragraph.ParagraphProperties = new ParagraphProperties()
        {
            BiDi = new BiDi(),
            TextDirection = new TextDirection() { Val = TextDirectionValues.TopToBottomRightToLeft }
        };

        return paragraph;
    }



    #region SAMPLE
    public static void SaveAsDocx2(object value)
    {

        using (MemoryStream mem = new MemoryStream())
        {
            // Create Document
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(mem,
            WordprocessingDocumentType.Document, true))
            {
                // Add a main document part. 
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                // Create the document structure and add some text.
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());
                Paragraph para = body.AppendChild(CreateRtlParagraph());

                //para.AppendChild(CreateRun("نام:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.Nam} "));

                //para.AppendChild(CreateRun("نام خانوادگی:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.NamKhanevadegi} "));

                //para.AppendChild(CreateRun("کد ملی:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.KodeMelli?.ToString() ?? string.Empty} "));

                //para.AppendChild(CreateRun("نام پدر:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.NamPedar} "));

                //para.AppendChild(CreateRun("نام مادر:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.NamMadar} "));

                //para.AppendChild(CreateRun("شماره گذرنامه:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.ShomarePassport} "));

                //para.AppendChild(CreateRun("اصالت:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.Melliyat?.Title} "));

                //para.AppendChild(CreateRun("تاهل:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.Taahol} "));

                //para.AppendChild(CreateRun("همسر:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.NamHamsar} "));

                //para.AppendChild(CreateRun("فرزندان:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.Farzandan} "));

                //para.AppendChild(CreateRun("کشور محل اقامت:", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.KeshvarEghamat?.Title} "));

                //para.AppendChild(CreateRun("شهر محل اقامت: ", true));
                //para.AppendChild(CreateRunWithNewLine($" {person.City} "));

                mainPart.Document.Save();
            }

            //System.IO.File.WriteAllBytes(file, mem.ToArray());
        }
    }

    public static void Save()
    {
        using (MemoryStream mem = new MemoryStream())
        {
            // Create Document
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(mem,
            WordprocessingDocumentType.Document, true))
            {
                // Add a main document part. 
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                // Create the document structure and add some text.
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());
                Paragraph para = body.AppendChild(new Paragraph());
                Run run = para.AppendChild(new Run());
                run.AppendChild(new Text("sample text"));
                mainPart.Document.Save();
            }
        }



    }

    public static void CreateTable(string fileName)
    {
        // Use the file name and path passed in as an argument 
        // to open an existing Word 2007 document.
        using (WordprocessingDocument doc = WordprocessingDocument.Open(fileName, true))
        {
            // Create an empty table.
            Table table = new Table();

            // Create a TableProperties object and specify its border information.
            TableProperties tblProp = new TableProperties(
                new TableBorders(
                    new TopBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Dashed),
                        Size = 24
                    },
                    new BottomBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Dashed),
                        Size = 24
                    },
                    new LeftBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Dashed),
                        Size = 24
                    },
                    new RightBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Dashed),
                        Size = 24
                    },
                    new InsideHorizontalBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Dashed),
                        Size = 24
                    },
                    new InsideVerticalBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Dashed),
                        Size = 24
                    }
                )
            );

            // Append the TableProperties object to the empty table.
            table.AppendChild(tblProp);

            // Create a row.
            TableRow tr = new TableRow();

            // Create a cell.
            TableCell tc1 = new TableCell();

            // Specify the width property of the table cell.
            tc1.Append(new TableCellProperties(
                new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "2400" }));

            // Specify the table cell content.
            tc1.Append(new Paragraph(new Run(new Text("some text"))));

            // Append the table cell to the table row.
            tr.Append(tc1);

            // Create a second table cell by copying the OuterXml value of the first table cell.
            TableCell tc2 = new TableCell(tc1.OuterXml);

            // Append the table cell to the table row.
            tr.Append(tc2);

            // Append the table row to the table.
            table.Append(tr);

            // Append the table to the document.
            doc.MainDocumentPart.Document.Body.Append(table);
        }
    }
    #endregion
}

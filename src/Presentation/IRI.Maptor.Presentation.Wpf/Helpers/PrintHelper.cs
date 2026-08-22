using System;
using System.Windows;
using System.Printing;
using System.Windows.Media;
using System.Windows.Controls;

namespace IRI.Maptor.Presentation.Wpf.Helpers;

public static class PrintHelper
{
    public static void Print1(FrameworkElement visual, bool applyRtl = true)
    {
        var printDlg = new System.Windows.Controls.PrintDialog();

        if (printDlg.ShowDialog() == true)
        {
            //get selected printer capabilities
            System.Printing.PrintCapabilities capabilities = printDlg.PrintQueue.GetPrintCapabilities(printDlg.PrintTicket);

            //get scale of the print wrt to screen of WPF visual
            double scale = Math.Min(capabilities.PageImageableArea.ExtentWidth / visual.ActualWidth, capabilities.PageImageableArea.ExtentHeight / visual.ActualHeight);

            var oldTransform = visual.LayoutTransform?.Clone();

            if (applyRtl)
            {
                //Transform the Visual to scale
                visual.LayoutTransform = new ScaleTransform(-scale, scale);
            }
            else
            {
                //Transform the Visual to scale
                visual.LayoutTransform = new ScaleTransform(scale, scale);
            }

            //get the size of the printer page
            Size size = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);

            //update the layout of the visual to the printer page size.
            visual.Measure(size);

            visual.Arrange(new Rect(new Point(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight), size));

            //now print the visual to printer to fit on the one page.
            printDlg.PrintVisual(visual, "A1");

            visual.LayoutTransform = oldTransform;
        }
    }

    public static void Print(FrameworkElement visual, bool applyRtl = true)
    {
        var printDlg = new PrintDialog();

        if (printDlg.ShowDialog() == true)
        {
            //get selected printer capabilities
            PrintCapabilities capabilities = printDlg.PrintQueue.GetPrintCapabilities(printDlg.PrintTicket);

            double scale = Math.Min(capabilities.PageImageableArea.ExtentWidth / visual.ActualWidth, capabilities.PageImageableArea.ExtentHeight / visual.ActualHeight);

            var drawingVisual = BuildGraphVisual(new PageMediaSize(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight), visual, scale, applyRtl);

            //now print the visual to printer to fit on the one page.
            printDlg.PrintVisual(drawingVisual, "A1");
        }
    }

    private static DrawingVisual BuildGraphVisual(PageMediaSize pageSize, Visual visual, double scale, bool applyRtl = true)
    {
        var drawingVisual = new DrawingVisual();

        //drawingVisual.Transform = new ScaleTransform(-1, 1);
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            var visualContent = visual;

            var rect = new Rect
            {
                X = 0,
                Y = 0,
                Width = pageSize.Width.Value,
                Height = pageSize.Height.Value
            };

            var stretch = Stretch.Uniform;
            var visualBrush = new VisualBrush(visualContent) { Stretch = stretch };

            if (applyRtl)
            {
                TransformGroup group = new TransformGroup();
                group.Children.Add(new ScaleTransform(-1, 1)); 
                group.Children.Add(new TranslateTransform(rect.Width, 0));
                visualBrush.Transform = group;
            }

            drawingContext.DrawRectangle(visualBrush, null, rect);
            drawingContext.PushOpacityMask(Brushes.White);
        }
        return drawingVisual;
    }

    //public static void PrintLargeObject(FrameworkElement visual, bool applyRtl = true)
    //{
    //    Print(GetLargeObject(visual));
    //}

    //private static FrameworkElement GetLargeObject(FrameworkElement element)
    //{
    //    FlowDocumentScrollViewer flowDocumentScrollViewer = new FlowDocumentScrollViewer();

    //    FlowDocument doc = new FlowDocument();

    //    doc.Blocks.Add(new Section(new BlockUIContainer(XamlClone(element))));

    //    flowDocumentScrollViewer.Document = doc;

    //    return flowDocumentScrollViewer;
    //}

    //public static UIElement XamlClone(UIElement original) 
    //{
    //    if (original == null)
    //        return (null);

    //    string s = XamlWriter.Save(original);
    //    StringReader stringReader = new StringReader(s);
    //    XmlReader xmlReader = XmlTextReader.Create(stringReader, new XmlReaderSettings());
    //    return (UIElement)XamlReader.Load(xmlReader);

    //}
}

using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace IRI.Maptor.Jab.Wpf.Helpers;

public class RichTextBoxHelper : DependencyObject
{
    public static FlowDocument GetDocumentXaml(DependencyObject obj)
    {
        return (FlowDocument)obj.GetValue(DocumentXamlProperty);
    }

    public static void SetDocumentXaml(DependencyObject obj, FlowDocument value)
    {
        obj.SetValue(DocumentXamlProperty, value);
    }

    public static readonly DependencyProperty DocumentXamlProperty =
      DependencyProperty.RegisterAttached(
        "DocumentXaml",
        typeof(FlowDocument),
        typeof(RichTextBoxHelper),
        new FrameworkPropertyMetadata
        {
            BindsTwoWayByDefault = true,
            PropertyChangedCallback = (obj, e) =>
            {
                var richTextBox = (RichTextBox)obj;

                var xaml = GetDocumentXaml(richTextBox);

                //FlowDocument doc = new FlowDocument();

                //var range = new TextRange(doc.ContentStart, doc.ContentEnd);

                //if (!string.IsNullOrEmpty(xaml))
                //{
                //    range.Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)), DataFormats.Xaml);
                //}

                if (richTextBox.Document != xaml && xaml != null)
                {
                    richTextBox.Document = xaml;
                }

                richTextBox.LostFocus -= RichTextBox_LostFocus;
                richTextBox.LostFocus += RichTextBox_LostFocus;

                #region Original Code
                //var richTextBox = (RichTextBox)obj;

                ////Parse the XAML to a document (or use XamlReader.Parse())
                //var xaml = GetDocumentXaml(richTextBox);
                //var doc = new FlowDocument();
                //var range = new TextRange(doc.ContentStart, doc.ContentEnd);

                //range.Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)),
                //DataFormats.Xaml);

                //// Set the document
                //richTextBox.Document = doc;

                //// When the document changes update the source
                //range.Changed += (obj2, e2) =>
                //  {
                //      if (richTextBox.Document == doc)
                //      {
                //          MemoryStream buffer = new MemoryStream();
                //          range.Save(buffer, DataFormats.Xaml);
                //          SetDocumentXaml(richTextBox,
                //      Encoding.UTF8.GetString(buffer.ToArray()));
                //      }
                //  };
                #endregion

            }
        });


    private static void RichTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var richTextBox = sender as RichTextBox;

        var flowDocument = richTextBox.Document;

        SetDocumentXaml(richTextBox, flowDocument);
    }

    //private static void RichTextBox_LostFocus(object sender, RoutedEventArgs e)
    //{
    //    var richTextBox = sender as RichTextBox;

    //    var flowDocument = richTextBox.Document;

    //    var range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);

    //    string result = string.Empty;

    //    using (MemoryStream buffer = new MemoryStream())
    //    {
    //        range.Save(buffer, DataFormats.Xaml);

    //        result = Encoding.UTF8.GetString(buffer.ToArray());
    //    }

    //    SetDocumentXaml(richTextBox, result);
    //}
     
    //
     
    //public static string GetDocumentXamlString(DependencyObject obj)
    //{
    //    return (string)obj.GetValue(DocumentXamlStringProperty);
    //}

    //public static void SetDocumentXamlString(DependencyObject obj, string value)
    //{
    //    obj.SetValue(DocumentXamlStringProperty, value);
    //}

    //// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    //public static readonly DependencyProperty DocumentXamlStringProperty =
    //    DependencyProperty.Register("DocumentXamlString", typeof(string), typeof(RichTextBoxHelper), new FrameworkPropertyMetadata
    //    {
    //        BindsTwoWayByDefault = true,
    //        PropertyChangedCallback = (obj, e) =>
    //        {
    //            var richTextBox = (RichTextBox)obj;

    //            var xaml = GetDocumentXamlString(richTextBox);

    //            FlowDocument doc = new FlowDocument();

    //            var range = new TextRange(doc.ContentStart, doc.ContentEnd);

    //            if (!string.IsNullOrEmpty(xaml))
    //            {
    //                range.Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)), DataFormats.Xaml);
    //            }

    //            richTextBox.Document = doc;

    //            richTextBox.LostFocus -= RichTextBox_LostFocus1;
    //            richTextBox.LostFocus += RichTextBox_LostFocus1; ;
    //        }
    //    });

    //private static void RichTextBox_LostFocus1(object sender, RoutedEventArgs e)
    //{
    //    throw new System.NotImplementedException();
    //}
}
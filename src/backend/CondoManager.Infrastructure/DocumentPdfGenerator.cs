using CondoManager.Application;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;
namespace CondoManager.Infrastructure;

public sealed class DocumentPdfGenerator : IDocumentPdfGenerator
{
    // Embedded OFL font: no dependency on installed Windows fonts or network at runtime.
    static readonly byte[] Font=ReadFont();
    static byte[] ReadFont(){using var s=typeof(DocumentPdfGenerator).Assembly.GetManifestResourceStream("CondoManager.Infrastructure.Fonts.NotoSans-Regular.ttf")!;using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
    public byte[] Generate(string title,IReadOnlyList<string> paragraphs,bool draft)
    {
        var builder=new PdfDocumentBuilder();var font=builder.AddTrueTypeFont(Font);
        var pages=new List<PdfPageBuilder>();PdfPageBuilder? page=null;int y=0;
        void NewPage(){page=builder.AddPage(595,842);pages.Add(page);page.AddText(draft?"BOZZA - NON DEFINITIVO":"CondoManager - documento non firmato",9,new PdfPoint(42,805),font);y=777;}
        // Fit by actual font metrics, splitting even unbroken long tokens. No silently truncated lines.
        void Paragraph(string text,int size)
        {
            foreach(var raw in text.Replace("\r","").Split('\n'))
            {
                var rest=raw.Replace('\t',' ');if(rest.Length==0){if(page==null||y<54)NewPage();y-=14;continue;}
                while(rest.Length>0)
                {
                    if(page==null||y<54)NewPage();var length=Math.Min(rest.Length,150);
                    while(length>1){var measured=page!.MeasureText(rest[..length],size,new PdfPoint(42,y),font);if(measured.Max(l=>l.BoundingBox.Right)-measured.Min(l=>l.BoundingBox.Left)<=510)break;length--;}
                    if(length<rest.Length){var space=rest.LastIndexOf(' ',length-1,length);if(space>length/2)length=space;}
                    page!.AddText(rest[..length],size,new PdfPoint(42,y),font);rest=rest[length..].TrimStart();y-=size+5;
                }
                y-=5;
            }
        }
        Paragraph(title,14);foreach(var p in paragraphs)Paragraph(p,10);
        for(var n=0;n<pages.Count;n++)pages[n].AddText($"Pagina {n+1} / {pages.Count} | {(draft?"BOZZA":"Copia applicativa non firmata")}",8,new PdfPoint(42,28),font);
        var bytes=builder.Build();using var check=PdfDocument.Open(bytes);if(check.NumberOfPages!=pages.Count)throw new InvalidOperationException("PDF non verificabile.");return bytes;
    }
}



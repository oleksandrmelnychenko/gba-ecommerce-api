using System.IO;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.XWPF.UserModel;

namespace GBA.Domain.Helpers.WordHelpers;

public sealed class WordDocument {
    public WordDocument() {
        Document = new XWPFDocument();
        Document.CreateStyles();
    }

    public XWPFDocument Document { get; }

    public void SetDefaultStyle(XWPFStyle style) {
        StyleBuilder.DefaultStyleID = style.StyleId;
        style.GetCTStyle().basedOn = null;
        Document.GetStyles().AddStyle(style);
    }

    public void AddStyle(XWPFStyle style) {
        Document.GetStyles().AddStyle(style);
    }

    public WordParagraph AddParagraph(ParagraphAlignment alignment = ParagraphAlignment.BOTH) {
        XWPFParagraph paragraph = Document.CreateParagraph();
        paragraph.Alignment = alignment;
        paragraph.CreateRun();

        return new WordParagraph(paragraph);
    }

    public void SetDocumentMargin(uint left, int top, uint right, int bottom) {
        Document.Document.body.sectPr = new CT_SectPr();

        Document.Document.body.sectPr.pgMar.right = right;
        Document.Document.body.sectPr.pgMar.left = left;
        Document.Document.body.sectPr.pgMar.top = (ulong)top;
        Document.Document.body.sectPr.pgMar.bottom = (ulong)bottom;
    }

    public WordList CreateList() {
        return new WordList(this);
    }

    public WordTable CreateTable(int rows, int columns) {
        return new WordTable(Document.CreateTable(rows, columns));
    }

    public void Write(Stream stream) {
        Document.Write(stream);
    }
}
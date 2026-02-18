using NPOI.XWPF.UserModel;

namespace GBA.Domain.Helpers.WordHelpers;

public sealed class WordParagraph {
    public WordParagraph(XWPFParagraph paragraph) {
        Paragraph = paragraph;
        Run = paragraph.CreateRun();
    }

    public XWPFParagraph Paragraph { get; }
    public XWPFRun Run { get; }

    public WordParagraph AppendText(params string[] lines) {
        Run.AppendText(lines);
        return this;
    }

    public WordParagraph SetText(string text) {
        Run.SetText(text);
        return this;
    }


    public WordParagraph SetParagraphAlignment(ParagraphAlignment paragraphAlignment) {
        Paragraph.Alignment = paragraphAlignment;
        return this;
    }

    public WordParagraph AddCarriageReturn() {
        Run.AddCarriageReturn();
        return this;
    }

    /// <summary>
    /// Add new line in table cell
    /// </summary>
    public WordParagraph AddBreak() {
        Run.AddBreak(BreakType.TEXTWRAPPING);
        return this;
    }

    /// <summary>
    /// Apply styles with id styleId
    /// </summary>
    /// <param name="styleId"></param>
    public WordParagraph SetStyleId(string styleId) {
        Paragraph.Style = styleId;
        return this;
    }

    public void SetNumberingId(string numId, string lvl) {
        Paragraph.SetNumID(numId, lvl);
    }
}
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.XWPF.UserModel;

namespace GBA.Domain.Helpers.WordHelpers;

public sealed class WordTable {
    public WordTable(XWPFTable table) {
        Table = table;
    }

    public XWPFTable Table { get; }
    public WordParagraph CurrentParagraph { get; private set; }
    public XWPFTableCell CurrentCell { get; private set; }

    public XWPFTableCell GetCell(int row, int column) {
        return Table.GetRow(row).GetCell(column);
    }

    public WordTable BuildCell(XWPFTableCell cell) {
        CurrentCell = cell;
        CurrentCell.RemoveParagraph(0);
        CurrentParagraph = new WordParagraph(CurrentCell.AddParagraph());

        return this;
    }

    public WordTable SetCurrentCellWidth(string widthInPercentage) {
        CT_TcPr cellProperties = CurrentCell.GetCTTc().AddNewTcPr();
        CT_TblWidth cellTcWidth = cellProperties.AddNewTcW();

        cellTcWidth.type = ST_TblWidth.pct;
        cellTcWidth.w = widthInPercentage;

        return this;
    }

    /// <summary>
    /// Set vertical alignment for the current cell
    /// </summary>
    /// <param name="alignment"></param>
    public WordTable SetVerticalAlignment(XWPFTableCell.XWPFVertAlign alignment) {
        CurrentCell.SetVerticalAlignment(alignment);
        return this;
    }

    public void RemoveBorders() {
        Table.SetInsideHBorder(XWPFTable.XWPFBorderType.NONE, 0, 0, "");
        Table.SetInsideVBorder(XWPFTable.XWPFBorderType.NONE, 0, 0, "");
        Table.SetLeftBorder(XWPFTable.XWPFBorderType.NONE, 0, 0, "");
        Table.SetRightBorder(XWPFTable.XWPFBorderType.NONE, 0, 0, "");
        Table.SetTopBorder(XWPFTable.XWPFBorderType.NONE, 0, 0, "");
        Table.SetBottomBorder(XWPFTable.XWPFBorderType.NONE, 0, 0, "");
    }

    /// <summary>
    /// Set style for every cell
    /// </summary>
    /// <param name="styleId"></param>
    public void SetCellsStyle(string styleId) {
        foreach (XWPFTableRow row in Table.Rows)
        foreach (XWPFTableCell cell in row.GetTableCells())
        foreach (XWPFParagraph cellParagraph in cell.Paragraphs)
            cellParagraph.Style = styleId;
    }
}
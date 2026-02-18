using System.Collections.Generic;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.XWPF.UserModel;

namespace GBA.Domain.Helpers.WordHelpers;

public sealed class WordList {
    private readonly WordDocument _wordDocument;
    private string _numberingId;

    public WordList(WordDocument wordDocument) {
        _wordDocument = wordDocument;
        _numberingId = GetDefaultNumberingId(wordDocument.Document);
    }

    public void SetNewNumbering(string numberingId) {
        _numberingId = numberingId;
    }

    public WordParagraph AddListItem(string lvl) {
        WordParagraph paragraph = _wordDocument.AddParagraph();
        paragraph.SetNumberingId(_numberingId, lvl);

        return paragraph;
    }

    private static string GetDefaultNumberingId(XWPFDocument document) {
        XWPFNumbering numbering = document.CreateNumbering();

        CT_AbstractNum ct_abn = new();
        CT_MultiLevelType mlt = new();
        mlt.val = ST_MultiLevelType.multilevel;
        ct_abn.multiLevelType = mlt;
        ct_abn.lvl = new List<CT_Lvl> {
            new() {
                ilvl = "0", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1." }, lvlJc = new CT_Jc { val = ST_Jc.left }, // TODO try to justify this
                rPr = new CT_RPr { b = new CT_OnOff { val = true } },
                pPr = new CT_PPr { ind = new CT_Ind { left = "360", hanging = 360 } }
            },
            new() {
                ilvl = "1", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1.%2." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { firstLineChars = "360" } }
            },
            new() {
                ilvl = "2", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1.%2.%3." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { firstLineChars = "360" } }
            },
            new() {
                ilvl = "3", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.none },
                lvlText = new CT_LevelText { val = "-" }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { firstLineChars = "360" } }
            },
            new() {
                ilvl = "4", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1. %2. %3. %4. %5." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { left = "2232", hanging = 792 } }
            },
            new() {
                ilvl = "5", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1. %2. %3. %4. %5. %6." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { left = "2736", hanging = 936 } }
            },
            new() {
                ilvl = "6", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1. %2. %3. %4. %5. %6. %7." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { left = "3240", hanging = 1080 } }
            },
            new() {
                ilvl = "7", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1. %2. %3. %4. %5. %6. %7. %8." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { left = "3744", hanging = 1224 } }
            },
            new() {
                ilvl = "8", start = new CT_DecimalNumber { val = "1" }, numFmt = new CT_NumFmt { val = ST_NumberFormat.@decimal },
                lvlText = new CT_LevelText { val = "%1. %2. %3. %4. %5. %6. %7. %8. %9." }, lvlJc = new CT_Jc { val = ST_Jc.left },
                pPr = new CT_PPr { ind = new CT_Ind { left = "4320", hanging = 1440 } }
            }
        };

        string abstractNumId = numbering.AddAbstractNum(new XWPFAbstractNum(ct_abn));
        string numId = numbering.AddNum(abstractNumId);

        return numId;
    }
}
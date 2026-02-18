using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.XWPF.UserModel;

namespace GBA.Domain.Helpers.WordHelpers;

public sealed class StyleBuilder {
    private readonly CT_Style _style;

    public StyleBuilder() {
        _style = new CT_Style {
            pPr = new CT_PPr(),
            rPr = new CT_RPr()
        };

        SetParentStyleId(DefaultStyleID);
    }

    public static string DefaultStyleID { get; set; } = "DEFAULT_STYLE_ID";

    public StyleBuilder SetFont(string font) {
        _style.rPr.rFonts = new CT_Fonts {
            ascii = font,
            cs = font,
            eastAsia = font,
            hAnsi = font,
            hint = ST_Hint.@default
        };
        return this;
    }

    public StyleBuilder SetFontSize(uint fontSize) {
        _style.rPr.sz = new CT_HpsMeasure { val = fontSize * 2 };

        return this;
    }

    public StyleBuilder SetIsDefault(bool isDefault) {
        _style.@default = isDefault ? ST_OnOff.True : ST_OnOff.False;
        return this;
    }

    public StyleBuilder IsBold(bool isBold) {
        _style.rPr.b = new CT_OnOff { val = isBold };

        return this;
    }

    public StyleBuilder IsItalic(bool IsItalic) {
        _style.rPr.i = new CT_OnOff { val = IsItalic };

        return this;
    }

    public StyleBuilder SetUnderline(ST_Underline underlineType) {
        _style.rPr.u = new CT_Underline {
            val = underlineType
        };

        return this;
    }

    /// <summary>
    /// Spacing between lines of paragraph
    /// </summary>
    /// <param name="spacing"></param>
    public StyleBuilder SetLineSpacing(int spacing) {
        if (_style.pPr.spacing != null)
            _style.pPr.spacing.line = spacing.ToString();
        else
            _style.pPr.spacing = new CT_Spacing { line = spacing.ToString() };

        return this;
    }

    /// <summary>
    /// Space after paragraph
    /// </summary>
    /// <param name="spacing"></param>
    public StyleBuilder SetSpaceAfter(uint spacing) {
        if (_style.pPr.spacing != null)
            _style.pPr.spacing.after = spacing;
        else
            _style.pPr.spacing = new CT_Spacing { after = spacing };

        return this;
    }

    /// <summary>
    /// Space before paragraph
    /// </summary>
    /// <param name="spacing"></param>
    public StyleBuilder SetSpaceBefore(uint spacing) {
        if (_style.pPr.spacing != null)
            _style.pPr.spacing.before = spacing;
        else
            _style.pPr.spacing = new CT_Spacing { before = spacing };

        return this;
    }

    public StyleBuilder SetFirstLineIndentationInPoint(int indentation) {
        if (_style.pPr.ind != null)
            _style.pPr.ind.firstLine = indentation;
        else
            _style.pPr.ind = new CT_Ind { firstLine = indentation };
        return this;
    }

    public StyleBuilder SetFirstLineIndentationInChars(int indentation) {
        if (_style.pPr.ind != null)
            _style.pPr.ind.firstLineChars = indentation.ToString();
        else
            _style.pPr.ind = new CT_Ind { firstLineChars = indentation.ToString() };
        return this;
    }

    /// <summary>
    /// Set Paragraph indentation In point units
    /// </summary>
    /// <param name="hanging"></param>
    /// <param name="right"></param>
    /// <param name="left"></param>
    public StyleBuilder SetIndentationInPoint(int right = 0, int left = 0, uint hanging = 0) {
        if (_style.pPr.ind != null) {
            _style.pPr.ind.hanging = hanging;
            _style.pPr.ind.right = right.ToString();
            _style.pPr.ind.left = left.ToString();
        } else {
            _style.pPr.ind = new CT_Ind {
                hanging = hanging,
                right = right.ToString(),
                left = left.ToString()
            };
        }

        return this;
    }

    /// <summary>
    /// Set Paragraph indentation In char units
    /// </summary>
    /// <param name="hanging"></param>
    /// <param name="right"></param>
    /// <param name="left"></param>
    public StyleBuilder SetIndentationInChars(uint hanging = 0, int right = 0, int left = 0) {
        if (_style.pPr.ind != null) {
            _style.pPr.ind.hangingChars = hanging.ToString();
            _style.pPr.ind.rightChars = right.ToString();
            _style.pPr.ind.leftChars = left.ToString();
        } else {
            _style.pPr.ind = new CT_Ind {
                hangingChars = hanging.ToString(),
                rightChars = right.ToString(),
                leftChars = left.ToString()
            };
        }

        return this;
    }

    /// <summary>
    /// Set parent styleID this style should be based on
    /// </summary>
    /// <param name="styleId">Parent styleID</param>
    public StyleBuilder SetParentStyleId(string styleId) {
        _style.basedOn = new CT_String { val = styleId };

        return this;
    }

    public StyleBuilder NoProof(bool noProof) {
        _style.rPr.noProof = new CT_OnOff { val = noProof };
        return this;
    }

    /// <summary>
    /// Style can be assigned to a paragraph by styleId
    /// </summary>
    /// <param name="styleId">Your style id which will be used by paragraph</param>
    public XWPFStyle Build(string styleId) {
        _style.styleId = styleId;

        return new XWPFStyle(_style);
    }

    public static XWPFStyle GetDefaultStyle() {
        return new XWPFStyle(new CT_Style {
            rPr = new CT_RPr {
                rFonts = new CT_Fonts {
                    ascii = "Times New Roman",
                    cs = "Times New Roman",
                    eastAsia = "Times New Roman",
                    hAnsi = "Times New Roman",
                    hint = ST_Hint.@default
                },
                sz = new CT_HpsMeasure { val = 22 },
                noProof = new CT_OnOff { val = true }
            },
            pPr = new CT_PPr {
                spacing = new CT_Spacing {
                    line = "-260",
                    after = 1,
                    before = 1
                }
            },
            @default = ST_OnOff.True,
            styleId = "DEFAULT_STYLE_ID"
        });
    }
}
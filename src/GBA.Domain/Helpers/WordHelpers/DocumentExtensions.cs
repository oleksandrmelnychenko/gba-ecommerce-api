using NPOI.XWPF.UserModel;

namespace GBA.Domain.Helpers.WordHelpers;

public static class DocumentExtensions {
    public static void AppendText(this XWPFRun xWPFRun, params string[] lines) {
        foreach (string line in lines) {
            xWPFRun.AppendText(line);
            if (lines.Length > 1) xWPFRun.AddCarriageReturn();
        }
    }
}
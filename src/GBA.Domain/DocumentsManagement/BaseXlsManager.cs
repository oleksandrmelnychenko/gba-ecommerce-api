using System;
using System.IO;
using GBA.Common.Helpers;
using OfficeOpenXml;

namespace GBA.Domain.DocumentsManagement;

public abstract class BaseXlsManager {
    protected (string fileName, string pdfName) SaveFiles(string fileName) {
        string pdfFile = string.Empty;

        try {
            pdfFile = HelperXlsManager.ConvertXlsxToPDF(fileName);
        } catch (Exception ex) {
            string logPath = Path.Combine(NoltFolderManager.GetDataFolderPath(), "excel_error_log.txt");

            File.AppendAllText(
                logPath,
                string.Format(
                    "\r\n{0}\r\n{1}\r\n",
                    ex.Message,
                    ex.InnerException != null ? ex.InnerException?.Message : string.Empty
                )
            );
        }

        return (fileName, pdfFile);
    }

    protected (string fileName, string pdfName) SaveFilesPages(string fileName) {
        string pdfFile = string.Empty;

        try {
            pdfFile = HelperXlsManager.ConvertXlsxToPDFPages(fileName);
        } catch (Exception ex) {
            string logPath = Path.Combine(NoltFolderManager.GetDataFolderPath(), "excel_error_log.txt");

            File.AppendAllText(
                logPath,
                string.Format(
                    "\r\n{0}\r\n{1}\r\n",
                    ex.Message,
                    ex.InnerException != null ? ex.InnerException?.Message : string.Empty
                )
            );
        }

        return (fileName, pdfFile);
    }

    protected ExcelPackage NewExcelPackage(string fileName) {
        FileInfo newFile = new(fileName);

        if (!newFile.Exists) return new ExcelPackage(newFile);

        newFile.Delete();

        newFile = new FileInfo(fileName);

        return new ExcelPackage(newFile);
    }
}
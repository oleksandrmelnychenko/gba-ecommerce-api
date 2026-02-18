using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace GBA.Common.Helpers;

public static class NoltFolderManager {
    private static string _serverPath;
    private static string _crmServerDataBaseUrl;

    // Cached paths - обчислюються один раз
    private static string _dataFolderPath;
    private static string _logFolderPath;
    private static string _imagesFolderPath;
    private static string _productImagesFolderPath;
    private static string _tempFolderPath;
    private static string _taxFreesFolderPath;
    private static string _sadsFolderPath;
    private static string _supplyDocumentsFolderPath;
    private static string _supplyPaymentTaskDocumentsFolderPath;
    private static string _supplyOrganizationAgreementsFolderPath;
    private static string _consumablesOrderDocumentsFolderPath;
    private static string _specificationsFolderPath;
    private static string _saleInvoicesFolderPath;
    private static string _saleReturnsFolderPath;
    private static string _organizationAgreementsFolderPath;
    private static string _seoContactsFolderPath;
    private static string _contactInfoFilePath;
    private static string _pagesFilePath;
    private static string _pagesLocaleFilePath;
    private static string _paymentImagesFolderPath;
    private static string _permissionVisualLocationFolderPath;

    public static void InitializeFolderManager(string serverPath) {
        _serverPath = serverPath;

        // Обчислюємо всі шляхи один раз
        _dataFolderPath = Path.Combine(_serverPath, "Data");
        _logFolderPath = Path.Combine(_serverPath, "Logs");
        _imagesFolderPath = Path.Combine(_serverPath, "Images");
        _productImagesFolderPath = Path.Combine(_imagesFolderPath, "Products");
        _tempFolderPath = Path.Combine(_dataFolderPath, "Temp");
        _supplyDocumentsFolderPath = Path.Combine(_dataFolderPath, "Supplies");
        _taxFreesFolderPath = Path.Combine(_supplyDocumentsFolderPath, "TaxFrees");
        _sadsFolderPath = Path.Combine(_supplyDocumentsFolderPath, "Sads");
        _supplyPaymentTaskDocumentsFolderPath = Path.Combine(_supplyDocumentsFolderPath, "SupplyPaymentTaskDocuments");
        _supplyOrganizationAgreementsFolderPath = Path.Combine(_supplyDocumentsFolderPath, "OrganizationAgreements");
        _consumablesOrderDocumentsFolderPath = Path.Combine(_dataFolderPath, "ConsumablesOrderDocuments");
        _specificationsFolderPath = Path.Combine(_dataFolderPath, "Specifications");
        _saleInvoicesFolderPath = Path.Combine(_dataFolderPath, "SaleInvoices");
        _saleReturnsFolderPath = Path.Combine(_dataFolderPath, "SaleReturns");
        _organizationAgreementsFolderPath = Path.Combine(_dataFolderPath, "OrganizationAgreements");
        _seoContactsFolderPath = Path.Combine(_dataFolderPath, "Seo", "Contacts");
        _contactInfoFilePath = Path.Combine(_dataFolderPath, "SeoData", "ContactInfo.json");
        _pagesFilePath = Path.Combine(_dataFolderPath, "SeoData", "Pages.json");
        _pagesLocaleFilePath = Path.Combine(_dataFolderPath, "SeoData", "PagesCopy.json");
        _paymentImagesFolderPath = Path.Combine(_imagesFolderPath, "PaymentConfirmation");
        _permissionVisualLocationFolderPath = Path.Combine(_dataFolderPath, "PermissionVisualLocation");

        // Створюємо всі необхідні директорії
        EnsureDirectoryExists(_dataFolderPath);
        EnsureDirectoryExists(_logFolderPath);
        EnsureDirectoryExists(_imagesFolderPath);
        EnsureDirectoryExists(_productImagesFolderPath);
        EnsureDirectoryExists(_tempFolderPath);
        EnsureDirectoryExists(_taxFreesFolderPath);
        EnsureDirectoryExists(_sadsFolderPath);
        EnsureDirectoryExists(_supplyDocumentsFolderPath);
        EnsureDirectoryExists(_supplyPaymentTaskDocumentsFolderPath);
        EnsureDirectoryExists(_supplyOrganizationAgreementsFolderPath);
        EnsureDirectoryExists(_consumablesOrderDocumentsFolderPath);
        EnsureDirectoryExists(_specificationsFolderPath);
        EnsureDirectoryExists(_saleInvoicesFolderPath);
        EnsureDirectoryExists(_saleReturnsFolderPath);
        EnsureDirectoryExists(_seoContactsFolderPath);
        EnsureDirectoryExists(_paymentImagesFolderPath);
        EnsureDirectoryExists(_permissionVisualLocationFolderPath);
    }

    private static void EnsureDirectoryExists(string path) {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    // Getters - просто повертають кешовані шляхи
    public static string GetDataFolderPath() => _dataFolderPath;
    public static string GetLogFolderPath() => _logFolderPath;
    public static string GetImagesFolderPath() => _imagesFolderPath;
    public static string GetProductImagesFolderPath() => _productImagesFolderPath;
    public static string GetTempFolderPath() => _tempFolderPath;
    public static string GetTaxFreesFolderPath() => _taxFreesFolderPath;
    public static string GetSadsFolderPath() => _sadsFolderPath;
    public static string GetSupplyPaymentTaskDocumentsFolderPath() => _supplyPaymentTaskDocumentsFolderPath;
    public static string GetSupplyOrganizationAgreementsFolderPath() => _supplyOrganizationAgreementsFolderPath;
    public static string GetConsumablesOrderDocumentsFolderPath() => _consumablesOrderDocumentsFolderPath;
    public static string GetSpecificationsFolderPath() => _specificationsFolderPath;
    public static string GetSaleInvoicesFolderPath() => _saleInvoicesFolderPath;
    public static string GetSaleReturnsFolderPath() => _saleReturnsFolderPath;
    public static string GetSeoContactsFolderPath() => _seoContactsFolderPath;
    public static string GetContactInfoPath() => _contactInfoFilePath;
    public static string GetPagesPath() => _pagesFilePath;
    public static string GetPagesLocalePath() => _pagesLocaleFilePath;
    public static string GetPaymentImagesFolderPath() => _paymentImagesFolderPath;
    public static string GetPermissionVisualLocationFolderPath() => _permissionVisualLocationFolderPath;

    public static string GetOrganizationAgreementsPath() {
        EnsureDirectoryExists(_organizationAgreementsFolderPath);
        return _organizationAgreementsFolderPath;
    }

    public static string GetSupplyDocumentsFolderPathByType(SupplyDocumentType type) {
        string path = Path.Combine(_supplyDocumentsFolderPath, $"{type}Documents");
        EnsureDirectoryExists(path);
        return path;
    }

    public static string GetEcommerceCrmConfigJsonFilePath() =>
        Path.Combine(_serverPath, "crm-configuration.json");

    public static string Convert(string path, string host) =>
        path.Replace(_serverPath, host).Replace('\\', '/');

    public static string GetFileType(string fileName) =>
        Path.GetExtension(fileName).TrimStart('.');

    public static async Task SaveFile(string path, IFormFile file) {
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
    }

    public static async Task SaveFile(string path, byte[] file) {
        await using var stream = new FileStream(path, FileMode.Create);
        await stream.WriteAsync(file);
    }

    public static void SetCrmServerDataBaseUrl(string url) => _crmServerDataBaseUrl = url;
    public static string GetCrmServerDataBaseUrl() => _crmServerDataBaseUrl;

    public static void DeleteFile(string path) {
        if (File.Exists(path)) File.Delete(path);
    }
}

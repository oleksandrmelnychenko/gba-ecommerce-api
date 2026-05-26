# GBA Ecommerce API Tests

Live contract tests for the DEV ecommerce API.

Default target:

```bash
dotnet test tests/GBA.Ecommerce.Api.Tests/GBA.Ecommerce.Api.Tests.csproj
```

Useful environment variables:

```bash
ECOM_API_BASE_URL=https://ecom-api-dev.85.17.167.167.nip.io
GBA_API_BASE_URL=https://gba-api-dev.85.17.167.167.nip.io
ECOM_API_CULTURE=uk
ECOM_API_SEARCH_QUERY=oil
ECOM_API_USERNAME=dev-user@example.com
ECOM_API_PASSWORD=dev-password
ECOM_API_RUN_REAL_AUTH=1
ECOM_API_RUN_REAL_REGISTRATION=1
ECOM_API_RUN_REAL_CART=1
ECOM_API_RUN_REAL_ORDER=1
```

The default suite is read-only. Authenticated cart write checks run only when credentials are provided and `ECOM_API_RUN_REAL_CART=1`.
Retail quick-order creation runs only with `ECOM_API_RUN_REAL_ORDER=1` or `ECOM_API_RUN_REAL_SALE=1`.
Refresh-token rotation runs only with credentials and `ECOM_API_RUN_REAL_AUTH=1`.
Disposable client registration runs only with `ECOM_API_RUN_REAL_REGISTRATION=1`.
Write-capable tests refuse to run unless `ECOM_API_BASE_URL` points at a DEV or local host.

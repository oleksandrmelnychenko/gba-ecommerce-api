using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class InitWithMergedMigrations : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "AccountingDocumentName",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentType = table.Column<int>(nullable: false),
                NameUK = table.Column<string>(maxLength: 120, nullable: true),
                NamePL = table.Column<string>(maxLength: 120, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_AccountingDocumentName", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ActProvidingServiceDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(maxLength: 500, nullable: true),
                FileName = table.Column<string>(maxLength: 500, nullable: true),
                ContentType = table.Column<string>(maxLength: 500, nullable: true),
                GeneratedName = table.Column<string>(maxLength: 500, nullable: true),
                Number = table.Column<string>(maxLength: 20, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ActProvidingServiceDocument", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "AgreementType",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 25, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_AgreementType", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "AgreementTypeCivilCode",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CodeOneC = table.Column<string>(maxLength: 25, nullable: true),
                NameUK = table.Column<string>(maxLength: 100, nullable: true),
                NamePL = table.Column<string>(maxLength: 100, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_AgreementTypeCivilCode", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "AllegroCategory",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                CategoryID = table.Column<int>(nullable: false),
                ParentCategoryID = table.Column<int>(nullable: false),
                Position = table.Column<int>(nullable: false),
                IsLeaf = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_AllegroCategory", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "AuditEntity",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Type = table.Column<int>(nullable: false),
                BaseEntityNetUID = table.Column<Guid>(nullable: false),
                EntityName = table.Column<string>(nullable: true),
                UpdatedBy = table.Column<string>(nullable: true),
                UpdatedByNetUID = table.Column<Guid>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_AuditEntity", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "AuditEntityPropertyNameTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                LocalizedName = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_AuditEntityPropertyNameTranslation", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Bank",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                MfoCode = table.Column<string>(maxLength: 6, nullable: false),
                EdrpouCode = table.Column<string>(maxLength: 10, nullable: false),
                City = table.Column<string>(maxLength: 150, nullable: true),
                Address = table.Column<string>(maxLength: 150, nullable: true),
                Phones = table.Column<string>(maxLength: 100, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Bank", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "BaseLifeCycleStatus",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SaleLifeCycleType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_BaseLifeCycleStatus", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "BaseSalePaymentStatus",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SalePaymentStatusType = table.Column<int>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_BaseSalePaymentStatus", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "CalculationType",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 25, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_CalculationType", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "CarBrand",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 100, nullable: true),
                Alias = table.Column<string>(nullable: true),
                Description = table.Column<string>(maxLength: 250, nullable: true),
                ImageUrl = table.Column<string>(maxLength: 100, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_CarBrand", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Category",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: true),
                RootCategoryID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Category", x => x.ID);
                table.ForeignKey(
                    "FK_Category_Category_RootCategoryID",
                    x => x.RootCategoryID,
                    "Category",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ChartMonth",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                Number = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ChartMonth", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ClientType",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                ClientTypeIcon = table.Column<string>(nullable: true),
                AllowMultiple = table.Column<bool>(nullable: false),
                Type = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientType", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ConsignmentItemMovementTypeName",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                NamePl = table.Column<string>(maxLength: 100, nullable: true),
                NameUa = table.Column<string>(maxLength: 100, nullable: true),
                MovementType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsignmentItemMovementTypeName", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ConsignmentNoteSetting",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 200, nullable: true),
                BrandAndNumberCar = table.Column<string>(maxLength: 200, nullable: true),
                TrailerNumber = table.Column<string>(maxLength: 200, nullable: true),
                Driver = table.Column<string>(maxLength: 200, nullable: true),
                Carrier = table.Column<string>(maxLength: 200, nullable: true),
                TypeTransportation = table.Column<string>(maxLength: 200, nullable: true),
                UnloadingPoint = table.Column<string>(maxLength: 500, nullable: true),
                LoadingPoint = table.Column<string>(maxLength: 500, nullable: true),
                Customer = table.Column<string>(maxLength: 200, nullable: true),
                ForReSale = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsignmentNoteSetting", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ConsumableProductCategory",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                Description = table.Column<string>(maxLength: 450, nullable: true),
                IsSupplyServiceCategory = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumableProductCategory", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Country",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                Code = table.Column<string>(maxLength: 25, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Country", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Currency",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                Code = table.Column<string>(maxLength: 25, nullable: true),
                CodeOneC = table.Column<string>(maxLength: 25, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Currency", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "CurrencyTrader",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FirstName = table.Column<string>(maxLength: 75, nullable: true),
                LastName = table.Column<string>(maxLength: 75, nullable: true),
                MiddleName = table.Column<string>(maxLength: 75, nullable: true),
                PhoneNumber = table.Column<string>(maxLength: 30, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_CurrencyTrader", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "DashboardNodeModule",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Language = table.Column<string>(maxLength: 2, nullable: true),
                Module = table.Column<string>(maxLength: 75, nullable: true),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                CssClass = table.Column<string>(maxLength: 200, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_DashboardNodeModule", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Debt",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Days = table.Column<int>(nullable: false),
                Total = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_Debt", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "DeliveryProductProtocolNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 20, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_DeliveryProductProtocolNumber", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "DocumentMonth",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 25, nullable: true),
                Number = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DocumentMonth", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "EcommerceContactInfo",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Address = table.Column<string>(maxLength: 250, nullable: false),
                Phone = table.Column<string>(maxLength: 30, nullable: false),
                Email = table.Column<string>(maxLength: 150, nullable: false),
                SiteUrl = table.Column<string>(maxLength: 200, nullable: false),
                Locale = table.Column<string>(maxLength: 2, nullable: true),
                PixelId = table.Column<string>(maxLength: 200, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_EcommerceContactInfo", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "EcommerceContacts",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                Phone = table.Column<string>(maxLength: 30, nullable: true),
                Skype = table.Column<string>(maxLength: 150, nullable: true),
                Icq = table.Column<string>(maxLength: 20, nullable: true),
                Email = table.Column<string>(maxLength: 150, nullable: true),
                ImgUrl = table.Column<string>(maxLength: 4000, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_EcommerceContacts", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "EcommercePage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PageName = table.Column<string>(nullable: true),
                TitleUa = table.Column<string>(maxLength: 100, nullable: true),
                TitleRu = table.Column<string>(maxLength: 100, nullable: true),
                DescriptionUa = table.Column<string>(maxLength: 1000, nullable: true),
                DescriptionRu = table.Column<string>(maxLength: 1000, nullable: true),
                KeyWords = table.Column<string>(maxLength: 1000, nullable: true),
                LdJson = table.Column<string>(maxLength: 4000, nullable: true),
                UrlUa = table.Column<string>(maxLength: 1000, nullable: true),
                UrlRu = table.Column<string>(maxLength: 1000, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_EcommercePage", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "EcommerceRegion",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                NameUa = table.Column<string>(maxLength: 150, nullable: true),
                NameRu = table.Column<string>(maxLength: 150, nullable: true),
                IsLocalPayment = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_EcommerceRegion", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "FilterItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: true),
                SQL = table.Column<string>(nullable: true),
                Order = table.Column<int>(nullable: false),
                Type = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_FilterItem", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "FilterOperationItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 25, nullable: true),
                SQL = table.Column<string>(maxLength: 25, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_FilterOperationItem", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Incoterm",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IncotermName = table.Column<string>(maxLength: 250, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Incoterm", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "MeasureUnit",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 25, nullable: true),
                Description = table.Column<string>(nullable: true),
                CodeOneC = table.Column<string>(maxLength: 25, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_MeasureUnit", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "OrganizationClient",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FullName = table.Column<string>(maxLength: 500, nullable: true),
                Address = table.Column<string>(maxLength: 500, nullable: true),
                Country = table.Column<string>(maxLength: 100, nullable: true),
                City = table.Column<string>(maxLength: 100, nullable: true),
                NIP = table.Column<string>(maxLength: 100, nullable: true),
                MarginAmount = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrganizationClient", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "OriginalNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                MainNumber = table.Column<string>(nullable: true),
                Number = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_OriginalNumber", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "PackingMarking",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PackingMarking", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "PackingMarkingPayment",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PackingMarkingPayment", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "PaymentCostMovement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OperationName = table.Column<string>(maxLength: 150, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentCostMovement", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "PaymentMovement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OperationName = table.Column<string>(maxLength: 150, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentMovement", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "PriceType",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 30, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PriceType", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ProductGroup",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                FullName = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: true),
                IsSubGroup = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SourceID = table.Column<byte[]>(maxLength: 16, nullable: true),
                IsActive = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductGroup", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Region",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 5, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Region", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ResidenceCard",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                ToDate = table.Column<DateTime>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ResidenceCard", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "RetailPaymentTypeTranslate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                LowPrice = table.Column<string>(maxLength: 250, nullable: true),
                FullPrice = table.Column<string>(maxLength: 250, nullable: true),
                CultureCode = table.Column<string>(maxLength: 5, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FastOrderSuccessMessage = table.Column<string>(maxLength: 500, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_RetailPaymentTypeTranslate", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SadPalletType",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 100, nullable: true),
                CssClass = table.Column<string>(maxLength: 50, nullable: true),
                Weight = table.Column<double>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SadPalletType", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SaleBaseShiftStatus",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ShiftStatus = table.Column<int>(nullable: false),
                Comment = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleBaseShiftStatus", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SaleInvoiceDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PaymentType = table.Column<int>(nullable: false),
                ClientPaymentType = table.Column<int>(nullable: false),
                City = table.Column<string>(nullable: true),
                Vat = table.Column<decimal>("money", nullable: false),
                ShippingAmount = table.Column<decimal>("money", nullable: false),
                ShippingAmountWithoutVat = table.Column<decimal>("money", nullable: false),
                ShippingAmountEur = table.Column<decimal>("money", nullable: false),
                ShippingAmountEurWithoutVat = table.Column<decimal>("money", nullable: false),
                ExchangeRateAmount = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleInvoiceDocument", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SaleInvoiceNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleInvoiceNumber", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SaleMessageNumerator",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CountMessage = table.Column<long>(nullable: false),
                Transfered = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleMessageNumerator", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SaleReturnItemStatusName",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SaleReturnItemStatus = table.Column<int>(nullable: false),
                NameUK = table.Column<string>(maxLength: 120, nullable: true),
                NamePL = table.Column<string>(maxLength: 120, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleReturnItemStatusName", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SeoPage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PageName = table.Column<string>(nullable: true),
                Title = table.Column<string>(maxLength: 100, nullable: true),
                Description = table.Column<string>(maxLength: 1000, nullable: true),
                KeyWords = table.Column<string>(maxLength: 1000, nullable: true),
                LdJson = table.Column<string>(maxLength: 4000, nullable: true),
                Url = table.Column<string>(maxLength: 1000, nullable: true),
                Locale = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SeoPage", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ServiceDetailItemKey",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Symbol = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                Type = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ServiceDetailItemKey", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "Statham",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FirstName = table.Column<string>(maxLength: 50, nullable: true),
                LastName = table.Column<string>(maxLength: 50, nullable: true),
                MiddleName = table.Column<string>(maxLength: 50, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Statham", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyDeliveryDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                TransportationType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyDeliveryDocument", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyInformationDeliveryProtocolKey",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Key = table.Column<string>(nullable: true),
                IsDefault = table.Column<bool>(nullable: false),
                TransportationType = table.Column<int>(nullable: false),
                KeyAssignedTo = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInformationDeliveryProtocolKey", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderNumber", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderPaymentDeliveryProtocolKey",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Key = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderPaymentDeliveryProtocolKey", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkrainePaymentDeliveryProtocolKey",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Key = table.Column<string>(maxLength: 150, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkrainePaymentDeliveryProtocolKey", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyProForm",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                DateFrom = table.Column<DateTime>(nullable: true),
                IsSkipped = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyProForm", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyServiceAccountDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(maxLength: 500, nullable: true),
                FileName = table.Column<string>(maxLength: 500, nullable: true),
                ContentType = table.Column<string>(maxLength: 500, nullable: true),
                GeneratedName = table.Column<string>(maxLength: 500, nullable: true),
                Number = table.Column<string>(maxLength: 20, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyServiceAccountDocument", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupplyServiceNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                IsPoland = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyServiceNumber", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "SupportVideo",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                NameUk = table.Column<string>(maxLength: 150, nullable: true),
                NamePl = table.Column<string>(maxLength: 150, nullable: true),
                Url = table.Column<string>(maxLength: 250, nullable: true),
                DocumentUrl = table.Column<string>(maxLength: 250, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupportVideo", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "TaxAccountingScheme",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CodeOneC = table.Column<string>(maxLength: 25, nullable: true),
                PurchaseTaxBaseMoment = table.Column<int>(nullable: false),
                SaleTaxBaseMoment = table.Column<int>(nullable: false),
                NameUK = table.Column<string>(maxLength: 100, nullable: true),
                NamePL = table.Column<string>(maxLength: 100, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TaxAccountingScheme", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "TaxInspection",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                InspectionNumber = table.Column<string>(maxLength: 50, nullable: true),
                InspectionType = table.Column<string>(maxLength: 150, nullable: true),
                InspectionName = table.Column<string>(maxLength: 250, nullable: true),
                InspectionRegionName = table.Column<string>(maxLength: 250, nullable: true),
                InspectionRegionCode = table.Column<string>(maxLength: 50, nullable: true),
                InspectionAddress = table.Column<string>(maxLength: 250, nullable: true),
                InspectionUSREOU = table.Column<string>(maxLength: 50, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TaxInspection", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "TermsOfDelivery",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TermsOfDelivery", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "TransporterType",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 50, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TransporterType", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "UserRole",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 40, nullable: true),
                Dashboard = table.Column<string>(maxLength: 100, nullable: true),
                UserRoleType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_UserRole", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "VatRate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Value = table.Column<double>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_VatRate", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "WorkingContract",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                ToDate = table.Column<DateTime>(nullable: false),
                PlaceOfWork = table.Column<string>(nullable: true),
                CurrentWorkplace = table.Column<string>(nullable: true),
                KindOfWork = table.Column<string>(nullable: true),
                Delegation = table.Column<string>(nullable: true),
                Premium = table.Column<string>(nullable: true),
                WorkTimeSize = table.Column<string>(nullable: true),
                VacationDays = table.Column<string>(nullable: true),
                NightWork = table.Column<string>(nullable: true),
                StudyLeave = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_WorkingContract", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "WorkPermit",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                ToDate = table.Column<DateTime>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_WorkPermit", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "AgreementTypeTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                AgreementTypeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_AgreementTypeTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_AgreementTypeTranslation_AgreementType_AgreementTypeID",
                    x => x.AgreementTypeID,
                    "AgreementType",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "AuditEntityProperty",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Type = table.Column<int>(nullable: false),
                Name = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: true),
                Value = table.Column<string>(nullable: true),
                AuditEntityID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_AuditEntityProperty", x => x.ID);
                table.ForeignKey(
                    "FK_AuditEntityProperty_AuditEntity_AuditEntityID",
                    x => x.AuditEntityID,
                    "AuditEntity",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "CalculationTypeTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                CalculationTypeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CalculationTypeTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_CalculationTypeTranslation_CalculationType_CalculationTypeID",
                    x => x.CalculationTypeID,
                    "CalculationType",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ChartMonthTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                ChartMonthID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ChartMonthTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_ChartMonthTranslation_ChartMonth_ChartMonthID",
                    x => x.ChartMonthID,
                    "ChartMonth",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientTypeRole",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                Description = table.Column<string>(nullable: true),
                ClientTypeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientTypeRole", x => x.ID);
                table.ForeignKey(
                    "FK_ClientTypeRole_ClientType_ClientTypeID",
                    x => x.ClientTypeID,
                    "ClientType",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ClientTypeTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                ClientTypeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientTypeTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_ClientTypeTranslation_ClientType_ClientTypeID",
                    x => x.ClientTypeID,
                    "ClientType",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ConsumableProductCategoryTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                Description = table.Column<string>(maxLength: 450, nullable: true),
                ConsumableProductCategoryID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumableProductCategoryTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_ConsumableProductCategoryTranslation_ConsumableProductCategory_ConsumableProductCategoryID",
                    x => x.ConsumableProductCategoryID,
                    "ConsumableProductCategory",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientBankDetailAccountNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                AccountNumber = table.Column<string>(nullable: true),
                CurrencyID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientBankDetailAccountNumber", x => x.ID);
                table.ForeignKey(
                    "FK_ClientBankDetailAccountNumber_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ClientBankDetailIbanNo",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IBANNO = table.Column<string>(nullable: true),
                CurrencyID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientBankDetailIbanNo", x => x.ID);
                table.ForeignKey(
                    "FK_ClientBankDetailIbanNo_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "CrossExchangeRate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CurrencyFromID = table.Column<long>(nullable: false),
                CurrencyToID = table.Column<long>(nullable: false),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                Code = table.Column<string>(maxLength: 30, nullable: true),
                Culture = table.Column<string>(maxLength: 5, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_CrossExchangeRate", x => x.ID);
                table.ForeignKey(
                    "FK_CrossExchangeRate_Currency_CurrencyFromID",
                    x => x.CurrencyFromID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CrossExchangeRate_Currency_CurrencyToID",
                    x => x.CurrencyToID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CurrencyTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                CurrencyID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CurrencyTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_CurrencyTranslation_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ExchangeRate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Culture = table.Column<string>(nullable: true),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                Currency = table.Column<string>(nullable: true),
                Code = table.Column<string>(nullable: true),
                CurrencyID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ExchangeRate", x => x.ID);
                table.ForeignKey(
                    "FK_ExchangeRate_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "GovCrossExchangeRate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CurrencyFromID = table.Column<long>(nullable: false),
                CurrencyToID = table.Column<long>(nullable: false),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                Code = table.Column<string>(maxLength: 30, nullable: true),
                Culture = table.Column<string>(maxLength: 5, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_GovCrossExchangeRate", x => x.ID);
                table.ForeignKey(
                    "FK_GovCrossExchangeRate_Currency_CurrencyFromID",
                    x => x.CurrencyFromID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_GovCrossExchangeRate_Currency_CurrencyToID",
                    x => x.CurrencyToID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "GovExchangeRate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Culture = table.Column<string>(nullable: true),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                Currency = table.Column<string>(nullable: true),
                Code = table.Column<string>(nullable: true),
                CurrencyID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_GovExchangeRate", x => x.ID);
                table.ForeignKey(
                    "FK_GovExchangeRate_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CurrencyTraderExchangeRate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CurrencyName = table.Column<string>(maxLength: 25, nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: false),
                FromDate = table.Column<DateTime>(nullable: false),
                CurrencyTraderID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CurrencyTraderExchangeRate", x => x.ID);
                table.ForeignKey(
                    "FK_CurrencyTraderExchangeRate_CurrencyTrader_CurrencyTraderID",
                    x => x.CurrencyTraderID,
                    "CurrencyTrader",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DashboardNode",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Language = table.Column<string>(maxLength: 2, nullable: true),
                Module = table.Column<string>(maxLength: 75, nullable: true),
                Route = table.Column<string>(maxLength: 4000, nullable: true),
                CssClass = table.Column<string>(maxLength: 200, nullable: true),
                ParentDashboardNodeID = table.Column<long>(nullable: true),
                DashboardNodeModuleID = table.Column<long>(nullable: true),
                DashboardNodeType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DashboardNode", x => x.ID);
                table.ForeignKey(
                    "FK_DashboardNode_DashboardNodeModule_DashboardNodeModuleID",
                    x => x.DashboardNodeModuleID,
                    "DashboardNodeModule",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DashboardNode_DashboardNode_ParentDashboardNodeID",
                    x => x.ParentDashboardNodeID,
                    "DashboardNode",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "RetailClient",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                PhoneNumber = table.Column<string>(nullable: false),
                EcommerceRegionId = table.Column<long>(nullable: true),
                ShoppingCartJson = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_RetailClient", x => x.ID);
                table.ForeignKey(
                    "FK_RetailClient_EcommerceRegion_EcommerceRegionId",
                    x => x.EcommerceRegionId,
                    "EcommerceRegion",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "FilterItemTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: true),
                FilterItemID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_FilterItemTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_FilterItemTranslation_FilterItem_FilterItemID",
                    x => x.FilterItemID,
                    "FilterItem",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "FilterOperationItemTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                FilterOperationItemID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_FilterOperationItemTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_FilterOperationItemTranslation_FilterOperationItem_FilterOperationItemID",
                    x => x.FilterOperationItemID,
                    "FilterOperationItem",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ConsumableProduct",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                VendorCode = table.Column<string>(maxLength: 3, nullable: true),
                ConsumableProductCategoryID = table.Column<long>(nullable: false),
                MeasureUnitID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumableProduct", x => x.ID);
                table.ForeignKey(
                    "FK_ConsumableProduct_ConsumableProductCategory_ConsumableProductCategoryID",
                    x => x.ConsumableProductCategoryID,
                    "ConsumableProductCategory",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsumableProduct_MeasureUnit_MeasureUnitID",
                    x => x.MeasureUnitID,
                    "MeasureUnit",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "MeasureUnitTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: true),
                MeasureUnitID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_MeasureUnitTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_MeasureUnitTranslation_MeasureUnit_MeasureUnitID",
                    x => x.MeasureUnitID,
                    "MeasureUnit",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Product",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                VendorCode = table.Column<string>(maxLength: 40, nullable: true),
                Name = table.Column<string>(maxLength: 120, nullable: true),
                NameUA = table.Column<string>(maxLength: 120, nullable: true),
                NamePL = table.Column<string>(maxLength: 120, nullable: true),
                Description = table.Column<string>(maxLength: 2000, nullable: true),
                DescriptionUA = table.Column<string>(maxLength: 2000, nullable: true),
                DescriptionPL = table.Column<string>(maxLength: 2000, nullable: true),
                Size = table.Column<string>(maxLength: 100, nullable: true),
                NotesPL = table.Column<string>(maxLength: 2000, nullable: true),
                NotesUA = table.Column<string>(maxLength: 2000, nullable: true),
                PackingStandard = table.Column<string>(nullable: true),
                OrderStandard = table.Column<string>(nullable: true),
                UCGFEA = table.Column<string>(nullable: true),
                Volume = table.Column<string>(nullable: true),
                Top = table.Column<string>(maxLength: 3, nullable: true),
                SynonymsUA = table.Column<string>(maxLength: 2000, nullable: true),
                SynonymsPL = table.Column<string>(maxLength: 2000, nullable: true),
                SearchName = table.Column<string>(maxLength: 120, nullable: true),
                SearchNameUA = table.Column<string>(maxLength: 120, nullable: true),
                SearchNamePL = table.Column<string>(maxLength: 120, nullable: true),
                SearchDescription = table.Column<string>(maxLength: 2000, nullable: true),
                SearchDescriptionUA = table.Column<string>(maxLength: 2000, nullable: true),
                SearchDescriptionPL = table.Column<string>(maxLength: 2000, nullable: true),
                SearchVendorCode = table.Column<string>(maxLength: 40, nullable: true),
                SearchSize = table.Column<string>(maxLength: 100, nullable: true),
                SearchSynonymsUA = table.Column<string>(maxLength: 2000, nullable: true),
                SearchSynonymsPL = table.Column<string>(maxLength: 2000, nullable: true),
                Weight = table.Column<double>(nullable: false),
                HasAnalogue = table.Column<bool>(nullable: false),
                HasComponent = table.Column<bool>(nullable: false),
                HasImage = table.Column<bool>(nullable: false),
                IsForWeb = table.Column<bool>(nullable: false),
                IsForSale = table.Column<bool>(nullable: false),
                IsForZeroSale = table.Column<bool>(nullable: false),
                MainOriginalNumber = table.Column<string>(maxLength: 80, nullable: true),
                Image = table.Column<string>(nullable: true),
                MeasureUnitID = table.Column<long>(nullable: false),
                SourceID = table.Column<byte[]>(maxLength: 16, nullable: true),
                ParentID = table.Column<byte[]>(maxLength: 16, nullable: true),
                OldEcommerceID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_Product", x => x.ID);
                table.ForeignKey(
                    "FK_Product_MeasureUnit_MeasureUnitID",
                    x => x.MeasureUnitID,
                    "MeasureUnit",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentCostMovementTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                OperationName = table.Column<string>(maxLength: 150, nullable: true),
                PaymentCostMovementID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentCostMovementTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentCostMovementTranslation_PaymentCostMovement_PaymentCostMovementID",
                    x => x.PaymentCostMovementID,
                    "PaymentCostMovement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentMovementTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                PaymentMovementID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentMovementTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentMovementTranslation_PaymentMovement_PaymentMovementID",
                    x => x.PaymentMovementID,
                    "PaymentMovement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PriceTypeTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 50, nullable: true),
                PriceTypeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PriceTypeTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_PriceTypeTranslation_PriceType_PriceTypeID",
                    x => x.PriceTypeID,
                    "PriceType",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Pricing",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 30, nullable: true),
                Comment = table.Column<string>(nullable: true),
                ExtraCharge = table.Column<double>(nullable: true),
                CalculatedExtraCharge = table.Column<decimal>("money", nullable: false),
                BasePricingID = table.Column<long>(nullable: true),
                CurrencyID = table.Column<long>(nullable: true),
                PriceTypeID = table.Column<long>(nullable: true),
                Culture = table.Column<string>(nullable: true),
                ForShares = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ForVat = table.Column<bool>(nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_Pricing", x => x.ID);
                table.ForeignKey(
                    "FK_Pricing_Pricing_BasePricingID",
                    x => x.BasePricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Pricing_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Pricing_PriceType_PriceTypeID",
                    x => x.PriceTypeID,
                    "PriceType",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductSubGroup",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                RootProductGroupID = table.Column<long>(nullable: false),
                SubProductGroupID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductSubGroup", x => x.ID);
                table.ForeignKey(
                    "FK_ProductSubGroup_ProductGroup_RootProductGroupID",
                    x => x.RootProductGroupID,
                    "ProductGroup",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductSubGroup_ProductGroup_SubProductGroupID",
                    x => x.SubProductGroupID,
                    "ProductGroup",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "RegionCode",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                RegionID = table.Column<long>(nullable: false),
                Value = table.Column<string>(maxLength: 10, nullable: true),
                City = table.Column<string>(maxLength: 150, nullable: true),
                District = table.Column<string>(maxLength: 150, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_RegionCode", x => x.ID);
                table.ForeignKey(
                    "FK_RegionCode_Region_RegionID",
                    x => x.RegionID,
                    "Region",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "StathamCar",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Volume = table.Column<double>(nullable: false),
                Number = table.Column<string>(maxLength: 150, nullable: true),
                StathamID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_StathamCar", x => x.ID);
                table.ForeignKey(
                    "FK_StathamCar_Statham_StathamID",
                    x => x.StathamID,
                    "Statham",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "StathamPassport",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PassportSeria = table.Column<string>(maxLength: 20, nullable: true),
                PassportNumber = table.Column<string>(maxLength: 20, nullable: true),
                PassportIssuedBy = table.Column<string>(maxLength: 250, nullable: true),
                City = table.Column<string>(maxLength: 150, nullable: true),
                Street = table.Column<string>(maxLength: 150, nullable: true),
                HouseNumber = table.Column<string>(maxLength: 50, nullable: true),
                PassportIssuedDate = table.Column<DateTime>(nullable: false),
                PassportCloseDate = table.Column<DateTime>(nullable: false),
                StathamID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_StathamPassport", x => x.ID);
                table.ForeignKey(
                    "FK_StathamPassport_Statham_StathamID",
                    x => x.StathamID,
                    "Statham",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInformationDeliveryProtocolKeyTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Key = table.Column<string>(nullable: true),
                SupplyInformationDeliveryProtocolKeyID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInformationDeliveryProtocolKeyTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInformationDeliveryProtocolKeyTranslation_SupplyInformationDeliveryProtocolKey_SupplyInformationDeliveryProtocolKeyID",
                    x => x.SupplyInformationDeliveryProtocolKeyID,
                    "SupplyInformationDeliveryProtocolKey",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProFormDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                SupplyProFormID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProFormDocument", x => x.ID);
                table.ForeignKey(
                    "FK_ProFormDocument_SupplyProForm_SupplyProFormID",
                    x => x.SupplyProFormID,
                    "SupplyProForm",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrganizationClientAgreement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                CurrencyID = table.Column<long>(nullable: false),
                OrganizationClientID = table.Column<long>(nullable: false),
                TaxAccountingSchemeID = table.Column<long>(nullable: true),
                AgreementTypeCivilCodeID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrganizationClientAgreement", x => x.ID);
                table.ForeignKey(
                    "FK_OrganizationClientAgreement_AgreementTypeCivilCode_AgreementTypeCivilCodeID",
                    x => x.AgreementTypeCivilCodeID,
                    "AgreementTypeCivilCode",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrganizationClientAgreement_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrganizationClientAgreement_OrganizationClient_OrganizationClientID",
                    x => x.OrganizationClientID,
                    "OrganizationClient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrganizationClientAgreement_TaxAccountingScheme_TaxAccountingSchemeID",
                    x => x.TaxAccountingSchemeID,
                    "TaxAccountingScheme",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Transporter",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                CssClass = table.Column<string>(nullable: true),
                Priority = table.Column<int>(nullable: false),
                TransporterTypeID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Transporter", x => x.ID);
                table.ForeignKey(
                    "FK_Transporter_TransporterType_TransporterTypeID",
                    x => x.TransporterTypeID,
                    "TransporterType",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TransporterTypeTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                TransporterTypeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_TransporterTypeTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_TransporterTypeTranslation_TransporterType_TransporterTypeID",
                    x => x.TransporterTypeID,
                    "TransporterType",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "UserRoleTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                UserRoleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_UserRoleTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_UserRoleTranslation_UserRole_UserRoleID",
                    x => x.UserRoleID,
                    "UserRole",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "UserDetails",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FirstName = table.Column<string>(nullable: true),
                MiddleName = table.Column<string>(nullable: true),
                LastName = table.Column<string>(nullable: true),
                FathersName = table.Column<string>(nullable: true),
                MothersName = table.Column<string>(nullable: true),
                DateOfBirth = table.Column<DateTime>(nullable: false),
                Address = table.Column<string>(nullable: true),
                Accommodation = table.Column<string>(nullable: true),
                Registration = table.Column<string>(nullable: true),
                Education = table.Column<string>(nullable: true),
                Profession = table.Column<string>(nullable: true),
                WorkExperience = table.Column<double>(nullable: false),
                FamilyStatus = table.Column<string>(nullable: true),
                NumberOfDependents = table.Column<int>(nullable: false),
                IsBigFamily = table.Column<bool>(nullable: false),
                SocialSecurityNumber = table.Column<string>(nullable: true),
                TIN = table.Column<string>(nullable: true),
                VATIN = table.Column<string>(nullable: true),
                PassportNumber = table.Column<string>(nullable: true),
                DocumentsExpirationDate = table.Column<DateTime>(nullable: false),
                MedicalCertificateToDate = table.Column<DateTime>(nullable: false),
                AdditionalSchools = table.Column<string>(nullable: true),
                VocationalCourses = table.Column<string>(nullable: true),
                BasicHealtAndSagetyEducation = table.Column<string>(nullable: true),
                SpecializedTraining = table.Column<string>(nullable: true),
                WorkHeight = table.Column<double>(nullable: false),
                HasPermissionToOperateCarts = table.Column<bool>(nullable: false),
                Caveats = table.Column<string>(nullable: true),
                ResidenceCardID = table.Column<long>(nullable: false),
                WorkingContractID = table.Column<long>(nullable: false),
                WorkPermitID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_UserDetails", x => x.ID);
                table.ForeignKey(
                    "FK_UserDetails_ResidenceCard_ResidenceCardID",
                    x => x.ResidenceCardID,
                    "ResidenceCard",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_UserDetails_WorkPermit_WorkPermitID",
                    x => x.WorkPermitID,
                    "WorkPermit",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_UserDetails_WorkingContract_WorkingContractID",
                    x => x.WorkingContractID,
                    "WorkingContract",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientTypeRoleTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 75, nullable: true),
                Description = table.Column<string>(nullable: true),
                ClientTypeRoleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientTypeRoleTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_ClientTypeRoleTranslation_ClientTypeRole_ClientTypeRoleID",
                    x => x.ClientTypeRoleID,
                    "ClientTypeRole",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "PerfectClient",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Lable = table.Column<string>(maxLength: 100, nullable: true),
                Value = table.Column<string>(nullable: true),
                IsSelected = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Description = table.Column<string>(maxLength: 250, nullable: true),
                Type = table.Column<int>(nullable: false, defaultValue: 1),
                ClientTypeRoleID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PerfectClient", x => x.ID);
                table.ForeignKey(
                    "FK_PerfectClient_ClientTypeRole_ClientTypeRoleID",
                    x => x.ClientTypeRoleID,
                    "ClientTypeRole",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientBankDetails",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                BankAndBranch = table.Column<string>(nullable: true),
                BankAddress = table.Column<string>(nullable: true),
                Swift = table.Column<string>(nullable: true),
                BranchCode = table.Column<string>(nullable: true),
                AccountNumberID = table.Column<long>(nullable: true),
                ClientBankDetailIbanNoID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientBankDetails", x => x.ID);
                table.ForeignKey(
                    "FK_ClientBankDetails_ClientBankDetailAccountNumber_AccountNumberID",
                    x => x.AccountNumberID,
                    "ClientBankDetailAccountNumber",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientBankDetails_ClientBankDetailIbanNo_ClientBankDetailIbanNoID",
                    x => x.ClientBankDetailIbanNoID,
                    "ClientBankDetailIbanNo",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "RetailClientPaymentImage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ImgUrl = table.Column<string>(maxLength: 1000, nullable: true),
                RetailClientId = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_RetailClientPaymentImage", x => x.ID);
                table.ForeignKey(
                    "FK_RetailClientPaymentImage_RetailClient_RetailClientId",
                    x => x.RetailClientId,
                    "RetailClient",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ConsumableProductTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                ConsumableProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumableProductTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_ConsumableProductTranslation_ConsumableProduct_ConsumableProductID",
                    x => x.ConsumableProductID,
                    "ConsumableProduct",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "AllegroProductReservation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ProductID = table.Column<long>(nullable: false),
                Qty = table.Column<double>(nullable: false),
                AllegroItemID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_AllegroProductReservation", x => x.ID);
                table.ForeignKey(
                    "FK_AllegroProductReservation_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductAnalogue",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                BaseProductID = table.Column<long>(nullable: false),
                AnalogueProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductAnalogue", x => x.ID);
                table.ForeignKey(
                    "FK_ProductAnalogue_Product_AnalogueProductID",
                    x => x.AnalogueProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductAnalogue_Product_BaseProductID",
                    x => x.BaseProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductCarBrand",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CarBrandID = table.Column<long>(nullable: false),
                ProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductCarBrand", x => x.ID);
                table.ForeignKey(
                    "FK_ProductCarBrand_CarBrand_CarBrandID",
                    x => x.CarBrandID,
                    "CarBrand",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductCarBrand_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductCategory",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CategoryID = table.Column<long>(nullable: false),
                ProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductCategory", x => x.ID);
                table.ForeignKey(
                    "FK_ProductCategory_Category_CategoryID",
                    x => x.CategoryID,
                    "Category",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductCategory_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductImage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ImageUrl = table.Column<string>(maxLength: 500, nullable: true),
                IsMainImage = table.Column<bool>(nullable: false),
                ProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductImage", x => x.ID);
                table.ForeignKey(
                    "FK_ProductImage_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductOriginalNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OriginalNumberID = table.Column<long>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                IsMainOriginalNumber = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductOriginalNumber", x => x.ID);
                table.ForeignKey(
                    "FK_ProductOriginalNumber_OriginalNumber_OriginalNumberID",
                    x => x.OriginalNumberID,
                    "OriginalNumber",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductOriginalNumber_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductProductGroup",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ProductGroupID = table.Column<long>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                VendorCode = table.Column<string>(maxLength: 50, nullable: true),
                OrderStandard = table.Column<double>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductProductGroup", x => x.ID);
                table.ForeignKey(
                    "FK_ProductProductGroup_ProductGroup_ProductGroupID",
                    x => x.ProductGroupID,
                    "ProductGroup",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductProductGroup_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductSet",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                BaseProductID = table.Column<long>(nullable: false),
                ComponentProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductSet", x => x.ID);
                table.ForeignKey(
                    "FK_ProductSet_Product_BaseProductID",
                    x => x.BaseProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductSet_Product_ComponentProductID",
                    x => x.ComponentProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductSlug",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Url = table.Column<string>(maxLength: 250, nullable: true),
                Locale = table.Column<string>(maxLength: 4, nullable: true),
                ProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductSlug", x => x.ID);
                table.ForeignKey(
                    "FK_ProductSlug_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "EcommerceDefaultPricing",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PricingID = table.Column<long>(nullable: false),
                PromotionalPricingID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_EcommerceDefaultPricing", x => x.ID);
                table.ForeignKey(
                    "FK_EcommerceDefaultPricing_Pricing_PricingID",
                    x => x.PricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_EcommerceDefaultPricing_Pricing_PromotionalPricingID",
                    x => x.PromotionalPricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PricingProductGroupDiscount",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("money", nullable: false),
                CalculatedExtraCharge = table.Column<decimal>("money", nullable: false),
                ProductGroupID = table.Column<long>(nullable: false),
                PricingID = table.Column<long>(nullable: false),
                BasePricingID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PricingProductGroupDiscount", x => x.ID);
                table.ForeignKey(
                    "FK_PricingProductGroupDiscount_Pricing_BasePricingID",
                    x => x.BasePricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PricingProductGroupDiscount_Pricing_PricingID",
                    x => x.PricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PricingProductGroupDiscount_ProductGroup_ProductGroupID",
                    x => x.ProductGroupID,
                    "ProductGroup",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PricingTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(maxLength: 4, nullable: true),
                Name = table.Column<string>(maxLength: 30, nullable: true),
                PricingID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PricingTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_PricingTranslation_Pricing_PricingID",
                    x => x.PricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ProductPricing",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PricingID = table.Column<long>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                Price = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductPricing", x => x.ID);
                table.ForeignKey(
                    "FK_ProductPricing_Pricing_PricingID",
                    x => x.PricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductPricing_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ProviderPricing",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                CurrencyID = table.Column<long>(nullable: true),
                BasePricingID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProviderPricing", x => x.ID);
                table.ForeignKey(
                    "FK_ProviderPricing_Pricing_BasePricingID",
                    x => x.BasePricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProviderPricing_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "User",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FirstName = table.Column<string>(nullable: true),
                LastName = table.Column<string>(nullable: true),
                MiddleName = table.Column<string>(nullable: true),
                Abbreviation = table.Column<string>(nullable: true),
                Email = table.Column<string>(nullable: true),
                PhoneNumber = table.Column<string>(nullable: true),
                Region = table.Column<string>(nullable: true),
                UserRoleID = table.Column<long>(nullable: true),
                UserDetailsId = table.Column<long>(nullable: true),
                IsActive = table.Column<bool>(nullable: false, defaultValueSql: "1")
            },
            constraints: table => {
                table.PrimaryKey("PK_User", x => x.ID);
                table.ForeignKey(
                    "FK_User_UserDetails_UserDetailsId",
                    x => x.UserDetailsId,
                    "UserDetails",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_User_UserRole_UserRoleID",
                    x => x.UserRoleID,
                    "UserRole",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PerfectClientTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                Description = table.Column<string>(nullable: true),
                PerfectClientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PerfectClientTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_PerfectClientTranslation_PerfectClient_PerfectClientID",
                    x => x.PerfectClientID,
                    "PerfectClient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PerfectClientValue",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Value = table.Column<string>(nullable: true),
                IsSelected = table.Column<bool>(nullable: false),
                PerfectClientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PerfectClientValue", x => x.ID);
                table.ForeignKey(
                    "FK_PerfectClientValue_PerfectClient_PerfectClientID",
                    x => x.PerfectClientID,
                    "PerfectClient",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Client",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                TIN = table.Column<string>(maxLength: 30, nullable: true),
                USREOU = table.Column<string>(maxLength: 30, nullable: true),
                SROI = table.Column<string>(nullable: true),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                FullName = table.Column<string>(maxLength: 200, nullable: true),
                FirstName = table.Column<string>(maxLength: 150, nullable: true),
                MiddleName = table.Column<string>(maxLength: 150, nullable: true),
                LastName = table.Column<string>(maxLength: 150, nullable: true),
                SupplierName = table.Column<string>(nullable: true),
                SupplierContactName = table.Column<string>(nullable: true),
                SupplierCode = table.Column<string>(maxLength: 40, nullable: true),
                Manufacturer = table.Column<string>(nullable: true),
                Brand = table.Column<string>(nullable: true),
                Comment = table.Column<string>(nullable: true),
                MobileNumber = table.Column<string>(nullable: true),
                ClientNumber = table.Column<string>(nullable: true),
                SMSNumber = table.Column<string>(nullable: true),
                FaxNumber = table.Column<string>(nullable: true),
                AccountantNumber = table.Column<string>(nullable: true),
                DirectorNumber = table.Column<string>(nullable: true),
                ICQ = table.Column<string>(nullable: true),
                EmailAddress = table.Column<string>(nullable: true),
                DeliveryAddress = table.Column<string>(nullable: true),
                LegalAddress = table.Column<string>(nullable: true),
                ActualAddress = table.Column<string>(nullable: true),
                IncotermsElse = table.Column<string>(nullable: true),
                Street = table.Column<string>(maxLength: 250, nullable: true),
                ZipCode = table.Column<string>(maxLength: 250, nullable: true),
                HouseNumber = table.Column<string>(maxLength: 250, nullable: true),
                ClearCartAfterDays = table.Column<int>(nullable: false, defaultValue: 3),
                OldEcommerceID = table.Column<long>(nullable: false),
                SourceID = table.Column<byte[]>(maxLength: 16, nullable: true),
                RegionID = table.Column<long>(nullable: true),
                RegionCodeID = table.Column<long>(nullable: true),
                CountryID = table.Column<long>(nullable: true),
                ClientBankDetailsID = table.Column<long>(nullable: true),
                TermsOfDeliveryID = table.Column<long>(nullable: true),
                PackingMarkingID = table.Column<long>(nullable: true),
                PackingMarkingPaymentID = table.Column<long>(nullable: true),
                Abbreviation = table.Column<string>(nullable: true),
                IsIndividual = table.Column<bool>(nullable: false),
                IsActive = table.Column<bool>(nullable: false),
                IsSubClient = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsTradePoint = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsPayForDelivery = table.Column<bool>(nullable: false),
                IsBlocked = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsIncotermsElse = table.Column<bool>(nullable: false),
                IsTemporaryClient = table.Column<bool>(nullable: false),
                IsFromECommerce = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsForRetail = table.Column<bool>(nullable: false),
                Manager = table.Column<string>(maxLength: 250, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Client", x => x.ID);
                table.ForeignKey(
                    "FK_Client_ClientBankDetails_ClientBankDetailsID",
                    x => x.ClientBankDetailsID,
                    "ClientBankDetails",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Client_Country_CountryID",
                    x => x.CountryID,
                    "Country",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Client_PackingMarking_PackingMarkingID",
                    x => x.PackingMarkingID,
                    "PackingMarking",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Client_PackingMarkingPayment_PackingMarkingPaymentID",
                    x => x.PackingMarkingPaymentID,
                    "PackingMarkingPayment",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Client_RegionCode_RegionCodeID",
                    x => x.RegionCodeID,
                    "RegionCode",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Client_Region_RegionID",
                    x => x.RegionID,
                    "Region",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Client_TermsOfDelivery_TermsOfDeliveryID",
                    x => x.TermsOfDeliveryID,
                    "TermsOfDelivery",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ActProvidingService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsAccounting = table.Column<bool>(nullable: false),
                Price = table.Column<decimal>("decimal(30,14)", nullable: false),
                UserID = table.Column<long>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: false),
                Comment = table.Column<string>(maxLength: 2000, nullable: true),
                Number = table.Column<string>(maxLength: 50, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ActProvidingService", x => x.ID);
                table.ForeignKey(
                    "FK_ActProvidingService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ColumnItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(nullable: true),
                CssClass = table.Column<string>(nullable: true),
                SQL = table.Column<string>(nullable: true),
                Order = table.Column<int>(nullable: false),
                Type = table.Column<int>(nullable: false),
                Template = table.Column<string>(nullable: true, defaultValue: ""),
                UserID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ColumnItem", x => x.ID);
                table.ForeignKey(
                    "FK_ColumnItem_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "CrossExchangeRateHistory",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                UpdatedByID = table.Column<long>(nullable: false),
                CrossExchangeRateID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CrossExchangeRateHistory", x => x.ID);
                table.ForeignKey(
                    "FK_CrossExchangeRateHistory_CrossExchangeRate_CrossExchangeRateID",
                    x => x.CrossExchangeRateID,
                    "CrossExchangeRate",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CrossExchangeRateHistory_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DataSyncOperation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OperationType = table.Column<int>(nullable: false),
                UserID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DataSyncOperation", x => x.ID);
                table.ForeignKey(
                    "FK_DataSyncOperation_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ExchangeRateHistory",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                ExchangeRateID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ExchangeRateHistory", x => x.ID);
                table.ForeignKey(
                    "FK_ExchangeRateHistory_ExchangeRate_ExchangeRateID",
                    x => x.ExchangeRateID,
                    "ExchangeRate",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ExchangeRateHistory_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "GovCrossExchangeRateHistory",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                UpdatedByID = table.Column<long>(nullable: false),
                GovCrossExchangeRateID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_GovCrossExchangeRateHistory", x => x.ID);
                table.ForeignKey(
                    "FK_GovCrossExchangeRateHistory_GovCrossExchangeRate_GovCrossExchangeRateID",
                    x => x.GovCrossExchangeRateID,
                    "GovCrossExchangeRate",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_GovCrossExchangeRateHistory_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "GovExchangeRateHistory",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("decimal(30,14)", nullable: false),
                GovExchangeRateID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_GovExchangeRateHistory", x => x.ID);
                table.ForeignKey(
                    "FK_GovExchangeRateHistory_GovExchangeRate_GovExchangeRateID",
                    x => x.GovExchangeRateID,
                    "GovExchangeRate",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_GovExchangeRateHistory_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "MisplacedSale",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                MisplacedSaleStatus = table.Column<int>(nullable: false),
                SaleID = table.Column<long>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                RetailClientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_MisplacedSale", x => x.ID);
                table.ForeignKey(
                    "FK_MisplacedSale_RetailClient_RetailClientID",
                    x => x.RetailClientID,
                    "RetailClient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MisplacedSale_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductSpecification",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 500, nullable: true),
                SpecificationCode = table.Column<string>(maxLength: 100, nullable: true),
                Locale = table.Column<string>(maxLength: 4, nullable: true),
                DutyPercent = table.Column<decimal>("money", nullable: false),
                IsActive = table.Column<bool>(nullable: false),
                AddedByID = table.Column<long>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                CustomsValue = table.Column<decimal>(nullable: false),
                Duty = table.Column<decimal>(nullable: false),
                VATValue = table.Column<decimal>(nullable: false),
                VATPercent = table.Column<decimal>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductSpecification", x => x.ID);
                table.ForeignKey(
                    "FK_ProductSpecification_User_AddedByID",
                    x => x.AddedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductSpecification_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductWriteOffRule",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                RuleLocale = table.Column<string>(maxLength: 4, nullable: true),
                RuleType = table.Column<int>(nullable: false),
                CreatedByID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                ProductID = table.Column<long>(nullable: true),
                ProductGroupID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductWriteOffRule", x => x.ID);
                table.ForeignKey(
                    "FK_ProductWriteOffRule_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductWriteOffRule_ProductGroup_ProductGroupID",
                    x => x.ProductGroupID,
                    "ProductGroup",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductWriteOffRule_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductWriteOffRule_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ShipmentList",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                IsSent = table.Column<bool>(nullable: false),
                TransporterID = table.Column<long>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ShipmentList", x => x.ID);
                table.ForeignKey(
                    "FK_ShipmentList_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ShipmentList_Transporter_TransporterID",
                    x => x.TransporterID,
                    "Transporter",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInformationTask",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                DeletedByID = table.Column<long>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInformationTask", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInformationTask_User_DeletedByID",
                    x => x.DeletedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInformationTask_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInformationTask_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyPaymentTask",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(nullable: true),
                TaskStatus = table.Column<int>(nullable: false),
                TaskStatusUpdated = table.Column<DateTime>(nullable: true),
                PayToDate = table.Column<DateTime>(nullable: true),
                UserID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                DeletedByID = table.Column<long>(nullable: true),
                IsAvailableForPayment = table.Column<bool>(nullable: false),
                TaskAssignedTo = table.Column<int>(nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                IsAccounting = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyPaymentTask", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyPaymentTask_User_DeletedByID",
                    x => x.DeletedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyPaymentTask_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyPaymentTask_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "UserScreenResolution",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Width = table.Column<int>(nullable: false),
                Height = table.Column<int>(nullable: false),
                UserID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_UserScreenResolution", x => x.ID);
                table.ForeignKey(
                    "FK_UserScreenResolution_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "PerfectClientValueTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Value = table.Column<string>(nullable: true),
                PerfectClientValueID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PerfectClientValueTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_PerfectClientValueTranslation_PerfectClientValue_PerfectClientValueID",
                    x => x.PerfectClientValueID,
                    "PerfectClientValue",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ClientContractDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                ClientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientContractDocument", x => x.ID);
                table.ForeignKey(
                    "FK_ClientContractDocument_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientInRole",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ClientID = table.Column<long>(nullable: false),
                ClientTypeID = table.Column<long>(nullable: false),
                ClientTypeRoleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientInRole", x => x.ID);
                table.ForeignKey(
                    "FK_ClientInRole_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientInRole_ClientType_ClientTypeID",
                    x => x.ClientTypeID,
                    "ClientType",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientInRole_ClientTypeRole_ClientTypeRoleID",
                    x => x.ClientTypeRoleID,
                    "ClientTypeRole",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientPerfectClient",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PerfectClientID = table.Column<long>(nullable: false),
                ClientID = table.Column<long>(nullable: false),
                PerfectClientValueID = table.Column<long>(nullable: true),
                Value = table.Column<string>(nullable: true),
                IsChecked = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientPerfectClient", x => x.ID);
                table.ForeignKey(
                    "FK_ClientPerfectClient_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ClientPerfectClient_PerfectClient_PerfectClientID",
                    x => x.PerfectClientID,
                    "PerfectClient",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ClientPerfectClient_PerfectClientValue_PerfectClientValueID",
                    x => x.PerfectClientValueID,
                    "PerfectClientValue",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientRegistrationTask",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsDone = table.Column<bool>(nullable: false),
                ClientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientRegistrationTask", x => x.ID);
                table.ForeignKey(
                    "FK_ClientRegistrationTask_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ClientSubClient",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                RootClientID = table.Column<long>(nullable: false),
                SubClientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientSubClient", x => x.ID);
                table.ForeignKey(
                    "FK_ClientSubClient_Client_RootClientID",
                    x => x.RootClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientSubClient_Client_SubClientID",
                    x => x.SubClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientUserProfile",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ClientID = table.Column<long>(nullable: false),
                UserProfileID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientUserProfile", x => x.ID);
                table.ForeignKey(
                    "FK_ClientUserProfile_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ClientUserProfile_User_UserProfileID",
                    x => x.UserProfileID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "DeliveryRecipient",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FullName = table.Column<string>(nullable: true),
                Priority = table.Column<int>(nullable: false),
                ClientID = table.Column<long>(nullable: false),
                MobilePhone = table.Column<string>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_DeliveryRecipient", x => x.ID);
                table.ForeignKey(
                    "FK_DeliveryRecipient_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PreOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(maxLength: 250, nullable: true),
                MobileNumber = table.Column<string>(maxLength: 25, nullable: true),
                Culture = table.Column<string>(maxLength: 4, nullable: true),
                Qty = table.Column<double>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                ClientID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PreOrder", x => x.ID);
                table.ForeignKey(
                    "FK_PreOrder_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PreOrder_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SaleReturn",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                ClientID = table.Column<long>(nullable: false),
                CreatedByID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                CanceledByID = table.Column<long>(nullable: true),
                IsCanceled = table.Column<bool>(nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleReturn", x => x.ID);
                table.ForeignKey(
                    "FK_SaleReturn_User_CanceledByID",
                    x => x.CanceledByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturn_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturn_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturn_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ServicePayer",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FirstName = table.Column<string>(nullable: true),
                LastName = table.Column<string>(nullable: true),
                MiddleName = table.Column<string>(nullable: true),
                MobilePhone = table.Column<string>(nullable: true),
                Comment = table.Column<string>(nullable: true),
                PaymentAddress = table.Column<string>(nullable: true),
                PaymentCard = table.Column<string>(nullable: true),
                ServiceType = table.Column<int>(nullable: false),
                ClientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ServicePayer", x => x.ID);
                table.ForeignKey(
                    "FK_ServicePayer_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ColumnItemTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(nullable: true),
                ColumnItemID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ColumnItemTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_ColumnItemTranslation_ColumnItem_ColumnItemID",
                    x => x.ColumnItemID,
                    "ColumnItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyPaymentTaskDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyPaymentTaskDocument", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyPaymentTaskDocument_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DeliveryRecipientAddress",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Value = table.Column<string>(maxLength: 500, nullable: true),
                Department = table.Column<string>(maxLength: 250, nullable: true),
                City = table.Column<string>(maxLength: 250, nullable: true),
                Priority = table.Column<int>(nullable: false),
                DeliveryRecipientID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DeliveryRecipientAddress", x => x.ID);
                table.ForeignKey(
                    "FK_DeliveryRecipientAddress_DeliveryRecipient_DeliveryRecipientID",
                    x => x.DeliveryRecipientID,
                    "DeliveryRecipient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "BillOfLadingService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceID = table.Column<long>(nullable: true),
                AccountingActProvidingServiceID = table.Column<long>(nullable: true),
                LoadDate = table.Column<DateTime>(nullable: true),
                BillOfLadingNumber = table.Column<string>(nullable: true),
                TermDeliveryInDays = table.Column<string>(nullable: true),
                IsAutoCalculatedValue = table.Column<bool>(nullable: false),
                IsShipped = table.Column<bool>(nullable: false),
                SupplyOrganizationID = table.Column<long>(nullable: false),
                SupplyExtraChargeType = table.Column<int>(nullable: false, defaultValueSql: "0"),
                IsCalculatedValue = table.Column<bool>(nullable: false),
                DeliveryProductProtocolID = table.Column<long>(nullable: false),
                TypeBillOfLadingService = table.Column<int>(nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_BillOfLadingService", x => x.ID);
                table.ForeignKey(
                    "FK_BillOfLadingService_ActProvidingService_AccountingActProvidingServiceID",
                    x => x.AccountingActProvidingServiceID,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_BillOfLadingService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_BillOfLadingService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_BillOfLadingService_ActProvidingService_ActProvidingServiceID",
                    x => x.ActProvidingServiceID,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_BillOfLadingService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_BillOfLadingService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_BillOfLadingService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_BillOfLadingService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "BillOfLadingDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                Number = table.Column<string>(nullable: true),
                Date = table.Column<DateTime>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                BillOfLadingServiceID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_BillOfLadingDocument", x => x.ID);
                table.ForeignKey(
                    "FK_BillOfLadingDocument_BillOfLadingService_BillOfLadingServiceID",
                    x => x.BillOfLadingServiceID,
                    "BillOfLadingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ContainerService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                LoadDate = table.Column<DateTime>(nullable: false),
                GroosWeight = table.Column<double>(nullable: false),
                ContainerNumber = table.Column<string>(nullable: true),
                TermDeliveryInDays = table.Column<string>(nullable: true),
                BillOfLadingDocumentID = table.Column<long>(nullable: true),
                ContainerOrganizationID = table.Column<long>(nullable: true),
                IsCalculatedExtraCharge = table.Column<bool>(nullable: false),
                SupplyExtraChargeType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ContainerService", x => x.ID);
                table.ForeignKey(
                    "FK_ContainerService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_BillOfLadingDocument_BillOfLadingDocumentID",
                    x => x.BillOfLadingDocumentID,
                    "BillOfLadingDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ContainerService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CustomAgencyService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                CustomAgencyOrganizationID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_CustomAgencyService", x => x.ID);
                table.ForeignKey(
                    "FK_CustomAgencyService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomAgencyService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomAgencyService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomAgencyService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomAgencyService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomAgencyService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomAgencyService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomAgencyService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CustomService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                CustomOrganizationID = table.Column<long>(nullable: true),
                ExciseDutyOrganizationID = table.Column<long>(nullable: true),
                SupplyOrderID = table.Column<long>(nullable: false),
                SupplyCustomType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CustomService", x => x.ID);
                table.ForeignKey(
                    "FK_CustomService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CustomService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "MergedService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceID = table.Column<long>(nullable: true),
                AccountingActProvidingServiceID = table.Column<long>(nullable: true),
                SupplyOrganizationID = table.Column<long>(nullable: false),
                SupplyOrderID = table.Column<long>(nullable: true),
                SupplyOrderUkraineID = table.Column<long>(nullable: true),
                SupplyExtraChargeType = table.Column<int>(nullable: false, defaultValueSql: "0"),
                IsCalculatedValue = table.Column<bool>(nullable: false),
                IsAutoCalculatedValue = table.Column<bool>(nullable: false),
                ConsumableProductID = table.Column<long>(nullable: true),
                DeliveryProductProtocolID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_MergedService", x => x.ID);
                table.ForeignKey(
                    "FK_MergedService_ActProvidingService_AccountingActProvidingServiceID",
                    x => x.AccountingActProvidingServiceID,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_ActProvidingService_ActProvidingServiceID",
                    x => x.ActProvidingServiceID,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_ConsumableProduct_ConsumableProductID",
                    x => x.ConsumableProductID,
                    "ConsumableProduct",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_MergedService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PlaneDeliveryService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                PlaneDeliveryOrganizationID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PlaneDeliveryService", x => x.ID);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PlaneDeliveryService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PortCustomAgencyService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                PortCustomAgencyOrganizationID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PortCustomAgencyService", x => x.ID);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortCustomAgencyService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PortWorkService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                PortWorkOrganizationID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PortWorkService", x => x.ID);
                table.ForeignKey(
                    "FK_PortWorkService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortWorkService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortWorkService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortWorkService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortWorkService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortWorkService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortWorkService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PortWorkService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TransportationService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                IsSealAndSignatureVerified = table.Column<bool>(nullable: false),
                TransportationOrganizationID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TransportationService", x => x.ID);
                table.ForeignKey(
                    "FK_TransportationService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TransportationService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TransportationService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TransportationService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TransportationService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TransportationService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TransportationService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TransportationService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "VehicleDeliveryService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                IsSealAndSignatureVerified = table.Column<bool>(nullable: false),
                VehicleDeliveryOrganizationID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_VehicleDeliveryService", x => x.ID);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleDeliveryService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ServiceDetailItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                UnitPrice = table.Column<decimal>(nullable: false),
                NetPrice = table.Column<decimal>(nullable: false),
                GrossPrice = table.Column<decimal>(nullable: false),
                Vat = table.Column<decimal>(nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                ServiceDetailItemKeyID = table.Column<long>(nullable: false),
                CustomAgencyServiceID = table.Column<long>(nullable: true),
                CustomServiceID = table.Column<long>(nullable: true),
                PlaneDeliveryServiceID = table.Column<long>(nullable: true),
                PortCustomAgencyServiceID = table.Column<long>(nullable: true),
                PortWorkServiceID = table.Column<long>(nullable: true),
                TransportationServiceID = table.Column<long>(nullable: true),
                VehicleDeliveryServiceID = table.Column<long>(nullable: true),
                MergedServiceID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ServiceDetailItem", x => x.ID);
                table.ForeignKey(
                    "FK_ServiceDetailItem_CustomAgencyService_CustomAgencyServiceID",
                    x => x.CustomAgencyServiceID,
                    "CustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_CustomService_CustomServiceID",
                    x => x.CustomServiceID,
                    "CustomService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_MergedService_MergedServiceID",
                    x => x.MergedServiceID,
                    "MergedService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_PlaneDeliveryService_PlaneDeliveryServiceID",
                    x => x.PlaneDeliveryServiceID,
                    "PlaneDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_PortCustomAgencyService_PortCustomAgencyServiceID",
                    x => x.PortCustomAgencyServiceID,
                    "PortCustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_PortWorkService_PortWorkServiceID",
                    x => x.PortWorkServiceID,
                    "PortWorkService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_ServiceDetailItemKey_ServiceDetailItemKeyID",
                    x => x.ServiceDetailItemKeyID,
                    "ServiceDetailItemKey",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_TransportationService_TransportationServiceID",
                    x => x.TransportationServiceID,
                    "TransportationService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ServiceDetailItem_VehicleDeliveryService_VehicleDeliveryServiceID",
                    x => x.VehicleDeliveryServiceID,
                    "VehicleDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "VehicleService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsActive = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                AccountingGrossPrice = table.Column<decimal>("money", nullable: false),
                AccountingSupplyCostsWithinCountry = table.Column<decimal>("money", nullable: false),
                AccountingNetPrice = table.Column<decimal>("money", nullable: false),
                AccountingVat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                AccountingVatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Name = table.Column<string>(nullable: true),
                UserID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                AccountingPaymentTaskID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: false),
                SupplyInformationTaskID = table.Column<long>(nullable: true),
                ExchangeRate = table.Column<decimal>("money", nullable: true),
                AccountingExchangeRate = table.Column<decimal>("money", nullable: true),
                IsIncludeAccountingValue = table.Column<bool>(nullable: false),
                ActProvidingServiceDocumentID = table.Column<long>(nullable: true),
                SupplyServiceAccountDocumentID = table.Column<long>(nullable: true),
                ActProvidingServiceId = table.Column<long>(nullable: true),
                AccountingActProvidingServiceId = table.Column<long>(nullable: true),
                LoadDate = table.Column<DateTime>(nullable: false),
                GrossWeight = table.Column<double>(nullable: false),
                VehicleNumber = table.Column<string>(nullable: true),
                TermDeliveryInDays = table.Column<string>(nullable: true),
                BillOfLadingDocumentID = table.Column<long>(nullable: true),
                VehicleOrganizationID = table.Column<long>(nullable: true),
                IsCalculatedExtraCharge = table.Column<bool>(nullable: false),
                SupplyExtraChargeType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_VehicleService", x => x.ID);
                table.ForeignKey(
                    "FK_VehicleService_ActProvidingService_AccountingActProvidingServiceId",
                    x => x.AccountingActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_SupplyPaymentTask_AccountingPaymentTaskID",
                    x => x.AccountingPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_ActProvidingServiceDocument_ActProvidingServiceDocumentID",
                    x => x.ActProvidingServiceDocumentID,
                    "ActProvidingServiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_ActProvidingService_ActProvidingServiceId",
                    x => x.ActProvidingServiceId,
                    "ActProvidingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_BillOfLadingDocument_BillOfLadingDocumentID",
                    x => x.BillOfLadingDocumentID,
                    "BillOfLadingDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_SupplyInformationTask_SupplyInformationTaskID",
                    x => x.SupplyInformationTaskID,
                    "SupplyInformationTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_SupplyServiceAccountDocument_SupplyServiceAccountDocumentID",
                    x => x.SupplyServiceAccountDocumentID,
                    "SupplyServiceAccountDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_VehicleService_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ActReconciliationItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                HasDifference = table.Column<bool>(nullable: false),
                NegativeDifference = table.Column<bool>(nullable: false),
                OrderedQty = table.Column<double>(nullable: false),
                ActualQty = table.Column<double>(nullable: false),
                QtyDifference = table.Column<double>(nullable: false),
                NetWeight = table.Column<double>(nullable: false),
                UnitPrice = table.Column<decimal>("money", nullable: false),
                CommentUA = table.Column<string>(maxLength: 500, nullable: true),
                CommentPL = table.Column<string>(maxLength: 500, nullable: true),
                ProductID = table.Column<long>(nullable: false),
                ActReconciliationID = table.Column<long>(nullable: false),
                SupplyOrderUkraineItemID = table.Column<long>(nullable: true),
                SupplyInvoiceOrderItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ActReconciliationItem", x => x.ID);
                table.ForeignKey(
                    "FK_ActReconciliationItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DepreciatedOrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                Reason = table.Column<string>(maxLength: 150, nullable: true),
                ProductID = table.Column<long>(nullable: false),
                DepreciatedOrderID = table.Column<long>(nullable: false),
                ActReconciliationItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_DepreciatedOrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_DepreciatedOrderItem_ActReconciliationItem_ActReconciliationItemID",
                    x => x.ActReconciliationItemID,
                    "ActReconciliationItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DepreciatedOrderItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductIncomeItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                RemainingQty = table.Column<double>(nullable: false),
                SupplyOrderUkraineItemID = table.Column<long>(nullable: true),
                PackingListPackageOrderItemID = table.Column<long>(nullable: true),
                SaleReturnItemID = table.Column<long>(nullable: true),
                ActReconciliationItemID = table.Column<long>(nullable: true),
                ProductCapitalizationItemID = table.Column<long>(nullable: true),
                ProductIncomeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductIncomeItem", x => x.ID);
                table.ForeignKey(
                    "FK_ProductIncomeItem_ActReconciliationItem_ActReconciliationItemID",
                    x => x.ActReconciliationItemID,
                    "ActReconciliationItem",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ProductTransferItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                Reason = table.Column<string>(maxLength: 150, nullable: true),
                ProductID = table.Column<long>(nullable: false),
                ProductTransferID = table.Column<long>(nullable: false),
                ActReconciliationItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductTransferItem", x => x.ID);
                table.ForeignKey(
                    "FK_ProductTransferItem_ActReconciliationItem_ActReconciliationItemID",
                    x => x.ActReconciliationItemID,
                    "ActReconciliationItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductTransferItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientAgreement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OriginalClientCode = table.Column<long>(nullable: false),
                ClientID = table.Column<long>(nullable: false),
                AgreementID = table.Column<long>(nullable: false),
                ProductReservationTerm = table.Column<int>(nullable: false),
                CurrentAmount = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientAgreement", x => x.ID);
                table.ForeignKey(
                    "FK_ClientAgreement_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ClientBalanceMovement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("money", nullable: false),
                ExchangeRateAmount = table.Column<decimal>("money", nullable: false),
                MovementType = table.Column<int>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientBalanceMovement", x => x.ID);
                table.ForeignKey(
                    "FK_ClientBalanceMovement_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientShoppingCart",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                ValidUntil = table.Column<DateTime>(nullable: false),
                IsOfferProcessed = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsOffer = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsVatCart = table.Column<bool>(nullable: false),
                OfferProcessingStatus = table.Column<int>(nullable: false),
                OfferProcessingStatusChangedByID = table.Column<long>(nullable: true),
                CreatedByID = table.Column<long>(nullable: true),
                ClientAgreementID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientShoppingCart", x => x.ID);
                table.ForeignKey(
                    "FK_ClientShoppingCart_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientShoppingCart_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientShoppingCart_User_OfferProcessingStatusChangedByID",
                    x => x.OfferProcessingStatusChangedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductGroupDiscount",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ClientAgreementID = table.Column<long>(nullable: false),
                ProductGroupID = table.Column<long>(nullable: false),
                IsActive = table.Column<bool>(nullable: false, defaultValueSql: "1"),
                DiscountRate = table.Column<double>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductGroupDiscount", x => x.ID);
                table.ForeignKey(
                    "FK_ProductGroupDiscount_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductGroupDiscount_ProductGroup_ProductGroupID",
                    x => x.ProductGroupID,
                    "ProductGroup",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Order",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OrderSource = table.Column<int>(nullable: false),
                OrderStatus = table.Column<int>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false),
                ClientShoppingCartID = table.Column<long>(nullable: true),
                IsMerged = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_Order", x => x.ID);
                table.ForeignKey(
                    "FK_Order_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Order_ClientShoppingCart_ClientShoppingCartID",
                    x => x.ClientShoppingCartID,
                    "ClientShoppingCart",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Order_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                UnpackedQty = table.Column<double>(nullable: false),
                OrderedQty = table.Column<double>(nullable: false),
                FromOfferQty = table.Column<double>(nullable: false),
                ReturnedQty = table.Column<double>(nullable: false),
                OneTimeDiscount = table.Column<decimal>("money", nullable: false),
                PricePerItem = table.Column<decimal>("decimal(30,14)", nullable: false),
                PricePerItemWithoutVat = table.Column<decimal>("decimal(30,14)", nullable: false),
                ExchangeRateAmount = table.Column<decimal>("money", nullable: false),
                DiscountAmount = table.Column<decimal>("money", nullable: false),
                OneTimeDiscountComment = table.Column<string>(maxLength: 450, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                IsValidForCurrentSale = table.Column<bool>(nullable: false, defaultValueSql: "1"),
                IsFromOffer = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsFromReSale = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OfferProcessingStatus = table.Column<int>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                OrderID = table.Column<long>(nullable: true),
                OfferProcessingStatusChangedByID = table.Column<long>(nullable: true),
                DiscountUpdatedByID = table.Column<long>(nullable: true),
                UserId = table.Column<long>(nullable: true),
                ClientShoppingCartID = table.Column<long>(nullable: true),
                AssignedSpecificationID = table.Column<long>(nullable: true),
                MisplacedSaleId = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_OrderItem_ProductSpecification_AssignedSpecificationID",
                    x => x.AssignedSpecificationID,
                    "ProductSpecification",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItem_ClientShoppingCart_ClientShoppingCartID",
                    x => x.ClientShoppingCartID,
                    "ClientShoppingCart",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItem_User_DiscountUpdatedByID",
                    x => x.DiscountUpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItem_MisplacedSale_MisplacedSaleId",
                    x => x.MisplacedSaleId,
                    "MisplacedSale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItem_User_OfferProcessingStatusChangedByID",
                    x => x.OfferProcessingStatusChangedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItem_Order_OrderID",
                    x => x.OrderID,
                    "Order",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_OrderItem_User_UserId",
                    x => x.UserId,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrderPackage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CBM = table.Column<double>(nullable: false),
                Width = table.Column<int>(nullable: false),
                Height = table.Column<int>(nullable: false),
                Lenght = table.Column<int>(nullable: false),
                Weight = table.Column<double>(nullable: false),
                OrderID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderPackage", x => x.ID);
                table.ForeignKey(
                    "FK_OrderPackage_Order_OrderID",
                    x => x.OrderID,
                    "Order",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrderItemMerged",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OldOrderID = table.Column<long>(nullable: false),
                OrderItemID = table.Column<long>(nullable: false),
                OldOrderItemID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderItemMerged", x => x.ID);
                table.ForeignKey(
                    "FK_OrderItemMerged_Order_OldOrderID",
                    x => x.OldOrderID,
                    "Order",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItemMerged_OrderItem_OldOrderItemID",
                    x => x.OldOrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItemMerged_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrderItemMovement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                OrderItemID = table.Column<long>(nullable: false),
                MovementType = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderItemMovement", x => x.ID);
                table.ForeignKey(
                    "FK_OrderItemMovement_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_OrderItemMovement_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "OrderPackageItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                OrderItemID = table.Column<long>(nullable: false),
                OrderPackageID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderPackageItem", x => x.ID);
                table.ForeignKey(
                    "FK_OrderPackageItem_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderPackageItem_OrderPackage_OrderPackageID",
                    x => x.OrderPackageID,
                    "OrderPackage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrderPackageUser",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                UserID = table.Column<long>(nullable: false),
                OrderPackageID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderPackageUser", x => x.ID);
                table.ForeignKey(
                    "FK_OrderPackageUser_OrderPackage_OrderPackageID",
                    x => x.OrderPackageID,
                    "OrderPackage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderPackageUser_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ClientInDebt",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ClientID = table.Column<long>(nullable: false),
                AgreementID = table.Column<long>(nullable: false),
                DebtID = table.Column<long>(nullable: false),
                SaleID = table.Column<long>(nullable: true),
                ReSaleID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientInDebt", x => x.ID);
                table.ForeignKey(
                    "FK_ClientInDebt_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientInDebt_Debt_DebtID",
                    x => x.DebtID,
                    "Debt",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Agreement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 100, nullable: true),
                DeferredPayment = table.Column<string>(nullable: true),
                TermsOfPayment = table.Column<string>(nullable: true),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                IsManagementAccounting = table.Column<bool>(nullable: false),
                IsAccounting = table.Column<bool>(nullable: false),
                IsSelected = table.Column<bool>(nullable: false),
                WithVATAccounting = table.Column<bool>(nullable: false),
                IsControlAmountDebt = table.Column<bool>(nullable: false),
                IsControlNumberDaysDebt = table.Column<bool>(nullable: false),
                IsPrePaymentFull = table.Column<bool>(nullable: false),
                IsPrePayment = table.Column<bool>(nullable: false),
                IsActive = table.Column<bool>(nullable: false),
                IsDefault = table.Column<bool>(nullable: false),
                AmountDebt = table.Column<decimal>("money", nullable: false),
                PrePaymentPercentages = table.Column<double>(nullable: false),
                NumberDaysDebt = table.Column<int>(nullable: false),
                CurrencyID = table.Column<long>(nullable: true),
                OrganizationID = table.Column<long>(nullable: true),
                PricingID = table.Column<long>(nullable: true),
                ProviderPricingID = table.Column<long>(nullable: true),
                TaxAccountingSchemeID = table.Column<long>(nullable: true),
                AgreementTypeCivilCodeID = table.Column<long>(nullable: true),
                PromotionalPricingID = table.Column<long>(nullable: true),
                ForReSale = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: true),
                ToDate = table.Column<DateTime>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Agreement", x => x.ID);
                table.ForeignKey(
                    "FK_Agreement_AgreementTypeCivilCode_AgreementTypeCivilCodeID",
                    x => x.AgreementTypeCivilCodeID,
                    "AgreementTypeCivilCode",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Agreement_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Agreement_Pricing_PricingID",
                    x => x.PricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Agreement_Pricing_PromotionalPricingID",
                    x => x.PromotionalPricingID,
                    "Pricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Agreement_ProviderPricing_ProviderPricingID",
                    x => x.ProviderPricingID,
                    "ProviderPricing",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Agreement_TaxAccountingScheme_TaxAccountingSchemeID",
                    x => x.TaxAccountingSchemeID,
                    "TaxAccountingScheme",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrganizationAgreement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 150, nullable: true),
                CurrentAmount = table.Column<decimal>("money", nullable: false),
                AccountingCurrentAmount = table.Column<decimal>("money", nullable: false, defaultValueSql: "0.00"),
                SupplyOrganizationID = table.Column<long>(nullable: false),
                CurrencyID = table.Column<long>(nullable: false),
                TaxAccountingSchemeID = table.Column<long>(nullable: true),
                AgreementTypeCivilCodeID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrganizationAgreement", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrganizationAgreement_AgreementTypeCivilCode_AgreementTypeCivilCodeID",
                    x => x.AgreementTypeCivilCodeID,
                    "AgreementTypeCivilCode",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrganizationAgreement_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_SupplyOrganizationAgreement_TaxAccountingScheme_TaxAccountingSchemeID",
                    x => x.TaxAccountingSchemeID,
                    "TaxAccountingScheme",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ReSale",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(maxLength: 250, nullable: true),
                ClientAgreementID = table.Column<long>(nullable: true),
                ChangedToInvoiceByID = table.Column<long>(nullable: true),
                OrganizationID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                SaleNumberID = table.Column<long>(nullable: true),
                BaseLifeCycleStatusID = table.Column<long>(nullable: false),
                BaseSalePaymentStatusID = table.Column<long>(nullable: false),
                FromStorageID = table.Column<long>(nullable: false),
                IsConsignmentActive = table.Column<bool>(nullable: false),
                ChangedToInvoice = table.Column<DateTime>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ReSale", x => x.ID);
                table.ForeignKey(
                    "FK_ReSale_BaseLifeCycleStatus_BaseLifeCycleStatusID",
                    x => x.BaseLifeCycleStatusID,
                    "BaseLifeCycleStatus",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSale_BaseSalePaymentStatus_BaseSalePaymentStatusID",
                    x => x.BaseSalePaymentStatusID,
                    "BaseSalePaymentStatus",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSale_User_ChangedToInvoiceByID",
                    x => x.ChangedToInvoiceByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSale_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSale_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Sale",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OrderID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false),
                BaseLifeCycleStatusID = table.Column<long>(nullable: false),
                BaseSalePaymentStatusID = table.Column<long>(nullable: false),
                TaxFreePackListID = table.Column<long>(nullable: true),
                SadID = table.Column<long>(nullable: true),
                DeliveryRecipientAddressID = table.Column<long>(nullable: true),
                DeliveryRecipientID = table.Column<long>(nullable: true),
                TransporterID = table.Column<long>(nullable: true),
                SaleNumberID = table.Column<long>(nullable: true),
                ShiftStatusID = table.Column<long>(nullable: true),
                SaleInvoiceDocumentID = table.Column<long>(nullable: true),
                SaleInvoiceNumberID = table.Column<long>(nullable: true),
                ChangedToInvoiceByID = table.Column<long>(nullable: true),
                RetailClientId = table.Column<long>(nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                OneTimeDiscountComment = table.Column<string>(maxLength: 450, nullable: true),
                IsMerged = table.Column<bool>(nullable: false),
                IsPrinted = table.Column<bool>(nullable: false),
                IsPrintedPaymentInvoice = table.Column<bool>(nullable: false),
                IsCashOnDelivery = table.Column<bool>(nullable: false),
                HasDocuments = table.Column<bool>(nullable: false),
                IsVatSale = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsLocked = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsPaymentBillDownloaded = table.Column<bool>(nullable: false),
                IsFullPayment = table.Column<bool>(nullable: false),
                IsImported = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ExpiredDays = table.Column<double>(nullable: false, defaultValueSql: "0.00"),
                TTN = table.Column<string>(nullable: true),
                ShippingAmount = table.Column<decimal>("money", nullable: false),
                ShippingAmountEur = table.Column<decimal>("money", nullable: false),
                CashOnDeliveryAmount = table.Column<decimal>("money", nullable: false),
                IsAcceptedToPacking = table.Column<bool>(nullable: false),
                MisplacedSaleId = table.Column<long>(nullable: true),
                ParentNetId = table.Column<Guid>(nullable: true),
                ChangedToInvoice = table.Column<DateTime>(nullable: true),
                ShipmentDate = table.Column<DateTime>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Sale", x => x.ID);
                table.ForeignKey(
                    "FK_Sale_BaseLifeCycleStatus_BaseLifeCycleStatusID",
                    x => x.BaseLifeCycleStatusID,
                    "BaseLifeCycleStatus",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_BaseSalePaymentStatus_BaseSalePaymentStatusID",
                    x => x.BaseSalePaymentStatusID,
                    "BaseSalePaymentStatus",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_User_ChangedToInvoiceByID",
                    x => x.ChangedToInvoiceByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_DeliveryRecipientAddress_DeliveryRecipientAddressID",
                    x => x.DeliveryRecipientAddressID,
                    "DeliveryRecipientAddress",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_DeliveryRecipient_DeliveryRecipientID",
                    x => x.DeliveryRecipientID,
                    "DeliveryRecipient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_MisplacedSale_MisplacedSaleId",
                    x => x.MisplacedSaleId,
                    "MisplacedSale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_Order_OrderID",
                    x => x.OrderID,
                    "Order",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_RetailClient_RetailClientId",
                    x => x.RetailClientId,
                    "RetailClient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_SaleInvoiceDocument_SaleInvoiceDocumentID",
                    x => x.SaleInvoiceDocumentID,
                    "SaleInvoiceDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_SaleInvoiceNumber_SaleInvoiceNumberID",
                    x => x.SaleInvoiceNumberID,
                    "SaleInvoiceNumber",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_SaleBaseShiftStatus_ShiftStatusID",
                    x => x.ShiftStatusID,
                    "SaleBaseShiftStatus",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_Transporter_TransporterID",
                    x => x.TransporterID,
                    "Transporter",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sale_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CountSaleMessage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SaleID = table.Column<long>(nullable: false),
                SaleMessageNumeratorID = table.Column<long>(nullable: false),
                Transfered = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CountSaleMessage", x => x.ID);
                table.ForeignKey(
                    "FK_CountSaleMessage_Sale_SaleID",
                    x => x.SaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_CountSaleMessage_SaleMessageNumerator_SaleMessageNumeratorID",
                    x => x.SaleMessageNumeratorID,
                    "SaleMessageNumerator",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ExpiredBillUserNotification",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                UserNotificationType = table.Column<int>(nullable: false),
                Locked = table.Column<bool>(nullable: false),
                Processed = table.Column<bool>(nullable: false),
                CreatedByID = table.Column<long>(nullable: false),
                LockedByID = table.Column<long>(nullable: true),
                LastViewedByID = table.Column<long>(nullable: true),
                ProcessedByID = table.Column<long>(nullable: true),
                AppliedAction = table.Column<int>(nullable: false),
                SaleNumber = table.Column<string>(maxLength: 50, nullable: true),
                FromClient = table.Column<string>(maxLength: 250, nullable: true),
                ExpiredDays = table.Column<double>(nullable: false),
                Deferred = table.Column<bool>(nullable: false),
                SaleID = table.Column<long>(nullable: false),
                ManagerID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ExpiredBillUserNotification", x => x.ID);
                table.ForeignKey(
                    "FK_ExpiredBillUserNotification_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ExpiredBillUserNotification_User_LastViewedByID",
                    x => x.LastViewedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ExpiredBillUserNotification_User_LockedByID",
                    x => x.LockedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ExpiredBillUserNotification_User_ManagerID",
                    x => x.ManagerID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ExpiredBillUserNotification_User_ProcessedByID",
                    x => x.ProcessedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ExpiredBillUserNotification_Sale_SaleID",
                    x => x.SaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrderItemBaseShiftStatus",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ShiftStatus = table.Column<int>(nullable: false),
                Comment = table.Column<string>(nullable: true),
                Qty = table.Column<double>(nullable: false),
                OrderItemID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                SaleID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderItemBaseShiftStatus", x => x.ID);
                table.ForeignKey(
                    "FK_OrderItemBaseShiftStatus_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItemBaseShiftStatus_Sale_SaleID",
                    x => x.SaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderItemBaseShiftStatus_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SaleExchangeRate",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SaleID = table.Column<long>(nullable: false),
                ExchangeRateID = table.Column<long>(nullable: false),
                Value = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleExchangeRate", x => x.ID);
                table.ForeignKey(
                    "FK_SaleExchangeRate_ExchangeRate_ExchangeRateID",
                    x => x.ExchangeRateID,
                    "ExchangeRate",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleExchangeRate_Sale_SaleID",
                    x => x.SaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SaleMerged",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OutputSaleID = table.Column<long>(nullable: false),
                InputSaleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleMerged", x => x.ID);
                table.ForeignKey(
                    "FK_SaleMerged_Sale_InputSaleID",
                    x => x.InputSaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleMerged_Sale_OutputSaleID",
                    x => x.OutputSaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ShipmentListItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                QtyPlaces = table.Column<double>(nullable: false),
                SaleID = table.Column<long>(nullable: false),
                ShipmentListID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ShipmentListItem", x => x.ID);
                table.ForeignKey(
                    "FK_ShipmentListItem_Sale_SaleID",
                    x => x.SaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ShipmentListItem_ShipmentList_ShipmentListID",
                    x => x.ShipmentListID,
                    "ShipmentList",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PackingListPackageOrderItemSupplyService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                NetValue = table.Column<decimal>("decimal(30,14)", nullable: false),
                GeneralValue = table.Column<decimal>("decimal(30,14)", nullable: false),
                ManagementValue = table.Column<decimal>("decimal(30,14)", nullable: false),
                Name = table.Column<string>(maxLength: 250, nullable: true),
                ExchangeRateDate = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                PackingListPackageOrderItemID = table.Column<long>(nullable: false),
                CurrencyID = table.Column<long>(nullable: false),
                BillOfLadingServiceID = table.Column<long>(nullable: true),
                ContainerServiceID = table.Column<long>(nullable: true),
                CustomAgencyServiceID = table.Column<long>(nullable: true),
                CustomServiceID = table.Column<long>(nullable: true),
                MergedServiceID = table.Column<long>(nullable: true),
                PlaneDeliveryServiceID = table.Column<long>(nullable: true),
                PortCustomAgencyServiceID = table.Column<long>(nullable: true),
                PortWorkServiceID = table.Column<long>(nullable: true),
                TransportationServiceID = table.Column<long>(nullable: true),
                VehicleDeliveryServiceID = table.Column<long>(nullable: true),
                VehicleServiceID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PackingListPackageOrderItemSupplyService", x => x.ID);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_BillOfLadingService_BillOfLadingServiceID",
                    x => x.BillOfLadingServiceID,
                    "BillOfLadingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_ContainerService_ContainerServiceID",
                    x => x.ContainerServiceID,
                    "ContainerService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_CustomAgencyService_CustomAgencyServiceID",
                    x => x.CustomAgencyServiceID,
                    "CustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_CustomService_CustomServiceID",
                    x => x.CustomServiceID,
                    "CustomService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_MergedService_MergedServiceID",
                    x => x.MergedServiceID,
                    "MergedService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_PlaneDeliveryService_PlaneDeliveryServiceID",
                    x => x.PlaneDeliveryServiceID,
                    "PlaneDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_PortCustomAgencyService_PortCustomAgencyServiceID",
                    x => x.PortCustomAgencyServiceID,
                    "PortCustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_PortWorkService_PortWorkServiceID",
                    x => x.PortWorkServiceID,
                    "PortWorkService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_TransportationService_TransportationServiceID",
                    x => x.TransportationServiceID,
                    "TransportationService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_VehicleDeliveryService_VehicleDeliveryServiceID",
                    x => x.VehicleDeliveryServiceID,
                    "VehicleDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItemSupplyService_VehicleService_VehicleServiceID",
                    x => x.VehicleServiceID,
                    "VehicleService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInvoiceBillOfLadingService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SupplyInvoiceID = table.Column<long>(nullable: false),
                BillOfLadingServiceID = table.Column<long>(nullable: false),
                Value = table.Column<decimal>("decimal(30,14)", nullable: false),
                AccountingValue = table.Column<decimal>("decimal(30,14)", nullable: false),
                IsCalculatedValue = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInvoiceBillOfLadingService", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInvoiceBillOfLadingService_BillOfLadingService_BillOfLadingServiceID",
                    x => x.BillOfLadingServiceID,
                    "BillOfLadingService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "IncomePaymentOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                BankAccount = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                IncomePaymentOrderType = table.Column<int>(nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                VAT = table.Column<decimal>("money", nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                ExchangeRate = table.Column<decimal>("money", nullable: false),
                AgreementEuroExchangeRate = table.Column<decimal>("money", nullable: false),
                AfterExchangeAmount = table.Column<decimal>("money", nullable: false),
                OverpaidAmount = table.Column<decimal>("money", nullable: false),
                IsManagementAccounting = table.Column<bool>(nullable: false),
                IsAccounting = table.Column<bool>(nullable: false),
                IsCanceled = table.Column<bool>(nullable: false),
                Account = table.Column<int>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                CurrencyID = table.Column<long>(nullable: false),
                PaymentRegisterID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                ClientID = table.Column<long>(nullable: true),
                ClientAgreementID = table.Column<long>(nullable: true),
                ColleagueID = table.Column<long>(nullable: true),
                OrganizationClientID = table.Column<long>(nullable: true),
                OrganizationClientAgreementID = table.Column<long>(nullable: true),
                TaxFreeID = table.Column<long>(nullable: true),
                SadID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_IncomePaymentOrder", x => x.ID);
                table.ForeignKey(
                    "FK_IncomePaymentOrder_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrder_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrder_User_ColleagueID",
                    x => x.ColleagueID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrder_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrder_OrganizationClientAgreement_OrganizationClientAgreementID",
                    x => x.OrganizationClientAgreementID,
                    "OrganizationClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrder_OrganizationClient_OrganizationClientID",
                    x => x.OrganizationClientID,
                    "OrganizationClient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrder_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "IncomePaymentOrderSale",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SaleID = table.Column<long>(nullable: true),
                ReSaleID = table.Column<long>(nullable: true),
                IncomePaymentOrderID = table.Column<long>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                OverpaidAmount = table.Column<decimal>("money", nullable: false),
                ExchangeRate = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_IncomePaymentOrderSale", x => x.ID);
                table.ForeignKey(
                    "FK_IncomePaymentOrderSale_IncomePaymentOrder_IncomePaymentOrderID",
                    x => x.IncomePaymentOrderID,
                    "IncomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrderSale_ReSale_ReSaleID",
                    x => x.ReSaleID,
                    "ReSale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_IncomePaymentOrderSale_Sale_SaleID",
                    x => x.SaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Sad",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                MarginAmount = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<decimal>("money", nullable: false),
                SadType = table.Column<int>(nullable: false),
                IsSend = table.Column<bool>(nullable: false),
                IsFromSale = table.Column<bool>(nullable: false),
                StathamID = table.Column<long>(nullable: true),
                StathamCarID = table.Column<long>(nullable: true),
                StathamPassportID = table.Column<long>(nullable: true),
                OrganizationID = table.Column<long>(nullable: true),
                SupplyOrderUkraineID = table.Column<long>(nullable: true),
                OrganizationClientID = table.Column<long>(nullable: true),
                OrganizationClientAgreementID = table.Column<long>(nullable: true),
                ClientID = table.Column<long>(nullable: true),
                ClientAgreementID = table.Column<long>(nullable: true),
                ResponsibleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_Sad", x => x.ID);
                table.ForeignKey(
                    "FK_Sad_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sad_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sad_OrganizationClientAgreement_OrganizationClientAgreementID",
                    x => x.OrganizationClientAgreementID,
                    "OrganizationClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sad_OrganizationClient_OrganizationClientID",
                    x => x.OrganizationClientID,
                    "OrganizationClient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sad_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sad_StathamCar_StathamCarID",
                    x => x.StathamCarID,
                    "StathamCar",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sad_Statham_StathamID",
                    x => x.StathamID,
                    "Statham",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Sad_StathamPassport_StathamPassportID",
                    x => x.StathamPassportID,
                    "StathamPassport",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SadDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(maxLength: 250, nullable: true),
                FileName = table.Column<string>(maxLength: 250, nullable: true),
                ContentType = table.Column<string>(maxLength: 250, nullable: true),
                GeneratedName = table.Column<string>(maxLength: 250, nullable: true),
                SadID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SadDocument", x => x.ID);
                table.ForeignKey(
                    "FK_SadDocument_Sad_SadID",
                    x => x.SadID,
                    "Sad",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SadPallet",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 250, nullable: true),
                SadID = table.Column<long>(nullable: false),
                SadPalletTypeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SadPallet", x => x.ID);
                table.ForeignKey(
                    "FK_SadPallet_Sad_SadID",
                    x => x.SadID,
                    "Sad",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SadPallet_SadPalletType_SadPalletTypeID",
                    x => x.SadPalletTypeID,
                    "SadPalletType",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SadItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                UnpackedQty = table.Column<double>(nullable: false),
                NetWeight = table.Column<double>(nullable: false),
                UnitPrice = table.Column<decimal>("money", nullable: false),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                SadID = table.Column<long>(nullable: false),
                SupplyOrderUkraineCartItemID = table.Column<long>(nullable: true),
                OrderItemID = table.Column<long>(nullable: true),
                SupplierID = table.Column<long>(nullable: true),
                ConsignmentItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SadItem", x => x.ID);
                table.ForeignKey(
                    "FK_SadItem_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SadItem_Sad_SadID",
                    x => x.SadID,
                    "Sad",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SadItem_Client_SupplierID",
                    x => x.SupplierID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SadPalletItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                SadItemID = table.Column<long>(nullable: false),
                SadPalletID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SadPalletItem", x => x.ID);
                table.ForeignKey(
                    "FK_SadPalletItem_SadItem_SadItemID",
                    x => x.SadItemID,
                    "SadItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SadPalletItem_SadPallet_SadPalletID",
                    x => x.SadPalletID,
                    "SadPallet",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SaleFutureReservation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ProductID = table.Column<long>(nullable: false),
                ClientID = table.Column<long>(nullable: false),
                SupplyOrderID = table.Column<long>(nullable: false),
                RemindDate = table.Column<DateTime>(nullable: false),
                Count = table.Column<double>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleFutureReservation", x => x.ID);
                table.ForeignKey(
                    "FK_SaleFutureReservation_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleFutureReservation_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ClientID = table.Column<long>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                SupplyOrderNumberID = table.Column<long>(nullable: false),
                SupplyProFormID = table.Column<long>(nullable: true),
                PortWorkServiceID = table.Column<long>(nullable: true),
                TransportationServiceID = table.Column<long>(nullable: true),
                CustomAgencyServiceID = table.Column<long>(nullable: true),
                PortCustomAgencyServiceID = table.Column<long>(nullable: true),
                PlaneDeliveryServiceID = table.Column<long>(nullable: true),
                VehicleDeliveryServiceID = table.Column<long>(nullable: true),
                AdditionalPaymentCurrencyID = table.Column<long>(nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                Qty = table.Column<double>(nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                AdditionalAmount = table.Column<decimal>("money", nullable: false),
                AdditionalPercent = table.Column<double>(nullable: false),
                IsDocumentSet = table.Column<bool>(nullable: false),
                IsCompleted = table.Column<bool>(nullable: false),
                IsOrderShipped = table.Column<bool>(nullable: false),
                IsOrderArrived = table.Column<bool>(nullable: false),
                IsPlaced = table.Column<bool>(nullable: false),
                IsPartiallyPlaced = table.Column<bool>(nullable: false),
                IsFullyPlaced = table.Column<bool>(nullable: false),
                IsGrossPricesCalculated = table.Column<bool>(nullable: false),
                IsOrderInsidePoland = table.Column<bool>(nullable: false),
                IsApproved = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                TransportationType = table.Column<int>(nullable: false),
                OrderShippedDate = table.Column<DateTime>(nullable: true),
                DateFrom = table.Column<DateTime>(nullable: true),
                CompleteDate = table.Column<DateTime>(nullable: true),
                ShipArrived = table.Column<DateTime>(nullable: true),
                VechicalArrived = table.Column<DateTime>(nullable: true),
                PlaneArrived = table.Column<DateTime>(nullable: true),
                OrderArrivedDate = table.Column<DateTime>(nullable: true),
                AdditionalPaymentFromDate = table.Column<DateTime>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrder", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrder_Currency_AdditionalPaymentCurrencyID",
                    x => x.AdditionalPaymentCurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_CustomAgencyService_CustomAgencyServiceID",
                    x => x.CustomAgencyServiceID,
                    "CustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_PlaneDeliveryService_PlaneDeliveryServiceID",
                    x => x.PlaneDeliveryServiceID,
                    "PlaneDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_PortCustomAgencyService_PortCustomAgencyServiceID",
                    x => x.PortCustomAgencyServiceID,
                    "PortCustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_PortWorkService_PortWorkServiceID",
                    x => x.PortWorkServiceID,
                    "PortWorkService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_SupplyOrderNumber_SupplyOrderNumberID",
                    x => x.SupplyOrderNumberID,
                    "SupplyOrderNumber",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_SupplyOrder_SupplyProForm_SupplyProFormID",
                    x => x.SupplyProFormID,
                    "SupplyProForm",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_TransportationService_TransportationServiceID",
                    x => x.TransportationServiceID,
                    "TransportationService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrder_VehicleDeliveryService_VehicleDeliveryServiceID",
                    x => x.VehicleDeliveryServiceID,
                    "VehicleDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CreditNoteDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                SupplyOrderID = table.Column<long>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                Number = table.Column<string>(nullable: true),
                Comment = table.Column<string>(nullable: true),
                FromDate = table.Column<DateTime>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CreditNoteDocument", x => x.ID);
                table.ForeignKey(
                    "FK_CreditNoteDocument_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PackingListDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                SupplyOrderID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PackingListDocument", x => x.ID);
                table.ForeignKey(
                    "FK_PackingListDocument_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ResponsibilityDeliveryProtocol",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SupplyOrderStatus = table.Column<int>(nullable: false),
                UserId = table.Column<long>(nullable: false),
                SupplyOrderID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ResponsibilityDeliveryProtocol", x => x.ID);
                table.ForeignKey(
                    "FK_ResponsibilityDeliveryProtocol_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ResponsibilityDeliveryProtocol_User_UserId",
                    x => x.UserId,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderContainerService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SupplyOrderID = table.Column<long>(nullable: false),
                ContainerServiceID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderContainerService", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderContainerService_ContainerService_ContainerServiceID",
                    x => x.ContainerServiceID,
                    "ContainerService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderContainerService_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderDeliveryDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                IsNotified = table.Column<bool>(nullable: false),
                IsReceived = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsProcessed = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(nullable: true),
                ProcessedDate = table.Column<DateTime>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                SupplyOrderID = table.Column<long>(nullable: false),
                SupplyDeliveryDocumentID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderDeliveryDocument", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderDeliveryDocument_SupplyDeliveryDocument_SupplyDeliveryDocumentID",
                    x => x.SupplyDeliveryDocumentID,
                    "SupplyDeliveryDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderDeliveryDocument_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderDeliveryDocument_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                StockNo = table.Column<string>(nullable: true),
                ItemNo = table.Column<string>(nullable: true),
                Qty = table.Column<double>(nullable: false),
                UnitPrice = table.Column<decimal>("money", nullable: false),
                TotalAmount = table.Column<decimal>("money", nullable: false),
                GrossWeight = table.Column<double>(nullable: false),
                NetWeight = table.Column<double>(nullable: false),
                Description = table.Column<string>(nullable: true),
                SupplyOrderID = table.Column<long>(nullable: true),
                IsPacked = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderItem_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderPolandPaymentDeliveryProtocol",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                UserID = table.Column<long>(nullable: false),
                SupplyPaymentTaskID = table.Column<long>(nullable: false),
                SupplyOrderID = table.Column<long>(nullable: false),
                SupplyOrderPaymentDeliveryProtocolKeyID = table.Column<long>(nullable: false),
                Name = table.Column<string>(nullable: true),
                Number = table.Column<string>(nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                GrossPrice = table.Column<decimal>("money", nullable: false),
                NetPrice = table.Column<decimal>("money", nullable: false),
                Vat = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                IsAccounting = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderPolandPaymentDeliveryProtocol", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderPolandPaymentDeliveryProtocol_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderPolandPaymentDeliveryProtocol_SupplyOrderPaymentDeliveryProtocolKey_SupplyOrderPaymentDeliveryProtocolKeyID",
                    x => x.SupplyOrderPaymentDeliveryProtocolKeyID,
                    "SupplyOrderPaymentDeliveryProtocolKey",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_SupplyOrderPolandPaymentDeliveryProtocol_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderPolandPaymentDeliveryProtocol_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderVehicleService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SupplyOrderID = table.Column<long>(nullable: false),
                VehicleServiceID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderVehicleService", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderVehicleService_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderVehicleService_VehicleService_VehicleServiceID",
                    x => x.VehicleServiceID,
                    "VehicleService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkraine",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                InvDate = table.Column<DateTime>(nullable: false),
                AdditionalPaymentFromDate = table.Column<DateTime>(nullable: true),
                IsPlaced = table.Column<bool>(nullable: false),
                IsPartialPlaced = table.Column<bool>(nullable: false),
                IsDirectFromSupplier = table.Column<bool>(nullable: false),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                InvNumber = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                ShipmentAmount = table.Column<decimal>("decimal(30,14)", nullable: false),
                ShipmentAmountLocal = table.Column<decimal>("decimal(30,14)", nullable: false),
                AdditionalAmount = table.Column<decimal>("money", nullable: false),
                AdditionalPercent = table.Column<double>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                SupplierID = table.Column<long>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false),
                AdditionalPaymentCurrencyID = table.Column<long>(nullable: true),
                VatPercent = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkraine", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderUkraine_Currency_AdditionalPaymentCurrencyID",
                    x => x.AdditionalPaymentCurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraine_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraine_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraine_Client_SupplierID",
                    x => x.SupplierID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkraineDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(maxLength: 500, nullable: true),
                FileName = table.Column<string>(maxLength: 500, nullable: true),
                ContentType = table.Column<string>(maxLength: 500, nullable: true),
                GeneratedName = table.Column<string>(maxLength: 500, nullable: true),
                SupplyOrderUkraineID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkraineDocument", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineDocument_SupplyOrderUkraine_SupplyOrderUkraineID",
                    x => x.SupplyOrderUkraineID,
                    "SupplyOrderUkraine",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkrainePaymentDeliveryProtocol",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Value = table.Column<decimal>("money", nullable: false),
                Discount = table.Column<double>(nullable: false),
                SupplyOrderUkrainePaymentDeliveryProtocolKeyID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                SupplyOrderUkraineID = table.Column<long>(nullable: false),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                IsAccounting = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkrainePaymentDeliveryProtocol", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderUkrainePaymentDeliveryProtocol_SupplyOrderUkraine_SupplyOrderUkraineID",
                    x => x.SupplyOrderUkraineID,
                    "SupplyOrderUkraine",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkrainePaymentDeliveryProtocol_SupplyOrderUkrainePaymentDeliveryProtocolKey_SupplyOrderUkrainePaymentDeliveryProt~",
                    x => x.SupplyOrderUkrainePaymentDeliveryProtocolKeyID,
                    "SupplyOrderUkrainePaymentDeliveryProtocolKey",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkrainePaymentDeliveryProtocol_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkrainePaymentDeliveryProtocol_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkraineCartItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                UploadedQty = table.Column<double>(nullable: false),
                ReservedQty = table.Column<double>(nullable: false),
                UnpackedQty = table.Column<double>(nullable: false),
                NetWeight = table.Column<double>(nullable: false),
                MaxQtyPerTF = table.Column<int>(nullable: false),
                UnitPrice = table.Column<decimal>("money", nullable: false),
                ItemPriority = table.Column<int>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: false),
                IsRecommended = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ProductID = table.Column<long>(nullable: false),
                CreatedByID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                ResponsibleID = table.Column<long>(nullable: true),
                TaxFreePackListID = table.Column<long>(nullable: true),
                SupplierID = table.Column<long>(nullable: true),
                PackingListPackageOrderItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkraineCartItem", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItem_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItem_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItem_Client_SupplierID",
                    x => x.SupplierID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItem_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkraineItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsFullyPlaced = table.Column<bool>(nullable: false),
                NotOrdered = table.Column<bool>(nullable: false),
                Qty = table.Column<double>(nullable: false),
                PlacedQty = table.Column<double>(nullable: false),
                RemainingQty = table.Column<double>(nullable: false),
                NetWeight = table.Column<double>(nullable: false),
                GrossWeight = table.Column<double>(nullable: false),
                UnitPrice = table.Column<decimal>("decimal(30,14)", nullable: false),
                GrossUnitPrice = table.Column<decimal>("decimal(30,14)", nullable: false),
                AccountingGrossUnitPrice = table.Column<decimal>("decimal(30,14)", nullable: false),
                AccountingGrossUnitPriceLocal = table.Column<decimal>("decimal(30,14)", nullable: false),
                UnitPriceLocal = table.Column<decimal>("decimal(30,14)", nullable: false),
                GrossUnitPriceLocal = table.Column<decimal>("decimal(30,14)", nullable: false),
                ExchangeRateAmount = table.Column<decimal>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                SupplyOrderUkraineID = table.Column<long>(nullable: false),
                SupplierID = table.Column<long>(nullable: true),
                ConsignmentItemID = table.Column<long>(nullable: true),
                PackingListPackageOrderItemID = table.Column<long>(nullable: true),
                ProductSpecificationID = table.Column<long>(nullable: true),
                VatPercent = table.Column<decimal>("money", nullable: false),
                VatAmount = table.Column<decimal>("decimal(30,14)", nullable: false),
                VatAmountLocal = table.Column<decimal>("decimal(30,14)", nullable: false),
                UnitDeliveryAmount = table.Column<decimal>("decimal(30,14)", nullable: false),
                UnitDeliveryAmountLocal = table.Column<decimal>("decimal(30,14)", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkraineItem", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineItem_ProductSpecification_ProductSpecificationID",
                    x => x.ProductSpecificationID,
                    "ProductSpecification",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineItem_Client_SupplierID",
                    x => x.SupplierID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineItem_SupplyOrderUkraine_SupplyOrderUkraineID",
                    x => x.SupplyOrderUkraineID,
                    "SupplyOrderUkraine",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyReturn",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                SupplierID = table.Column<long>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                StorageID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyReturn", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyReturn_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyReturn_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyReturn_Client_SupplierID",
                    x => x.SupplierID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TaxFreePackList",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                WeightLimit = table.Column<double>(nullable: false),
                MarginAmount = table.Column<decimal>("money", nullable: false),
                MaxPriceLimit = table.Column<decimal>(nullable: false),
                MinPriceLimit = table.Column<decimal>(nullable: false),
                MaxQtyInTaxFree = table.Column<int>(nullable: false),
                MaxPositionsInTaxFree = table.Column<int>(nullable: false),
                IsSent = table.Column<bool>(nullable: false),
                IsFromSale = table.Column<bool>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: true),
                SupplyOrderUkraineID = table.Column<long>(nullable: true),
                ClientID = table.Column<long>(nullable: true),
                ClientAgreementID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TaxFreePackList", x => x.ID);
                table.ForeignKey(
                    "FK_TaxFreePackList_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFreePackList_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFreePackList_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFreePackList_SupplyOrderUkraine_SupplyOrderUkraineID",
                    x => x.SupplyOrderUkraineID,
                    "SupplyOrderUkraine",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TaxFree",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                CustomCode = table.Column<string>(maxLength: 150, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                AmountPayedStatham = table.Column<decimal>("money", nullable: false),
                AmountInPLN = table.Column<decimal>("money", nullable: false),
                VatAmountInPLN = table.Column<decimal>("money", nullable: false),
                AmountInEur = table.Column<decimal>("money", nullable: false),
                MarginAmount = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<decimal>("money", nullable: false),
                Weight = table.Column<double>(nullable: false),
                TaxFreeStatus = table.Column<int>(nullable: false),
                DateOfPrint = table.Column<DateTime>(nullable: true),
                DateOfIssue = table.Column<DateTime>(nullable: true),
                DateOfStathamPayment = table.Column<DateTime>(nullable: true),
                DateOfTabulation = table.Column<DateTime>(nullable: true),
                FormedDate = table.Column<DateTime>(nullable: true),
                SelectedDate = table.Column<DateTime>(nullable: true),
                ReturnedDate = table.Column<DateTime>(nullable: true),
                ClosedDate = table.Column<DateTime>(nullable: true),
                CanceledDate = table.Column<DateTime>(nullable: true),
                StathamID = table.Column<long>(nullable: true),
                StathamCarID = table.Column<long>(nullable: true),
                StathamPassportID = table.Column<long>(nullable: true),
                TaxFreePackListID = table.Column<long>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_TaxFree", x => x.ID);
                table.ForeignKey(
                    "FK_TaxFree_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFree_StathamCar_StathamCarID",
                    x => x.StathamCarID,
                    "StathamCar",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFree_Statham_StathamID",
                    x => x.StathamID,
                    "Statham",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFree_StathamPassport_StathamPassportID",
                    x => x.StathamPassportID,
                    "StathamPassport",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFree_TaxFreePackList_TaxFreePackListID",
                    x => x.TaxFreePackListID,
                    "TaxFreePackList",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TaxFreeDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(maxLength: 250, nullable: true),
                FileName = table.Column<string>(maxLength: 250, nullable: true),
                ContentType = table.Column<string>(maxLength: 250, nullable: true),
                GeneratedName = table.Column<string>(maxLength: 250, nullable: true),
                TaxFreeID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_TaxFreeDocument", x => x.ID);
                table.ForeignKey(
                    "FK_TaxFreeDocument_TaxFree_TaxFreeID",
                    x => x.TaxFreeID,
                    "TaxFree",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "AdvancePayment",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                VatAmount = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                UserID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                TaxFreeID = table.Column<long>(nullable: true),
                SadID = table.Column<long>(nullable: true),
                ClientAgreementID = table.Column<long>(nullable: true),
                OrganizationClientAgreementID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_AdvancePayment", x => x.ID);
                table.ForeignKey(
                    "FK_AdvancePayment_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_AdvancePayment_OrganizationClientAgreement_OrganizationClientAgreementID",
                    x => x.OrganizationClientAgreementID,
                    "OrganizationClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_AdvancePayment_Sad_SadID",
                    x => x.SadID,
                    "Sad",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_AdvancePayment_TaxFree_TaxFreeID",
                    x => x.TaxFreeID,
                    "TaxFree",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_AdvancePayment_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OutcomePaymentOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                AdvanceNumber = table.Column<string>(maxLength: 6, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                AfterExchangeAmount = table.Column<decimal>("money", nullable: false),
                ExchangeRate = table.Column<decimal>("money", nullable: false),
                VAT = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                Account = table.Column<int>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                PaymentCurrencyRegisterID = table.Column<long>(nullable: false),
                ColleagueID = table.Column<long>(nullable: true),
                ConsumableProductOrganizationID = table.Column<long>(nullable: true),
                ClientAgreementID = table.Column<long>(nullable: true),
                SupplyOrderPolandPaymentDeliveryProtocolID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: true),
                OrganizationClientID = table.Column<long>(nullable: true),
                OrganizationClientAgreementID = table.Column<long>(nullable: true),
                TaxFreeID = table.Column<long>(nullable: true),
                SadID = table.Column<long>(nullable: true),
                IsUnderReport = table.Column<bool>(nullable: false),
                IsUnderReportDone = table.Column<bool>(nullable: false),
                IsCanceled = table.Column<bool>(nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_OutcomePaymentOrder", x => x.ID);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_User_ColleagueID",
                    x => x.ColleagueID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_OrganizationClientAgreement_OrganizationClientAgreementID",
                    x => x.OrganizationClientAgreementID,
                    "OrganizationClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_OrganizationClient_OrganizationClientID",
                    x => x.OrganizationClientID,
                    "OrganizationClient",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_Sad_SadID",
                    x => x.SadID,
                    "Sad",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_SupplyOrderPolandPaymentDeliveryProtocol_SupplyOrderPolandPaymentDeliveryProtocolID",
                    x => x.SupplyOrderPolandPaymentDeliveryProtocolID,
                    "SupplyOrderPolandPaymentDeliveryProtocol",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
                    x => x.SupplyOrganizationAgreementID,
                    "SupplyOrganizationAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_TaxFree_TaxFreeID",
                    x => x.TaxFreeID,
                    "TaxFree",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrder_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "AssignedPaymentOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                RootOutcomePaymentOrderID = table.Column<long>(nullable: true),
                RootIncomePaymentOrderID = table.Column<long>(nullable: true),
                AssignedOutcomePaymentOrderID = table.Column<long>(nullable: true),
                AssignedIncomePaymentOrderID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_AssignedPaymentOrder", x => x.ID);
                table.ForeignKey(
                    "FK_AssignedPaymentOrder_IncomePaymentOrder_AssignedIncomePaymentOrderID",
                    x => x.AssignedIncomePaymentOrderID,
                    "IncomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_AssignedPaymentOrder_OutcomePaymentOrder_AssignedOutcomePaymentOrderID",
                    x => x.AssignedOutcomePaymentOrderID,
                    "OutcomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_AssignedPaymentOrder_IncomePaymentOrder_RootIncomePaymentOrderID",
                    x => x.RootIncomePaymentOrderID,
                    "IncomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_AssignedPaymentOrder_OutcomePaymentOrder_RootOutcomePaymentOrderID",
                    x => x.RootOutcomePaymentOrderID,
                    "OutcomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OutcomePaymentOrderSupplyPaymentTask",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("money", nullable: false),
                OutcomePaymentOrderID = table.Column<long>(nullable: false),
                SupplyPaymentTaskID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OutcomePaymentOrderSupplyPaymentTask", x => x.ID);
                table.ForeignKey(
                    "FK_OutcomePaymentOrderSupplyPaymentTask_OutcomePaymentOrder_OutcomePaymentOrderID",
                    x => x.OutcomePaymentOrderID,
                    "OutcomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OutcomePaymentOrderSupplyPaymentTask_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CompanyCarFueling",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FuelAmount = table.Column<double>(nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                PricePerLiter = table.Column<decimal>("money", nullable: false),
                TotalPrice = table.Column<decimal>("money", nullable: false),
                TotalPriceWithVat = table.Column<decimal>("money", nullable: false),
                VatAmount = table.Column<decimal>("money", nullable: false),
                CompanyCarID = table.Column<long>(nullable: false),
                OutcomePaymentOrderID = table.Column<long>(nullable: false),
                ConsumableProductOrganizationID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CompanyCarFueling", x => x.ID);
                table.ForeignKey(
                    "FK_CompanyCarFueling_OutcomePaymentOrder_OutcomePaymentOrderID",
                    x => x.OutcomePaymentOrderID,
                    "OutcomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CompanyCarFueling_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CompanyCarRoadList",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Comment = table.Column<string>(maxLength: 150, nullable: true),
                FuelAmount = table.Column<double>(nullable: false),
                Mileage = table.Column<long>(nullable: false),
                TotalKilometers = table.Column<int>(nullable: false),
                InCityKilometers = table.Column<int>(nullable: false),
                OutsideCityKilometers = table.Column<int>(nullable: false),
                MixedModeKilometers = table.Column<int>(nullable: false),
                CompanyCarID = table.Column<long>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                CreatedByID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                OutcomePaymentOrderID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_CompanyCarRoadList", x => x.ID);
                table.ForeignKey(
                    "FK_CompanyCarRoadList_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CompanyCarRoadList_OutcomePaymentOrder_OutcomePaymentOrderID",
                    x => x.OutcomePaymentOrderID,
                    "OutcomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CompanyCarRoadList_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CompanyCarRoadList_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CompanyCarRoadListDriver",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CompanyCarRoadListID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CompanyCarRoadListDriver", x => x.ID);
                table.ForeignKey(
                    "FK_CompanyCarRoadListDriver_CompanyCarRoadList_CompanyCarRoadListID",
                    x => x.CompanyCarRoadListID,
                    "CompanyCarRoadList",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CompanyCarRoadListDriver_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentCostMovementOperation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PaymentCostMovementID = table.Column<long>(nullable: false),
                ConsumablesOrderItemID = table.Column<long>(nullable: true),
                DepreciatedConsumableOrderItemID = table.Column<long>(nullable: true),
                CompanyCarFuelingID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentCostMovementOperation", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentCostMovementOperation_CompanyCarFueling_CompanyCarFuelingID",
                    x => x.CompanyCarFuelingID,
                    "CompanyCarFueling",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentCostMovementOperation_PaymentCostMovement_PaymentCostMovementID",
                    x => x.PaymentCostMovementID,
                    "PaymentCostMovement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ConsignmentItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                RemainingQty = table.Column<double>(nullable: false),
                Weight = table.Column<double>(nullable: false),
                Price = table.Column<decimal>("decimal(30,14)", nullable: false),
                NetPrice = table.Column<decimal>("decimal(30,14)", nullable: false),
                AccountingPrice = table.Column<decimal>("decimal(30,14)", nullable: false),
                DutyPercent = table.Column<decimal>("money", nullable: false),
                ProductID = table.Column<long>(nullable: false),
                ConsignmentID = table.Column<long>(nullable: false),
                RootConsignmentItemID = table.Column<long>(nullable: true),
                ProductIncomeItemID = table.Column<long>(nullable: false),
                ProductSpecificationID = table.Column<long>(nullable: false),
                ExchangeRate = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsignmentItem", x => x.ID);
                table.ForeignKey(
                    "FK_ConsignmentItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItem_ProductIncomeItem_ProductIncomeItemID",
                    x => x.ProductIncomeItemID,
                    "ProductIncomeItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItem_ProductSpecification_ProductSpecificationID",
                    x => x.ProductSpecificationID,
                    "ProductSpecification",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItem_ConsignmentItem_RootConsignmentItemID",
                    x => x.RootConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyReturnItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                ProductID = table.Column<long>(nullable: false),
                SupplyReturnID = table.Column<long>(nullable: false),
                ConsignmentItemID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyReturnItem", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyReturnItem_ConsignmentItem_ConsignmentItemID",
                    x => x.ConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyReturnItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyReturnItem_SupplyReturn_SupplyReturnID",
                    x => x.SupplyReturnID,
                    "SupplyReturn",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TaxFreePackListOrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                NetWeight = table.Column<double>(nullable: false),
                Qty = table.Column<double>(nullable: false),
                UnpackedQty = table.Column<double>(nullable: false),
                OrderItemID = table.Column<long>(nullable: false),
                TaxFreePackListID = table.Column<long>(nullable: false),
                ConsignmentItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TaxFreePackListOrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_TaxFreePackListOrderItem_ConsignmentItem_ConsignmentItemID",
                    x => x.ConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFreePackListOrderItem_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFreePackListOrderItem_TaxFreePackList_TaxFreePackListID",
                    x => x.TaxFreePackListID,
                    "TaxFreePackList",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "TaxFreeItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                TaxFreeID = table.Column<long>(nullable: false),
                SupplyOrderUkraineCartItemID = table.Column<long>(nullable: true),
                TaxFreePackListOrderItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_TaxFreeItem", x => x.ID);
                table.ForeignKey(
                    "FK_TaxFreeItem_SupplyOrderUkraineCartItem_SupplyOrderUkraineCartItemID",
                    x => x.SupplyOrderUkraineCartItemID,
                    "SupplyOrderUkraineCartItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFreeItem_TaxFree_TaxFreeID",
                    x => x.TaxFreeID,
                    "TaxFree",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_TaxFreeItem_TaxFreePackListOrderItem_TaxFreePackListOrderItemID",
                    x => x.TaxFreePackListOrderItemID,
                    "TaxFreePackListOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ConsignmentItemMovement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsIncomeMovement = table.Column<bool>(nullable: false),
                Qty = table.Column<double>(nullable: false),
                RemainingQty = table.Column<double>(nullable: false),
                MovementType = table.Column<int>(nullable: false),
                ConsignmentItemID = table.Column<long>(nullable: false),
                ProductIncomeItemID = table.Column<long>(nullable: true),
                DepreciatedOrderItemID = table.Column<long>(nullable: true),
                SupplyReturnItemID = table.Column<long>(nullable: true),
                OrderItemID = table.Column<long>(nullable: true),
                ReSaleItemId = table.Column<long>(nullable: true),
                ProductTransferItemID = table.Column<long>(nullable: true),
                OrderItemBaseShiftStatusID = table.Column<long>(nullable: true),
                TaxFreeItemID = table.Column<long>(nullable: true),
                SadItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsignmentItemMovement", x => x.ID);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_ConsignmentItem_ConsignmentItemID",
                    x => x.ConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_DepreciatedOrderItem_DepreciatedOrderItemID",
                    x => x.DepreciatedOrderItemID,
                    "DepreciatedOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_OrderItemBaseShiftStatus_OrderItemBaseShiftStatusID",
                    x => x.OrderItemBaseShiftStatusID,
                    "OrderItemBaseShiftStatus",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_ProductIncomeItem_ProductIncomeItemID",
                    x => x.ProductIncomeItemID,
                    "ProductIncomeItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_ProductTransferItem_ProductTransferItemID",
                    x => x.ProductTransferItemID,
                    "ProductTransferItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_SadItem_SadItemID",
                    x => x.SadItemID,
                    "SadItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_SupplyReturnItem_SupplyReturnItemID",
                    x => x.SupplyReturnItemID,
                    "SupplyReturnItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsignmentItemMovement_TaxFreeItem_TaxFreeItemID",
                    x => x.TaxFreeItemID,
                    "TaxFreeItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductPlacement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                StorageNumber = table.Column<string>(maxLength: 5, nullable: true),
                RowNumber = table.Column<string>(maxLength: 5, nullable: true),
                CellNumber = table.Column<string>(maxLength: 5, nullable: true),
                ProductID = table.Column<long>(nullable: false),
                StorageID = table.Column<long>(nullable: false),
                PackingListPackageOrderItemID = table.Column<long>(nullable: true),
                SupplyOrderUkraineItemID = table.Column<long>(nullable: true),
                ProductIncomeItemID = table.Column<long>(nullable: true),
                ConsignmentItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductPlacement", x => x.ID);
                table.ForeignKey(
                    "FK_ProductPlacement_ConsignmentItem_ConsignmentItemID",
                    x => x.ConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductPlacement_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductPlacement_ProductIncomeItem_ProductIncomeItemID",
                    x => x.ProductIncomeItemID,
                    "ProductIncomeItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductPlacement_SupplyOrderUkraineItem_SupplyOrderUkraineItemID",
                    x => x.SupplyOrderUkraineItemID,
                    "SupplyOrderUkraineItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductPlacementMovement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromProductPlacementID = table.Column<long>(nullable: false),
                ToProductPlacementID = table.Column<long>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductPlacementMovement", x => x.ID);
                table.ForeignKey(
                    "FK_ProductPlacementMovement_ProductPlacement_FromProductPlacementID",
                    x => x.FromProductPlacementID,
                    "ProductPlacement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductPlacementMovement_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductPlacementMovement_ProductPlacement_ToProductPlacementID",
                    x => x.ToProductPlacementID,
                    "ProductPlacement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductReservation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                OrderItemID = table.Column<long>(nullable: false),
                ProductAvailabilityID = table.Column<long>(nullable: false),
                ConsignmentItemID = table.Column<long>(nullable: true),
                IsReSaleReservation = table.Column<bool>(nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductReservation", x => x.ID);
                table.ForeignKey(
                    "FK_ProductReservation_ConsignmentItem_ConsignmentItemID",
                    x => x.ConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductReservation_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ReSaleAvailability",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                RemainingQty = table.Column<double>(nullable: false),
                ConsignmentItemID = table.Column<long>(nullable: false),
                ProductAvailabilityID = table.Column<long>(nullable: false),
                OrderItemID = table.Column<long>(nullable: true),
                ProductTransferItemID = table.Column<long>(nullable: true),
                DepreciatedOrderItemID = table.Column<long>(nullable: true),
                ProductReservationID = table.Column<long>(nullable: true),
                PricePerItem = table.Column<decimal>("decimal(30,14)", nullable: false),
                ExchangeRate = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ReSaleAvailability", x => x.ID);
                table.ForeignKey(
                    "FK_ReSaleAvailability_ConsignmentItem_ConsignmentItemID",
                    x => x.ConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSaleAvailability_DepreciatedOrderItem_DepreciatedOrderItemID",
                    x => x.DepreciatedOrderItemID,
                    "DepreciatedOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSaleAvailability_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSaleAvailability_ProductReservation_ProductReservationID",
                    x => x.ProductReservationID,
                    "ProductReservation",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSaleAvailability_ProductTransferItem_ProductTransferItemID",
                    x => x.ProductTransferItemID,
                    "ProductTransferItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ReSaleItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                ReSaleID = table.Column<long>(nullable: false),
                PricePerItem = table.Column<decimal>("decimal(30,14)", nullable: false),
                ExtraCharge = table.Column<decimal>(nullable: false),
                ExchangeRate = table.Column<decimal>("money", nullable: false),
                ReSaleAvailabilityID = table.Column<long>(nullable: true),
                ProductID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ReSaleItem", x => x.ID);
                table.ForeignKey(
                    "FK_ReSaleItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSaleItem_ReSaleAvailability_ReSaleAvailabilityID",
                    x => x.ReSaleAvailabilityID,
                    "ReSaleAvailability",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ReSaleItem_ReSale_ReSaleID",
                    x => x.ReSaleID,
                    "ReSale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkraineCartItemReservation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                ProductAvailabilityID = table.Column<long>(nullable: false),
                SupplyOrderUkraineCartItemID = table.Column<long>(nullable: false),
                ConsignmentItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkraineCartItemReservation", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItemReservation_ConsignmentItem_ConsignmentItemID",
                    x => x.ConsignmentItemID,
                    "ConsignmentItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItemReservation_SupplyOrderUkraineCartItem_SupplyOrderUkraineCartItemID",
                    x => x.SupplyOrderUkraineCartItemID,
                    "SupplyOrderUkraineCartItem",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderUkraineCartItemReservationProductPlacement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                ProductPlacementID = table.Column<long>(nullable: false),
                SupplyOrderUkraineCartItemReservationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderUkraineCartItemReservationProductPlacement", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItemReservationProductPlacement_ProductPlacement_ProductPlacementID",
                    x => x.ProductPlacementID,
                    "ProductPlacement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderUkraineCartItemReservationProductPlacement_SupplyOrderUkraineCartItemReservation_SupplyOrderUkraineCartItemReserv~",
                    x => x.SupplyOrderUkraineCartItemReservationID,
                    "SupplyOrderUkraineCartItemReservation",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ConsumablesOrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                TotalPrice = table.Column<decimal>("money", nullable: false),
                PricePerItem = table.Column<decimal>("money", nullable: false),
                VAT = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<double>(nullable: false),
                Qty = table.Column<double>(nullable: false),
                IsService = table.Column<bool>(nullable: false),
                ConsumableProductCategoryID = table.Column<long>(nullable: false),
                ConsumablesOrderID = table.Column<long>(nullable: false),
                ConsumableProductID = table.Column<long>(nullable: true),
                ConsumableProductOrganizationID = table.Column<long>(nullable: true),
                SupplyOrganizationAgreementID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumablesOrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_ConsumablesOrderItem_ConsumableProductCategory_ConsumableProductCategoryID",
                    x => x.ConsumableProductCategoryID,
                    "ConsumableProductCategory",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsumablesOrderItem_ConsumableProduct_ConsumableProductID",
                    x => x.ConsumableProductID,
                    "ConsumableProduct",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsumablesOrderItem_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
                    x => x.SupplyOrganizationAgreementID,
                    "SupplyOrganizationAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ConsumablesOrderDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                ConsumablesOrderID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumablesOrderDocument", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "OutcomePaymentOrderConsumablesOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OutcomePaymentOrderID = table.Column<long>(nullable: false),
                ConsumablesOrderID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OutcomePaymentOrderConsumablesOrder", x => x.ID);
                table.ForeignKey(
                    "FK_OutcomePaymentOrderConsumablesOrder_OutcomePaymentOrder_OutcomePaymentOrderID",
                    x => x.OutcomePaymentOrderID,
                    "OutcomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DepreciatedConsumableOrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                DepreciatedConsumableOrderID = table.Column<long>(nullable: false),
                ConsumablesOrderItemID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DepreciatedConsumableOrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_DepreciatedConsumableOrderItem_ConsumablesOrderItem_ConsumablesOrderItemID",
                    x => x.ConsumablesOrderItemID,
                    "ConsumablesOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "CompanyCar",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                LicensePlate = table.Column<string>(maxLength: 20, nullable: true),
                CarBrand = table.Column<string>(maxLength: 100, nullable: true),
                TankCapacity = table.Column<double>(nullable: false),
                FuelAmount = table.Column<double>(nullable: false),
                InCityConsumption = table.Column<double>(nullable: false),
                OutsideCityConsumption = table.Column<double>(nullable: false),
                MixedModeConsumption = table.Column<double>(nullable: false),
                InitialMileage = table.Column<long>(nullable: false),
                Mileage = table.Column<long>(nullable: false),
                CreatedByID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                ConsumablesStorageID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_CompanyCar", x => x.ID);
                table.ForeignKey(
                    "FK_CompanyCar_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_CompanyCar_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ConsumablesOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                OrganizationNumber = table.Column<string>(maxLength: 50, nullable: true),
                IsPayed = table.Column<bool>(nullable: false),
                OrganizationFromDate = table.Column<DateTime>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                ConsumablesStorageID = table.Column<long>(nullable: true),
                SupplyPaymentTaskID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumablesOrder", x => x.ID);
                table.ForeignKey(
                    "FK_ConsumablesOrder_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsumablesOrder_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DepreciatedConsumableOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 250, nullable: true),
                CreatedByID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                DepreciatedToID = table.Column<long>(nullable: false),
                CommissionHeadID = table.Column<long>(nullable: false),
                ConsumablesStorageID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DepreciatedConsumableOrder", x => x.ID);
                table.ForeignKey(
                    "FK_DepreciatedConsumableOrder_User_CommissionHeadID",
                    x => x.CommissionHeadID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DepreciatedConsumableOrder_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DepreciatedConsumableOrder_User_DepreciatedToID",
                    x => x.DepreciatedToID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DepreciatedConsumableOrder_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "InvoiceDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                SupplyInvoiceID = table.Column<long>(nullable: true),
                PortWorkServiceID = table.Column<long>(nullable: true),
                TransportationServiceID = table.Column<long>(nullable: true),
                ContainerServiceID = table.Column<long>(nullable: true),
                VehicleServiceId = table.Column<long>(nullable: true),
                CustomServiceID = table.Column<long>(nullable: true),
                VehicleDeliveryServiceID = table.Column<long>(nullable: true),
                CustomAgencyServiceID = table.Column<long>(nullable: true),
                PlaneDeliveryServiceID = table.Column<long>(nullable: true),
                PortCustomAgencyServiceID = table.Column<long>(nullable: true),
                SupplyOrderPolandPaymentDeliveryProtocolID = table.Column<long>(nullable: true),
                PackingListID = table.Column<long>(nullable: true),
                MergedServiceID = table.Column<long>(nullable: true),
                Type = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_InvoiceDocument", x => x.ID);
                table.ForeignKey(
                    "FK_InvoiceDocument_ContainerService_ContainerServiceID",
                    x => x.ContainerServiceID,
                    "ContainerService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_CustomAgencyService_CustomAgencyServiceID",
                    x => x.CustomAgencyServiceID,
                    "CustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_CustomService_CustomServiceID",
                    x => x.CustomServiceID,
                    "CustomService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_MergedService_MergedServiceID",
                    x => x.MergedServiceID,
                    "MergedService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_PlaneDeliveryService_PlaneDeliveryServiceID",
                    x => x.PlaneDeliveryServiceID,
                    "PlaneDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_PortCustomAgencyService_PortCustomAgencyServiceID",
                    x => x.PortCustomAgencyServiceID,
                    "PortCustomAgencyService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_PortWorkService_PortWorkServiceID",
                    x => x.PortWorkServiceID,
                    "PortWorkService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_SupplyOrderPolandPaymentDeliveryProtocol_SupplyOrderPolandPaymentDeliveryProtocolID",
                    x => x.SupplyOrderPolandPaymentDeliveryProtocolID,
                    "SupplyOrderPolandPaymentDeliveryProtocol",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_TransportationService_TransportationServiceID",
                    x => x.TransportationServiceID,
                    "TransportationService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_VehicleDeliveryService_VehicleDeliveryServiceID",
                    x => x.VehicleDeliveryServiceID,
                    "VehicleDeliveryService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_InvoiceDocument_VehicleService_VehicleServiceId",
                    x => x.VehicleServiceId,
                    "VehicleService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PackingList",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                MarkNumber = table.Column<string>(maxLength: 100, nullable: true),
                InvNo = table.Column<string>(maxLength: 100, nullable: true),
                PlNo = table.Column<string>(maxLength: 100, nullable: true),
                RefNo = table.Column<string>(maxLength: 100, nullable: true),
                No = table.Column<string>(maxLength: 100, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                SupplyInvoiceID = table.Column<long>(nullable: false),
                ContainerServiceID = table.Column<long>(nullable: true),
                VehicleServiceId = table.Column<long>(nullable: true),
                ExtraCharge = table.Column<decimal>("money", nullable: false),
                AccountingExtraCharge = table.Column<decimal>("money", nullable: false),
                VatOnePercent = table.Column<decimal>(nullable: false),
                VatTwoPercent = table.Column<decimal>(nullable: false),
                IsDocumentsAdded = table.Column<bool>(nullable: false),
                IsPlaced = table.Column<bool>(nullable: false),
                IsVatOneApplied = table.Column<bool>(nullable: false),
                IsVatTwoApplied = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PackingList", x => x.ID);
                table.ForeignKey(
                    "FK_PackingList_ContainerService_ContainerServiceID",
                    x => x.ContainerServiceID,
                    "ContainerService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingList_VehicleService_VehicleServiceId",
                    x => x.VehicleServiceId,
                    "VehicleService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DynamicProductPlacementColumn",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                PackingListID = table.Column<long>(nullable: true),
                SupplyOrderUkraineID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_DynamicProductPlacementColumn", x => x.ID);
                table.ForeignKey(
                    "FK_DynamicProductPlacementColumn_PackingList_PackingListID",
                    x => x.PackingListID,
                    "PackingList",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DynamicProductPlacementColumn_SupplyOrderUkraine_SupplyOrderUkraineID",
                    x => x.SupplyOrderUkraineID,
                    "SupplyOrderUkraine",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PackingListPackage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                GrossWeight = table.Column<double>(nullable: false),
                NetWeight = table.Column<double>(nullable: false),
                CBM = table.Column<double>(nullable: false),
                Type = table.Column<int>(nullable: false),
                Lenght = table.Column<int>(nullable: false),
                Width = table.Column<int>(nullable: false),
                Height = table.Column<int>(nullable: false),
                PackingListID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PackingListPackage", x => x.ID);
                table.ForeignKey(
                    "FK_PackingListPackage_PackingList_PackingListID",
                    x => x.PackingListID,
                    "PackingList",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Organization",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 100, nullable: true),
                FullName = table.Column<string>(maxLength: 150, nullable: true),
                TIN = table.Column<string>(maxLength: 100, nullable: true),
                USREOU = table.Column<string>(maxLength: 100, nullable: true),
                SROI = table.Column<string>(maxLength: 150, nullable: true),
                Code = table.Column<string>(maxLength: 5, nullable: true),
                Culture = table.Column<string>(nullable: true),
                RegistrationNumber = table.Column<string>(maxLength: 150, nullable: true),
                PFURegistrationNumber = table.Column<string>(maxLength: 150, nullable: true),
                PhoneNumber = table.Column<string>(maxLength: 150, nullable: true),
                Address = table.Column<string>(maxLength: 250, nullable: true),
                RegistrationDate = table.Column<DateTime>(nullable: true),
                PFURegistrationDate = table.Column<DateTime>(nullable: true),
                IsIndividual = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CurrencyID = table.Column<long>(nullable: true),
                StorageID = table.Column<long>(nullable: true),
                TaxInspectionID = table.Column<long>(nullable: true),
                Manager = table.Column<string>(maxLength: 200, nullable: true),
                TypeTaxation = table.Column<int>(nullable: false, defaultValueSql: "0"),
                VatRateID = table.Column<long>(nullable: true),
                IsVatAgreements = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_Organization", x => x.ID);
                table.ForeignKey(
                    "FK_Organization_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Organization_TaxInspection_TaxInspectionID",
                    x => x.TaxInspectionID,
                    "TaxInspection",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Organization_VatRate_VatRateID",
                    x => x.VatRateID,
                    "VatRate",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ConsumablesStorage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 50, nullable: true),
                Description = table.Column<string>(maxLength: 250, nullable: true),
                ResponsibleUserID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ConsumablesStorage", x => x.ID);
                table.ForeignKey(
                    "FK_ConsumablesStorage_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ConsumablesStorage_User_ResponsibleUserID",
                    x => x.ResponsibleUserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DeliveryProductProtocol",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                TransportationType = table.Column<int>(nullable: false, defaultValueSql: "0"),
                OrganizationID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: false),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                IsCompleted = table.Column<bool>(nullable: false),
                IsShipped = table.Column<bool>(nullable: false),
                IsPlaced = table.Column<bool>(nullable: false),
                IsPartiallyPlaced = table.Column<bool>(nullable: false),
                DeliveryProductProtocolNumberID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DeliveryProductProtocol", x => x.ID);
                table.ForeignKey(
                    "FK_DeliveryProductProtocol_DeliveryProductProtocolNumber_DeliveryProductProtocolNumberID",
                    x => x.DeliveryProductProtocolNumberID,
                    "DeliveryProductProtocolNumber",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DeliveryProductProtocol_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_DeliveryProductProtocol_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "OrganizationTranslation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                CultureCode = table.Column<string>(nullable: true),
                Name = table.Column<string>(maxLength: 100, nullable: true),
                OrganizationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrganizationTranslation", x => x.ID);
                table.ForeignKey(
                    "FK_OrganizationTranslation_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "PaymentRegister",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 100, nullable: true),
                Type = table.Column<int>(nullable: false),
                AccountNumber = table.Column<string>(maxLength: 50, nullable: true),
                SortCode = table.Column<string>(maxLength: 20, nullable: true),
                IBAN = table.Column<string>(maxLength: 50, nullable: true),
                SwiftCode = table.Column<string>(maxLength: 50, nullable: true),
                BankName = table.Column<string>(maxLength: 100, nullable: true),
                CVV = table.Column<string>(maxLength: 3, nullable: true),
                City = table.Column<string>(maxLength: 100, nullable: true),
                FromDate = table.Column<DateTime>(nullable: true),
                ToDate = table.Column<DateTime>(nullable: true),
                IsActive = table.Column<bool>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                IsMain = table.Column<bool>(nullable: false),
                IsForRetail = table.Column<bool>(nullable: false),
                IsSelected = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentRegister", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentRegister_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SaleNumber",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Value = table.Column<string>(nullable: true),
                OrganizationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleNumber", x => x.ID);
                table.ForeignKey(
                    "FK_SaleNumber_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Storage",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 40, nullable: true),
                Locale = table.Column<string>(maxLength: 10, nullable: true),
                ForDefective = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ForVatProducts = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                AvailableForReSale = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                OrganizationID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Storage", x => x.ID);
                table.ForeignKey(
                    "FK_Storage_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrganization",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Name = table.Column<string>(maxLength: 255, nullable: true),
                Address = table.Column<string>(maxLength: 255, nullable: true),
                PhoneNumber = table.Column<string>(maxLength: 255, nullable: true),
                EmailAddress = table.Column<string>(maxLength: 255, nullable: true),
                Requisites = table.Column<string>(maxLength: 255, nullable: true),
                Swift = table.Column<string>(maxLength: 255, nullable: true),
                SwiftBic = table.Column<string>(maxLength: 255, nullable: true),
                IntermediaryBank = table.Column<string>(maxLength: 255, nullable: true),
                BeneficiaryBank = table.Column<string>(maxLength: 255, nullable: true),
                AccountNumber = table.Column<string>(maxLength: 255, nullable: true),
                Beneficiary = table.Column<string>(maxLength: 255, nullable: true),
                Bank = table.Column<string>(maxLength: 255, nullable: true),
                BankAccount = table.Column<string>(maxLength: 255, nullable: true),
                NIP = table.Column<string>(maxLength: 255, nullable: true),
                BankAccountPLN = table.Column<string>(maxLength: 255, nullable: true),
                BankAccountEUR = table.Column<string>(maxLength: 255, nullable: true),
                ContactPersonName = table.Column<string>(maxLength: 255, nullable: true),
                ContactPersonPhone = table.Column<string>(maxLength: 255, nullable: true),
                ContactPersonEmail = table.Column<string>(maxLength: 255, nullable: true),
                ContactPersonViber = table.Column<string>(maxLength: 255, nullable: true),
                ContactPersonSkype = table.Column<string>(maxLength: 255, nullable: true),
                ContactPersonComment = table.Column<string>(maxLength: 255, nullable: true),
                IsAgreementReceived = table.Column<bool>(nullable: false),
                IsBillReceived = table.Column<bool>(nullable: false),
                ExistTo = table.Column<DateTime>(nullable: false),
                AgreementReceiveDate = table.Column<DateTime>(nullable: true),
                BillReceiveDate = table.Column<DateTime>(nullable: true),
                OrganizationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrganization", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrganization_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DeliveryProductProtocolDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(maxLength: 500, nullable: true),
                FileName = table.Column<string>(maxLength: 500, nullable: true),
                ContentType = table.Column<string>(maxLength: 500, nullable: true),
                GeneratedName = table.Column<string>(maxLength: 500, nullable: true),
                Number = table.Column<string>(maxLength: 20, nullable: true),
                DeliveryProductProtocolID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DeliveryProductProtocolDocument", x => x.ID);
                table.ForeignKey(
                    "FK_DeliveryProductProtocolDocument_DeliveryProductProtocol_DeliveryProductProtocolID",
                    x => x.DeliveryProductProtocolID,
                    "DeliveryProductProtocol",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInvoice",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 100, nullable: true),
                ServiceNumber = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                NetPrice = table.Column<decimal>("money", nullable: false),
                ExtraCharge = table.Column<decimal>("money", nullable: false),
                IsShipped = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsPartiallyPlaced = table.Column<bool>(nullable: false),
                IsFullyPlaced = table.Column<bool>(nullable: false),
                DateFrom = table.Column<DateTime>(nullable: true),
                PaymentTo = table.Column<DateTime>(nullable: true),
                SupplyOrderID = table.Column<long>(nullable: false),
                NumberCustomDeclaration = table.Column<string>(maxLength: 20, nullable: true),
                DateCustomDeclaration = table.Column<DateTime>(nullable: true),
                DeliveryAmount = table.Column<decimal>("money", nullable: false),
                DiscountAmount = table.Column<decimal>("money", nullable: false),
                DeliveryProductProtocolID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInvoice", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInvoice_DeliveryProductProtocol_DeliveryProductProtocolID",
                    x => x.DeliveryProductProtocolID,
                    "DeliveryProductProtocol",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInvoice_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentCurrencyRegister",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Amount = table.Column<decimal>("money", nullable: false),
                InitialAmount = table.Column<decimal>("money", nullable: false),
                PaymentRegisterID = table.Column<long>(nullable: false),
                CurrencyID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentCurrencyRegister", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentCurrencyRegister_Currency_CurrencyID",
                    x => x.CurrencyID,
                    "Currency",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentCurrencyRegister_PaymentRegister_PaymentRegisterID",
                    x => x.PaymentRegisterID,
                    "PaymentRegister",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DepreciatedOrder",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                StorageID = table.Column<long>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                IsManagement = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DepreciatedOrder", x => x.ID);
                table.ForeignKey(
                    "FK_DepreciatedOrder_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DepreciatedOrder_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DepreciatedOrder_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductAvailability",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ProductID = table.Column<long>(nullable: false),
                StorageID = table.Column<long>(nullable: false),
                Amount = table.Column<double>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductAvailability", x => x.ID);
                table.ForeignKey(
                    "FK_ProductAvailability_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductAvailability_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductCapitalization",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                StorageID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductCapitalization", x => x.ID);
                table.ForeignKey(
                    "FK_ProductCapitalization_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductCapitalization_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductCapitalization_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductIncome",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ProductIncomeType = table.Column<int>(nullable: false),
                FromDate = table.Column<DateTime>(nullable: false),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                UserID = table.Column<long>(nullable: false),
                StorageID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductIncome", x => x.ID);
                table.ForeignKey(
                    "FK_ProductIncome_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductIncome_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductLocation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                StorageID = table.Column<long>(nullable: false),
                ProductPlacementID = table.Column<long>(nullable: false),
                OrderItemID = table.Column<long>(nullable: true),
                DepreciatedOrderItemID = table.Column<long>(nullable: true),
                ProductTransferItemID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductLocation", x => x.ID);
                table.ForeignKey(
                    "FK_ProductLocation_DepreciatedOrderItem_DepreciatedOrderItemID",
                    x => x.DepreciatedOrderItemID,
                    "DepreciatedOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductLocation_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductLocation_ProductPlacement_ProductPlacementID",
                    x => x.ProductPlacementID,
                    "ProductPlacement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductLocation_ProductTransferItem_ProductTransferItemID",
                    x => x.ProductTransferItemID,
                    "ProductTransferItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductLocation_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductTransfer",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                FromStorageID = table.Column<long>(nullable: false),
                ToStorageID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                IsManagement = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductTransfer", x => x.ID);
                table.ForeignKey(
                    "FK_ProductTransfer_Storage_FromStorageID",
                    x => x.FromStorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductTransfer_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductTransfer_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductTransfer_Storage_ToStorageID",
                    x => x.ToStorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SaleReturnItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                SaleReturnItemStatus = table.Column<int>(nullable: false),
                IsMoneyReturned = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                StorageID = table.Column<long>(nullable: false),
                OrderItemID = table.Column<long>(nullable: false),
                SaleReturnID = table.Column<long>(nullable: false),
                CreatedByID = table.Column<long>(nullable: false),
                UpdatedByID = table.Column<long>(nullable: true),
                MoneyReturnedByID = table.Column<long>(nullable: true),
                ExchangeRateAmount = table.Column<decimal>("money", nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                MoneyReturnedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleReturnItem", x => x.ID);
                table.ForeignKey(
                    "FK_SaleReturnItem_User_CreatedByID",
                    x => x.CreatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturnItem_User_MoneyReturnedByID",
                    x => x.MoneyReturnedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturnItem_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturnItem_SaleReturn_SaleReturnID",
                    x => x.SaleReturnID,
                    "SaleReturn",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturnItem_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SaleReturnItem_User_UpdatedByID",
                    x => x.UpdatedByID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrganizationDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                SupplyOrganizationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrganizationDocument", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrganizationDocument_SupplyOrganization_SupplyOrganizationID",
                    x => x.SupplyOrganizationID,
                    "SupplyOrganization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ActReconciliation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 500, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                ResponsibleID = table.Column<long>(nullable: false),
                SupplyOrderUkraineID = table.Column<long>(nullable: true),
                SupplyInvoiceID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_ActReconciliation", x => x.ID);
                table.ForeignKey(
                    "FK_ActReconciliation_User_ResponsibleID",
                    x => x.ResponsibleID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ActReconciliation_SupplyInvoice_SupplyInvoiceID",
                    x => x.SupplyInvoiceID,
                    "SupplyInvoice",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ActReconciliation_SupplyOrderUkraine_SupplyOrderUkraineID",
                    x => x.SupplyOrderUkraineID,
                    "SupplyOrderUkraine",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "OrderProductSpecification",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                SupplyInvoiceId = table.Column<long>(nullable: true),
                SadId = table.Column<long>(nullable: true),
                ProductSpecificationId = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_OrderProductSpecification", x => x.ID);
                table.ForeignKey(
                    "FK_OrderProductSpecification_ProductSpecification_ProductSpecificationId",
                    x => x.ProductSpecificationId,
                    "ProductSpecification",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderProductSpecification_Sad_SadId",
                    x => x.SadId,
                    "Sad",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_OrderProductSpecification_SupplyInvoice_SupplyInvoiceId",
                    x => x.SupplyInvoiceId,
                    "SupplyInvoice",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInformationDeliveryProtocol",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                UserID = table.Column<long>(nullable: false),
                SupplyOrderID = table.Column<long>(nullable: true),
                SupplyInvoiceID = table.Column<long>(nullable: true),
                SupplyProFormID = table.Column<long>(nullable: true),
                SupplyInformationDeliveryProtocolKeyID = table.Column<long>(nullable: false),
                Value = table.Column<string>(nullable: true),
                IsDefault = table.Column<bool>(nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInformationDeliveryProtocol", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInformationDeliveryProtocol_SupplyInformationDeliveryProtocolKey_SupplyInformationDeliveryProtocolKeyID",
                    x => x.SupplyInformationDeliveryProtocolKeyID,
                    "SupplyInformationDeliveryProtocolKey",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_SupplyInformationDeliveryProtocol_SupplyInvoice_SupplyInvoiceID",
                    x => x.SupplyInvoiceID,
                    "SupplyInvoice",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInformationDeliveryProtocol_SupplyOrder_SupplyOrderID",
                    x => x.SupplyOrderID,
                    "SupplyOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInformationDeliveryProtocol_SupplyProForm_SupplyProFormID",
                    x => x.SupplyProFormID,
                    "SupplyProForm",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInformationDeliveryProtocol_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInvoiceDeliveryDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SupplyInvoiceID = table.Column<long>(nullable: false),
                SupplyDeliveryDocumentID = table.Column<long>(nullable: false),
                DocumentUrl = table.Column<string>(maxLength: 500, nullable: true),
                ContentType = table.Column<string>(maxLength: 500, nullable: true),
                FileName = table.Column<string>(maxLength: 500, nullable: true),
                GeneratedName = table.Column<string>(maxLength: 500, nullable: true),
                Number = table.Column<string>(maxLength: 20, nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInvoiceDeliveryDocument", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInvoiceDeliveryDocument_SupplyDeliveryDocument_SupplyDeliveryDocumentID",
                    x => x.SupplyDeliveryDocumentID,
                    "SupplyDeliveryDocument",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInvoiceDeliveryDocument_SupplyInvoice_SupplyInvoiceID",
                    x => x.SupplyInvoiceID,
                    "SupplyInvoice",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInvoiceMergedService",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                SupplyInvoiceID = table.Column<long>(nullable: false),
                MergedServiceID = table.Column<long>(nullable: false),
                Value = table.Column<decimal>("decimal(30,14)", nullable: false),
                AccountingValue = table.Column<decimal>("decimal(30,14)", nullable: false),
                IsCalculatedValue = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInvoiceMergedService", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInvoiceMergedService_MergedService_MergedServiceID",
                    x => x.MergedServiceID,
                    "MergedService",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInvoiceMergedService_SupplyInvoice_SupplyInvoiceID",
                    x => x.SupplyInvoiceID,
                    "SupplyInvoice",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyInvoiceOrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                UnitPrice = table.Column<decimal>("money", nullable: false),
                SupplyOrderItemID = table.Column<long>(nullable: false),
                SupplyInvoiceID = table.Column<long>(nullable: false),
                RowNumber = table.Column<int>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyInvoiceOrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyInvoiceOrderItem_SupplyInvoice_SupplyInvoiceID",
                    x => x.SupplyInvoiceID,
                    "SupplyInvoice",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyInvoiceOrderItem_SupplyOrderItem_SupplyOrderItemID",
                    x => x.SupplyOrderItemID,
                    "SupplyOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "SupplyOrderPaymentDeliveryProtocol",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                UserID = table.Column<long>(nullable: false),
                SupplyPaymentTaskID = table.Column<long>(nullable: true),
                SupplyInvoiceID = table.Column<long>(nullable: true),
                SupplyProFormID = table.Column<long>(nullable: true),
                SupplyOrderPaymentDeliveryProtocolKeyID = table.Column<long>(nullable: false),
                Value = table.Column<decimal>("money", nullable: false),
                Discount = table.Column<double>(nullable: false),
                IsAccounting = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SupplyOrderPaymentDeliveryProtocol", x => x.ID);
                table.ForeignKey(
                    "FK_SupplyOrderPaymentDeliveryProtocol_SupplyInvoice_SupplyInvoiceID",
                    x => x.SupplyInvoiceID,
                    "SupplyInvoice",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderPaymentDeliveryProtocol_SupplyOrderPaymentDeliveryProtocolKey_SupplyOrderPaymentDeliveryProtocolKeyID",
                    x => x.SupplyOrderPaymentDeliveryProtocolKeyID,
                    "SupplyOrderPaymentDeliveryProtocolKey",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_SupplyOrderPaymentDeliveryProtocol_SupplyPaymentTask_SupplyPaymentTaskID",
                    x => x.SupplyPaymentTaskID,
                    "SupplyPaymentTask",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderPaymentDeliveryProtocol_SupplyProForm_SupplyProFormID",
                    x => x.SupplyProFormID,
                    "SupplyProForm",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SupplyOrderPaymentDeliveryProtocol_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentRegisterCurrencyExchange",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                ExchangeRate = table.Column<decimal>("money", nullable: false),
                IsCanceled = table.Column<bool>(nullable: false),
                FromPaymentCurrencyRegisterID = table.Column<long>(nullable: false),
                ToPaymentCurrencyRegisterID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false),
                CurrencyTraderID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentRegisterCurrencyExchange", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentRegisterCurrencyExchange_CurrencyTrader_CurrencyTraderID",
                    x => x.CurrencyTraderID,
                    "CurrencyTrader",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentRegisterCurrencyExchange_PaymentCurrencyRegister_FromPaymentCurrencyRegisterID",
                    x => x.FromPaymentCurrencyRegisterID,
                    "PaymentCurrencyRegister",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentRegisterCurrencyExchange_PaymentCurrencyRegister_ToPaymentCurrencyRegisterID",
                    x => x.ToPaymentCurrencyRegisterID,
                    "PaymentCurrencyRegister",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentRegisterCurrencyExchange_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentRegisterTransfer",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Number = table.Column<string>(maxLength: 50, nullable: true),
                Comment = table.Column<string>(maxLength: 450, nullable: true),
                FromDate = table.Column<DateTime>(nullable: false),
                Amount = table.Column<decimal>("money", nullable: false),
                IsCanceled = table.Column<bool>(nullable: false),
                FromPaymentCurrencyRegisterID = table.Column<long>(nullable: false),
                ToPaymentCurrencyRegisterID = table.Column<long>(nullable: false),
                UserID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentRegisterTransfer", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentRegisterTransfer_PaymentCurrencyRegister_FromPaymentCurrencyRegisterID",
                    x => x.FromPaymentCurrencyRegisterID,
                    "PaymentCurrencyRegister",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentRegisterTransfer_PaymentCurrencyRegister_ToPaymentCurrencyRegisterID",
                    x => x.ToPaymentCurrencyRegisterID,
                    "PaymentCurrencyRegister",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentRegisterTransfer_User_UserID",
                    x => x.UserID,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "ProductCapitalizationItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                RemainingQty = table.Column<double>(nullable: false),
                Weight = table.Column<double>(nullable: false),
                UnitPrice = table.Column<decimal>("money", nullable: false),
                ProductID = table.Column<long>(nullable: false),
                ProductCapitalizationID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductCapitalizationItem", x => x.ID);
                table.ForeignKey(
                    "FK_ProductCapitalizationItem_ProductCapitalization_ProductCapitalizationID",
                    x => x.ProductCapitalizationID,
                    "ProductCapitalization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductCapitalizationItem_Product_ProductID",
                    x => x.ProductID,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Consignment",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsVirtual = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FromDate = table.Column<DateTime>(nullable: false),
                StorageID = table.Column<long>(nullable: false),
                OrganizationID = table.Column<long>(nullable: false),
                ProductIncomeID = table.Column<long>(nullable: false),
                ProductTransferID = table.Column<long>(nullable: true),
                IsImportedFromOneC = table.Column<bool>(nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_Consignment", x => x.ID);
                table.ForeignKey(
                    "FK_Consignment_Organization_OrganizationID",
                    x => x.OrganizationID,
                    "Organization",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Consignment_ProductIncome_ProductIncomeID",
                    x => x.ProductIncomeID,
                    "ProductIncome",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Consignment_ProductTransfer_ProductTransferID",
                    x => x.ProductTransferID,
                    "ProductTransfer",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Consignment_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PackingListPackageOrderItem",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                PlacedQty = table.Column<double>(nullable: false),
                RemainingQty = table.Column<double>(nullable: false),
                UploadedQty = table.Column<double>(nullable: false),
                Placement = table.Column<string>(maxLength: 25, nullable: true),
                SupplyInvoiceOrderItemID = table.Column<long>(nullable: false),
                PackingListPackageID = table.Column<long>(nullable: true),
                PackingListID = table.Column<long>(nullable: true),
                IsPlaced = table.Column<bool>(nullable: false),
                IsErrorInPlaced = table.Column<bool>(nullable: false),
                IsReadyToPlaced = table.Column<bool>(nullable: false),
                ExchangeRateAmount = table.Column<decimal>("money", nullable: false),
                UnitPrice = table.Column<decimal>("money", nullable: false),
                UnitPriceEur = table.Column<decimal>("money", nullable: false),
                GrossUnitPriceEur = table.Column<decimal>("decimal(30,14)", nullable: false),
                AccountingGrossUnitPriceEur = table.Column<decimal>("decimal(30,14)", nullable: false),
                AccountingGeneralGrossUnitPriceEur = table.Column<decimal>("decimal(30,14)", nullable: false),
                ContainerUnitPriceEur = table.Column<decimal>("money", nullable: false),
                AccountingContainerUnitPriceEur = table.Column<decimal>("money", nullable: false),
                VatPercent = table.Column<decimal>("money", nullable: false),
                VatAmount = table.Column<decimal>("money", nullable: false),
                GrossWeight = table.Column<double>(nullable: false),
                NetWeight = table.Column<double>(nullable: false),
                ExchangeRateAmountUahToEur = table.Column<decimal>("money", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PackingListPackageOrderItem", x => x.ID);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItem_PackingList_PackingListID",
                    x => x.PackingListID,
                    "PackingList",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItem_PackingListPackage_PackingListPackageID",
                    x => x.PackingListPackageID,
                    "PackingListPackage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PackingListPackageOrderItem_SupplyInvoiceOrderItem_SupplyInvoiceOrderItemID",
                    x => x.SupplyInvoiceOrderItemID,
                    "SupplyInvoiceOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentDeliveryDocument",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                DocumentUrl = table.Column<string>(nullable: true),
                FileName = table.Column<string>(nullable: true),
                ContentType = table.Column<string>(nullable: true),
                GeneratedName = table.Column<string>(nullable: true),
                SupplyOrderPaymentDeliveryProtocolID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentDeliveryDocument", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentDeliveryDocument_SupplyOrderPaymentDeliveryProtocol_SupplyOrderPaymentDeliveryProtocolID",
                    x => x.SupplyOrderPaymentDeliveryProtocolID,
                    "SupplyOrderPaymentDeliveryProtocol",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "PaymentMovementOperation",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                PaymentMovementID = table.Column<long>(nullable: false),
                IncomePaymentOrderID = table.Column<long>(nullable: true),
                OutcomePaymentOrderID = table.Column<long>(nullable: true),
                PaymentRegisterTransferID = table.Column<long>(nullable: true),
                PaymentRegisterCurrencyExchangeID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_PaymentMovementOperation", x => x.ID);
                table.ForeignKey(
                    "FK_PaymentMovementOperation_IncomePaymentOrder_IncomePaymentOrderID",
                    x => x.IncomePaymentOrderID,
                    "IncomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentMovementOperation_OutcomePaymentOrder_OutcomePaymentOrderID",
                    x => x.OutcomePaymentOrderID,
                    "OutcomePaymentOrder",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentMovementOperation_PaymentMovement_PaymentMovementID",
                    x => x.PaymentMovementID,
                    "PaymentMovement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentMovementOperation_PaymentRegisterCurrencyExchange_PaymentRegisterCurrencyExchangeID",
                    x => x.PaymentRegisterCurrencyExchangeID,
                    "PaymentRegisterCurrencyExchange",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_PaymentMovementOperation_PaymentRegisterTransfer_PaymentRegisterTransferID",
                    x => x.PaymentRegisterTransferID,
                    "PaymentRegisterTransfer",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DynamicProductPlacementRow",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                SupplyOrderUkraineItemID = table.Column<long>(nullable: true),
                PackingListPackageOrderItemID = table.Column<long>(nullable: true),
                DynamicProductPlacementColumnID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DynamicProductPlacementRow", x => x.ID);
                table.ForeignKey(
                    "FK_DynamicProductPlacementRow_DynamicProductPlacementColumn_DynamicProductPlacementColumnID",
                    x => x.DynamicProductPlacementColumnID,
                    "DynamicProductPlacementColumn",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DynamicProductPlacementRow_PackingListPackageOrderItem_PackingListPackageOrderItemID",
                    x => x.PackingListPackageOrderItemID,
                    "PackingListPackageOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_DynamicProductPlacementRow_SupplyOrderUkraineItem_SupplyOrderUkraineItemID",
                    x => x.SupplyOrderUkraineItemID,
                    "SupplyOrderUkraineItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "DynamicProductPlacement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                IsApplied = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                Qty = table.Column<double>(nullable: false),
                StorageNumber = table.Column<string>(maxLength: 5, nullable: true),
                RowNumber = table.Column<string>(maxLength: 5, nullable: true),
                CellNumber = table.Column<string>(maxLength: 5, nullable: true),
                DynamicProductPlacementRowID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_DynamicProductPlacement", x => x.ID);
                table.ForeignKey(
                    "FK_DynamicProductPlacement_DynamicProductPlacementRow_DynamicProductPlacementRowID",
                    x => x.DynamicProductPlacementRowID,
                    "DynamicProductPlacementRow",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ActProvidingService_UserID",
            "ActProvidingService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_ActReconciliation_ResponsibleID",
            "ActReconciliation",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_ActReconciliation_SupplyInvoiceID",
            "ActReconciliation",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_ActReconciliation_SupplyOrderUkraineID",
            "ActReconciliation",
            "SupplyOrderUkraineID");

        migrationBuilder.CreateIndex(
            "IX_ActReconciliationItem_ActReconciliationID",
            "ActReconciliationItem",
            "ActReconciliationID");

        migrationBuilder.CreateIndex(
            "IX_ActReconciliationItem_ProductID",
            "ActReconciliationItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ActReconciliationItem_SupplyInvoiceOrderItemID",
            "ActReconciliationItem",
            "SupplyInvoiceOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ActReconciliationItem_SupplyOrderUkraineItemID",
            "ActReconciliationItem",
            "SupplyOrderUkraineItemID");

        migrationBuilder.CreateIndex(
            "IX_AdvancePayment_ClientAgreementID",
            "AdvancePayment",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_AdvancePayment_OrganizationClientAgreementID",
            "AdvancePayment",
            "OrganizationClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_AdvancePayment_OrganizationID",
            "AdvancePayment",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_AdvancePayment_SadID",
            "AdvancePayment",
            "SadID");

        migrationBuilder.CreateIndex(
            "IX_AdvancePayment_TaxFreeID",
            "AdvancePayment",
            "TaxFreeID");

        migrationBuilder.CreateIndex(
            "IX_AdvancePayment_UserID",
            "AdvancePayment",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_Agreement_AgreementTypeCivilCodeID",
            "Agreement",
            "AgreementTypeCivilCodeID");

        migrationBuilder.CreateIndex(
            "IX_Agreement_CurrencyID",
            "Agreement",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_Agreement_OrganizationID",
            "Agreement",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_Agreement_PricingID",
            "Agreement",
            "PricingID");

        migrationBuilder.CreateIndex(
            "IX_Agreement_PromotionalPricingID",
            "Agreement",
            "PromotionalPricingID");

        migrationBuilder.CreateIndex(
            "IX_Agreement_ProviderPricingID",
            "Agreement",
            "ProviderPricingID");

        migrationBuilder.CreateIndex(
            "IX_Agreement_TaxAccountingSchemeID",
            "Agreement",
            "TaxAccountingSchemeID");

        migrationBuilder.CreateIndex(
            "IX_AgreementTypeTranslation_AgreementTypeID",
            "AgreementTypeTranslation",
            "AgreementTypeID");

        migrationBuilder.CreateIndex(
            "IX_AllegroCategory_CategoryID",
            "AllegroCategory",
            "CategoryID");

        migrationBuilder.CreateIndex(
            "IX_AllegroCategory_ParentCategoryID",
            "AllegroCategory",
            "ParentCategoryID");

        migrationBuilder.CreateIndex(
            "IX_AllegroProductReservation_ProductID",
            "AllegroProductReservation",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_AssignedPaymentOrder_AssignedIncomePaymentOrderID",
            "AssignedPaymentOrder",
            "AssignedIncomePaymentOrderID",
            unique: true,
            filter: "[AssignedIncomePaymentOrderID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_AssignedPaymentOrder_AssignedOutcomePaymentOrderID",
            "AssignedPaymentOrder",
            "AssignedOutcomePaymentOrderID",
            unique: true,
            filter: "[AssignedOutcomePaymentOrderID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_AssignedPaymentOrder_RootIncomePaymentOrderID",
            "AssignedPaymentOrder",
            "RootIncomePaymentOrderID");

        migrationBuilder.CreateIndex(
            "IX_AssignedPaymentOrder_RootOutcomePaymentOrderID",
            "AssignedPaymentOrder",
            "RootOutcomePaymentOrderID");

        migrationBuilder.CreateIndex(
            "IX_AuditEntity_BaseEntityNetUID",
            "AuditEntity",
            "BaseEntityNetUID");

        migrationBuilder.CreateIndex(
            "IX_AuditEntityProperty_AuditEntityID",
            "AuditEntityProperty",
            "AuditEntityID");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingDocument_BillOfLadingServiceID",
            "BillOfLadingDocument",
            "BillOfLadingServiceID");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_AccountingActProvidingServiceID",
            "BillOfLadingService",
            "AccountingActProvidingServiceID",
            unique: true,
            filter: "[AccountingActProvidingServiceID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_AccountingPaymentTaskID",
            "BillOfLadingService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_ActProvidingServiceDocumentID",
            "BillOfLadingService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_ActProvidingServiceID",
            "BillOfLadingService",
            "ActProvidingServiceID",
            unique: true,
            filter: "[ActProvidingServiceID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_DeliveryProductProtocolID",
            "BillOfLadingService",
            "DeliveryProductProtocolID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_SupplyInformationTaskID",
            "BillOfLadingService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_SupplyOrganizationAgreementID",
            "BillOfLadingService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_SupplyOrganizationID",
            "BillOfLadingService",
            "SupplyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_SupplyPaymentTaskID",
            "BillOfLadingService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_SupplyServiceAccountDocumentID",
            "BillOfLadingService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_BillOfLadingService_UserID",
            "BillOfLadingService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_CalculationTypeTranslation_CalculationTypeID",
            "CalculationTypeTranslation",
            "CalculationTypeID");

        migrationBuilder.CreateIndex(
            "IX_Category_RootCategoryID",
            "Category",
            "RootCategoryID");

        migrationBuilder.CreateIndex(
            "IX_ChartMonthTranslation_ChartMonthID",
            "ChartMonthTranslation",
            "ChartMonthID");

        migrationBuilder.CreateIndex(
            "IX_Client_ClientBankDetailsID",
            "Client",
            "ClientBankDetailsID");

        migrationBuilder.CreateIndex(
            "IX_Client_CountryID",
            "Client",
            "CountryID");

        migrationBuilder.CreateIndex(
            "IX_Client_NetUID",
            "Client",
            "NetUID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_Client_PackingMarkingID",
            "Client",
            "PackingMarkingID");

        migrationBuilder.CreateIndex(
            "IX_Client_PackingMarkingPaymentID",
            "Client",
            "PackingMarkingPaymentID");

        migrationBuilder.CreateIndex(
            "IX_Client_RegionCodeID",
            "Client",
            "RegionCodeID");

        migrationBuilder.CreateIndex(
            "IX_Client_RegionID",
            "Client",
            "RegionID");

        migrationBuilder.CreateIndex(
            "IX_Client_TermsOfDeliveryID",
            "Client",
            "TermsOfDeliveryID");

        migrationBuilder.CreateIndex(
            "IX_ClientAgreement_AgreementID",
            "ClientAgreement",
            "AgreementID");

        migrationBuilder.CreateIndex(
            "IX_ClientAgreement_ClientID",
            "ClientAgreement",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientAgreement_NetUID",
            "ClientAgreement",
            "NetUID");

        migrationBuilder.CreateIndex(
            "IX_ClientBalanceMovement_ClientAgreementID",
            "ClientBalanceMovement",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_ClientBankDetailAccountNumber_CurrencyID",
            "ClientBankDetailAccountNumber",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_ClientBankDetailIbanNo_CurrencyID",
            "ClientBankDetailIbanNo",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_ClientBankDetails_AccountNumberID",
            "ClientBankDetails",
            "AccountNumberID");

        migrationBuilder.CreateIndex(
            "IX_ClientBankDetails_ClientBankDetailIbanNoID",
            "ClientBankDetails",
            "ClientBankDetailIbanNoID");

        migrationBuilder.CreateIndex(
            "IX_ClientContractDocument_ClientID",
            "ClientContractDocument",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientInDebt_AgreementID",
            "ClientInDebt",
            "AgreementID");

        migrationBuilder.CreateIndex(
            "IX_ClientInDebt_ClientID",
            "ClientInDebt",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientInDebt_DebtID",
            "ClientInDebt",
            "DebtID");

        migrationBuilder.CreateIndex(
            "IX_ClientInDebt_ReSaleID",
            "ClientInDebt",
            "ReSaleID");

        migrationBuilder.CreateIndex(
            "IX_ClientInDebt_SaleID",
            "ClientInDebt",
            "SaleID");

        migrationBuilder.CreateIndex(
            "IX_ClientInRole_ClientID",
            "ClientInRole",
            "ClientID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_ClientInRole_ClientTypeID",
            "ClientInRole",
            "ClientTypeID");

        migrationBuilder.CreateIndex(
            "IX_ClientInRole_ClientTypeRoleID",
            "ClientInRole",
            "ClientTypeRoleID");

        migrationBuilder.CreateIndex(
            "IX_ClientPerfectClient_ClientID",
            "ClientPerfectClient",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientPerfectClient_PerfectClientID",
            "ClientPerfectClient",
            "PerfectClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientPerfectClient_PerfectClientValueID",
            "ClientPerfectClient",
            "PerfectClientValueID");

        migrationBuilder.CreateIndex(
            "IX_ClientRegistrationTask_ClientID",
            "ClientRegistrationTask",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientShoppingCart_ClientAgreementID",
            "ClientShoppingCart",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_ClientShoppingCart_CreatedByID",
            "ClientShoppingCart",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_ClientShoppingCart_OfferProcessingStatusChangedByID",
            "ClientShoppingCart",
            "OfferProcessingStatusChangedByID");

        migrationBuilder.CreateIndex(
            "IX_ClientSubClient_RootClientID",
            "ClientSubClient",
            "RootClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientSubClient_SubClientID",
            "ClientSubClient",
            "SubClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientTypeRole_ClientTypeID",
            "ClientTypeRole",
            "ClientTypeID");

        migrationBuilder.CreateIndex(
            "IX_ClientTypeRoleTranslation_ClientTypeRoleID",
            "ClientTypeRoleTranslation",
            "ClientTypeRoleID");

        migrationBuilder.CreateIndex(
            "IX_ClientTypeTranslation_ClientTypeID",
            "ClientTypeTranslation",
            "ClientTypeID");

        migrationBuilder.CreateIndex(
            "IX_ClientUserProfile_ClientID",
            "ClientUserProfile",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientUserProfile_UserProfileID",
            "ClientUserProfile",
            "UserProfileID");

        migrationBuilder.CreateIndex(
            "IX_ColumnItem_UserID",
            "ColumnItem",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_ColumnItemTranslation_ColumnItemID",
            "ColumnItemTranslation",
            "ColumnItemID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCar_ConsumablesStorageID",
            "CompanyCar",
            "ConsumablesStorageID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_CompanyCar_CreatedByID",
            "CompanyCar",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCar_OrganizationID",
            "CompanyCar",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCar_UpdatedByID",
            "CompanyCar",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarFueling_CompanyCarID",
            "CompanyCarFueling",
            "CompanyCarID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarFueling_ConsumableProductOrganizationID",
            "CompanyCarFueling",
            "ConsumableProductOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarFueling_OutcomePaymentOrderID",
            "CompanyCarFueling",
            "OutcomePaymentOrderID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarFueling_UserID",
            "CompanyCarFueling",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarRoadList_CompanyCarID",
            "CompanyCarRoadList",
            "CompanyCarID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarRoadList_CreatedByID",
            "CompanyCarRoadList",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarRoadList_OutcomePaymentOrderID",
            "CompanyCarRoadList",
            "OutcomePaymentOrderID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarRoadList_ResponsibleID",
            "CompanyCarRoadList",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarRoadList_UpdatedByID",
            "CompanyCarRoadList",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarRoadListDriver_CompanyCarRoadListID",
            "CompanyCarRoadListDriver",
            "CompanyCarRoadListID");

        migrationBuilder.CreateIndex(
            "IX_CompanyCarRoadListDriver_UserID",
            "CompanyCarRoadListDriver",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_Consignment_OrganizationID",
            "Consignment",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_Consignment_ProductIncomeID",
            "Consignment",
            "ProductIncomeID");

        migrationBuilder.CreateIndex(
            "IX_Consignment_ProductTransferID",
            "Consignment",
            "ProductTransferID");

        migrationBuilder.CreateIndex(
            "IX_Consignment_StorageID",
            "Consignment",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItem_ConsignmentID",
            "ConsignmentItem",
            "ConsignmentID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItem_ProductID",
            "ConsignmentItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItem_ProductIncomeItemID",
            "ConsignmentItem",
            "ProductIncomeItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItem_ProductSpecificationID",
            "ConsignmentItem",
            "ProductSpecificationID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItem_RootConsignmentItemID",
            "ConsignmentItem",
            "RootConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_ConsignmentItemID",
            "ConsignmentItemMovement",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_DepreciatedOrderItemID",
            "ConsignmentItemMovement",
            "DepreciatedOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_OrderItemBaseShiftStatusID",
            "ConsignmentItemMovement",
            "OrderItemBaseShiftStatusID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_OrderItemID",
            "ConsignmentItemMovement",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_ProductIncomeItemID",
            "ConsignmentItemMovement",
            "ProductIncomeItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_ProductTransferItemID",
            "ConsignmentItemMovement",
            "ProductTransferItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_ReSaleItemId",
            "ConsignmentItemMovement",
            "ReSaleItemId");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_SadItemID",
            "ConsignmentItemMovement",
            "SadItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_SupplyReturnItemID",
            "ConsignmentItemMovement",
            "SupplyReturnItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsignmentItemMovement_TaxFreeItemID",
            "ConsignmentItemMovement",
            "TaxFreeItemID");

        migrationBuilder.CreateIndex(
            "IX_ConsumableProduct_ConsumableProductCategoryID",
            "ConsumableProduct",
            "ConsumableProductCategoryID");

        migrationBuilder.CreateIndex(
            "IX_ConsumableProduct_MeasureUnitID",
            "ConsumableProduct",
            "MeasureUnitID");

        migrationBuilder.CreateIndex(
            "IX_ConsumableProductCategoryTranslation_ConsumableProductCategoryID",
            "ConsumableProductCategoryTranslation",
            "ConsumableProductCategoryID");

        migrationBuilder.CreateIndex(
            "IX_ConsumableProductTranslation_ConsumableProductID",
            "ConsumableProductTranslation",
            "ConsumableProductID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrder_ConsumablesStorageID",
            "ConsumablesOrder",
            "ConsumablesStorageID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrder_SupplyPaymentTaskID",
            "ConsumablesOrder",
            "SupplyPaymentTaskID",
            unique: true,
            filter: "[SupplyPaymentTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrder_UserID",
            "ConsumablesOrder",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrderDocument_ConsumablesOrderID",
            "ConsumablesOrderDocument",
            "ConsumablesOrderID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrderItem_ConsumableProductCategoryID",
            "ConsumablesOrderItem",
            "ConsumableProductCategoryID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrderItem_ConsumableProductID",
            "ConsumablesOrderItem",
            "ConsumableProductID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrderItem_ConsumableProductOrganizationID",
            "ConsumablesOrderItem",
            "ConsumableProductOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrderItem_ConsumablesOrderID",
            "ConsumablesOrderItem",
            "ConsumablesOrderID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesOrderItem_SupplyOrganizationAgreementID",
            "ConsumablesOrderItem",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesStorage_OrganizationID",
            "ConsumablesStorage",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_ConsumablesStorage_ResponsibleUserID",
            "ConsumablesStorage",
            "ResponsibleUserID");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_AccountingActProvidingServiceId",
            "ContainerService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_AccountingPaymentTaskID",
            "ContainerService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_ActProvidingServiceDocumentID",
            "ContainerService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_ActProvidingServiceId",
            "ContainerService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_BillOfLadingDocumentID",
            "ContainerService",
            "BillOfLadingDocumentID");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_ContainerOrganizationID",
            "ContainerService",
            "ContainerOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_SupplyInformationTaskID",
            "ContainerService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_SupplyOrganizationAgreementID",
            "ContainerService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_SupplyPaymentTaskID",
            "ContainerService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_SupplyServiceAccountDocumentID",
            "ContainerService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_ContainerService_UserID",
            "ContainerService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_CountSaleMessage_SaleID",
            "CountSaleMessage",
            "SaleID");

        migrationBuilder.CreateIndex(
            "IX_CountSaleMessage_SaleMessageNumeratorID",
            "CountSaleMessage",
            "SaleMessageNumeratorID");

        migrationBuilder.CreateIndex(
            "IX_CreditNoteDocument_SupplyOrderID",
            "CreditNoteDocument",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_CrossExchangeRate_CurrencyFromID",
            "CrossExchangeRate",
            "CurrencyFromID");

        migrationBuilder.CreateIndex(
            "IX_CrossExchangeRate_CurrencyToID",
            "CrossExchangeRate",
            "CurrencyToID");

        migrationBuilder.CreateIndex(
            "IX_CrossExchangeRateHistory_CrossExchangeRateID",
            "CrossExchangeRateHistory",
            "CrossExchangeRateID");

        migrationBuilder.CreateIndex(
            "IX_CrossExchangeRateHistory_UpdatedByID",
            "CrossExchangeRateHistory",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_CurrencyTraderExchangeRate_CurrencyTraderID",
            "CurrencyTraderExchangeRate",
            "CurrencyTraderID");

        migrationBuilder.CreateIndex(
            "IX_CurrencyTranslation_CurrencyID",
            "CurrencyTranslation",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_AccountingActProvidingServiceId",
            "CustomAgencyService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_AccountingPaymentTaskID",
            "CustomAgencyService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_ActProvidingServiceDocumentID",
            "CustomAgencyService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_ActProvidingServiceId",
            "CustomAgencyService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_CustomAgencyOrganizationID",
            "CustomAgencyService",
            "CustomAgencyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_SupplyInformationTaskID",
            "CustomAgencyService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_SupplyOrganizationAgreementID",
            "CustomAgencyService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_SupplyPaymentTaskID",
            "CustomAgencyService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_SupplyServiceAccountDocumentID",
            "CustomAgencyService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_CustomAgencyService_UserID",
            "CustomAgencyService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_CustomService_AccountingActProvidingServiceId",
            "CustomService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_CustomService_AccountingPaymentTaskID",
            "CustomService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_CustomService_ActProvidingServiceDocumentID",
            "CustomService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_CustomService_ActProvidingServiceId",
            "CustomService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_CustomService_CustomOrganizationID",
            "CustomService",
            "CustomOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_CustomService_ExciseDutyOrganizationID",
            "CustomService",
            "ExciseDutyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_CustomService_SupplyInformationTaskID",
            "CustomService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_CustomService_SupplyOrderID",
            "CustomService",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_CustomService_SupplyOrganizationAgreementID",
            "CustomService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_CustomService_SupplyPaymentTaskID",
            "CustomService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_CustomService_SupplyServiceAccountDocumentID",
            "CustomService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_CustomService_UserID",
            "CustomService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_DashboardNode_DashboardNodeModuleID",
            "DashboardNode",
            "DashboardNodeModuleID");

        migrationBuilder.CreateIndex(
            "IX_DashboardNode_ParentDashboardNodeID",
            "DashboardNode",
            "ParentDashboardNodeID");

        migrationBuilder.CreateIndex(
            "IX_DataSyncOperation_UserID",
            "DataSyncOperation",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryProductProtocol_DeliveryProductProtocolNumberID",
            "DeliveryProductProtocol",
            "DeliveryProductProtocolNumberID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_DeliveryProductProtocol_OrganizationID",
            "DeliveryProductProtocol",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryProductProtocol_UserID",
            "DeliveryProductProtocol",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryProductProtocolDocument_DeliveryProductProtocolID",
            "DeliveryProductProtocolDocument",
            "DeliveryProductProtocolID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryRecipient_ClientID",
            "DeliveryRecipient",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_DeliveryRecipientAddress_DeliveryRecipientID",
            "DeliveryRecipientAddress",
            "DeliveryRecipientID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedConsumableOrder_CommissionHeadID",
            "DepreciatedConsumableOrder",
            "CommissionHeadID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedConsumableOrder_ConsumablesStorageID",
            "DepreciatedConsumableOrder",
            "ConsumablesStorageID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedConsumableOrder_CreatedByID",
            "DepreciatedConsumableOrder",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedConsumableOrder_DepreciatedToID",
            "DepreciatedConsumableOrder",
            "DepreciatedToID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedConsumableOrder_UpdatedByID",
            "DepreciatedConsumableOrder",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedConsumableOrderItem_ConsumablesOrderItemID",
            "DepreciatedConsumableOrderItem",
            "ConsumablesOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedConsumableOrderItem_DepreciatedConsumableOrderID",
            "DepreciatedConsumableOrderItem",
            "DepreciatedConsumableOrderID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedOrder_OrganizationID",
            "DepreciatedOrder",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedOrder_ResponsibleID",
            "DepreciatedOrder",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedOrder_StorageID",
            "DepreciatedOrder",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedOrderItem_ActReconciliationItemID",
            "DepreciatedOrderItem",
            "ActReconciliationItemID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedOrderItem_DepreciatedOrderID",
            "DepreciatedOrderItem",
            "DepreciatedOrderID");

        migrationBuilder.CreateIndex(
            "IX_DepreciatedOrderItem_ProductID",
            "DepreciatedOrderItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_DynamicProductPlacement_DynamicProductPlacementRowID",
            "DynamicProductPlacement",
            "DynamicProductPlacementRowID");

        migrationBuilder.CreateIndex(
            "IX_DynamicProductPlacementColumn_PackingListID",
            "DynamicProductPlacementColumn",
            "PackingListID");

        migrationBuilder.CreateIndex(
            "IX_DynamicProductPlacementColumn_SupplyOrderUkraineID",
            "DynamicProductPlacementColumn",
            "SupplyOrderUkraineID");

        migrationBuilder.CreateIndex(
            "IX_DynamicProductPlacementRow_DynamicProductPlacementColumnID",
            "DynamicProductPlacementRow",
            "DynamicProductPlacementColumnID");

        migrationBuilder.CreateIndex(
            "IX_DynamicProductPlacementRow_PackingListPackageOrderItemID",
            "DynamicProductPlacementRow",
            "PackingListPackageOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_DynamicProductPlacementRow_SupplyOrderUkraineItemID",
            "DynamicProductPlacementRow",
            "SupplyOrderUkraineItemID");

        migrationBuilder.CreateIndex(
            "IX_EcommerceDefaultPricing_PricingID",
            "EcommerceDefaultPricing",
            "PricingID");

        migrationBuilder.CreateIndex(
            "IX_EcommerceDefaultPricing_PromotionalPricingID",
            "EcommerceDefaultPricing",
            "PromotionalPricingID");

        migrationBuilder.CreateIndex(
            "IX_ExchangeRate_CurrencyID",
            "ExchangeRate",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_ExchangeRateHistory_ExchangeRateID",
            "ExchangeRateHistory",
            "ExchangeRateID");

        migrationBuilder.CreateIndex(
            "IX_ExchangeRateHistory_UpdatedByID",
            "ExchangeRateHistory",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_ExpiredBillUserNotification_CreatedByID",
            "ExpiredBillUserNotification",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_ExpiredBillUserNotification_LastViewedByID",
            "ExpiredBillUserNotification",
            "LastViewedByID");

        migrationBuilder.CreateIndex(
            "IX_ExpiredBillUserNotification_LockedByID",
            "ExpiredBillUserNotification",
            "LockedByID");

        migrationBuilder.CreateIndex(
            "IX_ExpiredBillUserNotification_ManagerID",
            "ExpiredBillUserNotification",
            "ManagerID");

        migrationBuilder.CreateIndex(
            "IX_ExpiredBillUserNotification_ProcessedByID",
            "ExpiredBillUserNotification",
            "ProcessedByID");

        migrationBuilder.CreateIndex(
            "IX_ExpiredBillUserNotification_SaleID",
            "ExpiredBillUserNotification",
            "SaleID");

        migrationBuilder.CreateIndex(
            "IX_FilterItemTranslation_FilterItemID",
            "FilterItemTranslation",
            "FilterItemID");

        migrationBuilder.CreateIndex(
            "IX_FilterOperationItemTranslation_FilterOperationItemID",
            "FilterOperationItemTranslation",
            "FilterOperationItemID");

        migrationBuilder.CreateIndex(
            "IX_GovCrossExchangeRate_CurrencyFromID",
            "GovCrossExchangeRate",
            "CurrencyFromID");

        migrationBuilder.CreateIndex(
            "IX_GovCrossExchangeRate_CurrencyToID",
            "GovCrossExchangeRate",
            "CurrencyToID");

        migrationBuilder.CreateIndex(
            "IX_GovCrossExchangeRateHistory_GovCrossExchangeRateID",
            "GovCrossExchangeRateHistory",
            "GovCrossExchangeRateID");

        migrationBuilder.CreateIndex(
            "IX_GovCrossExchangeRateHistory_UpdatedByID",
            "GovCrossExchangeRateHistory",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_GovExchangeRate_CurrencyID",
            "GovExchangeRate",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_GovExchangeRateHistory_GovExchangeRateID",
            "GovExchangeRateHistory",
            "GovExchangeRateID");

        migrationBuilder.CreateIndex(
            "IX_GovExchangeRateHistory_UpdatedByID",
            "GovExchangeRateHistory",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_ClientAgreementID",
            "IncomePaymentOrder",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_ClientID",
            "IncomePaymentOrder",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_ColleagueID",
            "IncomePaymentOrder",
            "ColleagueID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_CurrencyID",
            "IncomePaymentOrder",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_OrganizationClientAgreementID",
            "IncomePaymentOrder",
            "OrganizationClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_OrganizationClientID",
            "IncomePaymentOrder",
            "OrganizationClientID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_OrganizationID",
            "IncomePaymentOrder",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_PaymentRegisterID",
            "IncomePaymentOrder",
            "PaymentRegisterID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_SadID",
            "IncomePaymentOrder",
            "SadID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_TaxFreeID",
            "IncomePaymentOrder",
            "TaxFreeID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_UserID",
            "IncomePaymentOrder",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrderSale_IncomePaymentOrderID",
            "IncomePaymentOrderSale",
            "IncomePaymentOrderID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrderSale_ReSaleID",
            "IncomePaymentOrderSale",
            "ReSaleID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrderSale_SaleID",
            "IncomePaymentOrderSale",
            "SaleID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_ContainerServiceID",
            "InvoiceDocument",
            "ContainerServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_CustomAgencyServiceID",
            "InvoiceDocument",
            "CustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_CustomServiceID",
            "InvoiceDocument",
            "CustomServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_MergedServiceID",
            "InvoiceDocument",
            "MergedServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_PackingListID",
            "InvoiceDocument",
            "PackingListID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_PlaneDeliveryServiceID",
            "InvoiceDocument",
            "PlaneDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_PortCustomAgencyServiceID",
            "InvoiceDocument",
            "PortCustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_PortWorkServiceID",
            "InvoiceDocument",
            "PortWorkServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_SupplyInvoiceID",
            "InvoiceDocument",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_SupplyOrderPolandPaymentDeliveryProtocolID",
            "InvoiceDocument",
            "SupplyOrderPolandPaymentDeliveryProtocolID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_TransportationServiceID",
            "InvoiceDocument",
            "TransportationServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_VehicleDeliveryServiceID",
            "InvoiceDocument",
            "VehicleDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_InvoiceDocument_VehicleServiceId",
            "InvoiceDocument",
            "VehicleServiceId");

        migrationBuilder.CreateIndex(
            "IX_MeasureUnitTranslation_MeasureUnitID",
            "MeasureUnitTranslation",
            "MeasureUnitID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_AccountingActProvidingServiceID",
            "MergedService",
            "AccountingActProvidingServiceID",
            unique: true,
            filter: "[AccountingActProvidingServiceID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_MergedService_AccountingPaymentTaskID",
            "MergedService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_ActProvidingServiceDocumentID",
            "MergedService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_MergedService_ActProvidingServiceID",
            "MergedService",
            "ActProvidingServiceID",
            unique: true,
            filter: "[ActProvidingServiceID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_MergedService_ConsumableProductID",
            "MergedService",
            "ConsumableProductID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_DeliveryProductProtocolID",
            "MergedService",
            "DeliveryProductProtocolID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_SupplyInformationTaskID",
            "MergedService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_MergedService_SupplyOrderID",
            "MergedService",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_SupplyOrderUkraineID",
            "MergedService",
            "SupplyOrderUkraineID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_SupplyOrganizationAgreementID",
            "MergedService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_SupplyOrganizationID",
            "MergedService",
            "SupplyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_SupplyPaymentTaskID",
            "MergedService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_MergedService_SupplyServiceAccountDocumentID",
            "MergedService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_MergedService_UserID",
            "MergedService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_MisplacedSale_RetailClientID",
            "MisplacedSale",
            "RetailClientID");

        migrationBuilder.CreateIndex(
            "IX_MisplacedSale_UserID",
            "MisplacedSale",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_Order_ClientAgreementID",
            "Order",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_Order_ClientShoppingCartID",
            "Order",
            "ClientShoppingCartID");

        migrationBuilder.CreateIndex(
            "IX_Order_UserID",
            "Order",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_AssignedSpecificationID",
            "OrderItem",
            "AssignedSpecificationID");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_ClientShoppingCartID",
            "OrderItem",
            "ClientShoppingCartID");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_DiscountUpdatedByID",
            "OrderItem",
            "DiscountUpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_MisplacedSaleId",
            "OrderItem",
            "MisplacedSaleId");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_OfferProcessingStatusChangedByID",
            "OrderItem",
            "OfferProcessingStatusChangedByID");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_OrderID",
            "OrderItem",
            "OrderID");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_ProductID",
            "OrderItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_OrderItem_UserId",
            "OrderItem",
            "UserId");

        migrationBuilder.CreateIndex(
            "IX_OrderItemBaseShiftStatus_OrderItemID",
            "OrderItemBaseShiftStatus",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_OrderItemBaseShiftStatus_SaleID",
            "OrderItemBaseShiftStatus",
            "SaleID");

        migrationBuilder.CreateIndex(
            "IX_OrderItemBaseShiftStatus_UserID",
            "OrderItemBaseShiftStatus",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_OrderItemMerged_OldOrderID",
            "OrderItemMerged",
            "OldOrderID");

        migrationBuilder.CreateIndex(
            "IX_OrderItemMerged_OldOrderItemID",
            "OrderItemMerged",
            "OldOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_OrderItemMerged_OrderItemID",
            "OrderItemMerged",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_OrderItemMovement_OrderItemID",
            "OrderItemMovement",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_OrderItemMovement_UserID",
            "OrderItemMovement",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_OrderPackage_OrderID",
            "OrderPackage",
            "OrderID");

        migrationBuilder.CreateIndex(
            "IX_OrderPackageItem_OrderItemID",
            "OrderPackageItem",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_OrderPackageItem_OrderPackageID",
            "OrderPackageItem",
            "OrderPackageID");

        migrationBuilder.CreateIndex(
            "IX_OrderPackageUser_OrderPackageID",
            "OrderPackageUser",
            "OrderPackageID");

        migrationBuilder.CreateIndex(
            "IX_OrderPackageUser_UserID",
            "OrderPackageUser",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_OrderProductSpecification_ProductSpecificationId",
            "OrderProductSpecification",
            "ProductSpecificationId");

        migrationBuilder.CreateIndex(
            "IX_OrderProductSpecification_SadId",
            "OrderProductSpecification",
            "SadId");

        migrationBuilder.CreateIndex(
            "IX_OrderProductSpecification_SupplyInvoiceId",
            "OrderProductSpecification",
            "SupplyInvoiceId");

        migrationBuilder.CreateIndex(
            "IX_Organization_CurrencyID",
            "Organization",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_Organization_StorageID",
            "Organization",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_Organization_TaxInspectionID",
            "Organization",
            "TaxInspectionID");

        migrationBuilder.CreateIndex(
            "IX_Organization_VatRateID",
            "Organization",
            "VatRateID");

        migrationBuilder.CreateIndex(
            "IX_OrganizationClientAgreement_AgreementTypeCivilCodeID",
            "OrganizationClientAgreement",
            "AgreementTypeCivilCodeID");

        migrationBuilder.CreateIndex(
            "IX_OrganizationClientAgreement_CurrencyID",
            "OrganizationClientAgreement",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_OrganizationClientAgreement_OrganizationClientID",
            "OrganizationClientAgreement",
            "OrganizationClientID");

        migrationBuilder.CreateIndex(
            "IX_OrganizationClientAgreement_TaxAccountingSchemeID",
            "OrganizationClientAgreement",
            "TaxAccountingSchemeID");

        migrationBuilder.CreateIndex(
            "IX_OrganizationTranslation_OrganizationID",
            "OrganizationTranslation",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_ClientAgreementID",
            "OutcomePaymentOrder",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_ColleagueID",
            "OutcomePaymentOrder",
            "ColleagueID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_ConsumableProductOrganizationID",
            "OutcomePaymentOrder",
            "ConsumableProductOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_OrganizationClientAgreementID",
            "OutcomePaymentOrder",
            "OrganizationClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_OrganizationClientID",
            "OutcomePaymentOrder",
            "OrganizationClientID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_OrganizationID",
            "OutcomePaymentOrder",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_PaymentCurrencyRegisterID",
            "OutcomePaymentOrder",
            "PaymentCurrencyRegisterID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_SadID",
            "OutcomePaymentOrder",
            "SadID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_SupplyOrderPolandPaymentDeliveryProtocolID",
            "OutcomePaymentOrder",
            "SupplyOrderPolandPaymentDeliveryProtocolID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_SupplyOrganizationAgreementID",
            "OutcomePaymentOrder",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_TaxFreeID",
            "OutcomePaymentOrder",
            "TaxFreeID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_UserID",
            "OutcomePaymentOrder",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrderConsumablesOrder_ConsumablesOrderID",
            "OutcomePaymentOrderConsumablesOrder",
            "ConsumablesOrderID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrderConsumablesOrder_OutcomePaymentOrderID",
            "OutcomePaymentOrderConsumablesOrder",
            "OutcomePaymentOrderID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrderSupplyPaymentTask_OutcomePaymentOrderID",
            "OutcomePaymentOrderSupplyPaymentTask",
            "OutcomePaymentOrderID");

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrderSupplyPaymentTask_SupplyPaymentTaskID",
            "OutcomePaymentOrderSupplyPaymentTask",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_PackingList_ContainerServiceID",
            "PackingList",
            "ContainerServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingList_SupplyInvoiceID",
            "PackingList",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingList_VehicleServiceId",
            "PackingList",
            "VehicleServiceId");

        migrationBuilder.CreateIndex(
            "IX_PackingListDocument_SupplyOrderID",
            "PackingListDocument",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackage_PackingListID",
            "PackingListPackage",
            "PackingListID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItem_PackingListID",
            "PackingListPackageOrderItem",
            "PackingListID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItem_PackingListPackageID",
            "PackingListPackageOrderItem",
            "PackingListPackageID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItem_SupplyInvoiceOrderItemID",
            "PackingListPackageOrderItem",
            "SupplyInvoiceOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_BillOfLadingServiceID",
            "PackingListPackageOrderItemSupplyService",
            "BillOfLadingServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_ContainerServiceID",
            "PackingListPackageOrderItemSupplyService",
            "ContainerServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_CurrencyID",
            "PackingListPackageOrderItemSupplyService",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_CustomAgencyServiceID",
            "PackingListPackageOrderItemSupplyService",
            "CustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_CustomServiceID",
            "PackingListPackageOrderItemSupplyService",
            "CustomServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_MergedServiceID",
            "PackingListPackageOrderItemSupplyService",
            "MergedServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_PackingListPackageOrderItemID",
            "PackingListPackageOrderItemSupplyService",
            "PackingListPackageOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_PlaneDeliveryServiceID",
            "PackingListPackageOrderItemSupplyService",
            "PlaneDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_PortCustomAgencyServiceID",
            "PackingListPackageOrderItemSupplyService",
            "PortCustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_PortWorkServiceID",
            "PackingListPackageOrderItemSupplyService",
            "PortWorkServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_TransportationServiceID",
            "PackingListPackageOrderItemSupplyService",
            "TransportationServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_VehicleDeliveryServiceID",
            "PackingListPackageOrderItemSupplyService",
            "VehicleDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_PackingListPackageOrderItemSupplyService_VehicleServiceID",
            "PackingListPackageOrderItemSupplyService",
            "VehicleServiceID");

        migrationBuilder.CreateIndex(
            "IX_PaymentCostMovementOperation_CompanyCarFuelingID",
            "PaymentCostMovementOperation",
            "CompanyCarFuelingID",
            unique: true,
            filter: "[CompanyCarFuelingID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PaymentCostMovementOperation_ConsumablesOrderItemID",
            "PaymentCostMovementOperation",
            "ConsumablesOrderItemID",
            unique: true,
            filter: "[ConsumablesOrderItemID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PaymentCostMovementOperation_DepreciatedConsumableOrderItemID",
            "PaymentCostMovementOperation",
            "DepreciatedConsumableOrderItemID",
            unique: true,
            filter: "[DepreciatedConsumableOrderItemID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PaymentCostMovementOperation_PaymentCostMovementID",
            "PaymentCostMovementOperation",
            "PaymentCostMovementID");

        migrationBuilder.CreateIndex(
            "IX_PaymentCostMovementTranslation_PaymentCostMovementID",
            "PaymentCostMovementTranslation",
            "PaymentCostMovementID");

        migrationBuilder.CreateIndex(
            "IX_PaymentCurrencyRegister_CurrencyID",
            "PaymentCurrencyRegister",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_PaymentCurrencyRegister_PaymentRegisterID",
            "PaymentCurrencyRegister",
            "PaymentRegisterID");

        migrationBuilder.CreateIndex(
            "IX_PaymentDeliveryDocument_SupplyOrderPaymentDeliveryProtocolID",
            "PaymentDeliveryDocument",
            "SupplyOrderPaymentDeliveryProtocolID");

        migrationBuilder.CreateIndex(
            "IX_PaymentMovementOperation_IncomePaymentOrderID",
            "PaymentMovementOperation",
            "IncomePaymentOrderID",
            unique: true,
            filter: "[IncomePaymentOrderID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PaymentMovementOperation_OutcomePaymentOrderID",
            "PaymentMovementOperation",
            "OutcomePaymentOrderID",
            unique: true,
            filter: "[OutcomePaymentOrderID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PaymentMovementOperation_PaymentMovementID",
            "PaymentMovementOperation",
            "PaymentMovementID");

        migrationBuilder.CreateIndex(
            "IX_PaymentMovementOperation_PaymentRegisterCurrencyExchangeID",
            "PaymentMovementOperation",
            "PaymentRegisterCurrencyExchangeID",
            unique: true,
            filter: "[PaymentRegisterCurrencyExchangeID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PaymentMovementOperation_PaymentRegisterTransferID",
            "PaymentMovementOperation",
            "PaymentRegisterTransferID",
            unique: true,
            filter: "[PaymentRegisterTransferID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PaymentMovementTranslation_PaymentMovementID",
            "PaymentMovementTranslation",
            "PaymentMovementID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegister_OrganizationID",
            "PaymentRegister",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegisterCurrencyExchange_CurrencyTraderID",
            "PaymentRegisterCurrencyExchange",
            "CurrencyTraderID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegisterCurrencyExchange_FromPaymentCurrencyRegisterID",
            "PaymentRegisterCurrencyExchange",
            "FromPaymentCurrencyRegisterID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegisterCurrencyExchange_ToPaymentCurrencyRegisterID",
            "PaymentRegisterCurrencyExchange",
            "ToPaymentCurrencyRegisterID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegisterCurrencyExchange_UserID",
            "PaymentRegisterCurrencyExchange",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegisterTransfer_FromPaymentCurrencyRegisterID",
            "PaymentRegisterTransfer",
            "FromPaymentCurrencyRegisterID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegisterTransfer_ToPaymentCurrencyRegisterID",
            "PaymentRegisterTransfer",
            "ToPaymentCurrencyRegisterID");

        migrationBuilder.CreateIndex(
            "IX_PaymentRegisterTransfer_UserID",
            "PaymentRegisterTransfer",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_PerfectClient_ClientTypeRoleID",
            "PerfectClient",
            "ClientTypeRoleID");

        migrationBuilder.CreateIndex(
            "IX_PerfectClientTranslation_PerfectClientID",
            "PerfectClientTranslation",
            "PerfectClientID");

        migrationBuilder.CreateIndex(
            "IX_PerfectClientValue_PerfectClientID",
            "PerfectClientValue",
            "PerfectClientID");

        migrationBuilder.CreateIndex(
            "IX_PerfectClientValueTranslation_PerfectClientValueID",
            "PerfectClientValueTranslation",
            "PerfectClientValueID");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_AccountingActProvidingServiceId",
            "PlaneDeliveryService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_AccountingPaymentTaskID",
            "PlaneDeliveryService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_ActProvidingServiceDocumentID",
            "PlaneDeliveryService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_ActProvidingServiceId",
            "PlaneDeliveryService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_PlaneDeliveryOrganizationID",
            "PlaneDeliveryService",
            "PlaneDeliveryOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_SupplyInformationTaskID",
            "PlaneDeliveryService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_SupplyOrganizationAgreementID",
            "PlaneDeliveryService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_SupplyPaymentTaskID",
            "PlaneDeliveryService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_SupplyServiceAccountDocumentID",
            "PlaneDeliveryService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PlaneDeliveryService_UserID",
            "PlaneDeliveryService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_AccountingActProvidingServiceId",
            "PortCustomAgencyService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_AccountingPaymentTaskID",
            "PortCustomAgencyService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_ActProvidingServiceDocumentID",
            "PortCustomAgencyService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_ActProvidingServiceId",
            "PortCustomAgencyService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_PortCustomAgencyOrganizationID",
            "PortCustomAgencyService",
            "PortCustomAgencyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_SupplyInformationTaskID",
            "PortCustomAgencyService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_SupplyOrganizationAgreementID",
            "PortCustomAgencyService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_SupplyPaymentTaskID",
            "PortCustomAgencyService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_SupplyServiceAccountDocumentID",
            "PortCustomAgencyService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PortCustomAgencyService_UserID",
            "PortCustomAgencyService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_AccountingActProvidingServiceId",
            "PortWorkService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_AccountingPaymentTaskID",
            "PortWorkService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_ActProvidingServiceDocumentID",
            "PortWorkService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_ActProvidingServiceId",
            "PortWorkService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_PortWorkOrganizationID",
            "PortWorkService",
            "PortWorkOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_SupplyInformationTaskID",
            "PortWorkService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_SupplyOrganizationAgreementID",
            "PortWorkService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_SupplyPaymentTaskID",
            "PortWorkService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_SupplyServiceAccountDocumentID",
            "PortWorkService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_PortWorkService_UserID",
            "PortWorkService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_PreOrder_ClientID",
            "PreOrder",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_PreOrder_ProductID",
            "PreOrder",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_PriceTypeTranslation_PriceTypeID",
            "PriceTypeTranslation",
            "PriceTypeID");

        migrationBuilder.CreateIndex(
            "IX_Pricing_BasePricingID",
            "Pricing",
            "BasePricingID");

        migrationBuilder.CreateIndex(
            "IX_Pricing_CurrencyID",
            "Pricing",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_Pricing_PriceTypeID",
            "Pricing",
            "PriceTypeID");

        migrationBuilder.CreateIndex(
            "IX_PricingProductGroupDiscount_BasePricingID",
            "PricingProductGroupDiscount",
            "BasePricingID");

        migrationBuilder.CreateIndex(
            "IX_PricingProductGroupDiscount_PricingID",
            "PricingProductGroupDiscount",
            "PricingID");

        migrationBuilder.CreateIndex(
            "IX_PricingProductGroupDiscount_ProductGroupID",
            "PricingProductGroupDiscount",
            "ProductGroupID");

        migrationBuilder.CreateIndex(
            "IX_PricingTranslation_PricingID_CultureCode_Deleted",
            "PricingTranslation",
            new[] { "PricingID", "CultureCode", "Deleted" });

        migrationBuilder.CreateIndex(
            "IX_Product_MeasureUnitID",
            "Product",
            "MeasureUnitID");

        migrationBuilder.CreateIndex(
            "IX_Product_NetUID",
            "Product",
            "NetUID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_Product_Description_Deleted",
            "Product",
            new[] { "Description", "Deleted" });

        migrationBuilder.CreateIndex(
            "IX_Product_MainOriginalNumber_Deleted",
            "Product",
            new[] { "MainOriginalNumber", "Deleted" });

        migrationBuilder.CreateIndex(
            "IX_Product_Name_Deleted",
            "Product",
            new[] { "Name", "Deleted" });

        migrationBuilder.CreateIndex(
            "IX_Product_VendorCode_Deleted",
            "Product",
            new[] { "VendorCode", "Deleted" });

        migrationBuilder.CreateIndex(
            "IX_Product_Deleted_SearchNamePL_SearchVendorCode",
            "Product",
            new[] { "Deleted", "SearchNamePL", "SearchVendorCode" });

        migrationBuilder.CreateIndex(
            "IX_Product_Deleted_SearchNameUA_SearchVendorCode",
            "Product",
            new[] { "Deleted", "SearchNameUA", "SearchVendorCode" });

        migrationBuilder.CreateIndex(
            "IX_ProductAnalogue_AnalogueProductID",
            "ProductAnalogue",
            "AnalogueProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductAnalogue_BaseProductID",
            "ProductAnalogue",
            "BaseProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductAnalogue_Deleted_AnalogueProductID",
            "ProductAnalogue",
            new[] { "Deleted", "AnalogueProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductAnalogue_Deleted_BaseProductID",
            "ProductAnalogue",
            new[] { "Deleted", "BaseProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductAvailability_Amount",
            "ProductAvailability",
            "Amount");

        migrationBuilder.CreateIndex(
            "IX_ProductAvailability_ProductID",
            "ProductAvailability",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductAvailability_Deleted_ProductID",
            "ProductAvailability",
            new[] { "Deleted", "ProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductAvailability_ID_Deleted_ProductID",
            "ProductAvailability",
            new[] { "ID", "Deleted", "ProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductAvailability_ID_Deleted_StorageID",
            "ProductAvailability",
            new[] { "ID", "Deleted", "StorageID" });

        migrationBuilder.CreateIndex(
            "IX_ProductAvailability_StorageID_Amount_ProductID_Deleted",
            "ProductAvailability",
            new[] { "StorageID", "Amount", "ProductID", "Deleted" });

        migrationBuilder.CreateIndex(
            "IX_ProductCapitalization_OrganizationID",
            "ProductCapitalization",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_ProductCapitalization_ResponsibleID",
            "ProductCapitalization",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_ProductCapitalization_StorageID",
            "ProductCapitalization",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_ProductCapitalizationItem_ProductCapitalizationID",
            "ProductCapitalizationItem",
            "ProductCapitalizationID");

        migrationBuilder.CreateIndex(
            "IX_ProductCapitalizationItem_ProductID",
            "ProductCapitalizationItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductCarBrand_CarBrandID",
            "ProductCarBrand",
            "CarBrandID");

        migrationBuilder.CreateIndex(
            "IX_ProductCarBrand_ProductID",
            "ProductCarBrand",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductCategory_CategoryID",
            "ProductCategory",
            "CategoryID");

        migrationBuilder.CreateIndex(
            "IX_ProductCategory_ProductID",
            "ProductCategory",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductGroupDiscount_ClientAgreementID",
            "ProductGroupDiscount",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_ProductGroupDiscount_ProductGroupID",
            "ProductGroupDiscount",
            "ProductGroupID");

        migrationBuilder.CreateIndex(
            "IX_ProductImage_ProductID",
            "ProductImage",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductIncome_StorageID",
            "ProductIncome",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_ProductIncome_UserID",
            "ProductIncome",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_ProductIncomeItem_ActReconciliationItemID",
            "ProductIncomeItem",
            "ActReconciliationItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductIncomeItem_PackingListPackageOrderItemID",
            "ProductIncomeItem",
            "PackingListPackageOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductIncomeItem_ProductCapitalizationItemID",
            "ProductIncomeItem",
            "ProductCapitalizationItemID",
            unique: true,
            filter: "[ProductCapitalizationItemID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_ProductIncomeItem_ProductIncomeID",
            "ProductIncomeItem",
            "ProductIncomeID");

        migrationBuilder.CreateIndex(
            "IX_ProductIncomeItem_SaleReturnItemID",
            "ProductIncomeItem",
            "SaleReturnItemID",
            unique: true,
            filter: "[SaleReturnItemID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_ProductIncomeItem_SupplyOrderUkraineItemID",
            "ProductIncomeItem",
            "SupplyOrderUkraineItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocation_DepreciatedOrderItemID",
            "ProductLocation",
            "DepreciatedOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocation_OrderItemID",
            "ProductLocation",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocation_ProductPlacementID",
            "ProductLocation",
            "ProductPlacementID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocation_ProductTransferItemID",
            "ProductLocation",
            "ProductTransferItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocation_StorageID",
            "ProductLocation",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_ProductOriginalNumber_OriginalNumberID",
            "ProductOriginalNumber",
            "OriginalNumberID");

        migrationBuilder.CreateIndex(
            "IX_ProductOriginalNumber_ProductID",
            "ProductOriginalNumber",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductOriginalNumber_Deleted_OriginalNumberID",
            "ProductOriginalNumber",
            new[] { "Deleted", "OriginalNumberID" });

        migrationBuilder.CreateIndex(
            "IX_ProductOriginalNumber_Deleted_ProductID",
            "ProductOriginalNumber",
            new[] { "Deleted", "ProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductPlacement_ConsignmentItemID",
            "ProductPlacement",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacement_PackingListPackageOrderItemID",
            "ProductPlacement",
            "PackingListPackageOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacement_ProductID",
            "ProductPlacement",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacement_ProductIncomeItemID",
            "ProductPlacement",
            "ProductIncomeItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacement_StorageID",
            "ProductPlacement",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacement_SupplyOrderUkraineItemID",
            "ProductPlacement",
            "SupplyOrderUkraineItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementMovement_FromProductPlacementID",
            "ProductPlacementMovement",
            "FromProductPlacementID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementMovement_ResponsibleID",
            "ProductPlacementMovement",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementMovement_ToProductPlacementID",
            "ProductPlacementMovement",
            "ToProductPlacementID");

        migrationBuilder.CreateIndex(
            "IX_ProductPricing_PricingID",
            "ProductPricing",
            "PricingID");

        migrationBuilder.CreateIndex(
            "IX_ProductPricing_ProductID",
            "ProductPricing",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductPricing_Deleted_PricingID",
            "ProductPricing",
            new[] { "Deleted", "PricingID" });

        migrationBuilder.CreateIndex(
            "IX_ProductPricing_Deleted_ProductID",
            "ProductPricing",
            new[] { "Deleted", "ProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductProductGroup_ProductGroupID",
            "ProductProductGroup",
            "ProductGroupID");

        migrationBuilder.CreateIndex(
            "IX_ProductProductGroup_ProductID",
            "ProductProductGroup",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductProductGroup_Deleted_ProductGroupID",
            "ProductProductGroup",
            new[] { "Deleted", "ProductGroupID" });

        migrationBuilder.CreateIndex(
            "IX_ProductProductGroup_Deleted_ProductID",
            "ProductProductGroup",
            new[] { "Deleted", "ProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductProductGroup_Deleted_ProductID_ProductGroupID",
            "ProductProductGroup",
            new[] { "Deleted", "ProductID", "ProductGroupID" });

        migrationBuilder.CreateIndex(
            "IX_ProductReservation_ConsignmentItemID",
            "ProductReservation",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductReservation_OrderItemID",
            "ProductReservation",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductReservation_ProductAvailabilityID",
            "ProductReservation",
            "ProductAvailabilityID");

        migrationBuilder.CreateIndex(
            "IX_ProductSet_BaseProductID",
            "ProductSet",
            "BaseProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductSet_ComponentProductID",
            "ProductSet",
            "ComponentProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductSet_Deleted_BaseProductID",
            "ProductSet",
            new[] { "Deleted", "BaseProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductSet_Deleted_ComponentProductID",
            "ProductSet",
            new[] { "Deleted", "ComponentProductID" });

        migrationBuilder.CreateIndex(
            "IX_ProductSlug_ProductID",
            "ProductSlug",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductSpecification_AddedByID",
            "ProductSpecification",
            "AddedByID");

        migrationBuilder.CreateIndex(
            "IX_ProductSpecification_ProductID",
            "ProductSpecification",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductSubGroup_RootProductGroupID",
            "ProductSubGroup",
            "RootProductGroupID");

        migrationBuilder.CreateIndex(
            "IX_ProductSubGroup_SubProductGroupID",
            "ProductSubGroup",
            "SubProductGroupID");

        migrationBuilder.CreateIndex(
            "IX_ProductTransfer_FromStorageID",
            "ProductTransfer",
            "FromStorageID");

        migrationBuilder.CreateIndex(
            "IX_ProductTransfer_OrganizationID",
            "ProductTransfer",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_ProductTransfer_ResponsibleID",
            "ProductTransfer",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_ProductTransfer_ToStorageID",
            "ProductTransfer",
            "ToStorageID");

        migrationBuilder.CreateIndex(
            "IX_ProductTransferItem_ActReconciliationItemID",
            "ProductTransferItem",
            "ActReconciliationItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductTransferItem_ProductID",
            "ProductTransferItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductTransferItem_ProductTransferID",
            "ProductTransferItem",
            "ProductTransferID");

        migrationBuilder.CreateIndex(
            "IX_ProductWriteOffRule_CreatedByID",
            "ProductWriteOffRule",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_ProductWriteOffRule_ProductGroupID",
            "ProductWriteOffRule",
            "ProductGroupID");

        migrationBuilder.CreateIndex(
            "IX_ProductWriteOffRule_ProductID",
            "ProductWriteOffRule",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ProductWriteOffRule_UpdatedByID",
            "ProductWriteOffRule",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_ProFormDocument_SupplyProFormID",
            "ProFormDocument",
            "SupplyProFormID");

        migrationBuilder.CreateIndex(
            "IX_ProviderPricing_BasePricingID",
            "ProviderPricing",
            "BasePricingID");

        migrationBuilder.CreateIndex(
            "IX_ProviderPricing_CurrencyID",
            "ProviderPricing",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_RegionCode_RegionID",
            "RegionCode",
            "RegionID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_BaseLifeCycleStatusID",
            "ReSale",
            "BaseLifeCycleStatusID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_BaseSalePaymentStatusID",
            "ReSale",
            "BaseSalePaymentStatusID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_ChangedToInvoiceByID",
            "ReSale",
            "ChangedToInvoiceByID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_ClientAgreementID",
            "ReSale",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_FromStorageID",
            "ReSale",
            "FromStorageID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_OrganizationID",
            "ReSale",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_SaleNumberID",
            "ReSale",
            "SaleNumberID");

        migrationBuilder.CreateIndex(
            "IX_ReSale_UserID",
            "ReSale",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleAvailability_ConsignmentItemID",
            "ReSaleAvailability",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleAvailability_DepreciatedOrderItemID",
            "ReSaleAvailability",
            "DepreciatedOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleAvailability_OrderItemID",
            "ReSaleAvailability",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleAvailability_ProductAvailabilityID",
            "ReSaleAvailability",
            "ProductAvailabilityID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleAvailability_ProductReservationID",
            "ReSaleAvailability",
            "ProductReservationID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleAvailability_ProductTransferItemID",
            "ReSaleAvailability",
            "ProductTransferItemID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleItem_ProductID",
            "ReSaleItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleItem_ReSaleAvailabilityID",
            "ReSaleItem",
            "ReSaleAvailabilityID");

        migrationBuilder.CreateIndex(
            "IX_ReSaleItem_ReSaleID",
            "ReSaleItem",
            "ReSaleID");

        migrationBuilder.CreateIndex(
            "IX_ResponsibilityDeliveryProtocol_SupplyOrderID",
            "ResponsibilityDeliveryProtocol",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_ResponsibilityDeliveryProtocol_UserId",
            "ResponsibilityDeliveryProtocol",
            "UserId");

        migrationBuilder.CreateIndex(
            "IX_RetailClient_EcommerceRegionId",
            "RetailClient",
            "EcommerceRegionId");

        migrationBuilder.CreateIndex(
            "IX_RetailClientPaymentImage_RetailClientId",
            "RetailClientPaymentImage",
            "RetailClientId");

        migrationBuilder.CreateIndex(
            "IX_Sad_ClientAgreementID",
            "Sad",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_Sad_ClientID",
            "Sad",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_Sad_OrganizationClientAgreementID",
            "Sad",
            "OrganizationClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_Sad_OrganizationClientID",
            "Sad",
            "OrganizationClientID");

        migrationBuilder.CreateIndex(
            "IX_Sad_OrganizationID",
            "Sad",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_Sad_ResponsibleID",
            "Sad",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_Sad_StathamCarID",
            "Sad",
            "StathamCarID");

        migrationBuilder.CreateIndex(
            "IX_Sad_StathamID",
            "Sad",
            "StathamID");

        migrationBuilder.CreateIndex(
            "IX_Sad_StathamPassportID",
            "Sad",
            "StathamPassportID");

        migrationBuilder.CreateIndex(
            "IX_Sad_SupplyOrderUkraineID",
            "Sad",
            "SupplyOrderUkraineID",
            unique: true,
            filter: "[SupplyOrderUkraineID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_SadDocument_SadID",
            "SadDocument",
            "SadID");

        migrationBuilder.CreateIndex(
            "IX_SadItem_ConsignmentItemID",
            "SadItem",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_SadItem_OrderItemID",
            "SadItem",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_SadItem_SadID",
            "SadItem",
            "SadID");

        migrationBuilder.CreateIndex(
            "IX_SadItem_SupplierID",
            "SadItem",
            "SupplierID");

        migrationBuilder.CreateIndex(
            "IX_SadItem_SupplyOrderUkraineCartItemID",
            "SadItem",
            "SupplyOrderUkraineCartItemID");

        migrationBuilder.CreateIndex(
            "IX_SadPallet_SadID",
            "SadPallet",
            "SadID");

        migrationBuilder.CreateIndex(
            "IX_SadPallet_SadPalletTypeID",
            "SadPallet",
            "SadPalletTypeID");

        migrationBuilder.CreateIndex(
            "IX_SadPalletItem_SadItemID",
            "SadPalletItem",
            "SadItemID");

        migrationBuilder.CreateIndex(
            "IX_SadPalletItem_SadPalletID",
            "SadPalletItem",
            "SadPalletID");

        migrationBuilder.CreateIndex(
            "IX_Sale_BaseLifeCycleStatusID",
            "Sale",
            "BaseLifeCycleStatusID");

        migrationBuilder.CreateIndex(
            "IX_Sale_BaseSalePaymentStatusID",
            "Sale",
            "BaseSalePaymentStatusID");

        migrationBuilder.CreateIndex(
            "IX_Sale_ChangedToInvoiceByID",
            "Sale",
            "ChangedToInvoiceByID");

        migrationBuilder.CreateIndex(
            "IX_Sale_ClientAgreementID",
            "Sale",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_Sale_DeliveryRecipientAddressID",
            "Sale",
            "DeliveryRecipientAddressID");

        migrationBuilder.CreateIndex(
            "IX_Sale_DeliveryRecipientID",
            "Sale",
            "DeliveryRecipientID");

        migrationBuilder.CreateIndex(
            "IX_Sale_MisplacedSaleId",
            "Sale",
            "MisplacedSaleId",
            unique: true,
            filter: "[MisplacedSaleId] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_Sale_NetUID",
            "Sale",
            "NetUID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_Sale_OrderID",
            "Sale",
            "OrderID");

        migrationBuilder.CreateIndex(
            "IX_Sale_RetailClientId",
            "Sale",
            "RetailClientId");

        migrationBuilder.CreateIndex(
            "IX_Sale_SadID",
            "Sale",
            "SadID");

        migrationBuilder.CreateIndex(
            "IX_Sale_SaleInvoiceDocumentID",
            "Sale",
            "SaleInvoiceDocumentID");

        migrationBuilder.CreateIndex(
            "IX_Sale_SaleInvoiceNumberID",
            "Sale",
            "SaleInvoiceNumberID");

        migrationBuilder.CreateIndex(
            "IX_Sale_SaleNumberID",
            "Sale",
            "SaleNumberID");

        migrationBuilder.CreateIndex(
            "IX_Sale_ShiftStatusID",
            "Sale",
            "ShiftStatusID");

        migrationBuilder.CreateIndex(
            "IX_Sale_TaxFreePackListID",
            "Sale",
            "TaxFreePackListID");

        migrationBuilder.CreateIndex(
            "IX_Sale_TransporterID",
            "Sale",
            "TransporterID");

        migrationBuilder.CreateIndex(
            "IX_Sale_UserID",
            "Sale",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SaleExchangeRate_ExchangeRateID",
            "SaleExchangeRate",
            "ExchangeRateID");

        migrationBuilder.CreateIndex(
            "IX_SaleExchangeRate_SaleID",
            "SaleExchangeRate",
            "SaleID");

        migrationBuilder.CreateIndex(
            "IX_SaleFutureReservation_ClientID",
            "SaleFutureReservation",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_SaleFutureReservation_ProductID",
            "SaleFutureReservation",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_SaleFutureReservation_SupplyOrderID",
            "SaleFutureReservation",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SaleMerged_InputSaleID",
            "SaleMerged",
            "InputSaleID");

        migrationBuilder.CreateIndex(
            "IX_SaleMerged_OutputSaleID_Deleted",
            "SaleMerged",
            new[] { "OutputSaleID", "Deleted" });

        migrationBuilder.CreateIndex(
            "IX_SaleNumber_OrganizationID",
            "SaleNumber",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturn_CanceledByID",
            "SaleReturn",
            "CanceledByID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturn_ClientID",
            "SaleReturn",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturn_CreatedByID",
            "SaleReturn",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturn_UpdatedByID",
            "SaleReturn",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItem_CreatedByID",
            "SaleReturnItem",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItem_MoneyReturnedByID",
            "SaleReturnItem",
            "MoneyReturnedByID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItem_OrderItemID",
            "SaleReturnItem",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItem_SaleReturnID",
            "SaleReturnItem",
            "SaleReturnID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItem_StorageID",
            "SaleReturnItem",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItem_UpdatedByID",
            "SaleReturnItem",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_CustomAgencyServiceID",
            "ServiceDetailItem",
            "CustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_CustomServiceID",
            "ServiceDetailItem",
            "CustomServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_MergedServiceID",
            "ServiceDetailItem",
            "MergedServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_PlaneDeliveryServiceID",
            "ServiceDetailItem",
            "PlaneDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_PortCustomAgencyServiceID",
            "ServiceDetailItem",
            "PortCustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_PortWorkServiceID",
            "ServiceDetailItem",
            "PortWorkServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_ServiceDetailItemKeyID",
            "ServiceDetailItem",
            "ServiceDetailItemKeyID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_TransportationServiceID",
            "ServiceDetailItem",
            "TransportationServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServiceDetailItem_VehicleDeliveryServiceID",
            "ServiceDetailItem",
            "VehicleDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_ServicePayer_ClientID",
            "ServicePayer",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_ShipmentList_ResponsibleID",
            "ShipmentList",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_ShipmentList_TransporterID",
            "ShipmentList",
            "TransporterID");

        migrationBuilder.CreateIndex(
            "IX_ShipmentListItem_SaleID",
            "ShipmentListItem",
            "SaleID");

        migrationBuilder.CreateIndex(
            "IX_ShipmentListItem_ShipmentListID",
            "ShipmentListItem",
            "ShipmentListID");

        migrationBuilder.CreateIndex(
            "IX_StathamCar_StathamID",
            "StathamCar",
            "StathamID");

        migrationBuilder.CreateIndex(
            "IX_StathamPassport_StathamID",
            "StathamPassport",
            "StathamID");

        migrationBuilder.CreateIndex(
            "IX_Storage_OrganizationID",
            "Storage",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationDeliveryProtocol_SupplyInformationDeliveryProtocolKeyID",
            "SupplyInformationDeliveryProtocol",
            "SupplyInformationDeliveryProtocolKeyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationDeliveryProtocol_SupplyInvoiceID",
            "SupplyInformationDeliveryProtocol",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationDeliveryProtocol_SupplyOrderID",
            "SupplyInformationDeliveryProtocol",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationDeliveryProtocol_SupplyProFormID",
            "SupplyInformationDeliveryProtocol",
            "SupplyProFormID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationDeliveryProtocol_UserID",
            "SupplyInformationDeliveryProtocol",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationDeliveryProtocolKeyTranslation_SupplyInformationDeliveryProtocolKeyID",
            "SupplyInformationDeliveryProtocolKeyTranslation",
            "SupplyInformationDeliveryProtocolKeyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationTask_DeletedByID",
            "SupplyInformationTask",
            "DeletedByID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationTask_UpdatedByID",
            "SupplyInformationTask",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInformationTask_UserID",
            "SupplyInformationTask",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoice_DeliveryProductProtocolID",
            "SupplyInvoice",
            "DeliveryProductProtocolID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoice_SupplyOrderID",
            "SupplyInvoice",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceBillOfLadingService_BillOfLadingServiceID",
            "SupplyInvoiceBillOfLadingService",
            "BillOfLadingServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceBillOfLadingService_SupplyInvoiceID",
            "SupplyInvoiceBillOfLadingService",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceDeliveryDocument_SupplyDeliveryDocumentID",
            "SupplyInvoiceDeliveryDocument",
            "SupplyDeliveryDocumentID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceDeliveryDocument_SupplyInvoiceID",
            "SupplyInvoiceDeliveryDocument",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceMergedService_MergedServiceID",
            "SupplyInvoiceMergedService",
            "MergedServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceMergedService_SupplyInvoiceID",
            "SupplyInvoiceMergedService",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceOrderItem_SupplyInvoiceID",
            "SupplyInvoiceOrderItem",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceOrderItem_SupplyOrderItemID",
            "SupplyInvoiceOrderItem",
            "SupplyOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_AdditionalPaymentCurrencyID",
            "SupplyOrder",
            "AdditionalPaymentCurrencyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_ClientAgreementID",
            "SupplyOrder",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_ClientID",
            "SupplyOrder",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_CustomAgencyServiceID",
            "SupplyOrder",
            "CustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_OrganizationID",
            "SupplyOrder",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_PlaneDeliveryServiceID",
            "SupplyOrder",
            "PlaneDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_PortCustomAgencyServiceID",
            "SupplyOrder",
            "PortCustomAgencyServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_PortWorkServiceID",
            "SupplyOrder",
            "PortWorkServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_SupplyOrderNumberID",
            "SupplyOrder",
            "SupplyOrderNumberID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_SupplyProFormID",
            "SupplyOrder",
            "SupplyProFormID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_TransportationServiceID",
            "SupplyOrder",
            "TransportationServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_VehicleDeliveryServiceID",
            "SupplyOrder",
            "VehicleDeliveryServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderContainerService_ContainerServiceID",
            "SupplyOrderContainerService",
            "ContainerServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderContainerService_SupplyOrderID",
            "SupplyOrderContainerService",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderDeliveryDocument_SupplyDeliveryDocumentID",
            "SupplyOrderDeliveryDocument",
            "SupplyDeliveryDocumentID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderDeliveryDocument_SupplyOrderID",
            "SupplyOrderDeliveryDocument",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderDeliveryDocument_UserID",
            "SupplyOrderDeliveryDocument",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderItem_ProductID",
            "SupplyOrderItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderItem_SupplyOrderID",
            "SupplyOrderItem",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPaymentDeliveryProtocol_SupplyInvoiceID",
            "SupplyOrderPaymentDeliveryProtocol",
            "SupplyInvoiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPaymentDeliveryProtocol_SupplyOrderPaymentDeliveryProtocolKeyID",
            "SupplyOrderPaymentDeliveryProtocol",
            "SupplyOrderPaymentDeliveryProtocolKeyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPaymentDeliveryProtocol_SupplyPaymentTaskID",
            "SupplyOrderPaymentDeliveryProtocol",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPaymentDeliveryProtocol_SupplyProFormID",
            "SupplyOrderPaymentDeliveryProtocol",
            "SupplyProFormID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPaymentDeliveryProtocol_UserID",
            "SupplyOrderPaymentDeliveryProtocol",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPolandPaymentDeliveryProtocol_SupplyOrderID",
            "SupplyOrderPolandPaymentDeliveryProtocol",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPolandPaymentDeliveryProtocol_SupplyOrderPaymentDeliveryProtocolKeyID",
            "SupplyOrderPolandPaymentDeliveryProtocol",
            "SupplyOrderPaymentDeliveryProtocolKeyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPolandPaymentDeliveryProtocol_SupplyPaymentTaskID",
            "SupplyOrderPolandPaymentDeliveryProtocol",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderPolandPaymentDeliveryProtocol_UserID",
            "SupplyOrderPolandPaymentDeliveryProtocol",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraine_AdditionalPaymentCurrencyID",
            "SupplyOrderUkraine",
            "AdditionalPaymentCurrencyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraine_ClientAgreementID",
            "SupplyOrderUkraine",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraine_OrganizationID",
            "SupplyOrderUkraine",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraine_ResponsibleID",
            "SupplyOrderUkraine",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraine_SupplierID",
            "SupplyOrderUkraine",
            "SupplierID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItem_CreatedByID",
            "SupplyOrderUkraineCartItem",
            "CreatedByID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItem_PackingListPackageOrderItemID",
            "SupplyOrderUkraineCartItem",
            "PackingListPackageOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItem_ProductID",
            "SupplyOrderUkraineCartItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItem_ResponsibleID",
            "SupplyOrderUkraineCartItem",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItem_SupplierID",
            "SupplyOrderUkraineCartItem",
            "SupplierID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItem_TaxFreePackListID",
            "SupplyOrderUkraineCartItem",
            "TaxFreePackListID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItem_UpdatedByID",
            "SupplyOrderUkraineCartItem",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItemReservation_ConsignmentItemID",
            "SupplyOrderUkraineCartItemReservation",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItemReservation_ProductAvailabilityID",
            "SupplyOrderUkraineCartItemReservation",
            "ProductAvailabilityID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItemReservation_SupplyOrderUkraineCartItemID",
            "SupplyOrderUkraineCartItemReservation",
            "SupplyOrderUkraineCartItemID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItemReservationProductPlacement_ProductPlacementID",
            "SupplyOrderUkraineCartItemReservationProductPlacement",
            "ProductPlacementID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineCartItemReservationProductPlacement_SupplyOrderUkraineCartItemReservationID",
            "SupplyOrderUkraineCartItemReservationProductPlacement",
            "SupplyOrderUkraineCartItemReservationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineDocument_SupplyOrderUkraineID",
            "SupplyOrderUkraineDocument",
            "SupplyOrderUkraineID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineItem_ConsignmentItemID",
            "SupplyOrderUkraineItem",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineItem_PackingListPackageOrderItemID",
            "SupplyOrderUkraineItem",
            "PackingListPackageOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineItem_ProductID",
            "SupplyOrderUkraineItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineItem_ProductSpecificationID",
            "SupplyOrderUkraineItem",
            "ProductSpecificationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineItem_SupplierID",
            "SupplyOrderUkraineItem",
            "SupplierID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkraineItem_SupplyOrderUkraineID",
            "SupplyOrderUkraineItem",
            "SupplyOrderUkraineID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkrainePaymentDeliveryProtocol_SupplyOrderUkraineID",
            "SupplyOrderUkrainePaymentDeliveryProtocol",
            "SupplyOrderUkraineID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkrainePaymentDeliveryProtocol_SupplyOrderUkrainePaymentDeliveryProtocolKeyID",
            "SupplyOrderUkrainePaymentDeliveryProtocol",
            "SupplyOrderUkrainePaymentDeliveryProtocolKeyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkrainePaymentDeliveryProtocol_SupplyPaymentTaskID",
            "SupplyOrderUkrainePaymentDeliveryProtocol",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderUkrainePaymentDeliveryProtocol_UserID",
            "SupplyOrderUkrainePaymentDeliveryProtocol",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderVehicleService_SupplyOrderID",
            "SupplyOrderVehicleService",
            "SupplyOrderID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrderVehicleService_VehicleServiceID",
            "SupplyOrderVehicleService",
            "VehicleServiceID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganization_OrganizationID",
            "SupplyOrganization",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganizationAgreement_AgreementTypeCivilCodeID",
            "SupplyOrganizationAgreement",
            "AgreementTypeCivilCodeID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganizationAgreement_CurrencyID",
            "SupplyOrganizationAgreement",
            "CurrencyID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganizationAgreement_SupplyOrganizationID",
            "SupplyOrganizationAgreement",
            "SupplyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganizationAgreement_TaxAccountingSchemeID",
            "SupplyOrganizationAgreement",
            "TaxAccountingSchemeID");

        migrationBuilder.CreateIndex(
            "IX_SupplyOrganizationDocument_SupplyOrganizationID",
            "SupplyOrganizationDocument",
            "SupplyOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyPaymentTask_DeletedByID",
            "SupplyPaymentTask",
            "DeletedByID");

        migrationBuilder.CreateIndex(
            "IX_SupplyPaymentTask_UpdatedByID",
            "SupplyPaymentTask",
            "UpdatedByID");

        migrationBuilder.CreateIndex(
            "IX_SupplyPaymentTask_UserID",
            "SupplyPaymentTask",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_SupplyPaymentTaskDocument_SupplyPaymentTaskID",
            "SupplyPaymentTaskDocument",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturn_ClientAgreementID",
            "SupplyReturn",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturn_OrganizationID",
            "SupplyReturn",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturn_ResponsibleID",
            "SupplyReturn",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturn_StorageID",
            "SupplyReturn",
            "StorageID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturn_SupplierID",
            "SupplyReturn",
            "SupplierID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturnItem_ConsignmentItemID",
            "SupplyReturnItem",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturnItem_ProductID",
            "SupplyReturnItem",
            "ProductID");

        migrationBuilder.CreateIndex(
            "IX_SupplyReturnItem_SupplyReturnID",
            "SupplyReturnItem",
            "SupplyReturnID");

        migrationBuilder.CreateIndex(
            "IX_TaxFree_ResponsibleID",
            "TaxFree",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_TaxFree_StathamCarID",
            "TaxFree",
            "StathamCarID");

        migrationBuilder.CreateIndex(
            "IX_TaxFree_StathamID",
            "TaxFree",
            "StathamID");

        migrationBuilder.CreateIndex(
            "IX_TaxFree_StathamPassportID",
            "TaxFree",
            "StathamPassportID");

        migrationBuilder.CreateIndex(
            "IX_TaxFree_TaxFreePackListID",
            "TaxFree",
            "TaxFreePackListID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreeDocument_TaxFreeID",
            "TaxFreeDocument",
            "TaxFreeID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreeItem_SupplyOrderUkraineCartItemID",
            "TaxFreeItem",
            "SupplyOrderUkraineCartItemID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreeItem_TaxFreeID",
            "TaxFreeItem",
            "TaxFreeID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreeItem_TaxFreePackListOrderItemID",
            "TaxFreeItem",
            "TaxFreePackListOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackList_ClientAgreementID",
            "TaxFreePackList",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackList_ClientID",
            "TaxFreePackList",
            "ClientID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackList_OrganizationID",
            "TaxFreePackList",
            "OrganizationID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackList_ResponsibleID",
            "TaxFreePackList",
            "ResponsibleID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackList_SupplyOrderUkraineID",
            "TaxFreePackList",
            "SupplyOrderUkraineID",
            unique: true,
            filter: "[SupplyOrderUkraineID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackListOrderItem_ConsignmentItemID",
            "TaxFreePackListOrderItem",
            "ConsignmentItemID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackListOrderItem_OrderItemID",
            "TaxFreePackListOrderItem",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_TaxFreePackListOrderItem_TaxFreePackListID",
            "TaxFreePackListOrderItem",
            "TaxFreePackListID");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_AccountingActProvidingServiceId",
            "TransportationService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_AccountingPaymentTaskID",
            "TransportationService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_ActProvidingServiceDocumentID",
            "TransportationService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_ActProvidingServiceId",
            "TransportationService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_SupplyInformationTaskID",
            "TransportationService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_SupplyOrganizationAgreementID",
            "TransportationService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_SupplyPaymentTaskID",
            "TransportationService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_SupplyServiceAccountDocumentID",
            "TransportationService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_TransportationOrganizationID",
            "TransportationService",
            "TransportationOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_TransportationService_UserID",
            "TransportationService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_Transporter_TransporterTypeID",
            "Transporter",
            "TransporterTypeID");

        migrationBuilder.CreateIndex(
            "IX_TransporterTypeTranslation_TransporterTypeID",
            "TransporterTypeTranslation",
            "TransporterTypeID");

        migrationBuilder.CreateIndex(
            "IX_User_UserDetailsId",
            "User",
            "UserDetailsId");

        migrationBuilder.CreateIndex(
            "IX_User_UserRoleID",
            "User",
            "UserRoleID");

        migrationBuilder.CreateIndex(
            "IX_UserDetails_ResidenceCardID",
            "UserDetails",
            "ResidenceCardID");

        migrationBuilder.CreateIndex(
            "IX_UserDetails_WorkPermitID",
            "UserDetails",
            "WorkPermitID");

        migrationBuilder.CreateIndex(
            "IX_UserDetails_WorkingContractID",
            "UserDetails",
            "WorkingContractID");

        migrationBuilder.CreateIndex(
            "IX_UserRoleTranslation_UserRoleID",
            "UserRoleTranslation",
            "UserRoleID");

        migrationBuilder.CreateIndex(
            "IX_UserScreenResolution_UserID",
            "UserScreenResolution",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_AccountingActProvidingServiceId",
            "VehicleDeliveryService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_AccountingPaymentTaskID",
            "VehicleDeliveryService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_ActProvidingServiceDocumentID",
            "VehicleDeliveryService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_ActProvidingServiceId",
            "VehicleDeliveryService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_SupplyInformationTaskID",
            "VehicleDeliveryService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_SupplyOrganizationAgreementID",
            "VehicleDeliveryService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_SupplyPaymentTaskID",
            "VehicleDeliveryService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_SupplyServiceAccountDocumentID",
            "VehicleDeliveryService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_UserID",
            "VehicleDeliveryService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_VehicleDeliveryService_VehicleDeliveryOrganizationID",
            "VehicleDeliveryService",
            "VehicleDeliveryOrganizationID");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_AccountingActProvidingServiceId",
            "VehicleService",
            "AccountingActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_AccountingPaymentTaskID",
            "VehicleService",
            "AccountingPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_ActProvidingServiceDocumentID",
            "VehicleService",
            "ActProvidingServiceDocumentID",
            unique: true,
            filter: "[ActProvidingServiceDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_ActProvidingServiceId",
            "VehicleService",
            "ActProvidingServiceId");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_BillOfLadingDocumentID",
            "VehicleService",
            "BillOfLadingDocumentID");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_SupplyInformationTaskID",
            "VehicleService",
            "SupplyInformationTaskID",
            unique: true,
            filter: "[SupplyInformationTaskID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_SupplyOrganizationAgreementID",
            "VehicleService",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_SupplyPaymentTaskID",
            "VehicleService",
            "SupplyPaymentTaskID");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_SupplyServiceAccountDocumentID",
            "VehicleService",
            "SupplyServiceAccountDocumentID",
            unique: true,
            filter: "[SupplyServiceAccountDocumentID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_UserID",
            "VehicleService",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_VehicleService_VehicleOrganizationID",
            "VehicleService",
            "VehicleOrganizationID");

        migrationBuilder.AddForeignKey(
            "FK_BillOfLadingService_DeliveryProductProtocol_DeliveryProductProtocolID",
            "BillOfLadingService",
            "DeliveryProductProtocolID",
            "DeliveryProductProtocol",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_BillOfLadingService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "BillOfLadingService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_BillOfLadingService_SupplyOrganization_SupplyOrganizationID",
            "BillOfLadingService",
            "SupplyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ContainerService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "ContainerService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ContainerService_SupplyOrganization_ContainerOrganizationID",
            "ContainerService",
            "ContainerOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CustomAgencyService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "CustomAgencyService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CustomAgencyService_SupplyOrganization_CustomAgencyOrganizationID",
            "CustomAgencyService",
            "CustomAgencyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CustomService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "CustomService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CustomService_SupplyOrganization_CustomOrganizationID",
            "CustomService",
            "CustomOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CustomService_SupplyOrganization_ExciseDutyOrganizationID",
            "CustomService",
            "ExciseDutyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CustomService_SupplyOrder_SupplyOrderID",
            "CustomService",
            "SupplyOrderID",
            "SupplyOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_MergedService_SupplyOrderUkraine_SupplyOrderUkraineID",
            "MergedService",
            "SupplyOrderUkraineID",
            "SupplyOrderUkraine",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_MergedService_DeliveryProductProtocol_DeliveryProductProtocolID",
            "MergedService",
            "DeliveryProductProtocolID",
            "DeliveryProductProtocol",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_MergedService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "MergedService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_MergedService_SupplyOrganization_SupplyOrganizationID",
            "MergedService",
            "SupplyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_MergedService_SupplyOrder_SupplyOrderID",
            "MergedService",
            "SupplyOrderID",
            "SupplyOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PlaneDeliveryService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "PlaneDeliveryService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PlaneDeliveryService_SupplyOrganization_PlaneDeliveryOrganizationID",
            "PlaneDeliveryService",
            "PlaneDeliveryOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PortCustomAgencyService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "PortCustomAgencyService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PortCustomAgencyService_SupplyOrganization_PortCustomAgencyOrganizationID",
            "PortCustomAgencyService",
            "PortCustomAgencyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PortWorkService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "PortWorkService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PortWorkService_SupplyOrganization_PortWorkOrganizationID",
            "PortWorkService",
            "PortWorkOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_TransportationService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "TransportationService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_TransportationService_SupplyOrganization_TransportationOrganizationID",
            "TransportationService",
            "TransportationOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_VehicleDeliveryService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "VehicleDeliveryService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_VehicleDeliveryService_SupplyOrganization_VehicleDeliveryOrganizationID",
            "VehicleDeliveryService",
            "VehicleDeliveryOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_VehicleService_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "VehicleService",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_VehicleService_SupplyOrganization_VehicleOrganizationID",
            "VehicleService",
            "VehicleOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ActReconciliationItem_ActReconciliation_ActReconciliationID",
            "ActReconciliationItem",
            "ActReconciliationID",
            "ActReconciliation",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ActReconciliationItem_SupplyInvoiceOrderItem_SupplyInvoiceOrderItemID",
            "ActReconciliationItem",
            "SupplyInvoiceOrderItemID",
            "SupplyInvoiceOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ActReconciliationItem_SupplyOrderUkraineItem_SupplyOrderUkraineItemID",
            "ActReconciliationItem",
            "SupplyOrderUkraineItemID",
            "SupplyOrderUkraineItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_DepreciatedOrderItem_DepreciatedOrder_DepreciatedOrderID",
            "DepreciatedOrderItem",
            "DepreciatedOrderID",
            "DepreciatedOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ProductIncomeItem_SupplyOrderUkraineItem_SupplyOrderUkraineItemID",
            "ProductIncomeItem",
            "SupplyOrderUkraineItemID",
            "SupplyOrderUkraineItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ProductIncomeItem_ProductIncome_ProductIncomeID",
            "ProductIncomeItem",
            "ProductIncomeID",
            "ProductIncome",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ProductIncomeItem_PackingListPackageOrderItem_PackingListPackageOrderItemID",
            "ProductIncomeItem",
            "PackingListPackageOrderItemID",
            "PackingListPackageOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ProductIncomeItem_ProductCapitalizationItem_ProductCapitalizationItemID",
            "ProductIncomeItem",
            "ProductCapitalizationItemID",
            "ProductCapitalizationItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ProductIncomeItem_SaleReturnItem_SaleReturnItemID",
            "ProductIncomeItem",
            "SaleReturnItemID",
            "SaleReturnItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ProductTransferItem_ProductTransfer_ProductTransferID",
            "ProductTransferItem",
            "ProductTransferID",
            "ProductTransfer",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ClientAgreement_Agreement_AgreementID",
            "ClientAgreement",
            "AgreementID",
            "Agreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ClientInDebt_Agreement_AgreementID",
            "ClientInDebt",
            "AgreementID",
            "Agreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ClientInDebt_ReSale_ReSaleID",
            "ClientInDebt",
            "ReSaleID",
            "ReSale",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ClientInDebt_Sale_SaleID",
            "ClientInDebt",
            "SaleID",
            "Sale",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_Agreement_Organization_OrganizationID",
            "Agreement",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrganizationAgreement_SupplyOrganization_SupplyOrganizationID",
            "SupplyOrganizationAgreement",
            "SupplyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ReSale_Organization_OrganizationID",
            "ReSale",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ReSale_Storage_FromStorageID",
            "ReSale",
            "FromStorageID",
            "Storage",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ReSale_SaleNumber_SaleNumberID",
            "ReSale",
            "SaleNumberID",
            "SaleNumber",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_Sale_Sad_SadID",
            "Sale",
            "SadID",
            "Sad",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_Sale_SaleNumber_SaleNumberID",
            "Sale",
            "SaleNumberID",
            "SaleNumber",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_Sale_TaxFreePackList_TaxFreePackListID",
            "Sale",
            "TaxFreePackListID",
            "TaxFreePackList",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PackingListPackageOrderItemSupplyService_PackingListPackageOrderItem_PackingListPackageOrderItemID",
            "PackingListPackageOrderItemSupplyService",
            "PackingListPackageOrderItemID",
            "PackingListPackageOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyInvoiceBillOfLadingService_SupplyInvoice_SupplyInvoiceID",
            "SupplyInvoiceBillOfLadingService",
            "SupplyInvoiceID",
            "SupplyInvoice",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_IncomePaymentOrder_Organization_OrganizationID",
            "IncomePaymentOrder",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_IncomePaymentOrder_Sad_SadID",
            "IncomePaymentOrder",
            "SadID",
            "Sad",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_IncomePaymentOrder_TaxFree_TaxFreeID",
            "IncomePaymentOrder",
            "TaxFreeID",
            "TaxFree",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_IncomePaymentOrder_PaymentRegister_PaymentRegisterID",
            "IncomePaymentOrder",
            "PaymentRegisterID",
            "PaymentRegister",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_Sad_SupplyOrderUkraine_SupplyOrderUkraineID",
            "Sad",
            "SupplyOrderUkraineID",
            "SupplyOrderUkraine",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_Sad_Organization_OrganizationID",
            "Sad",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SadItem_ConsignmentItem_ConsignmentItemID",
            "SadItem",
            "ConsignmentItemID",
            "ConsignmentItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SadItem_SupplyOrderUkraineCartItem_SupplyOrderUkraineCartItemID",
            "SadItem",
            "SupplyOrderUkraineCartItemID",
            "SupplyOrderUkraineCartItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SaleFutureReservation_SupplyOrder_SupplyOrderID",
            "SaleFutureReservation",
            "SupplyOrderID",
            "SupplyOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrder_Organization_OrganizationID",
            "SupplyOrder",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrderUkraine_Organization_OrganizationID",
            "SupplyOrderUkraine",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrderUkraineCartItem_PackingListPackageOrderItem_PackingListPackageOrderItemID",
            "SupplyOrderUkraineCartItem",
            "PackingListPackageOrderItemID",
            "PackingListPackageOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrderUkraineCartItem_TaxFreePackList_TaxFreePackListID",
            "SupplyOrderUkraineCartItem",
            "TaxFreePackListID",
            "TaxFreePackList",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrderUkraineItem_ConsignmentItem_ConsignmentItemID",
            "SupplyOrderUkraineItem",
            "ConsignmentItemID",
            "ConsignmentItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrderUkraineItem_PackingListPackageOrderItem_PackingListPackageOrderItemID",
            "SupplyOrderUkraineItem",
            "PackingListPackageOrderItemID",
            "PackingListPackageOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyReturn_Organization_OrganizationID",
            "SupplyReturn",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyReturn_Storage_StorageID",
            "SupplyReturn",
            "StorageID",
            "Storage",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_TaxFreePackList_Organization_OrganizationID",
            "TaxFreePackList",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_AdvancePayment_Organization_OrganizationID",
            "AdvancePayment",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_OutcomePaymentOrder_Organization_OrganizationID",
            "OutcomePaymentOrder",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_OutcomePaymentOrder_SupplyOrganization_ConsumableProductOrganizationID",
            "OutcomePaymentOrder",
            "ConsumableProductOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_OutcomePaymentOrder_PaymentCurrencyRegister_PaymentCurrencyRegisterID",
            "OutcomePaymentOrder",
            "PaymentCurrencyRegisterID",
            "PaymentCurrencyRegister",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CompanyCarFueling_SupplyOrganization_ConsumableProductOrganizationID",
            "CompanyCarFueling",
            "ConsumableProductOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CompanyCarFueling_CompanyCar_CompanyCarID",
            "CompanyCarFueling",
            "CompanyCarID",
            "CompanyCar",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CompanyCarRoadList_CompanyCar_CompanyCarID",
            "CompanyCarRoadList",
            "CompanyCarID",
            "CompanyCar",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PaymentCostMovementOperation_ConsumablesOrderItem_ConsumablesOrderItemID",
            "PaymentCostMovementOperation",
            "ConsumablesOrderItemID",
            "ConsumablesOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PaymentCostMovementOperation_DepreciatedConsumableOrderItem_DepreciatedConsumableOrderItemID",
            "PaymentCostMovementOperation",
            "DepreciatedConsumableOrderItemID",
            "DepreciatedConsumableOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ConsignmentItem_Consignment_ConsignmentID",
            "ConsignmentItem",
            "ConsignmentID",
            "Consignment",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ConsignmentItemMovement_ReSaleItem_ReSaleItemId",
            "ConsignmentItemMovement",
            "ReSaleItemId",
            "ReSaleItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ProductPlacement_Storage_StorageID",
            "ProductPlacement",
            "StorageID",
            "Storage",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ProductPlacement_PackingListPackageOrderItem_PackingListPackageOrderItemID",
            "ProductPlacement",
            "PackingListPackageOrderItemID",
            "PackingListPackageOrderItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ProductReservation_ProductAvailability_ProductAvailabilityID",
            "ProductReservation",
            "ProductAvailabilityID",
            "ProductAvailability",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_ReSaleAvailability_ProductAvailability_ProductAvailabilityID",
            "ReSaleAvailability",
            "ProductAvailabilityID",
            "ProductAvailability",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrderUkraineCartItemReservation_ProductAvailability_ProductAvailabilityID",
            "SupplyOrderUkraineCartItemReservation",
            "ProductAvailabilityID",
            "ProductAvailability",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ConsumablesOrderItem_SupplyOrganization_ConsumableProductOrganizationID",
            "ConsumablesOrderItem",
            "ConsumableProductOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ConsumablesOrderItem_ConsumablesOrder_ConsumablesOrderID",
            "ConsumablesOrderItem",
            "ConsumablesOrderID",
            "ConsumablesOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ConsumablesOrderDocument_ConsumablesOrder_ConsumablesOrderID",
            "ConsumablesOrderDocument",
            "ConsumablesOrderID",
            "ConsumablesOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            "FK_OutcomePaymentOrderConsumablesOrder_ConsumablesOrder_ConsumablesOrderID",
            "OutcomePaymentOrderConsumablesOrder",
            "ConsumablesOrderID",
            "ConsumablesOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_DepreciatedConsumableOrderItem_DepreciatedConsumableOrder_DepreciatedConsumableOrderID",
            "DepreciatedConsumableOrderItem",
            "DepreciatedConsumableOrderID",
            "DepreciatedConsumableOrder",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CompanyCar_Organization_OrganizationID",
            "CompanyCar",
            "OrganizationID",
            "Organization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_CompanyCar_ConsumablesStorage_ConsumablesStorageID",
            "CompanyCar",
            "ConsumablesStorageID",
            "ConsumablesStorage",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_ConsumablesOrder_ConsumablesStorage_ConsumablesStorageID",
            "ConsumablesOrder",
            "ConsumablesStorageID",
            "ConsumablesStorage",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_DepreciatedConsumableOrder_ConsumablesStorage_ConsumablesStorageID",
            "DepreciatedConsumableOrder",
            "ConsumablesStorageID",
            "ConsumablesStorage",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_InvoiceDocument_SupplyInvoice_SupplyInvoiceID",
            "InvoiceDocument",
            "SupplyInvoiceID",
            "SupplyInvoice",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_InvoiceDocument_PackingList_PackingListID",
            "InvoiceDocument",
            "PackingListID",
            "PackingList",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_PackingList_SupplyInvoice_SupplyInvoiceID",
            "PackingList",
            "SupplyInvoiceID",
            "SupplyInvoice",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_Organization_Storage_StorageID",
            "Organization",
            "StorageID",
            "Storage",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_ActProvidingService_User_UserID",
            "ActProvidingService");

        migrationBuilder.DropForeignKey(
            "FK_ActReconciliation_User_ResponsibleID",
            "ActReconciliation");

        migrationBuilder.DropForeignKey(
            "FK_BillOfLadingService_User_UserID",
            "BillOfLadingService");

        migrationBuilder.DropForeignKey(
            "FK_ClientShoppingCart_User_CreatedByID",
            "ClientShoppingCart");

        migrationBuilder.DropForeignKey(
            "FK_ClientShoppingCart_User_OfferProcessingStatusChangedByID",
            "ClientShoppingCart");

        migrationBuilder.DropForeignKey(
            "FK_ContainerService_User_UserID",
            "ContainerService");

        migrationBuilder.DropForeignKey(
            "FK_CustomAgencyService_User_UserID",
            "CustomAgencyService");

        migrationBuilder.DropForeignKey(
            "FK_DeliveryProductProtocol_User_UserID",
            "DeliveryProductProtocol");

        migrationBuilder.DropForeignKey(
            "FK_MisplacedSale_User_UserID",
            "MisplacedSale");

        migrationBuilder.DropForeignKey(
            "FK_Order_User_UserID",
            "Order");

        migrationBuilder.DropForeignKey(
            "FK_OrderItem_User_DiscountUpdatedByID",
            "OrderItem");

        migrationBuilder.DropForeignKey(
            "FK_OrderItem_User_OfferProcessingStatusChangedByID",
            "OrderItem");

        migrationBuilder.DropForeignKey(
            "FK_OrderItem_User_UserId",
            "OrderItem");

        migrationBuilder.DropForeignKey(
            "FK_PlaneDeliveryService_User_UserID",
            "PlaneDeliveryService");

        migrationBuilder.DropForeignKey(
            "FK_PortCustomAgencyService_User_UserID",
            "PortCustomAgencyService");

        migrationBuilder.DropForeignKey(
            "FK_PortWorkService_User_UserID",
            "PortWorkService");

        migrationBuilder.DropForeignKey(
            "FK_ProductCapitalization_User_ResponsibleID",
            "ProductCapitalization");

        migrationBuilder.DropForeignKey(
            "FK_ProductIncome_User_UserID",
            "ProductIncome");

        migrationBuilder.DropForeignKey(
            "FK_ProductSpecification_User_AddedByID",
            "ProductSpecification");

        migrationBuilder.DropForeignKey(
            "FK_ProductTransfer_User_ResponsibleID",
            "ProductTransfer");

        migrationBuilder.DropForeignKey(
            "FK_SaleReturn_User_CanceledByID",
            "SaleReturn");

        migrationBuilder.DropForeignKey(
            "FK_SaleReturn_User_CreatedByID",
            "SaleReturn");

        migrationBuilder.DropForeignKey(
            "FK_SaleReturn_User_UpdatedByID",
            "SaleReturn");

        migrationBuilder.DropForeignKey(
            "FK_SaleReturnItem_User_CreatedByID",
            "SaleReturnItem");

        migrationBuilder.DropForeignKey(
            "FK_SaleReturnItem_User_MoneyReturnedByID",
            "SaleReturnItem");

        migrationBuilder.DropForeignKey(
            "FK_SaleReturnItem_User_UpdatedByID",
            "SaleReturnItem");

        migrationBuilder.DropForeignKey(
            "FK_SupplyInformationTask_User_DeletedByID",
            "SupplyInformationTask");

        migrationBuilder.DropForeignKey(
            "FK_SupplyInformationTask_User_UpdatedByID",
            "SupplyInformationTask");

        migrationBuilder.DropForeignKey(
            "FK_SupplyInformationTask_User_UserID",
            "SupplyInformationTask");

        migrationBuilder.DropForeignKey(
            "FK_SupplyOrderUkraine_User_ResponsibleID",
            "SupplyOrderUkraine");

        migrationBuilder.DropForeignKey(
            "FK_SupplyPaymentTask_User_DeletedByID",
            "SupplyPaymentTask");

        migrationBuilder.DropForeignKey(
            "FK_SupplyPaymentTask_User_UpdatedByID",
            "SupplyPaymentTask");

        migrationBuilder.DropForeignKey(
            "FK_SupplyPaymentTask_User_UserID",
            "SupplyPaymentTask");

        migrationBuilder.DropForeignKey(
            "FK_TransportationService_User_UserID",
            "TransportationService");

        migrationBuilder.DropForeignKey(
            "FK_VehicleDeliveryService_User_UserID",
            "VehicleDeliveryService");

        migrationBuilder.DropForeignKey(
            "FK_VehicleService_User_UserID",
            "VehicleService");

        migrationBuilder.DropForeignKey(
            "FK_ActReconciliation_SupplyInvoice_SupplyInvoiceID",
            "ActReconciliation");

        migrationBuilder.DropForeignKey(
            "FK_PackingList_SupplyInvoice_SupplyInvoiceID",
            "PackingList");

        migrationBuilder.DropForeignKey(
            "FK_SupplyInvoiceOrderItem_SupplyInvoice_SupplyInvoiceID",
            "SupplyInvoiceOrderItem");

        migrationBuilder.DropForeignKey(
            "FK_ActReconciliation_SupplyOrderUkraine_SupplyOrderUkraineID",
            "ActReconciliation");

        migrationBuilder.DropForeignKey(
            "FK_SupplyOrderUkraineItem_SupplyOrderUkraine_SupplyOrderUkraineID",
            "SupplyOrderUkraineItem");

        migrationBuilder.DropForeignKey(
            "FK_ActReconciliationItem_ActReconciliation_ActReconciliationID",
            "ActReconciliationItem");

        migrationBuilder.DropForeignKey(
            "FK_ActReconciliationItem_Product_ProductID",
            "ActReconciliationItem");

        migrationBuilder.DropForeignKey(
            "FK_ConsignmentItem_Product_ProductID",
            "ConsignmentItem");

        migrationBuilder.DropForeignKey(
            "FK_OrderItem_Product_ProductID",
            "OrderItem");

        migrationBuilder.DropForeignKey(
            "FK_ProductCapitalizationItem_Product_ProductID",
            "ProductCapitalizationItem");

        migrationBuilder.DropForeignKey(
            "FK_ProductSpecification_Product_ProductID",
            "ProductSpecification");

        migrationBuilder.DropForeignKey(
            "FK_SupplyOrderItem_Product_ProductID",
            "SupplyOrderItem");

        migrationBuilder.DropForeignKey(
            "FK_SupplyOrderUkraineItem_Product_ProductID",
            "SupplyOrderUkraineItem");

        migrationBuilder.DropForeignKey(
            "FK_ActReconciliationItem_SupplyInvoiceOrderItem_SupplyInvoiceOrderItemID",
            "ActReconciliationItem");

        migrationBuilder.DropForeignKey(
            "FK_PackingListPackageOrderItem_SupplyInvoiceOrderItem_SupplyInvoiceOrderItemID",
            "PackingListPackageOrderItem");

        migrationBuilder.DropForeignKey(
            "FK_ActReconciliationItem_SupplyOrderUkraineItem_SupplyOrderUkraineItemID",
            "ActReconciliationItem");

        migrationBuilder.DropForeignKey(
            "FK_ProductIncomeItem_SupplyOrderUkraineItem_SupplyOrderUkraineItemID",
            "ProductIncomeItem");

        migrationBuilder.DropForeignKey(
            "FK_Storage_Organization_OrganizationID",
            "Storage");

        migrationBuilder.DropTable(
            "AccountingDocumentName");

        migrationBuilder.DropTable(
            "AdvancePayment");

        migrationBuilder.DropTable(
            "AgreementTypeTranslation");

        migrationBuilder.DropTable(
            "AllegroCategory");

        migrationBuilder.DropTable(
            "AllegroProductReservation");

        migrationBuilder.DropTable(
            "AssignedPaymentOrder");

        migrationBuilder.DropTable(
            "AuditEntityProperty");

        migrationBuilder.DropTable(
            "AuditEntityPropertyNameTranslation");

        migrationBuilder.DropTable(
            "Bank");

        migrationBuilder.DropTable(
            "CalculationTypeTranslation");

        migrationBuilder.DropTable(
            "ChartMonthTranslation");

        migrationBuilder.DropTable(
            "ClientBalanceMovement");

        migrationBuilder.DropTable(
            "ClientContractDocument");

        migrationBuilder.DropTable(
            "ClientInDebt");

        migrationBuilder.DropTable(
            "ClientInRole");

        migrationBuilder.DropTable(
            "ClientPerfectClient");

        migrationBuilder.DropTable(
            "ClientRegistrationTask");

        migrationBuilder.DropTable(
            "ClientSubClient");

        migrationBuilder.DropTable(
            "ClientTypeRoleTranslation");

        migrationBuilder.DropTable(
            "ClientTypeTranslation");

        migrationBuilder.DropTable(
            "ClientUserProfile");

        migrationBuilder.DropTable(
            "ColumnItemTranslation");

        migrationBuilder.DropTable(
            "CompanyCarRoadListDriver");

        migrationBuilder.DropTable(
            "ConsignmentItemMovement");

        migrationBuilder.DropTable(
            "ConsignmentItemMovementTypeName");

        migrationBuilder.DropTable(
            "ConsignmentNoteSetting");

        migrationBuilder.DropTable(
            "ConsumableProductCategoryTranslation");

        migrationBuilder.DropTable(
            "ConsumableProductTranslation");

        migrationBuilder.DropTable(
            "ConsumablesOrderDocument");

        migrationBuilder.DropTable(
            "CountSaleMessage");

        migrationBuilder.DropTable(
            "CreditNoteDocument");

        migrationBuilder.DropTable(
            "CrossExchangeRateHistory");

        migrationBuilder.DropTable(
            "CurrencyTraderExchangeRate");

        migrationBuilder.DropTable(
            "CurrencyTranslation");

        migrationBuilder.DropTable(
            "DashboardNode");

        migrationBuilder.DropTable(
            "DataSyncOperation");

        migrationBuilder.DropTable(
            "DeliveryProductProtocolDocument");

        migrationBuilder.DropTable(
            "DocumentMonth");

        migrationBuilder.DropTable(
            "DynamicProductPlacement");

        migrationBuilder.DropTable(
            "EcommerceContactInfo");

        migrationBuilder.DropTable(
            "EcommerceContacts");

        migrationBuilder.DropTable(
            "EcommerceDefaultPricing");

        migrationBuilder.DropTable(
            "EcommercePage");

        migrationBuilder.DropTable(
            "ExchangeRateHistory");

        migrationBuilder.DropTable(
            "ExpiredBillUserNotification");

        migrationBuilder.DropTable(
            "FilterItemTranslation");

        migrationBuilder.DropTable(
            "FilterOperationItemTranslation");

        migrationBuilder.DropTable(
            "GovCrossExchangeRateHistory");

        migrationBuilder.DropTable(
            "GovExchangeRateHistory");

        migrationBuilder.DropTable(
            "IncomePaymentOrderSale");

        migrationBuilder.DropTable(
            "Incoterm");

        migrationBuilder.DropTable(
            "InvoiceDocument");

        migrationBuilder.DropTable(
            "MeasureUnitTranslation");

        migrationBuilder.DropTable(
            "OrderItemMerged");

        migrationBuilder.DropTable(
            "OrderItemMovement");

        migrationBuilder.DropTable(
            "OrderPackageItem");

        migrationBuilder.DropTable(
            "OrderPackageUser");

        migrationBuilder.DropTable(
            "OrderProductSpecification");

        migrationBuilder.DropTable(
            "OrganizationTranslation");

        migrationBuilder.DropTable(
            "OutcomePaymentOrderConsumablesOrder");

        migrationBuilder.DropTable(
            "OutcomePaymentOrderSupplyPaymentTask");

        migrationBuilder.DropTable(
            "PackingListDocument");

        migrationBuilder.DropTable(
            "PackingListPackageOrderItemSupplyService");

        migrationBuilder.DropTable(
            "PaymentCostMovementOperation");

        migrationBuilder.DropTable(
            "PaymentCostMovementTranslation");

        migrationBuilder.DropTable(
            "PaymentDeliveryDocument");

        migrationBuilder.DropTable(
            "PaymentMovementOperation");

        migrationBuilder.DropTable(
            "PaymentMovementTranslation");

        migrationBuilder.DropTable(
            "PerfectClientTranslation");

        migrationBuilder.DropTable(
            "PerfectClientValueTranslation");

        migrationBuilder.DropTable(
            "PreOrder");

        migrationBuilder.DropTable(
            "PriceTypeTranslation");

        migrationBuilder.DropTable(
            "PricingProductGroupDiscount");

        migrationBuilder.DropTable(
            "PricingTranslation");

        migrationBuilder.DropTable(
            "ProductAnalogue");

        migrationBuilder.DropTable(
            "ProductCarBrand");

        migrationBuilder.DropTable(
            "ProductCategory");

        migrationBuilder.DropTable(
            "ProductGroupDiscount");

        migrationBuilder.DropTable(
            "ProductImage");

        migrationBuilder.DropTable(
            "ProductLocation");

        migrationBuilder.DropTable(
            "ProductOriginalNumber");

        migrationBuilder.DropTable(
            "ProductPlacementMovement");

        migrationBuilder.DropTable(
            "ProductPricing");

        migrationBuilder.DropTable(
            "ProductProductGroup");

        migrationBuilder.DropTable(
            "ProductSet");

        migrationBuilder.DropTable(
            "ProductSlug");

        migrationBuilder.DropTable(
            "ProductSubGroup");

        migrationBuilder.DropTable(
            "ProductWriteOffRule");

        migrationBuilder.DropTable(
            "ProFormDocument");

        migrationBuilder.DropTable(
            "ResponsibilityDeliveryProtocol");

        migrationBuilder.DropTable(
            "RetailClientPaymentImage");

        migrationBuilder.DropTable(
            "RetailPaymentTypeTranslate");

        migrationBuilder.DropTable(
            "SadDocument");

        migrationBuilder.DropTable(
            "SadPalletItem");

        migrationBuilder.DropTable(
            "SaleExchangeRate");

        migrationBuilder.DropTable(
            "SaleFutureReservation");

        migrationBuilder.DropTable(
            "SaleMerged");

        migrationBuilder.DropTable(
            "SaleReturnItemStatusName");

        migrationBuilder.DropTable(
            "SeoPage");

        migrationBuilder.DropTable(
            "ServiceDetailItem");

        migrationBuilder.DropTable(
            "ServicePayer");

        migrationBuilder.DropTable(
            "ShipmentListItem");

        migrationBuilder.DropTable(
            "SupplyInformationDeliveryProtocol");

        migrationBuilder.DropTable(
            "SupplyInformationDeliveryProtocolKeyTranslation");

        migrationBuilder.DropTable(
            "SupplyInvoiceBillOfLadingService");

        migrationBuilder.DropTable(
            "SupplyInvoiceDeliveryDocument");

        migrationBuilder.DropTable(
            "SupplyInvoiceMergedService");

        migrationBuilder.DropTable(
            "SupplyOrderContainerService");

        migrationBuilder.DropTable(
            "SupplyOrderDeliveryDocument");

        migrationBuilder.DropTable(
            "SupplyOrderUkraineCartItemReservationProductPlacement");

        migrationBuilder.DropTable(
            "SupplyOrderUkraineDocument");

        migrationBuilder.DropTable(
            "SupplyOrderUkrainePaymentDeliveryProtocol");

        migrationBuilder.DropTable(
            "SupplyOrderVehicleService");

        migrationBuilder.DropTable(
            "SupplyOrganizationDocument");

        migrationBuilder.DropTable(
            "SupplyPaymentTaskDocument");

        migrationBuilder.DropTable(
            "SupplyServiceNumber");

        migrationBuilder.DropTable(
            "SupportVideo");

        migrationBuilder.DropTable(
            "TaxFreeDocument");

        migrationBuilder.DropTable(
            "TransporterTypeTranslation");

        migrationBuilder.DropTable(
            "UserRoleTranslation");

        migrationBuilder.DropTable(
            "UserScreenResolution");

        migrationBuilder.DropTable(
            "AgreementType");

        migrationBuilder.DropTable(
            "AuditEntity");

        migrationBuilder.DropTable(
            "CalculationType");

        migrationBuilder.DropTable(
            "ChartMonth");

        migrationBuilder.DropTable(
            "Debt");

        migrationBuilder.DropTable(
            "ColumnItem");

        migrationBuilder.DropTable(
            "CompanyCarRoadList");

        migrationBuilder.DropTable(
            "OrderItemBaseShiftStatus");

        migrationBuilder.DropTable(
            "ReSaleItem");

        migrationBuilder.DropTable(
            "SupplyReturnItem");

        migrationBuilder.DropTable(
            "TaxFreeItem");

        migrationBuilder.DropTable(
            "SaleMessageNumerator");

        migrationBuilder.DropTable(
            "CrossExchangeRate");

        migrationBuilder.DropTable(
            "DashboardNodeModule");

        migrationBuilder.DropTable(
            "DynamicProductPlacementRow");

        migrationBuilder.DropTable(
            "FilterItem");

        migrationBuilder.DropTable(
            "FilterOperationItem");

        migrationBuilder.DropTable(
            "GovCrossExchangeRate");

        migrationBuilder.DropTable(
            "GovExchangeRate");

        migrationBuilder.DropTable(
            "OrderPackage");

        migrationBuilder.DropTable(
            "CompanyCarFueling");

        migrationBuilder.DropTable(
            "DepreciatedConsumableOrderItem");

        migrationBuilder.DropTable(
            "PaymentCostMovement");

        migrationBuilder.DropTable(
            "SupplyOrderPaymentDeliveryProtocol");

        migrationBuilder.DropTable(
            "IncomePaymentOrder");

        migrationBuilder.DropTable(
            "PaymentRegisterCurrencyExchange");

        migrationBuilder.DropTable(
            "PaymentRegisterTransfer");

        migrationBuilder.DropTable(
            "PaymentMovement");

        migrationBuilder.DropTable(
            "PerfectClientValue");

        migrationBuilder.DropTable(
            "CarBrand");

        migrationBuilder.DropTable(
            "Category");

        migrationBuilder.DropTable(
            "OriginalNumber");

        migrationBuilder.DropTable(
            "ProductGroup");

        migrationBuilder.DropTable(
            "SadItem");

        migrationBuilder.DropTable(
            "SadPallet");

        migrationBuilder.DropTable(
            "ExchangeRate");

        migrationBuilder.DropTable(
            "CustomService");

        migrationBuilder.DropTable(
            "ServiceDetailItemKey");

        migrationBuilder.DropTable(
            "ShipmentList");

        migrationBuilder.DropTable(
            "SupplyInformationDeliveryProtocolKey");

        migrationBuilder.DropTable(
            "MergedService");

        migrationBuilder.DropTable(
            "SupplyDeliveryDocument");

        migrationBuilder.DropTable(
            "ProductPlacement");

        migrationBuilder.DropTable(
            "SupplyOrderUkraineCartItemReservation");

        migrationBuilder.DropTable(
            "SupplyOrderUkrainePaymentDeliveryProtocolKey");

        migrationBuilder.DropTable(
            "Sale");

        migrationBuilder.DropTable(
            "ReSaleAvailability");

        migrationBuilder.DropTable(
            "ReSale");

        migrationBuilder.DropTable(
            "SupplyReturn");

        migrationBuilder.DropTable(
            "TaxFreePackListOrderItem");

        migrationBuilder.DropTable(
            "DynamicProductPlacementColumn");

        migrationBuilder.DropTable(
            "CompanyCar");

        migrationBuilder.DropTable(
            "OutcomePaymentOrder");

        migrationBuilder.DropTable(
            "ConsumablesOrderItem");

        migrationBuilder.DropTable(
            "DepreciatedConsumableOrder");

        migrationBuilder.DropTable(
            "CurrencyTrader");

        migrationBuilder.DropTable(
            "PerfectClient");

        migrationBuilder.DropTable(
            "SadPalletType");

        migrationBuilder.DropTable(
            "SupplyOrderUkraineCartItem");

        migrationBuilder.DropTable(
            "DeliveryRecipientAddress");

        migrationBuilder.DropTable(
            "SaleInvoiceDocument");

        migrationBuilder.DropTable(
            "SaleInvoiceNumber");

        migrationBuilder.DropTable(
            "SaleBaseShiftStatus");

        migrationBuilder.DropTable(
            "Transporter");

        migrationBuilder.DropTable(
            "DepreciatedOrderItem");

        migrationBuilder.DropTable(
            "ProductReservation");

        migrationBuilder.DropTable(
            "ProductTransferItem");

        migrationBuilder.DropTable(
            "BaseLifeCycleStatus");

        migrationBuilder.DropTable(
            "BaseSalePaymentStatus");

        migrationBuilder.DropTable(
            "SaleNumber");

        migrationBuilder.DropTable(
            "PaymentCurrencyRegister");

        migrationBuilder.DropTable(
            "Sad");

        migrationBuilder.DropTable(
            "SupplyOrderPolandPaymentDeliveryProtocol");

        migrationBuilder.DropTable(
            "TaxFree");

        migrationBuilder.DropTable(
            "ConsumableProduct");

        migrationBuilder.DropTable(
            "ConsumablesOrder");

        migrationBuilder.DropTable(
            "ClientTypeRole");

        migrationBuilder.DropTable(
            "DeliveryRecipient");

        migrationBuilder.DropTable(
            "TransporterType");

        migrationBuilder.DropTable(
            "DepreciatedOrder");

        migrationBuilder.DropTable(
            "ProductAvailability");

        migrationBuilder.DropTable(
            "PaymentRegister");

        migrationBuilder.DropTable(
            "OrganizationClientAgreement");

        migrationBuilder.DropTable(
            "SupplyOrderPaymentDeliveryProtocolKey");

        migrationBuilder.DropTable(
            "StathamCar");

        migrationBuilder.DropTable(
            "StathamPassport");

        migrationBuilder.DropTable(
            "TaxFreePackList");

        migrationBuilder.DropTable(
            "ConsumableProductCategory");

        migrationBuilder.DropTable(
            "ConsumablesStorage");

        migrationBuilder.DropTable(
            "ClientType");

        migrationBuilder.DropTable(
            "OrganizationClient");

        migrationBuilder.DropTable(
            "Statham");

        migrationBuilder.DropTable(
            "User");

        migrationBuilder.DropTable(
            "UserDetails");

        migrationBuilder.DropTable(
            "UserRole");

        migrationBuilder.DropTable(
            "ResidenceCard");

        migrationBuilder.DropTable(
            "WorkPermit");

        migrationBuilder.DropTable(
            "WorkingContract");

        migrationBuilder.DropTable(
            "SupplyInvoice");

        migrationBuilder.DropTable(
            "SupplyOrderUkraine");

        migrationBuilder.DropTable(
            "ActReconciliation");

        migrationBuilder.DropTable(
            "Product");

        migrationBuilder.DropTable(
            "MeasureUnit");

        migrationBuilder.DropTable(
            "SupplyInvoiceOrderItem");

        migrationBuilder.DropTable(
            "SupplyOrderItem");

        migrationBuilder.DropTable(
            "SupplyOrder");

        migrationBuilder.DropTable(
            "CustomAgencyService");

        migrationBuilder.DropTable(
            "PlaneDeliveryService");

        migrationBuilder.DropTable(
            "PortCustomAgencyService");

        migrationBuilder.DropTable(
            "PortWorkService");

        migrationBuilder.DropTable(
            "SupplyOrderNumber");

        migrationBuilder.DropTable(
            "SupplyProForm");

        migrationBuilder.DropTable(
            "TransportationService");

        migrationBuilder.DropTable(
            "VehicleDeliveryService");

        migrationBuilder.DropTable(
            "SupplyOrderUkraineItem");

        migrationBuilder.DropTable(
            "ConsignmentItem");

        migrationBuilder.DropTable(
            "Consignment");

        migrationBuilder.DropTable(
            "ProductIncomeItem");

        migrationBuilder.DropTable(
            "ProductTransfer");

        migrationBuilder.DropTable(
            "ActReconciliationItem");

        migrationBuilder.DropTable(
            "PackingListPackageOrderItem");

        migrationBuilder.DropTable(
            "ProductCapitalizationItem");

        migrationBuilder.DropTable(
            "ProductIncome");

        migrationBuilder.DropTable(
            "SaleReturnItem");

        migrationBuilder.DropTable(
            "PackingListPackage");

        migrationBuilder.DropTable(
            "ProductCapitalization");

        migrationBuilder.DropTable(
            "OrderItem");

        migrationBuilder.DropTable(
            "SaleReturn");

        migrationBuilder.DropTable(
            "PackingList");

        migrationBuilder.DropTable(
            "ProductSpecification");

        migrationBuilder.DropTable(
            "MisplacedSale");

        migrationBuilder.DropTable(
            "Order");

        migrationBuilder.DropTable(
            "ContainerService");

        migrationBuilder.DropTable(
            "VehicleService");

        migrationBuilder.DropTable(
            "RetailClient");

        migrationBuilder.DropTable(
            "ClientShoppingCart");

        migrationBuilder.DropTable(
            "BillOfLadingDocument");

        migrationBuilder.DropTable(
            "EcommerceRegion");

        migrationBuilder.DropTable(
            "ClientAgreement");

        migrationBuilder.DropTable(
            "BillOfLadingService");

        migrationBuilder.DropTable(
            "Agreement");

        migrationBuilder.DropTable(
            "Client");

        migrationBuilder.DropTable(
            "ActProvidingService");

        migrationBuilder.DropTable(
            "SupplyPaymentTask");

        migrationBuilder.DropTable(
            "ActProvidingServiceDocument");

        migrationBuilder.DropTable(
            "DeliveryProductProtocol");

        migrationBuilder.DropTable(
            "SupplyInformationTask");

        migrationBuilder.DropTable(
            "SupplyOrganizationAgreement");

        migrationBuilder.DropTable(
            "SupplyServiceAccountDocument");

        migrationBuilder.DropTable(
            "ProviderPricing");

        migrationBuilder.DropTable(
            "ClientBankDetails");

        migrationBuilder.DropTable(
            "Country");

        migrationBuilder.DropTable(
            "PackingMarking");

        migrationBuilder.DropTable(
            "PackingMarkingPayment");

        migrationBuilder.DropTable(
            "RegionCode");

        migrationBuilder.DropTable(
            "TermsOfDelivery");

        migrationBuilder.DropTable(
            "DeliveryProductProtocolNumber");

        migrationBuilder.DropTable(
            "AgreementTypeCivilCode");

        migrationBuilder.DropTable(
            "SupplyOrganization");

        migrationBuilder.DropTable(
            "TaxAccountingScheme");

        migrationBuilder.DropTable(
            "Pricing");

        migrationBuilder.DropTable(
            "ClientBankDetailAccountNumber");

        migrationBuilder.DropTable(
            "ClientBankDetailIbanNo");

        migrationBuilder.DropTable(
            "Region");

        migrationBuilder.DropTable(
            "PriceType");

        migrationBuilder.DropTable(
            "Organization");

        migrationBuilder.DropTable(
            "Currency");

        migrationBuilder.DropTable(
            "Storage");

        migrationBuilder.DropTable(
            "TaxInspection");

        migrationBuilder.DropTable(
            "VatRate");
    }
}
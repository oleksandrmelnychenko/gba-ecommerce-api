using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Synchronizations;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.Messages.Communications.Hubs;
using GBA.Domain.Messages.DataSync;
using GBA.Domain.Messages.Logging;
using GBA.Domain.Repositories.DataSync.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.TranslationEntities;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using SlugGenerator;

namespace GBA.Services.Actors.DataSync;

public sealed class SyncProductsWorkerActor : ReceiveActor {
    private const string SUPPLY_PRICING_NAME = "��";
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"�<>. ^*()%!\\-]", RegexOptions.Compiled);

    private static readonly Regex ApostropheReplace = new("'+", RegexOptions.Compiled);

    private readonly IDbConnectionFactory _connectionFactory;

    private readonly IDataSyncRepositoriesFactory _dataSyncRepositoriesFactory;

    private readonly IStringLocalizer<SharedResource> _localizer;

    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    private readonly IUserRepositoriesFactory _userRepositoriesFactory;

    public SyncProductsWorkerActor(
        IStringLocalizer<SharedResource> localizer,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        IDataSyncRepositoriesFactory dataSyncRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _localizer = localizer;

        _connectionFactory = connectionFactory;

        _userRepositoriesFactory = userRepositoriesFactory;

        _dataSyncRepositoriesFactory = dataSyncRepositoriesFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<SynchronizeProductsMessage>(ProcessSynchronizeProductsMessage);
    }

    private void ProcessSynchronizeProductsMessage(SynchronizeProductsMessage message) {
        using IDbConnection oneCConnection = _connectionFactory.NewFenixOneCSqlConnection();
        using IDbConnection amgCConnection = _connectionFactory.NewAmgOneCSqlConnection();
        using IDbConnection remoteSyncConnection = _connectionFactory.NewSqlConnection();
        IUserRepository userRepository = _userRepositoriesFactory.NewUserRepository(remoteSyncConnection);

        IActorRef hubSenderActorRef = ActorReferenceManager.Instance.Get(CommunicationsActorNames.HUBS_SENDER_ACTOR);

        User currentUser = userRepository.GetByNetIdWithoutIncludes(message.UserNetId);

        _dataSyncRepositoriesFactory
            .NewDataSyncOperationRepository(remoteSyncConnection)
            .Add(new DataSyncOperation {
                UserId = currentUser.Id,
                OperationType = DataSyncOperationType.Products,
                ForAmg = message.ForAmg
            });

        SynchronizeMeasureUnits(hubSenderActorRef, oneCConnection, remoteSyncConnection, currentUser, amgCConnection, message.ForAmg);

        SynchronizeProductGroups(hubSenderActorRef, oneCConnection, remoteSyncConnection, currentUser, amgCConnection, message.ForAmg);

        SynchronizeProducts(hubSenderActorRef, oneCConnection, remoteSyncConnection, currentUser, amgCConnection, message.ForAmg);

        ActorReferenceManager.Instance.Get(DataSyncActorNames.DATA_SYNC_WORKER_ACTOR)
            .Tell(new StartDataSyncWorkMessage(message.SyncEntityTypes, message.UserNetId, message.ForAmg));
    }

    private void SynchronizeMeasureUnits(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        User currentUser,
        IDbConnection amgSyncConnection,
        bool forAmg) {
        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.MEASURE_UNITS_SYNC_START]));

        try {
            IMeasureUnitsSyncRepository measureUnitsSyncRepository =
                _dataSyncRepositoriesFactory
                    .NewMeasureUnitsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);

            IEnumerable<SyncMeasureUnit> syncMeasureUnits =
                forAmg ? measureUnitsSyncRepository.GetAmgAllSyncMeasureUnits() : measureUnitsSyncRepository.GetAllSyncMeasureUnits();

            IEnumerable<MeasureUnit> measureUnits =
                measureUnitsSyncRepository.GetAllMeasureUnit();

            foreach (SyncMeasureUnit unit in syncMeasureUnits) {
                MeasureUnit measureUnit = measureUnits.FirstOrDefault(m => m.CodeOneC.Equals(unit.Code));

                if (measureUnit == null) {
                    measureUnit = new MeasureUnit {
                        Name = unit.Name,
                        Description = unit.FullName,
                        CodeOneC = unit.Code
                    };

                    measureUnit.Id = measureUnitsSyncRepository.Add(measureUnit);

                    measureUnitsSyncRepository.Add(new MeasureUnitTranslation {
                        Name = unit.Name,
                        Description = unit.FullName,
                        CultureCode = "uk",
                        MeasureUnitId = measureUnit.Id
                    });
                } else if (!unit.IsDataEqual(measureUnit)) {
                    measureUnit.Name = unit.Name;
                    measureUnit.Description = unit.FullName;
                    measureUnit.Deleted = false;
                    measureUnitsSyncRepository.Update(measureUnit);

                    if (!measureUnit.MeasureUnitTranslations.Any()) continue;

                    MeasureUnitTranslation translation = measureUnit.MeasureUnitTranslations.First();

                    translation.Name = unit.Name;
                    translation.Description = measureUnit.Description;

                    measureUnitsSyncRepository.Update(translation);
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.MEASURE_UNITS_SYNC_END]));
        } catch (Exception exc) {
            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Measure units",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }

    private void SynchronizeProductGroups(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        User currentUser,
        IDbConnection amgSyncConnection,
        bool forAmg) {
        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_GROUPS_SYNC_START]));

        try {
            IProductGroupsSyncRepository productGroupsSyncRepository =
                _dataSyncRepositoriesFactory
                    .NewProductGroupsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);

            IEnumerable<SyncProductGroup> syncProductGroups =
                forAmg ? productGroupsSyncRepository.GetAmgAllSyncProductGroup() : productGroupsSyncRepository.GetAllSyncProductGroup();

            IEnumerable<ProductGroup> productGroups =
                productGroupsSyncRepository.GetAllProductGroups();

            foreach (SyncProductGroup syncProductGroup in syncProductGroups) {
                ProductGroup productGroup = productGroups.FirstOrDefault(p => syncProductGroup.SourceIdsOrNameEqual(p.SourceAmgId, p.SourceFenixId, p.Name));

                if (productGroup == null) {
                    productGroup = new ProductGroup {
                        Name = syncProductGroup.Name,
                        Description = syncProductGroup.Name,
                        FullName = syncProductGroup.Name,
                        IsSubGroup = syncProductGroup.IsSubGroup
                    };

                    if (forAmg)
                        productGroup.SourceAmgId = syncProductGroup.SourceId;
                    else
                        productGroup.SourceFenixId = syncProductGroup.SourceId;

                    productGroup.Id = productGroupsSyncRepository.Add(productGroup);
                } else {
                    productGroup.Name = syncProductGroup.Name;
                    productGroup.Description = syncProductGroup.Name;
                    productGroup.FullName = syncProductGroup.Name;
                    productGroup.IsSubGroup = syncProductGroup.IsSubGroup;
                    productGroup.Deleted = false;

                    if (forAmg)
                        productGroup.SourceAmgId = syncProductGroup.SourceId;
                    else
                        productGroup.SourceFenixId = syncProductGroup.SourceId;

                    productGroupsSyncRepository.Update(productGroup);
                }
            }

            IEnumerable<ProductGroup> updatedProductGroups =
                productGroupsSyncRepository.GetAllProductGroups();

            foreach (SyncProductGroup syncProductGroup in syncProductGroups) {
                ProductGroup productGroup = updatedProductGroups.FirstOrDefault(p => syncProductGroup.SourceIdsOrNameEqual(p.SourceAmgId, p.SourceFenixId, p.Name));

                ProductGroup rootProductGroup = null;

                if (syncProductGroup.IsSubGroup)
                    rootProductGroup = productGroupsSyncRepository.GetProductGroupBySourceId(syncProductGroup.ParentSourceId);

                if (productGroup == null || rootProductGroup == null) continue;

                ProductSubGroup productSubGroup =
                    productGroupsSyncRepository.GetProductSubGroupByIdsIfExists(rootProductGroup.Id, productGroup.Id);

                if (productSubGroup == null) {
                    productGroupsSyncRepository.Add(new ProductSubGroup {
                        RootProductGroupId = rootProductGroup.Id,
                        SubProductGroupId = productGroup.Id
                    });
                } else {
                    if (syncProductGroup.IsSubGroup) {
                        productSubGroup.Deleted = false;

                        productGroupsSyncRepository.Update(productSubGroup);
                    } else {
                        productGroupsSyncRepository.RemoveAssignmentsForRootProductGroupById(productGroup.Id);
                    }
                }
            }

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_GROUPS_SYNC_END]));
        } catch (Exception exc) {
            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Product groups",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }

    private void SynchronizeProducts(
        IActorRef hubSenderActorRef,
        IDbConnection oneCConnection,
        IDbConnection remoteSyncConnection,
        User currentUser,
        IDbConnection amgSyncConnection,
        bool forAmg) {
        hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCTS_SYNC_START]));

        try {
            IProductsSyncRepository productsSyncRepository =
                _dataSyncRepositoriesFactory.NewProductsSyncRepository(oneCConnection, remoteSyncConnection, amgSyncConnection);
            IProductRepository productRepository =
                _productRepositoriesFactory.NewProductRepository(remoteSyncConnection);

            IEnumerable<SyncProduct> syncProducts =
                forAmg ? productsSyncRepository.GetAmgAllSyncProducts() : productsSyncRepository.GetAllSyncProducts();

            List<MeasureUnit> measureUnits = productsSyncRepository.GetAllMeasureUnits();

            List<Product> products =
                syncProducts
                    .Select(syncProduct => {
                        MeasureUnit measureUnit =
                            measureUnits.FirstOrDefault(m => m.CodeOneC == syncProduct.MeasureUnitCode);

                        if (measureUnit != null) {
                            Product product = new() {
                                Description = string.IsNullOrEmpty(syncProduct.Description) ? string.Empty : syncProduct.Description,
                                DescriptionUA = string.IsNullOrEmpty(syncProduct.DescriptionUa) ? string.Empty : syncProduct.DescriptionUa,
                                DescriptionPL = string.IsNullOrEmpty(syncProduct.DescriptionPl) ? string.Empty : syncProduct.DescriptionPl,
                                SearchDescription = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.Description) ? string.Empty : syncProduct.Description,
                                    string.Empty),
                                SearchDescriptionUA = SpecialCharactersReplace.Replace(
                                    string.IsNullOrEmpty(syncProduct.DescriptionUa) ? string.Empty : syncProduct.DescriptionUa,
                                    string.Empty),
                                SearchDescriptionPL = StringOptimizations.RemovePolishDiacritics(
                                    SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.DescriptionPl) ? string.Empty : syncProduct.DescriptionPl, string.Empty)),
                                Name = string.IsNullOrEmpty(syncProduct.Name) ? string.Empty : syncProduct.Name,
                                NameUA = string.IsNullOrEmpty(syncProduct.NameUa) ? string.Empty : syncProduct.NameUa,
                                NamePL = string.IsNullOrEmpty(syncProduct.NamePl) ? string.Empty : syncProduct.NamePl,
                                SearchName = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.Name) ? string.Empty : syncProduct.Name, string.Empty),
                                SearchNameUA = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.NameUa) ? string.Empty : syncProduct.NameUa, string.Empty),
                                SearchNamePL = StringOptimizations.RemovePolishDiacritics(
                                    SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.NamePl) ? string.Empty : syncProduct.NamePl, string.Empty)),
                                HasAnalogue = false,
                                HasComponent = false,
                                HasImage = false,
                                Image = string.Empty,
                                Notes = string.Empty,
                                NotesUA = string.Empty,
                                NotesPL = string.Empty,
                                Size = string.IsNullOrEmpty(syncProduct.Size) ? string.Empty : syncProduct.Size,
                                SearchSize = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.Size) ? string.Empty : syncProduct.Size, string.Empty),
                                Volume = string.IsNullOrEmpty(syncProduct.Volume) ? string.Empty : syncProduct.Volume,
                                Weight = syncProduct.Weight,
                                IsForWeb = true,
                                IsForSale = syncProduct.IsForSale,
                                IsForZeroSale = syncProduct.IsZeroForSale,
                                OrderStandard = string.IsNullOrEmpty(syncProduct.OrderStandard) ? string.Empty : syncProduct.OrderStandard,
                                PackingStandard = string.IsNullOrEmpty(syncProduct.PackingStandard) ? string.Empty : syncProduct.PackingStandard,
                                Standard = string.IsNullOrEmpty(syncProduct.Standard) ? string.Empty : syncProduct.Standard,
                                VendorCode = string.IsNullOrEmpty(syncProduct.VendorCode) ? string.Empty : syncProduct.VendorCode,
                                SearchVendorCode = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.VendorCode) ? string.Empty : syncProduct.VendorCode,
                                    string.Empty),
                                SynonymsUA = string.Empty,
                                SynonymsPL = string.Empty,
                                SearchSynonymsUA = string.Empty,
                                SearchSynonymsPL = string.Empty,
                                UCGFEA = string.IsNullOrEmpty(syncProduct.UCGFEA) ? string.Empty : syncProduct.UCGFEA,
                                Top = string.IsNullOrEmpty(syncProduct.Top) ? string.Empty : syncProduct.Top.Length > 3 ? syncProduct.Top.Substring(0, 3) : syncProduct.Top,
                                MainOriginalNumber = string.IsNullOrEmpty(syncProduct.OriginalNumberCode) ? string.Empty : syncProduct.OriginalNumberCode,
                                MeasureUnitId = measureUnit.Id
                            };

                            if (forAmg) {
                                product.SourceAmgCode = syncProduct.Code;
                                product.SourceAmgId = syncProduct.SourceId;
                                product.ParentAmgId = syncProduct.ParentId;
                            } else {
                                product.ParentFenixId = syncProduct.ParentId;
                                product.SourceFenixCode = syncProduct.Code;
                                product.SourceFenixId = syncProduct.SourceId;
                            }

                            if (string.IsNullOrEmpty(product.NameUA))
                                product.NameUA = product.Name;

                            return product;
                        }

                        measureUnit = new MeasureUnit {
                            CodeOneC = syncProduct.MeasureUnitCode,
                            Name = syncProduct.MeasureUnitName
                        };

                        measureUnit.Id = productsSyncRepository.Add(measureUnit);

                        productsSyncRepository.Add(new MeasureUnitTranslation {
                            MeasureUnitId = measureUnit.Id,
                            Name = syncProduct.MeasureUnitName,
                            CultureCode = "uk"
                        });

                        measureUnits.Add(measureUnit);

                        Product newProduct = new() {
                            Description = string.IsNullOrEmpty(syncProduct.Description) ? string.Empty : syncProduct.Description,
                            DescriptionUA = string.IsNullOrEmpty(syncProduct.DescriptionUa) ? string.Empty : syncProduct.DescriptionUa,
                            DescriptionPL = string.IsNullOrEmpty(syncProduct.DescriptionPl) ? string.Empty : syncProduct.DescriptionPl,
                            SearchDescription = SpecialCharactersReplace.Replace(
                                string.IsNullOrEmpty(syncProduct.Description) ? string.Empty : syncProduct.Description.ToLower(),
                                string.Empty),
                            SearchDescriptionUA = SpecialCharactersReplace.Replace(
                                string.IsNullOrEmpty(syncProduct.DescriptionUa) ? string.Empty : syncProduct.DescriptionUa.ToLower(),
                                string.Empty),
                            SearchDescriptionPL = StringOptimizations.RemovePolishDiacritics(
                                SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.DescriptionPl) ? string.Empty : syncProduct.DescriptionPl.ToLower(),
                                    string.Empty)),
                            Name = string.IsNullOrEmpty(syncProduct.Name) ? string.Empty : syncProduct.Name,
                            NameUA = string.IsNullOrEmpty(syncProduct.NameUa) ? string.Empty : syncProduct.NameUa,
                            NamePL = string.IsNullOrEmpty(syncProduct.NamePl) ? string.Empty : syncProduct.NamePl,
                            SearchName = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.Name) ? string.Empty : syncProduct.Name.ToLower(), string.Empty),
                            SearchNameUA = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.NameUa) ? string.Empty : syncProduct.NameUa.ToLower(),
                                string.Empty),
                            SearchNamePL = StringOptimizations.RemovePolishDiacritics(
                                SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.NamePl) ? string.Empty : syncProduct.NamePl.ToLower(), string.Empty)),
                            HasAnalogue = false,
                            HasComponent = false,
                            HasImage = false,
                            Image = string.Empty,
                            Notes = string.Empty,
                            NotesUA = string.Empty,
                            NotesPL = string.Empty,
                            Size = string.IsNullOrEmpty(syncProduct.Size) ? string.Empty : syncProduct.Size,
                            SearchSize = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.Size) ? string.Empty : syncProduct.Size, string.Empty),
                            Volume = string.IsNullOrEmpty(syncProduct.Volume) ? string.Empty : syncProduct.Volume,
                            Weight = syncProduct.Weight,
                            IsForWeb = true,
                            IsForSale = syncProduct.IsForSale,
                            IsForZeroSale = syncProduct.IsZeroForSale,
                            OrderStandard = string.IsNullOrEmpty(syncProduct.OrderStandard) ? string.Empty : syncProduct.OrderStandard,
                            PackingStandard = string.IsNullOrEmpty(syncProduct.PackingStandard) ? string.Empty : syncProduct.PackingStandard,
                            Standard = string.IsNullOrEmpty(syncProduct.Standard) ? string.Empty : syncProduct.Standard,
                            VendorCode = string.IsNullOrEmpty(syncProduct.VendorCode) ? string.Empty : syncProduct.VendorCode,
                            SearchVendorCode = SpecialCharactersReplace.Replace(string.IsNullOrEmpty(syncProduct.VendorCode) ? string.Empty : syncProduct.VendorCode,
                                string.Empty),
                            SynonymsUA = string.Empty,
                            SynonymsPL = string.Empty,
                            SearchSynonymsUA = string.Empty,
                            SearchSynonymsPL = string.Empty,
                            UCGFEA = string.IsNullOrEmpty(syncProduct.UCGFEA) ? string.Empty : syncProduct.UCGFEA,
                            Top = string.IsNullOrEmpty(syncProduct.Top) ? string.Empty : syncProduct.Top.Length > 3 ? syncProduct.Top.Substring(0, 3) : syncProduct.Top,
                            MainOriginalNumber = string.IsNullOrEmpty(syncProduct.OriginalNumberName) ? string.Empty : syncProduct.OriginalNumberName,
                            MeasureUnitId = measureUnit.Id
                        };

                        if (forAmg) {
                            newProduct.SourceAmgCode = syncProduct.Code;
                            newProduct.SourceAmgId = syncProduct.SourceId;
                            newProduct.ParentAmgId = syncProduct.ParentId;
                        } else {
                            newProduct.ParentFenixId = syncProduct.ParentId;
                            newProduct.SourceFenixCode = syncProduct.Code;
                            newProduct.SourceFenixId = syncProduct.SourceId;
                        }

                        return newProduct;
                    }).ToList();

            StringBuilder builder = new();

            builder.Append("CREATE TABLE #TempProduct ");
            builder.Append("( ");
            builder.Append("[Id] bigint null, ");
            builder.Append("[Description] nvarchar(2000), ");
            builder.Append("[HasAnalogue] bit, ");
            builder.Append("[HasImage] bit, ");
            builder.Append("[IsForSale] bit, ");
            builder.Append("[IsForWeb] bit, ");
            builder.Append("[IsForZeroSale] bit, ");
            builder.Append("[MainOriginalNumber] nvarchar(80), ");
            builder.Append("[MeasureUnitID] bigint, ");
            builder.Append("[Name] nvarchar(120), ");
            builder.Append("[OrderStandard] nvarchar(max), ");
            builder.Append("[PackingStandard] nvarchar(max), ");
            builder.Append("[Standard] nvarchar(max), ");
            builder.Append("[Size] nvarchar(100), ");
            builder.Append("[UCGFEA] nvarchar(max), ");
            builder.Append("[Updated] datetime2(7), ");
            builder.Append("[VendorCode] nvarchar(40), ");
            builder.Append("[Volume] nvarchar(max), ");
            builder.Append("[Weight] float, ");
            builder.Append("[HasComponent] bit, ");
            builder.Append("[Image] nvarchar(max), ");
            builder.Append("[Top] nvarchar(3), ");
            builder.Append("[DescriptionPL] nvarchar(2000), ");
            builder.Append("[DescriptionUA] nvarchar(2000), ");
            builder.Append("[NamePL] nvarchar(120), ");
            builder.Append("[NameUA] nvarchar(120), ");
            builder.Append("[SourceAmgCode] bigint, ");
            builder.Append("[SourceFenixCode] bigint, ");
            builder.Append("[SourceAmgId] varbinary(16), ");
            builder.Append("[SourceFenixId] varbinary(16), ");
            builder.Append("[ParentAmgId] varbinary(16), ");
            builder.Append("[ParentFenixId] varbinary(16), ");
            builder.Append("[SearchDescriptionPL] nvarchar(2000), ");
            builder.Append("[SearchNamePL] nvarchar(120), ");
            builder.Append("[NotesPL] nvarchar(2000), ");
            builder.Append("[NotesUA] nvarchar(2000), ");
            builder.Append("[SearchDescriptionUA] nvarchar(2000), ");
            builder.Append("[SearchNameUA] nvarchar(120), ");
            builder.Append("[SearchSize] nvarchar(100), ");
            builder.Append("[SearchVendorCode] nvarchar(40), ");
            builder.Append("[SearchDescription] nvarchar(2000), ");
            builder.Append("[SearchName] nvarchar(120), ");
            builder.Append("[SearchSynonymsPL] nvarchar(2000), ");
            builder.Append("[SearchSynonymsUA] nvarchar(2000), ");
            builder.Append("[SynonymsPL] nvarchar(2000), ");
            builder.Append("[SynonymsUA] nvarchar(2000) ");
            builder.Append(") ");

            for (int i = 0; i < products.Count; i++) {
                if (i.Equals(0) || i % 100 == 0) {
                    builder.Append("INSERT INTO #TempProduct ");
                    builder.Append("( ");
                    builder.Append("[Description] ");
                    builder.Append(",[DescriptionPL] ");
                    builder.Append(",[DescriptionUA] ");
                    builder.Append(",[Name] ");
                    builder.Append(",[NamePL] ");
                    builder.Append(",[NameUA] ");
                    builder.Append(",[HasAnalogue] ");
                    builder.Append(",[HasImage] ");
                    builder.Append(",[IsForSale] ");
                    builder.Append(",[IsForWeb] ");
                    builder.Append(",[IsForZeroSale] ");
                    builder.Append(",[MainOriginalNumber] ");
                    builder.Append(",[MeasureUnitID] ");
                    builder.Append(",[OrderStandard] ");
                    builder.Append(",[PackingStandard] ");
                    builder.Append(",[Standard] ");
                    builder.Append(",[Size] ");
                    builder.Append(",[UCGFEA] ");
                    builder.Append(",[Updated] ");
                    builder.Append(",[VendorCode] ");
                    builder.Append(",[Volume] ");
                    builder.Append(",[Weight] ");
                    builder.Append(",[HasComponent] ");
                    builder.Append(",[Image] ");
                    builder.Append(",[Top] ");
                    builder.Append(",[SourceAmgCode] ");
                    builder.Append(",[SourceFenixCode] ");
                    builder.Append(",[SourceAmgId] ");
                    builder.Append(",[SourceFenixId] ");
                    builder.Append(",[ParentAmgId] ");
                    builder.Append(",[ParentFenixId] ");
                    builder.Append(",[SearchDescriptionPL] ");
                    builder.Append(",[SearchNamePL] ");
                    builder.Append(",[NotesPL] ");
                    builder.Append(",[NotesUA] ");
                    builder.Append(",[SearchDescriptionUA] ");
                    builder.Append(",[SearchNameUA] ");
                    builder.Append(",[SearchSize] ");
                    builder.Append(",[SearchVendorCode] ");
                    builder.Append(",[SearchDescription] ");
                    builder.Append(",[SearchName] ");
                    builder.Append(",[SearchSynonymsPL] ");
                    builder.Append(",[SearchSynonymsUA] ");
                    builder.Append(",[SynonymsPL] ");
                    builder.Append(",[SynonymsUA] ");
                    builder.Append(") ");

                    builder.Append("VALUES ");
                } else {
                    builder.Append(", ");
                }

                builder.Append("( ");
                builder.Append($"N'{ApostropheReplace.Replace(products[i].Description, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].DescriptionPL, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].DescriptionUA, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].Name, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].NamePL, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].NameUA, "''")}' ");
                builder.Append(products[i].HasAnalogue ? ",1 " : ",0 ");
                builder.Append(products[i].HasImage ? ",1 " : ",0 ");
                builder.Append(products[i].IsForSale ? ",1 " : ",0 ");
                builder.Append(products[i].IsForWeb ? ",1 " : ",0 ");
                builder.Append(products[i].IsForZeroSale ? ",1 " : ",0 ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].MainOriginalNumber, "''")}' ");
                builder.Append($",{products[i].MeasureUnitId} ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].OrderStandard, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].PackingStandard, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].Standard, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].Size, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].UCGFEA, "''")}' ");
                builder.Append(",GETUTCDATE() ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].VendorCode, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].Volume, "''")}' ");
                builder.Append($",{products[i].Weight.ToString(CultureInfo.InvariantCulture)} ");
                builder.Append(products[i].HasComponent ? ",1 " : ",0 ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].Image, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].Top, "''")}' ");
                builder.Append(products[i].SourceAmgCode != null ? $",{products[i].SourceAmgCode} " : ",NULL ");
                builder.Append(products[i].SourceFenixCode != null ? $",{products[i].SourceFenixCode} " : ",NULL ");
                builder.Append(products[i].SourceAmgId != null ? $",0x{BitConverter.ToString(products[i].SourceAmgId).Replace("-", "")} " : ",NULL ");
                builder.Append(products[i].SourceFenixId != null ? $",0x{BitConverter.ToString(products[i].SourceFenixId).Replace("-", "")} " : ",NULL ");
                builder.Append(products[i].ParentAmgId != null ? $",0x{BitConverter.ToString(products[i].ParentAmgId).Replace("-", "")} " : ",NULL ");
                builder.Append(products[i].ParentFenixId != null ? $",0x{BitConverter.ToString(products[i].ParentFenixId).Replace("-", "")} " : ",NULL ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchDescriptionPL, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchNamePL, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].NotesPL, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].NotesUA, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchDescriptionUA, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchNameUA, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchSize, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchVendorCode, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchDescription, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchName, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchSynonymsPL, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SearchSynonymsUA, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SynonymsPL, "''")}' ");
                builder.Append($",N'{ApostropheReplace.Replace(products[i].SynonymsUA, "''")}' ");
                builder.Append(") ");
            }

            builder.Append("UPDATE #TempProduct ");
            builder.Append("SET Id = [Product].ID ");
            builder.Append(", NameUA = CASE WHEN [TempProduct].[NameUA] IS NULL OR [TempProduct].[NameUA] = '' THEN [Product].[NameUA] ELSE [TempProduct].[NameUA] END ");
            builder.Append("FROM #TempProduct [TempProduct] ");
            builder.Append("LEFT JOIN [Product] ");
            if (forAmg)
                builder.Append("ON [Product].SourceAmgId = [TempProduct].SourceAmgId ");
            else
                builder.Append("ON [Product].SourceFenixId = [TempProduct].SourceFenixId ");

            builder.Append("OR [Product].VendorCode = [TempProduct].VendorCode ");
            builder.Append("WHERE [Product].ID IS NOT NULL ");

            builder.Append("UPDATE [Product] ");
            builder.Append("SET ");
            builder.Append("[Description] = [TempProduct].[Description] ");
            builder.Append(",[DescriptionPL] = [TempProduct].[DescriptionPL] ");
            builder.Append(",[DescriptionUA] = [TempProduct].[DescriptionUA] ");
            builder.Append(",[Name] = [TempProduct].[Name] ");
            builder.Append(",[NamePL] = [TempProduct].[NamePL] ");
            builder.Append(",[NameUA] = [TempProduct].[NameUA] ");
            builder.Append(",[HasAnalogue] = [TempProduct].[HasAnalogue] ");
            builder.Append(",[HasImage] = [TempProduct].[HasImage] ");
            builder.Append(",[IsForSale] = [TempProduct].[IsForSale] ");
            builder.Append(",[IsForWeb] = [TempProduct].[IsForWeb] ");
            builder.Append(",[IsForZeroSale] = [TempProduct].[IsForZeroSale] ");
            builder.Append(",[MainOriginalNumber] = [TempProduct].[MainOriginalNumber] ");
            builder.Append(",[MeasureUnitID] = [TempProduct].[MeasureUnitID] ");
            builder.Append(",[OrderStandard] = [TempProduct].[OrderStandard] ");
            builder.Append(",[PackingStandard] = [TempProduct].[PackingStandard] ");
            builder.Append(",[Standard] = [TempProduct].[Standard] ");
            builder.Append(",[Size] = [TempProduct].[Size] ");
            builder.Append(",[UCGFEA] = [TempProduct].[UCGFEA] ");
            builder.Append(",[Updated] = [TempProduct].[Updated] ");
            builder.Append(",[VendorCode] = [TempProduct].[VendorCode] ");
            builder.Append(",[Volume] = [TempProduct].[Volume] ");
            builder.Append(",[Weight]  = [TempProduct].[Weight] ");
            builder.Append(",[HasComponent] = [TempProduct].[HasComponent] ");
            builder.Append(",[Image] = [TempProduct].[Image] ");
            builder.Append(",[Top] = [TempProduct].[Top] ");
            if (forAmg) {
                builder.Append(",[SourceAmgCode] = [TempProduct].[SourceAmgCode] ");
                builder.Append(",[SourceAmgId] = [TempProduct].[SourceAmgId] ");
                builder.Append(",[ParentAmgId] = [TempProduct].[ParentAmgId] ");
            } else {
                builder.Append(",[SourceFenixCode] = [TempProduct].[SourceFenixCode] ");
                builder.Append(",[SourceFenixId] = [TempProduct].[SourceFenixId] ");
                builder.Append(",[ParentFenixId] = [TempProduct].[ParentFenixId] ");
            }

            builder.Append(",[SearchDescriptionPL] = [TempProduct].[SearchDescriptionPL] ");
            builder.Append(",[SearchNamePL] = [TempProduct].[SearchNamePL] ");
            builder.Append(",[NotesPL] = [TempProduct].[NotesPL] ");
            builder.Append(",[NotesUA] = [TempProduct].[NotesUA] ");
            builder.Append(",[SearchDescriptionUA] = [TempProduct].[SearchDescriptionUA] ");
            builder.Append(",[SearchNameUA] = [TempProduct].[SearchNameUA] ");
            builder.Append(",[SearchSize] = [TempProduct].[SearchSize] ");
            builder.Append(",[SearchVendorCode] = [TempProduct].[SearchVendorCode] ");
            builder.Append(",[SearchDescription] = [TempProduct].[SearchDescription] ");
            builder.Append(",[SearchName]  = [TempProduct].[SearchName] ");
            builder.Append(",[SearchSynonymsPL] = [TempProduct].[SearchSynonymsPL] ");
            builder.Append(",[SearchSynonymsUA] = [TempProduct].[SearchSynonymsUA] ");
            builder.Append(",[SynonymsPL] = [TempProduct].[SynonymsPL] ");
            builder.Append(",[SynonymsUA] = [TempProduct].[SynonymsUA] ");
            builder.Append(",[Deleted] = 0 ");
            builder.Append("FROM [Product] ");
            builder.Append("LEFT JOIN #TempProduct [TempProduct] ");
            builder.Append("ON [TempProduct].Id = [Product].ID ");
            builder.Append("WHERE [TempProduct].Id IS NOT NULL ");

            builder.Append(
                "INSERT INTO [Product] ([Description] ,[DescriptionPL] ,[DescriptionUA] ,[Name] ,[NamePL] ,[NameUA] ,[HasAnalogue] ,[HasImage] ,[IsForSale] ,[IsForWeb] ");
            builder.Append(
                ",[IsForZeroSale] ,[MainOriginalNumber] ,[MeasureUnitID] ,[OrderStandard] ,[PackingStandard], [Standard] ,[Size] ,[UCGFEA] ,[Updated] ,[VendorCode] ,[Volume] ,[Weight] ");
            builder.Append(
                ",[HasComponent] ,[Image] ,[Top] ,[ParentAmgId], [ParentFenixId] ,[SourceAmgId] ,[SourceFenixId] ,[SourceAmgCode] ,[SourceFenixCode] ,[SearchDescriptionPL] ,[SearchNamePL] ,[NotesPL] ,[NotesUA] ,[SearchDescriptionUA] ");
            builder.Append(
                ",[SearchNameUA] ,[SearchSize] ,[SearchVendorCode] ,[SearchDescription] ,[SearchName] ,[SearchSynonymsPL] ,[SearchSynonymsUA] ,[SynonymsPL] ,[SynonymsUA]) ");

            builder.Append(
                "SELECT [Description] ,[DescriptionPL] ,[DescriptionUA] ,[Name] ,[NamePL] ,[NameUA] ,[HasAnalogue] ,[HasImage] ,[IsForSale] ,[IsForWeb] ,[IsForZeroSale] ");
            builder.Append(
                ",[MainOriginalNumber] ,[MeasureUnitID] ,[OrderStandard] ,[PackingStandard], [Standard] ,[Size] ,[UCGFEA] ,[Updated] ,[VendorCode] ,[Volume] ,[Weight] ,[HasComponent] ");
            builder.Append(
                ",[Image] ,[Top] ,[ParentAmgId], [ParentFenixId] ,[SourceAmgId] ,[SourceFenixId] ,[SourceAmgCode] ,[SourceFenixCode] ,[SearchDescriptionPL] ,[SearchNamePL] ,[NotesPL] ,[NotesUA] ,[SearchDescriptionUA] ");
            builder.Append(
                ",[SearchNameUA] ,[SearchSize] ,[SearchVendorCode] ,[SearchDescription] ,[SearchName] ,[SearchSynonymsPL] ,[SearchSynonymsUA] ,[SynonymsPL] ,[SynonymsUA] ");
            builder.Append("FROM #TempProduct ");
            builder.Append("WHERE Id IS NULL ");

            builder.Append("DROP TABLE #TempProduct ");

            builder.Append("EXEC sp_updatestats ");

            productsSyncRepository.ExecuteQuery(builder.ToString());

            try {
                productsSyncRepository.CleanProductImages();

                string[] photos = Directory.GetFiles(NoltFolderManager.GetProductImagesFolderPath());

                foreach (string item in photos) {
                    string[] splitted = item.Split('\\');

                    string fullName = splitted.Last();

                    string name = fullName.Replace($".{fullName.Split('.').Last()}", "");

                    Product product = productsSyncRepository.GetProductByVendorCode(name);

                    if (product == null) {
                        if (name.ToLower().EndsWith(" copy")) name = name.ToLower().Replace(" copy", "");

                        if (name.ToLower().EndsWith("sorl")) name = name.ToLower().Replace("sorl", "sl");

                        if (name.ToLower().EndsWith("_yeni")) name = name.ToLower().Replace("_yeni", "");

                        if (name.ToLower().EndsWith("_kran")) name = name.ToLower().Replace("_kran", "");

                        if (name.ToLower().EndsWith("_vlagodelitelj")) name = name.ToLower().Replace("_vlagodelitelj", "");

                        if (name.ToLower().EndsWith("_ON")) name = name.ToLower().Replace("_ON", "");

                        if (name.ToLower().EndsWith("_ust")) name = name.ToLower().Replace("_ust", "");

                        if (name.ToLower().EndsWith("_mavi")) name = name.ToLower().Replace("_mavi", "");

                        if (name.ToLower().EndsWith("_k?rm?z?")) name = name.ToLower().Replace("_k?rm?z?", "");

                        if (name.ToLower().EndsWith("_yenii")) name = name.ToLower().Replace("_yenii", "");

                        if (name.ToLower().EndsWith("_siyah")) name = name.ToLower().Replace("_siyah", "");

                        if (name.ToLower().EndsWith("_renkli")) name = name.ToLower().Replace("_renkli", "");

                        if (name.ToLower().EndsWith("_bobin")) name = name.ToLower().Replace("_bobin", "");

                        if (name.ToLower().EndsWith("_renklii")) name = name.ToLower().Replace("_renklii", "");

                        if (name.ToLower().EndsWith("_y")) name = name.ToLower().Replace("_y", "");

                        if (name.ToLower().EndsWith("_kol")) name = name.ToLower().Replace("_kol", "");

                        if (name.ToLower().EndsWith("_alt")) name = name.ToLower().Replace("_alt", "");

                        if (name.EndsWith("_ORING")) name = name.Replace("_ORING", "");

                        if (name.EndsWith("_orjinal")) name = name.Replace("_orjinal", "");

                        if (name.ToLower().EndsWith("_")) name = name.Substring(0, name.Length - 1);

                        if (name.ToLower().EndsWith("_1")) name = name.Substring(0, name.Length - 2);

                        if (name.ToLower().EndsWith(" 1")) name = name.Substring(0, name.Length - 2);

                        if (name.ToLower().EndsWith(" (1)")) name = name.Substring(0, name.Length - 4);

                        if (name.ToLower().EndsWith(" 2")) name = name.Substring(0, name.Length - 2);

                        if (name.ToLower().EndsWith(" (2)")) name = name.Substring(0, name.Length - 4);

                        if (name.StartsWith("~")) name = name.Replace("~", ".");

                        if (name.Contains("#")) name = name.Replace("#", "/");

                        if (name.EndsWith("_PARCA1")) name = name.Replace("_PARCA1", "");

                        if (name.EndsWith("_PARCA2")) name = name.Replace("_PARCA2", "");

                        product = productsSyncRepository.SearchProductByVendorCode(name);

                        if (product == null) continue;

                        string url = $"http://37.48.104.145:13131/images/products/{fullName}".Replace("#", "%23");

                        productsSyncRepository.ExecuteQuery(
                            $"UPDATE [Product] SET HasImage = 1, [Image] = N'{url}' " +
                            $"WHERE ID = {product.Id}; " +
                            $"UPDATE [ProductImage] SET IsMainImage = 0 WHERE [ProductID] = {product.Id}; " +
                            "INSERT INTO [ProductImage] (ImageUrl, IsMainImage, ProductID, Updated) " +
                            $"VALUES (N'{url}', 1, {product.Id}, GETUTCDATE())"
                        );
                    } else {
                        string url = $"http://37.48.104.145:13131/images/products/{fullName}".Replace("#", "%23");

                        productsSyncRepository.ExecuteQuery(
                            $"UPDATE [Product] SET HasImage = 1, [Image] = N'{url}' " +
                            $"WHERE ID = {product.Id}; " +
                            $"UPDATE [ProductImage] SET IsMainImage = 0 WHERE [ProductID] = {product.Id}; " +
                            "INSERT INTO [ProductImage] (ImageUrl, IsMainImage, ProductID, Updated) " +
                            $"VALUES (N'{url}', 1, {product.Id}, GETUTCDATE())"
                        );
                    }
                }
            } catch (Exception) {
                //Ignored
            }

            builder = new StringBuilder();

            List<Product> allProducts =
                productsSyncRepository.GetAllProducts();

            builder.Append("DELETE FROM [ProductSlug]; ");

            for (int i = 0; i < allProducts.Count; i++) {
                if (i.Equals(0) || i % 100 == 0) {
                    builder.Append("INSERT INTO [ProductSlug] ");
                    builder.Append("([Updated], [Url], [Locale], [ProductID])");
                    builder.Append("VALUES ");
                } else {
                    builder.Append(", ");
                }

                string plSlug = $"{(string.IsNullOrEmpty(allProducts[i].NamePL) ? allProducts[i].Name : allProducts[i].NamePL)} {allProducts[i].VendorCode}".GenerateSlug();
                string ukSlug = $"{(string.IsNullOrEmpty(allProducts[i].NameUA) ? allProducts[i].Name : allProducts[i].NameUA)} {allProducts[i].VendorCode}".GenerateSlug();

                builder.Append(
                    $"(GETUTCDATE(), N'{plSlug}', N'pl', {allProducts[i].Id})");
                builder.Append(", ");
                builder.Append(
                    $"(GETUTCDATE(), N'{ukSlug}', N'uk', {allProducts[i].Id})");
            }

            productsSyncRepository.ExecuteQuery(builder.ToString());

            builder = new StringBuilder();

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCTS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() +
                                                                       _localizer[SharedResourceNames.ASSIGN_PRODUCTS_TO_PRODUCT_GROUPS_START]));

            productsSyncRepository.AssignProductsToProductGroups();

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() +
                                                                       _localizer[SharedResourceNames.ASSIGN_PRODUCTS_TO_PRODUCT_GROUPS_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ASSIGN_ANALOGUES_START]));

            List<SyncAnalogue> syncAnalogues =
                forAmg ? productsSyncRepository.GetAmgAllSyncAnalogues() : productsSyncRepository.GetAllSyncAnalogues();

            builder.Append("DELETE FROM [ProductAnalogue] ");

            builder.Append("CREATE TABLE #TempAnalogue ");
            builder.Append("( ");
            builder.Append("[BaseProductCode] bigint, ");
            builder.Append("[AnalogueProductCode] bigint ");
            builder.Append(") ");

            for (int i = 0; i < syncAnalogues.Count; i++) {
                if (i.Equals(0) || i % 100 == 0) {
                    builder.Append("INSERT INTO #TempAnalogue ");
                    builder.Append("( ");
                    builder.Append("[BaseProductCode], ");
                    builder.Append("[AnalogueProductCode] ");
                    builder.Append(") ");
                    builder.Append("VALUES ");
                } else {
                    builder.Append(", ");
                }

                builder.Append("( ");
                builder.Append($"{syncAnalogues[i].BaseProductCode}, ");
                builder.Append($"{syncAnalogues[i].AnalogueProductCode} ");
                builder.Append(") ");
            }

            builder.Append("INSERT INTO [ProductAnalogue] (BaseProductID, AnalogueProductID, Updated) ");
            builder.Append("SELECT [BaseProduct].ID, [AnalogueProduct].ID, GETUTCDATE() ");
            builder.Append("FROM #TempAnalogue ");
            builder.Append("LEFT JOIN [Product] AS [BaseProduct] ");
            if (forAmg)
                builder.Append("ON [BaseProduct].SourceAmgCode = #TempAnalogue.BaseProductCode ");
            else
                builder.Append("ON [BaseProduct].SourceFenixCode = #TempAnalogue.BaseProductCode ");

            builder.Append("LEFT JOIN [Product] AS [AnalogueProduct] ");
            if (forAmg)
                builder.Append("ON [AnalogueProduct].SourceAmgCode = #TempAnalogue.AnalogueProductCode ");
            else
                builder.Append("ON [AnalogueProduct].SourceFenixCode = #TempAnalogue.AnalogueProductCode ");

            builder.Append("WHERE [BaseProduct].ID IS NOT NULL ");
            builder.Append("AND [AnalogueProduct].ID IS NOT NULL ");

            builder.Append("DROP TABLE #TempAnalogue ");

            builder.Append("EXEC sp_updatestats ");

            productsSyncRepository.ExecuteQuery(builder.ToString());

            productRepository.UpdateProductHasAnalogue();

            if (File.Exists(Path.Combine(NoltFolderManager.GetDataFolderPath(), "cars.csv")) &&
                File.Exists(Path.Combine(NoltFolderManager.GetDataFolderPath(), "carProducts.csv"))) {
                productsSyncRepository.CleanCarBrandsAndAssignments();

                string[] parsedCarBrands = File.ReadAllLines(Path.Combine(NoltFolderManager.GetDataFolderPath(), "cars.csv"));

                for (int i = 1; i < parsedCarBrands.Length; i++) {
                    if (string.IsNullOrEmpty(parsedCarBrands[i])) continue;

                    string[] splitted = parsedCarBrands[i].Split(';');

                    if (!splitted.Length.Equals(3)) continue;

                    CarBrand carBrand = new() {
                        Name = splitted[1].Replace("\"", string.Empty),
                        Description = string.Empty,
                        ImageUrl = string.Empty,
                        Alias = splitted[2].Replace("\"", string.Empty)
                    };

                    carBrand.Id = productsSyncRepository.Add(carBrand);
                }

                string[] parsedCarBrandAliases = File.ReadAllLines(Path.Combine(NoltFolderManager.GetDataFolderPath(), "carProducts.csv"));

                builder = new StringBuilder();

                builder.Append("CREATE TABLE #TempProductCarBrand ");
                builder.Append("(ProductCode bigint, CarName nvarchar(100));");

                for (int i = 1; i < parsedCarBrandAliases.Length; i++) {
                    if (i.Equals(1) || i % 1000 == 0) {
                        builder.Append("INSERT INTO #TempProductCarBrand ");
                        builder.Append("([ProductCode], [CarName]) ");
                        builder.Append("VALUES ");
                    } else {
                        builder.Append(", ");
                    }

                    string[] splitted = parsedCarBrandAliases[i].Split(';');

                    builder.Append(string.Format(
                        "({0},N'{1}')",
                        splitted[0].Replace("\"", string.Empty),
                        splitted[2].Replace("\"", string.Empty)
                    ));
                }

                builder.Append("INSERT INTO [ProductCarBrand] ");
                builder.Append("(CarBrandID, ProductID, Updated) ");

                builder.Append("SELECT [CarBrand].ID [CarBrandID], [Product].ID [ProductID], GETUTCDATE() [Updated] ");
                builder.Append("FROM #TempProductCarBrand [Temp] ");
                builder.Append("LEFT JOIN [Product] ");
                if (forAmg)
                    builder.Append("ON [Product].SourceAmgCode = [Temp].ProductCode ");
                else
                    builder.Append("ON [Product].SourceFenixCode = [Temp].ProductCode ");

                builder.Append("AND [Product].Deleted = 0 ");
                builder.Append("LEFT JOIN [CarBrand] ");
                builder.Append("ON [CarBrand].[Name] = [Temp].CarName ");
                builder.Append("AND [CarBrand].Deleted = 0 ");
                builder.Append("WHERE [Product].ID IS NOT NULL ");
                builder.Append("AND [CarBrand].ID IS NOT NULL ");

                productsSyncRepository.ExecuteQuery(builder.ToString());
            }

            builder = new StringBuilder();

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ASSIGN_ANALOGUES_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ASSIGN_COMPONENTS_START]));

            List<SyncComponent> syncComponents =
                forAmg ? productsSyncRepository.GetAmgAllSyncComponents() : productsSyncRepository.GetAllSyncComponents();

            builder.Append("DELETE FROM [ProductSet] ");

            builder.Append("CREATE TABLE #TempComponent ");
            builder.Append("( ");
            builder.Append("[BaseProductCode] bigint, ");
            builder.Append("[ComponentProductCode] bigint, ");
            builder.Append("[SetComponentsQty] bigint ");
            builder.Append(") ");

            for (int i = 0; i < syncComponents.Count; i++) {
                if (i.Equals(0) || i % 100 == 0) {
                    builder.Append("INSERT INTO #TempComponent ");
                    builder.Append("( ");
                    builder.Append("[BaseProductCode], ");
                    builder.Append("[ComponentProductCode], ");
                    builder.Append("[SetComponentsQty] ");
                    builder.Append(") ");
                    builder.Append("VALUES ");
                } else {
                    builder.Append(", ");
                }

                builder.Append("( ");
                builder.Append($"{syncComponents[i].BaseProductCode}, ");
                builder.Append($"{syncComponents[i].ComponentProductCode}, ");
                builder.Append($"{syncComponents[i].SetComponentsQty} ");
                builder.Append(") ");
            }

            builder.Append("INSERT INTO [ProductSet] (BaseProductID, ComponentProductID, SetComponentsQty, Updated) ");
            builder.Append("SELECT [BaseProduct].ID, [ComponentProduct].ID, #TempComponent.SetComponentsQty, GETUTCDATE() ");
            builder.Append("FROM #TempComponent ");
            builder.Append("LEFT JOIN [Product] AS [BaseProduct] ");
            if (forAmg)
                builder.Append("ON [BaseProduct].SourceAmgCode = #TempComponent.BaseProductCode ");
            else
                builder.Append("ON [BaseProduct].SourceFenixCode = #TempComponent.BaseProductCode ");

            builder.Append("LEFT JOIN [Product] AS [ComponentProduct] ");
            if (forAmg)
                builder.Append("ON [ComponentProduct].SourceAmgCode = #TempComponent.ComponentProductCode ");
            else
                builder.Append("ON [ComponentProduct].SourceFenixCode = #TempComponent.ComponentProductCode ");

            builder.Append("WHERE [BaseProduct].ID IS NOT NULL ");
            builder.Append("AND [ComponentProduct].ID IS NOT NULL ");

            builder.Append("DROP TABLE #TempComponent ");

            builder.Append("EXEC sp_updatestats ");

            productsSyncRepository.ExecuteQuery(builder.ToString());

            productRepository.UpdateProductHasComponent();

            builder = new StringBuilder();

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ASSIGN_COMPONENTS_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORIGINAL_NUMBERS_SYNC_START]));

            List<SyncOriginalNumber> syncOriginalNumbers =
                forAmg ? productsSyncRepository.GetAmgAllSyncOriginalNumbers() : productsSyncRepository.GetAllSyncOriginalNumbers();

            builder.Append("DELETE FROM [ProductOriginalNumber] ");
            builder.Append("DELETE FROM [OriginalNumber] ");

            builder.Append("CREATE TABLE #TempOriginalNumber ");
            builder.Append("( ");
            builder.Append("[ProductCode] bigint, ");
            builder.Append("[MainNumber] nvarchar(max), ");
            builder.Append("[SearchNumber] nvarchar(max), ");
            builder.Append("[IsMainNumber] bit ");
            builder.Append(") ");

            for (int i = 0; i < syncOriginalNumbers.Count; i++) {
                if (i.Equals(0) || i % 100 == 0) {
                    builder.Append("INSERT INTO #TempOriginalNumber ");
                    builder.Append("( ");
                    builder.Append("[ProductCode], ");
                    builder.Append("[MainNumber], ");
                    builder.Append("[SearchNumber], ");
                    builder.Append("[IsMainNumber] ");
                    builder.Append(") ");

                    builder.Append("VALUES ");
                } else {
                    builder.Append(", ");
                }

                builder.Append("( ");
                builder.Append($"N'{syncOriginalNumbers[i].ProductCode}' ");
                builder.Append(
                    string.IsNullOrEmpty(syncOriginalNumbers[i].OriginalNumber)
                        ? ",N''"
                        : $",N'{ApostropheReplace.Replace(syncOriginalNumbers[i].OriginalNumber, "''")}' "
                );
                builder.Append(
                    string.IsNullOrEmpty(syncOriginalNumbers[i].OriginalNumber)
                        ? ",N''"
                        : ",N'" +
                          SpecialCharactersReplace.Replace(
                              ApostropheReplace.Replace(syncOriginalNumbers[i].OriginalNumber, "''"),
                              string.Empty
                          )
                          + "' "
                );
                builder.Append(syncOriginalNumbers[i].IsMainNumber ? ",1 " : ",0 ");
                builder.Append(") ");
            }

            builder.Append("INSERT INTO [OriginalNumber] (MainNumber, Number, Updated) ");
            builder.Append("SELECT [MainNumber], [SearchNumber], GETUTCDATE() ");
            builder.Append("FROM #TempOriginalNumber ");
            builder.Append("GROUP BY [MainNumber], [SearchNumber] ");
            builder.Append("INSERT INTO [ProductOriginalNumber] (ProductID, OriginalNumberID, IsMainOriginalNumber, Updated) ");
            builder.Append("SELECT [Product].ID, [OriginalNumber].ID, [TempOriginalNumber].IsMainNumber, GETUTCDATE() ");
            builder.Append("FROM #TempOriginalNumber AS [TempOriginalNumber] ");
            builder.Append("LEFT JOIN [Product] ");
            if (forAmg)
                builder.Append("ON [Product].SourceAmgCode = [TempOriginalNumber].ProductCode ");
            else
                builder.Append("ON [Product].SourceFenixCode = [TempOriginalNumber].ProductCode ");

            builder.Append("LEFT JOIN [OriginalNumber] ");
            builder.Append("ON [OriginalNumber].MainNumber = [TempOriginalNumber].MainNumber ");
            builder.Append("AND [OriginalNumber].Number = [TempOriginalNumber].SearchNumber ");
            builder.Append("WHERE [Product].ID IS NOT NULL ");
            builder.Append("AND [OriginalNumber].ID IS NOT NULL ");

            builder.Append("DROP TABLE #TempOriginalNumber; ");

            builder.Append("UPDATE [Product] SET MainOriginalNumber = [OriginalNumber].MainNumber ");
            builder.Append("FROM [Product] ");
            builder.Append("LEFT JOIN [ProductOriginalNumber] ");
            builder.Append("ON [ProductOriginalNumber].ProductID = [Product].ID ");
            builder.Append("AND [ProductOriginalNumber].Deleted = 0 ");
            builder.Append("AND [ProductOriginalNumber].IsMainOriginalNumber = 1 ");
            builder.Append("LEFT JOIN [OriginalNumber] ");
            builder.Append("ON [OriginalNumber].ID = [ProductOriginalNumber].OriginalNumberID ");
            builder.Append("WHERE [Product].Deleted = 0 ");
            builder.Append("AND [OriginalNumber].ID IS NOT NULL ");

            builder.Append("EXEC sp_updatestats ");

            productsSyncRepository.ExecuteQuery(builder.ToString());

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.ORIGINAL_NUMBERS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRICINGS_SYNC_START]));

            IEnumerable<Currency> currencies =
                productsSyncRepository.GetAllCurrencies();

            IEnumerable<PriceType> priceTypes =
                productsSyncRepository.GetAllPriceTypes();

            IEnumerable<SyncPricing> syncPricings =
                forAmg ? productsSyncRepository.GetAmgAllSyncPricings() : productsSyncRepository.GetAllSyncPricings();

            PriceType assigned = priceTypes.First(x => x.Name == PriceTypeConst.ASSIGNED);

            PriceType calculated = priceTypes.First(x => x.Name == PriceTypeConst.CALCULATED);

            List<Pricing> pricings =
                productsSyncRepository.GetAllPricings();

            foreach (SyncPricing syncPricing in syncPricings
                         .OrderByDescending(s => string.IsNullOrEmpty(s.BaseName))) {
                Pricing pricing = pricings.FirstOrDefault(x => x.Name == syncPricing.Name);

                if (pricing == null)
                    pricing = new Pricing {
                        Name = syncPricing.Name,
                        CurrencyId = currencies.First(c => c.Code.Equals("EUR")).Id,
                        ForVat = syncPricing.ForVat,
                        Culture = syncPricing.Name.StartsWith("�L") || syncPricing.Name.StartsWith("PL") ? "pl" : "uk",
                        ExtraCharge = Convert.ToDouble(syncPricing.Discount)
                    };

                if (!string.IsNullOrEmpty(syncPricing.BaseName)) {
                    if (pricings.All(p => p.Name != syncPricing.BaseName)) continue;

                    Pricing basePricing = pricings.First(p => p.Name == syncPricing.BaseName);
                    pricing.PriceTypeId = calculated.Id;
                    pricing.BasePricingId = basePricing.Id;

                    if (syncPricing.Discount > 0)
                        if (basePricing.CalculatedExtraCharge.Equals(decimal.Zero))
                            pricing.CalculatedExtraCharge = syncPricing.Discount;
                        else
                            pricing.CalculatedExtraCharge =
                                decimal.Round(
                                    basePricing.CalculatedExtraCharge + syncPricing.Discount +
                                    basePricing.CalculatedExtraCharge * syncPricing.Discount / 100,
                                    4
                                );
                    else
                        pricing.CalculatedExtraCharge = decimal.Zero;
                } else {
                    pricing.PriceTypeId = assigned.Id;
                    pricing.CalculatedExtraCharge = syncPricing.Discount;
                }

                if (pricing.IsNew()) {
                    pricing.Id = productsSyncRepository.Add(pricing);

                    productsSyncRepository.Add(new PricingTranslation {
                        Name = syncPricing.Name,
                        PricingId = pricing.Id,
                        CultureCode = "uk"
                    });

                    productsSyncRepository.Add(new PricingTranslation {
                        Name = syncPricing.Name,
                        PricingId = pricing.Id,
                        CultureCode = "pl"
                    });
                } else {
                    pricing.Deleted = false;

                    productsSyncRepository.Update(pricing);
                }

                if (pricings.All(x => x.Name != pricing.Name))
                    pricings.Add(pricing);
            }

            if (pricings.All(p => p.Name != SUPPLY_PRICING_NAME)) {
                Pricing pricing = new() {
                    Name = SUPPLY_PRICING_NAME,
                    CurrencyId = currencies.First(c => c.Code.Equals("EUR")).Id,
                    ForVat = false,
                    ExtraCharge = 0
                };

                pricing.Id = productsSyncRepository.Add(pricing);

                productsSyncRepository.Add(new PricingTranslation {
                    Name = SUPPLY_PRICING_NAME,
                    PricingId = pricing.Id,
                    CultureCode = "uk"
                });

                productsSyncRepository.Add(new PricingTranslation {
                    Name = SUPPLY_PRICING_NAME,
                    PricingId = pricing.Id,
                    CultureCode = "pl"
                });

                pricings.Add(pricing);
            }

            productsSyncRepository.SetSharesPricings();

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRICINGS_SYNC_END]));

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_PRICES_SYNC_START]));

            IEnumerable<SyncProductPrice> syncProductPrices =
                forAmg ? productsSyncRepository.GetAmgAllSyncProductPrices() : productsSyncRepository.GetAllSyncProductPrices();

            List<ProductPricing> productPricings =
                syncProductPrices
                    .Where(p => pricings.Any(pr => pr.Name == p.PricingName))
                    .Select(price => {
                        return new ProductPricing {
                            PricingId = pricings.First(p => p.Name == price.PricingName).Id,
                            ProductId = price.ProductCode,
                            Price = price.Price
                        };
                    }).ToList();

            builder = new StringBuilder();

            builder.Append("UPDATE [ProductPricing] SET Deleted = 1 WHERE Deleted = 0;");

            builder.Append("CREATE TABLE #TempProductPrice (PricingId bigint, ProductCode bigint, Price money);");

            for (int i = 0; i < productPricings.Count; i++) {
                if (i.Equals(0) || i % 100 == 0) {
                    builder.Append("INSERT INTO #TempProductPrice ([PricingId], [ProductCode], [Price]) ");
                    builder.Append("VALUES ");
                } else {
                    builder.Append(",");
                }

                builder.Append($"({productPricings[i].PricingId},{productPricings[i].ProductId},{productPricings[i].Price.ToString(CultureInfo.InvariantCulture)})");
            }

            builder.Append("INSERT INTO [ProductPricing] ([ProductID], [PricingID], [Price], [Updated]) ");

            builder.Append("SELECT [Product].ID, [TempProductPrice].PricingId, [TempProductPrice].Price, GETUTCDATE() ");
            builder.Append("FROM #TempProductPrice [TempProductPrice] ");
            builder.Append("LEFT JOIN [Product] ");
            if (forAmg)
                builder.Append("ON [Product].SourceAmgCode = [TempProductPrice].ProductCode ");
            else
                builder.Append("ON [Product].SourceFenixCode = [TempProductPrice].ProductCode ");

            builder.Append("WHERE [Product].ID IS NOT NULL ");
            builder.Append("EXEC sp_updatestats ");

            productsSyncRepository.ExecuteQuery(builder.ToString());

            hubSenderActorRef.Tell(new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.PRODUCT_PRICES_SYNC_END],
                true));
        } catch (Exception exc) {
            hubSenderActorRef.Tell(
                new PushDataSyncNotificationMessage(DateTimeHelper.GetCurrentDateInString() + _localizer[SharedResourceNames.SYNC_ERROR], true, true));

            ActorReferenceManager
                .Instance
                .Get(BaseActorNames.LOG_MANAGER_ACTOR)
                .Tell(
                    new AddDataSyncLogMessage(
                        "SYNC_ERROR Products",
                        $"{currentUser?.LastName ?? string.Empty} {currentUser?.FirstName ?? string.Empty}",
                        JsonConvert.SerializeObject(new {
                            exc.Message,
                            exc.StackTrace
                        })
                    )
                );
        }
    }
}
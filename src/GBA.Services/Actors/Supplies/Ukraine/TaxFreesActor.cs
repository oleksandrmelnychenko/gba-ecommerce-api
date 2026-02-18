using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Supplies;
using GBA.Domain.Messages.Supplies.Ukraine.TaxFrees;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;
using GBA.Domain.Repositories.Users.Contracts;

namespace GBA.Services.Actors.Supplies.Ukraine;

public sealed class TaxFreesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;
    private readonly IUserRepositoriesFactory _userRepositoriesFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public TaxFreesActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IUserRepositoriesFactory userRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _userRepositoriesFactory = userRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;

        Receive<GetAllTaxFreesFilteredMessage>(message => {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewTaxFreeRepository(connection, exchangeRateConnection)
                    .GetAllFiltered(
                        message.From,
                        message.To,
                        message.Limit,
                        message.Offset,
                        message.Value,
                        message.Status,
                        message.StathamNetId
                    )
            );
        });

        Receive<GetTaxFreeByNetIdMessage>(message => {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyUkraineRepositoriesFactory
                    .NewTaxFreeRepository(connection, exchangeRateConnection)
                    .GetByNetId(
                        message.NetId
                    )
            );
        });

        Receive<GetTaxFreeDocumentForPrintingByNetIdMessage>(message => {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);

            TaxFree taxFree = taxFreeRepository.GetByNetIdWithPackList(message.NetId);

            if (taxFree != null) {
                taxFree.TaxFreeStatus = TaxFreeStatus.Printed;
                taxFree.DateOfPrint = DateTime.UtcNow;

                taxFreeRepository.Update(taxFree);

                if (taxFree.TaxFreePackList.IsFromSale) {
                    taxFree = taxFreeRepository.GetByNetIdFromSaleForPrinting(message.NetId);

                    (string xlsxFile, string pdfFile) =
                        _xlsFactoryManager
                            .NewTaxFreeAndSadXlsManager()
                            .ExportTaxFreeToXlsx(message.FolderPath, taxFree, true);

                    Sender.Tell(
                        new GenerateTaxFreePrintingDocumentResponse(
                            xlsxFile,
                            pdfFile,
                            taxFreeRepository
                                .GetById(
                                    taxFree.Id
                                )
                        )
                    );
                } else {
                    taxFree = taxFreeRepository.GetByNetIdForPrinting(message.NetId);

                    (string xlsxFile, string pdfFile) =
                        _xlsFactoryManager
                            .NewTaxFreeAndSadXlsManager()
                            .ExportTaxFreeToXlsx(message.FolderPath, taxFree);

                    Sender.Tell(
                        new GenerateTaxFreePrintingDocumentResponse(
                            xlsxFile,
                            pdfFile,
                            taxFreeRepository
                                .GetById(
                                    taxFree.Id
                                )
                        )
                    );
                }
            } else {
                Sender.Tell(new GenerateTaxFreePrintingDocumentResponse(string.Empty, string.Empty, taxFree: null));
            }
        });

        Receive<GetTaxFreeDocumentForPrintingByNetIdsMessage>(message => {
            using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);

            List<TaxFree> taxFrees = taxFreeRepository.GetByNetIdsWithPackList(message.NetIds);

            if (!taxFrees.Any())
                Sender.Tell(new GenerateTaxFreePrintingDocumentResponse(string.Empty, string.Empty, taxFrees));
            else
                for (int i = 0; i < taxFrees.Count; i++)
                    if (taxFrees[i].TaxFreePackList.IsFromSale)
                        taxFrees[i] = taxFreeRepository.GetByNetIdFromSaleForPrinting(taxFrees[i].NetUid);
                    else
                        taxFrees[i] = taxFreeRepository.GetByNetIdForPrinting(taxFrees[i].NetUid);

            (string xlsxFile, string pdfFile) =
                _xlsFactoryManager
                    .NewTaxFreeAndSadXlsManager()
                    .ExportTaxFreesToXlsx(message.FolderPath, taxFrees);

            Sender.Tell(
                new GenerateTaxFreePrintingDocumentResponse(
                    xlsxFile,
                    pdfFile,
                    taxFreeRepository
                        .GetByNetIds(
                            message.NetIds
                        )
                )
            );
        });

        Receive<UploadTaxFreeDocumentsByTaxFreeNetIdMessage>(message => {
            try {
                using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);

                TaxFree taxFree = taxFreeRepository.GetByNetId(message.NetId);

                if (taxFree == null) throw new Exception("TaxFree with provided NetId does not exists in database");

                _supplyUkraineRepositoriesFactory
                    .NewTaxFreeDocumentRepository(connection)
                    .Add(
                        message
                            .TaxFreeDocuments
                            .Select(document => {
                                document.TaxFreeId = taxFree.Id;

                                return document;
                            })
                    );

                Sender.Tell(
                    taxFreeRepository
                        .GetByNetId(
                            message.NetId
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });

        Receive<RemoveTaxFreeDocumentByNetIdMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            _supplyUkraineRepositoriesFactory
                .NewTaxFreeDocumentRepository(connection)
                .Remove(message.NetId);
        });

        Receive<UpdateTaxFreeMessage>(message => {
            try {
                using IDbConnection exchangeRateConnection = _connectionFactory.NewSqlConnection();
                using IDbConnection connection = _connectionFactory.NewSqlConnection();
                if (message.TaxFree == null) throw new Exception("Tax free entity cannot be null");
                if (message.TaxFree.IsNew()) throw new Exception("New Tax free entity is not valid payload");

                User user = _userRepositoriesFactory.NewUserRepository(connection).GetByNetIdWithoutIncludes(message.UserNetId);

                ITaxFreeRepository taxFreeRepository = _supplyUkraineRepositoriesFactory.NewTaxFreeRepository(connection, exchangeRateConnection);

                if (message.TaxFree.Statham != null && !message.TaxFree.Statham.IsNew())
                    message.TaxFree.StathamId = message.TaxFree.Statham.Id;
                else
                    message.TaxFree.StathamId = null;
                if (message.TaxFree.StathamCar != null && !message.TaxFree.StathamCar.IsNew())
                    message.TaxFree.StathamCarId = message.TaxFree.StathamCar.Id;
                else
                    message.TaxFree.StathamCarId = null;
                if (message.TaxFree.StathamPassport != null && !message.TaxFree.StathamPassport.IsNew())
                    message.TaxFree.StathamPassportId = message.TaxFree.StathamPassport.Id;
                else
                    message.TaxFree.StathamPassportId = null;

                TaxFree taxFreeFromDb = taxFreeRepository.GetById(message.TaxFree.Id);

                if (taxFreeFromDb != null)
                    if (!taxFreeFromDb.AmountPayedStatham.Equals(message.TaxFree.AmountPayedStatham))
                        message.TaxFree.DateOfStathamPayment = DateTime.Now.Date;

                message.TaxFree.DateOfPrint =
                    message.TaxFree.DateOfPrint.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.DateOfPrint.Value)
                        : message.TaxFree.DateOfPrint;

                message.TaxFree.DateOfIssue =
                    message.TaxFree.DateOfIssue.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.DateOfIssue.Value)
                        : message.TaxFree.DateOfIssue;

                message.TaxFree.DateOfStathamPayment =
                    message.TaxFree.DateOfStathamPayment.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.DateOfStathamPayment.Value)
                        : message.TaxFree.DateOfStathamPayment;

                message.TaxFree.DateOfTabulation =
                    message.TaxFree.DateOfTabulation.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.DateOfTabulation.Value)
                        : message.TaxFree.DateOfTabulation;

                message.TaxFree.FormedDate =
                    message.TaxFree.FormedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.FormedDate.Value)
                        : message.TaxFree.FormedDate;

                message.TaxFree.SelectedDate =
                    message.TaxFree.SelectedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.SelectedDate.Value)
                        : message.TaxFree.SelectedDate;

                message.TaxFree.ReturnedDate =
                    message.TaxFree.ReturnedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.ReturnedDate.Value)
                        : message.TaxFree.ReturnedDate;

                message.TaxFree.ClosedDate =
                    message.TaxFree.ClosedDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.ClosedDate.Value)
                        : message.TaxFree.ClosedDate;

                message.TaxFree.CanceledDate =
                    message.TaxFree.CanceledDate.HasValue
                        ? TimeZoneInfo.ConvertTimeToUtc(message.TaxFree.CanceledDate.Value)
                        : message.TaxFree.CanceledDate;

                if (!message.TaxFree.TaxFreeStatus.Equals(taxFreeFromDb.TaxFreeStatus))
                    switch (message.TaxFree.TaxFreeStatus) {
                        case TaxFreeStatus.Formed:
                            message.TaxFree.FormedDate = message.TaxFree.FormedDate ?? DateTime.UtcNow;
                            break;
                        case TaxFreeStatus.Printed:
                            message.TaxFree.DateOfPrint = message.TaxFree.DateOfPrint ?? DateTime.UtcNow;
                            break;
                        case TaxFreeStatus.Tabulated:
                            message.TaxFree.DateOfTabulation = message.TaxFree.DateOfTabulation ?? DateTime.UtcNow;
                            break;
                        case TaxFreeStatus.Returned:
                            message.TaxFree.ReturnedDate = message.TaxFree.ReturnedDate ?? DateTime.UtcNow;
                            break;
                        case TaxFreeStatus.Closed:
                            message.TaxFree.ClosedDate = message.TaxFree.ClosedDate ?? DateTime.UtcNow;
                            break;
                        case TaxFreeStatus.NotFormed:
                        default:
                            break;
                    }

                message.TaxFree.ResponsibleId = user.Id;

                taxFreeRepository.Update(message.TaxFree);

                Sender.Tell(
                    taxFreeRepository
                        .GetById(
                            message.TaxFree.Id
                        )
                );
            } catch (Exception exc) {
                Sender.Tell(exc);
            }
        });
    }
}
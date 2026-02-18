using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.XmlDocumentModels;
using GBA.Domain.Messages.XmlDocuments;
using GBA.Domain.Repositories.XmlDocuments.Contracts;
using GBA.Domain.XmlDocumentManagement.Contracts;

namespace GBA.Services.Actors.XmlDocuments;

public sealed class XmlDocumentActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IXmlDocumentRepositoriesFactory _xmlDocumentRepositoriesFactory;
    private readonly IXmlManager _xmlManager;

    public XmlDocumentActor(
        IXmlManager xmlManager,
        IDbConnectionFactory connectionFactory,
        IXmlDocumentRepositoriesFactory xmlDocumentRepositoriesFactory) {
        _xmlManager = xmlManager;
        _connectionFactory = connectionFactory;
        _xmlDocumentRepositoriesFactory = xmlDocumentRepositoriesFactory;

        Receive<GetNewXmlDocumentMessage>(ProcessGenerateNewXmlDocuments);
    }

    private void ProcessGenerateNewXmlDocuments(GetNewXmlDocumentMessage message) {
        using IDbConnection additionalConnection = _connectionFactory.NewSqlConnection();
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        try {
            XDocument xmlDocument;

            int quantityDownloadDocuments;

            if (message.TypeOfXmlDocument == TypeOfXmlDocument.Sales) {
                List<Sale> sales =
                    _xmlDocumentRepositoriesFactory
                        .NewXmlDocumentRepository(connection, additionalConnection)
                        .GetSalesXmlDocumentByDate(message.From, message.To);

                quantityDownloadDocuments = sales.Count;

                xmlDocument = _xmlManager.GetSalesXmlDocuments(message.PathToFolder, sales);
            } else {
                ProductIncomesModel productIncomesModel =
                    _xmlDocumentRepositoriesFactory
                        .NewXmlDocumentRepository(connection, additionalConnection)
                        .GetProductIncomesXmlDocumentByDate(message.From, message.To);

                quantityDownloadDocuments = productIncomesModel.SupplyOrders.Count + productIncomesModel.SupplyOrderUkraines.Count;

                xmlDocument = _xmlManager.GetProductIncomeDocument(message.PathToFolder, productIncomesModel);
            }

            if (xmlDocument == null)
                throw new Exception(XmlDocumentResourceNames.DOWNLOAD_FAIL);

            Sender.Tell(quantityDownloadDocuments);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}
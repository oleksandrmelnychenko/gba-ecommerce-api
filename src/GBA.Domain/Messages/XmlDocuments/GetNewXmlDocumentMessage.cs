using System;
using GBA.Domain.EntityHelpers.XmlDocumentModels;

namespace GBA.Domain.Messages.XmlDocuments;

public sealed class GetNewXmlDocumentMessage {
    public GetNewXmlDocumentMessage(
        string pathToFolder,
        TypeOfXmlDocument typeOfXmlDocument,
        DateTime from,
        DateTime to) {
        PathToFolder = pathToFolder;
        TypeOfXmlDocument = typeOfXmlDocument;
        From = from;
        To = to.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public string PathToFolder { get; }

    public TypeOfXmlDocument TypeOfXmlDocument { get; }

    public DateTime From { get; }

    public DateTime To { get; }
}
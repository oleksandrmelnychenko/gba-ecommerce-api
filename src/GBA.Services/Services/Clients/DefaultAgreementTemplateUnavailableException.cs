using System;

namespace GBA.Services.Services.Clients;

public sealed class DefaultAgreementTemplateUnavailableException(string message)
    : InvalidOperationException(message);

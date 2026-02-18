using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients.PerfectClients;
using GBA.Domain.Messages.Clients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Services.Actors.Clients;

public sealed class PerfectClientsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public PerfectClientsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<AddPerfectClientMessage>(ProcessAddPerfectClientMessage);

        Receive<SaveAllPerfectClientsMessage>(ProcessSaveAllPerfectClientsMessage);

        Receive<UpdatePerfectClientMessage>(ProcessUpdatePerfectClientMessage);

        Receive<DeletePerfectClientMessage>(ProcessDeletePerfectClientMessage);
    }

    private void ProcessAddPerfectClientMessage(AddPerfectClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPerfectClientRepository perfectClientRepository = _clientRepositoriesFactory.NewPerfectClientRepository(connection);

        long perfectClientId = perfectClientRepository.Add(message.PerfectClient);

        if (message.PerfectClient.PerfectClientTranslations.Any()) {
            bool first = true;

            foreach (PerfectClientTranslation translation in message.PerfectClient.PerfectClientTranslations) {
                if (string.IsNullOrEmpty(translation.CultureCode)) {
                    if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                        if (first) {
                            translation.CultureCode = "pl";

                            first = false;
                        } else {
                            translation.CultureCode = "uk";
                        }
                    } else {
                        if (first) {
                            translation.CultureCode = "uk";

                            first = false;
                        } else {
                            translation.CultureCode = "pl";
                        }
                    }
                }

                translation.PerfectClientId = perfectClientId;
            }

            _clientRepositoriesFactory.NewPerfectClientTranslationRepository(connection).Add(message.PerfectClient.PerfectClientTranslations);
        }

        if (message.PerfectClient.Type.Equals(PerfectClientType.Toggle))
            foreach (PerfectClientValue value in message.PerfectClient.Values) {
                value.PerfectClientId = perfectClientId;

                long valueId = _clientRepositoriesFactory.NewPerfectClientValueRepository(connection).Add(value);

                bool first = true;

                if (!value.PerfectClientValueTranslations.Any()) continue;

                foreach (PerfectClientValueTranslation translation in value.PerfectClientValueTranslations) {
                    if (string.IsNullOrEmpty(translation.CultureCode)) {
                        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                            if (first) {
                                translation.CultureCode = "pl";

                                first = false;
                            } else {
                                translation.CultureCode = "uk";
                            }
                        } else {
                            if (first) {
                                translation.CultureCode = "uk";

                                first = false;
                            } else {
                                translation.CultureCode = "pl";
                            }
                        }
                    }

                    translation.PerfectClientValueId = valueId;
                }

                _clientRepositoriesFactory.NewPerfectClientValueTranslationRepository(connection).Add(value.PerfectClientValueTranslations);
            }

        Sender.Tell(perfectClientRepository.GetById(perfectClientId));
    }

    private void ProcessSaveAllPerfectClientsMessage(SaveAllPerfectClientsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPerfectClientRepository perfectClientRepository = _clientRepositoriesFactory.NewPerfectClientRepository(connection);
        IPerfectClientValueRepository perfectClientValueRepository = _clientRepositoriesFactory.NewPerfectClientValueRepository(connection);

        foreach (PerfectClient client in message.PerfectClients)
            if (client.IsNew()) {
                long clientId = perfectClientRepository.Add(client);

                if (!client.Type.Equals(PerfectClientType.Toggle)) continue;

                foreach (PerfectClientValue value in client.Values)
                    if (value.IsNew()) {
                        value.PerfectClientId = clientId;

                        perfectClientValueRepository.Add(value);
                    } else {
                        perfectClientValueRepository.Update(value);
                    }
            } else {
                perfectClientRepository.Update(client);

                if (!client.Type.Equals(PerfectClientType.Toggle)) continue;

                foreach (PerfectClientValue value in client.Values)
                    if (value.IsNew()) {
                        value.PerfectClientId = client.Id;

                        perfectClientValueRepository.Add(value);
                    } else {
                        perfectClientValueRepository.Update(value);
                    }
            }

        Sender.Tell(string.Empty);
    }

    private void ProcessUpdatePerfectClientMessage(UpdatePerfectClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPerfectClientRepository perfectClientRepository = _clientRepositoriesFactory.NewPerfectClientRepository(connection);
        IPerfectClientValueRepository perfectClientValueRepository = _clientRepositoriesFactory.NewPerfectClientValueRepository(connection);

        perfectClientRepository.Update(message.PerfectClient);

        if (message.PerfectClient.PerfectClientTranslations.Any()) {
            IPerfectClientTranslationRepository perfectClientTranslationRepository = _clientRepositoriesFactory.NewPerfectClientTranslationRepository(connection);

            bool first = true;

            foreach (PerfectClientTranslation translation in message.PerfectClient.PerfectClientTranslations) {
                if (string.IsNullOrEmpty(translation.CultureCode)) {
                    if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                        if (first) {
                            translation.CultureCode = "pl";

                            first = false;
                        } else {
                            translation.CultureCode = "uk";
                        }
                    } else {
                        if (first) {
                            translation.CultureCode = "uk";

                            first = false;
                        } else {
                            translation.CultureCode = "pl";
                        }
                    }
                }

                if (translation.IsNew()) {
                    translation.PerfectClientId = message.PerfectClient.Id;

                    perfectClientTranslationRepository.Add(translation);
                } else {
                    perfectClientTranslationRepository.Update(translation);
                }
            }
        }

        if (message.PerfectClient.Type.Equals(PerfectClientType.Toggle)) {
            IPerfectClientValueTranslationRepository perfectClientValueTranslationRepository =
                _clientRepositoriesFactory.NewPerfectClientValueTranslationRepository(connection);

            foreach (PerfectClientValue value in message.PerfectClient.Values)
                if (value.IsNew()) {
                    value.PerfectClientId = message.PerfectClient.Id;

                    long valueId = perfectClientValueRepository.Add(value);

                    if (!value.PerfectClientValueTranslations.Any()) continue;

                    bool first = true;

                    foreach (PerfectClientValueTranslation translation in value.PerfectClientValueTranslations) {
                        if (string.IsNullOrEmpty(translation.CultureCode)) {
                            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                                if (first) {
                                    translation.CultureCode = "pl";

                                    first = false;
                                } else {
                                    translation.CultureCode = "uk";
                                }
                            } else {
                                if (first) {
                                    translation.CultureCode = "uk";

                                    first = false;
                                } else {
                                    translation.CultureCode = "pl";
                                }
                            }
                        }

                        if (translation.IsNew()) {
                            translation.PerfectClientValueId = valueId;

                            perfectClientValueTranslationRepository.Add(translation);
                        } else {
                            perfectClientValueTranslationRepository.Update(translation);
                        }
                    }
                } else {
                    perfectClientValueRepository.Update(value);

                    if (!value.PerfectClientValueTranslations.Any()) continue;

                    bool first = true;

                    foreach (PerfectClientValueTranslation translation in value.PerfectClientValueTranslations) {
                        if (string.IsNullOrEmpty(translation.CultureCode)) {
                            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                                if (first) {
                                    translation.CultureCode = "pl";

                                    first = false;
                                } else {
                                    translation.CultureCode = "uk";
                                }
                            } else {
                                if (first) {
                                    translation.CultureCode = "uk";

                                    first = false;
                                } else {
                                    translation.CultureCode = "pl";
                                }
                            }
                        }

                        if (translation.IsNew()) {
                            translation.PerfectClientValueId = value.Id;

                            perfectClientValueTranslationRepository.Add(translation);
                        } else {
                            perfectClientValueTranslationRepository.Update(translation);
                        }
                    }
                }
        }

        Sender.Tell(perfectClientRepository.GetByNetId(message.PerfectClient.NetUid));
    }

    private void ProcessDeletePerfectClientMessage(DeletePerfectClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IPerfectClientRepository perfectClientRepository = _clientRepositoriesFactory.NewPerfectClientRepository(connection);

        PerfectClient perfectClient = perfectClientRepository.GetByNetId(message.NetId);

        if (perfectClient != null) {
            if (!perfectClientRepository.IsAssingedToAnyClient(perfectClient.Id)) {
                perfectClientRepository.Remove(message.NetId);

                Sender.Tell(new Tuple<string, PerfectClient>(string.Empty, perfectClient));
            } else {
                Sender.Tell(new Tuple<string, PerfectClient>(PerfectClientResourceNames.ASSIGNED_TO_CLIENT, null));
            }
        } else {
            Sender.Tell(new Tuple<string, PerfectClient>(string.Empty, null));
        }
    }
}
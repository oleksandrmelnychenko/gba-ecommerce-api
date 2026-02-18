using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Clients.PerfectClients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class PerfectClientRepository : IPerfectClientRepository {
    private readonly IDbConnection _connection;

    public PerfectClientRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PerfectClient perfectClient) {
        return _connection.Query<long>(
                "INSERT INTO PerfectClient (Lable, Value, Type, IsSelected, ClientTypeRoleId, Updated) " +
                "VALUES (@Lable, @Value, @Type, @IsSelected, @ClientTypeRoleId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                perfectClient
            )
            .Single();
    }

    public void Update(PerfectClient perfectClient) {
        _connection.Execute(
            "UPDATE PerfectClient SET " +
            "Lable = @Lable, Value = @Value, Type = @Type, IsSelected = @IsSelected, ClientTypeRoleId = @ClientTypeRoleId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            perfectClient
        );
    }

    public PerfectClient GetById(long id) {
        PerfectClient perfectClientToreturn = null;

        _connection
            .Query<PerfectClient, PerfectClientTranslation, PerfectClientTranslation, PerfectClientValue, PerfectClientValueTranslation, PerfectClientValueTranslation,
                PerfectClient>(
                "SELECT * FROM PerfectClient " +
                "LEFT JOIN PerfectClientTranslation AS CurrentTranslation " +
                "ON PerfectClient.ID = CurrentTranslation.PerfectClientID " +
                "AND CurrentTranslation.CultureCode = @Culture " +
                "AND CurrentTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientTranslation " +
                "ON PerfectClient.ID = PerfectClientTranslation.PerfectClientID " +
                "AND PerfectClientTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN PerfectClientValue " +
                "ON PerfectClient.ID = PerfectClientValue.PerfectClientID " +
                "AND PerfectClientValue.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation AS CurrentValueTranslation " +
                "ON PerfectClientValue.ID = CurrentValueTranslation.PerfectClientValueID " +
                "AND CurrentValueTranslation.CultureCode = @Culture " +
                "AND CurrentValueTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation " +
                "ON PerfectClientValue.ID = PerfectClientValueTranslation.PerfectClientValueID " +
                "AND PerfectClientValueTranslation.Deleted = 0 " +
                "WHERE PerfectClient.ID = @Id",
                (client, currentClientTranslation, translation, value, currentValueTranslation, valueTranslation) => {
                    if (currentClientTranslation != null) {
                        client.Lable = currentClientTranslation?.Name;
                        client.Description = currentClientTranslation?.Description;
                    }

                    if (value != null) {
                        if (currentValueTranslation != null)
                            value.Value = currentValueTranslation?.Value;

                        if (perfectClientToreturn != null) {
                            if (!perfectClientToreturn.Values.Any(v => v.Id.Equals(value.Id))) perfectClientToreturn.Values.Add(value);

                            if (translation != null)
                                if (!perfectClientToreturn.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    perfectClientToreturn.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!perfectClientToreturn.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    perfectClientToreturn.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            value.IsSelected = true;

                            client.Values.Add(value);

                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            perfectClientToreturn = client;
                        }
                    } else {
                        if (perfectClientToreturn != null) {
                            if (translation != null)
                                if (!perfectClientToreturn.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    perfectClientToreturn.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!perfectClientToreturn.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    perfectClientToreturn.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            perfectClientToreturn = client;
                        }
                    }

                    return client;
                },
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            perfectClientToreturn.PerfectClientTranslations = perfectClientToreturn.PerfectClientTranslations.OrderBy(t => t.CultureCode).ToArray();

            if (perfectClientToreturn.Values.Any())
                foreach (PerfectClientValue value in perfectClientToreturn.Values)
                    value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderBy(t => t.CultureCode).ToArray();
        } else {
            perfectClientToreturn.PerfectClientTranslations = perfectClientToreturn.PerfectClientTranslations.OrderByDescending(t => t.CultureCode).ToArray();

            if (perfectClientToreturn.Values.Any())
                foreach (PerfectClientValue value in perfectClientToreturn.Values)
                    value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderByDescending(t => t.CultureCode).ToArray();
        }

        return perfectClientToreturn;
    }

    public PerfectClient GetByNetId(Guid netId) {
        PerfectClient perfectClientToreturn = null;

        _connection
            .Query<PerfectClient, PerfectClientTranslation, PerfectClientTranslation, PerfectClientValue, PerfectClientValueTranslation, PerfectClientValueTranslation,
                PerfectClient>(
                "SELECT * FROM PerfectClient " +
                "LEFT JOIN PerfectClientTranslation AS CurrentTranslation " +
                "ON PerfectClient.ID = CurrentTranslation.PerfectClientID " +
                "AND CurrentTranslation.CultureCode = @Culture " +
                "AND CurrentTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientTranslation " +
                "ON PerfectClient.ID = PerfectClientTranslation.PerfectClientID " +
                "AND PerfectClientTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN PerfectClientValue " +
                "ON PerfectClient.ID = PerfectClientValue.PerfectClientID " +
                "AND PerfectClientValue.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation AS CurrentValueTranslation " +
                "ON PerfectClientValue.ID = CurrentValueTranslation.PerfectClientValueID " +
                "AND CurrentValueTranslation.CultureCode = @Culture " +
                "AND CurrentValueTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation " +
                "ON PerfectClientValue.ID = PerfectClientValueTranslation.PerfectClientValueID " +
                "AND PerfectClientValueTranslation.Deleted = 0 " +
                "WHERE PerfectClient.NetUID = @NetId",
                (client, currentClientTranslation, translation, value, currentValueTranslation, valueTranslation) => {
                    if (currentClientTranslation != null) {
                        client.Lable = currentClientTranslation?.Name;
                        client.Description = currentClientTranslation?.Description;
                    }

                    if (value != null) {
                        if (currentValueTranslation != null)
                            value.Value = currentValueTranslation?.Value;

                        if (perfectClientToreturn != null) {
                            if (!perfectClientToreturn.Values.Any(v => v.Id.Equals(value.Id))) perfectClientToreturn.Values.Add(value);

                            if (translation != null)
                                if (!perfectClientToreturn.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    perfectClientToreturn.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!perfectClientToreturn.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    perfectClientToreturn.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            value.IsSelected = true;

                            client.Values.Add(value);

                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            perfectClientToreturn = client;
                        }
                    } else {
                        if (perfectClientToreturn != null) {
                            if (translation != null)
                                if (!perfectClientToreturn.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    perfectClientToreturn.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!perfectClientToreturn.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    perfectClientToreturn.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            perfectClientToreturn = client;
                        }
                    }

                    return client;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, NetId = netId.ToString() }
            )
            .ToList();

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            perfectClientToreturn.PerfectClientTranslations = perfectClientToreturn.PerfectClientTranslations.OrderBy(t => t.CultureCode).ToArray();

            if (perfectClientToreturn.Values.Any())
                foreach (PerfectClientValue value in perfectClientToreturn.Values)
                    value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderBy(t => t.CultureCode).ToArray();
        } else {
            perfectClientToreturn.PerfectClientTranslations = perfectClientToreturn.PerfectClientTranslations.OrderByDescending(t => t.CultureCode).ToArray();

            if (perfectClientToreturn.Values.Any())
                foreach (PerfectClientValue value in perfectClientToreturn.Values)
                    value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderByDescending(t => t.CultureCode).ToArray();
        }

        return perfectClientToreturn;
    }

    public List<PerfectClient> GetAllByType(PerfectClientType type) {
        List<PerfectClient> clients = new();

        _connection
            .Query<PerfectClient, PerfectClientTranslation, PerfectClientTranslation, PerfectClientValue, PerfectClientValueTranslation, PerfectClientValueTranslation,
                PerfectClient>(
                "SELECT * FROM PerfectClient " +
                "LEFT JOIN PerfectClientTranslation AS CurrentTranslation " +
                "ON PerfectClient.ID = CurrentTranslation.PerfectClientID " +
                "AND CurrentTranslation.CultureCode = @Culture " +
                "AND CurrentTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientTranslation " +
                "ON PerfectClient.ID = PerfectClientTranslation.PerfectClientID " +
                "AND PerfectClientTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN PerfectClientValue " +
                "ON PerfectClient.ID = PerfectClientValue.PerfectClientID " +
                "AND PerfectClientValue.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation AS CurrentValueTranslation " +
                "ON PerfectClientValue.ID = CurrentValueTranslation.PerfectClientValueID " +
                "AND CurrentValueTranslation.CultureCode = @Culture " +
                "AND CurrentValueTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation " +
                "ON PerfectClientValue.ID = PerfectClientValueTranslation.PerfectClientValueID " +
                "AND PerfectClientValueTranslation.Deleted = 0 " +
                "WHERE PerfectClient.Deleted = 0 " +
                "AND PerfectClient.Type = @Type",
                (client, currentClientTranslation, translation, value, currentValueTranslation, valueTranslation) => {
                    if (currentClientTranslation != null) {
                        client.Lable = currentClientTranslation?.Name;
                        client.Description = currentClientTranslation?.Description;
                    }

                    if (value != null) {
                        if (currentValueTranslation != null)
                            value.Value = currentValueTranslation?.Value;

                        if (clients.Any(c => c.Id.Equals(client.Id))) {
                            if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v => v.Id.Equals(value.Id))) clients.First(c => c.Id.Equals(client.Id)).Values.Add(value);

                            if (translation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v =>
                                        v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    clients.First(c => c.Id.Equals(client.Id)).Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            value.IsSelected = true;

                            client.Values.Add(value);

                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            clients.Add(client);
                        }
                    } else {
                        if (clients.Any(c => c.Id.Equals(client.Id))) {
                            if (translation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v =>
                                        v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    clients.First(c => c.Id.Equals(client.Id)).Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            clients.Add(client);
                        }
                    }

                    return client;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Type = type }
            )
            .ToList();

        clients.ForEach(client => {
            if (client.Values.Any())
                foreach (PerfectClientValue value in client.Values)
                    if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                        value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderBy(t => t.CultureCode).ToArray();
                    else
                        value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderByDescending(t => t.CultureCode).ToArray();

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                client.PerfectClientTranslations = client.PerfectClientTranslations.OrderBy(t => t.CultureCode).ToArray();
            else
                client.PerfectClientTranslations = client.PerfectClientTranslations.OrderByDescending(t => t.CultureCode).ToArray();
        });

        return clients;
    }

    public List<PerfectClient> GetAll() {
        List<PerfectClient> clients = new();

        _connection
            .Query<PerfectClient, PerfectClientTranslation, PerfectClientTranslation, PerfectClientValue, PerfectClientValueTranslation, PerfectClientValueTranslation,
                PerfectClient>(
                "SELECT * FROM PerfectClient " +
                "LEFT JOIN PerfectClientTranslation AS CurrentTranslation " +
                "ON PerfectClient.ID = CurrentTranslation.PerfectClientID " +
                "AND CurrentTranslation.CultureCode = @Culture " +
                "AND CurrentTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientTranslation " +
                "ON PerfectClient.ID = PerfectClientTranslation.PerfectClientID " +
                "AND PerfectClientTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN PerfectClientValue " +
                "ON PerfectClient.ID = PerfectClientValue.PerfectClientID " +
                "AND PerfectClientValue.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation AS CurrentValueTranslation " +
                "ON PerfectClientValue.ID = CurrentValueTranslation.PerfectClientValueID " +
                "AND CurrentValueTranslation.CultureCode = @Culture " +
                "AND CurrentValueTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation " +
                "ON PerfectClientValue.ID = PerfectClientValueTranslation.PerfectClientValueID " +
                "AND PerfectClientValueTranslation.Deleted = 0 " +
                "WHERE PerfectClient.Deleted = 0",
                (client, currentClientTranslation, translation, value, currentValueTranslation, valueTranslation) => {
                    if (currentClientTranslation != null) {
                        client.Lable = currentClientTranslation?.Name;
                        client.Description = currentClientTranslation?.Description;
                    }

                    if (value != null) {
                        if (currentValueTranslation != null)
                            value.Value = currentValueTranslation?.Value;

                        if (clients.Any(c => c.Id.Equals(client.Id))) {
                            if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v => v.Id.Equals(value.Id))) clients.First(c => c.Id.Equals(client.Id)).Values.Add(value);

                            if (translation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v =>
                                        v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    clients.First(c => c.Id.Equals(client.Id)).Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            value.IsSelected = true;

                            client.Values.Add(value);

                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            clients.Add(client);
                        }
                    } else {
                        if (clients.Any(c => c.Id.Equals(client.Id))) {
                            if (translation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v =>
                                        v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    clients.First(c => c.Id.Equals(client.Id)).Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            clients.Add(client);
                        }
                    }

                    return client;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();

        clients.ForEach(client => {
            if (client.Values.Any())
                foreach (PerfectClientValue value in client.Values)
                    if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                        value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderBy(t => t.CultureCode).ToArray();
                    else
                        value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderByDescending(t => t.CultureCode).ToArray();

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                client.PerfectClientTranslations = client.PerfectClientTranslations.OrderBy(t => t.CultureCode).ToArray();
            else
                client.PerfectClientTranslations = client.PerfectClientTranslations.OrderByDescending(t => t.CultureCode).ToArray();
        });

        return clients;
    }

    public List<PerfectClient> GetAll(long roleId) {
        List<PerfectClient> clients = new();

        _connection
            .Query<PerfectClient, PerfectClientTranslation, PerfectClientTranslation, PerfectClientValue, PerfectClientValueTranslation, PerfectClientValueTranslation,
                PerfectClient>(
                "SELECT * FROM PerfectClient " +
                "LEFT JOIN PerfectClientTranslation AS CurrentTranslation " +
                "ON PerfectClient.ID = CurrentTranslation.PerfectClientID " +
                "AND CurrentTranslation.CultureCode = @Culture " +
                "AND CurrentTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientTranslation " +
                "ON PerfectClient.ID = PerfectClientTranslation.PerfectClientID " +
                "AND PerfectClientTranslation.Deleted = 0 " +
                "LEFT OUTER JOIN PerfectClientValue " +
                "ON PerfectClient.ID = PerfectClientValue.PerfectClientID " +
                "AND PerfectClientValue.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation AS CurrentValueTranslation " +
                "ON PerfectClientValue.ID = CurrentValueTranslation.PerfectClientValueID " +
                "AND CurrentValueTranslation.CultureCode = @Culture " +
                "AND CurrentValueTranslation.Deleted = 0 " +
                "LEFT JOIN PerfectClientValueTranslation " +
                "ON PerfectClientValue.ID = PerfectClientValueTranslation.PerfectClientValueID " +
                "AND PerfectClientValueTranslation.Deleted = 0 " +
                "WHERE PerfectClient.Deleted = 0" +
                "AND ClientTypeRoleID = @RoleId",
                (client, currentClientTranslation, translation, value, currentValueTranslation, valueTranslation) => {
                    if (currentClientTranslation != null) {
                        client.Lable = currentClientTranslation?.Name;
                        client.Description = currentClientTranslation?.Description;
                    }

                    if (value != null) {
                        if (currentValueTranslation != null)
                            value.Value = currentValueTranslation?.Value;

                        if (clients.Any(c => c.Id.Equals(client.Id))) {
                            if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v => v.Id.Equals(value.Id))) clients.First(c => c.Id.Equals(client.Id)).Values.Add(value);

                            if (translation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v =>
                                        v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    clients.First(c => c.Id.Equals(client.Id)).Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            value.IsSelected = true;

                            client.Values.Add(value);

                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            clients.Add(client);
                        }
                    } else {
                        if (clients.Any(c => c.Id.Equals(client.Id))) {
                            if (translation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    clients.First(c => c.Id.Equals(client.Id)).PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!clients.First(c => c.Id.Equals(client.Id)).Values.Any(v =>
                                        v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    clients.First(c => c.Id.Equals(client.Id)).Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);
                        } else {
                            if (translation != null)
                                if (!client.PerfectClientTranslations.Any(t => t.Id.Equals(translation.Id)))
                                    client.PerfectClientTranslations.Add(translation);

                            if (valueTranslation != null)
                                if (!client.Values.Any(v => v.Id.Equals(value.Id) && v.PerfectClientValueTranslations.Any(t => t.Id.Equals(valueTranslation.Id))))
                                    client.Values.First(v => v.Id.Equals(value.Id)).PerfectClientValueTranslations.Add(valueTranslation);

                            clients.Add(client);
                        }
                    }

                    return client;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, RoleId = roleId }
            )
            .ToList();

        clients.ForEach(client => {
            if (client.Values.Any())
                foreach (PerfectClientValue value in client.Values)
                    if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                        value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderBy(t => t.CultureCode).ToArray();
                    else
                        value.PerfectClientValueTranslations = value.PerfectClientValueTranslations.OrderByDescending(t => t.CultureCode).ToArray();

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl"))
                client.PerfectClientTranslations = client.PerfectClientTranslations.OrderBy(t => t.CultureCode).ToArray();
            else
                client.PerfectClientTranslations = client.PerfectClientTranslations.OrderByDescending(t => t.CultureCode).ToArray();
        });

        return clients;
    }

    public bool IsAssingedToAnyClient(long perfectClientId) {
        return _connection.Query<long>(
            "SELECT DISTINCT PerfectClient.ID FROM PerfectClient " +
            "LEFT JOIN ClientPerfectClient " +
            "ON ClientPerfectClient.PerfectClientID = PerfectClient.ID " +
            "LEFT JOIN Client " +
            "ON ClientPerfectClient.ClientID = Client.ID " +
            "WHERE PerfectClient.ID = @Id " +
            "AND PerfectClient.Deleted = 0 " +
            "AND ClientPerfectClient.Deleted = 0 " +
            "AND Client.Deleted = 0",
            new { Id = perfectClientId }
        ).ToArray().Any();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PerfectClient SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}
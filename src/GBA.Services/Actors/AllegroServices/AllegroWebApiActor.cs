using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Akka.Actor;
using AllegroWebApi;
using GBA.Common.Helpers;
using GBA.Domain.AllegroSellFormEntities;
using GBA.Domain.Messages.AllegroServices.Selling;
using GBA.Domain.Messages.AllegroServices.WebApi;

namespace GBA.Services.Actors.AllegroServices;

public sealed class AllegroWebApiActor : ReceiveActor {
    private servicePortClient _allegroWebApiClient;

    private long _localVersion;

    private doLoginResponse _sessionData;

    public AllegroWebApiActor() {
        ReceiveAsync<GetMySellingItemsMessage>(async message => {
            Sender.Tell(await GetMySellingItems());
        });

        ReceiveAsync<GetAllAllegroOriginalCategoriesMessage>(async message => {
            Sender.Tell(await GetAllCategories());
        });

        ReceiveAsync<GetSellFormFieldsMessage>(async message => {
            Sender.Tell(await GetSellFromFields(message.CategoryId));
        });

        ReceiveAsync<GetSellFormFieldsByCategoryIdMessage>(async message => {
            Sender.Tell(await GetSellFromFields(message.CategoryId));
        });

        ReceiveAsync<CheckNewSellingItemMessage>(async message => {
            Sender.Tell(await CheckNewSellingItem(message.NewSellingRequest));
        });

        ReceiveAsync<AddNewSellingItemMessage>(async message => {
            Sender.Tell(await AddNewAuction(message.NewSellingRequest));
        });
    }

    protected override async void PreStart() {
        try {
            // await InitializeWebApiClient();
        } catch (Exception) {
            // ignored
        }

        base.PreStart();
    }

    private async Task InitializeWebApiClient() {
        _allegroWebApiClient = new servicePortClient(servicePortClient.EndpointConfiguration.servicePort, new EndpointAddress(ConfigurationManager.AllegroBaseWebApiUrl));

        doQueryAllSysStatusResponse sysStatuses = await _allegroWebApiClient.doQueryAllSysStatusAsync(1, ConfigurationManager.AllegroApiKey);

        _localVersion = sysStatuses.sysCountryStatus[0].verKey;

        _sessionData = await _allegroWebApiClient.doLoginAsync(
            new doLoginRequest(
                ConfigurationManager.AllegroUserName,
                ConfigurationManager.AllegroPassword,
                1,
                ConfigurationManager.AllegroApiKey,
                _localVersion
            )
        );
    }

    private async Task<doGetMySellItemsResponse> GetMySellingItems() {
        try {
            return await _allegroWebApiClient.doGetMySellItemsAsync(
                new doGetMySellItemsRequest(
                    _sessionData.sessionHandlePart,
                    null,
                    null,
                    string.Empty,
                    0,
                    null,
                    100,
                    0
                )
            );
        } catch (Exception) {
            await InitializeWebApiClient();

            return await GetMySellingItems();
        }
    }

    private async Task<CatInfoType[]> GetAllCategories() {
        try {
            return (await _allegroWebApiClient.doGetCatsDataAsync(new doGetCatsDataRequest(1, _localVersion, ConfigurationManager.AllegroApiKey, false))).catsList;
        } catch (Exception) {
            await InitializeWebApiClient();

            return await GetAllCategories();
        }
    }

    private async Task<doGetSellFormFieldsForCategoryResponse> GetSellFromFields(int categoryId) {
        try {
            return await _allegroWebApiClient.doGetSellFormFieldsForCategoryAsync(ConfigurationManager.AllegroApiKey, 1, categoryId);
        } catch (Exception) {
            await InitializeWebApiClient();

            return await GetSellFromFields(categoryId);
        }
    }

    private async Task<object> AddNewAuction(NewAllegroSellingItemRequest newSellingRequest) {
        doNewAuctionExtResponse response = await _allegroWebApiClient.doNewAuctionExtAsync(
            new doNewAuctionExtRequest(
                _sessionData.sessionHandlePart,
                GenerateFieldsValue(newSellingRequest).ToArray(),
                0,
                0,
                null,
                new VariantStruct[] { },
                new TagNameStruct[] { },
                new AfterSalesServiceConditionsStruct {
                    returnPolicy = "117554cf-3359-477c-bb73-efba1ccc93d0",
                    warranty = "59cee0b4-c65b-4f5f-a021-be60f6d298ac",
                    impliedWarranty = "a887d66f-6a3b-48b3-8200-9da31ba9be74"
                },
                null
            )
        );

        return response;
    }

    private async Task<object> CheckNewSellingItem(NewAllegroSellingItemRequest newSellingRequest) {
        doCheckNewAuctionExtResponse response = await _allegroWebApiClient.doCheckNewAuctionExtAsync(
            new doCheckNewAuctionExtRequest(
                _sessionData.sessionHandlePart,
                GenerateFieldsValue(newSellingRequest).ToArray(),
                new VariantStruct[] { },
                new TagNameStruct[] { },
                new AfterSalesServiceConditionsStruct {
                    returnPolicy = "f4484117-33aa-42b0-802a-3e2c99cfde33",
                    warranty = "776c2f4d-18f7-49e2-9c75-7a9e12c9f2eb",
                    impliedWarranty = "661c2ac3-6790-4c7f-891a-a7667cbfd7a1"
                }
            )
        );

        return response;
    }

    private static List<FieldsValue> GenerateFieldsValue(NewAllegroSellingItemRequest newSellingRequest) {
        List<FieldsValue> formFields = new() {
            new FieldsValue {
                fid = 1,
                fvalueString = newSellingRequest.Title
            },
            new FieldsValue {
                fid = 2,
                fvalueInt = newSellingRequest.CategoryId,
                fvalueIntSpecified = true
            },
            new FieldsValue {
                fid = 341,
                fvalueString = newSellingRequest.Description
            }
        };

        switch (newSellingRequest.ItemCountType) {
            case AllegroItemCountType.Sztuk:
                formFields.Add(new FieldsValue {
                    fid = 28,
                    fvalueInt = 0,
                    fvalueIntSpecified = true
                });
                break;
            case AllegroItemCountType.Kompletów:
                formFields.Add(new FieldsValue {
                    fid = 28,
                    fvalueInt = 1,
                    fvalueIntSpecified = true
                });
                break;
            case AllegroItemCountType.Par:
                formFields.Add(new FieldsValue {
                    fid = 28,
                    fvalueInt = 2,
                    fvalueIntSpecified = true
                });
                break;
        }

        if (newSellingRequest.IsAuction && newSellingRequest.IsBuyNow) {
            if (newSellingRequest.MinimalPrice.Equals(0))
                formFields.Add(new FieldsValue {
                    fid = 7,
                    fvalueFloat = newSellingRequest.MinimalPrice,
                    fvalueFloatSpecified = true
                });
            formFields.Add(new FieldsValue {
                fid = 6,
                fvalueFloat = newSellingRequest.StartingPrice,
                fvalueFloatSpecified = true
            });
            formFields.Add(new FieldsValue {
                fid = 8,
                fvalueFloat = newSellingRequest.Price,
                fvalueFloatSpecified = true
            });
        } else if (newSellingRequest.IsAuction) {
            if (newSellingRequest.MinimalPrice.Equals(0))
                formFields.Add(new FieldsValue {
                    fid = 7,
                    fvalueFloat = newSellingRequest.MinimalPrice,
                    fvalueFloatSpecified = true
                });
            formFields.Add(new FieldsValue {
                fid = 6,
                fvalueFloat = newSellingRequest.StartingPrice,
                fvalueFloatSpecified = true
            });
        } else {
            formFields.Add(new FieldsValue {
                fid = 5,
                fvalueInt = newSellingRequest.ItemCount,
                fvalueIntSpecified = true
            });
            formFields.Add(new FieldsValue {
                fid = 8,
                fvalueFloat = newSellingRequest.Price,
                fvalueFloatSpecified = true
            });
        }

        if (newSellingRequest.IsOfferResuming)
            formFields.Add(new FieldsValue {
                fid = 30,
                fvalueInt = 1,
                fvalueIntSpecified = true
            });
        else
            formFields.Add(new FieldsValue {
                fid = 30,
                fvalueInt = 0,
                fvalueIntSpecified = true
            });

        if (newSellingRequest.UntilLastItem)
            formFields.Add(new FieldsValue {
                fid = 4,
                fvalueInt = 99,
                fvalueIntSpecified = true
            });
        else
            switch (newSellingRequest.SellingLifeTime) {
                case AllegroSellingLifeTime.ThreeDays:
                    formFields.Add(new FieldsValue {
                        fid = 4,
                        fvalueInt = 0,
                        fvalueIntSpecified = true
                    });
                    break;
                case AllegroSellingLifeTime.FiveDays:
                    formFields.Add(new FieldsValue {
                        fid = 4,
                        fvalueInt = 1,
                        fvalueIntSpecified = true
                    });
                    break;
                case AllegroSellingLifeTime.SevenDays:
                    formFields.Add(new FieldsValue {
                        fid = 4,
                        fvalueInt = 2,
                        fvalueIntSpecified = true
                    });
                    break;
                case AllegroSellingLifeTime.TenDays:
                    formFields.Add(new FieldsValue {
                        fid = 4,
                        fvalueInt = 3,
                        fvalueIntSpecified = true
                    });
                    break;
                case AllegroSellingLifeTime.TwentyDays:
                    formFields.Add(new FieldsValue {
                        fid = 4,
                        fvalueInt = 6,
                        fvalueIntSpecified = true
                    });
                    break;
            }

        switch (newSellingRequest.InvoiceOption) {
            case AllegroSellingInvoiceOption.VAT:
                formFields.Add(new FieldsValue {
                    fid = 14,
                    fvalueInt = 32,
                    fvalueIntSpecified = true
                });
                break;
            case AllegroSellingInvoiceOption.VATMargin:
                formFields.Add(new FieldsValue {
                    fid = 14,
                    fvalueInt = 64,
                    fvalueIntSpecified = true
                });
                break;
            case AllegroSellingInvoiceOption.WithoutVAT:
                formFields.Add(new FieldsValue {
                    fid = 14,
                    fvalueInt = 128,
                    fvalueIntSpecified = true
                });
                break;
        }

        if (newSellingRequest.IsDeferredSelling)
            formFields.Add(new FieldsValue {
                fid = 3,
                fvalueDatetime = (float)newSellingRequest.SellingStartDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                fvalueDatetimeSpecified = true
            });

        foreach (AllegroSellFromField value in newSellingRequest.CategoryFields)
            switch (value.ValueType) {
                case AllegroSellFromFieldValueType.String:
                    formFields.Add(new FieldsValue {
                        fid = value.Id,
                        fvalueString = value.Value
                    });
                    break;
                case AllegroSellFromFieldValueType.Integer:
                    formFields.Add(new FieldsValue {
                        fid = value.Id,
                        fvalueInt = Convert.ToInt32(value.Value),
                        fvalueIntSpecified = true
                    });
                    break;
                case AllegroSellFromFieldValueType.Float:
                    formFields.Add(new FieldsValue {
                        fid = value.Id,
                        fvalueFloat = float.Parse(value.Value),
                        fvalueFloatSpecified = true
                    });
                    break;
                case AllegroSellFromFieldValueType.Image:
                    formFields.Add(new FieldsValue {
                        fid = value.Id,
                        fvalueImage = Convert.FromBase64String(value.Value)
                    });
                    break;
                case AllegroSellFromFieldValueType.UnixTimestamp:
                    formFields.Add(new FieldsValue {
                        fid = value.Id,
                        fvalueDatetime = (float)DateTime.Parse(value.Value).Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                        fvalueDatetimeSpecified = true
                    });
                    break;
                case AllegroSellFromFieldValueType.DateTime:
                    formFields.Add(new FieldsValue {
                        fid = value.Id,
                        fvalueDate = value.Value
                    });
                    break;
            }

        if (newSellingRequest.Images.Length > 8) {
            int startingId = 16;

            for (int i = 0; i < 8; i++)
                formFields.Add(new FieldsValue {
                    fid = startingId++,
                    fvalueImage = Convert.FromBase64String(newSellingRequest.Images[i])
                });
        } else {
            int startingId = 16;

            foreach (string imageString in newSellingRequest.Images)
                formFields.Add(new FieldsValue {
                    fid = startingId++,
                    fvalueImage = Convert.FromBase64String(imageString)
                });
        }

        //Location hard coded
        formFields.Add(new FieldsValue {
            fid = 9,
            fvalueInt = 1,
            fvalueIntSpecified = true
        });
        formFields.Add(new FieldsValue {
            fid = 10,
            fvalueInt = 13,
            fvalueIntSpecified = true
        });
        formFields.Add(new FieldsValue {
            fid = 32,
            fvalueString = "37-700"
        });
        formFields.Add(new FieldsValue {
            fid = 11,
            fvalueString = "Przemysl"
        });

        //Temporary hard coded delivery info

        formFields.Add(new FieldsValue {
            fid = 45,
            fvalueFloat = 14,
            fvalueFloatSpecified = true
        });
        formFields.Add(new FieldsValue {
            fid = 145,
            fvalueFloat = 0,
            fvalueFloatSpecified = true
        });
        formFields.Add(new FieldsValue {
            fid = 245,
            fvalueInt = 100,
            fvalueIntSpecified = true
        });

        return formFields;
    }
}
using System.Collections.Generic;
using System.Linq;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Helpers.Products;

public static class AnaloguesFinder {
    private static readonly Dictionary<string, HashSet<string>> _analoguesDictionary = new();

    public static HashSet<AnalogueForUpload> FindAllAnaloguePairs(HashSet<AnalogueForUpload> productAnalogues, IProductAnalogueRepository productAnalogueRepository) {
        IncludeAnaloguePairsFromDb(productAnalogues, productAnalogueRepository);

        _analoguesDictionary.Clear();
        PopulateDirectAnalogues(productAnalogues);

        bool changesMade;
        do {
            changesMade = IncludeIndirectAnalogues();
        } while (changesMade);

        HashSet<AnalogueForUpload> allPairs = new();
        CreateListOfAnaloguePairsFromDictionary(allPairs);

        return allPairs;
    }

    private static void PopulateDirectAnalogues(HashSet<AnalogueForUpload> productAnalogues) {
        foreach (AnalogueForUpload pair in productAnalogues) {
            if (!BaseProductAlreadyInList(pair.VendorCode))
                _analoguesDictionary[pair.VendorCode] = new HashSet<string>();

            _analoguesDictionary[pair.VendorCode].Add(pair.AnalogueVendorCode);

            if (!BaseProductAlreadyInList(pair.AnalogueVendorCode))
                _analoguesDictionary[pair.AnalogueVendorCode] = new HashSet<string>();

            _analoguesDictionary[pair.AnalogueVendorCode].Add(pair.VendorCode);
        }
    }

    private static bool IncludeIndirectAnalogues() {
        bool changesMade = false;
        Dictionary<string, HashSet<string>> tempDictionary = new();

        foreach (KeyValuePair<string, HashSet<string>> kvp in _analoguesDictionary) {
            string vendorCode = kvp.Key;
            HashSet<string> analogues = kvp.Value;

            if (!tempDictionary.ContainsKey(vendorCode))
                tempDictionary[vendorCode] = new HashSet<string>(analogues);

            foreach (string analogue in analogues) {
                if (!tempDictionary.ContainsKey(analogue))
                    tempDictionary[analogue] = new HashSet<string>(_analoguesDictionary[analogue]);

                foreach (string indirectAnalogue in _analoguesDictionary[analogue]) {
                    if (indirectAnalogue != vendorCode && !tempDictionary[vendorCode].Contains(indirectAnalogue)) {
                        tempDictionary[vendorCode].Add(indirectAnalogue);
                        changesMade = true;
                    }

                    if (!tempDictionary[analogue].Contains(vendorCode)) {
                        tempDictionary[analogue].Add(vendorCode);
                        changesMade = true;
                    }
                }
            }
        }

        if (changesMade) {
            _analoguesDictionary.Clear();
            foreach (KeyValuePair<string, HashSet<string>> kvp in tempDictionary) _analoguesDictionary[kvp.Key] = new HashSet<string>(kvp.Value);
        }

        return changesMade;
    }

    private static bool BaseProductAlreadyInList(string analogueVendorCode) {
        return _analoguesDictionary.ContainsKey(analogueVendorCode);
    }

    private static void CreateListOfAnaloguePairsFromDictionary(HashSet<AnalogueForUpload> allPairs) {
        foreach (KeyValuePair<string, HashSet<string>> kvp in _analoguesDictionary) {
            string vendorCode = kvp.Key;
            HashSet<string> analogues = kvp.Value;

            foreach (string analogueVendorCode in analogues) {
                allPairs.Add(new AnalogueForUpload { VendorCode = vendorCode, AnalogueVendorCode = analogueVendorCode });

                allPairs.Add(new AnalogueForUpload { VendorCode = analogueVendorCode, AnalogueVendorCode = vendorCode });
            }
        }
    }

    private static void IncludeAnaloguePairsFromDb(HashSet<AnalogueForUpload> pairsToInsert, IProductAnalogueRepository productAnalogueRepository) {
        List<AnalogueForUpload> existingAnaloguesFromDb = new();

        foreach (AnalogueForUpload analogueForUpload in pairsToInsert) {
            existingAnaloguesFromDb.AddRange(
                productAnalogueRepository
                    .GetAllProductAnaloguesByBaseProductVendorCode(analogueForUpload.VendorCode)
                    .Select(a => new AnalogueForUpload {
                        VendorCode = a.BaseProduct.VendorCode,
                        AnalogueVendorCode = a.AnalogueProduct.VendorCode
                    }));

            existingAnaloguesFromDb.AddRange(
                productAnalogueRepository
                    .GetAllProductAnaloguesByBaseProductVendorCode(analogueForUpload.AnalogueVendorCode)
                    .Select(a => new AnalogueForUpload {
                        VendorCode = a.BaseProduct.VendorCode,
                        AnalogueVendorCode = a.AnalogueProduct.VendorCode
                    }));
        }

        pairsToInsert.UnionWith(existingAnaloguesFromDb);
    }
}
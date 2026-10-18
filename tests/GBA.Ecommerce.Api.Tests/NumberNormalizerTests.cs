using GBA.Search.Sync;

namespace GBA.Ecommerce.Api.Tests;

public sealed class NumberNormalizerTests {
    [Theory]
    [InlineData("ISS1004С", "ISS1004C")]
    [InlineData("2491-CСB", "2491-CCB")]
    [InlineData("K1200М", "K1200M")]
    [InlineData("0102-НС1000Р", "0102-HC1000P")]
    [InlineData("09973050МFSS", "09973050MFSS")]
    public void Indexed_codes_fold_cyrillic_lookalikes_to_latin(string cyrillic, string latin) {
        Assert.Equal(NumberNormalizer.Normalize(latin), NumberNormalizer.Normalize(cyrillic));
    }

    [Theory]
    [InlineData("ISS1004С")]
    [InlineData("ISS1004C")]
    [InlineData("iss-1004c")]
    [InlineData("0102-НС1000Р")]
    [InlineData("СК-1200")]
    [InlineData("3021-С")]
    public void Query_codes_normalize_exactly_like_indexed_codes(string query) {
        Assert.Equal(NumberNormalizer.Normalize(query), NumberNormalizer.NormalizeQuery(query));
    }

    [Fact]
    public void Query_words_without_digits_keep_their_cyrillic_letters() {
        Assert.Equal("Фільтр Масляний", NumberNormalizer.NormalizeQuery("фільтр  масляний"));
    }

    [Fact]
    public void Query_separators_split_terms_and_punctuation_joins_code_parts() {
        Assert.Equal("2434 03", NumberNormalizer.NormalizeQuery("2434 03"));
        Assert.Equal("243403", NumberNormalizer.NormalizeQuery("2434-03"));
        Assert.Equal("w 95026", NumberNormalizer.NormalizeQuery("W\t950/26"));
    }

    [Fact]
    public void Elasticsearch_char_filter_mappings_match_the_csharp_fold() {
        foreach (string mapping in NumberNormalizer.LookalikeCharFilterMappings()) {
            string[] parts = mapping.Split(" => ");
            Assert.Equal(parts[1], NumberNormalizer.FoldLookalikes(parts[0]));
        }
    }
}

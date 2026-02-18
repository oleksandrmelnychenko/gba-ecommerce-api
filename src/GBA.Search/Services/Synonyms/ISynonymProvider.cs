namespace GBA.Search.Services.Synonyms;

public interface ISynonymProvider {
    string Apply(string normalizedQuery);
}

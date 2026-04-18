using System.Linq;
using System.Threading.Tasks;
using Italbytz.Tcg.Abstractions;
using Italbytz.Tcg.InMemory;

namespace Italbytz.Tcg.Abstractions.Tests;

[TestClass]
public class InMemoryRepositoriesTests
{
    [TestMethod]
    public async Task CardRepositoryStoresAndListsCardsByGame()
    {
        var repository = new InMemoryTcgCardRepository();

        await repository.UpsertAsync(new TcgCard { Game = TcgGameIds.Pokemon, Id = "base1-1", Name = "Alakazam" });
        await repository.UpsertAsync(new TcgCard { Game = TcgGameIds.MagicTheGathering, Id = "mtg-1", Name = "Black Lotus" });

        var pokemonCards = await repository.ListByGameAsync(TcgGameIds.Pokemon);

        Assert.HasCount(1, pokemonCards);
        Assert.AreEqual("Alakazam", pokemonCards.Single().Name);
    }

    [TestMethod]
    public async Task SetRepositoryRemovesEntriesByGameAndId()
    {
        var repository = new InMemoryTcgSetRepository();

        await repository.UpsertAsync(new TcgSet { Game = TcgGameIds.Pokemon, Id = "base1", Name = "Base" });
        await repository.RemoveAsync(TcgGameIds.Pokemon, "base1");

        var set = await repository.GetByIdAsync(TcgGameIds.Pokemon, "base1");

        Assert.IsNull(set);
    }

    [TestMethod]
    public void QueryDefaultsAreSuitableForPagedApiAccess()
    {
        var query = new TcgCardQuery();

        Assert.AreEqual(1, query.PageNumber);
        Assert.AreEqual(20, query.PageSize);
        Assert.IsEmpty(query.Filters);
    }
}
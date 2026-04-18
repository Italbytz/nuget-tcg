using System;
using System.Net.Http;
using System.Threading.Tasks;
using Italbytz.Tcg.Abstractions;
using Italbytz.Tcg.Scryfall;

namespace Italbytz.Tcg.Abstractions.Tests;

[TestClass]
public class ScryfallClientTests
{
    [TestMethod]
    public async Task SearchCardsAsyncMapsMagicCardsToSharedAbstraction()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            StringAssert.Contains(request.RequestUri!.ToString(), "cards/search");
            StringAssert.Contains(request.RequestUri!.ToString(), "q=Black Lotus set%3Alea");

            return FakeHttpMessageHandler.Json("""
                {
                  "data": [
                    {
                      "id": "0000-1111",
                      "name": "Black Lotus",
                      "collector_number": "233",
                      "layout": "normal",
                      "type_line": "Artifact",
                      "rarity": "rare",
                      "set": "lea",
                      "set_name": "Limited Edition Alpha",
                      "mana_cost": "{0}",
                      "oracle_text": "{T}, Sacrifice Black Lotus: Add three mana of any one color.",
                      "color_identity": ["C"],
                      "image_uris": {
                        "normal": "https://cards.scryfall.io/normal/front/test.jpg"
                      }
                    }
                  ],
                  "total_cards": 1
                }
                """);
        });

        using var client = new ScryfallClient(new HttpClient(handler), new ScryfallOptions());
        var query = new TcgCardQuery
        {
            Game = TcgGameIds.MagicTheGathering,
            Text = "Black Lotus"
        };
        query.Filters["set"] = "lea";

        var result = await client.SearchCardsAsync(query);

        Assert.HasCount(1, result.Items);
        Assert.AreEqual(TcgGameIds.MagicTheGathering, result.Items[0].Game);
        Assert.AreEqual("Black Lotus", result.Items[0].Name);
        Assert.AreEqual("lea", result.Items[0].SetId);
        Assert.AreEqual("{0}", result.Items[0].Attributes["manaCost"]);
    }

    [TestMethod]
    public async Task GetSetByIdAsyncMapsSetMetadata()
    {
        var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.Json("""
            {
              "code": "lea",
              "name": "Limited Edition Alpha",
              "released_at": "1993-08-05",
              "icon_svg_uri": "https://svgs.scryfall.io/sets/lea.svg",
              "card_count": 295,
              "set_type": "core"
            }
            """));

        using var client = new ScryfallClient(new HttpClient(handler), new ScryfallOptions());

        var set = await client.GetSetByIdAsync(TcgGameIds.MagicTheGathering, "lea");

        Assert.IsNotNull(set);
        Assert.AreEqual("Limited Edition Alpha", set.Name);
        Assert.AreEqual(295, set.CardCount);
        Assert.AreEqual("core", set.Metadata["setType"]);
        Assert.AreEqual(new Uri("https://svgs.scryfall.io/sets/lea.svg"), set.SymbolUri);
    }
}
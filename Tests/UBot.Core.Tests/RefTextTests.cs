using UBot.Core;
using UBot.Core.Client;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using Xunit;

namespace UBot.Core.Tests;

public class RefTextTests
{
    [Fact]
    public void Load_ShouldPreferTurkeyTextColumn_ForTurkeyClient()
    {
        var previousClientType = Game.ClientType;
        var previousReferenceManager = Game.ReferenceManager;

        try
        {
            Game.ClientType = GameClientType.Turkey;
            Game.ReferenceManager = new ReferenceManager { LanguageTab = 9 };

            var text = new RefText();
            var loaded = text.Load(new ReferenceParser("1\t1\tSN_TEST\tKorean\t0\t0\t0\t0\t0\t0\t0\t0\t0\tTurkey Text\t0\t0\t0"));

            Assert.True(loaded);
            Assert.Equal("SN_TEST", text.PrimaryKey);
            Assert.Equal("Turkey Text", text.Data);
        }
        finally
        {
            Game.ClientType = previousClientType;
            Game.ReferenceManager = previousReferenceManager;
        }
    }

    [Fact]
    public void Load_ShouldFallBackToNameStrId_WhenTurkeyTextColumnIsMissing()
    {
        var previousClientType = Game.ClientType;
        var previousReferenceManager = Game.ReferenceManager;

        try
        {
            Game.ClientType = GameClientType.Turkey;
            Game.ReferenceManager = new ReferenceManager();

            var text = new RefText();
            var loaded = text.Load(new ReferenceParser("1\t1\tSN_EMPTY\tKorean Text\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0"));

            Assert.True(loaded);
            Assert.Equal("SN_EMPTY", text.Data);
        }
        finally
        {
            Game.ClientType = previousClientType;
            Game.ReferenceManager = previousReferenceManager;
        }
    }
}

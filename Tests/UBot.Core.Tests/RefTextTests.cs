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
        var previousClientType = UBot.Core.RuntimeAccess.Session.ClientType;
        var previousReferenceManager = UBot.Core.RuntimeAccess.Session.ReferenceManager;

        try
        {
            UBot.Core.RuntimeAccess.Session.ClientType = GameClientType.Turkey;
            UBot.Core.RuntimeAccess.Session.ReferenceManager = new ReferenceManager { LanguageTab = 9 };

            var text = new RefText();
            var loaded = text.Load(new ReferenceParser("1\t1\tSN_TEST\tKorean\t0\t0\t0\t0\t0\t0\t0\t0\t0\tTurkey Text\t0\t0\t0"));

            Assert.True(loaded);
            Assert.Equal("SN_TEST", text.PrimaryKey);
            Assert.Equal("Turkey Text", text.Data);
        }
        finally
        {
            UBot.Core.RuntimeAccess.Session.ClientType = previousClientType;
            UBot.Core.RuntimeAccess.Session.ReferenceManager = previousReferenceManager;
        }
    }

    [Fact]
    public void Load_ShouldFallBackToNameStrId_WhenTurkeyTextColumnIsMissing()
    {
        var previousClientType = UBot.Core.RuntimeAccess.Session.ClientType;
        var previousReferenceManager = UBot.Core.RuntimeAccess.Session.ReferenceManager;

        try
        {
            UBot.Core.RuntimeAccess.Session.ClientType = GameClientType.Turkey;
            UBot.Core.RuntimeAccess.Session.ReferenceManager = new ReferenceManager();

            var text = new RefText();
            var loaded = text.Load(new ReferenceParser("1\t1\tSN_EMPTY\tKorean Text\t0\t0\t0\t0\t0\t0\t0\t0\t0\t0"));

            Assert.True(loaded);
            Assert.Equal("SN_EMPTY", text.Data);
        }
        finally
        {
            UBot.Core.RuntimeAccess.Session.ClientType = previousClientType;
            UBot.Core.RuntimeAccess.Session.ReferenceManager = previousReferenceManager;
        }
    }
}

using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Open5e;
using Xunit;

namespace OpenCombatEngine.Implementation.Tests.Open5e
{
    public class Open5eIntegrationTests
    {
        // NOTE: These tests hit the real API. In a robust CI/CD, we'd mock the HttpClient.
        // For development speed here, we'll hit the live API but handle potential failures gracefully (or assume internet access).
        
        [Fact]
        public async Task Can_Fetch_And_Map_Fireball()
        {
            var http = new HttpClient();
            var client = new Open5eClient(http);
            var dice = new StandardDiceRoller();
            var source = new Open5eContentSource(client, dice);

            var result = await source.GetSpellAsync("fireball");

            result.IsSuccess.Should().BeTrue();
            var spell = result.Value;
            spell.Name.Should().Be("Fireball");
            spell.Level.Should().Be(3);
            spell.Range.Should().Contain("150");
        }

        [Fact]
        public async Task Can_Fetch_And_Map_Ancient_Red_Dragon()
        {
            var http = new HttpClient();
            var client = new Open5eClient(http);
            var dice = new StandardDiceRoller();
            var source = new Open5eContentSource(client, dice);

            // "ancient-red-dragon"
            var result = await source.GetMonsterAsync("ancient-red-dragon");

            result.IsSuccess.Should().BeTrue();
            var monster = result.Value;
            monster.Name.Should().Be("Ancient Red Dragon");
            monster.HitPoints.Max.Should().BeGreaterThan(500); 
            monster.AbilityScores.Strength.Should().Be(30);
        }
    }
}

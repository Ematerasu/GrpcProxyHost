using ScriptsOfTribute.Board.Cards;
using ScriptsOfTribute.Serializers;
using ScriptsOfTribute;

namespace GrpcHost.Models;

public class FullyConstructedSerializedPlayer : SerializedPlayer
{
    public FullyConstructedSerializedPlayer(
        PlayerEnum playerId,
        List<UniqueCard> hand,
        List<UniqueCard> drawPile,
        List<UniqueCard> cooldownPile,
        List<UniqueCard> played,
        List<SerializedAgent> agents,
        int power,
        uint patronCalls,
        int coins,
        int prestige,
        List<UniqueCard> knownUpcomingDraws)
        : base(playerId, hand, drawPile, cooldownPile, played, agents, power, patronCalls, coins, prestige)
    {
        typeof(SerializedPlayer)
            .GetField(nameof(KnownUpcomingDraws))!
            .SetValue(this, knownUpcomingDraws);
    }
}
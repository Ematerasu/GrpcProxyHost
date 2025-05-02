using MessagePack;
using ScriptsOfTribute;
using ScriptsOfTribute.Board;
using ScriptsOfTribute.Board.CardAction;
using ScriptsOfTribute.Board.Cards;

namespace GrpcHost;

[MessagePackObject]
public class PlayRequest
{
    [Key(0)] public string Command { get; set; } = "Play";
    [Key(1)] public FullGameStateDto GameState { get; set; }
    [Key(2)] public List<MoveDto> LegalMoves { get; set; } = new();
    [Key(3)] public long TimeoutMs { get; set; }
}

[MessagePackObject]
public class PlayResponse
{
    [Key(0)] public int MoveId { get; set; }
}

[MessagePackObject]
public class SelectPatronRequest
{
    [Key(0)] public string Command { get; set; } = "SelectPatron";
    [Key(1)] public int Round { get; set; }
    [Key(2)] public List<PatronId> PatronIds { get; set; } = new();
}

[MessagePackObject]
public class SelectPatronResponse
{
    [Key(0)] public PatronId Selected { get; set; }
}

[MessagePackObject]
public class GameEndRequest
{
    [Key(0)] public string Command { get; set; } = "GameEnd";
    [Key(1)] public PlayerEnum Winner { get; set; }
    [Key(2)] public GameEndReason Reason { get; set; }
    [Key(3)] public string AdditionalContext { get; set; } = "";
    [Key(4)] public FullGameStateDto GameState { get; set; }
}

[MessagePackObject]
public class GenericCommand
{
    [Key(0)] public string Command { get; set; } = "";
}

[MessagePackObject]
public class RegisterResponse
{
    [Key(0)] public string Name { get; set; }
}

[MessagePackObject]
[Union(0, typeof(SimpleCardMoveDto))]
[Union(1, typeof(SimplePatronMoveDto))]
[Union(2, typeof(MakeChoiceMoveCardDto))]
[Union(3, typeof(MakeChoiceMoveEffectDto))]
[Union(4, typeof(EndTurnMoveDto))]
public abstract class MoveDto
{
    [Key(0)] public CommandEnum Command { get; set; }
    [Key(1)] public int MoveId { get; set; }
}

[MessagePackObject]
public class SimpleCardMoveDto : MoveDto
{
    [Key(2)] public int CardId { get; set; }
}

[MessagePackObject]
public class SimplePatronMoveDto : MoveDto
{
    [Key(2)] public PatronId Patron { get; set; }
}

[MessagePackObject]
public class MakeChoiceMoveCardDto : MoveDto
{
    [Key(2)] public List<int> CardIds { get; set; } = new();
}

[MessagePackObject]
public class MakeChoiceMoveEffectDto : MoveDto
{
    [Key(2)] public List<UniqueEffectDto> EffectNames { get; set; } = new();
}

[MessagePackObject]
public class EndTurnMoveDto : MoveDto
{

}

[MessagePackObject]
public class FullGameStateDto
{
    [Key(0)] public string StateId { get; set; }
    [Key(1)] public SerializedPlayerDto CurrentPlayer { get; set; }
    [Key(2)] public SerializedPlayerDto EnemyPlayer { get; set; }
    [Key(3)] public Dictionary<PatronId, PlayerEnum> PatronStates { get; set; }
    [Key(4)] public List<UniqueCardDto> TavernAvailableCards { get; set; } = new();
    [Key(5)] public List<UniqueCardDto> TavernCards { get; set; } = new();
    [Key(6)] public BoardState BoardState { get; set; }
    [Key(7)] public SerializedChoiceDto? PendingChoice { get; set; }
    [Key(8)] public Dictionary<PatronId, ComboStateDto> ComboStates { get; set; } = new();
    [Key(9)] public List<UniqueBaseEffectDto> UpcomingEffects { get; set; } = new();
    [Key(10)] public List<UniqueBaseEffectDto> StartOfNextTurnEffects { get; set; } = new();
    [Key(11)] public List<string> CompletedActions { get; set; } = new();
    [Key(12)] public EndGameStateDto? GameEndState { get; set; }
    [Key(13)] public ulong InitialSeed { get; set; }
    [Key(14)] public ulong CurrentSeed { get; set; }
    [Key(15)] public bool Cheats { get; set; }

}

[MessagePackObject]
public class ComboStateDto
{
    [Key(0)] public List<UniqueBaseEffectDto>[] Effects { get; set; } = new List<UniqueBaseEffectDto>[4];
    [Key(1)] public int CurrentCombo { get; set; }

}

[MessagePackObject]
public class UniqueCardDto
{
    [Key(0)] public string Name { get; set; }
    [Key(1)] public PatronId Deck { get; set; }
    [Key(2)] public CardId CommonId { get; set; }
    [Key(3)] public int Cost { get; set; }
    [Key(4)] public CardType Type { get; set; }
    [Key(5)] public int Hp { get; set; }
    [Key(6)] public UniqueBaseEffectDto?[] Effects { get; set; } = new UniqueBaseEffectDto?[4];
    [Key(7)] public int hash { get; set; }
    [Key(8)] public bool Taunt { get; set; }
    [Key(9)] public int UniqueId { get; set; }

}

[MessagePackObject]
public class SerializedAgentDto
{
    [Key(0)] public int CurrentHp { get; set; }
    [Key(1)] public bool Activated { get; set; }
    [Key(2)] public UniqueCardDto RepresentingCard { get; set; }

}

[MessagePackObject]
public class ChoiceContextDto
{
    [Key(0)] public ChoiceType ChoiceType { get; set; }

    [Key(1)] public PatronId? PatronSource { get; set; }

    [Key(2)] public int? CardId { get; set; }

    [Key(3)] public int Combo { get; set; } = 1;
}

[MessagePackObject]
public class SerializedChoiceDto
{
    [Key(0)] public Choice.DataType Type { get; set; }
    [Key(1)] public int MaxChoices { get; set; }
    [Key(2)] public int MinChoices { get; set; }
    [Key(3)] public ChoiceContextDto Context { get; set; }
    [Key(4)] public ChoiceFollowUp ChoiceFollowUp { get; set; }
    [Key(5)] public List<UniqueCardDto>? Cards { get; set; }
    [Key(6)] public List<UniqueEffectDto>? Effects { get; set; }

}

[MessagePackObject]
public class SerializedPlayerDto
{
    [Key(0)] public PlayerEnum PlayerID { get; set; }
    [Key(1)] public List<UniqueCardDto> Hand { get; set; } = new();
    [Key(2)] public List<UniqueCardDto> DrawPile { get; set; } = new();
    [Key(3)] public List<UniqueCardDto> CooldownPile { get; set; } = new();
    [Key(4)] public List<UniqueCardDto> Played { get; set; } = new();
    [Key(5)] public List<SerializedAgentDto> Agents { get; set; } = new();
    [Key(6)] public int Power { get; set; }
    [Key(7)] public int PatronCalls { get; set; }
    [Key(8)] public int Coins { get; set; }
    [Key(9)] public int Prestige { get; set; }
    [Key(10)] public List<UniqueCardDto> KnownUpcomingDraws { get; set; } = new();

}

[MessagePackObject]
[Union(0, typeof(UniqueEffectDto))]
[Union(1, typeof(UniqueEffectOrDto))]
[Union(2, typeof(UniqueEffectCompositeDto))]
public abstract class UniqueBaseEffectDto
{
    [Key(0)] public int Combo { get; set; }
    [Key(1)] public int OriginCardId { get; set; }
}

[MessagePackObject]
public class UniqueEffectDto : UniqueBaseEffectDto
{
    [Key(2)] public EffectType Type { get; set; }
    [Key(3)] public int Amount { get; set; }

}

[MessagePackObject]
public class UniqueEffectOrDto : UniqueBaseEffectDto
{
    [Key(2)] public UniqueEffectDto Left { get; set; }
    [Key(3)] public UniqueEffectDto Right { get; set; }

}

[MessagePackObject]
public class UniqueEffectCompositeDto : UniqueBaseEffectDto
{
    [Key(2)] public UniqueEffectDto Left { get; set; }
    [Key(3)] public UniqueEffectDto Right { get; set; }
}

[MessagePackObject]
public class EndGameStateDto
{
    [Key(0)] public PlayerEnum Winner { get; set; }
    [Key(1)] public GameEndReason Reason { get; set; }
    [Key(2)] public string AdditionalContext { get; set; }
}
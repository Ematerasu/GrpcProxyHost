using GrpcHost.Models;
using ScriptsOfTribute;
using ScriptsOfTribute.Board;
using ScriptsOfTribute.Board.Cards;
using ScriptsOfTribute.Serializers;

namespace GrpcHost;

public static class ModelConverter
{
    public static UniqueCard ToModel(this UniqueCardDto dto)
    {
        var card = new Card(dto.Name, dto.Deck, dto.CommonId, dto.Cost, dto.Type, dto.Hp, new ComplexEffect[4], dto.hash, null, dto.Taunt, 1);
        var unique = card.CreateUniqueCopy();

        UniqueIdMapper.Register(dto.UniqueId, unique.UniqueId.Value);
        UniqueIdMapper.RegisterCard(dto.UniqueId, unique);

        for (int i = 0; i < 4; i++)
        {
            if (dto.Effects[i] is { } effectDto)
            {
                unique.Effects[i] = effectDto.ToModel();
            }
        }

        return unique;
    }

    public static UniqueComplexEffect ToModel(this UniqueBaseEffectDto dto)
    {
        var originCard = UniqueIdMapper.GetCard(dto.OriginCardId);

        return dto switch
        {
            UniqueEffectDto simple => new UniqueEffect(simple.Type, simple.Amount, simple.Combo, originCard),

            UniqueEffectOrDto or => new UniqueEffectOr(
                new UniqueEffect(or.Left.Type, or.Left.Amount, or.Left.Combo, originCard),
                new UniqueEffect(or.Right.Type, or.Right.Amount, or.Right.Combo, originCard),
                or.Combo,
                originCard
            ),

            UniqueEffectCompositeDto comp => new UniqueEffectComposite(
                new UniqueEffect(comp.Left.Type, comp.Left.Amount, comp.Left.Combo, originCard),
                new UniqueEffect(comp.Right.Type, comp.Right.Amount, comp.Right.Combo, originCard),
                originCard
            ),

            _ => throw new NotSupportedException($"Unknown effect DTO type: {dto.GetType().Name}")
        };
    }

    public static UniqueEffect ToModel(this UniqueEffectDto dto)
    {
        return new UniqueEffect(dto.Type, dto.Amount, dto.Combo, UniqueIdMapper.GetCard(dto.OriginCardId));
    }

    public static SerializedAgent ToModel(this SerializedAgentDto dto)
    {
        var card = dto.RepresentingCard.ToModel();
        var agent = Agent.FromCard(card);
        if (dto.Activated)
            agent.MarkActivated();
        else
            agent.Refresh();

        agent.Damage(agent.CurrentHp - dto.CurrentHp);

        return new SerializedAgent(agent);
    }

    public static SerializedPlayer ToModel(this SerializedPlayerDto dto)
    {
        return new FullyConstructedSerializedPlayer(
            playerId: dto.PlayerID,
            hand: dto.Hand.Select(c => c.ToModel()).ToList(),
            drawPile: dto.DrawPile.Select(c => c.ToModel()).ToList(),
            cooldownPile: dto.CooldownPile.Select(c => c.ToModel()).ToList(),
            played: dto.Played.Select(c => c.ToModel()).ToList(),
            agents: dto.Agents.Select(a => a.ToModel()).ToList(),
            power: dto.Power,
            patronCalls: (uint)dto.PatronCalls,
            coins: dto.Coins,
            prestige: dto.Prestige,
            knownUpcomingDraws: dto.KnownUpcomingDraws.Select(c => c.ToModel()).ToList()
        );
    }

    public static ChoiceContext? ToModel(this ChoiceContextDto dto)
    {
        return dto.ChoiceType switch
        {
            ChoiceType.PATRON_ACTIVATION => new ChoiceContext(dto.PatronSource!.Value),
            ChoiceType.CARD_EFFECT or ChoiceType.EFFECT_CHOICE =>
                new ChoiceContext(
                    UniqueIdMapper.GetCard(dto.CardId!.Value),
                    dto.ChoiceType,
                    dto.Combo
                ),
            _ => throw new Exception("Invalid ChoiceContextDto")
        };
    }

    public static SerializedChoice? ToModel(this SerializedChoiceDto? dto)
    {
        if (dto == null) return null;

        return new SerializedChoice(
            maxChoices: dto.MaxChoices,
            minChoices: dto.MinChoices,
            context: dto.Context.ToModel(),
            possibleCards: dto.Cards?.Select(c => c.ToModel()).ToList(),
            possibleEffects: dto.Effects?.Select(e => e.ToModel()).ToList(),
            choiceFollowUp: dto.ChoiceFollowUp,
            type: dto.Type
        );
    }

    public static ComboState ToModel(this ComboStateDto dto)
    {
        var effects = new List<UniqueBaseEffect>[4];
        for (int i = 0; i < 4; i++)
        {
            effects[i] = dto.Effects[i].SelectMany(e => e.ToModel().Decompose()).ToList();
        }

        return new ComboState(effects, dto.CurrentCombo);
    }

    public static FullGameState ToModel(this FullGameStateDto dto)
    {
        var tavernAvailable = dto.TavernAvailableCards.Select(c => c.ToModel()).ToList();
        var tavernCards = dto.TavernCards.Select(c => c.ToModel()).ToList();

        var current = dto.CurrentPlayer.ToModel();
        var enemy = dto.EnemyPlayer.ToModel();
        var choice = dto.PendingChoice.ToModel();

        var comboStates = dto.ComboStates.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToModel()
        );

        return new FullGameState(
            currentPlayer: current,
            enemyPlayer: enemy,
            patronStates: new ProxyPatronStates(dto.PatronStates),
            tavernAvailableCards: tavernAvailable,
            tavernCards: tavernCards,
            boardState: dto.BoardState,
            pendingChoice: choice,
            comboStates: new ComboStates(comboStates),
            upcomingEffects: dto.UpcomingEffects.SelectMany(e => e.ToModel().Decompose()).ToList(),
            startOfNextTurnEffects: dto.StartOfNextTurnEffects.SelectMany(e => e.ToModel().Decompose()).ToList(),
            completedActions: new(),
            gameEndState: dto.GameEndState.ToModel(),
            initialSeed: dto.InitialSeed,
            currentSeed: dto.CurrentSeed,
            cheats: dto.Cheats
        );
    }

    public static Move ToModel(this MoveDto dto)
    {
        Move move = dto switch
        {
            SimpleCardMoveDto cardDto => CreateSimpleCardMove(dto.Command, cardDto.CardId),
            SimplePatronMoveDto patronDto => Move.CallPatron(patronDto.Patron),
            MakeChoiceMoveCardDto cardChoice => Move.MakeChoice(cardChoice.CardIds.Select(UniqueIdMapper.GetCard).ToList()),
            MakeChoiceMoveEffectDto effectChoice => Move.MakeChoice(effectChoice.EffectNames.Select(e => e.ToModel()).ToList()),
            EndTurnMoveDto => Move.EndTurn(),
            _ => throw new NotSupportedException($"Unsupported MoveDto type: {dto.GetType().Name}")
        };

        UniqueIdMapper.Register(dto.MoveId, move.UniqueId.Value);
        return move;
    }

    private static Move CreateSimpleCardMove(CommandEnum command, int cardId)
    {
        var card = UniqueIdMapper.GetCard(cardId);

        return command switch
        {
            CommandEnum.PLAY_CARD => Move.PlayCard(card),
            CommandEnum.BUY_CARD => Move.BuyCard(card),
            CommandEnum.ACTIVATE_AGENT => Move.ActivateAgent(card),
            CommandEnum.ATTACK => Move.Attack(card),
            _ => throw new NotSupportedException($"Unknown SimpleCardMove command: {command}")
        };
    }

    public static EndGameState? ToModel(this EndGameStateDto? dto)
    {
        if (dto == null) return null;

        return new EndGameState(dto.Winner, dto.Reason, dto.AdditionalContext);
    }
}
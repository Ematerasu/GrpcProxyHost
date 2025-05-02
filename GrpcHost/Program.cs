using MessagePack;
using ScriptsOfTribute.Board;
using ScriptsOfTribute.Serializers;
using ScriptsOfTributeGRPC;
using System.IO.Pipes;

namespace GrpcHost;

internal class Program
{
    private static AIServiceAdapter _aiService;
    private static EngineServiceAdapter _engineService;
    private static GrpcServer _grpcServer;

    static async Task Main()
    {
        Console.WriteLine("GrpcBotProxy starting...");

        _aiService = new AIServiceAdapter("localhost", port: 50000);
        _engineService = new EngineServiceAdapter();
        _grpcServer = new GrpcServer("localhost", port: 49000, _engineService);

        Console.WriteLine("GrpcBotProxy ready. Listening on pipe: GrpcBotPipe");

        while (true)
        {
            var pipe = new NamedPipeServerStream("GrpcBotPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await pipe.WaitForConnectionAsync();
            Console.WriteLine("[Proxy] Pipe connected.");

            try
            {
                await HandleClient(pipe);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Proxy] Fatal error in client: {ex.Message}");
            }
            finally
            {
                pipe.Dispose();
            }
        }
    }

    private static async Task HandleClient(NamedPipeServerStream pipe)
    {
        try
        {
            var lengthBytes = new byte[4];
            int readLength = await pipe.ReadAsync(lengthBytes, 0, 4);

            int length = BitConverter.ToInt32(lengthBytes, 0);

            var buffer = new byte[length];
            int readPayload = await pipe.ReadAsync(buffer, 0, length);

            var cmd = ExtractCommand(buffer);
            Console.WriteLine($"[Pipe] Received command: {cmd}");

            switch (cmd)
            {
                case "Play":
                    await HandlePlay(buffer, pipe);
                    break;

                case "SelectPatron":
                    await HandleSelectPatron(buffer, pipe);
                    break;

                case "PregamePrepare":
                    Console.WriteLine("[AI] PregamePrepare");
                    _aiService.PregamePrepare();
                    var responseBytes = MessagePackSerializer.Serialize(new GenericCommand { Command = "OK" });
                    await pipe.WriteAsync(BitConverter.GetBytes(responseBytes.Length));
                    await pipe.WriteAsync(responseBytes);
                    await pipe.FlushAsync();
                    break;

                case "GameEnd":
                    await HandleGameEnd(buffer, pipe);
                    break;

                case "Register":
                    Console.WriteLine("[AI] Register");
                    var name = _aiService.RegisterBot();
                    var registerResponse = MessagePackSerializer.Serialize(new RegisterResponse { Name = name });
                    await pipe.WriteAsync(BitConverter.GetBytes(registerResponse.Length));
                    await pipe.WriteAsync(registerResponse);
                    await pipe.FlushAsync();
                    break;

                default:
                    Console.WriteLine($"Unknown command: {cmd}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in pipe handler: {ex.Message}");
        }
    }

    private static async Task HandlePlay(byte[] buffer, NamedPipeServerStream pipe)
    {
        var request = MessagePackSerializer.Deserialize<PlayRequest>(buffer);

        var fullState = request.GameState.ToModel();
        Console.WriteLine($"[Play] FullGameState reconstructed. StateId={fullState.StateId}");
        var gameState = new GameState(fullState);
        var legalMoves = request.LegalMoves.Select(m => m.ToModel()).ToList();
        Console.WriteLine($"[Play] Converted {legalMoves.Count} legal moves");

        _engineService.RegisterState(gameState.StateId, gameState);
        _engineService.RegisterMovesList(gameState.StateId, legalMoves);

        var chosen = _aiService.Play(gameState, legalMoves, TimeSpan.FromMilliseconds(request.TimeoutMs));
        var mappedMoveId = UniqueIdMapper.ToUnity(chosen.UniqueId.Value);
        Console.WriteLine($"[Play] AI chose move: {chosen.UniqueId.Value} ({chosen.Command}), mapped to: {mappedMoveId}");
        var response = new PlayResponse { MoveId = mappedMoveId };
        var responseBytes = MessagePackSerializer.Serialize(response);
        await pipe.WriteAsync(BitConverter.GetBytes(responseBytes.Length));
        await pipe.WriteAsync(responseBytes);
        await pipe.FlushAsync();
    }

    private static async Task HandleSelectPatron(byte[] buffer, NamedPipeServerStream pipe)
    {
        var request = MessagePackSerializer.Deserialize<SelectPatronRequest>(buffer);
        Console.WriteLine($"[SelectPatron] Round={request.Round}, Options={string.Join(", ", request.PatronIds)}");
        var patron = _aiService.SelectPatron(request.PatronIds, request.Round);
        Console.WriteLine($"[SelectPatron] Selected: {patron}");
        var response = new SelectPatronResponse { Selected = patron };
        var responseBytes = MessagePackSerializer.Serialize(response);
        await pipe.WriteAsync(BitConverter.GetBytes(responseBytes.Length));
        await pipe.WriteAsync(responseBytes);
        await pipe.FlushAsync();
    }

    private static async Task HandleGameEnd(byte[] buffer, NamedPipeServerStream pipe)
    {
        var request = MessagePackSerializer.Deserialize<GameEndRequest>(buffer);
        _engineService.CleanCache();

        if (request.GameState != null)
        {
            var final = request.GameState.ToModel();
            _aiService.GameEnd(new ScriptsOfTribute.Board.EndGameState(request.Winner, request.Reason,request.AdditionalContext), final);
        }
        var responseBytes = MessagePackSerializer.Serialize(new GenericCommand { Command = "OK" });
        await pipe.WriteAsync(BitConverter.GetBytes(responseBytes.Length));
        await pipe.WriteAsync(responseBytes);
        await pipe.FlushAsync();

        _aiService.CloseConnection();
        _grpcServer?.Dispose();

        pipe.Dispose(); // Propably not needed
        Environment.Exit(0);
    }

    public static string ExtractCommand(byte[] buffer)
    {
        var reader = new MessagePackReader(buffer);

        int arrayLength = reader.ReadArrayHeader();

        if (arrayLength < 1)
            throw new InvalidOperationException("Invalid MessagePack buffer — expected array with at least one element.");

        return reader.ReadString();
    }
}

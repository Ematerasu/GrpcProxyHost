using ScriptsOfTribute;
using ScriptsOfTribute.Board.Cards;

namespace GrpcHost;

public static class UniqueIdMapper
{
    private static Dictionary<int, int> _fromUnityToProxy = new();
    private static Dictionary<int, int> _fromProxyToUnity = new();
    private static Dictionary<int, UniqueCard> _cardRegistry = new();

    public static void Register(int unityId, int proxyId)
    {
        _fromUnityToProxy[unityId] = proxyId;
        _fromProxyToUnity[proxyId] = unityId;
    }

    public static void RegisterCard(int unityId, UniqueCard card)
    {
        _cardRegistry[unityId] = card;
    }

    public static UniqueCard GetCard(int unityId)
    {
        return _cardRegistry[unityId];
    }

    public static int ToProxy(int unityId) => _fromUnityToProxy[unityId];
    public static int ToUnity(int proxyId) => _fromProxyToUnity[proxyId];

    public static void Clear()
    {
        _fromUnityToProxy.Clear();
        _fromProxyToUnity.Clear();
    }

    public static bool TryGetProxy(int unityId, out int proxyId)
    {
        return _fromUnityToProxy.TryGetValue(unityId, out proxyId);
    }

    public static bool TryGetUnity(int proxyId, out int unityId)
    {
        return _fromProxyToUnity.TryGetValue(proxyId, out unityId);
    }
}


using ScriptsOfTribute;
using ScriptsOfTribute.Serializers;

namespace GrpcHost.Models;

public class ProxyPatronStates : PatronStates
{
    public ProxyPatronStates(Dictionary<PatronId, PlayerEnum> fromDto)
            : base(new List<Patron>()) // fake
    {
        typeof(PatronStates)
            .GetField(nameof(All))!
            .SetValue(this, fromDto);
    }

}

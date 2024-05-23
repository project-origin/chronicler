using System.Threading.Tasks;
using Grpc.Core;

namespace ProjectOrigin.Chronicler.Server.Services;

public class ChroniclerService : V1.RegistryService.RegistryServiceBase
{
    public override Task<V1.ClaimIntentResponse> RegisterClaimIntent(V1.ClaimIntentRequest request, ServerCallContext context)
    {
        throw new System.NotImplementedException();
    }
}

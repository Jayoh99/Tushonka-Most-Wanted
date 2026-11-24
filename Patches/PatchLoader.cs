
namespace TarkovMostWanted.Patches;

using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Common.Models.Logging;

[Injectable(TypePriority = OnLoadOrder.Preload)]
public class PatchLoader(
    IEnumerable<IRuntimePatch> patches,
    ISptLogger<PatchLoader> logger)
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        foreach (var patch in patches)
        {
            patch.Enable();
        }

        logger.Info("[Most Wanted] Runtime patches loaded.");

        return Task.CompletedTask;
    }
}


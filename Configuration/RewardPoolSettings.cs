
namespace TarkovMostWanted.Configuration;

using TarkovMostWanted.Bounties;
using RewardItem =
    TarkovMostWanted.Bounties.BountyRewardPools.RewardItem;

public class RewardPoolSettings
{
    public RewardItem[] Pmc { get; set; } =
        BountyRewardPools.Pmc.ToArray();

    public RewardItem[] Scav { get; set; } =
        BountyRewardPools.Pmc.ToArray();

    public RewardItem[] BossStandard { get; set; } =
        BountyRewardPools.BossStandard.ToArray();

    public RewardItem[] BossRare { get; set; } =
        BountyRewardPools.BossRare.ToArray();

    public RewardItem[] BossJackpot { get; set; } =
        BountyRewardPools.BossJackpot.ToArray();
}


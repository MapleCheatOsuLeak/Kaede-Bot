namespace Kaede_Bot.Models.Internal;

public class KudosEvaluationModel
{
    public float ComplexityMultiplier { get; set; }
    public List<KudosUserContributionModel> Contributions { get; set; }
}

public class KudosUserContributionModel
{
    public ulong UserId { get; set; }
    public float ContributionFactor { get; set; }
}

using MegaCrit.Sts2.Core.Models.Cards;
using Characters = MegaCrit.Sts2.Core.Models.Characters;

namespace HideDetailsMod.HideDetailsModCode.AlternateArts.Cards.Token;

public class MindRotArt : AlternateCardArt<MindRot>
{
    static private readonly CardImg RotIronclad = new("token/mind_rot");
    static private readonly CardImg RotSilent = new("token/mind_rot");
    static private readonly CardImg RotRegent = new("token/mind_rot_regent");
    static private readonly CardImg RotNecrobinder = new("token/mind_rot_necrobinder");
    static private readonly CardImg RotDefect = new("token/mind_rot");
    public override CardImg? Get(MindRot card)
    {
        return card.Owner?.Character switch
        {
            Characters.Ironclad => RotIronclad, // TODO: ironclad mind rot
            Characters.Silent => RotSilent, // TODO: silent mind rot
            Characters.Regent => RotRegent,
            Characters.Necrobinder => RotNecrobinder,
            Characters.Defect => RotDefect,
            // returns "token/mind_rot" which is the defect version
            _ => null,
        };
    }
}

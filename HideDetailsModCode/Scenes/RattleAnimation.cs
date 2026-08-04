using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

partial class RattleAnimation : Control
{
    static public AddedNode<NCard, RattleAnimation> Node = new("HideDetailsMod/scenes/cards/Rattle.tscn",
        (card, animation) => animation.SetCard(card));

    //reference to the animation player node
#nullable disable
    AnimationPlayer animation_player;
    NCard card;

#nullable restore
    private CardModel? model;

    // Fetches the card model (ideally for rattle?)
    void SetCard(NCard card)
    {
        this.card = card;
        model = card.Model;

        MainFile.Logger.Info("Card Set!");
    }

    public override void _Ready()
    {
        animation_player = GetNode<AnimationPlayer>("AnimationPlayer");
        
        card.Body.RemoveChildSafely(this);
        card.Body.AddChildSafely(animation_player);
        card._ancientPortrait.AddSiblingSafely(this);
        //Manually replace cost, effect text ect. if not hidden
        
        if (ModConfig.HideTitle(Get()) == true) card.AddChildSafely(card._titleLabel);
        if (Mod)
        

        UpdateModel(model);
        
        MainFile.Logger.Info("RattleAnimation readied!");
    }

    void UpdateModel(CardModel? cardModel)
    {
        model = cardModel;
        if (animation_player is null) return;
        if (model is Rattle)
        {
            Visible = true;
        }
        else
        {
            Visible = false;
            StopAnimation();
        }
    }

    //Allows the rattle animation to loop as many times as osty will hit the taget for.
    public void PlayAndLoopAnimation()
    {
        MainFile.Logger.Info("Playing rattle animation...");
        //TODO: Make Animation loop for each time card hits this turn
        var no_of_hits = ((CalculatedVar)(model.DynamicVars["CalculatedHits"])).Calculate(null);
       
        for (int i = 0; i <= no_of_hits; i++)
        {
            animation_player.Play("rattle_rattling");
        }
    }

    public void StopAnimation()
    {
        if (animation_player.IsPlaying()) animation_player.Stop();
    }

    public override void _Process(double delta)
    {
        if (model != card?.Model) UpdateModel(card?.Model);

        if (!Visible) return;

        if (model?.Pile.Type == PileType.Play) PlayAndLoopAnimation();
    }
}
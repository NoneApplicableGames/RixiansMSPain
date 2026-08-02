using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

partial class RattleAnimation : Control
{
    static public AddedNode<NCard, RattleAnimation> Node = new("res://HideDetailsMod/scenes/cards/rattle.tscn",
        (card, animation) => animation.SetCard(card));


    //reference to the animation player node
#nullable disable
    private AnimationPlayer animation_player;
    private NCard card;

#nullable restore
    private CardModel? model;

    // Fetches the card model (ideally for rattle?)
    void SetCard(NCard card)
    {
        this.card = card;
        model = card.Model;
    }

    public override void _Ready()
    {
        animation_player = GetNode<AnimationPlayer>("AnimationPlayer");

        card.RemoveChildSafely(this);
        card._ancientPortrait.AddSiblingSafely(this);

        UpdateModel(model);
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
    public void LoopAnimation()
    {
        
        //TODO: Make Animation loop for each time card hits this turn
        var no_of_hits = ((CalculatedVar)(model.DynamicVars["CalculatedHits"])).Calculate();
        //for (int i = 0; i <= no_of_hits; i++)
        //{
        animation_player.Play("rattle_rattling");
        //}
    }

    public void StopAnimation()
    {
        if (animation_player.IsPlaying()) animation_player.Stop();
    }

    public override void _Process(double delta)
    {
        if (model != card?.Model) UpdateModel(card?.Model);

        if (!Visible) return;

        switch (model?.Pile?.Type)
        {
            case PileType.Play:
                LoopAnimation();
                break;
            default:
                break;
        }
    }
}
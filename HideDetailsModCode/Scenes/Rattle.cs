using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
namespace HideDetailsMod.HideDetailsModCode.Scenes;

partial class RattleCardModelShake : Control

{
    static public NodeAnimation<NCard, RattleCardModelShake> Node =
        new("C:/Users/User/Desktop/Megadot/RixiansMSPain/HideDetailsMod/scenes/cards/Rattle.tscn");

    CardModel? model;

    void SetCard(NCard card)
    {
        this.card = card;
        model = card.Model;
    }

    void UpdateModel(CardModel? cardModel)
    {
        model = cardModel;
        if (NodeAnimation is null) return;
        if (cardModel is Rattle)
        {
            Visible = true;
        }
        else
        {
            Visible = false;
            Reset();
        }
    }

    void _process()
    {
        if (model != card?.Model) UpdateModel(card?.Model);

        if (!Visible) return;

        switch (model?.Pile?.Type)
        {
            case Piletype.play:
                for (int i =(CalculatedVar)(this.card.DynamicVars["CalculatedHits"].Cal))
                {
                    NodeAnimation.play();
                }
        }
    }
}

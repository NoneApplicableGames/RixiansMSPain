// using System.Reflection;
// using System.Reflection.Emit;
// using BaseLib.Utils;
// using BaseLib.Utils.Patching;
// using Godot;
// using HarmonyLib;
// using MegaCrit.Sts2.Core.Commands;
// using MegaCrit.Sts2.Core.Commands.Builders;
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// using MegaCrit.Sts2.Core.Helpers;
// using MegaCrit.Sts2.Core.Models;
// using MegaCrit.Sts2.Core.Models.Cards;
// using MegaCrit.Sts2.Core.Nodes.Cards;
// using MegaCrit.Sts2.Core.Nodes.Rooms;
// using MegaCrit.Sts2.Core.Nodes.Vfx;

// namespace HideDetailsMod.HideDetailsModCode.Patches;

// // [HarmonyPatch]

// partial class FlattenFlatten : Control
// {
//     static public AddedNode<NCard, FlattenFlatten> _ = new(card => new FlattenFlatten().SetCard(card));
// #nullable disable
//     NCard card;
// #nullable restore
//     CardModel? model;

//     private FlattenFlatten SetCard(NCard card)
//     {
//         this.card = card;
//         model = card.Model;

//         return this;
//     }

//     public override void _Ready()
//     {

//         // animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
//         // animation.AnimationFinished += OnAnimationFinished;

//         // card.RemoveChildSafely(this);
//         // card._ancientPortrait.AddSiblingSafely(this);
//         UpdateModel(model);
//     }

//     public override void _Process(double delta)
//     {
//         if (model != card?.Model) UpdateModel(card?.Model);

//         if (!Visible) return;

//         switch (model?.Pile?.Type)
//         {
//             // case PileType.Exhaust: PlayBackAndForth(); break;
//             case PileType.Play: PlayBackAndForth(); break;
//             default:
//                 Reset();
//                 break;
//         }
//     }

//     private void Reset()
//     {
//         throw new NotImplementedException();
//     }

//     void UpdateModel(CardModel? cardModel)
//     {
//         model = cardModel;
//         if (cardModel is not Flatten) Reset();
//     }
// }
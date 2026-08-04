using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

partial class FlattenAnimation : Control
{
	static public AddedNode<NCard, FlattenAnimation> Node = new("HideDetailsMod/scenes/cards/flatten.tscn",
		(card, animation) => animation.SetCard(card));


	//reference to the animation player node
#nullable disable
	AnimationPlayer animation_player;
	NCard card;

#nullable restore
	private CardModel? model;

	// Fetches the card model (ideally for flatten?)
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

		UpdateModel(model);
		
		MainFile.Logger.Info("Flatten readied!");
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

  

	public void StopAnimation()
	{
		if (animation_player.IsPlaying()) animation_player.Stop();
	}

	public override void _Process(double delta)
	{
		if (model != card?.Model) UpdateModel(card?.Model);

		if (!Visible) return;

		if (model?.Pile.Type == PileType.Play)  animation_player.Play("flatten");
	}
}

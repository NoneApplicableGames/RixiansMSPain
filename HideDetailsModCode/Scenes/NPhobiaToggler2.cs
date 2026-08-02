using Godot;
using MegaCrit.Sts2.Core.Nodes.Animation;
using MegaCrit.Sts2.Core.Saves;

namespace HideDetailsMod.HideDetailsModCode.Scenes;

/// <summary>
/// Will start/stop the animation from an AnimationPlayer depending on if Phobia Mode is on
/// Used in instances where the animation can make people uncomfortable (ie the Infection card overlay)
/// </summary>
partial class NPhobiaToggler2 : NPhobiaAnimationToggler;

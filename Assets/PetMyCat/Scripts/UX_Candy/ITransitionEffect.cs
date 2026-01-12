using System;
using UnityEngine;

namespace UX_Candy
{
/// <summary>
/// Minimal interface for scene-transition cover effects.
/// Implementations should be safe to call repeatedly and should not throw if called while already playing.
/// </summary>
public interface ITransitionEffect
{
    /// <summary>
    /// Duration (seconds) for playing the cover IN (covering the screen).
    /// Used by transition orchestration to know how long to wait.
    /// </summary>
    float InDuration { get; }

    /// <summary>
    /// Duration (seconds) for playing the cover OUT (revealing the screen).
    /// Used by transition orchestration to know how long to wait.
    /// </summary>
    float OutDuration { get; }

    /// <summary>
    /// Puts the effect instantly into the "covered" state (blocking the screen).
    /// </summary>
    void SnapCovered();

    /// <summary>
    /// Puts the effect instantly into the "revealed" state (not blocking the screen).
    /// </summary>
    void SnapRevealed();

    /// <summary>
    /// Plays the cover IN animation.
    /// </summary>
    void PlayIn();

    /// <summary>
    /// Plays the cover OUT animation.
    /// </summary>
    void PlayOut();
}
}

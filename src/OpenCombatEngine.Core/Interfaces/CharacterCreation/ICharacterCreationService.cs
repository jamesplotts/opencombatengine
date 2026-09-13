// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System.Collections.Generic;
using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Core.Interfaces.CharacterCreation
{
    /// <summary>
    /// Selects how much of <see cref="ICharacterCreationService"/>'s question
    /// sequence is actually surfaced to the caller. Mirrors the System
    /// Engine gRPC contract's CharacterCreationMode enum.
    /// </summary>
    public enum CharacterCreationMode
    {
        /// <summary>
        /// Default value, should not be used — the service rejects a
        /// session started with this mode.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// The caller picks race, class, and gender; everything else
        /// (background, ability scores, starting spells) is rolled by
        /// the service itself — fewer questions, not zero.
        /// </summary>
        Quick = 1,

        /// <summary>
        /// Every step is a real, caller-visible prompt.
        /// </summary>
        Detailed = 2
    }

    /// <summary>
    /// One step of an in-progress character-creation conversation — either
    /// the next question to ask, or (once <see cref="Done"/>) the finished
    /// character.
    /// </summary>
    /// <param name="Success">
    /// False for a real rejection (unknown/expired session, an answer that
    /// isn't a real member of the pending prompt's choices) — never thrown
    /// as an exception, since callers (the gRPC service methods) need to
    /// translate this into a response error field.
    /// </param>
    /// <param name="Error">Set when <paramref name="Success"/> is false.</param>
    /// <param name="Done">
    /// True once every question this session's mode requires has been
    /// answered — <paramref name="Character"/> is set, <paramref name="PromptText"/>/
    /// <paramref name="Choices"/> are not.
    /// </param>
    /// <param name="PromptText">Set when <paramref name="Done"/> is false.</param>
    /// <param name="Choices">
    /// Enumerated options the caller picks from; empty means the caller
    /// should collect free text instead (today, the gender prompt), or —
    /// when <paramref name="AbilityScoreRolls"/> is set — that no answer
    /// is expected until the caller acknowledges the reveal.
    /// </param>
    /// <param name="Character">Set only when <paramref name="Done"/> is true.</param>
    /// <param name="AbilityScoreRolls">
    /// Set only on the one prompt where the player just chose "roll 4d6,
    /// drop the lowest" as their ability-score method: the six already-
    /// rolled sets, so a caller can show an interactive per-die reveal
    /// instead of a blind pre-summed total. <paramref name="PromptText"/>
    /// is the roll-intro line; <paramref name="Choices"/> is empty here —
    /// the real next question (assigning each rolled total to an
    /// ability) only arrives once <see cref="ICharacterCreationService.Answer"/>
    /// is called again to acknowledge the reveal (any answer content
    /// works; it's ignored).
    /// </param>
    public record CharacterCreationPrompt(
        bool Success,
        string? Error,
        bool Done,
        string? PromptText,
        IReadOnlyList<string>? Choices,
        CreatureState? Character,
        IReadOnlyList<AbilityScoreRollSet>? AbilityScoreRolls = null);

    /// <summary>
    /// One die from a drop-lowest roll (today, only ability-score
    /// generation's 4d6-drop-lowest uses this) — kept alongside whether
    /// it was the one dropped, rather than omitted, so a caller can show
    /// it struck through alongside the ones that counted.
    /// </summary>
    /// <param name="Value">The face this die landed on.</param>
    /// <param name="Dropped">
    /// True for the single lowest die of the set (ties broken by
    /// original roll order — which specific die is marked doesn't change
    /// the total, only which one a client visually excludes).
    /// </param>
    public record RolledDie(int Value, bool Dropped);

    /// <summary>
    /// One ability score's worth of dice from a drop-lowest roll.
    /// </summary>
    /// <param name="Dice">All 4 dice, in rolled order, including the dropped one.</param>
    /// <param name="Total">The sum of the 3 kept dice (every entry in <paramref name="Dice"/> with <see cref="RolledDie.Dropped"/> false).</param>
    public record AbilityScoreRollSet(IReadOnlyList<RolledDie> Dice, int Total);

    /// <summary>
    /// Runs an interactive, stateful character-creation conversation — the
    /// service owns the entire question sequence (what to ask next given
    /// what's answered so far, when a spellcasting class needs an extra
    /// step, when the character is done), not any caller. Session state is
    /// held in-memory, keyed by a caller-supplied session id — ephemeral by
    /// design; a process restart or an abandoned session loses that
    /// session's in-progress answers.
    /// </summary>
    public interface ICharacterCreationService
    {
        /// <summary>
        /// Begins a new session and returns its first prompt (always:
        /// choose a race).
        /// </summary>
        /// <param name="sessionId">
        /// Caller-supplied session key. Starting a session with an id
        /// already in use replaces the prior session's state.
        /// </param>
        /// <param name="mode">Must not be <see cref="CharacterCreationMode.Unspecified"/>.</param>
        /// <param name="characterName">
        /// The finished character's <see cref="CreatureState.Name"/> —
        /// never asked as a prompt of its own.
        /// </param>
        CharacterCreationPrompt Start(string sessionId, CharacterCreationMode mode, string characterName);

        /// <summary>
        /// Records <paramref name="answer"/> against whatever question
        /// <paramref name="sessionId"/> currently has pending, and returns
        /// either the next prompt or the finished character.
        /// </summary>
        /// <param name="sessionId">Must be a session <see cref="Start"/> returned and that hasn't finished yet.</param>
        /// <param name="answer">
        /// The chosen option's raw value (must be a real member of the
        /// pending prompt's choices), or free text for a prompt with no
        /// enumerated choices.
        /// </param>
        CharacterCreationPrompt Answer(string sessionId, string answer);
    }
}

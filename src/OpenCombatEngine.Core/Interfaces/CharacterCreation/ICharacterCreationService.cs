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
    /// should collect free text instead (today, only the gender prompt).
    /// </param>
    /// <param name="Character">Set only when <paramref name="Done"/> is true.</param>
    public record CharacterCreationPrompt(
        bool Success,
        string? Error,
        bool Done,
        string? PromptText,
        IReadOnlyList<string>? Choices,
        CreatureState? Character);

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

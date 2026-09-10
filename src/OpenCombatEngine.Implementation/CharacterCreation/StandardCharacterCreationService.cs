// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.CharacterCreation;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.States;

namespace OpenCombatEngine.Implementation.CharacterCreation
{
    /// <summary>
    /// Standard implementation of <see cref="ICharacterCreationService"/> —
    /// owns the entire SRD 5.1 level-1 character-creation question
    /// sequence (design doc's "engine owns domain logic, Master is a thin
    /// relay" principle). Session state is a plain in-memory
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/>, ephemeral by
    /// design — see the interface's own doc comment.
    /// </summary>
    /// <remarks>
    /// Deliberate scope cut: skill and saving-throw proficiencies are
    /// computed nowhere here, even though class/background data would let
    /// them be. <see cref="CreatureState"/> has no field to persist them
    /// on today — the only place proficiency lives in this engine
    /// (ProficiencyFeature, part of the bulk 5e.tools class-import/Feature
    /// pipeline) doesn't round-trip through Actor.character_data's JSON
    /// wire format at all, a pre-existing gap this service doesn't attempt
    /// to fix. Ability scores, hit points, armor class, starting
    /// equipment/gold, and spellcasting (for Wizard/Cleric) all have a
    /// real place to live on <see cref="CreatureState"/> and are computed
    /// correctly.
    /// </remarks>
    public class StandardCharacterCreationService : ICharacterCreationService
    {
        private static readonly IReadOnlyList<string> RaceNames = SrdCharacterCreationData.Races.Select(r => r.Name).ToList();
        private static readonly IReadOnlyList<string> ClassNames = SrdCharacterCreationData.Classes.Select(c => c.Name).ToList();
        private static readonly IReadOnlyList<string> BackgroundNames = SrdCharacterCreationData.Backgrounds.Select(b => b.Name).ToList();
        private static readonly IReadOnlyList<string> AbilityScoreMethods = new List<string> { "standard_array", "random_4d6_drop_lowest" };
        // A fixed choice list, not free text — a downstream client renders
        // whatever this service offers, and an unbounded gender box was
        // exactly the wrong control for it.
        private static readonly IReadOnlyList<string> GenderOptions = new List<string> { "Male", "Female" };
        private static readonly IReadOnlyList<int> StandardArray = new List<int> { 15, 14, 13, 12, 10, 8 };
        private static readonly IReadOnlyList<string> AbilityOrder = new List<string> { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };

        private enum Phase { Race, Class, Gender, Background, AbilityMethod, AssignScores, PickCantrips, PickLeveledSpells, Done }

        private sealed class Session
        {
            public CharacterCreationMode Mode;
            public string CharacterName = "Adventurer";
            public string? Race;
            public string? Class;
            public string? Gender;
            public string? Background;
            public string? AbilityScoreMethod;
            public readonly List<int> UnassignedScores = new();
            public readonly Dictionary<string, int> AbilityAssignments = new();
            public readonly List<string> RemainingAbilities = new(AbilityOrder);
            public List<string> CantripOptions = new();
            public readonly List<string> ChosenCantrips = new();
            public List<string> LeveledSpellOptions = new();
            public readonly List<string> ChosenLeveledSpells = new();
            public int LeveledSpellsRemaining;
            public Phase Phase;
        }

        private readonly ConcurrentDictionary<string, Session> _sessions = new();
        private readonly IDiceRoller _diceRoller;
        private readonly ISpellRepository _spellRepository;
        private readonly Random _random = new();

        public StandardCharacterCreationService(IDiceRoller diceRoller, ISpellRepository spellRepository)
        {
            _diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            _spellRepository = spellRepository ?? throw new ArgumentNullException(nameof(spellRepository));
        }

        /// <summary>
        /// Returns className's real cantrip and first-level spell options
        /// from the live spell repository (<see cref="ISpell.Classes"/>),
        /// used both internally (quick-roll's auto-pick, detailed-roll's
        /// actual choice validation) and by the System Engine contract's
        /// own ListClassSpells RPC.
        /// </summary>
        public (IReadOnlyList<string> Cantrips, IReadOnlyList<string> LeveledSpells) ListClassSpells(string className)
        {
            var all = _spellRepository.GetAllSpells()
                .Where(s => s.Classes.Any(c => string.Equals(c, className, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            var cantrips = all.Where(s => s.Level == 0).Select(s => s.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            var leveled = all.Where(s => s.Level == 1).Select(s => s.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            return (cantrips, leveled);
        }

        public CharacterCreationPrompt Start(string sessionId, CharacterCreationMode mode, string characterName)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Fail("session_id is required.");
            if (mode == CharacterCreationMode.Unspecified)
                return Fail("mode is required.");

            var session = new Session
            {
                Mode = mode,
                CharacterName = string.IsNullOrWhiteSpace(characterName) ? "Adventurer" : characterName,
                Phase = Phase.Race,
            };
            _sessions[sessionId] = session;
            return Prompt("Choose your race.", RaceNames);
        }

        public CharacterCreationPrompt Answer(string sessionId, string answer)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return Fail("Unknown or expired session.");
            answer ??= string.Empty;

            switch (session.Phase)
            {
                case Phase.Race:
                    return HandleRace(session, answer);
                case Phase.Class:
                    return HandleClass(session, answer);
                case Phase.Gender:
                    return HandleGender(sessionId, session, answer);
                case Phase.Background:
                    return HandleBackground(session, answer);
                case Phase.AbilityMethod:
                    return HandleAbilityMethod(session, answer);
                case Phase.AssignScores:
                    return HandleAssignScore(sessionId, session, answer);
                case Phase.PickCantrips:
                    return HandlePickCantrip(sessionId, session, answer);
                case Phase.PickLeveledSpells:
                    return HandlePickLeveledSpell(sessionId, session, answer);
                default:
                    return Fail("This session has already finished.");
            }
        }

        private static CharacterCreationPrompt HandleRace(Session session, string answer)
        {
            var race = SrdCharacterCreationData.Races.FirstOrDefault(r => string.Equals(r.Name, answer, StringComparison.OrdinalIgnoreCase));
            if (race is null) return Fail($"'{answer}' is not a valid race. Choose one of: {string.Join(", ", RaceNames)}.");
            session.Race = race.Name;
            session.Phase = Phase.Class;
            return Prompt("Choose your class.", ClassNames);
        }

        private static CharacterCreationPrompt HandleClass(Session session, string answer)
        {
            var srdClass = SrdCharacterCreationData.Classes.FirstOrDefault(c => string.Equals(c.Name, answer, StringComparison.OrdinalIgnoreCase));
            if (srdClass is null) return Fail($"'{answer}' is not a valid class. Choose one of: {string.Join(", ", ClassNames)}.");
            session.Class = srdClass.Name;
            session.Phase = Phase.Gender;
            return Prompt("What is your character's gender?", GenderOptions);
        }

        private CharacterCreationPrompt HandleGender(string sessionId, Session session, string answer)
        {
            var gender = GenderOptions.FirstOrDefault(g => string.Equals(g, answer, StringComparison.OrdinalIgnoreCase));
            if (gender is null)
                return Fail($"'{answer}' is not a valid gender. Choose one of: {string.Join(", ", GenderOptions)}.");
            session.Gender = gender;
            if (session.Mode == CharacterCreationMode.Quick)
            {
                AutoRollRemainingForQuickMode(session);
                return Finish(sessionId, session);
            }
            session.Phase = Phase.Background;
            return Prompt("Choose your background.", BackgroundNames);
        }

        private static CharacterCreationPrompt HandleBackground(Session session, string answer)
        {
            var background = SrdCharacterCreationData.Backgrounds.FirstOrDefault(b => string.Equals(b.Name, answer, StringComparison.OrdinalIgnoreCase));
            if (background is null) return Fail($"'{answer}' is not a valid background. Choose one of: {string.Join(", ", BackgroundNames)}.");
            session.Background = background.Name;
            session.Phase = Phase.AbilityMethod;
            return Prompt("Choose your ability score method.", AbilityScoreMethods);
        }

        private CharacterCreationPrompt HandleAbilityMethod(Session session, string answer)
        {
            if (!AbilityScoreMethods.Contains(answer, StringComparer.OrdinalIgnoreCase))
                return Fail($"'{answer}' is not a valid ability score method. Choose one of: {string.Join(", ", AbilityScoreMethods)}.");
            session.AbilityScoreMethod = answer;
            session.UnassignedScores.AddRange(RollAbilityScores(answer));
            session.Phase = Phase.AssignScores;
            return NextAssignScorePrompt(session);
        }

        private static CharacterCreationPrompt NextAssignScorePrompt(Session session)
        {
            var value = session.UnassignedScores[0];
            return Prompt($"Assign the score {value} to which ability?", session.RemainingAbilities);
        }

        private CharacterCreationPrompt HandleAssignScore(string sessionId, Session session, string answer)
        {
            var ability = session.RemainingAbilities.FirstOrDefault(a => string.Equals(a, answer, StringComparison.OrdinalIgnoreCase));
            if (ability is null) return Fail($"'{answer}' is not a remaining ability. Choose one of: {string.Join(", ", session.RemainingAbilities)}.");

            var value = session.UnassignedScores[0];
            session.UnassignedScores.RemoveAt(0);
            session.AbilityAssignments[ability] = value;
            session.RemainingAbilities.Remove(ability);

            if (session.UnassignedScores.Count > 0)
                return NextAssignScorePrompt(session);

            return AdvancePastScoreAssignment(sessionId, session);
        }

        private CharacterCreationPrompt AdvancePastScoreAssignment(string sessionId, Session session)
        {
            var srdClass = SrdCharacterCreationData.Classes.First(c => c.Name == session.Class);
            if (!srdClass.IsSpellcaster)
            {
                session.Phase = Phase.Done;
                return Finish(sessionId, session);
            }

            var (cantrips, _) = ListClassSpells(srdClass.Name);
            session.CantripOptions = cantrips.ToList();
            session.Phase = Phase.PickCantrips;
            return NextCantripPrompt(session);
        }

        private static CharacterCreationPrompt NextCantripPrompt(Session session) =>
            Prompt($"Choose a cantrip ({session.ChosenCantrips.Count + 1} of 3).", session.CantripOptions);

        private CharacterCreationPrompt HandlePickCantrip(string sessionId, Session session, string answer)
        {
            var cantrip = session.CantripOptions.FirstOrDefault(c => string.Equals(c, answer, StringComparison.OrdinalIgnoreCase));
            if (cantrip is null) return Fail($"'{answer}' is not one of this class's real cantrips.");
            session.ChosenCantrips.Add(cantrip);
            session.CantripOptions.Remove(cantrip);

            if (session.ChosenCantrips.Count < 3)
                return NextCantripPrompt(session);

            var srdClass = SrdCharacterCreationData.Classes.First(c => c.Name == session.Class);
            var (_, leveled) = ListClassSpells(srdClass.Name);
            session.LeveledSpellOptions = leveled.ToList();
            session.LeveledSpellsRemaining = LeveledSpellCountFor(srdClass, session.AbilityAssignments);
            session.Phase = Phase.PickLeveledSpells;
            return NextLeveledSpellPrompt(session);
        }

        private static CharacterCreationPrompt NextLeveledSpellPrompt(Session session)
        {
            var chosen = session.ChosenLeveledSpells.Count;
            return Prompt($"Choose a 1st-level spell ({chosen + 1} of {session.LeveledSpellsRemaining}).", session.LeveledSpellOptions);
        }

        private CharacterCreationPrompt HandlePickLeveledSpell(string sessionId, Session session, string answer)
        {
            var spell = session.LeveledSpellOptions.FirstOrDefault(s => string.Equals(s, answer, StringComparison.OrdinalIgnoreCase));
            if (spell is null) return Fail($"'{answer}' is not one of this class's real 1st-level spells.");
            session.ChosenLeveledSpells.Add(spell);
            session.LeveledSpellOptions.Remove(spell);

            if (session.ChosenLeveledSpells.Count < session.LeveledSpellsRemaining)
                return NextLeveledSpellPrompt(session);

            session.Phase = Phase.Done;
            return Finish(sessionId, session);
        }

        /// <summary>
        /// Quick mode's "the rest is rolled up": fills in every field a
        /// detailed session would still be asking about, reusing the
        /// exact same decision logic (real background, real ability
        /// scores via 4d6-drop-lowest assigned by class priority, real
        /// spell picks chosen randomly from the class's actual list) —
        /// never a shortcut that produces a less-valid character.
        /// </summary>
#pragma warning disable CA5394 // Random is an insecure random number generator — fine for "which of these SRD options to auto-pick," not a security context, same suppression StandardDiceRoller.cs already uses for the same reason.
        private void AutoRollRemainingForQuickMode(Session session)
        {
            session.Background = SrdCharacterCreationData.Backgrounds[_random.Next(SrdCharacterCreationData.Backgrounds.Count)].Name;
            session.AbilityScoreMethod = "random_4d6_drop_lowest";

            var srdClass = SrdCharacterCreationData.Classes.First(c => c.Name == session.Class);
            var scores = RollAbilityScores(session.AbilityScoreMethod).OrderByDescending(v => v).ToList();
            var priority = AbilityPriorityFor(srdClass);
            for (var i = 0; i < priority.Count; i++)
                session.AbilityAssignments[priority[i]] = scores[i];

            if (srdClass.IsSpellcaster)
            {
                var (cantrips, leveled) = ListClassSpells(srdClass.Name);
                var cantripPool = cantrips.ToList();
                for (var i = 0; i < 3 && cantripPool.Count > 0; i++)
                {
                    var pick = cantripPool[_random.Next(cantripPool.Count)];
                    session.ChosenCantrips.Add(pick);
                    cantripPool.Remove(pick);
                }

                var leveledPool = leveled.ToList();
                var need = LeveledSpellCountFor(srdClass, session.AbilityAssignments);
                for (var i = 0; i < need && leveledPool.Count > 0; i++)
                {
                    var pick = leveledPool[_random.Next(leveledPool.Count)];
                    session.ChosenLeveledSpells.Add(pick);
                    leveledPool.Remove(pick);
                }
            }
        }
#pragma warning restore CA5394

        private static List<string> AbilityPriorityFor(SrdClass srdClass)
        {
            // Primary ability first, Constitution always second (every SRD
            // class benefits from more hit points), remaining abilities in
            // a fixed, stable order — a reasonable default assignment for
            // "the rest is rolled up," not an attempt at true SRD-optimal
            // build guidance.
            var primary = srdClass.Name switch
            {
                "Fighter" => "Strength",
                "Wizard" => "Intelligence",
                "Cleric" => "Wisdom",
                "Rogue" => "Dexterity",
                _ => "Strength",
            };
            var order = new List<string> { primary, "Constitution" };
            order.AddRange(AbilityOrder.Where(a => a != primary && a != "Constitution"));
            return order;
        }

        private List<int> RollAbilityScores(string method)
        {
            if (string.Equals(method, "standard_array", StringComparison.OrdinalIgnoreCase))
                return new List<int>(StandardArray);

            // random_4d6_drop_lowest: roll 4d6, drop the lowest die, six
            // times — IDiceRoller has no built-in "drop lowest" helper, so
            // this is built from Roll("4d6")'s own IndividualRolls.
            var scores = new List<int>();
            for (var i = 0; i < 6; i++)
            {
                var result = _diceRoller.Roll("4d6");
                if (!result.IsSuccess)
                {
                    scores.Add(10); // should not happen; a safe SRD-average fallback rather than throwing
                    continue;
                }
                var rolls = result.Value.IndividualRolls.OrderBy(r => r).ToList();
                scores.Add(rolls.Skip(1).Sum()); // drop the lowest of the four
            }
            return scores;
        }

        /// <summary>
        /// SRD's "prepared spells = casting-ability modifier + character
        /// level, minimum 1" formula, applied at level 1 for both
        /// prepared-caster classes this scope supports.
        /// </summary>
        private static int LeveledSpellCountFor(SrdClass srdClass, Dictionary<string, int> abilityAssignments)
        {
            var abilityName = srdClass.SpellcastingAbility.ToString();
            var score = abilityAssignments.TryGetValue(abilityName, out var s) ? s : 10;
            var modifier = AbilityModifier(score);
            return Math.Max(1, modifier + 1); // + character level (always 1 at this scope)
        }

        private static int AbilityModifier(int score) => (int)Math.Floor((score - 10) / 2.0);

        private CharacterCreationPrompt Finish(string sessionId, Session session)
        {
            _sessions.TryRemove(sessionId, out _);

            var race = SrdCharacterCreationData.Races.First(r => r.Name == session.Race);
            var srdClass = SrdCharacterCreationData.Classes.First(c => c.Name == session.Class);
            var background = SrdCharacterCreationData.Backgrounds.First(b => b.Name == session.Background);

            int Score(string ability) => session.AbilityAssignments.TryGetValue(ability, out var v) ? v : 10;
            var strength = Score("Strength") + race.StrengthBonus;
            var dexterity = Score("Dexterity") + race.DexterityBonus;
            var constitution = Score("Constitution") + race.ConstitutionBonus;
            var intelligence = Score("Intelligence") + race.IntelligenceBonus;
            var wisdom = Score("Wisdom") + race.WisdomBonus;
            var charisma = Score("Charisma") + race.CharismaBonus;

            var conModifier = AbilityModifier(constitution);
            var maxHp = srdClass.HitDie + conModifier;
            var dexModifier = AbilityModifier(dexterity);
            var armorClass = 10 + dexModifier;

            var inventoryItems = new Collection<ItemInstanceState>();
            foreach (var item in srdClass.StartingEquipment) inventoryItems.Add(new ItemInstanceState(item));
            foreach (var item in background.StartingEquipment) inventoryItems.Add(new ItemInstanceState(item));

            SpellCasterState? spellcasting = null;
            if (srdClass.IsSpellcaster)
            {
                var knownAndPrepared = new Collection<string>();
                foreach (var c in session.ChosenCantrips) knownAndPrepared.Add(c);
                foreach (var s in session.ChosenLeveledSpells) knownAndPrepared.Add(s);
                spellcasting = new SpellCasterState(
                    CastingAbility: srdClass.SpellcastingAbility,
                    IsPreparedCaster: true,
                    KnownSpellNames: new Collection<string>(knownAndPrepared.ToList()),
                    PreparedSpellNames: new Collection<string>(knownAndPrepared.ToList()),
                    Slots: new Collection<SpellSlotState> { new(Level: 1, Max: 2, Current: 2) },
                    PactSlotsMax: 0,
                    PactSlotsCurrent: 0,
                    PactSlotLevel: 0);
            }

            var character = new CreatureState(
                Id: Guid.NewGuid(),
                Name: session.CharacterName,
                Team: "Player",
                AbilityScores: new AbilityScoresState(strength, dexterity, constitution, intelligence, wisdom, charisma),
                HitPoints: new HitPointsState(maxHp, maxHp, 0),
                CombatStats: new CombatStatsState(armorClass, dexModifier, race.Speed, new Collection<DamageType>(), new Collection<DamageType>(), new Collection<DamageType>()),
                LevelManager: new LevelManagerState(0, new Collection<ClassLevelState> { new(srdClass.Name, 1, srdClass.HitDie) }),
                ActionEconomy: new ActionEconomyState(true, true, true),
                Inventory: new InventoryState(inventoryItems, Gold: background.StartingGoldPieces),
                Spellcasting: spellcasting,
                Gender: session.Gender,
                RaceName: session.Race);

            return new CharacterCreationPrompt(true, null, true, null, null, character);
        }

        private static CharacterCreationPrompt Prompt(string text, IReadOnlyList<string> choices) =>
            new(true, null, false, text, choices, null);

        private static CharacterCreationPrompt Fail(string error) =>
            new(false, error, false, null, null, null);
    }
}

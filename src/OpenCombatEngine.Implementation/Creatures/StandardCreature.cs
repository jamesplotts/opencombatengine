using System;
using System.Collections.Generic;
using System.Linq;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Dice;
using OpenCombatEngine.Core.Interfaces.Effects;
using OpenCombatEngine.Core.Interfaces.Features;
using OpenCombatEngine.Core.Interfaces.Items;
using OpenCombatEngine.Core.Interfaces.Races;
using OpenCombatEngine.Core.Interfaces.Spatial;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Models.Combat;
using OpenCombatEngine.Core.Models.States;
using OpenCombatEngine.Implementation.Conditions;
using OpenCombatEngine.Implementation.Dice;
using OpenCombatEngine.Implementation.Effects;
using OpenCombatEngine.Implementation.Items;
using OpenCombatEngine.Implementation.Spells;

namespace OpenCombatEngine.Implementation.Creatures
{
    public class StandardCreature : ICreature, IStateful<CreatureState>
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Team { get; set; } = "Player";
        public OpenCombatEngine.Core.Interfaces.Races.IRaceDefinition? Race { get; }
        
        public IAbilityScores AbilityScores { get; }
        public IHitPoints HitPoints { get; }
        public ICombatStats CombatStats { get; }
        public IConditionManager Conditions { get; }
        public IActionEconomy ActionEconomy { get; }
        public IEffectManager Effects { get; }
        public IMovement Movement { get; }

        public EncumbranceLevel EncumbranceLevel
        {
            get
            {
                var str = AbilityScores.Strength;
                var weight = Inventory.TotalWeight;

                if (weight > str * 15) return EncumbranceLevel.OverCapacity;
                if (weight > str * 10) return EncumbranceLevel.HeavilyEncumbered;
                if (weight > str * 5) return EncumbranceLevel.Encumbered;
                return EncumbranceLevel.None;
            }
        }

        public ICheckManager Checks { get; }
        public IInventory Inventory { get; }
        public IEquipmentManager Equipment { get; }
        public ISpellCaster? Spellcasting { get; }
        public ITurnManager TurnManager { get; }
        
        public ILevelManager LevelManager { get; }
        public IDictionary<string, int> Senses { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly List<IFeature> _features = new();
        
        public int ProficiencyBonus => LevelManager.ProficiencyBonus;

        public StandardCreature(
            string id,
            string name,
            IAbilityScores abilityScores,
            IHitPoints hitPoints,
            IInventory inventory,
            ITurnManager turnManager,
            IConditionManager? conditions = null,
            IActionEconomy? actionEconomy = null,
            IMovement? movement = null,
            ICheckManager? checkManager = null,
            IEquipmentManager? equipmentManager = null,
            ISpellCaster? spellcasting = null,
            IEffectManager? effectManager = null,
            OpenCombatEngine.Core.Interfaces.Races.IRaceDefinition? race = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id cannot be empty", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            
            Id = Guid.Parse(id);
            Name = name;

            AbilityScores = abilityScores ?? throw new ArgumentNullException(nameof(abilityScores));
            HitPoints = hitPoints ?? throw new ArgumentNullException(nameof(hitPoints));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            TurnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
            
            // Initialize components
            // Create Equipment first so CombatStats can use it
            Equipment = equipmentManager ?? new StandardEquipmentManager(this);
            
            // Conditions needs 'this', so we create it here if null
            Conditions = conditions ?? new StandardConditionManager(this);
            
            CombatStats = new StandardCombatStats(equipment: Equipment, abilities: AbilityScores);

            if (effectManager == null)
            {
                Effects = new StandardEffectManager(this);
            }
            else
            {
                Effects = effectManager;
            }
            CombatStats.SetEffectManager(Effects);

            ActionEconomy = actionEconomy ?? new StandardActionEconomy();
            
            if (movement == null)
            {
                var stdMove = new StandardMovement(CombatStats, Conditions);
                stdMove.Creature = this;
                Movement = stdMove;
            }
            else
            {
                Movement = movement;
                if (Movement is StandardMovement stdMove)
                {
                    stdMove.Creature = this;
                }
            }

            Checks = checkManager ?? new StandardCheckManager(AbilityScores, new StandardDiceRoller(), this);
            
            LevelManager = new StandardLevelManager(this);

            Race = race;
            if (Race != null)
            {
                foreach (var feature in Race.RacialFeatures)
                {
                    AddFeature(feature);
                }
                // Apply Ability Score Increases
                foreach (var kvp in Race.AbilityScoreIncreases)
                {
                    // TODO: Apply ASI. StandardAbilityScores needs to support modification.
                }
            }

            // Default to Intelligence if creating new caster
            Spellcasting = spellcasting ?? new StandardSpellCaster(AbilityScores, ProficiencyBonus, Ability.Intelligence);

            // Subscribe to events
            HitPoints.Died += OnDied;
            
            if (Spellcasting is StandardSpellCaster stdSpellcaster)
            {
                stdSpellcaster.SetEffectManager(Effects);
            }
            
            HitPoints.DamageTaken += OnDamageTaken;
        }

        public StandardCreature(CreatureState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            Id = state.Id;
            Name = state.Name;
            Team = "Neutral";
            AbilityScores = new StandardAbilityScores(state.AbilityScores);
            
            Inventory = new StandardInventory();
            Conditions = state.Conditions != null 
                ? new StandardConditionManager(this, state.Conditions) 
                : new StandardConditionManager(this);
            
            // For state restoration, we might not have equipment set up yet in the state object in a way that CombatStats can use directly?
            // StandardCombatStats(state) uses values from state.
            // We need to pass AbilityScores and Equipment (which we create later).
            // But Equipment needs 'this'.
            // So we create Equipment first.
            Equipment = new StandardEquipmentManager(this); // Pass 'this'
            
            CombatStats = state.CombatStats != null 
                ? new StandardCombatStats(state.CombatStats, Equipment, AbilityScores) 
                : new StandardCombatStats(abilities: AbilityScores, equipment: Equipment); // Fallback

            HitPoints = new StandardHitPoints(state.HitPoints, CombatStats);
            
            TurnManager = new StandardTurnManager(new StandardDiceRoller());
            ActionEconomy = new StandardActionEconomy();
            
            var stdMove = new StandardMovement(CombatStats, Conditions);
            stdMove.Creature = this;
            Movement = stdMove;

            Effects = new StandardEffectManager(this);
            CombatStats.SetEffectManager(Effects);
            
            Checks = new StandardCheckManager(AbilityScores, new StandardDiceRoller(), this);
            // Equipment already created above
            Spellcasting = null; 

            LevelManager = new StandardLevelManager(this);
            // TODO: Restore LevelManager state if we had it. For now, it starts fresh (Level 0/1?).
            // We need to add LevelManager state to CreatureState later.

            HitPoints.Died += OnDied;
            HitPoints.DamageTaken += OnDamageTaken;
        }

        private void OnDied(object? sender, EventArgs e)
        {
            // Death logic
        }

        private void OnDamageTaken(object? sender, OpenCombatEngine.Core.Models.Events.DamageTakenEventArgs e)
        {
            if (Spellcasting?.ConcentratingOn != null)
            {
                int dc = Math.Max(10, e.Amount / 2);
                var saveResult = Checks.RollSavingThrow(Ability.Constitution);
                if (saveResult.IsSuccess && saveResult.Value < dc)
                {
                    Spellcasting.BreakConcentration();
                }
            }
        }

        public void StartTurn()
        {
            ActionEconomy.ResetTurn();
            Movement.ResetTurn();
            Conditions.Tick();
            Effects.Tick();
            
            foreach (var feature in _features)
            {
                feature.OnStartTurn(this);
            }
            
            if (HitPoints.Current <= 0 && !HitPoints.IsDead && !HitPoints.IsStable)
            {
                var rollResult = Checks.RollDeathSave();
                if (rollResult.IsSuccess)
                {
                    int roll = rollResult.Value;
                    if (roll == 20) HitPoints.Heal(1);
                    else if (roll == 1) HitPoints.RecordDeathSave(false, critical: true);
                    else if (roll >= 10) HitPoints.RecordDeathSave(true);
                    else HitPoints.RecordDeathSave(false);
                }
            }
        }

        public void EndTurn()
        {
        }

        public void Rest(RestType type, int hitDiceToSpend = 0)
        {
            if (type == RestType.ShortRest)
            {
                if (hitDiceToSpend > 0)
                {
                    var result = HitPoints.UseHitDice(hitDiceToSpend);
                    if (result.IsSuccess)
                    {
                        int conMod = AbilityScores.GetModifier(Ability.Constitution);
                        int totalHealing = result.Value + (conMod * hitDiceToSpend);
                        HitPoints.Heal(totalHealing);
                    }
                }
            }
            else if (type == RestType.LongRest)
            {
                HitPoints.Heal(HitPoints.Max);
                HitPoints.RecoverHitDice(HitPoints.HitDiceTotal / 2);
            }
        }

        public CreatureState GetState()
        {
             var abilityState = (AbilityScores as IStateful<AbilityScoresState>)?.GetState() 
                ?? throw new InvalidOperationException("AbilityScores component does not support state export");
                
            var hpState = (HitPoints as IStateful<HitPointsState>)?.GetState()
                ?? throw new InvalidOperationException("HitPoints component does not support state export");

            var combatState = (CombatStats as IStateful<CombatStatsState>)?.GetState()
                ?? throw new InvalidOperationException("CombatStats component does not support state export");

            var conditionState = (Conditions as IStateful<ConditionManagerState>)?.GetState()
                ?? throw new InvalidOperationException("Conditions component does not support state export");

            return new CreatureState(Id, Name, abilityState, hpState, combatState, conditionState);
        }

        public void AddFeature(IFeature feature)
        {
            ArgumentNullException.ThrowIfNull(feature);
            _features.Add(feature);
            feature.OnApplied(this);
        }

        public void RemoveFeature(IFeature feature)
        {
            ArgumentNullException.ThrowIfNull(feature);
            if (_features.Remove(feature))
            {
                feature.OnRemoved(this);
            }
        }

        public void ModifyOutgoingAttack(AttackResult attack)
        {
            ArgumentNullException.ThrowIfNull(attack);

            if (Effects != null)
            {
                attack.AttackRoll = Effects.ApplyStatBonuses(StatType.AttackRoll, attack.AttackRoll);
                int damageBonus = Effects.ApplyStatBonuses(StatType.DamageRoll, 0);
                if (damageBonus > 0)
                {
                    var type = attack.Damage.Count > 0 ? attack.Damage[0].Type : DamageType.Force;
                    attack.AddDamage(new DamageRoll(damageBonus, type));
                }
            }

            foreach (var feature in _features)
            {
                feature.OnOutgoingAttack(this, attack);
            }
        }

        public AttackOutcome ResolveAttack(AttackResult attack)
        {
            ArgumentNullException.ThrowIfNull(attack);

            var targetAC = CombatStats.ArmorClass;
            
            if (attack.TargetCover == CoverType.Half) targetAC += 2;
            else if (attack.TargetCover == CoverType.ThreeQuarters) targetAC += 5;
            else if (attack.TargetCover == CoverType.Total)
            {
                return new AttackOutcome(false, 0, "Target has Total Cover.");
            }

            // Obscurement Logic:
            // If target is Heavily Obscured, they are effectively invisible to the attacker (unless attacker has special senses).
            // Attack rolls against an invisible target have Disadvantage.
            // We don't have a direct "Disadvantage" flag on AttackResult yet, but we can simulate it or note it.
            // For now, let's just log it or ensure the roll reflects it (caller responsibility to roll with disadvantage?).
            // Ideally, the caller (CombatManager or Action) checks Obscurement before rolling.
            // But if we are resolving here, the roll is already done.
            // So we can't force a reroll here easily without changing the flow.
            // We will assume the caller handled the Disadvantage on the roll itself if they knew about the obscurement.
            // However, if the target is heavily obscured and the attacker blindly attacks, we might want to impose a flat miss chance or check.
            // For this abstract cycle, we'll trust the input roll but maybe add a note to the outcome if obscured.
            
            if (attack.TargetObscurement == ObscurementType.Heavily)
            {
                // Optional: Fail automatically if attacker relies on sight? 
                // For now, just proceed.
            }

            bool isHit = attack.AttackRoll >= targetAC || attack.IsCritical;
            
            if (!isHit)
            {
                return new AttackOutcome(false, 0, "Missed.");
            }

            int totalDamage = 0;
            foreach (var damageRoll in attack.Damage)
            {
                int amount = damageRoll.Amount;
                
                if (CombatStats.Immunities.Contains(damageRoll.Type))
                {
                    amount = 0;
                }
                else
                {
                    if (CombatStats.Resistances.Contains(damageRoll.Type)) amount /= 2;
                    if (CombatStats.Vulnerabilities.Contains(damageRoll.Type)) amount *= 2;
                }
                
                if (amount > 0)
                {
                    HitPoints.TakeDamage(amount, damageRoll.Type);
                    totalDamage += amount;
                }
            }

            string message = isHit ? $"Hit for {totalDamage} damage!" : "Missed!";
            if (attack.IsCritical) message = $"Critical Hit! {totalDamage} damage!"; 

            return new AttackOutcome(true, totalDamage, message);
        }

        private readonly List<OpenCombatEngine.Core.Interfaces.Actions.IAction> _customActions = new();

        public void AddAction(OpenCombatEngine.Core.Interfaces.Actions.IAction action)
        {
            _customActions.Add(action);
        }

        public void RemoveAction(OpenCombatEngine.Core.Interfaces.Actions.IAction action)
        {
            _customActions.Remove(action);
        }

        public IEnumerable<OpenCombatEngine.Core.Interfaces.Actions.IAction> Actions
        {
            get
            {
                // Move Action
                int speed = Movement.Speed;
                yield return new OpenCombatEngine.Implementation.Actions.MoveAction(speed);

                // Unarmed Strike
                var strMod = AbilityScores.GetModifier(Ability.Strength);
                var proficiency = ProficiencyBonus;
                var attackBonus = strMod + proficiency;
                var damageBonus = strMod;
                var damageDice = "1";
                var diceRoller = new StandardDiceRoller();

            yield return new OpenCombatEngine.Implementation.Actions.AttackAction(
                "Unarmed Strike",
                "Punch, kick, or headbutt.",
                attackBonus,
                damageDice,
                DamageType.Bludgeoning,
                damageBonus,
                diceRoller
            );
            
            // Spells
            if (Spellcasting != null)
            {
                foreach (var spell in Spellcasting.KnownSpells)
                {
                    yield return new OpenCombatEngine.Implementation.Actions.CastSpellAction(spell);
                }
            }

            // Custom actions
            foreach (var action in _customActions)
            {
                yield return action;
            }

            // Magic Item Actions
            // Check Equipment
            foreach (var item in Equipment.GetEquippedItems())
            {
                if (item is IMagicItem magicItem)
                {
                    foreach (var ability in magicItem.Abilities)
                    {
                        yield return new OpenCombatEngine.Implementation.Actions.UseMagicItemAction(magicItem, ability);
                    }
                }
            }
            }
        }
    }
}

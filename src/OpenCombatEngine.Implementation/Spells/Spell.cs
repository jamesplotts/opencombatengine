using System;
using OpenCombatEngine.Core.Enums;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Interfaces.Spells;
using OpenCombatEngine.Core.Results;

namespace OpenCombatEngine.Implementation.Spells
{
    public class Spell : ISpell
    {
        public string Name { get; }
        public int Level { get; }
        public SpellSchool School { get; }
        public string CastingTime { get; }
        public string Range { get; }
        public string Components { get; }
        public string Duration { get; }
        public string Description { get; }

        private readonly Func<ICreature, object?, Result<bool>> _effect;

        public Spell(
            string name, 
            int level, 
            SpellSchool school, 
            string castingTime, 
            string range, 
            string components, 
            string duration, 
            string description,
            Func<ICreature, object?, Result<bool>> effect)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name cannot be empty", nameof(name));
            if (level < 0 || level > 9) throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 0 and 9");
            
            Name = name;
            Level = level;
            School = school;
            CastingTime = castingTime;
            Range = range;
            Components = components;
            Duration = duration;
            Description = description;
            _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }

        public Result<bool> Cast(ICreature caster, object? target = null)
        {
            if (caster == null) return Result<bool>.Failure("Caster cannot be null.");
            return _effect(caster, target);
        }
    }
}

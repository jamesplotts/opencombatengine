using System;

namespace OpenCombatEngine.Core.Interfaces.Items
{
    public interface IItem
    {
        Guid Id { get; }
        string Name { get; }
        string Description { get; }
        double Weight { get; }
        int Value { get; }
    }
}

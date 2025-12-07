using System;
using OpenCombatEngine.Core.Interfaces.Conditions;
using OpenCombatEngine.Core.Interfaces.Creatures;
using OpenCombatEngine.Core.Models.Spatial;

namespace OpenCombatEngine.Core.Models.Events
{
    public class MovedEventArgs : EventArgs
    {
        public Position From { get; }
        public Position To { get; }

        public MovedEventArgs(Position from, Position to)
        {
            From = from;
            To = to;
        }
    }

    public class ConditionEventArgs : EventArgs
    {
        public ICondition Condition { get; }

        public ConditionEventArgs(ICondition condition)
        {
            Condition = condition;
        }
    }

    public class ActionEventArgs : EventArgs
    {
        public string ActionName { get; }
        public object? Target { get; }

        public ActionEventArgs(string actionName, object? target)
        {
            ActionName = actionName;
            Target = target;
        }
    }
}

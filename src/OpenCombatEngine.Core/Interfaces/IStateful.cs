namespace OpenCombatEngine.Core.Interfaces
{
    /// <summary>
    /// Defines an object that can export its state as a serializable DTO.
    /// </summary>
    /// <typeparam name="TState">The type of the state DTO.</typeparam>
    public interface IStateful<out TState>
    {
        /// <summary>
        /// Gets the current state of the object as a serializable DTO.
        /// </summary>
        /// <returns>The state DTO.</returns>
        TState GetState();
    }
}

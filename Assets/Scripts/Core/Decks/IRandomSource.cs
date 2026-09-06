namespace Graphaclysm.Core.Decks
{
    /// <summary>
    /// Small deterministic random abstraction. A run can persist the state as its seed later.
    /// </summary>
    public interface IRandomSource
    {
        int Next(int exclusiveMaximum);
    }
}

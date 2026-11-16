namespace MassFixStructure.Core
{
    public enum ActivationMode
    {
        /// <summary>
        /// Mass repair activates automatically on every hammer repair click.
        /// </summary>
        AlwaysOnRepair,

        /// <summary>
        /// Mass repair activates only when holding down the configured modifier key (e.g. LeftShift).
        /// Otherwise vanilla single-piece repair runs.
        /// </summary>
        HoldModifierKey
    }
}

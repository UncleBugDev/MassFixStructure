namespace MassFixStructure.Core
{
    public enum PlaySoundMode
    {
        /// <summary>
        /// Plays the standard repair hit sound and dust VFX once at the origin piece.
        /// </summary>
        SingleAtOrigin,

        /// <summary>
        /// Completely silences repair sound and particle effects.
        /// </summary>
        Muted
    }
}

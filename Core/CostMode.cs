namespace MassFixStructure.Core
{
    public enum CostMode
    {
        /// <summary>
        /// Standard vanilla cost for a single hammer swing, regardless of how many pieces are repaired.
        /// </summary>
        VanillaPerSwing,

        /// <summary>
        /// Base swing cost plus an additional scaled percentage for each piece repaired beyond the first.
        /// </summary>
        PerPieceScaled,

        /// <summary>
        /// No stamina and no hammer durability consumed.
        /// </summary>
        Free
    }
}

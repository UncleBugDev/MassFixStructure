namespace MassFixStructure.Core
{
    public enum RepairMode
    {
        /// <summary>
        /// BFS graph traversal along physically touching / adjacent structural pieces.
        /// </summary>
        Connected,

        /// <summary>
        /// BFS graph traversal, but restricted to pieces belonging to the same category (e.g. Building, Furniture).
        /// </summary>
        ConnectedSameCategory,

        /// <summary>
        /// All pieces within SearchRadius from the targeted piece.
        /// </summary>
        Radius
    }
}

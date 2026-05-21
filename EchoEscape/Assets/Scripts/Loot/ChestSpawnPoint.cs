using UnityEngine;

namespace EchoEscape
{
    /// <summary>
    /// Marks a possible location where the game manager can spawn a random chest.
    /// </summary>
    /// <remarks>
    /// Attach this empty marker script to scene objects placed in optional reward routes.
    /// EchoEscapeGameManager searches for these markers at scene start and chooses some of them for chest placement.
    /// </remarks>
    public class ChestSpawnPoint : MonoBehaviour
    {
    }
}

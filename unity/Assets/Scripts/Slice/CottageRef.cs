using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Saved on each cottage prefab so the runtime can rebuild its record (plan, front door) in a baked scene.</summary>
    public class CottageRef : MonoBehaviour
    {
        public string plotId;
        public Vector3 frontDoorLocal, frontOutLocal = Vector3.back;
    }


}

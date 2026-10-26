using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Burrow : MonoBehaviour
{
    public Vector2 spawnRangeSize = new Vector2(10, 5); // зјБъ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = transform.position;
        Vector3 topLeft = center - new Vector3(spawnRangeSize.x / 2, spawnRangeSize.y / 2, 0);
        Vector3 bottomRight = center + new Vector3(spawnRangeSize.x / 2, -spawnRangeSize.y / 2, 0);
        Gizmos.DrawLine(topLeft, new Vector3(bottomRight.x, topLeft.y, topLeft.z)); // ????
        Gizmos.DrawLine(topLeft, new Vector3(topLeft.x, bottomRight.y, topLeft.z)); // ???
        Gizmos.DrawLine(bottomRight, new Vector3(topLeft.x, bottomRight.y, bottomRight.z)); // ???
        Gizmos.DrawLine(bottomRight, new Vector3(bottomRight.x, topLeft.y, bottomRight.z)); // ???
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonTile : MonoBehaviour
{
    public List<Vector3Int> exits;
    public Vector3Int roomSize;
    public int tileSize;
    [Tooltip("How many tiles the corner of the room is removed from the center of the object")]
    public Vector3 tileRoomOffset;

    [Header("Debug")]
    public Vector3 moveToMiddle;

    public void OnDrawGizmosSelected()
    {
        foreach(Vector3Int exit in exits)
        {
            //Object position
            //move to middle because it isn't always in the middle of the room
            //subtract half the room size to move it to generate dungeon origin of bottom left not center
            //Add exit location multiplied by tile size
            //Add half a tile to align with middle of room
            Gizmos.DrawSphere(this.transform.localPosition + moveToMiddle - ((Vector3)roomSize * tileSize / 2) + (exit * tileSize) + (Vector3.one * tileSize / 2), 0.5f);
            Gizmos.DrawWireSphere(this.transform.localPosition + moveToMiddle, 0.5f);

        }
    }
}

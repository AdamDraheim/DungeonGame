using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GenerateDungeon : MonoBehaviour
{

    [Serializable]
    public struct DungeonRooms
    {
        [SerializeField]
        public DungeonTile room;
        [SerializeField]
        public int minRequired;
        [SerializeField]
        public int maxRequired;
        [SerializeField]
        public int[] allowedFloors;
        [SerializeField]
        public string identifier;
    }

    [Header("Dungeon Tiles")]
    public List<DungeonRooms> rooms;
    public DungeonTile straight;
    public DungeonTile Left;
    public DungeonTile Right;
    public DungeonTile T_Shape;
    public DungeonTile plusShape;
    public DungeonTile endCap;
    public DungeonTile stairs;
    public int tileSize;

    [Header("Dungeon Attributes")]
    public Vector3Int dungeonSize;
    public int[] minRoomsToUse;
    public int[] maxRoomsToUse;
    public int[] numStairsPerFloor;
    public int roomBorder;

    [Header("Anthill")]
    [Range(0, 1.0f)]
    public float threshold;
    public float distanceExp;
    public float pheroExp;
    public int numAnts;
    public int numIters;

    [Header("Debug")]
    public bool useSeed;
    public int seed;

    private int[] numRoomsPerFloor;
    private bool[,,] used;
    private bool[,,] roomWithBoundaries;
    private bool[,,] roomBounds;

    private float[,,] distance;
    private float[,] paths;
    private Dictionary<int, Vector3Int>[] matrixOfLocations;
    private Dictionary<string, int> requiredCount;

    private Dictionary<Vector3Int, DungeonTile> stairRooms;

    private List<DungeonTile> assignments;
    private List<Vector3Int> unused;

    private Dictionary<Vector3Int, DungeonRooms> activeRooms;
    private AntHill[] anthill;

    private void Awake()
    {

        if(maxRoomsToUse.Length != dungeonSize.y || minRoomsToUse.Length != minRoomsToUse.Length)
        {
            Debug.LogError("Generate Dungeon Error: max and min rooms for each floor must be specified");
            return;
        }

        if(useSeed)
            UnityEngine.Random.InitState(seed);

        for (int y = 0; y < dungeonSize.y; y++)
        {
            if (maxRoomsToUse[y] < minRoomsToUse[y])
            {
                int temp = minRoomsToUse[y];
                minRoomsToUse[y] = maxRoomsToUse[y];
                maxRoomsToUse[y] = temp;
            }
        }

        used = new bool[dungeonSize.x, dungeonSize.y, dungeonSize.z];
        roomWithBoundaries = new bool[dungeonSize.x, dungeonSize.y, dungeonSize.z];
        roomBounds = new bool[dungeonSize.x, dungeonSize.y, dungeonSize.z];
        numRoomsPerFloor = new int[dungeonSize.y];

        requiredCount = new Dictionary<string, int>();
        activeRooms = new Dictionary<Vector3Int, DungeonRooms>();
        matrixOfLocations = new Dictionary<int, Vector3Int>[dungeonSize.y];

        for(int y = 0; y < dungeonSize.y; y++)
        {
            matrixOfLocations[y] = new Dictionary<int, Vector3Int>();
        }
        
        unused = new List<Vector3Int>();

        for(int x = 0; x < dungeonSize.x; x++)
        {
            for(int y = 0; y < dungeonSize.y; y++)
            {
                for(int z = 0; z < dungeonSize.z; z++)
                {
                    unused.Add(new Vector3Int(x, y, z));
                }
            }
        }

        PlaceRequiredRoomsInEmpty();
        for (int y = 0; y < dungeonSize.y; y++)
            PlaceRoomsInDungeon(y);

        foreach (Vector3Int pos in activeRooms.Keys)
        {
            DungeonTile obj = Instantiate(activeRooms[pos].room, this.transform);
            obj.transform.localPosition = (pos - obj.tileRoomOffset) * tileSize;
        }

        PlaceStairs();
        CalculateRoomDistances();
        CalculateBestPaths();
        PlaceCorridors();
    }

    private bool PlaceRoomsInDungeon(int y)
    {


        int successes = numRoomsPerFloor[y];
        for(int i = 0; i < maxRoomsToUse[y]; i++)
        {

            bool succeeded = PlaceRoomInEmpty(y);
            if (!succeeded && successes < minRoomsToUse[y])
            {
                Debug.LogWarning("Generate Dungeon: Failed to place " + minRoomsToUse[y] + " rooms on floor " + y);
                return false;
            }
            else
            {
                successes++;
            }
        }

        return true;
    }

    private void PlaceRequiredRoomsInEmpty()
    {
        List<DungeonRooms> required = new List<DungeonRooms>();
        foreach (DungeonRooms requiredRoomCheck in rooms)
        {
            for (int idx = 0; idx < requiredRoomCheck.minRequired; idx++)
            {
                required.Add(requiredRoomCheck);
            }
        }

        foreach (DungeonRooms room in required) {
            Vector3Int randPos;
            int numTries = 5;

            while (numTries > 0)
            {
                randPos = new Vector3Int(0, 0, 0);
                while (!(randPos.x >= roomBorder && randPos.z >= roomBorder &&
                    randPos.x < dungeonSize.x - roomBorder && randPos.z < dungeonSize.z - roomBorder)
                    || (room.allowedFloors.Length > 0 && !room.allowedFloors.Contains<int>(randPos.y)))
                {
                    randPos = unused[UnityEngine.Random.Range(0, unused.Count)];
                }

                if (checkRoomFitsAndAssign(randPos, room.room.roomSize))
                {
                    foreach (Vector3Int exit in room.room.exits)
                    {
                        matrixOfLocations[randPos.y + exit.y].Add(matrixOfLocations[randPos.y].Count, randPos);
                    }
                    activeRooms.Add(randPos, room);
                    numRoomsPerFloor[randPos.y]++;

                    if (!requiredCount.ContainsKey(room.identifier))
                    {
                        requiredCount.Add(room.identifier, 0);
                    }
                    requiredCount[room.identifier]++;

                    break;
                }
                numTries--;
                
            }
        }
    }

    private void PlaceStairs()
    {
        stairRooms = new Dictionary<Vector3Int, DungeonTile>();
        for(int y = 0; y < dungeonSize.y - 1; y++){

            for (int num = 0; num < numStairsPerFloor[y]; num++)
            {
                Vector3Int randPos;
                int numTries = 20;

                while (numTries > 0)
                {
                    randPos = new Vector3Int(0, 0, 0);
                    while (!(randPos.x >= roomBorder && randPos.z >= roomBorder &&
                        randPos.x < dungeonSize.x - roomBorder && randPos.z < dungeonSize.z - roomBorder)
                        || (y != randPos.y))
                    {
                        randPos = unused[UnityEngine.Random.Range(0, unused.Count)];
                        randPos = new Vector3Int(randPos.x, y, randPos.z);
                    }

                    if (checkRoomFitsAndAssign(randPos, stairs.roomSize))
                    {
                        matrixOfLocations[y].Add(matrixOfLocations[y].Count, randPos);
                        matrixOfLocations[y + 1].Add(matrixOfLocations[y + 1].Count, randPos);

                        stairRooms.Add(randPos, stairs);
                        break;
                    }
                    numTries--;
                }
            }
        }

        foreach (Vector3Int pos in stairRooms.Keys)
        {
            DungeonTile obj = Instantiate(stairRooms[pos], this.transform);
            obj.transform.localPosition = (pos - obj.tileRoomOffset) * tileSize;
        }


    }

    private bool PlaceRoomInEmpty(int y)
    {
        Vector3Int randPos;
        int numTries = 5;

        while (numTries > 0)
        {
            //Get random position in grid for room
            randPos = new Vector3Int(0, 0, 0);

            List<Vector3Int> unusedOnFloor = new List<Vector3Int>();
            foreach(Vector3Int u in unused)
            {
                if (u.y == y) unusedOnFloor.Add(u);
            }

            while (!(randPos.x >= roomBorder && randPos.z >= roomBorder &&
                randPos.x < dungeonSize.x - roomBorder && randPos.z < dungeonSize.z - roomBorder))
            {
                randPos = unusedOnFloor[UnityEngine.Random.Range(0, unusedOnFloor.Count)];
            }

            bool[] checkRooms = new bool[rooms.Count];
            List<int> roomNums = new List<int>();
            for (int i = 0; i < rooms.Count; i++) 
            {
                //Ensure that there is at most a certain number of the specified rooms
                if (!requiredCount.ContainsKey(rooms[i].identifier) || requiredCount[rooms[i].identifier] < rooms[i].maxRequired || rooms[i].maxRequired == -1)
                    roomNums.Add(i); 
            }

            // Get a random room type and remove it so not checked again
            int room = roomNums[UnityEngine.Random.Range(0, roomNums.Count)];
            roomNums.Remove(room);

            if(room < rooms.Count && checkRoomFitsAndAssign(randPos, rooms[room].room.roomSize))
            {
                foreach (Vector3Int exit in rooms[room].room.exits)
                {
                    matrixOfLocations[randPos.y + exit.y].Add(matrixOfLocations[randPos.y].Count, randPos);
                }
                activeRooms.Add(randPos, rooms[room]);
                numRoomsPerFloor[y]++;

                if (!requiredCount.ContainsKey(rooms[room].identifier))
                {
                    requiredCount.Add(rooms[room].identifier, 0);
                }
                requiredCount[rooms[room].identifier]++;

                return true;
            }

            numTries--;
        }

        return false;

    }

    private bool checkRoomFitsAndAssign(Vector3Int pos, Vector3Int size)
    {
        for(int x = pos.x; x < pos.x + size.x; x++)
        {
            for(int y = pos.y; y < pos.y + size.y; y++)
            {
                for (int z = pos.z; z < pos.z + size.z; z++)
                {
                    if (x >= roomBorder && y >= 0 && z >= roomBorder && x < used.GetLength(0) - roomBorder 
                        && y < used.GetLength(1) && z < used.GetLength(2) - roomBorder)
                    {
                        if (roomWithBoundaries[x, y, z]) 
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        for (int x = pos.x; x < pos.x + size.x; x++)
        {
            for (int y = pos.y; y < pos.y + size.y; y++)
            {
                for (int z = pos.z; z < pos.z + size.z; z++)
                {
                    used[x, y, z] = true;
                    roomBounds[x, y, z] = true;
                }
            }
        }

        for (int x = pos.x - roomBorder; x < pos.x + size.x + roomBorder; x++)
        {
            for (int y = pos.y - roomBorder; y < pos.y + size.y + roomBorder; y++)
            {
                for (int z = pos.z -  roomBorder; z < pos.z + size.z + roomBorder; z++)
                {
                    if(x >= 0 && y >= 0 && z >= 0 && x < used.GetLength(0) && y < used.GetLength(1) && z < used.GetLength(2))
                    {
                        roomWithBoundaries[x, y, z] = true;
                    }
                }
            }
        }

        return true;
    }

    private void CalculateRoomDistances()
    {
        distance = new float[dungeonSize.y, activeRooms.Count + dungeonSize.y - 1, activeRooms.Count + dungeonSize.y - 1];
        paths = new float[activeRooms.Count + dungeonSize.y - 1, activeRooms.Count + dungeonSize.y - 1];

        for (int y = 0; y < dungeonSize.y; y++)
        {

            for(int x = 0; x < activeRooms.Count + dungeonSize.y - 1; x++)
            {
                for (int z = 0; z < activeRooms.Count + dungeonSize.y - 1; z++)
                {

                    distance[y, x, z] = -1;
                }
            }

            foreach (int key in matrixOfLocations[y].Keys)
            {
                foreach (int key_other in matrixOfLocations[y].Keys)
                {
                    Vector3Int loc1 = matrixOfLocations[y][key];
                    Vector3Int loc2 = matrixOfLocations[y][key_other];

                    float dist = Vector3.Distance(loc1, loc2);
                    distance[y, key, key_other] = dist;
                }
            }
        }
    }

    private void CalculateBestPaths()
    {
        anthill = new AntHill[dungeonSize.y];

        for (int y = 0; y < dungeonSize.y; y++)
        {
            float[,] distanceMatrix = new float[distance.GetLength(1), distance.GetLength(2)];
            for (int x = 0; x < distanceMatrix.GetLength(1); x++)
            {
                for (int z = 0; z < distanceMatrix.GetLength(1); z++)
                {
                    distanceMatrix[x, z] = distance[y, x, z];
                }
            }

            List<Vector3Int> floorRooms = new List<Vector3Int>();
            foreach(Vector3Int loc in activeRooms.Keys.ToList<Vector3Int>())
            {
                if (loc.y == y) floorRooms.Add(loc);
            }

            foreach(Vector3Int loc in stairRooms.Keys.ToList<Vector3Int>())
            {
                if (matrixOfLocations[y].ContainsValue(loc))
                {
                    floorRooms.Add(loc);
                }
            }

            AntHill newHill = new AntHill(floorRooms, distanceMatrix, numAnts, this.distanceExp, this.pheroExp);
            anthill[y] = newHill;
            newHill.RunAntHill(numIters, 0.5f, 0.2f);
            this.paths = newHill.GetPathsThreshold(threshold);

            for (int idx = 0; idx < paths.GetLength(0); idx++)
            {
                for (int j = idx; j < paths.GetLength(1); j++)
                {
                    if (paths[idx, j] == 1.0f)
                    {
                        MakePaths(this.matrixOfLocations[y][idx], this.matrixOfLocations[y][j], y);
                    }
                }
            }
        }
        FillUnusedExits();
    }

    private void MakePaths(Vector3Int start, Vector3Int end, int yLevel)
    {
        DungeonTile begin;
        if (activeRooms.ContainsKey(start))
            begin = activeRooms[start].room;
        else
            begin = stairRooms[start];
        

        DungeonTile goal;
        if (activeRooms.ContainsKey(end))
            goal = activeRooms[end].room;
        else
            goal = stairRooms[end];

        Vector3Int bestStart = Vector3Int.one;
        Vector3Int bestEnd = Vector3Int.one;
        float best = float.MaxValue;

        foreach(Vector3Int exit in begin.exits)
        {
            foreach (Vector3Int exit2 in goal.exits)
            {
                if(Vector3.Distance(start + exit, end + exit2) < best && (start.y + exit.y == end.y + exit2.y))
                {
                    best = Vector3.Distance(start + exit, end + exit2);
                    bestStart = start + exit;
                    bestEnd = end + exit2;
                }
            }
        }

        bool[,,] tempUseMap = new bool[dungeonSize.x, dungeonSize.y, dungeonSize.z];
        Dictionary<Vector3Int, Vector3Int> previousMap = new Dictionary<Vector3Int, Vector3Int>();

        Queue<Vector3Int> toVisit = new Queue<Vector3Int>();
        toVisit.Enqueue(bestEnd);
        Vector3Int curr = bestEnd;

        int visitCount = 0;

        while(toVisit.Count > 0)
        {
            visitCount++;
            curr = toVisit.Dequeue();
            tempUseMap[curr.x, curr.y, curr.z] = true;

            if (visitCount > dungeonSize.x * dungeonSize.y * dungeonSize.z)
            {
                Debug.LogError("Generate Dungeon: Breadthwise search took more iterations than possible size");
                break;
            }

            if (curr.Equals(bestStart))
            {
                break;
            }

            if (previousMap.ContainsKey(curr))
            {
                Vector3Int previousDir = curr - previousMap[curr];

                if(previousDir.magnitude == 1)
                {
                    if (AttemptEnqueue(curr + previousDir, tempUseMap, ref toVisit)) previousMap.Add(curr + previousDir, curr);
                }
            }

            //Add in each direction
            if (AttemptEnqueue(new Vector3Int(curr.x + 1, curr.y, curr.z), tempUseMap, ref toVisit)) previousMap.Add(new Vector3Int(curr.x + 1, curr.y, curr.z), curr);
            if (AttemptEnqueue(new Vector3Int(curr.x - 1, curr.y, curr.z), tempUseMap, ref toVisit)) previousMap.Add(new Vector3Int(curr.x - 1, curr.y, curr.z), curr);
            //if (AttemptEnqueue(new Vector3Int(curr.x, curr.y + 1, curr.z), tempUseMap, ref toVisit)) previousMap.Add(new Vector3Int(curr.x, curr.y + 1, curr.z), curr);
            //if (AttemptEnqueue(new Vector3Int(curr.x, curr.y - 1, curr.z), tempUseMap, ref toVisit)) previousMap.Add(new Vector3Int(curr.x, curr.y - 1, curr.z), curr);
            if (AttemptEnqueue(new Vector3Int(curr.x, curr.y, curr.z + 1), tempUseMap, ref toVisit)) previousMap.Add(new Vector3Int(curr.x, curr.y, curr.z + 1), curr);
            if (AttemptEnqueue(new Vector3Int(curr.x, curr.y, curr.z - 1), tempUseMap, ref toVisit)) previousMap.Add(new Vector3Int(curr.x, curr.y, curr.z - 1), curr);

        }

        while (!curr.Equals(bestEnd))
        {
            used[curr.x, curr.y, curr.z] = true;
            if (previousMap.ContainsKey(curr))
            {
                curr = previousMap[curr];
            }
            else
            {
                break;
            }
        }
        used[curr.x, curr.y, curr.z] = true;
    }

    private bool AttemptEnqueue(Vector3Int point, bool[,,] tempUsed, ref Queue<Vector3Int> queue)
    {
        if(point.x >= 0 && point.y >= 0 && point.z >= 0 && point.x < dungeonSize.x && point.y < dungeonSize.y && point.z < dungeonSize.z)
        {
            if (!tempUsed[point.x, point.y, point.z] && !roomBounds[point.x, point.y, point.z] && !queue.Contains(point))
            {
                queue.Enqueue(point);
                return true;
            }
        }
        return false;
    }

    private void FillUnusedExits()
    {
        foreach(Vector3Int roomLoc in activeRooms.Keys)
        {
            foreach(Vector3Int exit in activeRooms[roomLoc].room.exits)
            {
                Vector3Int pos = roomLoc + exit;
                if(pos.x >= 0 && pos.y >=0  && pos.z >= 0 && pos.x < dungeonSize.x && pos.y < dungeonSize.y && pos.z < dungeonSize.z)
                    used[pos.x, pos.y, pos.z] = true;
            }
        }
    }
    private void PlaceCorridors()
    {
        for (int x = 0; x < dungeonSize.x; x++)
        {
            for (int y = 0; y < dungeonSize.y; y++)
            {
                for (int z = 0; z < dungeonSize.z; z++)
                {
                    Vector3Int p = new Vector3Int(x, y, z);
                    int total = (CheckLeft(p) ? 1 : 0) + (CheckForward(p) ? 2 : 0) + (CheckRight(p) ? 4 : 0) + (CheckBack(p) ? 8 : 0);
                    DungeonTile t;

                    if (!CheckInRoom(p))
                    {
                        switch (total)
                        {
                            //End caps
                            case 1:
                                t = Instantiate(endCap, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                            case 2:
                                t = Instantiate(endCap, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                            case 4:
                                t = Instantiate(endCap, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                            case 8:
                                t = Instantiate(endCap, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                            //Straights
                            case 5:
                                t = Instantiate(straight, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                            case 10:
                                t = Instantiate(straight, this.transform);
                                t.transform.localPosition = (p * tileSize) + (new Vector3(-1, 0, 1) * tileSize / 4);
                                t.transform.localEulerAngles = new Vector3(0, 90, 0);
                                break;
                            //Curves Left
                            case 3:
                                t = Instantiate(Left, this.transform);
                                t.transform.localPosition = (p * tileSize) + (Vector3.forward * tileSize / 2);
                                t.transform.localEulerAngles = new Vector3(0, 180, 0);
                                break;
                            case 12:
                                t = Instantiate(Left, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                            //Curves Right
                            case 6:
                                t = Instantiate(Right, this.transform);
                                t.transform.localPosition = (p * tileSize) + (Vector3.forward * tileSize / 2.0f);
                                t.transform.localEulerAngles = new Vector3(0, 180, 0);
                                break;
                            case 9:
                                t = Instantiate(Right, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                            //T shape
                            case 7:
                                t = Instantiate(T_Shape, this.transform);
                                t.transform.localPosition = (p * tileSize) + (Vector3.forward * tileSize / 2.0f);
                                t.transform.localEulerAngles = new Vector3(0, 180, 0);
                                break;
                            case 14:
                                t = Instantiate(T_Shape, this.transform);
                                t.transform.localPosition = (p * tileSize) + (new Vector3(-1, 0, 1) * tileSize / 4);
                                t.transform.localEulerAngles = new Vector3(0, 90, 0);
                                break;
                            case 11:
                                t = Instantiate(T_Shape, this.transform);
                                t.transform.localPosition = (p * tileSize) + (new Vector3(1, 0, 1) * tileSize / 4);
                                t.transform.localEulerAngles = new Vector3(0, 270, 0);
                                break;
                            case 13:
                                t = Instantiate(T_Shape, this.transform);
                                t.transform.localPosition = (p * tileSize);
                                break;
                            case 15:
                                t = Instantiate(plusShape, this.transform);
                                t.transform.localPosition = (p - t.tileRoomOffset) * tileSize;
                                break;
                        }
                    }
                }
            }
        }
    }

    private bool CheckLeft(Vector3Int pos)
    {
        return (used[pos.x, pos.y, pos.z] && pos.x > 0 && used[pos.x - 1, pos.y, pos.z]);
    }

    private bool CheckRight(Vector3Int pos)
    {
        return (used[pos.x, pos.y, pos.z] && pos.x < dungeonSize.x - 1 && used[pos.x + 1, pos.y, pos.z]);
    }

    private bool CheckForward(Vector3Int pos)
    {
        return (used[pos.x, pos.y, pos.z] && pos.z > 0 && used[pos.x, pos.y, pos.z - 1]);
    }
    private bool CheckBack(Vector3Int pos)
    {
        return (used[pos.x, pos.y, pos.z] && pos.z < dungeonSize.z - 1 && used[pos.x, pos.y, pos.z + 1]);
    }

    private bool CheckInRoom(Vector3Int pos)
    {
        return roomBounds[pos.x, pos.y, pos.z];
    }


    public void OnDrawGizmosSelected()
    {
        if (anthill != null)
        {
            for (int y = 0; y < dungeonSize.y; y++)
            {
                AntHill hill = anthill[y];
                if (hill != null)
                {
                    float[,] phero = hill.GetPathsThreshold(threshold);

                    for (int i = 0; i < phero.GetLength(0); i++)
                    {
                        for (int j = 0; j < phero.GetLength(1); j++)
                        {
                            if (i != j && phero[i, j] == 1.0)
                            {
                                Gizmos.DrawLine(matrixOfLocations[y][i] * tileSize, matrixOfLocations[y][j] * tileSize);
                            }
                        }
                    }
                }
            }
        }
    }
}

class Ant
{
    private List<Vector3Int> toVisit;
    private List<int> visited;
    private float[,] distances;
    private float[,] phero;

    private int curr;

    public Ant(List<Vector3Int> toVisit, float[,] distances, float[,] phero, int start)
    {
        this.toVisit = toVisit;
        this.visited = new List<int>();

        this.distances = distances;
        this.phero = phero;

        if(start >= toVisit.Count)
        {
            start = toVisit.Count - 1;
        }

        this.curr = start;
        visited.Add(start);
    }

    public float[,] RunAnt(float distExp, float pheroExp)
    {
        float[,] newPhero = new float[phero.GetLength(0), phero.GetLength(1)];
        while(this.visited.Count < this.toVisit.Count)
        {
            int next = GetNext(distExp, pheroExp);
            visited.Add(next);

            newPhero[curr, next] = 1.0f;
            newPhero[next, curr] = 1.0f;
            curr = next;
        }

        return newPhero;
    }

    private int GetNext(float distExp, float pheroExp)
    {
        Dictionary<int, float> costs = new Dictionary<int, float>();
        float total = 0;
        for(int idx = 0; idx < toVisit.Count; idx++)
        {
            if (!visited.Contains(idx))
            {
                float dist_cost = Mathf.Pow((1.0f / distances[curr, idx]), distExp);
                float phero_cost = Mathf.Pow(phero[curr, idx], pheroExp);
                float total_cost = dist_cost * phero_cost;

                costs.Add(idx, total_cost);
                total += total_cost;
            }
        }

        float rng = UnityEngine.Random.Range(0, total);
        total = 0;

        foreach(int cost in costs.Keys)
        {
            total += costs[cost];
            if(total > rng)
            {
                return cost;
            }
        }

        return costs.Keys.ToList<int>()[0];

    }
}

class AntHill
{

    private float[,] distances;
    private float[,] pheromones;
    private int numAnts;
    private List<Vector3Int> locales;

    private float distExp;
    private float pheroExp;


    public AntHill(List<Vector3Int> locales, float[,] distances, int numAnts, float distExp, float pheroExp)
    {
        this.distances = distances;
        this.pheromones = new float[distances.GetLength(0), distances.GetLength(1)];
        this.numAnts = numAnts;
        this.locales = locales;
        this.distExp = distExp;
        this.pheroExp = pheroExp;
    }

    public void RunAntHill(int numIterations, float phero, float phero_iter_loss)
    {
        for(int i = 0; i < numIterations; i++)
        {
            for(int ant = 0; ant < numAnts; ant++)
            {
                Ant newAnt = new Ant(this.locales, this.distances, this.pheromones, UnityEngine.Random.Range(0, this.locales.Count));

                float[,] newPhero = newAnt.RunAnt(this.distExp, this.pheroExp);

                for (int x = 0; x < newPhero.GetLength(0); x++)
                {
                    for(int  y = 0; y < newPhero.GetLength(1); y++)
                    {
                        this.pheromones[x, y] += newPhero[x, y] * phero;
                    }
                }
            }

            //Find highest pheromones
            float maxValue = 0.0f;
            for (int x = 0; x < pheromones.GetLength(0); x++)
            {
                for (int y = 0; y < pheromones.GetLength(1); y++)
                {
                    if (this.pheromones[x, y] > maxValue)
                    {
                        maxValue = this.pheromones[x, y];
                    }
                }
            }
            //Normalize pheromones
            for (int x = 0; x < pheromones.GetLength(0); x++)
            {
                for (int y = 0; y < pheromones.GetLength(1); y++)
                {
                    this.pheromones[x, y] /= maxValue;
                }
            }

        }

        for (int x = 0; x < pheromones.GetLength(0); x++)
        {
            for (int y = 0; y < pheromones.GetLength(1); y++)
            {
                this.pheromones[x, y] -= phero_iter_loss;
                this.pheromones[x, y] = Mathf.Max(0.0f, this.pheromones[x, y]);


            }
        }

    }

    public float[,] GetPaths()
    {
        return this.pheromones;
    }

    public float[,] GetPathsThreshold(float threshold)
    {
        float[,] pheroThresh = new float[distances.GetLength(0), distances.GetLength(1)];

        for (int x = 0; x < pheromones.GetLength(0); x++)
        {
            for (int y = 0; y < pheromones.GetLength(1); y++)
            {
                pheroThresh[x, y] = (this.pheromones[x, y] > threshold ? 1.0f : 0.0f);
            }
        }

        return pheroThresh;
    }

    public int[] GetBestConnectionForCoords()
    {
        int[] best = new int[pheromones.GetLength(0)];
        Dictionary<int, int> pairs = new Dictionary<int, int>();
        for (int x = 0; x < pheromones.GetLength(0); x++)
        {
            float currBest = 0.0f;
            int bestIdx = 0;
            for (int y = 0; y < pheromones.GetLength(1); y++)
            {
                if (!(best.Contains(y) && best[x] == x))
                {
                    if (pheromones[x, y] > currBest)
                    {
                        currBest = pheromones[x, y];
                        bestIdx = y;
                    }
                }
            }
            pairs.Add(x, bestIdx);
            best[x] = bestIdx;
        }

        return best;
    }
}

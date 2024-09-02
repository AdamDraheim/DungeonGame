using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.UIElements;

public class MovementMap : MonoBehaviour
{

    [Header("Grid fields")]
    [SerializeField]
    private Vector3 origin;
    [SerializeField]
    private Vector3 bounds;
    [SerializeField]
    private int numCellsX;
    [SerializeField]
    private int numCellsY;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float offset;

    [Header("Masking and Point Placement")]
    [SerializeField]
    private LayerMask groundMask;
    [SerializeField]
    private LayerMask blockMask;
    [SerializeField]
    private float borderRadius;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float randomizedPlacement;

    [Header("Subgrid Fields")]
    [SerializeField]
    private int numSubgridsX;
    [SerializeField]
    private int numSubgridsY;
    [SerializeField]
    private int numSubDivs;

    [Header("Vertical Management")]
    [SerializeField]
    private int numSubGridsHeight;
    [SerializeField]
    private float maxHeightDiff;


    private GridPoint targ;
    private GridPoint org;


    public static MovementMap map;

    private struct GridPoint
    {
        public Vector3 location;
        public Vector3 normal;
        public List<GridPoint> neighbors;
        public bool valid;
        public bool debugDraw;

        public override bool Equals(System.Object obj)
        {
            if (!(obj is GridPoint))
            {
                return false;
            }

            GridPoint b = (GridPoint)obj;

            return (this.valid == b.valid) && this.location.Equals(b.location);
        }

        public override int GetHashCode()
        {
            return 0;
        }

    }

    private struct subGrid
    {
        public List<GridPoint> gridPoints;
        public subGrid[,,] subs;
        public Vector3 origin;
        public Vector3 end;

        public float subGridResX;
        public float subGridResY;
        public float subGridResH;
        public int depth;
        public bool isLeaf;

    }

    private GridPoint[,] grid;
    private List<GridPoint> allPoints;
    private bool gridMade;

    private subGrid rootGrid;

    private float resolutionX;
    private float resolutionY;

    private bool generated;


    private void Start()
    {

        if(map == null)
        {
            map = this;
        }
        else
        {
            Destroy(this.gameObject);
        }

    }

    private void Update()
    {
        if (!generated)
        {
            GenerateMap();
            generated = true;
        }
    }

    public bool CheckMoveInBounds(Vector3 position, Vector3 dir)
    {
        Vector3 newPos = position + dir;

        return Vector3.Dot(newPos - origin, newPos - (origin + bounds)) < 0;
    }

    public Vector3 GetTrajectory(Vector3 position, Vector3 target, int maxIterations, float newPref = 0.5f)
    {
        getClosest(position, out GridPoint start);
        getClosest(target, out GridPoint end);

        targ = end;
        org = start;

        org.debugDraw = true;

        List<Vector3> visited = new List<Vector3>();
        Dictionary<GridPoint, GridPoint> path = new Dictionary<GridPoint, GridPoint>();
        path.Add(start, new GridPoint());

        Queue<GridPoint> toVisit = new Queue<GridPoint>();
        toVisit.Enqueue(start);
        GridPoint curr = start;
        int iter = 0;


        while(toVisit.Count > 0)
        {
            //toVisit.GetMin(out curr, true);
            curr = toVisit.Dequeue();

            if (curr.Equals(end))
            {
                break;
            }

            if(iter > maxIterations)
            {
                break;
            }

            foreach(GridPoint neighbor in curr.neighbors)
            {
                if (neighbor.valid)
                {
                    if (!path.ContainsKey(neighbor))
                    {
                        //float heuristic = Vector3.Distance(neighbor.location, end.location) + Vector3.Distance(neighbor.location, start.location);
                        //if (!toVisit.Contains(neighbor, heuristic))
                        //{
                        //    toVisit.Add(neighbor, heuristic);
                        //    path.Add(neighbor, curr);
                        //}

                        toVisit.Enqueue(neighbor);
                        path.Add(neighbor, curr);
                        
                    }
                }
            }
            iter++;
        }

        GridPoint pre = new GridPoint();
        Vector3 trajectory = Vector3.zero;
        while (!curr.Equals(start))
        {
            pre = curr;
            curr = path[curr];
            trajectory = (newPref * (pre.location - curr.location).normalized + ((1.0f - newPref) * trajectory)).normalized;
        }

        //trajectory = (newPref * (pre.location - curr.location).normalized + ((1.0f - newPref) * trajectory)).normalized;

        return trajectory;

    }

    private void updateListsForGridPoint(GridPoint p, GridPoint new_p)
    {
        subGrid currSub = rootGrid;

        while (!currSub.isLeaf)
        {
            float subGridResX = currSub.subGridResX;
            float subGridResY = currSub.subGridResY;
            float subGridResH = currSub.subGridResH;

            int adjPosX = (int)((p.location.x - origin.x) / subGridResX);
            int adjPosY = (int)((p.location.z - origin.z) / subGridResY);
            int adjPosH = (int)((p.location.y - origin.y) / subGridResH);

            if (currSub.subs[adjPosX, adjPosY, adjPosH].gridPoints.Count == 0)
            {
                break;
            }
            currSub = currSub.subs[adjPosX, adjPosY, adjPosH];
        }

        
        for(int idx = 0; idx < p.neighbors.Count; idx++)
        {
            p.neighbors[idx].neighbors.Remove(p);
            p.neighbors[idx].neighbors.Add(new_p);
        }

        for(int idx = 0; idx < allPoints.Count; idx++)
        {
            if (comparePoints(allPoints[idx], p))
            {
                allPoints[idx] = new_p;
                break;
            }
        }

        List<GridPoint> points = currSub.gridPoints;
        for (int idx = 0; idx < points.Count; idx++)
        {
            if (comparePoints(points[idx], p))
            {
                points[idx] = new_p;
                return;
            }
        }

        Debug.LogWarning("No points in subgrid list matched gridpoint");
    }

    private bool getClosest(Vector3 position, out GridPoint point)
    {

        subGrid currSub = rootGrid;

        while (!currSub.isLeaf)
        {
            float subGridResX = currSub.subGridResX;
            float subGridResY = currSub.subGridResY;
            float subGridResH = currSub.subGridResH;

            int adjPosX = (int)((position.x - origin.x) / subGridResX);
            int adjPosY = (int)((position.z - origin.z) / subGridResY);
            int adjPosH = (int)((position.y - origin.y) / subGridResH);

            if (currSub.subs[adjPosX, adjPosY, adjPosH].gridPoints.Count == 0)
            {
                break;
            }
            currSub = currSub.subs[adjPosX, adjPosY, adjPosH];
        }

        float bestDist = float.MaxValue;
        point = currSub.gridPoints[0];

        foreach (GridPoint p in currSub.gridPoints)
        {
            if (p.valid)
            {
                float dist = Vector3.Distance(p.location, position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    point = p;
                }
            }
        }

        return true;

    }

    private bool inBounds(Vector3 position, Vector3 start, Vector3 end)
    {
        return Vector3.Dot(start - position, end - position) < 0;
    }

    private subGrid subDivide(subGrid sgPoint, int depth)
    {
        Vector3 start = new Vector3(sgPoint.origin.x, sgPoint.origin.y, sgPoint.origin.z);
        Vector3 end = new Vector3(sgPoint.end.x, sgPoint.end.y, sgPoint.end.z);
        sgPoint.isLeaf = false;
        sgPoint.subs = new subGrid[numSubgridsX, numSubgridsY, numSubGridsHeight];

        float resX = (end.x - start.x) / numSubgridsX;
        float resY = (end.z - start.z) / numSubgridsY;
        float resH = (end.y - start.y) / numSubGridsHeight;

        sgPoint.subGridResH = resH;
        sgPoint.subGridResX = resX;
        sgPoint.subGridResY = resY;

        for (int i = 0; i < numSubgridsX; i++)
        {
            for (int j = 0; j < numSubgridsY; j++)
            {
                for (int h = 0; h < numSubGridsHeight; h++)
                {
                    subGrid sg = new subGrid();
                    sg.depth = depth + 1;
                    sg.isLeaf = true;
                    sg.origin = start + new Vector3(i * resX, h * resH, j * resY);
                    sg.end = start + new Vector3((i + 1) * resX, (h + 1) * resH, (j + 1) * resY);


                    sg.gridPoints = new List<GridPoint>();

                    sgPoint.subs[i, j, h] = sg;
                }
            }
        }
        List<GridPoint> newPoints = new List<GridPoint>();

        foreach (GridPoint p in sgPoint.gridPoints)
        {
            int adjPosX = (int)((p.location.x - sgPoint.origin.x) / resX);
            int adjPosY = (int)((p.location.z - sgPoint.origin.z) / resY);
            int adjPosH = (int)((p.location.y - sgPoint.origin.y) / resH);


            sgPoint.subs[adjPosX, adjPosY, adjPosH].gridPoints.Add(p);

            //Recalculate at each vertical component for height differences
            for (int h = 0; h < numSubGridsHeight; h++)
            {

                GridPoint newVert = copyPoint(p);
                newVert.location = new Vector3(p.location.x, ((h+0.95f) * resH) + origin.y, p.location.z);
                
                if (Physics.Raycast(newVert.location, Vector3.down, out RaycastHit hit, resH, groundMask))
                {

                    newVert.valid = true;
                    newVert.location = hit.point;
                }
                else
                {
                    newVert.valid = false;
                }
                if (!comparePoints(newVert, p))
                {
                    newPoints.Add(newVert);
                    sgPoint.subs[adjPosX, adjPosY, h].gridPoints.Add(newVert);
                    foreach (GridPoint neighbor in newVert.neighbors)
                    {
                        neighbor.neighbors.Add(newVert);
                    }
                }

            }
        }

        foreach (GridPoint newpoint in newPoints)
        {
            allPoints.Add(newpoint);
        }

        for (int i = 0; i < numSubgridsX; i++)
        {
            for (int j = 0; j < numSubgridsY; j++)
            {
                for (int h = 0; h < numSubGridsHeight; h++)
                {

                    if (sgPoint.subs[i, j, h].depth < numSubDivs)
                    {
                        subDivide(sgPoint.subs[i, j, h], depth + 1);
                    }
                }
            }
        }

        return sgPoint;
    }

    private GridPoint copyPoint(GridPoint point)
    {
        GridPoint newPoint = new GridPoint();
        newPoint.valid = point.valid;
        newPoint.location = point.location;
        newPoint.neighbors = new List<GridPoint>();
        foreach(GridPoint neighbor in point.neighbors)
        {
            newPoint.neighbors.Add(neighbor);
        }
        return newPoint;
    }

    private bool comparePoints(GridPoint p1, GridPoint p2)
    {
        if(p1.valid == p2.valid 
            && p1.location.Equals(p2.location))
        {
            return true;
        }
        return false;
    }

    private void GenerateMap()
    {
        
        grid = new GridPoint[numCellsX, numCellsY];
        allPoints = new List<GridPoint>();
        rootGrid = new subGrid();
        rootGrid.gridPoints = new List<GridPoint>();
      
        Vector3 end = origin + bounds;

        resolutionX = (end.x - origin.x) / numCellsX;
        resolutionY = (end.z - origin.z) / numCellsY;
        float subGridResX = (end.x - origin.x) / numSubgridsX;
        float subGridResY = (end.z - origin.z) / numSubgridsY;

        for (int idx = 0; idx < numCellsX; idx++)
        {
            for (int idx2 = 0; idx2 < numCellsY; idx2++)
            {
                grid[idx, idx2] = new GridPoint();
                grid[idx, idx2].location = origin + new Vector3(resolutionX * idx, 0, resolutionY * idx2);
                if (idx % 2 == 0)
                {
                    grid[idx, idx2].location = grid[idx, idx2].location + new Vector3(0, 0, resolutionY * offset);
                }
                grid[idx, idx2].neighbors = new List<GridPoint>();
            }
        }


        for (int idx = 0; idx < numCellsX; idx++)
        {
            for (int idx2 = 0; idx2 < numCellsY; idx2++)
            {

                //Left and Right points
                if(idx > 0)
                {
                    grid[idx, idx2].neighbors.Add(grid[idx - 1, idx2]);
                }
                if (idx < numCellsX - 1)
                {
                    grid[idx, idx2].neighbors.Add(grid[idx + 1, idx2]);
                }

                //Up and down Points
                if(idx2 < numCellsY - 1)
                {
                    grid[idx, idx2].neighbors.Add(grid[idx, idx2 + 1]);
                }

                //Up and down Points
                if (idx2 > 0)
                {
                    grid[idx, idx2].neighbors.Add(grid[idx, idx2 - 1]);
                }

                if(idx % 2 == 0)
                {
                    if (idx > 0)
                    {
                        if(idx2 < numCellsY - 1)
                        {
                            grid[idx, idx2].neighbors.Add(grid[idx - 1, idx2 + 1]);
                        }
                    }
                    if (idx < numCellsX - 1)
                    {
                        if (idx2 < numCellsY - 1)
                        {
                            grid[idx, idx2].neighbors.Add(grid[idx + 1, idx2 + 1]);
                        }
                    }
                }
                else
                {
                    if (idx > 0)
                    {
                        if (idx2 > 0)
                        {
                            grid[idx, idx2].neighbors.Add(grid[idx - 1, idx2 - 1]);
                        }
                    }
                    if (idx < numCellsX - 1)
                    {
                        if (idx2 > 0)
                        {
                            grid[idx, idx2].neighbors.Add(grid[idx + 1, idx2 - 1]);
                        }
                    }
                }
            }
        }

        for (int idx = 0; idx < numCellsX; idx++)
        {
            for (int idx2 = 0; idx2 < numCellsY; idx2++)
            {
                allPoints.Add(grid[idx, idx2]);
            }
        }

        rootGrid.gridPoints = allPoints;
        rootGrid.depth = 0;
        rootGrid.isLeaf = false;
        rootGrid.origin = origin;
        rootGrid.end = end;
        rootGrid = subDivide(rootGrid, 1);
        checkForWallCollisions();
        cleanValidAndNeighbors();
        gridMade = true;
    }

    private void checkForWallCollisions()
    {
       
        for(int idx = 0; idx < allPoints.Count; idx++)
        {
            GridPoint orig_point = allPoints[idx];
            GridPoint point = copyPoint(orig_point);
            
            Vector3 adjPos = point.location + (Vector3.up * 0.05f);

            if (!point.valid) continue;

            Vector3 randomAdd = new Vector3(UnityEngine.Random.Range(-randomizedPlacement, randomizedPlacement) * resolutionX,
                                            0,
                                            UnityEngine.Random.Range(-randomizedPlacement, randomizedPlacement) * resolutionY);

            adjPos += randomAdd;

            // Move points away from wall
            Vector3[] checks = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right, 
                                 new Vector3(1, 0, 1), new Vector3(-1, 0, -1), new Vector3(1, 0, -1), 
                                new Vector3(-1, 0, 1), Vector3.up};
            foreach (Vector3 vec in checks)
            {
                Vector3 orig = adjPos + (vec.Equals(Vector3.up) ? (Vector3.down * 3.0f) : Vector3.zero);
                if (Physics.Raycast(orig, vec, out RaycastHit hit, 5.0f, blockMask) && point.valid)
                {
                    float dist = Vector3.Distance(hit.point, adjPos);
                    if (vec.Equals(Vector3.up)) 
                    { 
                        point.valid = false;
                    }
                    else
                    {
                        adjPos += -vec.normalized * Math.Min(dist, borderRadius);
                    }
                }
            }

            point.location = adjPos;

            updateListsForGridPoint(orig_point, point);
        }

    }

    private void cleanValidAndNeighbors()
    {
        foreach(GridPoint point in allPoints)
        {
            List<GridPoint> toRemove = new List<GridPoint>();
            foreach(GridPoint neighbor in point.neighbors)
            {
                if(!neighbor.valid || !point.valid || Mathf.Abs(neighbor.location.y - point.location.y) > maxHeightDiff)
                {
                    toRemove.Add(neighbor);
                }
            }

            foreach(GridPoint rem in toRemove)
            {
                rem.neighbors.Remove(point);
                point.neighbors.Remove(rem);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {

        Gizmos.DrawWireCube(origin + (bounds / 2), bounds);
        
        Gizmos.DrawSphere(targ.location, 0.5f);
        Gizmos.DrawSphere(org.location, 0.5f);

        if (gridMade)
        {
            foreach(GridPoint p in allPoints)
            {
                if (p.valid)
                {
                    Gizmos.DrawSphere(p.location, 0.25f);
                    foreach (GridPoint neighbor in p.neighbors) {
                        Gizmos.DrawLine(p.location, neighbor.location);
                    }

                    if (p.debugDraw)
                    {
                        Gizmos.DrawSphere(p.location, 1.0f);
                    }
                }
            }

            for (int idx = 0; idx < numSubgridsX; idx++)
            {
                for (int idx2 = 0; idx2 < numSubgridsY; idx2++)
                {
                    for (int h = 0; h < numSubGridsHeight; h++)
                    {

                        Vector3 bounds = (rootGrid.subs[idx, idx2, h].end - rootGrid.subs[idx, idx2, h].origin);
                        Gizmos.DrawWireCube(rootGrid.subs[idx, idx2, h].origin + (bounds / 2), bounds);

                    }
                }
            }
        }
    }
}

public class Node<T>
{
    public T data;
    public Node<T> left, right, parent;
    public int count;
    public float value;

    public Node(T data, float value)
    {
        this.data = data;
        this.value = value;
        this.count = 1;
        left = right = parent = null;
    }
}

public class BinarySearchTree<T>
{

    private Node<T> root;
    public int Count;

    public BinarySearchTree()
    {
        Count = 0;
    }

    public void Add(T item, float value)
    {
        if(root == null)
        {
            root = new Node<T>(item, value);
            Count++;
            return;
        }

        bool placed = false;
        Node<T> curr = root;
        while (!placed)
        {
            curr.count++;
            if (value <= curr.value)
            {
                if(curr.left == null)
                {
                    curr.left = new Node<T>(item, value);
                    curr.left.parent = curr;
                    placed = true;
                }
                else
                {
                    curr = curr.left;
                }
            }
            else
            {
                if(curr.right == null)
                {
                    curr.right = new Node<T>(item, value);
                    curr.right.parent = curr;
                    placed = true;
                }
                else
                {
                    curr = curr.right;
                }
            }
        }
        Count++;

    }

    public bool GetMin(out T data, bool remove)
    {
        if(root == null)
        {
            data = default(T);
            return false;
        }

        Node<T> check = root;

        while(check.left != null)
        {
            check = check.left;
        }

        data = check.data;

        if (remove)
        {
            RemoveNode(check);
        }

        return true;

    }

    private void RemoveNode(Node<T> node)
    {

        // Is root node
        if(node.parent == null)
        {
            root = null;
            Count = 0;
            return;
        }

        DecrementParents(node);
        Count--;


        // Is leaf node
        if (node.left == null && node.right == null)
        {
            if(node.value <= node.parent.value)
            {
                node.parent.left = null;
            }
            else
            {
                node.parent.right = null;
            }

        }else

        // No left node
        if(node.left == null && node.right != null)
        {
            if (node.value <= node.parent.value)
            {
                node.parent.left = node.right;
            }
            else
            {
                node.parent.right = node.right;
            }

        }
        else

        // No right node
        if (node.left != null && node.right == null)
        {
            if (node.value <= node.parent.value)
            {
                node.parent.left = node.left;
            }
            else
            {
                node.parent.right = node.left;
            }

        }
        // Both defined
        else
        {
            if (node.value <= node.parent.value)
            {
                node.left.count = node.count - 1;
                if (node.left.count >= node.right.count)
                {
                    node.parent.left = node.left;
                }
                else
                {
                    node.parent.left = node.right;
                }
            }
            else
            {
                node.right.count = node.count - 1;
                if (node.left.count >= node.right.count)
                {
                    node.parent.right = node.left;
                }
                else
                {
                    node.parent.right = node.right;
                }
            }
        }

    }

    public bool Contains(T node, float value)
    {
        Node<T> curr = root;
        
        if(root == null)
        {
            return false;
        }

        while(curr != null)
        {
            if(curr.data.Equals(node))
            {
                return true;
            }
            if(value <= curr.value)
            {
                curr = curr.left;
            }
            else
            {
                curr = curr.right;
            }
        }
        return false;
    }

    private void DecrementParents(Node<T> node)
    {
        Node<T> curr = node;
        while(curr.parent != null)
        {
            curr.parent.count--;
            curr = curr.parent;
        }
    }

}

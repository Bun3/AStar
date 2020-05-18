using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Node
{
    public Node(int _x, int _y, bool _isWall = false) { x = _x; y = _y; isWall = _isWall; }

    public int x = 0, y = 0;
    public bool isWall = false;
    public Node parent = null;

    //G: 시작부터 현재 노드까지 거리, H: 현재 노드부터 타겟까지의 거리(장애물 무시)
    public int G = 0, H = 0;
    //F: G + H
    public int F { get { return G + H; } }

    public static implicit operator Vector2Int(Node node) => new Vector2Int(node.x, node.y);
    public static implicit operator Vector2(Node node) => new Vector2(node.x, node.y);
    public override string ToString()
    {
        return "{X: " + x + ", Y: " + y + ", W" + isWall + "}";
    }
}

public class AStar : MonoBehaviour
{
    [SerializeField] private GameObject end = null;

    [SerializeField] private Vector2Int bottomLeft = Vector2Int.zero;
    [SerializeField] private Vector2Int rightTop = Vector2Int.zero;

    private Node[,] nodeArray = null;
    private Node startNode, targetNode, curNode;
    private List<Node> openList, closeList, finalList;

    [SerializeField] private bool allowDiagonal = false;
    [SerializeField] private bool dontCrossCorners = false;

    [SerializeField] private float speed = 1.0f;
    
    private void Awake()
    {
        Vector2Int size = Vector2Int.zero;
        size.x = rightTop.x - bottomLeft.x;
        size.y = rightTop.y - bottomLeft.x;

        nodeArray = new Node[size.x, size.y];
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Collider2D col = Physics2D.OverlapCircle(new Vector2(x, y), 0.4f);
                bool isWall = false;
                if (col != null)
                    isWall = col.gameObject.layer == LayerMask.NameToLayer("Wall");
                nodeArray[x, y] = new Node(x, y, isWall);
            }
        }

        openList = new List<Node>();
        closeList = new List<Node>();
        finalList = new List<Node>();

        FindingPath();
        DrawPath(finalList);
        StartCoroutine(IFollowPath(finalList, speed));
    }

    public void FindingPath()
    {
        Vector2Int startPos = Vector2Int.zero, endPos = Vector2Int.zero;
        startPos = Vector2Int.FloorToInt(transform.position);
        endPos = Vector2Int.FloorToInt(end.transform.position);

        startNode = nodeArray[startPos.x, startPos.y];
        targetNode = nodeArray[endPos.x, endPos.y];

        openList.Clear();
        openList.Add(startNode);
        closeList.Clear();
        finalList.Clear();

        while (openList.Count > 0)
        {
            curNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
                if (openList[i].F <= curNode.F && openList[i].H < curNode.H)
                    curNode = openList[i];

            openList.Remove(curNode);
            closeList.Add(curNode);

            if (curNode.Equals(targetNode))
            {
                while (targetNode != startNode)
                {
                    finalList.Add(targetNode);
                    targetNode = targetNode.parent;
                }
                finalList.Add(targetNode);
                finalList.Reverse();
                break;
            }

            if (allowDiagonal)
            {
                AddOpenList(new Vector2Int(curNode.x + 1, curNode.y + 1));
                AddOpenList(new Vector2Int(curNode.x - 1, curNode.y - 1));
                AddOpenList(new Vector2Int(curNode.x + 1, curNode.y - 1));
                AddOpenList(new Vector2Int(curNode.x - 1, curNode.y + 1));
            }
            AddOpenList(new Vector2Int(curNode.x + 1, curNode.y));
            AddOpenList(new Vector2Int(curNode.x - 1, curNode.y));
            AddOpenList(new Vector2Int(curNode.x, curNode.y + 1));
            AddOpenList(new Vector2Int(curNode.x, curNode.y - 1));
        }
        print(openList.Count > 0 ? "탐색 종료" : "길이 막혔어요 ㅠㅠ");
    }

    public void AddOpenList(Vector2Int checkVec)
    {
        if (checkVec.x < bottomLeft.x || checkVec.x > rightTop.x - 1 || checkVec.y < bottomLeft.y || checkVec.y > rightTop.y - 1) return;
        if (nodeArray[checkVec.x, checkVec.y].isWall) return;
        if (closeList.Contains(nodeArray[checkVec.x, checkVec.y])) return;

        //코너를 가로질러 갈 때 수직 수평 방향에 장애물이 둘 다 있다면 막힌 것이므로 리턴
        if (allowDiagonal && (nodeArray[curNode.x, checkVec.y].isWall && nodeArray[checkVec.x, curNode.y].isWall)) return;

        //코너를 가로지르면 안될 때 수직 수평 방향 중 하나라도 있으면 벽과 맞닿으므로 리턴
        if ((allowDiagonal && dontCrossCorners) && (nodeArray[curNode.x, checkVec.y].isWall || nodeArray[checkVec.x, curNode.y].isWall)) return;

        Node freindNode = nodeArray[checkVec.x, checkVec.y];
        int cost = curNode.G + (curNode.x - checkVec.x == 0 || curNode.y - checkVec.y == 0 ? 10 : 14);
        if (cost < freindNode.G || !openList.Contains(freindNode))
        {
            freindNode.G = cost;
            freindNode.H = (Mathf.Abs(freindNode.x - targetNode.x) + Mathf.Abs(freindNode.y - targetNode.y)) * 10;
            freindNode.parent = curNode;

            openList.Add(freindNode);
        }
    }

    private IEnumerator IFollowPath(List<Node> list, float speed = 1.0f)
    {
        if (list.Count.Equals(1)) yield break;

        while (!(transform.position.x.Equals(list[1].x) && transform.position.y.Equals(list[1].y)))
        {
            transform.position = Vector2.MoveTowards(transform.position, list[1], Time.smoothDeltaTime * speed);
            yield return null;
        }
        list.RemoveAt(0);
        yield return StartCoroutine(IFollowPath(list, speed));
    }

    private void DrawPath(List<Node> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
            Debug.DrawLine(new Vector2(list[i].x, list[i].y), new Vector2(list[i + 1].x, list[i + 1].y), Color.blue, 100f);
    }
}

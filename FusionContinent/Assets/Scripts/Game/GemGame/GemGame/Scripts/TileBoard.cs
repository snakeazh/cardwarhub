using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using FusionContinent.Model;
using UnityEngine;

public class TileBoard : MonoBehaviour
{
    public GameManager2048 gameManager;
    //public TileState[] tileStates;
    public Tile tilesPrefab;

    private TileGrid grid;
    private List<Tile> tiles;
    private bool waiting;

    public int score; // 当前分数

    private Vector2 startTouch;
    private Vector2 endTouch;
    private bool isSwiping;

    private void Awake()
    {
        grid = GetComponentInChildren<TileGrid>();
        tiles = new List<Tile>(16);
        score = 0;
    }

    public void ClearBoard()
    {
        foreach (var cell in grid.cells)
        {
            cell.tile = null;
        }
        foreach (var tile in tiles)
        {
            Destroy(tile.gameObject);
        }
        tiles.Clear();
        score = 0; // 清空分数
    }

    public void CreateTile()
    {
        Tile tile = Instantiate(tilesPrefab, grid.transform);
        tile.SetState( 2);
        tile.Spawn(grid.GetRandomEmptyCell());
        tiles.Add(tile);
    }

    private void Update()
    {
        if (!waiting)
        {
            HandleSwipe();
        }
    }

    private void HandleSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startTouch = Input.mousePosition;
            isSwiping = true;
        }
        else if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            endTouch = Input.mousePosition;
            Vector2 delta = endTouch - startTouch;

            if (delta.magnitude > 50f) // 阈值
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x > 0)
                        MoveTiles(Vector2Int.right, grid.width - 2, -1, 0, 1); // 右滑
                    else
                        MoveTiles(Vector2Int.left, 1, 1, 0, 1); // 左滑
                }
                else
                {
                    if (delta.y > 0)
                        MoveTiles(Vector2Int.up, 0, 1, 1, 1); // 上滑
                    else
                        MoveTiles(Vector2Int.down, 0, 1, grid.height - 2, -1); // 下滑
                }
            }

            isSwiping = false;
        }
    }

    private void MoveTiles(Vector2Int direction, int startX, int incrementX, int startY, int incrementY)
    {
        bool changed = false;
        for (int x = startX; x >= 0 && x < grid.width; x += incrementX)
        {
            for (int y = startY; y >= 0 && y < grid.height; y += incrementY)
            {
                TileCell cell = grid.GetCell(x, y);
                if (cell.occupied)
                {
                    changed |= MoveTile(cell.tile, direction);
                }
            }
        }
        if (changed)
        {
            StartCoroutine(WaitForChanges());
        }
    }

    private bool MoveTile(Tile tile, Vector2Int direction)
    {
        TileCell newCell = null;
        TileCell adjacent = grid.GetAdjacentCell(tile.cell, direction);
        while (adjacent != null)
        {
            if (adjacent.occupied)
            {
                if (CanMerge(tile, adjacent.tile))
                {
                    Merge(tile, adjacent.tile);
                    return true;
                }
                break;
            }
            newCell = adjacent;
            adjacent = grid.GetAdjacentCell(adjacent, direction);
        }

        if (newCell != null)
        {
            tile.MoveTo(newCell);
            return true;
        }
        return false;
    }

    private bool CanMerge(Tile a, Tile b)
    {
        return a.number == b.number && !b.locked;
    }

    private void Merge(Tile a, Tile b)
    {
        tiles.Remove(a);
        a.Merge(b.cell);

        //int index = Mathf.Clamp(IndexOf(b.state) + 1, 0, tileStates.Length - 1);
        int number = b.number * 2;

        b.SetState(number);

        // 计分
        score += number;
        Global.Publisher.Publish(MessagePublisher.GEMGAME_SCORE_CHANGE, number);
        // LogTool.Log("Score: " + score);
    }

    private int IndexOf(TileState state)
    {
        /*for (int i = 0; i < tileStates.Length; i++)
        {
            if (state == tileStates[i])
            {
                return i;
            }
        }*/
        return -1;
    }

    private IEnumerator WaitForChanges()
    {
        waiting = true;
        yield return new WaitForSeconds(0.1f);
        waiting = false;

        foreach (var tile in tiles)
        {
            tile.locked = false;
        }

        if (tiles.Count != grid.size)
        {
            CreateTile();
        }

        if (CheckForGameOver())
        {
            LogTool.Log("Game Over! Final Score: " + score);
            if (gameManager == null)
            {
                gameManager = this.transform.parent.Find("GemGameManager").GetComponent<GameManager2048>();
            }
            // 打印结束分数
                // gameManager= this.transform.parent.GetComponent<GameGemPanel>().GemGameManager.GetComponent<GameManager2048>();
            gameManager.GameOver();
        }
    }

    private bool CheckForGameOver()
    {
        if (tiles.Count != grid.size)
        {
            return false;
        }
        foreach (var tile in tiles)
        {
            TileCell up = grid.GetAdjacentCell(tile.cell, Vector2Int.up);
            TileCell down = grid.GetAdjacentCell(tile.cell, Vector2Int.down);
            TileCell left = grid.GetAdjacentCell(tile.cell, Vector2Int.left);
            TileCell right = grid.GetAdjacentCell(tile.cell, Vector2Int.right);

            if (up != null && CanMerge(tile, up.tile)) return false;
            if (down != null && CanMerge(tile, down.tile)) return false;
            if (left != null && CanMerge(tile, left.tile)) return false;
            if (right != null && CanMerge(tile, right.tile)) return false;
        }
        return true;
    }
}

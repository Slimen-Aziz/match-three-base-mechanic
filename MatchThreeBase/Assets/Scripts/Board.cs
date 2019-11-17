using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Board : MonoBehaviour
{
    static Board m_board;
    public static Board myBoard
    {
        get
        {
            return m_board;
        }
    }

    public int width;
    public int height;

    public int borderPadding = 1;

    public float swapTime = 0.5f;

    public GameObject edge;

    public GameObject normalTile;
    public GameObject[] gems;

    Tile[,] m_boardTiles;
    Gem[,] m_boardGems;

    Tile m_clickedTile;
    Tile m_targetTile;

    bool m_switchingEnabled = true;


    void Start()
    {
        m_boardGems = new Gem[width, height];
        m_boardTiles = new Tile[width, height];

        SetupTiles();
        SetupCamera();
        FillBoard(10, 0.5f);

    }

    void Awake()
    {
        if (m_board == null) m_board = this;
        else if (m_board != this)
        {
            Debug.LogError("Error: there are multiple Instances of a Singleton! #1", gameObject);
            Debug.LogError("Error: there are multiple Instances of a Singleton! #2", m_board.gameObject);
        }

    }

    #region BoardSetupAndManagement

    void SetupTiles()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject tile = Instantiate(normalTile, new Vector3(i, j, 0), Quaternion.identity) as GameObject;
                tile.transform.parent = transform;
                tile.name = "Tile[" + i + ", " + j + "]";
                m_boardTiles[i, j] = tile.GetComponent<Tile>();
                m_boardTiles[i, j].Init(i, j);
                AddBorderEdge(i, j);

            }

        }

    }

    void AddBorderEdge(int i, int j)
    {
        if (i == 0)
        {
            GameObject leftEdge = Instantiate(edge, new Vector3(-0.6944f, j, 0), Quaternion.identity) as GameObject;
            leftEdge.transform.parent = transform;
        }

        if (j == 0)
        {
            GameObject bottomEdge = Instantiate(edge, new Vector3(i, -0.6944f, 0), Quaternion.Euler(new Vector3(0, 0, 90))) as GameObject;
            bottomEdge.transform.parent = transform;
        }

        if (i == width - 1)
        {
            GameObject rightEdge = Instantiate(edge, new Vector3(width - 1 + 0.6944f, j, 0), Quaternion.Euler(new Vector3(0, 180, 0))) as GameObject;
            rightEdge.transform.parent = transform;
        }

        if (j == height - 1)
        {
            GameObject topEdge = Instantiate(edge, new Vector3(i, height - 1 + 0.6944f, 0), Quaternion.Euler(new Vector3(0, 0, -90))) as GameObject;
            topEdge.transform.parent = transform;
        }

    }

    void SetupCamera()
    {
        float xPos = (float)(width - 1) / 2f;
        float yPos = (float)(height - 1) / 2f;

        Camera.main.transform.position = new Vector3(xPos, yPos, -10f);

        float aspectRatio = (float)Screen.width / (float)Screen.height;
        float verticalSize = (float)height / 2f + (float)borderPadding;
        float horizontalSize = ((float)width / 2f + (float)borderPadding) / aspectRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;

        SpriteRenderer background = Camera.main.transform.GetComponentInChildren<SpriteRenderer>();
        float bgHeight = 2f * Camera.main.orthographicSize;
        float bgWidth = bgHeight * Camera.main.aspect;

        background.size = new Vector2(bgWidth, bgHeight);

    }

    GameObject GetRandomGem()
    {
        int rndIndex = UnityEngine.Random.Range(0, gems.Length);
        if (gems[rndIndex] == null) print("there's no valid Gem prefab at the index: " + rndIndex);
        return gems[rndIndex];
    }

    public void PlaceGem(Gem gameGem, int x, int y)
    {
        if (gameGem == null)
        {
            print("Invalid Gem");
            return;
        }

        gameGem.transform.position = new Vector3(x, y, 0);
        gameGem.transform.rotation = Quaternion.identity;
        if (IsWithinBounds(x, y)) m_boardGems[x, y] = gameGem;
        gameGem.SetCoord(x, y);

    }

    void FillBoard(int yOffset = 0, float dropTime = 0.1f)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (m_boardGems[i, j] == null)
                {
                    Gem gem = FillRandomAt(i, j, yOffset, dropTime);

                    while (HasMatchUponFill(i, j))
                    {
                        ClearGemAt(i, j);
                        gem = FillRandomAt(i, j, yOffset, dropTime);
                    }

                }

            }

        }

    }

    Gem FillRandomAt(int x, int y, int yOffset = 0, float dropTime = 0.1f)
    {
        GameObject randomGem = Instantiate(GetRandomGem()) as GameObject;

        if (randomGem != null)
        {
            PlaceGem(randomGem.GetComponent<Gem>(), x, y);

            if (yOffset != 0)
            {
                randomGem.transform.position = new Vector3(x, y + yOffset, 0);
                randomGem.GetComponent<Gem>().Move(x, y, dropTime);
            }

            randomGem.transform.parent = transform;
            return randomGem.GetComponent<Gem>();

        }

        return null;

    }

    void ClearGemAt(int x, int y)
    {
        Gem gemToClear = (IsWithinBounds(x, y)) ? m_boardGems[x, y] : null;
        if (gemToClear != null)
        {
            m_boardGems[x, y] = null;
            Destroy(gemToClear.gameObject);
        }

    }

    void ClearGems(List<Gem> gemsToClear)
    {
        foreach (Gem gem in gemsToClear)
        {
            if (gem != null) ClearGemAt(gem.xIndex, gem.yIndex);
        }

    }

    void ClearAndRefillBoard(List<Gem> gems)
    {
        StartCoroutine(ClearAndRefillRoutine(gems));
    }

    IEnumerator ClearAndRefillRoutine(List<Gem> gems)
    {
        m_switchingEnabled = false;
        List<Gem> matches = gems;

        do
        {
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));

            yield return null;

            yield return StartCoroutine(RefillRoutine());

            matches = FindAllMatches();

            yield return new WaitForSeconds(0.5f);

        }
        while (matches.Count != 0);

        m_switchingEnabled = true;

    }

    IEnumerator ClearAndCollapseRoutine(List<Gem> gems)
    {
        List<Gem> collapsingGems = new List<Gem>();
        List<Gem> matches = new List<Gem>();

        bool isFinished = false;

        yield return new WaitForSeconds(0.5f);

        while (!isFinished)
        {
            ClearGems(gems);

            yield return new WaitForSeconds(0.25f);

            collapsingGems = CollapseColumns(gems);

            while (!IsCollapsed(collapsingGems))
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            matches = FindingMatchesThrough(collapsingGems);

            if (matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else yield return StartCoroutine(ClearAndCollapseRoutine(matches));

        }

        yield return null;

    }

    IEnumerator RefillRoutine()
    {
        FillBoard(10, 0.5f);
        yield return null;
    }

    #endregion

    #region TileManagement

    public void ClickTile(Tile tile)
    {
        if (m_clickedTile == null) m_clickedTile = tile;
    }

    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(tile, m_clickedTile)) m_targetTile = tile;
    }

    public void ReleaseTile()
    {
        if (m_clickedTile != null && m_targetTile != null) SwitchTiles(m_clickedTile, m_targetTile);

        m_clickedTile = null;
        m_targetTile = null;

    }

    void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        if (m_switchingEnabled)
        {
            Gem clickedGem = m_boardGems[clickedTile.xIndex, clickedTile.yIndex];
            Gem targetGem = m_boardGems[targetTile.xIndex, targetTile.yIndex];

            if (clickedGem != null && targetGem != null)
            {
                clickedGem.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                targetGem.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);

                yield return new WaitForSeconds(swapTime);

                List<Gem> clickedGemMatches = FindMatchesAt(clickedGem.xIndex, clickedGem.yIndex);
                List<Gem> targetGemMatches = FindMatchesAt(targetGem.xIndex, targetGem.yIndex);

                var matches = clickedGemMatches.Union(targetGemMatches).ToList();

                if (matches.Count == 0)
                {
                    clickedGem.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                    targetGem.Move(targetTile.xIndex, targetTile.yIndex, swapTime);

                    yield return new WaitForSeconds(swapTime);

                }
                else
                {
                    yield return new WaitForSeconds(swapTime);

                    ClearAndRefillBoard(matches);

                }

            }

        }

    }

    List<Gem> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<Gem> collapsingGems = new List<Gem>();

        for (int i = 0; i < height - 1; i++)
        {
            if (m_boardGems[column, i] == null)
            {
                for (int j = i + 1; j < height; j++)
                {
                    if (m_boardGems[column, j] != null)
                    {
                        m_boardGems[column, j].Move(column, i, collapseTime * (j - i));
                        m_boardGems[column, i] = m_boardGems[column, j];
                        m_boardGems[column, i].SetCoord(column, i);

                        if (!collapsingGems.Contains(m_boardGems[column, i])) collapsingGems.Add(m_boardGems[column, i]);

                        m_boardGems[column, j] = null;

                        break;

                    }

                }

            }

        }

        return collapsingGems;

    }

    List<Gem> CollapseColumns(List<Gem> gems)
    {
        List<Gem> collapsingGems = new List<Gem>();
        List<int> columnsToCollapse = GetColumns(gems);

        foreach (int column in columnsToCollapse)
        {
            collapsingGems = collapsingGems.Union(CollapseColumn(column)).ToList();
        }

        return collapsingGems;

    }

    #endregion

    #region FindingMatches

    List<Gem> FindMatches(int x, int y, Vector2 searchDirection, int minMatches = 3)
    {
        List<Gem> matches = new List<Gem>();
        Gem startGem = null;

        if (IsWithinBounds(x, y)) startGem = m_boardGems[x, y];

        if (startGem != null) matches.Add(startGem);
        else return null;

        int nextX;
        int nextY;

        int maxSearchValue = (width > height) ? width : height;

        for (int i = 1; i < maxSearchValue - 1; i++)
        {
            nextX = x + (int)searchDirection.x * i;
            nextY = y + (int)searchDirection.y * i;

            if (!IsWithinBounds(nextX, nextY)) break;

            Gem nextGem = m_boardGems[nextX, nextY];

            if (nextGem == null) break;
            else
            {
                if (startGem.matchType == nextGem.matchType && !matches.Contains(nextGem)) matches.Add(nextGem);
                else break;
            }

        }


        if (matches.Count >= minMatches) return matches;

        return null;

    }

    List<Gem> FindVerticalMatches(int x, int y, int minMatches = 3)
    {
        List<Gem> upwardMatches = FindMatches(x, y, new Vector2(0, 1), 2);
        List<Gem> downwardMatches = FindMatches(x, y, new Vector2(0, -1), 2);

        if (upwardMatches == null) upwardMatches = new List<Gem>();
        if (downwardMatches == null) downwardMatches = new List<Gem>();

        var combinedMatches = upwardMatches.Union(downwardMatches).ToList();

        return (combinedMatches.Count >= minMatches) ? combinedMatches : null;

    }

    List<Gem> FindHorizontalMatches(int x, int y, int minMatches = 3)
    {
        List<Gem> leftMatches = FindMatches(x, y, new Vector2(-1, 0), 2);
        List<Gem> rightMatches = FindMatches(x, y, new Vector2(1, 0), 2);

        if (leftMatches == null) leftMatches = new List<Gem>();
        if (rightMatches == null) rightMatches = new List<Gem>();

        var combinedMatches = leftMatches.Union(rightMatches).ToList();

        return (combinedMatches.Count >= minMatches) ? combinedMatches : null;

    }

    List<Gem> FindMatchesAt(int x, int y, int minMatches = 3)
    {
        List<Gem> horizontalMatches = FindHorizontalMatches(x, y, minMatches);
        List<Gem> verticalMatches = FindVerticalMatches(x, y, minMatches);

        if (horizontalMatches == null) horizontalMatches = new List<Gem>();
        if (verticalMatches == null) verticalMatches = new List<Gem>();

        var combinedMatches = horizontalMatches.Union(verticalMatches).ToList();

        return combinedMatches;

    }

    List<Gem> FindingMatchesThrough(List<Gem> gems, int minMatches = 3)
    {
        List<Gem> matches = new List<Gem>();

        foreach (Gem gem in gems)
        {
            matches = matches.Union(FindMatchesAt(gem.xIndex, gem.yIndex, minMatches)).ToList();
        }

        return matches;

    }

    List<Gem> FindAllMatches()
    {
        List<Gem> matches = new List<Gem>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                matches = matches.Union(FindMatchesAt(i, j)).ToList();
            }

        }

        return matches;

    }

    #endregion

    #region Utilities

    bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);
    }

    bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex) return true;
        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex) return true;
        return false;
    }

    bool HasMatchUponFill(int x, int y, int minMatches = 3)
    {
        //we're only checking for left and downward matches because upon filling the board all the other cases are empty.
        List<Gem> leftMatches = FindMatches(x, y, new Vector2(-1, 0), minMatches);
        List<Gem> downwardMatches = FindMatches(x, y, new Vector2(0, -1), minMatches);

        if (leftMatches == null) leftMatches = new List<Gem>();
        if (downwardMatches == null) downwardMatches = new List<Gem>();

        return (leftMatches.Count > 0 || downwardMatches.Count > 0);

    }

    List<int> GetColumns(List<Gem> gems)
    {
        List<int> columns = new List<int>();

        foreach (Gem gem in gems)
        {
            if (!columns.Contains(gem.xIndex)) columns.Add(gem.xIndex);
        }

        return columns;

    }

    bool IsCollapsed(List<Gem> gems)
    {
        foreach (Gem gem in gems)
        {
            if (gem != null)
            {
                if (gem.transform.position.y - (float)gem.yIndex > 0.001f) return false;
            }

        }

        return true;

    }

    #endregion

}

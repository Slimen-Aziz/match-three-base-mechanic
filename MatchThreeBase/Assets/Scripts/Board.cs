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

    [SerializeField] private Camera mainCam;
    [SerializeField] private SpriteRenderer background;

    [SerializeField] private int width;
    [SerializeField] private int height;

    [SerializeField] private int borderPadding = 1;

    [SerializeField] private float swapTime = 0.5f;

    private Transform _transform;

    public GameObject edge;
    public GameObject normalTile;
    public GameObject[] gems;

    private Tile[,] _boardTiles;
    private Gem[,] _boardGems;

    private Tile _clickedTile;
    private Tile _targetTile;

    private bool _switchingEnabled = true;


    private void Awake()
    {
        if (m_board == null) m_board = this;
        else if (m_board != this)
        {
            Debug.LogError("Error: there are multiple Instances of a Singleton! #1", gameObject);
            Debug.LogError("Error: there are multiple Instances of a Singleton! #2", m_board.gameObject);
        }
    }
    
    private void Start()
    {
        Tile.OnClick += ClickTile;
        Tile.OnDrag += DragToTile;
        Tile.OnRelease += ReleaseTile;
        
        _transform = transform;
        _boardGems = new Gem[width, height];
        _boardTiles = new Tile[width, height];
        
        SetupTiles();
        SetupCamera();
        FillBoard(10, 0.5f);
    }

    #region BoardSetupAndManagement

    private void SetupTiles()
    {
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                var tile = Instantiate(normalTile, new Vector3(i, j), Quaternion.identity, _transform);
                tile.name = "Tile[" + i + ", " + j + "]";
                _boardTiles[i, j] = tile.GetComponent<Tile>();
                _boardTiles[i, j].Initialize(i, j);
                AddBorderEdge(i, j);
            }
        }
    }

    private void AddBorderEdge(int i, int j)
    {
        if (i == 0) Instantiate(edge, new Vector3(-0.6944f, j, 0), Quaternion.identity, _transform);

        if (j == 0) Instantiate(edge, new Vector3(i, -0.6944f, 0), Quaternion.Euler(new Vector3(0, 0, 90)), _transform);

        if (i == width - 1) Instantiate(edge, new Vector3(width - 1 + 0.6944f, j, 0), Quaternion.Euler(new Vector3(0, 180, 0)), _transform);

        if (j == height - 1) Instantiate(edge, new Vector3(i, height - 1 + 0.6944f, 0), Quaternion.Euler(new Vector3(0, 0, -90)), _transform);
    }

    private void SetupCamera()
    {
        var xPos = (float)(width - 1) / 2f;
        var yPos = (float)(height - 1) / 2f;

        mainCam.transform.position = new Vector3(xPos, yPos, -10f);

        var aspectRatio = (float)Screen.width / (float)Screen.height;
        var verticalSize = (float)height / 2f + (float)borderPadding;
        var horizontalSize = ((float)width / 2f + (float)borderPadding) / aspectRatio;

        mainCam.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;

        var bgHeight = 2f * mainCam.orthographicSize;
        var bgWidth = bgHeight * mainCam.aspect;

        background.size = new Vector2(bgWidth, bgHeight);
    }

    private GameObject GetRandomGem()
    {
        var rndIndex = Random.Range(0, gems.Length);
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

        var gemTransform = gameGem.transform;
        gemTransform.position = new Vector3(x, y, 0);
        gemTransform.rotation = Quaternion.identity;
        
        if (IsWithinBounds(x, y)) _boardGems[x, y] = gameGem;
        gameGem.SetCoord(x, y);
    }

    private void FillBoard(int yOffset = 0, float dropTime = 0.1f)
    {
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (_boardGems[i, j] == null)
                {
                    var gem = FillRandomAt(i, j, yOffset, dropTime);

                    while (HasMatchUponFill(i, j))
                    {
                        ClearGemAt(i, j);
                        gem = FillRandomAt(i, j, yOffset, dropTime);
                    }

                }

            }
        }
    }

    private Gem FillRandomAt(int x, int y, int yOffset = 0, float dropTime = 0.1f)
    {
        var randomGem = Instantiate(GetRandomGem());

        if (randomGem != null)
        {
            PlaceGem(randomGem.GetComponent<Gem>(), x, y);

            if (yOffset != 0)
            {
                randomGem.transform.position = new Vector3(x, y + yOffset, 0);
                randomGem.GetComponent<Gem>().Move(x, y, dropTime);
            }

            randomGem.transform.parent = _transform;
            return randomGem.GetComponent<Gem>();
        }

        return null;
    }

    private void ClearGemAt(int x, int y)
    {
        var gemToClear = (IsWithinBounds(x, y)) ? _boardGems[x, y] : null;
        if (gemToClear != null)
        {
            _boardGems[x, y] = null;
            Destroy(gemToClear.gameObject);
        }
    }

    private void ClearGems(List<Gem> gemsToClear)
    {
        for (var i = 0; i < gemsToClear.Count; i++)
        {
            var gem = gemsToClear[i];
            if (gem != null) ClearGemAt(gem.xIndex, gem.yIndex);
        }
    }

    private void ClearAndRefillBoard(List<Gem> inGems)
    {
        StartCoroutine(ClearAndRefillRoutine(inGems));
    }

    IEnumerator ClearAndRefillRoutine(List<Gem> inGems)
    {
        _switchingEnabled = false;
        List<Gem> matches = inGems;

        do
        {
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));

            yield return null;

            yield return StartCoroutine(RefillRoutine());

            matches = FindAllMatches();

            yield return new WaitForSeconds(0.5f);

        }
        while (matches.Count != 0);

        _switchingEnabled = true;

    }

    IEnumerator ClearAndCollapseRoutine(List<Gem> gems)
    {
        var collapsingGems = new List<Gem>();
        var matches = new List<Gem>();

        var isFinished = false;

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

    private void ClickTile(Tile tile)
    {
        if (_clickedTile == null) 
            _clickedTile = tile;
    }

    private void DragToTile(Tile tile)
    {
        if (_clickedTile != null && IsNextTo(tile, _clickedTile)) _targetTile = tile;
    }

    private void ReleaseTile()
    {
        if (_clickedTile != null && _targetTile != null) SwitchTiles(_clickedTile, _targetTile);
        _clickedTile = null;
        _targetTile = null;
    }

    private void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        if (_switchingEnabled)
        {
            Gem clickedGem = _boardGems[clickedTile.xIndex, clickedTile.yIndex];
            Gem targetGem = _boardGems[targetTile.xIndex, targetTile.yIndex];

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

    private List<Gem> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        var collapsingGems = new List<Gem>();

        for (int i = 0; i < height - 1; i++)
        {
            if (_boardGems[column, i] == null)
            {
                for (int j = i + 1; j < height; j++)
                {
                    if (_boardGems[column, j] != null)
                    {
                        _boardGems[column, j].Move(column, i, collapseTime * (j - i));
                        _boardGems[column, i] = _boardGems[column, j];
                        _boardGems[column, i].SetCoord(column, i);

                        if (!collapsingGems.Contains(_boardGems[column, i])) collapsingGems.Add(_boardGems[column, i]);

                        _boardGems[column, j] = null;

                        break;

                    }

                }

            }

        }

        return collapsingGems;

    }

    private List<Gem> CollapseColumns(List<Gem> inGems)
    {
        var collapsingGems = new List<Gem>();
        var columnsToCollapse = GetColumns(inGems);

        foreach (int column in columnsToCollapse)
        {
            collapsingGems = collapsingGems.Union(CollapseColumn(column)).ToList();
        }

        return collapsingGems;
    }

    #endregion

    #region FindingMatches

    private List<Gem> FindMatches(int x, int y, Vector2 searchDirection, int minMatches = 3)
    {
        var matches = new List<Gem>();
        Gem startGem = null;

        if (IsWithinBounds(x, y)) startGem = _boardGems[x, y];

        if (startGem != null) matches.Add(startGem);
        else return null;

        int nextX;
        int nextY;

        var maxSearchValue = (width > height) ? width : height;

        for (var i = 1; i < maxSearchValue - 1; i++)
        {
            nextX = x + (int) searchDirection.x * i;
            nextY = y + (int) searchDirection.y * i;

            if (!IsWithinBounds(nextX, nextY)) break;

            Gem nextGem = _boardGems[nextX, nextY];

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

    private List<Gem> FindVerticalMatches(int x, int y, int minMatches = 3)
    {
        var upwardMatches = FindMatches(x, y, new Vector2(0, 1), 2);
        var downwardMatches = FindMatches(x, y, new Vector2(0, -1), 2);
        if (upwardMatches == null) upwardMatches = new List<Gem>();
        if (downwardMatches == null) downwardMatches = new List<Gem>();
        var combinedMatches = upwardMatches.Union(downwardMatches).ToList();
        return (combinedMatches.Count >= minMatches) ? combinedMatches : null;
    }

    private List<Gem> FindHorizontalMatches(int x, int y, int minMatches = 3)
    {
        var leftMatches = FindMatches(x, y, new Vector2(-1, 0), 2);
        var rightMatches = FindMatches(x, y, new Vector2(1, 0), 2);
        if (leftMatches == null) leftMatches = new List<Gem>();
        if (rightMatches == null) rightMatches = new List<Gem>();
        var combinedMatches = leftMatches.Union(rightMatches).ToList();
        return (combinedMatches.Count >= minMatches) ? combinedMatches : null;
    }

    private List<Gem> FindMatchesAt(int x, int y, int minMatches = 3)
    {
        var horizontalMatches = FindHorizontalMatches(x, y, minMatches);
        var verticalMatches = FindVerticalMatches(x, y, minMatches);
        if (horizontalMatches == null) horizontalMatches = new List<Gem>();
        if (verticalMatches == null) verticalMatches = new List<Gem>();
        var combinedMatches = horizontalMatches.Union(verticalMatches).ToList();
        return combinedMatches;
    }

    private List<Gem> FindingMatchesThrough(List<Gem> gems, int minMatches = 3)
    {
        var matches = new List<Gem>();

        foreach (Gem gem in gems)
        {
            matches = matches.Union(FindMatchesAt(gem.xIndex, gem.yIndex, minMatches)).ToList();
        }

        return matches;
    }

    private List<Gem> FindAllMatches()
    {
        var matches = new List<Gem>();

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

    private bool IsWithinBounds(int x, int y) => (x >= 0 && x < width && y >= 0 && y < height);

    private bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex) return true;
        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex) return true;
        return false;
    }

    private bool HasMatchUponFill(int x, int y, int minMatches = 3)
    {
        //we're only checking for left and downward matches because upon filling the board all the other cases are empty.
        var leftMatches = FindMatches(x, y, new Vector2(-1, 0), minMatches);
        var downwardMatches = FindMatches(x, y, new Vector2(0, -1), minMatches);
        if (leftMatches == null) leftMatches = new List<Gem>();
        if (downwardMatches == null) downwardMatches = new List<Gem>();
        return (leftMatches.Count > 0 || downwardMatches.Count > 0);
    }

    private List<int> GetColumns(List<Gem> inGems)
    {
        var columns = new List<int>();

        foreach (var gem in inGems)
        {
            if (!columns.Contains(gem.xIndex)) columns.Add(gem.xIndex);
        }

        return columns;
    }

    private bool IsCollapsed(List<Gem> inGems)
    {
        foreach (var gem in inGems)
        {
            if (gem != null)
            {
                if (gem.transform.position.y - (float) gem.yIndex > 0.001f) return false;
            }

        }

        return true;
    }

    #endregion

}

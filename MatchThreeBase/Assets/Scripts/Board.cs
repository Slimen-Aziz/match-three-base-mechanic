using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Board : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private SpriteRenderer background;

    [SerializeField] private int width;
    [SerializeField] private int height;

    [SerializeField] private int borderPadding = 1;

    [SerializeField] private float swapTime = 0.5f;

    [SerializeField] private GameObject edge;
    [SerializeField] private GameObject normalTile;
    [SerializeField] private GameObject[] gems;
    
    private Transform _transform;
    private WaitForSeconds _swapSeconds;

    private Tile[,] _boardTiles;
    private Gem[,] _boardGems;

    private Tile _clickedTile;
    private Tile _targetTile;

    private bool _switchingEnabled = true;

    private void Start()
    {
        Application.targetFrameRate = 60;
        
        Tile.OnClick += ClickTile;
        Tile.OnDrag += DragToTile;
        Tile.OnRelease += ReleaseTile;
        Gem.OnPlaceGem += PlaceGem;

        _swapSeconds = new WaitForSeconds(swapTime);
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
        var xPos = (width - 1) / 2f;
        var yPos = (height - 1) / 2f;

        mainCam.transform.position = new Vector3(xPos, yPos, -10f);

        var aspectRatio = Screen.width / Screen.height;
        var verticalSize = height / 2f + borderPadding;
        var horizontalSize = (width / 2f + borderPadding) / aspectRatio;

        mainCam.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;

        var bgHeight = 2f * mainCam.orthographicSize;
        var bgWidth = bgHeight * mainCam.aspect;

        background.size = new Vector2(bgWidth, bgHeight);
    }

    private GameObject GetRandomGem()
    {
        var rndIndex = Random.Range(0, gems.Length);
        var gem = gems[rndIndex];
        if (gem == null) Debug.LogError($"there's no valid Gem prefab at the index: {rndIndex}");
        return gem;
    }

    private void PlaceGem(Gem inGem)
    {
        if (IsWithinBounds(inGem.XIndex, inGem.YIndex)) 
            _boardGems[inGem.XIndex, inGem.YIndex] = inGem;
    }

    private void FillBoard(int yOffset = 0, float dropTime = 0.1f)
    {
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (_boardGems[i, j] != null) continue;
                
                FillRandomAt(i, j, yOffset, dropTime);

                while (HasMatchUponFill(i, j))
                {
                    ClearGemAt(i, j);
                    FillRandomAt(i, j, yOffset, dropTime);
                }

            }
        }
    }

    private void FillRandomAt(int x, int y, int yOffset = 0, float dropTime = 0.1f)
    {
        var foundGem = Instantiate(GetRandomGem(), _transform).TryGetComponent<Gem>(out var randomGem);

        if (!foundGem) return;

        randomGem.PlaceGem(x, y);

        if (yOffset == 0) return;
        
        randomGem.transform.position = new Vector3(x, y + yOffset);
        randomGem.Move(x, y, dropTime);
    }

    private void ClearGemAt(int x, int y)
    {
        var gemToClear = (IsWithinBounds(x, y)) ? _boardGems[x, y] : null;
        if (gemToClear == null) return;
        _boardGems[x, y] = null;
        Destroy(gemToClear.gameObject);
    }

    private void ClearGems(List<Gem> gemsToClear)
    {
        for (var i = 0; i < gemsToClear.Count; i++)
        {
            var gem = gemsToClear[i];
            if (gem != null) ClearGemAt(gem.XIndex, gem.YIndex);
        }
    }

    private void ClearAndRefillBoard(List<Gem> inGems)
    {
        StartCoroutine(ClearAndRefillRoutine(inGems));
        
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
    }

    /*private IEnumerator ClearAndCollapseRoutine(List<Gem> inGems)
    {
        var collapsingGems = new List<Gem>();
        var matches = new List<Gem>();

        var isFinished = false;

        yield return new WaitForSeconds(0.5f);

        while (!isFinished)
        {
            ClearGems(inGems);

            yield return new WaitForSeconds(0.25f);

            collapsingGems = CollapseColumns(inGems);

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
            
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
        }

        yield return null;
    }*/
    
    private IEnumerator ClearAndCollapseRoutine(List<Gem> inGems)
    {
        yield return new WaitForSeconds(0.5f);

        ClearGems(inGems);

        yield return new WaitForSeconds(0.25f);

        var collapsingGems = CollapseColumns(inGems);

        while (!IsCollapsed(collapsingGems))
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        var matches = FindingMatchesThrough(collapsingGems);

        if (matches.Count == 0)
        {
            yield return null;
            yield break;
        }
            
        yield return StartCoroutine(ClearAndCollapseRoutine(matches));
    }

    private IEnumerator RefillRoutine()
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
        if (_clickedTile != null && IsNextTo(tile, _clickedTile)) 
            _targetTile = tile;
    }

    private void ReleaseTile()
    {
        if (_clickedTile != null && _targetTile != null) SwitchTiles(_clickedTile, _targetTile);
        _clickedTile = null;
        _targetTile = null;
    }

    private void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        if (!_switchingEnabled) return;
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
        
        IEnumerator SwitchTilesRoutine(Tile inClickedTile, Tile inTargetTile)
        {
            var clickedGem = _boardGems[inClickedTile.XIndex, inClickedTile.YIndex];
            var targetGem = _boardGems[inTargetTile.XIndex, inTargetTile.YIndex];

            if (clickedGem == null || targetGem == null) yield break;
        
            clickedGem.Move(inTargetTile.XIndex, inTargetTile.YIndex, swapTime);
            targetGem.Move(inClickedTile.XIndex, inClickedTile.YIndex, swapTime);

            yield return _swapSeconds;

            var clickedGemMatches = FindMatchesAt(clickedGem.XIndex, clickedGem.YIndex);
            var targetGemMatches = FindMatchesAt(targetGem.XIndex, targetGem.YIndex);

            var matches = clickedGemMatches.Union(targetGemMatches).ToList();

            if (matches.Count == 0)
            {
                clickedGem.Move(inClickedTile.XIndex, inClickedTile.YIndex, swapTime);
                targetGem.Move(inTargetTile.XIndex, inTargetTile.YIndex, swapTime);
                yield return _swapSeconds;
            }
            else
            {
                yield return _swapSeconds;
                ClearAndRefillBoard(matches);
            }
        }
    }

    private List<Gem> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        var collapsingGems = new List<Gem>();

        for (var i = 0; i < height - 1; i++)
        {
            if (_boardGems[column, i] != null) continue;
            for (var j = i + 1; j < height; j++)
            {
                if (_boardGems[column, j] == null) continue;
                _boardGems[column, j].Move(column, i, collapseTime * (j - i));
                _boardGems[column, i] = _boardGems[column, j];
                if (!collapsingGems.Contains(_boardGems[column, i])) collapsingGems.Add(_boardGems[column, i]);
                _boardGems[column, j] = null;
                break;
            }
        }

        return collapsingGems;
    }

    private List<Gem> CollapseColumns(List<Gem> inGems)
    {
        var collapsingGems = new List<Gem>();
        var columnsToCollapse = GetColumns(inGems);

        for (var i = 0; i < columnsToCollapse.Count; i++)
        {
            var column = columnsToCollapse[i];
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

        var maxSearchValue = (width > height) ? width : height;

        for (var i = 1; i < maxSearchValue - 1; i++)
        {
            var nextX = x + (int) searchDirection.x * i;
            var nextY = y + (int) searchDirection.y * i;

            if (!IsWithinBounds(nextX, nextY)) break;

            var nextGem = _boardGems[nextX, nextY];

            if (nextGem == null) break;
            
            if (startGem.Type != nextGem.Type || matches.Contains(nextGem)) break;
            
            matches.Add(nextGem);
        }

        return matches.Count >= minMatches ? matches : null;
    }

    private List<Gem> FindVerticalMatches(int x, int y, int minMatches = 3)
    {
        var upwardMatches = FindMatches(x, y, Vector2.up, 2);
        var downwardMatches = FindMatches(x, y, Vector2.down, 2);
        if (upwardMatches == null) upwardMatches = new List<Gem>();
        if (downwardMatches == null) downwardMatches = new List<Gem>();
        var combinedMatches = upwardMatches.Union(downwardMatches).ToList();
        return (combinedMatches.Count >= minMatches) ? combinedMatches : null;
    }

    private List<Gem> FindHorizontalMatches(int x, int y, int minMatches = 3)
    {
        var leftMatches = FindMatches(x, y, Vector2.left, 2);
        var rightMatches = FindMatches(x, y, Vector2.right, 2);
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

    private List<Gem> FindingMatchesThrough(List<Gem> inGems, int minMatches = 3)
    {
        var matches = new List<Gem>();

        for (var i = 0; i < inGems.Count; i++)
        {
            var gem = inGems[i];
            matches = matches.Union(FindMatchesAt(gem.XIndex, gem.YIndex, minMatches)).ToList();
        }

        return matches;
    }

    private List<Gem> FindAllMatches()
    {
        var matches = new List<Gem>();

        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
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
        if (Mathf.Abs(start.XIndex - end.XIndex) == 1 && start.YIndex == end.YIndex) return true;
        if (Mathf.Abs(start.YIndex - end.YIndex) == 1 && start.XIndex == end.XIndex) return true;
        return false;
    }

    private bool HasMatchUponFill(int x, int y, int minMatches = 3)
    {
        // Only checking for left and downward matches because upon filling the board all the other cases are empty.
        var leftMatches = FindMatches(x, y, new Vector2(-1, 0), minMatches);
        var downwardMatches = FindMatches(x, y, new Vector2(0, -1), minMatches);
        if (leftMatches == null) leftMatches = new List<Gem>();
        if (downwardMatches == null) downwardMatches = new List<Gem>();
        return (leftMatches.Count > 0 || downwardMatches.Count > 0);
    }

    private List<int> GetColumns(List<Gem> inGems)
    {
        var columns = new List<int>();

        for (var i = 0; i < inGems.Count; i++)
        {
            var gem = inGems[i];
            if (!columns.Contains(gem.XIndex)) columns.Add(gem.XIndex);
        }

        return columns;
    }

    private bool IsCollapsed(List<Gem> inGems)
    {
        for (var i = 0; i < inGems.Count; i++)
        {
            var gem = inGems[i];
            if (gem.transform.position.y - gem.YIndex > 0.001f) return false;
        }

        return true;
    }

    #endregion

}

using UnityEngine;

public class Tile : MonoBehaviour
{
    public int xIndex;
    public int yIndex;


    public void Init(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    void OnMouseDown()
    {
        if (Board.myBoard != null) Board.myBoard.ClickTile(this);
    }

    void OnMouseEnter()
    {
        if (Board.myBoard != null) Board.myBoard.DragToTile(this);
    }

    void OnMouseUp()
    {
        if (Board.myBoard != null) Board.myBoard.ReleaseTile();
    }

}

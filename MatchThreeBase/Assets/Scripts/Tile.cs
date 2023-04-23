using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public static event Action<Tile> OnClick;
    public static event Action<Tile> OnDrag;
    public static event Action OnRelease;
    
    public int xIndex;
    public int yIndex;

    public void Initialize(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    private void OnMouseDown()
    {
        OnClick?.Invoke(this);
    }

    private void OnMouseEnter()
    {
        OnDrag?.Invoke(this);
    }

    private void OnMouseUp()
    {
        OnRelease?.Invoke();
    }

}

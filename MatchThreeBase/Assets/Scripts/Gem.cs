using System;
using DG.Tweening;
using UnityEngine;

public enum GemType
{
    Blue,
    Green,
    Orange,
    Purple,
    Red,
    Teal
};

public class Gem : MonoBehaviour
{
    public static event Action<Gem> OnPlaceGem;
    
    [SerializeField] private GemType matchType;
    [SerializeField] private Ease movementEase;
    
    public GemType Type => matchType;
    public int XIndex { get; private set; }
    public int YIndex { get; private set; }

    private bool _isMoving;

    public void PlaceGem(int x, int y)
    {
        var gemTransform = transform;
        gemTransform.position = new Vector3(x, y);
        gemTransform.rotation = Quaternion.identity;
        XIndex = x;
        YIndex = y;
        OnPlaceGem?.Invoke(this);
    }
    
    public void Move(int x, int y, float moveTime)
    {
        if (_isMoving) return;
        var destination = new Vector3(x, y);
        transform.DOMove(destination, moveTime)
            .SetEase(movementEase)
            .OnComplete(() =>
            {
                PlaceGem((int) destination.x, (int) destination.y);
                _isMoving = false;
            });
    }

}

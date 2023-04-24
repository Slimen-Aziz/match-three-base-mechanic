using System;
using System.Collections;
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
    
    public GemType Type => matchType;
    public int XIndex { get; private set; }
    public int YIndex { get; private set; }

    private bool _isMoving;
    
    public void Move(int x, int y, float moveTime)
    {
        if (!_isMoving) StartCoroutine(MoveRoutine(new Vector3(x, y), moveTime));
        
        IEnumerator MoveRoutine(Vector3 destination, float moveTime)
        {
            var startPos = transform.position;
            var reachedDestination = false;
            var elapsedTime = 0f;

            _isMoving = true;

            while (!reachedDestination)
            {
                if (Vector3.Distance(transform.position, destination) < 0.01f)
                {
                    reachedDestination = true;
                    PlaceGem((int) destination.x, (int) destination.y);
                    break;
                }

                elapsedTime += Time.deltaTime;

                var lerpValue = elapsedTime / moveTime;

                // Smoothing the interpolation
                lerpValue = Mathf.Pow(lerpValue, 3) * (lerpValue * (6 * lerpValue - 15) + 10);

                transform.position = Vector3.Lerp(startPos, destination, lerpValue);


                yield return null;
            }

            _isMoving = false;
        }
        
    }

    public void PlaceGem(int x, int y)
    {
        var gemTransform = transform;
        gemTransform.position = new Vector3(x, y);
        gemTransform.rotation = Quaternion.identity;
        XIndex = x;
        YIndex = y;
        OnPlaceGem?.Invoke(this);
    }

}

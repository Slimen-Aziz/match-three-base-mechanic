using System.Collections;
using UnityEngine;

public class Gem : MonoBehaviour
{
    public enum GemType
    {
        Blue,
        Green,
        Orange,
        Purple,
        Red,
        Teal
    };

    public int xIndex;
    public int yIndex;

    public GemType matchType;

    private bool _isMoving;


    public void SetCoord(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    //moveTime represents the length of the movement transition
    public void Move(int x, int y, float moveTime)
    {
        if (!_isMoving) StartCoroutine(MoveRoutine(new Vector3(x, y, 0), moveTime));
        
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
                    if (Board.myBoard != null) Board.myBoard.PlaceGem(this, (int)destination.x, (int)destination.y);
                    break;
                }

                elapsedTime += Time.deltaTime;

                var lerpValue = elapsedTime / moveTime;

                //smoothing the interpolation
                lerpValue = Mathf.Pow(lerpValue, 3) * (lerpValue * (6 * lerpValue - 15) + 10);

                transform.position = Vector3.Lerp(startPos, destination, lerpValue);


                yield return null;

            }

            _isMoving = false;
        }
        
    }

}

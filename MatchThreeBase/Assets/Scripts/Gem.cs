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

    bool m_isMoving = false;


    public void SetCoord(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    //moveTime represents the length of the movement transition
    public void Move(int x, int y, float moveTime)
    {
        if (!m_isMoving) StartCoroutine(MoveRoutine(new Vector3(x, y, 0), moveTime));
    }

    IEnumerator MoveRoutine(Vector3 destination, float moveTime)
    {
        Vector3 startPos = transform.position;
        bool reachedDestination = false;
        float elapsedTime = 0f;

        m_isMoving = true;

        while (!reachedDestination)
        {
            if (Vector3.Distance(transform.position, destination) < 0.01f)
            {
                reachedDestination = true;
                if (Board.myBoard != null) Board.myBoard.PlaceGem(this, (int)destination.x, (int)destination.y);
                break;
            }

            elapsedTime += Time.deltaTime;

            float lerpValue = elapsedTime / moveTime;

            //smoothing the interpolation
            lerpValue = Mathf.Pow(lerpValue, 3) * (lerpValue * (6 * lerpValue - 15) + 10);

            transform.position = Vector3.Lerp(startPos, destination, lerpValue);


            yield return null;

        }

        m_isMoving = false;

    }

}

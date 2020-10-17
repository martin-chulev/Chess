using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceController : MonoBehaviour
{
    public GameController GameController;
    public GameObject WhitePieces;
    public GameObject BlackPieces;
    public Sprite QueenSprite;

    public float MoveSpeed = 20;

    public float HighestRankY = 3.5f;
    public float LowestRankY = -3.5f;

    [HideInInspector]
    public bool DoubleStep = false;
    [HideInInspector]
    public bool MovingY = false;
    [HideInInspector]
    public bool MovingX = false;

    private Vector3 oldPosition;
    private Vector3 newPositionY;
    private Vector3 newPositionX;

    private bool moved = false;

    // Use this for initialization
    void Start()
    {
        if (GameController == null) GameController = FindObjectOfType<GameController>();
        if (this.name.Contains("Knight")) MoveSpeed *= 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (MovingY || MovingX)
        {
            if (Mathf.Abs(oldPosition.x - newPositionX.x) == Mathf.Abs(oldPosition.y - newPositionX.y))
            {
                MoveDiagonally();
            }
            else
            {
                MoveSideBySide();
            }
        }
    }

    void OnMouseDown()
    {
        if (GameController.SelectedPiece != null && GameController.SelectedPiece.GetComponent<PieceController>().IsMoving() == true)
        {
            // Prevent clicks during movement
            return;
        }

        if (GameController.SelectedPiece == this.gameObject)
        {
            GameController.DeselectPiece();
        }
        else
        {
            if (GameController.SelectedPiece == null)
            {
                GameController.SelectPiece(this.gameObject);
            }
            else
            {
                if (this.tag == GameController.SelectedPiece.tag)
                {
                    GameController.SelectPiece(this.gameObject);
                }
                else if ((this.tag == "White" && GameController.SelectedPiece.tag == "Black") || (this.tag == "Black" && GameController.SelectedPiece.tag == "White"))
                {
                    GameController.SelectedPiece.GetComponent<PieceController>().MovePiece(this.transform.position);
                }
            }
        }
    }

    public bool MovePiece(Vector3 newPosition, bool castling = false)
    {
        GameObject encounteredEnemy = null;

        newPosition.z = this.transform.position.z;
        this.oldPosition = this.transform.position;

        if (castling || ValidateMovement(oldPosition, newPosition, out encounteredEnemy))
        {
            // Double-step
            if (this.name.Contains("Pawn") && Mathf.Abs(oldPosition.y - newPosition.y) == 2)
            {
                this.DoubleStep = true;
            }
            // Promotion
            else if (this.name.Contains("Pawn") && (newPosition.y == HighestRankY || newPosition.y == LowestRankY))
            {
                this.Promote();
            }
            // Castling
            else if (this.name.Contains("King") && Mathf.Abs(oldPosition.x - newPosition.x) == 2)
            {
                if (oldPosition.x - newPosition.x == 2) // queenside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x - 4, oldPosition.y, this.tag);
                    Vector3 newRookPosition = oldPosition;
                    newRookPosition.x -= 1;
                    rook.GetComponent<PieceController>().MovePiece(newRookPosition, true);
                }
                else if (oldPosition.x - newPosition.x == -2) // kingside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x + 3, oldPosition.y, this.tag);
                    Vector3 newRookPosition = oldPosition;
                    newRookPosition.x += 1;
                    rook.GetComponent<PieceController>().MovePiece(newRookPosition, true);
                }
            }
            this.moved = true;

            this.newPositionY = newPosition;
            this.newPositionY.x = this.transform.position.x;
            this.newPositionX = newPosition;
            MovingY = true; // Start movement

            Destroy(encounteredEnemy);
            return true;
        }
        else
        {
            GameController.GetComponent<AudioSource>().Play();
            return false;
        }
    }

    public bool ValidateMovement(Vector3 oldPosition, Vector3 newPosition, out GameObject encounteredEnemy, bool excludeCheck = false)
    {
        bool isValid = false;
        encounteredEnemy = GetPieceOnPosition(newPosition.x, newPosition.y);

        if ((oldPosition.x == newPosition.x && oldPosition.y == newPosition.y) || encounteredEnemy != null && encounteredEnemy.tag == this.tag)
        {
            return false;
        }

        if (this.name.Contains("King"))
        {
            // If the path is 1 square away in any direction
            if (Mathf.Abs(oldPosition.x - newPosition.x) <= 1 && Mathf.Abs(oldPosition.y - newPosition.y) <= 1)
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
            // Check for castling
            else if (Mathf.Abs(oldPosition.x - newPosition.x) == 2 && oldPosition.y == newPosition.y && this.moved == false)
            {
                if (oldPosition.x - newPosition.x == 2) // queenside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x - 4, oldPosition.y, this.tag);
                    if (rook.name.Contains("Rook") && rook.GetComponent<PieceController>().moved == false &&
                        CountPiecesBetweenPoints(oldPosition, rook.transform.position, Direction.Horizontal) == 0)
                    {
                        if (excludeCheck == true ||
                            (excludeCheck == false &&
                             IsInCheck(new Vector3(oldPosition.x - 0, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x - 1, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x - 2, oldPosition.y)) == false))
                        {
                            isValid = true;
                        }
                    }
                }
                else if (oldPosition.x - newPosition.x == -2) // kingside castling
                {
                    GameObject rook = GetPieceOnPosition(oldPosition.x + 3, oldPosition.y, this.tag);
                    if (rook.name.Contains("Rook") && rook.GetComponent<PieceController>().moved == false &&
                        CountPiecesBetweenPoints(oldPosition, rook.transform.position, Direction.Horizontal) == 0)
                    {
                        if (excludeCheck == true ||
                            (excludeCheck == false &&
                             IsInCheck(new Vector3(oldPosition.x + 0, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x + 1, oldPosition.y)) == false &&
                             IsInCheck(new Vector3(oldPosition.x + 2, oldPosition.y)) == false))
                        {
                            isValid = true;
                        }
                    }
                }
            }
        }

        if (this.name.Contains("Rook") || this.name.Contains("Queen"))
        {
            // If the path is a straight horizontal or vertical line
            if ((oldPosition.x == newPosition.x && CountPiecesBetweenPoints(oldPosition, newPosition, Direction.Vertical) == 0) ||
                (oldPosition.y == newPosition.y && CountPiecesBetweenPoints(oldPosition, newPosition, Direction.Horizontal) == 0))
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
        }

        if (this.name.Contains("Bishop") || this.name.Contains("Queen"))
        {
            // If the path is a straight diagonal line
            if (Mathf.Abs(oldPosition.x - newPosition.x) == Mathf.Abs(oldPosition.y - newPosition.y) &&
                CountPiecesBetweenPoints(oldPosition, newPosition, Direction.Diagonal) == 0)
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
        }

        if (this.name.Contains("Knight"))
        {
            // If the path is an 'L' shape
            if ((Mathf.Abs(oldPosition.x - newPosition.x) == 1 && Mathf.Abs(oldPosition.y - newPosition.y) == 2) ^
                (Mathf.Abs(oldPosition.x - newPosition.x) == 2 && Mathf.Abs(oldPosition.y - newPosition.y) == 1))
            {
                if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                {
                    isValid = true;
                }
            }
        }

        if (this.name.Contains("Pawn"))
        {
            // If the new position is on the rank above (White) or below (Black)
            if ((this.tag == "White" && oldPosition.y + 1 == newPosition.y) ||
               (this.tag == "Black" && oldPosition.y - 1 == newPosition.y))
            {
                GameObject otherPiece = GetPieceOnPosition(newPosition.x, newPosition.y);

                // If moving forward
                if (oldPosition.x == newPosition.x && otherPiece == null)
                {
                    if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                    {
                        isValid = true;
                    }
                }
                // If moving diagonally
                else if (oldPosition.x == newPosition.x - 1 || oldPosition.x == newPosition.x + 1)
                {
                    // Check if en passant is available
                    if (otherPiece == null)
                    {
                        otherPiece = GetPieceOnPosition(newPosition.x, oldPosition.y);
                        if (otherPiece != null && otherPiece.GetComponent<PieceController>().DoubleStep == false)
                        {
                            otherPiece = null;
                        }
                    }
                    // If an enemy piece is encountered
                    if (otherPiece != null && otherPiece.tag != this.tag)
                    {
                        if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                        {
                            isValid = true;
                        }
                    }
                }

                encounteredEnemy = otherPiece;
            }
            // Double-step
            else if ((this.tag == "White" && oldPosition.x == newPosition.x && oldPosition.y + 2 == newPosition.y) ||
                     (this.tag == "Black" && oldPosition.x == newPosition.x && oldPosition.y - 2 == newPosition.y))
            {
                if (this.moved == false && GetPieceOnPosition(newPosition.x, newPosition.y) == null)
                {
                    if (excludeCheck == true || (excludeCheck == false && IsInCheck(newPosition) == false))
                    {
                        isValid = true;
                    }
                }
            }
        }

        return isValid;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color">"White" or "Black" for specific color, null for any color</param>
    /// <returns>Returns the piece on a given position on the board, null if the square is empty</returns>
    GameObject GetPieceOnPosition(float positionX, float positionY, string color = null)
    {
        if (color == null || color.ToLower() == "white")
        {
            foreach (Transform piece in WhitePieces.transform)
            {
                if (piece.position.x == positionX && piece.position.y == positionY)
                {
                    return piece.gameObject;
                }
            }
        }
        if (color == null || color.ToLower() == "black")
        {
            foreach (Transform piece in BlackPieces.transform)
            {
                if (piece.position.x == positionX && piece.position.y == positionY)
                {
                    return piece.gameObject;
                }
            }
        }

        return null;
    }

    int CountPiecesBetweenPoints(Vector3 pointA, Vector3 pointB, Direction direction)
    {
        int count = 0;

        foreach (Transform piece in WhitePieces.transform)
        {
            if ((direction == Direction.Horizontal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) && piece.position.y == pointA.y) ||
                (direction == Direction.Vertical && piece.position.y > Mathf.Min(pointA.y, pointB.y) && piece.position.y < Mathf.Max(pointA.y, pointB.y) && piece.position.x == pointA.x))
            {
                count++;
            }
            else if (direction == Direction.Diagonal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) &&
                     ((pointA.y - pointA.x == pointB.y - pointB.x && piece.position.y - piece.position.x == pointA.y - pointA.x) ||
                      (pointA.y + pointA.x == pointB.y + pointB.x && piece.position.y + piece.position.x == pointA.y + pointA.x)))
            {
                count++;
            }
        }
        foreach (Transform piece in BlackPieces.transform)
        {
            if ((direction == Direction.Horizontal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) && piece.position.y == pointA.y) ||
                (direction == Direction.Vertical && piece.position.y > Mathf.Min(pointA.y, pointB.y) && piece.position.y < Mathf.Max(pointA.y, pointB.y) && piece.position.x == pointA.x))
            {
                count++;
            }
            else if (direction == Direction.Diagonal && piece.position.x > Mathf.Min(pointA.x, pointB.x) && piece.position.x < Mathf.Max(pointA.x, pointB.x) &&
                     ((pointA.y - pointA.x == pointB.y - pointB.x && piece.position.y - piece.position.x == pointA.y - pointA.x) ||
                      (pointA.y + pointA.x == pointB.y + pointB.x && piece.position.y + piece.position.x == pointA.y + pointA.x)))
            {
                count++;
            }
        }

        return count;
    }

    public bool IsInCheck(Vector3 potentialPosition)
    {
        bool isInCheck = false;

        // Temporarily move piece to the wanted position
        Vector3 currentPosition = this.transform.position;
        this.transform.SetPositionAndRotation(potentialPosition, this.transform.rotation);

        GameObject encounteredEnemy;

        if (this.tag == "Black")
        {
            Vector3 kingPosition = BlackPieces.transform.Find("Black King").position;
            foreach (Transform piece in WhitePieces.transform)
            {
                // If piece is not potentially captured
                if (piece.position.x != potentialPosition.x || piece.position.y != potentialPosition.y) {
                    if (piece.GetComponent<PieceController>().ValidateMovement(piece.position, kingPosition, out encounteredEnemy, true))
                    {
                        Debug.Log("Black King is in check by: " + piece);
                        isInCheck = true;
                        break;
                    }
                }
            }
        }
        else if (this.tag == "White")
        {
            Vector3 kingPosition = WhitePieces.transform.Find("White King").position;
            foreach (Transform piece in BlackPieces.transform)
            {
                // If piece is not potentially captured
                if (piece.position.x != potentialPosition.x || piece.position.y != potentialPosition.y)
                {
                    if (piece.GetComponent<PieceController>().ValidateMovement(piece.position, kingPosition, out encounteredEnemy, true))
                    {
                        Debug.Log("White King is in check by: " + piece);
                        isInCheck = true;
                        break;
                    }
                }
            }
        }

        // Move back to the real position
        this.transform.SetPositionAndRotation(currentPosition, this.transform.rotation);
        return isInCheck;
    }

    void MoveSideBySide()
    {
        if (MovingY == true)
        {
            this.transform.SetPositionAndRotation(Vector3.Lerp(this.transform.position, newPositionY, Time.deltaTime * MoveSpeed), this.transform.rotation);
            if (this.transform.position == newPositionY)
            {
                MovingY = false;
                MovingX = true;
            }
        }
        if (MovingX == true)
        {
            this.transform.SetPositionAndRotation(Vector3.Lerp(this.transform.position, newPositionX, Time.deltaTime * MoveSpeed), this.transform.rotation);
            if (this.transform.position == newPositionX)
            {
                this.transform.SetPositionAndRotation(newPositionX, this.transform.rotation);
                MovingX = false;
                if (GameController.SelectedPiece != null)
                {
                    GameController.DeselectPiece();
                    GameController.EndTurn();
                }
            }
        }
    }

    void MoveDiagonally()
    {
        if (MovingY == true)
        {
            this.transform.SetPositionAndRotation(Vector3.Lerp(this.transform.position, newPositionX, Time.deltaTime * MoveSpeed), this.transform.rotation);
            if (this.transform.position == newPositionX)
            {
                this.transform.SetPositionAndRotation(newPositionX, this.transform.rotation);
                MovingY = false;
                MovingX = false;
                if (GameController.SelectedPiece != null)
                {
                    GameController.DeselectPiece();
                    GameController.EndTurn();
                }
            }
        }
    }

    void Promote()
    {
        this.name = this.name.Replace("Pawn", "Queen");
        this.GetComponent<SpriteRenderer>().sprite = QueenSprite;
    }

    public bool IsMoving()
    {
        return MovingX || MovingY;
    }

    enum Direction
    {
        Horizontal,
        Vertical,
        Diagonal
    }
}

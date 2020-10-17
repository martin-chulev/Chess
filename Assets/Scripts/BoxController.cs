using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxController : MonoBehaviour
{
    public GameController GameController;

    public float HighestRankY = 3.5f;
    public float LowestRankY = -3.5f;

    // Use this for initialization
    void Start()
    {
        if (GameController == null) GameController = FindObjectOfType<GameController>();

        string algebraicName = "";
        algebraicName += (char)(this.transform.position.x - LowestRankY + 'A');
        algebraicName += this.transform.position.y - LowestRankY + 1;
        this.transform.parent.name = algebraicName;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnMouseDown()
    {
        if (GameController.SelectedPiece != null && GameController.SelectedPiece.GetComponent<PieceController>().IsMoving() == true)
        {
            // Prevent clicks during movement
            return;
        }

        if (GameController.SelectedPiece != null)
        {
            GameController.SelectedPiece.GetComponent<PieceController>().MovePiece(this.transform.position);
        }
    }
}

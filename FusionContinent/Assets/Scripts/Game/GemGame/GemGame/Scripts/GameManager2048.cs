using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager2048 : MonoBehaviour
{
    public TileBoard board;
    public Action GameOverAction;

    private void Start()
    {
       // NewGame();
    }
    

    public void NewGame()
    {
        board.ClearBoard();
        board.CreateTile();
        board.CreateTile();
        board.enabled = true;
    }

    public void GameOver()
    {
        board.enabled = false;
        GameOverAction?.Invoke();
    }
}

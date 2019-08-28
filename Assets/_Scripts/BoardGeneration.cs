using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGeneration : MonoBehaviour
{
    public Color[] BlockColors;
    [SerializeField] private GameObject block;
    [SerializeField] private int tryCount = 100;

    public void GenerateGameBoard(ref BlockStruct[,] GameBoard, BoardManager boardManager)
    {
        for (int x = 0; x < GameBoard.GetLength(0); x++)
        {
            for (int y = 0; y < GameBoard.GetLength(1); y++)
            {
                GameBoard[x, y] = new BlockStruct();

                #region "Без стартовых 'Матчей'";
                List<Color> possibleColors = new List<Color>();
                possibleColors.AddRange(BlockColors);
                if (x > 1 && GameBoard[x - 2, y].Color == GameBoard[x - 1, y].Color)
                {
                    possibleColors.Remove(GameBoard[x - 1, y].Color);
                }
                if (y > 1 && GameBoard[x, y - 1].Color == GameBoard[x, y - 2].Color)
                {
                    possibleColors.Remove(GameBoard[x, y - 1].Color);
                }
                Color blockColor = possibleColors[Random.Range(0, possibleColors.Count)];
                #endregion;

                GameBoard[x, y].SetStartData(x,y,blockColor);
            }
        }

        int counter = 0;
        while(!boardManager.CheckPossibleMatches() || counter >=  tryCount)
        {
            GameBoard = RegenerateBoard(GameBoard);
            counter++;
        }

        if(counter >= tryCount)
        {
            boardManager.EndGame();
            Debug.Log("Cant start the game 100 times!");
        }
    }

    private BlockStruct[,] RegenerateBoard(BlockStruct[,] GameBoard)
    {
        for (int x = 0; x < GameBoard.GetLength(0); x++)
        {
            for (int y = 0; y < GameBoard.GetLength(1); y++)
            {
                #region "Без стартовых 'Матчей'";
                List<Color> possibleColors = new List<Color>();
                possibleColors.AddRange(BlockColors);
                if (x > 1 && GameBoard[x - 2, y].Color == GameBoard[x - 1, y].Color)
                {
                    possibleColors.Remove(GameBoard[x - 1, y].Color);
                }
                if (y > 1 && GameBoard[x, y - 1].Color == GameBoard[x, y - 2].Color)
                {
                    possibleColors.Remove(GameBoard[x, y - 1].Color);
                }
                Color blockColor = possibleColors[Random.Range(0, possibleColors.Count)];
                #endregion;

                GameBoard[x, y].Color = blockColor;
            }
        }
        return GameBoard;
    }

    public void VisualizeBlocks(BlockStruct[,] GameBoard)
    {
        for (int x = 0; x < GameBoard.GetLength(0); x++)
        {
            for (int y = 0; y < GameBoard.GetLength(1); y++)
            {
                GameObject newBlock = Instantiate(this.block, new Vector3(x, 0, y), Quaternion.identity);
                newBlock.transform.parent = transform;
                newBlock.name = $"Block[{x},{y}]";
                BlockScript blockScript = newBlock.GetComponent<BlockScript>();
                blockScript.SetStartData(GameBoard[x, y].Color, x, y);
                GameBoard[x, y].SetDelegates(
                    blockScript.Deactivate,
                    blockScript.Activate,
                    blockScript.Deselect,
                    blockScript.Select,
                    blockScript.SetColor
                );
            }
        }
    }
}

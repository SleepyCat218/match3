using UnityEngine;

public class BlockScript : MonoBehaviour
{
    private BoardManager boardManager;
    private int _x;
    private int _y;
    private Color _selectedColor;
    private Color _originColor;
    private MeshRenderer render;

    #region "Unity functions";
    private void Awake()
    {
        render = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        boardManager = transform.parent.GetComponent<BoardManager>();
        render.material.color = _originColor;
    }

    private void OnMouseDown()
    {
        if (!render.enabled)
        {
            return;
        }
        BlockStruct block = boardManager.GameBoard[_x, _y];
        if (block.selected)
        {
            boardManager.Deselect(_x, _y);
        }
        else
        {
            if (boardManager.prevSelected == null)
            {
                boardManager.Select(_x, _y);
            }
            else
            {
                if (boardManager.IsBlockNeighbour(_x, _y))
                {
                    boardManager.SwapBlocks(boardManager.GameBoard[_x, _y], boardManager.prevSelected);
                    if(!boardManager.CheckMatchesForSwappedBlocks(boardManager.prevSelected, boardManager.GameBoard[_x, _y]))
                    {
                        boardManager.SwapBlocks(boardManager.GameBoard[_x, _y], boardManager.prevSelected);
                        
                    }
                    boardManager.Deselect(boardManager.prevSelected.X, boardManager.prevSelected.Y);
                }
                else
                {
                    boardManager.Deselect(boardManager.prevSelected.X, boardManager.prevSelected.Y);
                    boardManager.Select(_x, _y);
                }
            }
        }
    }
    #endregion;

    public void SetStartData(Color originColor, int x, int y)
    {
        SetColor(originColor);
        _x = x;
        _y = y;
    }

    #region "Делегаты";
    public void Select()
    {
        render.material.color = _selectedColor;
        PlaySoundSelected();
    }

    public void Deactivate()
    {
        render.enabled = false;
    }

    public void Activate()
    {
        render.enabled = true;
    }

    public void Deselect()
    {
        render.material.color = _originColor;
    }

    public void SetColor(Color color)
    {
        _originColor = color;
        _selectedColor = new Color(_originColor.r / 2, _originColor.g / 2, _originColor.b / 2);
        render.material.color = _originColor;
    }
    #endregion;

    private void PlaySoundSelected()
    {
        //SFXManager.instance.PlaySFX(Clip.Select);
    }
}

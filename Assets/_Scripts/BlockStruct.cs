using UnityEngine;

public class BlockStruct
{
    public int X, Y;
    public Color Color;
    public bool selected = false;
    public bool deleted = false;
    public bool cleared = false;

    public void SetStartData(int x, int y, Color color)
    {
        this.X = x;
        this.Y = y;
        this.Color = color;
    }

    public delegate void VisualChangeColorDelegate(Color color);
    public delegate void VisualClickDelegate();

    public VisualClickDelegate VisualDeactivate;
    public VisualClickDelegate VisualActivate;

    public VisualClickDelegate VisualDeselect;
    public VisualClickDelegate VisualSelect;
    public VisualChangeColorDelegate VisualChangeColor;

    public void SwapColor(BlockStruct otherBlock)
    {
        Color tempColor = otherBlock.Color;
        otherBlock.Color = this.Color;
        this.Color = tempColor;
    }

    public void SwapForShift(BlockStruct otherBlock)
    {
        Color tempCol = otherBlock.Color;
        bool tempCleared = otherBlock.cleared;
        bool tempDel = otherBlock.deleted;

        otherBlock.Color = this.Color;
        this.Color = tempCol;

        otherBlock.deleted = this.deleted;
        this.deleted = tempDel;

        otherBlock.cleared = this.cleared;
        this.cleared = tempCleared;
    }

    public void SetDelegates(VisualClickDelegate deactivate, VisualClickDelegate activate, VisualClickDelegate deselect, VisualClickDelegate select, VisualChangeColorDelegate changeColor)
    {
        VisualDeactivate = deactivate;
        VisualActivate = activate;
        VisualDeselect = deselect;
        VisualSelect = select;
        VisualChangeColor = changeColor;
    }
}

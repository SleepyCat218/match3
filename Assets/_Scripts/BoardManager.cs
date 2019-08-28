using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    
    private GameObject[,] tiles;
    public int xSize, ySize;
    public BlockStruct[,] GameBoard;
    public BlockStruct prevSelected = null;
    private BoardGeneration boardGeneration;

    void Awake()
    {
        boardGeneration = GetComponent<BoardGeneration>();
        GameBoard = new BlockStruct[xSize, ySize];
        boardGeneration.GenerateGameBoard(ref GameBoard, this);
        boardGeneration.VisualizeBlocks(GameBoard);
        prevSelected = null;
    }

    public void Deselect(int x, int y)
    {
        GameBoard[x, y].selected = false;
        GameBoard[x, y].VisualDeselect?.Invoke();
        prevSelected = null;
    }

    public void Select(int x, int y)
    {
        GameBoard[x, y].selected = true;
        prevSelected = GameBoard[x, y];
        GameBoard[x, y].VisualSelect?.Invoke();
    }

    public bool IsBlockNeighbour(int x, int y)
    {
        if (Mathf.Abs(prevSelected.X - x) == 1 && prevSelected.Y == y)
        {
            return true;
        }
        if (Mathf.Abs(prevSelected.Y - y) == 1 && prevSelected.X == x)
        {
            return true;
        }
        return false;
    }

    public void SwapBlocks(BlockStruct firstBlock, BlockStruct secondBlock)
    {
        firstBlock.SwapColor(secondBlock);
        secondBlock.VisualChangeColor?.Invoke(secondBlock.Color);
        firstBlock.VisualChangeColor?.Invoke(firstBlock.Color);
    }

    private List<BlockStruct> CheckVerticalMatches(int x, int startY)
    {
        Color matchingColor = GameBoard[x, startY].Color;
        List<BlockStruct> verticalMatches = new List<BlockStruct>();
        int y = startY;
        while(y > 0 && GameBoard[x,y-1].Color == matchingColor)
        {
            verticalMatches.Add(GameBoard[x, y-1]);
            y--;
        }
        y = startY;
        while (y < ySize-1 && GameBoard[x, y+1].Color == matchingColor)
        {
            verticalMatches.Add(GameBoard[x, y+1]);
            y++;
        }
        
        if(verticalMatches.Count>1)
        {
            return verticalMatches;
        }
        else
        {
            return new List<BlockStruct>();
        }
    }

    private List<BlockStruct> CheckHorizontalMatches(int startX, int y)
    {
        Color matchingColor = GameBoard[startX, y].Color;
        List<BlockStruct> horizontalMatches = new List<BlockStruct>();
        int x = startX;
        while (x > 0 && GameBoard[x-1, y].Color == matchingColor)
        {
            horizontalMatches.Add(GameBoard[x-1, y]);
            x--;
        }
        x = startX;
        while (x < xSize - 1 && GameBoard[x + 1, y].Color == matchingColor)
        {
            horizontalMatches.Add(GameBoard[x + 1, y]);
            x++;
        }

        if (horizontalMatches.Count > 1)
        {
            return horizontalMatches;
        }
        else
        {
            return new List<BlockStruct>();
        }
    }

    /// <summary>
    /// Метод проверяет наличие "матчей" после свапа для блоков, которые были свапнуты. Если есть, удаляем "матчи" и затем производим "осыпание" доски.
    /// </summary>
    /// <param name="prevBlock">Блок, на который кликнули ранее.</param>
    /// <param name="currentBlock">Блок, на который кликнули сейчас</param>
    /// <returns>Метод возвращает были "матчи" после свапа или нет</returns>
    public bool CheckMatchesForSwappedBlocks(BlockStruct prevBlock, BlockStruct currentBlock)
    {
        List<BlockStruct> matchingBlocks = new List<BlockStruct>();
        matchingBlocks.AddRange(CheckVerticalMatches(prevBlock.X, prevBlock.Y));
        matchingBlocks.AddRange(CheckHorizontalMatches(prevBlock.X, prevBlock.Y));
        if (matchingBlocks.Count > 0)
        {
            matchingBlocks.Add(prevBlock);
        }

        List<BlockStruct> verticalMatchingBlocks = new List<BlockStruct>();
        verticalMatchingBlocks.AddRange(CheckVerticalMatches(currentBlock.X, currentBlock.Y));
        verticalMatchingBlocks.AddRange(CheckHorizontalMatches(currentBlock.X, currentBlock.Y));
        if(verticalMatchingBlocks.Count>0)
        {
            matchingBlocks.Add(currentBlock);
            matchingBlocks.AddRange(verticalMatchingBlocks);
        }

        if (matchingBlocks.Count > 0)
        {
            ClearMatches(matchingBlocks);
            FindNullBlocks();
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Метод проверяет есть ли "матчи" для блока по заданным координатам
    /// </summary>
    /// <param name="x">Координата X</param>
    /// <param name="y">Координата Y</param>
    /// <param name="existMatches">Список блоков, которые уже есть в "матчах", чтобы не добавлять повторно</param>
    /// <returns>Метод возвращает список блоков, которые составляют "матч"</returns>
    public List<BlockStruct> CheckMatches(int x, int y, List<BlockStruct> existMatches)
    {
        if(GameBoard[x,y].deleted || GameBoard[x,y].cleared)
        {
            return new List<BlockStruct>();
        }
        List<BlockStruct> matchingBlocks = new List<BlockStruct>();
        matchingBlocks.AddRange(CheckVerticalMatches(x, y));
        matchingBlocks.AddRange(CheckHorizontalMatches(x, y));
        if(matchingBlocks.Count>0)
        {
            matchingBlocks.Add(GameBoard[x, y]);
            List<BlockStruct> result = new List<BlockStruct>();
            foreach (var item in matchingBlocks)
            {
                if(!existMatches.Contains(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }
        return new List<BlockStruct>();
    }

    public void ClearMatches(List<BlockStruct> matches)
    {
        foreach (var item in matches)
        {
            item.deleted = true;
            item.Color = Color.black;
            item.VisualChangeColor?.Invoke(item.Color);
            item.VisualDeactivate?.Invoke();
        }
    }

    /// <summary>
    /// 1) Метод производит "осыпание" доски
    /// 2) Далее метод проверку на наличие новых матчей (до появления новых блоков).Если таковые есть, метод убирает новые матчи и производит повторное "осыпание" доски.
    /// 3) Выполняется пункт 2 пока при проверке новых матчей таковых не обнаружится.
    /// 4) Генерируются новые блоки.
    /// </summary>
    private void FindNullBlocks()
    {
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                if (GameBoard[x, y].deleted)
                {
                    ShiftBlocksDown(x, y);
                    break;
                }
            }
        }

        #region 'После "осыпания' ищем новые 'матчи' в цикле, пока на доске не останется 'матчей'".
        bool needRepeat = false;
        do
        {
            List<BlockStruct> matchingBlocksAfterBlocksShifted = new List<BlockStruct>();

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    matchingBlocksAfterBlocksShifted.AddRange(CheckMatches(x, y, matchingBlocksAfterBlocksShifted));
                }
            }

            if (matchingBlocksAfterBlocksShifted.Count > 0)
            {
                needRepeat = true;
                ClearMatches(matchingBlocksAfterBlocksShifted);
            }
            else
            {
                needRepeat = false;
            }

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    if (GameBoard[x, y].deleted)
                    {
                        ShiftBlocksDown(x, y);
                        break;
                    }
                }
            }
        } while (needRepeat);
        #endregion;


        #region "Подбираем цвет блокам без цвета"
        List<BlockStruct> BlocksWithoutColor = new List<BlockStruct>();
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                if (GameBoard[x, y].cleared)
                {
                    BlocksWithoutColor.Add(GameBoard[x, y]);
                }
            }
        }

        //Счетчик введён, для гарантии что скрипт не уйдёт в бесконечный цикл
        int counter = 0;
        do
        {
            foreach (var item in BlocksWithoutColor)
            {
                item.Color = GetNewColor(item.X, item.Y);
            }
            counter++;
        } while (!CheckPossibleMatches() || counter >= 100);

        if(counter >= 100)
        {
            Debug.Log("There is no possible matches!");
            EndGame();
        }
        #endregion;

        VisualizeBlocks();
    }

    private void VisualizeBlocks()
    {
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                if (GameBoard[x, y].cleared)
                {
                    GameBoard[x, y].cleared = false;
                }
                GameBoard[x, y].VisualChangeColor?.Invoke(GameBoard[x, y].Color);
                GameBoard[x, y].VisualActivate?.Invoke();
            }
        }
    }

    /// <summary>
    /// Метод для осыпания колонки блоков. Уничтоженные блоки "поднимаются" вверх, остальные осыпаются по мере возможности.
    /// </summary>
    /// <param name="x">Координата X колонки блоков</param>
    /// <param name="yStart">Стартовая координата Y колонки блоков</param>
    private void ShiftBlocksDown(int x, int yStart)
    {
        List<BlockStruct> blocks = new List<BlockStruct>();
        int nullCount = 0;

        for (int y = yStart; y < ySize; y++)
        {
            if(GameBoard[x,y].deleted)
            {
                nullCount++;
            }
            blocks.Add(GameBoard[x,y]);
            
        }

        for (int i = 0; i < nullCount; i++)
        {
            for (int k = 0; k < blocks.Count - 1; k++)
            {
                blocks[k].SwapForShift(blocks[k + 1]);
            }
        }

        foreach (var item in blocks)
        {
            if(item.deleted)
            {
                item.cleared = true;
            }
            item.deleted = false;
        }
    }

    private Color GetNewColor(int x, int y)
    {
        List<Color> posCol = GetPossibleColors(GameBoard[x, y]);
        return posCol[Random.Range(0, posCol.Count)];
    }

    #region "Поиск возможных ходов по всей доске".
    /// <summary>
    /// Для проверки наличия возможных ходов нужно проверить кажый блок.
    /// Всего возможно 6 ситуаций, когда есть ход: 3 по горизонтали, 3 по вертикали.
    /// </summary>
    /// <returns></returns>
    public bool CheckPossibleMatches()
    {
        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                if (CheckHorizontalPossible(GameBoard[x, y]) || CheckVerticalPossible(GameBoard[x, y]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// По вертикали возможны следующие ситуации:
    /// 1) Два блока подряд одного цвета, следующий блок другого цвета, а рядом с этим блоком другого цвета (сверху, справа или слева) есть блок с нужным цветом
    /// 2) Два блока подряд одного цвета, под ними блок другого цвета, а рядом с этим блоком другого цвета (снизу, справа или слева) есть блок с нужным цветом
    /// 3) Два блока одного цвета, а между ними блок другого цвета, и слева или справа от блока другого цвета есть блок с нужным цветом
    /// </summary>
    /// <param name="block">Блок для которого ищем возможные ходы</param>
    /// <returns>Возвращает true, если есть возможные ходы, иначе false</returns>
    private bool CheckVerticalPossible(BlockStruct block)
    {
        if (block.Y < ySize - 1 && GameBoard[block.X, block.Y + 1].Color == block.Color)
        {
            if (block.Y > 0)
            {
                if (block.X > 0 && GameBoard[block.X - 1, block.Y - 1].Color == block.Color)
                {
                    return true;
                }
                if (block.X < xSize - 1 && GameBoard[block.X + 1, block.Y - 1].Color == block.Color)
                {
                    return true;
                }
                if (block.Y > 1 && GameBoard[block.X, block.Y -2].Color == block.Color)
                {
                    return true;
                }
            }
            if (block.Y < ySize - 2)
            {
                if (block.X > 0 && GameBoard[block.X - 1, block.Y + 2].Color == block.Color)
                {
                    return true;
                }
                if (block.X < xSize - 1 && GameBoard[block.X + 1, block.Y + 2].Color == block.Color)
                {
                    return true;
                }
                if (block.Y < ySize - 3 && GameBoard[block.X, block.Y + 3].Color == block.Color)
                {
                    return true;
                }
            }
        }

        if (block.Y < ySize - 2 && GameBoard[block.X, block.Y + 2].Color == block.Color)
        {
            if (block.X > 0 && GameBoard[block.X - 1, block.Y + 1].Color == block.Color)
            {
                return true;
            }
            if (block.X < xSize - 1 && GameBoard[block.X + 1, block.Y + 1].Color == block.Color)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// См. описание метода для поиска по вертикали. Аналогично только по горизонтали
    /// </summary>
    /// <param name="block">Блок для которого ищем возможные ходы</param>
    /// <returns>Возвращает true, если есть возможные ходы, иначе false</returns>
    private bool CheckHorizontalPossible(BlockStruct block)
    {
        if (block.X < xSize - 1 && GameBoard[block.X + 1, block.Y].Color == block.Color)
        {
            if (block.X > 0)
            {
                if (block.Y > 0 && GameBoard[block.X - 1, block.Y - 1].Color == block.Color)
                {
                    return true;
                }
                if (block.Y < ySize - 1 && GameBoard[block.X - 1, block.Y + 1].Color == block.Color)
                {
                    return true;
                }
                if (block.X > 1 && GameBoard[block.X - 2, block.Y].Color == block.Color)
                {
                    return true;
                }
            }
            if (block.X < xSize - 2)
            {
                if (block.Y > 0 && GameBoard[block.X + 2, block.Y - 1].Color == block.Color)
                {
                    return true;
                }
                if (block.Y < ySize - 1 && GameBoard[block.X + 2, block.Y + 1].Color == block.Color)
                {
                    return true;
                }
                if (block.X < xSize - 3 && GameBoard[block.X + 3, block.Y].Color == block.Color)
                {
                    return true;
                }
            }
        }

        if (block.X < xSize - 2 && GameBoard[block.X + 2, block.Y].Color == block.Color)
        {
            if (block.Y > 0 && GameBoard[block.X + 1, block.Y - 1].Color == block.Color)
            {
                return true;
            }
            if (block.Y < ySize - 1 && GameBoard[block.X + 1, block.Y + 1].Color == block.Color)
            {
                return true;
            }
        }
        return false;
    }
    #endregion;

    /// <summary>
    /// Метод подбирает цвет для блока. Цвет не должен создать ситуацию "готовых матчей". Но также мы не должны выйти за границы массива.
    /// </summary>
    /// <param name="block">Блок, для которого подбираем цвет</param>
    /// <returns></returns>
    private List<Color> GetPossibleColors(BlockStruct block)
    {
        List<Color> possibleColors = new List<Color>();
        possibleColors.AddRange(boardGeneration.BlockColors);

        if (block.X > 1 && GameBoard[block.X - 2, block.Y].Color == GameBoard[block.X - 1, block.Y].Color)
        {
            possibleColors.Remove(GameBoard[block.X - 1, block.Y].Color);
        }
        if (block.X < xSize - 2 && GameBoard[block.X + 1, block.Y].Color == GameBoard[block.X + 2, block.Y].Color && !GameBoard[block.X + 1, block.Y].cleared && !GameBoard[block.X + 2, block.Y].cleared)
        {
            possibleColors.Remove(GameBoard[block.X + 1, block.Y].Color);
        }
        if (block.X > 0 && block.X < xSize - 1 && GameBoard[block.X + 1, block.Y].Color == GameBoard[block.X - 1, block.Y].Color && !GameBoard[block.X + 1, block.Y].cleared)
        {
            possibleColors.Remove(GameBoard[block.X + 1, block.Y].Color);
        }
        if (block.Y > 1 && GameBoard[block.X, block.Y - 1].Color == GameBoard[block.X, block.Y - 2].Color)
        {
            possibleColors.Remove(GameBoard[block.X, block.Y - 1].Color);
        }
        if(block.Y > 0 && block.Y < ySize - 1 && GameBoard[block.X, block.Y + 1].Color == GameBoard[block.X, block.Y - 1].Color && !GameBoard[block.X, block.Y + 1].cleared)
        {
            possibleColors.Remove(GameBoard[block.X, block.Y - 1].Color);
        }
        if (block.Y < ySize - 2 && GameBoard[block.X, block.Y + 1].Color == GameBoard[block.X, block.Y + 2].Color && !GameBoard[block.X, block.Y + 1].cleared && !GameBoard[block.X, block.Y + 2].cleared)
        {
            possibleColors.Remove(GameBoard[block.X, block.Y + 1].Color);
        }

        if(possibleColors.Count > 0)
        {
            return possibleColors;
        }
        else
        {
            //такой ситуации быть с 6 цветами не может, но на всякий случай я её добавлю
            return new List<Color> { Color.white };
        }
    }


    public void EndGame()
    {
        Debug.Log("EndGame!");
    }
}

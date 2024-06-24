// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

namespace Chess;

/// <summary>
/// https://www.chessprogramming.org/Material#InsufficientMaterial
/// </summary>
internal class InsufficientMaterialRule : EndGameRule
{
    internal override EndgameType Type => EndgameType.InsufficientMaterial;

    public InsufficientMaterialRule(ChessComponent board) : base(board) { }

    internal override bool IsEndGame()
    {
        var pieces = board.Pieces.Cast<CellComponent>().ToList();
		
        return IsFirstLevelDraw(pieces)
            || IsSecondLevelDraw(pieces)
            || IsThirdLevelDraw(pieces);
    }

    private bool IsFirstLevelDraw(List<CellComponent> pieces)
    {
        return pieces.All(p => p.GetPiece()?.Type == FigureType.King);
    }

    private bool IsSecondLevelDraw(List<CellComponent> pieces)
    {
        var hasStrongPieces = pieces.Count(p => p.GetPiece()?.Type == FigureType.Pawn
                                                || p.GetPiece()?.Type == FigureType.Queen
                                                || p.GetPiece()?.Type == FigureType.Rook) > 0;

        // The only piece remaining will be Bishop or Knight, what results in draw
        return !hasStrongPieces && pieces.Count(p => p.GetPiece()?.Type != FigureType.King) == 1;
    }

    private bool IsThirdLevelDraw(List<CellComponent> pieces)
    {
        var isDraw = false;

        if (pieces.Count == 4)
        {
            if (pieces.All(p => p.GetPiece()?.Type == FigureType.King || p.GetPiece()?.Type == FigureType.Bishop))
            {
                var firstPiece = pieces.First(p => p.GetPiece()?.Type == FigureType.Bishop);
                var lastPiece = pieces.Last(p => p.GetPiece()?.Type == FigureType.Bishop);

                isDraw = firstPiece.Color != lastPiece.Color && BishopsAreOnSameColor();
            }
            else if (pieces.All(p => p.GetPiece()?.Type == FigureType.King || p.GetPiece()?.Type == FigureType.Knight))
            {
                var firstPiece = pieces.First(p => p.GetPiece()?.Type == FigureType.Knight);
                var lastPiece = pieces.Last(p => p.GetPiece()?.Type == FigureType.Knight);

                isDraw = firstPiece.Color == lastPiece.Color;
            }
        }

        return isDraw;
    }

    private bool BishopsAreOnSameColor()
    {
        var bishopsCoords = new List<Position>();

        for (short i = 0; i < board.Pieces.GetLength(0) && bishopsCoords.Count < 2; i++)
        {
            for (short j = 0; j < board.Pieces.GetLength(1) && bishopsCoords.Count < 2; j++)
            {
                if (board.Pieces[i, j].GetPiece()?.Type == FigureType.Bishop)
                    bishopsCoords.Add(new Position(i, j));
            }
        }

        return (bishopsCoords[0].X + bishopsCoords[1].X + bishopsCoords[0].Y + bishopsCoords[1].Y) % 2 == 0;
    }
}


// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

using System;
namespace Chess;

/// <summary>
/// Coordinate like Position on Chess table counting from 0
/// </summary>
public struct Position
{
    /// <summary>
    /// Whether X and Y are in valid range [0; ChessBoard.MAX_COLS/MAX_ROWS[
    /// </summary>
    public bool HasValue => HasValueX & HasValueY;

    /// <summary>
    /// Whether X is in range [0; ChessBoard.MAX_COLS[
    /// </summary>
    public bool HasValueX => X is >= 0 and < ChessComponent.MAX_COLS;

    /// <summary>
    /// Whether Y is in range [0; ChessBoard.MAX_ROWS[
    /// </summary>
    public bool HasValueY => Y is >= 0 and < ChessComponent.MAX_ROWS;

    /// <summary>
    /// Horizontal position (File) on chess board
    /// </summary>
    public short X { get; set; } = -1;

    /// <summary>
    /// Vertical position (Rank) on chess board
    /// </summary>
    public short Y { get; set; } = -1;

    /// <summary>
    /// Initializes an empty Position:<br/>
    /// {X = -1, Y = -1}
    /// </summary>
    public Position() { }

    /// <summary>
    /// Initializes a new Position in chess board<br/>
    /// Counting from 0 
    /// </summary>
    public Position(short x, short y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Equalizing 2 Positions
    /// </summary>
    public static bool operator ==(Position a, Position b) => a.X == b.X && a.Y == b.Y;

    /// <summary>
    /// Equalizing 2 Positions
    /// </summary>
    public static bool operator !=(Position a, Position b) => !(a.X == b.X && a.Y == b.Y);

    /// <summary>
    /// Equalizing 2 Positions
    /// </summary>
    public bool Equals(Position other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Position other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);
}

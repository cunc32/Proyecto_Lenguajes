using UnityEngine;
using System.Collections.Generic;
public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountx, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        
        // Top right
        int x = currentX + 1;
        int y = currentY + 2;
        if(x<tileCountx && y < tileCountY)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        x = currentX + 2;
        y = currentY + 1;

        if (x < tileCountx && y < tileCountY) 
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));
        
        // Top Left
        x = currentX - 1;
        y = currentY + 2;
        if (x >= 0 && y < tileCountY)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        x = currentX - 2;
        y = currentY + 1;
        if (x >= 0 && y < tileCountY)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));
        
        //Bottm Right
        x = currentX + 1;
        y = currentY - 2;
        if (x < tileCountx && y >= 0)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));
        
        x = currentX + 2;
        y = currentY + 1;

        if (x < tileCountx && y >= 0) 
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        //Bottm Left
        x = currentX - 1;
        y = currentY - 2;
        if (x >= 0 && y >= 0)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));
        
        x = currentX - 2 ;
        y = currentY - 1;

        if (x >= 0 && y >= 0) 
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));
        return r;
    }
}

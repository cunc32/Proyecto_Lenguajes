using Unity.VisualScripting;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System.Collections.Generic;

public class Chessboard : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = -0.4f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;
    
    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // LOGIC
    private ChessPiece[,] _chessPieces;
    private ChessPiece currentlyDragging;
    private bool dragging = false;
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private const int TILE_COUNT_CHESS = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int actualTile;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private bool isSelected;
    private void Awake()
    {
        isWhiteTurn = true;
        GenerateGrid(tileSize, TILE_COUNT_CHESS, TILE_COUNT_CHESS);
        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            // Obtenemos los indicies de las casillas sobre las que pasa el mouse
            Vector2Int hitPosition = getTileIndex(info.transform.gameObject);

            // Nada a seleccion
            if (actualTile == -Vector2Int.one)
            {
                // Targeted
                actualTile = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            // Seleccion a seleccion
            if (actualTile != hitPosition)
            {
                tiles[actualTile.x, actualTile.y].layer = (ContainsValidMove(ref availableMoves, actualTile)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                actualTile = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // Presionamos mouse para seleccionar
            if (!dragging && Input.GetMouseButtonDown(0))
            {
                if (_chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // Comprobar nuestro turno
                    if ((_chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn)||(_chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn))
                    {
                        currentlyDragging = _chessPieces[hitPosition.x, hitPosition.y];
                        // Obtén una lista de los lugares a los que puede ir el número 1 y resalta las casillas como soldadura.
                        availableMoves = currentlyDragging.GetAvailableMoves(ref _chessPieces, TILE_COUNT_CHESS, TILE_COUNT_CHESS);
                        dragging = true;
                        HighlightTiles();
                    }
                }
            }
            // Soltamos el mouse para colocar
            else if (dragging && Input.GetMouseButtonDown(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));

                currentlyDragging = null;
                dragging = false;
                RemoveHighlightTiles();

            }
            else if (currentlyDragging == null && Input.GetMouseButtonUp(0))
            {
                RemoveHighlightTiles();
            }
        }
        else
        {
            if (actualTile != -Vector2Int.one)
            {
                tiles[actualTile.x, actualTile.y].layer = LayerMask.NameToLayer("Tile");
                actualTile = -Vector2Int.one;
            }
            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        //Si estamos arrastrando una pieza.
        if (dragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }

    }
    
    // Generate the board
    private void GenerateGrid(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Spawning pieces
    private void SpawnAllPieces()
    {
        _chessPieces = new ChessPiece[TILE_COUNT_CHESS, TILE_COUNT_CHESS];
        int whiteTeam = 0, blackTeam = 1;

        // Spawn White Pieces
        _chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        _chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        _chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        _chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        _chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        _chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        _chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        _chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < 8; i++)
            _chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        // Spawn Black Pieces
        _chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        _chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        _chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        _chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        _chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        _chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        _chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        _chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < 8; i++)
            _chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1]).GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        cp.gameObject.layer = LayerMask.NameToLayer("Pieces");
        cp.SetScale(Vector3.one * 32.3f);

        return cp;
    }
    //Positioning 
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_CHESS; x++)
            for (int y = 0; y < TILE_COUNT_CHESS; y++)
            {
                if (_chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
                if ((x == 2 || x == 5) && y == 0)
                    _chessPieces[x, y].transform.Rotate(Vector3.back, -90);
                if ((x == 1 || x == 6) && y == 0)
                    _chessPieces[x, y].transform.Rotate(Vector3.back, 180);
                if ((x == 2 || x == 5) && y == 7)
                    _chessPieces[x, y].transform.Rotate(Vector3.back, 90);
            }
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        _chessPieces[x, y].currentX = x;
        _chessPieces[x, y].currentY = y;
        _chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //Highlight tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                tiles[i, j].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }
    
    //ChakeMate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
    }
    
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void OnResetButton()
    {
        //UI
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);
        
        //Fields reset
        currentlyDragging = null;
        availableMoves = new List<Vector2Int>();
        // Clean up
        for (int x = 0; x < TILE_COUNT_CHESS; x++)
        {
            for (int y = 0; y < TILE_COUNT_CHESS; y++)
            {
                if (_chessPieces[x, y] != null)
                    Destroy(_chessPieces[x, y].gameObject);

                _chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);

        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }

    public void OnExitButton()
    {
        Application.Quit();
    }
    
    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
            return false;
        
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
    
        //¿Hay otra pieza en la posición de destino?
        if (_chessPieces[x, y] != null)
        {

            ChessPiece ocp = _chessPieces[x, y];

            if (cp.team == ocp.team)
                return false;

            //Si es el equipo enemigo
            if (ocp.team == 0)
            {

                if (ocp.type == ChessPieceType.King)
                    CheckMate(1);
                
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                
                if (ocp.type == ChessPieceType.King)
                    CheckMate(0);
                
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);

                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        _chessPieces[x, y] = cp;
        _chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        
        return true;
    }
    private Vector2Int getTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_CHESS; x++)
            for (int y = 0; y < TILE_COUNT_CHESS; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one;
    }

}
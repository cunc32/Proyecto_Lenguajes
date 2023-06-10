using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class Chessboard : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = -0.4f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    [Header("Prefabs & Materials")] 
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    
    // LOGIC
    private ChessPiece[,] _chessPieces;
    private const int TILE_COUNT_CHESS = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int actualTile;
    
    private void Awake()
    {
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
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            // Obtenemos los indicies de las casillas sobre las que pasa el mouse
            Vector2Int hitPosition = getTileIndex(info.transform.gameObject);

            if (actualTile == -Vector2Int.one)
            {
                // Targeted
                actualTile = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            } 
            if (actualTile != hitPosition)
            {
                tiles[actualTile.x, actualTile.y].layer = LayerMask.NameToLayer("Tile");
                actualTile = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
        else
        {
            if (actualTile != -Vector2Int.one)
            {
                tiles[actualTile.x, actualTile.y].layer = LayerMask.NameToLayer("Tile");
                actualTile = -Vector2Int.one;
            }
        }
    }

    // Generate the board
    private void GenerateGrid(float tileSize, int tileCountX, int tileCountY)
    {
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
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y + 1) * tileSize);
        vertices[2] = new Vector3((x + 1) * tileSize, 0, y * tileSize);
        vertices[3] = new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize);

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
        cp.GetComponentInChildren<MeshRenderer>().material = teamMaterials[team];
        
        return cp;
    }

    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_CHESS; x++)
            for (int y = 0; y < TILE_COUNT_CHESS; y++)
            {
                if (_chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
                if ((x == 2 || x == 5) && y == 0)
                    _chessPieces[x, y].transform.rotation = Quaternion.Euler(0,90,0);
                if ((x == 2 || x == 5) && y == 7)
                    _chessPieces[x, y].transform.rotation = Quaternion.Euler(0,-90,0);
            }
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        _chessPieces[x, y].currentX = x;
        _chessPieces[x, y].currentY = y;
        Vector3 pos = _chessPieces[x, y].transform.position;
        _chessPieces[x, y].transform.position = new Vector3(x * tileSize + .5f, yOffset - 0.5f, y * tileSize + .5f);
    }

    
    // Operations
    private Vector2Int getTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_CHESS; x++)
            for (int y = 0; y < TILE_COUNT_CHESS; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
        
        return -Vector2Int.one;
    }
}

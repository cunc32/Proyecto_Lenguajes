using System;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System.Collections.Generic;
using Unity.Netcode;
using Random = System.Random;

public class Chessboard : NetworkBehaviour
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
    [SerializeField] private GameObject popMenu;
    [SerializeField] private Material[] personalizedMaterials;
    [SerializeField] private GameObject[] terrains;
    
    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private GameObject powerupPrefab;

    // LOGIC
    private struct movement : INetworkSerializable
    {
        public int prevX;
        public int prevY;
        public int newX;
        public int newY;
        public bool whiteTurn;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref prevX);
            serializer.SerializeValue(ref prevY);
            serializer.SerializeValue(ref newX);
            serializer.SerializeValue(ref newY);
            serializer.SerializeValue(ref whiteTurn);
        }
    }
    
    private NetworkVariable<movement> lastMovement = new NetworkVariable<movement>(new movement
    {
        prevX = 0,
        prevY = 0,
        newX = 0,
        newY = 0,
        whiteTurn = true
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private Powerup[,] powerups;
    private ChessPiece[,] _chessPieces;
    private ChessPiece currentlyDragging;
    private ChessPiece selectedPiece;
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
    private bool moving;
    private int myID;
    private int turnCount;
    private int moveCount;
    private int nextPowerup;
    private Vector2Int previousPosition;
    private int activeTerrain;
    
    public override void OnNetworkSpawn()
    {
        lastMovement.OnValueChanged += (movement previousValue, movement newValue) =>
        {
            Debug.Log("Message Received " + newValue.prevX + ", " + newValue.prevY + "\n" +
                      newValue.newX + ", " + newValue.newY);
            // ChessPiece movedPiece = _chessPieces[newValue.prevX, newValue.prevY];
            // Debug.Log('1');
            // MoveTo(_chessPieces[newValue.prevX, newValue.prevY], newValue.newX, newValue.newY, true);
            // Debug.Log('2');
            // isWhiteTurn = newValue.whiteTurn;
            // _chessPieces[newValue.newX, newValue.newY] = movedPiece;
            // _chessPieces[newValue.prevX, newValue.prevY] = null;
            // Debug.Log('3');
            // movedPiece.SetPosition(GetTileCenter(newValue.newX, newValue.newY));
            // Debug.Log('4');

            ChessPiece cp = _chessPieces[newValue.prevX, newValue.prevY];
            int x = newValue.newX;
            int y = newValue.newY;
            
            Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
    
            Debug.Log("MoveTo Ingresado en "+OwnerClientId);
            //¿Hay otra pieza en la posición de destino?
            if (_chessPieces[x, y] != null)
            {

                ChessPiece ocp = _chessPieces[x, y];

                if (cp.team == ocp.team)
                    return;

                //Si es el equipo enemigo
                if (ocp.team == 0)
                {
                    
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
            Debug.Log("MoveTo Ejecutado en "+OwnerClientId);

            _chessPieces[previousPosition.x, previousPosition.y] = null;

            PositionSinglePiece(x, y);

            isWhiteTurn = !isWhiteTurn;
                
                
        };
    }

    private void Awake()
    {
        activeTerrain = new Random().Next(0, 4);
        terrains[activeTerrain].SetActive(true);
        nextPowerup = 0;
        powerups = new Powerup[8, 8];
        isWhiteTurn = true;
        moving = false;
        GenerateGrid(tileSize, TILE_COUNT_CHESS, TILE_COUNT_CHESS);
        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            terrains[activeTerrain].SetActive(false);
            activeTerrain = new Random().Next(0, 4);
            terrains[activeTerrain].SetActive(true);
        }
        
        checkPowerup();
        checkRemainingPieces();
        if (turnCount == nextPowerup)
        {
            Vector2Int powerupPos = (Vector2Int.one * -1);
            while (true)
            {
                powerupPos = new Vector2Int(new Random().Next(1, 8), new Random().Next(1, 8));
                if (_chessPieces[powerupPos.x, powerupPos.y] == null) break;
            }
            Powerup pu = Instantiate(powerupPrefab).GetComponent<Powerup>();
            pu.currentX = powerupPos.x;
            pu.currentY = powerupPos.y;
            pu.type = (PowerupType)(new Random().Next(1, 10));
            pu.transform.position = GetTileCenter(powerupPos.x, powerupPos.y);
            powerups[powerupPos.x, powerupPos.y] = pu;
            nextPowerup = new Random().Next(3, 6);
            turnCount = 0;
        }

        if (moveCount == 10)
        {
            Debug.Log("Todas las piezas movidas");
            MoveAllPieces();
            ShootAll();
            turnCount++;
            moveCount = 0;
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Tablero Owner ID: " + OwnerClientId);
            GetCountServerRpc(new ServerRpcParams
                { Receive = new ServerRpcReceiveParams 
                    { SenderClientId = NetworkManager.Singleton.LocalClientId } 
                });
            Debug.Log("ID asignado manualmente: "+myID);
        }

        //Debug.Log("Seleccionado: "+ selectedPiece+ " Conteo de movimientos: "+moveCount);
        
        if (selectedPiece != null && selectedPiece.Acted())
        {
            if (Input.GetMouseButtonDown(0))
            {
                selectedPiece = null;
                moveCount++;
            }
        }
        
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            moving = false;
            dragging = false;
            selectedPiece.Shoot(false);
            popMenu.SetActive(false);
            currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        
        #region Menu Selector
        
        if (!moving && Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Pieces")))
            {
                for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    if (_chessPieces[i, j] != null && _chessPieces[i, j].transform.gameObject == info.transform.gameObject)
                    {
                        selectedPiece = _chessPieces[i, j];
                    }

                if (!selectedPiece.Acted())
                {
                    popMenu.SetActive(true);
                    Vector3 camPos = currentCamera.transform.position;
                    Vector3 direction = camPos - selectedPiece.transform.position;

                    float anglexz = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
                    float anglexy =
                        Mathf.Atan2(direction.y, Vector2.SqrMagnitude(new Vector2(direction.x, direction.z))) *
                        Mathf.Rad2Deg;

                    popMenu.transform.SetPositionAndRotation(selectedPiece.transform.position,
                        Quaternion.Euler(anglexy, 270 - anglexz, 0));
                    direction.z = -Math.Abs(direction.z);
                    popMenu.transform.Translate(direction * 0.2f);
                }
                else
                    selectedPiece = null;
            }
        }
        #endregion
        
        if (moving && Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
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
                if (_chessPieces[hitPosition.x, hitPosition.y] != null && !_chessPieces[hitPosition.x, hitPosition.y].Acted())
                {
                    currentlyDragging = _chessPieces[hitPosition.x, hitPosition.y];
                    // Obtén una lista de los lugares a los que puede ir el número 1 y resalta las casillas como soldadura.
                    availableMoves = currentlyDragging.GetAvailableMoves(ref _chessPieces, TILE_COUNT_CHESS, TILE_COUNT_CHESS);
                    dragging = true;
                    HighlightTiles();
                }
            }
            // Soltamos el mouse para colocar
            else if (dragging && Input.GetMouseButtonDown(0))
            {
                previousPosition.x = currentlyDragging.currentX;
                previousPosition.y = currentlyDragging.currentY;

                moveCount++;
                // bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y, false);

                currentlyDragging.nextX = hitPosition.x;
                currentlyDragging.nextY = hitPosition.y;
                currentlyDragging.MoveAction();
                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                currentlyDragging.MakeAction();

                currentlyDragging = null;
                selectedPiece = null;
                dragging = false;
                moving = false;
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
    
    #region Spawning and Positioning

    // Spawning pieces
    private void SpawnAllPieces()
    {
        _chessPieces = new ChessPiece[TILE_COUNT_CHESS, TILE_COUNT_CHESS];
        int whiteTeam = 0, blackTeam = 1;
        
        // Spawn White Pieces
        for (int i = 0; i < 5; i++)
            _chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        // Spawn Black Pieces
        for (int i = 3; i < 8; i++)
            _chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1]).GetComponent<ChessPiece>();

        int newTeam = team % 2;
        cp.type = type;
        cp.team = team;
        cp.actualPowerup = PowerupType.None;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[newTeam];
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
            // if ((x == 2 || x == 5) && y == 0)
            //     _chessPieces[x, y].transform.Rotate(Vector3.back, -90);
            // if ((x == 1 || x == 6) && y == 0)
            //     _chessPieces[x, y].transform.Rotate(Vector3.back, 180);
            if (x >= 3 && x < 8 && y == 6)
                _chessPieces[x, y].transform.Rotate(Vector3.back, 180);
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

    #endregion
    
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

    #region UI Buttons

    // UI Buttons
    public void OnMoverButton()
    {
        moving = true;
        popMenu.SetActive(false);
    }
    public void onShootButton()
    {
        selectedPiece.Shoot(true);
        selectedPiece.MakeAction();
        selectedPiece.ShootAction();
        popMenu.SetActive(false);
    }
    public void OnEditButton()
    {
        selectedPiece.transform.GetComponent<MeshRenderer>().material = personalizedMaterials[new Random().Next(0, personalizedMaterials.Length)];
        popMenu.SetActive(false);
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

    #endregion

    #region Online Buttons

    // Online Buttons
    public void OnServerButton()
    {
        NetworkManager.Singleton.StartServer();
        Debug.Log("LocalClientID Singleton" + NetworkManager.Singleton.ConnectedClientsIds.Count);

    }
    public void OnHostButton()
    {
        NetworkManager.Singleton.StartHost();
        Debug.Log("LocalClientID Singleton: " + NetworkManager.Singleton.ConnectedClientsIds.Count);
        Debug.Log("LocalClientID: " + NetworkManager.Singleton.LocalClientId);
    }
    public void OnClientButton()
    {
        NetworkManager.Singleton.StartClient();
        Debug.Log("LocalClientID Singleton: " + NetworkManager.ServerClientId);
        Debug.Log("LocalClientID: " + NetworkManager.Singleton.LocalClientId);
        Debug.Log("Owner ID: " + OwnerClientId);
    }

    #endregion
    
    #region Operations
    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }
    private bool MoveTo(ChessPiece cp, int x, int y, bool net)
    {
        // if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        //     return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
    
        // Debug.Log("MoveTo Ingresado en "+OwnerClientId);
        //¿Hay otra pieza en la posición de destino?
        if (_chessPieces[x, y] != null)
        {

            ChessPiece ocp = _chessPieces[x, y];

            if (cp.team == ocp.team)
                return false;

            //Si es el equipo enemigo
            if (ocp.team == 0)
            {

                
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                                - bounds
                                + new Vector3(tileSize / 2, 0, tileSize / 2)
                                + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {

                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);

                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                                - bounds
                                + new Vector3(tileSize / 2, 0, tileSize / 2)
                                + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        _chessPieces[x, y] = cp;
        // Debug.Log("MoveTo Ejecutado en "+OwnerClientId);
        if (IsServer)
        {
            lastMovement.Value = new movement
            {
                prevX = previousPosition.x,
                prevY = previousPosition.y,
                newX = x,
                newY = y,
                whiteTurn = !isWhiteTurn
            };
        }
        else
        {
            MovePieceServerRpc(previousPosition.x, previousPosition.y, x, y, !isWhiteTurn);
        }

        if (powerups[x, y] != null)
        {
            _chessPieces[x, y].actualPowerup = powerups[x, y].type;
            Destroy(powerups[x, y].gameObject);
            powerups[x, y] = null;
        }

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
    private void checkRemainingPieces()
    {
        int whiteCount = 0;
        int blackCount = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_chessPieces[i, j] != null)
                {
                    if (_chessPieces[i, j].team == 0) whiteCount++;
                    else blackCount++;
                }
            }
        }

        if (whiteCount == 0)
        {
            Debug.Log("Black Team Wins");
            CheckMate(1);
        }

        if (blackCount == 0)
        {
            Debug.Log("White Team Wins");
            CheckMate(0);
        }

    }
    private void checkPowerup()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_chessPieces[i, j] != null)
                {
                    if (_chessPieces[i, j].actualPowerup == PowerupType.Promotion)
                    {
                        int team = _chessPieces[i, j].team;
                        Destroy(_chessPieces[i, j].gameObject);
                        _chessPieces[i, j] = SpawnSinglePiece((ChessPieceType)(new Random().Next(2, 6)), team);
                        PositionSinglePiece(i, j, true);
                        _chessPieces[i, j].actualPowerup = PowerupType.None;
                    }
                    else if (_chessPieces[i, j].actualPowerup == PowerupType.Invincible)
                    {
                        _chessPieces[i, j].Invincible();
                        _chessPieces[i, j].actualPowerup = PowerupType.None;
                    }
                    else if (_chessPieces[i, j].actualPowerup == PowerupType.Minimize)
                    {
                        _chessPieces[i, j].SetScale(Vector3.one * 10f);
                        _chessPieces[i, j].actualPowerup = PowerupType.None;
                    }
                    else if (_chessPieces[i, j].actualPowerup == PowerupType.Shambles)
                    {
                        int team = _chessPieces[i, j].team;
                        int piece = new Random().Next(0, 4);
                        int count = 0;
                        for (int k = 0; k < 8; k++)
                        for (int l = 0; l < 8; l++)
                        {
                            if (_chessPieces[k, l] != null)
                            {
                                if (_chessPieces[k, l].team != team)
                                {
                                    if (count == piece)
                                    {
                                        (_chessPieces[i, j], _chessPieces[k, l]) =
                                            (_chessPieces[k, l], _chessPieces[i, j]);
                                        _chessPieces[k, l].actualPowerup = PowerupType.None;
                                        PositionSinglePiece(i, j, true);
                                        PositionSinglePiece(k, l, true);
                                        break;
                                    }

                                    count++;
                                }
                            }
                        }
                    }
                    else if (_chessPieces[i, j].actualPowerup == PowerupType.Teleport)
                    {
                        int position = new Random().Next(0, 63);
                        int count = 0;
                        for (int k = 0; k < 8; k++)
                        for (int l = 0; l < 8; l++)
                        {
                            if (_chessPieces[k, l] == null && count >= position)
                            {
                                _chessPieces[k, l] = _chessPieces[i, j];
                                _chessPieces[i, j].actualPowerup = PowerupType.None;
                                _chessPieces[i, j] = null;
                                PositionSinglePiece(k, l, true);
                                break;
                            }

                            count++;
                        }
                    }
                    else if (_chessPieces[i, j].actualPowerup == PowerupType.TeleportOponent)
                    {
                        int position = new Random().Next(0, 63);
                        int count = 0;
                        int enemyCount = 0;
                        int enemy = new Random().Next(0, 4);
                        bool transportDone = false;

                        for (int k = 0; k < 8; k++)
                        {
                            for (int l = 0; l < 8; l++)
                            {
                                if (_chessPieces[k, l] == null)
                                {
                                    if (count == position)
                                    {
                                        for (int m = 0; m < 8; m++)
                                        {
                                            for (int n = 0; n < 8; n++)
                                            {
                                                if (_chessPieces[m, n] != null && _chessPieces[m, n].team != _chessPieces[i, j].team)
                                                {
                                                    if (enemy == enemyCount)
                                                    {
                                                        Debug.Log("Transportando");
                                                        _chessPieces[k, l] = _chessPieces[m, n];
                                                        _chessPieces[k, l].currentX = m;
                                                        _chessPieces[k, l].currentY = n;
                                                        _chessPieces[m, n] = null;
                                                        PositionSinglePiece(k, l, true);
                                                        _chessPieces[i, j].actualPowerup = PowerupType.None;
                                                        transportDone = true;
                                                        break;
                                                    }
                                                    enemyCount++;
                                                }
                                            }
                                            if (transportDone)
                                                break;
                                        }
                                    }
                                    count++;
                                }
                                if (transportDone)
                                    break;
                            }
                            if (transportDone)
                                break;
                        }
                    }
                    else if (_chessPieces[i, j].actualPowerup == PowerupType.Freeze)
                    {
                        int enemy = new Random().Next(0, 4);
                        int count = 0;
                        for (int k = 0; k < 8; k++)
                        for (int l = 0; l < 8; l++)
                        {
                            if (_chessPieces[k, l] != null && _chessPieces[k, l].team != _chessPieces[i, j].team)
                            {
                                if (count == enemy)
                                {
                                    _chessPieces[k, l].MakeAction();
                                }

                                count++;
                            }
                        }
                    }
                }
                
            }
        }
    }
    private void ShootAll()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_chessPieces[i, j] != null && _chessPieces[i, j].ActionType())
                {
                    _chessPieces[i, j].NextTurn();
                    _chessPieces[i, j].GetComponentInChildren<WeaponController>().TryShoot(_chessPieces[i, j].damage);
                }
            }
        }
    }
    private void MoveAllPieces()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (_chessPieces[i, j] != null && !_chessPieces[i, j].ActionType())
                {
                    _chessPieces[i, j].NextTurn();
                    bool validMove = MoveTo(_chessPieces[i, j], _chessPieces[i, j].nextX, _chessPieces[i, j].nextY, false);
                }
            }
        }
    }

    #endregion
    
    #region Online RPC's
    // Online RPC's
    [ServerRpc(RequireOwnership = false)]
    private void MovePieceServerRpc(int prevX, int prevY, int x, int y, bool whiteTurn)
    {
        Debug.Log("RPC Executed");
        lastMovement.Value = new movement
        {
            prevX = prevX,
            prevY = prevY,
            newX = x,
            newY = y,
            whiteTurn = whiteTurn
        };
    }
    [ServerRpc(RequireOwnership = false)]
    private void GetCountServerRpc(ServerRpcParams serverRpcParams)
    {
        SendIdClientRpc(NetworkManager.Singleton.ConnectedClientsIds.Count - 1, new ClientRpcParams { Send = new ClientRpcSendParams
        {
            TargetClientIds = new List<ulong> { serverRpcParams.Receive.SenderClientId }
        }, Receive = new ClientRpcReceiveParams
        {
            
        } } );
    }
    [ClientRpc]
    private void SendIdClientRpc(int count, ClientRpcParams clientRpcParams)
    {
        myID = count;
        Debug.Log("Correctoooo");
    }
    #endregion
}
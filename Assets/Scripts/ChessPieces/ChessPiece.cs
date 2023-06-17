using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}
public class ChessPiece : MonoBehaviour
{
    [Header("Padre")]
    [SerializeField] private GameObject chessBoard;
    
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    private Camera currentCamera;

	private void Start(){
		//transform.rotation = Quaternion.Euler((team==0) ? Vector3.zero : new Vector3(0,180,0));
	}

    private bool shoot = false;
    private bool isSelected = false;
    
    public void Update()
    {
        
        Vector3 VScreen = new Vector3();
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);

        // Obtener el objeto seleccionado al hacer clic
 
        VScreen.x = Input.mousePosition.x;
        VScreen.y = Input.mousePosition.y;
        VScreen.z = 11.84f;
        RaycastHit hit;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            shoot = false;
        }
        
        if (Physics.Raycast(ray, out hit, 200, LayerMask.GetMask("Pieces")) && shoot)
        {
            if (Input.GetMouseButtonDown(0)) // Verificar clic izquierdo
            {
                if (hit.transform.gameObject == gameObject)
                {
                    isSelected = true;
                }
                else
                {
                    isSelected = false;
                }
            }
        }

        if (shoot)
        {
            // Calcular la dirección del mouse en el mundo
            Vector3 VWold = new Vector3();
 
            VScreen.x = Input.mousePosition.x;
            VScreen.y = Input.mousePosition.y;
            VScreen.z = Camera.main.transform.position.z;
            VWold = Camera.main.ScreenToWorldPoint(new Vector3(VScreen.x, VScreen.y, 11.84f));
            Vector3 direction = VWold - transform.position;

            // Calcular el ángulo de rotación en grados
            float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;

            // Aplicar la rotación al objeto
            if (type == ChessPieceType.Knight)
                transform.rotation = Quaternion.Euler(-90, -angle-90, 0);
            else if (type == ChessPieceType.Bishop)
                transform.rotation = Quaternion.Euler(-90, -angle-180, 0);
            else 
                transform.rotation = Quaternion.Euler(-90, -angle-270, 0);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isSelected = false;
            shoot = false;
        }
        
    }

    public void Shoot(bool shooting)
    {
        shoot = true;
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        r.Add(new Vector2Int(3, 3));
        r.Add(new Vector2Int(3, 4));
        r.Add(new Vector2Int(4, 3));
        r.Add(new Vector2Int(4, 4));
        return r;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }
}

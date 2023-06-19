using System;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

public class NetworkPlayer : NetworkBehaviour
{
    private NetworkVariable<int> miVar = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        Debug.Log("Owner ID: " + OwnerClientId);
        miVar.OnValueChanged += (value, newValue) =>
        {
            Debug.Log("New Value: " + newValue + "; On Owner: " + OwnerClientId);
        };
    }

    // Start is called before the first frame update
    private void Update()
    {
        if (!IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            miVar.Value = new Random().Next(1, 100);
        }

        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        float moveSpeed = 3f;

        transform.position += moveDir * (moveSpeed * Time.deltaTime);
    }
}

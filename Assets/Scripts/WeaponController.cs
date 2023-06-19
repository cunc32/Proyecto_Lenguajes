using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Transform weaponMuzzle;

    [Header("Info")]
    public string weaponName;
    public Sprite icon;

    [Header("General")]
    public LayerMask hittableLayers;
    public GameObject bulletHolePrefab;

    [Header("Shoot Paramaters")]
    public float fireRange = 200;

    [Header("Sounds & Visuals")]
    public GameObject flashEffect;

    public void TryShoot(int damage)
    {
        HandleShoot(damage);
    }

    private void HandleShoot(int damage)
    {
        GameObject flashClone = Instantiate(flashEffect, weaponMuzzle.position, Quaternion.Euler(weaponMuzzle.forward), transform);
        Destroy(flashClone, 1f);

        Vector3 origin = transform.parent.position;
        origin.y = 0.3f;

        RaycastHit[] hits;
        hits = Physics.RaycastAll(origin, transform.parent.up * -1f, fireRange, hittableLayers);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform != transform.parent)
            {
                if (transform.parent.GetComponent<ChessPiece>().team != hit.collider.gameObject.GetComponent<ChessPiece>().team
                    && hit.collider.gameObject.GetComponent<ChessPiece>().Shot(damage) == 0)
                    Destroy(hit.collider.gameObject, 1f);
            }
        }
    }
}

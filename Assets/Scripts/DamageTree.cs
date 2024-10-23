using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageTree : MonoBehaviour
{
    private int health = 4;
    public void Damage()
    {
        health--;
        if (health <= 0)
        {
            // Destroy(gameObject);
        }
    }
}

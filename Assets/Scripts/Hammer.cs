using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hammer : MonoBehaviour
{
    public static Action<Collider> attack;
    
    private void OnCollisionEnter(Collision other)
    {
        // if (other.collider.tag == "Mouse")
        // {
        //     attack(other.collider);
        // }
    }
}

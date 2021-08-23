using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MouseAI : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float moveSpeed;
    [SerializeField] public float stopTime;
    [SerializeField] private float moveUpDistance;
    [SerializeField] private float moveDownDistance;
    private float downPoint;
    private float upPoint;
    public bool isUp;
    public bool isMoving;
    public bool canBeHit;
    public GameObject effect;
    public int index;
    void Start()
    {
        upPoint = transform.position.y + moveUpDistance;
        downPoint = transform.position.y - moveDownDistance;
        transform.position -= Vector3.up * moveDownDistance;
    }

    private void OnEnable()
    {
        Hammer.attack += Die;

    }

    private void OnDisable()
    {
        Hammer.attack -= Die;
    }

    // Update is called once per frame
    void Update()
    {
        
        if (isUp && !isMoving)
        {
            GameManager.instance.num++;
            GameManager.onNumChange();
            StartCoroutine(MoveDown());
        }

        if (!isUp && !isMoving)
        {
            StartCoroutine(MoveUp());
        }
    }

    private IEnumerator MoveUp()
    {
        isUp = true;
        isMoving = true;
        canBeHit = false;
        yield return new WaitForSeconds(Random.Range(0,4));
        canBeHit = true;
        while (transform.position.y< upPoint)
        {
            transform.position += new Vector3(0,Time.deltaTime,0) *moveSpeed;
            yield return null;
        }
        isMoving = false;
    }
    
    private IEnumerator MoveDown()
    {
        isUp = false;
        isMoving = true;
        yield return new WaitForSeconds(stopTime);
        while (transform.position.y> downPoint)
        {
            transform.position -= new Vector3(0,Time.deltaTime,0) *moveSpeed;
            yield return null;
        }
        isMoving = false;
    }

    public void Die(Collider col)
    {
        if (col.gameObject == gameObject)
        {
            GameManager.instance.point++;
        }
    }
    
}

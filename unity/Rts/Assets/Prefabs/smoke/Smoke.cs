using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour
{

    [SerializeField]
    private float speed;

    [SerializeField]
    private float rotSpeed;

    public GameObject target;
    public Vector3 direction = Vector3.forward;
    // Update is called once per frame
    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        if (target)
        {
            transform.LookAt(target.transform);
        }
        else
        {
            Destroy(gameObject);
        }

        transform.Rotate(new Vector3(0, 0, 1) * Time.deltaTime * rotSpeed, Space.World);
    }

    void OnCollisionEnter(Collision col)
    {
        if (gameObject && target)
        {
            if (col.collider.name == target.name)
            {
                Destroy(gameObject);
            }
        }
 
    }
}
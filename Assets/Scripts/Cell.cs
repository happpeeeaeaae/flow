using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{

    public float speed = 1f;
    public Vector3 direction = new Vector3(1, 0.2f, 0);

    public int screenWidth = 20;
    public int screenHeight = 10;


    private Vector3 velocity;
    private int xl, yl;

    void Start()
    {
        velocity = speed * direction.normalized;
        xl = screenWidth / 2;
        yl = screenHeight / 2;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 velocity = direction * speed;
        transform.position += velocity * Time.deltaTime;
        velocityAfterCollision(velocity);
    }

    void velocityAfterCollision(Vector3 velocity)
    {
        var pos = transform.position;
        if (pos.x >= xl)
        {
            transform.position = new Vector3(xl, pos.y, pos.z);
            direction.x = -direction.x;
        } else if (transform.position.x <= -xl)
        {
            transform.position = new Vector3(-xl, pos.y, pos.z);
            direction.x = -direction.x;
        }
        if (pos.y >= yl)
        {
            transform.position = new Vector3(pos.x, yl, pos.z);
            direction.y = -direction.y;
        }
        else if (pos.y <= -yl)
        {
            transform.position = new Vector3(pos.x, -yl, pos.z); ;
            direction.y = -direction.y;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Ce script se trouve sur chaque unitee
public class Unit : MonoBehaviour {

    //Click stuff
    public bool currentlySelected = false;

    [SerializeField]
    public GameObject circle;

    [SerializeField]
    public int health;

    [SerializeField]
    public TextMesh healthBar;

    [SerializeField]
    public GameObject firePoint;

    [SerializeField]
    public GameObject smokePrefab;

    [SerializeField]
    public int power;

    [SerializeField]
    public float coolDownTime;

    private float currentCoolDown = 0.0f;

    //Boom on the canon of this unit
    //private GameObject boomOnTheCanon;
 
    private float boomDuration = 0.2f;
    private float boomTimer = 0.0f;
    private bool boom = false;
    private void Start()
    {
        hideCircle();
    }
    void Update () {
        if(health <= 0)
        {
            Destroy(gameObject);
        }
        refreshTimers();
        refreshHealthBar();
        clearFire();
       
    }
    public void  checkDistanceWithEnemy(GameObject enemy)
    {
        if (enemy) {
            float dist = Vector3.Distance(enemy.transform.position, transform.position);
            if (dist < 5.0f)
            {
                fire(enemy);

            }
        }
    }
    void takeDmg(int dmg)
    {
        //Take dmg
        if (dmg > 0)
        {
            health = health - dmg;
        }
    }
    
    void refreshHealthBar()
    {
        healthBar.transform.LookAt(Camera.main.transform);
        healthBar.text = health.ToString();
    }

    void fire(GameObject target)
    {
        //Si le cooldown est fini
        if (currentCoolDown <= 0.0f)
        {
            target.GetComponent<Unit>().takeDmg(power);

            GameObject smoke = Instantiate(smokePrefab, firePoint.transform.position, Quaternion.identity);
            smoke.GetComponent<Smoke>().target = target;
            boomTimer = boomDuration;
            boom = true;

            print("booom");
            currentCoolDown = coolDownTime;
        }
       
    }

    void clearFire()
    {
        if(boomTimer == 0.0f && boom)
        {
           //Destroy(boomOnTheCanon);
        }
    }

    void refreshTimers()
    {
        if(currentCoolDown > 0.0f)
        {
            currentCoolDown -= Time.deltaTime;
        }
        else
        {
            currentCoolDown = 0.0f;
        }

        if(boomTimer > 0.0f)
        {
            boomTimer -= Time.deltaTime;
        }
        else
        {
            boomTimer = 0.0f;
        }
        
    }

    public Vector3 getPosition()
    {
        return transform.position;
    }
    public void select()
    {
        currentlySelected = true;
        showCircle();
    }

    public void deselect()
    {
        hideCircle();
        currentlySelected = false;
    }

    private void hideCircle()
    {
        //Hide the circle
        circle.GetComponent<Renderer>().enabled = false;
    }

    private void showCircle()
    {
        //Show the circle
        circle.GetComponent<Renderer>().enabled = true;
    }
}

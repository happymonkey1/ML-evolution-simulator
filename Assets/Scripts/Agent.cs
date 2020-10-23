using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{

    public float baseEnergy;
    public float baseHealth;
    public float baseSize;
    public float baseSpeed;

    private Transform _transform;

    public float maxEnergy;
    public float maxHealth;
    public float maxSpeed;

    public float currentHealth;
    public float currentEnergy;
    public float currentSpeed;
    public float size;

    private Vector2 _velocity;


    public int ID { get; set; }

    

    // Start is called before the first frame update
    void Start()
    {
        _transform = GetComponent<Transform>();
        _velocity = new Vector2(0f, 0f);

        Birth(GameManager.instance.objIDs++);
        
    }

    void Birth(int id)
    {
        baseEnergy += (Random.Range(-50f, 50f));
        baseSize += NativeMethods.GetRandomFloat(0f, 1f);
        baseSize = Mathf.Clamp(baseSize, 0.1f, 100f);
        //baseHealth = 100;
        baseSpeed = 1.4f;

        size = baseSize;
        maxHealth = baseHealth + (100f * size);
        maxEnergy = baseEnergy + (50f * NativeMethods.PowOneOverE(size));
        maxSpeed = Mathf.Clamp(baseSpeed + (1 - size)*NativeMethods.InverseE, GameManager.MIN_SPEED, GameManager.MAX_SPEED);

        currentEnergy = maxEnergy;
        currentHealth = maxHealth;
        currentSpeed = maxSpeed;

        ID = id;
        GameManager.instance.agents[id] = this;

        _transform.localScale = new Vector3(size, size, 1f);

        int x = (int)(NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.x, GameManager.instance.worldBounds.width / 2));
        int y = (int)(NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.y, GameManager.instance.worldBounds.height / 2));
        _transform.position = new Vector3(x, y, 0);
    }

    // Update is called once per frame
    void Update()
    {
        Think();

        Move();

        UseEnergy();
    }

    void Think()
    {
        if (Random.Range(0, 100) > 80f)
        {
            if (currentEnergy > 0)
            {
                float dir = Random.Range(0, 2 * Mathf.PI);
                _velocity = (new Vector2(Mathf.Cos(dir), Mathf.Sin(dir)) * (currentSpeed * Time.deltaTime));
            }
        }
    }

    void Move()
    {
        _transform.position = _transform.position + new Vector3(_velocity.x, _velocity.y, 0);
    }


    void UseEnergy()
    {
        float energyCostToMove = (NativeMethods.PowOneOverE(Mathf.Abs(_velocity.magnitude) * size));
        float energyCostToMaintainSize = size * NativeMethods.InverseE;


        float totalEnergyCost = energyCostToMove + energyCostToMaintainSize;
        currentEnergy -= totalEnergyCost * Time.deltaTime;
    }
    

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{

    public int neuronInput;
    public int neuronOutput;
    private List<double> _brainVision;
    private List<double> _brainDecisions;

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
    public float direction;
    public float size;

    private Vector2 _velocity;


    Network Brain { get; set; }
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
        baseSize = Mathf.Clamp(baseSize, 0.25f, 100f);
        //baseHealth = 100;
        baseSpeed = 1.4f;

        size = baseSize;
        maxHealth = baseHealth + (100f * size);
        maxEnergy = baseEnergy + (50f * NativeMethods.PowOneOverE(size));
        maxSpeed = Mathf.Clamp(baseSpeed, GameManager.MIN_SPEED, GameManager.MAX_SPEED);

        currentEnergy = maxEnergy;
        currentHealth = maxHealth;
        currentSpeed = maxSpeed;

        ID = id;
        GameManager.instance.agents[id] = this;

        _transform.localScale = new Vector3(size, size, 1f);

        int x = (int)(NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.x, GameManager.instance.worldBounds.width / 2));
        int y = (int)(NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.y, GameManager.instance.worldBounds.height / 2));
        _transform.position = new Vector3(x, y, 0);



        Brain = Preset.Random(neuronInput, new System.Random().Next(0, 3), neuronOutput);
        
    }

    void FindClosestFood()
    {

    }

    void Update()
    {
        Debug.LogWarning("START");
        See();

        Think();

        Move();

        Metabolism();
        Debug.LogWarning("STOP");
    }


    void See()
    {
        _brainVision = new List<double>();

        double hunger = (currentEnergy / maxEnergy);
        hunger = (hunger < 0) ? 0 : hunger;
        _brainVision.Add(currentEnergy);

        double speed = Mathf.Abs(currentSpeed / maxSpeed);
        speed = (speed > 1) ? 1.0 : speed;
        _brainVision.Add(currentSpeed);
        
        Debug.Log($"inputs: {string.Join(" ", _brainVision)}");
    
    }


    // Update is called once per frame
    

    void Think()
    {
        double max = 0;
        int maxIndex = 0;
        _brainDecisions = Brain.NoTraceActivate(_brainVision);



        Debug.Log($"outputs: {string.Join(" ", _brainDecisions)}");

        for (int i=0; i < _brainDecisions.Count; i++)
        {
            if (_brainDecisions[i] > max)
            {
                max = _brainDecisions[i];
                maxIndex = i;
            }
        }


        switch (maxIndex)
        {
            case 0: //SPEED
                currentSpeed = maxSpeed * (float)(max);
                break;
            case 1: //DIRECTION
                direction = (Mathf.PI * 2) * (float)(max);
                break;
        }

    }

    void Move()
    {
        _velocity = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * (currentSpeed * Time.deltaTime);
        _transform.position = _transform.position + new Vector3(_velocity.x, _velocity.y, 0);
    }


    void Metabolism()
    {
        float moveCost = (NativeMethods.PowOneOverE(Mathf.Abs(_velocity.magnitude)));
        float sizeCost = 2 * size * NativeMethods.InverseE;


        float totalEnergyCost = moveCost + sizeCost;
        currentEnergy -= totalEnergyCost * Time.deltaTime;

        if (currentEnergy < 0)
            currentHealth -= NativeMethods.InverseE * maxHealth * Time.deltaTime;
    }
    

}

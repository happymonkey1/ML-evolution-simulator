using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{

    public int neuronInput;
    public int neuronOutput;
    public bool biasNeuron;
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
    public float maxSize;

    public float currentHealth;
    public float currentEnergy;
    public float currentSpeed;
    public float direction;
    public float currentSize;

    public float currentMaturity;
    public float maturationAge;

    public float age;



    //BEHAVIOR 
    public bool isDead = false;

    //WANTS
    private bool _wantsToEat = false;
    private bool _wantsToMate = true;
    private bool _canMate = false;

    private Vector2 _velocity;

    public int MIN_FOOD_DIST = 1;

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
        baseSize += NativeMethods.GetRandomFloat(0f, 2f);
        baseSize = Mathf.Clamp(baseSize, GameManager.MIN_SIZE, 100f);
        //baseHealth = 100;
        baseSpeed = 1.4f;

        maxSize = baseSize;
        maxHealth = baseHealth + (100f * maxSize);
        maxEnergy = baseEnergy + (50f * NativeMethods.PowOneOverE(maxSize));
        maxSpeed = Mathf.Clamp(baseSpeed, GameManager.MIN_SPEED, GameManager.MAX_SPEED);
        maturationAge = new System.Random().Next(5, 20);



        currentMaturity = age / maturationAge;
        currentSpeed = 0f;
        currentSize = Mathf.Clamp(baseSize * currentMaturity, GameManager.MIN_SIZE, maxSize);
        currentHealth = maxHealth * NativeMethods.InverseE;
        currentEnergy = maxEnergy;
        


        ID = id;
        GameManager.instance.agents.Add(this);

        _transform.localScale = new Vector3(currentSize, currentSize, 1f);

        float x = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.x, GameManager.instance.worldBounds.width / 2));
        float y = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.y, GameManager.instance.worldBounds.height / 2));
        _transform.position = new Vector3(x, y, 0);

        age = 0f;

        direction = (float)(new System.Random().NextDouble() * Mathf.PI * 2);
        isDead = false;

        _wantsToMate = true;

        Brain = Preset.EmptyRandom(neuronInput, new System.Random().Next(0, 3), neuronOutput, biasNeuron);
        
    }

    public (float dist, int key) FindClosestFood()
    {
        float min = float.PositiveInfinity;
        int fKey = 0;
        for (int i=0; i < GameManager.instance.foods.Count; i++)
        {
            Food f = GameManager.instance.foods[i];
            if (f.isDead) continue;

            float dist = Mathf.Pow(f.transform.position.x - transform.position.x, 2) + Mathf.Pow(f.transform.position.x - transform.position.x, 2);
            if (dist < min) {
                
                min = dist;
                fKey = i;
                if (dist <= MIN_FOOD_DIST)
                    break;
            }
        }

        return (min, fKey);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {

        Debug.Log("COLLISION");
        if (collision.gameObject.tag == "Food")
        {
            Eat(collision.gameObject.GetComponent<Food>());
        }

    }

    private void Eat(Food f)
    {
        float hunger = maxEnergy - currentEnergy;
        currentEnergy = Mathf.Min(f.currentBioMass + currentEnergy, maxEnergy);
        if (currentEnergy / maxEnergy > 0.8)
            Reproduce();

        Debug.Log("EAT BREH");
        f.currentBioMass -= hunger;
    }

    private void Reproduce()
    {

        if (_wantsToMate && _canMate)
        {
            Debug.Log("Someone touched themselves");
            Agent baby = GameManager.instance.CreateAgent();
            baby.Brain = Brain.CrossOver(Brain, Brain, true);
            baby.transform.position = transform.position;
            _wantsToMate = false;
        }

        
        
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

        if(biasNeuron)
            _brainVision.Add(1.0);

        double hunger = (currentEnergy / maxEnergy);
        hunger = (hunger < 0) ? 0 : hunger;
        _brainVision.Add(currentEnergy);


        _canMate = (currentMaturity >= 1.0) ? true : false;
        _brainVision.Add(currentMaturity);


        double speed = Mathf.Abs(currentSpeed / maxSpeed);
        speed = (speed > 1) ? 1.0 : speed; 
        _brainVision.Add(currentSpeed);

        double health = currentHealth / maxHealth;
        health = (health < 0) ? 0 : health;
        _brainVision.Add(health);

        //======================================

        _brainVision.Add(FindClosestFood().dist);

        //======================================

        _brainVision.Add(age);

        

        //Debug.Log($"inputs: {string.Join(" ", _brainVision)}");

    }


    // Update is called once per frame


    void Think()
    {
        double max = 0;
        int maxIndex = 0;
        _brainDecisions = Brain.NoTraceActivate(_brainVision);



      //  Debug.Log($"outputs: {string.Join(" ", _brainDecisions)}");

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
            case 2:
                if (max > 0.6)
                    _wantsToEat = true;
                else
                    _wantsToEat = false;
                break;
            case 3:
                if (max > 0.6)
                    _wantsToMate = true;
                else
                    _wantsToMate = false;
                break;

        }

    }

    void Move()
    {
        currentSize = Mathf.Clamp(maxSize * age / maturationAge, GameManager.MIN_SIZE, maxSize);
        _transform.localScale = new Vector3(currentSize, currentSize, 1f);

        _velocity = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * (currentSpeed * Time.deltaTime);
        _transform.position = _transform.position + new Vector3(_velocity.x, _velocity.y, 0);
    }


    void Metabolism()
    {
        float moveCost = (NativeMethods.PowOneOverE(Mathf.Abs(_velocity.magnitude)));
        float sizeCost = 2 * currentSize;


        float totalEnergyCost = moveCost + sizeCost;
        currentEnergy -= totalEnergyCost * Time.deltaTime;

        if (currentEnergy < 0)
            currentHealth -= NativeMethods.InverseE * maxHealth * Time.deltaTime;

        if (currentHealth < 0)
            isDead = true;
        

        age += 1 * Time.deltaTime;
        currentMaturity = Mathf.Min(age / maturationAge, 1.0f);
    }
    

}

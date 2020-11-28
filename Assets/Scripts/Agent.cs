using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Agent : MonoBehaviour
{

    public int neuronInput;
    public int neuronOutput;
    public bool biasNeuron;
    private List<double> _brainVision;
    private List<double> _brainDecisions;

    private Transform _transform;
    private SpriteRenderer _sr;
    private Collider2D _col;
    private Rigidbody2D _rb;


    public BaseGenes genes = new BaseGenes();
    

    public float maxEnergy;
    public float maxHealth;
    public float MaxSpeed { get { return _maxSpeed; } set { _maxSpeed = Mathf.Clamp(value, GameManager.MIN_SPEED, GameManager.MAX_SPEED); } }
    private float _maxSpeed;
    public float maxSize;

    public float currentHealth;
    public float currentEnergy;
    public float CurrentSpeed { get { return _currentSpeed; } set { _currentSpeed = Mathf.Clamp(value, -MaxSpeed, MaxSpeed); } }
    [SerializeField]
    private float _currentSpeed;
    public float CurrentTurningSpeed { get { return _currentTurningSpeed; } set { _currentTurningSpeed = Mathf.Clamp(value, 0, Mathf.PI); } }
    [SerializeField]
    private float _currentTurningSpeed;

    public float wantDirection;
    public float facingDirection;
    public float currentSize;
    

    public float currentMaturity;
    public float maturationAge;
    public float age;
    public float lifeExpectancy;
    public float litterSize;
    public float lastGestationAge;

    //BEHAVIOR 
    public bool isDead = false;

    //WANTS
    private bool _wantsToEat = false;
    private bool _wantsToMate = true;
    private bool _canMate = false;

    private Vector2 _velocity;

    public int MIN_FOOD_DIST = 1;

    Network Brain { get; set; }
    [SerializeField]
    public int ID { get; set; }

    private bool _mouseDown = false;

    // Start is called before the first frame update
    void Awake()
    {
        _transform = GetComponent<Transform>();
        _velocity = new Vector2(0f, 0f);
        _sr = GetComponentInChildren<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        _rb = GetComponent<Rigidbody2D>();
    }

    

    public void Birth(int id, BaseGenes? pattern = null)
    {
        if (pattern == null)
            genes.RandomizeBaseValues();
        else
            genes.Set((BaseGenes)pattern, true);


        maxSize = genes.baseSize;
        maxHealth = genes.baseHealth + (100f * maxSize);
        maxEnergy = genes.baseEnergy + (.5f * NativeMethods.PowOneOverE(maxSize));
        MaxSpeed = genes.baseMaxSpeed;


        maturationAge = genes.baseMaturationAge;
        lifeExpectancy = genes.baseLifeExpectancy;
        litterSize = genes.baseMaxLitterSize;
        age = 0f;
        lastGestationAge = 0f;

        currentMaturity = age / maturationAge;
        CurrentSpeed = 0f;
        CurrentTurningSpeed = genes.baseTurningSpeed;
        currentSize = Mathf.Clamp(genes.baseSize * currentMaturity, GameManager.MIN_SIZE, maxSize);
        currentHealth = maxHealth;
        currentEnergy = maxEnergy * 0.35f;

        ID = id;
        GameManager.instance.agents.Add(this);

        _transform.localScale = new Vector3(currentSize, currentSize, 1f);

        float x, y;
        if (!Globals.GAUSSIAN_SPAWNING)
        {
            x = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.x, GameManager.instance.worldBounds.width, false));
            y = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.y, GameManager.instance.worldBounds.height, false));
        }
        else
        {
            x = (float)(NativeMethods.RandomGaussian() * GameManager.instance.worldBounds.width + GameManager.instance.worldBounds.x + GameManager.instance.worldBounds.width / 2) / 8 * (1f-GameManager.instance.agentDistributionDensity);
            y = (float)(NativeMethods.RandomGaussian() * GameManager.instance.worldBounds.height + GameManager.instance.worldBounds.y + GameManager.instance.worldBounds.height / 2) / 8 * (1f- GameManager.instance.agentDistributionDensity);
        }
        _transform.position = new Vector3(x, y, 0);

        _sr.color = new Color(genes.baseColorR, genes.baseColorG, genes.baseColorB, 1f);

        facingDirection = wantDirection = (float)(new System.Random().NextDouble() * Mathf.PI * 2);
        _transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, facingDirection * (180/Mathf.PI)));
        isDead = false;

        _wantsToMate = true;

        Brain = Preset.EmptyRandom(neuronInput, new System.Random().Next(0, 3), neuronOutput, biasNeuron);
        _brainVision = new List<double>();
        _brainDecisions = new List<double>();

    }

    public (float, float, int) FindClosestAgent()
    {
        if (GameManager.instance.foods.Count == 0) return (1000f, -1, 0);
        float min = float.PositiveInfinity;
        float angle = 0;
        int agentsNearby = 0;
        Collider2D[] foundColliders = Physics2D.OverlapCircleAll(_transform.position, genes.baseVision);
        foreach (Collider2D col in foundColliders)
        {
            if (col == _col) continue;
            if (col.gameObject.tag == "Agent")
            {
                agentsNearby++;
                ColliderDistance2D dist = _col.Distance(col);
                float dy = col.transform.position.y - _transform.position.y;
                float dx = col.transform.position.x - _transform.position.x;
                if (dist.distance < min)
                {
                    min = dist.distance;
                    angle = Mathf.Atan2(dy, dx);
                }
            }
        }

        while (angle < 0)
            angle += Mathf.PI * 2;

        return ((min <= genes.baseVision) ? min : 1000f, angle, agentsNearby);
    }

    public (float, float, int) FindClosestFood()
    {
        if (GameManager.instance.foods.Count == 0) return (1000f, -1, 0);
        float min = float.PositiveInfinity;
        float angle = 0;
        int foodsNearby = 0;

        Collider2D[] foundColliders = Physics2D.OverlapCircleAll(_transform.position, genes.baseVision);
        
        foreach (Collider2D col in foundColliders)
        {
            if (col.gameObject.tag == "Food")
            {
                foodsNearby++;
                ColliderDistance2D dist = _col.Distance(col);
                float dy = col.transform.position.y - _transform.position.y;
                float dx = col.transform.position.x - _transform.position.x;
                if (dist.distance < min)
                {

                    min = dist.distance;
                    angle = Mathf.Atan2(dy, dx);
                }
            }
        }

        while (angle < 0)
            angle += Mathf.PI * 2;

        return ((min <= genes.baseVision) ? min : 1000f, angle, foodsNearby);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Food")
        {

            float dx = collider.gameObject.transform.position.x - _transform.position.x;
            float dy = collider.gameObject.transform.position.y - _transform.position.y;
            float angleBtwn = Mathf.Atan2(dy, dx);
            while (angleBtwn < 0)
                angleBtwn += Mathf.PI * 2;

           // if (angleBtwn <= Mathf.PI/4)
                Eat(collider.gameObject.GetComponent<Food>());
        }

    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Food")
        {
            //float angleBtwn = Vector3.Angle(_transform.position + new Vector3(Mathf.Cos(facingDirection), Mathf.Sin(facingDirection)), collision.gameObject.transform.position);

            float dx = collision.gameObject.transform.position.x - _transform.position.x;
            float dy = collision.gameObject.transform.position.y - _transform.position.y;
            float angleBtwn = Mathf.Atan2(dy, dx);
            while (angleBtwn < 0)
                angleBtwn += Mathf.PI * 2;

            //if (angleBtwn - facingDirection <= Mathf.PI/4)
            Eat(collision.gameObject.GetComponent<Food>());
        }
        else if (collision.gameObject.tag == "Agent") {
            if (currentEnergy / maxEnergy > 0.8f)
                SexualReproduction(collision.gameObject.GetComponent<Agent>());
        }
    }



    private void Eat(Food f)
    {
        float hunger = maxEnergy - currentEnergy;
        float dinner = 0;
        if (f.currentBioMass - hunger >= 0)
            dinner = hunger;
        else
            dinner = f.currentBioMass;

        currentEnergy += dinner;
        if (currentEnergy > maxEnergy)
            Debug.LogWarning("FUCK THIS SHOULDNT APPEAR");

        f.currentBioMass -= dinner;
        f.CheckDead();
    }


    private void Reproduce()
    {
        if (_wantsToMate && _canMate)
        {

            int litter = Random.Range(1, Mathf.CeilToInt(genes.baseMaxLitterSize) + 1);
            Debug.Log($"Someone touched themselves and had {litter} kid(s)");
            float delay = 1f;
            float inBetween = 2f;
            for (int i = 0; i < litter; i++)
                StartCoroutine(LayEgg(delay + inBetween*i));

            lastGestationAge = age + (delay + inBetween*(litterSize-1));
        }
    }

    private void SexualReproduction(Agent otherAgent)
    {
        if (_wantsToMate && _canMate && otherAgent._wantsToMate && otherAgent._canMate)
        {
            float minLitter = Mathf.Min(genes.baseMaxLitterSize, otherAgent.genes.baseMaxLitterSize);
            int litter = Random.Range(1, Mathf.CeilToInt(minLitter) + 1);
            Debug.Log($"Someone had seggs and had {litter} kid(s)");
            float delay = 1f;
            float inBetween = 2f;
            for (int i = 0; i < litter; i++)
                StartCoroutine(LaySexEgg(delay + inBetween * i, otherAgent.Brain, otherAgent.genes));

            lastGestationAge = age + (delay + inBetween * (litter));

            _canMate = false;
            otherAgent._canMate = false;
        }
    }

    IEnumerator LaySexEgg(float delay, Network otherBrain, BaseGenes otherGenes)
    {
        yield return new WaitForSeconds(delay);
        Agent baby = GameManager.instance.CreateAgent();
        baby.Birth(GameManager.instance.objIDs, genes.Mix(otherGenes, true));
        baby.Brain = Brain.CrossOver(Brain, otherBrain, false);
        baby.transform.position = transform.position;
    }

    IEnumerator LayEgg(float delay)
    {
        yield return new WaitForSeconds(delay);
        Agent baby = GameManager.instance.CreateAgent();
        baby.Birth(GameManager.instance.objIDs, genes);
        baby.Brain = Brain.CrossOver(Brain, Brain, true);
        baby.transform.position = transform.position;
    }
    

    
    public void DoTick()
    {
        See();

        Think();

        
    }

    public void DebugGismos()
    {
        if (_brainVision.Count == 0) return;

        Debug.DrawLine(_transform.position, _transform.position + new Vector3(Mathf.Cos(facingDirection) * genes.baseVision, Mathf.Sin(facingDirection) * genes.baseVision));
        Debug.DrawLine(_transform.position, _transform.position + new Vector3(Mathf.Cos(wantDirection) * genes.baseVision, Mathf.Sin(wantDirection) * genes.baseVision), Color.red);

        (float x, float y) closestFood = ((float)_brainVision[5], (float)_brainVision[6]);
        Debug.DrawLine(_transform.position, _transform.position + new Vector3(Mathf.Cos(closestFood.y) * ((closestFood.x < 1000f) ? closestFood.x : .5f), Mathf.Sin(closestFood.y) * ((closestFood.x < 1000f) ? closestFood.x : .5f)), Color.blue);

        (float x, float y) closestAgent = ((float)_brainVision[7], (float)_brainVision[8]);
        Debug.DrawLine(_transform.position, _transform.position + new Vector3(Mathf.Cos(closestAgent.y) * ((closestAgent.x < 1000f) ? closestAgent.x : .5f), Mathf.Sin(closestAgent.y) * ((closestAgent.x < 1000f) ? closestAgent.x : .5f)), Color.green);
    }

    public void DoUpdate()
    {
        Move();
        Metabolism();
    }

    void See()
    {
        _brainVision = new List<double>();

        if(biasNeuron)
            _brainVision.Add(1.0);                                     //0

        double hunger = (maxEnergy - currentEnergy);
        hunger = (hunger < 0) ? 0 : hunger;
        _brainVision.Add(hunger);                                      //1

        double speed = Mathf.Abs(CurrentSpeed / MaxSpeed);
        speed = (speed > 1) ? 1.0 : speed; 
        _brainVision.Add(speed);                                       //2

        double health = currentHealth / maxHealth;
        health = (health < 0) ? 0 : health;
        _brainVision.Add(health);                                      //3

        _brainVision.Add(_transform.rotation.z);                             //4

        //======================================
        (float x, float y, int z) closestFood = FindClosestFood();
        _brainVision.Add(closestFood.x);                               //5

        _brainVision.Add(closestFood.y);                               //6
        
        (float x, float y, int z) closestAgent = FindClosestAgent();
        _brainVision.Add( closestAgent.x );                            //7

        _brainVision.Add(closestAgent.y);                              //8
        

        _brainVision.Add(closestFood.z);                               //9

        _brainVision.Add(closestAgent.z);                              //10
        //======================================

        _brainVision.Add(age);                                         //11

        double canMate = (_canMate) ? 1.0 : 0.0;
        _brainVision.Add(canMate);                             //12

        //Debug.Log($"inputs: {string.Join(" ", _brainVision)}");

    }


    void ThinkJob()
    {
        double max = 0;
        int maxIndex = 0;

        NativeArray<double> results = new NativeArray<double>();

        Network.FeedForwardJob brainJob = new Network.FeedForwardJob();
        brainJob.vision = _brainVision;
        brainJob.brain = Brain;
        brainJob.result = results;

        JobHandle handle = brainJob.Schedule();
        handle.Complete();

        double[] temp = new double[Brain.output];
        results.CopyTo(temp);
        _brainDecisions = temp.ToList();
        results.Dispose();


        _brainDecisions = Brain.FeedForward(_brainVision);

       

        //Debug.Log($"outputs: {string.Join(" ", _brainDecisions)}");

        for (int i = 0; i < _brainDecisions.Count; i++)
        {
            if (_brainDecisions[i] > max)
            {
                max = _brainDecisions[i];
                maxIndex = i;
            }
        }


        float speedRatio;
        float turnRatio;
        switch (maxIndex)
        {
            case 0: //Forward
                speedRatio = (float)((max - 0.5) / 0.5);
                CurrentSpeed = MaxSpeed * speedRatio;
                break;
            case 1: //DOWN
                speedRatio = -(float)((max - 0.5) / 0.5);
                CurrentSpeed = MaxSpeed * speedRatio;
                break;
            case 2: //RIGHT
                turnRatio = -(float)((max - 0.5) / 0.5);
                wantDirection += genes.baseTurningSpeed * turnRatio;
                break;
            case 3: //LEFT
                turnRatio = (float)((max - 0.5) / 0.5);
                wantDirection += genes.baseTurningSpeed * turnRatio;
                break;
        }
    }

    void Think()
    {
        double max = 0;
        int maxIndex = 0;
        _brainDecisions = Brain.FeedForward(_brainVision);

        

        //Debug.Log($"outputs: {string.Join(" ", _brainDecisions)}");

        for (int i=0; i < _brainDecisions.Count; i++)
        {
            if (_brainDecisions[i] > max)
            {
                max = _brainDecisions[i];
                maxIndex = i;
            }
        }


        float speedRatio;
        float turnRatio;
        switch (maxIndex)
        {
            case 0: //Forward
                speedRatio = (float)((max - 0.5) / 0.5);
                CurrentSpeed = MaxSpeed * speedRatio;
                break;
            case 1: //DOWN
                speedRatio = -(float)((max - 0.5) / 0.5);
                CurrentSpeed = MaxSpeed * speedRatio;
                break;
            case 2: //RIGHT
                turnRatio = -(float)((max - 0.5) / 0.5);
                wantDirection += genes.baseTurningSpeed * turnRatio;
                break;
            case 3: //LEFT
                turnRatio = (float)((max - 0.5) / 0.5);
                wantDirection += genes.baseTurningSpeed * turnRatio;
                break;
        }

    }

    void Move()
    {
        facingDirection = transform.rotation.eulerAngles.z * (Mathf.PI / 180f);

        currentSize = Mathf.Clamp(maxSize * age / maturationAge, GameManager.MIN_SIZE, maxSize);
        _transform.localScale = new Vector3(currentSize, currentSize, 1f);

        Vector2 moveVel = new Vector2(Mathf.Cos(facingDirection), Mathf.Sin(facingDirection)) * (CurrentSpeed * Time.deltaTime);
        if (_rb.velocity != moveVel) {
            _rb.velocity = _rb.velocity + moveVel;
        }

        Vector3 final = _transform.position + new Vector3(_rb.velocity.x, _rb.velocity.y, 0);


        if (GameManager.IS_WORLD_WRAPPING)
        {
            float width = GameManager.instance.worldBounds.width;
            float height = GameManager.instance.worldBounds.height;
            final.x = final.x % width;
            final.y = final.y % height;

            final.x = (width + final.x) % width;
            final.y = (height + final.y) % height;   
        }
        //_transform.position = final;
        

        if (facingDirection != wantDirection)
        {
            facingDirection = Mathf.Lerp(facingDirection, wantDirection, 0.6f * Time.deltaTime);
            _transform.rotation = Quaternion.Euler(0.0f, 0.0f, facingDirection * (180 / Mathf.PI));
        }


        if(_mouseDown)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 gamePos = Camera.main.ScreenToWorldPoint(mousePos);
            transform.position = gamePos;
        }
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
        

        age += 1f * Time.deltaTime;
        currentMaturity = Mathf.Min(age / maturationAge, 1.0f);
        if (currentMaturity < 1.0f || (age - lastGestationAge) < genes.baseGestationPeriod)
            _canMate = false;
        else
            _canMate = true;

        if (age >= lifeExpectancy)
            if (UnityEngine.Random.value > 0.1f)
            {
                //Debug.Log("OLD ASS DIED");
                //isDead = true;
            }

    }

    private void OnMouseDown()
    {
        _mouseDown = true;
    }

    private void OnMouseUp()
    {
        _mouseDown = false;
    }
}



public struct BaseGenes
{
    public float baseEnergy;
    public float baseHealth;
    public float baseSize;
    public float baseMaxSpeed;
    public float baseTurningSpeed;
    public float baseVision;
    
    public float baseLifeExpectancy;
    public float baseMaturationAge;
    public float baseMaxLitterSize;
    public float baseGestationPeriod;


    public float baseColorR;
    public float baseColorG;
    public float baseColorB;


    public void Set(BaseGenes pattern, bool mutate)
    {
        baseEnergy = pattern.baseEnergy + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseEnergy * .01f, pattern.baseEnergy * 0.1f, true) : 0f);
        baseHealth = pattern.baseHealth + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseHealth * .01f, pattern.baseHealth * 0.1f, true) : 0f);
        baseSize = pattern.baseSize + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseSize * .01f, pattern.baseSize * 0.1f, true) : 0f);
        baseMaxSpeed = pattern.baseMaxSpeed + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseMaxSpeed * .01f, pattern.baseMaxSpeed * 0.1f, true) : 0f);
        baseTurningSpeed = pattern.baseTurningSpeed + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseTurningSpeed * .01f, pattern.baseTurningSpeed * 0.1f, true) : 0f);
        baseVision = pattern.baseVision + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseVision * .01f, pattern.baseVision * 0.1f, true) : 0f);

        baseLifeExpectancy = pattern.baseLifeExpectancy + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseLifeExpectancy * .01f, pattern.baseLifeExpectancy * 0.1f, true) : 0f);
        baseMaturationAge = pattern.baseMaturationAge + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseMaturationAge * .01f, pattern.baseMaturationAge * 0.1f, true) : 0f);
        baseMaxLitterSize = pattern.baseMaxLitterSize + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseMaxLitterSize * .01f, pattern.baseMaxLitterSize * 0.1f, true) : 0f);
        baseGestationPeriod = pattern.baseGestationPeriod + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseGestationPeriod * .1f, pattern.baseGestationPeriod * 0.2f, true) : 0f);


        baseColorR = Mathf.Clamp(pattern.baseColorR + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);
        baseColorG = Mathf.Clamp(pattern.baseColorG + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);
        baseColorB = Mathf.Clamp(pattern.baseColorB + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);
    }

    public BaseGenes Mix(BaseGenes otherPattern, bool mutate)
    {
        BaseGenes newGenes;
        float energy = (UnityEngine.Random.value > 0.5) ? baseEnergy : otherPattern.baseEnergy;
        newGenes.baseEnergy = energy + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(energy * .01f, energy * 0.1f, true) : 0f);
        
        float health = (UnityEngine.Random.value > 0.5) ? baseHealth : otherPattern.baseHealth;
        newGenes.baseHealth = health + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(health * .01f, health * 0.1f, true) : 0f);
        
        float size = (UnityEngine.Random.value > 0.5) ? baseSize : otherPattern.baseSize;
        newGenes.baseSize = size + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(size * .01f, size * 0.1f, true) : 0f);
        
        float maxSpeed = (UnityEngine.Random.value > 0.5) ? baseMaxSpeed : otherPattern.baseMaxSpeed;
        newGenes.baseMaxSpeed = maxSpeed + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(maxSpeed * .01f, maxSpeed * 0.1f, true) : 0f);

        float turningSpeed = (UnityEngine.Random.value > 0.5) ? baseTurningSpeed : otherPattern.baseTurningSpeed;
        newGenes.baseTurningSpeed = turningSpeed + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(turningSpeed * .01f, turningSpeed * 0.1f, true) : 0f);
        
        float vision = (UnityEngine.Random.value > 0.5) ? baseVision : otherPattern.baseVision;
        newGenes.baseVision = vision + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(vision * .01f, vision * 0.1f, true) : 0f);

        float lifeExpectancy = (UnityEngine.Random.value > 0.5) ? baseLifeExpectancy : otherPattern.baseLifeExpectancy;
        newGenes.baseLifeExpectancy = lifeExpectancy + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(lifeExpectancy * .01f, lifeExpectancy * 0.1f, true) : 0f);
        
        float maturationAge = (UnityEngine.Random.value > 0.5) ? baseMaturationAge : otherPattern.baseMaturationAge;
        newGenes.baseMaturationAge = maturationAge + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(maturationAge * .01f, maturationAge * 0.1f, true) : 0f);
        
        float litterSize = (UnityEngine.Random.value > 0.5) ? baseMaxLitterSize : otherPattern.baseMaxLitterSize;
        newGenes.baseMaxLitterSize = litterSize + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(litterSize * .01f, litterSize * 0.1f, true) : 0f);
        
        float gestationPeriod = (UnityEngine.Random.value > 0.5) ? baseGestationPeriod : otherPattern.baseGestationPeriod;
        newGenes.baseGestationPeriod = gestationPeriod + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(gestationPeriod * .1f, gestationPeriod * 0.2f, true) : 0f);


        float r = Random.Range(baseColorR, otherPattern.baseColorR);
        newGenes.baseColorR = Mathf.Clamp(r + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);

        float g = Random.Range(baseColorG, otherPattern.baseColorG);
        newGenes.baseColorG = Mathf.Clamp(g + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);

        float b = Random.Range(baseColorB, otherPattern.baseColorB);
        newGenes.baseColorB = Mathf.Clamp(b + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);

        return newGenes;
    }


    public void RandomizeBaseValues()
    {
        baseEnergy = (Random.Range(75f, 85f));
        baseSize = NativeMethods.GetRandomFloat(.9f, 1.1f);
        baseSize = Mathf.Clamp(baseSize, GameManager.MIN_SIZE, GameManager.MAX_SIZE);
        //baseHealth = 100;
        baseMaxSpeed = 2.1f;
        baseTurningSpeed = UnityEngine.Random.Range(Mathf.PI / 8, Mathf.PI / 4);
        baseVision = 10f;

        baseMaturationAge = new System.Random().Next(5, 20);
        baseLifeExpectancy = baseMaturationAge + Random.Range(5, 7);
        baseMaxLitterSize = Random.Range(1f, 3f);
        baseGestationPeriod = Random.Range(12f, 15f);

        baseColorR = Random.Range(0f, 1f);
        baseColorG = Random.Range(0f, 1f);
        baseColorB = Random.Range(0f, 1f);
    }
}

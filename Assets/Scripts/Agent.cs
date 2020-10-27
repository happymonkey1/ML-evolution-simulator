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

    private Transform _transform;
    private SpriteRenderer _sr;


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
    public float facingDirection;
    public float wantDirection;
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
    public int ID { get; set; }

    

    // Start is called before the first frame update
    void Awake()
    {
        _transform = GetComponent<Transform>();
        _velocity = new Vector2(0f, 0f);
        _sr = GetComponentInChildren<SpriteRenderer>();


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
        currentSize = Mathf.Clamp(genes.baseSize * currentMaturity, GameManager.MIN_SIZE, maxSize);
        currentHealth = maxHealth * NativeMethods.InverseE;
        currentEnergy = maxEnergy;

        ID = id;
        GameManager.instance.agents.Add(this);

        _transform.localScale = new Vector3(currentSize, currentSize, 1f);

        float x = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.x, GameManager.instance.worldBounds.width, false));
        float y = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.y, GameManager.instance.worldBounds.height, false));
        _transform.position = new Vector3(x, y, 0);

        _sr.color = new Color(genes.baseColorR, genes.baseColorG, genes.baseColorB, 1f);

        facingDirection = wantDirection = (float)(new System.Random().NextDouble() * Mathf.PI * 2);
        isDead = false;

        _wantsToMate = true;

        Brain = Preset.EmptyRandom(neuronInput, new System.Random().Next(0, 3), neuronOutput, biasNeuron);
        
    }

    public (bool, float) FindClosestAgent()
    {
        if (GameManager.instance.agents.Count == 0) return (false, 0);

        float min = float.PositiveInfinity;
        float angle = 0f;
        for (int i = 0; i < GameManager.instance.agents.Count; i++)
        {
            Agent a = GameManager.instance.agents[i];
            if (a.isDead) continue;
            if (a.ID == this.ID) continue;
            

            float dx = a.transform.position.x - transform.position.x;
            float dy = a.transform.position.y - transform.position.y;
            float dist = Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2);
            if (dist < min)
            {
                angle = Mathf.Atan2(dy, dx);
                min = dist;
            }
        }


        while (angle < 0)
            angle += Mathf.PI * 2;

        return ((min <= genes.baseVision) ? true : false, angle);
    }

    public (bool, float) FindClosestFood()
    {
        if (GameManager.instance.foods.Count == 0) return (false, -1);
        float min = float.PositiveInfinity;
        float angle = 0;

        Collider2D[] foundColliders = Physics2D.OverlapCircleAll(_transform.position, genes.baseVision);
        foreach (Collider2D col in foundColliders)
        {
            if (col.gameObject.tag == "Food")
            {
                Food f = col.gameObject.GetComponent<Food>();
                if (f.isDead) continue;

                float dx = f.transform.position.x - transform.position.x;
                float dy = f.transform.position.y - transform.position.y;
                float dist = Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2);
                if (dist < min)
                {

                    min = dist;
                    angle = Mathf.Atan2(dy, dx);
                }
            }
        }
        /*for (int i=0; i < GameManager.instance.foods.Count; i++)
        {
            Food f = GameManager.instance.foods[i];
            if (f.isDead) continue;

            float dx = f.transform.position.x - transform.position.x;
            float dy = f.transform.position.y - transform.position.y;
            float dist = Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2);
            if (dist < min)
            {

                min = dist;
                angle = Mathf.Atan2(dy, dx);
            }
        }*/

        while (angle < 0)
            angle += Mathf.PI * 2;

        return ((min <= genes.baseVision) ? true : false, angle);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Food")
        {
            Eat(collider.gameObject.GetComponent<Food>());
        }

    }
     void OnCollisionEnter2D(Collision2D collision)
    {
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

        f.currentBioMass = 0;
        f.CheckDead();
    }

    private void Reproduce()
    {

        if (_wantsToMate && _canMate)
        {

            int litter = Random.Range(1, Mathf.CeilToInt(genes.baseMaxLitterSize)+1);
            if (litter == 0)
                litter = 1;
            Debug.Log($"Someone touched themselves and had {litter} kid(s)");
            float delay = 1f;
            float inBetween = 2f;
            for (int i = 0; i < litter; i++)
                StartCoroutine(LayEgg(delay + inBetween*i));

            lastGestationAge = age + (delay + inBetween*(litterSize-1));
        }

        
        
    }

    IEnumerator LayEgg(float delay)
    {
        yield return new WaitForSeconds(delay);
        Agent baby = GameManager.instance.CreateAgent();
        baby.Birth(GameManager.instance.objIDs, genes);
        baby.Brain = Brain.CrossOver(Brain, Brain, true);
        baby.transform.position = transform.position;
    }
    

    void Update()
    {
        
    }

    public void DoTick()
    {
        See();

        Think();

        Move();

        Metabolism();
    }

    void See()
    {
        _brainVision = new List<double>();

        if(biasNeuron)
            _brainVision.Add(1.0);                                     //0

        double hunger = (currentEnergy / maxEnergy);
        hunger = (hunger < 0) ? 0 : hunger;
        _brainVision.Add(currentEnergy);                               //1

        double speed = Mathf.Abs(CurrentSpeed / MaxSpeed);
        speed = (speed > 1) ? 1.0 : speed; 
        _brainVision.Add(CurrentSpeed);                                //2

        double health = currentHealth / maxHealth;
        health = (health < 0) ? 0 : health;
        _brainVision.Add(health);                                      //3

        //======================================
        (bool x, float y) closestFood = FindClosestFood();
        _brainVision.Add((closestFood.x) ? 1.0f : 0.0f);               //4

        _brainVision.Add(closestFood.y);                               //5

        (bool x, float y) closestAgent = FindClosestAgent();
        _brainVision.Add( (closestAgent.x) ? 1.0f : 0.0f );            //6

        _brainVision.Add(closestAgent.y);                              //7
        //======================================

        _brainVision.Add(age);

        _canMate = (currentMaturity >= 1.0) ? true : false;
        _brainVision.Add(currentMaturity);

        //Debug.Log($"inputs: {string.Join(" ", _brainVision)}");

    }


    // Update is called once per frame


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

        if (max < 0.6) return;

        switch (maxIndex)
        {
            case 0: //Forward
                CurrentSpeed = MaxSpeed;
                break;
            case 1: //DOWN
                CurrentSpeed = -MaxSpeed;
                break;
            case 2: //RIGHT
                wantDirection = facingDirection - (Mathf.PI / 8);
                break;
            case 3: //LEFT
                wantDirection = facingDirection + (Mathf.PI / 8);
                break;

        }

    }

    void Move()
    {
        currentSize = Mathf.Clamp(maxSize * age / maturationAge, GameManager.MIN_SIZE, maxSize);
        _transform.localScale = new Vector3(currentSize, currentSize, 1f);

        _velocity = new Vector2(Mathf.Cos(facingDirection), Mathf.Sin(facingDirection)) * (CurrentSpeed * Time.deltaTime);
        Vector3 final = _transform.position + new Vector3(_velocity.x, _velocity.y, 0);

        float width = GameManager.instance.worldBounds.width;
        float height = GameManager.instance.worldBounds.height;
        final.x = final.x % width;
        final.y = final.y % height;

        final.x = (width + final.x) % width;
        final.y = (height + final.y) % height; 

        _transform.position = final;

        if (facingDirection != wantDirection)
        {
            facingDirection = Mathf.Lerp(facingDirection, wantDirection, 0.6f * Time.deltaTime);
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
        

        age += 1 * Time.deltaTime;
        currentMaturity = Mathf.Min(age / maturationAge, 1.0f);
        if (currentMaturity <= 1.0f && (age - lastGestationAge) < genes.baseGestationPeriod)
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
    

}



public struct BaseGenes
{
    public float baseEnergy;
    public float baseHealth;
    public float baseSize;
    public float baseMaxSpeed;
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
        baseVision = pattern.baseVision + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseVision * .01f, pattern.baseVision * 0.1f, true) : 0f);

        baseLifeExpectancy = pattern.baseLifeExpectancy + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseLifeExpectancy * .01f, pattern.baseLifeExpectancy * 0.1f, true) : 0f);
        baseMaturationAge = pattern.baseMaturationAge + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseMaturationAge * .01f, pattern.baseMaturationAge * 0.1f, true) : 0f);
        baseMaxLitterSize = pattern.baseMaxLitterSize + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseMaxLitterSize * .01f, pattern.baseMaxLitterSize * 0.1f, true) : 0f);
        baseGestationPeriod = pattern.baseGestationPeriod + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(pattern.baseGestationPeriod * .1f, pattern.baseGestationPeriod * 0.2f, true) : 0f);


        baseColorR = Mathf.Clamp(pattern.baseColorR + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);
        baseColorG = Mathf.Clamp(pattern.baseColorG + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);
        baseColorB = Mathf.Clamp(pattern.baseColorB + ((mutate && UnityEngine.Random.value < Globals.BASE_GENE_MUTATION_CHANCE) ? NativeMethods.GetRandomFloat(.01f, .1f, true) : 0f), 0f, 1f);
    }


    public void RandomizeBaseValues()
    {
        baseEnergy = (Random.Range(75f, 85f));
        baseSize = NativeMethods.GetRandomFloat(.9f, 1.1f);
        baseSize = Mathf.Clamp(baseSize, GameManager.MIN_SIZE, GameManager.MAX_SIZE);
        //baseHealth = 100;
        baseMaxSpeed = 2.1f;
        baseVision = 5;

        baseMaturationAge = new System.Random().Next(5, 20);
        baseLifeExpectancy = baseMaturationAge + Random.Range(5, 7);
        baseMaxLitterSize = Random.Range(1f, 3f);
        baseGestationPeriod = Random.Range(12f, 15f);

        baseColorR = Random.Range(0f, 1f);
        baseColorG = Random.Range(0f, 1f);
        baseColorB = Random.Range(0f, 1f);
    }
}

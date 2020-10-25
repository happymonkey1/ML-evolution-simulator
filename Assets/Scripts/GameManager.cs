using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{


    public static GameManager instance;


    public int worldWidth;
    public int worldHeight;
    public Rect worldBounds; 
    public int objIDs { get; set; }

    public List<Agent> agents = new List<Agent>();
    public List<Food> foods = new List<Food>();

    public int maxFood;
    public int maxAgents;

    public GameObject agentPrefab;
    public GameObject foodPrefab;

    public static float MIN_SPEED = 0.2f;
    public static float MAX_SPEED = 100f;
    public static float MIN_SIZE = 0.1f;

    // Start is called before the first frame update
    void Awake()
    {
        Restart();
    }

    void Restart()
    {
        if (instance == null)
            instance = this;

        worldBounds = new Rect(-worldWidth / 2, -worldHeight / 2, worldWidth, worldHeight);

        objIDs = 0;

        agents = new List<Agent>();
        foods = new List<Food>();
    }

    public Agent CreateAgent()
    {
        return Instantiate(agentPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Agent>();
    }

    void CreateFood()
    {
        Instantiate(foodPrefab, new Vector3(0, 0, 0), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        maxFood = 100 * 200 / agents.Count;

        if (agents.Count < maxAgents)
            CreateAgent();

        if (foods.Count < maxFood)
            CreateFood();

        for (int i = 0; i < agents.Count; i++)
            if (agents[i].isDead)
                Destroy(agents[i]);
    }
}

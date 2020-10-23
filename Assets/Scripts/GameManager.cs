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

    public Dictionary<int, Agent> agents = new Dictionary<int, Agent>();
    public Dictionary<int, Food> foods = new Dictionary<int, Food>();

    public int maxFood;
    public int maxAgents;

    public GameObject agentPrefab;
    public GameObject foodPrefab;

    public static float MIN_SPEED = 0.2f;
    public static float MAX_SPEED = 100f;

    // Start is called before the first frame update
    void Awake()
    {
        Genome g = new Genome();


        Restart();
    }

    void Restart()
    {
        if (instance == null)
            instance = this;

        worldBounds = new Rect(-worldWidth / 2, -worldHeight / 2, worldWidth, worldHeight);

        objIDs = 0;

        agents = new Dictionary<int, Agent>();
        foods = new Dictionary<int, Food>();
    }

    void CreateAgent()
    {
        Instantiate(agentPrefab, new Vector3(0, 0, 0), Quaternion.identity);
    }

    void CreateFood()
    {
        Instantiate(foodPrefab, new Vector3(0, 0, 0), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        if (agents.Count < maxAgents)
            CreateAgent();

        if (foods.Count < maxFood)
            CreateFood();
    }
}

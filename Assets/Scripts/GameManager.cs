using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{


    public static GameManager instance;


    public int worldWidth;
    public int worldHeight;
    public Rect worldBounds; 
    public int objIDs { get; set; }

    public List<Agent> agents = new List<Agent>();
    public int currentAgents = 0;
    public List<Food> foods = new List<Food>();

    public int ticksPerFrame = 1;

    public int maxFood;
    public int initialAgentPopulation;
    private int _spawnedAgents;

    public GameObject agentPrefab;
    public GameObject foodPrefab;

    public static float MIN_SPEED = 0.2f;
    public static float MAX_SPEED = 100f;
    public static float MIN_SIZE = 0.4f;
    public static float MAX_SIZE = 1.25f;

    public static bool IS_WORLD_WRAPPING = false;

    private float k;
    // Start is called before the first frame update
    void Awake()
    {
        Restart();
    }

    void Restart()
    {
        if (instance == null)
            instance = this;


        worldBounds = new Rect(0, 0, worldWidth, worldHeight);

        objIDs = 0;

        agents = new List<Agent>();
        foods = new List<Food>();

        _spawnedAgents = 0;
        k = 1;
    }

    public Agent CreateAgent()
    {
        return Instantiate(agentPrefab, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<Agent>();
        
    }

    void CreateFood()
    {
        Instantiate(foodPrefab, new Vector3(0, 0, 0), Quaternion.identity);
    }

    void DestroyRandomFood()
    {
        int removeIndex = Random.Range(0, foods.Count - 1);
        Food f = foods[removeIndex];
        foods.RemoveAt(removeIndex);

        Destroy(f.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
        //maxFood = 300;

        for (int t = 0; t < ticksPerFrame; t++)
        {
            k = Mathf.Exp((300 - agents.Count) / (100 * 5f));
            maxFood = (int)(k * 100 * (300 / ((agents.Count > 0) ? agents.Count : 1)));
            if (agents.Count < initialAgentPopulation)
            {
                CreateAgent().Birth(objIDs++);
            }

            if (foods.Count < maxFood)
                CreateFood();
            else if (foods.Count > maxFood)
                DestroyRandomFood();


            /*Parallel.For(0, agents.Count, i => {
                if (agents[i].isDead)
                {
                    GameObject g = agents[i].gameObject;
                    agents.RemoveAt(i);
                    Destroy(g);
                }

                agents[i].DoTick();
            });*/

            for (int i = 0; i < agents.Count; i++)
            {
                agents[i].DoTick();
                if (agents[i].isDead)
                {
                    GameObject g = agents[i].gameObject;
                    agents.RemoveAt(i);
                    Destroy(g);
                }
            }

            currentAgents = agents.Count;

        }
    }
}

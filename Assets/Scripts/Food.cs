using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Food : MonoBehaviour
{

    private Transform _transform;
    private SpriteRenderer _spriteRenderer;
    public enum BioMassType
    {
        Plant,
        Meat
    }

    public float currentBioMass;
    public float maxBioMass;

    public float respawnDelay;

    public BioMassType type;
    public int ID;

    public bool isDead = false;

    // Start is called before the first frame update
    void Start()
    {
        if (_transform == null)
            _transform = transform;

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Create(GameManager.instance.objIDs++);
    }

    void Create(int id)
    {
        this.ID = id;
        currentBioMass = maxBioMass;

        if (type == BioMassType.Meat)
            _spriteRenderer.color = Color.red;
        else if (type == BioMassType.Plant)
            _spriteRenderer.color = Color.green;

        GameManager.instance.foods.Add(this);

        float x = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.x, GameManager.instance.worldBounds.width / 2));
        float y = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.y, GameManager.instance.worldBounds.height / 2));
        _transform.position = new Vector3(x, y, 0);

        isDead = false;
    }

    public IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        

        Create(this.ID);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentBioMass <= 0)
            StartCoroutine(Respawn());
    }
}

﻿using System.Collections;
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
        GameManager.instance.foods.Add(this);
        this.ID = id;

        Initialize();
    }

    void Initialize()
    {
        
        currentBioMass = maxBioMass;

        if (type == BioMassType.Meat)
            _spriteRenderer.color = Color.red;
        else if (type == BioMassType.Plant)
            _spriteRenderer.color = Color.green;



        float x = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.x, GameManager.instance.worldBounds.width, false));
        float y = (NativeMethods.GetRandomFloat(GameManager.instance.worldBounds.y, GameManager.instance.worldBounds.height, false));
        _transform.position = new Vector3(x, y, 0);

        isDead = false;
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);



        Initialize();
    }

    public void CheckDead()
    {
        if (currentBioMass <= 0)
            StartCoroutine(Respawn());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}

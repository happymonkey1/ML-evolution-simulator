using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class NEAT 
{

    public int input;
    public int output;


    public delegate double Fitness(Network genome, dynamic set, int amount = 1, double growth = 0.0001);
    public Fitness fitness;


    public int popSize = 50;
    public int elitism = 0;
    public int provenance = 0;
    public float mutationRate = 0.3f;
    public int mutationAmount = 1;

    public bool fitnessPopulation = false;
    public int generation = 0;
    public NEAT(int input, int output, Fitness fitness)
    {
        this.input = input;
        this.output = output;
        this.fitness = fitness;


        

    }

    public async Task<Network> Evolve()
    {
        return new Network(0, 0);
        
    }
}

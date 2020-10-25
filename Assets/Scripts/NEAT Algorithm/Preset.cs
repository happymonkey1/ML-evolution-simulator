using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Preset
{
    

    public static Network Random(int input, int hidden, int output)
    {

        int connections = hidden * 2;
        int backConnections = Globals.BACK_CONNECTIONS;
        int selfConnections = Globals.SELF_CONNECTIONS;
        int gates = Globals.GATES;


        Network network = new Network(input, output);

        for (int i=0; i < hidden; i++)
            network.Mutate(MUTATION_TYPE.ADD_NODE);

        for (int i = 0; i < connections; i++)
            network.Mutate(MUTATION_TYPE.ADD_CONN);

        for (int i = 0; i < backConnections; i++)
            network.Mutate(MUTATION_TYPE.ADD_BACK_CONN);

        for (int i = 0; i < selfConnections; i++)
            network.Mutate(MUTATION_TYPE.ADD_SELF_CONN);

        for (int i = 0; i < gates; i++)
            network.Mutate(MUTATION_TYPE.ADD_GATE);

        

        return network;
    }

    public static Network RandomPlusBias(int input, int hidden, int output)
    {

        int connections = hidden * 2;
        int backConnections = Globals.BACK_CONNECTIONS;
        int selfConnections = Globals.SELF_CONNECTIONS;
        int gates = Globals.GATES;


        Network network = new Network(input, output);

        for (int i = 0; i < hidden; i++)
            network.Mutate(MUTATION_TYPE.ADD_NODE);

        for (int i = 0; i < connections; i++)
            network.Mutate(MUTATION_TYPE.ADD_CONN);

        for (int i = 0; i < backConnections; i++)
            network.Mutate(MUTATION_TYPE.ADD_BACK_CONN);

        for (int i = 0; i < selfConnections; i++)
            network.Mutate(MUTATION_TYPE.ADD_SELF_CONN);

        for (int i = 0; i < gates; i++)
            network.Mutate(MUTATION_TYPE.ADD_GATE);



        return network;
    }
}

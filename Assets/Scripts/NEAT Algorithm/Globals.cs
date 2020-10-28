using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globals
{

    public const int MAX_ITERATIONS = 100;
    public const bool CLEAR = true;

    public const int BACK_CONNECTIONS = 0;
    public const int SELF_CONNECTIONS = 0;
    public const int GATES = 0;

    public const float MUTATION_CHANCE = .3f;
    public const float BASE_GENE_MUTATION_CHANCE = 0.05f;

    public const bool ALLOW_OUTPUT_ACTIVATION_MUTATION = false;
}

public class Mutations
{
    public const bool SUB_NODE_KEEP_GATES = true;
    public static List<MUTATION_TYPE> ALLOWED_MUTATIONS = new List<MUTATION_TYPE>() { 
        MUTATION_TYPE.ADD_NODE, 
        MUTATION_TYPE.SUB_NODE,
        MUTATION_TYPE.MOD_WEIGHT, 
        MUTATION_TYPE.ADD_CONN, 
        MUTATION_TYPE.SUB_CONN,
        MUTATION_TYPE.MOD_ACTIVATION
    };
}

public enum MUTATION_TYPE
{
    ADD_NODE,
    SUB_NODE,
    ADD_CONN,
    SUB_CONN,
    MOD_WEIGHT,
    MOD_BIAS,
    MOD_ACTIVATION,
    ADD_SELF_CONN,
    SUB_SELF_CONN,
    ADD_GATE,
    SUB_GATE,
    ADD_BACK_CONN,
    SUB_BACK_CONN,
    SWAP_NODES
}

public struct CONNECTION_TYPE
{
    public const string ALL_TO_ALL = "OUTPUT";
    public const string ALL_TO_ELSE = "INPUT";
    public const string ONE_TO_ONE = "SELF";
}

public struct GATING_TYPE
{
    public const string OUTPUT = "OUTPUT";
    public const string INPUT = "INPUT";
    public const string SELF = "SELF";
}

public struct Connections
{
    public List<Connection> In { get; set; }
    public List<Connection> Out { get; set; }
    public List<Connection> Gated { get; set; }
    public Connection self;
    public Connections(Node f, Node t)
    {
        In = new List<Connection>();
        Out = new List<Connection>();
        Gated = new List<Connection>();
        self = new Connection(f, t, 0);
    }

}

public struct GroupConnections
{
    public List<Connection> In { get; set; }
    public List<Connection> Out { get; set; }
    public List<Connection> Gated { get; set; }
    public List<Connection> Self { get; set; }

}

public struct CrossTrace
{
    public List<Node> Nodes { get; set; }
    public List<double> Values { get; set; }

}
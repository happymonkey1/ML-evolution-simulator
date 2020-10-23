using System.Collections;
using System.Collections.Generic;
using System;
public class Node
{
    public enum NodeType
    {
        INPUT,
        HIDDEN,
        OUTPUT
    }

    public NodeType type;
    public int activationIndex;
    public double bias;

    public delegate double Squash(double x, bool derivate = false);
    public Squash squash;

    public double activation;
    private double _derivative;
    public double state;
    public double old;
    public double mask;

    public Connections connections;

    public double responsibilityError;
    public double projectedError;
    public double gatedError;

    public Node(NodeType t, int i)
    {
        type = t;
        activationIndex = i;

        Init();
    }

    public Node(int i)
    {
        type = NodeType.HIDDEN;
        activationIndex = i;

        Init();
    }

    public void Init()
    {
        bias = (type == NodeType.INPUT) ? 0 : new Random().NextDouble() * 0.2 - 0.1;
        activation = state = old = 0.0;

        mask = 1.0;
        connections = new Connections(this, this);

        responsibilityError = 0;
        projectedError = 0;
        gatedError = 0;
    }

    public double Activate(double? input)
    {
        if (input != null)
        {
            activation = (double)input;
            return activation;
        }

        old = state;
        state = connections.self.gain * connections.self.weight * state + bias;

        for (int i=0; i < this.connections.In.Count; i++)
        {
            Connection c = connections.In[i];
            state += c.From.activation * c.weight * c.gain;
        }

        activation = squash(state) * mask;
        _derivative = squash(state, true);

        List<Node> nodes = new List<Node>();
        List<double> influences = new List<double>();

        for (int i=0; i < connections.Gated.Count; i++)
        {
            Connection c = connections.Gated[i];
            Node n = c.To;

            int index = nodes.IndexOf(n);
            if (index > -1)
                influences[index] += c.weight * c.From.activation;
            else
            {
                nodes.Add(n);
                influences.Add(c.weight * c.From.activation + ((n.connections.self.Gater == this) ? n.old : 0));
            }
            c.gain = activation;
        }
        
        for (int i=0; i < connections.In.Count; i++)
        {
            Connection c = connections.In[i];

            c.eligibility = connections.self.gain * connections.self.weight * c.eligibility + c.From.activation * c.gain;

            for (int j =0; j < nodes.Count; j++)
            {
                Node node = nodes[j];
                double influence = influences[j];

                int index = c.crossTrace.Nodes.IndexOf(node);
                if (index > -1)
                    c.crossTrace.Values[index] = node.connections.self.gain * node.connections.self.weight * c.crossTrace.Values[index] + _derivative * c.eligibility * influence;
                else
                {
                    c.crossTrace.Nodes.Add(node);
                    c.crossTrace.Values.Add(_derivative * c.eligibility * influence);
                }
            }
        }
    }

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

public struct CrossTrace
{
    public List<Node> Nodes { get; set; }
    public List<double> Values { get; set; }

}

public class Connection
{
    public Node From { get; set; }
    public Node To { get; set; }
    public Node Gater { get; set; }
    public double weight;
    
    public double gain;
    public double eligibility;

    public double previousDeltaWeight = 0;
    public double totalDeltaWeight = 0;

    public CrossTrace crossTrace;
    public Connection(Node from, Node to, double w = 0.0)
    {
        From = from;
        To = to;

        crossTrace.Nodes = new List<Node>();
        crossTrace.Values = new List<double>();
        weight = (w == 0.0) ? new Random().NextDouble() * 0.2 - 0.1 : w;
        gain = 1;

        previousDeltaWeight = 0.0;
        totalDeltaWeight = 0.0;
        eligibility = 0.0;
    }
}

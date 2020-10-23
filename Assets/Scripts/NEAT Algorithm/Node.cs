using System.Collections;
using System.Collections.Generic;
using System;
public class Node
{
    public enum NodeType
    {
        INPUT,
        HIDDEN,
        OUTPUT,
        CONSTANT
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
    public double previousDeltaBias;
    public double totalDeltaBias;

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

        
        connections = new Connections(this, this);

        responsibilityError = 0.0;
        projectedError = 0.0;
        gatedError = 0.0;

        mask = 1.0;
        previousDeltaBias = 0.0;
        totalDeltaBias = 0.0;
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

        return activation;
    }


    public void Propogate(bool update, double target, double? rate = null, double? momentum = null)
    {
        rate = (rate != null) ? rate : 0.3;
        momentum = (momentum != null) ? momentum : (double)0.3;

        double error = 0.0;
        if (type == NodeType.OUTPUT)
            responsibilityError = projectedError = target - activation;
        else
        {
            for (int i=0; i < connections.Out.Count; i++)
            {
                Connection c = connections.Out[i];
                Node n = c.To;

                error += n.responsibilityError * c.weight * c.gain;
            }


            projectedError = _derivative * error;
            error = 0.0;

            for (int i =0; i < connections.Gated.Count; i++)
            {
                Connection c = connections.Gated[i];
                Node n = c.To;
                double influence = (n.connections.self.Gater == this) ? n.old : 0.0;

                influence += c.weight * c.From.activation;
                error = n.responsibilityError * influence;
            }


            gatedError = _derivative * error;
            responsibilityError = projectedError + gatedError;
        }

        if (type == NodeType.CONSTANT) return;

        for (int i=0; i < connections.In.Count; i++)
        {
            Connection c = connections.In[i];
            double gradient = projectedError * c.eligibility;

            for (int j=0; j < c.crossTrace.Nodes.Count; j++)
            {
                Node node = c.crossTrace.Nodes[j];
                double value = c.crossTrace.Values[j];
                gradient += node.responsibilityError * value;
            }

            double deltaWeight = (double)rate * (double)gradient * mask;
            c.totalDeltaWeight += deltaWeight;

            if (update)
            {
                c.totalDeltaWeight += (double)momentum * c.previousDeltaWeight;
                c.weight += c.totalDeltaWeight;
                c.previousDeltaWeight = c.totalDeltaWeight;
                c.totalDeltaWeight = 0.0;
            }
        }

        double deltaBias = (double)rate * responsibilityError;
        totalDeltaBias += deltaBias;
        if (update)
        {
            totalDeltaBias = (double)momentum * previousDeltaBias;
            bias += totalDeltaBias;
            previousDeltaBias = totalDeltaBias;
            totalDeltaBias = 0; 
        }
    }

    public void Connect<T>(T t, double? weight = null)
    {
        List<Connection> connections = new List<Connection>();
        var target = (T is Node) ? t as Node : t as Genome;
        if (target.GetType() is typeof(Node))
        {
            if (target == this)
            {
                if (this.connections.self.weight == 0.0)
                    this.connections.self.weight = (weight != null) ? (double)weight : 0.0;

                connections.Add(this.connections.self);
            }
            else if (IsProjectingTo(target))
                throw new System.ArgumentException("This connection already exists");
            else
            {
                Connection c = new Connection(this, target, (double)weight);
                target.connections.In.Add(c);
                this.connections.Out.Add(c);

                connections.Add(c);
            }
        }
    }

    public bool IsProjectingTo(Node target)
    {
        return false;
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

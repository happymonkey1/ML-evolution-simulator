using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;
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
    public int index;
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

    public Node(NodeType t)
    {
        type = t;

        Init();
    }

    public Node()
    {
        type = NodeType.HIDDEN;

        Init();
    }

    public void Init()
    {
        bias = (type == NodeType.INPUT) ? 0 : 0;//new Random().NextDouble() * 0.2 - 0.1;
        activation = state = old = 0.0;

        
        connections = new Connections(this, this);

        responsibilityError = 0.0;
        projectedError = 0.0;
        gatedError = 0.0;
        state = 0.0;

        squash = ACTIVATION.SIGMOID;
        mask = 1.0;
        previousDeltaBias = 0.0;
        totalDeltaBias = 0.0;
    }

    public double Activate(double? input = null)
    {
        if (type == Node.NodeType.CONSTANT) return 1.0;

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

    public double NoTraceActivate(double? input = null)
    {
        if (input != null)
        {
            activation = (double)input;
            return activation;
        }

        state = connections.self.gain * connections.self.weight * state + bias;

        for(int i = 0; i < connections.In.Count; i++)
        {
            Connection c = connections.In[i];
            state += c.From.activation * c.weight * c.gain;
        }

        activation = squash(state);

        for (int i = 0; i < connections.Gated.Count; i++)
            connections.Gated[i].gain = activation;

        return activation;
    }

    public double NoTraceActivateForward(double? input = null)
    {
        if (type != NodeType.INPUT)
        {
            activation = squash(state);
        }
        else
        {
            activation = (double)input;
        }

        for (int i=0; i < connections.Out.Count; i++)
        {
            Connection c = connections.Out[i];
            c.To.state += c.weight * activation * c.gain;
        }

        for (int i = 0; i < connections.Gated.Count; i++)
            connections.Gated[i].gain = activation;


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

    public List<Connection> Connect(Node target, double? weight = null)
    {
        List<Connection> connections = new List<Connection>();
        if (target == this)
        {
            if (this.connections.self.weight == 0.0)
                this.connections.self.weight = (weight != null) ? (double)weight : 1.0;

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
        return connections;
    }

    public List<Connection> Connect(NodeGroup target, double? weight = null)
    {
        List<Connection> connections = new List<Connection>();
        for (var i = 0; i < target.nodes.Count; i++)
        {
            double w = (weight != null) ? (double)weight : 1.0;
            Connection c = new Connection(this, target.nodes[i], w);
            target.nodes[i].connections.In.Add(c);
            this.connections.Out.Add(c);
            target.connections.In.Add(c);

            connections.Add(c);
        }

        return connections;
    }

    public void Disconnect(Node node, bool twoSided)
    {
        if (this == node)
        {
            connections.self.weight = 0.0;
            return;
        }

        for (int i=0; i < this.connections.Out.Count; i++)
        {
            Connection c = this.connections.Out[i];
            if (c.To == node)
            {
                connections.Out.RemoveAt(i);
                int j = c.To.connections.In.IndexOf(c);
                c.To.connections.In.RemoveAt(j);
                if (c.Gater != null)
                    c.Gater.UnGate(c);
                break;
            }
        }

        if (twoSided)
            node.Disconnect(this, false);
    }

    public void Gate(params Connection[] conns)
    {
        for (int i=0; i < conns.Length; i++)
        {
            Connection c = conns[i];

            connections.Gated.Add(c);
            c.Gater = this;
        }
    }

    public void UnGate(params Connection[] conns)
    {
        for (int i = 0; i < conns.Length; i++)
        {
            Connection c = conns[i];

            int index = connections.Gated.IndexOf(c);
            connections.Gated.RemoveAt(index);
            c.Gater = null;
            c.gain = 1;
        }
    }

    public void Clear()
    {
        for (int i=0; i < connections.In.Count; i++)
        {
            Connection c = connections.In[i];

            c.eligibility = 0.0;
            c.crossTrace.Nodes = new List<Node>();
            c.crossTrace.Values = new List<double>();
        }

        for (int i=0; i < connections.Gated.Count; i++)
        {
            Connection c = connections.Gated[i];
            c.gain = 0.0;
        }

        responsibilityError = projectedError = gatedError = 0.0;
        old = state = activation = 0;
    }

    public void Mutate(MUTATION_TYPE method)
    {
        throw new MissingMethodException();
    }

    public bool IsProjectingTo(Node target)
    {
        if (target == this && connections.self.weight != 0.0) return true;

        for (int i=0; i < connections.Out.Count; i++)
        {
            Connection c = connections.Out[i];
            if (c.To == target)
                return true;
        }

        
        return false;
    }
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
        weight = (w == 0.0) ? UnityEngine.Random.Range(-1f, 1f) : w;
        gain = 1.0;

        previousDeltaWeight = 0.0;
        totalDeltaWeight = 0.0;
        eligibility = 0.0;
    }

    //https://en.wikipedia.org/wiki/Pairing_function (Cantor pairing function)
    public static int InnovationID(int a, int b) { return 1 / 2 * (a + b) * (a + b + 1) + b; }
}


public struct ConnectionData
{
    public double weight;
    public int from;
    public int to;
    public int gater;

    public ConnectionData(double w, int f, int t, int g)
    {
        weight = w;
        from = f;
        to = t;
        gater = g;
    }
}

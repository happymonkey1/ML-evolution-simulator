using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Layer
{

    public NodeGroup output = null;
    public List<Node> nodes = new List<Node>();
    public Connections connections = new Connections();

    public Layer()
    {
        output = null;
        nodes = new List<Node>();
        connections = new Connections();
    }

    public List<double> Activate(params double[] vs)
    {
        List<double> values = new List<double>();
        int valuesLength = vs.Length;
        values.AddRange(vs);

        List<double> returnValues = new List<double>();

        if (valuesLength > 0 && valuesLength != nodes.Count)
            throw new System.Exception("Values and nodes list should be the same");

        for (int i = 0; i < nodes.Count; i++) {
            double activation;
            if (valuesLength == 0)
                activation = nodes[i].Activate();
            else
                activation = nodes[i].Activate(values[i]);

            returnValues.Add(activation);
        }

        return returnValues;
    }

    public void Propogate(double rate, double momentum, List<double> targets = null)
    {
        if (targets != null && targets.Count != nodes.Count)
            throw new System.Exception("Targets and nodes list should be the same");

        for(int i= nodes.Count-1; i >= 0; i--)
        {
            if (targets == null)
            {
                Debug.LogWarning("CHECK TARGET INPUT VALUE");
                nodes[i].Propogate(true, 0.0, rate, momentum);
            }
            else 
            {
                nodes[i].Propogate(true, targets[i], rate, momentum);
            }
        }
    }

    public List<Connection> Connect(NodeGroup target, string method, double weight) { return output.Connect(target, method, weight); }

    public List<Connection> Connect(Node target, string method, double weight) { return output.Connect(target, method, weight); }

    public List<Connection> Connect(Layer target, string method, double weight) { return target.Input(this, method, weight); }

    public void Gate(List<Connection> conns, string method) { this.output.Gate(conns, method); }

    public void Set(Node values)
    {
        for(int i=0; i < nodes.Count; i++)
        {
            Node node = nodes[i];

            node.bias = values.bias;
            node.squash = values.squash;
            node.type = values.type;

        }
    }

    public void Disconnect(NodeGroup target, bool twoSided = false)
    {
        for (int i=0; i < nodes.Count; i++)
        {
            for (int j=0; j < target.nodes.Count; j++)
            {
                nodes[i].Disconnect(target.nodes[j], twoSided);

                for (int k=connections.Out.Count-1; k >= 0; k--)
                {
                    Connection c = connections.Out[k];
                    if (c.From == nodes[i] && c.To == target.nodes[j])
                    {
                        connections.Out.RemoveAt(k);
                        break;
                    }
                }

                if (twoSided)
                {
                    for (int k = connections.In.Count - 1; k >= 0; k--)
                    {
                        Connection c = connections.In[k];
                        if (c.From == nodes[i] && c.To == target.nodes[j])
                        {
                            connections.In.RemoveAt(k);
                            break;
                        }
                    }
                }
            }
        }
    }
    public void Discconect(Node target, bool twoSided = false)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Disconnect(target, twoSided);
            for (int j = 0; j < connections.Out.Count; j++)
            {
                Connection c = connections.Out[j];
                if (c.From == nodes[i] && c.To == target)
                {
                    connections.Out.RemoveAt(j);
                    break;
                }
            }

            if (twoSided)
            {
                for (int k = connections.In.Count - 1; k >= 0; k--)
                {
                    Connection c = connections.In[k];
                    if (c.From == target && c.To == nodes[i])
                    {
                        connections.In.RemoveAt(k);
                        break;
                    }
                }
            }

        }
    }

    public void Clear()
    {
        for (int i = 0; i < nodes.Count; i++)
            nodes[i].Clear();
    }

    public abstract List<Connection> Input(Layer group, string method, double weight);
    public abstract List<Connection> Input(NodeGroup group, string method, double weight);
    
}


public class DenseLayer : Layer
{
    public DenseLayer(int size)
    {

        NodeGroup block = new NodeGroup(size);
        nodes.Add(block);
        output = block;
    }

    public override List<Connection> Input(Layer group, string method, double weight) { return group.output.Connect(output, method, weight); }

    public override List<Connection> Input(NodeGroup group, string method, double weight) { return group.Connect(group, method, weight); }
}

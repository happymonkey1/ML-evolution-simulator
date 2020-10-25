using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeGroup : Node
{

    new public GroupConnections connections;
    public List<Node> nodes;
    public NodeGroup(int size)
    {
        connections = new GroupConnections();
        nodes = new List<Node>();

        for (int i = 0; i < size; i++)
            nodes.Add(new Node());
    }

    public List<double> Activate(double[] valueList = null)
    {
        List<double> values = new List<double>();

        if (valueList != null && valueList.Length != nodes.Count)
            throw new System.Exception("Input values array should be same size as internal list");

        for (int i = 0; i < nodes.Count; i++)
        {
            double activation;
            if (valueList == null)
                activation = nodes[i].Activate();
            else
                activation = nodes[i].Activate(valueList[i]);

            values.Add(activation);
        }

        return values;
    }

    public void Propogate(double? rate = null, double? momentum = null, double[] targets = null)
    {
        List<Connection> connections = new List<Connection>();

        if (targets != null && targets.Length != nodes.Count)
            throw new System.Exception("Input values array should be same size as internal list");

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (targets == null)
            {
                Debug.LogWarning("TAKE A LOOK AT TARGET INPUT VALUE0");
                nodes[i].Propogate(true, 0.0, rate, momentum);
            }
            else
                nodes[i].Propogate(true, targets[i], rate, momentum);
        }
    }

    public List<Connection> Connect(NodeGroup target, string method, double weight)
    {
        List<Connection> connections = new List<Connection>();
        if (method == CONNECTION_TYPE.ALL_TO_ALL || method == CONNECTION_TYPE.ALL_TO_ELSE)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < target.nodes.Count; j++)
                {
                    if (method == CONNECTION_TYPE.ALL_TO_ALL && nodes[i] == target.nodes[j]) continue;
                    List<Connection> c = nodes[i].Connect(target.nodes[j], weight);
                    this.connections.Out.Add(c[0]);
                    target.connections.In.Add(c[0]);
                    connections.Add(c[0]);
                }
            }
        }
        else if (method == CONNECTION_TYPE.ONE_TO_ONE)
        {
            if (nodes.Count != target.nodes.Count)
                throw new System.Exception("in and out groups should be the same size");

            for (int i = 0; i < nodes.Count; i++)
            {
                List<Connection> c = nodes[i].Connect(target.nodes[i], weight);
                this.connections.Self.Add(c[0]);
                connections.Add(c[0]);
            }
        }
        else
            throw new System.Exception("No method specified");

        return connections;
    }

    public List<Connection> Connect(Layer target, string method, double weight)
    {
        return target.Input(this, method, weight);
    }

    public List<Connection> Connect(Node target, string method, double weight)
    {
        List<Connection> connections = new List<Connection>();
        for (int i = 0; i < nodes.Count; i++)
        {
            List<Connection> c = nodes[i].Connect(target, weight);
            this.connections.Out.Add(c[0]);
            connections.Add(c[0]);
        }

        return connections;
    }

    public void Gate(List<Connection> connections, string method)
    {
        List<Node> nodes1 = new List<Node>();
        List<Node> nodes2 = new List<Node>();

        for (int i = 0; i < connections.Count; i++)
        {
            Connection c = connections[i];
            if (!nodes1.Contains(c.From))
                nodes1.Add(c.From);
            if (!nodes2.Contains(c.To))
                nodes2.Add(c.To);
        }

        switch (method)
        {
            case GATING_TYPE.INPUT:
                for (int i = 0; i < nodes2.Count; i++)
                {
                    Node n = nodes2[i];
                    Node gater = nodes[i % nodes.Count];

                    for (int j = 0; j < n.connections.In.Count; j++)
                    {
                        Connection c = n.connections.In[j];
                        if (connections.Contains(c))
                            gater.Gate(c);
                    }
                }
                break;

            case GATING_TYPE.OUTPUT:
                for (int i = 0; i < nodes1.Count; i++)
                {
                    Node n = nodes1[i];
                    Node gater = nodes[i % nodes.Count];

                    for (int j = 0; j < n.connections.Out.Count; j++)
                    {
                        Connection c = n.connections.Out[j];
                        if (connections.Contains(c))
                            gater.Gate(c);
                    }
                }
                break;

            case GATING_TYPE.SELF:
                for (int i = 0; i < nodes1.Count; i++)
                {
                    Node n = nodes1[i];
                    Node gater = nodes[i % nodes.Count];

                    if (connections.Contains(n.connections.self))
                        gater.Gate(n.connections.self);
                }
                break;
        }

    }

    public void Set(Node values)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].bias = values.bias;
            nodes[i].squash = values.squash;
            nodes[i].type = values.type;
        }
    }

    public void Disconnect(NodeGroup target, bool twoSided = false)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = 0; j < target.nodes.Count; j++)
            {
                nodes[i].Disconnect(target.nodes[j], twoSided);

                for (int k = connections.Out.Count - 1; k >= 0; k--)
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

    public void Disconnect(Node target, bool twoSided = false)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Disconnect(target, twoSided);

            for (int j = connections.Out.Count - 1; j >= 0; j--)
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
                for (int j = connections.In.Count - 1; j >= 0; j--)
                {
                    Connection c = connections.In[j];

                    if (c.From == nodes[i] && c.To == target)
                    {
                        connections.In.RemoveAt(j);
                        break;
                    }
                }
            }
        }
    }

    public void Clear()
    {
        for(int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Clear();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class Network 
{

    public int input;
    public int output;

    public List<Node> nodes;
    public List<Connection> connections;
    public List<Connection> gates;
    public List<Connection> selfConnections;

    public bool biased;
    double dropout = 0.0;

    private System.Random _rand;

    double score = 0.0;

    public Network(int i, int o, bool biased = false, bool fullConnect = true, bool isChild = false)
    {
        this.biased = biased;
        input = i;
        output = o;

        _rand = new System.Random();
        nodes = new List<Node>();
        connections = new List<Connection>();
        gates = new List<Connection>();
        selfConnections = new List<Connection>();
        dropout = 0.0;

        if (biased && !isChild)
        {
            nodes.Add(new Node(Node.NodeType.INPUT));
            
        }
        for (int j=0; j < input + output; j++)
        {
            Node.NodeType type = (j < input) ? Node.NodeType.INPUT : Node.NodeType.OUTPUT;
            nodes.Add(new Node(type));
            nodes[j].index = j;
        }

        if (biased && !isChild)
            input += 1;
        

        if (fullConnect)
            FullConnect();

    }

    public void FullConnect()
    {
        //connect input and output directly
        for (int j = 0; j < input; j++)
        {
            for (int k = input; k < output + input; k++)
            {
                // https://stats.stackexchange.com/a/248040/147931
                double weight = UnityEngine.Random.value * input * Math.Sqrt(2 / input);
                Connect(nodes[j], nodes[k], weight);
            }
        }
    }

    public List<double> Activate(List<double> inputs, bool training)
    {
        List<double> output = new List<double>();

        for(int i=0; i < nodes.Count; i++)
        {
            if (nodes[i].type == Node.NodeType.INPUT)
                nodes[i].Activate(inputs[i]);
            else if (nodes[i].type == Node.NodeType.OUTPUT)
            {
                double activation = nodes[i].Activate();
                output.Add(activation);
            }else
            {
                if (training) nodes[i].mask = (_rand.NextDouble() < dropout) ? 0 : 1;
                nodes[i].Activate();
            }
        }

        return output;
    }

    public List<double> NoTraceActivate(List<double> inputs)
    {
        List<double> output = new List<double>();


        for (int i = 0; i < nodes.Count; i++)
        {
            
            if (nodes[i].type == Node.NodeType.INPUT)
                nodes[i].NoTraceActivate(inputs[i]);
            else if (nodes[i].type == Node.NodeType.OUTPUT)
            {
                double activation = nodes[i].NoTraceActivate();
                output.Add(activation);
            }
            else
            {
                nodes[i].NoTraceActivate();
            }
        }

        return output;
    }

    public struct FeedForwardJob : IJob
    {
        public Network brain;
        public List<double> vision;
        public NativeArray<double> result;

        public void Execute()
        {
            result.CopyFrom(brain.FeedForward(vision).ToArray());
        }
    }

    public List<double> FeedForward(List<double> inputs)
    {
        List<double> output = new List<double>();


        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].type == Node.NodeType.INPUT)
                nodes[i].NoTraceActivateForward(inputs[i]);
            else if (nodes[i].type == Node.NodeType.OUTPUT)
            {
                double activation = nodes[i].NoTraceActivateForward();
                output.Add(activation);
            }
            else
            {
                nodes[i].NoTraceActivateForward();
            }
        }

        if (Globals.CLEAR)
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].Clear();

        return output;
    }

    public void Propogate(double rate, double momentum, bool update, List<double> targets = null)
    {
        if (targets != null && targets.Count != output)
            throw new Exception("Output and targets should be the same length");

        var targetIndex = targets.Count;

        for (int i = nodes.Count - 1; i >= nodes.Count - output; i--)
            nodes[i].Propogate(update, targets[--targetIndex], rate, momentum);

        for (int i = nodes.Count - output; i >= input; i--)
            nodes[i].Propogate(true, 0.0, rate, momentum);
    }

    public void Clear()
    {
        for (int i = 0; i < nodes.Count; i++)
            nodes[i].Clear();
    }

    public List<Connection> Connect(Node from, Node to, double? weight = null)
    {
        List<Connection> connections = from.Connect(to, (weight != null) ? weight : null);

        for(int i =0; i < connections.Count; i++)
        {
            Connection c = connections[i];
            if (from != to)
                this.connections.Add(c);
            else
                this.selfConnections.Add(c);
        }

        return connections;
    }

    public void Disconnect(Node from, Node to)
    {
        List<Connection> connections = (from == to) ? this.selfConnections : this.connections;

        for (int i = 0; i < connections.Count; i++)
        {
            Connection c = connections[i];
            if (c.From == from && c.To == to)
                if (c.Gater != null) { 
                    UnGate(c);
                    connections.RemoveAt(i);
                    break;
            }
        }

        from.Disconnect(to, false);
    }

    public void Gate(Node node, Connection conn)
    {
        if (nodes.IndexOf(node) == -1)
            throw new Exception("Node is not in network");
        else if (conn.Gater != null)
        {
            Debug.LogWarning("connection is already gated");
            return;
        }
        node.Gate(conn);
        gates.Add(conn);
    }

    public void UnGate(Connection conn)
    {
        int index = gates.IndexOf(conn);
        if (index == -1)
            throw new Exception("Connection is not gated");

        gates.RemoveAt(index);
        conn.Gater.UnGate(conn);
    }

    public void Remove(Node node)
    {
        int index = nodes.IndexOf(node);
        if (index == -1)
            throw new Exception("Node does not exist in network");

        List<Node> gaters = new List<Node>();

        Disconnect(node, node);

        List<Node> inputs = new List<Node>();
        for (int i= node.connections.In.Count; i >= 0; i++)
        {
            Connection c = node.connections.In[i];
            if (Mutations.SUB_NODE_KEEP_GATES && c.Gater != null && c.Gater != node)
                gaters.Add(c.Gater);
            inputs.Add(c.From);
            Disconnect(c.From, node);
        }

        List<Node> outputs = new List<Node>();
        for (int i = node.connections.Out.Count; i >= 0; i++)
        {
            Connection c = node.connections.Out[i];
            if (Mutations.SUB_NODE_KEEP_GATES && c.Gater != null && c.Gater != node)
                gaters.Add(c.Gater);
            outputs.Add(c.To);
            Disconnect(node, c.To);
        }

        /*
        List<Connection> connections = new List<Connection>();
        for (int i=0; i < inputs.Count; i++)
        {
            Node inputNode = inputs[i];
            for (int j=0; j < outputs.Count; j++)
            {
                Node outputNode = outputs[j];
                if (!inputNode.IsProjectingTo(outputNode))
                {
                    List<Connection> c = Connect(inputNode, outputNode, 1.0);
                    connections.Add(c[0]);
                }
            }
        }

        for (int i=0; i < gaters.Count; i++)
        {
            if (connections.Count == 0) break;

            Node gater = gaters[i];
            int connectionIndex = _rand.Next(connections.Count);

            Gate(gater, connections[connectionIndex]);
            connections.RemoveAt(connectionIndex);
        }

        for (int i = node.connections.Gated.Count - 1; i >= 0; i--)
            UnGate(node.connections.Gated[i]);*/

        Disconnect(node, node);
        nodes.RemoveAt(index);
    }

    public void Mutate(MUTATION_TYPE method)
    {
        switch (method)
        {
            case MUTATION_TYPE.ADD_NODE:
                if (connections.Count == 0) break;
                Connection c = connections[UnityEngine.Random.Range(0, nodes.Count)];
                Node gater = c.Gater;
                double oldWeight = c.weight;
                Disconnect(c.From, c.To);


                //int toIndex = nodes.IndexOf(c.To);
                int toIndex = 0;
                for (int i = 0; i < nodes.Count; i++)
                    if (nodes[i].type != Node.NodeType.INPUT)
                        toIndex = i;
                
                Node node = new Node(Node.NodeType.HIDDEN);

                node.Mutate(MUTATION_TYPE.MOD_ACTIVATION);

                int minBound = Math.Min(toIndex, nodes.Count - output);
                nodes.Insert(minBound, node);

                Connection newConn1 = Connect(c.From, node, oldWeight)[0];
                Connection newConn2 = Connect(node, c.To, oldWeight)[0];

                if (gater != null)
                    Gate(gater, (UnityEngine.Random.value >= 0.5f) ? newConn1 : newConn2);

                
                break;

            case MUTATION_TYPE.SUB_NODE:
                if (nodes.Count == input + output)
                {
                    Debug.LogWarning("No more nodes left to remove");
                    break;
                }

                int index = _rand.Next(nodes.Count - output - input) + input;
                Remove(nodes[index]);
                break;
            case MUTATION_TYPE.MOD_ACTIVATION:
                if (!Globals.ALLOW_OUTPUT_ACTIVATION_MUTATION && input + output == nodes.Count)
                {
                    Debug.LogWarning("No nodes available to mutate activation");
                    break;
                }

                int mutateIndex = 0;
                if (Globals.ALLOW_OUTPUT_ACTIVATION_MUTATION)
                    mutateIndex = UnityEngine.Random.Range(input, nodes.Count);
                else
                    mutateIndex = _rand.Next(nodes.Count - output - input) + input;
                nodes[mutateIndex].Mutate(MUTATION_TYPE.MOD_ACTIVATION);

                break;
            case MUTATION_TYPE.ADD_CONN:
                List<(Node, Node)> available = new List<(Node, Node)>();

                Debug.Log("THIS BITCH MUTATED A CONNECTION");

                
                Node rand1 = nodes[UnityEngine.Random.Range(0, nodes.Count)];
                Node rand2 = nodes[UnityEngine.Random.Range(0, nodes.Count)];

                while(!CheckIfNodesCanBeConnected(rand1, rand2))
                {
                    rand1 = nodes[UnityEngine.Random.Range(0, nodes.Count)];
                    rand2 = nodes[UnityEngine.Random.Range(0, nodes.Count)];
                }

                if(rand1.index > rand2.index)
                {
                    Node temp = rand1;
                    rand1 = rand2;
                    rand2 = temp;
                }

                Connect(rand1, rand2);
                break;

                

            case MUTATION_TYPE.SUB_CONN:
                List<Connection> possible = new List<Connection>();
                for(int i=0; i < connections.Count; i++)
                {
                    Connection conn = connections[i];
                    if (conn.From.connections.Out.Count > 0 && conn.To.connections.In.Count > 0 && nodes.IndexOf(conn.To) > nodes.IndexOf(conn.From))
                        possible.Add(conn);
                }

                if (possible.Count == 0)
                {
                    Debug.LogWarning("No connections to remove");
                    break;
                }

                Connection randomConn = possible[_rand.Next(possible.Count)];
                Disconnect(randomConn.From, randomConn.To);
                break;
            case MUTATION_TYPE.MOD_WEIGHT:
                if (connections.Count + selfConnections.Count == 0) break;

                Debug.Log("NEW WEIGHT LETS FUCKIN GOOOOOO");
                List<Connection> allConnections = new List<Connection>();
                allConnections.AddRange(connections);
                allConnections.AddRange(selfConnections);
                Connection c1 = allConnections[_rand.Next(allConnections.Count)];

                if (UnityEngine.Random.value < 0.1f)
                    c1.weight = UnityEngine.Random.Range(-1f, 1f);
                else
                {
                    c1.weight += NativeMethods.RandomGaussian() / 50;
                    if (c1.weight > 1f)
                        c1.weight = 1f;
                    else if (c1.weight < -1f)
                        c1.weight = -1f;
                }
                

                break;
            case MUTATION_TYPE.MOD_BIAS:
                int biasIndex = _rand.Next(nodes.Count - input) + input;
                nodes[biasIndex].Mutate(method);
                break;
            default:
                throw new Exception("MUTATION CASE HAS NOT BEEN ADDED");  
        }
    }

    public bool CheckIfNodesCanBeConnected(Node n1, Node n2)
    {
        if (n1.type == n2.type) return false;
        if (n1.IsProjectingTo(n2)) return false;
        if (n1 == n2) return false;
        return true;
    }

    public (double error, int iterations, TimeSpan elapsed) Train( dynamic set )
    {
        if (set[0].input.Count != input || set[0].output.Count != output)
            throw new Exception("Dataset input/output size should be the same as network input/output size");

        double targetError = 0.05;
        double baseRate = 0.3;
        double dropout = 0.0;
        double momentum = 0.0;
        int batchSize = 1;
        int maxIterations = 100;

        bool crossValidate = false;
        bool clear = false;

        DateTime start = new DateTime().Date;

        if (batchSize > set.Count)
            throw new Exception("Batch size must be smaller or equal to dataset length");

        this.dropout = dropout;

        if(crossValidate)
        {
            throw new Exception("IM A LAZY BASTARD");
        }

        double currentRate = baseRate;
        int iteration = 0;
        double error = 1.0;
        while (error > targetError && iteration < maxIterations)
        {
            iteration++;

            Debug.LogWarning("NEED IMPLEMENT ALL RATE POLICIES");
            currentRate = RATE_FUNCTIONS.FIXED(baseRate, iteration);

            if (crossValidate)
            {
                throw new Exception("IM A LAZY BASTARD");
            }else
            {
                error = TrainSet(set, batchSize, currentRate, momentum);
                if (Globals.CLEAR) Clear();
            }
        }

        if (Globals.CLEAR) Clear();

        if (dropout > 0)
        {
            for (int i=0; i < nodes.Count; i++)
            {
                if (nodes[i].type == Node.NodeType.HIDDEN || nodes[i].type == Node.NodeType.CONSTANT)
                    nodes[i].mask = 1 - this.dropout;
            }
        }

        return (error, iteration, new DateTime().Date - start);
    }

    private double TrainSet(dynamic set, int batchSize, double currentRate, double momentum)
    {
        double errorSum = 0.0;
        for (int i= ((biased) ? 1 : 0); i < set.Count; i++)
        {
            var input = set[i].input;
            var target = set[i].output;

            bool update = !!((i + 1) % batchSize == 0 || (i + 1) == set.Count);

            List<double> output = Activate(input, true);
            Propogate(currentRate, momentum, update, target);

            errorSum += COST_FUNCTIONS.MEAN_SQUARED_ERROR(target, output);
        }

        return errorSum / set.Count;
    }

    public (double error, TimeSpan elapsed) Test(dynamic set)
    {
        if (dropout > 0.0)
            for (int i=0; i < nodes.Count; i++)
                if (nodes[i].type == Node.NodeType.HIDDEN || nodes[i].type == Node.NodeType.CONSTANT)
                    nodes[i].mask = 1 - dropout;

        double error = 0.0;
        DateTime start = new DateTime().Date;

        for (int i= ((biased) ? 1 : 0); i < set.Count; i++)
        {
            var input = set[i].input;
            var target = set[i].output;
            List<double> output = Activate(input, false);
            error += COST_FUNCTIONS.MEAN_SQUARED_ERROR(target, output);
        }

        error /= set.Count;

        return (error, new DateTime().Date - start);
    }

    public void Set(Node values)
    {
        for (int i=0; i < nodes.Count; i++)
        {
            nodes[i].bias = values.bias;
            nodes[i].squash = values.squash;
        }
    }

    public async Task<(double error, int iterations, TimeSpan elapsed)> Evolve(dynamic set)
    {
        if (set[0].input.Count != input || set[0].output.Count != output)
            throw new Exception("Dataset input/output size should be the same as network input/output size");

        double targetError = 0.05;
        double growth = 0.0001;
        int amount = 1;

        DateTime start = new DateTime().Date;

        NEAT neat = new NEAT(input, output, FitnessSingleThreaded);

        double error = Double.NegativeInfinity;
        double bestFitness = Double.NegativeInfinity;
        Network bestGenome = null;

        while (error < -targetError && neat.generation < Globals.MAX_ITERATIONS)
        {
            Network fittest = await neat.Evolve();
            double fitness = fittest.score;

            error = fitness + (fittest.nodes.Count - fittest.input - fittest.output + fittest.connections.Count + fittest.gates.Count) * growth;

            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                bestGenome = fittest;
            }
        }

        if (bestGenome != null)
        {
            nodes = bestGenome.nodes;
            connections = bestGenome.connections;
            selfConnections = bestGenome.selfConnections;
            gates = bestGenome.gates;

            if (Globals.CLEAR) Clear();
        }

        return (-error, neat.generation, new DateTime().Date - start);
    }

    private double FitnessSingleThreaded(Network genome, dynamic set, int amount = 1, double growth = 0.0001)
    {
        double score = 0;
        for (int i=0; i < amount; i++)
        {
            score -= genome.Test(set).error;
        }

        score -= (genome.nodes.Count - genome.input - genome.output + genome.connections.Count + genome.gates.Count) * growth;
        score = Double.IsNaN(score) ? Double.NegativeInfinity : score;

        return score / amount;
    }

    public Network CrossOver(Network network1, Network network2, bool equal)
    {
        if (network1.input != network2.input || network1.output != network2.output)
            throw new Exception("Networks are not of the same size");

        Network offspring = new Network(network1.input, network1.output, biased, false, true);
        offspring.connections = new List<Connection>();
        offspring.nodes = new List<Node>();

        int max = Mathf.Max(network1.nodes.Count, network2.nodes.Count);
        int min = Mathf.Min(network1.nodes.Count, network2.nodes.Count);
        int size = Mathf.FloorToInt(UnityEngine.Random.value * (max - min + 1) + min);
        

        int outputSize = network1.output;

        for (int i = 0; i < network1.nodes.Count; i++)
            network1.nodes[i].index = i;

        for (int i = 0; i < network2.nodes.Count; i++)
            network2.nodes[i].index = i;

        for(int i=0; i < size; i++)
        {
            Node node;
            if (i < size - outputSize)
            {
                float rand = UnityEngine.Random.value;
                node = (rand >= 0.5) ? network1.nodes[i] : network2.nodes[i];
                Node other = (rand < 0.5) ? network1.nodes[i] : network2.nodes[i];

                if (node == null || node.type == Node.NodeType.OUTPUT)
                    node = other;
            }
            else
            {
                if (UnityEngine.Random.value >= 0.5)
                    node = network1.nodes[network1.nodes.Count + i - size];
                else
                    node = network2.nodes[network2.nodes.Count + i - size];
            }

            Node newNode = new Node();
            newNode.bias = node.bias;
            newNode.squash = node.squash;
            newNode.type = node.type;

            offspring.nodes.Add(newNode);
        }

        Dictionary<int, ConnectionData> newConnections1 = new Dictionary<int, ConnectionData>();
        Dictionary<int, ConnectionData> newConnections2 = new Dictionary<int, ConnectionData>();

        for (int i=0; i < network1.connections.Count; i++)
        {
            Connection c = network1.connections[i];
            ConnectionData data = new ConnectionData(c.weight, c.From.index, c.To.index, (c.Gater != null) ? c.Gater.index : -1);
            newConnections1[Connection.InnovationID(data.from, data.to)] = data;
        }

        /*for (int i = 0; i < network1.selfConnections.Count; i++)
        {
            Connection c = network1.selfConnections[i];
            ConnectionData data = new ConnectionData(c.weight, c.From.index, c.To.index, (c.Gater != null) ? c.Gater.index : -1);
            newConnections1[Connection.InnovationID(data.from, data.to)] = data;
        }*/

        for (int i = 0; i < network2.connections.Count; i++)
        {
            Connection c = network2.connections[i];
            ConnectionData data = new ConnectionData(c.weight, c.From.index, c.To.index, (c.Gater != null) ? c.Gater.index : -1);
            newConnections2[Connection.InnovationID(data.from, data.to)] = data;
        }

        /*for (int i = 0; i < network2.selfConnections.Count; i++)
        {
            Connection c = network2.selfConnections[i];
            ConnectionData data = new ConnectionData(c.weight, c.From.index, c.To.index, (c.Gater != null) ? c.Gater.index : -1);
            newConnections2[Connection.InnovationID(data.from, data.to)] = data;
        }*/



        if (this.connections.Count > 0)
        {
            List<ConnectionData> connections = new List<ConnectionData>();
            List<int> keys1 = new List<int>(newConnections1.Keys);
            List<int> keys2 = new List<int>(newConnections1.Keys);

            for (int i = keys1.Count - 1; i >= 0; i--)
            {
                ConnectionData conn;
                if (newConnections2.TryGetValue(keys1[i], out conn))
                {
                    conn = (UnityEngine.Random.value >= 0.5) ? newConnections1[keys1[i]] : newConnections2[keys1[i]];
                    connections.Add(conn);

                    newConnections2.Remove(keys1[i]);
                }
                else
                    connections.Add(newConnections1[keys1[i]]);
                
            }


            for (int i = 0; i < keys2.Count; i++)
            {
                ConnectionData conn;
                if (newConnections2.TryGetValue(keys2[i], out conn))
                    connections.Add(newConnections2[keys2[i]]);
            }


            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionData cData = connections[i];
                if (cData.to < size && cData.from < size)
                {
                    Node from = offspring.nodes[cData.from];
                    Node to = offspring.nodes[cData.to];
                    Connection conn = offspring.Connect(from, to, cData.weight)[0];

                    conn.weight = cData.weight;

                    if (cData.gater != -1 && cData.gater < size)
                    {
                        offspring.Gate(offspring.nodes[cData.gater], conn);
                    }
                }
            }
        }

        if (UnityEngine.Random.value <= Globals.MUTATION_CHANCE)
        {
            float rand = UnityEngine.Random.value;
            if (rand < .8f)
                offspring.Mutate(MUTATION_TYPE.MOD_WEIGHT);

            rand = UnityEngine.Random.value;
            if (rand < .05f)
                offspring.Mutate(MUTATION_TYPE.ADD_CONN);

            rand = UnityEngine.Random.value;
            if (rand < .01f)
                offspring.Mutate(MUTATION_TYPE.ADD_NODE);

            rand = UnityEngine.Random.value;
            if (rand < .05f)
                offspring.Mutate(MUTATION_TYPE.SUB_CONN);

            rand = UnityEngine.Random.value;
            if (rand < .05f)
                offspring.Mutate(MUTATION_TYPE.MOD_BIAS);
        }

        return offspring;

    }
}

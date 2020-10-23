using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Genome
{

    private List<Node> inputNodes;
    private List<Node> hiddenNodes;
    private List<Node> outputNodes;

    public List<List<Node>> nodeNodes = new List<List<Node>>();
    public List<Connection> connectionNodes = new List<Connection>();


    public Genome()
    {
        inputNodes = new List<Node>();
        hiddenNodes = new List<Node>();
        outputNodes = new List<Node>();

        nodeNodes = new List<List<Node>>() { inputNodes, hiddenNodes, outputNodes };

        inputNodes.Add(new Node(Node.NodeType.INPUT, 0));
        hiddenNodes.Add(new Node(Node.NodeType.INPUT, 1));
        outputNodes.Add(new Node(Node.NodeType.INPUT, 2));
    }

    public void SetActivationIndexes()
    {
        for (int i=0; i < inputNodes.Count; i++)
            inputNodes[i].activationIndex = i;
    }

    public void AddNode(Node g)
    {
        switch (g.type) { 
            case Node.NodeType.INPUT:
                //IF PERFORMANCE IS AN ISSUE, CONSIDER USING STACK OR LINKEDLIST
                inputNodes.Add(g);
                break;
            case Node.NodeType.HIDDEN:
                hiddenNodes.Add(g);
                break;
            case Node.NodeType.OUTPUT:
                outputNodes.Add(g);
                break;
        }
    }

    public void Activate()
    {

    }
}

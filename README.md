# ML-evolution-simulator

This simulation is an attempt to have somewhat complex behaviour emerge without explicit programming (through the use of neural networks).

## Environment Details

The simulation contains agents and food sources. Each agent starts with an empty brain (neural net) and randomized weights.
Currently trying to implement a closed energy system where no energy is created or destroyed after the start of the simulation (WIP).

## Agent Details

Agents need to eat in order to survive, and will expend energy eventually exhausting their internal supply and dieing. 
It is possible for agents to "sexually" reproduce, where two agents create a new agent(s) with a hybrid brain and slightly tuned weights. 
Each brain (neural net) takes sensory information like position of food, other agents, age, etc as inputs, and the output is movement.

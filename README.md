# ML-evolution-simulator

This simulation is an attempt to have somewhat complex behaviour emerge without explicit programming (through the use of neural networks).

The simulation contains agents and food sources. Agents start with an empty brain (neural net) and randomized weights.
Upon reproduction, two agents create a new agent(s) with a hybrid brain and slightly tuned weights. 

Each brain (neural net) takes sensory information like position of food, other agents, age, etc as inputs, and the output is movement.

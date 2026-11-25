# GraphView

A reusable node-and-edge graph used by the automation flow and content map. It lays out nodes, draws labelled edges and supports selecting nodes.

## Integration
GraphView renders GraphCanvasModel data supplied by its props; GraphModels.cs contains those shared graph models. It does not own project state, so each workspace maps its own data into the graph.

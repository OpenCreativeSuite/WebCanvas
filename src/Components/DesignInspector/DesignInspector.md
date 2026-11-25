# DesignInspector

The right-side inspector for Design and block editing. It edits page settings, selected-node content, attributes, layout, typography, manual CSS and child-node structure.

## Integration
It uses CurrentPage, CurrentNode, selection and node-edit methods on StudioSession. Changes are undoable and saved through the active page or block document. The app shell mounts it when Design or a block editor is active.


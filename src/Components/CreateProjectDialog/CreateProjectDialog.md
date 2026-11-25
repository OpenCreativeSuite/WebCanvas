# CreateProjectDialog

A modal for creating a new project: name, parent folder, starter/blank choice and template options.

## Integration
It uses StudioSession.CreateName, CreateParent, CreateStarter and the project-creation action. Folder selection goes through **OpenCreativeSuite.Components.Files**. The root app mounts it while CreateOpen is true.

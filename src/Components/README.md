# Components

Each visual component owns one directory:

```
Components/
  ComponentName/
    ComponentName.cx
    ComponentName.css
    ComponentName.md
```

- `.cx` contains the component class (and, where needed, its small props/option types).
- `.css` contains only that component's selectors. Shared primitives live in `Ui/Ui.css`.
- `.md` explains what the component renders and which `StudioSession` state and actions it uses.

Component files share the `WebCanvasDotNet.Components` namespace, so moving a component between directories does not change its call sites. App-wide tokens and layout live in `src/Styles/Tokens.css`, `Shared.css`, `MainLayout.css` and `DialogShared.css`; `Overrides.css` loads last for text scale and global hover fallbacks.

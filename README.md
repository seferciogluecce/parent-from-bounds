# Parent From Bounds

Parent From Bounds creates a parent GameObject at the center or bottom of selected objects' combined bounds. It makes grouping objects easier without manually repositioning the parent or disturbing the objects' world-space transforms.

![Parent From Bounds demo](media/demo.gif)

## Key Features

- Create a parent at the center or bottom of selected GameObjects' combined bounds.
- Group multiple objects while preserving their world-space transforms.
- Calculate bounds using Renderers, with 3D Collider and Transform fallbacks.
- Include inactive objects in bounds calculations.
- Filter nested selections and reject cross-scene selection.
- Undo parent creation and regrouping in Unity.

## Installation

Copy `Editor/ParentFromBounds.cs` into your Unity project and keep it inside an `Editor` folder, for example:

```text
Assets/Editor/ParentFromBounds.cs
```

No dependencies or package setup are required.

## Usage

Select one or more GameObjects in the Hierarchy, then run one of the commands from Unity's GameObject menu:

```text
GameObject > Parent From Bounds > Create Parent at Center
GameObject > Parent From Bounds > Create Parent at Bottom
```

![Parent From Bounds menu](media/menu.png)

### Create Parent at Center

The new parent is placed at the center of the selected objects' combined bounds.

![Create Parent at Center demo](media/center-demo.gif)

### Create Parent at Bottom

The new parent is placed at the bottom-center of the selected objects' combined bounds.

![Create Parent at Bottom demo](media/bottom-demo.gif)

## Behavior

- Multiple selected objects are grouped under one new parent.
- If both a parent and its child are selected, only the top-level selected parent is processed.
- If selected top-level objects have different parents but are in the same scene, the tool creates a new scene-root parent and regroups them.
- If selected top-level objects span multiple scenes, the tool does nothing and logs a warning.
- Inactive selected objects and inactive child objects are included in bounds calculations.
- Renderer bounds are used first.
- 3D Collider bounds are used when a selected root or subtree has no eligible Renderer bounds.
- Transform position is used when no eligible Renderer or 3D Collider bounds exist.
- Selected objects keep their world-space transforms when reparented.
- Each operation is registered with Unity Undo.

## Limitations

- Collider2D is not supported.
- Disabled Renderers and disabled Colliders are ignored.
- ParticleSystemRenderer bounds are ignored.
- Cross-scene selection is not supported.

## Compatibility

Tested with Unity `6000.3.15f1`.

## 7 Tools in 7 Days — Day 1

This tool is part of the **7 Tools in 7 Days** series.

[Read the Day 1 development story on Substack](https://ecesefercioglu.substack.com/p/7-tools-in-7-days-day-1-parent-from)

| Day | Tool |
| --- | --- |
| **Day 1** | **[Parent From Bounds](https://github.com/seferciogluecce/parent-from-bounds)** |
| Day 2 | [Separate Mesh Bodies](https://github.com/seferciogluecce/separate-mesh-bodies) |
| Day 3 | [Brick Wall Generator](https://github.com/seferciogluecce/brick-wall-generator) |
| Day 4 | [Particle System Context Previewer](https://github.com/seferciogluecce/particle-system-context-previewer) |
| Day 5 | [Object Layout Tool](https://github.com/seferciogluecce/object-layout-tool) |
| Day 6 | [DOTween Selection Animation Previewer](https://github.com/seferciogluecce/dotween-selection-animation-previewer) |
| Day 7 | [Wave Object Distributor](https://github.com/seferciogluecce/wave-object-distributor) |

## License

This project is available under the MIT License. See [LICENSE](LICENSE).
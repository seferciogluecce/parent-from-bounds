using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class ParentFromBounds
{
    private const string MenuRoot = "GameObject/Parent From Bounds/";
    private const string CenterMenuPath = MenuRoot + "Create Parent at Center";
    private const string BottomMenuPath = MenuRoot + "Create Parent at Bottom";
    private const string CenterUndoName = "Create Parent at Center";
    private const string BottomUndoName = "Create Parent at Bottom";
    private const string CrossSceneSelectionWarning =
        "Parent From Bounds cannot create a parent from a selection that spans multiple scenes. " +
        "Select objects from one scene only.";

    private static bool invocationHandled;

    private enum BoundsAnchor
    {
        Center,
        Bottom
    }

    [MenuItem(CenterMenuPath, false, 1)]
    private static void CreateParentAtCenterMenu()
    {
        if (!BeginMenuInvocation())
            return;

        CreateParentAtCenter();
    }

    [MenuItem(BottomMenuPath, false, 2)]
    private static void CreateParentAtBottomMenu()
    {
        if (!BeginMenuInvocation())
            return;

        CreateParentAtBottom();
    }

    [MenuItem(CenterMenuPath, true)]
    [MenuItem(BottomMenuPath, true)]
    private static bool ValidateCreateParent()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && GetTopLevelSelection().Length > 0;
    }

    internal static GameObject CreateParentAtCenter()
    {
        return CreateParent(BoundsAnchor.Center, CenterUndoName, " Parent");
    }

    internal static GameObject CreateParentAtBottom()
    {
        return CreateParent(BoundsAnchor.Bottom, BottomUndoName, " Bottom Parent");
    }

    private static bool BeginMenuInvocation()
    {
        // Unity can invoke GameObject context-menu commands once for every selected
        // hierarchy item. Handle only the first invocation from the current event.
        if (invocationHandled)
            return false;

        invocationHandled = true;
        EditorApplication.delayCall += () => invocationHandled = false;
        return true;
    }

    private static GameObject CreateParent(BoundsAnchor anchor, string undoName, string parentNameSuffix)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return null;

        Transform[] selected = GetTopLevelSelection();
        if (selected.Length == 0)
            return null;

        if (!TryGetSingleScene(selected, out Scene targetScene))
        {
            Debug.LogWarning(CrossSceneSelectionWarning);
            return null;
        }

        Transform commonParent = GetCommonParent(selected);
        if (commonParent != null)
            SortBySiblingIndex(selected);

        Vector3 parentPosition = GetParentPosition(selected, anchor);
        int firstSiblingIndex = GetFirstSiblingIndex(selected, commonParent);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(undoName);

        string parentName = GetParentName(selected, parentNameSuffix);
        GameObject parentObject = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(parentObject, undoName);

        Transform parent = parentObject.transform;
        if (targetScene.IsValid() && parentObject.scene != targetScene)
            SceneManager.MoveGameObjectToScene(parentObject, targetScene);

        if (commonParent != null)
        {
            Undo.SetTransformParent(parent, commonParent, undoName);
            parent.localRotation = Quaternion.identity;
            parent.localScale = Vector3.one;
            parent.SetSiblingIndex(firstSiblingIndex);
        }

        parent.position = parentPosition;
        parentObject.name = GameObjectUtility.GetUniqueNameForSibling(commonParent, parentObject.name);

        foreach (Transform selectedRoot in selected)
            Undo.SetTransformParent(selectedRoot, parent, undoName);

        Selection.activeGameObject = parentObject;
        EditorGUIUtility.PingObject(parentObject);
        Undo.CollapseUndoOperations(undoGroup);
        return parentObject;
    }

    private static Vector3 GetParentPosition(Transform[] selected, BoundsAnchor anchor)
    {
        Bounds bounds = GetCombinedSelectionBounds(selected);
        Vector3 position = bounds.center;
        if (anchor == BoundsAnchor.Bottom)
            position.y = bounds.min.y;

        return position;
    }

    private static Bounds GetCombinedSelectionBounds(Transform[] selected)
    {
        Bounds combined = default;
        bool found = false;

        foreach (Transform root in selected)
        {
            Bounds rootBounds;
            if (!TryGetRootBounds(root, out rootBounds))
                rootBounds = new Bounds(root.position, Vector3.zero);

            Encapsulate(ref combined, rootBounds, ref found);
        }

        return combined;
    }

    private static bool TryGetRootBounds(Transform root, out Bounds bounds)
    {
        if (TryGetRendererBounds(root, out bounds))
            return true;

        if (TryGetColliderBounds(root, out bounds))
            return true;

        bounds = default;
        return false;
    }

    private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer || !renderer.enabled)
                continue;

            Encapsulate(ref bounds, renderer.bounds, ref found);
        }

        return found;
    }

    private static bool TryGetColliderBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (!collider.enabled)
                continue;

            Encapsulate(ref bounds, collider.bounds, ref found);
        }

        return found;
    }

    private static void Encapsulate(ref Bounds combined, Bounds next, ref bool found)
    {
        if (!found)
        {
            combined = next;
            found = true;
            return;
        }

        combined.Encapsulate(next);
    }

    private static Transform[] GetTopLevelSelection()
    {
        Transform[] selection = Selection.transforms;
        var selectedSet = new HashSet<Transform>(selection);
        var result = new List<Transform>(selection.Length);

        foreach (Transform transform in selection)
        {
            if (transform == null)
                continue;

            bool hasSelectedAncestor = false;
            for (Transform ancestor = transform.parent; ancestor != null; ancestor = ancestor.parent)
            {
                if (!selectedSet.Contains(ancestor))
                    continue;

                hasSelectedAncestor = true;
                break;
            }

            if (!hasSelectedAncestor)
                result.Add(transform);
        }

        return result.ToArray();
    }

    private static bool TryGetSingleScene(Transform[] transforms, out Scene scene)
    {
        scene = transforms[0].gameObject.scene;
        for (int i = 1; i < transforms.Length; i++)
        {
            if (transforms[i].gameObject.scene != scene)
                return false;
        }

        return true;
    }

    private static Transform GetCommonParent(Transform[] transforms)
    {
        Transform parent = transforms[0].parent;
        for (int i = 1; i < transforms.Length; i++)
        {
            if (transforms[i].parent != parent)
                return null;
        }

        return parent;
    }

    private static void SortBySiblingIndex(Transform[] transforms)
    {
        System.Array.Sort(transforms, (left, right) => left.GetSiblingIndex().CompareTo(right.GetSiblingIndex()));
    }

    private static int GetFirstSiblingIndex(Transform[] transforms, Transform commonParent)
    {
        if (commonParent == null)
            return 0;

        int firstSiblingIndex = int.MaxValue;
        foreach (Transform transform in transforms)
            firstSiblingIndex = Mathf.Min(firstSiblingIndex, transform.GetSiblingIndex());

        return firstSiblingIndex;
    }

    private static string GetParentName(Transform[] transforms, string suffix)
    {
        string sourceName = transforms != null && transforms.Length > 0 && transforms[0] != null
            ? transforms[0].name
            : "GameObject";

        return sourceName + suffix;
    }
}
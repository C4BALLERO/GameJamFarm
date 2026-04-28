#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Generates minimal AnimatorControllers parameters compatible with gameplay scripts:
/// Player (Idle/Walk/Attack/Death), FarmAnimal (Idle/Walk + Death), Enemy (Walk + Death).
/// Run once via menu, then assign controllers on Animator components next to SpriteRenderer setups.
/// </summary>
public static class Animator2DControllersGenerator
{
    private const string Folder = "Assets/animations_05";

    [MenuItem("Tools/Dark Farm/Generate 2D Animator Controllers")]
    public static void GenerateAll()
    {
        EnsureFolder(Folder);

        BuildPlayer($"{Folder}/Player2D.controller");
        BuildFarmAnimal($"{Folder}/FarmAnimal2D.controller");
        BuildEnemy($"{Folder}/Enemy2D.controller");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Animator2DControllersGenerator] Done. Assign controllers under Assets/animations_05 to Animator components.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var cur = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    private static void ReplaceAsset(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);
    }

    private static void BuildPlayer(string assetPath)
    {
        ReplaceAsset(assetPath);

        var c = AnimatorController.CreateAnimatorControllerAtPath(assetPath);

        ClearParameters(c);
        c.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        c.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        c.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        var sm = c.layers[0].stateMachine;

        var idle = sm.AddState("Idle");
        var walk = sm.AddState("Walk");
        var attack = sm.AddState("Attack");
        var death = sm.AddState("Death");

        sm.defaultState = idle;

        AddBoolEdge(idle, walk, "IsMoving", true);
        AddBoolEdge(walk, idle, "IsMoving", false);

        AddTriggerEdge(idle, attack, "Attack");
        AddTriggerEdge(walk, attack, "Attack");

        var attackToIdle = attack.AddTransition(idle);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.95f;

        var anyDeath = sm.AddAnyStateTransition(death);
        anyDeath.hasExitTime = false;
        anyDeath.duration = 0f;
        anyDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");

        EditorUtility.SetDirty(c);
    }

    private static void BuildFarmAnimal(string assetPath)
    {
        ReplaceAsset(assetPath);

        var c = AnimatorController.CreateAnimatorControllerAtPath(assetPath);

        ClearParameters(c);
        c.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        c.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        var sm = c.layers[0].stateMachine;

        var idle = sm.AddState("Idle");
        var walk = sm.AddState("Walk");
        var death = sm.AddState("Death");

        sm.defaultState = idle;

        AddBoolEdge(idle, walk, "IsMoving", true);
        AddBoolEdge(walk, idle, "IsMoving", false);

        var anyDeath = sm.AddAnyStateTransition(death);
        anyDeath.hasExitTime = false;
        anyDeath.duration = 0f;
        anyDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");

        EditorUtility.SetDirty(c);
    }

    private static void BuildEnemy(string assetPath)
    {
        ReplaceAsset(assetPath);

        var c = AnimatorController.CreateAnimatorControllerAtPath(assetPath);

        ClearParameters(c);
        c.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        c.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        c.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        c.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        c.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        var sm = c.layers[0].stateMachine;

        var idle = sm.AddState("Idle");
        var walk = sm.AddState("Walk");
        var attack = sm.AddState("Attack");
        var death = sm.AddState("Death");

        sm.defaultState = idle;

        AddBoolEdge(idle, walk, "IsMoving", true);
        AddBoolEdge(walk, idle, "IsMoving", false);

        AddTriggerEdge(idle, attack, "Attack");
        AddTriggerEdge(walk, attack, "Attack");

        var attackToIdle = attack.AddTransition(idle);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.85f;

        var anyDeath = sm.AddAnyStateTransition(death);
        anyDeath.hasExitTime = false;
        anyDeath.duration = 0f;
        anyDeath.AddCondition(AnimatorConditionMode.If, 0, "Death");

        EditorUtility.SetDirty(c);
    }

    private static void ClearParameters(AnimatorController c)
    {
        while (c.parameters.Length > 0)
            c.RemoveParameter(0);
    }

    private static void AddBoolEdge(AnimatorState from, AnimatorState to, string param, bool wantTrue)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(wantTrue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    private static void AddTriggerEdge(AnimatorState from, AnimatorState to, string triggerName)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(AnimatorConditionMode.If, 0, triggerName);
    }
}
#endif

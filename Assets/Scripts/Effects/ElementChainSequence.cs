using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementChainSequence : MonoBehaviour
{
    public static void Run(string sequenceName, IReadOnlyList<Rigidbody2D> targets, float stepDelay, Action<Rigidbody2D> onStep)
    {
        if (targets == null || targets.Count == 0 || onStep == null) return;

        GameObject runnerObject = new GameObject(sequenceName);
        ElementChainSequence runner = runnerObject.AddComponent<ElementChainSequence>();
        runner.StartCoroutine(runner.Play(targets, stepDelay, onStep));
    }

    private IEnumerator Play(IReadOnlyList<Rigidbody2D> targets, float stepDelay, Action<Rigidbody2D> onStep)
    {
        float delay = Mathf.Max(0f, stepDelay);

        for (int i = 0; i < targets.Count; i++)
        {
            Rigidbody2D target = targets[i];
            if (target != null)
            {
                onStep(target);
            }

            if (delay > 0f && i < targets.Count - 1)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        Destroy(gameObject);
    }
}

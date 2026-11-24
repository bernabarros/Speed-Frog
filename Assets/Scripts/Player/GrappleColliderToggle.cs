using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleColliderToggle : MonoBehaviour
{
    private const string EnableTag = "Enable";
    private const string DisableTag = "Disable";

    [SerializeField, Min(0f)] private float _disableDuration = 1f;

    private readonly Dictionary<Collider2D, bool> originalColliderStates = new Dictionary<Collider2D, bool>();
    private Coroutine restoreCoroutine;

    public bool IsEnableTarget(Transform target)
    {
        while (target != null)
        {
            if (target.CompareTag(EnableTag))
            {
                return true;
            }

            target = target.parent;
        }

        return false;
    }

    public void HandleGrappleHit(Collider2D hitCollider)
    {
        if (!IsEnableTarget(hitCollider.transform))
        {
            return;
        }

        foreach (GameObject disableObject in GameObject.FindGameObjectsWithTag(DisableTag))
        {
            foreach (Collider2D collider in disableObject.GetComponentsInChildren<Collider2D>(true))
            {
                if (!originalColliderStates.ContainsKey(collider))
                {
                    originalColliderStates.Add(collider, collider.enabled);
                }

                collider.enabled = false;
            }
        }

        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
        }

        restoreCoroutine = StartCoroutine(RestoreCollidersAfterDelay());
    }

    private IEnumerator RestoreCollidersAfterDelay()
    {
        yield return new WaitForSeconds(_disableDuration);

        RestoreColliders();
        restoreCoroutine = null;
    }

    private void RestoreColliders()
    {
        foreach (KeyValuePair<Collider2D, bool> state in originalColliderStates)
        {
            if (state.Key != null)
            {
                state.Key.enabled = state.Value;
            }
        }

        originalColliderStates.Clear();
    }

    private void OnDisable()
    {
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
            restoreCoroutine = null;
        }

        RestoreColliders();
    }
}

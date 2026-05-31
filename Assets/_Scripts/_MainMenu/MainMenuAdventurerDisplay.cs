using UnityEngine;

/// <summary>
/// Menu-only display for the hooded adventurer: frozen Roll pose, hidden sword, hand aimed at campfire.
/// Works in the Scene view (ExecuteAlways) so you can position objects without entering Play mode.
/// </summary>
[ExecuteAlways]
public class MainMenuAdventurerDisplay : MonoBehaviour
{
    const string SitPoseStateName = "MenuSitPose";

    [SerializeField] Animator animator;
    [SerializeField] Transform fireTarget;
    [SerializeField] Transform rightHandBone;
    [SerializeField] Transform[] swordObjects;
    [SerializeField] [Range(0f, 1f)] float poseNormalizedTime = 0.9f;
    [SerializeField] float handAimSpeed = 8f;
    [SerializeField] bool aimHandAtFire = true;

    static readonly string[] SwordNameHints = { "Sword", "Weapon", "Blade" };
    static readonly string[] RightHandNameHints =
    {
        "RightHand", "Hand_R", "mixamorig:RightHand", "Right Hand", "R_Hand"
    };

    void OnEnable()
    {
        RefreshDisplay();
    }

    void Start()
    {
        RefreshDisplay();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RefreshDisplay();
    }
#endif

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        AimHandAtFire(Time.deltaTime);
    }

    void RefreshDisplay()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        HideSwords();
        ApplyFrozenPose();

        if (rightHandBone == null)
            rightHandBone = FindBoneByNameHints(transform, RightHandNameHints);
    }

    void ApplyFrozenPose()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.speed = 0f;
        animator.Play(SitPoseStateName, 0, poseNormalizedTime);
        animator.Update(0f);
    }

    void AimHandAtFire(float deltaTime)
    {
        if (!aimHandAtFire || rightHandBone == null || fireTarget == null)
            return;

        Vector3 toFire = fireTarget.position - rightHandBone.position;
        if (toFire.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(toFire.normalized, Vector3.up);
        rightHandBone.rotation = Quaternion.Slerp(
            rightHandBone.rotation,
            targetRotation,
            deltaTime * handAimSpeed);
    }

    void HideSwords()
    {
        if (swordObjects != null && swordObjects.Length > 0)
        {
            foreach (Transform sword in swordObjects)
            {
                if (sword != null)
                    sword.gameObject.SetActive(false);
            }
            return;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == null)
                continue;

            string name = child.name;
            foreach (string hint in SwordNameHints)
            {
                if (name.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    static Transform FindBoneByNameHints(Transform root, string[] hints)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == null)
                continue;

            string name = child.name;
            foreach (string hint in hints)
            {
                if (name.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
            }
        }

        return null;
    }

    public void SetFireTarget(Transform target)
    {
        fireTarget = target;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}

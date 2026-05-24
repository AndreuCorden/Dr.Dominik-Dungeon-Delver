using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelHandler : MonoBehaviour
{
    [Header("References")]
    public GridGenerator gridGen;
    public GameObject playerPrefab;

    [Header("Level Settings")]
    public bool shouldFloorFall = true;
    public int levelIndex = 0;

    [Header("Animation Tweaks")]
    public float fallDistance = 8f;
    public float animationSpeed = 0.6f;
    public float staggerWaveValue = 0.04f;

    private GameObject activePlayer;
    private GameObject dummyVisualContainer;
    private Camera mainCam;
    private CameraFollow camFollowScript;
    private bool isLevelIntroActive = false;

    IEnumerator Start()
    {
        if (gridGen == null) gridGen = GetComponent<GridGenerator>();
        mainCam = Camera.main;

        if (mainCam != null)
        {
            camFollowScript = mainCam.GetComponent<CameraFollow>();
            if (camFollowScript != null) camFollowScript.enabled = false;
        }

        yield return StartCoroutine(RunLevelSetupSequence());
    }

    private IEnumerator RunLevelSetupSequence()
    {
        // Clear stale enemy registers before generating the new world map layouts
        BaseEnemy.OccupiedTiles.Clear();

        // Phase 1: Generate the full functional world silently
        gridGen.GenerateDesignedLevel(levelIndex);
        SpawnPlayer(gridGen.playerSpawnPos);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateRoom(levelIndex);
        }

        // Calculate what the natural gameplay position *should* be right now
        Vector3 defaultGameplayOffset = new Vector3(0f, 7f, -7f);
        if (camFollowScript != null && camFollowScript.offset != Vector3.zero)
        {
            defaultGameplayOffset = camFollowScript.offset;
        }

        // Snap the camera instantly to its track position
        if (mainCam != null && activePlayer != null)
        {
            mainCam.transform.position = activePlayer.transform.position + defaultGameplayOffset;
        }

        // Phase 2: Create dummy visual clones FIRST while real world components are active
        CreateDummyVisualClone(out List<Transform> dummyFoundations, out List<Transform> dummyPropsAndEnemies);

        // Tell the state switcher we are in the intro phase, then apply states
        isLevelIntroActive = true;
        SetRealWorldState(false);

        // Animate the foundational floors and walls inward
        yield return StartCoroutine(AnimateDummyGroupInward(dummyFoundations, true));

        // Phase 3: Foundations landed! Now slide decorations, traps, and enemies down from above
        yield return StartCoroutine(AnimateDummyGroupInward(dummyPropsAndEnemies, false));

        // Phase 4: THE MAGIC CUT - Trade visual dummies for live elements
        if (dummyVisualContainer != null) Destroy(dummyVisualContainer);

        isLevelIntroActive = false;
        SetRealWorldState(true);

        yield return new WaitForSeconds(0.5f);

        // Phase 5: Instantly hand tracking operations over to the camera tracking script
        if (camFollowScript != null && activePlayer != null)
        {
            camFollowScript.target = activePlayer.transform;
            camFollowScript.offset = defaultGameplayOffset;
            camFollowScript.enabled = true;
        }

        yield return new WaitForEndOfFrame();

        // Phase 6: Release Falling Floor Mechanics
        if (shouldFloorFall)
        {
            if (TryGetComponent<FloorManager>(out FloorManager fm))
            {
                fm.StartFallingLogic();
            }
        }
    }

    public void StartExitTransition(int nextTargetIndex)
    {
        StartCoroutine(TriggerLevelCollapseRoutine(nextTargetIndex));
    }

    private IEnumerator TriggerLevelCollapseRoutine(int nextTargetIndex)
    {
        isLevelIntroActive = false;
        SetRealWorldState(false);

        if (camFollowScript != null) camFollowScript.enabled = false;

        Vector3 centerPoint = activePlayer != null ? activePlayer.transform.position : Vector3.zero;

        // Slide out grid generation assets
        foreach (Transform child in gridGen.transform)
        {
            if (child == null) continue;
            float rippleDelay = Vector3.Distance(centerPoint, child.position) * staggerWaveValue;
            StartCoroutine(SlideDownBlock(child, rippleDelay));
        }

        yield return new WaitForSeconds(animationSpeed + 0.3f);

        // Clean up any remaining generation debris manually before proceeding
        foreach (Transform child in gridGen.transform)
        {
            if (child != null) Destroy(child.gameObject);
        }

        // Set layout variables tracking loop index parameters
        levelIndex = nextTargetIndex;

        // Re-execute initialization steps inside loop bounds
        yield return StartCoroutine(RunLevelSetupSequence());
    }

    private void SetRealWorldState(bool isEnabled)
    {
        if (!isEnabled && isLevelIntroActive)
        {
            DeactivateChildrenRecursive(gridGen.transform, false);
        }
        else
        {
            DeactivateChildrenRecursive(gridGen.transform, true);

            foreach (Transform child in gridGen.transform)
            {
                if (child == null) continue;

                if (child.TryGetComponent<Collider>(out Collider col)) col.enabled = isEnabled;

                foreach (var mono in child.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (mono != this) mono.enabled = isEnabled;
                }
                foreach (var c in child.GetComponentsInChildren<Collider>())
                {
                    c.enabled = isEnabled;
                }
            }
        }

        if (activePlayer != null)
        {
            if (activePlayer.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = !isEnabled;
                if (!isEnabled) rb.linearVelocity = Vector3.zero;
            }
            if (activePlayer.TryGetComponent<PlayerController>(out PlayerController pc))
            {
                pc.enabled = isEnabled;
            }

            foreach (var renderer in activePlayer.GetComponentsInChildren<Renderer>())
            {
                if (!isEnabled && isLevelIntroActive) renderer.enabled = false;
                else renderer.enabled = true;
            }
        }
    }

    private void DeactivateChildrenRecursive(Transform parent, bool state)
    {
        foreach (Transform child in parent)
        {
            if (child != null) child.gameObject.SetActive(state);
        }
    }

    private void CreateDummyVisualClone(out List<Transform> foundations, out List<Transform> propsAndEnemies)
    {
        dummyVisualContainer = new GameObject("Visual_Dummy_Level");
        foundations = new List<Transform>();
        propsAndEnemies = new List<Transform>();

        foreach (Transform child in gridGen.transform)
        {
            // FIX: If a floor block has already dropped below the baseline due to mechanics,
            // or is marked dead, DO NOT clone it into the intro animation setup!
            if (child == null || child.position.y < -1f) continue;

            GameObject dummyPiece = Instantiate(child.gameObject, child.position, child.rotation, dummyVisualContainer.transform);
            dummyPiece.SetActive(false);

            string nameLower = child.name.ToLower();
            bool isFoundation = nameLower.Contains("floor") || nameLower.Contains("wall") || nameLower.Contains("door") || nameLower.Contains("arrowwall");

            if (isFoundation) foundations.Add(dummyPiece.transform);
            else propsAndEnemies.Add(dummyPiece.transform);

            foreach (var comp in dummyPiece.GetComponentsInChildren<Component>())
            {
                if (comp is Transform || comp is MeshFilter || comp is MeshRenderer || comp is SkinnedMeshRenderer) continue;
                Destroy(comp);
            }

            if (isFoundation)
                dummyPiece.transform.position -= new Vector3(0, fallDistance, 0);
            else
                dummyPiece.transform.position += new Vector3(0, fallDistance, 0);

            dummyPiece.transform.localScale = child.localScale;
        }
    }

    private IEnumerator AnimateDummyGroupInward(List<Transform> pieces, bool riseFromBelow)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] == null) continue;

            Vector3 finalTargetPos = pieces[i].position + new Vector3(0, riseFromBelow ? fallDistance : -fallDistance, 0);
            Vector3 targetScale = pieces[i].localScale;

            pieces[i].localScale = Vector3.zero;

            float rippleDelay = Vector3.Distance(gridGen.playerSpawnPos, finalTargetPos) * staggerWaveValue;
            StartCoroutine(SlideUpBlock(pieces[i], finalTargetPos, targetScale, rippleDelay));
        }

        yield return new WaitForSeconds(animationSpeed + 0.2f);
    }

    private IEnumerator SlideUpBlock(Transform block, Vector3 destination, Vector3 finalScale, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (block != null) block.gameObject.SetActive(true);

        float elapsed = 0;
        Vector3 startingPos = (block != null) ? block.position : Vector3.zero;

        while (elapsed < animationSpeed && block != null)
        {
            float t = elapsed / animationSpeed;
            float backOutCurve = 1f + 1.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);

            block.position = Vector3.LerpUnclamped(startingPos, destination, backOutCurve);
            block.localScale = Vector3.LerpUnclamped(Vector3.zero, finalScale, backOutCurve);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (block != null)
        {
            block.position = destination;
            block.localScale = finalScale;
        }
    }

    private IEnumerator SlideDownBlock(Transform block, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (block == null) yield break;

        float elapsed = 0;
        Vector3 startingPos = block.position;
        Vector3 fallDestination = startingPos - new Vector3(0, fallDistance, 0);
        Vector3 initialScale = block.localScale;

        while (elapsed < animationSpeed && block != null)
        {
            float t = elapsed / animationSpeed;
            float smoothDrop = t * t;

            block.position = Vector3.Lerp(startingPos, fallDestination, smoothDrop);
            block.localScale = Vector3.Lerp(initialScale, Vector3.zero, smoothDrop);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (block != null) Destroy(block.gameObject);
    }

    void SpawnPlayer(Vector3 spawnPos)
    {
        if (PlayerController.Instance != null)
        {
            activePlayer = PlayerController.Instance.gameObject;
            PlayerController.Instance.ResetState(spawnPos);
        }
        else
        {
            activePlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            activePlayer.name = "Player";
        }

        if (activePlayer.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = true;
        }
    }

    void Awake() { BaseEnemy.OccupiedTiles.Clear(); }
}
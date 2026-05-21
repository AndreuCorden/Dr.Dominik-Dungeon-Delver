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

    [Header("Cinematic Camera Settings")]
    public Vector3 cinematicCamOffsetModifier = new Vector3(-8f, 10f, -10f); 
    public float cameraPanSpeed = 1.4f;                             

    private GameObject activePlayer;
    private GameObject dummyVisualContainer;
    private Camera mainCam;
    private CameraFollow camFollowScript;

    System.Collections.IEnumerator Start()
    {
        if (gridGen == null) gridGen = GetComponent<GridGenerator>();
        mainCam = Camera.main;
        
        if (mainCam != null)
        {
            camFollowScript = mainCam.GetComponent<CameraFollow>();
            if (camFollowScript != null) camFollowScript.enabled = false; 
        }

        // Phase 1: Generate the full functional world silently
        gridGen.GenerateDesignedLevel(levelIndex);
        SpawnPlayer(gridGen.playerSpawnPos);

        // Calculate what the natural gameplay position *should* be right now
        Vector3 defaultGameplayOffset = new Vector3(0f, 7f, -7f); // Fallback if Inspector is empty
        if (camFollowScript != null && camFollowScript.offset != Vector3.zero)
        {
            defaultGameplayOffset = camFollowScript.offset;
        }
        else if (camFollowScript != null && mainCam != null && activePlayer != null)
        {
            // Calculate a clean default offset based on initial scene setup design rules
            defaultGameplayOffset = mainCam.transform.position - activePlayer.transform.position;
        }

        Vector3 gameplayTargetPos = activePlayer.transform.position + defaultGameplayOffset;

        // Position camera back for the cinematic sweep using the validated gameplay baseline
        if (mainCam != null)
        {
            mainCam.transform.position = gameplayTargetPos + cinematicCamOffsetModifier;
        }

        // Deep-hide all real components so nothing lingers floating in the sky
        SetRealWorldState(false);

        // Phase 2: Create, separate, and animate the foundational floors and walls inward
        CreateDummyVisualClone(out List<Transform> dummyFoundations, out List<Transform> dummyPropsAndEnemies);
        yield return StartCoroutine(AnimateDummyGroupInward(dummyFoundations, true)); 

        // Phase 3: Foundations landed! Now slide decorations, traps, and enemies down from above
        yield return StartCoroutine(AnimateDummyGroupInward(dummyPropsAndEnemies, false)); 

        // Phase 4: THE MAGIC CUT - Trade visual dummies for live elements
        Destroy(dummyVisualContainer);
        SetRealWorldState(true);

        // Phase 5: Smoothly pan camera back down along all three axes (X, Y, Z)
        if (mainCam != null && camFollowScript != null && activePlayer != null)
        {
            Vector3 cinematicStartPos = mainCam.transform.position;

            float elapsed = 0f;
            while (elapsed < cameraPanSpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / cameraPanSpeed;
                float smoothT = t * t * (3f - 2f * t); 

                gameplayTargetPos = activePlayer.transform.position + defaultGameplayOffset;

                mainCam.transform.position = Vector3.Lerp(cinematicStartPos, gameplayTargetPos, smoothT);
                yield return null;
            }

            // FIX: Explicitly hand values over to the target follow script properties
            camFollowScript.target = activePlayer.transform;
            camFollowScript.offset = defaultGameplayOffset;
            camFollowScript.enabled = true; // Turn your script on to manage LateUpdate operations
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

    public void TriggerLevelCollapse()
    {
        Vector3 centerPoint = activePlayer != null ? activePlayer.transform.position : Vector3.zero;
        StartCoroutine(AnimateLevelOutward(centerPoint));
    }

    private void SetRealWorldState(bool isEnabled)
    {
        DeactivateChildrenRecursive(gridGen.transform, isEnabled);

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
                renderer.enabled = isEnabled;
            }
        }
    }

    private void DeactivateChildrenRecursive(Transform parent, bool state)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(state);
        }
    }

    private void CreateDummyVisualClone(out List<Transform> foundations, out List<Transform> propsAndEnemies)
    {
        dummyVisualContainer = new GameObject("Visual_Dummy_Level");
        foundations = new List<Transform>();
        propsAndEnemies = new List<Transform>();

        foreach (Transform child in gridGen.transform)
        {
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
            {
                dummyPiece.transform.position -= new Vector3(0, fallDistance, 0); 
            }
            else
            {
                dummyPiece.transform.position += new Vector3(0, fallDistance, 0); 
            }
            
            dummyPiece.transform.localScale = child.localScale;
        }
    }

    private System.Collections.IEnumerator AnimateDummyGroupInward(List<Transform> pieces, bool riseFromBelow)
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

    private System.Collections.IEnumerator SlideUpBlock(Transform block, Vector3 destination, Vector3 finalScale, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (block != null)
        {
            block.gameObject.SetActive(true);
        }

        float elapsed = 0;
        Vector3 startingPos = block.position;

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

    private System.Collections.IEnumerator AnimateLevelOutward(Vector3 explosionOrigin)
    {
        foreach (Transform child in gridGen.transform)
        {
            float rippleDelay = Vector3.Distance(explosionOrigin, child.position) * staggerWaveValue;
            StartCoroutine(SlideDownBlock(child, rippleDelay));
        }
        yield return null;
    }

    private System.Collections.IEnumerator SlideDownBlock(Transform block, float delay)
    {
        yield return new WaitForSeconds(delay);
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
            rb.isKinematic = false; 
        }
    }

    void Awake() { BaseEnemy.OccupiedTiles.Clear(); }
}
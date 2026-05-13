using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Settings")]
    public float moveSpeed = 5f;
    public float timeBetweenSteps = 1.0f;
    public LayerMask floorLayer;
    public LayerMask blockingLayers;

    [Header("Visual Setup")]
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private Vector3 visualLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 visualLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 visualLocalScale = Vector3.one;
    [SerializeField] private bool disablePrimitiveBody = true;
    [SerializeField] private Material visualOverrideMaterial;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip moveClip;
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private float animationBlendDuration = 0.08f;

    protected Vector3 targetPosition;
    protected bool isMoving = false;
    protected bool isFalling = false;
    protected float nextMoveTime;
    protected Transform player;

    private GameObject spawnedVisual;
    private Animator enemyAnimator;
    private Animation legacyAnimation;
    private PlayableGraph animationGraph;
    private AnimationClip currentClip;
    private bool isPlayingOneShot;
    private float oneShotTimer;
    private float oneShotDuration;

    public static HashSet<Vector3> OccupiedTiles = new HashSet<Vector3>();

    protected Vector3 GetRoundedPos(Vector3 pos)
    {
        // Preserve original Y height, round X and Z for the grid
        return new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        SetupVisuals();
        CacheAnimationComponents();

        transform.position = GetRoundedPos(transform.position);
        targetPosition = transform.position;

        Vector3 currentTile = GetRoundedPos(transform.position);
        if (!OccupiedTiles.Contains(currentTile)) OccupiedTiles.Add(currentTile);

        PlayLoopAnimation(idleClip);
    }

    protected virtual void Update()
    {
        if (isFalling) { HandleFalling(); return; }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f) FinishMovement();
        }
        else
        {
            CheckForVoid();
            if (Time.time >= nextMoveTime) DetermineNextStep();
        }

        UpdateAnimationState();
    }

    protected abstract void DetermineNextStep();

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;

        Vector3 currentTile = GetRoundedPos(transform.position);
        if (!OccupiedTiles.Contains(currentTile)) OccupiedTiles.Add(currentTile);
    }

    protected bool TryMove(Vector3 direction)
    {
        Vector3 potentialDest = GetRoundedPos(transform.position + direction);

        bool hasFloor = Physics.Raycast(potentialDest + Vector3.up, Vector3.down, 2f, floorLayer);
        bool isClaimed = OccupiedTiles.Contains(potentialDest);
        bool isPhysicallyBlocked = Physics.CheckSphere(potentialDest + (Vector3.up * 0.5f), 0.3f, blockingLayers);

        bool isPlayerInWay = false;
        if (player != null)
        {
            Vector3 roundedPlayerPos = GetRoundedPos(player.position);
            if (Mathf.Abs(potentialDest.x - roundedPlayerPos.x) < 0.1f &&
                Mathf.Abs(potentialDest.z - roundedPlayerPos.z) < 0.1f)
            {
                isPlayerInWay = true;
            }
        }

        if (hasFloor && !isClaimed && !isPhysicallyBlocked && !isPlayerInWay)
        {
            OccupiedTiles.Remove(GetRoundedPos(transform.position));
            OccupiedTiles.Add(potentialDest);

            targetPosition = potentialDest;
            isMoving = true;
            transform.forward = direction;
            return true;
        }
        return false;
    }

    protected IEnumerator AttackLunge(Vector3 dir)
    {
        PlayAttackAnimation();

        Vector3 originalPos = transform.position;
        Vector3 lungePos = originalPos + (dir * 0.3f);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 15f;
            transform.position = Vector3.Lerp(originalPos, lungePos, Mathf.PingPong(t, 1));
            yield return null;
        }
        transform.position = originalPos;
    }

    protected void CheckForVoid()
    {
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer)) isFalling = true;
    }

    protected void HandleFalling()
    {
        StopAllAnimationPlayback(false);
        transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);
        transform.Rotate(Vector3.up * Time.deltaTime * 200f);
        if (transform.position.y < -10f) Die();
    }

    public void Die()
    {
        Vector3 gridPos = GetRoundedPos(transform.position);
        OccupiedTiles.Remove(gridPos);
        Destroy(gameObject);
    }

    protected void PlayAttackAnimation()
    {
        PlayOneShotAnimation(attackClip);
    }

    private void SetupVisuals()
    {
        if (visualPrefab == null)
            return;

        spawnedVisual = Instantiate(visualPrefab, transform);
        spawnedVisual.transform.localPosition = visualLocalPosition;
        spawnedVisual.transform.localRotation = Quaternion.Euler(visualLocalEulerAngles);
        spawnedVisual.transform.localScale = visualLocalScale;

        if (visualOverrideMaterial != null)
        {
            Renderer[] renderers = spawnedVisual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                renderer.material = visualOverrideMaterial;
        }

        if (!disablePrimitiveBody)
            return;

        bool hasVisualRenderer = spawnedVisual.GetComponentInChildren<Renderer>(true) != null;
        if (!hasVisualRenderer)
            return;

        MeshRenderer primitiveRenderer = GetComponent<MeshRenderer>();
        if (primitiveRenderer != null)
            primitiveRenderer.enabled = false;

        MeshFilter primitiveFilter = GetComponent<MeshFilter>();
        if (primitiveFilter != null)
            primitiveFilter.sharedMesh = null;
    }

    protected void CacheAnimationComponents()
    {
        enemyAnimator = GetComponentInChildren<Animator>(true);
        legacyAnimation = enemyAnimator == null ? GetComponentInChildren<Animation>(true) : null;
    }

    private void UpdateAnimationState()
    {
        if (spawnedVisual != null && !spawnedVisual.activeInHierarchy)
            return;

        if (isPlayingOneShot)
        {
            oneShotTimer += Time.deltaTime;
            if (oneShotTimer >= oneShotDuration)
            {
                isPlayingOneShot = false;
                oneShotTimer = 0f;
                oneShotDuration = 0f;
                currentClip = null;
                StopAllAnimationPlayback(true);
            }
            return;
        }

        AnimationClip wantedClip = isMoving ? moveClip : idleClip;
        PlayLoopAnimation(wantedClip);
    }

    private void PlayLoopAnimation(AnimationClip clip)
    {
        if (clip == null || currentClip == clip)
            return;

        if (enemyAnimator != null)
        {
            PlayAnimatorClip(clip);
        }
        else if (legacyAnimation != null)
        {
            PlayLegacyClip(clip, true);
        }

        currentClip = clip;
    }

    protected void SetSpawnedVisualActive(bool active)
    {
        if (spawnedVisual == null)
            return;

        spawnedVisual.SetActive(active);
        CacheAnimationComponents();
    }

    private void PlayOneShotAnimation(AnimationClip clip)
    {
        if (clip == null)
            return;

        if (enemyAnimator != null)
        {
            PlayAnimatorClip(clip);
        }
        else if (legacyAnimation != null)
        {
            PlayLegacyClip(clip, false);
        }
        else
        {
            return;
        }

        isPlayingOneShot = true;
        oneShotTimer = 0f;
        oneShotDuration = Mathf.Max(clip.length, 0.01f);
        currentClip = clip;
    }

    private void PlayAnimatorClip(AnimationClip clip)
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();

        animationGraph = PlayableGraph.Create("EnemyAnimationGraph");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "EnemyAnimationOutput", enemyAnimator);
        AnimationClipPlayable playable = AnimationClipPlayable.Create(animationGraph, clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        output.SetSourcePlayable(playable);
        animationGraph.Play();
    }

    private void PlayLegacyClip(AnimationClip clip, bool loop)
    {
        if (legacyAnimation.GetClip(clip.name) == null)
            legacyAnimation.AddClip(clip, clip.name);

        AnimationState state = legacyAnimation[clip.name];
        if (state != null)
            state.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

        legacyAnimation.CrossFade(clip.name, Mathf.Max(animationBlendDuration, 0f));
    }

    private void StopAllAnimationPlayback(bool rebindAnimator)
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();

        if (enemyAnimator != null && rebindAnimator)
        {
            enemyAnimator.Rebind();
            enemyAnimator.Update(Mathf.Max(animationBlendDuration, 0f));
        }

        if (legacyAnimation != null)
            legacyAnimation.Stop();
    }

    protected virtual void OnDestroy()
    {
        StopAllAnimationPlayback(false);
        OccupiedTiles.Remove(GetRoundedPos(transform.position));
        OccupiedTiles.Remove(GetRoundedPos(targetPosition));
    }

    protected virtual void OnDisable()
    {
        StopAllAnimationPlayback(false);
    }
}

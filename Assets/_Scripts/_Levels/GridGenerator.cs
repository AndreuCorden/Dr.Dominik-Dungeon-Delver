using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public List<LevelBlueprint> levels = new List<LevelBlueprint>(); // Design your levels in the Inspector!

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject CoinPrefab;
    public GameObject doorPrefab;
    public GameObject spikeTrapFloorPrefab;
    public GameObject gargoylePrefab;
    public GameObject arrowWallPrefab;
    public GameObject pressurePlatePrefab;
    public GameObject mimicPrefab;
    public GameObject bossEnemyPrefab;

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public GameObject trailEnemyPrefab;
    public GameObject patrollerEnemyPrefab;

    [Header("Decor Settings")]
    public GameObject[] decorPrefabs;

    [HideInInspector] public Vector3 doorPosition;
    [HideInInspector] public Vector3 playerSpawnPos = Vector3.up;

    public void GenerateDesignedLevel(int index)
    {
        if (index >= levels.Count) return;

        // --- MISSING FUNCTIONALITY: CLEAN SLATE ---
        BaseEnemy.OccupiedTiles.Clear();

        if (TryGetComponent<FloorManager>(out FloorManager fmComponent))
        {
            float customTime = levels[index].time;
            fmComponent.setTimeBetweenRows(customTime);
        }

        string[] rows = levels[index].layout.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        System.Array.Reverse(rows);

        // Helper for Arrow Trap pairing (used for 'A' and 'L')
        // This allows multiple arrow shooters in one level if needed
        ArrowTrap lastSpawnedShooter = null;

        for (int z = 0; z < rows.Length; z++)
        {
            for (int x = 0; x < rows[z].Length; x++)
            {
                char c = rows[z][x];
                Vector3 pos = new Vector3(x, 0, z);
                GameObject currentFloor = null;

                // 1. Spawn Floor (Restored logic: Wall 'W' and Arrow 'A' provide their own collision)
                if (c != ' ' && c != 'W' && c != 'A' && c != '3')
                {
                    currentFloor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                }

                // 2. Spawn Specific Objects
                switch (c)
                {
                    case 'W':
                        PlaceWall(pos, x, z, rows.Length);
                        break;

                    case 'P':
                        playerSpawnPos = pos + Vector3.up;
                        break;

                    case 'D':
                        Vector3 doorOffset = new Vector3(-0.07f, 0.32f, -0.42f);
                        GameObject doorInstance = Instantiate(doorPrefab, pos + doorOffset, Quaternion.identity, transform);
                        doorInstance.name = "LevelExitDoor";
                        doorPosition = pos;

                        if (currentFloor != null)
                        {
                            // 1. Remove falling logic
                            FallingTile ft = currentFloor.GetComponent<FallingTile>();
                            if (ft != null) Destroy(ft);

                            // 2. Add the Goal logic (It will find the player on its own in Start)
                            currentFloor.AddComponent<LevelGoal>();
                        }
                        break;

                    case 'A': // --- RESTORED: ROTATION ---
                              // Rotate 90 degrees to face Right (as in your old code)
                        GameObject arrowWall = Instantiate(arrowWallPrefab, pos + Vector3.up, Quaternion.Euler(0, 90, 0), transform);
                        lastSpawnedShooter = arrowWall.GetComponent<ArrowTrap>();
                        PlaceWall(pos, x, z, rows.Length);
                        break;

                    case 'L': // --- RESTORED: PARENTING & ASSIGNMENT ---
                        if (currentFloor != null)
                        {
                            GameObject plate = Instantiate(pressurePlatePrefab, new Vector3(pos.x, 0.5f, pos.z), Quaternion.identity, transform);
                            if (lastSpawnedShooter != null)
                                plate.GetComponent<PressurePlate>().wallTrap = lastSpawnedShooter;
                        }
                        break;

                    case 'C': // --- RESTORED: ROTATION & PARENTING ---
                        if (currentFloor != null)
                        {
                            Quaternion coinRot = Quaternion.Euler(0, 0, 0);
                            Instantiate(CoinPrefab, pos + Vector3.up * 0.52f, coinRot, transform);
                        }
                        break;

                    case 'G': // --- RESTORED: LAYER ASSIGNMENT ---
                        if (currentFloor != null)
                        {
                            Instantiate(gargoylePrefab, pos + Vector3.up * 1.25f, Quaternion.identity, transform);
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                        }
                        break;

                    case 'M': // --- RESTORED: PARENTING ---
                        if (currentFloor != null)
                        {
                            Instantiate(mimicPrefab, pos, Quaternion.identity, transform);
                        }
                        break;

                    case 'I': // Spike Trap
                        Instantiate(spikeTrapFloorPrefab, pos, Quaternion.identity, transform);
                        break;

                    case 'F': // Basic Enemy
                        Instantiate(enemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case 'T': // Patroller
                        Instantiate(patrollerEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case 'S': // Trail Enemy
                        Instantiate(trailEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;
                    case 'B': // Boss Enemy
                        {
                            // 1. Spawn the pristine, unedited Boss Enemy prefab structure
                            GameObject bossObj = Instantiate(bossEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);

                            // 2. Scan the armature hierarchy to locate Eye bones AND the Mouth Tip bone
                            Transform leftEyeBone = null;
                            Transform rightEyeBone = null;
                            Transform headTipBone = null;

                            foreach (Transform child in bossObj.GetComponentsInChildren<Transform>(true))
                            {
                                if (child == null) continue;

                                if (child.name.Equals("LeftEye"))
                                {
                                    leftEyeBone = child;
                                }
                                else if (child.name.Equals("RightEye"))
                                {
                                    rightEyeBone = child;
                                }
                                else if (child.name.Equals("Head.downTip_end")) // Updated bone name match!
                                {
                                    headTipBone = child;
                                }

                                // Optimization: Stop scanning once all three target components are secured
                                if (leftEyeBone != null && rightEyeBone != null && headTipBone != null) break;
                            }

                            // 3. Programmatically build and anchor the Left Eye Point Light
                            if (leftEyeBone != null)
                            {
                                GameObject leftLightObj = new GameObject("Healthy_Boss_LeftEye_Light");
                                leftLightObj.transform.SetParent(leftEyeBone);
                                leftLightObj.transform.localPosition = Vector3.zero;
                                leftLightObj.transform.localRotation = Quaternion.identity;

                                Light leftLight = leftLightObj.AddComponent<Light>();
                                leftLight.type = LightType.Point;
                                leftLight.color = Color.red;
                                leftLight.range = 10f;
                                leftLight.intensity = 0.4f;
                                leftLight.shadows = LightShadows.None;
                            }

                            // 4. Programmatically build and anchor the Right Eye Point Light
                            if (rightEyeBone != null)
                            {
                                GameObject rightLightObj = new GameObject("Healthy_Boss_RightEye_Light");
                                rightLightObj.transform.SetParent(rightEyeBone);
                                rightLightObj.transform.localPosition = Vector3.zero;
                                rightLightObj.transform.localRotation = Quaternion.identity;

                                Light rightLight = rightLightObj.AddComponent<Light>();
                                rightLight.type = LightType.Point;
                                rightLight.color = Color.red;
                                rightLight.range = 10f;
                                rightLight.intensity = 0.4f;
                                rightLight.shadows = LightShadows.None;
                            }

                            // =========================================================
                            // 5. GENERATE THE SHOOT POINT AT THE CORRECT MOUTH BONE
                            // =========================================================
                            if (headTipBone != null)
                            {
                                // Create a brand new clean shoot point object container
                                GameObject shootPointObj = new GameObject("Generated_Boss_FireShootPoint");
                                shootPointObj.transform.SetParent(headTipBone);

                                // Pin it exactly to the bone position
                                shootPointObj.transform.localPosition = Vector3.zero;

                                // Align its direction directly with the main Boss body's facing direction
                                shootPointObj.transform.rotation = bossObj.transform.rotation;

                                // Link it directly to the BossEnemy component
                                if (bossObj.TryGetComponent<BossEnemy>(out var bossController))
                                {
                                    bossController.shootPoint = shootPointObj.transform;
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Boss Generator Warning: 'Head.downTip_end' bone wasn't found. Fire shoot point assigned to fallback position.");
                                if (bossObj.TryGetComponent<BossEnemy>(out var bossController))
                                {
                                    bossController.shootPoint = bossObj.transform;
                                }
                            }
                        }
                        break;

                    case '0':
                        {
                            GameObject decor = Instantiate(decorPrefabs[0], pos, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }

                    case '1':
                        {
                            GameObject decor = Instantiate(decorPrefabs[1], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }

                    case '2':
                        {
                            GameObject decor = Instantiate(decorPrefabs[2], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }

                    case '3':
                        // 1. Place the wall first using the helper
                        PlaceWall(pos, x, z, rows.Length);

                        // 2. Determine lantern position/rotation based on which wall it's on
                        Vector3 lanternOffset;
                        Quaternion lanternRotation;

                        if (x == 0 && z != rows.Length - 1) // Side wall (Facing Right)
                        {
                            lanternOffset = new Vector3(0.7f, 0.75f, 0);
                            lanternRotation = Quaternion.Euler(0, -90, 0);
                        }
                        else // Back wall (Facing Forward/Down)
                        {
                            lanternOffset = new Vector3(0, 0.75f, -0.7f);
                            lanternRotation = Quaternion.identity;
                        }

                        // 3. Spawn the small torch
                        GameObject smallTorch = Instantiate(decorPrefabs[3], pos + lanternOffset, lanternRotation, transform);

                        // Nuke the broken URP data loops instantly
                        var data3 = smallTorch.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                        var lights3 = smallTorch.GetComponentsInChildren<Light>(true);
                        foreach (var d in data3) { DestroyImmediate(d); }
                        foreach (var l in lights3) { DestroyImmediate(l); }

                        // Create a pristine light object
                        GameObject smallLightObj = new GameObject("Healthy_SmallTorch_Light");
                        Transform smallCrystalTarget = null;

                        // Search for the crystal component dynamically
                        foreach (Transform child in smallTorch.GetComponentsInChildren<Transform>(true))
                        {
                            if (child.name.Contains("Crystal"))
                            {
                                smallCrystalTarget = child;
                                break;
                            }
                        }

                        // Anchor light directly to the crystal center
                        if (smallCrystalTarget != null)
                        {
                            smallLightObj.transform.SetParent(smallCrystalTarget);
                            smallLightObj.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            smallLightObj.transform.SetParent(smallTorch.transform);
                            smallLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                        }

                        // Configure small torch light settings
                        Light smallLight = smallLightObj.AddComponent<Light>();
                        smallLight.type = LightType.Point;
                        smallLight.color = new Color(1f, 0.58f, 0.16f); // Warm cozy orange
                        smallLight.range = 5f;                          // Medium reach for small torch
                        smallLight.intensity = 1.2f;
                        smallLight.shadows = LightShadows.Soft;

                        break;

                    case '4':
                        {
                            // 1. Spawn the big torch
                            GameObject bigTorch = Instantiate(decorPrefabs[4], pos + Vector3.up * 0.5f, Quaternion.identity, transform);

                            // Mark the grid tile layer for gameplay logic
                            currentFloor.layer = LayerMask.NameToLayer("Trap");

                            // Nuke the broken URP data loops instantly
                            var data4 = bigTorch.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                            var lights4 = bigTorch.GetComponentsInChildren<Light>(true);
                            foreach (var d in data4) { DestroyImmediate(d); }
                            foreach (var l in lights4) { DestroyImmediate(l); }

                            // Create a pristine light object
                            GameObject bigLightObj = new GameObject("Healthy_BigTorch_Light");
                            Transform bigCrystalTarget = null;

                            // Search for the crystal component dynamically
                            foreach (Transform child in bigTorch.GetComponentsInChildren<Transform>(true))
                            {
                                if (child.name.Contains("Crystal"))
                                {
                                    bigCrystalTarget = child;
                                    break;
                                }
                            }

                            // Anchor light directly to the big crystal center
                            if (bigCrystalTarget != null)
                            {
                                bigLightObj.transform.SetParent(bigCrystalTarget);
                                bigLightObj.transform.localPosition = Vector3.zero;
                            }
                            else
                            {
                                bigLightObj.transform.SetParent(bigTorch.transform);
                                bigLightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f); // Higher fallback offset for a larger object
                            }

                            // Configure big torch light settings (Brighter, wider radius)
                            Light bigLight = bigLightObj.AddComponent<Light>();
                            bigLight.type = LightType.Point;
                            bigLight.color = new Color(1f, 0.65f, 0.25f); // Bright, intense amber flare
                            bigLight.range = 8f;                         // Reaches further into the dungeon halls
                            bigLight.intensity = 2.0f;                    // Noticeably brighter than the wall torch
                            bigLight.shadows = LightShadows.Soft;

                            if (bigTorch.GetComponent<FallingTile>() == null)
                            {
                                bigTorch.AddComponent<FallingTile>();
                            }

                            break;
                        }
                    case '5':
                        {
                            GameObject decor = Instantiate(decorPrefabs[5], pos + Vector3.up * 0.5f, Quaternion.identity, transform);

                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");

                            // Nuke any broken legacy URP scripts
                            var data5 = decor.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                            var lights5 = decor.GetComponentsInChildren<Light>(true);
                            foreach (var d in data5) { DestroyImmediate(d); }
                            foreach (var l in lights5) { DestroyImmediate(l); }

                            // ==========================================
                            // FIX SHADOWS & ADD EMISSION GLOW
                            // ==========================================
                            // 1. Tell the crystal mesh NOT to cast shadows from its own internal light
                            MeshRenderer renderer5 = decor.GetComponentInChildren<MeshRenderer>();
                            if (renderer5 != null)
                            {
                                renderer5.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                                // 2. Make the crystal material physically glow in the dark (URP Emission)
                                // This turns on the glowing look so it doesn't look black/unlit from the inside out
                                Color pinkGlowColor = new Color(0.914f, 0.584f, 0.8f);
                                renderer5.material.EnableKeyword("_EMISSION");
                                renderer5.material.SetColor("_EmissionColor", pinkGlowColor * 2.0f); // Multiply by 2 for extra intensity!
                            }

                            // 3. Create the clean child GameObject for the light source
                            GameObject pinkLightObj = new GameObject("Crystal_Pink_Light");
                            pinkLightObj.transform.SetParent(decor.transform);
                            pinkLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                            // 4. Configure light
                            Light pinkLight = pinkLightObj.AddComponent<Light>();
                            pinkLight.type = LightType.Point;
                            pinkLight.color = new Color(0.914f, 0.584f, 0.8f);
                            pinkLight.range = 6f;
                            pinkLight.intensity = 1.8f;
                            pinkLight.shadows = LightShadows.Soft; // It will still cast shadows on surrounding walls/floors!

                            break;
                        }

                    case '6':
                        {
                            GameObject decor = Instantiate(decorPrefabs[6], pos + Vector3.up * 0.5f, Quaternion.identity, transform);

                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");

                            // Nuke any broken legacy URP scripts
                            var data6 = decor.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                            var lights6 = decor.GetComponentsInChildren<Light>(true);
                            foreach (var d in data6) { DestroyImmediate(d); }
                            foreach (var l in lights6) { DestroyImmediate(l); }

                            // ==========================================
                            // FIX SHADOWS & ADD EMISSION GLOW
                            // ==========================================
                            // 1. Tell the crystal mesh NOT to cast shadows from its own internal light
                            MeshRenderer renderer6 = decor.GetComponentInChildren<MeshRenderer>();
                            if (renderer6 != null)
                            {
                                renderer6.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                                // 2. Make the crystal material physically glow in the dark (URP Emission)
                                Color yellowGlowColor = new Color(1f, 1f, 0.137f);
                                renderer6.material.EnableKeyword("_EMISSION");
                                renderer6.material.SetColor("_EmissionColor", yellowGlowColor * 2.0f);
                            }

                            // 3. Create the clean child GameObject for the light source
                            GameObject yellowLightObj = new GameObject("Crystal_Yellow_Light");
                            yellowLightObj.transform.SetParent(decor.transform);
                            yellowLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                            // 4. Configure light
                            Light yellowLight = yellowLightObj.AddComponent<Light>();
                            yellowLight.type = LightType.Point;
                            yellowLight.color = new Color(1f, 1f, 0.137f);
                            yellowLight.range = 6f;
                            yellowLight.intensity = 1.8f;
                            yellowLight.shadows = LightShadows.Soft;

                            break;
                        }
                }
            }
        }
    }

    private void PlaceWall(Vector3 pos, int x, int z, int rowCount)
    {
        if (x == 0 && z != rowCount - 1)
        {
            // Offset: Move slightly Right (X+) and forward (Z+) to close the back gap
            Vector3 wallOffset = new Vector3(0.3f, 0.25f, 0.2f);
            Instantiate(wallPrefab, pos + wallOffset, Quaternion.Euler(0, -90, 0), transform);
        }
        // BACK WALLS (Top of the room)
        else
        {
            // Offset: Move slightly Down (Z-) so it sits ON the floor, not on the line
            Vector3 wallOffset = new Vector3(-0.07f, 0.3f, -0.3f);
            Instantiate(wallPrefab, pos + wallOffset, Quaternion.identity, transform);
        }
    }
}
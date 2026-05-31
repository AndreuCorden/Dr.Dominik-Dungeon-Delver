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

        // --- CLEAN SLATE ---
        BaseEnemy.OccupiedTiles.Clear();

        if (TryGetComponent<FloorManager>(out FloorManager fmComponent))
        {
            float customTime = levels[index].time;
            fmComponent.setTimeBetweenRows(customTime);
        }

        string[] rows = levels[index].layout.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        System.Array.Reverse(rows);

        ArrowTrap lastSpawnedShooter = null;

        for (int z = 0; z < rows.Length; z++)
        {
            for (int x = 0; x < rows[z].Length; x++)
            {
                char c = rows[z][x];
                Vector3 pos = new Vector3(x, 0, z);
                GameObject currentFloor = null;

                // 1. Spawn Floor
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
                            FallingTile ft = currentFloor.GetComponent<FallingTile>();
                            if (ft != null) Destroy(ft);
                            currentFloor.AddComponent<LevelGoal>();
                        }
                        break;

                    case 'A': 
                        GameObject arrowWall = Instantiate(arrowWallPrefab, pos + Vector3.up, Quaternion.Euler(0, 90, 0), transform);
                        lastSpawnedShooter = arrowWall.GetComponent<ArrowTrap>();
                        PlaceWall(pos, x, z, rows.Length);
                        break;

                    case 'L': 
                        if (currentFloor != null)
                        {
                            GameObject plate = Instantiate(pressurePlatePrefab, new Vector3(pos.x, 0.5f, pos.z), Quaternion.identity, transform);
                            if (lastSpawnedShooter != null)
                                plate.GetComponent<PressurePlate>().wallTrap = lastSpawnedShooter;
                        }
                        break;

                    case 'C': 
                        if (currentFloor != null)
                        {
                            Quaternion coinRot = Quaternion.Euler(0, 0, 0);
                            Instantiate(CoinPrefab, pos + Vector3.up * 0.52f, coinRot, transform);
                        }
                        break;

                    case 'G': 
                        if (currentFloor != null)
                        {
                            Instantiate(gargoylePrefab, pos + Vector3.up * 1.25f, Quaternion.identity, transform);
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                        }
                        break;

                    case 'M': 
                        if (currentFloor != null)
                        {
                            Instantiate(mimicPrefab, pos, Quaternion.Euler(0, 180, 0), transform);
                        }
                        break;

                    case 'I': 
                        Instantiate(spikeTrapFloorPrefab, pos, Quaternion.identity, transform);
                        break;

                    case 'F': 
                        Instantiate(enemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case 'T': 
                        Instantiate(patrollerEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case 'S': 
                        Instantiate(trailEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case 'B': // Boss Enemy
                        {
                            GameObject bossObj = Instantiate(bossEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);

                            Transform leftEyeBone = null;
                            Transform rightEyeBone = null;
                            Transform headTipBone = null;

                            foreach (Transform child in bossObj.GetComponentsInChildren<Transform>(true))
                            {
                                if (child == null) continue;

                                if (child.name.Equals("LeftEye")) leftEyeBone = child;
                                else if (child.name.Equals("RightEye")) rightEyeBone = child;
                                else if (child.name.Equals("Head.downTip_end")) headTipBone = child;

                                if (leftEyeBone != null && rightEyeBone != null && headTipBone != null) break;
                            }

                            // --- OPTIMIZED: Left Eye Light ---
                            if (leftEyeBone != null)
                            {
                                GameObject leftLightObj = new GameObject("Healthy_Boss_LeftEye_Light");
                                leftLightObj.transform.SetParent(leftEyeBone);
                                leftLightObj.transform.localPosition = Vector3.zero;
                                leftLightObj.transform.localRotation = Quaternion.identity;

                                Light leftLight = leftLightObj.AddComponent<Light>();
                                leftLight.type = LightType.Point;
                                leftLight.color = Color.red;
                                leftLight.range = 4f;              // PERFORMANCE FIX: Dropped range from 10 to 4
                                leftLight.intensity = 0.6f;
                                leftLight.shadows = LightShadows.None; // PERFORMANCE FIX: Keep shadows off for tiny eyes
                                leftLight.renderMode = LightRenderMode.ForcePixel;
                            }

                            // --- OPTIMIZED: Right Eye Light ---
                            if (rightEyeBone != null)
                            {
                                GameObject rightLightObj = new GameObject("Healthy_Boss_RightEye_Light");
                                rightLightObj.transform.SetParent(rightEyeBone);
                                rightLightObj.transform.localPosition = Vector3.zero;
                                rightLightObj.transform.localRotation = Quaternion.identity;

                                Light rightLight = rightLightObj.AddComponent<Light>();
                                rightLight.type = LightType.Point;
                                rightLight.color = Color.red;
                                rightLight.range = 4f;              // PERFORMANCE FIX: Dropped range from 10 to 4
                                rightLight.intensity = 0.6f;
                                rightLight.shadows = LightShadows.None; // PERFORMANCE FIX: Keep shadows off
                                rightLight.renderMode = LightRenderMode.ForcePixel;
                            }

                            if (headTipBone != null)
                            {
                                GameObject shootPointObj = new GameObject("Generated_Boss_FireShootPoint");
                                shootPointObj.transform.SetParent(headTipBone);
                                shootPointObj.transform.localPosition = Vector3.zero;
                                shootPointObj.transform.rotation = bossObj.transform.rotation;

                                if (bossObj.TryGetComponent<BossEnemy>(out var bossController))
                                {
                                    bossController.shootPoint = shootPointObj.transform;
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Boss Generator Warning: 'Head.downTip_end' bone wasn't found.");
                                if (bossObj.TryGetComponent<BossEnemy>(out var bossController))
                                {
                                    bossController.shootPoint = bossObj.transform;
                                }
                            }
                        }
                        break;

                    case '0':
                    case '1':
                    case '2':
                        {
                            int decorIndex = c - '0';
                            GameObject decor = Instantiate(decorPrefabs[decorIndex], pos + (decorIndex > 0 ? Vector3.up * 0.5f : Vector3.zero), Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null) decor.AddComponent<FallingTile>();
                            if (currentFloor != null) currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }

                    case '3': // Small Torch
                        {
                            PlaceWall(pos, x, z, rows.Length);

                            Vector3 lanternOffset = (x == 0 && z != rows.Length - 1) ? new Vector3(0.7f, 0.75f, 0) : new Vector3(0, 0.75f, -0.7f);
                            Quaternion lanternRotation = (x == 0 && z != rows.Length - 1) ? Quaternion.Euler(0, -90, 0) : Quaternion.identity;

                            GameObject smallTorch = Instantiate(decorPrefabs[3], pos + lanternOffset, lanternRotation, transform);

                            var data3 = smallTorch.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                            var lights3 = smallTorch.GetComponentsInChildren<Light>(true);
                            foreach (var d in data3) { DestroyImmediate(d); }
                            foreach (var l in lights3) { DestroyImmediate(l); }

                            GameObject smallLightObj = new GameObject("Healthy_SmallTorch_Light");
                            Transform smallCrystalTarget = null;

                            foreach (Transform child in smallTorch.GetComponentsInChildren<Transform>(true))
                            {
                                if (child.name.Contains("Crystal")) { smallCrystalTarget = child; break; }
                            }

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

                            // --- OPTIMIZED: Small Torch Configuration ---
                            Light smallLight = smallLightObj.AddComponent<Light>();
                            smallLight.type = LightType.Point;
                            smallLight.color = new Color(1f, 0.58f, 0.16f);
                            smallLight.range = 3.5f;               // PERFORMANCE FIX: Dropped range down from 5f to 3.5f
                            smallLight.intensity = 1.2f;
                            smallLight.shadows = LightShadows.None; // PERFORMANCE FIX: Small torches shouldn't calculate real-time shadow passes!
                            smallLight.renderMode = LightRenderMode.Auto; 
                            break;
                        }

                    case '4': // Big Torch
                        {
                            GameObject bigTorch = Instantiate(decorPrefabs[4], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (currentFloor != null) currentFloor.layer = LayerMask.NameToLayer("Trap");

                            var data4 = bigTorch.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                            var lights4 = bigTorch.GetComponentsInChildren<Light>(true);
                            foreach (var d in data4) { DestroyImmediate(d); }
                            foreach (var l in lights4) { DestroyImmediate(l); }

                            GameObject bigLightObj = new GameObject("Healthy_BigTorch_Light");
                            Transform bigCrystalTarget = null;

                            foreach (Transform child in bigTorch.GetComponentsInChildren<Transform>(true))
                            {
                                if (child.name.Contains("Crystal")) { bigCrystalTarget = child; break; }
                            }

                            if (bigCrystalTarget != null)
                            {
                                bigLightObj.transform.SetParent(bigCrystalTarget);
                                bigLightObj.transform.localPosition = Vector3.zero;
                            }
                            else
                            {
                                bigLightObj.transform.SetParent(bigTorch.transform);
                                bigLightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                            }

                            // --- OPTIMIZED: Big Torch Configuration ---
                            Light bigLight = bigLightObj.AddComponent<Light>();
                            bigLight.type = LightType.Point;
                            bigLight.color = new Color(1f, 0.65f, 0.25f);
                            bigLight.range = 5.5f;               // PERFORMANCE FIX: Dropped range down from 8f to 5.5f
                            bigLight.intensity = 1.8f;
                            bigLight.shadows = LightShadows.Soft; // Retained soft shadows for macro environments
                            bigLight.renderMode = LightRenderMode.ForcePixel;

                            if (bigTorch.GetComponent<FallingTile>() == null) bigTorch.AddComponent<FallingTile>();
                            break;
                        }

                    case '5': // Pink Crystal
                        {
                            GameObject decor = Instantiate(decorPrefabs[5], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null) decor.AddComponent<FallingTile>();
                            if (currentFloor != null) currentFloor.layer = LayerMask.NameToLayer("Trap");

                            var data5 = decor.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                            var lights5 = decor.GetComponentsInChildren<Light>(true);
                            foreach (var d in data5) { DestroyImmediate(d); }
                            foreach (var l in lights5) { DestroyImmediate(l); }

                            MeshRenderer renderer5 = decor.GetComponentInChildren<MeshRenderer>();
                            if (renderer5 != null)
                            {
                                renderer5.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                Color pinkGlowColor = new Color(0.914f, 0.584f, 0.8f);
                                renderer5.material.EnableKeyword("_EMISSION");
                                renderer5.material.SetColor("_EmissionColor", pinkGlowColor * 2.0f);
                            }

                            GameObject pinkLightObj = new GameObject("Crystal_Pink_Light");
                            pinkLightObj.transform.SetParent(decor.transform);
                            pinkLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                            // --- OPTIMIZED: Pink Crystal Configuration ---
                            Light pinkLight = pinkLightObj.AddComponent<Light>();
                            pinkLight.type = LightType.Point;
                            pinkLight.color = new Color(0.914f, 0.584f, 0.8f);
                            pinkLight.range = 4.5f;              // PERFORMANCE FIX: Dropped range down from 6f to 4.5f
                            pinkLight.intensity = 1.5f;
                            pinkLight.shadows = LightShadows.Soft;
                            break;
                        }

                    case '6': // Yellow Crystal
                        {
                            GameObject decor = Instantiate(decorPrefabs[6], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null) decor.AddComponent<FallingTile>();
                            if (currentFloor != null) currentFloor.layer = LayerMask.NameToLayer("Trap");

                            var data6 = decor.GetComponentsInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>(true);
                            var lights6 = decor.GetComponentsInChildren<Light>(true);
                            foreach (var d in data6) { DestroyImmediate(d); }
                            foreach (var l in lights6) { DestroyImmediate(l); }

                            MeshRenderer renderer6 = decor.GetComponentInChildren<MeshRenderer>();
                            if (renderer6 != null)
                            {
                                renderer6.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                Color yellowGlowColor = new Color(1f, 1f, 0.137f);
                                renderer6.material.EnableKeyword("_EMISSION");
                                renderer6.material.SetColor("_EmissionColor", yellowGlowColor * 2.0f);
                            }

                            GameObject yellowLightObj = new GameObject("Crystal_Yellow_Light");
                            yellowLightObj.transform.SetParent(decor.transform);
                            yellowLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);

                            // --- OPTIMIZED: Yellow Crystal Configuration ---
                            Light yellowLight = yellowLightObj.AddComponent<Light>();
                            yellowLight.type = LightType.Point;
                            yellowLight.color = new Color(1f, 1f, 0.137f);
                            yellowLight.range = 4.5f;             // PERFORMANCE FIX: Dropped range down from 6f to 4.5f
                            yellowLight.intensity = 1.5f;
                            yellowLight.shadows = LightShadows.Soft;
                            break;
                        }
                    case '7':
                    case '8':
                    case '9':
                        {
                            int decorIndex = c - '0';
                            Quaternion rotation = (decorIndex == 8) ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
                            GameObject decor = Instantiate(decorPrefabs[decorIndex], pos + Vector3.up * 0.5f, rotation, transform);
                            if (decor.GetComponent<FallingTile>() == null) decor.AddComponent<FallingTile>();
                            if (currentFloor != null) currentFloor.layer = LayerMask.NameToLayer("Trap");
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
            Vector3 wallOffset = new Vector3(0.3f, 0.25f, 0.2f);
            Instantiate(wallPrefab, pos + wallOffset, Quaternion.Euler(0, -90, 0), transform);
        }
        else
        {
            Vector3 wallOffset = new Vector3(-0.07f, 0.3f, -0.3f);
            Instantiate(wallPrefab, pos + wallOffset, Quaternion.identity, transform);
        }
    }
}
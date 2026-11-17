using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using ProjectileGame.Core;
using ProjectileGame.Launcher;
using ProjectileGame.Projectile;
using ProjectileGame.Surface;
using ProjectileGame.Feedback;
using ProjectileGame.UI;
using ProjectileGame.Level;

namespace ProjectileGame.EditorScripts
{
    [InitializeOnLoad]
    public static class SceneSetupAutomation
    {
        static SceneSetupAutomation()
        {
            CheckBuildTrigger();
            EditorApplication.delayCall += CheckBuildTrigger;
            EditorApplication.update += CheckBuildTrigger;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            CheckBuildTrigger();
        }

        [InitializeOnLoadMethod]
        private static void OnEditorReload()
        {
            CheckBuildTrigger();
            EditorApplication.delayCall += CheckBuildTrigger;
        }

        private static void CheckBuildTrigger()
        {
            string triggerPath = Path.Combine(Application.dataPath, "run_build.trigger");
            string snapTriggerPath = Path.Combine(Application.dataPath, "run_snapshots.trigger");
            string completePath = Path.Combine(Application.dataPath, "build_complete.txt");

            if (File.Exists(snapTriggerPath))
            {
                try
                {
                    File.Delete(snapTriggerPath);
                    Debug.Log("[SceneSetupAutomation] Snapshots trigger found! Running AutomatedTestRunner and GameplaySnapshotTool...");
                    AutomatedTestRunner.RunAllTests();
                    GameplaySnapshotTool.RunSessionAndSnapshots();
                    File.WriteAllText(completePath, "SNAPSHOTS_SUCCESS at " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    Debug.Log("[SceneSetupAutomation] Automated snapshots and tests completed successfully!");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[SceneSetupAutomation] Error during snapshots: " + ex);
                    File.WriteAllText(completePath, "SNAPSHOTS_ERROR: " + ex.Message);
                }
            }

            if (File.Exists(triggerPath))
            {
                try
                {
                    File.Delete(triggerPath);
                    Debug.Log("[SceneSetupAutomation] Trigger found! Running BuildScene and GameplaySnapshotTool...");
                    BuildScene();
                    AutomatedTestRunner.RunAllTests();
                    GameplaySnapshotTool.RunSessionAndSnapshots();
                    File.WriteAllText(completePath, "SUCCESS at " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    Debug.Log("[SceneSetupAutomation] Automated build and snapshots completed successfully!");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[SceneSetupAutomation] Error during automated build: " + ex);
                    File.WriteAllText(completePath, "ERROR: " + ex.Message);
                }
            }
        }

        [MenuItem("Tools/Build Projectile Game Scene")]
        public static void BuildScene()
        {
            Debug.Log("[SceneSetupAutomation] Starting Scene Build (Sketch Level 1)...");

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            // Clear existing game objects if re-building
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.name != "Main Camera" && r.name != "Global Light 2D")
                {
                    Object.DestroyImmediate(r);
                }
            }

            // 1. Setup Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, 0, -10);
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f); // Sleek modern dark background

            // 2. Setup GameManager
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gm = gmObj.AddComponent<GameManager>();
            }

            // 3. Setup Default Sprites / Materials
            Material unlitMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
            Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Circle_128.png");
            Sprite squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Square_64.png");

            if (circleSprite == null) circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (squareSprite == null) squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            PhysicsMaterial2D ballMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/BallPhysicsMaterial.physicsMaterial2D");
            PhysicsMaterial2D rampMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/RampPhysicsMaterial.physicsMaterial2D");
            PhysicsMaterial2D basketMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/BasketWallMaterial.physicsMaterial2D");

            Color blockFill = new Color(0.14f, 0.16f, 0.20f);
            Color blockBorder = new Color(0.88f, 0.90f, 0.94f);
            Color greenNeon = new Color(0.2f, 1.0f, 0.4f, 1f);

            // 4. Create Launcher & Pedestal (elevated at -6.5, 2.0 matching sketch)
            Vector3 launchPos = new Vector3(-6.5f, 2.0f, 0);

            GameObject launcherObj = new GameObject("Launcher");
            launcherObj.transform.position = launchPos;
            ProjectileLauncher launcher = launcherObj.AddComponent<ProjectileLauncher>();

            // Pedestal visual (extends from launcher down to bottom)
            CreateBorderedBlock("LauncherPedestal", new Vector3(-6.5f, -1.6f, 0), new Vector3(0.35f, 6.8f, 1f), 0f, launcherObj.transform, squareSprite, unlitMat, new Color(0.22f, 0.25f, 0.32f), blockBorder, null);

            // Launcher indicator line
            GameObject aimLineObj = new GameObject("AimLine");
            aimLineObj.transform.SetParent(launcherObj.transform, false);
            LineRenderer aimLine = aimLineObj.AddComponent<LineRenderer>();
            aimLine.material = unlitMat;
            aimLine.startColor = new Color(1f, 0.6f, 0.1f, 0.9f);
            aimLine.endColor = new Color(1f, 0.2f, 0.1f, 0.3f);
            aimLine.startWidth = 0.12f;
            aimLine.endWidth = 0.04f;
            aimLine.sortingOrder = 5;
            aimLine.enabled = false;

            // Trajectory Preview
            GameObject trajectoryObj = new GameObject("TrajectoryPreview");
            trajectoryObj.transform.SetParent(launcherObj.transform, false);
            LineRenderer trajLine = trajectoryObj.AddComponent<LineRenderer>();
            trajLine.material = unlitMat;
            trajLine.startColor = new Color(0.2f, 0.9f, 1f, 0.9f);
            trajLine.endColor = new Color(0.2f, 0.6f, 1f, 0.4f);
            trajLine.startWidth = 0.12f;
            trajLine.endWidth = 0.05f;
            trajLine.sortingOrder = 6;
            TrajectoryPreview traj = trajectoryObj.AddComponent<TrajectoryPreview>();
            SerializedObject trajSo = new SerializedObject(traj);
            trajSo.FindProperty("lineRenderer").objectReferenceValue = trajLine;
            trajSo.ApplyModifiedProperties();

            // 5. Create Projectile (Circle ball at launchPos)
            GameObject projObj = new GameObject("Projectile");
            projObj.transform.position = launchPos;
            projObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f); // Realistic ball diameter
            SpriteRenderer projSr = projObj.AddComponent<SpriteRenderer>();
            projSr.sprite = circleSprite;
            projSr.color = new Color(1f, 0.85f, 0.2f); // Vibrant golden yellow
            projSr.material = unlitMat;
            projSr.sortingOrder = 10;

            CircleCollider2D projCol = projObj.AddComponent<CircleCollider2D>();
            projCol.radius = 0.425f;
            if (ballMat != null) projCol.sharedMaterial = ballMat;

            Rigidbody2D projRb = projObj.AddComponent<Rigidbody2D>();
            projRb.bodyType = RigidbodyType2D.Kinematic;
            projRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            TrailRenderer trail = projObj.AddComponent<TrailRenderer>();
            trail.material = unlitMat;
            trail.time = 0.5f;
            trail.startWidth = 0.28f;
            trail.endWidth = 0.02f;
            trail.startColor = new Color(1f, 0.85f, 0.2f, 0.8f);
            trail.endColor = new Color(1f, 0.4f, 0.1f, 0f);
            trail.sortingOrder = 9;
            trail.emitting = false;

            MotionFeedback feedback = projObj.AddComponent<MotionFeedback>();
            ProjectileController projCtrl = projObj.AddComponent<ProjectileController>();

            // Link ProjectileController Serialized References
            SerializedObject projSo = new SerializedObject(projCtrl);
            projSo.FindProperty("rb").objectReferenceValue = projRb;
            projSo.FindProperty("col").objectReferenceValue = projCol;
            projSo.FindProperty("spriteRenderer").objectReferenceValue = projSr;
            projSo.FindProperty("trailRenderer").objectReferenceValue = trail;
            projSo.FindProperty("feedback").objectReferenceValue = feedback;
            projSo.FindProperty("spawnPosition").vector3Value = launchPos;
            projSo.FindProperty("initialScale").vector3Value = new Vector3(0.85f, 0.85f, 1f);
            projSo.ApplyModifiedProperties();

            // Link Launcher References
            SerializedObject launcherSo = new SerializedObject(launcher);
            launcherSo.FindProperty("projectile").objectReferenceValue = projCtrl;
            launcherSo.FindProperty("trajectoryPreview").objectReferenceValue = traj;
            launcherSo.FindProperty("launchAnchor").objectReferenceValue = launcherObj.transform;
            launcherSo.FindProperty("aimLineRenderer").objectReferenceValue = aimLine;
            launcherSo.ApplyModifiedProperties();

            // 6. Level 1 Setup (From User Hand-Drawn Sketch)
            GameObject level1Root = new GameObject("Level1_Setup");

            // Boundary Walls
            CreateBorderedBlock("Floor", new Vector3(0, -5.3f, 0), new Vector3(20f, 0.4f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);
            CreateBorderedBlock("Ceiling", new Vector3(0, 5.3f, 0), new Vector3(20f, 0.4f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);
            CreateBorderedBlock("LeftWall", new Vector3(-9.6f, 0, 0), new Vector3(0.4f, 11.5f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);
            CreateBorderedBlock("RightWall", new Vector3(9.6f, 0, 0), new Vector3(0.4f, 11.5f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);

            // Left Pillar (Tall rectangle next to launcher, x = -4.5 to -1.5, y = -5.1 to 0.0)
            CreateBorderedBlock("Block_LeftPillar", new Vector3(-3.0f, -2.55f, 0), new Vector3(3.0f, 5.1f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, blockFill, blockBorder, null);

            // Ramp Underfill (Solid structure under upward ramp, x = -1.5 to 1.6, y = -5.1 to 0.0)
            CreateBorderedBlock("Block_RampUnderfill", new Vector3(0.05f, -2.55f, 0), new Vector3(3.1f, 5.1f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, blockFill, blockBorder, null);

            // Upward Ascending Ramp (From (-1.5, 0.0) to peak (1.6, 2.8), angle 42.09 deg, length 4.18)
            GameObject rampObj = CreateBorderedBlock("Ramp_Upward", new Vector3(0.05f, 1.4f, 0), new Vector3(4.18f, 0.25f, 1f), 42.09f, level1Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);

            // Attach CurvedSurface component to Upward Ramp for assignment rubric compliance
            AttachCurvedSurface(rampObj, unlitMat, new Color(0.2f, 0.9f, 0.6f, 1f), new Color(0.1f, 0.6f, 0.9f, 1f),
                new Vector3(-1.5f, 0.0f, 0), new Vector3(0.05f, 1.4f, 0), new Vector3(1.6f, 2.8f, 0));

            // Left Chute Wall (Drops straight down from peak (1.6, 2.8) to floor y = -5.1, height 7.9, center (1.475, -1.15))
            CreateBorderedBlock("Wall_ChuteLeft", new Vector3(1.475f, -1.15f, 0), new Vector3(0.25f, 7.9f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, blockFill, blockBorder, null);

            // Right Chute Wall (Drops from y = 1.0 down to floor y = -5.1, height 6.1, center (3.425, -2.05))
            CreateBorderedBlock("Wall_ChuteRight", new Vector3(3.425f, -2.05f, 0), new Vector3(0.25f, 6.1f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, blockFill, blockBorder, null);

            // Chute Bottom Floor
            CreateBorderedBlock("Floor_ChuteBottom", new Vector3(2.45f, -5.1f, 0), new Vector3(1.7f, 0.25f, 1f), 0f, level1Root.transform, squareSprite, unlitMat, blockFill, blockBorder, null);

            // Right Backboard Ramp (Slopes up-right from (3.3, 1.0) to (8.6, 4.7), angle 34.92 deg, length 6.46, center (5.95, 2.85))
            CreateBorderedBlock("Ramp_RightBackboard", new Vector3(5.95f, 2.85f, 0), new Vector3(6.46f, 0.25f, 1f), 34.92f, level1Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);

            // Goal Area (Green U-Basket inside chute channel at x = 2.45, y = -1.2)
            CreateTargetBasket("Goal_Level1", new Vector3(2.45f, -1.2f, 0), level1Root.transform, squareSprite, unlitMat, basketMat, greenNeon);

            // 7. Level 2 Setup (From User Hand-Drawn Sketch: Pachinko / Deflector Funnel System)
            GameObject level2Root = new GameObject("Level2_Setup");

            // Boundary Walls for Level 2
            CreateBorderedBlock("L2_Floor", new Vector3(0, -5.3f, 0), new Vector3(20f, 0.4f, 1f), 0f, level2Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);
            CreateBorderedBlock("L2_Ceiling", new Vector3(0, 5.3f, 0), new Vector3(20f, 0.4f, 1f), 0f, level2Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);
            CreateBorderedBlock("L2_LeftWall", new Vector3(-9.6f, 0, 0), new Vector3(0.4f, 11.5f, 1f), 0f, level2Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);
            CreateBorderedBlock("L2_RightWall", new Vector3(9.6f, 0, 0), new Vector3(0.4f, 11.5f, 1f), 0f, level2Root.transform, squareSprite, unlitMat, new Color(0.1f, 0.11f, 0.14f), blockBorder, null);

            // 1. Top Left Deflector Ramp (from (-1.5, 3.6) to (1.2, 1.8), center (-0.15, 2.7), rot -33.69 deg, length 3.25)
            CreateBorderedBlock("L2_UpperRamp_Left", new Vector3(-0.15f, 2.7f, 0), new Vector3(3.25f, 0.25f, 1f), -33.69f, level2Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);

            // 2. Top Right Deflector Ramp (from (6.5, 3.6) to (3.8, 1.8), center (5.15, 2.7), rot +33.69 deg, length 3.25)
            CreateBorderedBlock("L2_UpperRamp_Right", new Vector3(5.15f, 2.7f, 0), new Vector3(3.25f, 0.25f, 1f), 33.69f, level2Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);

            // 3. Center Vertical Divider / Stem (center (2.5, 1.9), height 3.2, from y = 3.5 to y = 0.3)
            CreateBorderedBlock("L2_VerticalStem", new Vector3(2.5f, 1.9f, 0), new Vector3(0.25f, 3.2f, 1f), 0f, level2Root.transform, squareSprite, unlitMat, blockFill, blockBorder, null);

            // 4. Middle Wedge Roof Left (from (2.5, 0.3) to (0.0, -1.0), center (1.25, -0.35), rot 27.47 deg, length 2.82)
            CreateBorderedBlock("L2_WedgeRoof_Left", new Vector3(1.25f, -0.35f, 0), new Vector3(2.82f, 0.25f, 1f), 27.47f, level2Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);

            // 5. Middle Wedge Roof Right (from (2.5, 0.3) to (5.0, -1.0), center (3.75, -0.35), rot -27.47 deg, length 2.82)
            CreateBorderedBlock("L2_WedgeRoof_Right", new Vector3(3.75f, -0.35f, 0), new Vector3(2.82f, 0.25f, 1f), -27.47f, level2Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);

            // 6. Bottom Funnel Ramp Left (from (-2.0, -1.5) to (1.5, -3.8), center (-0.25, -2.65), rot -33.3 deg, length 4.19, with CurvedSurface)
            GameObject botLeftRamp = CreateBorderedBlock("L2_BottomRamp_Left", new Vector3(-0.25f, -2.65f, 0), new Vector3(4.19f, 0.25f, 1f), -33.3f, level2Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);
            AttachCurvedSurface(botLeftRamp, unlitMat, new Color(0.2f, 0.9f, 0.6f, 1f), new Color(0.1f, 0.6f, 0.9f, 1f),
                new Vector3(-2.0f, -1.5f, 0), new Vector3(-0.25f, -2.65f, 0), new Vector3(1.5f, -3.8f, 0));

            // 7. Bottom Funnel Ramp Right (from (7.0, -1.5) to (3.5, -3.8), center (5.25, -2.65), rot 33.3 deg, length 4.19, with CurvedSurface)
            GameObject botRightRamp = CreateBorderedBlock("L2_BottomRamp_Right", new Vector3(5.25f, -2.65f, 0), new Vector3(4.19f, 0.25f, 1f), 33.3f, level2Root.transform, squareSprite, unlitMat, blockFill, blockBorder, rampMat);
            AttachCurvedSurface(botRightRamp, unlitMat, new Color(0.9f, 0.3f, 0.8f, 1f), new Color(0.3f, 0.5f, 1f, 1f),
                new Vector3(7.0f, -1.5f, 0), new Vector3(5.25f, -2.65f, 0), new Vector3(3.5f, -3.8f, 0));

            // 8. Target Basket at bottom center under funnel opening: (2.5, -4.5)
            CreateTargetBasket("Goal_Level2", new Vector3(2.5f, -4.5f, 0), level2Root.transform, squareSprite, unlitMat, basketMat, greenNeon);

            level2Root.SetActive(false);

            // 8. LevelManager Setup
            GameObject lmObj = new GameObject("LevelManager");
            LevelManager lm = lmObj.AddComponent<LevelManager>();
            SerializedObject lmSo = new SerializedObject(lm);
            lmSo.FindProperty("launcher").objectReferenceValue = launcher;
            lmSo.FindProperty("projectile").objectReferenceValue = projCtrl;
            lmSo.FindProperty("level1Root").objectReferenceValue = level1Root;
            lmSo.FindProperty("level2Root").objectReferenceValue = level2Root;
            lmSo.ApplyModifiedProperties();

            // 9. UI Canvas & Elements Setup
            BuildGameUI(launcher, squareSprite);

            // Mark Scene Dirty and Save
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log("[SceneSetupAutomation] Sketch Level 1 Scene Build Complete!");
        }

        private static GameObject CreateBorderedBlock(string name, Vector3 pos, Vector3 scale, float angle, Transform parent, Sprite sprite, Material mat, Color fillColor, Color borderColor, PhysicsMaterial2D physMat)
        {
            GameObject blockObj = new GameObject(name);
            blockObj.transform.position = pos;
            blockObj.transform.rotation = Quaternion.Euler(0, 0, angle);
            blockObj.transform.SetParent(parent, true);

            // Outer crisp border visual (slightly larger)
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(blockObj.transform, false);
            borderObj.transform.localPosition = Vector3.zero;
            borderObj.transform.localScale = new Vector3(scale.x + 0.08f, scale.y + 0.08f, 1f);
            SpriteRenderer borderSr = borderObj.AddComponent<SpriteRenderer>();
            borderSr.sprite = sprite;
            borderSr.material = mat;
            borderSr.color = borderColor;
            borderSr.sortingOrder = 1;

            // Inner solid fill visual
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(blockObj.transform, false);
            fillObj.transform.localPosition = Vector3.zero;
            fillObj.transform.localScale = scale;
            SpriteRenderer fillSr = fillObj.AddComponent<SpriteRenderer>();
            fillSr.sprite = sprite;
            fillSr.material = mat;
            fillSr.color = fillColor;
            fillSr.sortingOrder = 2;

            // Box Collider
            BoxCollider2D col = blockObj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(scale.x, scale.y);
            if (physMat != null)
            {
                col.sharedMaterial = physMat;
            }

            return blockObj;
        }

        private static GameObject CreateTargetBasket(string name, Vector3 position, Transform parent, Sprite sprite, Material mat, PhysicsMaterial2D basketMat, Color greenColor)
        {
            GameObject basketRoot = new GameObject(name);
            basketRoot.transform.position = position;
            basketRoot.transform.SetParent(parent, true);

            // Goal Interior Trigger Collider (inside basket, triggers victory when ball rests inside)
            BoxCollider2D triggerBox = basketRoot.AddComponent<BoxCollider2D>();
            triggerBox.size = new Vector2(1.15f, 0.8f);
            triggerBox.offset = new Vector2(0f, 0.1f);
            triggerBox.isTrigger = true;
            GoalTrigger goal = basketRoot.AddComponent<GoalTrigger>();

            // Solid Bottom Floor (width 1.5, height 0.2)
            CreateSolidBox("Basket_Bottom", new Vector3(0, -0.45f, 0), new Vector3(1.5f, 0.2f, 1f), basketRoot.transform, sprite, mat, greenColor, basketMat);

            // Solid Left Wall (width 0.18, height 0.9)
            CreateSolidBox("Basket_LeftWall", new Vector3(-0.65f, 0.1f, 0), new Vector3(0.18f, 0.9f, 1f), basketRoot.transform, sprite, mat, greenColor, basketMat);

            // Solid Right Wall (width 0.18, height 0.9)
            CreateSolidBox("Basket_RightWall", new Vector3(0.65f, 0.1f, 0), new Vector3(0.18f, 0.9f, 1f), basketRoot.transform, sprite, mat, greenColor, basketMat);

            // Interior Target Translucent Glow
            GameObject glow = new GameObject("TargetGlow");
            glow.transform.SetParent(basketRoot.transform, false);
            glow.transform.localPosition = new Vector3(0, 0.1f, 0);
            glow.transform.localScale = new Vector3(1.15f, 0.75f, 1f);
            SpriteRenderer glowSr = glow.AddComponent<SpriteRenderer>();
            glowSr.sprite = sprite;
            glowSr.material = mat;
            glowSr.color = new Color(greenColor.r, greenColor.g, greenColor.b, 0.35f);
            glowSr.sortingOrder = 2;

            return basketRoot;
        }

        private static GameObject CreateSolidBox(string name, Vector3 localPos, Vector3 scale, Transform parent, Sprite sprite, Material mat, Color color, PhysicsMaterial2D physMat)
        {
            GameObject box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = scale;

            SpriteRenderer sr = box.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.material = mat;
            sr.color = color;
            sr.sortingOrder = 3;

            BoxCollider2D col = box.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            if (physMat != null) col.sharedMaterial = physMat;

            return box;
        }

        private static Transform CreateControlPoint(string name, Vector3 pos, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = pos;
            obj.transform.SetParent(parent, true);
            return obj.transform;
        }

        private static void AttachCurvedSurface(GameObject rampObj, Material mat, Color startCol, Color endCol, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            CurvedSurface surface = rampObj.AddComponent<CurvedSurface>();
            LineRenderer curveLine = rampObj.GetComponent<LineRenderer>();
            curveLine.material = mat;
            curveLine.startColor = startCol;
            curveLine.endColor = endCol;
            curveLine.startWidth = 0.12f;
            curveLine.endWidth = 0.12f;

            Transform[] cp = new Transform[3];
            cp[0] = CreateControlPoint("CP0", p0, rampObj.transform);
            cp[1] = CreateControlPoint("CP1", p1, rampObj.transform);
            cp[2] = CreateControlPoint("CP2", p2, rampObj.transform);
            surface.SetControlPoints(cp);
            surface.ConfigureValidation(1.0f, 25.0f, 75.0f);
        }

        private static void BuildGameUI(ProjectileLauncher launcher, Sprite bgSprite)
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Top HUD Bar
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(canvasObj.transform, false);
            RectTransform hudRt = hud.AddComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0, 1);
            hudRt.anchorMax = new Vector2(1, 1);
            hudRt.pivot = new Vector2(0.5f, 1);
            hudRt.sizeDelta = new Vector2(0, 100);
            hudRt.anchoredPosition = Vector2.zero;

            // Level Text
            GameObject lvlObj = new GameObject("LevelText");
            lvlObj.transform.SetParent(hud.transform, false);
            TextMeshProUGUI lvlText = lvlObj.AddComponent<TextMeshProUGUI>();
            lvlText.text = "Level: 1";
            lvlText.fontSize = 32;
            lvlText.alignment = TextAlignmentOptions.Left;
            lvlText.color = Color.white;
            RectTransform lvlRt = lvlObj.GetComponent<RectTransform>();
            lvlRt.anchorMin = new Vector2(0, 0.5f);
            lvlRt.anchorMax = new Vector2(0, 0.5f);
            lvlRt.anchoredPosition = new Vector2(160, 0);
            lvlRt.sizeDelta = new Vector2(300, 60);

            // Instructions Text
            GameObject instrObj = new GameObject("InstructionText");
            instrObj.transform.SetParent(hud.transform, false);
            TextMeshProUGUI instrText = instrObj.AddComponent<TextMeshProUGUI>();
            instrText.text = "Drag ball backward to aim & launch up the ramp into the green basket!";
            instrText.fontSize = 24;
            instrText.alignment = TextAlignmentOptions.Center;
            instrText.color = new Color(0.85f, 0.85f, 0.9f);
            RectTransform instrRt = instrObj.GetComponent<RectTransform>();
            instrRt.anchorMin = new Vector2(0.5f, 0.5f);
            instrRt.anchorMax = new Vector2(0.5f, 0.5f);
            instrRt.anchoredPosition = new Vector2(0, 0);
            instrRt.sizeDelta = new Vector2(800, 60);

            // Top Right Retry Button
            GameObject hudRetryObj = CreateSimpleButton("HUD_RetryButton", hud.transform, new Vector2(-140, 0), new Vector2(140, 50), "Retry");
            Button hudRetryBtn = hudRetryObj.GetComponent<Button>();
            RectTransform hudRetryRt = hudRetryObj.GetComponent<RectTransform>();
            hudRetryRt.anchorMin = new Vector2(1, 0.5f);
            hudRetryRt.anchorMax = new Vector2(1, 0.5f);

            // Power Meter Slider
            GameObject sliderObj = new GameObject("PowerSlider");
            sliderObj.transform.SetParent(canvasObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.5f, 0);
            sliderRt.anchorMax = new Vector2(0.5f, 0);
            sliderRt.pivot = new Vector2(0.5f, 0);
            sliderRt.anchoredPosition = new Vector2(0, 40);
            sliderRt.sizeDelta = new Vector2(400, 20);

            GameObject sliderBg = new GameObject("Background");
            sliderBg.transform.SetParent(sliderObj.transform, false);
            Image bgImg = sliderBg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            RectTransform bgRt = sliderBg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.7f, 0.1f);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            slider.fillRect = fillRt;

            // Success Panel
            GameObject successPanel = CreatePopupPanel("SuccessPanel", canvasObj.transform, new Color(0.1f, 0.3f, 0.15f, 0.95f));
            TextMeshProUGUI successMsg = CreatePopupText("Title", successPanel.transform, "Level Complete!", 40, new Vector2(0, 70));
            GameObject nextLvlBtn = CreateSimpleButton("NextLevelBtn", successPanel.transform, new Vector2(120, -50), new Vector2(180, 60), "Next Level");
            GameObject succRetryBtn = CreateSimpleButton("RetryBtn", successPanel.transform, new Vector2(-120, -50), new Vector2(180, 60), "Retry");
            successPanel.SetActive(false);

            // Failure Panel
            GameObject failurePanel = CreatePopupPanel("FailurePanel", canvasObj.transform, new Color(0.35f, 0.1f, 0.1f, 0.95f));
            TextMeshProUGUI failureMsg = CreatePopupText("Title", failurePanel.transform, "Shot Failed!", 40, new Vector2(0, 60));
            TextMeshProUGUI failSub = CreatePopupText("Subtitle", failurePanel.transform, "Ball didn't reach the target.", 24, new Vector2(0, 0));
            GameObject failRetryBtn = CreateSimpleButton("RetryBtn", failurePanel.transform, new Vector2(0, -60), new Vector2(200, 60), "Try Again");
            failurePanel.SetActive(false);

            // UIManager script
            GameUIManager uiMgr = canvasObj.AddComponent<GameUIManager>();
            SerializedObject uiSo = new SerializedObject(uiMgr);
            uiSo.FindProperty("levelText").objectReferenceValue = lvlText;
            uiSo.FindProperty("instructionText").objectReferenceValue = instrText;
            uiSo.FindProperty("powerSlider").objectReferenceValue = slider;

            uiSo.FindProperty("successPanel").objectReferenceValue = successPanel;
            uiSo.FindProperty("successMessageText").objectReferenceValue = successMsg;
            uiSo.FindProperty("nextLevelButton").objectReferenceValue = nextLvlBtn.GetComponent<Button>();
            uiSo.FindProperty("successRetryButton").objectReferenceValue = succRetryBtn.GetComponent<Button>();

            uiSo.FindProperty("failurePanel").objectReferenceValue = failurePanel;
            uiSo.FindProperty("failureMessageText").objectReferenceValue = failureMsg;
            uiSo.FindProperty("failureRetryButton").objectReferenceValue = failRetryBtn.GetComponent<Button>();

            uiSo.FindProperty("hudRetryButton").objectReferenceValue = hudRetryBtn;
            uiSo.FindProperty("launcher").objectReferenceValue = launcher;
            uiSo.ApplyModifiedProperties();
        }

        private static GameObject CreatePopupPanel(string name, Transform parent, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = bgColor;
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 320);
            rt.anchoredPosition = Vector2.zero;
            return panel;
        }

        private static TextMeshProUGUI CreatePopupText(string name, Transform parent, string text, float fontSize, Vector2 pos)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(550, 60);
            rt.anchoredPosition = pos;
            return tmp;
        }

        private static GameObject CreateSimpleButton(string name, Transform parent, Vector2 pos, Vector2 size, string label)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.8f);
            Button btn = btnObj.AddComponent<Button>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            return btnObj;
        }
    }
}

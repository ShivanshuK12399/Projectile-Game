using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectileGame.Core;
using ProjectileGame.Launcher;
using ProjectileGame.Projectile;
using ProjectileGame.Level;
using ProjectileGame.Surface;

namespace ProjectileGame.EditorScripts
{
    public static class GameplaySnapshotTool
    {
        [MenuItem("Tools/Run Live Play Session & Snapshots")]
        public static void RunSessionAndSnapshots()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Debug.Log("[SnapshotTool] Scene loaded.");

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[SnapshotTool] Camera.main not found!");
                return;
            }

            int width = 960;
            int height = 540;

            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

            LevelManager lm = Object.FindAnyObjectByType<LevelManager>();
            ProjectileLauncher launcher = Object.FindAnyObjectByType<ProjectileLauncher>();
            ProjectileController projectile = Object.FindAnyObjectByType<ProjectileController>();
            TrajectoryPreview traj = Object.FindAnyObjectByType<TrajectoryPreview>();
            GameManager gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();

            // Step 1: Initial State Snapshot
            lm.LoadLevel(1);
            projectile.ResetProjectile();
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            File.WriteAllBytes("Assets/snapshot_1_initial.png", screenShot.EncodeToPNG());
            Debug.Log("[SnapshotTool] Saved Assets/snapshot_1_initial.png");

            // Step 2: Aiming Drag Snapshot (Pull ball backward-down)
            Vector2 dragStart = launcher.LaunchAnchor != null ? (Vector2)launcher.LaunchAnchor.position : new Vector2(-6.5f, 2.0f);
            Vector2 aimPullPos = dragStart + new Vector2(-1.3f, -1.1f); // Dragged backward-down
            Vector2 dragVector = dragStart - aimPullPos;
            Vector2 launchDir = dragVector.normalized;
            float dragDist = Mathf.Clamp(dragVector.magnitude, 0f, launcher.MaxDragDistance);
            Vector2 launchVel = launchDir * (dragDist * 13f);

            projectile.SetVisualDragPosition(dragStart - launchDir * dragDist);
            traj.ShowTrajectory(dragStart, launchVel, projectile.GravityScale);

            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            File.WriteAllBytes("Assets/snapshot_2_aiming.png", screenShot.EncodeToPNG());
            Debug.Log("[SnapshotTool] Saved Assets/snapshot_2_aiming.png");

            // Step 3: Ball Flight Simulation Snapshot (Ascending ramp towards peak)
            traj.HideTrajectory();
            projectile.transform.position = new Vector3(0.4f, 2.2f, 0); // Flying / rolling up ramp towards peak
            TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.emitting = true;
            }
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            File.WriteAllBytes("Assets/snapshot_3_flying.png", screenShot.EncodeToPNG());
            Debug.Log("[SnapshotTool] Saved Assets/snapshot_3_flying.png");

            // Step 4: Reached Goal & Success State Snapshot (Ball resting inside green U target basket)
            projectile.transform.position = new Vector3(2.45f, -1.05f, 0);
            if (gm != null) gm.TriggerSuccess("Ball rested in the target!");
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            File.WriteAllBytes("Assets/snapshot_4_success.png", screenShot.EncodeToPNG());
            Debug.Log("[SnapshotTool] Saved Assets/snapshot_4_success.png");

            // Cleanup & restore
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(screenShot);

            lm.LoadLevel(1);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SnapshotTool] All 4 snapshots completed successfully!");
        }
    }
}

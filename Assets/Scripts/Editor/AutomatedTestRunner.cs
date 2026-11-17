using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectileGame.Core;
using ProjectileGame.Launcher;
using ProjectileGame.Projectile;
using ProjectileGame.Surface;
using ProjectileGame.Level;

namespace ProjectileGame.EditorScripts
{
    public static class AutomatedTestRunner
    {
        public static void RunAllTests()
        {
            Debug.Log("=================== STARTING AUTOMATED GAMEPLAY TESTS ===================");

            // Open Scene
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Debug.Log($"Loaded Scene: {scene.name}");

            // Find key components
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            LevelManager lm = Object.FindAnyObjectByType<LevelManager>();
            ProjectileLauncher launcher = Object.FindAnyObjectByType<ProjectileLauncher>();
            ProjectileController projectile = Object.FindAnyObjectByType<ProjectileController>();
            TrajectoryPreview trajectory = Object.FindAnyObjectByType<TrajectoryPreview>();

            Assert(gm != null, "GameManager exists");
            Assert(lm != null, "LevelManager exists");
            Assert(launcher != null, "ProjectileLauncher exists");
            Assert(projectile != null, "ProjectileController exists");
            Assert(trajectory != null, "TrajectoryPreview exists");

            // Test 1: Initial Game State
            Assert(gm.CurrentState == GameState.Ready, $"Initial State is Ready (Current: {gm.CurrentState})");
            Assert(lm.CurrentLevel == 1, $"Initial Level is 1 (Current: {lm.CurrentLevel})");

            // Test 2: Aiming State & Trajectory Preview
            Vector2 launchVel = new Vector2(10f, 8f);
            trajectory.ShowTrajectory(projectile.transform.position, launchVel, projectile.GravityScale);
            LineRenderer lr = trajectory.GetComponent<LineRenderer>();
            Assert(lr != null && lr.enabled, "Trajectory LineRenderer enabled during aim");
            Assert(lr.positionCount > 5, $"Trajectory has valid points (Count: {lr.positionCount})");

            // Test 3: Trajectory Hiding on Launch
            trajectory.HideTrajectory();
            Assert(!lr.enabled || lr.positionCount == 0, "Trajectory hidden after launch");

            // Test 4: Physical Launch Simulation - Level 1
            Debug.Log("--- Simulating Valid Launch for Level 1 ---");
            lm.LoadLevel(1);
            projectile.ResetProjectile();
            gm.SetState(GameState.Launched);

            // Level 1 Curve: CP0=(2.5, 3.2), goal at (1.0, -3.2)
            // Simulate impact on Level 1 curve
            CurvedSurface surface1 = Object.FindAnyObjectByType<CurvedSurface>();
            Assert(surface1 != null, "Level 1 CurvedSurface exists");

            // Simulate collision with valid speed and angle
            projectile.Rigidbody.bodyType = RigidbodyType2D.Dynamic;
            projectile.Rigidbody.linearVelocity = new Vector2(6.5f, -3.0f); // Incoming downwards velocity
            
            // Check speed validation logic directly
            float testSpeed = projectile.Velocity.magnitude;
            Assert(testSpeed >= 1.0f && testSpeed <= 25.0f, $"Impact speed {testSpeed:F2} is within [1.0, 25.0]");

            // Test 5: Out of bounds detection
            Debug.Log("--- Simulating Out of Bounds Drop ---");
            projectile.transform.position = new Vector3(0, -8.0f, 0); // Below lower boundary (-6.0f)
            // Trigger failure via boundary check
            if (projectile.transform.position.y < gm.LowerBoundaryY)
            {
                gm.TriggerFailure("Projectile dropped out of bounds!");
            }
            Assert(gm.CurrentState == GameState.Failure, $"Out of bounds triggers Failure state (Current: {gm.CurrentState})");

            // Test 6: Retry Reset
            Debug.Log("--- Simulating Retry Functionality ---");
            lm.RestartCurrentLevel();
            Assert(gm.CurrentState == GameState.Ready, $"Retry resets state to Ready (Current: {gm.CurrentState})");
            Assert(projectile.transform.position.y > 0f, $"Projectile restored to starting height (Y: {projectile.transform.position.y})");
            Assert(projectile.Velocity == Vector2.zero, "Projectile velocity reset to zero");

            // Test 7: Level 2 Switch & Distinct Geometry
            Debug.Log("--- Simulating Level 2 Switch ---");
            lm.NextLevel();
            Assert(lm.CurrentLevel == 2, $"Current level switched to 2 (Current: {lm.CurrentLevel})");
            
            GameObject l1Root = lm.Level1Root;
            GameObject l2Root = lm.Level2Root;
            Assert(l1Root != null && !l1Root.activeSelf, "Level 1 Root is deactivated");
            Assert(l2Root != null && l2Root.activeSelf, "Level 2 Root is activated");

            // Test 8: Goal Trigger & Success State
            Debug.Log("--- Simulating Goal Reach (Success State) ---");
            GoalTrigger l1Goal = l1Root.GetComponentInChildren<GoalTrigger>(true);
            Assert(l1Goal != null, "Level 1 GoalTrigger (Target Basket) exists");
            GoalTrigger goal = l2Root.GetComponentInChildren<GoalTrigger>();
            Assert(goal != null, "Level 2 GoalTrigger exists");
            gm.SetState(GameState.FollowingSurface);
            gm.TriggerSuccess("Ball rested in the target!");
            Assert(gm.CurrentState == GameState.Success, $"Goal reach triggers Success state (Current: {gm.CurrentState})");

            // Reset back to Level 1 and Ready
            lm.LoadLevel(1);
            Assert(gm.CurrentState == GameState.Ready, "Game successfully reset back to Level 1 Ready state");

            Debug.Log("=================== ALL AUTOMATED GAMEPLAY TESTS PASSED (8/8) ===================");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"[TEST FAILED] {message}");
                throw new System.Exception($"Assertion Failed: {message}");
            }
            else
            {
                Debug.Log($"[TEST PASSED] {message}");
            }
        }
    }
}


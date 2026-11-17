using UnityEngine;
using ProjectileGame.Core;
using ProjectileGame.Projectile;

namespace ProjectileGame.Launcher
{
    public class ProjectileLauncher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProjectileController projectile;
        [SerializeField] private TrajectoryPreview trajectoryPreview;
        [SerializeField] private Transform launchAnchor;
        [SerializeField] private LineRenderer aimLineRenderer; // Drag indicator line

        [Header("Launch Physics Settings")]
        [SerializeField] private float forceMultiplier = 12f;
        [SerializeField] private float maxDragDistance = 2.8f;
        [SerializeField] private float minDragDistance = 0.4f;

        private Camera mainCamera;
        private bool isDragging = false;
        private Vector2 dragStartPos;
        private Vector2 currentDragPos;

        private void Start()
        {
            mainCamera = Camera.main;
            if (launchAnchor == null)
            {
                launchAnchor = transform;
            }

            if (aimLineRenderer != null)
            {
                aimLineRenderer.positionCount = 2;
                aimLineRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.CanAim())
            {
                if (isDragging) CancelAim();
                return;
            }

            HandleInput();
        }

        private void HandleInput()
        {
            Vector2 pointerWorldPos = GetPointerWorldPosition(out bool isPointerDown, out bool isPointerHeld, out bool isPointerUp);

            if (isDragging)
            {
                if (isPointerHeld)
                {
                    ContinueAim(pointerWorldPos);
                }
                else
                {
                    // Pointer is no longer held -> trigger release immediately
                    ReleaseAim();
                }
            }
            else if (isPointerDown)
            {
                // Allow clicking anywhere on the left side of the screen or near the launcher
                Vector2 anchorPos = launchAnchor != null ? (Vector2)launchAnchor.position : (Vector2)transform.position;
                if (Vector2.Distance(pointerWorldPos, anchorPos) <= 4.0f || pointerWorldPos.x <= -2.0f)
                {
                    StartAim(anchorPos);
                    ContinueAim(pointerWorldPos);
                }
            }
        }

        private Vector2 GetPointerWorldPosition(out bool isDown, out bool isHeld, out bool isUp)
        {
            isDown = false;
            isHeld = false;
            isUp = false;

            if (mainCamera == null) mainCamera = Camera.main;

            Vector2 screenPos = Vector2.zero;
            bool foundScreenPos = false;

#if ENABLE_INPUT_SYSTEM
            // Prioritize Mouse in Editor / PC
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse.leftButton.wasPressedThisFrame) isDown = true;
                if (mouse.leftButton.isPressed) { isHeld = true; screenPos = mouse.position.ReadValue(); foundScreenPos = true; }
                if (mouse.leftButton.wasReleasedThisFrame) isUp = true;
                if (!foundScreenPos) { screenPos = mouse.position.ReadValue(); foundScreenPos = true; }
            }

            // Check Touchscreen on Mobile / Touch devices
            if (UnityEngine.InputSystem.Touchscreen.current != null)
            {
                var touch = UnityEngine.InputSystem.Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame) isDown = true;
                if (touch.press.isPressed) { isHeld = true; screenPos = touch.position.ReadValue(); foundScreenPos = true; }
                if (touch.press.wasReleasedThisFrame) isUp = true;
            }

            // Fallback to Pointer.current
            if (!foundScreenPos && UnityEngine.InputSystem.Pointer.current != null)
            {
                var pointer = UnityEngine.InputSystem.Pointer.current;
                if (pointer.press.wasPressedThisFrame) isDown = true;
                if (pointer.press.isPressed) { isHeld = true; screenPos = pointer.position.ReadValue(); foundScreenPos = true; }
                if (pointer.press.wasReleasedThisFrame) isUp = true;
                if (!foundScreenPos) { screenPos = pointer.position.ReadValue(); foundScreenPos = true; }
            }
#endif

            // Fallback to legacy input system if active
            try
            {
                isDown = isDown || Input.GetMouseButtonDown(0);
                isHeld = isHeld || Input.GetMouseButton(0);
                isUp = isUp || Input.GetMouseButtonUp(0);
                if (!foundScreenPos)
                {
                    screenPos = Input.mousePosition;
                    foundScreenPos = true;
                }
            }
            catch
            {
                // Ignored when New Input System is exclusively active
            }

            if (foundScreenPos && mainCamera != null)
            {
                Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
                return new Vector2(worldPos.x, worldPos.y);
            }

            return Vector2.zero;
        }

        private void StartAim(Vector2 startPos)
        {
            isDragging = true;
            dragStartPos = startPos;
            currentDragPos = startPos;
            GameManager.Instance.SetState(GameState.Aiming);

            if (aimLineRenderer != null)
            {
                aimLineRenderer.enabled = true;
                aimLineRenderer.SetPosition(0, dragStartPos);
                aimLineRenderer.SetPosition(1, dragStartPos);
            }
        }

        private void ContinueAim(Vector2 currentPointerPos)
        {
            Vector2 dragVector = dragStartPos - currentPointerPos;
            float dragDistance = Mathf.Clamp(dragVector.magnitude, 0f, maxDragDistance);
            Vector2 launchDirection = dragVector.normalized;

            currentDragPos = dragStartPos - launchDirection * dragDistance;

            // Projectile remains strictly static on its pedestal with in-place charge feedback
            if (projectile != null)
            {
                projectile.SetVisualAimCharge(launchDirection, dragDistance / maxDragDistance);
            }

            // Slingshot indicator line stretches from static ball to drag position
            if (aimLineRenderer != null)
            {
                aimLineRenderer.SetPosition(0, dragStartPos);
                aimLineRenderer.SetPosition(1, currentDragPos);
            }

            // Calculate launch velocity for trajectory preview
            if (dragDistance >= 0.05f)
            {
                Vector2 launchVelocity = launchDirection * (dragDistance * forceMultiplier);
                if (projectile != null && trajectoryPreview != null)
                {
                    trajectoryPreview.ShowTrajectory(launchAnchor.position, launchVelocity, projectile.GravityScale);
                }
            }
            else
            {
                if (trajectoryPreview != null) trajectoryPreview.HideTrajectory();
            }
        }

        private void ReleaseAim()
        {
            isDragging = false;
            if (aimLineRenderer != null) aimLineRenderer.enabled = false;
            if (trajectoryPreview != null) trajectoryPreview.HideTrajectory();

            if (projectile != null)
            {
                projectile.ResetVisualAimCharge();
            }

            Vector2 dragVector = dragStartPos - currentDragPos;
            float dragDistance = Mathf.Clamp(dragVector.magnitude, 0f, maxDragDistance);

            if (dragDistance >= minDragDistance)
            {
                Vector2 launchDirection = dragVector.normalized;
                Vector2 launchVelocity = launchDirection * (dragDistance * forceMultiplier);

                if (projectile != null)
                {
                    projectile.Launch(launchVelocity);
                    GameManager.Instance.SetState(GameState.Launched);
                }
            }
            else
            {
                // Insufficient pull, cancel aim and restore ball to pedestal
                if (projectile != null)
                {
                    projectile.ResetProjectile();
                }
                GameManager.Instance.SetState(GameState.Ready);
            }
        }

        public void CancelAim()
        {
            isDragging = false;
            if (projectile != null)
            {
                projectile.ResetVisualAimCharge();
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Aiming)
                {
                    projectile.ResetProjectile();
                }
            }
            if (aimLineRenderer != null) aimLineRenderer.enabled = false;
            if (trajectoryPreview != null) trajectoryPreview.HideTrajectory();
        }

        public void SetProjectile(ProjectileController newProjectile)
        {
            projectile = newProjectile;
        }

        public Transform LaunchAnchor => launchAnchor;
        public float MaxDragDistance => maxDragDistance;
        public float CurrentPowerRatio => isDragging ? Mathf.Clamp01((dragStartPos - currentDragPos).magnitude / maxDragDistance) : 0f;
    }
}


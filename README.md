# 2D Physics-Based Projectile Game

A complete, playable, stable 2D physics-based projectile game developed in Unity (Unity 6000.3.8f1, Universal Render Pipeline 2D) based on the technical assignment requirements from Keb3Task Pvt. Ltd.

---

## 1. Game Overview
The player controls a projectile ball launched from an elevated starting pedestal using an intuitive drag-to-aim mechanism. When dragged, the projectile ball remains strictly static in its place on the pedestal while an aiming pull-indicator stretches backward, and the game calculates and renders a real-time **Trajectory Preview** showing the predicted flight path. Upon release, the projectile is launched directly from the pedestal into free 2D physics flight.

When the ball reaches the curved ramp, the game validates the incoming physical parameters (speed, angle of impact, and surface alignment):
- **Correct Shot:** The ball smoothly traverses the curved surface, slides down the ramp, and safely lands in the goal area at the bottom, triggering a **Success** state.
- **Incorrect Shot (wrong angle, excessive velocity, or missing the surface):** The ball fractures, flashes red, or falls out of bounds, triggering a **Failure** state.
- **Retry Mechanism:** An on-screen Retry button seamlessly resets the projectile and gameplay state without reloading the scene.
- **Level Progression:** Features two distinct playable surface setups (Level 1 and Level 2) with different curve geometry and difficulty tolerances.

---

## 2. Controls & Platform Compatibility
- **Mouse (Unity Editor / PC):** Click and drag backwards on or near the projectile. Release the mouse button to launch.
- **Touch (Android Mobile):** Touch and drag backwards on the projectile. Release your finger to launch.
- **No Keyboard Controls Required:** All actions—aiming, launching, retrying, and advancing levels—are fully operable via pointer/touch input and on-screen UI buttons.

---

## 3. Implementation Approach

### A. Core Architecture
The codebase is structured into modular, decoupled components following single responsibility principles:
- `GameState.cs`: Enum defining `Ready`, `Aiming`, `Launched`, `FollowingSurface`, `Success`, and `Failure`.
- `GameManager.cs`: Coordinates state transitions, out-of-bounds boundary checks, and flight timeouts.
- `ProjectileLauncher.cs`: Handles touch/mouse input, drag clamping, launch force calculation, and coordinates with the Trajectory Preview.
- `TrajectoryPreview.cs`: Real-time parabolic physics projection with segment circle-casting.
- `ProjectileController.cs`: Manages 2D Rigidbody dynamics, squash & stretch animation, trail rendering, and reset logic.
- `CurvedSurface.cs`: Evaluates Bezier splines to build `EdgeCollider2D` and `LineRenderer` visual geometry, validates impact speed and angle, and executes smooth jitter-free curve sliding.
- `GoalTrigger.cs`: Detects when the projectile reaches the goal catch box.
- `MotionFeedback.cs`: Lightweight motion feedback routines (launch stretch, impact pulse, curve sliding trail, and failure flash).
- `LevelManager.cs`: Controls Level 1 and Level 2 setups and level switching.
- `GameUIManager.cs`: Responsive HUD showing current level, instructions, real-time launch power meter, and success/failure dialogue panels.

---

## 4. How Trajectory Preview Works
The Trajectory Preview is the **only optional enhancement** implemented in this project:
1. While dragging backwards, the launcher calculates the launch velocity vector:
   $$\vec{v}_0 = \hat{d}_{launch} \cdot (d_{drag} \cdot k_{force})$$
2. The `TrajectoryPreview` script samples discrete future positions at time intervals $\Delta t = 0.04s$ using the kinematic equation:
   $$\vec{p}(t) = \vec{p}_0 + \vec{v}_0 t + \frac{1}{2} \vec{g} t^2$$
3. Between consecutive predicted positions, a 2D circle cast detects physical obstacles (like the ramp or boundaries), clipping the line at the exact predicted point of impact.
4. Rendered with an unlit glowing gradient `LineRenderer` that updates dynamically during drag and immediately hides upon release.

---

## 5. Curved Surface Interaction & Validation Logic

### A. Physics Interaction
The curved ramp is generated using quadratic and cubic Bezier splines converted into an `EdgeCollider2D`.

### B. Validation & Transition Criteria
When `OnCollisionEnter2D` occurs on the curved ramp:
1. **Pre-Collision Velocity Measurement:**
   Incoming velocity is sampled immediately prior to physics collision solving via `PreCollisionVelocity` / `collision.relativeVelocity` rather than post-collision arrested velocities.
2. **Impact Speed Check:**
   $$s = \|\vec{v}_{incoming}\|$$
   Must fall within $[v_{min}, v_{max}]$ ($1.0 \le s \le 25.0$ m/s for Level 1, $1.5 \le s \le 25.0$ m/s for Level 2).
3. **Acute Tangent Angle Deviation Check:**
   $$\theta = \min\left(\arccos\left(\frac{\vec{v} \cdot \hat{t}}{\|\vec{v}\| \|\hat{t}\|}\right), 180^\circ - \arccos\left(\frac{\vec{v} \cdot \hat{t}}{\|\vec{v}\| \|\hat{t}\|}\right)\right) \le \theta_{max}$$
   ($\le 75^\circ$ for Level 1, $\le 65^\circ$ for Level 2).
4. **100% Pure Unity Built-in Rigidbody2D Simulation:**
   - **Zero Velocity Overriding:** The ball remains 100% driven by Unity's native `Rigidbody2D` dynamic physics. The script does not artificially override velocities upon impact, allowing Unity's Box2D physics solver to naturally calculate all contact normal impulses, restitution bounces, friction-generated torque, and gravity!
   - **Realistic Tennis/Rubber Ball Feel:** Equipped with a tuned `PhysicsMaterial2D` (bounciness $0.55$, friction $0.4$) on both the ball's `CircleCollider2D` and the ramp's `EdgeCollider2D`. The ball produces satisfying, realistic physical bounces on impact, naturally transitions into rotational rolling along the outer face of the curve, and launches cleanly into the basket.
   - **Continuous Collision Precision:** Uses `CollisionDetectionMode2D.Continuous` on the projectile Rigidbody2D, guaranteeing that high-velocity impacts never tunnel or clip through the curve edges.
   - **Non-Instant Failure:** Imperfect shots never freeze mid-air or instantly fail on the ramp; the ball bounces naturally with 2D physics restitution, giving the player full time to watch the trajectory play out. Failure is only triggered if the ball drops below the screen (`lowerBoundaryY = -6.0`) or exceeds the flight timeout ($12.0$s).

### C. Target Catch Basket & Rest Detection
The target goal is structured as a physical U-shaped catch basket:
- **Solid Physical Colliders:** `Bottom`, `LeftWall`, and `RightWall` are equipped with real `BoxCollider2D` components using a custom `PhysicsMaterial2D` (friction $0.8$, bounciness $0.15$) to catch the ball physically, absorb kinetic energy, and prevent it from bouncing out or clipping through the floor.
- **Rest Detection Logic:** An interior trigger volume monitors the projectile once inside the cup. Victory is confirmed when the ball's velocity drops below the rest threshold ($\le 0.35$ m/s for $0.25$s, or contained $> 1.2$s), ensuring the player sees the ball physically come to rest inside the basket before the win popup appears.

---

## 6. Two Distinct Playable Surface Setups

1. **Level 1 (Rebuilt from User Hand-Drawn Sketch):**
   - **Geometry:** Rebuilt to match the user's architectural sketch using clean rectangular sprites with crisp white borders:
     - **Elevated Launcher:** Positioned on the left at `(-6.5, 2.0)` atop a vertical stand.
     - **Left Pillar (`Block_LeftPillar`):** Solid charcoal block with white borders at `(-3.0, -2.55)` (scale: $3.0 \times 5.1$).
     - **Upward Ascending Ramp (`Ramp_Upward`):** Straight ascending ramp rising at $42.09^\circ$ to a sharp peak at `(1.6, 2.8)`, with `CurvedSurface` and `RampPhysicsMaterial` (bounciness $0.55$, friction $0.4$).
     - **Peak Drop & Vertical Chute:** Vertical drop wall (`Wall_ChuteLeft`) dropping to the floor, forming a $1.7$-unit channel.
     - **Right Backboard Ramp (`Ramp_RightBackboard`):** Sloping up-right at $34.92^\circ$ towards the upper-right corner to catch overshoots and funnel them down into the chute.
     - **Target Basket (`Goal_Level1`):** U-shaped neon green basket nestled inside the chute at `(2.45, -1.2)` with solid walls and floor (`BasketWallMaterial`) and `GoalTrigger` rest detection.
   - **Intended Trajectory:** Launch along the central slope to roll over the peak into the chute, or lob high to bounce off the right backboard into the green basket.
2. **Level 2 (Challenging S-Curve Setup):**
   - **Geometry:** Multi-arc cubic S-curve ramp situated higher and further back, leading into an offset target box.
   - **Tolerances:** Tighter angle deviation ($65^\circ$) and requires higher initial launch force to crest the entry lip.
   - **Progression:** Accessible via the "Next Level" button upon completing Level 1.

---

## 7. Motion Feedback Implemented (All 4 Types)
1. **Launch Feedback:** Squash & stretch scale deformation along the launch vector upon release, accompanied by an aiming line indicator.
2. **Impact Feedback:** Visual scale pulse (1.25x bounce pulse) when contacting the curved surface.
3. **Slide / Success Feedback:** Glowing trail renderer and color tint shift (vibrant mint green) during surface traversal; celebratory particle burst and banner when landing in the goal.
4. **Failure Feedback:** Triple red blink and shrink disintegrate effect on invalid impact or out-of-bounds fall.

---

## 8. Quality & Verification
- All automated gameplay tests pass with **8/8 green checks** in Unity batchmode (`AutomatedTestRunner.cs`).
- Verified zero compilation warnings or errors in Unity 6000.3.8f1.
- Clean scene setup automated via `SceneSetupAutomation.cs` (no manual Inspector configuration needed).


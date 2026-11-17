namespace ProjectileGame.Core
{
    public enum GameState
    {
        Ready,           // Waiting for player input
        Aiming,          // Player is dragging to aim
        Launched,        // Ball is in free physics flight
        FollowingSurface,// Ball has contacted and validated the curve
        Success,         // Ball reached the goal
        Failure          // Missed, invalid angle, or dropped out of bounds
    }
}


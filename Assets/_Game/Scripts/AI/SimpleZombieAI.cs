using UnityEngine;
using UnityEngine.AI;

namespace ProtocoloZero.AI
{
    /// <summary>
    /// Minimal Phase 06 zombie state machine: PATROL waypoints with a short
    /// wait at each arrival, interval-based distance detection, NavMesh CHASE
    /// at a distinct speed, and RETURN to the nearest patrol point after the
    /// loss threshold. No health, damage or combat (Phase 07 stays out).
    /// Movement uses the NavMeshAgent only; the transform is never teleported
    /// except for one bounded startup snap onto the baked NavMesh.
    /// The agent starts disabled in the prefab on purpose: the first Tick
    /// enables it exactly once, and only after sampling real NavMesh geometry
    /// (the bootstrap at order -200 builds it in Awake before any AI runs).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("ProtocoloZero/AI/Simple Zombie AI")]
    public sealed class SimpleZombieAI : MonoBehaviour
    {
        public enum ZombieState
        {
            Patrol,
            Chase,
            Returning
        }

        [Header("Targets")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform[] waypoints = new Transform[0];

        [Header("Detection (meters, horizontal)")]
        [SerializeField, Min(0.5f)] private float detectRange = 8f;
        [SerializeField, Min(0.5f)] private float loseRange = 12f;
        [SerializeField, Min(0.05f)] private float decisionInterval = 0.25f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float patrolSpeed = 1.5f;
        [SerializeField, Min(0f)] private float chaseSpeed = 2.5f;
        [SerializeField, Min(0f)] private float waypointWaitTime = 1.5f;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.8f;
        [SerializeField, Min(0.1f)] private float sampleRange = 2f;
        [SerializeField, Min(0.1f)] private float startWarpRange = 5f;
        [SerializeField] private bool returnToPatrol = true;

        [Header("Startup ground snap")]
        [Tooltip("Maximum allowed vertical difference between the authored spawn and the sampled " +
            "NavMesh point. Rejects startup warps onto disconnected shelf-top islands (measured " +
            "shelf hit y=4.15 vs floor y=0.08). Only a small horizontal floor snap is permitted.")]
        [SerializeField, Min(0.05f)] private float maxStartupHeightDelta = 0.5f;

        [Header("Diagnostics")]
        [Tooltip("When enabled, logs state/on-mesh/path/destination/player-distance/velocity " +
            "at most once per second and on every state change. Off by default; the " +
            "LogNavigationStatus context menu works regardless.")]
        [SerializeField] private bool debugLogNavigation;

        public ZombieState CurrentState { get; private set; } = ZombieState.Patrol;
        public int CurrentWaypointIndex { get; private set; }
        public bool IsNavigationReady { get; private set; }
        public bool HasValidDestination { get; private set; }
        public int DestinationRequestCount { get; private set; }
        public Transform PlayerTarget => playerTarget;
        public Transform[] Waypoints => waypoints;
        public float DetectRange => detectRange;
        public float LoseRange => loseRange;
        public float DecisionInterval => decisionInterval;
        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float WaypointWaitTime => waypointWaitTime;
        public float StoppingDistance => stoppingDistance;
        public bool ReturnToPatrol => returnToPatrol;
        public float MaxStartupHeightDelta => maxStartupHeightDelta;

        private NavMeshAgent agent;
        private NavMeshPath cachedPath;
        private Vector3 acceptedDestination;
        private NavMeshQueryFilter navFilter;
        private bool navFilterBuilt;
        private ZombieState lastLoggedState;
        private float diagClock;
        private float decisionClock;
        private float waitClock;
        private float rescanClock;
        private bool waitingAtWaypoint;
        private bool warnedNotReady;
        private bool startupWarpDone;

        private const float RescanDelay = 1f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ZombieDecision.ValidateRanges(ref detectRange, ref loseRange);
        }

        private void OnEnable()
        {
            CurrentState = ZombieState.Patrol;
            decisionClock = 0f;
            waitClock = 0f;
            rescanClock = 0f;
            waitingAtWaypoint = false;
            warnedNotReady = false;
            startupWarpDone = false;
            HasValidDestination = false;
            DestinationRequestCount = 0;
            IsNavigationReady = false;
            navFilterBuilt = false;
            lastLoggedState = CurrentState;
            diagClock = 0f;
        }

        private void OnDisable()
        {
            StopAgent();
            CurrentState = ZombieState.Patrol;
            HasValidDestination = false;
            IsNavigationReady = false;
        }

        private void OnValidate()
        {
            ZombieDecision.ValidateRanges(ref detectRange, ref loseRange);
            if (decisionInterval < ZombieDecision.MinDecisionInterval)
                decisionInterval = ZombieDecision.MinDecisionInterval;
            if (patrolSpeed < 0f)
                patrolSpeed = 0f;
            if (chaseSpeed < 0f)
                chaseSpeed = 0f;
            if (waypointWaitTime < 0f)
                waypointWaitTime = 0f;
            if (stoppingDistance < 0f)
                stoppingDistance = 0f;
            if (sampleRange < 0.1f)
                sampleRange = 0.1f;
            if (startWarpRange < 0.1f)
                startWarpRange = 0.1f;
            if (maxStartupHeightDelta < 0.05f)
                maxStartupHeightDelta = 0.05f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Public single-step entry so EditMode tests can drive the brain
        /// without a NavMesh: with no baked surface the call repairs nothing
        /// and throws nothing, it just reports not-ready.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime < 0f)
                deltaTime = 0f;
            if (!EnsureNavigationReady())
                return;

            decisionClock += deltaTime;
            bool senseNow = decisionClock >= decisionInterval;
            if (senseNow)
                decisionClock = 0f;

            float distance = senseNow ? EvaluateDistanceToPlayer() : -1f;

            switch (CurrentState)
            {
                case ZombieState.Patrol:
                    UpdatePatrol(deltaTime, senseNow, distance);
                    break;
                case ZombieState.Chase:
                    UpdateChase(senseNow, distance);
                    break;
                case ZombieState.Returning:
                    UpdateReturning(deltaTime, senseNow, distance);
                    break;
            }

            MaybeLogNavigation(null);
        }

        /// <summary>
        /// Horizontal meters to the player, or -1 when no target is assigned.
        /// </summary>
        public float EvaluateDistanceToPlayer()
        {
            if (playerTarget == null)
                return -1f;
            return ZombieDecision.HorizontalDistance(transform.position, playerTarget.position);
        }

        private void UpdatePatrol(float deltaTime, bool senseNow, float distance)
        {
            if (senseNow && distance >= 0f && ZombieDecision.ShouldStartChase(distance, detectRange))
            {
                EnterChase();
                return;
            }

            if (waypoints == null || waypoints.Length == 0)
            {
                StopAgent();
                return;
            }

            if (waitingAtWaypoint)
            {
                waitClock -= deltaTime;
                if (waitClock <= 0f)
                {
                    waitingAtWaypoint = false;
                    CurrentWaypointIndex = ZombieDecision.NextWaypointIndex(CurrentWaypointIndex, waypoints.Length);
                    if (!RescanDue(deltaTime))
                        return;
                    TryBeginMoveToWaypoint(CurrentWaypointIndex, patrolSpeed);
                }
                return;
            }

            // A pending path is work in progress: never cancel it with a new
            // SetDestination, and do not burn the rescan throttle waiting.
            if (IsPathPending())
                return;

            if (IsPathInvalidOrPartialIdle())
            {
                if (!RescanDue(deltaTime))
                    return;
                ScanForReachableWaypoint(patrolSpeed);
                return;
            }

            if (!HasValidDestination)
            {
                if (!RescanDue(deltaTime))
                    return;
                ScanFromCurrentInclusive(patrolSpeed);
                return;
            }

            if (HasArrived())
            {
                waitingAtWaypoint = true;
                waitClock = waypointWaitTime;
                StopAgent();
                return;
            }

            if (!HasActivePath())
            {
                if (!RescanDue(deltaTime))
                    return;
                TryBeginMoveToWaypoint(CurrentWaypointIndex, patrolSpeed);
            }
        }

        private void UpdateChase(bool senseNow, float distance)
        {
            if (senseNow)
            {
                if (distance < 0f || ZombieDecision.ShouldLoseTarget(distance, loseRange))
                {
                    EnterReturn();
                    return;
                }
                MoveToPosition(playerTarget.position, chaseSpeed);
            }
        }

        private void UpdateReturning(float deltaTime, bool senseNow, float distance)
        {
            if (senseNow && distance >= 0f && ZombieDecision.ShouldStartChase(distance, detectRange))
            {
                EnterChase();
                return;
            }

            if (waypoints == null || waypoints.Length == 0)
            {
                CurrentState = ZombieState.Patrol;
                StopAgent();
                return;
            }

            // Same pending-path contract as patrol: never cancel a path that
            // is still computing.
            if (IsPathPending())
                return;

            if (IsPathInvalidOrPartialIdle() || !HasValidDestination)
            {
                if (!RescanDue(deltaTime))
                    return;
                ScanForReachableWaypoint(patrolSpeed);
                return;
            }

            if (HasArrived())
            {
                CurrentState = ZombieState.Patrol;
                waitingAtWaypoint = true;
                waitClock = waypointWaitTime;
                StopAgent();
            }
        }

        private void EnterChase()
        {
            CurrentState = ZombieState.Chase;
            waitingAtWaypoint = false;
            if (playerTarget != null)
                MoveToPosition(playerTarget.position, chaseSpeed);
        }

        private void EnterReturn()
        {
            waitingAtWaypoint = false;
            if (!returnToPatrol || waypoints == null || waypoints.Length == 0)
            {
                CurrentState = ZombieState.Patrol;
                StopAgent();
                return;
            }

            CurrentState = ZombieState.Returning;
            int nearest = FindNearestWaypointIndex();
            if (nearest < 0)
            {
                HasValidDestination = false;
                StopAgent();
                return;
            }
            CurrentWaypointIndex = nearest;
            if (!TryBeginMoveToWaypoint(nearest, patrolSpeed))
                ScanForReachableWaypoint(patrolSpeed);
        }

        private int FindNearestWaypointIndex()
        {
            if (waypoints == null || waypoints.Length == 0)
                return -1;
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null)
                    continue;
                float candidate = ZombieDecision.HorizontalDistance(transform.position, waypoints[i].position);
                if (candidate < bestDistance)
                {
                    bestDistance = candidate;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// Issues one destination for a single waypoint. The target is sampled
        /// with this agent's own type filter and accepted only when a full
        /// NavMesh route from the agent's current position exists
        /// (<see cref="NavMeshPathStatus.PathComplete"/>). Partial or invalid
        /// routes are rejected without touching the agent's current path, so a
        /// zombie on a disconnected island never reports a valid destination.
        /// Never recurses: callers own the bounded retry loops.
        /// </summary>
        private bool TryBeginMoveToWaypoint(int index, float speed)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length)
                return false;
            Transform point = waypoints[index];
            if (point == null)
                return false;
            BuildNavFilter();
            if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, sampleRange, navFilter))
                return false;
            return SafeSetDestination(hit.position, speed);
        }

        /// <summary>
        /// Chase steering toward the player's floor position. The target is
        /// re-sampled onto this agent's NavMesh type and only accepted on a
        /// full path, so the zombie never walks through walls or hops islands.
        /// Called at most once per decision interval (plus a no-path refill).
        /// </summary>
        private void MoveToPosition(Vector3 destination, float speed)
        {
            BuildNavFilter();
            Vector3 target = destination;
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, sampleRange, navFilter))
                target = hit.position;
            SafeSetDestination(target, speed);
        }

        /// <summary>
        /// Bounded iterative scan: tries each waypoint at most once, so an
        /// all-null or all-unsampleable list stops the agent and schedules a
        /// later retry instead of recursing into a stack overflow.
        /// </summary>
        private bool ScanForReachableWaypoint(float speed)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                HasValidDestination = false;
                StopAgent();
                return false;
            }
            for (int visited = 0; visited < waypoints.Length; visited++)
            {
                CurrentWaypointIndex = ZombieDecision.NextWaypointIndex(CurrentWaypointIndex, waypoints.Length);
                if (TryBeginMoveToWaypoint(CurrentWaypointIndex, speed))
                    return true;
            }
            HasValidDestination = false;
            StopAgent();
            return false;
        }

        private bool ScanFromCurrentInclusive(float speed)
        {
            if (TryBeginMoveToWaypoint(CurrentWaypointIndex, speed))
                return true;
            return ScanForReachableWaypoint(speed);
        }

        /// <summary>
        /// Throttles NavMesh re-queries so retries never become a per-frame
        /// tight loop. First call after a reset is always due.
        /// </summary>
        private bool RescanDue(float deltaTime)
        {
            if (rescanClock > 0f)
            {
                rescanClock -= deltaTime;
                return false;
            }
            rescanClock = RescanDelay;
            return true;
        }

        private bool EnsureNavigationReady()
        {
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                return false;

            BuildNavFilter();

            // Intentional enable-after-real-NavMesh: the prefab ships the
            // agent disabled; it is enabled exactly once below, only after a
            // real baked sample on THIS agent type exists at floor level.
            // A sample far above the authored spawn (e.g. a shelf-top island
            // 4 m up) or inside solid geometry is rejected instead of warped
            // to: the zombie waits for valid ground and never teleports up.
            if (!agent.enabled)
            {
                if (!TrySampleStartupGround(transform.position, out NavMeshHit hit))
                {
                    WarnNotReadyOnce();
                    return false;
                }
                agent.enabled = true;
                if (!startupWarpDone)
                {
                    startupWarpDone = true;
                    agent.Warp(hit.position);
                }
            }

            if (agent.isOnNavMesh)
            {
                IsNavigationReady = true;
                return true;
            }

            // One bounded startup snap only; never teleport during play, and
            // never snap across a vertical gap onto another platform.
            if (!startupWarpDone
                && TrySampleStartupGround(transform.position, out NavMeshHit snap))
            {
                startupWarpDone = true;
                agent.Warp(snap.position);
                if (agent.isOnNavMesh)
                {
                    IsNavigationReady = true;
                    return true;
                }
            }

            WarnNotReadyOnce();
            return false;
        }

        private void BuildNavFilter()
        {
            if (navFilterBuilt || agent == null)
                return;
            navFilter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = agent.areaMask
            };
            navFilterBuilt = true;
        }

        /// <summary>
        /// Samples the NavMesh with this agent's own type filter and accepts
        /// the hit only when it is a genuine floor snap: the vertical gap to
        /// the authored spawn stays within <see cref="maxStartupHeightDelta"/>
        /// and a standing body capsule at the hit point is free of solid
        /// geometry (own trigger capsule ignored). Returns false for
        /// shelf-top islands and obstructed spawns without moving anything.
        /// </summary>
        private bool TrySampleStartupGround(Vector3 spawn, out NavMeshHit hit)
        {
            hit = default;
            if (!NavMesh.SamplePosition(spawn, out hit, startWarpRange, navFilter))
                return false;
            float verticalGap = Mathf.Abs(hit.position.y - spawn.y);
            if (verticalGap > maxStartupHeightDelta)
            {
                WarnHeightRejectedOnce(spawn, hit.position, verticalGap);
                return false;
            }
            if (IsBodyBlockedAt(hit.position))
                return false;
            return true;
        }

        private bool IsBodyBlockedAt(Vector3 feet)
        {
            float radius = agent != null ? agent.radius : 0.35f;
            float height = agent != null ? agent.height : 1.8f;
            // The bottom sphere starts a skin width above the feet: resting
            // contact with the floor plane itself is standing, not blocked.
            Vector3 bottom = feet + Vector3.up * (radius + 0.05f);
            Vector3 top = feet + Vector3.up * Mathf.Max(height - radius, radius * 2f);
            foreach (Collider collider in Physics.OverlapCapsule(bottom, top, radius,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.IsChildOf(transform)
                    || (playerTarget != null && collider.transform.IsChildOf(playerTarget)))
                    continue;
                return true;
            }
            return false;
        }

        private void WarnNotReadyOnce()
        {
            if (warnedNotReady)
                return;
            warnedNotReady = true;
            Debug.LogWarning(
                $"[SimpleZombieAI] '{name}' is not on a NavMesh yet; waiting for the NavMesh surface.",
                this);
        }

        private void WarnHeightRejectedOnce(Vector3 spawn, Vector3 hit, float gap)
        {
            if (warnedNotReady)
                return;
            warnedNotReady = true;
            Debug.LogWarning(
                $"[SimpleZombieAI] '{name}' startup NavMesh hit at {hit} is {gap:F2} m above " +
                $"spawn {spawn} (max {maxStartupHeightDelta:F2} m); staying off-mesh, no upward warp.",
                this);
        }

        /// <summary>
        /// Requests a destination only after proving it reachable: a cached
        /// path is calculated with this agent's own type filter from the
        /// agent's current position, and the destination is accepted solely on
        /// <see cref="NavMeshPathStatus.PathComplete"/>. A partial or invalid
        /// route clears the valid flag instead of reporting success, so
        /// <see cref="HasValidDestination"/> never claims an island-hop. A
        /// resolved partial/invalid path observed later is cleared the same
        /// way by the rescan guards, regardless of hasPath.
        /// </summary>
        private bool SafeSetDestination(Vector3 destination, float speed)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return false;
            BuildNavFilter();
            if (cachedPath == null)
                cachedPath = new NavMeshPath();
            cachedPath.ClearCorners();
            Vector3 from = agent.isOnNavMesh ? transform.position : destination;
            if (!NavMesh.CalculatePath(from, destination, navFilter, cachedPath)
                || cachedPath.status != NavMeshPathStatus.PathComplete)
            {
                StopAgent();
                MaybeLogNavigation("reject-partial");
                return false;
            }
            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
            if (agent.isStopped)
                agent.isStopped = false;
            DestinationRequestCount++;
            if (!agent.SetPath(cachedPath))
            {
                StopAgent();
                return false;
            }
            HasValidDestination = true;
            acceptedDestination = destination;
            MaybeLogNavigation("accept");
            return true;
        }

        private bool AgentUsable()
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        private bool IsPathPending()
        {
            return AgentUsable() && agent.pathPending;
        }

        /// <summary>
        /// A pending path still counts as work in progress, so the brain never
        /// resets it with a per-frame SetDestination.
        /// </summary>
        private bool HasActivePath()
        {
            if (!AgentUsable())
                return false;
            return agent.hasPath || agent.pathPending;
        }

        private bool IsPathInvalidOrPartialIdle()
        {
            if (!AgentUsable() || agent.pathPending || !HasValidDestination)
                return false;
            // Any resolved partial or invalid route is unusable, whether or
            // not the agent still holds path corners: the destination was
            // never truly reachable from this island.
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                HasValidDestination = false;
                return true;
            }
            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                HasValidDestination = false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Manual navigation snapshot for the inspector (right-click the
        /// component header). Always available; the periodic debug log stays
        /// behind <see cref="debugLogNavigation"/> and defaults to off.
        /// </summary>
        [ContextMenu("LogNavigationStatus")]
        public void LogNavigationStatus()
        {
            string agentInfo = !AgentUsable()
                ? $"agent unavailable enabled={agent != null && agent.enabled}"
                : $"enabled={agent.enabled} onMesh={agent.isOnNavMesh} " +
                  $"pending={agent.pathPending} status={agent.pathStatus} " +
                  $"hasPath={agent.hasPath} vel={agent.velocity}";
            float distance = EvaluateDistanceToPlayer();
            Vector3 dest = AgentUsable() && agent.hasPath ? agent.destination : transform.position;
            Debug.Log(
                $"[SimpleZombieAI] '{name}' state={CurrentState} ready={IsNavigationReady} " +
                $"valid={HasValidDestination} req={DestinationRequestCount} wpIdx={CurrentWaypointIndex} " +
                $"dest={dest} playerDist={distance:F2} {agentInfo}",
                this);
        }

        private void MaybeLogNavigation(string reason)
        {
            if (!debugLogNavigation)
            {
                lastLoggedState = CurrentState;
                return;
            }
            bool stateChanged = CurrentState != lastLoggedState;
            diagClock += reason == null ? Time.deltaTime : 0f;
            if (!stateChanged && reason == null && diagClock < 1f)
                return;
            if (reason == null)
                diagClock = 0f;
            lastLoggedState = CurrentState;
            LogNavigationStatus();
        }

        /// <summary>
        /// True arrival only: a valid assigned destination, a finished,
        /// non-partial, non-invalid path, and remaining distance within the
        /// stopping radius. Missing paths never count as visited.
        /// </summary>
        private bool HasArrived()
        {
            if (!HasValidDestination || !AgentUsable())
                return false;
            if (agent.pathPending)
                return false;
            if (!agent.hasPath)
                return cachedPath != null && cachedPath.status == NavMeshPathStatus.PathComplete
                    && Vector3.Distance(transform.position, acceptedDestination)
                        <= Mathf.Max(stoppingDistance, 0.05f);
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid
                || agent.pathStatus == NavMeshPathStatus.PathPartial)
                return false;
            return agent.remainingDistance <= Mathf.Max(stoppingDistance, 0.05f);
        }

        private void StopAgent()
        {
            HasValidDestination = false;
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
                return;
            if (!agent.isStopped)
                agent.isStopped = true;
            if (agent.hasPath)
                agent.ResetPath();
        }
    }
}

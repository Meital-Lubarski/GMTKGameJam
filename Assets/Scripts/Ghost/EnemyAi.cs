using System.Collections.Generic;
using General;
using UnityEngine;
using UnityEngine.AI;

namespace Ghost
{
    public class EnemyAi : MonoBehaviour, IFlashlightTarget
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform player;

        [Header("Visibility")]
        [Tooltip(
            "Renderers kept hidden until the flashlight beam is on the ghost. " +
            "Leave empty to use every renderer found in the children."
        )]
        [SerializeField] private Renderer[] visualRenderers;

        [Header("Layers")]
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private LayerMask whatIsPlayer;

        [Header("Stats")]
        [SerializeField] private float health;

        // Speed Management
        [Header("Speed Settings")]
        [SerializeField] private float initialSpeed = 2f;
        [SerializeField] private float maxSpeed = 5f; // Should be the player's max speed
        [SerializeField] private float speedIncreaseRate = 0.5f; // Amount to increase speed by
        [SerializeField] private float speedIncreaseInterval = 5f; // How often to increase speed (in seconds)
        private float _speedTimer;

        // Patroling
        [Header("Patroling")]
        [SerializeField] private float walkPointRange;
        private Vector3 _walkPoint;
        private bool _walkPointSet;

        // States
        [Header("States")]
        [SerializeField] private float sightRange;
        [Tooltip("Also acts as the catch radius: staying this close catches the player.")]
        [SerializeField] private float attackRange;
        private bool _playerInSightRange;
        private bool _playerInAttackRange;

        // Catching
        [Header("Catching")]
        [Tooltip(
            "How long the ghost has to keep the player inside the catch radius " +
            "before she catches him. The player spends that time being closed in on, " +
            "and gets away by leaving the radius or stunning her."
        )]
        [SerializeField] private float catchTime = 3f;
        private float _catchTimer;
        private bool _hasCaughtPlayer;

        // Set while the ghost is on the player but has not caught him yet.
        private bool _isApproaching;

        // Stun
        [Header("Stun")]
        [Tooltip(
            "How long the ghost keeps going after the flashlight catches her, " +
            "before the stun actually takes hold. Keeps her walking in the beam " +
            "for a moment instead of freezing the instant she is lit."
        )]
        [SerializeField] private float stunDelay = 2f;

        private float _stunTimer;

        // A stun that was triggered but has not taken hold yet.
        private float _pendingStunDelay;
        private float _pendingStunDuration;

        // Whether the flashlight beam is currently on her.
        private bool _isIlluminated;

        public bool IsStunned => _stunTimer > 0f;
        private bool HasPendingStun => _pendingStunDuration > 0f;

        /// <summary>
        /// How much of the stun is left, in seconds, and zero when she is not
        /// stunned. Read by anything that shows the stun to the player, so what
        /// is on screen cannot drift away from the stun itself.
        /// </summary>
        public float StunTimeRemaining => Mathf.Max(0f, _stunTimer);

        /// <summary>
        /// Whether the flashlight is on her, and so whether she is being drawn
        /// at all. Anything hanging off her has to come and go with it.
        /// </summary>
        public bool IsIlluminated => _isIlluminated;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();

            if (visualRenderers == null || visualRenderers.Length == 0)
                visualRenderers = CollectOwnRenderers();

            agent.speed = initialSpeed;
            _walkPointSet = false;

            // The ghost stays unseen until the flashlight beam finds her.
            SetVisualsVisible(false);
        }

        private void Update()
        {
            // Once the player is caught the run is over, so stop all behaviour.
            if (_hasCaughtPlayer) return;

            /*
             * A stunned ghost stops moving and cannot catch the player, but she
             * keeps turning towards him: the visual is a flat sprite, so it must
             * never be seen from the side.
             */
            if (IsStunned)
            {
                HandleStun();
                return;
            }

            /*
             * The flashlight has caught her but the stun has not landed yet, so
             * she keeps walking and can still catch the player until it does.
             */
            if (HasPendingStun)
            {
                TickPendingStun();

                // The delay just ran out, so let the stun take over from here.
                if (IsStunned) return;
            }

            // Check for sight and attack range
            _playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
            _playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

            if (!_playerInSightRange && !_playerInAttackRange) Patroling();
            if (_playerInSightRange && !_playerInAttackRange) ChasePlayer();
            if (_playerInAttackRange && _playerInSightRange) AttackPlayer();

            HandleApproach();
            HandleSpeedIncrease();
        }

        /// <summary>
        /// The window between reaching the player and catching him. Leaving the
        /// radius ends it with the player safe, and so does a stun; only staying
        /// inside it for the whole of <see cref="catchTime"/> gets him caught.
        /// </summary>
        private void HandleApproach()
        {
            // The player escaped the radius, so he is safe and the ghost goes
            // back to chasing him normally.
            if (!_playerInAttackRange)
            {
                SetApproaching(false);
                return;
            }

            SetApproaching(true);

            _catchTimer += Time.deltaTime;

            if (_catchTimer >= catchTime)
                CatchPlayer();
        }

        /// <summary>
        /// Enters or leaves the closing-in state, announcing it once per change
        /// so the animation only switches on the way in and out.
        /// </summary>
        private void SetApproaching(bool isApproaching)
        {
            if (_isApproaching == isApproaching) return;

            _isApproaching = isApproaching;

            // Every stay inside the radius is timed from scratch, so a player
            // who gets away undoes all the progress towards the catch.
            _catchTimer = 0f;

            if (isApproaching)
                EventManager.OnGhostApproachStarted?.Invoke(catchTime);
            else
                EventManager.OnGhostApproachEnded?.Invoke();
        }

        /// <summary>
        /// Leaves the closing-in state without announcing it, for the two
        /// endings that bring an animation of their own: the stun and the kill.
        /// Announcing here would fire the "back to walking" change as well and
        /// the two would fight over the same frame.
        /// </summary>
        private void DropApproachSilently()
        {
            _isApproaching = false;
            _catchTimer = 0f;
        }

        private void CatchPlayer()
        {
            _hasCaughtPlayer = true;

            // The kill takes over from the closing-in animation.
            DropApproachSilently();

            // Freeze the ghost in place; the listener handles the actual loss/scene.
            if (agent.isOnNavMesh) agent.ResetPath();

            /*
             * The player has to see who caught him, with the flashlight on or
             * off, and see her from the front rather than edge on.
             */
            SetVisualsVisible(true);
            FacePlayer();

            EventManager.OnPlayerCaught?.Invoke();
        }

        /// <summary>
        /// Called by the flashlight while its beam is on the ghost.
        /// Hiding the renderers only affects what is drawn: the ghost keeps
        /// patrolling, chasing and catching while unseen.
        /// </summary>
        public void SetIlluminated(bool isIlluminated)
        {
            /*
             * Once she has caught the player she stays on screen, so the
             * flashlight can no longer hide her by looking away or switching off.
             */
            if (_hasCaughtPlayer) return;

            _isIlluminated = isIlluminated;

            SetVisualsVisible(isIlluminated);
        }

        /// <summary>
        /// Every renderer under the ghost except the ones the stun indicator
        /// owns. Those appear on their own terms, so the flashlight toggle must
        /// not drag them along with the rest of her.
        /// </summary>
        private Renderer[] CollectOwnRenderers()
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            List<Renderer> ownRenderers = new List<Renderer>(allRenderers.Length);

            foreach (Renderer candidate in allRenderers)
            {
                if (candidate.GetComponentInParent<GhostStunIndicator>(true) != null)
                    continue;

                ownRenderers.Add(candidate);
            }

            return ownRenderers.ToArray();
        }

        private void SetVisualsVisible(bool isVisible)
        {
            if (visualRenderers == null) return;

            foreach (Renderer visualRenderer in visualRenderers)
            {
                if (visualRenderer != null)
                    visualRenderer.enabled = isVisible;
            }
        }

        /// <summary>
        /// Called by the flashlight when its beam catches the ghost.
        /// The duration comes from how much battery is left. The stun does not
        /// land straight away: she keeps closing in for <see cref="stunDelay"/>
        /// first, so the player has to light her up early enough to be saved.
        /// </summary>
        public void Stun(float duration)
        {
            if (duration <= 0f || _hasCaughtPlayer) return;

            /*
             * Already frozen. A stun cannot be stacked on top of one that is
             * running, nor refresh it: she has to come out of it first. The
             * beam can never hold her still indefinitely, so every stun costs
             * the player the walk back into range.
             */
            if (IsStunned) return;

            /*
             * Start the delay only for a fresh stun. Being hit again while the
             * delay is running must not push the stun further away, otherwise
             * flicking the beam could hold it off forever.
             */
            if (!HasPendingStun) _pendingStunDelay = stunDelay;

            _pendingStunDuration = Mathf.Max(_pendingStunDuration, duration);

            // With no delay configured the stun takes hold immediately.
            if (_pendingStunDelay <= 0f) BeginStun();
        }

        private void TickPendingStun()
        {
            _pendingStunDelay -= Time.deltaTime;

            if (_pendingStunDelay > 0f) return;

            BeginStun();
        }

        private void BeginStun()
        {
            _stunTimer = _pendingStunDuration;

            _pendingStunDelay = 0f;
            _pendingStunDuration = 0f;

            /*
             * Freezing throws away any progress towards the catch, so the
             * player is safe until she recovers and starts closing in again.
             * The stun animation takes over from the closing-in one by itself,
             * so no end is announced that would fight it.
             */
            DropApproachSilently();

            SetAgentStopped(true);

            EventManager.OnGhostStunned?.Invoke(_stunTimer);
        }

        private void HandleStun()
        {
            _stunTimer -= Time.deltaTime;

            FacePlayer();

            if (_stunTimer > 0f) return;

            _stunTimer = 0f;

            SetAgentStopped(false);

            EventManager.OnGhostStunEnded?.Invoke();
        }

        private void SetAgentStopped(bool isStopped)
        {
            if (agent == null || !agent.isOnNavMesh) return;

            agent.isStopped = isStopped;

            // Drop any leftover momentum so the stun freezes her on the spot
            // instead of letting her glide to a stop.
            if (isStopped) agent.velocity = Vector3.zero;
        }

        /// <summary>
        /// Turns the ghost towards the player without tipping her over.
        /// The rotation is flattened because the ghost is a 2D sprite living
        /// in a 3D scene.
        /// </summary>
        private void FacePlayer()
        {
            if (player == null) return;

            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0f;

            if (directionToPlayer.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        private void HandleSpeedIncrease()
        {
            // If we reached max speed, no need to keep increasing
            if (agent.speed >= maxSpeed) return;

            _speedTimer += Time.deltaTime;
            
            if (_speedTimer >= speedIncreaseInterval)
            {
                // Increases speed, ensuring it doesn't exceed maxSpeed
                agent.speed = Mathf.Min(agent.speed + speedIncreaseRate, maxSpeed);
                _speedTimer = 0f; // Reset timer for the next interval
            }
        }

        private void Patroling()
        {
            if (!_walkPointSet) SearchWalkPoint();

            if (_walkPointSet)
                agent.SetDestination(_walkPoint);

            Vector3 distanceToWalkPoint = transform.position - _walkPoint;

            // Walkpoint reached
            if (distanceToWalkPoint.magnitude < 1f)
                _walkPointSet = false;
        }

        private void SearchWalkPoint()
        {
            // Calculate random point in range
            float randomZ = Random.Range(-walkPointRange, walkPointRange);
            float randomX = Random.Range(-walkPointRange, walkPointRange);

            Vector3 candidate = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

            // Validate the candidate point is actually on the NavMesh (robust regardless of height/ground layer)
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                _walkPoint = hit.position;
                _walkPointSet = true;
            }
        }

        private void ChasePlayer()
        {
            agent.SetDestination(player.position);
        }

        private void AttackPlayer()
        {
            // Make sure enemy doesn't move
            agent.SetDestination(transform.position);

            // The ghost always looks at the player
            FacePlayer();
        }
    }
}
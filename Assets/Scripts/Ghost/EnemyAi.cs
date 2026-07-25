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

        // Attacking
        private bool _alreadyAttacked;

        // States
        [Header("States")]
        [SerializeField] private float sightRange;
        [Tooltip("Also acts as the catch radius: staying this close catches the player.")]
        [SerializeField] private float attackRange;
        private bool _playerInSightRange;
        private bool _playerInAttackRange;

        // Catching
        [Header("Catching")]
        [Tooltip("How long the player must stay inside attack range to be caught.")]
        [SerializeField] private float catchTime = 2f;
        private float _catchTimer;
        private bool _hasCaughtPlayer;

        // Stun
        [Header("Stun")]
        [Tooltip(
            "How long the ghost keeps going after the flashlight catches her, " +
            "before the stun actually takes hold."
        )]
        [SerializeField] private float stunDelay = 2f;

        private float _stunTimer;

        // A stun that was triggered but has not taken hold yet.
        private float _pendingStunDelay;
        private float _pendingStunDuration;

        private bool IsStunned => _stunTimer > 0f;
        private bool HasPendingStun => _pendingStunDuration > 0f;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();

            if (visualRenderers == null || visualRenderers.Length == 0)
                visualRenderers = GetComponentsInChildren<Renderer>(true);

            agent.speed = initialSpeed;
            _alreadyAttacked = false;
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

            HandleCatch();
            HandleSpeedIncrease();
        }

        private void HandleCatch()
        {
            // Reset the moment the player escapes the catch radius, so only
            // continuous contact counts toward being caught.
            if (!_playerInAttackRange)
            {
                _catchTimer = 0f;
                return;
            }

            _catchTimer += Time.deltaTime;

            if (_catchTimer >= catchTime)
                CatchPlayer();
        }

        private void CatchPlayer()
        {
            _hasCaughtPlayer = true;

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

            SetVisualsVisible(isIlluminated);
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
        /// The duration comes from how much battery is left.
        /// </summary>
        public void Stun(float duration)
        {
            if (duration <= 0f || _hasCaughtPlayer) return;

            // Already frozen, so only make sure the longer stun wins.
            if (IsStunned)
            {
                _stunTimer = Mathf.Max(_stunTimer, duration);
                return;
            }

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

            SetAgentStopped(true);

            EventManager.OnGhostStunned?.Invoke(_stunTimer);
        }

        private void HandleStun()
        {
            _stunTimer -= Time.deltaTime;

            FacePlayer();

            // Being stunned interrupts any catch that was in progress.
            _catchTimer = 0f;

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

            if (!_alreadyAttacked)
            {
                // TODO: Attack code here

                _alreadyAttacked = true; 
                // Note: Remember to reset _alreadyAttacked after a certain time so it can attack again
            }
        }
    }
}
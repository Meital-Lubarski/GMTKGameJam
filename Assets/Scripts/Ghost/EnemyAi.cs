using UnityEngine;
using UnityEngine.AI;

namespace Ghost
{
    public class EnemyAi : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform player;
        
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
        [SerializeField] private float attackRange;
        private bool _playerInSightRange;
        private bool _playerInAttackRange;

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();

            agent.speed = initialSpeed;
            _alreadyAttacked = false;
            _walkPointSet = false;
        }

        private void Update()
        {
            // Check for sight and attack range
            _playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
            _playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

            if (!_playerInSightRange && !_playerInAttackRange) Patroling();
            if (_playerInSightRange && !_playerInAttackRange) ChasePlayer();
            if (_playerInAttackRange && _playerInSightRange) AttackPlayer();

            HandleSpeedIncrease();
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

            _walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

            if (Physics.Raycast(_walkPoint, -transform.up, 2f, whatIsGround))
                _walkPointSet = true;
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
            transform.LookAt(player);

            if (!_alreadyAttacked)
            {
                // TODO: Attack code here

                _alreadyAttacked = true; 
                // Note: Remember to reset _alreadyAttacked after a certain time so it can attack again
            }
        }
    }
}
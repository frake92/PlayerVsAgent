using System.Collections;
using UnityEngine;

public class AgentMovement : MonoBehaviour
{
    private ZolaRLAgent agent;
    private Transform player;
    public Transform centerOfThing;
    private Animator animator;
    public Rigidbody2D rb;
    
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private float minMovementThreshold = 0.01f;
    
    private Vector3 lastPosition;
    private float lastPositionTime;
    private bool isDefinitelyStuck = false;
    private float stuckTime = 0f;
    private float lastActualMovementTime = 0f;
    private Vector3 lastFramePosition;
    private int stationaryFrames = 0;

    private float lastRepositionTime = 0f;
    private Vector3 repositionTargetPoint;
    private bool isRepositioning = false;
    
    private const float STUCK_CHECK_INTERVAL = 0.5f;
    private const float STUCK_MOVEMENT_THRESHOLD = 0.05f;
    private const float TOO_FAR_DISTANCE = 4.0f;
    private const float OPTIMAL_DISTANCE_MIN = 0.25f;
    private const float OPTIMAL_DISTANCE_MAX = 1.5f;
    private const float DIMENSIONAL_WAVE_MIN_DISTANCE = 1.5f;

    [SerializeField] private float circlingStrafeSpeed = 1.8f;
    [SerializeField] private float repositionCooldown = 3.0f;
    
    
    public void Initialize(ZolaRLAgent agent, Transform player, Transform centerOfThing)
    {
        this.agent = agent;
        this.player = player;
        this.centerOfThing = centerOfThing;
        this.animator = agent.animation.animator;
        
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        
        ResetState();
    }
    
    public void ResetState()
    {
        lastPosition = transform.position;
        lastPositionTime = Time.time;
        lastFramePosition = transform.position;
        lastActualMovementTime = Time.time;
        stationaryFrames = 0;
        isDefinitelyStuck = false;
        stuckTime = 0f;
        lastRepositionTime = 0f;
        isRepositioning = false;
        rb.linearVelocity = Vector2.zero;
    }
    
    public void UpdateFacingDirection()
    {
        if (player == null || Player.Instance == null) return;
        
        if (Player.Instance.centerOfPlayer.transform.position.x <= centerOfThing.transform.position.x)
        {
            animator.SetFloat("FacingDirectionX", -1);
            animator.GetComponent<SpriteRenderer>().flipX = false;
        }
        else
        {
            animator.SetFloat("FacingDirectionX", 1);
            animator.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void CircleAroundPlayer()
    {
        if (player == null || agent.IsMovementDisabled) return;
        
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        Vector3 perpendicular = new Vector3(-dirToPlayer.y, dirToPlayer.x, 0).normalized;
        
        if (Time.frameCount % 120 < 60)
            perpendicular *= -1;
        
        float currentDistance = Vector3.Distance(transform.position, player.position);
        float targetDistance = Mathf.Clamp(currentDistance, OPTIMAL_DISTANCE_MIN, OPTIMAL_DISTANCE_MAX);
        
        Vector3 idealPos = player.position - dirToPlayer * targetDistance;
        Vector3 circlePos = idealPos + perpendicular * 3f;
        
        Vector2 direction = (circlePos - transform.position).normalized;
        Vector3 beforePosition = transform.position;
        float previousDistance = Vector3.Distance(transform.position, player.position);
        
        UpdateFacingDirection();
        
        rb.linearVelocity = direction * moveSpeed * circlingStrafeSpeed;
        
        StartCoroutine(ProcessMovementResultsNextFrame(beforePosition, previousDistance));
    }
    public void RepositionToOptimalRange()
    {
        if (player == null || agent.IsMovementDisabled) return;
        
        if (Time.time - lastRepositionTime < repositionCooldown && !isRepositioning)
            return;
            
        float currentDistance = Vector3.Distance(transform.position, player.position);
        
        if (currentDistance >= OPTIMAL_DISTANCE_MIN && currentDistance <= OPTIMAL_DISTANCE_MAX && !isRepositioning)
            return;
        
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float optimalDistance = (OPTIMAL_DISTANCE_MIN + OPTIMAL_DISTANCE_MAX) / 2f;
        
        float randomAngle = Random.Range(-45f, 45f);
        Quaternion rotation = Quaternion.Euler(0, 0, randomAngle);
        Vector3 rotatedDir = rotation * dirToPlayer;
        
        if (!isRepositioning)
        {
            repositionTargetPoint = player.position - rotatedDir * optimalDistance;
            isRepositioning = true;
            lastRepositionTime = Time.time;
        }
        
        Vector2 direction = (repositionTargetPoint - transform.position).normalized;
        Vector3 beforePosition = transform.position;
        float previousDistance = Vector3.Distance(transform.position, player.position);
        
        rb.linearVelocity = direction * moveSpeed * 1.2f;
        
        if (Vector3.Distance(transform.position, repositionTargetPoint) < 0.5f)
        {
            isRepositioning = false;
            agent.AddReward(0.05f);
        }
        
        StartCoroutine(ProcessMovementResultsNextFrame(beforePosition, previousDistance));
    }
    
    public void CheckIfStuck()
    {
        float timeSinceLastPositionCheck = Time.time - lastPositionTime;
        if (timeSinceLastPositionCheck > STUCK_CHECK_INTERVAL)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            isDefinitelyStuck = distanceMoved < STUCK_MOVEMENT_THRESHOLD && 
                              animator.GetFloat("Speed") > 0 && 
                              distanceToPlayer > 1.5f;
            
            lastPosition = transform.position;
            lastPositionTime = Time.time;
            
            if (isDefinitelyStuck)
            {
                stuckTime += STUCK_CHECK_INTERVAL;
                if (stuckTime > 1.0f && agent.CanAttack)
                {
                    if (distanceToPlayer > DIMENSIONAL_WAVE_MIN_DISTANCE)
                    {
                        agent.GetComponent<AgentCombat>().PerformAttack(AttackType.DimensionalWave);
                    }
                    else if (distanceToPlayer <= DIMENSIONAL_WAVE_MIN_DISTANCE)
                    {
                        if (Random.value < 0.7f)
                        {
                            agent.GetComponent<AgentCombat>().PerformAttack(AttackType.Melee);
                        }
                        else
                        {
                            agent.GetComponent<AgentCombat>().PerformAttack(AttackType.SpatialEruption);
                        }
                    }
                }
            }
            else
            {
                stuckTime = 0f;
            }
        }
    }
    
    public void AutoMoveWhenFarFromPlayer()
    {
        if (Vector3.Distance(transform.position, player.position) > TOO_FAR_DISTANCE)
        {
            if (Time.frameCount % 3 == 0)
            {
                MoveTowardsPlayer();
            }
        }
    }
    
    public void MoveTowardsPlayer()
    {
        if (player == null || agent.IsMovementDisabled) return;
        
        Vector2 direction = (player.position - transform.position).normalized;
        UpdateFacingDirection();
        
        Vector3 beforePosition = transform.position;
        float previousDistance = Vector3.Distance(transform.position, player.position);
        
        float distanceMultiplier = 1.0f;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer > OPTIMAL_DISTANCE_MAX)
        {
            distanceMultiplier = 1.3f;
        }
        
        rb.linearVelocity = direction * moveSpeed * distanceMultiplier;
        
        StartCoroutine(ProcessMovementResultsNextFrame(beforePosition, previousDistance));
    }
    
    public void MoveAwayFromPlayer()
    {
        if (player == null || agent.IsMovementDisabled) return;
            
        Vector2 direction = (transform.position - player.position).normalized;
        UpdateFacingDirection();
        
        rb.linearVelocity = direction * moveSpeed;
    }
    
    private IEnumerator ProcessMovementResultsNextFrame(Vector3 beforePosition, float previousDistance)
    {
        yield return new WaitForFixedUpdate();
        
        float moveDelta = Vector3.Distance(beforePosition, transform.position);
        
        if (moveDelta > minMovementThreshold)
        {
            lastActualMovementTime = Time.time;
            stationaryFrames = 0;
            
            agent.AddReward(0.01f);
            
            float newDistance = Vector3.Distance(transform.position, player.position);
            
            if (newDistance < previousDistance)
            {
                float approachReward = Mathf.Min(0.05f, (previousDistance - newDistance) * 0.1f);
                agent.AddReward(approachReward);
            }
            
            if ((previousDistance <= OPTIMAL_DISTANCE_MIN && newDistance > OPTIMAL_DISTANCE_MIN && newDistance <= OPTIMAL_DISTANCE_MAX) ||
                (previousDistance > OPTIMAL_DISTANCE_MAX && newDistance <= OPTIMAL_DISTANCE_MAX && newDistance > OPTIMAL_DISTANCE_MIN))
            {
                agent.AddReward(0.03f);
            }
        }
        else
        {
            stationaryFrames++;
            if (stationaryFrames > 3)
            {
                if (Time.time - lastActualMovementTime > 1.0f)
                {
                    isDefinitelyStuck = true;
                    stuckTime += Time.deltaTime;
                }
            }
        }
    }
    
    public void AutoMoveBasedOnDistance()
    {
        if (agent.IsAttacking || agent.IsMovementDisabled || player == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (agent.HP < 350 && agent.CanAttack && Time.frameCount % 60 == 0)
        {
            float healthPercent = (float)agent.HP / 500;
            float healProbability = Mathf.Lerp(0.9f, 0.4f, healthPercent);

            if (Random.value < healProbability)
            {
                agent.GetComponent<AgentCombat>().PerformAttack(AttackType.TemporalSurge);
                return;
            }
        }

        bool hasBeenIdle = Time.time - lastActualMovementTime > 0.3f;

        if (distanceToPlayer > 1.5f && distanceToPlayer < 3.0f)
        {
            if (hasBeenIdle && agent.CanAttack && Time.frameCount % 30 == 0)
            {
                float rand = Random.value;
                if (rand < 0.7f)
                {
                    agent.GetComponent<AgentCombat>().PerformAttack(AttackType.Melee);
                    return;
                }
                else if (rand < 0.9f)
                {
                    agent.GetComponent<AgentCombat>().PerformAttack(AttackType.SpatialEruption);
                    return;
                }
            }

            if (rb.linearVelocity.magnitude < 0.5f || hasBeenIdle)
            {
                MoveTowardsPlayer();
                agent.AddReward(0.05f);
            }
            return;
        }

        if (distanceToPlayer < 0.5f)
        {
            MoveAwayFromPlayer();
            return;
        }

        if (distanceToPlayer > 3.0f)
        {
            MoveTowardsPlayer();
            float distanceReward = Mathf.Clamp(distanceToPlayer / 10f, 0.03f, 0.1f);
            agent.AddReward(distanceReward);
            return;
        }

        if (hasBeenIdle || rb.linearVelocity.magnitude < 0.5f)
        {
            MoveTowardsPlayer();
            return;
        }

        if (distanceToPlayer >= 2.0f && distanceToPlayer <= 3.0f)
        {
            if (rb.linearVelocity.magnitude < 0.5f || hasBeenIdle)
            {
                MoveTowardsPlayer();
            }
            return;
        }

        if (rb.linearVelocity.magnitude < 0.5f || Time.time - lastActualMovementTime > 0.3f)
        {
            MoveTowardsPlayer();
        }
    }
    
    public void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            
            rb.Sleep();
            rb.WakeUp();
        }
        
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
        }
    }
    
    private void OnAttackStart()
    {
        StopMovement();
    }
    
    public bool IsStuck => isDefinitelyStuck;
    public float StuckTime => stuckTime;
    public void ResetStuckState()
    {
        isDefinitelyStuck = false;
        stuckTime = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
    
    public float LastActualMovementTime => lastActualMovementTime;
    public Vector3 LastFramePosition => lastFramePosition;
    public void UpdateLastFramePosition() => lastFramePosition = transform.position;
    public int StationaryFrames => stationaryFrames;
    public void SetStationaryFrames(int value) => stationaryFrames = value;
}
using UnityEngine;

public class AgentAnimation : MonoBehaviour
{
    private ZolaRLAgent agent;
    public Animator animator;
    private AgentMovement movement;
    
    [SerializeField] private float minMovementThreshold = 0.01f;
    [SerializeField] private float movementAnimationSpeed = 1.0f;
    [SerializeField] private float attackAnimationSpeed = 1.0f;
    [SerializeField] private float dimensionalWaveAnimationSpeed = 0.7f;
    
    public void Initialize(ZolaRLAgent agent)
    {
        this.agent = agent;
        this.movement = agent.GetComponent<AgentMovement>();
        
        animator.speed = 1f;
        animator.SetFloat("Speed", 0f);
    }
    
    public void UpdateAnimations()
    {
        if (!agent.IsAttacking && !agent.IsMovementDisabled)
        {
            UpdateMovementAnimation();
        }
        else
        {
            SlowDownMovementAnimation();
        }
        
        movement.UpdateLastFramePosition();
    }
    
    private void UpdateMovementAnimation()
    {
        float frameMoveAmount = Vector3.Distance(transform.position, movement.LastFramePosition);
        
        if (frameMoveAmount < minMovementThreshold)
        {
            int stationaryFrames = movement.StationaryFrames + 1;
            movement.SetStationaryFrames(stationaryFrames);
            
            if (stationaryFrames > 2)
            {
                float currentSpeed = animator.GetFloat("Speed");
                if (currentSpeed > 0)
                {
                    float newSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 5f);
                    animator.SetFloat("Speed", newSpeed);
                }
                
                if (Time.time - movement.LastActualMovementTime > 1.0f && agent.CanAttack && Time.frameCount % 30 == 0)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, agent.PlayerTransform.position);
                    
                    if (distanceToPlayer < 2.5f)
                    {
                        if (Random.value < 1.5f)
                        {
                            agent.combat.PerformAttack(AttackType.Melee);
                        }
                        else
                        {
                            agent.combat.PerformAttack(AttackType.SpatialEruption);
                        }
                    }
                    else
                    {
                        agent.combat.PerformAttack(AttackType.DimensionalWave);
                    }
                }
            }
        }
        else
        {
            movement.SetStationaryFrames(0);
            
            if (frameMoveAmount > minMovementThreshold)
            {
                float velocity = frameMoveAmount / Time.deltaTime;
                
                float normalizedSpeed = Mathf.Clamp(velocity / 3.5f, 0f, 0.7f);
                
                float currentSpeed = animator.GetFloat("Speed");
                float newSpeed = Mathf.Lerp(currentSpeed, normalizedSpeed, Time.deltaTime * 3f);
                
                animator.SetFloat("Speed", newSpeed);
            }
        }
    }
    
    private void SlowDownMovementAnimation()
    {
        if (animator.GetFloat("Speed") > 0 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
        {
            float currentSpeed = animator.GetFloat("Speed");
            float newSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * 8f);
            animator.SetFloat("Speed", newSpeed);
        }
    }
    
    public void ResetMovementAnimation()
    {
        animator.SetFloat("Speed", 0f);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum AttackType
{
    Melee = 1,
    SpatialEruption = 2,
    TemporalSurge = 3,
    DimensionalWave = 4,
    VoidRift = 5
}
public class AgentCombat : MonoBehaviour
{
    private ZolaRLAgent agent;
    private Transform player;
    private Animator animator;
    private AgentMovement movement;
    
    private float lastAttackTime = 0f;
    private float attackCooldown = 1.5f;
    private Dictionary<string, bool> attackHasDealtDamage = new Dictionary<string, bool>();
    private float damageResetTime = 0.1f;
    public Dictionary<int, float> lastAttackTimeByType = new Dictionary<int, float>();
    private Dictionary<int, float> attackCooldownsByType = new Dictionary<int, float>();

    [Header("Attack Prefabs")]
    public GameObject voidRiftPrefab;
    public GameObject spatialEruptionPrefab;
    public GameObject temporalSurgePrefab;
    public GameObject dimensionalStartPrefab;
    public GameObject dimensionalEndPrefab;

    [Header("Attack Transform Points")]
    public Transform temporalPoint;
    public Transform spatialPoint;
    public Transform dimensionalPoint;

    [Header("Boss Component References")]
    public DimensionalWave dimensionalWave;
    public AethericStrike aethericStrike;
    
    public void Initialize(ZolaRLAgent agent, Transform player)
    {
        this.agent = agent;
        this.player = player;
        this.animator = agent.animation.animator;
        this.movement = agent.GetComponent<AgentMovement>();
        InitializeDictionaries();
    }
    
    private void InitializeDictionaries()
    {
        attackHasDealtDamage = new Dictionary<string, bool>
        {
            { "aethericStrike", false },
            { "spatialEruption", false },
            { "dimensionalWave", false },
            { "voidRift", false },
            { "temporalSurge", false }
        };

        lastAttackTimeByType = new Dictionary<int, float>();
        for (int i = 1; i <= 5; i++)
        {
            lastAttackTimeByType[i] = 0f;
        }

        attackCooldownsByType = new Dictionary<int, float>
        {
            { 1, 1.5f },
            { 2, 2.0f },
            { 3, 10.0f },
            { 4, 5.0f },
            { 5, 7.0f }
        };
    }
    
    public void ResetState()
    {
        List<string> keys = new List<string>(attackHasDealtDamage.Keys);
        
        foreach (var key in keys)
        {
            attackHasDealtDamage[key] = false;
        }
    }
    
    public void UpdateCooldowns()
    {
        if (!agent.CanAttack && Time.time - lastAttackTime > attackCooldown && !agent.IsAttacking)
        {
            agent.CanAttack = true;
        }
    }
    
    public int ValidateAction(int action)
    {
        if (agent.HP < 350 && action != 3)
        {
            float healthPercent = (float)agent.HP / 500;
            float healProbability = Mathf.Lerp(0.9f, 0.4f, healthPercent);
            
            if (UnityEngine.Random.value < healProbability && Time.time - lastAttackTimeByType[3] >= attackCooldownsByType[3])
            {
                lastAttackTimeByType[3] = Time.time;
                return 3;
            }
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer < 2.0f)
        {
            if (distanceToPlayer < 1.5f && Time.time - lastAttackTimeByType[1] >= attackCooldownsByType[1])
            {
                if (UnityEngine.Random.value < 0.85f || action == 1)
                {
                    lastAttackTimeByType[1] = Time.time;
                    return 1;
                }
            }
            else if (Time.time - lastAttackTimeByType[2] >= attackCooldownsByType[2])
            {
                if (UnityEngine.Random.value < 0.3f || action == 2)
                {
                    lastAttackTimeByType[2] = Time.time;
                    return 2;
                }
            }
        }
        
        if (action >= 1 && action <= 5)
        {
            float specificCooldown = attackCooldownsByType[action];
            if (Time.time - lastAttackTimeByType[action] < specificCooldown)
            {
                if (distanceToPlayer < 1.0f && Time.time - lastAttackTimeByType[1] >= attackCooldownsByType[1])
                {
                    lastAttackTimeByType[1] = Time.time;
                    return 1;
                }
                
                return 6;
            }
        }
        
        if (!agent.CanAttack && action <= 5)
        {
            return 6;
        }

        if (agent.movement.IsStuck && agent.movement.StuckTime > 1.5f && action != 4 && distanceToPlayer > 2.5f) 
        {
            lastAttackTimeByType[4] = Time.time;
            return 4;
        }
        
        if (action <= 5)
        {
            lastAttackTimeByType[action] = Time.time;
        }
        
        return ValidateDistanceForAttacks(action);
    }
    
    private int ValidateDistanceForAttacks(int action)
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (action == 1 && distanceToPlayer > 1.0f)
        {
            return 6;
        }
        
        if (action == 2)
        {
            if (distanceToPlayer < 0.5f || distanceToPlayer > 1.5f)
            {
                if (distanceToPlayer < 0.5f)
                {
                    if (Time.time - lastAttackTimeByType[1] >= attackCooldownsByType[1])
                    {
                        lastAttackTimeByType[1] = Time.time;
                        return 1;
                    }
                    return 7;
                }
                else if (distanceToPlayer > 1.5f)
                {
                    return 6;
                }
            }
        }
        
        return action;
    }
    
    public void PerformAttack(AttackType attackType)
    {
        if (!agent.CanAttack) return;
        
        switch (attackType)
        {
            case AttackType.Melee:
                StartCoroutine(Attack());
                break;
            case AttackType.SpatialEruption:
                StartCoroutine(SpatialEruption());
                break;
            case AttackType.TemporalSurge:
                StartCoroutine(TemporalSurge());
                break;
            case AttackType.DimensionalWave:
                StartCoroutine(DimensionalWave());
                break;
            case AttackType.VoidRift:
                StartCoroutine(VoidRift());
                break;
        }
    }
    
    IEnumerator Attack()
    {
        if (!agent.CanAttack || agent.IsAttacking) yield break;

        agent.IsAttacking = true;
        agent.CanAttack = false;
        lastAttackTime = Time.time;
        agent.IsMovementDisabled = true;
        agent.GetComponent<AgentMovement>().StopMovement();
        animator.SetTrigger("aethericStrike");
        movement.StopMovement();
        attackHasDealtDamage["aethericStrike"] = false;
        
        yield return new WaitForSeconds(1.5f);

        if (aethericStrike.inRange && !attackHasDealtDamage["aethericStrike"])
        {
            PlayerHP.Instance.TakeDamage(BaseStatsForZolaBoss.aethricStirkeDamage);
            float damagePercentage = (float)BaseStatsForZolaBoss.aethricStirkeDamage / PlayerHP.Instance.maxHP;
            float reward = 0.3f + damagePercentage * 1.0f;
            agent.AddReward(reward);
            
            attackHasDealtDamage["aethericStrike"] = true;
            
            StartCoroutine(ResetDamageFlag("aethericStrike"));
        }
        else
        {
            agent.AddReward(-0.2f);
        }

        yield return new WaitForSeconds(1f);

        agent.IsMovementDisabled = false;
        agent.CanAttack = true;
        agent.IsAttacking = false;
        agent.GetComponent<AgentAnimation>().ResetMovementAnimation();
    }

    IEnumerator SpatialEruption()
    {
        if (!agent.CanAttack || agent.IsAttacking) yield break;
    
        agent.IsAttacking = true;
        agent.CanAttack = false;
        lastAttackTime = Time.time;
        agent.IsMovementDisabled = true;
        animator.SetTrigger("spatialEruption");
        movement.StopMovement();
        attackHasDealtDamage["spatialEruption"] = false;
        
        yield return new WaitForSeconds(1.1f);

        if (spatialEruptionPrefab != null && spatialPoint != null) {
            Instantiate(spatialEruptionPrefab, spatialPoint.position, Quaternion.identity);
        }

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, 3f, LayerMask.GetMask("Player"));
        if (hitPlayers.Length > 0 && !attackHasDealtDamage["spatialEruption"])
        {
            PlayerHP.Instance.TakeDamage(BaseStatsForZolaBoss.spatialEruptionDamage);
            float damagePercentage = (float)BaseStatsForZolaBoss.spatialEruptionDamage / PlayerHP.Instance.maxHP;
            float reward = 0.2f + damagePercentage * 1.0f;
            agent.AddReward(reward);
            attackHasDealtDamage["spatialEruption"] = true;
            
            StartCoroutine(ResetDamageFlag("spatialEruption"));
        }
        else 
        {
            agent.AddReward(-0.1f);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        agent.IsMovementDisabled = false;
        agent.CanAttack = true;
        agent.IsAttacking = false;
        agent.GetComponent<AgentAnimation>().ResetMovementAnimation();
    }

    IEnumerator TemporalSurge()
    {
        agent.IsMovementDisabled = false;
        agent.CanAttack = true;
        agent.IsAttacking = false;
        
        agent.GetComponent<AgentAnimation>().ResetMovementAnimation();

        if (!agent.CanAttack || agent.IsAttacking) yield break;
    
        lastAttackTime = Time.time;
        agent.IsAttacking = true;
        agent.CanAttack = false;
        lastAttackTime = Time.time;
        agent.IsMovementDisabled = true;
        agent.movement.StopMovement();
        animator.SetTrigger("temporalSurge");
        
        GameObject surge = null;
        if (temporalSurgePrefab != null) {
            surge = Instantiate(temporalSurgePrefab, temporalPoint.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.25f);
        
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.25f);
            
            int healAmount = (int)(500 * 0.025f);
            agent.Heal(healAmount);
        }
        
        if (surge != null) {
            surge.transform.GetChild(0).GetComponent<Animator>().SetTrigger("ki");
            surge.transform.GetChild(1).GetComponent<Animator>().SetTrigger("ki");
            yield return new WaitForSeconds(0.55f);
            Destroy(surge);
        }
        
        agent.IsMovementDisabled = false;
        agent.CanAttack = true;
        agent.IsAttacking = false;
        
        agent.GetComponent<AgentAnimation>().ResetMovementAnimation();
    }

    IEnumerator DimensionalWave()
    {
        if (!agent.CanAttack || agent.IsAttacking) yield break;
    
        agent.IsAttacking = true;
        agent.CanAttack = false;
        lastAttackTime = Time.time;
        agent.IsMovementDisabled = true;
        
        attackHasDealtDamage["dimensionalWave"] = false;
        
        animator.SetTrigger("dimensionalKi");
        movement.StopMovement();
        Vector3 originalPosition = transform.position;
        
        yield return new WaitForSeconds(0.45f);
        transform.position = new Vector3(transform.position.x, transform.position.y + 15, transform.position.z);
        
        if (dimensionalStartPrefab != null) {
            Instantiate(dimensionalStartPrefab, originalPosition, Quaternion.identity);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        Vector3 targetPosition = player.position;
        
        yield return new WaitForSeconds(1.0f);
        
        animator.SetTrigger("dimensionalBe");
        yield return new WaitForSeconds(0.55f);
        
        transform.position = targetPosition;
        
        if (dimensionalEndPrefab != null) {
            Instantiate(dimensionalEndPrefab, targetPosition, Quaternion.identity);
        }
        
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, 2f, LayerMask.GetMask("Player"));
        if (hitPlayers.Length > 0 && !attackHasDealtDamage["dimensionalWave"])
        {
            PlayerHP.Instance.TakeDamage(BaseStatsForZolaBoss.dimensionalWaveDamage);
            float damagePercentage = (float)BaseStatsForZolaBoss.dimensionalWaveDamage / PlayerHP.Instance.maxHP;
            float reward = 0.3f + damagePercentage * 1.0f;
            agent.AddReward(reward);
            attackHasDealtDamage["dimensionalWave"] = true;
            
            StartCoroutine(ResetDamageFlag("dimensionalWave"));
        }
        else
        {
            agent.AddReward(-0.2f);
        }  
        
        yield return new WaitForSeconds(0.8f);
        agent.IsMovementDisabled = false;
        agent.CanAttack = true;
        agent.IsAttacking = false;
        agent.GetComponent<AgentAnimation>().ResetMovementAnimation();
        
        agent.GetComponent<AgentMovement>().ResetStuckState();
    }

    IEnumerator VoidRift()
    {
        if (!agent.CanAttack || agent.IsAttacking) yield break;
        
        agent.IsAttacking = true;
        agent.CanAttack = false;
        lastAttackTime = Time.time;
        agent.IsMovementDisabled = true;
        animator.SetTrigger("voidRift");
        movement.StopMovement();
        yield return new WaitForSeconds(0.45f);
        
        Vector3 targetPosition = GetValidRiftPosition();
        
        if (voidRiftPrefab != null)
        {
            GameObject riftObject = Instantiate(voidRiftPrefab, targetPosition, Quaternion.identity);
            Destroy(riftObject, 5f);
        }

        float distanceToPlayer = Vector3.Distance(targetPosition, player.position);
        float proximityBonus = Mathf.Clamp01(1.0f - (distanceToPlayer / 3.0f));

        int activeRifts = GameObject.FindGameObjectsWithTag("BlackHole").Length;
     
        float reward = activeRifts > 2 ? -0.1f : 0.1f + proximityBonus * 0.3f;
        agent.AddReward(reward);
        
        yield return new WaitForSeconds(1.5f);
        agent.IsMovementDisabled = false;
        agent.CanAttack = true;
        agent.IsAttacking = false;
        
        agent.GetComponent<AgentAnimation>().ResetMovementAnimation();
    }
    
    private Vector3 GetValidRiftPosition()
    {
        Vector3 targetPosition = player.position;
        targetPosition.z = transform.position.z;
        
        GridSystem grid = FindFirstObjectByType<GridSystem>();
        if (grid != null)
        {
            int cellX, cellY;
            if (grid.WorldToCell(targetPosition, out cellX, out cellY))
            {
                Vector3 validPosition = grid.CellToWorld(cellX, cellY);
                targetPosition = new Vector3(validPosition.x, validPosition.y, targetPosition.z);
            }
            else
            {
                Vector3 roomCenter = grid.transform.position;
                Vector3 direction = (targetPosition - roomCenter).normalized;
                
                for (float dist = 0.5f; dist < grid.roomSize.x/2; dist += 0.5f)
                {
                    Vector3 testPos = roomCenter + direction * dist;
                    if (grid.WorldToCell(testPos, out cellX, out cellY) && grid.IsWalkable(testPos))
                    {
                        targetPosition = grid.CellToWorld(cellX, cellY);
                        break;
                    }
                }
            }
        }
        
        return targetPosition;
    }
    
    IEnumerator ResetDamageFlag(string attackName)
    {
        yield return new WaitForSeconds(damageResetTime);
        attackHasDealtDamage[attackName] = false;
    }
}

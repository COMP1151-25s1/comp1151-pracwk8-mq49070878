using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class EnemyHandler : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Attack,
        Retreat
    }

    public EnemyState currentState = EnemyState.Patrol;

    
    [Range(0f, 1f)]
    public float aggressiveness = 0.5f;

    [Range(0f, 1f)]
    public float intelligence = 1f;

    public Vector2Int moneyDropOnDeath = new Vector2Int(25, 50);

    
    
    public PlayerHandler playerHandler;
    public Rigidbody2D rigidbody;

    [HideInInspector] public float currentHP = 100f;
    [HideInInspector] public float maxHP = 100f;

    [HideInInspector] public float currentTurbo = 0f;
    [HideInInspector] public float maxTurbo = 5f;

    public float topSpeed = 15f;
    public float detectionRange = 20f;
    public float attackRange = 10f;
    public float patrolSpeed = 5f;
    public float safeDistance = 12f;
    public Slider healthSlider;

    public BodyPiece bodyPiece;
    public List<WingPiece> wingPieces;
    public EnginePiece enginePiece;
    public GunPiece gunPiece;
    
    private bool inCombat = false;
    private float disengageDistance = 30f; // Tweak as needed


    private void Start()
    {
        if (playerHandler == null)
            playerHandler = GameObject.FindObjectOfType<PlayerHandler>();

        currentHP = maxHP = 100f;
        currentTurbo = 0f; 
        maxTurbo = 5f;

        healthSlider.maxValue = maxHP;

        rigidbody.linearVelocity = Vector2.zero;

        PullDataFromShipHandler();
    }

    private void FixedUpdate()
    {
        if (currentState == EnemyState.Patrol)
        {
            Patrol();
        }
        else if (currentState == EnemyState.Attack)
        {
            Attack();
        }
        else if (currentState == EnemyState.Retreat)
        {
            Retreat();
        }

        SmartNavigation();
    }

    private void Update()
    {
        healthSlider.value = currentHP;
        healthSlider.maxValue = maxHP;

        DetectPlayer();
    }

    private void DetectPlayer()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerHandler.transform.position);

        if (!inCombat && distanceToPlayer < detectionRange)
        {
            // First time entering combat: make a decision
            float decision = Random.Range(0f, 1f);
            currentState = decision <= aggressiveness ? EnemyState.Attack : EnemyState.Retreat;
            inCombat = true;
        }
        else if (inCombat && distanceToPlayer > disengageDistance)
        {
            // Player has left combat range
            currentState = EnemyState.Patrol;
            inCombat = false;
        }
    }



    
    private Vector2 patrolDirection;
    private float directionChangeTimer;
    private float nextDirectionChangeTime;
    
    private void Patrol()
    {
        rigidbody.linearVelocity = patrolDirection * patrolSpeed;

        directionChangeTimer -= Time.deltaTime;

        if (directionChangeTimer <= 0f)
        {
            ChooseNewPatrolDirection();
        }

        if (Vector2.Distance(transform.position, playerHandler.transform.position) < attackRange)
        {
            currentState = EnemyState.Attack;
        }
    }

    private void ChooseNewPatrolDirection()
    {
        patrolDirection = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
        directionChangeTimer = Random.Range(1f, 8f);
    }

    private void Attack()
    {
        Vector2 direction = (playerHandler.transform.position - transform.position).normalized;

        float distanceToPlayer = Vector2.Distance(transform.position, playerHandler.transform.position);
        if (distanceToPlayer < safeDistance)
        {
            rigidbody.linearVelocity = -direction * topSpeed;
        }
        else
        {
            rigidbody.linearVelocity = direction * topSpeed;
        }

        if (distanceToPlayer < attackRange)
        {
            FireAtPlayer();
        }
    }

    private void FireAtPlayer()
    {
        if (gunPiece != null)
        {
            gunPiece.FireButtonPressed();
        }
    }

    private Vector2 retreatTarget;
    private bool hasRetreatTarget = false;

    private void Retreat()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerHandler.transform.position);

        if (!hasRetreatTarget || distanceToPlayer < attackRange)
        {
            Vector2 awayFromPlayer = (transform.position - playerHandler.transform.position).normalized;
            Vector2 randomOffset = Random.insideUnitCircle * 5f;
            retreatTarget = (Vector2)transform.position + awayFromPlayer * 15f + randomOffset;

            hasRetreatTarget = true;
        }

        Vector2 direction = (retreatTarget - (Vector2)transform.position).normalized;
        rigidbody.linearVelocity = direction * topSpeed;

        if (Vector2.Distance(transform.position, retreatTarget) < 1f)
        {
            hasRetreatTarget = false;
        }

        if (distanceToPlayer > detectionRange)
        {
            currentState = EnemyState.Patrol;
            hasRetreatTarget = false;
        }
    }


    private void SmartNavigation()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.up, 3f);
        
        if (hit.collider != null && !hit.collider.CompareTag("Player"))
        {
            Vector2 avoidDirection = Vector2.Perpendicular(hit.normal).normalized;
        }

        if (rigidbody.linearVelocity.magnitude > 0)
        {
            float angle;

            if (currentState != EnemyState.Attack)
            {
                angle = Mathf.Atan2(
                    rigidbody.linearVelocity.y,
                    rigidbody.linearVelocity.x
                ) * Mathf.Rad2Deg;
            }
            else
            {
                Vector2 toPlayer = playerHandler.transform.position - transform.position;
                angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

                if (intelligence < 1f)
                {
                    float inaccuracy = (1f - intelligence) * 30f;
                    angle += Random.Range(-inaccuracy, inaccuracy);
                }
            }

            float smoothAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle + 90f, Time.fixedDeltaTime * 2f);
            transform.rotation = Quaternion.Euler(0, 0, smoothAngle);
        }

    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        if (currentHP <= 0)
        {
            GameHandler.credits += Random.Range(moneyDropOnDeath.x, moneyDropOnDeath.y);
            Destroy(this.gameObject);
        }

        if (!inCombat)
        {
            inCombat = true;
        }

        float decision = Random.Range(0f, 1f);
        currentState = decision <= aggressiveness ? EnemyState.Attack : EnemyState.Retreat;
    }


    private void PullDataFromShipHandler()
    {
        bodyPiece = GetComponentInChildren<BodyPiece>();
        List<WingPiece> wings = GetComponentsInChildren<WingPiece>().ToList();
        
        wingPieces.Clear();
        foreach (WingPiece obj in wings)
        {
            wingPieces.Add(obj);
        }

        enginePiece = GetComponentInChildren<EnginePiece>();
        gunPiece = GetComponentInChildren<GunPiece>();
    }
}


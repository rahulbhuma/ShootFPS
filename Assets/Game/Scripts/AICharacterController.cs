using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AICharacterController : MonoBehaviour
{
    [SerializeField, Tooltip("The NavMeshAgent to control this AI")]
    private NavMeshAgent m_Agent; // Reference to the NavMeshAgent

    [SerializeField, Tooltip("The Prefab used to instantiate drop items as the AI moves about.")]
    private GameObject m_DropPrefab; // Ensure this is assigned in the Inspector

    [SerializeField, Tooltip("The percentage chance of dropping an item in any given frame.")]
    [Range(0, 1)]
    private float m_DropChance = 0.01f;

    private Vector3 movePosition; // Target position for movement

    private float health = 100f; // Placeholder health value

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        if (m_Agent == null)
            Debug.LogError("NavMeshAgent component is missing on this GameObject!");

        if (m_DropPrefab == null)
            Debug.LogError("Drop prefab is not assigned in the Inspector!");
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject, 0.25f); // Destroy this GameObject with a slight delay
    }

    void Update()
    {
        // Ensure the NavMeshAgent is set up before proceeding
        if (m_Agent == null) return;

        // Check if the agent has reached its destination or if no target position is set
        if (movePosition == Vector3.zero || Vector3.Distance(transform.position, m_Agent.destination) < 1f)
        {
            SelectNewDestination();
        }

        // Drop an item with a random chance
        if (m_DropPrefab != null && Random.value < m_DropChance)
        {
            Instantiate(m_DropPrefab, transform.position - Vector3.forward, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
            Debug.Log("Drop item spawned at: " + (transform.position - Vector3.forward));
        }
    }

    void SelectNewDestination()
    {
        // Select a random position within a 50-unit radius
        Vector2 pos = Random.insideUnitCircle * 50;
        movePosition = new Vector3(pos.x, 0, pos.y); // Correctly set the move position

        // Ensure the position is valid within the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(movePosition, out hit, 4f, NavMesh.AllAreas))
        {
            m_Agent.SetDestination(hit.position);
            Debug.Log("New destination set: " + hit.position);
        }
        else
        {
            Debug.LogWarning("No valid NavMesh position found for destination.");
        }
    }

    void SpawnTurret()
    {
        if (m_DropPrefab == null)
        {
            Debug.LogError("Drop prefab is not assigned in the Inspector!");
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

        // Instantiate the drop item at the spawn position with a random rotation
        Instantiate(m_DropPrefab, spawnPosition, Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0)));
        Debug.Log("Drop item spawned at: " + spawnPosition);
    }
}

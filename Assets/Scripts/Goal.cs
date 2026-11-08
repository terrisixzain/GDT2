using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Goal : MonoBehaviour
{
    public static event Action<Goal> OnGoalReached;

    [SerializeField] private int goalIndex = 1;
    [SerializeField] private int scoreValue = 100;
    [SerializeField] private bool isFinalGoal = false;

    public int GoalIndex => goalIndex;
    public int ScoreValue => scoreValue;
    public bool IsFinalGoal => isFinalGoal;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (OnGoalReached != null)
            {
                OnGoalReached.Invoke(this);
            }

            gameObject.SetActive(false);
        }
    }
}
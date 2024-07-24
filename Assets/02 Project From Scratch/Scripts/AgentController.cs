using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class AgentController : Agent
{
    [SerializeField] private GameObject m_Target;
    [SerializeField] private float m_SpawningRange = 5f;
    [SerializeField] private float m_MaxSpeed = 2f;
    [SerializeField] private float m_TimeLimit = 30f; // seconds

    private float m_StartingDistance;
    private float m_LastDistance;
    private float m_StartingTime;

    public override void OnEpisodeBegin()
    {
       // Reset the starting conditions and make them random
       
       // target positioning
       m_Target.transform.localPosition = new Vector3(
           Random.Range(-m_SpawningRange, m_SpawningRange), 
           0f, 
           Random.Range(-m_SpawningRange, m_SpawningRange));
       
       // agent positioning
       gameObject.transform.localPosition = new Vector3(
           Random.Range(-m_SpawningRange, m_SpawningRange), 
           0f, 
           Random.Range(-m_SpawningRange, m_SpawningRange));
       
       // starting conditions
       m_StartingDistance = GetDistanceToTarget();
       m_LastDistance = m_StartingDistance;
       m_StartingTime = Time.time;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var direction = (m_Target.transform.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(direction); // x y z, size = 3
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // read taken NN decisions
        var xVelocity = actions.ContinuousActions[0];
        var zVelocity = actions.ContinuousActions[1];
        
        // apply decisions to the agent
        var xDistance = xVelocity * m_MaxSpeed * Time.fixedDeltaTime;
        var zDistance = zVelocity * m_MaxSpeed * Time.fixedDeltaTime;
        transform.position += new Vector3(xDistance, 0f, zDistance);
        
        // Rewarding
        // Distance is decreasing == GOOD
        // Max reward = 1.0
        // Mix reward = -1.0

        var newDistance = GetDistanceToTarget();
        var deltaDistance = m_LastDistance - newDistance;
        var deltaReward = deltaDistance / m_StartingDistance;
        AddReward(deltaReward);;
        m_LastDistance = newDistance;
        
        // Finish Conditions
        // reward < -1.0 -- Total FAIL
        if (newDistance > m_StartingDistance * 2f)
        {
            EndEpisode();
        }
        
        // motivation to perform action fast
        AddReward(-.0001f);

        // Time Limit
        var passedTime = Time.time - m_StartingTime;
        if (passedTime > m_TimeLimit)
        {
            EndEpisode();
        }
        
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            EndEpisode();
        }
    }

    private float GetDistanceToTarget()
    {
        return (m_Target.transform.localPosition - transform.localPosition).magnitude;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class AgentController03_Discrete : Agent
{
    [SerializeField] private GameObject m_Target;
    [SerializeField] private Animator m_Animator;
    
    
    [SerializeField] private float m_SpawningRange = 5f;
    
    [SerializeField] private float m_MaxSpeed = 2f; // units per second
    [SerializeField] private float m_MaxAngularSpeed = 90f; //degree per second
    
    [SerializeField] private float m_TimeLimit = 30f; // seconds

    
    
    private float m_StartingDistance;
    private float m_LastDistance;
    private float m_StartingTime;

    public override void OnEpisodeBegin()
    {
       // Reset the starting conditions and make them random
       m_StartingDistance = 0f;

       while (m_StartingDistance < 2f)
       {
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
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var direction = (m_Target.transform.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(direction); // x y z, size = 3
        sensor.AddObservation(transform.forward); //+3
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // read taken NN decisions
        var rotationValue = actions.DiscreteActions[0]; // 0 1 2
        var throttleValue = actions.DiscreteActions[1]; // 0 1 2
        
        // do -1, and will have -1 0 1
        // 0-1 = -1
        // 1-1 = 0
        // 2-1 = 1

        rotationValue -= 1;
        throttleValue -= 1;
        
        // apply decisions to the agent
        var rotationAngle = rotationValue * m_MaxAngularSpeed * Time.fixedDeltaTime;
        var movementDistance = throttleValue * m_MaxSpeed * Time.fixedDeltaTime;
        
        transform.Rotate(transform.up, rotationAngle);                   // Rotation
        transform.localPosition += transform.forward * movementDistance; // movement

        if (Math.Abs(throttleValue) > .02f 
            && !m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Run In Place"))
        {
            m_Animator.Play("Run In Place");
        }
        else if(Math.Abs(throttleValue) <= .02f && !m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            m_Animator.Play("Idle");
        }
        
        
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
        AddReward(-.0005f);

        // Time Limit
        var passedTime = Time.time - m_StartingTime;
        if (passedTime > m_TimeLimit)
        {
            EndEpisode();
        }
        
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetAxis("Horizontal") > .33f ? 2 : (Input.GetAxis("Horizontal") < -.33f ? 0 : 1);
        discreteActionsOut[1] = Input.GetAxis("Vertical") > .33f ? 2 : (Input.GetAxis("Vertical") < -.33f ? 0 : 1);
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

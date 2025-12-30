using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform target;        //문
    public NavMeshAgent agent;

    Vector3 dis;
    public float targetDistance;

    public void Move()
    {
        if (agent == null) Debug.LogError("agent가 없습니다.");
        else
        {
            dis = transform.position - target.position;

            if (dis.magnitude < targetDistance)
            {
                agent.SetDestination(target.position);
            }
        }
    }
    public void Update()
    {
        Move();
    }
}

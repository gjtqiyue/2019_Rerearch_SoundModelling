﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SoundSystem;

public class Agent : AgentWithSound
{
    public enum AgentStatus
    {
        InWaitingTime,
        InPatrolling,
        InSearching
    }

    public float patrolSpeed;
    public float searchSpeed;
    public float waitTime;
    public bool canMakeSound;
    public float volume;

    [SerializeField]
    private List<Transform> patrolPoints = new List<Transform>();

    [SerializeField]
    private AgentStatus status;
    private AgentStatus previous_status;

    private int targetPoint;
    private int lastPoint;

    private float waitTimer;

    private AgentSoundComponent soundComp;

    [SerializeField]
    private List<PointIntensity> searchPath;

    [SerializeField]
    private bool loop = false;

    private void Start()
    {
        waitTimer = waitTime;
        lastPoint = targetPoint = -1;
        soundComp = GetComponent<AgentSoundComponent>();
    }

    private void Update()
    {
        if (canMakeSound)
            soundComp.MakeSound(gameObject, transform.position, volume, SoundType.Walk);

        DoBehaviour();
    }

    private void DoBehaviour()
    {
        switch (status) {
            case AgentStatus.InPatrolling:

                Patrol();
                break;

            case AgentStatus.InWaitingTime:

                waitTimer -= Time.deltaTime;

                if (waitTimer <= 0)
                {
                    status = AgentStatus.InPatrolling;
                    previous_status = AgentStatus.InWaitingTime;
                    waitTimer = waitTime;
                }
                break;

            case AgentStatus.InSearching:

                if (searchPath == null || searchPath.Count == 0)
                {
                    //exit search mode
                    status = previous_status;
                    previous_status = AgentStatus.InSearching;
                    break;
                }

                // if we are not at the path destination, we move to there
                if (Vector3.SqrMagnitude(searchPath[0].pos - transform.position) > 0.5)
                {
                    transform.position = Vector3.MoveTowards(transform.position, searchPath[0].pos, searchSpeed);
                }
                else
                {
                    searchPath.RemoveAt(0);
                }

                break;

            default:
                break; 
        }
    }

    private void Patrol()
    {
        if (patrolPoints.Count <= 0) return;

        if (targetPoint == -1)
        {
            targetPoint = FindCloestPoint();
            lastPoint = targetPoint - 1 < 0 ? targetPoint + 1 : targetPoint - 1;
        }

        transform.position = Vector3.MoveTowards(transform.position, patrolPoints[targetPoint].position, patrolSpeed);

        if (Vector3.SqrMagnitude(patrolPoints[targetPoint].position - transform.position) < 0.05 && patrolPoints.Count > 1)
        {
            status = AgentStatus.InWaitingTime;
            previous_status = AgentStatus.InPatrolling;
            GetNextTargetPoint();
        }
    }

    private void GetNextTargetPoint()
    {
        int next = targetPoint - lastPoint > 0 ? targetPoint + 1 : targetPoint - 1;
        if (targetPoint == patrolPoints.Count - 1 || targetPoint == 0)
        {
            if (loop)
            {
                targetPoint = (next + patrolPoints.Count) % patrolPoints.Count;
                lastPoint = targetPoint == 0 ? -1 : patrolPoints.Count;
            }
            else
            {
                next = lastPoint;
                lastPoint = targetPoint;
                targetPoint = next;
            }
        }
        else
        {
            lastPoint = targetPoint;
            targetPoint = next;
        }
    }

    private int FindCloestPoint()
    {
        int minPointIndex = 0;
        float minDistance = int.MaxValue;
        for (int i=0; i<patrolPoints.Count; i++)
        {
            float dist = Vector3.SqrMagnitude(patrolPoints[i].position - transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                minPointIndex = i;
            }
        }

        return minPointIndex;
    }

    public override void SearchSoundSource(List<PointIntensity> path)
    {
        if (status != AgentStatus.InSearching)
        {
            previous_status = status;
            status = AgentStatus.InSearching;
        }

        //compare two plan's first intensity to decide if we take the new plan
        if (searchPath == null)
        {
            searchPath = path;
        }
        else if (searchPath.Count == 0)
        {
            searchPath = path;
        }
        else if (path[0].net_intensity > searchPath[0].net_intensity)
        {
            searchPath = path;
        }
        
    }
}

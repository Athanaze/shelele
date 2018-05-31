using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
//THIS SCRIPT IS ON THE TANK PREFAB
public class TanksMove : MonoBehaviour {
    NavMeshAgent agent;

	public void GoToTarget (Transform target) {
		agent = GetComponent<NavMeshAgent>();
		agent.SetDestination (target.position);
	}
}

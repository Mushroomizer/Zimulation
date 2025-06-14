using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Team
{
    Humans = 0,
    Zombie = 1
}

public static class TeamExtensions
{
    public static string Tag(this Team team)
    {
        return team switch
        {
            Team.Humans => "Human",
            Team.Zombie => "Zombie",
            _ => ""
        };
    }
}

public class HumanoidAgent : Agent
{
    [SerializeField] private Material _humanMaterial, _zombieMaterial;
    [SerializeField] private float _turnSpeed = 30f;
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _attackRange = 2f;
    private float m_Existential;
    private float attackSpeed = 1f;
    private float attackCooldown;
    private float attackTimer;

    public Team team;

    private BehaviorParameters m_BehaviorParameters;
    private Renderer m_Renderer;
    private EnvironmentController _environmentController;
    private Rigidbody _rb;

    public override void Initialize()
    {
        m_Renderer = GetComponent<Renderer>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        _rb = GetComponent<Rigidbody>();

        _environmentController = GetComponentInParent<EnvironmentController>();
        if (_environmentController != null)
        {
            m_Existential = 1f / _environmentController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        attackCooldown = 1f / attackSpeed;

        base.Initialize();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed)
            {
                continuousActionsOut[0] = 1;
            }
            else if (keyboard.sKey.isPressed)
            {
                continuousActionsOut[0] = -1;
            }

            if (keyboard.dKey.isPressed)
            {
                continuousActionsOut[1] = 1;
            }
            else if (keyboard.aKey.isPressed)
            {
                continuousActionsOut[1] = -1;
            }

            else if (keyboard.spaceKey.isPressed)
            {
                continuousActionsOut[2] = 1;
            }
        }
    }

    private void Update()
    {
        attackTimer += Time.deltaTime;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        switch (team)
        {
            case Team.Humans:
                AddReward(m_Existential);
                break;
            case Team.Zombie:
                AddReward(-m_Existential);
                break;
            default:
                Debug.LogWarning($"No existential reward rules set for team: {team.ToString()}");
                break;
        }

        var moveAction = actions.ContinuousActions[0];
        if (moveAction != 0f)
        {
            _rb.AddRelativeForce(new Vector3(0f, 0f, moveAction * 3 * _moveSpeed), ForceMode.Acceleration);
        }

        var turnAction = actions.ContinuousActions[1];
        if (turnAction != 0f)
        {
            transform.Rotate(Vector3.up, turnAction * _turnSpeed);
        }

        var attackAction = actions.ContinuousActions[2];
        if (attackAction != 0 && attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            // Do some kind of attack animation and death animation
            var ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out var hit, 2f)){
                var tag = hit.collider.tag;
                if (team == Team.Humans)
                {
                    // if (tag == Team.Zombie.Tag())
                    // {
                    //     _environmentController.KillZombie(this, hit.collider.gameObject.GetComponent<HumanoidAgent>());
                    // }
                }
                else if (team == Team.Zombie)
                {
                    if (tag == Team.Humans.Tag())
                    {
                        _environmentController.Zombify(this, hit.collider.gameObject.GetComponent<HumanoidAgent>());
                    }
                }
            }
        }

        base.OnActionReceived(actions);
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
    }

    public void SetTeam(Team m_team)
    {
        m_BehaviorParameters.TeamId = (int)m_team;
        switch (m_team)
        {
            case Team.Humans:
                gameObject.tag = Team.Humans.Tag();
                m_Renderer.material = _humanMaterial;
                team = Team.Humans;
                break;
            case Team.Zombie:
                gameObject.tag = Team.Zombie.Tag();
                m_Renderer.material = _zombieMaterial;
                team = Team.Zombie;
                break;
            default:
                Debug.LogWarning($"No material for team: {m_team.ToString()}");
                break;
        }
    }
}
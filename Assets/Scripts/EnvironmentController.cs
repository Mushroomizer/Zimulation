using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

public class EnvironmentController : MonoBehaviour
{
    [SerializeField] private Vector2 _environmentSize = new(5f, 5f);
    [SerializeField] private int _humanCount = 1;
    [SerializeField] private int _zombieCount = 1;

    [SerializeField] private List<HumanoidAgent> _agentList;
    [SerializeField] private GameObject _humanPrefab,_zombiePrefab;
    [SerializeField] private LayerMask _entityMask;

    private SimpleMultiAgentGroup _humansGroup;
    private SimpleMultiAgentGroup _zombiesGroup;

    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    private int m_ResetTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _humansGroup = new SimpleMultiAgentGroup();
        _zombiesGroup = new SimpleMultiAgentGroup();
        ResetScene();
    }

    
    
    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            _humansGroup.GroupEpisodeInterrupted();
            _zombiesGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    private void ResetScene()
    {
        m_ResetTimer = 0;
        //Reset Agents
        foreach (var item in _agentList)
        {
            Destroy(item.gameObject);
        }
        _agentList.Clear();

        // Spawn and set up Humans
        for (var i = 0; i < _humanCount; i++)
        {
            var human = Instantiate(_humanPrefab, GetSpawnPositionForTeam(Team.Humans),
                Quaternion.Euler(0, Random.Range(0f, 360f), 0),transform);
            var agent = human.GetComponent<HumanoidAgent>();
            agent.SetTeam(Team.Humans);
            _humansGroup.RegisterAgent(agent);
            _agentList.Add(agent);
        }

        // Spawn and set up Zombies
        for (var i = 0; i < _zombieCount; i++)
        {
            var zombie = Instantiate(_zombiePrefab, GetSpawnPositionForTeam(Team.Zombie),
                Quaternion.Euler(0, Random.Range(0f, 360f), 0),transform);
            var agent = zombie.GetComponent<HumanoidAgent>();
            agent.SetTeam(Team.Zombie);
            _zombiesGroup.RegisterAgent(agent);
            _agentList.Add(agent);
        }
    }

    /// <summary>
    /// Get a random spawn position for an entity that isnt too close to the enemy team
    /// </summary>
    /// <param name="mTeam"></param>
    /// <returns>a random spawn position</returns>
    private Vector3 GetSpawnPositionForTeam(Team mTeam)
    {
        var positionFound = false;
        var tries = 0;
        var randomSpawnPos = Vector3.zero;
        do
        {
            var randomPosX = Random.Range(-_environmentSize.x / 2, _environmentSize.x / 2);
            var randomPosZ = Random.Range(-_environmentSize.y / 2, _environmentSize.y / 2);
            randomSpawnPos = new Vector3(randomPosX, 0f, randomPosZ);
            var hitColliders = Physics.OverlapSphere(randomSpawnPos, 2.5f, _entityMask);

            //TODO: map team to tag and check that instead, getting components is slow
            if (hitColliders.Length == 0 && hitColliders.All(c => c.GetComponent<HumanoidAgent>().team == mTeam))
            {
                // Now make sure we aren't trying to spawn on top of another object
                if (!Physics.CheckBox(randomSpawnPos, new Vector3(1f, 0.2f, 1f)))
                {
                    positionFound = true;
                }
                // TODO:Maybe adjust the position a little if we havent found it yet, instead of trying to check all over
            }

            tries++;
        } while (!positionFound && tries < 100);

        if (!positionFound)
        {
            Debug.LogWarning($"No spawn position found for agent in team {mTeam}, too many agents in the environment?");
        }

        return randomSpawnPos;
    }

    public void Zombify(HumanoidAgent zombie, HumanoidAgent human)
    {
        Debug.Log("Human turned!");
        _humansGroup.AddGroupReward(-1f);
        _zombiesGroup.AddGroupReward(1f);
        _humansGroup.UnregisterAgent(human);
        Destroy(human.gameObject);
        _agentList.Remove(human);
        
        var m_zombie = Instantiate(_zombiePrefab, human.transform.position,
             human.transform.rotation,transform);
        var agent = m_zombie.GetComponent<HumanoidAgent>();
        agent.SetTeam(Team.Zombie);
        
        _zombiesGroup.RegisterAgent(agent);
        _agentList.Add(agent);
        
        // Zombies win!
        if (_humansGroup.GetRegisteredAgents().Count <= 0)
        {
            Debug.Log("Zombies win");
            _zombiesGroup.AddGroupReward(5f);
            _humansGroup.AddGroupReward(-5f);

            _zombiesGroup.EndGroupEpisode();
            _humansGroup.EndGroupEpisode();
            ResetScene();
        }
    }

    public void KillZombie(HumanoidAgent human, HumanoidAgent zombie)
    {
        Debug.Log("Zombie killed!");
        _humansGroup.AddGroupReward(1f);
        _zombiesGroup.AddGroupReward(-1f);
        _zombiesGroup.UnregisterAgent(zombie);
        Destroy(zombie.gameObject);
        _agentList.Remove(zombie);
        // Humans win!
        if (_zombiesGroup.GetRegisteredAgents().Count <= 0)
        {
            Debug.Log("Humans win");
            _zombiesGroup.AddGroupReward(-5f);
            _humansGroup.AddGroupReward(5f);

            _zombiesGroup.EndGroupEpisode();
            _humansGroup.EndGroupEpisode();
            
            ResetScene();
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace CCengine
{
    public class EntityFactory
    {
    //     [SerializeField] private GameObject towerPrefab;
    //     [SerializeField] private GameObject creepPrefab;
    //     [SerializeField] private GameObject heroPrefab;

    //     private int nextEntityId = 1; // Simple ID generator

    //     public EntityBehavior CreateTower(Vector3 position, int health)
    //     {
    //         int id = GenerateUniqueId();
    //         Tower data = new Tower(id, position, health);

    //         GameObject towerObject = GameObject.Instantiate(towerPrefab, position, Quaternion.identity);
    //         EntityBehavior entityBehavior = towerObject.GetComponent<EntityBehavior>();
    //         entityBehavior.Initialize(data);

    //         return entityBehavior;
    //     }

    //     public EntityBehavior CreateCreep(Vector3 position, float speed, Vector3[] path)
    //     {
    //         int id = GenerateUniqueId();
    //         CreepData data = new CreepData(id, position, speed, path);

    //         GameObject creepObject = GameObject.Instantiate(creepPrefab, position, Quaternion.identity);
    //         EntityBehavior entityBehavior = creepObject.GetComponent<EntityBehavior>();
    //         entityBehavior.Initialize(data);

    //         // Set up creep movement along the path
    //         // This could be handled in EntityBehavior based on data

    //         return entityBehavior;
    //     }

    //     public EntityBehavior CreateHero(Vector3 position, string name)
    //     {
    //         int id = GenerateUniqueId();
    //         HeroData data = new HeroData(id, position, name);

    //         GameObject heroObject = GameObject.Instantiate(heroPrefab, position, Quaternion.identity);
    //         EntityBehavior entityBehavior = heroObject.GetComponent<EntityBehavior>();
    //         entityBehavior.Initialize(data);

    //         return entityBehavior;
    //     }

    //     private int GenerateUniqueId()
    //     {
    //         return nextEntityId++;
    //     }
    }
}
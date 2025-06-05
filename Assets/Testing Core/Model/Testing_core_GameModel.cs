using System.Collections.Generic;
using Core.Events;
using Core.Model;
using Testing_Core.Model.Events;
using UnityEngine;
using Zenject;

namespace Testing_Core.Model
{
    public class Testing_core_GameModel : Entity
    {
        [Inject] private readonly GameConfig GameConfig = null!;
        [Inject] private readonly EventQueue EventQueue = null!;

        [Inject] private readonly Testing_core_EnemyLogic EnemyLogic = null!;
        
        public readonly List<EntId> Enemies = new List<EntId>();
        
        public void Start()
        {

            Enemies.Add(EnemyLogic.SpawnEnemy(100, Vector2.one, GameConfig.EnemyPrefab));
            Enemies.Add(EnemyLogic.SpawnEnemy(100, Vector2.one * 5, GameConfig.EnemyPrefab));
            Enemies.Add(EnemyLogic.SpawnEnemy(100, Vector2.one * 10, GameConfig.EnemyPrefab));
            
            StartGameEvent newEvent = EventQueue.Execute<StartGameEvent>();
            newEvent.TestArgument = 666;

            PostStartGameEvent secondEvent = EventQueue.Execute<PostStartGameEvent>();
            
            return;
        }

    }
}
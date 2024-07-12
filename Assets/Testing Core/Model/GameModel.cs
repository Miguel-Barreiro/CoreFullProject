using System.Collections.Generic;
using Core.Events;
using Core.Model;
using Testing_Core.Model.Events;
using UnityEngine;
using Zenject;

namespace Testing_Core.Model
{
    public class GameModel : BaseEntity
    {
        [Inject] private readonly GameConfig GameConfig = null!;
        [Inject] private readonly EventQueue EventQueue = null!;
        
        public readonly List<EnemyEntity> Enemies = new List<EnemyEntity>();
        
        public void Start()
        {
            Enemies.Add(new EnemyEntity(100, Vector2.one, GameConfig.EnemyPrefab));
            Enemies.Add(new EnemyEntity(100, Vector2.one * 5, GameConfig.EnemyPrefab));
            Enemies.Add(new EnemyEntity(100, Vector2.one * 10, GameConfig.EnemyPrefab));

            StartGameEvent newEvent = EventQueue.Execute<StartGameEvent>();
            newEvent.TestArgument = 666;
            
            
            return;
        }
        
    }
}
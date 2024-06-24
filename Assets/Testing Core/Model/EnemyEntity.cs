using Core.Model;
using Core.Systems;
using Testing_Core.Components;
using UnityEngine;

namespace Testing_Core.Model
{
    public sealed class EnemyEntity : BaseEntity, IPositionEntity, IAlive
    {
        public EnemyEntity(int maxHealth, Vector2 startPosition, GameObject prefab)
        {
            MaxHealth = maxHealth;
            Health = maxHealth;
            Position = startPosition;
            Prefab = prefab;
            IsAlive = true;
        }

        #region Alive

        public bool IsAlive { get; set; }
        public int Health { get; set; } 
        public int MaxHealth { get; }

        #endregion


        #region Position

        public Vector2 Position { get; }
        public GameObject Prefab { get; }

         #endregion
 
    }
}
﻿using Core.Model;
using Testing_Core.Components;
using UnityEngine;

namespace Testing_Core.Model
{
    public sealed class EnemyComponent : Entity, IAlive
    {
        // public EnemyEntity(int maxHealth, Vector2 startPosition, GameObject prefab)
        // {
        //     MaxHealth = maxHealth;
        //     Health = maxHealth;
        //     Position = startPosition;
        //     Prefab = prefab;
        //     IsAlive = true;
        // }

        // #region Alive
        //
        // public bool IsAlive { get; set; }
        // public int Health { get; set; } 
        // public int MaxHealth { get; set; }
        //
        // #endregion
        //
        //
        // #region Position
        //
        // public Vector2 Position { get; set; }
        // public GameObject Prefab { get; private set; }
        //
        // #endregion
        //
        // public EntId ID { get; set; }
    }
}
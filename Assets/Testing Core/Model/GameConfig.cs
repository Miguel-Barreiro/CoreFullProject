using Core.VSEngine;
using UnityEngine;
using XNode;

namespace Testing_Core.Model
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Testing Core/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        public int PlayerHealth;
        
        public GameObject EnemyPrefab;
        
        public ActionGraph EnemyBehaviorGraph;
        
    }
}
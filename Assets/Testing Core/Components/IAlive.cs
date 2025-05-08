using System.ComponentModel;
using Core.Model;
using Core.Model.ModelSystems;

namespace Testing_Core.Components
{
    
    public struct AliveComponentData : IComponentData
    {
        public EntId ID { get; set; }
        public bool IsAlive { get; set; }
        public int Health { get; set; }
        
        public int MaxHealth { get; set; }
        
        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                IsAlive = false;
            }
        }
    }

    public interface IAlive : Component<AliveComponentData>
    {
        // public EntId ID { get; set; }
        // public bool IsAlive { get; set; }
        // public int Health { get; set; }
        //
        // public int MaxHealth { get; set; }
        //
        // public void TakeDamage(int damage)
        // {
        //     Health -= damage;
        //     if (Health <= 0)
        //     {
        //         IsAlive = false;
        //     }
        // }
    }
}
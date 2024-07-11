using Core.Model;

namespace Testing_Core.Components
{
    public interface IAlive : IComponent
    {
        public bool IsAlive { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; }
        
        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                IsAlive = false;
            }
        }
    }
}
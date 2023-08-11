using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECS3D.ECSEngine.Internal
{
    public class GameEntity
    {
        public Engine Engine { get; set; }

        public int Id { get; internal set; }
        public string EntityName { get; set; }

        public List<GameEntity> Children = new List<GameEntity>();
        public Dictionary<Type, EntityComponent> Components { get; } = new Dictionary<Type, EntityComponent>();

        public GameEntity CreateChild(string name)
        {
            var child = Engine.CreateEntity(this, name);
            return child;
        }

        public T AddComponent<T>(T component) where T : EntityComponent
        {
            var componentType = component.GetType();
            Components.Add(componentType, component);
            component.Engine = Engine;
            component.Entity = this;
            return (T)component;
        }

        public T GetComponent<T>() where T : EntityComponent
        {
            var type = typeof(T);
            return (T)Components[type];
        }

        public void Awake()
        {
            foreach (var component in Components.Values)
            {
                component.Awake();
            }
        }

        public void Update()
        {
            foreach (var component in Components.Values)
            {
                component.EarlyUpdate();
                component.Update();
                component.LateUpdate();
            }
        }
    }
}

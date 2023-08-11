using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECS3D.ECSEngine.Internal
{
    public abstract class EntityComponent
    {
        public Engine Engine;
        public GameEntity Entity { get; internal set; }
        public bool Enabled { get; internal set; } = true;

        public void Enable()
        {
            if (!Enabled)
            {
                OnEnable();
                Enabled = true;

            }
        }

        public void Disable()
        {
            if (Enabled)
            {
                OnDisable();
                Enabled = false;

            }
        }

        public virtual void Awake()
        {

        }

        public virtual void OnEnable()
        {

        }

        public virtual void OnDisable()
        {

        }

        public virtual void EarlyUpdate()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void LateUpdate()
        {

        }
    }
}

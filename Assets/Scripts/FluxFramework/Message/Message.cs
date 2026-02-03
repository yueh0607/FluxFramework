namespace FluxFramework
{
    public abstract class Message : IPoolable
    {
        public uint SenderId { get; set; }
        public uint TargetId { get; set; }

        public virtual void OnSpawn()
        {
            SenderId = 0;
            TargetId = 0;
        }

        public virtual void OnDespawn()
        {
            SenderId = 0;
            TargetId = 0;
        }
    }
}

namespace FluxFramework
{
    public struct TickEventArgs
    {
        public float DeltaTime;
        public bool Handled;
    }

    public abstract class EventArgs : IPoolable
    {
        public bool Handled { get; set; }

        public virtual void OnSpawn()
        {
            Handled = false;
        }

        public virtual void OnDespawn()
        {
            Handled = false;
        }
    }
}

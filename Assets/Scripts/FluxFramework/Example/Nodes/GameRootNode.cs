namespace FluxFramework.Example
{
    /// <summary>
    /// 游戏根视图节点
    /// 所有游戏物体的父节点
    /// </summary>
    public class GameRootNode : ViewNode
    {
        public override void OnSpawn()
        {
            base.OnSpawn();
            CreateEmpty("GameRoot");
        }
    }
}

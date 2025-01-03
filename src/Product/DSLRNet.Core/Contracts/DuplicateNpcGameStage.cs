namespace DSLRNet.Core.Contracts;

using DSLRNet.Core.DAL;

public class DuplicateNpcGameStage
{
    public int NpcID { get; set; }

    public GameStage GameStage { get; set; }

    public bool RequiresNewItemLot { get; set; }
}

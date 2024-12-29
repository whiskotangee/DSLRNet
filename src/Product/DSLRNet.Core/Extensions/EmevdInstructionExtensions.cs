namespace DSLRNet.Core.Extensions;

public static class EmevdInstructionExtensions
{
    public static bool IsProcessAwardItemLot(this EMEVD.Instruction instruction)
    {
        return instruction.Bank == 2003 && instruction.ID == 4;
    }

    public static bool IsProcessUnknown200476(this EMEVD.Instruction instruction)
    {
        return instruction.Bank == 2004 && instruction.ID == 76;
    }

    public static bool IsProcessHandleBossDefeatAndDisplayBanner(this EMEVD.Instruction instruction)
    {
        return instruction.Bank == 2003 && instruction.ID == 12;
    }

    public static bool IsProcessSetEventFlagID(this EMEVD.Instruction instruction)
    {
        return instruction.Bank == 2003 && instruction.ID == 66;
    }

    public static bool IsProcessAwardItemsIncludingClients(this EMEVD.Instruction instruction)
    {
        return instruction.Bank == 2003 && instruction.ID == 36;
    }

    public static bool IsInitializeEvent(this EMEVD.Instruction instruction)
    {
        return instruction.Bank == 2000 && (instruction.ID == 0 || instruction.ID == 6);
    }
}

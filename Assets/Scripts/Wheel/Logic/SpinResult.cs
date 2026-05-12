public struct SpinResult
{
    public int sliceIndex;
    public int amount;
    public bool isDeath;
    public bool isValid;

    public static SpinResult Invalid => new SpinResult { isValid = false, sliceIndex = -1 };
}

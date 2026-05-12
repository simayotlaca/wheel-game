public class SliceHighlightAnimator
{
    private SliceView[] slices;

    public void Init(SliceView[] slices)
    {
        this.slices = slices;
    }

    public void HighlightWinner(int sliceIndex)
    {
        if (slices == null) return;
        if (sliceIndex < 0 || sliceIndex >= slices.Length) return;
        if (slices[sliceIndex] != null) slices[sliceIndex].Highlight();
    }

    public void DimNonWinners(int winnerIndex)
    {
        if (slices == null) return;
        for (int i = 0; i < slices.Length; i++)
        {
            if (slices[i] != null) slices[i].SetDimmed(i != winnerIndex);
        }
    }

    public void UndimAll()
    {
        if (slices == null) return;
        for (int i = 0; i < slices.Length; i++)
        {
            if (slices[i] != null) slices[i].SetDimmed(false);
        }
    }
}

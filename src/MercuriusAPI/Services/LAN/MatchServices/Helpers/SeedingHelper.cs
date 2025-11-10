namespace MercuriusAPI.Services.LAN.MatchServices.Helpers;

public static class SeedingHelper
{
    public static int[] GenerateBracketSlotOrder(int slotCount)
    {
        int[] result = new int[slotCount];
        int half = slotCount / 2;

        int middleLeft = half - 1;
        int middleRight = half;

        result[0] = 0;
        result[1] = slotCount - 1;

        for (int i = 2; i < slotCount; i++)
        {
            if (i % 2 == 0)
            {
                result[i] = middleLeft;
                middleLeft--;
            }
            else
            {
                result[i] = middleRight;
                middleRight++;
            }
        }

        return result;
    }
}

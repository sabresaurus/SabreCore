using Unity.Collections;

namespace Sabresaurus.SabreSlice
{
    public enum Classification
    {
        Front,
        Straddle,
        Back
    }

    public class Classifier
    {
        public static Classification Classify(int index1, int index2, int index3, NativeArray<float> classificationArray)
        {
            int numberInFront = 0;
            int numberBehind = 0;

            if (classificationArray[index1] == 1)
                numberInFront++;
            else
                numberBehind++;

            if (classificationArray[index2] == 1)
                numberInFront++;
            else
                numberBehind++;

            if (classificationArray[index3] == 1)
                numberInFront++;
            else
                numberBehind++;

            if (numberInFront == 0) // None in front, all must be behind
                return Classification.Back;
            if (numberBehind == 0) // None behind, all must be in front
                return Classification.Front;

            return Classification.Straddle;
        }
    }
}
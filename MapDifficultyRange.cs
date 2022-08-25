namespace OsuVideoUploader
{
    public class MapDifficultyRange
    {
        private float min;
        private float mid;
        private float max;

        public MapDifficultyRange(float min, float mid, float max)
        {
            this.min = min;
            this.mid = mid;
            this.max = max;
        }

        public float ValueFor(float difficulty)
        {
            if (difficulty > 5)
                return mid + (max - mid) * (difficulty - 5) / 5;
            if (difficulty < 5)
                return mid - (mid - min) * (5 - difficulty) / 5;
            return mid;
        }

        public float DifficultyFor(float val)
        {
            if (val < mid) // > 5.0f (inverted)
                return (val * 5.0f - mid * 5.0f) / (max - mid) + 5;

            if (val > mid) // < 5.0f (inverted)
                return 5 - (mid * 5.0f - val * 5.0f) / (mid - min);

            return 5;
        }
    }
}

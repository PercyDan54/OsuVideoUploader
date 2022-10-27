namespace OsuVideoUploader
{
    public static class TimeSpanUtil
    {
        public static string FormatTime(TimeSpan timeSpan)
        {
            string format = "m\\:ss";
            if (timeSpan.TotalMinutes >= 10)
            {
                format = "m" + format;
            }

            if (timeSpan.TotalHours >= 1)
            {
                format = "h\\:" + format;
                if (timeSpan.TotalHours >= 10)
                {
                    format = "h" + format;
                }
            }

            return timeSpan.ToString(format);
        }
    }
}

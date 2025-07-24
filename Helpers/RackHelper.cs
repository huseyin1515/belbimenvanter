using System;

namespace BelbimEnv.Helpers
{
    public static class RackHelper
    {
        public static (int, int) ParseKabinU(string? kabinU)
        {
            if (string.IsNullOrWhiteSpace(kabinU)) return (0, 0);
            try
            {
                if (kabinU.Contains('-'))
                {
                    var parts = kabinU.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int start) && int.TryParse(parts[1].Trim(), out int end))
                    {
                        return (Math.Min(start, end), Math.Max(start, end));
                    }
                }
                else if (int.TryParse(kabinU.Trim(), out int singleU))
                {
                    return (singleU, singleU);
                }
            }
            catch { return (0, 0); }
            return (0, 0);
        }
    }
}
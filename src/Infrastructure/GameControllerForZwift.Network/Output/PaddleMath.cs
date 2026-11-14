namespace GameControllerForZwift.Network.Output
{
    /// <summary>Maps steer/brake axes to the analog ZL (location 0) / ZR (location 1) paddle values of the Zwift Ride.</summary>
    internal static class PaddleMath
    {
        /// <summary>
        /// ZL: outward (negative) = steer left, inward (positive) = brake. ZR: outward (positive) = steer right, inward (negative) = brake.
        /// When steering and braking compete for the same paddle the larger magnitude wins (steer on ties).
        /// </summary>
        public static (sbyte Zl, sbyte Zr) Compute(double steer, double brake)
        {
            int s = Scale(steer, -100, 100);
            int b = Scale(brake, 0, 100);
            int left = Math.Min(s, 0);
            int right = Math.Max(s, 0);
            int zl = -left >= b ? left : b;
            int zr = right >= b ? right : -b;
            return ((sbyte)zl, (sbyte)zr);
        }

        private static int Scale(double v, int min, int max)
            => double.IsNaN(v) ? 0 : (int)Math.Clamp(Math.Round(100 * v, MidpointRounding.AwayFromZero), min, max);
    }
}

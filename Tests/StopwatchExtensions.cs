namespace System.Diagnostics
{
    /// <summary>
    /// <see cref="Stopwatch" /> の計測用拡張メソッド。
    /// </summary>
    public static class StopwatchExtensions
    {
        /// <summary>
        /// 指定回数 action を実行し、合計経過ミリ秒を返す。
        /// </summary>
        /// <param name="sw">計測に使うストップウォッチ。</param>
        /// <param name="action">実行する処理。</param>
        /// <param name="iterations">実行回数。</param>
        /// <returns>経過時間（ミリ秒）。</returns>
        public static long Time(this Stopwatch sw, Action action, int iterations)
        {
            sw.Reset();
            sw.Start();
            for (var i = 0; i < iterations; i++)
            {
                action();
            }
            sw.Stop();

            return sw.ElapsedMilliseconds;
        }

        /// <summary>
        /// action を 1 回実行し、経過ミリ秒を返す。
        /// </summary>
        /// <param name="sw">計測に使うストップウォッチ。</param>
        /// <param name="action">実行する処理。</param>
        /// <returns>経過時間（ミリ秒）。</returns>
        public static long Time(this Stopwatch sw, Action action)
        {
            return Time(sw, action, 1);
        }
    }
}

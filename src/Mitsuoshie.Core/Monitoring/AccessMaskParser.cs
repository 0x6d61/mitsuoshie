namespace Mitsuoshie.Core.Monitoring;

/// <summary>
/// AccessMask 文字列（"0x1", "0x10002" 等）を int にパースする共通ユーティリティ。
/// </summary>
internal static class AccessMaskParser
{
    /// <summary>
    /// AccessMask 文字列を int にパースする。
    /// "0x" プレフィックスがあれば16進数、なければ10進数として解釈する。
    /// </summary>
    public static bool TryParse(string accessMask, out int mask)
    {
        mask = 0;
        if (string.IsNullOrEmpty(accessMask)) return false;

        if (accessMask.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(accessMask.AsSpan(2),
                System.Globalization.NumberStyles.HexNumber, null, out mask);
        }

        return int.TryParse(accessMask, out mask);
    }
}

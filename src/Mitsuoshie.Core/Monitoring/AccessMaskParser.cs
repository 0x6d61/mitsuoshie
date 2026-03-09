namespace Mitsuoshie.Core.Monitoring;

/// <summary>
/// AccessMask 文字列（"0x1", "0x10002", "0x80000000" 等）を int にパースする共通ユーティリティ。
/// Windows ACCESS_MASK は 32bit 符号なし値のため、内部で uint パースして unchecked キャストする。
/// </summary>
internal static class AccessMaskParser
{
    /// <summary>
    /// AccessMask 文字列を int にパースする。
    /// "0x" プレフィックスがあれば16進数、なければ10進数として解釈する。
    /// bit31（GENERIC_READ 等）を含む値も正しくパースされる。
    /// </summary>
    public static bool TryParse(string accessMask, out int mask)
    {
        mask = 0;
        if (string.IsNullOrEmpty(accessMask)) return false;

        uint raw;
        bool success;
        if (accessMask.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            success = uint.TryParse(accessMask.AsSpan(2),
                System.Globalization.NumberStyles.HexNumber, null, out raw);
        }
        else
        {
            success = uint.TryParse(accessMask, out raw);
        }

        if (success)
        {
            mask = unchecked((int)raw);
        }
        return success;
    }
}

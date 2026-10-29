using System.Text;

namespace CodeReviewCrew.Core.Diff;

/// <summary>Groups diff files into chunks small enough to fit in a single model request.</summary>
public static class DiffChunker
{
    public const string TruncationMarker = "\n[... diff truncated: file too large for a single analysis ...]\n";

    public static IReadOnlyList<string> Chunk(IEnumerable<DiffFile> files, int maxChars)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxChars, 1000);

        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var file in files)
        {
            var text = file.Text.Length > maxChars
                ? string.Concat(file.Text.AsSpan(0, maxChars - TruncationMarker.Length), TruncationMarker)
                : file.Text;

            if (current.Length > 0 && current.Length + text.Length > maxChars)
            {
                chunks.Add(current.ToString());
                current.Clear();
            }

            current.Append(text);
        }

        if (current.Length > 0)
        {
            chunks.Add(current.ToString());
        }

        return chunks;
    }
}

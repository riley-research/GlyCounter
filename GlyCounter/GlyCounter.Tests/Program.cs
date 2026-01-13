using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

internal static class Program
{
    static int Main(string[] args)
    {
        string solutionRoot = AppContext.BaseDirectory;
        string maybeSolution = Path.GetFullPath(Path.Combine(solutionRoot, "..", "..", "..", ".."));
        string expected = args.Length > 0 ? args[0] : Path.Combine(maybeSolution, "GlyCounter", "TestData", "Expected");
        string actual = args.Length > 1 ? args[1] : Path.Combine(maybeSolution, "GlyCounter", "TestData", "Actual");
        double numericTolerance = args.Length > 2 && double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var t) ? t : 1e-6;
        int maxDiffReports = args.Length > 3 && int.TryParse(args[3], out var m) ? Math.Max(1, m) : 2000;

        Console.WriteLine($"Expected: {expected}");
        Console.WriteLine($"Actual:   {actual}");
        Console.WriteLine($"Numeric tolerance: {numericTolerance}");

        if (!Directory.Exists(expected))
        {
            Console.Error.WriteLine("Expected directory not found: " + expected);
            return 2;
        }

        if (!Directory.Exists(actual))
        {
            Console.Error.WriteLine("Actual directory not found: " + actual);
            return 3;
        }

        var reportDir = Path.Combine(Path.GetDirectoryName(actual) ?? actual, "TestResults");
        Directory.CreateDirectory(reportDir);
        string reportPath = Path.Combine(reportDir, $"diff_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        // Group diffs by relative file path
        var diffsByFile = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        int diffsCount = 0;

        var expectedFiles = Directory.GetFiles(expected, "*", SearchOption.AllDirectories)
            .Select(p => MakeRelative(p, expected)).OrderBy(p => p).ToArray();

        foreach (var relPath in expectedFiles)
        {
            string expFull = Path.Combine(expected, relPath);
            string actFull = Path.Combine(actual, relPath);

            if (!File.Exists(actFull))
            {
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffReports, relPath, $"MISSING: expected file exists, actual file missing"))
                    break;
                continue;
            }

            // Try TSV-by-scan comparison, else fallback to line-by-line.
            try
            {
                bool usedTsv = CompareByScanIfPossible(expFull, actFull, relPath, diffsByFile, ref diffsCount, numericTolerance, maxDiffReports);
                if (!usedTsv)
                {
                    if (CompareLineByLine(expFull, actFull, relPath, diffsByFile, ref diffsCount, maxDiffReports))
                        break;
                }
            }
            catch (Exception ex)
            {
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffReports, relPath, $"ERROR comparing: {ex.Message}"))
                    break;
            }

            if (diffsCount >= maxDiffReports) break;
        }

        // Unexpected files in actual
        var actualFiles = Directory.GetFiles(actual, "*", SearchOption.AllDirectories)
            .Select(p => MakeRelative(p, actual)).OrderBy(p => p).ToArray();
        foreach (var relPath in actualFiles)
        {
            if (!expectedFiles.Contains(relPath))
            {
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffReports, relPath, $"UNEXPECTED: exists in actual but not in expected"))
                    break;
            }
            if (diffsCount >= maxDiffReports) break;
        }

        // Build final report grouped by file with short summary header
        var reportLines = new List<string>();
        if (diffsByFile.Count == 0)
        {
            reportLines.Add("No differences found.");
        }
        else
        {
            int total = 0;
            foreach (var kvp in diffsByFile.OrderBy(k => k.Key))
            {
                var rel = kvp.Key;
                var list = kvp.Value;
                reportLines.Add($"File: {rel} — {list.Count} difference(s)");
                reportLines.AddRange(list.Select(l => $"  {l}"));
                reportLines.Add(string.Empty);
                total += list.Count;
            }
            reportLines.Insert(0, $"Total files with differences: {diffsByFile.Count}. Total differences: {total}");
            reportLines.Insert(1, "");
        }

        File.WriteAllLines(reportPath, reportLines, Encoding.UTF8);

        Console.WriteLine($"Report written to: {reportPath}");
        if (diffsByFile.Count > 0)
        {
            Console.WriteLine($"Differences found in {diffsByFile.Count} file(s).");
            return 1;
        }

        Console.WriteLine("No differences found.");
        return 0;
    }

    // Adds a diff message to the dictionary for the given relative file path.
    // Returns true when maximum report count is reached (caller should stop processing).
    private static bool AddDiff(Dictionary<string, List<string>> byFile, ref int diffsCount, int maxDiffReports, string relPath, string message)
    {
        if (!byFile.TryGetValue(relPath, out var list))
        {
            list = new List<string>();
            byFile[relPath] = list;
        }
        list.Add(message);
        diffsCount++;
        return diffsCount >= maxDiffReports;
    }

    private static bool CompareByScanIfPossible(string expectedFile, string actualFile, string relPath,
        Dictionary<string, List<string>> diffsByFile, ref int diffsCount, double tol, int maxDiffs)
    {
        var expAllLines = File.ReadAllLines(expectedFile, Encoding.UTF8).Where(l => l != null).ToArray();
        var actAllLines = File.ReadAllLines(actualFile, Encoding.UTF8).Where(l => l != null).ToArray();

        if (expAllLines.Length == 0 || actAllLines.Length == 0)
        {
            AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, "ERROR: one of the files is empty.");
            return false;
        }

        int expScanIndex = FindScanIndex(expAllLines);
        int actScanIndex = FindScanIndex(actAllLines);

        if (expScanIndex < 0 || actScanIndex < 0)
            return false;

        string[] expCols = SplitLine(expAllLines[0]);
        string[] actCols = SplitLine(actAllLines[0]);

        bool expHasHeader = LooksLikeHeader(expAllLines[0], expAllLines.Skip(1).Take(10).ToArray());
        bool actHasHeader = LooksLikeHeader(actAllLines[0], actAllLines.Skip(1).Take(10).ToArray());

        IEnumerable<string> expDataLines = expHasHeader ? expAllLines.Skip(1) : expAllLines;
        IEnumerable<string> actDataLines = actHasHeader ? actAllLines.Skip(1) : actAllLines;

        var expDict = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);
        var actDict = new Dictionary<string, List<string[]>>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in expDataLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = SplitLine(line);
            string key = fields.Length > expScanIndex ? fields[expScanIndex] : "<NO_SCAN>";
            if (!expDict.TryGetValue(key, out var list)) { list = new List<string[]>(); expDict[key] = list; }
            list.Add(fields);
        }

        foreach (var line in actDataLines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = SplitLine(line);
            string key = fields.Length > actScanIndex ? fields[actScanIndex] : "<NO_SCAN>";
            if (!actDict.TryGetValue(key, out var list)) { list = new List<string[]>(); actDict[key] = list; }
            list.Add(fields);
        }

        var allKeys = new HashSet<string>(expDict.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(actDict.Keys);

        foreach (var key in allKeys.OrderBy(k => k))
        {
            bool inExp = expDict.TryGetValue(key, out var expRows);
            bool inAct = actDict.TryGetValue(key, out var actRows);

            if (!inExp)
            {
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"MISSING_SCAN: Scan {key} present in actual but not in expected."))
                    return true;
                continue;
            }

            if (!inAct)
            {
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"MISSING_SCAN: Scan {key} present in expected but not in actual."))
                    return true;
                continue;
            }

            expRows = expRows.OrderBy(r => string.Join("\t", r)).ToList();
            actRows = actRows.OrderBy(r => string.Join("\t", r)).ToList();

            int rowCount = Math.Max(expRows.Count, actRows.Count);
            for (int i = 0; i < rowCount; i++)
            {
                if (i >= expRows.Count)
                {
                    if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"EXTRA_ROW: Scan {key} has extra row in actual (index {i})."))
                        return true;
                    break;
                }
                if (i >= actRows.Count)
                {
                    if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"MISSING_ROW: Scan {key} missing row in actual (expected index {i})."))
                        return true;
                    break;
                }

                if (CompareRowFields(expectedFile, actualFile, relPath, key, expCols, actCols, expRows[i], actRows[i], diffsByFile, ref diffsCount, tol, maxDiffs))
                    return true;
            }

            if (diffsCount >= maxDiffs) return true;
        }

        return true;
    }

    private static bool CompareRowFields(string expectedFile, string actualFile, string relPath, string key,
        string[] expCols, string[] actCols, string[] expFields, string[] actFields,
        Dictionary<string, List<string>> diffsByFile, ref int diffsCount, double tol, int maxDiffs)
    {
        int maxCols = Math.Max(expCols.Length, actCols.Length);
        for (int c = 0; c < maxCols; c++)
        {
            string colName = (c < expCols.Length ? expCols[c] : (c < actCols.Length ? actCols[c] : $"Col{c}"));
            string expVal = c < expFields.Length ? expFields[c] : "<MISSING>";
            string actVal = c < actFields.Length ? actFields[c] : "<MISSING>";

            if (string.Equals(expVal, actVal, StringComparison.Ordinal))
                continue;

            if (double.TryParse(expVal, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dExp) &&
                double.TryParse(actVal, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dAct))
            {
                if (double.IsNaN(dExp) && double.IsNaN(dAct)) continue;
                if (Math.Abs(dExp - dAct) <= tol) continue;
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"DIFF_SCAN:{key}\tColumn:{colName}\tExpected:{dExp}\tActual:{dAct}"))
                    return true;
            }
            else
            {
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"DIFF_SCAN:{key}\tColumn:{colName}\tExpected:{Truncate(expVal, 400)}\tActual:{Truncate(actVal, 400)}"))
                    return true;
            }

            if (diffsCount >= maxDiffs) return true;
        }

        return false;
    }

    private static bool CompareLineByLine(string expectedFile, string actualFile, string relPath,
        Dictionary<string, List<string>> diffsByFile, ref int diffsCount, int maxDiffs)
    {
        var expLines = File.ReadAllLines(expectedFile, Encoding.UTF8);
        var actLines = File.ReadAllLines(actualFile, Encoding.UTF8);
        int max = Math.Max(expLines.Length, actLines.Length);
        for (int i = 0; i < max; i++)
        {
            string e = i < expLines.Length ? expLines[i] : "<EOF>";
            string a = i < actLines.Length ? actLines[i] : "<EOF>";
            if (!string.Equals(e, a, StringComparison.Ordinal))
            {
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"DIFF_LINE (line {i + 1}):"))
                    return true;
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"  Expected: {Truncate(e, 400)}"))
                    return true;
                if (AddDiff(diffsByFile, ref diffsCount, maxDiffs, relPath, $"  Actual:   {Truncate(a, 400)}"))
                    return true;
            }
            if (diffsCount >= maxDiffs) return true;
        }
        return false;
    }

    private static int FindScanIndex(string[] lines)
    {
        if (lines.Length > 0)
        {
            var first = lines[0];
            int idx = IndexOfScanColumn(first);
            if (idx >= 0) return idx;
        }
        return InferScanIndexFromData(lines, maxLinesToInspect: 200);
    }

    private static int IndexOfScanColumn(string headerLine)
    {
        if (string.IsNullOrWhiteSpace(headerLine)) return -1;
        var cols = SplitLine(headerLine);
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i].Trim().ToLowerInvariant();
            if (c == "scannumber" || c == "scan" || c == "scan#" || c == "scan number" || c == "scan_num" || c == "scannr" || c == "scanid")
                return i;
        }
        return -1;
    }

    private static int InferScanIndexFromData(string[] lines, int maxLinesToInspect = 100)
    {
        var data = lines.Where(l => !string.IsNullOrWhiteSpace(l)).Take(maxLinesToInspect).ToArray();
        if (data.Length == 0) return -1;

        var firstCols = SplitLine(data[0]);
        int colCount = firstCols.Length;
        if (colCount == 0) return -1;

        int[] integerCounts = new int[colCount];
        int linesConsidered = 0;

        foreach (var line in data)
        {
            var cols = SplitLine(line);
            if (cols.Length == 0) continue;
            linesConsidered++;
            for (int c = 0; c < colCount; c++)
            {
                if (c < cols.Length && int.TryParse(cols[c], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    integerCounts[c]++;
            }
        }

        if (linesConsidered == 0) return -1;

        int best = -1;
        double bestFraction = 0.0;
        for (int c = 0; c < colCount; c++)
        {
            double fraction = (double)integerCounts[c] / linesConsidered;
            if (fraction > bestFraction)
            {
                bestFraction = fraction;
                best = c;
            }
        }

        if (best >= 0 && bestFraction >= 0.7) return best;

        return -1;
    }

    private static bool LooksLikeHeader(string firstLine, string[] nextLines)
    {
        var firstCols = SplitLine(firstLine);
        if (firstCols.Length == 0) return false;
        int numericCountNext = 0, totalNext = 0;
        foreach (var l in nextLines)
        {
            if (string.IsNullOrWhiteSpace(l)) continue;
            var cols = SplitLine(l);
            totalNext++;
            if (cols.Length > 0 && double.TryParse(cols[0], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                numericCountNext++;
        }

        if (totalNext > 0 && numericCountNext >= Math.Max(1, totalNext / 2))
        {
            if (!double.TryParse(firstCols[0], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _))
                return true;
        }

        if (IndexOfScanColumn(firstLine) >= 0) return true;

        return false;
    }

    private static string[] SplitLine(string line)
    {
        if (line == null) return Array.Empty<string>();
        if (line.Contains('\t')) return line.Split('\t');
        return line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string MakeRelative(string fullPath, string baseDir)
    {
        var p = Path.GetFullPath(fullPath);
        var b = Path.GetFullPath(baseDir);
        if (!b.EndsWith(Path.DirectorySeparatorChar)) b += Path.DirectorySeparatorChar;
        if (p.StartsWith(b, StringComparison.OrdinalIgnoreCase))
            return p.Substring(b.Length);
        return p;
    }

    private static string Truncate(string s, int maxLen) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= maxLen ? s : s.Substring(0, maxLen) + "…");
}
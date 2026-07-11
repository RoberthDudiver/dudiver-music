using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DudiverMusic.Services;

/// <summary>Orden "natural" (Track 2 antes que Track 10), igual que el Explorador de Windows.</summary>
public sealed class NaturalComparer : IComparer<string>
{
    public static readonly NaturalComparer Instance = new();

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string x, string y);

    public int Compare(string? x, string? y) => StrCmpLogicalW(x ?? "", y ?? "");
}

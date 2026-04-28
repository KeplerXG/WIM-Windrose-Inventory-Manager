# Extracts embedded files from a .NET 5+ single-file bundle (e.g. Wrapper.WindroseEditor.exe).
# Uses the bundle SHA-256 marker and Microsoft.NET.HostModel.Bundle manifest layout (runtime v8.0.0).

param(
    [Parameter(Mandatory = $true)][string] $BundlePath,
    [Parameter(Mandatory = $true)][string] $OutputDir
)

$ErrorActionPreference = 'Stop'

$marker = [byte[]]@(
    0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
    0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
    0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
    0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
)

$src = [System.IO.File]::ReadAllBytes($BundlePath)

function Find-AllMarkerPositions([byte[]]$hay, [byte[]]$needle) {
    $hits = [System.Collections.Generic.List[int]]::new()
    $n = $needle.Length
    for ($i = 0; $i -le $hay.Length - $n; $i++) {
        if ($hay[$i] -ne $needle[0]) { continue }
        $ok = $true
        for ($j = 1; $j -lt $n; $j++) {
            if ($hay[$i + $j] -ne $needle[$j]) { $ok = $false; break }
        }
        if ($ok) { $hits.Add($i) | Out-Null }
    }
    return $hits
}

$hits = Find-AllMarkerPositions $src $marker
if ($hits.Count -eq 0) {
    throw "Bundle marker not found. Not a .NET single-file bundle?"
}

Add-Type -Language CSharp @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class BundleExtract
{
    public sealed class Entry
    {
        public long Offset;
        public long Size;
        public long CompressedSize;
        public byte Type;
        public string RelativePath = "";
    }

    public sealed class Result
    {
        public long HeaderOffset;
        public uint Major;
        public uint Minor;
        public int FileCount;
        public List<Entry> Entries = new List<Entry>();
    }

    static int Read7BitEncodedInt(BinaryReader r)
    {
        int count = 0;
        int shift = 0;
        byte b;
        do
        {
            b = r.ReadByte();
            count |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return count;
    }

    static string ReadBinaryString(BinaryReader r)
    {
        int len = Read7BitEncodedInt(r);
        if (len < 0 || len > 1024 * 1024 * 64) throw new InvalidDataException("Bad string length: " + len);
        var bytes = r.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }

    public static bool TryParseManifest(byte[] file, long headerOffset, out Result parsed, out string error)
    {
        parsed = null;
        error = null;
        try
        {
            if (headerOffset < 0 || headerOffset > file.Length - 32)
            {
                error = "header OOB";
                return false;
            }

            using (MemoryStream ms = new MemoryStream(file, false))
            {
                ms.Position = headerOffset;
                using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true))
                {
                    uint major = br.ReadUInt32();
                    uint minor = br.ReadUInt32();
                    int count = br.ReadInt32();

                    if (major < 1 || major > 20 || count < 1 || count > 200000)
                    {
                        error = "bad header major=" + major + " count=" + count;
                        return false;
                    }

                    ReadBinaryString(br);

                    if (major >= 2)
                    {
                        br.ReadInt64();
                        br.ReadInt64();
                        br.ReadInt64();
                        br.ReadInt64();
                        br.ReadUInt64();
                    }

                    Result res = new Result();
                    res.HeaderOffset = headerOffset;
                    res.Major = major;
                    res.Minor = minor;
                    res.FileCount = count;

                    for (int i = 0; i < count; i++)
                    {
                        long off = br.ReadInt64();
                        long sz = br.ReadInt64();
                        long comp = 0;
                        if (major >= 6)
                        {
                            comp = br.ReadInt64();
                        }

                        byte type = br.ReadByte();
                        string path = ReadBinaryString(br);

                        if (off < 0 || sz < 0 || off > file.Length)
                        {
                            error = "bad entry off=" + off + " sz=" + sz + " path=" + path;
                            return false;
                        }
                        long need = comp > 0 ? comp : sz;
                        if (need < 0 || off + need > file.Length)
                        {
                            error = "bad entry payload OOB off=" + off + " need=" + need + " path=" + path;
                            return false;
                        }

                        Entry ent = new Entry();
                        ent.Offset = off;
                        ent.Size = sz;
                        ent.CompressedSize = comp;
                        ent.Type = type;
                        ent.RelativePath = path.Replace('\\', '/');
                        res.Entries.Add(ent);
                    }

                    parsed = res;
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static byte[] ReadPayload(byte[] file, Entry e)
    {
        if (e.CompressedSize == 0)
        {
            if (e.Offset + e.Size > file.Length) throw new InvalidDataException("Uncompressed read OOB: " + e.RelativePath);
            var buf = new byte[e.Size];
            Buffer.BlockCopy(file, (int)e.Offset, buf, 0, (int)e.Size);
            return buf;
        }

        if (e.Offset + e.CompressedSize > file.Length) throw new InvalidDataException("Compressed read OOB: " + e.RelativePath);
        var comp = new byte[e.CompressedSize];
        Buffer.BlockCopy(file, (int)e.Offset, comp, 0, (int)e.CompressedSize);

        using (MemoryStream cms = new MemoryStream(comp, false))
        using (DeflateStream dfs = new DeflateStream(cms, CompressionMode.Decompress))
        using (MemoryStream outMs = new MemoryStream())
        {
            dfs.CopyTo(outMs);
            byte[] decomp = outMs.ToArray();
            if (decomp.Length != e.Size)
            {
                throw new InvalidDataException("Decompressed size mismatch for " + e.RelativePath + ": expected " + e.Size + ", got " + decomp.Length);
            }
            return decomp;
        }
    }
}
'@

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

$chosen = $null
$chosenHeader = [long]0
foreach ($sigPos in $hits) {
    if ($sigPos -lt 8) { continue }
    $headerOff = [BitConverter]::ToInt64($src, $sigPos - 8)
    [BundleExtract+Result]$parsed = $null
    [string]$err = $null
    if ([BundleExtract]::TryParseManifest($src, [long]$headerOff, [ref]$parsed, [ref]$err)) {
        $chosen = $parsed
        $chosenHeader = $headerOff
        break
    }
}

if ($null -eq $chosen) {
    throw "Could not parse bundle manifest at any marker hit (hits=$($hits.Count)). First marker index=$($hits[0])"
}

Write-Host "Bundle major=$($chosen.Major) minor=$($chosen.Minor) files=$($chosen.FileCount) headerOffset=$chosenHeader"
foreach ($e in $chosen.Entries) {
    $rel = $e.RelativePath
    $destPath = Join-Path $OutputDir ($rel -replace '/', [IO.Path]::DirectorySeparatorChar)
    $dir = Split-Path $destPath -Parent
    if (-not [string]::IsNullOrEmpty($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    $bytes = [BundleExtract]::ReadPayload($src, $e)
    [IO.File]::WriteAllBytes($destPath, $bytes)
    Write-Host ("{0,-8} {1}" -f $e.Type, $rel)
}

Write-Host "Done. Output: $OutputDir"

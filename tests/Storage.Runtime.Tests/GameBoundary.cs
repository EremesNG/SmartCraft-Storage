// Minimal external ZDO boundary, based on the inspected native ZDOMan lifecycle.
// Replacing ZDOMan resets the native ID table; looking up an old ID is invalid.
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine { public struct Vector3 { public Vector3(float x, float y, float z) { } } }
public static class StableHash { public static int GetStableHashCode(this string s) => s.GetHashCode(); }
public static class ZLog { public static void LogWarning(string s) => Console.Error.WriteLine(s); }
public struct ZDOID : IEquatable<ZDOID>
{
    public static readonly ZDOID None = default;
    public int Generation, Number;
    public bool Equals(ZDOID other) => Generation == other.Generation && Number == other.Number;
    public override bool Equals(object other) => other is ZDOID id && Equals(id);
    public override int GetHashCode() => HashCode.Combine(Generation, Number);
    public static bool operator ==(ZDOID a, ZDOID b) => a.Equals(b);
    public static bool operator !=(ZDOID a, ZDOID b) => !a.Equals(b);
}
public sealed class ZDO
{
    public ZDOID m_uid;
    public bool Persistent, Distant;
    private long owner;
    private int prefab;
    public readonly Dictionary<string, object> Data = new();
    public long GetOwner() => owner;
    public void SetOwner(long value) => owner = value;
    public void SetPrefab(int value) => prefab = value;
    public int GetPrefab() => prefab;
    public void Set(string key, string value) => Data[key] = value;
    public void Set(string key, bool value) => Data[key] = value;
    public string GetString(string key, string fallback = "") => Data.TryGetValue(key, out var value) ? (string)value : fallback;
    public bool GetBool(string key, bool fallback = false) => Data.TryGetValue(key, out var value) ? (bool)value : fallback;
    public ZDO Copy(int generation) { var copy = new ZDO { m_uid = new ZDOID { Generation = generation, Number = m_uid.Number }, Persistent = Persistent, Distant = Distant, owner = owner, prefab = prefab }; foreach (var pair in Data) copy.Data.Add(pair.Key, pair.Value); return copy; }
}
public sealed class ZDOMan
{
    public static ZDOMan instance;
    private static int generation;
    public readonly List<ZDO> Objects;
    public ZDOMan(IEnumerable<ZDO> saved = null) { generation++; Objects = saved?.Select(x => x.Copy(generation)).ToList() ?? new(); instance = this; }
    public static long GetSessionID() => generation;
    public ZDO GetZDO(ZDOID id) { if (id.Generation != generation) throw new ArgumentOutOfRangeException(nameof(id), "obsolete native ID table"); return Objects.FirstOrDefault(x => x.m_uid == id); }
    public bool GetAllZDOsWithPrefabIterative(string prefab, List<ZDO> results, ref int index) { results.AddRange(Objects.Where(x => x.GetPrefab() == prefab.GetStableHashCode())); return true; }
    public ZDO CreateNewZDO(UnityEngine.Vector3 position, int prefab) { var value = new ZDO { m_uid = new ZDOID { Generation = generation, Number = Objects.Count == 0 ? 1 : Objects.Max(x => x.m_uid.Number) + 1 } }; value.SetPrefab(prefab); Objects.Add(value); return value; }
    public void DestroyZDO(ZDO value) => Objects.Remove(value);
}
public sealed class ZNet
{
    public static ZNet instance = new();
    public long World = 42;
    public bool IsServer() => true;
    public object GetWorld() => this;
    public long GetWorldUID() => World;
}
public sealed class ZNetScene
{
    public static ZNetScene instance = new();
    public bool HasPrefab(int prefab) => true;
}

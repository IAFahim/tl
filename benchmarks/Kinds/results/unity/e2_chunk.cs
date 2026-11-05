var sb = new System.Text.StringBuilder();
var w = new Unity.Entities.World("K0");
try {
  var em = w.EntityManager;
  var asm = typeof(K0Probe.P4).Assembly;
  foreach (int pad in new int[]{0,4,8,16,32,56,120,248,504}) {
    var types = new System.Collections.Generic.List<Unity.Entities.ComponentType>();
    int rem = pad;
    for (int k = 14; k >= 2; k--) { int s = 1 << k; while (rem >= s) { types.Add(Unity.Entities.ComponentType.ReadWrite(asm.GetType("K0Probe.P" + s))); rem -= s; if (s >= 8) break; } }
    var arch = em.CreateArchetype(types.ToArray());
    var e = em.CreateEntity(arch);
    var ch = em.GetChunk(e);
    sb.AppendLine("pad=" + pad + " comps=" + types.Count + " remUnplaced=" + rem + " archChunkCapacity=" + arch.ChunkCapacity + " chunk.Capacity=" + ch.Capacity);
  }
  var ct = typeof(Unity.Entities.EntityManager).Assembly.GetType("Unity.Entities.Chunk");
  foreach (var f in ct.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)) if (f.Name.Contains("Size") || f.Name.Contains("Max") || f.Name.Contains("Capacity")) sb.AppendLine("Chunk." + f.Name + "=" + f.GetValue(null) + " (const/static, public=" + f.IsPublic + ")");
} finally { w.Dispose(); }
return sb.ToString();

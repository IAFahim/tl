var sb = new System.Text.StringBuilder();
var w = new Unity.Entities.World("K0");
try {
  var em = w.EntityManager;
  var asm = typeof(K0Probe.P4).Assembly;
  int lo = 0, hi = 16384 * 2 - 4; string lastMsg = null; int firstFail = -1;
  System.Func<int, string> tryPad = (pad) => {
    var types = new System.Collections.Generic.List<Unity.Entities.ComponentType>();
    for (int k = 2; k <= 14; k++) if ((pad & (1 << k)) != 0) types.Add(Unity.Entities.ComponentType.ReadWrite(asm.GetType("K0Probe.P" + (1 << k))));
    try { var a = em.CreateArchetype(types.ToArray()); var e = em.CreateEntity(a); return "OK cap=" + em.GetChunk(e).Capacity; }
    catch (System.Exception ex) { return "EX " + ex.GetType().Name + ": " + ex.Message; }
  };
  while (hi - lo > 4) { int mid = ((lo + hi) / 2) & ~3; if (mid == lo) break; var r = tryPad(mid); if (r.StartsWith("OK")) lo = mid; else { hi = mid; lastMsg = r; } }
  sb.AppendLine("largest accepted pad=" + lo + " (total w/ Entity=" + (lo + 8) + ") -> " + tryPad(lo));
  sb.AppendLine("smallest rejected pad=" + hi + " (total=" + (hi + 8) + ") -> " + tryPad(hi));
  foreach (int p in new int[]{16312,16316,16320,16324}) sb.AppendLine("pad=" + p + " total=" + (p+8) + " " + tryPad(p));
} finally { w.Dispose(); }
return sb.ToString();

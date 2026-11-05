var sb = new System.Text.StringBuilder();
var w = new Unity.Entities.World("K0");
try {
  var em = w.EntityManager;
  var arch = em.CreateArchetype(typeof(K0Probe.B8_C2));
  var e = em.CreateEntity(arch);
  var sib = em.CreateEntity(arch);
  var bs = em.GetBuffer<K0Probe.B8_C2>(sib); bs.Add(new K0Probe.B8_C2{a=1,b=1});
  var b = em.GetBuffer<K0Probe.B8_C2>(e);
  long pSib = 0, p0 = 0, p1 = 0, p2 = 0, pAfterShrinkNoTrim = 0;
  var mi = typeof(Unity.Entities.DynamicBuffer<K0Probe.B8_C2>).GetMethod("GetUnsafeReadOnlyPtr");
  var ipCtor = typeof(System.IntPtr).GetConstructor(new System.Type[]{ typeof(void).MakePointerType() });
  System.Func<Unity.Entities.DynamicBuffer<K0Probe.B8_C2>, long> P = (x) => ((System.IntPtr)ipCtor.Invoke(new object[]{ mi.Invoke((object)x, null) })).ToInt64();
  pSib = P(bs); p0 = P(b);
  sb.AppendLine("same chunk=" + (em.GetChunk(e) == em.GetChunk(sib)) + " chunkEntityCount=" + em.GetChunk(e).Count + " capacity=" + b.Capacity + " internalCap=" + b.Capacity);
  for (int i = 0; i < 10; i++) b.Add(new K0Probe.B8_C2{a=i,b=i});
  p1 = P(b);
  sb.AppendLine("after 10 adds: len=" + b.Length + " cap=" + b.Capacity);
  b.RemoveRange(1, b.Length - 1);
  pAfterShrinkNoTrim = P(b);
  sb.AppendLine("after remove to 1: len=" + b.Length + " cap=" + b.Capacity);
  b.TrimExcess();
  p2 = P(b);
  sb.AppendLine("after TrimExcess: len=" + b.Length + " cap=" + b.Capacity + " value0.a=" + b[0].a);
  sb.AppendLine("ptr sibling(inline never overflowed)=0x" + pSib.ToString("x") + " e initial inline=0x" + p0.ToString("x") + " overflow=0x" + p1.ToString("x") + " shrunkNoTrim=0x" + pAfterShrinkNoTrim.ToString("x") + " afterTrim=0x" + p2.ToString("x"));
  sb.AppendLine("dist(afterTrim, sibling)=" + System.Math.Abs(p2 - pSib) + " dist(overflow,sibling)=" + System.Math.Abs(p1 - pSib) + " dist(afterTrim, initialInline)=" + System.Math.Abs(p2 - p0) + " afterTrim==initialInline=" + (p2 == p0));
  var ch = em.GetChunk(e);
  foreach (var f in typeof(Unity.Entities.ArchetypeChunk).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)) {
    sb.Append("ArchetypeChunk." + f.Name + ":" + f.FieldType.Name + " ");
    if (f.FieldType.IsPointer) { object o = f.GetValue(ch); long c = ((System.IntPtr)ipCtor.Invoke(new object[]{ o })).ToInt64(); sb.Append("=0x" + c.ToString("x") + " "); sb.AppendLine("afterTrim-chunk=" + (p2 - c) + " overflow-chunk=" + (p1 - c)); }
  }
} finally { w.Dispose(); }
return sb.ToString();

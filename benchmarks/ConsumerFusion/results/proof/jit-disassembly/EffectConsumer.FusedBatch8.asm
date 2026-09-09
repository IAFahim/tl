; Assembly listing for method Tl.ConsumerFusion.ConsumerBenchmarks`1[Tl.ConsumerFusion.EffectConsumer]:FusedBatch8():Tl.ConsumerFusion.ConsumerReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 1 inlinees with PGO data; 8 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x60]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       mov      qword ptr [rbp-0x20], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0029
       mov      rcx, 0x1000000000000
       mov      qword ptr [rbp-0x58], rcx
       mov      qword ptr [rbp-0x50], rcx
       xor      r14d, r14d
       mov      rcx, gword ptr [rbx+0x08]
       cmp      dword ptr [rcx+0x08], 0
       jle      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0048
       mov      rcx, gword ptr [rbx+0x10]
       mov      r8d, r14d
       sar      r8d, 3
       and      r8d, 3
       cmp      r8d, dword ptr [rcx+0x08]
       jae      G_M000_IG07
       shl      r8, 4
       lea      rsi, bword ptr [rcx+r8+0x10]
       mov      rcx, gword ptr [rbx+0x08]
       test     rcx, rcx
       je       G_M000_IG06
       mov      r8d, dword ptr [rcx+0x08]
       mov      edi, r14d
       lea      rdx, [rdi+0x08]
       cmp      r8, rdx
       jb       G_M000_IG06
       lea      rcx, bword ptr [rcx+4*rdi+0x10]
       mov      r8d, 8
       lea      rdi, [rbp-0x50]
       lea      rdx, [rbp-0x48]
       call     [Tl.ConsumerFusion.FusedPulse:Forward[Tl.ConsumerFusion.ConsumerInput,Tl.ConsumerFusion.EffectConsumer](byref,byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback]
       mov      qword ptr [rbp-0x50], rax
       add      r14d, 8
       mov      rax, gword ptr [rbx+0x08]
       cmp      dword ptr [rax+0x08], r14d
       jg       SHORT G_M000_IG03
 
G_M000_IG04:                ;; offset=0x00B6
       mov      eax, dword ptr [rbp-0x50]
       movzx    rcx, word  ptr [rbp-0x4C]
       movzx    rdx, word  ptr [rbp-0x4A]
       mov      edi, dword ptr [rbp-0x48]
       mov      rsi, qword ptr [rbp-0x40]
       mov      r8d, dword ptr [rbp-0x38]
       mov      r9d, dword ptr [rbp-0x34]
       mov      r10d, dword ptr [rbp-0x30]
       mov      r11d, dword ptr [rbp-0x28]
       mov      rbx, qword ptr [rbp-0x20]
       mov      dword ptr [r15], eax
       mov      word  ptr [r15+0x04], cx
       mov      word  ptr [r15+0x06], dx
       mov      dword ptr [r15+0x08], edi
       mov      qword ptr [r15+0x10], rsi
       mov      dword ptr [r15+0x18], r8d
       mov      dword ptr [r15+0x1C], r9d
       mov      dword ptr [r15+0x20], r10d
       mov      dword ptr [r15+0x24], r11d
       mov      qword ptr [r15+0x28], rbx
       mov      rax, r15
 
G_M000_IG05:                ;; offset=0x0108
       add      rsp, 72
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x0113
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG07:                ;; offset=0x011A
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 288


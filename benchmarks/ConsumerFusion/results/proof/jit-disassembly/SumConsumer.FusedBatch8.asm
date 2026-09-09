; Assembly listing for method Tl.ConsumerFusion.ConsumerBenchmarks`1[Tl.ConsumerFusion.SumConsumer]:FusedBatch8():Tl.ConsumerFusion.ConsumerReceipt:this (Tier1)
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
       sub      rsp, 24
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x20], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x001B
       mov      rcx, 0x1000000000000
       mov      qword ptr [rbp-0x30], rcx
       mov      qword ptr [rbp-0x28], rcx
       xor      r14d, r14d
       mov      rcx, gword ptr [rbx+0x08]
       cmp      dword ptr [rcx+0x08], 0
       jle      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x003A
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
       je       SHORT G_M000_IG06
       mov      r8d, dword ptr [rcx+0x08]
       mov      edi, r14d
       lea      rdx, [rdi+0x08]
       cmp      r8, rdx
       jb       SHORT G_M000_IG06
       lea      rcx, bword ptr [rcx+4*rdi+0x10]
       mov      r8d, 8
       lea      rdi, [rbp-0x28]
       lea      rdx, [rbp-0x20]
       call     [Tl.ConsumerFusion.FusedPulse:Forward[Tl.ConsumerFusion.ConsumerInput,Tl.ConsumerFusion.SumConsumer](byref,byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback]
       mov      qword ptr [rbp-0x28], rax
       add      r14d, 8
       mov      rax, gword ptr [rbx+0x08]
       cmp      dword ptr [rax+0x08], r14d
       jg       SHORT G_M000_IG03
 
G_M000_IG04:                ;; offset=0x00A0
       mov      eax, dword ptr [rbp-0x28]
       movzx    rcx, word  ptr [rbp-0x24]
       movzx    rdx, word  ptr [rbp-0x22]
       mov      edi, dword ptr [rbp-0x20]
       mov      dword ptr [r15], eax
       mov      word  ptr [r15+0x04], cx
       mov      word  ptr [r15+0x06], dx
       mov      dword ptr [r15+0x08], edi
       vxorps   ymm0, ymm0, ymm0
       vmovups  ymmword ptr [r15+0x10], ymm0
       mov      rax, r15
 
G_M000_IG05:                ;; offset=0x00CC
       vzeroupper 
       add      rsp, 24
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x00DA
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG07:                ;; offset=0x00E1
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 231


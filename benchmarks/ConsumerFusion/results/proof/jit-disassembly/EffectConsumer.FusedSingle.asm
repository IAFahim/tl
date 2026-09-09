; Assembly listing for method Tl.ConsumerFusion.ConsumerBenchmarks`1[Tl.ConsumerFusion.EffectConsumer]:FusedSingle():Tl.ConsumerFusion.ConsumerReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 100
; 21 inlinees with PGO data; 108 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 104
       lea      rbp, [rsp+0x90]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0030
       mov      r8, 0x1000000000000
       mov      qword ptr [rbp-0x60], r8
       mov      r14d, dword ptr [rbp-0x60]
       movzx    r13, word  ptr [rbp-0x5C]
       movzx    r12, word  ptr [rbp-0x5A]
       xor      eax, eax
       mov      r8, gword ptr [rbx+0x08]
       cmp      dword ptr [r8+0x08], eax
       jg       G_M000_IG32
 
G_M000_IG03:                ;; offset=0x005C
       mov      eax, dword ptr [rbp-0x58]
       mov      rcx, qword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x48]
       mov      edi, dword ptr [rbp-0x44]
       mov      esi, dword ptr [rbp-0x40]
       mov      r8d, dword ptr [rbp-0x38]
       mov      r9, qword ptr [rbp-0x30]
       movzx    r10, r13w
       movzx    r11, r12w
       mov      dword ptr [r15], r14d
       mov      word  ptr [r15+0x04], r10w
       mov      word  ptr [r15+0x06], r11w
       mov      dword ptr [r15+0x08], eax
       mov      qword ptr [r15+0x10], rcx
       mov      dword ptr [r15+0x18], edx
       mov      dword ptr [r15+0x1C], edi
       mov      dword ptr [r15+0x20], esi
       mov      dword ptr [r15+0x24], r8d
       mov      qword ptr [r15+0x28], r9
       mov      rax, r15
 
G_M000_IG04:                ;; offset=0x00A8
       add      rsp, 104
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x00B7
       cmp      edi, r12d
       setb     r14b
       movzx    r14, r14b
       jmp      G_M000_IG34
 
G_M000_IG06:                ;; offset=0x00C7
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG25
 
G_M000_IG07:                ;; offset=0x00DB
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG28
 
G_M000_IG08:                ;; offset=0x00F2
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       jmp      G_M000_IG30
 
G_M000_IG09:                ;; offset=0x0102
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG70
 
G_M000_IG10:                ;; offset=0x0119
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG65
 
G_M000_IG11:                ;; offset=0x012D
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG56
 
G_M000_IG12:                ;; offset=0x0141
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG85
 
G_M000_IG13:                ;; offset=0x0155
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG99
 
G_M000_IG14:                ;; offset=0x016C
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG96
 
G_M000_IG15:                ;; offset=0x0180
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG117
 
G_M000_IG16:                ;; offset=0x0197
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG114
 
G_M000_IG17:                ;; offset=0x01AB
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG132
 
G_M000_IG18:                ;; offset=0x01BF
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG143
 
G_M000_IG19:                ;; offset=0x01D6
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG140
 
G_M000_IG20:                ;; offset=0x01EA
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG160
 
G_M000_IG21:                ;; offset=0x0201
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG157
 
G_M000_IG22:                ;; offset=0x0215
       xor      r8d, r8d
 
G_M000_IG23:                ;; offset=0x0218
       movzx    r14, r8b
       mov      r8, qword ptr [rbp-0x50]
       inc      r8
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG44
 
G_M000_IG24:                ;; offset=0x0230
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG25:                ;; offset=0x023F
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD00]
       mov      esi, r14d
       mov      edx, edi
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 3
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG46
 
G_M000_IG26:                ;; offset=0x0270
       mov      r12, bword ptr [rbp-0x88]
 
G_M000_IG27:                ;; offset=0x0277
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG28:                ;; offset=0x0282
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD04]
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x68]
       mov      rcx, r12
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 4
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG48
 
G_M000_IG29:                ;; offset=0x02B7
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG30:                ;; offset=0x02C2
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x68]
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
 
G_M000_IG31:                ;; offset=0x02E2
       mov      edi, dword ptr [rbp-0x64]
       mov      eax, r13d
       shl      rax, 32
       or       rdi, rax
       mov      eax, dword ptr [rbp-0x6C]
       shl      rax, 48
       or       rdi, rax
       mov      qword ptr [rbp-0x78], rdi
       mov      r14d, dword ptr [rbp-0x78]
       movzx    r13, word  ptr [rbp-0x74]
       movzx    r12, word  ptr [rbp-0x72]
       mov      rdi, qword ptr [rbp-0x80]
       inc      edi
       mov      r8, gword ptr [rbx+0x08]
       cmp      dword ptr [r8+0x08], edi
       mov      rax, rdi
       jle      G_M000_IG03
 
G_M000_IG32:                ;; offset=0x0322
       mov      rsi, gword ptr [rbx+0x10]
       mov      edx, eax
       sar      edx, 3
       and      edx, 3
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG237
       shl      rdx, 4
       lea      r9, bword ptr [rsi+rdx+0x10]
       mov      bword ptr [rbp-0x88], r9
       cmp      eax, dword ptr [r8+0x08]
       jae      G_M000_IG237
       mov      qword ptr [rbp-0x80], rax
       mov      r10d, dword ptr [r8+4*rax+0x10]
       mov      dword ptr [rbp-0x64], r10d
       movzx    r8, r12w
       test     r8b, 1
       je       G_M000_IG234
       movzx    r8, r12w
       test     r8b, 2
       jne      G_M000_IG235
       mov      r8d, r14d
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       imul     esi, r8d, 600
       mov      r12d, r14d
       sub      r12d, esi
       mov      esi, r10d
       imul     r11, rsi, 0x1B4E81B5
       shr      r11, 38
       imul     esi, r11d, 600
       mov      edi, r10d
       sub      edi, esi
       mov      dword ptr [rbp-0x68], edi
       cmp      r10d, r14d
       jb       G_M000_IG05
 
G_M000_IG33:                ;; offset=0x03BB
       mov      r14d, r11d
       sub      r14d, r8d
 
G_M000_IG34:                ;; offset=0x03C1
       movzx    r8, r13w
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   r8, r8d
       mov      esi, r14d
       cmp      r8, rsi
       jl       G_M000_IG236
       add      r13d, r14d
       movzx    r13, r13w
       mov      r11d, 1
       cmp      edi, 599
       je       G_M000_IG190
 
G_M000_IG35:                ;; offset=0x03F7
       cmp      edi, 47
       jb       G_M000_IG124
 
G_M000_IG36:                ;; offset=0x0400
       cmp      edi, 200
       jb       G_M000_IG79
 
G_M000_IG37:                ;; offset=0x040C
       cmp      edi, 321
       jb       G_M000_IG61
 
G_M000_IG38:                ;; offset=0x0418
       cmp      edi, 515
       jae      G_M000_IG51
 
G_M000_IG39:                ;; offset=0x0424
       cmp      edi, 514
       je       SHORT G_M000_IG43
 
G_M000_IG40:                ;; offset=0x042C
       test     r14d, r14d
       jne      G_M000_IG22
 
G_M000_IG41:                ;; offset=0x0435
       cmp      r12d, 321
       jb       G_M000_IG22
 
G_M000_IG42:                ;; offset=0x0442
       mov      r8d, 1
       jmp      G_M000_IG23
 
G_M000_IG43:                ;; offset=0x044D
       mov      r8d, 2
       jmp      G_M000_IG23
 
G_M000_IG44:                ;; offset=0x0458
       cmp      r14d, 2
       ja       G_M000_IG229
       mov      r8d, r14d
       lea      rsi, [reloc @RWD12]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG45:                ;; offset=0x047C
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG25
 
G_M000_IG46:                ;; offset=0x04A5
       cmp      r14d, 2
       ja       G_M000_IG231
       mov      r8d, r14d
       lea      rsi, [reloc @RWD24]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG47:                ;; offset=0x04C9
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      r12, bword ptr [rbp-0x88]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       jmp      G_M000_IG28
 
G_M000_IG48:                ;; offset=0x04FF
       cmp      r14d, 2
       ja       G_M000_IG30
 
G_M000_IG49:                ;; offset=0x0509
       mov      r8d, r14d
       lea      rsi, [reloc @RWD36]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG50:                ;; offset=0x0523
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       jmp      G_M000_IG30
 
G_M000_IG51:                ;; offset=0x0552
       cmp      edi, 599
       je       G_M000_IG232
       test     r14d, r14d
       jne      SHORT G_M000_IG53
 
G_M000_IG52:                ;; offset=0x0563
       cmp      r12d, 515
       jae      SHORT G_M000_IG57
 
G_M000_IG53:                ;; offset=0x056C
       xor      r8d, r8d
 
G_M000_IG54:                ;; offset=0x056F
       movzx    rsi, r8b
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 2
       mov      qword ptr [rbp-0x50], r8
       test     esi, esi
       jne      SHORT G_M000_IG58
 
G_M000_IG55:                ;; offset=0x0583
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG56:                ;; offset=0x0592
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD48]
       mov      edx, edi
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG57:                ;; offset=0x05B3
       mov      r8d, 1
       jmp      SHORT G_M000_IG54
 
G_M000_IG58:                ;; offset=0x05BB
       cmp      esi, 2
       ja       G_M000_IG233
 
G_M000_IG59:                ;; offset=0x05C4
       mov      r8d, esi
       lea      rdx, [reloc @RWD52]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG60:                ;; offset=0x05DE
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD48]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      SHORT G_M000_IG56
 
G_M000_IG61:                ;; offset=0x060C
       cmp      edi, 320
       je       G_M000_IG224
       test     r14d, r14d
       je       G_M000_IG71
 
G_M000_IG62:                ;; offset=0x0621
       xor      r8d, r8d
 
G_M000_IG63:                ;; offset=0x0624
       movzx    rsi, r8b
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 3
       mov      qword ptr [rbp-0x50], r8
       test     esi, esi
       jne      G_M000_IG73
 
G_M000_IG64:                ;; offset=0x063C
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG65:                ;; offset=0x064B
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD64]
       mov      edx, edi
       mov      rcx, r9
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      eax, dword ptr [rbp-0x68]
       cmp      eax, 320
       je       G_M000_IG226
       test     r14d, r14d
       je       G_M000_IG75
 
G_M000_IG66:                ;; offset=0x067E
       xor      r8d, r8d
 
G_M000_IG67:                ;; offset=0x0681
       movzx    rsi, r8b
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 4
       mov      qword ptr [rbp-0x50], r8
       test     esi, esi
       jne      G_M000_IG77
 
G_M000_IG68:                ;; offset=0x0699
       mov      r12, bword ptr [rbp-0x88]
 
G_M000_IG69:                ;; offset=0x06A0
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG70:                ;; offset=0x06AB
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      edx, eax
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG71:                ;; offset=0x06CC
       cmp      r12d, 123
       jb       G_M000_IG62
 
G_M000_IG72:                ;; offset=0x06D6
       mov      r8d, 1
       jmp      G_M000_IG63
 
G_M000_IG73:                ;; offset=0x06E1
       cmp      esi, 2
       ja       G_M000_IG225
       mov      r8d, esi
       lea      rdx, [reloc @RWD68]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG74:                ;; offset=0x0704
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG65
 
G_M000_IG75:                ;; offset=0x0735
       cmp      r12d, 200
       jb       G_M000_IG66
 
G_M000_IG76:                ;; offset=0x0742
       mov      r8d, 1
       jmp      G_M000_IG67
 
G_M000_IG77:                ;; offset=0x074D
       cmp      esi, 2
       ja       G_M000_IG228
       mov      r8d, esi
       lea      rdx, [reloc @RWD80]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG78:                ;; offset=0x0770
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      r12, bword ptr [rbp-0x88]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       jmp      G_M000_IG70
 
G_M000_IG79:                ;; offset=0x07A6
       cmp      edi, 76
       jb       G_M000_IG109
 
G_M000_IG80:                ;; offset=0x07AF
       cmp      edi, 123
       jb       G_M000_IG91
 
G_M000_IG81:                ;; offset=0x07B8
       test     r14d, r14d
       je       SHORT G_M000_IG86
 
G_M000_IG82:                ;; offset=0x07BD
       xor      esi, esi
 
G_M000_IG83:                ;; offset=0x07BF
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 3
       mov      qword ptr [rbp-0x50], r8
       test     esi, esi
       jne      SHORT G_M000_IG88
 
G_M000_IG84:                ;; offset=0x07CF
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG85:                ;; offset=0x07DE
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD64]
       mov      edx, edi
       mov      rcx, r9
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG86:                ;; offset=0x07FF
       cmp      r12d, 123
       jb       SHORT G_M000_IG82
 
G_M000_IG87:                ;; offset=0x0805
       mov      esi, 1
       jmp      SHORT G_M000_IG83
 
G_M000_IG88:                ;; offset=0x080C
       cmp      esi, 2
       ja       G_M000_IG223
 
G_M000_IG89:                ;; offset=0x0815
       mov      r8d, esi
       lea      rdx, [reloc @RWD92]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG90:                ;; offset=0x082F
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      SHORT G_M000_IG85
 
G_M000_IG91:                ;; offset=0x085D
       cmp      edi, 122
       je       G_M000_IG102
 
G_M000_IG92:                ;; offset=0x0866
       test     r14d, r14d
       je       G_M000_IG100
 
G_M000_IG93:                ;; offset=0x086F
       xor      r8d, r8d
 
G_M000_IG94:                ;; offset=0x0872
       movzx    r14, r8b
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 2
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG103
 
G_M000_IG95:                ;; offset=0x088B
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG96:                ;; offset=0x089A
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD04]
       mov      esi, r14d
       mov      edx, edi
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r12d, dword ptr [rbp-0x68]
       lea      r8d, [r12-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD104]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD108]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD112]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 4
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG106
 
G_M000_IG97:                ;; offset=0x08F8
       mov      rax, bword ptr [rbp-0x88]
 
G_M000_IG98:                ;; offset=0x08FF
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG99:                ;; offset=0x090A
       lea      r8, [rbp-0x58]
       mov      esi, r14d
       mov      edx, r12d
       mov      rcx, rax
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG100:                ;; offset=0x0927
       cmp      r12d, 76
       jb       G_M000_IG93
 
G_M000_IG101:                ;; offset=0x0931
       mov      r8d, 1
       jmp      G_M000_IG94
 
G_M000_IG102:                ;; offset=0x093C
       mov      r8d, 2
       jmp      G_M000_IG94
 
G_M000_IG103:                ;; offset=0x0947
       cmp      r14d, 2
       ja       G_M000_IG220
 
G_M000_IG104:                ;; offset=0x0951
       mov      r8d, r14d
       lea      rsi, [reloc @RWD116]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG105:                ;; offset=0x096B
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG96
 
G_M000_IG106:                ;; offset=0x099C
       cmp      r14d, 2
       ja       G_M000_IG222
 
G_M000_IG107:                ;; offset=0x09A6
       mov      r8d, r14d
       lea      rsi, [reloc @RWD128]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG108:                ;; offset=0x09C0
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      rax, bword ptr [rbp-0x88]
       vmulss   xmm1, xmm0, dword ptr [rax]
       vaddss   xmm1, xmm1, dword ptr [rax+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm1
       jmp      G_M000_IG99
 
G_M000_IG109:                ;; offset=0x09EA
       cmp      edi, 75
       je       G_M000_IG119
 
G_M000_IG110:                ;; offset=0x09F3
       test     r14d, r14d
       je       G_M000_IG118
 
G_M000_IG111:                ;; offset=0x09FC
       xor      r8d, r8d
 
G_M000_IG112:                ;; offset=0x09FF
       movzx    r14, r8b
       mov      r8, qword ptr [rbp-0x50]
       inc      r8
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG120
 
G_M000_IG113:                ;; offset=0x0A17
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG114:                ;; offset=0x0A26
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD140]
       mov      esi, r14d
       mov      edx, edi
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 3
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG122
 
G_M000_IG115:                ;; offset=0x0A57
       mov      r12, bword ptr [rbp-0x88]
 
G_M000_IG116:                ;; offset=0x0A5E
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG117:                ;; offset=0x0A69
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD64]
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x68]
       mov      rcx, r12
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG118:                ;; offset=0x0A8E
       cmp      r12d, 47
       jb       G_M000_IG111
       jmp      G_M000_IG216
 
G_M000_IG119:                ;; offset=0x0A9D
       mov      r8d, 2
       jmp      G_M000_IG112
 
G_M000_IG120:                ;; offset=0x0AA8
       cmp      r14d, 2
       ja       G_M000_IG217
       mov      r8d, r14d
       lea      rsi, [reloc @RWD144]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG121:                ;; offset=0x0ACC
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD140]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG114
 
G_M000_IG122:                ;; offset=0x0AFD
       cmp      r14d, 2
       ja       G_M000_IG219
       mov      r8d, r14d
       lea      rsi, [reloc @RWD156]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG123:                ;; offset=0x0B21
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      r12, bword ptr [rbp-0x88]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       jmp      G_M000_IG117
 
G_M000_IG124:                ;; offset=0x0B57
       cmp      edi, 11
       jb       G_M000_IG150
 
G_M000_IG125:                ;; offset=0x0B60
       cmp      edi, 18
       jb       G_M000_IG149
 
G_M000_IG126:                ;; offset=0x0B69
       cmp      edi, 29
       jb       G_M000_IG136
 
G_M000_IG127:                ;; offset=0x0B72
       cmp      edi, 46
       je       SHORT G_M000_IG133
 
G_M000_IG128:                ;; offset=0x0B77
       test     r14d, r14d
       je       G_M000_IG214
 
G_M000_IG129:                ;; offset=0x0B80
       xor      r8d, r8d
 
G_M000_IG130:                ;; offset=0x0B83
       movzx    rsi, r8b
       lea      r8d, [rdi-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD168]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD140]
       mov      r8, qword ptr [rbp-0x50]
       inc      r8
       mov      qword ptr [rbp-0x50], r8
       test     esi, esi
       jne      SHORT G_M000_IG134
 
G_M000_IG131:                ;; offset=0x0BBB
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG132:                ;; offset=0x0BCA
       lea      r8, [rbp-0x58]
       mov      edx, edi
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG133:                ;; offset=0x0BE0
       mov      r8d, 2
       jmp      SHORT G_M000_IG130
 
G_M000_IG134:                ;; offset=0x0BE8
       cmp      esi, 2
       ja       G_M000_IG215
       mov      r8d, esi
       lea      rdx, [reloc @RWD172]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG135:                ;; offset=0x0C0B
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmulss   xmm1, xmm0, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm1
       mov      dword ptr [rbp-0x6C], r11d
       jmp      SHORT G_M000_IG132
 
G_M000_IG136:                ;; offset=0x0C31
       cmp      edi, 28
       je       G_M000_IG210
       test     r14d, r14d
       je       G_M000_IG144
 
G_M000_IG137:                ;; offset=0x0C43
       xor      r8d, r8d
 
G_M000_IG138:                ;; offset=0x0C46
       movzx    r14, r8b
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 2
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG145
 
G_M000_IG139:                ;; offset=0x0C5F
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG140:                ;; offset=0x0C6E
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD04]
       mov      esi, r14d
       mov      edx, edi
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 4
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG147
 
G_M000_IG141:                ;; offset=0x0CA2
       mov      r12, bword ptr [rbp-0x88]
 
G_M000_IG142:                ;; offset=0x0CA9
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG143:                ;; offset=0x0CB4
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x68]
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG144:                ;; offset=0x0CD9
       cmp      r12d, 18
       jb       G_M000_IG137
       jmp      G_M000_IG209
 
G_M000_IG145:                ;; offset=0x0CE8
       cmp      r14d, 2
       ja       G_M000_IG211
       mov      r8d, r14d
       lea      rsi, [reloc @RWD184]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG146:                ;; offset=0x0D0C
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG140
 
G_M000_IG147:                ;; offset=0x0D3D
       cmp      r14d, 2
       ja       G_M000_IG213
       mov      r8d, r14d
       lea      rsi, [reloc @RWD196]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG148:                ;; offset=0x0D61
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      r12, bword ptr [rbp-0x88]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       jmp      G_M000_IG143
 
G_M000_IG149:                ;; offset=0x0D97
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG31
 
G_M000_IG150:                ;; offset=0x0DA0
       cmp      edi, 3
       jb       G_M000_IG184
 
G_M000_IG151:                ;; offset=0x0DA9
       cmp      edi, 7
       jb       G_M000_IG166
 
G_M000_IG152:                ;; offset=0x0DB2
       cmp      edi, 10
       je       G_M000_IG161
 
G_M000_IG153:                ;; offset=0x0DBB
       test     r14d, r14d
       je       G_M000_IG205
 
G_M000_IG154:                ;; offset=0x0DC4
       xor      r8d, r8d
 
G_M000_IG155:                ;; offset=0x0DC7
       movzx    r14, r8b
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 2
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG162
 
G_M000_IG156:                ;; offset=0x0DE0
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG157:                ;; offset=0x0DEF
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD48]
       mov      esi, r14d
       mov      edx, edi
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 4
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG164
 
G_M000_IG158:                ;; offset=0x0E23
       mov      r12, bword ptr [rbp-0x88]
 
G_M000_IG159:                ;; offset=0x0E2A
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG160:                ;; offset=0x0E35
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x68]
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG161:                ;; offset=0x0E5A
       mov      r8d, 2
       jmp      G_M000_IG155
 
G_M000_IG162:                ;; offset=0x0E65
       cmp      r14d, 2
       ja       G_M000_IG206
       mov      r8d, r14d
       lea      rsi, [reloc @RWD208]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG163:                ;; offset=0x0E89
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD48]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG157
 
G_M000_IG164:                ;; offset=0x0EBA
       cmp      r14d, 2
       ja       G_M000_IG208
       mov      r8d, r14d
       lea      rsi, [reloc @RWD220]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG165:                ;; offset=0x0EDE
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      r12, bword ptr [rbp-0x88]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       jmp      G_M000_IG160
 
G_M000_IG166:                ;; offset=0x0F14
       cmp      edi, 6
       je       G_M000_IG195
       test     r14d, r14d
       je       G_M000_IG194
       xor      r8d, r8d
 
G_M000_IG167:                ;; offset=0x0F29
       movzx    rsi, r8b
       mov      r8, qword ptr [rbp-0x50]
       inc      r8
       mov      qword ptr [rbp-0x50], r8
       test     esi, esi
       jne      G_M000_IG178
 
G_M000_IG168:                ;; offset=0x0F40
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG169:                ;; offset=0x0F4F
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD00]
       mov      edx, edi
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       test     r14d, r14d
       je       G_M000_IG198
 
G_M000_IG170:                ;; offset=0x0F71
       xor      r14d, r14d
 
G_M000_IG171:                ;; offset=0x0F74
       mov      r12d, dword ptr [rbp-0x68]
       lea      r8d, [r12-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD48]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD64]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 2
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG180
 
G_M000_IG172:                ;; offset=0x0FAB
       mov      rax, bword ptr [rbp-0x88]
 
G_M000_IG173:                ;; offset=0x0FB2
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG174:                ;; offset=0x0FBD
       lea      r8, [rbp-0x58]
       mov      esi, r14d
       mov      edx, r12d
       mov      rcx, rax
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x50]
       add      r8, 4
       mov      qword ptr [rbp-0x50], r8
       test     r14d, r14d
       jne      G_M000_IG182
 
G_M000_IG175:                ;; offset=0x0FEA
       mov      rax, bword ptr [rbp-0x88]
 
G_M000_IG176:                ;; offset=0x0FF1
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
 
G_M000_IG177:                ;; offset=0x0FFC
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      esi, r14d
       mov      edx, r12d
       mov      rcx, rax
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG178:                ;; offset=0x1021
       cmp      esi, 2
       ja       G_M000_IG196
       mov      r8d, esi
       lea      rdx, [reloc @RWD232]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG179:                ;; offset=0x1044
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG169
 
G_M000_IG180:                ;; offset=0x106D
       cmp      r14d, 2
       ja       G_M000_IG200
       mov      r8d, r14d
       lea      rsi, [reloc @RWD244]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG181:                ;; offset=0x1091
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      rax, bword ptr [rbp-0x88]
       vmulss   xmm1, xmm0, dword ptr [rax]
       vaddss   xmm1, xmm1, dword ptr [rax+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm1
       jmp      G_M000_IG174
 
G_M000_IG182:                ;; offset=0x10BB
       cmp      r14d, 2
       ja       G_M000_IG202
       mov      r8d, r14d
       lea      rsi, [reloc @RWD256]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG183:                ;; offset=0x10DF
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       mov      rax, bword ptr [rbp-0x88]
       vmovss   xmm0, dword ptr [rax]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [rax+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       jmp      G_M000_IG177
 
G_M000_IG184:                ;; offset=0x1111
       test     r14d, r14d
       je       G_M000_IG191
       xor      esi, esi
 
G_M000_IG185:                ;; offset=0x111C
       mov      r8, qword ptr [rbp-0x50]
       inc      r8
       mov      qword ptr [rbp-0x50], r8
       test     esi, esi
       jne      SHORT G_M000_IG188
 
G_M000_IG186:                ;; offset=0x112B
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      dword ptr [rbp-0x6C], r11d
 
G_M000_IG187:                ;; offset=0x113A
       lea      r8, [rbp-0x58]
       vmovss   xmm0, dword ptr [reloc @RWD00]
       mov      edx, edi
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG31
 
G_M000_IG188:                ;; offset=0x1158
       cmp      esi, 2
       ja       SHORT G_M000_IG192
       mov      r8d, esi
       lea      rdx, [reloc @RWD268]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG189:                ;; offset=0x1177
       mov      r8d, dword ptr [rbp-0x44]
       inc      r8d
       mov      dword ptr [rbp-0x44], r8d
       vmovss   xmm0, dword ptr [r9]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x58]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      dword ptr [rbp-0x6C], r11d
       jmp      SHORT G_M000_IG187
 
G_M000_IG190:                ;; offset=0x119D
       mov      dword ptr [rbp-0x6C], 5
       mov      r11d, dword ptr [rbp-0x6C]
       jmp      G_M000_IG35
 
G_M000_IG191:                ;; offset=0x11AD
       mov      esi, 1
       jmp      G_M000_IG185
 
G_M000_IG192:                ;; offset=0x11B7
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG187
 
G_M000_IG193:                ;; offset=0x11C0
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG187
 
G_M000_IG194:                ;; offset=0x11D4
       mov      r8d, 1
       jmp      G_M000_IG167
 
G_M000_IG195:                ;; offset=0x11DF
       mov      r8d, 2
       jmp      G_M000_IG167
 
G_M000_IG196:                ;; offset=0x11EA
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG169
 
G_M000_IG197:                ;; offset=0x11F3
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG169
 
G_M000_IG198:                ;; offset=0x1207
       cmp      r12d, 3
       jb       G_M000_IG170
       mov      r14d, 1
       jmp      G_M000_IG171
 
G_M000_IG199:                ;; offset=0x121C
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG173
 
G_M000_IG200:                ;; offset=0x1228
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG174
 
G_M000_IG201:                ;; offset=0x1234
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG176
 
G_M000_IG202:                ;; offset=0x1240
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG177
 
G_M000_IG203:                ;; offset=0x124C
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG174
 
G_M000_IG204:                ;; offset=0x1263
       mov      r8d, dword ptr [rbp-0x40]
       inc      r8d
       mov      dword ptr [rbp-0x40], r8d
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG177
 
G_M000_IG205:                ;; offset=0x127A
       cmp      r12d, 3
       jb       G_M000_IG154
       mov      r8d, 1
       jmp      G_M000_IG155
 
G_M000_IG206:                ;; offset=0x128F
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG157
 
G_M000_IG207:                ;; offset=0x1298
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG159
 
G_M000_IG208:                ;; offset=0x12A4
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG160
 
G_M000_IG209:                ;; offset=0x12B0
       mov      r8d, 1
       jmp      G_M000_IG138
 
G_M000_IG210:                ;; offset=0x12BB
       mov      r8d, 2
       jmp      G_M000_IG138
 
G_M000_IG211:                ;; offset=0x12C6
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG140
 
G_M000_IG212:                ;; offset=0x12CF
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG142
 
G_M000_IG213:                ;; offset=0x12DB
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG143
 
G_M000_IG214:                ;; offset=0x12E7
       cmp      r12d, 29
       jb       G_M000_IG129
       mov      r8d, 1
       jmp      G_M000_IG130
 
G_M000_IG215:                ;; offset=0x12FC
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG132
 
G_M000_IG216:                ;; offset=0x1305
       mov      r8d, 1
       jmp      G_M000_IG112
 
G_M000_IG217:                ;; offset=0x1310
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG114
 
G_M000_IG218:                ;; offset=0x1319
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG116
 
G_M000_IG219:                ;; offset=0x1325
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG117
 
G_M000_IG220:                ;; offset=0x1331
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG96
 
G_M000_IG221:                ;; offset=0x133A
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG98
 
G_M000_IG222:                ;; offset=0x1346
       mov      rax, bword ptr [rbp-0x88]
       jmp      G_M000_IG99
 
G_M000_IG223:                ;; offset=0x1352
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG85
 
G_M000_IG224:                ;; offset=0x135B
       mov      r8d, 2
       jmp      G_M000_IG63
 
G_M000_IG225:                ;; offset=0x1366
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG65
 
G_M000_IG226:                ;; offset=0x136F
       mov      r8d, 2
       jmp      G_M000_IG67
 
G_M000_IG227:                ;; offset=0x137A
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG69
 
G_M000_IG228:                ;; offset=0x1386
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG70
 
G_M000_IG229:                ;; offset=0x1392
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG25
 
G_M000_IG230:                ;; offset=0x139B
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG27
 
G_M000_IG231:                ;; offset=0x13A7
       mov      r12, bword ptr [rbp-0x88]
       jmp      G_M000_IG28
 
G_M000_IG232:                ;; offset=0x13B3
       mov      r8d, 2
       jmp      G_M000_IG54
 
G_M000_IG233:                ;; offset=0x13BE
       mov      dword ptr [rbp-0x6C], r11d
       jmp      G_M000_IG56
 
G_M000_IG234:                ;; offset=0x13C7
       mov      rdi, 0x7FE09339B698
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x455
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG235:                ;; offset=0x1403
       mov      rdi, 0x7FE09339B698
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x4CD
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r15
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG236:                ;; offset=0x143F
       mov      rdi, 0x7FE093390D20
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      edi, 0x4F7
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r12, rax
       mov      edi, 0x503
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r12
       mov      rdi, r13
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r13
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG237:                ;; offset=0x1496
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41000000h		;         8
RWD08  	dd	40A00000h		;         5
RWD12  	dd	00000200h ; case G_M000_IG24
       	dd	0000044Ch ; case G_M000_IG45
       	dd	00000097h ; case G_M000_IG06
RWD24  	dd	0000136Bh ; case G_M000_IG230
       	dd	00000499h ; case G_M000_IG47
       	dd	000000ABh ; case G_M000_IG07
RWD36  	dd	00000287h ; case G_M000_IG29
       	dd	000004F3h ; case G_M000_IG50
       	dd	000000C2h ; case G_M000_IG08
RWD48  	dd	40400000h		;         3
RWD52  	dd	00000553h ; case G_M000_IG55
       	dd	000005AEh ; case G_M000_IG60
       	dd	000000FDh ; case G_M000_IG11
RWD64  	dd	40000000h		;         2
RWD68  	dd	0000060Ch ; case G_M000_IG64
       	dd	000006D4h ; case G_M000_IG74
       	dd	000000E9h ; case G_M000_IG10
RWD80  	dd	0000134Ah ; case G_M000_IG227
       	dd	00000740h ; case G_M000_IG78
       	dd	000000D2h ; case G_M000_IG09
RWD92  	dd	0000079Fh ; case G_M000_IG84
       	dd	000007FFh ; case G_M000_IG90
       	dd	00000111h ; case G_M000_IG12
RWD104 	dd	42380000h		;        46
RWD108 	dd	41A80000h		;        21
RWD112 	dd	42080000h		;        34
RWD116 	dd	0000085Bh ; case G_M000_IG95
       	dd	0000093Bh ; case G_M000_IG105
       	dd	0000013Ch ; case G_M000_IG14
RWD128 	dd	0000130Ah ; case G_M000_IG221
       	dd	00000990h ; case G_M000_IG108
       	dd	00000125h ; case G_M000_IG13
RWD140 	dd	41500000h		;        13
RWD144 	dd	000009E7h ; case G_M000_IG113
       	dd	00000A9Ch ; case G_M000_IG121
       	dd	00000167h ; case G_M000_IG16
RWD156 	dd	000012E9h ; case G_M000_IG218
       	dd	00000AF1h ; case G_M000_IG123
       	dd	00000150h ; case G_M000_IG15
RWD168 	dd	41880000h		;        17
RWD172 	dd	00000B8Bh ; case G_M000_IG131
       	dd	00000BDBh ; case G_M000_IG135
       	dd	0000017Bh ; case G_M000_IG17
RWD184 	dd	00000C2Fh ; case G_M000_IG139
       	dd	00000CDCh ; case G_M000_IG146
       	dd	000001A6h ; case G_M000_IG19
RWD196 	dd	0000129Fh ; case G_M000_IG212
       	dd	00000D31h ; case G_M000_IG148
       	dd	0000018Fh ; case G_M000_IG18
RWD208 	dd	00000DB0h ; case G_M000_IG156
       	dd	00000E59h ; case G_M000_IG163
       	dd	000001D1h ; case G_M000_IG21
RWD220 	dd	00001268h ; case G_M000_IG207
       	dd	00000EAEh ; case G_M000_IG165
       	dd	000001BAh ; case G_M000_IG20
RWD232 	dd	00000F10h ; case G_M000_IG168
       	dd	00001014h ; case G_M000_IG179
       	dd	000011C3h ; case G_M000_IG197
RWD244 	dd	000011ECh ; case G_M000_IG199
       	dd	00001061h ; case G_M000_IG181
       	dd	0000121Ch ; case G_M000_IG203
RWD256 	dd	00001204h ; case G_M000_IG201
       	dd	000010AFh ; case G_M000_IG183
       	dd	00001233h ; case G_M000_IG204
RWD268 	dd	000010FBh ; case G_M000_IG186
       	dd	00001147h ; case G_M000_IG189
       	dd	00001190h ; case G_M000_IG193

; Total bytes of code 5276


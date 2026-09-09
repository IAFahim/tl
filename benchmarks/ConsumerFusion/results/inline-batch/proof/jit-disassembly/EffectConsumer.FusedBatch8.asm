; Assembly listing for method Tl.ConsumerFusion.ConsumerBenchmarks`1[Tl.ConsumerFusion.EffectConsumer]:FusedBatch8():Tl.ConsumerFusion.ConsumerReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 2
; 22 inlinees with PGO data; 112 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 152
       lea      rbp, [rsp+0xC0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      qword ptr [rbp-0x30], rsi
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x002F
       mov      r8, 0x1000000000000
       mov      qword ptr [rbp-0x70], r8
       mov      r14d, dword ptr [rbp-0x70]
       movzx    r13, word  ptr [rbp-0x6C]
       movzx    r12, word  ptr [rbp-0x6A]
       xor      eax, eax
       mov      r8, gword ptr [rbx+0x08]
       cmp      dword ptr [r8+0x08], eax
       jg       G_M000_IG213
 
G_M000_IG03:                ;; offset=0x005B
       mov      eax, dword ptr [rbp-0x60]
       mov      rcx, qword ptr [rbp-0x58]
       mov      edx, dword ptr [rbp-0x50]
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x48]
       mov      r8d, dword ptr [rbp-0x40]
       mov      r9, qword ptr [rbp-0x38]
       movzx    r10, r13w
       movzx    r11, r12w
       mov      r15, qword ptr [rbp-0x30]
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
 
G_M000_IG04:                ;; offset=0x00AB
       add      rsp, 152
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x00BD
       mov      edi, 5
       jmp      G_M000_IG212
 
G_M000_IG06:                ;; offset=0x00C7
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       jmp      G_M000_IG54
 
G_M000_IG07:                ;; offset=0x00D7
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG52
 
G_M000_IG08:                ;; offset=0x00EE
       mov      esi, dword ptr [rbp-0x48]
       inc      esi
       mov      dword ptr [rbp-0x48], esi
       jmp      G_M000_IG49
 
G_M000_IG09:                ;; offset=0x00FB
       mov      r10d, dword ptr [rbp-0x48]
       inc      r10d
       mov      dword ptr [rbp-0x48], r10d
       jmp      G_M000_IG79
 
G_M000_IG10:                ;; offset=0x010B
       mov      r11d, dword ptr [rbp-0x48]
       inc      r11d
       mov      dword ptr [rbp-0x48], r11d
       jmp      G_M000_IG94
 
G_M000_IG11:                ;; offset=0x011B
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG107
 
G_M000_IG12:                ;; offset=0x0132
       mov      esi, dword ptr [rbp-0x48]
       inc      esi
       mov      dword ptr [rbp-0x48], esi
       jmp      G_M000_IG104
 
G_M000_IG13:                ;; offset=0x013F
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG123
 
G_M000_IG14:                ;; offset=0x0156
       mov      esi, dword ptr [rbp-0x48]
       inc      esi
       mov      dword ptr [rbp-0x48], esi
       jmp      G_M000_IG120
 
G_M000_IG15:                ;; offset=0x0163
       mov      r11d, dword ptr [rbp-0x48]
       inc      r11d
       mov      dword ptr [rbp-0x48], r11d
       jmp      G_M000_IG139
 
G_M000_IG16:                ;; offset=0x0173
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG153
 
G_M000_IG17:                ;; offset=0x018A
       mov      esi, dword ptr [rbp-0x48]
       inc      esi
       mov      dword ptr [rbp-0x48], esi
       jmp      G_M000_IG150
 
G_M000_IG18:                ;; offset=0x0197
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG172
 
G_M000_IG19:                ;; offset=0x01AE
       mov      esi, dword ptr [rbp-0x48]
       inc      esi
       mov      dword ptr [rbp-0x48], esi
       jmp      G_M000_IG169
 
G_M000_IG20:                ;; offset=0x01BB
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       jmp      G_M000_IG191
 
G_M000_IG21:                ;; offset=0x01CB
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG189
 
G_M000_IG22:                ;; offset=0x01E2
       mov      esi, dword ptr [rbp-0x48]
       inc      esi
       mov      dword ptr [rbp-0x48], esi
       mov      esi, dword ptr [rbp-0x7C]
       jmp      G_M000_IG184
 
G_M000_IG23:                ;; offset=0x01F2
       mov      r11d, dword ptr [rbp-0x48]
       inc      r11d
       mov      dword ptr [rbp-0x48], r11d
       jmp      G_M000_IG208
 
G_M000_IG24:                ;; offset=0x0202
       mov      r11d, dword ptr [rbp-0x48]
       inc      r11d
       mov      dword ptr [rbp-0x48], r11d
       jmp      G_M000_IG73
 
G_M000_IG25:                ;; offset=0x0212
       mov      esi, 1
       jmp      G_M000_IG71
 
G_M000_IG26:                ;; offset=0x021C
       mov      esi, 2
       jmp      G_M000_IG71
 
G_M000_IG27:                ;; offset=0x0226
       cmp      esi, 2
       ja       G_M000_IG240
       mov      r11d, esi
       lea      r14, [reloc @RWD00]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG28:                ;; offset=0x024A
       mov      r11d, dword ptr [rbp-0x4C]
       inc      r11d
       mov      dword ptr [rbp-0x4C], r11d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG73
 
G_M000_IG29:                ;; offset=0x0277
       mov      r8d, dword ptr [rbp-0x48]
       inc      r8d
       mov      dword ptr [rbp-0x48], r8d
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG85
 
G_M000_IG30:                ;; offset=0x028E
       mov      esi, 2
       jmp      G_M000_IG77
 
G_M000_IG31:                ;; offset=0x0298
       cmp      esi, 2
       ja       G_M000_IG234
       mov      r11d, esi
       mov      qword ptr [rbp-0xB8], r11
       lea      r11, [reloc @RWD16]
       mov      r10, qword ptr [rbp-0xB8]
       mov      r11d, dword ptr [r11+4*r10]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG32:                ;; offset=0x02CA
       mov      r10d, dword ptr [rbp-0x4C]
       inc      r10d
       mov      dword ptr [rbp-0x4C], r10d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG79
 
G_M000_IG33:                ;; offset=0x02F7
       cmp      r12d, 200
       jb       G_M000_IG81
 
G_M000_IG34:                ;; offset=0x0304
       mov      r8d, 1
       jmp      G_M000_IG82
 
G_M000_IG35:                ;; offset=0x030F
       mov      r8d, 2
       jmp      G_M000_IG82
 
G_M000_IG36:                ;; offset=0x031A
       cmp      esi, 2
       ja       G_M000_IG236
       mov      r8d, esi
       lea      rdx, [reloc @RWD32]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG37:                ;; offset=0x033D
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       mov      r12, bword ptr [rbp-0xA8]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD44]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG85
 
G_M000_IG38:                ;; offset=0x0373
       cmp      r15d, r12d
       setb     r14b
       movzx    r14, r14b
 
G_M000_IG39:                ;; offset=0x037E
       mov      esi, r13d
       neg      esi
       add      esi, 0xFFFF
       movsxd   rsi, esi
       mov      r11d, r14d
       cmp      rsi, r11
       jl       G_M000_IG244
       add      r13d, r14d
       movzx    r13, r13w
       cmp      r15d, 47
       jb       G_M000_IG131
 
G_M000_IG40:                ;; offset=0x03A9
       cmp      r15d, 200
       jb       G_M000_IG88
 
G_M000_IG41:                ;; offset=0x03B6
       cmp      r15d, 321
       jb       G_M000_IG74
 
G_M000_IG42:                ;; offset=0x03C3
       cmp      r15d, 515
       jae      G_M000_IG67
 
G_M000_IG43:                ;; offset=0x03D0
       cmp      r15d, 514
       je       G_M000_IG60
 
G_M000_IG44:                ;; offset=0x03DD
       test     r14d, r14d
       jne      SHORT G_M000_IG46
 
G_M000_IG45:                ;; offset=0x03E2
       cmp      r12d, 321
       jae      G_M000_IG59
 
G_M000_IG46:                ;; offset=0x03EF
       xor      esi, esi
 
G_M000_IG47:                ;; offset=0x03F1
       movzx    r14, sil
       mov      rsi, qword ptr [rbp-0x58]
       inc      rsi
       mov      qword ptr [rbp-0x58], rsi
       test     r14d, r14d
       jne      G_M000_IG61
 
G_M000_IG48:                ;; offset=0x0409
       mov      esi, dword ptr [rbp-0x50]
       inc      esi
       mov      dword ptr [rbp-0x50], esi
 
G_M000_IG49:                ;; offset=0x0411
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD48]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 3
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG63
 
G_M000_IG50:                ;; offset=0x0443
       mov      r12, bword ptr [rbp-0xA8]
 
G_M000_IG51:                ;; offset=0x044A
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG52:                ;; offset=0x0455
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD52]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 4
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG65
 
G_M000_IG53:                ;; offset=0x048A
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG54:                ;; offset=0x0495
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD44]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
 
G_M000_IG55:                ;; offset=0x04B5
       mov      r14d, dword ptr [rbp-0x74]
       mov      r11d, dword ptr [rbp-0x78]
       mov      rdi, qword ptr [rbp-0x90]
       add      rdi, 4
       mov      r9, r12
       mov      r12d, r15d
       mov      r8d, dword ptr [rbp-0x94]
       mov      r10, bword ptr [rbp-0xB0]
 
G_M000_IG56:                ;; offset=0x04DC
       dec      r8d
       mov      dword ptr [rbp-0x94], r8d
       test     r8d, r8d
       je       G_M000_IG211
 
G_M000_IG57:                ;; offset=0x04EF
       mov      qword ptr [rbp-0x90], rdi
       mov      ecx, dword ptr [r10+rdi]
       mov      dword ptr [rbp-0x74], ecx
       mov      esi, ecx
       imul     rdx, rsi, 0x1B4E81B5
       shr      rdx, 38
       mov      dword ptr [rbp-0x78], edx
       imul     esi, edx, 600
       mov      r15d, ecx
       sub      r15d, esi
       cmp      ecx, r14d
       jb       G_M000_IG38
 
G_M000_IG58:                ;; offset=0x0522
       mov      r14d, edx
       sub      r14d, r11d
       mov      edx, dword ptr [rbp-0x78]
       jmp      G_M000_IG39
 
G_M000_IG59:                ;; offset=0x0530
       mov      esi, 1
       jmp      G_M000_IG47
 
G_M000_IG60:                ;; offset=0x053A
       mov      esi, 2
       jmp      G_M000_IG47
 
G_M000_IG61:                ;; offset=0x0544
       cmp      r14d, 2
       ja       G_M000_IG237
       mov      esi, r14d
       lea      r11, [reloc @RWD56]
       mov      r11d, dword ptr [r11+4*rsi]
       lea      r12, G_M000_IG02
       add      r11, r12
       jmp      r11
 
G_M000_IG62:                ;; offset=0x0569
       mov      esi, dword ptr [rbp-0x4C]
       inc      esi
       mov      dword ptr [rbp-0x4C], esi
       vmovss   xmm0, dword ptr [r9]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG49
 
G_M000_IG63:                ;; offset=0x058B
       cmp      r14d, 2
       ja       G_M000_IG239
       mov      r8d, r14d
       lea      rsi, [reloc @RWD68]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG64:                ;; offset=0x05AF
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       mov      r12, bword ptr [rbp-0xA8]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD52]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG52
 
G_M000_IG65:                ;; offset=0x05E5
       cmp      r14d, 2
       ja       G_M000_IG54
       mov      r8d, r14d
       lea      rsi, [reloc @RWD80]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG66:                ;; offset=0x0609
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD44]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG54
 
G_M000_IG67:                ;; offset=0x0638
       cmp      r15d, 599
       je       G_M000_IG26
 
G_M000_IG68:                ;; offset=0x0645
       test     r14d, r14d
       jne      SHORT G_M000_IG70
 
G_M000_IG69:                ;; offset=0x064A
       cmp      r12d, 515
       jae      G_M000_IG25
 
G_M000_IG70:                ;; offset=0x0657
       xor      esi, esi
 
G_M000_IG71:                ;; offset=0x0659
       movzx    rsi, sil
       mov      r11, qword ptr [rbp-0x58]
       add      r11, 2
       mov      qword ptr [rbp-0x58], r11
       test     esi, esi
       jne      G_M000_IG27
 
G_M000_IG72:                ;; offset=0x0671
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
 
G_M000_IG73:                ;; offset=0x067C
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD12]
       mov      edx, r15d
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG55
 
G_M000_IG74:                ;; offset=0x06A5
       cmp      r15d, 320
       je       G_M000_IG30
 
G_M000_IG75:                ;; offset=0x06B2
       test     r14d, r14d
       je       G_M000_IG86
 
G_M000_IG76:                ;; offset=0x06BB
       xor      esi, esi
 
G_M000_IG77:                ;; offset=0x06BD
       movzx    rsi, sil
       mov      r11, qword ptr [rbp-0x58]
       add      r11, 3
       mov      qword ptr [rbp-0x58], r11
       test     esi, esi
       jne      G_M000_IG31
 
G_M000_IG78:                ;; offset=0x06D5
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
 
G_M000_IG79:                ;; offset=0x06E0
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD28]
       mov      edx, r15d
       mov      rcx, r9
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       cmp      r15d, 320
       je       G_M000_IG35
 
G_M000_IG80:                ;; offset=0x070A
       test     r14d, r14d
       je       G_M000_IG33
 
G_M000_IG81:                ;; offset=0x0713
       xor      r8d, r8d
 
G_M000_IG82:                ;; offset=0x0716
       movzx    rsi, r8b
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 4
       mov      qword ptr [rbp-0x58], r8
       test     esi, esi
       jne      G_M000_IG36
 
G_M000_IG83:                ;; offset=0x072E
       mov      r12, bword ptr [rbp-0xA8]
 
G_M000_IG84:                ;; offset=0x0735
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG85:                ;; offset=0x0740
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD44]
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG55
 
G_M000_IG86:                ;; offset=0x0762
       cmp      r12d, 123
       jb       G_M000_IG76
 
G_M000_IG87:                ;; offset=0x076C
       mov      esi, 1
       jmp      G_M000_IG77
 
G_M000_IG88:                ;; offset=0x0776
       cmp      r15d, 76
       jb       G_M000_IG115
 
G_M000_IG89:                ;; offset=0x0780
       cmp      r15d, 123
       jb       G_M000_IG99
 
G_M000_IG90:                ;; offset=0x078A
       test     r14d, r14d
       je       SHORT G_M000_IG95
 
G_M000_IG91:                ;; offset=0x078F
       xor      esi, esi
 
G_M000_IG92:                ;; offset=0x0791
       mov      r11, qword ptr [rbp-0x58]
       add      r11, 3
       mov      qword ptr [rbp-0x58], r11
       test     esi, esi
       jne      SHORT G_M000_IG97
 
G_M000_IG93:                ;; offset=0x07A1
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
 
G_M000_IG94:                ;; offset=0x07AC
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD28]
       mov      edx, r15d
       mov      rcx, r9
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG55
 
G_M000_IG95:                ;; offset=0x07D5
       cmp      r12d, 123
       jb       SHORT G_M000_IG91
 
G_M000_IG96:                ;; offset=0x07DB
       mov      esi, 1
       jmp      SHORT G_M000_IG92
 
G_M000_IG97:                ;; offset=0x07E2
       cmp      esi, 2
       ja       G_M000_IG233
       mov      r11d, esi
       lea      r14, [reloc @RWD92]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG98:                ;; offset=0x0806
       mov      r11d, dword ptr [rbp-0x4C]
       inc      r11d
       mov      dword ptr [rbp-0x4C], r11d
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG94
 
G_M000_IG99:                ;; offset=0x0833
       cmp      r15d, 122
       je       G_M000_IG110
 
G_M000_IG100:                ;; offset=0x083D
       test     r14d, r14d
       je       G_M000_IG108
 
G_M000_IG101:                ;; offset=0x0846
       xor      esi, esi
 
G_M000_IG102:                ;; offset=0x0848
       movzx    r14, sil
       mov      rsi, qword ptr [rbp-0x58]
       add      rsi, 2
       mov      qword ptr [rbp-0x58], rsi
       test     r14d, r14d
       jne      G_M000_IG111
 
G_M000_IG103:                ;; offset=0x0861
       mov      esi, dword ptr [rbp-0x50]
       inc      esi
       mov      dword ptr [rbp-0x50], esi
 
G_M000_IG104:                ;; offset=0x0869
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD52]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       lea      r8d, [r15-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD104]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD108]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD112]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 4
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG113
 
G_M000_IG105:                ;; offset=0x08C3
       mov      r12, bword ptr [rbp-0xA8]
 
G_M000_IG106:                ;; offset=0x08CA
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG107:                ;; offset=0x08D5
       lea      r8, [rbp-0x60]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG55
 
G_M000_IG108:                ;; offset=0x08F2
       cmp      r12d, 76
       jb       G_M000_IG101
 
G_M000_IG109:                ;; offset=0x08FC
       mov      esi, 1
       jmp      G_M000_IG102
 
G_M000_IG110:                ;; offset=0x0906
       mov      esi, 2
       jmp      G_M000_IG102
 
G_M000_IG111:                ;; offset=0x0910
       cmp      r14d, 2
       ja       G_M000_IG230
       mov      esi, r14d
       lea      r11, [reloc @RWD116]
       mov      r11d, dword ptr [r11+4*rsi]
       lea      r12, G_M000_IG02
       add      r11, r12
       jmp      r11
 
G_M000_IG112:                ;; offset=0x0935
       mov      esi, dword ptr [rbp-0x4C]
       inc      esi
       mov      dword ptr [rbp-0x4C], esi
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD52]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG104
 
G_M000_IG113:                ;; offset=0x095F
       cmp      r14d, 2
       ja       G_M000_IG232
       mov      r8d, r14d
       lea      rsi, [reloc @RWD128]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG114:                ;; offset=0x0983
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       mov      r12, bword ptr [rbp-0xA8]
       vmulss   xmm1, xmm0, dword ptr [r12]
       vaddss   xmm1, xmm1, dword ptr [r12+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm1
       jmp      G_M000_IG107
 
G_M000_IG115:                ;; offset=0x09B1
       cmp      r15d, 75
       je       G_M000_IG126
 
G_M000_IG116:                ;; offset=0x09BB
       test     r14d, r14d
       je       G_M000_IG124
 
G_M000_IG117:                ;; offset=0x09C4
       xor      esi, esi
 
G_M000_IG118:                ;; offset=0x09C6
       movzx    r14, sil
       mov      rsi, qword ptr [rbp-0x58]
       inc      rsi
       mov      qword ptr [rbp-0x58], rsi
       test     r14d, r14d
       jne      G_M000_IG127
 
G_M000_IG119:                ;; offset=0x09DE
       mov      esi, dword ptr [rbp-0x50]
       inc      esi
       mov      dword ptr [rbp-0x50], esi
 
G_M000_IG120:                ;; offset=0x09E6
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD140]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 3
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG129
 
G_M000_IG121:                ;; offset=0x0A18
       mov      r12, bword ptr [rbp-0xA8]
 
G_M000_IG122:                ;; offset=0x0A1F
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG123:                ;; offset=0x0A2A
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD28]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG55
 
G_M000_IG124:                ;; offset=0x0A4F
       cmp      r12d, 47
       jb       G_M000_IG117
 
G_M000_IG125:                ;; offset=0x0A59
       mov      esi, 1
       jmp      G_M000_IG118
 
G_M000_IG126:                ;; offset=0x0A63
       mov      esi, 2
       jmp      G_M000_IG118
 
G_M000_IG127:                ;; offset=0x0A6D
       cmp      r14d, 2
       ja       G_M000_IG227
       mov      esi, r14d
       lea      r11, [reloc @RWD144]
       mov      r11d, dword ptr [r11+4*rsi]
       lea      r12, G_M000_IG02
       add      r11, r12
       jmp      r11
 
G_M000_IG128:                ;; offset=0x0A92
       mov      esi, dword ptr [rbp-0x4C]
       inc      esi
       mov      dword ptr [rbp-0x4C], esi
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD140]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG120
 
G_M000_IG129:                ;; offset=0x0ABC
       cmp      r14d, 2
       ja       G_M000_IG229
       mov      r8d, r14d
       lea      rsi, [reloc @RWD156]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG130:                ;; offset=0x0AE0
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       mov      r12, bword ptr [rbp-0xA8]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG123
 
G_M000_IG131:                ;; offset=0x0B16
       cmp      r15d, 11
       jb       G_M000_IG162
 
G_M000_IG132:                ;; offset=0x0B20
       cmp      r15d, 18
       jb       G_M000_IG161
 
G_M000_IG133:                ;; offset=0x0B2A
       cmp      r15d, 29
       jb       G_M000_IG145
 
G_M000_IG134:                ;; offset=0x0B34
       cmp      r15d, 46
       je       SHORT G_M000_IG142
 
G_M000_IG135:                ;; offset=0x0B3A
       test     r14d, r14d
       je       SHORT G_M000_IG140
 
G_M000_IG136:                ;; offset=0x0B3F
       xor      esi, esi
 
G_M000_IG137:                ;; offset=0x0B41
       movzx    rsi, sil
       lea      r11d, [r15-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD168]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD52]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD140]
       mov      r11, qword ptr [rbp-0x58]
       inc      r11
       mov      qword ptr [rbp-0x58], r11
       test     esi, esi
       jne      SHORT G_M000_IG143
 
G_M000_IG138:                ;; offset=0x0B79
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
 
G_M000_IG139:                ;; offset=0x0B84
       lea      r8, [rbp-0x60]
       mov      edx, r15d
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG55
 
G_M000_IG140:                ;; offset=0x0BA2
       cmp      r12d, 29
       jb       SHORT G_M000_IG136
 
G_M000_IG141:                ;; offset=0x0BA8
       mov      esi, 1
       jmp      SHORT G_M000_IG137
 
G_M000_IG142:                ;; offset=0x0BAF
       mov      esi, 2
       jmp      SHORT G_M000_IG137
 
G_M000_IG143:                ;; offset=0x0BB6
       cmp      esi, 2
       ja       G_M000_IG226
       mov      r11d, esi
       lea      r14, [reloc @RWD172]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG144:                ;; offset=0x0BDA
       mov      r11d, dword ptr [rbp-0x4C]
       inc      r11d
       mov      dword ptr [rbp-0x4C], r11d
       vmulss   xmm1, xmm0, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm1
       jmp      SHORT G_M000_IG139
 
G_M000_IG145:                ;; offset=0x0BFC
       cmp      r15d, 28
       je       G_M000_IG156
 
G_M000_IG146:                ;; offset=0x0C06
       test     r14d, r14d
       je       G_M000_IG154
 
G_M000_IG147:                ;; offset=0x0C0F
       xor      esi, esi
 
G_M000_IG148:                ;; offset=0x0C11
       movzx    r14, sil
       mov      rsi, qword ptr [rbp-0x58]
       add      rsi, 2
       mov      qword ptr [rbp-0x58], rsi
       test     r14d, r14d
       jne      G_M000_IG157
 
G_M000_IG149:                ;; offset=0x0C2A
       mov      esi, dword ptr [rbp-0x50]
       inc      esi
       mov      dword ptr [rbp-0x50], esi
 
G_M000_IG150:                ;; offset=0x0C32
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD52]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 4
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG159
 
G_M000_IG151:                ;; offset=0x0C67
       mov      r12, bword ptr [rbp-0xA8]
 
G_M000_IG152:                ;; offset=0x0C6E
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG153:                ;; offset=0x0C79
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD44]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG55
 
G_M000_IG154:                ;; offset=0x0C9E
       cmp      r12d, 18
       jb       G_M000_IG147
 
G_M000_IG155:                ;; offset=0x0CA8
       mov      esi, 1
       jmp      G_M000_IG148
 
G_M000_IG156:                ;; offset=0x0CB2
       mov      esi, 2
       jmp      G_M000_IG148
 
G_M000_IG157:                ;; offset=0x0CBC
       cmp      r14d, 2
       ja       G_M000_IG223
       mov      esi, r14d
       lea      r11, [reloc @RWD184]
       mov      r11d, dword ptr [r11+4*rsi]
       lea      r12, G_M000_IG02
       add      r11, r12
       jmp      r11
 
G_M000_IG158:                ;; offset=0x0CE1
       mov      esi, dword ptr [rbp-0x4C]
       inc      esi
       mov      dword ptr [rbp-0x4C], esi
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD52]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG150
 
G_M000_IG159:                ;; offset=0x0D0B
       cmp      r14d, 2
       ja       G_M000_IG225
       mov      r8d, r14d
       lea      rsi, [reloc @RWD196]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG160:                ;; offset=0x0D2F
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       mov      r12, bword ptr [rbp-0xA8]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD44]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG153
 
G_M000_IG161:                ;; offset=0x0D65
       mov      r12, r9
       jmp      G_M000_IG55
 
G_M000_IG162:                ;; offset=0x0D6D
       cmp      r15d, 3
       jb       G_M000_IG204
 
G_M000_IG163:                ;; offset=0x0D77
       cmp      r15d, 7
       jb       G_M000_IG179
 
G_M000_IG164:                ;; offset=0x0D81
       cmp      r15d, 10
       je       G_M000_IG174
 
G_M000_IG165:                ;; offset=0x0D8B
       test     r14d, r14d
       je       G_M000_IG173
 
G_M000_IG166:                ;; offset=0x0D94
       xor      esi, esi
 
G_M000_IG167:                ;; offset=0x0D96
       movzx    r14, sil
       mov      rsi, qword ptr [rbp-0x58]
       add      rsi, 2
       mov      qword ptr [rbp-0x58], rsi
       test     r14d, r14d
       jne      G_M000_IG175
 
G_M000_IG168:                ;; offset=0x0DAF
       mov      esi, dword ptr [rbp-0x50]
       inc      esi
       mov      dword ptr [rbp-0x50], esi
 
G_M000_IG169:                ;; offset=0x0DB7
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD12]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r9
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 4
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG177
 
G_M000_IG170:                ;; offset=0x0DEC
       mov      r12, bword ptr [rbp-0xA8]
 
G_M000_IG171:                ;; offset=0x0DF3
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG172:                ;; offset=0x0DFE
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD44]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG55
 
G_M000_IG173:                ;; offset=0x0E23
       cmp      r12d, 3
       jb       G_M000_IG166
       mov      esi, 1
       jmp      G_M000_IG167
 
G_M000_IG174:                ;; offset=0x0E37
       mov      esi, 2
       jmp      G_M000_IG167
 
G_M000_IG175:                ;; offset=0x0E41
       cmp      r14d, 2
       ja       G_M000_IG220
       mov      esi, r14d
       lea      r11, [reloc @RWD208]
       mov      r11d, dword ptr [r11+4*rsi]
       lea      r12, G_M000_IG02
       add      r11, r12
       jmp      r11
 
G_M000_IG176:                ;; offset=0x0E66
       mov      esi, dword ptr [rbp-0x4C]
       inc      esi
       mov      dword ptr [rbp-0x4C], esi
       vmovss   xmm0, dword ptr [r9]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG169
 
G_M000_IG177:                ;; offset=0x0E90
       cmp      r14d, 2
       ja       G_M000_IG222
       mov      r8d, r14d
       lea      rsi, [reloc @RWD220]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG178:                ;; offset=0x0EB4
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       mov      r12, bword ptr [rbp-0xA8]
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD44]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG172
 
G_M000_IG179:                ;; offset=0x0EEA
       cmp      r15d, 6
       je       G_M000_IG193
 
G_M000_IG180:                ;; offset=0x0EF4
       test     r14d, r14d
       je       G_M000_IG192
 
G_M000_IG181:                ;; offset=0x0EFD
       xor      esi, esi
 
G_M000_IG182:                ;; offset=0x0EFF
       movzx    rsi, sil
       mov      dword ptr [rbp-0x7C], esi
       mov      r11, qword ptr [rbp-0x58]
       inc      r11
       mov      qword ptr [rbp-0x58], r11
       test     esi, esi
       jne      G_M000_IG194
 
G_M000_IG183:                ;; offset=0x0F19
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
 
G_M000_IG184:                ;; offset=0x0F24
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD48]
       mov      edx, r15d
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       test     r14d, r14d
       je       G_M000_IG196
 
G_M000_IG185:                ;; offset=0x0F47
       xor      r14d, r14d
 
G_M000_IG186:                ;; offset=0x0F4A
       lea      r8d, [r15-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 2
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG198
 
G_M000_IG187:                ;; offset=0x0F7C
       mov      r12, bword ptr [rbp-0xA8]
 
G_M000_IG188:                ;; offset=0x0F83
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG189:                ;; offset=0x0F8E
       lea      r8, [rbp-0x60]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, qword ptr [rbp-0x58]
       add      r8, 4
       mov      qword ptr [rbp-0x58], r8
       test     r14d, r14d
       jne      G_M000_IG201
 
G_M000_IG190:                ;; offset=0x0FBB
       mov      r8d, dword ptr [rbp-0x50]
       inc      r8d
       mov      dword ptr [rbp-0x50], r8d
 
G_M000_IG191:                ;; offset=0x0FC6
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD44]
       mov      esi, r14d
       mov      edx, r15d
       mov      rcx, r12
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG55
 
G_M000_IG192:                ;; offset=0x0FEB
       mov      esi, 1
       jmp      G_M000_IG182
 
G_M000_IG193:                ;; offset=0x0FF5
       mov      esi, 2
       jmp      G_M000_IG182
 
G_M000_IG194:                ;; offset=0x0FFF
       cmp      esi, 2
       ja       G_M000_IG217
       mov      r11d, esi
       mov      qword ptr [rbp-0xB8], r11
       lea      r11, [reloc @RWD232]
       mov      rbx, qword ptr [rbp-0xB8]
       mov      r11d, dword ptr [r11+4*rbx]
       lea      rsi, G_M000_IG02
       add      r11, rsi
       jmp      r11
 
G_M000_IG195:                ;; offset=0x1031
       mov      esi, dword ptr [rbp-0x4C]
       inc      esi
       mov      dword ptr [rbp-0x4C], esi
       vmovss   xmm0, dword ptr [r9]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       mov      esi, dword ptr [rbp-0x7C]
       jmp      G_M000_IG184
 
G_M000_IG196:                ;; offset=0x1056
       cmp      r12d, 3
       jb       G_M000_IG185
 
G_M000_IG197:                ;; offset=0x1060
       mov      r14d, 1
       jmp      G_M000_IG186
 
G_M000_IG198:                ;; offset=0x106B
       cmp      r14d, 2
       ja       G_M000_IG219
 
G_M000_IG199:                ;; offset=0x1075
       mov      r8d, r14d
       lea      rsi, [reloc @RWD244]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG200:                ;; offset=0x108F
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       mov      r12, bword ptr [rbp-0xA8]
       vmulss   xmm1, xmm0, dword ptr [r12]
       vaddss   xmm1, xmm1, dword ptr [r12+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm1
       jmp      G_M000_IG189
 
G_M000_IG201:                ;; offset=0x10BD
       cmp      r14d, 2
       ja       G_M000_IG191
 
G_M000_IG202:                ;; offset=0x10C7
       mov      r8d, r14d
       lea      rsi, [reloc @RWD256]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG203:                ;; offset=0x10E1
       mov      r8d, dword ptr [rbp-0x4C]
       inc      r8d
       mov      dword ptr [rbp-0x4C], r8d
       vmovss   xmm0, dword ptr [r12]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD44]
       vaddss   xmm0, xmm0, dword ptr [r12+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      G_M000_IG191
 
G_M000_IG204:                ;; offset=0x1110
       test     r14d, r14d
       je       G_M000_IG214
 
G_M000_IG205:                ;; offset=0x1119
       xor      esi, esi
 
G_M000_IG206:                ;; offset=0x111B
       mov      r11, qword ptr [rbp-0x58]
       inc      r11
       mov      qword ptr [rbp-0x58], r11
       test     esi, esi
       jne      SHORT G_M000_IG209
 
G_M000_IG207:                ;; offset=0x112A
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
 
G_M000_IG208:                ;; offset=0x1135
       lea      r8, [rbp-0x60]
       vmovss   xmm0, dword ptr [reloc @RWD48]
       mov      edx, r15d
       mov      rcx, r9
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG55
 
G_M000_IG209:                ;; offset=0x115B
       cmp      esi, 2
       ja       G_M000_IG215
       mov      r11d, esi
       lea      r14, [reloc @RWD268]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG210:                ;; offset=0x117F
       mov      r11d, dword ptr [rbp-0x4C]
       inc      r11d
       mov      dword ptr [rbp-0x4C], r11d
       vmovss   xmm0, dword ptr [r9]
       vaddss   xmm0, xmm0, dword ptr [r9+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x60]
       vmovss   dword ptr [rbp-0x60], xmm0
       jmp      SHORT G_M000_IG208
 
G_M000_IG211:                ;; offset=0x11A1
       mov      edi, 1
       cmp      r12d, 599
       je       G_M000_IG05
 
G_M000_IG212:                ;; offset=0x11B3
       mov      ecx, r14d
       mov      edx, r13d
       shl      rdx, 32
       or       rcx, rdx
       mov      edi, edi
       shl      rdi, 48
       or       rdi, rcx
       mov      qword ptr [rbp-0x88], rdi
       mov      r14d, dword ptr [rbp-0x88]
       movzx    r13, word  ptr [rbp-0x84]
       movzx    r12, word  ptr [rbp-0x82]
       mov      eax, dword ptr [rbp-0x64]
       add      eax, 8
       mov      rbx, gword ptr [rbp-0xA0]
       mov      rdi, gword ptr [rbx+0x08]
       cmp      dword ptr [rdi+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG213:                ;; offset=0x1201
       mov      r8, gword ptr [rbx+0x10]
       mov      esi, eax
       sar      esi, 3
       and      esi, 3
       cmp      esi, dword ptr [r8+0x08]
       jae      G_M000_IG245
       shl      rsi, 4
       lea      r9, bword ptr [r8+rsi+0x10]
       mov      bword ptr [rbp-0xA8], r9
       mov      gword ptr [rbp-0xA0], rbx
       mov      r8, gword ptr [rbx+0x08]
       test     r8, r8
       je       G_M000_IG241
       mov      esi, dword ptr [r8+0x08]
       mov      edx, eax
       add      rdx, 8
       cmp      rsi, rdx
       jb       G_M000_IG241
       mov      dword ptr [rbp-0x64], eax
       mov      esi, eax
       lea      r10, bword ptr [r8+4*rsi+0x10]
       movzx    r8, r12w
       test     r8b, 1
       je       G_M000_IG242
       movzx    r8, r12w
       test     r8b, 2
       jne      G_M000_IG243
       movzx    r13, r13w
       mov      r8d, r14d
       imul     r11, r8, 0x1B4E81B5
       shr      r11, 38
       imul     r8d, r11d, 600
       mov      r12d, r14d
       sub      r12d, r8d
       mov      bword ptr [rbp-0xB0], r10
       xor      edi, edi
       mov      r8d, 9
       jmp      G_M000_IG56
 
G_M000_IG214:                ;; offset=0x12A7
       mov      esi, 1
       jmp      G_M000_IG206
 
G_M000_IG215:                ;; offset=0x12B1
       jmp      G_M000_IG208
 
G_M000_IG216:                ;; offset=0x12B6
       mov      esi, dword ptr [rbp-0x7C]
       jmp      G_M000_IG183
 
G_M000_IG217:                ;; offset=0x12BE
       jmp      G_M000_IG184
 
G_M000_IG218:                ;; offset=0x12C3
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG188
 
G_M000_IG219:                ;; offset=0x12CF
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG189
 
G_M000_IG220:                ;; offset=0x12DB
       jmp      G_M000_IG169
 
G_M000_IG221:                ;; offset=0x12E0
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG171
 
G_M000_IG222:                ;; offset=0x12EC
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG172
 
G_M000_IG223:                ;; offset=0x12F8
       jmp      G_M000_IG150
 
G_M000_IG224:                ;; offset=0x12FD
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG152
 
G_M000_IG225:                ;; offset=0x1309
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG153
 
G_M000_IG226:                ;; offset=0x1315
       jmp      G_M000_IG139
 
G_M000_IG227:                ;; offset=0x131A
       jmp      G_M000_IG120
 
G_M000_IG228:                ;; offset=0x131F
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG122
 
G_M000_IG229:                ;; offset=0x132B
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG123
 
G_M000_IG230:                ;; offset=0x1337
       jmp      G_M000_IG104
 
G_M000_IG231:                ;; offset=0x133C
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG106
 
G_M000_IG232:                ;; offset=0x1348
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG107
 
G_M000_IG233:                ;; offset=0x1354
       jmp      G_M000_IG94
 
G_M000_IG234:                ;; offset=0x1359
       jmp      G_M000_IG79
 
G_M000_IG235:                ;; offset=0x135E
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG84
 
G_M000_IG236:                ;; offset=0x136A
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG85
 
G_M000_IG237:                ;; offset=0x1376
       jmp      G_M000_IG49
 
G_M000_IG238:                ;; offset=0x137B
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG51
 
G_M000_IG239:                ;; offset=0x1387
       mov      r12, bword ptr [rbp-0xA8]
       jmp      G_M000_IG52
 
G_M000_IG240:                ;; offset=0x1393
       jmp      G_M000_IG73
 
G_M000_IG241:                ;; offset=0x1398
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG242:                ;; offset=0x139F
       mov      rdi, 0x7FAA285AB698
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x455
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG243:                ;; offset=0x13DB
       mov      rdi, 0x7FAA285AB698
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x4CD
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r15
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG244:                ;; offset=0x1417
       mov      rdi, 0x7FAA285A0D20
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      edi, 0x4F7
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      r12, rax
       mov      edi, 0x503
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r12
       mov      rdi, r13
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r13
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG245:                ;; offset=0x146E
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	00000642h ; case G_M000_IG72
       	dd	0000021Bh ; case G_M000_IG28
       	dd	000001D3h ; case G_M000_IG24
RWD12  	dd	40400000h		;         3
RWD16  	dd	000006A6h ; case G_M000_IG78
       	dd	0000029Bh ; case G_M000_IG32
       	dd	000000CCh ; case G_M000_IG09
RWD28  	dd	40000000h		;         2
RWD32  	dd	0000132Fh ; case G_M000_IG235
       	dd	0000030Eh ; case G_M000_IG37
       	dd	00000248h ; case G_M000_IG29
RWD44  	dd	40A00000h		;         5
RWD48  	dd	3F800000h		;         1
RWD52  	dd	41000000h		;         8
RWD56  	dd	000003DAh ; case G_M000_IG48
       	dd	0000053Ah ; case G_M000_IG62
       	dd	000000BFh ; case G_M000_IG08
RWD68  	dd	0000134Ch ; case G_M000_IG238
       	dd	00000580h ; case G_M000_IG64
       	dd	000000A8h ; case G_M000_IG07
RWD80  	dd	0000045Bh ; case G_M000_IG53
       	dd	000005DAh ; case G_M000_IG66
       	dd	00000098h ; case G_M000_IG06
RWD92  	dd	00000772h ; case G_M000_IG93
       	dd	000007D7h ; case G_M000_IG98
       	dd	000000DCh ; case G_M000_IG10
RWD104 	dd	42380000h		;        46
RWD108 	dd	41A80000h		;        21
RWD112 	dd	42080000h		;        34
RWD116 	dd	00000832h ; case G_M000_IG103
       	dd	00000906h ; case G_M000_IG112
       	dd	00000103h ; case G_M000_IG12
RWD128 	dd	0000130Dh ; case G_M000_IG231
       	dd	00000954h ; case G_M000_IG114
       	dd	000000ECh ; case G_M000_IG11
RWD140 	dd	41500000h		;        13
RWD144 	dd	000009AFh ; case G_M000_IG119
       	dd	00000A63h ; case G_M000_IG128
       	dd	00000127h ; case G_M000_IG14
RWD156 	dd	000012F0h ; case G_M000_IG228
       	dd	00000AB1h ; case G_M000_IG130
       	dd	00000110h ; case G_M000_IG13
RWD168 	dd	41880000h		;        17
RWD172 	dd	00000B4Ah ; case G_M000_IG138
       	dd	00000BABh ; case G_M000_IG144
       	dd	00000134h ; case G_M000_IG15
RWD184 	dd	00000BFBh ; case G_M000_IG149
       	dd	00000CB2h ; case G_M000_IG158
       	dd	0000015Bh ; case G_M000_IG17
RWD196 	dd	000012CEh ; case G_M000_IG224
       	dd	00000D00h ; case G_M000_IG160
       	dd	00000144h ; case G_M000_IG16
RWD208 	dd	00000D80h ; case G_M000_IG168
       	dd	00000E37h ; case G_M000_IG176
       	dd	0000017Fh ; case G_M000_IG19
RWD220 	dd	000012B1h ; case G_M000_IG221
       	dd	00000E85h ; case G_M000_IG178
       	dd	00000168h ; case G_M000_IG18
RWD232 	dd	00001287h ; case G_M000_IG216
       	dd	00001002h ; case G_M000_IG195
       	dd	000001B3h ; case G_M000_IG22
RWD244 	dd	00001294h ; case G_M000_IG218
       	dd	00001060h ; case G_M000_IG200
       	dd	0000019Ch ; case G_M000_IG21
RWD256 	dd	00000F8Ch ; case G_M000_IG190
       	dd	000010B2h ; case G_M000_IG203
       	dd	0000018Ch ; case G_M000_IG20
RWD268 	dd	000010FBh ; case G_M000_IG207
       	dd	00001150h ; case G_M000_IG210
       	dd	000001C3h ; case G_M000_IG23

; Total bytes of code 5236


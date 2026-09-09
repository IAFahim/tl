; Assembly listing for method Tl.ConsumerFusion.ConsumerBenchmarks`1[Tl.ConsumerFusion.StateConsumer]:FusedSingle():Tl.ConsumerFusion.ConsumerReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 100
; 21 inlinees with PGO data; 88 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x50]
       mov      qword ptr [rbp-0x30], rsi
 
G_M000_IG02:                ;; offset=0x0017
       vxorps   xmm0, xmm0, xmm0
       xor      ebx, ebx
       xor      r15d, r15d
       xor      r14d, r14d
       xor      r13d, r13d
       mov      dword ptr [rbp-0x48], r13d
       mov      rcx, 0x1000000000000
       mov      qword ptr [rbp-0x38], rcx
       mov      r12d, dword ptr [rbp-0x38]
       movzx    rcx, word  ptr [rbp-0x34]
       movzx    rdx, word  ptr [rbp-0x32]
       xor      esi, esi
       mov      r8, gword ptr [rdi+0x08]
       cmp      dword ptr [r8+0x08], esi
       jg       G_M000_IG124
 
G_M000_IG03:                ;; offset=0x0054
       vmovd    eax, xmm0
       movzx    rcx, cx
       movzx    rdx, dx
       mov      rdi, qword ptr [rbp-0x30]
       mov      dword ptr [rdi], r12d
       mov      word  ptr [rdi+0x04], cx
       mov      word  ptr [rdi+0x06], dx
       mov      dword ptr [rdi+0x08], eax
       mov      qword ptr [rdi+0x10], rbx
       mov      dword ptr [rdi+0x18], r15d
       mov      dword ptr [rdi+0x1C], r14d
       mov      r13d, dword ptr [rbp-0x48]
       mov      dword ptr [rdi+0x20], r13d
       xor      eax, eax
       mov      dword ptr [rdi+0x24], eax
 
G_M000_IG04:                ;; offset=0x0089
       mov      qword ptr [rdi+0x28], rax
       mov      rax, rdi
 
G_M000_IG05:                ;; offset=0x0090
       add      rsp, 40
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x009F
       align    [0 bytes for IG07]
 
G_M000_IG07:                ;; offset=0x009F
       cmp      eax, r11d
       setb     r10b
       movzx    r10, r10b
       jmp      G_M000_IG126
 
G_M000_IG08:                ;; offset=0x00AF
       cmp      eax, 599
       je       G_M000_IG162
       test     r10d, r10d
       jne      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00BF
       cmp      r11d, 515
       jae      G_M000_IG82
 
G_M000_IG10:                ;; offset=0x00CC
       xor      eax, eax
 
G_M000_IG11:                ;; offset=0x00CE
       movzx    r10, al
       add      rbx, 2
       test     r10d, r10d
       je       G_M000_IG122
 
G_M000_IG12:                ;; offset=0x00DF
       cmp      r10d, 2
       ja       G_M000_IG123
       mov      r11d, r10d
       lea      rax, [reloc @RWD00]
       mov      eax, dword ptr [rax+4*r11]
       lea      r10, G_M000_IG02
       add      rax, r10
       jmp      rax
 
G_M000_IG13:                ;; offset=0x0103
       inc      r14d
       mov      dword ptr [rbp-0x44], r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       mov      r14d, dword ptr [rbp-0x44]
       jmp      G_M000_IG123
 
G_M000_IG14:                ;; offset=0x012A
       cmp      eax, 320
       je       G_M000_IG159
       test     r10d, r10d
       je       SHORT G_M000_IG23
 
G_M000_IG15:                ;; offset=0x013A
       xor      r12d, r12d
 
G_M000_IG16:                ;; offset=0x013D
       movzx    r12, r12b
       add      rbx, 3
       test     r12d, r12d
       jne      SHORT G_M000_IG25
 
G_M000_IG17:                ;; offset=0x014A
       inc      r15d
 
G_M000_IG18:                ;; offset=0x014D
       cmp      eax, 320
       je       G_M000_IG161
       test     r10d, r10d
       je       G_M000_IG27
 
G_M000_IG19:                ;; offset=0x0161
       xor      eax, eax
 
G_M000_IG20:                ;; offset=0x0163
       movzx    r10, al
       add      rbx, 4
       test     r10d, r10d
       je       G_M000_IG122
 
G_M000_IG21:                ;; offset=0x0174
       cmp      r10d, 2
       ja       G_M000_IG123
       mov      r11d, r10d
       lea      rax, [reloc @RWD16]
       mov      eax, dword ptr [rax+4*r11]
       lea      r10, G_M000_IG02
       add      rax, r10
       jmp      rax
 
G_M000_IG22:                ;; offset=0x0198
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG123
 
G_M000_IG23:                ;; offset=0x01A8
       cmp      r11d, 123
       jb       SHORT G_M000_IG15
 
G_M000_IG24:                ;; offset=0x01AE
       mov      r12d, 1
       jmp      SHORT G_M000_IG16
 
G_M000_IG25:                ;; offset=0x01B6
       cmp      r12d, 2
       ja       SHORT G_M000_IG18
       mov      dword ptr [rbp-0x44], r14d
       mov      r12d, r12d
       lea      r13, [reloc @RWD28]
       mov      r13d, dword ptr [r13+4*r12]
       lea      r14, G_M000_IG02
       add      r13, r14
       jmp      r13
 
G_M000_IG26:                ;; offset=0x01DC
       mov      r14d, dword ptr [rbp-0x44]
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD40]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG18
 
G_M000_IG27:                ;; offset=0x01FF
       cmp      r11d, 200
       jb       G_M000_IG19
 
G_M000_IG28:                ;; offset=0x020C
       mov      eax, 1
       jmp      G_M000_IG20
 
G_M000_IG29:                ;; offset=0x0216
       cmp      eax, 76
       jb       G_M000_IG94
 
G_M000_IG30:                ;; offset=0x021F
       cmp      eax, 123
       jb       G_M000_IG85
 
G_M000_IG31:                ;; offset=0x0228
       test     r10d, r10d
       je       G_M000_IG83
 
G_M000_IG32:                ;; offset=0x0231
       xor      eax, eax
 
G_M000_IG33:                ;; offset=0x0233
       add      rbx, 3
       test     eax, eax
       je       G_M000_IG122
 
G_M000_IG34:                ;; offset=0x023F
       cmp      eax, 2
       ja       G_M000_IG123
       mov      r10d, eax
       lea      r11, [reloc @RWD44]
       mov      r11d, dword ptr [r11+4*r10]
       lea      rax, G_M000_IG02
       add      r11, rax
       jmp      r11
 
G_M000_IG35:                ;; offset=0x0263
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD40]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG123
 
G_M000_IG36:                ;; offset=0x0282
       cmp      eax, 11
       jb       G_M000_IG54
 
G_M000_IG37:                ;; offset=0x028B
       cmp      eax, 18
       jb       G_M000_IG123
 
G_M000_IG38:                ;; offset=0x0294
       cmp      eax, 29
       jb       G_M000_IG45
 
G_M000_IG39:                ;; offset=0x029D
       cmp      eax, 46
       je       G_M000_IG113
 
G_M000_IG40:                ;; offset=0x02A6
       test     r10d, r10d
       je       G_M000_IG157
 
G_M000_IG41:                ;; offset=0x02AF
       xor      r10d, r10d
 
G_M000_IG42:                ;; offset=0x02B2
       movzx    r11, r10b
       add      eax, -29
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rax
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD56]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD60]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD64]
       inc      rbx
       test     r11d, r11d
       je       G_M000_IG122
 
G_M000_IG43:                ;; offset=0x02E6
       cmp      r11d, 2
       ja       G_M000_IG123
       mov      eax, r11d
       lea      r10, [reloc @RWD68]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG44:                ;; offset=0x030B
       inc      r14d
       vmulss   xmm1, xmm1, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG123
 
G_M000_IG45:                ;; offset=0x0322
       cmp      eax, 28
       je       G_M000_IG156
       test     r10d, r10d
       je       SHORT G_M000_IG51
 
G_M000_IG46:                ;; offset=0x0330
       xor      eax, eax
 
G_M000_IG47:                ;; offset=0x0332
       movzx    r10, al
       add      rbx, 2
       test     r10d, r10d
       jne      SHORT G_M000_IG52
 
G_M000_IG48:                ;; offset=0x033F
       inc      r15d
 
G_M000_IG49:                ;; offset=0x0342
       add      rbx, 4
       test     r10d, r10d
       je       G_M000_IG122
 
G_M000_IG50:                ;; offset=0x034F
       cmp      r10d, 2
       ja       G_M000_IG123
       mov      eax, r10d
       lea      r10, [reloc @RWD80]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG51:                ;; offset=0x0374
       cmp      r11d, 18
       jb       SHORT G_M000_IG46
       jmp      G_M000_IG155
 
G_M000_IG52:                ;; offset=0x037F
       cmp      r10d, 2
       ja       SHORT G_M000_IG49
       mov      r11d, r10d
       lea      rax, [reloc @RWD92]
       mov      eax, dword ptr [rax+4*r11]
       lea      r12, G_M000_IG02
       add      rax, r12
       jmp      rax
 
G_M000_IG53:                ;; offset=0x039F
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD60]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      SHORT G_M000_IG49
 
G_M000_IG54:                ;; offset=0x03BB
       cmp      eax, 3
       jb       G_M000_IG142
 
G_M000_IG55:                ;; offset=0x03C4
       cmp      eax, 7
       jb       G_M000_IG66
 
G_M000_IG56:                ;; offset=0x03CD
       cmp      eax, 10
       je       SHORT G_M000_IG63
 
G_M000_IG57:                ;; offset=0x03D2
       test     r10d, r10d
       je       G_M000_IG154
 
G_M000_IG58:                ;; offset=0x03DB
       xor      eax, eax
 
G_M000_IG59:                ;; offset=0x03DD
       movzx    r10, al
       add      rbx, 2
       test     r10d, r10d
       jne      SHORT G_M000_IG64
 
G_M000_IG60:                ;; offset=0x03EA
       inc      r15d
 
G_M000_IG61:                ;; offset=0x03ED
       add      rbx, 4
       test     r10d, r10d
       je       G_M000_IG122
 
G_M000_IG62:                ;; offset=0x03FA
       cmp      r10d, 2
       ja       G_M000_IG123
       mov      eax, r10d
       lea      r10, [reloc @RWD104]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG63:                ;; offset=0x041F
       mov      eax, 2
       jmp      SHORT G_M000_IG59
 
G_M000_IG64:                ;; offset=0x0426
       cmp      r10d, 2
       ja       SHORT G_M000_IG61
       mov      r11d, r10d
       lea      rax, [reloc @RWD116]
       mov      eax, dword ptr [rax+4*r11]
       lea      r12, G_M000_IG02
       add      rax, r12
       jmp      rax
 
G_M000_IG65:                ;; offset=0x0446
       inc      r14d
       mov      dword ptr [rbp-0x44], r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       mov      r14d, dword ptr [rbp-0x44]
       jmp      SHORT G_M000_IG61
 
G_M000_IG66:                ;; offset=0x046A
       cmp      eax, 6
       je       G_M000_IG149
       test     r10d, r10d
       je       G_M000_IG148
       xor      r12d, r12d
 
G_M000_IG67:                ;; offset=0x047F
       movzx    r12, r12b
       inc      rbx
       test     r12d, r12d
       jne      SHORT G_M000_IG75
 
G_M000_IG68:                ;; offset=0x048B
       inc      r15d
 
G_M000_IG69:                ;; offset=0x048E
       test     r10d, r10d
       je       G_M000_IG152
 
G_M000_IG70:                ;; offset=0x0497
       xor      r10d, r10d
 
G_M000_IG71:                ;; offset=0x049A
       add      eax, -3
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rax
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD40]
       add      rbx, 2
       test     r10d, r10d
       jne      G_M000_IG77
 
G_M000_IG72:                ;; offset=0x04C3
       inc      r15d
 
G_M000_IG73:                ;; offset=0x04C6
       add      rbx, 4
       test     r10d, r10d
       je       G_M000_IG122
 
G_M000_IG74:                ;; offset=0x04D3
       cmp      r10d, 2
       ja       G_M000_IG123
       mov      eax, r10d
       lea      r10, [reloc @RWD128]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG75:                ;; offset=0x04F8
       cmp      r12d, 2
       ja       SHORT G_M000_IG69
       mov      dword ptr [rbp-0x44], r14d
       mov      r12d, r12d
       mov      qword ptr [rbp-0x50], r12
       lea      r12, [reloc @RWD140]
       mov      r14, qword ptr [rbp-0x50]
       mov      r12d, dword ptr [r12+4*r14]
       lea      r13, G_M000_IG02
       add      r12, r13
       jmp      r12
 
G_M000_IG76:                ;; offset=0x0525
       mov      r14d, dword ptr [rbp-0x44]
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG69
 
G_M000_IG77:                ;; offset=0x0540
       cmp      r10d, 2
       ja       SHORT G_M000_IG73
       mov      eax, r10d
       lea      r11, [reloc @RWD152]
       mov      r11d, dword ptr [r11+4*rax]
       lea      r12, G_M000_IG02
       add      r11, r12
       jmp      r11
 
G_M000_IG78:                ;; offset=0x0561
       inc      r14d
       vmulss   xmm1, xmm1, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG73
 
G_M000_IG79:                ;; offset=0x0578
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG121
 
G_M000_IG80:                ;; offset=0x0588
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG119
 
G_M000_IG81:                ;; offset=0x0598
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       mov      r14d, dword ptr [rbp-0x44]
       jmp      G_M000_IG18
 
G_M000_IG82:                ;; offset=0x05AC
       mov      eax, 1
       jmp      G_M000_IG11
 
G_M000_IG83:                ;; offset=0x05B6
       cmp      r11d, 123
       jb       G_M000_IG32
 
G_M000_IG84:                ;; offset=0x05C0
       mov      eax, 1
       jmp      G_M000_IG33
 
G_M000_IG85:                ;; offset=0x05CA
       cmp      eax, 122
       je       G_M000_IG107
 
G_M000_IG86:                ;; offset=0x05D3
       test     r10d, r10d
       je       G_M000_IG105
 
G_M000_IG87:                ;; offset=0x05DC
       xor      r10d, r10d
 
G_M000_IG88:                ;; offset=0x05DF
       movzx    r11, r10b
       add      rbx, 2
       test     r11d, r11d
       jne      G_M000_IG108
 
G_M000_IG89:                ;; offset=0x05F0
       inc      r15d
 
G_M000_IG90:                ;; offset=0x05F3
       add      eax, -76
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rax
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD164]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD168]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD172]
       add      rbx, 4
       test     r11d, r11d
       je       G_M000_IG122
 
G_M000_IG91:                ;; offset=0x0624
       cmp      r11d, 2
       ja       G_M000_IG123
 
G_M000_IG92:                ;; offset=0x062E
       mov      eax, r11d
       lea      r10, [reloc @RWD176]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG93:                ;; offset=0x0649
       inc      r14d
       vmulss   xmm1, xmm1, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG123
 
G_M000_IG94:                ;; offset=0x0660
       cmp      eax, 75
       je       SHORT G_M000_IG102
 
G_M000_IG95:                ;; offset=0x0665
       test     r10d, r10d
       je       SHORT G_M000_IG101
 
G_M000_IG96:                ;; offset=0x066A
       xor      eax, eax
 
G_M000_IG97:                ;; offset=0x066C
       movzx    r10, al
       inc      rbx
       test     r10d, r10d
       jne      SHORT G_M000_IG103
 
G_M000_IG98:                ;; offset=0x0678
       inc      r15d
 
G_M000_IG99:                ;; offset=0x067B
       add      rbx, 3
       test     r10d, r10d
       je       G_M000_IG122
 
G_M000_IG100:                ;; offset=0x0688
       cmp      r10d, 2
       ja       G_M000_IG123
       mov      eax, r10d
       lea      r10, [reloc @RWD188]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG101:                ;; offset=0x06AD
       cmp      r11d, 47
       jb       SHORT G_M000_IG96
       jmp      G_M000_IG158
 
G_M000_IG102:                ;; offset=0x06B8
       mov      eax, 2
       jmp      SHORT G_M000_IG97
 
G_M000_IG103:                ;; offset=0x06BF
       cmp      r10d, 2
       ja       SHORT G_M000_IG99
       mov      r11d, r10d
       lea      rax, [reloc @RWD200]
       mov      eax, dword ptr [rax+4*r11]
       lea      r12, G_M000_IG02
       add      rax, r12
       jmp      rax
 
G_M000_IG104:                ;; offset=0x06DF
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD64]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      SHORT G_M000_IG99
 
G_M000_IG105:                ;; offset=0x06FB
       cmp      r11d, 76
       jb       G_M000_IG87
 
G_M000_IG106:                ;; offset=0x0705
       mov      r10d, 1
       jmp      G_M000_IG88
 
G_M000_IG107:                ;; offset=0x0710
       mov      r10d, 2
       jmp      G_M000_IG88
 
G_M000_IG108:                ;; offset=0x071B
       cmp      r11d, 2
       ja       G_M000_IG90
 
G_M000_IG109:                ;; offset=0x0725
       mov      r10d, r11d
       lea      r12, [reloc @RWD212]
       mov      r12d, dword ptr [r12+4*r10]
       lea      r13, G_M000_IG02
       add      r12, r13
       jmp      r12
 
G_M000_IG110:                ;; offset=0x0740
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD60]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG90
 
G_M000_IG111:                ;; offset=0x075F
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG90
 
G_M000_IG112:                ;; offset=0x076F
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG99
 
G_M000_IG113:                ;; offset=0x077F
       mov      r10d, 2
       jmp      G_M000_IG42
 
G_M000_IG114:                ;; offset=0x078A
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG49
 
G_M000_IG115:                ;; offset=0x079A
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG61
 
G_M000_IG116:                ;; offset=0x07AA
       xor      eax, eax
 
G_M000_IG117:                ;; offset=0x07AC
       movzx    r10, al
       inc      rbx
       test     r10d, r10d
       jne      G_M000_IG136
 
G_M000_IG118:                ;; offset=0x07BC
       inc      r15d
 
G_M000_IG119:                ;; offset=0x07BF
       add      rbx, 3
       test     r10d, r10d
       jne      G_M000_IG138
 
G_M000_IG120:                ;; offset=0x07CC
       inc      r15d
 
G_M000_IG121:                ;; offset=0x07CF
       add      rbx, 4
       test     r10d, r10d
       jne      G_M000_IG140
 
G_M000_IG122:                ;; offset=0x07DC
       inc      r15d
 
G_M000_IG123:                ;; offset=0x07DF
       mov      eax, r8d
       mov      ecx, ecx
       shl      rcx, 32
       or       rax, rcx
       mov      ecx, edx
       shl      rcx, 48
       or       rax, rcx
       mov      qword ptr [rbp-0x40], rax
       mov      r12d, dword ptr [rbp-0x40]
       movzx    rcx, word  ptr [rbp-0x3C]
       movzx    rdx, word  ptr [rbp-0x3A]
       inc      esi
       mov      r8, gword ptr [rdi+0x08]
       cmp      dword ptr [r8+0x08], esi
       jle      G_M000_IG03
 
G_M000_IG124:                ;; offset=0x0814
       mov      r9, gword ptr [rdi+0x10]
       mov      r10d, esi
       sar      r10d, 3
       and      r10d, 3
       cmp      r10d, dword ptr [r9+0x08]
       jae      G_M000_IG166
       shl      r10, 4
       lea      r9, bword ptr [r9+r10+0x10]
       cmp      esi, dword ptr [r8+0x08]
       jae      G_M000_IG166
       mov      r8d, dword ptr [r8+4*rsi+0x10]
       movzx    r10, dx
       test     r10b, 1
       je       G_M000_IG163
       movzx    rdx, dx
       test     dl, 2
       jne      G_M000_IG164
       mov      edx, r12d
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       imul     r10d, edx, 600
       mov      r11d, r12d
       sub      r11d, r10d
       mov      r10d, r8d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       imul     r13d, r10d, 600
       mov      eax, r8d
       sub      eax, r13d
       cmp      r8d, r12d
       jb       G_M000_IG07
 
G_M000_IG125:                ;; offset=0x089E
       sub      r10d, edx
 
G_M000_IG126:                ;; offset=0x08A1
       movzx    rdx, cx
       neg      edx
       add      edx, 0xFFFF
       movsxd   rdx, edx
       mov      r12d, r10d
       cmp      rdx, r12
       jl       G_M000_IG165
       add      ecx, r10d
       movzx    rcx, cx
       mov      edx, 1
       cmp      eax, 599
       je       G_M000_IG146
 
G_M000_IG127:                ;; offset=0x08D1
       cmp      eax, 47
       jb       G_M000_IG36
 
G_M000_IG128:                ;; offset=0x08DA
       cmp      eax, 200
       jb       G_M000_IG29
 
G_M000_IG129:                ;; offset=0x08E5
       cmp      eax, 321
       jb       G_M000_IG14
 
G_M000_IG130:                ;; offset=0x08F0
       cmp      eax, 515
       jae      G_M000_IG08
 
G_M000_IG131:                ;; offset=0x08FB
       cmp      eax, 514
       je       SHORT G_M000_IG135
 
G_M000_IG132:                ;; offset=0x0902
       test     r10d, r10d
       jne      G_M000_IG116
 
G_M000_IG133:                ;; offset=0x090B
       cmp      r11d, 321
       jb       G_M000_IG116
 
G_M000_IG134:                ;; offset=0x0918
       mov      eax, 1
       jmp      G_M000_IG117
 
G_M000_IG135:                ;; offset=0x0922
       mov      eax, 2
       jmp      G_M000_IG117
 
G_M000_IG136:                ;; offset=0x092C
       cmp      r10d, 2
       ja       G_M000_IG119
       mov      r11d, r10d
       lea      rax, [reloc @RWD224]
       mov      eax, dword ptr [rax+4*r11]
       lea      r13, G_M000_IG02
       add      rax, r13
       jmp      rax
 
G_M000_IG137:                ;; offset=0x0950
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG119
 
G_M000_IG138:                ;; offset=0x0967
       cmp      r10d, 2
       ja       G_M000_IG121
       mov      eax, r10d
       lea      r11, [reloc @RWD236]
       mov      r11d, dword ptr [r11+4*rax]
       lea      r12, G_M000_IG02
       add      r11, r12
       jmp      r11
 
G_M000_IG139:                ;; offset=0x098C
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD60]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG121
 
G_M000_IG140:                ;; offset=0x09AB
       cmp      r10d, 2
       ja       G_M000_IG123
       mov      eax, r10d
       lea      r10, [reloc @RWD248]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG141:                ;; offset=0x09D0
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD260]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG123
 
G_M000_IG142:                ;; offset=0x09EF
       test     r10d, r10d
       je       SHORT G_M000_IG147
       xor      eax, eax
 
G_M000_IG143:                ;; offset=0x09F6
       inc      rbx
       test     eax, eax
       je       G_M000_IG122
 
G_M000_IG144:                ;; offset=0x0A01
       cmp      eax, 2
       ja       G_M000_IG123
       mov      eax, eax
       lea      r10, [reloc @RWD264]
       mov      r10d, dword ptr [r10+4*rax]
       lea      r11, G_M000_IG02
       add      r10, r11
       jmp      r10
 
G_M000_IG145:                ;; offset=0x0A24
       inc      r14d
       vmovss   xmm1, dword ptr [r9]
       vaddss   xmm1, xmm1, dword ptr [r9+0x04]
       vaddss   xmm0, xmm1, xmm0
       jmp      G_M000_IG123
 
G_M000_IG146:                ;; offset=0x0A3B
       mov      edx, 5
       jmp      G_M000_IG127
 
G_M000_IG147:                ;; offset=0x0A45
       mov      eax, 1
       jmp      SHORT G_M000_IG143
 
G_M000_IG148:                ;; offset=0x0A4C
       mov      r12d, 1
       jmp      G_M000_IG67
 
G_M000_IG149:                ;; offset=0x0A57
       mov      r12d, 2
       jmp      G_M000_IG67
 
G_M000_IG150:                ;; offset=0x0A62
       mov      r14d, dword ptr [rbp-0x44]
       jmp      G_M000_IG68
 
G_M000_IG151:                ;; offset=0x0A6B
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       mov      r14d, dword ptr [rbp-0x44]
       jmp      G_M000_IG69
 
G_M000_IG152:                ;; offset=0x0A7F
       cmp      r11d, 3
       jb       G_M000_IG70
       mov      r10d, 1
       jmp      G_M000_IG71
 
G_M000_IG153:                ;; offset=0x0A94
       mov      r13d, dword ptr [rbp-0x48]
       inc      r13d
       mov      dword ptr [rbp-0x48], r13d
       jmp      G_M000_IG73
 
G_M000_IG154:                ;; offset=0x0AA4
       cmp      r11d, 3
       jb       G_M000_IG58
       mov      eax, 1
       jmp      G_M000_IG59
 
G_M000_IG155:                ;; offset=0x0AB8
       mov      eax, 1
       jmp      G_M000_IG47
 
G_M000_IG156:                ;; offset=0x0AC2
       mov      eax, 2
       jmp      G_M000_IG47
 
G_M000_IG157:                ;; offset=0x0ACC
       cmp      r11d, 29
       jb       G_M000_IG41
       mov      r10d, 1
       jmp      G_M000_IG42
 
G_M000_IG158:                ;; offset=0x0AE1
       mov      eax, 1
       jmp      G_M000_IG97
 
G_M000_IG159:                ;; offset=0x0AEB
       mov      r12d, 2
       jmp      G_M000_IG16
 
G_M000_IG160:                ;; offset=0x0AF6
       mov      r14d, dword ptr [rbp-0x44]
       jmp      G_M000_IG17
 
G_M000_IG161:                ;; offset=0x0AFF
       mov      eax, 2
       jmp      G_M000_IG20
 
G_M000_IG162:                ;; offset=0x0B09
       mov      eax, 2
       jmp      G_M000_IG11
 
G_M000_IG163:                ;; offset=0x0B13
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
 
G_M000_IG164:                ;; offset=0x0B4F
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
 
G_M000_IG165:                ;; offset=0x0B8B
       mov      rdi, 0x7FE093390D20
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      edi, 0x4F7
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x503
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r12
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r12
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG166:                ;; offset=0x0BE2
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	000007C5h ; case G_M000_IG122
       	dd	000000ECh ; case G_M000_IG13
       	dd	00000181h ; case G_M000_IG22
RWD12  	dd	40400000h		;         3
RWD16  	dd	000007C5h ; case G_M000_IG122
       	dd	000009B9h ; case G_M000_IG141
       	dd	00000181h ; case G_M000_IG22
RWD28  	dd	00000ADFh ; case G_M000_IG160
       	dd	000001C5h ; case G_M000_IG26
       	dd	00000581h ; case G_M000_IG81
RWD40  	dd	40000000h		;         2
RWD44  	dd	000007C5h ; case G_M000_IG122
       	dd	0000024Ch ; case G_M000_IG35
       	dd	00000181h ; case G_M000_IG22
RWD56  	dd	41880000h		;        17
RWD60  	dd	41000000h		;         8
RWD64  	dd	41500000h		;        13
RWD68  	dd	000007C5h ; case G_M000_IG122
       	dd	000002F4h ; case G_M000_IG44
       	dd	00000181h ; case G_M000_IG22
RWD80  	dd	000007C5h ; case G_M000_IG122
       	dd	000009B9h ; case G_M000_IG141
       	dd	00000181h ; case G_M000_IG22
RWD92  	dd	00000328h ; case G_M000_IG48
       	dd	00000388h ; case G_M000_IG53
       	dd	00000773h ; case G_M000_IG114
RWD104 	dd	000007C5h ; case G_M000_IG122
       	dd	000009B9h ; case G_M000_IG141
       	dd	00000181h ; case G_M000_IG22
RWD116 	dd	000003D3h ; case G_M000_IG60
       	dd	0000042Fh ; case G_M000_IG65
       	dd	00000783h ; case G_M000_IG115
RWD128 	dd	000007C5h ; case G_M000_IG122
       	dd	000009B9h ; case G_M000_IG141
       	dd	00000181h ; case G_M000_IG22
RWD140 	dd	00000A4Bh ; case G_M000_IG150
       	dd	0000050Eh ; case G_M000_IG76
       	dd	00000A54h ; case G_M000_IG151
RWD152 	dd	000004ACh ; case G_M000_IG72
       	dd	0000054Ah ; case G_M000_IG78
       	dd	00000A7Dh ; case G_M000_IG153
RWD164 	dd	42380000h		;        46
RWD168 	dd	41A80000h		;        21
RWD172 	dd	42080000h		;        34
RWD176 	dd	000007C5h ; case G_M000_IG122
       	dd	00000632h ; case G_M000_IG93
       	dd	00000181h ; case G_M000_IG22
RWD188 	dd	000007C5h ; case G_M000_IG122
       	dd	0000024Ch ; case G_M000_IG35
       	dd	00000181h ; case G_M000_IG22
RWD200 	dd	00000661h ; case G_M000_IG98
       	dd	000006C8h ; case G_M000_IG104
       	dd	00000758h ; case G_M000_IG112
RWD212 	dd	000005D9h ; case G_M000_IG89
       	dd	00000729h ; case G_M000_IG110
       	dd	00000748h ; case G_M000_IG111
RWD224 	dd	000007A5h ; case G_M000_IG118
       	dd	00000939h ; case G_M000_IG137
       	dd	00000571h ; case G_M000_IG80
RWD236 	dd	000007B5h ; case G_M000_IG120
       	dd	00000975h ; case G_M000_IG139
       	dd	00000561h ; case G_M000_IG79
RWD248 	dd	000007C5h ; case G_M000_IG122
       	dd	000009B9h ; case G_M000_IG141
       	dd	00000181h ; case G_M000_IG22
RWD260 	dd	40A00000h		;         5
RWD264 	dd	000007C5h ; case G_M000_IG122
       	dd	00000A0Dh ; case G_M000_IG145
       	dd	00000181h ; case G_M000_IG22

; Total bytes of code 3048


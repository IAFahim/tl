; Assembly listing for method Tl.ConsumerFusion.FusedPulse:Forward[Tl.ConsumerFusion.ConsumerInput,Tl.ConsumerFusion.EffectConsumer](byref,byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 759169
; 20 inlinees with PGO data; 104 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x60]
       mov      r15, rsi
       mov      rbx, rdx
 
G_M000_IG02:                ;; offset=0x0019
       movzx    rsi, word  ptr [rdi+0x06]
       test     sil, 1
       je       G_M000_IG167
       test     sil, 2
       jne      G_M000_IG168
       test     r8d, r8d
       je       G_M000_IG169
 
G_M000_IG03:                ;; offset=0x003A
       mov      r14d, dword ptr [rdi]
       movzx    r13, word  ptr [rdi+0x04]
       mov      esi, r14d
       imul     rax, rsi, 0x1B4E81B5
       shr      rax, 38
       imul     esi, eax, 600
       mov      r12d, r14d
       sub      r12d, esi
       mov      r9, rcx
       mov      bword ptr [rbp-0x50], r9
       mov      r10d, r8d
       mov      dword ptr [rbp-0x3C], r10d
       xor      r11d, r11d
       cmp      r11d, r10d
       jl       G_M000_IG21
 
G_M000_IG04:                ;; offset=0x0076
       mov      eax, 1
       mov      edi, 5
       cmp      r12d, 599
       cmove    eax, edi
       mov      edi, r14d
       mov      ecx, r13d
       shl      rcx, 32
       or       rdi, rcx
       shl      rax, 48
       or       rax, rdi
 
G_M000_IG05:                ;; offset=0x009E
       add      rsp, 56
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x00AD
       cmp      edx, r12d
       setb     r14b
       movzx    r14, r14b
       jmp      G_M000_IG23
 
G_M000_IG07:                ;; offset=0x00BD
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG19
 
G_M000_IG08:                ;; offset=0x00C9
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      SHORT G_M000_IG17
 
G_M000_IG09:                ;; offset=0x00D2
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      SHORT G_M000_IG15
 
G_M000_IG10:                ;; offset=0x00DB
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG54
 
G_M000_IG11:                ;; offset=0x00E7
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG59
 
G_M000_IG12:                ;; offset=0x00F3
       xor      r8d, r8d
 
G_M000_IG13:                ;; offset=0x00F6
       movzx    r14, r8b
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       inc      qword ptr [r8]
       test     r14d, r14d
       jne      G_M000_IG32
 
G_M000_IG14:                ;; offset=0x010F
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG15:                ;; offset=0x0116
       vmovss   xmm0, dword ptr [reloc @RWD00]
       mov      r8, rbx
       mov      esi, r14d
       mov      rcx, r15
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, r12
       add      qword ptr [r8], 3
       test     r14d, r14d
       jne      G_M000_IG34
 
G_M000_IG16:                ;; offset=0x013F
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG17:                ;; offset=0x0146
       vmovss   xmm0, dword ptr [reloc @RWD04]
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x30]
       mov      rcx, r15
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       add      qword ptr [r12], 4
       test     r14d, r14d
       jne      G_M000_IG36
 
G_M000_IG18:                ;; offset=0x0173
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG19:                ;; offset=0x017A
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x30]
       mov      rcx, r15
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
 
G_M000_IG20:                ;; offset=0x0199
       mov      r14d, dword ptr [rbp-0x38]
       mov      r12d, dword ptr [rbp-0x30]
       mov      eax, dword ptr [rbp-0x2C]
       mov      rdi, qword ptr [rbp-0x48]
       inc      edi
       mov      r10d, dword ptr [rbp-0x3C]
       cmp      edi, r10d
       mov      r11, rdi
       mov      r9, bword ptr [rbp-0x50]
       jge      G_M000_IG04
 
G_M000_IG21:                ;; offset=0x01BE
       mov      qword ptr [rbp-0x48], r11
       mov      edi, dword ptr [r9+4*r11]
       mov      dword ptr [rbp-0x38], edi
       mov      r8d, edi
       imul     rcx, r8, 0x1B4E81B5
       shr      rcx, 38
       mov      dword ptr [rbp-0x2C], ecx
       imul     r8d, ecx, 600
       mov      edx, edi
       sub      edx, r8d
       mov      dword ptr [rbp-0x30], edx
       cmp      edi, r14d
       jb       G_M000_IG06
 
G_M000_IG22:                ;; offset=0x01F2
       mov      r14d, ecx
       sub      r14d, eax
 
G_M000_IG23:                ;; offset=0x01F8
       mov      r8d, r13d
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   r8, r8d
       mov      esi, r14d
       cmp      r8, rsi
       jl       G_M000_IG208
       add      r13d, r14d
       movzx    r13, r13w
       cmp      edx, 47
       jb       G_M000_IG111
 
G_M000_IG24:                ;; offset=0x0224
       cmp      edx, 200
       jb       G_M000_IG71
 
G_M000_IG25:                ;; offset=0x0230
       cmp      edx, 321
       jb       G_M000_IG49
 
G_M000_IG26:                ;; offset=0x023C
       cmp      edx, 515
       jae      G_M000_IG38
 
G_M000_IG27:                ;; offset=0x0248
       cmp      edx, 514
       je       SHORT G_M000_IG31
 
G_M000_IG28:                ;; offset=0x0250
       test     r14d, r14d
       jne      G_M000_IG12
 
G_M000_IG29:                ;; offset=0x0259
       cmp      r12d, 321
       jb       G_M000_IG12
 
G_M000_IG30:                ;; offset=0x0266
       mov      r8d, 1
       jmp      G_M000_IG13
 
G_M000_IG31:                ;; offset=0x0271
       mov      r8d, 2
       jmp      G_M000_IG13
 
G_M000_IG32:                ;; offset=0x027C
       cmp      r14d, 2
       ja       G_M000_IG15
       mov      r8d, r14d
       lea      rsi, [reloc @RWD12]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rax, G_M000_IG02
       add      rsi, rax
       jmp      rsi
 
G_M000_IG33:                ;; offset=0x02A0
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG15
 
G_M000_IG34:                ;; offset=0x02BF
       cmp      r14d, 2
       ja       G_M000_IG17
       mov      r8d, r14d
       lea      rsi, [reloc @RWD24]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG35:                ;; offset=0x02E3
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG17
 
G_M000_IG36:                ;; offset=0x030A
       cmp      r14d, 2
       ja       G_M000_IG19
       mov      r8d, r14d
       lea      rsi, [reloc @RWD36]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG37:                ;; offset=0x032E
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG19
 
G_M000_IG38:                ;; offset=0x0355
       cmp      edx, 599
       je       SHORT G_M000_IG46
 
G_M000_IG39:                ;; offset=0x035D
       test     r14d, r14d
       jne      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x0362
       cmp      r12d, 515
       jae      SHORT G_M000_IG45
 
G_M000_IG41:                ;; offset=0x036B
       xor      r8d, r8d
 
G_M000_IG42:                ;; offset=0x036E
       movzx    rsi, r8b
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       add      qword ptr [r8], 2
       test     esi, esi
       jne      SHORT G_M000_IG47
 
G_M000_IG43:                ;; offset=0x0383
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG44:                ;; offset=0x038A
       vmovss   xmm0, dword ptr [reloc @RWD48]
       mov      r8, rbx
       mov      rcx, r15
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG45:                ;; offset=0x03A8
       mov      r8d, 1
       jmp      SHORT G_M000_IG42
 
G_M000_IG46:                ;; offset=0x03B0
       mov      r8d, 2
       jmp      SHORT G_M000_IG42
 
G_M000_IG47:                ;; offset=0x03B8
       cmp      esi, 2
       ja       SHORT G_M000_IG44
       mov      r8d, esi
       lea      rax, [reloc @RWD52]
       mov      eax, dword ptr [rax+4*r8]
       lea      r14, G_M000_IG02
       add      rax, r14
       jmp      rax
 
G_M000_IG48:                ;; offset=0x03D7
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD48]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      SHORT G_M000_IG44
 
G_M000_IG49:                ;; offset=0x03FB
       cmp      edx, 320
       je       G_M000_IG62
 
G_M000_IG50:                ;; offset=0x0407
       test     r14d, r14d
       je       G_M000_IG60
 
G_M000_IG51:                ;; offset=0x0410
       xor      r8d, r8d
 
G_M000_IG52:                ;; offset=0x0413
       movzx    rsi, r8b
       cmp      byte  ptr [rbx], bl
       lea      r8, bword ptr [rbx+0x08]
       mov      rax, r8
       mov      bword ptr [rbp-0x58], rax
       mov      r8, rax
       add      qword ptr [r8], 3
       test     esi, esi
       jne      G_M000_IG63
 
G_M000_IG53:                ;; offset=0x0433
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG54:                ;; offset=0x043A
       vmovss   xmm0, dword ptr [reloc @RWD64]
       mov      r8, rbx
       mov      rcx, r15
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      eax, dword ptr [rbp-0x30]
       cmp      eax, 320
       je       G_M000_IG68
 
G_M000_IG55:                ;; offset=0x0461
       test     r14d, r14d
       je       G_M000_IG66
 
G_M000_IG56:                ;; offset=0x046A
       xor      r8d, r8d
 
G_M000_IG57:                ;; offset=0x046D
       movzx    rsi, r8b
       mov      r12, bword ptr [rbp-0x58]
       mov      r8, r12
       add      qword ptr [r8], 4
       test     esi, esi
       jne      G_M000_IG69
 
G_M000_IG58:                ;; offset=0x0484
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG59:                ;; offset=0x048B
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      r8, rbx
       mov      edx, eax
       mov      rcx, r15
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG60:                ;; offset=0x04AB
       cmp      r12d, 123
       jb       G_M000_IG51
 
G_M000_IG61:                ;; offset=0x04B5
       mov      r8d, 1
       jmp      G_M000_IG52
 
G_M000_IG62:                ;; offset=0x04C0
       mov      r8d, 2
       jmp      G_M000_IG52
 
G_M000_IG63:                ;; offset=0x04CB
       cmp      esi, 2
       ja       G_M000_IG54
 
G_M000_IG64:                ;; offset=0x04D4
       mov      r8d, esi
       mov      qword ptr [rbp-0x60], r8
       lea      r8, [reloc @RWD68]
       mov      r10, qword ptr [rbp-0x60]
       mov      r8d, dword ptr [r8+4*r10]
       lea      r9, G_M000_IG02
       add      r8, r9
       jmp      r8
 
G_M000_IG65:                ;; offset=0x04F7
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG54
 
G_M000_IG66:                ;; offset=0x051E
       cmp      r12d, 200
       jb       G_M000_IG56
 
G_M000_IG67:                ;; offset=0x052B
       mov      r8d, 1
       jmp      G_M000_IG57
 
G_M000_IG68:                ;; offset=0x0536
       mov      r8d, 2
       jmp      G_M000_IG57
 
G_M000_IG69:                ;; offset=0x0541
       cmp      esi, 2
       ja       G_M000_IG59
       mov      r8d, esi
       lea      rdx, [reloc @RWD80]
       mov      edx, dword ptr [rdx+4*r8]
       lea      rcx, G_M000_IG02
       add      rdx, rcx
       jmp      rdx
 
G_M000_IG70:                ;; offset=0x0564
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG59
 
G_M000_IG71:                ;; offset=0x058B
       cmp      edx, 76
       jb       G_M000_IG97
 
G_M000_IG72:                ;; offset=0x0594
       cmp      edx, 123
       jb       G_M000_IG82
 
G_M000_IG73:                ;; offset=0x059D
       test     r14d, r14d
       je       SHORT G_M000_IG78
 
G_M000_IG74:                ;; offset=0x05A2
       xor      esi, esi
 
G_M000_IG75:                ;; offset=0x05A4
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       add      qword ptr [r8], 3
       test     esi, esi
       jne      SHORT G_M000_IG80
 
G_M000_IG76:                ;; offset=0x05B5
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG77:                ;; offset=0x05BC
       vmovss   xmm0, dword ptr [reloc @RWD64]
       mov      r8, rbx
       mov      rcx, r15
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG78:                ;; offset=0x05DA
       cmp      r12d, 123
       jb       SHORT G_M000_IG74
 
G_M000_IG79:                ;; offset=0x05E0
       mov      esi, 1
       jmp      SHORT G_M000_IG75
 
G_M000_IG80:                ;; offset=0x05E7
       cmp      esi, 2
       ja       SHORT G_M000_IG77
       mov      r8d, esi
       lea      rax, [reloc @RWD92]
       mov      eax, dword ptr [rax+4*r8]
       lea      r14, G_M000_IG02
       add      rax, r14
       jmp      rax
 
G_M000_IG81:                ;; offset=0x0606
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      SHORT G_M000_IG77
 
G_M000_IG82:                ;; offset=0x062A
       cmp      edx, 122
       je       G_M000_IG92
 
G_M000_IG83:                ;; offset=0x0633
       test     r14d, r14d
       je       G_M000_IG90
 
G_M000_IG84:                ;; offset=0x063C
       xor      r8d, r8d
 
G_M000_IG85:                ;; offset=0x063F
       movzx    r14, r8b
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       add      qword ptr [r8], 2
       test     r14d, r14d
       jne      G_M000_IG93
 
G_M000_IG86:                ;; offset=0x0659
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG87:                ;; offset=0x0660
       vmovss   xmm0, dword ptr [reloc @RWD04]
       mov      r8, rbx
       mov      esi, r14d
       mov      rcx, r15
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      eax, dword ptr [rbp-0x30]
       lea      r8d, [rax-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD104]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD108]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD112]
       mov      r8, r12
       add      qword ptr [r8], 4
       test     r14d, r14d
       jne      G_M000_IG95
 
G_M000_IG88:                ;; offset=0x06B4
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG89:                ;; offset=0x06BB
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, eax
       mov      rcx, r15
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG90:                ;; offset=0x06D6
       cmp      r12d, 76
       jb       G_M000_IG84
 
G_M000_IG91:                ;; offset=0x06E0
       mov      r8d, 1
       jmp      G_M000_IG85
 
G_M000_IG92:                ;; offset=0x06EB
       mov      r8d, 2
       jmp      G_M000_IG85
 
G_M000_IG93:                ;; offset=0x06F6
       cmp      r14d, 2
       ja       G_M000_IG87
       mov      r8d, r14d
       lea      rsi, [reloc @RWD116]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rax, G_M000_IG02
       add      rsi, rax
       jmp      rsi
 
G_M000_IG94:                ;; offset=0x071A
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG87
 
G_M000_IG95:                ;; offset=0x0741
       cmp      r14d, 2
       ja       G_M000_IG89
       mov      r8d, r14d
       lea      rsi, [reloc @RWD128]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG96:                ;; offset=0x0765
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmulss   xmm1, xmm0, dword ptr [r15]
       vaddss   xmm1, xmm1, dword ptr [r15+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm1
       jmp      G_M000_IG89
 
G_M000_IG97:                ;; offset=0x0784
       cmp      edx, 75
       je       G_M000_IG106
 
G_M000_IG98:                ;; offset=0x078D
       test     r14d, r14d
       je       SHORT G_M000_IG105
 
G_M000_IG99:                ;; offset=0x0792
       xor      r8d, r8d
 
G_M000_IG100:                ;; offset=0x0795
       movzx    r14, r8b
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       inc      qword ptr [r8]
       test     r14d, r14d
       jne      SHORT G_M000_IG107
 
G_M000_IG101:                ;; offset=0x07AA
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG102:                ;; offset=0x07B1
       vmovss   xmm0, dword ptr [reloc @RWD140]
       mov      r8, rbx
       mov      esi, r14d
       mov      rcx, r15
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, r12
       add      qword ptr [r8], 3
       test     r14d, r14d
       jne      G_M000_IG109
 
G_M000_IG103:                ;; offset=0x07DA
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG104:                ;; offset=0x07E1
       vmovss   xmm0, dword ptr [reloc @RWD64]
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x30]
       mov      rcx, r15
       mov      edi, 2
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG105:                ;; offset=0x0805
       cmp      r12d, 47
       jb       SHORT G_M000_IG99
       jmp      G_M000_IG203
 
G_M000_IG106:                ;; offset=0x0810
       mov      r8d, 2
       jmp      G_M000_IG100
 
G_M000_IG107:                ;; offset=0x081B
       cmp      r14d, 2
       ja       SHORT G_M000_IG102
       mov      r8d, r14d
       lea      rsi, [reloc @RWD144]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rax, G_M000_IG02
       add      rsi, rax
       jmp      rsi
 
G_M000_IG108:                ;; offset=0x083B
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD140]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG102
 
G_M000_IG109:                ;; offset=0x0862
       cmp      r14d, 2
       ja       G_M000_IG104
       mov      r8d, r14d
       lea      rsi, [reloc @RWD156]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG110:                ;; offset=0x0886
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG104
 
G_M000_IG111:                ;; offset=0x08AD
       cmp      edx, 11
       jb       G_M000_IG136
 
G_M000_IG112:                ;; offset=0x08B6
       cmp      edx, 18
       jb       G_M000_IG135
 
G_M000_IG113:                ;; offset=0x08BF
       cmp      edx, 29
       jb       G_M000_IG123
 
G_M000_IG114:                ;; offset=0x08C8
       cmp      edx, 46
       je       SHORT G_M000_IG120
 
G_M000_IG115:                ;; offset=0x08CD
       test     r14d, r14d
       je       G_M000_IG200
 
G_M000_IG116:                ;; offset=0x08D6
       xor      r8d, r8d
 
G_M000_IG117:                ;; offset=0x08D9
       movzx    rsi, r8b
       lea      r8d, [rdx-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD168]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD140]
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       inc      qword ptr [r8]
       test     esi, esi
       jne      SHORT G_M000_IG121
 
G_M000_IG118:                ;; offset=0x0912
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG119:                ;; offset=0x0919
       mov      r8, rbx
       mov      rcx, r15
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG120:                ;; offset=0x092C
       mov      r8d, 2
       jmp      SHORT G_M000_IG117
 
G_M000_IG121:                ;; offset=0x0934
       cmp      esi, 2
       ja       SHORT G_M000_IG119
       mov      r8d, esi
       lea      rax, [reloc @RWD172]
       mov      eax, dword ptr [rax+4*r8]
       lea      r14, G_M000_IG02
       add      rax, r14
       jmp      rax
 
G_M000_IG122:                ;; offset=0x0953
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmulss   xmm1, xmm0, dword ptr [r15]
       vaddss   xmm1, xmm1, dword ptr [r15+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm1
       jmp      SHORT G_M000_IG119
 
G_M000_IG123:                ;; offset=0x096F
       cmp      edx, 28
       je       G_M000_IG197
 
G_M000_IG124:                ;; offset=0x0978
       test     r14d, r14d
       je       G_M000_IG195
 
G_M000_IG125:                ;; offset=0x0981
       xor      r8d, r8d
 
G_M000_IG126:                ;; offset=0x0984
       movzx    r14, r8b
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       add      qword ptr [r8], 2
       test     r14d, r14d
       jne      SHORT G_M000_IG131
 
G_M000_IG127:                ;; offset=0x099A
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG128:                ;; offset=0x09A1
       vmovss   xmm0, dword ptr [reloc @RWD04]
       mov      r8, rbx
       mov      esi, r14d
       mov      rcx, r15
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, r12
       add      qword ptr [r8], 4
       test     r14d, r14d
       jne      SHORT G_M000_IG133
 
G_M000_IG129:                ;; offset=0x09C9
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG130:                ;; offset=0x09D0
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x30]
       mov      rcx, r15
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG131:                ;; offset=0x09F4
       cmp      r14d, 2
       ja       SHORT G_M000_IG128
       mov      r8d, r14d
       lea      rsi, [reloc @RWD184]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rax, G_M000_IG02
       add      rsi, rax
       jmp      rsi
 
G_M000_IG132:                ;; offset=0x0A14
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG128
 
G_M000_IG133:                ;; offset=0x0A3B
       cmp      r14d, 2
       ja       SHORT G_M000_IG130
       mov      r8d, r14d
       lea      rsi, [reloc @RWD196]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG134:                ;; offset=0x0A5B
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG130
 
G_M000_IG135:                ;; offset=0x0A82
       jmp      G_M000_IG20
 
G_M000_IG136:                ;; offset=0x0A87
       cmp      edx, 3
       jb       G_M000_IG160
 
G_M000_IG137:                ;; offset=0x0A90
       cmp      edx, 7
       jae      G_M000_IG151
 
G_M000_IG138:                ;; offset=0x0A99
       cmp      edx, 6
       je       G_M000_IG150
 
G_M000_IG139:                ;; offset=0x0AA2
       test     r14d, r14d
       je       G_M000_IG175
 
G_M000_IG140:                ;; offset=0x0AAB
       xor      r8d, r8d
 
G_M000_IG141:                ;; offset=0x0AAE
       movzx    rsi, r8b
       mov      dword ptr [rbp-0x34], esi
       cmp      byte  ptr [rbx], bl
       lea      r8, bword ptr [rbx+0x08]
       mov      rax, r8
       mov      bword ptr [rbp-0x58], rax
       mov      r8, rax
       inc      qword ptr [r8]
       test     esi, esi
       jne      G_M000_IG176
 
G_M000_IG142:                ;; offset=0x0AD0
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG143:                ;; offset=0x0AD7
       vmovss   xmm0, dword ptr [reloc @RWD00]
       mov      r8, rbx
       mov      rcx, r15
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       test     r14d, r14d
       je       G_M000_IG180
 
G_M000_IG144:                ;; offset=0x0AF6
       xor      r14d, r14d
 
G_M000_IG145:                ;; offset=0x0AF9
       mov      r12d, dword ptr [rbp-0x30]
       lea      r8d, [r12-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD48]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD64]
       mov      rax, bword ptr [rbp-0x58]
       mov      r8, rax
       add      qword ptr [r8], 2
       test     r14d, r14d
       jne      G_M000_IG182
 
G_M000_IG146:                ;; offset=0x0B2F
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG147:                ;; offset=0x0B36
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, r12d
       mov      rcx, r15
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      rax, bword ptr [rbp-0x58]
       mov      r8, rax
       add      qword ptr [r8], 4
       test     r14d, r14d
       jne      G_M000_IG183
 
G_M000_IG148:                ;; offset=0x0B61
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG149:                ;; offset=0x0B68
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, r12d
       mov      rcx, r15
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG150:                ;; offset=0x0B8C
       mov      r8d, 2
       jmp      G_M000_IG141
 
G_M000_IG151:                ;; offset=0x0B97
       cmp      edx, 10
       je       G_M000_IG159
 
G_M000_IG152:                ;; offset=0x0BA0
       test     r14d, r14d
       je       G_M000_IG188
 
G_M000_IG153:                ;; offset=0x0BA9
       xor      r8d, r8d
 
G_M000_IG154:                ;; offset=0x0BAC
       movzx    r14, r8b
       cmp      byte  ptr [rbx], bl
       lea      rax, bword ptr [rbx+0x08]
       mov      r12, rax
       mov      r8, r12
       add      qword ptr [r8], 2
       test     r14d, r14d
       jne      G_M000_IG189
 
G_M000_IG155:                ;; offset=0x0BC9
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG156:                ;; offset=0x0BD0
       vmovss   xmm0, dword ptr [reloc @RWD48]
       mov      r8, rbx
       mov      esi, r14d
       mov      rcx, r15
       mov      edi, 1
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       mov      r8, r12
       add      qword ptr [r8], 4
       test     r14d, r14d
       jne      G_M000_IG190
 
G_M000_IG157:                ;; offset=0x0BFC
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG158:                ;; offset=0x0C03
       vmovss   xmm0, dword ptr [reloc @RWD08]
       mov      r8, rbx
       mov      esi, r14d
       mov      edx, dword ptr [rbp-0x30]
       mov      rcx, r15
       mov      edi, 3
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG159:                ;; offset=0x0C27
       mov      r8d, 2
       jmp      G_M000_IG154
 
G_M000_IG160:                ;; offset=0x0C32
       test     r14d, r14d
       je       G_M000_IG171
 
G_M000_IG161:                ;; offset=0x0C3B
       xor      esi, esi
 
G_M000_IG162:                ;; offset=0x0C3D
       cmp      byte  ptr [rbx], bl
       lea      r12, bword ptr [rbx+0x08]
       mov      r8, r12
       inc      qword ptr [r8]
       test     esi, esi
       jne      G_M000_IG172
 
G_M000_IG163:                ;; offset=0x0C51
       lea      r8, bword ptr [rbx+0x10]
       inc      dword ptr [r8]
 
G_M000_IG164:                ;; offset=0x0C58
       vmovss   xmm0, dword ptr [reloc @RWD00]
       mov      r8, rbx
       mov      rcx, r15
       xor      edi, edi
       call     [Tl.ConsumerFusion.EffectConsumer:Notify(ushort,float,byte,uint,byref,byref)]
       jmp      G_M000_IG20
 
G_M000_IG165:                ;; offset=0x0C73
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG44
 
G_M000_IG166:                ;; offset=0x0C7F
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG77
 
G_M000_IG167:                ;; offset=0x0C8B
       mov      rdi, 0x7FE09339B698
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x455
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG168:                ;; offset=0x0CC7
       mov      rdi, 0x7FE09339B698
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x4CD
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG169:                ;; offset=0x0D03
       mov      rax, qword ptr [rdi]
 
G_M000_IG170:                ;; offset=0x0D06
       add      rsp, 56
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG171:                ;; offset=0x0D15
       mov      esi, 1
       jmp      G_M000_IG162
 
G_M000_IG172:                ;; offset=0x0D1F
       cmp      esi, 2
       ja       G_M000_IG164
       mov      r8d, esi
       lea      rax, [reloc @RWD208]
       mov      eax, dword ptr [rax+4*r8]
       lea      r14, G_M000_IG02
       add      rax, r14
       jmp      rax
 
G_M000_IG173:                ;; offset=0x0D42
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG164
 
G_M000_IG174:                ;; offset=0x0D61
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG164
 
G_M000_IG175:                ;; offset=0x0D6D
       mov      r8d, 1
       jmp      G_M000_IG141
 
G_M000_IG176:                ;; offset=0x0D78
       cmp      esi, 2
       ja       G_M000_IG143
       mov      r8d, esi
       mov      qword ptr [rbp-0x60], r8
       lea      r8, [reloc @RWD220]
       mov      r9, qword ptr [rbp-0x60]
       mov      r8d, dword ptr [r8+4*r9]
       lea      rsi, G_M000_IG02
       add      r8, rsi
       jmp      r8
 
G_M000_IG177:                ;; offset=0x0DA4
       mov      esi, dword ptr [rbp-0x34]
       jmp      G_M000_IG142
 
G_M000_IG178:                ;; offset=0x0DAC
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       mov      esi, dword ptr [rbp-0x34]
       jmp      G_M000_IG143
 
G_M000_IG179:                ;; offset=0x0DCE
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       mov      esi, dword ptr [rbp-0x34]
       jmp      G_M000_IG143
 
G_M000_IG180:                ;; offset=0x0DDD
       cmp      r12d, 3
       jb       G_M000_IG144
 
G_M000_IG181:                ;; offset=0x0DE7
       mov      r14d, 1
       jmp      G_M000_IG145
 
G_M000_IG182:                ;; offset=0x0DF2
       cmp      r14d, 2
       ja       G_M000_IG147
       mov      r8d, r14d
       lea      rsi, [reloc @RWD232]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG183:                ;; offset=0x0E16
       cmp      r14d, 2
       ja       G_M000_IG149
       mov      r8d, r14d
       lea      rsi, [reloc @RWD244]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG184:                ;; offset=0x0E3A
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmulss   xmm1, xmm0, dword ptr [r15]
       vaddss   xmm1, xmm1, dword ptr [r15+0x04]
       vaddss   xmm1, xmm1, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm1
       jmp      G_M000_IG147
 
G_M000_IG185:                ;; offset=0x0E59
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG147
 
G_M000_IG186:                ;; offset=0x0E65
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG149
 
G_M000_IG187:                ;; offset=0x0E8C
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG149
 
G_M000_IG188:                ;; offset=0x0E98
       cmp      r12d, 3
       jb       G_M000_IG153
       mov      r8d, 1
       jmp      G_M000_IG154
 
G_M000_IG189:                ;; offset=0x0EAD
       cmp      r14d, 2
       ja       G_M000_IG156
       mov      r8d, r14d
       lea      rsi, [reloc @RWD256]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rax, G_M000_IG02
       add      rsi, rax
       jmp      rsi
 
G_M000_IG190:                ;; offset=0x0ED1
       cmp      r14d, 2
       ja       G_M000_IG158
       mov      r8d, r14d
       lea      rsi, [reloc @RWD268]
       mov      esi, dword ptr [rsi+4*r8]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG191:                ;; offset=0x0EF5
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD48]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG156
 
G_M000_IG192:                ;; offset=0x0F1C
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG156
 
G_M000_IG193:                ;; offset=0x0F28
       lea      r8, bword ptr [rbx+0x14]
       inc      dword ptr [r8]
       vmovss   xmm0, dword ptr [r15]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [r15+0x04]
       vaddss   xmm0, xmm0, dword ptr [rbx]
       vmovss   dword ptr [rbx], xmm0
       jmp      G_M000_IG158
 
G_M000_IG194:                ;; offset=0x0F4F
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG158
 
G_M000_IG195:                ;; offset=0x0F5B
       cmp      r12d, 18
       jb       G_M000_IG125
 
G_M000_IG196:                ;; offset=0x0F65
       mov      r8d, 1
       jmp      G_M000_IG126
 
G_M000_IG197:                ;; offset=0x0F70
       mov      r8d, 2
       jmp      G_M000_IG126
 
G_M000_IG198:                ;; offset=0x0F7B
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG128
 
G_M000_IG199:                ;; offset=0x0F87
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG130
 
G_M000_IG200:                ;; offset=0x0F93
       cmp      r12d, 29
       jb       G_M000_IG116
 
G_M000_IG201:                ;; offset=0x0F9D
       mov      r8d, 1
       jmp      G_M000_IG117
 
G_M000_IG202:                ;; offset=0x0FA8
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG119
 
G_M000_IG203:                ;; offset=0x0FB4
       mov      r8d, 1
       jmp      G_M000_IG100
 
G_M000_IG204:                ;; offset=0x0FBF
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG102
 
G_M000_IG205:                ;; offset=0x0FCB
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG104
 
G_M000_IG206:                ;; offset=0x0FD7
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG87
 
G_M000_IG207:                ;; offset=0x0FE3
       lea      r8, bword ptr [rbx+0x18]
       inc      dword ptr [r8]
       jmp      G_M000_IG89
 
G_M000_IG208:                ;; offset=0x0FEF
       mov      rdi, 0x7FE093390D20
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      edi, 0x4F7
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      edi, 0x503
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r14
       mov      rdi, r12
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r12
       call     CORINFO_HELP_THROW
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41000000h		;         8
RWD08  	dd	40A00000h		;         5
RWD12  	dd	000000F6h ; case G_M000_IG14
       	dd	00000287h ; case G_M000_IG33
       	dd	000000B9h ; case G_M000_IG09
RWD24  	dd	00000126h ; case G_M000_IG16
       	dd	000002CAh ; case G_M000_IG35
       	dd	000000B0h ; case G_M000_IG08
RWD36  	dd	0000015Ah ; case G_M000_IG18
       	dd	00000315h ; case G_M000_IG37
       	dd	000000A4h ; case G_M000_IG07
RWD48  	dd	40400000h		;         3
RWD52  	dd	0000036Ah ; case G_M000_IG43
       	dd	000003BEh ; case G_M000_IG48
       	dd	00000C5Ah ; case G_M000_IG165
RWD64  	dd	40000000h		;         2
RWD68  	dd	0000041Ah ; case G_M000_IG53
       	dd	000004DEh ; case G_M000_IG65
       	dd	000000C2h ; case G_M000_IG10
RWD80  	dd	0000046Bh ; case G_M000_IG58
       	dd	0000054Bh ; case G_M000_IG70
       	dd	000000CEh ; case G_M000_IG11
RWD92  	dd	0000059Ch ; case G_M000_IG76
       	dd	000005EDh ; case G_M000_IG81
       	dd	00000C66h ; case G_M000_IG166
RWD104 	dd	42380000h		;        46
RWD108 	dd	41A80000h		;        21
RWD112 	dd	42080000h		;        34
RWD116 	dd	00000640h ; case G_M000_IG86
       	dd	00000701h ; case G_M000_IG94
       	dd	00000FBEh ; case G_M000_IG206
RWD128 	dd	0000069Bh ; case G_M000_IG88
       	dd	0000074Ch ; case G_M000_IG96
       	dd	00000FCAh ; case G_M000_IG207
RWD140 	dd	41500000h		;        13
RWD144 	dd	00000791h ; case G_M000_IG101
       	dd	00000822h ; case G_M000_IG108
       	dd	00000FA6h ; case G_M000_IG204
RWD156 	dd	000007C1h ; case G_M000_IG103
       	dd	0000086Dh ; case G_M000_IG110
       	dd	00000FB2h ; case G_M000_IG205
RWD168 	dd	41880000h		;        17
RWD172 	dd	000008F9h ; case G_M000_IG118
       	dd	0000093Ah ; case G_M000_IG122
       	dd	00000F8Fh ; case G_M000_IG202
RWD184 	dd	00000981h ; case G_M000_IG127
       	dd	000009FBh ; case G_M000_IG132
       	dd	00000F62h ; case G_M000_IG198
RWD196 	dd	000009B0h ; case G_M000_IG129
       	dd	00000A42h ; case G_M000_IG134
       	dd	00000F6Eh ; case G_M000_IG199
RWD208 	dd	00000C38h ; case G_M000_IG163
       	dd	00000D29h ; case G_M000_IG173
       	dd	00000D48h ; case G_M000_IG174
RWD220 	dd	00000D8Bh ; case G_M000_IG177
       	dd	00000D93h ; case G_M000_IG178
       	dd	00000DB5h ; case G_M000_IG179
RWD232 	dd	00000B16h ; case G_M000_IG146
       	dd	00000E21h ; case G_M000_IG184
       	dd	00000E40h ; case G_M000_IG185
RWD244 	dd	00000B48h ; case G_M000_IG148
       	dd	00000E4Ch ; case G_M000_IG186
       	dd	00000E73h ; case G_M000_IG187
RWD256 	dd	00000BB0h ; case G_M000_IG155
       	dd	00000EDCh ; case G_M000_IG191
       	dd	00000F03h ; case G_M000_IG192
RWD268 	dd	00000BE3h ; case G_M000_IG157
       	dd	00000F0Fh ; case G_M000_IG193
       	dd	00000F36h ; case G_M000_IG194

; Total bytes of code 4166


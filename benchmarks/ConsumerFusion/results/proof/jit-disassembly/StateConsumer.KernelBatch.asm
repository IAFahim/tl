; Assembly listing for method Tl.ConsumerFusion.FusedPulse:Forward[Tl.ConsumerFusion.ConsumerInput,Tl.ConsumerFusion.StateConsumer](byref,byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 787713
; 20 inlinees with PGO data; 84 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       push     rax
       lea      rbp, [rsp+0x30]
 
G_M000_IG02:                ;; offset=0x0010
       movzx    rax, word  ptr [rdi+0x06]
       test     al, 1
       je       G_M000_IG156
       test     al, 2
       jne      G_M000_IG157
       test     r8d, r8d
       je       G_M000_IG158
 
G_M000_IG03:                ;; offset=0x002D
       mov      ebx, dword ptr [rdi]
       movzx    r15, word  ptr [rdi+0x04]
       mov      edi, ebx
       imul     rdi, rdi, 0x1B4E81B5
       shr      rdi, 38
       imul     eax, edi, 600
       mov      r14d, ebx
       sub      r14d, eax
       xor      eax, eax
       cmp      eax, r8d
       jl       G_M000_IG21
 
G_M000_IG04:                ;; offset=0x0058
       mov      eax, 1
       mov      edi, 5
       cmp      r14d, 599
       cmove    eax, edi
       mov      edi, ebx
       mov      ecx, r15d
       shl      rcx, 32
       or       rdi, rcx
       shl      rax, 48
       or       rax, rdi
 
G_M000_IG05:                ;; offset=0x007F
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x008E
       align    [0 bytes for IG07]
 
G_M000_IG07:                ;; offset=0x008E
       cmp      r13d, r14d
       setb     r11b
       movzx    r11, r11b
       jmp      G_M000_IG23
 
G_M000_IG08:                ;; offset=0x009E
       lea      rbx, bword ptr [rdx+0x18]
       inc      dword ptr [rbx]
       jmp      SHORT G_M000_IG18
 
G_M000_IG09:                ;; offset=0x00A6
       lea      rbx, bword ptr [rdx+0x18]
       inc      dword ptr [rbx]
       jmp      SHORT G_M000_IG16
 
G_M000_IG10:                ;; offset=0x00AE
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      SHORT G_M000_IG20
 
G_M000_IG11:                ;; offset=0x00B6
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       mov      rsi, bword ptr [rbp-0x30]
       jmp      G_M000_IG54
 
G_M000_IG12:                ;; offset=0x00C5
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      SHORT G_M000_IG20
 
G_M000_IG13:                ;; offset=0x00CD
       xor      r11d, r11d
 
G_M000_IG14:                ;; offset=0x00D0
       movzx    rdi, r11b
       lea      r11, bword ptr [rdx+0x08]
       mov      r14, r11
       inc      qword ptr [r14]
       test     edi, edi
       jne      G_M000_IG32
 
G_M000_IG15:                ;; offset=0x00E6
       lea      rbx, bword ptr [rdx+0x10]
       inc      dword ptr [rbx]
 
G_M000_IG16:                ;; offset=0x00EC
       mov      rbx, r11
       add      qword ptr [rbx], 3
       test     edi, edi
       jne      G_M000_IG35
 
G_M000_IG17:                ;; offset=0x00FB
       lea      rbx, bword ptr [rdx+0x10]
       inc      dword ptr [rbx]
 
G_M000_IG18:                ;; offset=0x0101
       add      qword ptr [r11], 4
       test     edi, edi
       jne      G_M000_IG37
 
G_M000_IG19:                ;; offset=0x010D
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
 
G_M000_IG20:                ;; offset=0x0113
       mov      ebx, r9d
       mov      r14d, r13d
       mov      edi, r10d
       inc      eax
       cmp      eax, r8d
       jge      G_M000_IG04
 
G_M000_IG21:                ;; offset=0x0127
       mov      r9d, dword ptr [rcx+4*rax]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       imul     r11d, r10d, 600
       mov      r13d, r9d
       sub      r13d, r11d
       cmp      r9d, ebx
       jb       G_M000_IG07
 
G_M000_IG22:                ;; offset=0x014F
       mov      r11d, r10d
       sub      r11d, edi
 
G_M000_IG23:                ;; offset=0x0155
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG199
       add      r15d, r11d
       movzx    r15, r15w
       cmp      r13d, 47
       jb       G_M000_IG106
 
G_M000_IG24:                ;; offset=0x0180
       cmp      r13d, 200
       jb       G_M000_IG69
 
G_M000_IG25:                ;; offset=0x018D
       cmp      r13d, 321
       jb       G_M000_IG49
 
G_M000_IG26:                ;; offset=0x019A
       cmp      r13d, 515
       jae      G_M000_IG39
 
G_M000_IG27:                ;; offset=0x01A7
       cmp      r13d, 514
       je       SHORT G_M000_IG31
 
G_M000_IG28:                ;; offset=0x01B0
       test     r11d, r11d
       jne      G_M000_IG13
 
G_M000_IG29:                ;; offset=0x01B9
       cmp      r14d, 321
       jb       G_M000_IG13
 
G_M000_IG30:                ;; offset=0x01C6
       mov      r11d, 1
       jmp      G_M000_IG14
 
G_M000_IG31:                ;; offset=0x01D1
       mov      r11d, 2
       jmp      G_M000_IG14
 
G_M000_IG32:                ;; offset=0x01DC
       cmp      edi, 2
       ja       G_M000_IG16
 
G_M000_IG33:                ;; offset=0x01E5
       mov      ebx, edi
       lea      r14, [reloc @RWD00]
       mov      r14d, dword ptr [r14+4*rbx]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG34:                ;; offset=0x01FF
       lea      rbx, bword ptr [rdx+0x14]
       inc      dword ptr [rbx]
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG16
 
G_M000_IG35:                ;; offset=0x021B
       cmp      edi, 2
       ja       G_M000_IG18
       mov      ebx, edi
       lea      r14, [reloc @RWD12]
       mov      r14d, dword ptr [r14+4*rbx]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG36:                ;; offset=0x023E
       lea      rbx, bword ptr [rdx+0x14]
       inc      dword ptr [rbx]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG18
 
G_M000_IG37:                ;; offset=0x0262
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD28]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG38:                ;; offset=0x0285
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD40]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG39:                ;; offset=0x02A9
       cmp      r13d, 599
       je       SHORT G_M000_IG46
 
G_M000_IG40:                ;; offset=0x02B2
       test     r11d, r11d
       jne      SHORT G_M000_IG42
 
G_M000_IG41:                ;; offset=0x02B7
       cmp      r14d, 515
       jae      SHORT G_M000_IG45
 
G_M000_IG42:                ;; offset=0x02C0
       xor      r11d, r11d
 
G_M000_IG43:                ;; offset=0x02C3
       movzx    rdi, r11b
       lea      r11, bword ptr [rdx+0x08]
       add      qword ptr [r11], 2
       test     edi, edi
       jne      SHORT G_M000_IG47
 
G_M000_IG44:                ;; offset=0x02D3
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG45:                ;; offset=0x02DE
       mov      r11d, 1
       jmp      SHORT G_M000_IG43
 
G_M000_IG46:                ;; offset=0x02E6
       mov      r11d, 2
       jmp      SHORT G_M000_IG43
 
G_M000_IG47:                ;; offset=0x02EE
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD44]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      r14, G_M000_IG02
       add      r11, r14
       jmp      r11
 
G_M000_IG48:                ;; offset=0x0311
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD56]
       mov      bword ptr [rbp-0x30], rsi
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       mov      rsi, bword ptr [rbp-0x30]
       jmp      G_M000_IG20
 
G_M000_IG49:                ;; offset=0x033D
       cmp      r13d, 320
       je       SHORT G_M000_IG61
 
G_M000_IG50:                ;; offset=0x0346
       test     r11d, r11d
       je       SHORT G_M000_IG59
 
G_M000_IG51:                ;; offset=0x034B
       xor      edi, edi
 
G_M000_IG52:                ;; offset=0x034D
       movzx    rdi, dil
       lea      rbx, bword ptr [rdx+0x08]
       mov      r12, rbx
       add      qword ptr [r12], 3
       test     edi, edi
       jne      SHORT G_M000_IG62
 
G_M000_IG53:                ;; offset=0x0361
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
 
G_M000_IG54:                ;; offset=0x0367
       cmp      r13d, 320
       je       G_M000_IG66
 
G_M000_IG55:                ;; offset=0x0374
       test     r11d, r11d
       je       G_M000_IG64
 
G_M000_IG56:                ;; offset=0x037D
       xor      r11d, r11d
 
G_M000_IG57:                ;; offset=0x0380
       movzx    rdi, r11b
       mov      r11, rbx
       add      qword ptr [r11], 4
       test     edi, edi
       jne      G_M000_IG67
 
G_M000_IG58:                ;; offset=0x0393
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG59:                ;; offset=0x039E
       cmp      r14d, 123
       jb       SHORT G_M000_IG51
 
G_M000_IG60:                ;; offset=0x03A4
       mov      edi, 1
       jmp      SHORT G_M000_IG52
 
G_M000_IG61:                ;; offset=0x03AB
       mov      edi, 2
       jmp      SHORT G_M000_IG52
 
G_M000_IG62:                ;; offset=0x03B2
       cmp      edi, 2
       ja       SHORT G_M000_IG54
       mov      bword ptr [rbp-0x30], rsi
       mov      edi, edi
       lea      r12, [reloc @RWD60]
       mov      r12d, dword ptr [r12+4*rdi]
       lea      rsi, G_M000_IG02
       add      r12, rsi
       jmp      r12
 
G_M000_IG63:                ;; offset=0x03D5
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       mov      rsi, bword ptr [rbp-0x30]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD72]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG54
 
G_M000_IG64:                ;; offset=0x03FD
       cmp      r14d, 200
       jb       G_M000_IG56
 
G_M000_IG65:                ;; offset=0x040A
       mov      r11d, 1
       jmp      G_M000_IG57
 
G_M000_IG66:                ;; offset=0x0415
       mov      r11d, 2
       jmp      G_M000_IG57
 
G_M000_IG67:                ;; offset=0x0420
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD76]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      r14, G_M000_IG02
       add      r11, r14
       jmp      r11
 
G_M000_IG68:                ;; offset=0x0443
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD40]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG69:                ;; offset=0x0467
       cmp      r13d, 76
       jb       G_M000_IG93
 
G_M000_IG70:                ;; offset=0x0471
       cmp      r13d, 123
       jb       SHORT G_M000_IG79
 
G_M000_IG71:                ;; offset=0x0477
       test     r11d, r11d
       je       SHORT G_M000_IG75
 
G_M000_IG72:                ;; offset=0x047C
       xor      r11d, r11d
 
G_M000_IG73:                ;; offset=0x047F
       lea      rbx, bword ptr [rdx+0x08]
       mov      rdi, rbx
       add      qword ptr [rdi], 3
       test     r11d, r11d
       jne      SHORT G_M000_IG77
 
G_M000_IG74:                ;; offset=0x048F
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG75:                ;; offset=0x049A
       cmp      r14d, 123
       jb       SHORT G_M000_IG72
 
G_M000_IG76:                ;; offset=0x04A0
       mov      r11d, 1
       jmp      SHORT G_M000_IG73
 
G_M000_IG77:                ;; offset=0x04A8
       cmp      r11d, 2
       ja       G_M000_IG20
       mov      edi, r11d
       lea      r11, [reloc @RWD88]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      r14, G_M000_IG02
       add      r11, r14
       jmp      r11
 
G_M000_IG78:                ;; offset=0x04CD
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD72]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG79:                ;; offset=0x04F1
       cmp      r13d, 122
       je       SHORT G_M000_IG88
 
G_M000_IG80:                ;; offset=0x04F7
       test     r11d, r11d
       je       SHORT G_M000_IG86
 
G_M000_IG81:                ;; offset=0x04FC
       xor      r11d, r11d
 
G_M000_IG82:                ;; offset=0x04FF
       movzx    rdi, r11b
       lea      rbx, bword ptr [rdx+0x08]
       mov      r11, rbx
       add      qword ptr [r11], 2
       test     edi, edi
       jne      SHORT G_M000_IG89
 
G_M000_IG83:                ;; offset=0x0512
       lea      r11, bword ptr [rdx+0x10]
       inc      dword ptr [r11]
 
G_M000_IG84:                ;; offset=0x0519
       lea      r11d, [r13-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD100]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD104]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD108]
       mov      r11, rbx
       add      qword ptr [r11], 4
       test     edi, edi
       jne      SHORT G_M000_IG91
 
G_M000_IG85:                ;; offset=0x0549
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG86:                ;; offset=0x0554
       cmp      r14d, 76
       jb       SHORT G_M000_IG81
 
G_M000_IG87:                ;; offset=0x055A
       mov      r11d, 1
       jmp      SHORT G_M000_IG82
 
G_M000_IG88:                ;; offset=0x0562
       mov      r11d, 2
       jmp      SHORT G_M000_IG82
 
G_M000_IG89:                ;; offset=0x056A
       cmp      edi, 2
       ja       SHORT G_M000_IG84
       mov      r11d, edi
       lea      r14, [reloc @RWD112]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG90:                ;; offset=0x058A
       lea      r11, bword ptr [rdx+0x14]
       inc      dword ptr [r11]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG84
 
G_M000_IG91:                ;; offset=0x05AF
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD124]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG92:                ;; offset=0x05D2
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmulss   xmm0, xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG93:                ;; offset=0x05EE
       cmp      r13d, 75
       je       SHORT G_M000_IG101
 
G_M000_IG94:                ;; offset=0x05F4
       test     r11d, r11d
       je       SHORT G_M000_IG100
 
G_M000_IG95:                ;; offset=0x05F9
       xor      r11d, r11d
 
G_M000_IG96:                ;; offset=0x05FC
       movzx    rdi, r11b
       lea      rbx, bword ptr [rdx+0x08]
       mov      r11, rbx
       inc      qword ptr [r11]
       test     edi, edi
       jne      SHORT G_M000_IG102
 
G_M000_IG97:                ;; offset=0x060E
       lea      r11, bword ptr [rdx+0x10]
       inc      dword ptr [r11]
 
G_M000_IG98:                ;; offset=0x0615
       mov      r11, rbx
       add      qword ptr [r11], 3
       test     edi, edi
       jne      SHORT G_M000_IG104
 
G_M000_IG99:                ;; offset=0x0620
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG100:                ;; offset=0x062B
       cmp      r14d, 47
       jb       SHORT G_M000_IG95
       jmp      G_M000_IG193
 
G_M000_IG101:                ;; offset=0x0636
       mov      r11d, 2
       jmp      SHORT G_M000_IG96
 
G_M000_IG102:                ;; offset=0x063E
       cmp      edi, 2
       ja       SHORT G_M000_IG98
       mov      r11d, edi
       lea      r14, [reloc @RWD136]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG103:                ;; offset=0x065E
       lea      r11, bword ptr [rdx+0x14]
       inc      dword ptr [r11]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD148]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      SHORT G_M000_IG98
 
G_M000_IG104:                ;; offset=0x0680
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD152]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG105:                ;; offset=0x06A3
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD72]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG106:                ;; offset=0x06C7
       cmp      r13d, 11
       jb       G_M000_IG128
 
G_M000_IG107:                ;; offset=0x06D1
       cmp      r13d, 18
       jb       G_M000_IG20
 
G_M000_IG108:                ;; offset=0x06DB
       cmp      r13d, 29
       jb       G_M000_IG117
 
G_M000_IG109:                ;; offset=0x06E5
       cmp      r13d, 46
       je       SHORT G_M000_IG114
 
G_M000_IG110:                ;; offset=0x06EB
       test     r11d, r11d
       je       G_M000_IG190
 
G_M000_IG111:                ;; offset=0x06F4
       xor      r11d, r11d
 
G_M000_IG112:                ;; offset=0x06F7
       movzx    rdi, r11b
       lea      r11d, [r13-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD164]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD148]
       lea      rbx, bword ptr [rdx+0x08]
       mov      r11, rbx
       inc      qword ptr [r11]
       test     edi, edi
       jne      SHORT G_M000_IG115
 
G_M000_IG113:                ;; offset=0x072E
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG114:                ;; offset=0x0739
       mov      r11d, 2
       jmp      SHORT G_M000_IG112
 
G_M000_IG115:                ;; offset=0x0741
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD168]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      r14, G_M000_IG02
       add      r11, r14
       jmp      r11
 
G_M000_IG116:                ;; offset=0x0764
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmulss   xmm0, xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG117:                ;; offset=0x0780
       cmp      r13d, 28
       je       G_M000_IG187
 
G_M000_IG118:                ;; offset=0x078A
       test     r11d, r11d
       je       G_M000_IG185
 
G_M000_IG119:                ;; offset=0x0793
       xor      r11d, r11d
 
G_M000_IG120:                ;; offset=0x0796
       movzx    rdi, r11b
       lea      rbx, bword ptr [rdx+0x08]
       mov      r11, rbx
       add      qword ptr [r11], 2
       test     edi, edi
       jne      SHORT G_M000_IG124
 
G_M000_IG121:                ;; offset=0x07A9
       lea      r11, bword ptr [rdx+0x10]
       inc      dword ptr [r11]
 
G_M000_IG122:                ;; offset=0x07B0
       mov      r11, rbx
       add      qword ptr [r11], 4
       test     edi, edi
       jne      SHORT G_M000_IG126
 
G_M000_IG123:                ;; offset=0x07BB
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG124:                ;; offset=0x07C6
       cmp      edi, 2
       ja       SHORT G_M000_IG122
       mov      r11d, edi
       lea      r14, [reloc @RWD180]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG125:                ;; offset=0x07E6
       lea      r11, bword ptr [rdx+0x14]
       inc      dword ptr [r11]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      SHORT G_M000_IG122
 
G_M000_IG126:                ;; offset=0x0808
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD192]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG127:                ;; offset=0x082B
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD40]
       mov      bword ptr [rbp-0x30], rsi
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       mov      rsi, bword ptr [rbp-0x30]
       jmp      G_M000_IG20
 
G_M000_IG128:                ;; offset=0x0857
       cmp      r13d, 3
       jb       G_M000_IG150
 
G_M000_IG129:                ;; offset=0x0861
       cmp      r13d, 7
       jae      G_M000_IG142
 
G_M000_IG130:                ;; offset=0x086B
       cmp      r13d, 6
       je       G_M000_IG141
 
G_M000_IG131:                ;; offset=0x0875
       test     r11d, r11d
       je       G_M000_IG164
 
G_M000_IG132:                ;; offset=0x087E
       xor      edi, edi
 
G_M000_IG133:                ;; offset=0x0880
       movzx    rdi, dil
       lea      rbx, bword ptr [rdx+0x08]
       mov      r12, rbx
       inc      qword ptr [r12]
       test     edi, edi
       jne      G_M000_IG165
 
G_M000_IG134:                ;; offset=0x0897
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
 
G_M000_IG135:                ;; offset=0x089D
       test     r11d, r11d
       je       G_M000_IG170
 
G_M000_IG136:                ;; offset=0x08A6
       xor      r11d, r11d
 
G_M000_IG137:                ;; offset=0x08A9
       lea      edi, [r13-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD56]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD72]
       mov      rdi, rbx
       add      qword ptr [rdi], 2
       test     r11d, r11d
       jne      G_M000_IG172
 
G_M000_IG138:                ;; offset=0x08D6
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
 
G_M000_IG139:                ;; offset=0x08DC
       mov      rdi, rbx
       add      qword ptr [rdi], 4
       test     r11d, r11d
       jne      G_M000_IG173
 
G_M000_IG140:                ;; offset=0x08EC
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG141:                ;; offset=0x08F7
       mov      edi, 2
       jmp      SHORT G_M000_IG133
 
G_M000_IG142:                ;; offset=0x08FE
       cmp      r13d, 10
       je       SHORT G_M000_IG149
 
G_M000_IG143:                ;; offset=0x0904
       test     r11d, r11d
       je       G_M000_IG178
 
G_M000_IG144:                ;; offset=0x090D
       xor      r11d, r11d
 
G_M000_IG145:                ;; offset=0x0910
       movzx    rdi, r11b
       lea      rbx, bword ptr [rdx+0x08]
       mov      r11, rbx
       add      qword ptr [r11], 2
       test     edi, edi
       jne      G_M000_IG179
 
G_M000_IG146:                ;; offset=0x0927
       lea      r11, bword ptr [rdx+0x10]
       inc      dword ptr [r11]
 
G_M000_IG147:                ;; offset=0x092E
       mov      r11, rbx
       add      qword ptr [r11], 4
       test     edi, edi
       jne      G_M000_IG180
 
G_M000_IG148:                ;; offset=0x093D
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG149:                ;; offset=0x0948
       mov      r11d, 2
       jmp      SHORT G_M000_IG145
 
G_M000_IG150:                ;; offset=0x0950
       test     r11d, r11d
       je       G_M000_IG160
 
G_M000_IG151:                ;; offset=0x0959
       xor      edi, edi
 
G_M000_IG152:                ;; offset=0x095B
       lea      rbx, bword ptr [rdx+0x08]
       mov      r11, rbx
       inc      qword ptr [r11]
       test     edi, edi
       jne      G_M000_IG161
 
G_M000_IG153:                ;; offset=0x096D
       lea      rdi, bword ptr [rdx+0x10]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG154:                ;; offset=0x0978
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG155:                ;; offset=0x0983
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG156:                ;; offset=0x098E
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
 
G_M000_IG157:                ;; offset=0x09CA
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
 
G_M000_IG158:                ;; offset=0x0A06
       mov      rax, qword ptr [rdi]
 
G_M000_IG159:                ;; offset=0x0A09
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG160:                ;; offset=0x0A18
       mov      edi, 1
       jmp      G_M000_IG152
 
G_M000_IG161:                ;; offset=0x0A22
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD204]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG162:                ;; offset=0x0A45
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG163:                ;; offset=0x0A61
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG164:                ;; offset=0x0A6C
       mov      edi, 1
       jmp      G_M000_IG133
 
G_M000_IG165:                ;; offset=0x0A76
       cmp      edi, 2
       ja       G_M000_IG135
 
G_M000_IG166:                ;; offset=0x0A7F
       mov      bword ptr [rbp-0x30], rsi
       mov      edi, edi
       lea      r12, [reloc @RWD216]
       mov      r12d, dword ptr [r12+4*rdi]
       lea      rsi, G_M000_IG02
       add      r12, rsi
       jmp      r12
 
G_M000_IG167:                ;; offset=0x0A9D
       mov      rsi, bword ptr [rbp-0x30]
       jmp      G_M000_IG134
 
G_M000_IG168:                ;; offset=0x0AA6
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       mov      rsi, bword ptr [rbp-0x30]
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG135
 
G_M000_IG169:                ;; offset=0x0AC6
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       mov      rsi, bword ptr [rbp-0x30]
       jmp      G_M000_IG135
 
G_M000_IG170:                ;; offset=0x0AD5
       cmp      r14d, 3
       jb       G_M000_IG136
 
G_M000_IG171:                ;; offset=0x0ADF
       mov      r11d, 1
       jmp      G_M000_IG137
 
G_M000_IG172:                ;; offset=0x0AEA
       cmp      r11d, 2
       ja       G_M000_IG139
       mov      edi, r11d
       lea      r14, [reloc @RWD228]
       mov      r14d, dword ptr [r14+4*rdi]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG173:                ;; offset=0x0B0F
       cmp      r11d, 2
       ja       G_M000_IG20
       mov      edi, r11d
       lea      r11, [reloc @RWD240]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG174:                ;; offset=0x0B34
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmulss   xmm0, xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG139
 
G_M000_IG175:                ;; offset=0x0B50
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG139
 
G_M000_IG176:                ;; offset=0x0B5B
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD40]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG177:                ;; offset=0x0B7F
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG178:                ;; offset=0x0B8A
       cmp      r14d, 3
       jb       G_M000_IG144
       mov      r11d, 1
       jmp      G_M000_IG145
 
G_M000_IG179:                ;; offset=0x0B9F
       cmp      edi, 2
       ja       G_M000_IG147
       mov      r11d, edi
       lea      r14, [reloc @RWD252]
       mov      r14d, dword ptr [r14+4*r11]
       lea      r12, G_M000_IG02
       add      r14, r12
       jmp      r14
 
G_M000_IG180:                ;; offset=0x0BC3
       cmp      edi, 2
       ja       G_M000_IG20
       mov      edi, edi
       lea      r11, [reloc @RWD264]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rbx, G_M000_IG02
       add      r11, rbx
       jmp      r11
 
G_M000_IG181:                ;; offset=0x0BE6
       lea      r11, bword ptr [rdx+0x14]
       inc      dword ptr [r11]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD56]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG147
 
G_M000_IG182:                ;; offset=0x0C0B
       lea      r11, bword ptr [rdx+0x18]
       inc      dword ptr [r11]
       jmp      G_M000_IG147
 
G_M000_IG183:                ;; offset=0x0C17
       lea      rdi, bword ptr [rdx+0x14]
       inc      dword ptr [rdi]
       vmovss   xmm0, dword ptr [rsi]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD40]
       vaddss   xmm0, xmm0, dword ptr [rsi+0x04]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG20
 
G_M000_IG184:                ;; offset=0x0C3B
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG185:                ;; offset=0x0C46
       cmp      r14d, 18
       jb       G_M000_IG119
 
G_M000_IG186:                ;; offset=0x0C50
       mov      r11d, 1
       jmp      G_M000_IG120
 
G_M000_IG187:                ;; offset=0x0C5B
       mov      r11d, 2
       jmp      G_M000_IG120
 
G_M000_IG188:                ;; offset=0x0C66
       lea      r11, bword ptr [rdx+0x18]
       inc      dword ptr [r11]
       jmp      G_M000_IG122
 
G_M000_IG189:                ;; offset=0x0C72
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG190:                ;; offset=0x0C7D
       cmp      r14d, 29
       jb       G_M000_IG111
 
G_M000_IG191:                ;; offset=0x0C87
       mov      r11d, 1
       jmp      G_M000_IG112
 
G_M000_IG192:                ;; offset=0x0C92
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG193:                ;; offset=0x0C9D
       mov      r11d, 1
       jmp      G_M000_IG96
 
G_M000_IG194:                ;; offset=0x0CA8
       lea      r11, bword ptr [rdx+0x18]
       inc      dword ptr [r11]
       jmp      G_M000_IG98
 
G_M000_IG195:                ;; offset=0x0CB4
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG196:                ;; offset=0x0CBF
       lea      r11, bword ptr [rdx+0x18]
       inc      dword ptr [r11]
       jmp      G_M000_IG84
 
G_M000_IG197:                ;; offset=0x0CCB
       lea      rdi, bword ptr [rdx+0x18]
       inc      dword ptr [rdi]
       jmp      G_M000_IG20
 
G_M000_IG198:                ;; offset=0x0CD6
       mov      rsi, bword ptr [rbp-0x30]
       jmp      G_M000_IG53
 
G_M000_IG199:                ;; offset=0x0CDF
       mov      rdi, 0x7FE093390D20
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x4F7
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x503
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r14
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
RWD00  	dd	000000D6h ; case G_M000_IG15
       	dd	000001EFh ; case G_M000_IG34
       	dd	00000096h ; case G_M000_IG09
RWD12  	dd	000000EBh ; case G_M000_IG17
       	dd	0000022Eh ; case G_M000_IG36
       	dd	0000008Eh ; case G_M000_IG08
RWD24  	dd	41000000h		;         8
RWD28  	dd	000000FDh ; case G_M000_IG19
       	dd	00000275h ; case G_M000_IG38
       	dd	0000009Eh ; case G_M000_IG10
RWD40  	dd	40A00000h		;         5
RWD44  	dd	000002C3h ; case G_M000_IG44
       	dd	00000301h ; case G_M000_IG48
       	dd	00000968h ; case G_M000_IG154
RWD56  	dd	40400000h		;         3
RWD60  	dd	00000CC6h ; case G_M000_IG198
       	dd	000003C5h ; case G_M000_IG63
       	dd	000000A6h ; case G_M000_IG11
RWD72  	dd	40000000h		;         2
RWD76  	dd	00000383h ; case G_M000_IG58
       	dd	00000433h ; case G_M000_IG68
       	dd	000000B5h ; case G_M000_IG12
RWD88  	dd	0000047Fh ; case G_M000_IG74
       	dd	000004BDh ; case G_M000_IG78
       	dd	00000973h ; case G_M000_IG155
RWD100 	dd	42380000h		;        46
RWD104 	dd	41A80000h		;        21
RWD108 	dd	42080000h		;        34
RWD112 	dd	00000502h ; case G_M000_IG83
       	dd	0000057Ah ; case G_M000_IG90
       	dd	00000CAFh ; case G_M000_IG196
RWD124 	dd	00000539h ; case G_M000_IG85
       	dd	000005C2h ; case G_M000_IG92
       	dd	00000CBBh ; case G_M000_IG197
RWD136 	dd	000005FEh ; case G_M000_IG97
       	dd	0000064Eh ; case G_M000_IG103
       	dd	00000C98h ; case G_M000_IG194
RWD148 	dd	41500000h		;        13
RWD152 	dd	00000610h ; case G_M000_IG99
       	dd	00000693h ; case G_M000_IG105
       	dd	00000CA4h ; case G_M000_IG195
RWD164 	dd	41880000h		;        17
RWD168 	dd	0000071Eh ; case G_M000_IG113
       	dd	00000754h ; case G_M000_IG116
       	dd	00000C82h ; case G_M000_IG192
RWD180 	dd	00000799h ; case G_M000_IG121
       	dd	000007D6h ; case G_M000_IG125
       	dd	00000C56h ; case G_M000_IG188
RWD192 	dd	000007ABh ; case G_M000_IG123
       	dd	0000081Bh ; case G_M000_IG127
       	dd	00000C62h ; case G_M000_IG189
RWD204 	dd	0000095Dh ; case G_M000_IG153
       	dd	00000A35h ; case G_M000_IG162
       	dd	00000A51h ; case G_M000_IG163
RWD216 	dd	00000A8Dh ; case G_M000_IG167
       	dd	00000A96h ; case G_M000_IG168
       	dd	00000AB6h ; case G_M000_IG169
RWD228 	dd	000008C6h ; case G_M000_IG138
       	dd	00000B24h ; case G_M000_IG174
       	dd	00000B40h ; case G_M000_IG175
RWD240 	dd	000008DCh ; case G_M000_IG140
       	dd	00000B4Bh ; case G_M000_IG176
       	dd	00000B6Fh ; case G_M000_IG177
RWD252 	dd	00000917h ; case G_M000_IG146
       	dd	00000BD6h ; case G_M000_IG181
       	dd	00000BFBh ; case G_M000_IG182
RWD264 	dd	0000092Dh ; case G_M000_IG148
       	dd	00000C07h ; case G_M000_IG183
       	dd	00000C2Bh ; case G_M000_IG184

; Total bytes of code 3382


; Assembly listing for method __TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 124 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       mov      r13d, edi
       mov      r15, rsi
       mov      r14d, edx
       mov      rbx, rcx
 
G_M000_IG02:                ;; offset=0x002D
       movzx    rdi, word  ptr [r15+0x0C]
       movzx    rsi, byte  ptr [r15+0x0E]
       movzx    rax, r13w
       cmp      edi, eax
       jne      G_M000_IG101
 
G_M000_IG03:                ;; offset=0x0043
       and      esi, 3
       cmp      esi, 1
       jne      G_M000_IG101
       movzx    rdi, r13w
       lea      rsi, [rbp-0x28]
       call     [Tl.Timeline:TryGetCompiledRoute(ushort,byref):bool]
       test     eax, eax
       je       G_M000_IG101
       test     byte  ptr [(reloc 0x7fdb602f7b58)], 1
       je       G_M000_IG103
 
G_M000_IG04:                ;; offset=0x0072
       movzx    rax, byte  ptr [rbp-0x28]
       cmp      eax, 256
       jae      G_M000_IG104
       mov      rcx, 0x7FDB5CA00D48
       cmp      word  ptr [rcx+2*rax], 1
       jne      G_M000_IG101
       cmp      byte  ptr [rbp-0x27], 1
       jne      G_M000_IG101
       mov      rax, qword ptr [r15]
       mov      ecx, dword ptr [r15+0x08]
       movzx    rdx, word  ptr [r15+0x0C]
       movzx    rdi, byte  ptr [r15+0x0E]
       movzx    rsi, r13w
       test     esi, esi
       jne      SHORT G_M000_IG06
       movzx    rsi, r13w
       cmp      edx, esi
       jne      SHORT G_M000_IG06
       movsxd   rdx, r14d
       mov      esi, edi
       and      esi, 3
       cmp      esi, 1
       jne      SHORT G_M000_IG06
       test     rdx, rdx
       jle      SHORT G_M000_IG05
       mov      rsi, rdx
       neg      rsi
       mov      r8, 0x7FFFFFFFFFFFFFFF
       add      rsi, r8
       cmp      rsi, rax
       jl       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00EB
       test     rdx, rdx
       jge      SHORT G_M000_IG07
       mov      rsi, rdx
       neg      rsi
       mov      r8, 0x8000000000000000
       add      rsi, r8
       cmp      rsi, rax
       jle      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x0108
       xor      eax, eax
       jmp      G_M000_IG54
       align    [0 bytes for IG09]
 
G_M000_IG07:                ;; offset=0x010F
       add      rdx, rax
       add      ecx, r14d
       test     r14d, r14d
       je       G_M000_IG53
       mov      rsi, rax
       sar      rsi, 63
       and      rsi, 63
       add      rsi, rax
       sar      rsi, 6
       shl      rsi, 6
       mov      r8, rax
       sub      r8, rsi
       jns      SHORT G_M000_IG08
       add      r8, 64
 
G_M000_IG08:                ;; offset=0x0140
       mov      esi, r8d
       cmp      r14d, 1
       je       G_M000_IG94
       cmp      r14d, -1
       je       G_M000_IG87
       cmp      r14d, 1
       jg       G_M000_IG55
       cmp      rax, rdx
       jle      G_M000_IG52
 
G_M000_IG09:                ;; offset=0x016A
       test     esi, esi
       je       SHORT G_M000_IG11
 
G_M000_IG10:                ;; offset=0x016E
       dec      esi
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0172
       mov      esi, 63
 
G_M000_IG12:                ;; offset=0x0177
       dec      rax
       mov      r8d, 192
       test     esi, esi
       jne      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0184
       mov      r8d, 196
 
G_M000_IG14:                ;; offset=0x018A
       cmp      esi, 63
       jne      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x018F
       or       r8d, 8
       movzx    r8, r8b
 
G_M000_IG16:                ;; offset=0x0197
       xor      r9d, r9d
       mov      dword ptr [rbp-0x48], r9d
       cmp      esi, 16
       jb       G_M000_IG43
       cmp      esi, 32
       jb       G_M000_IG33
       cmp      esi, 32
       jb       SHORT G_M000_IG17
       cmp      esi, 48
       jb       SHORT G_M000_IG25
 
G_M000_IG17:                ;; offset=0x01BA
       cmp      esi, 48
       jb       G_M000_IG51
       cmp      esi, 64
       jae      G_M000_IG51
       mov      r9d, r8d
       cmp      esi, 48
       jne      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x01D4
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG19:                ;; offset=0x01DF
       cmp      esi, 63
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x01E4
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG21:                ;; offset=0x01EC
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x01FA
       mov      r9d, -1
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x0202
       mov      r9d, 1
 
G_M000_IG24:                ;; offset=0x0208
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      G_M000_IG51
 
G_M000_IG25:                ;; offset=0x0227
       mov      r9d, r8d
       cmp      esi, 16
       jne      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x022F
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG27:                ;; offset=0x023A
       cmp      esi, 47
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x023F
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG29:                ;; offset=0x0247
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x0255
       mov      r9d, -1
       jmp      SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x025D
       mov      r9d, 1
 
G_M000_IG32:                ;; offset=0x0263
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      G_M000_IG51
 
G_M000_IG33:                ;; offset=0x0282
       mov      r9d, r8d
       test     esi, esi
       je       SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x0289
       cmp      esi, 16
       jne      SHORT G_M000_IG36
 
G_M000_IG35:                ;; offset=0x028E
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG36:                ;; offset=0x0299
       cmp      esi, 31
       je       SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x029E
       cmp      esi, 47
       jne      SHORT G_M000_IG39
 
G_M000_IG38:                ;; offset=0x02A3
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG39:                ;; offset=0x02AB
       lea      r8d, [rsi-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x48], xmm0
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x02E3
       mov      r9d, -1
       jmp      SHORT G_M000_IG42
 
G_M000_IG41:                ;; offset=0x02EB
       mov      r9d, 1
 
G_M000_IG42:                ;; offset=0x02F1
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x48]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      SHORT G_M000_IG51
 
G_M000_IG43:                ;; offset=0x030A
       mov      r9d, r8d
       test     esi, esi
       jne      SHORT G_M000_IG45
 
G_M000_IG44:                ;; offset=0x0311
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG45:                ;; offset=0x031C
       cmp      esi, 31
       jne      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0321
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG47:                ;; offset=0x0329
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x0337
       mov      r9d, -1
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:                ;; offset=0x033F
       mov      r9d, 1
 
G_M000_IG50:                ;; offset=0x0345
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
 
G_M000_IG51:                ;; offset=0x0357
       cmp      rax, rdx
       jg       G_M000_IG09
 
G_M000_IG52:                ;; offset=0x0360
       movzx    rax, r13w
       movzx    rdi, dil
       mov      qword ptr [r15], rdx
       mov      dword ptr [r15+0x08], ecx
       mov      word  ptr [r15+0x0C], ax
       mov      byte  ptr [r15+0x0E], dil
 
G_M000_IG53:                ;; offset=0x0378
       mov      eax, 1
 
G_M000_IG54:                ;; offset=0x037D
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG55:                ;; offset=0x038A
       cmp      rax, rdx
       jl       G_M000_IG78
       jmp      SHORT G_M000_IG52
 
G_M000_IG56:                ;; offset=0x0395
       mov      r9d, 1
 
G_M000_IG57:                ;; offset=0x039B
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      G_M000_IG75
 
G_M000_IG58:                ;; offset=0x03BA
       mov      r9d, r8d
       cmp      esi, 16
       jne      SHORT G_M000_IG59
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG59:                ;; offset=0x03CD
       cmp      esi, 47
       jne      SHORT G_M000_IG60
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG60:                ;; offset=0x03DA
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       SHORT G_M000_IG61
       mov      r9d, -1
       jmp      SHORT G_M000_IG62
 
G_M000_IG61:                ;; offset=0x03F0
       mov      r9d, 1
 
G_M000_IG62:                ;; offset=0x03F6
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      G_M000_IG75
 
G_M000_IG63:                ;; offset=0x0415
       mov      r9d, r8d
       test     esi, esi
       je       SHORT G_M000_IG64
       cmp      esi, 16
       jne      SHORT G_M000_IG65
 
G_M000_IG64:                ;; offset=0x0421
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG65:                ;; offset=0x042C
       cmp      esi, 31
       je       SHORT G_M000_IG66
       cmp      esi, 47
       jne      SHORT G_M000_IG67
 
G_M000_IG66:                ;; offset=0x0436
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG67:                ;; offset=0x043E
       lea      r8d, [rsi-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x40], xmm0
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       SHORT G_M000_IG68
       mov      r9d, -1
       jmp      SHORT G_M000_IG69
 
G_M000_IG68:                ;; offset=0x047E
       mov      r9d, 1
 
G_M000_IG69:                ;; offset=0x0484
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x40]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      SHORT G_M000_IG75
 
G_M000_IG70:                ;; offset=0x049D
       mov      r9d, r8d
       test     esi, esi
       jne      SHORT G_M000_IG71
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG71:                ;; offset=0x04AF
       cmp      esi, 31
       jne      SHORT G_M000_IG72
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG72:                ;; offset=0x04BC
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       SHORT G_M000_IG73
       mov      r9d, -1
       jmp      SHORT G_M000_IG74
 
G_M000_IG73:                ;; offset=0x04D2
       mov      r9d, 1
 
G_M000_IG74:                ;; offset=0x04D8
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
 
G_M000_IG75:                ;; offset=0x04EA
       inc      rax
       cmp      esi, 63
       je       SHORT G_M000_IG76
       inc      esi
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x04F6
       xor      esi, esi
 
G_M000_IG77:                ;; offset=0x04F8
       cmp      rax, rdx
       jge      G_M000_IG52
 
G_M000_IG78:                ;; offset=0x0501
       mov      r8d, 64
       test     esi, esi
       jne      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x050B
       mov      r8d, 68
 
G_M000_IG80:                ;; offset=0x0511
       cmp      esi, 63
       jne      SHORT G_M000_IG82
 
G_M000_IG81:                ;; offset=0x0516
       or       r8d, 8
       movzx    r8, r8b
 
G_M000_IG82:                ;; offset=0x051E
       xor      r9d, r9d
       mov      dword ptr [rbp-0x40], r9d
       cmp      esi, 16
       jb       G_M000_IG70
 
G_M000_IG83:                ;; offset=0x052E
       cmp      esi, 32
       jb       G_M000_IG63
       cmp      esi, 32
       jb       SHORT G_M000_IG84
       cmp      esi, 48
       jb       G_M000_IG58
 
G_M000_IG84:                ;; offset=0x0545
       cmp      esi, 48
       jb       SHORT G_M000_IG75
       cmp      esi, 64
       jae      SHORT G_M000_IG75
       mov      r9d, r8d
       cmp      esi, 48
       jne      SHORT G_M000_IG85
       mov      r9d, r8d
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG85:                ;; offset=0x0562
       cmp      esi, 63
       jne      SHORT G_M000_IG86
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG86:                ;; offset=0x056F
       mov      r8, bword ptr [rbx]
       vmovss   xmm0, dword ptr [r8]
       test     r9b, 128
       je       G_M000_IG56
       mov      r9d, -1
       jmp      G_M000_IG57
 
G_M000_IG87:                ;; offset=0x058C
       lea      eax, [rsi-0x01]
       mov      r8d, 63
       test     esi, esi
       mov      esi, r8d
       cmovne   esi, eax
       mov      eax, 192
       mov      r8d, 196
       test     esi, esi
       cmove    eax, r8d
       mov      r8d, eax
       or       r8d, 8
       movzx    r8, r8b
       cmp      esi, 63
       cmove    eax, r8d
       cmp      esi, 16
       jb       G_M000_IG93
       cmp      esi, 32
       jb       G_M000_IG90
       cmp      esi, 32
       jb       SHORT G_M000_IG88
       cmp      esi, 48
       jb       SHORT G_M000_IG89
 
G_M000_IG88:                ;; offset=0x05DC
       cmp      esi, 48
       jb       G_M000_IG52
       cmp      esi, 64
       jae      G_M000_IG52
       mov      r8d, eax
       or       eax, 1
       movzx    rax, al
       cmp      esi, 48
       cmove    r8d, eax
       mov      eax, r8d
       or       eax, 2
       movzx    rax, al
       cmp      esi, 63
       cmove    r8d, eax
       mov      rsi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rsi]
       mov      eax, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    eax, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, eax
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG52
 
G_M000_IG89:                ;; offset=0x0645
       mov      r8d, eax
       or       eax, 1
       movzx    rax, al
       cmp      esi, 16
       cmove    r8d, eax
       mov      eax, r8d
       or       eax, 2
       movzx    rax, al
       cmp      esi, 47
       cmove    r8d, eax
       mov      rsi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rsi]
       mov      eax, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    eax, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, eax
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG52
 
G_M000_IG90:                ;; offset=0x069C
       mov      r8d, eax
       test     esi, esi
       je       SHORT G_M000_IG91
       cmp      esi, 16
       jne      SHORT G_M000_IG92
 
G_M000_IG91:                ;; offset=0x06A8
       mov      r8d, eax
       or       r8d, 1
       movzx    r8, r8b
 
G_M000_IG92:                ;; offset=0x06B3
       mov      eax, r8d
       or       eax, 2
       movzx    rax, al
       cmp      esi, 31
       cmove    r8d, eax
       add      esi, -16
       mov      eax, esi
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x38], xmm0
       mov      rax, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rax]
       mov      esi, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    esi, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG52
 
G_M000_IG93:                ;; offset=0x0722
       mov      r8d, eax
       or       eax, 1
       movzx    rax, al
       test     esi, esi
       cmove    r8d, eax
       mov      rsi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rsi]
       mov      eax, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    eax, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, eax
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG52
 
G_M000_IG94:                ;; offset=0x0760
       mov      eax, 64
       mov      r8d, 68
       test     esi, esi
       cmove    eax, r8d
       mov      r8d, eax
       or       r8d, 8
       movzx    r8, r8b
       cmp      esi, 63
       cmove    eax, r8d
       cmp      esi, 16
       jb       G_M000_IG100
       cmp      esi, 32
       jb       G_M000_IG97
       cmp      esi, 32
       jb       SHORT G_M000_IG95
       cmp      esi, 48
       jb       SHORT G_M000_IG96
 
G_M000_IG95:                ;; offset=0x079F
       cmp      esi, 48
       jb       G_M000_IG52
       cmp      esi, 64
       jae      G_M000_IG52
       mov      r8d, eax
       or       eax, 1
       movzx    rax, al
       cmp      esi, 48
       cmove    r8d, eax
       mov      eax, r8d
       or       eax, 2
       movzx    rax, al
       cmp      esi, 63
       cmove    r8d, eax
       mov      rsi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rsi]
       mov      eax, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    eax, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, eax
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG52
 
G_M000_IG96:                ;; offset=0x0808
       mov      r8d, eax
       or       eax, 1
       movzx    rax, al
       cmp      esi, 16
       cmove    r8d, eax
       mov      eax, r8d
       or       eax, 2
       movzx    rax, al
       cmp      esi, 47
       cmove    r8d, eax
       mov      rsi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rsi]
       mov      eax, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    eax, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, eax
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG52
 
G_M000_IG97:                ;; offset=0x085F
       mov      r8d, eax
       test     esi, esi
       je       SHORT G_M000_IG98
       cmp      esi, 16
       jne      SHORT G_M000_IG99
 
G_M000_IG98:                ;; offset=0x086B
       mov      r8d, eax
       or       r8d, 1
       movzx    r8, r8b
 
G_M000_IG99:                ;; offset=0x0876
       mov      eax, r8d
       or       eax, 2
       movzx    rax, al
       cmp      esi, 31
       cmove    r8d, eax
       add      esi, -16
       mov      eax, esi
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       mov      rax, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rax]
       mov      esi, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    esi, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x30]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG52
 
G_M000_IG100:                ;; offset=0x08E5
       mov      r8d, eax
       or       eax, 1
       movzx    rax, al
       test     esi, esi
       cmove    r8d, eax
       mov      rax, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rax]
       mov      esi, -1
       mov      r9d, 1
       test     r8b, 128
       cmove    esi, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG52
 
G_M000_IG101:                ;; offset=0x0923
       xor      eax, eax
 
G_M000_IG102:                ;; offset=0x0925
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG103:                ;; offset=0x0932
       mov      rdi, 0x7FDB602F7AF0
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG04
 
G_M000_IG104:                ;; offset=0x0946
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2380

; Assembly listing for method __TlGeneratedSchema1:.cctor() (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:                ;; offset=0x0000
       sub      rsp, 520
 
G_M000_IG02:                ;; offset=0x0007
       lea      rdi, [rsp+0x08]
       call     [__TlGeneratedSchema1:CreateModules():__TlGeneratedSchema1+ModuleMap]
       mov      rdi, 0x7FDB5CA00D48
       lea      rsi, [rsp+0x08]
       mov      edx, 512
       call     CORINFO_HELP_MEMCPY
       nop      
 
G_M000_IG03:                ;; offset=0x002C
       add      rsp, 520
       ret      
 
; Total bytes of code 52

; Assembly listing for method __TlGeneratedSchema1:CreateModules():__TlGeneratedSchema1+ModuleMap (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 3 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbx
       sub      rsp, 512
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rsp], xmm8
       vmovdqa  xmmword ptr [rsp+0x10], xmm8
       mov      rax, -480
       vmovdqa  xmmword ptr [rsp+rax+0x200], xmm8
       vmovdqa  xmmword ptr [rsp+rax+0x210], xmm8
       vmovdqa  xmmword ptr [rsp+rax+0x220], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x0046
       mov      word  ptr [rsp], 1
       mov      rdi, rbx
       lea      rsi, [rsp]
       mov      edx, 512
       call     CORINFO_HELP_MEMCPY
       mov      rax, rbx
 
G_M000_IG03:                ;; offset=0x0060
       add      rsp, 512
       pop      rbx
       ret      
 
; Total bytes of code 105


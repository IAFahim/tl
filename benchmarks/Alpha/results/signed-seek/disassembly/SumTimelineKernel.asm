; Assembly listing for method SumTimeline:.cctor() (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 3 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
 
G_M000_IG02:                ;; offset=0x0000
       mov      rax, 0x7FA930200B90
       mov      byte  ptr [rax], 0
       mov      rax, 0x7FA930200BA8
       mov      dword ptr [rax], 0x3F800000
       mov      rax, 0x7FA930200BC0
       mov      dword ptr [rax], 0x40400000
       mov      rax, 0x7FA930200BD8
       mov      dword ptr [rax], 0x40A00000
 
G_M000_IG03:                ;; offset=0x003D
       ret      
 
; Total bytes of code 62

; Assembly listing for method SumTimeline:SeekCore(long,uint,byte,int,byref,byref,byref):bool (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 116 single block inlinees; 20 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x20], ymm8
 
G_M000_IG02:                ;; offset=0x0014
       movsxd   rax, ecx
       movzx    rdx, dl
       and      edx, 3
       cmp      edx, 1
       jne      G_M000_IG99
 
G_M000_IG03:                ;; offset=0x0026
       test     rax, rax
       jle      SHORT G_M000_IG04
       mov      rdx, rax
       neg      rdx
       mov      r10, 0x7FFFFFFFFFFFFFFF
       add      rdx, r10
       cmp      rdx, rdi
       jl       G_M000_IG99
 
G_M000_IG04:                ;; offset=0x0047
       test     rax, rax
       jge      SHORT G_M000_IG05
       mov      rdx, rax
       neg      rdx
       mov      r10, 0x8000000000000000
       add      rdx, r10
       cmp      rdx, rdi
       jg       G_M000_IG99
 
G_M000_IG05:                ;; offset=0x0068
       add      rax, rdi
       mov      qword ptr [r9], rax
       add      esi, ecx
       mov      rdx, bword ptr [rbp+0x10]
       mov      dword ptr [rdx], esi
       test     ecx, ecx
       je       G_M000_IG50
       mov      rax, rdi
       sar      rax, 63
       and      rax, 63
       add      rax, rdi
       sar      rax, 6
       shl      rax, 6
       mov      rdx, rdi
       sub      rdx, rax
       jns      SHORT G_M000_IG06
       add      rdx, 64
 
G_M000_IG06:                ;; offset=0x00A0
       mov      eax, edx
       cmp      ecx, 1
       je       G_M000_IG91
       cmp      ecx, -1
       je       G_M000_IG84
       cmp      ecx, 1
       jg       G_M000_IG52
       cmp      rdi, qword ptr [r9]
       jle      G_M000_IG50
       align    [0 bytes for IG07]
 
G_M000_IG07:                ;; offset=0x00C6
       test     eax, eax
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x00CA
       dec      eax
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00CE
       mov      eax, 63
 
G_M000_IG10:                ;; offset=0x00D3
       dec      rdi
       mov      ecx, 192
       test     eax, eax
       jne      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00DF
       mov      ecx, 196
 
G_M000_IG12:                ;; offset=0x00E4
       cmp      eax, 63
       jne      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00E9
       or       ecx, 8
       movzx    rcx, cl
 
G_M000_IG14:                ;; offset=0x00EF
       xor      edx, edx
       mov      dword ptr [rbp-0x20], edx
       cmp      eax, 16
       jb       G_M000_IG41
       cmp      eax, 32
       jb       G_M000_IG31
       cmp      eax, 32
       jb       SHORT G_M000_IG15
       cmp      eax, 48
       jb       SHORT G_M000_IG23
 
G_M000_IG15:                ;; offset=0x0110
       cmp      eax, 48
       jb       G_M000_IG49
       cmp      eax, 64
       jae      G_M000_IG49
       mov      edx, ecx
       cmp      eax, 48
       jne      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x0129
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG17:                ;; offset=0x0131
       cmp      eax, 63
       jne      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0136
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG19:                ;; offset=0x013C
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x0148
       mov      edx, -1
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x014F
       mov      edx, 1
 
G_M000_IG22:                ;; offset=0x0154
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
       jmp      G_M000_IG49
 
G_M000_IG23:                ;; offset=0x0171
       mov      edx, ecx
       cmp      eax, 16
       jne      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0178
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG25:                ;; offset=0x0180
       cmp      eax, 47
       jne      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x0185
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG27:                ;; offset=0x018B
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x0197
       mov      edx, -1
       jmp      SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x019E
       mov      edx, 1
 
G_M000_IG30:                ;; offset=0x01A3
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
       jmp      G_M000_IG49
 
G_M000_IG31:                ;; offset=0x01C0
       mov      edx, ecx
       test     eax, eax
       je       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x01C6
       cmp      eax, 16
       jne      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x01CB
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG34:                ;; offset=0x01D3
       cmp      eax, 31
       je       SHORT G_M000_IG36
 
G_M000_IG35:                ;; offset=0x01D8
       cmp      eax, 47
       jne      SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x01DD
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG37:                ;; offset=0x01E3
       lea      ecx, [rax-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x20], xmm0
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       SHORT G_M000_IG39
 
G_M000_IG38:                ;; offset=0x0218
       mov      edx, -1
       jmp      SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x021F
       mov      edx, 1
 
G_M000_IG40:                ;; offset=0x0224
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x20]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
       jmp      SHORT G_M000_IG49
 
G_M000_IG41:                ;; offset=0x023B
       mov      edx, ecx
       test     eax, eax
       jne      SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x0241
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG43:                ;; offset=0x0249
       cmp      eax, 31
       jne      SHORT G_M000_IG45
 
G_M000_IG44:                ;; offset=0x024E
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG45:                ;; offset=0x0254
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0260
       mov      edx, -1
       jmp      SHORT G_M000_IG48
 
G_M000_IG47:                ;; offset=0x0267
       mov      edx, 1
 
G_M000_IG48:                ;; offset=0x026C
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
 
G_M000_IG49:                ;; offset=0x027C
       cmp      rdi, qword ptr [r9]
       jg       G_M000_IG07
 
G_M000_IG50:                ;; offset=0x0285
       mov      eax, 1
 
G_M000_IG51:                ;; offset=0x028A
       add      rsp, 32
       pop      rbp
       ret      
 
G_M000_IG52:                ;; offset=0x0290
       cmp      rdi, qword ptr [r9]
       jl       G_M000_IG75
       jmp      SHORT G_M000_IG50
 
G_M000_IG53:                ;; offset=0x029B
       mov      edx, 1
 
G_M000_IG54:                ;; offset=0x02A0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
       jmp      G_M000_IG72
 
G_M000_IG55:                ;; offset=0x02BD
       mov      edx, ecx
       cmp      eax, 16
       jne      SHORT G_M000_IG56
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG56:                ;; offset=0x02CC
       cmp      eax, 47
       jne      SHORT G_M000_IG57
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG57:                ;; offset=0x02D7
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       SHORT G_M000_IG58
       mov      edx, -1
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:                ;; offset=0x02EA
       mov      edx, 1
 
G_M000_IG59:                ;; offset=0x02EF
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
       jmp      G_M000_IG72
 
G_M000_IG60:                ;; offset=0x030C
       mov      edx, ecx
       test     eax, eax
       je       SHORT G_M000_IG61
       cmp      eax, 16
       jne      SHORT G_M000_IG62
 
G_M000_IG61:                ;; offset=0x0317
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG62:                ;; offset=0x031F
       cmp      eax, 31
       je       SHORT G_M000_IG63
       cmp      eax, 47
       jne      SHORT G_M000_IG64
 
G_M000_IG63:                ;; offset=0x0329
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG64:                ;; offset=0x032F
       lea      ecx, [rax-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x18], xmm0
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       SHORT G_M000_IG65
       mov      edx, -1
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x036B
       mov      edx, 1
 
G_M000_IG66:                ;; offset=0x0370
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x18]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
       jmp      SHORT G_M000_IG72
 
G_M000_IG67:                ;; offset=0x0387
       mov      edx, ecx
       test     eax, eax
       jne      SHORT G_M000_IG68
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG68:                ;; offset=0x0395
       cmp      eax, 31
       jne      SHORT G_M000_IG69
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG69:                ;; offset=0x03A0
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       SHORT G_M000_IG70
       mov      edx, -1
       jmp      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x03B3
       mov      edx, 1
 
G_M000_IG71:                ;; offset=0x03B8
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
 
G_M000_IG72:                ;; offset=0x03C8
       inc      rdi
       cmp      eax, 63
       je       SHORT G_M000_IG73
       inc      eax
       jmp      SHORT G_M000_IG74
 
G_M000_IG73:                ;; offset=0x03D4
       xor      eax, eax
 
G_M000_IG74:                ;; offset=0x03D6
       cmp      rdi, qword ptr [r9]
       jge      G_M000_IG50
 
G_M000_IG75:                ;; offset=0x03DF
       mov      ecx, 64
       test     eax, eax
       jne      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x03E8
       mov      ecx, 68
 
G_M000_IG77:                ;; offset=0x03ED
       cmp      eax, 63
       jne      SHORT G_M000_IG79
 
G_M000_IG78:                ;; offset=0x03F2
       or       ecx, 8
       movzx    rcx, cl
 
G_M000_IG79:                ;; offset=0x03F8
       xor      edx, edx
       mov      dword ptr [rbp-0x18], edx
       cmp      eax, 16
       jb       SHORT G_M000_IG67
 
G_M000_IG80:                ;; offset=0x0402
       cmp      eax, 32
       jb       G_M000_IG60
       cmp      eax, 32
       jb       SHORT G_M000_IG81
       cmp      eax, 48
       jb       G_M000_IG55
 
G_M000_IG81:                ;; offset=0x0419
       cmp      eax, 48
       jb       SHORT G_M000_IG72
       cmp      eax, 64
       jae      SHORT G_M000_IG72
       mov      edx, ecx
       cmp      eax, 48
       jne      SHORT G_M000_IG82
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG82:                ;; offset=0x0432
       cmp      eax, 63
       jne      SHORT G_M000_IG83
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG83:                ;; offset=0x043D
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       test     dl, 128
       je       G_M000_IG53
       mov      edx, -1
       jmp      G_M000_IG54
 
G_M000_IG84:                ;; offset=0x0457
       lea      r9d, [rax-0x01]
       mov      ecx, 63
       test     eax, eax
       mov      eax, ecx
       cmovne   eax, r9d
       mov      ecx, 192
       mov      edx, 196
       test     eax, eax
       cmove    ecx, edx
       mov      edx, ecx
       or       edx, 8
       movzx    rdx, dl
       cmp      eax, 63
       cmove    ecx, edx
       cmp      eax, 16
       jb       G_M000_IG90
       cmp      eax, 32
       jb       G_M000_IG87
       cmp      eax, 32
       jb       SHORT G_M000_IG85
       cmp      eax, 48
       jb       SHORT G_M000_IG86
 
G_M000_IG85:                ;; offset=0x04A1
       cmp      eax, 48
       jb       G_M000_IG50
       cmp      eax, 64
       jae      G_M000_IG50
       mov      edx, ecx
       or       ecx, 1
       movzx    rcx, cl
       cmp      eax, 48
       cmove    edx, ecx
       mov      ecx, edx
       or       ecx, 2
       movzx    rcx, cl
       cmp      eax, 63
       cmove    edx, ecx
       mov      rax, bword ptr [r8]
       vmovss   xmm0, dword ptr [rax]
       mov      r8d, -1
       mov      ecx, 1
       test     dl, 128
       cmove    r8d, ecx
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG50
 
G_M000_IG86:                ;; offset=0x0506
       mov      edx, ecx
       or       ecx, 1
       movzx    rcx, cl
       cmp      eax, 16
       cmove    edx, ecx
       mov      ecx, edx
       or       ecx, 2
       movzx    rcx, cl
       cmp      eax, 47
       cmove    edx, ecx
       mov      rax, bword ptr [r8]
       vmovss   xmm0, dword ptr [rax]
       mov      r8d, -1
       mov      ecx, 1
       test     dl, 128
       cmove    r8d, ecx
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG50
 
G_M000_IG87:                ;; offset=0x0559
       mov      edx, ecx
       test     eax, eax
       je       SHORT G_M000_IG88
       cmp      eax, 16
       jne      SHORT G_M000_IG89
 
G_M000_IG88:                ;; offset=0x0564
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG89:                ;; offset=0x056C
       mov      ecx, edx
       or       ecx, 2
       movzx    rcx, cl
       cmp      eax, 31
       cmove    edx, ecx
       add      eax, -16
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x10], xmm0
       mov      r8, bword ptr [r8]
       vmovss   xmm0, dword ptr [r8]
       mov      eax, -1
       mov      ecx, 1
       test     dl, 128
       cmove    eax, ecx
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, eax
       vmulss   xmm1, xmm1, dword ptr [rbp-0x10]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      G_M000_IG50
 
G_M000_IG90:                ;; offset=0x05D6
       mov      edx, ecx
       or       ecx, 1
       movzx    rcx, cl
       test     eax, eax
       cmove    edx, ecx
       mov      rax, bword ptr [r8]
       vmovss   xmm0, dword ptr [rax]
       mov      r8d, -1
       mov      ecx, 1
       test     dl, 128
       cmove    r8d, ecx
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG50
 
G_M000_IG91:                ;; offset=0x0612
       mov      ecx, 64
       mov      edx, 68
       test     eax, eax
       cmove    ecx, edx
       mov      edx, ecx
       or       edx, 8
       movzx    rdx, dl
       cmp      eax, 63
       cmove    ecx, edx
       cmp      eax, 16
       jb       G_M000_IG98
       cmp      eax, 32
       jb       G_M000_IG94
       cmp      eax, 32
       jb       SHORT G_M000_IG92
       cmp      eax, 48
       jb       SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x064B
       cmp      eax, 48
       jb       G_M000_IG50
       cmp      eax, 64
       jae      G_M000_IG50
       mov      edx, ecx
       or       ecx, 1
       movzx    rcx, cl
       cmp      eax, 48
       cmove    edx, ecx
       mov      ecx, edx
       or       ecx, 2
       movzx    rcx, cl
       cmp      eax, 63
       cmove    edx, ecx
       mov      rax, bword ptr [r8]
       vmovss   xmm0, dword ptr [rax]
       mov      r8d, -1
       mov      ecx, 1
       test     dl, 128
       cmove    r8d, ecx
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG50
 
G_M000_IG93:                ;; offset=0x06B0
       mov      edx, ecx
       or       ecx, 1
       movzx    rcx, cl
       cmp      eax, 16
       cmove    edx, ecx
       mov      ecx, edx
       or       ecx, 2
       movzx    rcx, cl
       cmp      eax, 47
       cmove    edx, ecx
       mov      rax, bword ptr [r8]
       vmovss   xmm0, dword ptr [rax]
       mov      r8d, -1
       mov      ecx, 1
       test     dl, 128
       cmove    r8d, ecx
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG50
 
G_M000_IG94:                ;; offset=0x0703
       mov      edx, ecx
       test     eax, eax
       je       SHORT G_M000_IG95
       cmp      eax, 16
       jne      SHORT G_M000_IG96
 
G_M000_IG95:                ;; offset=0x070E
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
 
G_M000_IG96:                ;; offset=0x0716
       cmp      eax, 31
       jne      SHORT G_M000_IG97
       or       edx, 2
       movzx    rdx, dl
 
G_M000_IG97:                ;; offset=0x0721
       lea      ecx, [rax-0x10]
       mov      eax, ecx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x08], xmm0
       mov      eax, edx
       mov      r8, bword ptr [r8]
       vmovss   xmm0, dword ptr [r8]
       mov      ecx, -1
       mov      edx, 1
       test     al, 128
       cmove    ecx, edx
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, ecx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r8], xmm0
       jmp      G_M000_IG50
 
G_M000_IG98:                ;; offset=0x0780
       mov      edx, ecx
       or       edx, 1
       movzx    rdx, dl
       test     eax, eax
       cmove    ecx, edx
       mov      eax, ecx
       mov      rcx, bword ptr [r8]
       vmovss   xmm0, dword ptr [rcx]
       mov      edx, -1
       mov      edi, 1
       test     al, 128
       cmove    edx, edi
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rcx], xmm0
       jmp      G_M000_IG50
 
G_M000_IG99:                ;; offset=0x07BA
       mov      qword ptr [r9], rdi
       mov      rdx, bword ptr [rbp+0x10]
       mov      dword ptr [rdx], esi
       xor      eax, eax
 
G_M000_IG100:                ;; offset=0x07C5
       add      rsp, 32
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 1995


; Assembly listing for method SignedSeekBenchmarks:RunDirect[LiteralPositiveOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x001C
       xor      r15d, r15d
       xor      r14d, r14d
       vxorps   xmm0, xmm0, xmm0
       vmovss   dword ptr [rbp-0x2C], xmm0
       mov      r13d, 0x1000
 
G_M000_IG03:                ;; offset=0x0031
       mov      r12d, 1
       jmp      G_M000_IG19
 
G_M000_IG04:                ;; offset=0x003C
       test     byte  ptr [(reloc 0x7f80446f6678)], 1
       je       G_M000_IG35
 
G_M000_IG05:                ;; offset=0x0049
       mov      rcx, 0x7F7083800C88
       vmovss   xmm1, dword ptr [rcx]
       vmovss   dword ptr [rbp-0x48], xmm1
       jmp      SHORT G_M000_IG10
 
G_M000_IG06:                ;; offset=0x005E
       lea      ecx, [r12-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rcx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rbp-0x4C], xmm1
       test     byte  ptr [(reloc 0x7f80446f6678)], 1
       je       G_M000_IG34
 
G_M000_IG07:                ;; offset=0x0086
       mov      rcx, 0x7F7083800C70
       vmovss   xmm2, dword ptr [rcx]
       mov      rcx, 0x7F7083800C88
       vmovss   xmm3, dword ptr [rcx]
       vsubss   xmm3, xmm3, xmm2
       vmulss   xmm1, xmm3, dword ptr [rbp-0x4C]
       vaddss   xmm1, xmm2, xmm1
       vmovss   dword ptr [rbp-0x48], xmm1
       jmp      SHORT G_M000_IG10
 
G_M000_IG08:                ;; offset=0x00B6
       test     byte  ptr [(reloc 0x7f80446f6678)], 1
       je       G_M000_IG33
 
G_M000_IG09:                ;; offset=0x00C3
       mov      rcx, 0x7F7083800C70
       vmovss   xmm2, dword ptr [rcx]
       vmovss   dword ptr [rbp-0x48], xmm2
 
G_M000_IG10:                ;; offset=0x00D6
       cmp      r12d, 48
       ja       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x00DC
       mov      rcx, 0x1000000010001
       bt       rcx, r12
       jae      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00EC
       or       edi, 1
       movzx    rdi, dil
 
G_M000_IG13:                ;; offset=0x00F3
       cmp      r12d, 63
       ja       SHORT G_M000_IG16
 
G_M000_IG14:                ;; offset=0x00F9
       mov      rcx, 0x7FFF7FFF7FFFFFFF
       bt       rcx, r12
       jb       SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0109
       or       edi, 2
       movzx    rdi, dil
       mov      r12d, edi
       mov      edi, r12d
 
G_M000_IG16:                ;; offset=0x0116
       test     dil, 128
       je       G_M000_IG29
 
G_M000_IG17:                ;; offset=0x0120
       mov      ecx, -1
 
G_M000_IG18:                ;; offset=0x0125
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, ecx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x48]
       vaddss   xmm0, xmm1, dword ptr [rbp-0x2C]
       inc      r15
       inc      r14d
       vmovss   dword ptr [rbp-0x2C], xmm0
       mov      r12, qword ptr [rbp-0x38]
 
G_M000_IG19:                ;; offset=0x0146
       lea      rdi, [r12-0x01]
       mov      qword ptr [rbp-0x38], rdi
       test     r12, r12
       je       G_M000_IG30
 
G_M000_IG20:                ;; offset=0x0158
       xor      edi, edi
       mov      dword ptr [rbp-0x48], edi
       mov      rdi, r15
       sar      rdi, 63
       and      rdi, 63
       add      rdi, r15
       sar      rdi, 6
       shl      rdi, 6
       mov      rcx, r15
       sub      rcx, rdi
       jns      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x017B
       add      rcx, 64
 
G_M000_IG22:                ;; offset=0x017F
       mov      r12d, ecx
       mov      edi, 64
       test     r12d, r12d
       jne      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x018C
       mov      edi, 68
 
G_M000_IG24:                ;; offset=0x0191
       cmp      r12d, 63
       jne      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x0197
       or       edi, 8
       movzx    rdi, dil
       mov      dword ptr [rbp-0x3C], edi
       mov      edi, dword ptr [rbp-0x3C]
 
G_M000_IG26:                ;; offset=0x01A4
       cmp      r12d, 16
       jb       G_M000_IG08
 
G_M000_IG27:                ;; offset=0x01AE
       cmp      r12d, 32
       jb       G_M000_IG06
       cmp      r12d, 48
       jb       G_M000_IG04
       test     byte  ptr [(reloc 0x7f80446f6678)], 1
       je       G_M000_IG36
 
G_M000_IG28:                ;; offset=0x01CF
       mov      rcx, 0x7F7083800CA0
       vmovss   xmm1, dword ptr [rcx]
       vmovss   dword ptr [rbp-0x48], xmm1
       jmp      G_M000_IG10
 
G_M000_IG29:                ;; offset=0x01E7
       mov      ecx, 1
       jmp      G_M000_IG18
 
G_M000_IG30:                ;; offset=0x01F1
       dec      r13d
       jne      G_M000_IG03
 
G_M000_IG31:                ;; offset=0x01FA
       mov      qword ptr [rbx], r15
       mov      dword ptr [rbx+0x08], r14d
       mov      byte  ptr [rbx+0x0C], 1
       mov      eax, dword ptr [rbp-0x2C]
       mov      dword ptr [rbx+0x10], eax
       mov      dword ptr [rbx+0x14], 0x1000
       mov      rax, rbx
 
G_M000_IG32:                ;; offset=0x0215
       add      rsp, 40
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG33:                ;; offset=0x0224
       mov      dword ptr [rbp-0x3C], edi
       mov      rdi, 0x7F80446F6610
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edi, dword ptr [rbp-0x3C]
       jmp      G_M000_IG09
 
G_M000_IG34:                ;; offset=0x023E
       mov      dword ptr [rbp-0x3C], edi
       mov      rdi, 0x7F80446F6610
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edi, dword ptr [rbp-0x3C]
       jmp      G_M000_IG07
 
G_M000_IG35:                ;; offset=0x0258
       mov      dword ptr [rbp-0x3C], edi
       mov      rdi, 0x7F80446F6610
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edi, dword ptr [rbp-0x3C]
       jmp      G_M000_IG05
 
G_M000_IG36:                ;; offset=0x0272
       mov      dword ptr [rbp-0x3C], edi
       mov      rdi, 0x7F80446F6610
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edi, dword ptr [rbp-0x3C]
       jmp      G_M000_IG28
 
RWD00  	dd	41700000h		;        15

; Total bytes of code 652

; Assembly listing for method SignedSeekBenchmarks:RunTyped[LiteralPositiveOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 39 single block inlinees; 7 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x70]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x0025
       xor      edi, edi
       mov      dword ptr [rbp-0x2C], edi
 
G_M000_IG03:                ;; offset=0x002A
       mov      qword ptr [rbp-0x40], rdi
 
G_M000_IG04:                ;; offset=0x002E
       mov      dword ptr [rbp-0x38], edi
       mov      byte  ptr [rbp-0x34], 1
 
G_M000_IG05:                ;; offset=0x0035
       xor      r15d, r15d
       mov      r14d, 0x1000
 
G_M000_IG06:                ;; offset=0x003E
       mov      rdi, qword ptr [rbp-0x40]
       mov      r13d, dword ptr [rbp-0x38]
       movzx    r12, byte  ptr [rbp-0x34]
       mov      eax, r12d
       and      eax, 3
       cmp      eax, 1
       jne      SHORT G_M000_IG07
       mov      rax, 0x7FFFFFFFFFFFFFFE
       cmp      rdi, rax
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0065
       xor      eax, eax
       jmp      G_M000_IG37
 
G_M000_IG08:                ;; offset=0x006C
       lea      rax, [rdi+0x01]
       mov      qword ptr [rbp-0x48], rax
       inc      r13d
       mov      rcx, rdi
       sar      rcx, 63
       and      rcx, 63
       add      rcx, rdi
       sar      rcx, 6
       shl      rcx, 6
       sub      rdi, rcx
       jns      SHORT G_M000_IG09
       add      rdi, 64
 
G_M000_IG09:                ;; offset=0x0096
       mov      ecx, edi
       mov      edi, 64
       test     ecx, ecx
       jne      SHORT G_M000_IG10
       mov      edi, 68
 
G_M000_IG10:                ;; offset=0x00A6
       cmp      ecx, 63
       jne      SHORT G_M000_IG11
       or       edi, 8
       movzx    rdi, dil
 
G_M000_IG11:                ;; offset=0x00B2
       xor      edx, edx
       mov      dword ptr [rbp-0x60], edx
       cmp      ecx, 16
       jb       G_M000_IG31
       cmp      ecx, 32
       jb       G_M000_IG24
       cmp      ecx, 32
       jb       SHORT G_M000_IG12
       cmp      ecx, 48
       jb       G_M000_IG18
 
G_M000_IG12:                ;; offset=0x00D7
       cmp      ecx, 48
       jb       G_M000_IG36
       cmp      ecx, 64
       jae      G_M000_IG36
       mov      edx, edi
       cmp      ecx, 48
       jne      SHORT G_M000_IG13
       mov      edx, edi
       or       edx, 1
       movzx    rdx, dl
       mov      edi, edx
       mov      edx, edi
 
G_M000_IG13:                ;; offset=0x00FC
       cmp      ecx, 63
       jne      SHORT G_M000_IG14
       or       edx, 2
       movzx    rdx, dl
       mov      dword ptr [rbp-0x4C], edx
       mov      edx, dword ptr [rbp-0x4C]
 
G_M000_IG14:                ;; offset=0x010D
       test     byte  ptr [(reloc 0x7f804442f988)], 1
       je       G_M000_IG43
 
G_M000_IG15:                ;; offset=0x011A
       vmovss   xmm0, dword ptr [rbp-0x2C]
       test     dl, 128
       je       SHORT G_M000_IG16
       mov      edi, -1
       jmp      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x012B
       mov      edi, 1
 
G_M000_IG17:                ;; offset=0x0130
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edi
       mov      rdi, 0x7F7083800BD8
       vmulss   xmm1, xmm1, dword ptr [rdi]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x2C], xmm0
       jmp      G_M000_IG36
 
G_M000_IG18:                ;; offset=0x0154
       mov      edx, edi
       cmp      ecx, 16
       jne      SHORT G_M000_IG19
       mov      edx, edi
       or       edx, 1
       movzx    rdx, dl
       mov      edi, edx
       mov      edx, edi
 
G_M000_IG19:                ;; offset=0x0167
       cmp      ecx, 47
       jne      SHORT G_M000_IG20
       or       edx, 2
       movzx    rdx, dl
       mov      dword ptr [rbp-0x50], edx
       mov      edx, dword ptr [rbp-0x50]
 
G_M000_IG20:                ;; offset=0x0178
       test     byte  ptr [(reloc 0x7f804442f988)], 1
       je       G_M000_IG42
 
G_M000_IG21:                ;; offset=0x0185
       vmovss   xmm0, dword ptr [rbp-0x2C]
       test     dl, 128
       je       SHORT G_M000_IG22
       mov      edi, -1
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x0196
       mov      edi, 1
 
G_M000_IG23:                ;; offset=0x019B
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edi
       mov      rdi, 0x7F7083800BC0
       vmulss   xmm1, xmm1, dword ptr [rdi]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x2C], xmm0
       jmp      G_M000_IG36
 
G_M000_IG24:                ;; offset=0x01BF
       mov      edx, edi
       test     ecx, ecx
       je       SHORT G_M000_IG25
       cmp      ecx, 16
       jne      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x01CA
       mov      edx, edi
       or       edx, 1
       movzx    rdx, dl
       mov      edi, edx
       mov      edx, edi
 
G_M000_IG26:                ;; offset=0x01D6
       cmp      ecx, 31
       jne      SHORT G_M000_IG27
       or       edx, 2
       movzx    rdx, dl
       mov      dword ptr [rbp-0x54], edx
       mov      edx, dword ptr [rbp-0x54]
 
G_M000_IG27:                ;; offset=0x01E7
       add      ecx, -16
       mov      edi, ecx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rbp-0x58], xmm0
       test     byte  ptr [(reloc 0x7f804442f988)], 1
       je       G_M000_IG41
 
G_M000_IG28:                ;; offset=0x020F
       mov      rdi, 0x7F7083800BA8
       vmovss   xmm1, dword ptr [rdi]
       mov      rdi, 0x7F7083800BC0
       vmovss   xmm2, dword ptr [rdi]
       vsubss   xmm2, xmm2, xmm1
       vmulss   xmm0, xmm2, dword ptr [rbp-0x58]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x60], xmm0
       vmovss   xmm0, dword ptr [rbp-0x2C]
       test     dl, 128
       je       SHORT G_M000_IG29
       mov      edi, -1
       jmp      SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x024E
       mov      edi, 1
 
G_M000_IG30:                ;; offset=0x0253
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x60]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x2C], xmm0
       jmp      SHORT G_M000_IG36
 
G_M000_IG31:                ;; offset=0x026B
       mov      edx, edi
       test     ecx, ecx
       jne      SHORT G_M000_IG32
       mov      edx, edi
       or       edx, 1
       movzx    rdx, dl
       mov      dword ptr [rbp-0x64], edx
       mov      edx, dword ptr [rbp-0x64]
 
G_M000_IG32:                ;; offset=0x027F
       test     byte  ptr [(reloc 0x7f804442f988)], 1
       je       G_M000_IG40
 
G_M000_IG33:                ;; offset=0x028C
       vmovss   xmm0, dword ptr [rbp-0x2C]
       test     dl, 128
       je       SHORT G_M000_IG34
       mov      ecx, -1
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x029D
       mov      ecx, 1
 
G_M000_IG35:                ;; offset=0x02A2
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, ecx
       mov      rcx, 0x7F7083800BA8
       vmovss   xmm2, dword ptr [rcx]
       vmulss   xmm1, xmm1, xmm2
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x2C], xmm0
 
G_M000_IG36:                ;; offset=0x02C5
       mov      rax, qword ptr [rbp-0x48]
       mov      qword ptr [rbp-0x40], rax
       mov      dword ptr [rbp-0x38], r13d
       mov      byte  ptr [rbp-0x34], r12b
       mov      eax, 1
 
G_M000_IG37:                ;; offset=0x02DA
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r15d, eax
       dec      r14d
       jne      G_M000_IG06
 
G_M000_IG38:                ;; offset=0x02EE
       mov      rax, qword ptr [rbp-0x40]
       mov      ecx, dword ptr [rbp-0x38]
       movzx    rdx, byte  ptr [rbp-0x34]
       vmovss   xmm0, dword ptr [rbp-0x2C]
       mov      qword ptr [rbx], rax
       mov      dword ptr [rbx+0x08], ecx
       mov      byte  ptr [rbx+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [rbx+0x10], eax
       mov      dword ptr [rbx+0x14], r15d
       mov      rax, rbx
 
G_M000_IG39:                ;; offset=0x0315
       add      rsp, 72
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG40:                ;; offset=0x0324
       mov      dword ptr [rbp-0x64], edx
       mov      rdi, 0x7F804442F910
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edx, dword ptr [rbp-0x64]
       jmp      G_M000_IG33
 
G_M000_IG41:                ;; offset=0x033E
       mov      dword ptr [rbp-0x54], edx
       mov      rdi, 0x7F804442F910
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edx, dword ptr [rbp-0x54]
       jmp      G_M000_IG28
 
G_M000_IG42:                ;; offset=0x0358
       mov      dword ptr [rbp-0x50], edx
       mov      rdi, 0x7F804442F910
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edx, dword ptr [rbp-0x50]
       jmp      G_M000_IG21
 
G_M000_IG43:                ;; offset=0x0372
       mov      dword ptr [rbp-0x4C], edx
       mov      rdi, 0x7F804442F910
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       mov      edx, dword ptr [rbp-0x4C]
       jmp      G_M000_IG15
 
RWD00  	dd	41700000h		;        15

; Total bytes of code 908

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[LiteralPositiveOne]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0083
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       mov      edx, 1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0098
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009A
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AA
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D4
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 225

; Assembly listing for method SignedSeekBenchmarks:RunDirect[LiteralNegativeOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, 1
       jmp      G_M000_IG16
       align    [0 bytes for IG04]
 
G_M000_IG04:                ;; offset=0x0027
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:                ;; offset=0x0030
       lea      r10d, [rsi-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x005C
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG07:                ;; offset=0x0063
       cmp      esi, 48
       ja       SHORT G_M000_IG10
 
G_M000_IG08:                ;; offset=0x0068
       mov      r10, 0x1000000010001
       bt       r10, rsi
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0078
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG10:                ;; offset=0x0080
       cmp      esi, 63
       ja       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x0085
       mov      r10, 0x7FFF7FFF7FFFFFFF
       bt       r10, rsi
       jb       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0095
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG13:                ;; offset=0x009D
       test     r9b, 128
       je       G_M000_IG26
 
G_M000_IG14:                ;; offset=0x00A7
       mov      esi, -1
 
G_M000_IG15:                ;; offset=0x00AC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       dec      ecx
       mov      rsi, r8
 
G_M000_IG16:                ;; offset=0x00C2
       lea      r8, [rsi-0x01]
       test     rsi, rsi
       je       SHORT G_M000_IG27
 
G_M000_IG17:                ;; offset=0x00CB
       xor      esi, esi
       mov      dword ptr [rbp-0x08], esi
       dec      rax
       mov      rsi, rax
       mov      r9, rsi
       sar      r9, 63
       and      r9, 63
       add      r9, rsi
       sar      r9, 6
       shl      r9, 6
       sub      rsi, r9
       jns      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x00F1
       add      rsi, 64
 
G_M000_IG19:                ;; offset=0x00F5
       mov      r9d, 192
       test     esi, esi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x00FF
       mov      r9d, 196
 
G_M000_IG21:                ;; offset=0x0105
       cmp      esi, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x010A
       or       r9d, 8
       movzx    r9, r9b
 
G_M000_IG23:                ;; offset=0x0112
       cmp      esi, 16
       jb       G_M000_IG06
 
G_M000_IG24:                ;; offset=0x011B
       cmp      esi, 32
       jb       G_M000_IG05
       cmp      esi, 48
       jb       G_M000_IG04
 
G_M000_IG25:                ;; offset=0x012D
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      G_M000_IG07
 
G_M000_IG26:                ;; offset=0x0139
       mov      esi, 1
       jmp      G_M000_IG15
 
G_M000_IG27:                ;; offset=0x0143
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG28:                ;; offset=0x014B
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG29:                ;; offset=0x0166
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 364

; Assembly listing for method SignedSeekBenchmarks:RunTyped[LiteralNegativeOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 39 single block inlinees; 7 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
 
G_M000_IG02:                ;; offset=0x001A
       xor      eax, eax
       mov      dword ptr [rbp-0x04], eax
 
G_M000_IG03:                ;; offset=0x001F
       mov      qword ptr [rbp-0x18], rax
 
G_M000_IG04:                ;; offset=0x0023
       mov      dword ptr [rbp-0x10], eax
       mov      byte  ptr [rbp-0x0C], 1
 
G_M000_IG05:                ;; offset=0x002A
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x002F
       mov      rdx, qword ptr [rbp-0x18]
       mov      esi, dword ptr [rbp-0x10]
       movzx    r8, byte  ptr [rbp-0x0C]
       mov      r9d, r8d
       and      r9d, 3
       cmp      r9d, 1
       jne      SHORT G_M000_IG07
       mov      r9, 0x8000000000000001
       cmp      rdx, r9
       jge      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0057
       xor      r9d, r9d
       jmp      G_M000_IG35
 
G_M000_IG08:                ;; offset=0x005F
       lea      r9, [rdx-0x01]
       dec      esi
       mov      r10, rdx
       sar      r10, 63
       and      r10, 63
       add      r10, rdx
       sar      r10, 6
       shl      r10, 6
       sub      rdx, r10
       jns      SHORT G_M000_IG09
       add      rdx, 64
 
G_M000_IG09:                ;; offset=0x0084
       test     edx, edx
       je       SHORT G_M000_IG10
       dec      edx
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:                ;; offset=0x008C
       mov      edx, 63
 
G_M000_IG11:                ;; offset=0x0091
       mov      r10d, 192
       test     edx, edx
       jne      SHORT G_M000_IG12
       mov      r10d, 196
 
G_M000_IG12:                ;; offset=0x00A1
       cmp      edx, 63
       jne      SHORT G_M000_IG13
       or       r10d, 8
       movzx    r10, r10b
 
G_M000_IG13:                ;; offset=0x00AE
       xor      r11d, r11d
       mov      dword ptr [rbp-0x20], r11d
       cmp      edx, 16
       jb       G_M000_IG30
       cmp      edx, 32
       jb       G_M000_IG24
       cmp      edx, 32
       jb       SHORT G_M000_IG14
       cmp      edx, 48
       jb       SHORT G_M000_IG19
 
G_M000_IG14:                ;; offset=0x00D1
       cmp      edx, 48
       jb       G_M000_IG34
       cmp      edx, 64
       jae      G_M000_IG34
       mov      r11d, r10d
       cmp      edx, 48
       jne      SHORT G_M000_IG15
       mov      r11d, r10d
       or       r11d, 1
       movzx    r11, r11b
 
G_M000_IG15:                ;; offset=0x00F6
       cmp      edx, 63
       jne      SHORT G_M000_IG16
       or       r11d, 2
       movzx    r11, r11b
 
G_M000_IG16:                ;; offset=0x0103
       vmovss   xmm0, dword ptr [rbp-0x04]
       test     r11b, 128
       je       SHORT G_M000_IG17
       mov      r10d, -1
       jmp      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0116
       mov      r10d, 1
 
G_M000_IG18:                ;; offset=0x011C
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x04], xmm0
       jmp      G_M000_IG34
 
G_M000_IG19:                ;; offset=0x013B
       mov      r11d, r10d
       cmp      edx, 16
       jne      SHORT G_M000_IG20
       mov      r11d, r10d
       or       r11d, 1
       movzx    r11, r11b
 
G_M000_IG20:                ;; offset=0x014E
       cmp      edx, 47
       jne      SHORT G_M000_IG21
       or       r11d, 2
       movzx    r11, r11b
 
G_M000_IG21:                ;; offset=0x015B
       vmovss   xmm0, dword ptr [rbp-0x04]
       test     r11b, 128
       je       SHORT G_M000_IG22
       mov      r10d, -1
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x016E
       mov      r10d, 1
 
G_M000_IG23:                ;; offset=0x0174
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x04], xmm0
       jmp      G_M000_IG34
 
G_M000_IG24:                ;; offset=0x0193
       mov      r11d, r10d
       test     edx, edx
       je       SHORT G_M000_IG25
       cmp      edx, 16
       jne      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x019F
       mov      r11d, r10d
       or       r11d, 1
       movzx    r11, r11b
 
G_M000_IG26:                ;; offset=0x01AA
       cmp      edx, 31
       jne      SHORT G_M000_IG27
       or       r11d, 2
       movzx    r11, r11b
 
G_M000_IG27:                ;; offset=0x01B7
       lea      r10d, [rdx-0x10]
       mov      edx, r10d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x20], xmm0
       vmovss   xmm0, dword ptr [rbp-0x04]
       test     r11b, 128
       je       SHORT G_M000_IG28
       mov      edx, -1
       jmp      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01F6
       mov      edx, 1
 
G_M000_IG29:                ;; offset=0x01FB
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x20]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x04], xmm0
       jmp      SHORT G_M000_IG34
 
G_M000_IG30:                ;; offset=0x0213
       mov      r11d, r10d
       test     edx, edx
       jne      SHORT G_M000_IG31
       mov      r11d, r10d
       or       r11d, 1
       movzx    r11, r11b
 
G_M000_IG31:                ;; offset=0x0225
       vmovss   xmm0, dword ptr [rbp-0x04]
       test     r11b, 128
       je       SHORT G_M000_IG32
       mov      edx, -1
       jmp      SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x0237
       mov      edx, 1
 
G_M000_IG33:                ;; offset=0x023C
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x04], xmm0
 
G_M000_IG34:                ;; offset=0x024D
       mov      qword ptr [rbp-0x18], r9
       mov      dword ptr [rbp-0x10], esi
       mov      byte  ptr [rbp-0x0C], r8b
       mov      r9d, 1
 
G_M000_IG35:                ;; offset=0x025E
       test     r9d, r9d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG36:                ;; offset=0x0271
       mov      rcx, qword ptr [rbp-0x18]
       mov      edx, dword ptr [rbp-0x10]
       movzx    rsi, byte  ptr [rbp-0x0C]
       vmovss   xmm0, dword ptr [rbp-0x04]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG37:                ;; offset=0x0299
       add      rsp, 32
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 671

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[LiteralNegativeOne]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0083
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       mov      edx, -1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0098
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009A
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AA
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D4
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 225

; Assembly listing for method SignedSeekBenchmarks:RunDirect[RuntimePositiveOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x28], rax
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x0018
       xor      r15d, r15d
       xor      r14d, r14d
       vxorps   xmm0, xmm0, xmm0
       vmovss   dword ptr [rbp-0x1C], xmm0
       test     byte  ptr [(reloc 0x7f8044888948)], 1
       je       G_M000_IG41
 
G_M000_IG03:                ;; offset=0x0034
       mov      eax, 0x1000
 
G_M000_IG04:                ;; offset=0x0039
       mov      ecx, dword ptr [(reloc 0x7f804326b140)]
       test     ecx, ecx
       jle      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0043
       mov      edx, 1
       jmp      SHORT G_M000_IG07
       align    [0 bytes for IG11]
 
G_M000_IG06:                ;; offset=0x004A
       mov      edx, -1
 
G_M000_IG07:                ;; offset=0x004F
       test     ecx, ecx
       jle      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0053
       mov      edi, ecx
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0057
       movsxd   rdi, ecx
       neg      rdi
 
G_M000_IG10:                ;; offset=0x005D
       jmp      G_M000_IG34
 
G_M000_IG11:                ;; offset=0x0062
       lea      rdi, [r15-0x01]
 
G_M000_IG12:                ;; offset=0x0066
       mov      rsi, rdi
       sar      rsi, 63
       and      rsi, 63
       add      rsi, rdi
       sar      rsi, 6
       shl      rsi, 6
       sub      rdi, rsi
       jns      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0081
       add      rdi, 64
 
G_M000_IG14:                ;; offset=0x0085
       mov      esi, 64
       test     edx, edx
       jge      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x008E
       mov      esi, 192
 
G_M000_IG16:                ;; offset=0x0093
       test     edi, edi
       jne      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0097
       or       esi, 4
       movzx    rsi, sil
 
G_M000_IG18:                ;; offset=0x009E
       cmp      edi, 63
       jne      SHORT G_M000_IG20
 
G_M000_IG19:                ;; offset=0x00A3
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG20:                ;; offset=0x00AA
       cmp      edi, 16
       jb       SHORT G_M000_IG24
 
G_M000_IG21:                ;; offset=0x00AF
       cmp      edi, 32
       jb       SHORT G_M000_IG23
       cmp      edi, 48
       jb       SHORT G_M000_IG22
       mov      dword ptr [rbp-0x28], 0x40A00000
       jmp      SHORT G_M000_IG25
 
G_M000_IG22:                ;; offset=0x00C2
       mov      dword ptr [rbp-0x28], 0x40400000
       jmp      SHORT G_M000_IG25
 
G_M000_IG23:                ;; offset=0x00CB
       lea      r8d, [rdi-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x28], xmm1
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x00F7
       mov      dword ptr [rbp-0x28], 0x3F800000
 
G_M000_IG25:                ;; offset=0x00FE
       cmp      edi, 48
       ja       SHORT G_M000_IG28
 
G_M000_IG26:                ;; offset=0x0103
       mov      r8, 0x1000000010001
       bt       r8, rdi
       jae      SHORT G_M000_IG28
 
G_M000_IG27:                ;; offset=0x0113
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG28:                ;; offset=0x011A
       cmp      edi, 63
       ja       SHORT G_M000_IG31
 
G_M000_IG29:                ;; offset=0x011F
       mov      r8, 0x7FFF7FFF7FFFFFFF
       bt       r8, rdi
       jb       SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x012F
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG31:                ;; offset=0x0136
       test     sil, 128
       je       SHORT G_M000_IG37
 
G_M000_IG32:                ;; offset=0x013C
       mov      edi, -1
 
G_M000_IG33:                ;; offset=0x0141
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, dword ptr [rbp-0x1C]
       movsxd   rdi, edx
       add      r15, rdi
       add      r14d, edx
       vmovss   dword ptr [rbp-0x1C], xmm0
       mov      rdi, rcx
 
G_M000_IG34:                ;; offset=0x0164
       lea      rcx, [rdi-0x01]
       test     rdi, rdi
       je       SHORT G_M000_IG38
 
G_M000_IG35:                ;; offset=0x016D
       xor      edi, edi
       mov      dword ptr [rbp-0x28], edi
       test     edx, edx
       jle      G_M000_IG11
 
G_M000_IG36:                ;; offset=0x017A
       mov      rdi, r15
       jmp      G_M000_IG12
 
G_M000_IG37:                ;; offset=0x0182
       mov      edi, 1
       jmp      SHORT G_M000_IG33
 
G_M000_IG38:                ;; offset=0x0189
       dec      eax
       jne      G_M000_IG04
 
G_M000_IG39:                ;; offset=0x0191
       mov      qword ptr [rbx], r15
       mov      dword ptr [rbx+0x08], r14d
       mov      byte  ptr [rbx+0x0C], 1
       mov      eax, dword ptr [rbp-0x1C]
       mov      dword ptr [rbx+0x10], eax
       mov      dword ptr [rbx+0x14], 0x1000
       mov      rax, rbx
 
G_M000_IG40:                ;; offset=0x01AC
       add      rsp, 24
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG41:                ;; offset=0x01B7
       mov      rdi, 0x7F80448888E0
       call     CORINFO_HELP_GET_NONGCSTATIC_BASE
       jmp      G_M000_IG03
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 459

; Assembly listing for method SignedSeekBenchmarks:RunTyped[RuntimePositiveOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 126 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
 
G_M000_IG02:                ;; offset=0x001A
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x001F
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0023
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002A
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x002F
       mov      edx, dword ptr [(reloc 0x7f804326b140)]
       mov      rsi, qword ptr [rbp-0x20]
       mov      r8d, dword ptr [rbp-0x18]
       movzx    r9, byte  ptr [rbp-0x14]
       movsxd   r10, edx
       mov      r11d, r9d
       and      r11d, 3
       cmp      r11d, 1
       jne      SHORT G_M000_IG08
       test     r10, r10
       jle      SHORT G_M000_IG07
       mov      r11, r10
       neg      r11
       mov      rbx, 0x7FFFFFFFFFFFFFFF
       add      r11, rbx
       cmp      r11, rsi
       jl       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x006F
       test     r10, r10
       jge      SHORT G_M000_IG09
       mov      r11, r10
       neg      r11
       mov      rbx, 0x8000000000000000
       add      r11, rbx
       cmp      r11, rsi
       jle      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x008C
       xor      r11d, r11d
       jmp      G_M000_IG56
       align    [0 bytes for IG11]
 
G_M000_IG09:                ;; offset=0x0094
       add      r10, rsi
       add      r8d, edx
       test     edx, edx
       je       G_M000_IG55
       mov      r11, rsi
       sar      r11, 63
       and      r11, 63
       add      r11, rsi
       sar      r11, 6
       shl      r11, 6
       mov      rbx, rsi
       sub      rbx, r11
       jns      SHORT G_M000_IG10
       add      rbx, 64
 
G_M000_IG10:                ;; offset=0x00C4
       mov      r11d, ebx
       cmp      edx, 1
       je       G_M000_IG116
       cmp      edx, -1
       je       G_M000_IG91
       cmp      edx, 1
       jg       G_M000_IG59
       cmp      rsi, r10
       jle      G_M000_IG54
 
G_M000_IG11:                ;; offset=0x00EB
       test     r11d, r11d
       je       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00F0
       dec      r11d
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00F5
       mov      r11d, 63
 
G_M000_IG14:                ;; offset=0x00FB
       dec      rsi
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0108
       mov      edx, 196
 
G_M000_IG16:                ;; offset=0x010D
       cmp      r11d, 63
       jne      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0113
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG18:                ;; offset=0x0119
       xor      ebx, ebx
       mov      dword ptr [rbp-0x40], ebx
       cmp      r11d, 16
       jb       G_M000_IG45
       cmp      r11d, 32
       jb       G_M000_IG35
       cmp      r11d, 32
       jb       SHORT G_M000_IG19
       cmp      r11d, 48
       jb       SHORT G_M000_IG27
 
G_M000_IG19:                ;; offset=0x013E
       cmp      r11d, 48
       jb       G_M000_IG53
       cmp      r11d, 64
       jae      G_M000_IG53
       mov      ebx, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x015A
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG21:                ;; offset=0x0160
       cmp      r11d, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x0166
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x016C
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0176
       mov      edx, -1
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x017D
       mov      edx, 1
 
G_M000_IG26:                ;; offset=0x0182
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG27:                ;; offset=0x01A0
       mov      ebx, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01A8
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG29:                ;; offset=0x01AE
       cmp      r11d, 47
       jne      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01B4
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG31:                ;; offset=0x01BA
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x01C4
       mov      edx, -1
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x01CB
       mov      edx, 1
 
G_M000_IG34:                ;; offset=0x01D0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG35:                ;; offset=0x01EE
       mov      ebx, edx
       test     r11d, r11d
       je       SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x01F5
       cmp      r11d, 16
       jne      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x01FB
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG38:                ;; offset=0x0201
       cmp      r11d, 31
       je       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x0207
       cmp      r11d, 47
       jne      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x020D
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG41:                ;; offset=0x0213
       lea      edx, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x40], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x0247
       mov      edx, -1
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:                ;; offset=0x024E
       mov      edx, 1
 
G_M000_IG44:                ;; offset=0x0253
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x40]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG45:                ;; offset=0x026B
       mov      ebx, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0272
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG47:                ;; offset=0x0278
       cmp      r11d, 31
       jne      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x027E
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG49:                ;; offset=0x0284
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG51
 
G_M000_IG50:                ;; offset=0x028E
       mov      edx, -1
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0295
       mov      edx, 1
 
G_M000_IG52:                ;; offset=0x029A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG53:                ;; offset=0x02AB
       cmp      rsi, r10
       jg       G_M000_IG11
 
G_M000_IG54:                ;; offset=0x02B4
       movzx    rdx, r9b
       mov      qword ptr [rbp-0x20], r10
       mov      dword ptr [rbp-0x18], r8d
       mov      byte  ptr [rbp-0x14], dl
 
G_M000_IG55:                ;; offset=0x02C3
       mov      r11d, 1
 
G_M000_IG56:                ;; offset=0x02C9
       test     r11d, r11d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG57:                ;; offset=0x02DC
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG58:                ;; offset=0x0304
       add      rsp, 56
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG59:                ;; offset=0x030B
       mov      rdx, rsi
       cmp      rdx, r10
       jl       G_M000_IG81
       jmp      SHORT G_M000_IG54
       align    [0 bytes for IG60]
 
G_M000_IG60:                ;; offset=0x0319
       mov      esi, 1
 
G_M000_IG61:                ;; offset=0x031E
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG62:                ;; offset=0x033C
       mov      ebx, esi
       cmp      r11d, 16
       jne      SHORT G_M000_IG63
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG63:                ;; offset=0x034B
       cmp      r11d, 47
       jne      SHORT G_M000_IG64
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG64:                ;; offset=0x0357
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG65
       mov      esi, -1
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x0368
       mov      esi, 1
 
G_M000_IG66:                ;; offset=0x036D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG67:                ;; offset=0x038B
       mov      ebx, esi
       test     r11d, r11d
       je       SHORT G_M000_IG68
       cmp      r11d, 16
       jne      SHORT G_M000_IG69
 
G_M000_IG68:                ;; offset=0x0398
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG69:                ;; offset=0x039F
       cmp      r11d, 31
       je       SHORT G_M000_IG70
       cmp      r11d, 47
       jne      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x03AB
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG71:                ;; offset=0x03B1
       lea      esi, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG72
       mov      esi, -1
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x03EC
       mov      esi, 1
 
G_M000_IG73:                ;; offset=0x03F1
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG74:                ;; offset=0x0409
       mov      ebx, esi
       test     r11d, r11d
       jne      SHORT G_M000_IG75
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG75:                ;; offset=0x0417
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG76
       mov      esi, -1
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x0428
       mov      esi, 1
 
G_M000_IG77:                ;; offset=0x042D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG78:                ;; offset=0x043E
       inc      rdx
       cmp      r11d, 63
       je       SHORT G_M000_IG79
       inc      r11d
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x044C
       xor      r11d, r11d
 
G_M000_IG80:                ;; offset=0x044F
       cmp      rdx, r10
       jge      G_M000_IG54
 
G_M000_IG81:                ;; offset=0x0458
       mov      esi, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG83
 
G_M000_IG82:                ;; offset=0x0462
       mov      esi, 68
 
G_M000_IG83:                ;; offset=0x0467
       cmp      r11d, 63
       jne      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x046D
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG85:                ;; offset=0x0474
       xor      ebx, ebx
       mov      dword ptr [rbp-0x38], ebx
       cmp      r11d, 16
       jb       SHORT G_M000_IG74
 
G_M000_IG86:                ;; offset=0x047F
       cmp      r11d, 32
       jb       G_M000_IG67
       cmp      r11d, 32
       jb       SHORT G_M000_IG87
       cmp      r11d, 48
       jb       G_M000_IG62
 
G_M000_IG87:                ;; offset=0x0499
       cmp      r11d, 48
       jb       SHORT G_M000_IG78
       cmp      r11d, 64
       jae      SHORT G_M000_IG78
       mov      ebx, esi
       cmp      r11d, 48
       jne      SHORT G_M000_IG88
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG88:                ;; offset=0x04B4
       cmp      r11d, 63
       jne      SHORT G_M000_IG89
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG89:                ;; offset=0x04C0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       G_M000_IG60
 
G_M000_IG90:                ;; offset=0x04CE
       mov      esi, -1
       jmp      G_M000_IG61
 
G_M000_IG91:                ;; offset=0x04D8
       test     r11d, r11d
       je       SHORT G_M000_IG92
       dec      r11d
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x04E2
       mov      r11d, 63
 
G_M000_IG93:                ;; offset=0x04E8
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG94
       mov      edx, 196
 
G_M000_IG94:                ;; offset=0x04F7
       cmp      r11d, 63
       jne      SHORT G_M000_IG95
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG95:                ;; offset=0x0503
       xor      esi, esi
       mov      dword ptr [rbp-0x30], esi
       cmp      r11d, 16
       jb       G_M000_IG112
       cmp      r11d, 32
       jb       G_M000_IG106
       cmp      r11d, 32
       jb       SHORT G_M000_IG96
       cmp      r11d, 48
       jb       SHORT G_M000_IG101
 
G_M000_IG96:                ;; offset=0x0528
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG97
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG97:                ;; offset=0x054D
       cmp      r11d, 63
       jne      SHORT G_M000_IG98
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG98:                ;; offset=0x055A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG99
       mov      edx, -1
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:                ;; offset=0x056C
       mov      edx, 1
 
G_M000_IG100:                ;; offset=0x0571
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG101:                ;; offset=0x058F
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG102
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG102:                ;; offset=0x05A0
       cmp      r11d, 47
       jne      SHORT G_M000_IG103
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG103:                ;; offset=0x05AD
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG104
       mov      edx, -1
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x05BF
       mov      edx, 1
 
G_M000_IG105:                ;; offset=0x05C4
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG106:                ;; offset=0x05E2
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG107
       cmp      r11d, 16
       jne      SHORT G_M000_IG108
 
G_M000_IG107:                ;; offset=0x05EF
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG108:                ;; offset=0x05F8
       cmp      r11d, 31
       jne      SHORT G_M000_IG109
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG109:                ;; offset=0x0605
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG110
       mov      edx, -1
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:                ;; offset=0x0644
       mov      edx, 1
 
G_M000_IG111:                ;; offset=0x0649
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x30]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG112:                ;; offset=0x0664
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG113
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG113:                ;; offset=0x0674
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG114
       mov      r11d, -1
       jmp      SHORT G_M000_IG115
 
G_M000_IG114:                ;; offset=0x0687
       mov      r11d, 1
 
G_M000_IG115:                ;; offset=0x068D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG116:                ;; offset=0x06A4
       mov      edx, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG117
       mov      edx, 68
 
G_M000_IG117:                ;; offset=0x06B3
       cmp      r11d, 63
       jne      SHORT G_M000_IG118
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG118:                ;; offset=0x06BF
       xor      esi, esi
       mov      dword ptr [rbp-0x28], esi
       cmp      r11d, 16
       jb       G_M000_IG135
       cmp      r11d, 32
       jb       G_M000_IG129
       cmp      r11d, 32
       jb       SHORT G_M000_IG119
       cmp      r11d, 48
       jb       SHORT G_M000_IG124
 
G_M000_IG119:                ;; offset=0x06E4
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG120
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG120:                ;; offset=0x0709
       cmp      r11d, 63
       jne      SHORT G_M000_IG121
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG121:                ;; offset=0x0716
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG122
       mov      edx, -1
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:                ;; offset=0x0728
       mov      edx, 1
 
G_M000_IG123:                ;; offset=0x072D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG124:                ;; offset=0x074B
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG125
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG125:                ;; offset=0x075C
       cmp      r11d, 47
       jne      SHORT G_M000_IG126
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG126:                ;; offset=0x0769
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG127
       mov      edx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:                ;; offset=0x077B
       mov      edx, 1
 
G_M000_IG128:                ;; offset=0x0780
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG129:                ;; offset=0x079E
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG130
       cmp      r11d, 16
       jne      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x07AB
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG131:                ;; offset=0x07B4
       cmp      r11d, 31
       jne      SHORT G_M000_IG132
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG132:                ;; offset=0x07C1
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG133
       mov      edx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x0800
       mov      edx, 1
 
G_M000_IG134:                ;; offset=0x0805
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG135:                ;; offset=0x0820
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG136
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG136:                ;; offset=0x0830
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG137
       mov      edx, -1
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:                ;; offset=0x0842
       mov      edx, 1
 
G_M000_IG138:                ;; offset=0x0847
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2141

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[RuntimePositiveOne]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       mov      edx, dword ptr [(reloc 0x7f804326b140)]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0089
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0099
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009B
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AB
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D5
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 226

; Assembly listing for method SignedSeekBenchmarks:RunDirect[RuntimeNegativeOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, dword ptr [(reloc 0x7f804326b144)]
       test     esi, esi
       jle      SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0027
       mov      r8d, 1
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG10]
 
G_M000_IG05:                ;; offset=0x002F
       mov      r8d, -1
 
G_M000_IG06:                ;; offset=0x0035
       test     esi, esi
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0039
       mov      r9d, esi
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x003E
       movsxd   r9, esi
       neg      r9
 
G_M000_IG09:                ;; offset=0x0044
       jmp      G_M000_IG33
 
G_M000_IG10:                ;; offset=0x0049
       lea      r9, [rax-0x01]
 
G_M000_IG11:                ;; offset=0x004D
       mov      r10, r9
       sar      r10, 63
       and      r10, 63
       add      r10, r9
       sar      r10, 6
       shl      r10, 6
       sub      r9, r10
       jns      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0068
       add      r9, 64
 
G_M000_IG13:                ;; offset=0x006C
       mov      r10d, 64
       test     r8d, r8d
       jge      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x0077
       mov      r10d, 192
 
G_M000_IG15:                ;; offset=0x007D
       test     r9d, r9d
       jne      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x0082
       or       r10d, 4
       movzx    r10, r10b
 
G_M000_IG17:                ;; offset=0x008A
       cmp      r9d, 63
       jne      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0090
       or       r10d, 8
       movzx    r10, r10b
 
G_M000_IG19:                ;; offset=0x0098
       cmp      r9d, 16
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x009E
       cmp      r9d, 32
       jb       SHORT G_M000_IG22
       cmp      r9d, 48
       jb       SHORT G_M000_IG21
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      SHORT G_M000_IG24
 
G_M000_IG21:                ;; offset=0x00B3
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG24
 
G_M000_IG22:                ;; offset=0x00BC
       lea      r11d, [r9-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x00E8
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG24:                ;; offset=0x00EF
       cmp      r9d, 48
       ja       SHORT G_M000_IG27
 
G_M000_IG25:                ;; offset=0x00F5
       mov      r11, 0x1000000010001
       bt       r11, r9
       jae      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x0105
       or       r10d, 1
       movzx    r10, r10b
 
G_M000_IG27:                ;; offset=0x010D
       cmp      r9d, 63
       ja       SHORT G_M000_IG30
 
G_M000_IG28:                ;; offset=0x0113
       mov      r11, 0x7FFF7FFF7FFFFFFF
       bt       r11, r9
       jb       SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x0123
       or       r10d, 2
       movzx    r10, r10b
 
G_M000_IG30:                ;; offset=0x012B
       test     r10b, 128
       je       SHORT G_M000_IG36
 
G_M000_IG31:                ;; offset=0x0131
       mov      r9d, -1
 
G_M000_IG32:                ;; offset=0x0137
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       movsxd   r9, r8d
       add      rax, r9
       add      ecx, r8d
       mov      r9, rsi
 
G_M000_IG33:                ;; offset=0x0155
       lea      rsi, [r9-0x01]
       test     r9, r9
       je       SHORT G_M000_IG37
 
G_M000_IG34:                ;; offset=0x015E
       xor      r9d, r9d
       mov      dword ptr [rbp-0x08], r9d
       test     r8d, r8d
       jle      G_M000_IG10
 
G_M000_IG35:                ;; offset=0x016E
       mov      r9, rax
       jmp      G_M000_IG11
 
G_M000_IG36:                ;; offset=0x0176
       mov      r9d, 1
       jmp      SHORT G_M000_IG32
 
G_M000_IG37:                ;; offset=0x017E
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG38:                ;; offset=0x0186
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG39:                ;; offset=0x01A1
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 423

; Assembly listing for method SignedSeekBenchmarks:RunTyped[RuntimeNegativeOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 126 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
 
G_M000_IG02:                ;; offset=0x001A
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x001F
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0023
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002A
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x002F
       mov      edx, dword ptr [(reloc 0x7f804326b144)]
       mov      rsi, qword ptr [rbp-0x20]
       mov      r8d, dword ptr [rbp-0x18]
       movzx    r9, byte  ptr [rbp-0x14]
       movsxd   r10, edx
       mov      r11d, r9d
       and      r11d, 3
       cmp      r11d, 1
       jne      SHORT G_M000_IG08
       test     r10, r10
       jle      SHORT G_M000_IG07
       mov      r11, r10
       neg      r11
       mov      rbx, 0x7FFFFFFFFFFFFFFF
       add      r11, rbx
       cmp      r11, rsi
       jl       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x006F
       test     r10, r10
       jge      SHORT G_M000_IG09
       mov      r11, r10
       neg      r11
       mov      rbx, 0x8000000000000000
       add      r11, rbx
       cmp      r11, rsi
       jle      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x008C
       xor      r11d, r11d
       jmp      G_M000_IG56
       align    [0 bytes for IG11]
 
G_M000_IG09:                ;; offset=0x0094
       add      r10, rsi
       add      r8d, edx
       test     edx, edx
       je       G_M000_IG55
       mov      r11, rsi
       sar      r11, 63
       and      r11, 63
       add      r11, rsi
       sar      r11, 6
       shl      r11, 6
       mov      rbx, rsi
       sub      rbx, r11
       jns      SHORT G_M000_IG10
       add      rbx, 64
 
G_M000_IG10:                ;; offset=0x00C4
       mov      r11d, ebx
       cmp      edx, 1
       je       G_M000_IG116
       cmp      edx, -1
       je       G_M000_IG91
       cmp      edx, 1
       jg       G_M000_IG59
       cmp      rsi, r10
       jle      G_M000_IG54
 
G_M000_IG11:                ;; offset=0x00EB
       test     r11d, r11d
       je       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00F0
       dec      r11d
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00F5
       mov      r11d, 63
 
G_M000_IG14:                ;; offset=0x00FB
       dec      rsi
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0108
       mov      edx, 196
 
G_M000_IG16:                ;; offset=0x010D
       cmp      r11d, 63
       jne      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0113
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG18:                ;; offset=0x0119
       xor      ebx, ebx
       mov      dword ptr [rbp-0x40], ebx
       cmp      r11d, 16
       jb       G_M000_IG45
       cmp      r11d, 32
       jb       G_M000_IG35
       cmp      r11d, 32
       jb       SHORT G_M000_IG19
       cmp      r11d, 48
       jb       SHORT G_M000_IG27
 
G_M000_IG19:                ;; offset=0x013E
       cmp      r11d, 48
       jb       G_M000_IG53
       cmp      r11d, 64
       jae      G_M000_IG53
       mov      ebx, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x015A
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG21:                ;; offset=0x0160
       cmp      r11d, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x0166
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x016C
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0176
       mov      edx, -1
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x017D
       mov      edx, 1
 
G_M000_IG26:                ;; offset=0x0182
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG27:                ;; offset=0x01A0
       mov      ebx, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01A8
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG29:                ;; offset=0x01AE
       cmp      r11d, 47
       jne      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01B4
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG31:                ;; offset=0x01BA
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x01C4
       mov      edx, -1
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x01CB
       mov      edx, 1
 
G_M000_IG34:                ;; offset=0x01D0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG35:                ;; offset=0x01EE
       mov      ebx, edx
       test     r11d, r11d
       je       SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x01F5
       cmp      r11d, 16
       jne      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x01FB
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG38:                ;; offset=0x0201
       cmp      r11d, 31
       je       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x0207
       cmp      r11d, 47
       jne      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x020D
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG41:                ;; offset=0x0213
       lea      edx, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x40], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x0247
       mov      edx, -1
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:                ;; offset=0x024E
       mov      edx, 1
 
G_M000_IG44:                ;; offset=0x0253
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x40]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG45:                ;; offset=0x026B
       mov      ebx, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0272
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG47:                ;; offset=0x0278
       cmp      r11d, 31
       jne      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x027E
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG49:                ;; offset=0x0284
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG51
 
G_M000_IG50:                ;; offset=0x028E
       mov      edx, -1
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0295
       mov      edx, 1
 
G_M000_IG52:                ;; offset=0x029A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG53:                ;; offset=0x02AB
       cmp      rsi, r10
       jg       G_M000_IG11
 
G_M000_IG54:                ;; offset=0x02B4
       movzx    rdx, r9b
       mov      qword ptr [rbp-0x20], r10
       mov      dword ptr [rbp-0x18], r8d
       mov      byte  ptr [rbp-0x14], dl
 
G_M000_IG55:                ;; offset=0x02C3
       mov      r11d, 1
 
G_M000_IG56:                ;; offset=0x02C9
       test     r11d, r11d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG57:                ;; offset=0x02DC
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG58:                ;; offset=0x0304
       add      rsp, 56
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG59:                ;; offset=0x030B
       mov      rdx, rsi
       cmp      rdx, r10
       jl       G_M000_IG81
       jmp      SHORT G_M000_IG54
       align    [0 bytes for IG60]
 
G_M000_IG60:                ;; offset=0x0319
       mov      esi, 1
 
G_M000_IG61:                ;; offset=0x031E
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG62:                ;; offset=0x033C
       mov      ebx, esi
       cmp      r11d, 16
       jne      SHORT G_M000_IG63
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG63:                ;; offset=0x034B
       cmp      r11d, 47
       jne      SHORT G_M000_IG64
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG64:                ;; offset=0x0357
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG65
       mov      esi, -1
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x0368
       mov      esi, 1
 
G_M000_IG66:                ;; offset=0x036D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG67:                ;; offset=0x038B
       mov      ebx, esi
       test     r11d, r11d
       je       SHORT G_M000_IG68
       cmp      r11d, 16
       jne      SHORT G_M000_IG69
 
G_M000_IG68:                ;; offset=0x0398
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG69:                ;; offset=0x039F
       cmp      r11d, 31
       je       SHORT G_M000_IG70
       cmp      r11d, 47
       jne      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x03AB
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG71:                ;; offset=0x03B1
       lea      esi, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG72
       mov      esi, -1
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x03EC
       mov      esi, 1
 
G_M000_IG73:                ;; offset=0x03F1
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG74:                ;; offset=0x0409
       mov      ebx, esi
       test     r11d, r11d
       jne      SHORT G_M000_IG75
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG75:                ;; offset=0x0417
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG76
       mov      esi, -1
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x0428
       mov      esi, 1
 
G_M000_IG77:                ;; offset=0x042D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG78:                ;; offset=0x043E
       inc      rdx
       cmp      r11d, 63
       je       SHORT G_M000_IG79
       inc      r11d
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x044C
       xor      r11d, r11d
 
G_M000_IG80:                ;; offset=0x044F
       cmp      rdx, r10
       jge      G_M000_IG54
 
G_M000_IG81:                ;; offset=0x0458
       mov      esi, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG83
 
G_M000_IG82:                ;; offset=0x0462
       mov      esi, 68
 
G_M000_IG83:                ;; offset=0x0467
       cmp      r11d, 63
       jne      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x046D
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG85:                ;; offset=0x0474
       xor      ebx, ebx
       mov      dword ptr [rbp-0x38], ebx
       cmp      r11d, 16
       jb       SHORT G_M000_IG74
 
G_M000_IG86:                ;; offset=0x047F
       cmp      r11d, 32
       jb       G_M000_IG67
       cmp      r11d, 32
       jb       SHORT G_M000_IG87
       cmp      r11d, 48
       jb       G_M000_IG62
 
G_M000_IG87:                ;; offset=0x0499
       cmp      r11d, 48
       jb       SHORT G_M000_IG78
       cmp      r11d, 64
       jae      SHORT G_M000_IG78
       mov      ebx, esi
       cmp      r11d, 48
       jne      SHORT G_M000_IG88
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG88:                ;; offset=0x04B4
       cmp      r11d, 63
       jne      SHORT G_M000_IG89
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG89:                ;; offset=0x04C0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       G_M000_IG60
 
G_M000_IG90:                ;; offset=0x04CE
       mov      esi, -1
       jmp      G_M000_IG61
 
G_M000_IG91:                ;; offset=0x04D8
       test     r11d, r11d
       je       SHORT G_M000_IG92
       dec      r11d
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x04E2
       mov      r11d, 63
 
G_M000_IG93:                ;; offset=0x04E8
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG94
       mov      edx, 196
 
G_M000_IG94:                ;; offset=0x04F7
       cmp      r11d, 63
       jne      SHORT G_M000_IG95
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG95:                ;; offset=0x0503
       xor      esi, esi
       mov      dword ptr [rbp-0x30], esi
       cmp      r11d, 16
       jb       G_M000_IG112
       cmp      r11d, 32
       jb       G_M000_IG106
       cmp      r11d, 32
       jb       SHORT G_M000_IG96
       cmp      r11d, 48
       jb       SHORT G_M000_IG101
 
G_M000_IG96:                ;; offset=0x0528
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG97
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG97:                ;; offset=0x054D
       cmp      r11d, 63
       jne      SHORT G_M000_IG98
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG98:                ;; offset=0x055A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG99
       mov      edx, -1
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:                ;; offset=0x056C
       mov      edx, 1
 
G_M000_IG100:                ;; offset=0x0571
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG101:                ;; offset=0x058F
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG102
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG102:                ;; offset=0x05A0
       cmp      r11d, 47
       jne      SHORT G_M000_IG103
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG103:                ;; offset=0x05AD
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG104
       mov      edx, -1
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x05BF
       mov      edx, 1
 
G_M000_IG105:                ;; offset=0x05C4
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG106:                ;; offset=0x05E2
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG107
       cmp      r11d, 16
       jne      SHORT G_M000_IG108
 
G_M000_IG107:                ;; offset=0x05EF
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG108:                ;; offset=0x05F8
       cmp      r11d, 31
       jne      SHORT G_M000_IG109
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG109:                ;; offset=0x0605
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG110
       mov      edx, -1
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:                ;; offset=0x0644
       mov      edx, 1
 
G_M000_IG111:                ;; offset=0x0649
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x30]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG112:                ;; offset=0x0664
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG113
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG113:                ;; offset=0x0674
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG114
       mov      r11d, -1
       jmp      SHORT G_M000_IG115
 
G_M000_IG114:                ;; offset=0x0687
       mov      r11d, 1
 
G_M000_IG115:                ;; offset=0x068D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG116:                ;; offset=0x06A4
       mov      edx, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG117
       mov      edx, 68
 
G_M000_IG117:                ;; offset=0x06B3
       cmp      r11d, 63
       jne      SHORT G_M000_IG118
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG118:                ;; offset=0x06BF
       xor      esi, esi
       mov      dword ptr [rbp-0x28], esi
       cmp      r11d, 16
       jb       G_M000_IG135
       cmp      r11d, 32
       jb       G_M000_IG129
       cmp      r11d, 32
       jb       SHORT G_M000_IG119
       cmp      r11d, 48
       jb       SHORT G_M000_IG124
 
G_M000_IG119:                ;; offset=0x06E4
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG120
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG120:                ;; offset=0x0709
       cmp      r11d, 63
       jne      SHORT G_M000_IG121
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG121:                ;; offset=0x0716
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG122
       mov      edx, -1
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:                ;; offset=0x0728
       mov      edx, 1
 
G_M000_IG123:                ;; offset=0x072D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG124:                ;; offset=0x074B
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG125
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG125:                ;; offset=0x075C
       cmp      r11d, 47
       jne      SHORT G_M000_IG126
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG126:                ;; offset=0x0769
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG127
       mov      edx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:                ;; offset=0x077B
       mov      edx, 1
 
G_M000_IG128:                ;; offset=0x0780
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG129:                ;; offset=0x079E
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG130
       cmp      r11d, 16
       jne      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x07AB
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG131:                ;; offset=0x07B4
       cmp      r11d, 31
       jne      SHORT G_M000_IG132
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG132:                ;; offset=0x07C1
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG133
       mov      edx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x0800
       mov      edx, 1
 
G_M000_IG134:                ;; offset=0x0805
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG135:                ;; offset=0x0820
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG136
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG136:                ;; offset=0x0830
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG137
       mov      edx, -1
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:                ;; offset=0x0842
       mov      edx, 1
 
G_M000_IG138:                ;; offset=0x0847
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2141

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[RuntimeNegativeOne]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       mov      edx, dword ptr [(reloc 0x7f804326b144)]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0089
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0099
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009B
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AB
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D5
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 226

; Assembly listing for method SignedSeekBenchmarks:RunDirect[AlternatingOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       push     rax
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
 
G_M000_IG02:                ;; offset=0x000E
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 16
       mov      esi, 0x1000
 
G_M000_IG03:                ;; offset=0x0020
       mov      r8, 0x7F70A8000618
       mov      r8, gword ptr [r8]
       mov      r8d, dword ptr [r8+rdx]
       test     r8d, r8d
       jle      SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0036
       mov      r9d, 1
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG10]
 
G_M000_IG05:                ;; offset=0x003E
       mov      r9d, -1
 
G_M000_IG06:                ;; offset=0x0044
       test     r8d, r8d
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0049
       mov      r10d, r8d
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x004E
       movsxd   r10, r8d
       neg      r10
 
G_M000_IG09:                ;; offset=0x0054
       jmp      G_M000_IG33
 
G_M000_IG10:                ;; offset=0x0059
       lea      r10, [rax-0x01]
 
G_M000_IG11:                ;; offset=0x005D
       mov      r11, r10
       sar      r11, 63
       and      r11, 63
       add      r11, r10
       sar      r11, 6
       shl      r11, 6
       sub      r10, r11
       jns      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0078
       add      r10, 64
 
G_M000_IG13:                ;; offset=0x007C
       mov      r11d, 64
       test     r9d, r9d
       jge      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x0087
       mov      r11d, 192
 
G_M000_IG15:                ;; offset=0x008D
       test     r10d, r10d
       jne      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x0092
       or       r11d, 4
       movzx    r11, r11b
 
G_M000_IG17:                ;; offset=0x009A
       cmp      r10d, 63
       jne      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x00A0
       or       r11d, 8
       movzx    r11, r11b
 
G_M000_IG19:                ;; offset=0x00A8
       cmp      r10d, 16
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x00AE
       cmp      r10d, 32
       jb       SHORT G_M000_IG22
       cmp      r10d, 48
       jb       SHORT G_M000_IG21
       mov      dword ptr [rbp-0x10], 0x40A00000
       jmp      SHORT G_M000_IG24
 
G_M000_IG21:                ;; offset=0x00C3
       mov      dword ptr [rbp-0x10], 0x40400000
       jmp      SHORT G_M000_IG24
 
G_M000_IG22:                ;; offset=0x00CC
       lea      ebx, [r10-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rbx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x10], xmm1
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x00F8
       mov      dword ptr [rbp-0x10], 0x3F800000
 
G_M000_IG24:                ;; offset=0x00FF
       cmp      r10d, 48
       ja       SHORT G_M000_IG27
 
G_M000_IG25:                ;; offset=0x0105
       mov      rbx, 0x1000000010001
       bt       rbx, r10
       jae      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x0115
       or       r11d, 1
       movzx    r11, r11b
 
G_M000_IG27:                ;; offset=0x011D
       cmp      r10d, 63
       ja       SHORT G_M000_IG30
 
G_M000_IG28:                ;; offset=0x0123
       mov      rbx, 0x7FFF7FFF7FFFFFFF
       bt       rbx, r10
       jb       SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x0133
       or       r11d, 2
       movzx    r11, r11b
 
G_M000_IG30:                ;; offset=0x013B
       test     r11b, 128
       je       SHORT G_M000_IG36
 
G_M000_IG31:                ;; offset=0x0141
       mov      r10d, -1
 
G_M000_IG32:                ;; offset=0x0147
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x10]
       vaddss   xmm0, xmm1, xmm0
       movsxd   r10, r9d
       add      rax, r10
       add      ecx, r9d
       mov      r10, r8
 
G_M000_IG33:                ;; offset=0x0165
       lea      r8, [r10-0x01]
       test     r10, r10
       je       SHORT G_M000_IG37
 
G_M000_IG34:                ;; offset=0x016E
       xor      r10d, r10d
       mov      dword ptr [rbp-0x10], r10d
       test     r9d, r9d
       jle      G_M000_IG10
 
G_M000_IG35:                ;; offset=0x017E
       mov      r10, rax
       jmp      G_M000_IG11
 
G_M000_IG36:                ;; offset=0x0186
       mov      r10d, 1
       jmp      SHORT G_M000_IG32
 
G_M000_IG37:                ;; offset=0x018E
       add      rdx, 4
       dec      esi
       jne      G_M000_IG03
 
G_M000_IG38:                ;; offset=0x019A
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG39:                ;; offset=0x01B5
       add      rsp, 8
       pop      rbx
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 444

; Assembly listing for method SignedSeekBenchmarks:RunTyped[AlternatingOne]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 126 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
 
G_M000_IG02:                ;; offset=0x001A
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x001F
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0023
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002A
       xor      ecx, ecx
 
G_M000_IG06:                ;; offset=0x002C
       mov      rdx, 0x7F70A8000618
       mov      rdx, gword ptr [rdx]
       cmp      ecx, 0x1000
       jae      G_M000_IG139
       mov      edx, dword ptr [rdx+4*rcx+0x10]
       mov      rsi, qword ptr [rbp-0x20]
       mov      r8d, dword ptr [rbp-0x18]
       movzx    r9, byte  ptr [rbp-0x14]
       movsxd   r10, edx
       mov      r11d, r9d
       and      r11d, 3
       cmp      r11d, 1
       jne      SHORT G_M000_IG08
       test     r10, r10
       jle      SHORT G_M000_IG07
       mov      r11, r10
       neg      r11
       mov      rbx, 0x7FFFFFFFFFFFFFFF
       add      r11, rbx
       cmp      r11, rsi
       jl       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0083
       test     r10, r10
       jge      SHORT G_M000_IG09
       mov      r11, r10
       neg      r11
       mov      rbx, 0x8000000000000000
       add      r11, rbx
       cmp      r11, rsi
       jle      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x00A0
       xor      r11d, r11d
       jmp      G_M000_IG56
       align    [0 bytes for IG11]
 
G_M000_IG09:                ;; offset=0x00A8
       add      r10, rsi
       add      r8d, edx
       test     edx, edx
       je       G_M000_IG55
       mov      r11, rsi
       sar      r11, 63
       and      r11, 63
       add      r11, rsi
       sar      r11, 6
       shl      r11, 6
       mov      rbx, rsi
       sub      rbx, r11
       jns      SHORT G_M000_IG10
       add      rbx, 64
 
G_M000_IG10:                ;; offset=0x00D8
       mov      r11d, ebx
       cmp      edx, 1
       je       G_M000_IG116
       cmp      edx, -1
       je       G_M000_IG91
       cmp      edx, 1
       jg       G_M000_IG59
       cmp      rsi, r10
       jle      G_M000_IG54
 
G_M000_IG11:                ;; offset=0x00FF
       test     r11d, r11d
       je       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0104
       dec      r11d
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0109
       mov      r11d, 63
 
G_M000_IG14:                ;; offset=0x010F
       dec      rsi
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x011C
       mov      edx, 196
 
G_M000_IG16:                ;; offset=0x0121
       cmp      r11d, 63
       jne      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0127
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG18:                ;; offset=0x012D
       xor      ebx, ebx
       mov      dword ptr [rbp-0x40], ebx
       cmp      r11d, 16
       jb       G_M000_IG45
       cmp      r11d, 32
       jb       G_M000_IG35
       cmp      r11d, 32
       jb       SHORT G_M000_IG19
       cmp      r11d, 48
       jb       SHORT G_M000_IG27
 
G_M000_IG19:                ;; offset=0x0152
       cmp      r11d, 48
       jb       G_M000_IG53
       cmp      r11d, 64
       jae      G_M000_IG53
       mov      ebx, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x016E
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG21:                ;; offset=0x0174
       cmp      r11d, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x017A
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x0180
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x018A
       mov      edx, -1
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x0191
       mov      edx, 1
 
G_M000_IG26:                ;; offset=0x0196
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG27:                ;; offset=0x01B4
       mov      ebx, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01BC
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG29:                ;; offset=0x01C2
       cmp      r11d, 47
       jne      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01C8
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG31:                ;; offset=0x01CE
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x01D8
       mov      edx, -1
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x01DF
       mov      edx, 1
 
G_M000_IG34:                ;; offset=0x01E4
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG35:                ;; offset=0x0202
       mov      ebx, edx
       test     r11d, r11d
       je       SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x0209
       cmp      r11d, 16
       jne      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x020F
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG38:                ;; offset=0x0215
       cmp      r11d, 31
       je       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x021B
       cmp      r11d, 47
       jne      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x0221
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG41:                ;; offset=0x0227
       lea      edx, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x40], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x025B
       mov      edx, -1
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:                ;; offset=0x0262
       mov      edx, 1
 
G_M000_IG44:                ;; offset=0x0267
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x40]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG45:                ;; offset=0x027F
       mov      ebx, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0286
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG47:                ;; offset=0x028C
       cmp      r11d, 31
       jne      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x0292
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG49:                ;; offset=0x0298
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG51
 
G_M000_IG50:                ;; offset=0x02A2
       mov      edx, -1
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x02A9
       mov      edx, 1
 
G_M000_IG52:                ;; offset=0x02AE
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG53:                ;; offset=0x02BF
       cmp      rsi, r10
       jg       G_M000_IG11
 
G_M000_IG54:                ;; offset=0x02C8
       movzx    rdx, r9b
       mov      qword ptr [rbp-0x20], r10
       mov      dword ptr [rbp-0x18], r8d
       mov      byte  ptr [rbp-0x14], dl
 
G_M000_IG55:                ;; offset=0x02D7
       mov      r11d, 1
 
G_M000_IG56:                ;; offset=0x02DD
       test     r11d, r11d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       inc      ecx
       cmp      ecx, 0x1000
       jl       G_M000_IG06
 
G_M000_IG57:                ;; offset=0x02F6
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG58:                ;; offset=0x031E
       add      rsp, 56
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG59:                ;; offset=0x0325
       mov      rdx, rsi
       cmp      rdx, r10
       jl       G_M000_IG81
       jmp      SHORT G_M000_IG54
       align    [0 bytes for IG60]
 
G_M000_IG60:                ;; offset=0x0333
       mov      esi, 1
 
G_M000_IG61:                ;; offset=0x0338
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG62:                ;; offset=0x0356
       mov      ebx, esi
       cmp      r11d, 16
       jne      SHORT G_M000_IG63
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG63:                ;; offset=0x0365
       cmp      r11d, 47
       jne      SHORT G_M000_IG64
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG64:                ;; offset=0x0371
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG65
       mov      esi, -1
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x0382
       mov      esi, 1
 
G_M000_IG66:                ;; offset=0x0387
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG67:                ;; offset=0x03A5
       mov      ebx, esi
       test     r11d, r11d
       je       SHORT G_M000_IG68
       cmp      r11d, 16
       jne      SHORT G_M000_IG69
 
G_M000_IG68:                ;; offset=0x03B2
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG69:                ;; offset=0x03B9
       cmp      r11d, 31
       je       SHORT G_M000_IG70
       cmp      r11d, 47
       jne      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x03C5
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG71:                ;; offset=0x03CB
       lea      esi, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG72
       mov      esi, -1
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x0406
       mov      esi, 1
 
G_M000_IG73:                ;; offset=0x040B
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG74:                ;; offset=0x0423
       mov      ebx, esi
       test     r11d, r11d
       jne      SHORT G_M000_IG75
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG75:                ;; offset=0x0431
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG76
       mov      esi, -1
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x0442
       mov      esi, 1
 
G_M000_IG77:                ;; offset=0x0447
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG78:                ;; offset=0x0458
       inc      rdx
       cmp      r11d, 63
       je       SHORT G_M000_IG79
       inc      r11d
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x0466
       xor      r11d, r11d
 
G_M000_IG80:                ;; offset=0x0469
       cmp      rdx, r10
       jge      G_M000_IG54
 
G_M000_IG81:                ;; offset=0x0472
       mov      esi, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG83
 
G_M000_IG82:                ;; offset=0x047C
       mov      esi, 68
 
G_M000_IG83:                ;; offset=0x0481
       cmp      r11d, 63
       jne      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x0487
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG85:                ;; offset=0x048E
       xor      ebx, ebx
       mov      dword ptr [rbp-0x38], ebx
       cmp      r11d, 16
       jb       SHORT G_M000_IG74
 
G_M000_IG86:                ;; offset=0x0499
       cmp      r11d, 32
       jb       G_M000_IG67
       cmp      r11d, 32
       jb       SHORT G_M000_IG87
       cmp      r11d, 48
       jb       G_M000_IG62
 
G_M000_IG87:                ;; offset=0x04B3
       cmp      r11d, 48
       jb       SHORT G_M000_IG78
       cmp      r11d, 64
       jae      SHORT G_M000_IG78
       mov      ebx, esi
       cmp      r11d, 48
       jne      SHORT G_M000_IG88
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG88:                ;; offset=0x04CE
       cmp      r11d, 63
       jne      SHORT G_M000_IG89
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG89:                ;; offset=0x04DA
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       G_M000_IG60
 
G_M000_IG90:                ;; offset=0x04E8
       mov      esi, -1
       jmp      G_M000_IG61
 
G_M000_IG91:                ;; offset=0x04F2
       test     r11d, r11d
       je       SHORT G_M000_IG92
       dec      r11d
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x04FC
       mov      r11d, 63
 
G_M000_IG93:                ;; offset=0x0502
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG94
       mov      edx, 196
 
G_M000_IG94:                ;; offset=0x0511
       cmp      r11d, 63
       jne      SHORT G_M000_IG95
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG95:                ;; offset=0x051D
       xor      esi, esi
       mov      dword ptr [rbp-0x30], esi
       cmp      r11d, 16
       jb       G_M000_IG112
       cmp      r11d, 32
       jb       G_M000_IG106
       cmp      r11d, 32
       jb       SHORT G_M000_IG96
       cmp      r11d, 48
       jb       SHORT G_M000_IG101
 
G_M000_IG96:                ;; offset=0x0542
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG97
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG97:                ;; offset=0x0567
       cmp      r11d, 63
       jne      SHORT G_M000_IG98
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG98:                ;; offset=0x0574
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG99
       mov      edx, -1
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:                ;; offset=0x0586
       mov      edx, 1
 
G_M000_IG100:                ;; offset=0x058B
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG101:                ;; offset=0x05A9
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG102
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG102:                ;; offset=0x05BA
       cmp      r11d, 47
       jne      SHORT G_M000_IG103
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG103:                ;; offset=0x05C7
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG104
       mov      edx, -1
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x05D9
       mov      edx, 1
 
G_M000_IG105:                ;; offset=0x05DE
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG106:                ;; offset=0x05FC
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG107
       cmp      r11d, 16
       jne      SHORT G_M000_IG108
 
G_M000_IG107:                ;; offset=0x0609
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG108:                ;; offset=0x0612
       cmp      r11d, 31
       jne      SHORT G_M000_IG109
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG109:                ;; offset=0x061F
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG110
       mov      edx, -1
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:                ;; offset=0x065E
       mov      edx, 1
 
G_M000_IG111:                ;; offset=0x0663
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x30]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG112:                ;; offset=0x067E
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG113
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG113:                ;; offset=0x068E
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG114
       mov      r11d, -1
       jmp      SHORT G_M000_IG115
 
G_M000_IG114:                ;; offset=0x06A1
       mov      r11d, 1
 
G_M000_IG115:                ;; offset=0x06A7
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG116:                ;; offset=0x06BE
       mov      edx, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG117
       mov      edx, 68
 
G_M000_IG117:                ;; offset=0x06CD
       cmp      r11d, 63
       jne      SHORT G_M000_IG118
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG118:                ;; offset=0x06D9
       xor      esi, esi
       mov      dword ptr [rbp-0x28], esi
       cmp      r11d, 16
       jb       G_M000_IG135
       cmp      r11d, 32
       jb       G_M000_IG129
       cmp      r11d, 32
       jb       SHORT G_M000_IG119
       cmp      r11d, 48
       jb       SHORT G_M000_IG124
 
G_M000_IG119:                ;; offset=0x06FE
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG120
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG120:                ;; offset=0x0723
       cmp      r11d, 63
       jne      SHORT G_M000_IG121
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG121:                ;; offset=0x0730
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG122
       mov      edx, -1
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:                ;; offset=0x0742
       mov      edx, 1
 
G_M000_IG123:                ;; offset=0x0747
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG124:                ;; offset=0x0765
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG125
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG125:                ;; offset=0x0776
       cmp      r11d, 47
       jne      SHORT G_M000_IG126
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG126:                ;; offset=0x0783
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG127
       mov      edx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:                ;; offset=0x0795
       mov      edx, 1
 
G_M000_IG128:                ;; offset=0x079A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG129:                ;; offset=0x07B8
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG130
       cmp      r11d, 16
       jne      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x07C5
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG131:                ;; offset=0x07CE
       cmp      r11d, 31
       jne      SHORT G_M000_IG132
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG132:                ;; offset=0x07DB
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG133
       mov      edx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x081A
       mov      edx, 1
 
G_M000_IG134:                ;; offset=0x081F
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG135:                ;; offset=0x083A
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG136
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG136:                ;; offset=0x084A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG137
       mov      edx, -1
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:                ;; offset=0x085C
       mov      edx, 1
 
G_M000_IG138:                ;; offset=0x0861
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG139:                ;; offset=0x0877
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2173

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[AlternatingOne]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x60]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0029
       xor      esi, esi
       mov      dword ptr [rbp-0x2C], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x58]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0044
       xor      ecx, ecx
       mov      qword ptr [rbp-0x40], rcx
 
G_M000_IG04:                ;; offset=0x004A
       mov      dword ptr [rbp-0x38], ecx
       mov      word  ptr [rbp-0x34], r14w
       mov      byte  ptr [rbp-0x32], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0058
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x40], xmm0
 
G_M000_IG06:                ;; offset=0x0061
       lea      rcx, bword ptr [rbp-0x40]
       mov      bword ptr [rbp-0x50], rcx
       lea      rcx, bword ptr [rbp-0x2C]
       mov      bword ptr [rbp-0x48], rcx
       xor      r14d, r14d
       mov      r13d, 16
       mov      r12d, 0x1000
 
G_M000_IG07:                ;; offset=0x0080
       movzx    rdi, word  ptr [rbx+0x0C]
       mov      rcx, 0x7F70A8000618
       mov      rcx, gword ptr [rcx]
       mov      edx, dword ptr [rcx+r13]
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x009C
       lea      rcx, [rbp-0x48]
       mov      rsi, bword ptr [rbp-0x50]
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00AC
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x00AE
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       add      r13, 4
       dec      r12d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00C2
       mov      rax, qword ptr [rbp-0x40]
       mov      ecx, dword ptr [rbp-0x38]
       movzx    rdx, byte  ptr [rbp-0x32]
       vmovss   xmm0, dword ptr [rbp-0x2C]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00EC
       add      rsp, 56
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 251

; Assembly listing for method SignedSeekBenchmarks:RunDirect[LiteralPositiveFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, 5
       jmp      G_M000_IG16
       align    [0 bytes for IG04]
 
G_M000_IG04:                ;; offset=0x0027
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:                ;; offset=0x0030
       lea      r10d, [rsi-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x005C
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG07:                ;; offset=0x0063
       cmp      esi, 48
       ja       SHORT G_M000_IG10
 
G_M000_IG08:                ;; offset=0x0068
       mov      r10, 0x1000000010001
       bt       r10, rsi
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0078
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG10:                ;; offset=0x0080
       cmp      esi, 63
       ja       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x0085
       mov      r10, 0x7FFF7FFF7FFFFFFF
       bt       r10, rsi
       jb       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0095
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG13:                ;; offset=0x009D
       test     r9b, 128
       je       G_M000_IG26
 
G_M000_IG14:                ;; offset=0x00A7
       mov      esi, -1
 
G_M000_IG15:                ;; offset=0x00AC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       inc      rax
       inc      ecx
       mov      rsi, r8
 
G_M000_IG16:                ;; offset=0x00C5
       lea      r8, [rsi-0x01]
       test     rsi, rsi
       je       SHORT G_M000_IG27
 
G_M000_IG17:                ;; offset=0x00CE
       xor      esi, esi
       mov      dword ptr [rbp-0x08], esi
       mov      rsi, rax
       sar      rsi, 63
       and      rsi, 63
       add      rsi, rax
       sar      rsi, 6
       shl      rsi, 6
       mov      r9, rax
       sub      r9, rsi
       jns      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x00F1
       add      r9, 64
 
G_M000_IG19:                ;; offset=0x00F5
       mov      esi, r9d
       mov      r9d, 64
       test     esi, esi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x0102
       mov      r9d, 68
 
G_M000_IG21:                ;; offset=0x0108
       cmp      esi, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x010D
       or       r9d, 8
       movzx    r9, r9b
 
G_M000_IG23:                ;; offset=0x0115
       cmp      esi, 16
       jb       G_M000_IG06
 
G_M000_IG24:                ;; offset=0x011E
       cmp      esi, 32
       jb       G_M000_IG05
       cmp      esi, 48
       jb       G_M000_IG04
 
G_M000_IG25:                ;; offset=0x0130
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      G_M000_IG07
 
G_M000_IG26:                ;; offset=0x013C
       mov      esi, 1
       jmp      G_M000_IG15
 
G_M000_IG27:                ;; offset=0x0146
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG28:                ;; offset=0x014E
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG29:                ;; offset=0x0169
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 367

; Assembly listing for method SignedSeekBenchmarks:RunTyped[LiteralPositiveFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 39 single block inlinees; 7 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x30]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  xmmword ptr [rbp-0x28], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
 
G_M000_IG02:                ;; offset=0x001B
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x0020
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0024
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002B
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x0030
       mov      rdx, qword ptr [rbp-0x20]
       mov      esi, dword ptr [rbp-0x18]
       movzx    r8, byte  ptr [rbp-0x14]
       mov      r9d, r8d
       and      r9d, 3
       cmp      r9d, 1
       jne      SHORT G_M000_IG07
       mov      r9, 0x7FFFFFFFFFFFFFFA
       cmp      rdx, r9
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0058
       xor      r9d, r9d
       jmp      G_M000_IG40
       align    [0 bytes for IG10]
 
G_M000_IG08:                ;; offset=0x0060
       lea      r9, [rdx+0x05]
       add      esi, 5
       mov      r10, rdx
       sar      r10, 63
       and      r10, 63
       add      r10, rdx
       sar      r10, 6
       shl      r10, 6
       mov      r11, rdx
       sub      r11, r10
       jns      SHORT G_M000_IG09
       add      r11, 64
 
G_M000_IG09:                ;; offset=0x0089
       mov      r10d, r11d
       cmp      rdx, r9
       jge      G_M000_IG39
 
G_M000_IG10:                ;; offset=0x0095
       mov      r11d, 64
       test     r10d, r10d
       jne      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00A0
       mov      r11d, 68
 
G_M000_IG12:                ;; offset=0x00A6
       cmp      r10d, 63
       jne      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00AC
       or       r11d, 8
       movzx    r11, r11b
 
G_M000_IG14:                ;; offset=0x00B4
       xor      ebx, ebx
       mov      dword ptr [rbp-0x28], ebx
       cmp      r10d, 16
       jb       G_M000_IG32
 
G_M000_IG15:                ;; offset=0x00C3
       cmp      r10d, 32
       jb       G_M000_IG26
       cmp      r10d, 32
       jb       SHORT G_M000_IG16
       cmp      r10d, 48
       jb       SHORT G_M000_IG21
 
G_M000_IG16:                ;; offset=0x00D9
       cmp      r10d, 48
       jb       G_M000_IG36
       cmp      r10d, 64
       jae      G_M000_IG36
       mov      ebx, r11d
       cmp      r10d, 48
       jne      SHORT G_M000_IG17
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG17:                ;; offset=0x00FE
       cmp      r10d, 63
       jne      SHORT G_M000_IG18
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG18:                ;; offset=0x010A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG19
       mov      r11d, -1
       jmp      SHORT G_M000_IG20
 
G_M000_IG19:                ;; offset=0x011C
       mov      r11d, 1
 
G_M000_IG20:                ;; offset=0x0122
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG36
 
G_M000_IG21:                ;; offset=0x0141
       mov      ebx, r11d
       cmp      r10d, 16
       jne      SHORT G_M000_IG22
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG22:                ;; offset=0x0152
       cmp      r10d, 47
       jne      SHORT G_M000_IG23
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x015E
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG24
       mov      r11d, -1
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0170
       mov      r11d, 1
 
G_M000_IG25:                ;; offset=0x0176
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG36
 
G_M000_IG26:                ;; offset=0x0195
       mov      ebx, r11d
       test     r10d, r10d
       je       SHORT G_M000_IG27
       cmp      r10d, 16
       jne      SHORT G_M000_IG28
 
G_M000_IG27:                ;; offset=0x01A3
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG28:                ;; offset=0x01AB
       cmp      r10d, 31
       jne      SHORT G_M000_IG29
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG29:                ;; offset=0x01B7
       lea      r11d, [r10-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG30
       mov      r11d, -1
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01F3
       mov      r11d, 1
 
G_M000_IG31:                ;; offset=0x01F9
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG36
 
G_M000_IG32:                ;; offset=0x0212
       mov      ebx, r11d
       test     r10d, r10d
       jne      SHORT G_M000_IG33
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG33:                ;; offset=0x0222
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG34
       mov      r11d, -1
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x0234
       mov      r11d, 1
 
G_M000_IG35:                ;; offset=0x023A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG36:                ;; offset=0x024C
       inc      rdx
       cmp      r10d, 63
       je       SHORT G_M000_IG37
       inc      r10d
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x025A
       xor      r10d, r10d
 
G_M000_IG38:                ;; offset=0x025D
       cmp      rdx, r9
       jl       G_M000_IG10
 
G_M000_IG39:                ;; offset=0x0266
       movzx    rdx, r8b
       mov      qword ptr [rbp-0x20], r9
       mov      dword ptr [rbp-0x18], esi
       mov      byte  ptr [rbp-0x14], dl
       mov      r9d, 1
 
G_M000_IG40:                ;; offset=0x027A
       test     r9d, r9d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG41:                ;; offset=0x028D
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG42:                ;; offset=0x02B5
       add      rsp, 40
       pop      rbx
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 700

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[LiteralPositiveFive]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0083
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       mov      edx, 5
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0098
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009A
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AA
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D4
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 225

; Assembly listing for method SignedSeekBenchmarks:RunDirect[LiteralNegativeFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, 5
       jmp      G_M000_IG16
       align    [0 bytes for IG04]
 
G_M000_IG04:                ;; offset=0x0027
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:                ;; offset=0x0030
       lea      r10d, [rsi-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x005C
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG07:                ;; offset=0x0063
       cmp      esi, 48
       ja       SHORT G_M000_IG10
 
G_M000_IG08:                ;; offset=0x0068
       mov      r10, 0x1000000010001
       bt       r10, rsi
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0078
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG10:                ;; offset=0x0080
       cmp      esi, 63
       ja       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x0085
       mov      r10, 0x7FFF7FFF7FFFFFFF
       bt       r10, rsi
       jb       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0095
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG13:                ;; offset=0x009D
       test     r9b, 128
       je       G_M000_IG26
 
G_M000_IG14:                ;; offset=0x00A7
       mov      esi, -1
 
G_M000_IG15:                ;; offset=0x00AC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       dec      ecx
       mov      rsi, r8
 
G_M000_IG16:                ;; offset=0x00C2
       lea      r8, [rsi-0x01]
       test     rsi, rsi
       je       SHORT G_M000_IG27
 
G_M000_IG17:                ;; offset=0x00CB
       xor      esi, esi
       mov      dword ptr [rbp-0x08], esi
       dec      rax
       mov      rsi, rax
       mov      r9, rsi
       sar      r9, 63
       and      r9, 63
       add      r9, rsi
       sar      r9, 6
       shl      r9, 6
       sub      rsi, r9
       jns      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x00F1
       add      rsi, 64
 
G_M000_IG19:                ;; offset=0x00F5
       mov      r9d, 192
       test     esi, esi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x00FF
       mov      r9d, 196
 
G_M000_IG21:                ;; offset=0x0105
       cmp      esi, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x010A
       or       r9d, 8
       movzx    r9, r9b
 
G_M000_IG23:                ;; offset=0x0112
       cmp      esi, 16
       jb       G_M000_IG06
 
G_M000_IG24:                ;; offset=0x011B
       cmp      esi, 32
       jb       G_M000_IG05
       cmp      esi, 48
       jb       G_M000_IG04
 
G_M000_IG25:                ;; offset=0x012D
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      G_M000_IG07
 
G_M000_IG26:                ;; offset=0x0139
       mov      esi, 1
       jmp      G_M000_IG15
 
G_M000_IG27:                ;; offset=0x0143
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG28:                ;; offset=0x014B
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG29:                ;; offset=0x0166
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 364

; Assembly listing for method SignedSeekBenchmarks:RunTyped[LiteralNegativeFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 39 single block inlinees; 7 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x30]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  xmmword ptr [rbp-0x28], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
 
G_M000_IG02:                ;; offset=0x001B
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x0020
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0024
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002B
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x0030
       mov      rdx, qword ptr [rbp-0x20]
       mov      esi, dword ptr [rbp-0x18]
       movzx    r8, byte  ptr [rbp-0x14]
       mov      r9d, r8d
       and      r9d, 3
       cmp      r9d, 1
       jne      SHORT G_M000_IG07
       mov      r9, 0x8000000000000005
       cmp      rdx, r9
       jge      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0058
       xor      r9d, r9d
       jmp      G_M000_IG50
       align    [0 bytes for IG10]
 
G_M000_IG08:                ;; offset=0x0060
       lea      r9, [rdx-0x05]
       add      esi, -5
       mov      r10, rdx
       sar      r10, 63
       and      r10, 63
       add      r10, rdx
       sar      r10, 6
       shl      r10, 6
       mov      r11, rdx
       sub      r11, r10
       jns      SHORT G_M000_IG09
       add      r11, 64
 
G_M000_IG09:                ;; offset=0x0089
       mov      r10d, r11d
       cmp      rdx, r9
       jle      G_M000_IG49
 
G_M000_IG10:                ;; offset=0x0095
       test     r10d, r10d
       je       SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x009A
       dec      r10d
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x009F
       mov      r10d, 63
 
G_M000_IG13:                ;; offset=0x00A5
       dec      rdx
       mov      r11d, 192
       test     r10d, r10d
       jne      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x00B3
       mov      r11d, 196
 
G_M000_IG15:                ;; offset=0x00B9
       cmp      r10d, 63
       jne      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x00BF
       or       r11d, 8
       movzx    r11, r11b
 
G_M000_IG17:                ;; offset=0x00C7
       xor      ebx, ebx
       mov      dword ptr [rbp-0x28], ebx
       cmp      r10d, 16
       jb       G_M000_IG43
       cmp      r10d, 32
       jb       G_M000_IG34
       cmp      r10d, 32
       jb       SHORT G_M000_IG18
       cmp      r10d, 48
       jb       SHORT G_M000_IG26
 
G_M000_IG18:                ;; offset=0x00EC
       cmp      r10d, 48
       jb       G_M000_IG48
       cmp      r10d, 64
       jae      G_M000_IG48
       mov      ebx, r11d
       cmp      r10d, 48
       jne      SHORT G_M000_IG20
 
G_M000_IG19:                ;; offset=0x0109
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG20:                ;; offset=0x0111
       cmp      r10d, 63
       jne      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x0117
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG22:                ;; offset=0x011D
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x0127
       mov      r11d, -1
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x012F
       mov      r11d, 1
 
G_M000_IG25:                ;; offset=0x0135
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG48
 
G_M000_IG26:                ;; offset=0x0154
       mov      ebx, r11d
       cmp      r10d, 16
       jne      SHORT G_M000_IG28
 
G_M000_IG27:                ;; offset=0x015D
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG28:                ;; offset=0x0165
       cmp      r10d, 47
       jne      SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x016B
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG30:                ;; offset=0x0171
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x017B
       mov      r11d, -1
       jmp      SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x0183
       mov      r11d, 1
 
G_M000_IG33:                ;; offset=0x0189
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG48
 
G_M000_IG34:                ;; offset=0x01A8
       mov      ebx, r11d
       test     r10d, r10d
       je       SHORT G_M000_IG36
 
G_M000_IG35:                ;; offset=0x01B0
       cmp      r10d, 16
       jne      SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x01B6
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG37:                ;; offset=0x01BE
       cmp      r10d, 31
       jne      SHORT G_M000_IG39
 
G_M000_IG38:                ;; offset=0x01C4
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG39:                ;; offset=0x01CA
       lea      r11d, [r10-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x01FE
       mov      r11d, -1
       jmp      SHORT G_M000_IG42
 
G_M000_IG41:                ;; offset=0x0206
       mov      r11d, 1
 
G_M000_IG42:                ;; offset=0x020C
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG48
 
G_M000_IG43:                ;; offset=0x0225
       mov      ebx, r11d
       test     r10d, r10d
       jne      SHORT G_M000_IG44
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG44:                ;; offset=0x0235
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG46
 
G_M000_IG45:                ;; offset=0x023F
       mov      r11d, -1
       jmp      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0247
       mov      r11d, 1
 
G_M000_IG47:                ;; offset=0x024D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG48:                ;; offset=0x025F
       cmp      rdx, r9
       jg       G_M000_IG10
 
G_M000_IG49:                ;; offset=0x0268
       movzx    rdx, r8b
       mov      qword ptr [rbp-0x20], r9
       mov      dword ptr [rbp-0x18], esi
       mov      byte  ptr [rbp-0x14], dl
       mov      r9d, 1
 
G_M000_IG50:                ;; offset=0x027C
       test     r9d, r9d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG51:                ;; offset=0x028F
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG52:                ;; offset=0x02B7
       add      rsp, 40
       pop      rbx
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 702

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[LiteralNegativeFive]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0083
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       mov      edx, -5
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0098
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009A
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AA
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D4
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 225

; Assembly listing for method SignedSeekBenchmarks:RunDirect[RuntimePositiveFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, dword ptr [(reloc 0x7f804326b148)]
       test     esi, esi
       jle      SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0027
       mov      r8d, 1
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG10]
 
G_M000_IG05:                ;; offset=0x002F
       mov      r8d, -1
 
G_M000_IG06:                ;; offset=0x0035
       test     esi, esi
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0039
       mov      r9d, esi
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x003E
       movsxd   r9, esi
       neg      r9
 
G_M000_IG09:                ;; offset=0x0044
       jmp      G_M000_IG33
 
G_M000_IG10:                ;; offset=0x0049
       lea      r9, [rax-0x01]
 
G_M000_IG11:                ;; offset=0x004D
       mov      r10, r9
       sar      r10, 63
       and      r10, 63
       add      r10, r9
       sar      r10, 6
       shl      r10, 6
       sub      r9, r10
       jns      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0068
       add      r9, 64
 
G_M000_IG13:                ;; offset=0x006C
       mov      r10d, 64
       test     r8d, r8d
       jge      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x0077
       mov      r10d, 192
 
G_M000_IG15:                ;; offset=0x007D
       test     r9d, r9d
       jne      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x0082
       or       r10d, 4
       movzx    r10, r10b
 
G_M000_IG17:                ;; offset=0x008A
       cmp      r9d, 63
       jne      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0090
       or       r10d, 8
       movzx    r10, r10b
 
G_M000_IG19:                ;; offset=0x0098
       cmp      r9d, 16
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x009E
       cmp      r9d, 32
       jb       SHORT G_M000_IG22
       cmp      r9d, 48
       jb       SHORT G_M000_IG21
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      SHORT G_M000_IG24
 
G_M000_IG21:                ;; offset=0x00B3
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG24
 
G_M000_IG22:                ;; offset=0x00BC
       lea      r11d, [r9-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x00E8
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG24:                ;; offset=0x00EF
       cmp      r9d, 48
       ja       SHORT G_M000_IG27
 
G_M000_IG25:                ;; offset=0x00F5
       mov      r11, 0x1000000010001
       bt       r11, r9
       jae      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x0105
       or       r10d, 1
       movzx    r10, r10b
 
G_M000_IG27:                ;; offset=0x010D
       cmp      r9d, 63
       ja       SHORT G_M000_IG30
 
G_M000_IG28:                ;; offset=0x0113
       mov      r11, 0x7FFF7FFF7FFFFFFF
       bt       r11, r9
       jb       SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x0123
       or       r10d, 2
       movzx    r10, r10b
 
G_M000_IG30:                ;; offset=0x012B
       test     r10b, 128
       je       SHORT G_M000_IG36
 
G_M000_IG31:                ;; offset=0x0131
       mov      r9d, -1
 
G_M000_IG32:                ;; offset=0x0137
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       movsxd   r9, r8d
       add      rax, r9
       add      ecx, r8d
       mov      r9, rsi
 
G_M000_IG33:                ;; offset=0x0155
       lea      rsi, [r9-0x01]
       test     r9, r9
       je       SHORT G_M000_IG37
 
G_M000_IG34:                ;; offset=0x015E
       xor      r9d, r9d
       mov      dword ptr [rbp-0x08], r9d
       test     r8d, r8d
       jle      G_M000_IG10
 
G_M000_IG35:                ;; offset=0x016E
       mov      r9, rax
       jmp      G_M000_IG11
 
G_M000_IG36:                ;; offset=0x0176
       mov      r9d, 1
       jmp      SHORT G_M000_IG32
 
G_M000_IG37:                ;; offset=0x017E
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG38:                ;; offset=0x0186
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG39:                ;; offset=0x01A1
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 423

; Assembly listing for method SignedSeekBenchmarks:RunTyped[RuntimePositiveFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 126 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
 
G_M000_IG02:                ;; offset=0x001A
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x001F
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0023
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002A
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x002F
       mov      edx, dword ptr [(reloc 0x7f804326b148)]
       mov      rsi, qword ptr [rbp-0x20]
       mov      r8d, dword ptr [rbp-0x18]
       movzx    r9, byte  ptr [rbp-0x14]
       movsxd   r10, edx
       mov      r11d, r9d
       and      r11d, 3
       cmp      r11d, 1
       jne      SHORT G_M000_IG08
       test     r10, r10
       jle      SHORT G_M000_IG07
       mov      r11, r10
       neg      r11
       mov      rbx, 0x7FFFFFFFFFFFFFFF
       add      r11, rbx
       cmp      r11, rsi
       jl       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x006F
       test     r10, r10
       jge      SHORT G_M000_IG09
       mov      r11, r10
       neg      r11
       mov      rbx, 0x8000000000000000
       add      r11, rbx
       cmp      r11, rsi
       jle      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x008C
       xor      r11d, r11d
       jmp      G_M000_IG56
       align    [0 bytes for IG11]
 
G_M000_IG09:                ;; offset=0x0094
       add      r10, rsi
       add      r8d, edx
       test     edx, edx
       je       G_M000_IG55
       mov      r11, rsi
       sar      r11, 63
       and      r11, 63
       add      r11, rsi
       sar      r11, 6
       shl      r11, 6
       mov      rbx, rsi
       sub      rbx, r11
       jns      SHORT G_M000_IG10
       add      rbx, 64
 
G_M000_IG10:                ;; offset=0x00C4
       mov      r11d, ebx
       cmp      edx, 1
       je       G_M000_IG116
       cmp      edx, -1
       je       G_M000_IG91
       cmp      edx, 1
       jg       G_M000_IG59
       cmp      rsi, r10
       jle      G_M000_IG54
 
G_M000_IG11:                ;; offset=0x00EB
       test     r11d, r11d
       je       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00F0
       dec      r11d
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00F5
       mov      r11d, 63
 
G_M000_IG14:                ;; offset=0x00FB
       dec      rsi
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0108
       mov      edx, 196
 
G_M000_IG16:                ;; offset=0x010D
       cmp      r11d, 63
       jne      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0113
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG18:                ;; offset=0x0119
       xor      ebx, ebx
       mov      dword ptr [rbp-0x40], ebx
       cmp      r11d, 16
       jb       G_M000_IG45
       cmp      r11d, 32
       jb       G_M000_IG35
       cmp      r11d, 32
       jb       SHORT G_M000_IG19
       cmp      r11d, 48
       jb       SHORT G_M000_IG27
 
G_M000_IG19:                ;; offset=0x013E
       cmp      r11d, 48
       jb       G_M000_IG53
       cmp      r11d, 64
       jae      G_M000_IG53
       mov      ebx, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x015A
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG21:                ;; offset=0x0160
       cmp      r11d, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x0166
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x016C
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0176
       mov      edx, -1
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x017D
       mov      edx, 1
 
G_M000_IG26:                ;; offset=0x0182
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG27:                ;; offset=0x01A0
       mov      ebx, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01A8
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG29:                ;; offset=0x01AE
       cmp      r11d, 47
       jne      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01B4
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG31:                ;; offset=0x01BA
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x01C4
       mov      edx, -1
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x01CB
       mov      edx, 1
 
G_M000_IG34:                ;; offset=0x01D0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG35:                ;; offset=0x01EE
       mov      ebx, edx
       test     r11d, r11d
       je       SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x01F5
       cmp      r11d, 16
       jne      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x01FB
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG38:                ;; offset=0x0201
       cmp      r11d, 31
       je       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x0207
       cmp      r11d, 47
       jne      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x020D
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG41:                ;; offset=0x0213
       lea      edx, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x40], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x0247
       mov      edx, -1
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:                ;; offset=0x024E
       mov      edx, 1
 
G_M000_IG44:                ;; offset=0x0253
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x40]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG45:                ;; offset=0x026B
       mov      ebx, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0272
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG47:                ;; offset=0x0278
       cmp      r11d, 31
       jne      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x027E
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG49:                ;; offset=0x0284
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG51
 
G_M000_IG50:                ;; offset=0x028E
       mov      edx, -1
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0295
       mov      edx, 1
 
G_M000_IG52:                ;; offset=0x029A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG53:                ;; offset=0x02AB
       cmp      rsi, r10
       jg       G_M000_IG11
 
G_M000_IG54:                ;; offset=0x02B4
       movzx    rdx, r9b
       mov      qword ptr [rbp-0x20], r10
       mov      dword ptr [rbp-0x18], r8d
       mov      byte  ptr [rbp-0x14], dl
 
G_M000_IG55:                ;; offset=0x02C3
       mov      r11d, 1
 
G_M000_IG56:                ;; offset=0x02C9
       test     r11d, r11d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG57:                ;; offset=0x02DC
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG58:                ;; offset=0x0304
       add      rsp, 56
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG59:                ;; offset=0x030B
       mov      rdx, rsi
       cmp      rdx, r10
       jl       G_M000_IG81
       jmp      SHORT G_M000_IG54
       align    [0 bytes for IG60]
 
G_M000_IG60:                ;; offset=0x0319
       mov      esi, 1
 
G_M000_IG61:                ;; offset=0x031E
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG62:                ;; offset=0x033C
       mov      ebx, esi
       cmp      r11d, 16
       jne      SHORT G_M000_IG63
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG63:                ;; offset=0x034B
       cmp      r11d, 47
       jne      SHORT G_M000_IG64
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG64:                ;; offset=0x0357
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG65
       mov      esi, -1
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x0368
       mov      esi, 1
 
G_M000_IG66:                ;; offset=0x036D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG67:                ;; offset=0x038B
       mov      ebx, esi
       test     r11d, r11d
       je       SHORT G_M000_IG68
       cmp      r11d, 16
       jne      SHORT G_M000_IG69
 
G_M000_IG68:                ;; offset=0x0398
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG69:                ;; offset=0x039F
       cmp      r11d, 31
       je       SHORT G_M000_IG70
       cmp      r11d, 47
       jne      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x03AB
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG71:                ;; offset=0x03B1
       lea      esi, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG72
       mov      esi, -1
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x03EC
       mov      esi, 1
 
G_M000_IG73:                ;; offset=0x03F1
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG74:                ;; offset=0x0409
       mov      ebx, esi
       test     r11d, r11d
       jne      SHORT G_M000_IG75
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG75:                ;; offset=0x0417
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG76
       mov      esi, -1
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x0428
       mov      esi, 1
 
G_M000_IG77:                ;; offset=0x042D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG78:                ;; offset=0x043E
       inc      rdx
       cmp      r11d, 63
       je       SHORT G_M000_IG79
       inc      r11d
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x044C
       xor      r11d, r11d
 
G_M000_IG80:                ;; offset=0x044F
       cmp      rdx, r10
       jge      G_M000_IG54
 
G_M000_IG81:                ;; offset=0x0458
       mov      esi, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG83
 
G_M000_IG82:                ;; offset=0x0462
       mov      esi, 68
 
G_M000_IG83:                ;; offset=0x0467
       cmp      r11d, 63
       jne      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x046D
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG85:                ;; offset=0x0474
       xor      ebx, ebx
       mov      dword ptr [rbp-0x38], ebx
       cmp      r11d, 16
       jb       SHORT G_M000_IG74
 
G_M000_IG86:                ;; offset=0x047F
       cmp      r11d, 32
       jb       G_M000_IG67
       cmp      r11d, 32
       jb       SHORT G_M000_IG87
       cmp      r11d, 48
       jb       G_M000_IG62
 
G_M000_IG87:                ;; offset=0x0499
       cmp      r11d, 48
       jb       SHORT G_M000_IG78
       cmp      r11d, 64
       jae      SHORT G_M000_IG78
       mov      ebx, esi
       cmp      r11d, 48
       jne      SHORT G_M000_IG88
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG88:                ;; offset=0x04B4
       cmp      r11d, 63
       jne      SHORT G_M000_IG89
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG89:                ;; offset=0x04C0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       G_M000_IG60
 
G_M000_IG90:                ;; offset=0x04CE
       mov      esi, -1
       jmp      G_M000_IG61
 
G_M000_IG91:                ;; offset=0x04D8
       test     r11d, r11d
       je       SHORT G_M000_IG92
       dec      r11d
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x04E2
       mov      r11d, 63
 
G_M000_IG93:                ;; offset=0x04E8
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG94
       mov      edx, 196
 
G_M000_IG94:                ;; offset=0x04F7
       cmp      r11d, 63
       jne      SHORT G_M000_IG95
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG95:                ;; offset=0x0503
       xor      esi, esi
       mov      dword ptr [rbp-0x30], esi
       cmp      r11d, 16
       jb       G_M000_IG112
       cmp      r11d, 32
       jb       G_M000_IG106
       cmp      r11d, 32
       jb       SHORT G_M000_IG96
       cmp      r11d, 48
       jb       SHORT G_M000_IG101
 
G_M000_IG96:                ;; offset=0x0528
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG97
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG97:                ;; offset=0x054D
       cmp      r11d, 63
       jne      SHORT G_M000_IG98
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG98:                ;; offset=0x055A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG99
       mov      edx, -1
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:                ;; offset=0x056C
       mov      edx, 1
 
G_M000_IG100:                ;; offset=0x0571
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG101:                ;; offset=0x058F
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG102
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG102:                ;; offset=0x05A0
       cmp      r11d, 47
       jne      SHORT G_M000_IG103
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG103:                ;; offset=0x05AD
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG104
       mov      edx, -1
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x05BF
       mov      edx, 1
 
G_M000_IG105:                ;; offset=0x05C4
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG106:                ;; offset=0x05E2
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG107
       cmp      r11d, 16
       jne      SHORT G_M000_IG108
 
G_M000_IG107:                ;; offset=0x05EF
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG108:                ;; offset=0x05F8
       cmp      r11d, 31
       jne      SHORT G_M000_IG109
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG109:                ;; offset=0x0605
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG110
       mov      edx, -1
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:                ;; offset=0x0644
       mov      edx, 1
 
G_M000_IG111:                ;; offset=0x0649
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x30]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG112:                ;; offset=0x0664
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG113
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG113:                ;; offset=0x0674
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG114
       mov      r11d, -1
       jmp      SHORT G_M000_IG115
 
G_M000_IG114:                ;; offset=0x0687
       mov      r11d, 1
 
G_M000_IG115:                ;; offset=0x068D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG116:                ;; offset=0x06A4
       mov      edx, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG117
       mov      edx, 68
 
G_M000_IG117:                ;; offset=0x06B3
       cmp      r11d, 63
       jne      SHORT G_M000_IG118
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG118:                ;; offset=0x06BF
       xor      esi, esi
       mov      dword ptr [rbp-0x28], esi
       cmp      r11d, 16
       jb       G_M000_IG135
       cmp      r11d, 32
       jb       G_M000_IG129
       cmp      r11d, 32
       jb       SHORT G_M000_IG119
       cmp      r11d, 48
       jb       SHORT G_M000_IG124
 
G_M000_IG119:                ;; offset=0x06E4
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG120
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG120:                ;; offset=0x0709
       cmp      r11d, 63
       jne      SHORT G_M000_IG121
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG121:                ;; offset=0x0716
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG122
       mov      edx, -1
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:                ;; offset=0x0728
       mov      edx, 1
 
G_M000_IG123:                ;; offset=0x072D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG124:                ;; offset=0x074B
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG125
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG125:                ;; offset=0x075C
       cmp      r11d, 47
       jne      SHORT G_M000_IG126
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG126:                ;; offset=0x0769
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG127
       mov      edx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:                ;; offset=0x077B
       mov      edx, 1
 
G_M000_IG128:                ;; offset=0x0780
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG129:                ;; offset=0x079E
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG130
       cmp      r11d, 16
       jne      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x07AB
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG131:                ;; offset=0x07B4
       cmp      r11d, 31
       jne      SHORT G_M000_IG132
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG132:                ;; offset=0x07C1
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG133
       mov      edx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x0800
       mov      edx, 1
 
G_M000_IG134:                ;; offset=0x0805
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG135:                ;; offset=0x0820
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG136
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG136:                ;; offset=0x0830
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG137
       mov      edx, -1
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:                ;; offset=0x0842
       mov      edx, 1
 
G_M000_IG138:                ;; offset=0x0847
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2141

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[RuntimePositiveFive]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       mov      edx, dword ptr [(reloc 0x7f804326b148)]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0089
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0099
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009B
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AB
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D5
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 226

; Assembly listing for method SignedSeekBenchmarks:RunDirect[RuntimeNegativeFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, dword ptr [(reloc 0x7f804326b14c)]
       test     esi, esi
       jle      SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0027
       mov      r8d, 1
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG10]
 
G_M000_IG05:                ;; offset=0x002F
       mov      r8d, -1
 
G_M000_IG06:                ;; offset=0x0035
       test     esi, esi
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0039
       mov      r9d, esi
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x003E
       movsxd   r9, esi
       neg      r9
 
G_M000_IG09:                ;; offset=0x0044
       jmp      G_M000_IG33
 
G_M000_IG10:                ;; offset=0x0049
       lea      r9, [rax-0x01]
 
G_M000_IG11:                ;; offset=0x004D
       mov      r10, r9
       sar      r10, 63
       and      r10, 63
       add      r10, r9
       sar      r10, 6
       shl      r10, 6
       sub      r9, r10
       jns      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0068
       add      r9, 64
 
G_M000_IG13:                ;; offset=0x006C
       mov      r10d, 64
       test     r8d, r8d
       jge      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x0077
       mov      r10d, 192
 
G_M000_IG15:                ;; offset=0x007D
       test     r9d, r9d
       jne      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x0082
       or       r10d, 4
       movzx    r10, r10b
 
G_M000_IG17:                ;; offset=0x008A
       cmp      r9d, 63
       jne      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0090
       or       r10d, 8
       movzx    r10, r10b
 
G_M000_IG19:                ;; offset=0x0098
       cmp      r9d, 16
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x009E
       cmp      r9d, 32
       jb       SHORT G_M000_IG22
       cmp      r9d, 48
       jb       SHORT G_M000_IG21
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      SHORT G_M000_IG24
 
G_M000_IG21:                ;; offset=0x00B3
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG24
 
G_M000_IG22:                ;; offset=0x00BC
       lea      r11d, [r9-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x00E8
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG24:                ;; offset=0x00EF
       cmp      r9d, 48
       ja       SHORT G_M000_IG27
 
G_M000_IG25:                ;; offset=0x00F5
       mov      r11, 0x1000000010001
       bt       r11, r9
       jae      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x0105
       or       r10d, 1
       movzx    r10, r10b
 
G_M000_IG27:                ;; offset=0x010D
       cmp      r9d, 63
       ja       SHORT G_M000_IG30
 
G_M000_IG28:                ;; offset=0x0113
       mov      r11, 0x7FFF7FFF7FFFFFFF
       bt       r11, r9
       jb       SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x0123
       or       r10d, 2
       movzx    r10, r10b
 
G_M000_IG30:                ;; offset=0x012B
       test     r10b, 128
       je       SHORT G_M000_IG36
 
G_M000_IG31:                ;; offset=0x0131
       mov      r9d, -1
 
G_M000_IG32:                ;; offset=0x0137
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       movsxd   r9, r8d
       add      rax, r9
       add      ecx, r8d
       mov      r9, rsi
 
G_M000_IG33:                ;; offset=0x0155
       lea      rsi, [r9-0x01]
       test     r9, r9
       je       SHORT G_M000_IG37
 
G_M000_IG34:                ;; offset=0x015E
       xor      r9d, r9d
       mov      dword ptr [rbp-0x08], r9d
       test     r8d, r8d
       jle      G_M000_IG10
 
G_M000_IG35:                ;; offset=0x016E
       mov      r9, rax
       jmp      G_M000_IG11
 
G_M000_IG36:                ;; offset=0x0176
       mov      r9d, 1
       jmp      SHORT G_M000_IG32
 
G_M000_IG37:                ;; offset=0x017E
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG38:                ;; offset=0x0186
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG39:                ;; offset=0x01A1
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 423

; Assembly listing for method SignedSeekBenchmarks:RunTyped[RuntimeNegativeFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 126 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
 
G_M000_IG02:                ;; offset=0x001A
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x001F
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0023
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002A
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x002F
       mov      edx, dword ptr [(reloc 0x7f804326b14c)]
       mov      rsi, qword ptr [rbp-0x20]
       mov      r8d, dword ptr [rbp-0x18]
       movzx    r9, byte  ptr [rbp-0x14]
       movsxd   r10, edx
       mov      r11d, r9d
       and      r11d, 3
       cmp      r11d, 1
       jne      SHORT G_M000_IG08
       test     r10, r10
       jle      SHORT G_M000_IG07
       mov      r11, r10
       neg      r11
       mov      rbx, 0x7FFFFFFFFFFFFFFF
       add      r11, rbx
       cmp      r11, rsi
       jl       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x006F
       test     r10, r10
       jge      SHORT G_M000_IG09
       mov      r11, r10
       neg      r11
       mov      rbx, 0x8000000000000000
       add      r11, rbx
       cmp      r11, rsi
       jle      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x008C
       xor      r11d, r11d
       jmp      G_M000_IG56
       align    [0 bytes for IG11]
 
G_M000_IG09:                ;; offset=0x0094
       add      r10, rsi
       add      r8d, edx
       test     edx, edx
       je       G_M000_IG55
       mov      r11, rsi
       sar      r11, 63
       and      r11, 63
       add      r11, rsi
       sar      r11, 6
       shl      r11, 6
       mov      rbx, rsi
       sub      rbx, r11
       jns      SHORT G_M000_IG10
       add      rbx, 64
 
G_M000_IG10:                ;; offset=0x00C4
       mov      r11d, ebx
       cmp      edx, 1
       je       G_M000_IG116
       cmp      edx, -1
       je       G_M000_IG91
       cmp      edx, 1
       jg       G_M000_IG59
       cmp      rsi, r10
       jle      G_M000_IG54
 
G_M000_IG11:                ;; offset=0x00EB
       test     r11d, r11d
       je       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00F0
       dec      r11d
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00F5
       mov      r11d, 63
 
G_M000_IG14:                ;; offset=0x00FB
       dec      rsi
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0108
       mov      edx, 196
 
G_M000_IG16:                ;; offset=0x010D
       cmp      r11d, 63
       jne      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0113
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG18:                ;; offset=0x0119
       xor      ebx, ebx
       mov      dword ptr [rbp-0x40], ebx
       cmp      r11d, 16
       jb       G_M000_IG45
       cmp      r11d, 32
       jb       G_M000_IG35
       cmp      r11d, 32
       jb       SHORT G_M000_IG19
       cmp      r11d, 48
       jb       SHORT G_M000_IG27
 
G_M000_IG19:                ;; offset=0x013E
       cmp      r11d, 48
       jb       G_M000_IG53
       cmp      r11d, 64
       jae      G_M000_IG53
       mov      ebx, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x015A
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG21:                ;; offset=0x0160
       cmp      r11d, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x0166
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x016C
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0176
       mov      edx, -1
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x017D
       mov      edx, 1
 
G_M000_IG26:                ;; offset=0x0182
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG27:                ;; offset=0x01A0
       mov      ebx, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01A8
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG29:                ;; offset=0x01AE
       cmp      r11d, 47
       jne      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01B4
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG31:                ;; offset=0x01BA
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x01C4
       mov      edx, -1
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x01CB
       mov      edx, 1
 
G_M000_IG34:                ;; offset=0x01D0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG35:                ;; offset=0x01EE
       mov      ebx, edx
       test     r11d, r11d
       je       SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x01F5
       cmp      r11d, 16
       jne      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x01FB
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG38:                ;; offset=0x0201
       cmp      r11d, 31
       je       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x0207
       cmp      r11d, 47
       jne      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x020D
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG41:                ;; offset=0x0213
       lea      edx, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x40], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x0247
       mov      edx, -1
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:                ;; offset=0x024E
       mov      edx, 1
 
G_M000_IG44:                ;; offset=0x0253
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x40]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG45:                ;; offset=0x026B
       mov      ebx, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0272
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG47:                ;; offset=0x0278
       cmp      r11d, 31
       jne      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x027E
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG49:                ;; offset=0x0284
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG51
 
G_M000_IG50:                ;; offset=0x028E
       mov      edx, -1
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0295
       mov      edx, 1
 
G_M000_IG52:                ;; offset=0x029A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG53:                ;; offset=0x02AB
       cmp      rsi, r10
       jg       G_M000_IG11
 
G_M000_IG54:                ;; offset=0x02B4
       movzx    rdx, r9b
       mov      qword ptr [rbp-0x20], r10
       mov      dword ptr [rbp-0x18], r8d
       mov      byte  ptr [rbp-0x14], dl
 
G_M000_IG55:                ;; offset=0x02C3
       mov      r11d, 1
 
G_M000_IG56:                ;; offset=0x02C9
       test     r11d, r11d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG57:                ;; offset=0x02DC
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG58:                ;; offset=0x0304
       add      rsp, 56
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG59:                ;; offset=0x030B
       mov      rdx, rsi
       cmp      rdx, r10
       jl       G_M000_IG81
       jmp      SHORT G_M000_IG54
       align    [0 bytes for IG60]
 
G_M000_IG60:                ;; offset=0x0319
       mov      esi, 1
 
G_M000_IG61:                ;; offset=0x031E
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG62:                ;; offset=0x033C
       mov      ebx, esi
       cmp      r11d, 16
       jne      SHORT G_M000_IG63
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG63:                ;; offset=0x034B
       cmp      r11d, 47
       jne      SHORT G_M000_IG64
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG64:                ;; offset=0x0357
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG65
       mov      esi, -1
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x0368
       mov      esi, 1
 
G_M000_IG66:                ;; offset=0x036D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG67:                ;; offset=0x038B
       mov      ebx, esi
       test     r11d, r11d
       je       SHORT G_M000_IG68
       cmp      r11d, 16
       jne      SHORT G_M000_IG69
 
G_M000_IG68:                ;; offset=0x0398
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG69:                ;; offset=0x039F
       cmp      r11d, 31
       je       SHORT G_M000_IG70
       cmp      r11d, 47
       jne      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x03AB
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG71:                ;; offset=0x03B1
       lea      esi, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG72
       mov      esi, -1
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x03EC
       mov      esi, 1
 
G_M000_IG73:                ;; offset=0x03F1
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG74:                ;; offset=0x0409
       mov      ebx, esi
       test     r11d, r11d
       jne      SHORT G_M000_IG75
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG75:                ;; offset=0x0417
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG76
       mov      esi, -1
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x0428
       mov      esi, 1
 
G_M000_IG77:                ;; offset=0x042D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG78:                ;; offset=0x043E
       inc      rdx
       cmp      r11d, 63
       je       SHORT G_M000_IG79
       inc      r11d
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x044C
       xor      r11d, r11d
 
G_M000_IG80:                ;; offset=0x044F
       cmp      rdx, r10
       jge      G_M000_IG54
 
G_M000_IG81:                ;; offset=0x0458
       mov      esi, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG83
 
G_M000_IG82:                ;; offset=0x0462
       mov      esi, 68
 
G_M000_IG83:                ;; offset=0x0467
       cmp      r11d, 63
       jne      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x046D
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG85:                ;; offset=0x0474
       xor      ebx, ebx
       mov      dword ptr [rbp-0x38], ebx
       cmp      r11d, 16
       jb       SHORT G_M000_IG74
 
G_M000_IG86:                ;; offset=0x047F
       cmp      r11d, 32
       jb       G_M000_IG67
       cmp      r11d, 32
       jb       SHORT G_M000_IG87
       cmp      r11d, 48
       jb       G_M000_IG62
 
G_M000_IG87:                ;; offset=0x0499
       cmp      r11d, 48
       jb       SHORT G_M000_IG78
       cmp      r11d, 64
       jae      SHORT G_M000_IG78
       mov      ebx, esi
       cmp      r11d, 48
       jne      SHORT G_M000_IG88
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG88:                ;; offset=0x04B4
       cmp      r11d, 63
       jne      SHORT G_M000_IG89
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG89:                ;; offset=0x04C0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       G_M000_IG60
 
G_M000_IG90:                ;; offset=0x04CE
       mov      esi, -1
       jmp      G_M000_IG61
 
G_M000_IG91:                ;; offset=0x04D8
       test     r11d, r11d
       je       SHORT G_M000_IG92
       dec      r11d
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x04E2
       mov      r11d, 63
 
G_M000_IG93:                ;; offset=0x04E8
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG94
       mov      edx, 196
 
G_M000_IG94:                ;; offset=0x04F7
       cmp      r11d, 63
       jne      SHORT G_M000_IG95
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG95:                ;; offset=0x0503
       xor      esi, esi
       mov      dword ptr [rbp-0x30], esi
       cmp      r11d, 16
       jb       G_M000_IG112
       cmp      r11d, 32
       jb       G_M000_IG106
       cmp      r11d, 32
       jb       SHORT G_M000_IG96
       cmp      r11d, 48
       jb       SHORT G_M000_IG101
 
G_M000_IG96:                ;; offset=0x0528
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG97
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG97:                ;; offset=0x054D
       cmp      r11d, 63
       jne      SHORT G_M000_IG98
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG98:                ;; offset=0x055A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG99
       mov      edx, -1
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:                ;; offset=0x056C
       mov      edx, 1
 
G_M000_IG100:                ;; offset=0x0571
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG101:                ;; offset=0x058F
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG102
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG102:                ;; offset=0x05A0
       cmp      r11d, 47
       jne      SHORT G_M000_IG103
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG103:                ;; offset=0x05AD
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG104
       mov      edx, -1
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x05BF
       mov      edx, 1
 
G_M000_IG105:                ;; offset=0x05C4
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG106:                ;; offset=0x05E2
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG107
       cmp      r11d, 16
       jne      SHORT G_M000_IG108
 
G_M000_IG107:                ;; offset=0x05EF
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG108:                ;; offset=0x05F8
       cmp      r11d, 31
       jne      SHORT G_M000_IG109
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG109:                ;; offset=0x0605
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG110
       mov      edx, -1
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:                ;; offset=0x0644
       mov      edx, 1
 
G_M000_IG111:                ;; offset=0x0649
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x30]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG112:                ;; offset=0x0664
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG113
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG113:                ;; offset=0x0674
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG114
       mov      r11d, -1
       jmp      SHORT G_M000_IG115
 
G_M000_IG114:                ;; offset=0x0687
       mov      r11d, 1
 
G_M000_IG115:                ;; offset=0x068D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG116:                ;; offset=0x06A4
       mov      edx, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG117
       mov      edx, 68
 
G_M000_IG117:                ;; offset=0x06B3
       cmp      r11d, 63
       jne      SHORT G_M000_IG118
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG118:                ;; offset=0x06BF
       xor      esi, esi
       mov      dword ptr [rbp-0x28], esi
       cmp      r11d, 16
       jb       G_M000_IG135
       cmp      r11d, 32
       jb       G_M000_IG129
       cmp      r11d, 32
       jb       SHORT G_M000_IG119
       cmp      r11d, 48
       jb       SHORT G_M000_IG124
 
G_M000_IG119:                ;; offset=0x06E4
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG120
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG120:                ;; offset=0x0709
       cmp      r11d, 63
       jne      SHORT G_M000_IG121
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG121:                ;; offset=0x0716
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG122
       mov      edx, -1
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:                ;; offset=0x0728
       mov      edx, 1
 
G_M000_IG123:                ;; offset=0x072D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG124:                ;; offset=0x074B
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG125
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG125:                ;; offset=0x075C
       cmp      r11d, 47
       jne      SHORT G_M000_IG126
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG126:                ;; offset=0x0769
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG127
       mov      edx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:                ;; offset=0x077B
       mov      edx, 1
 
G_M000_IG128:                ;; offset=0x0780
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG129:                ;; offset=0x079E
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG130
       cmp      r11d, 16
       jne      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x07AB
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG131:                ;; offset=0x07B4
       cmp      r11d, 31
       jne      SHORT G_M000_IG132
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG132:                ;; offset=0x07C1
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG133
       mov      edx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x0800
       mov      edx, 1
 
G_M000_IG134:                ;; offset=0x0805
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG135:                ;; offset=0x0820
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG136
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG136:                ;; offset=0x0830
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG137
       mov      edx, -1
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:                ;; offset=0x0842
       mov      edx, 1
 
G_M000_IG138:                ;; offset=0x0847
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2141

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[RuntimeNegativeFive]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       mov      edx, dword ptr [(reloc 0x7f804326b14c)]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0089
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0099
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009B
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AB
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D5
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 226

; Assembly listing for method SignedSeekBenchmarks:RunDirect[RepeatedPositiveOneFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, 5
       jmp      G_M000_IG16
       align    [0 bytes for IG04]
 
G_M000_IG04:                ;; offset=0x0027
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:                ;; offset=0x0030
       lea      r10d, [rsi-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x005C
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG07:                ;; offset=0x0063
       cmp      esi, 48
       ja       SHORT G_M000_IG10
 
G_M000_IG08:                ;; offset=0x0068
       mov      r10, 0x1000000010001
       bt       r10, rsi
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0078
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG10:                ;; offset=0x0080
       cmp      esi, 63
       ja       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x0085
       mov      r10, 0x7FFF7FFF7FFFFFFF
       bt       r10, rsi
       jb       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0095
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG13:                ;; offset=0x009D
       test     r9b, 128
       je       G_M000_IG26
 
G_M000_IG14:                ;; offset=0x00A7
       mov      esi, -1
 
G_M000_IG15:                ;; offset=0x00AC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       inc      rax
       inc      ecx
       mov      rsi, r8
 
G_M000_IG16:                ;; offset=0x00C5
       lea      r8, [rsi-0x01]
       test     rsi, rsi
       je       SHORT G_M000_IG27
 
G_M000_IG17:                ;; offset=0x00CE
       xor      esi, esi
       mov      dword ptr [rbp-0x08], esi
       mov      rsi, rax
       sar      rsi, 63
       and      rsi, 63
       add      rsi, rax
       sar      rsi, 6
       shl      rsi, 6
       mov      r9, rax
       sub      r9, rsi
       jns      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x00F1
       add      r9, 64
 
G_M000_IG19:                ;; offset=0x00F5
       mov      esi, r9d
       mov      r9d, 64
       test     esi, esi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x0102
       mov      r9d, 68
 
G_M000_IG21:                ;; offset=0x0108
       cmp      esi, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x010D
       or       r9d, 8
       movzx    r9, r9b
 
G_M000_IG23:                ;; offset=0x0115
       cmp      esi, 16
       jb       G_M000_IG06
 
G_M000_IG24:                ;; offset=0x011E
       cmp      esi, 32
       jb       G_M000_IG05
       cmp      esi, 48
       jb       G_M000_IG04
 
G_M000_IG25:                ;; offset=0x0130
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      G_M000_IG07
 
G_M000_IG26:                ;; offset=0x013C
       mov      esi, 1
       jmp      G_M000_IG15
 
G_M000_IG27:                ;; offset=0x0146
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG28:                ;; offset=0x014E
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG29:                ;; offset=0x0169
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 367

; Assembly listing for method SignedSeekBenchmarks:RunTyped[RepeatedPositiveOneFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 134 single block inlinees; 29 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 104
       lea      rbp, [rsp+0x90]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x002D
       xor      edx, edx
       mov      dword ptr [rbp-0x2C], edx
 
G_M000_IG03:                ;; offset=0x0032
       mov      qword ptr [rbp-0x40], rdx
 
G_M000_IG04:                ;; offset=0x0036
       mov      dword ptr [rbp-0x38], edx
       mov      byte  ptr [rbp-0x34], 1
       lea      rdx, bword ptr [rbp-0x40]
       mov      bword ptr [rbp-0x50], rdx
       lea      rdx, bword ptr [rbp-0x2C]
       mov      bword ptr [rbp-0x48], rdx
       xor      r15d, r15d
       mov      r14d, 0x1000
 
G_M000_IG05:                ;; offset=0x0056
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG31
 
G_M000_IG06:                ;; offset=0x0061
       mov      rdx, bword ptr [rbp-0x50]
       mov      r9, qword ptr [rdx]
       mov      r8d, dword ptr [rdx+0x08]
       movzx    rdx, byte  ptr [rdx+0x0C]
       mov      edi, edx
       and      edi, 3
       cmp      edi, 1
       jne      G_M000_IG31
       mov      rdi, 0x7FFFFFFFFFFFFFFE
       cmp      r9, rdi
       jg       G_M000_IG31
       lea      rdi, [r9+0x01]
       inc      r8d
       mov      rsi, r9
       sar      rsi, 63
       and      rsi, 63
       add      rsi, r9
       sar      rsi, 6
       shl      rsi, 6
       sub      r9, rsi
       jns      SHORT G_M000_IG07
       add      r9, 64
 
G_M000_IG07:                ;; offset=0x00B7
       mov      esi, 64
       test     r9d, r9d
       jne      SHORT G_M000_IG08
       mov      esi, 68
 
G_M000_IG08:                ;; offset=0x00C6
       cmp      r9d, 63
       jne      SHORT G_M000_IG09
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG09:                ;; offset=0x00D3
       xor      ecx, ecx
       mov      dword ptr [rbp-0x58], ecx
       cmp      r9d, 16
       jb       G_M000_IG26
       cmp      r9d, 32
       jb       G_M000_IG20
       cmp      r9d, 32
       jb       SHORT G_M000_IG10
       cmp      r9d, 48
       jb       SHORT G_M000_IG15
 
G_M000_IG10:                ;; offset=0x00F8
       cmp      r9d, 48
       jb       G_M000_IG30
       cmp      r9d, 64
       jae      G_M000_IG30
       mov      ecx, esi
       cmp      r9d, 48
       jne      SHORT G_M000_IG11
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG11:                ;; offset=0x011C
       cmp      r9d, 63
       jne      SHORT G_M000_IG12
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG12:                ;; offset=0x0128
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG13
       mov      r9d, -1
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x013D
       mov      r9d, 1
 
G_M000_IG14:                ;; offset=0x0143
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG30
 
G_M000_IG15:                ;; offset=0x0161
       mov      ecx, esi
       cmp      r9d, 16
       jne      SHORT G_M000_IG16
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG16:                ;; offset=0x0171
       cmp      r9d, 47
       jne      SHORT G_M000_IG17
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG17:                ;; offset=0x017D
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG18
       mov      r9d, -1
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0192
       mov      r9d, 1
 
G_M000_IG19:                ;; offset=0x0198
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG30
 
G_M000_IG20:                ;; offset=0x01B6
       mov      ecx, esi
       test     r9d, r9d
       je       SHORT G_M000_IG21
       cmp      r9d, 16
       jne      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x01C3
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG22:                ;; offset=0x01CB
       cmp      r9d, 31
       jne      SHORT G_M000_IG23
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG23:                ;; offset=0x01D7
       add      r9d, -16
       mov      esi, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG24
       mov      esi, -1
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0219
       mov      esi, 1
 
G_M000_IG25:                ;; offset=0x021E
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x58]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG30
 
G_M000_IG26:                ;; offset=0x0236
       mov      ecx, esi
       test     r9d, r9d
       jne      SHORT G_M000_IG27
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG27:                ;; offset=0x0245
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG28
       mov      esi, -1
       jmp      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x025A
       mov      esi, 1
 
G_M000_IG29:                ;; offset=0x025F
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG30:                ;; offset=0x0270
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rdi
       mov      dword ptr [r9+0x08], r8d
       mov      byte  ptr [r9+0x0C], dl
       mov      r13d, 1
       jmp      SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x0287
       xor      r13d, r13d
 
G_M000_IG32:                ;; offset=0x028A
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG58
 
G_M000_IG33:                ;; offset=0x0295
       mov      rdx, bword ptr [rbp-0x50]
       mov      r9, qword ptr [rdx]
       mov      r8d, dword ptr [rdx+0x08]
       movzx    rdx, byte  ptr [rdx+0x0C]
       mov      edi, edx
       and      edi, 3
       cmp      edi, 1
       jne      G_M000_IG58
       mov      rdi, 0x7FFFFFFFFFFFFFFE
       cmp      r9, rdi
       jg       G_M000_IG58
       lea      rdi, [r9+0x01]
       inc      r8d
       mov      rsi, r9
       sar      rsi, 63
       and      rsi, 63
       add      rsi, r9
       sar      rsi, 6
       shl      rsi, 6
       sub      r9, rsi
       jns      SHORT G_M000_IG34
       add      r9, 64
 
G_M000_IG34:                ;; offset=0x02EB
       mov      esi, 64
       test     r9d, r9d
       jne      SHORT G_M000_IG35
       mov      esi, 68
 
G_M000_IG35:                ;; offset=0x02FA
       cmp      r9d, 63
       jne      SHORT G_M000_IG36
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG36:                ;; offset=0x0307
       xor      ecx, ecx
       mov      dword ptr [rbp-0x60], ecx
       cmp      r9d, 16
       jb       G_M000_IG53
       cmp      r9d, 32
       jb       G_M000_IG47
       cmp      r9d, 32
       jb       SHORT G_M000_IG37
       cmp      r9d, 48
       jb       SHORT G_M000_IG42
 
G_M000_IG37:                ;; offset=0x032C
       cmp      r9d, 48
       jb       G_M000_IG57
       cmp      r9d, 64
       jae      G_M000_IG57
       mov      ecx, esi
       cmp      r9d, 48
       jne      SHORT G_M000_IG38
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG38:                ;; offset=0x0350
       cmp      r9d, 63
       jne      SHORT G_M000_IG39
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG39:                ;; offset=0x035C
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG40
       mov      r9d, -1
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x0371
       mov      r9d, 1
 
G_M000_IG41:                ;; offset=0x0377
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG57
 
G_M000_IG42:                ;; offset=0x0395
       mov      ecx, esi
       cmp      r9d, 16
       jne      SHORT G_M000_IG43
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG43:                ;; offset=0x03A5
       cmp      r9d, 47
       jne      SHORT G_M000_IG44
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG44:                ;; offset=0x03B1
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG45
       mov      r9d, -1
       jmp      SHORT G_M000_IG46
 
G_M000_IG45:                ;; offset=0x03C6
       mov      r9d, 1
 
G_M000_IG46:                ;; offset=0x03CC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG57
 
G_M000_IG47:                ;; offset=0x03EA
       mov      ecx, esi
       test     r9d, r9d
       je       SHORT G_M000_IG48
       cmp      r9d, 16
       jne      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x03F7
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG49:                ;; offset=0x03FF
       cmp      r9d, 31
       jne      SHORT G_M000_IG50
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG50:                ;; offset=0x040B
       add      r9d, -16
       mov      esi, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x60], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG51
       mov      esi, -1
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x044D
       mov      esi, 1
 
G_M000_IG52:                ;; offset=0x0452
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x60]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG57
 
G_M000_IG53:                ;; offset=0x046A
       mov      ecx, esi
       test     r9d, r9d
       jne      SHORT G_M000_IG54
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG54:                ;; offset=0x0479
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG55
       mov      esi, -1
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:                ;; offset=0x048E
       mov      esi, 1
 
G_M000_IG56:                ;; offset=0x0493
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG57:                ;; offset=0x04A4
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rdi
       mov      dword ptr [r9+0x08], r8d
       mov      byte  ptr [r9+0x0C], dl
       mov      r12d, 1
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:                ;; offset=0x04BB
       xor      r12d, r12d
 
G_M000_IG59:                ;; offset=0x04BE
       and      r13d, r12d
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG87
 
G_M000_IG60:                ;; offset=0x04CC
       mov      rdx, bword ptr [rbp-0x50]
       mov      r9, qword ptr [rdx]
       mov      r8d, dword ptr [rdx+0x08]
       movzx    rdx, byte  ptr [rdx+0x0C]
       mov      edi, edx
       and      edi, 3
       cmp      edi, 1
       jne      G_M000_IG87
       mov      rdi, 0x7FFFFFFFFFFFFFFE
       cmp      r9, rdi
       jg       G_M000_IG87
       lea      rdi, [r9+0x01]
       inc      r8d
       mov      rsi, r9
       sar      rsi, 63
       and      rsi, 63
       add      rsi, r9
       sar      rsi, 6
       shl      rsi, 6
       sub      r9, rsi
       jns      SHORT G_M000_IG61
       add      r9, 64
 
G_M000_IG61:                ;; offset=0x0522
       mov      esi, 64
       test     r9d, r9d
       jne      SHORT G_M000_IG62
       mov      esi, 68
 
G_M000_IG62:                ;; offset=0x0531
       cmp      r9d, 63
       jne      SHORT G_M000_IG63
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG63:                ;; offset=0x053E
       xor      ecx, ecx
       mov      dword ptr [rbp-0x68], ecx
       cmp      r9d, 16
       jb       G_M000_IG81
       cmp      r9d, 32
       jb       G_M000_IG74
       cmp      r9d, 32
       jb       SHORT G_M000_IG64
       cmp      r9d, 48
       jb       SHORT G_M000_IG69
 
G_M000_IG64:                ;; offset=0x0563
       cmp      r9d, 48
       jb       G_M000_IG86
       cmp      r9d, 64
       jae      G_M000_IG86
       mov      ecx, esi
       cmp      r9d, 48
       jne      SHORT G_M000_IG65
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG65:                ;; offset=0x0587
       cmp      r9d, 63
       jne      SHORT G_M000_IG66
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG66:                ;; offset=0x0593
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG67
       mov      r9d, -1
       jmp      SHORT G_M000_IG68
 
G_M000_IG67:                ;; offset=0x05A8
       mov      r9d, 1
 
G_M000_IG68:                ;; offset=0x05AE
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG86
 
G_M000_IG69:                ;; offset=0x05CC
       mov      ecx, esi
       cmp      r9d, 16
       jne      SHORT G_M000_IG70
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG70:                ;; offset=0x05DC
       cmp      r9d, 47
       jne      SHORT G_M000_IG71
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG71:                ;; offset=0x05E8
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG72
       mov      r9d, -1
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x05FD
       mov      r9d, 1
 
G_M000_IG73:                ;; offset=0x0603
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG86
 
G_M000_IG74:                ;; offset=0x0621
       mov      ecx, esi
       test     r9d, r9d
       je       SHORT G_M000_IG75
       cmp      r9d, 16
       jne      SHORT G_M000_IG76
 
G_M000_IG75:                ;; offset=0x062E
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG76:                ;; offset=0x0636
       cmp      r9d, 31
       je       SHORT G_M000_IG77
       cmp      r9d, 47
       jne      SHORT G_M000_IG78
 
G_M000_IG77:                ;; offset=0x0642
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG78:                ;; offset=0x0648
       add      r9d, -16
       mov      esi, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x68], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG79
       mov      esi, -1
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x068A
       mov      esi, 1
 
G_M000_IG80:                ;; offset=0x068F
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x68]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG86
 
G_M000_IG81:                ;; offset=0x06A7
       mov      ecx, esi
       test     r9d, r9d
       jne      SHORT G_M000_IG82
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG82:                ;; offset=0x06B6
       cmp      r9d, 31
       jne      SHORT G_M000_IG83
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG83:                ;; offset=0x06C2
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG84
       mov      esi, -1
       jmp      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x06D7
       mov      esi, 1
 
G_M000_IG85:                ;; offset=0x06DC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG86:                ;; offset=0x06ED
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rdi
       mov      dword ptr [r9+0x08], r8d
       mov      byte  ptr [r9+0x0C], dl
       mov      r12d, 1
       jmp      SHORT G_M000_IG88
 
G_M000_IG87:                ;; offset=0x0704
       xor      r12d, r12d
 
G_M000_IG88:                ;; offset=0x0707
       and      r13d, r12d
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG116
 
G_M000_IG89:                ;; offset=0x0715
       mov      rdx, bword ptr [rbp-0x50]
       mov      r9, qword ptr [rdx]
       mov      r8d, dword ptr [rdx+0x08]
       movzx    rdx, byte  ptr [rdx+0x0C]
       mov      edi, edx
       and      edi, 3
       cmp      edi, 1
       jne      G_M000_IG116
       mov      rdi, 0x7FFFFFFFFFFFFFFE
       cmp      r9, rdi
       jg       G_M000_IG116
       lea      rdi, [r9+0x01]
       inc      r8d
       mov      rsi, r9
       sar      rsi, 63
       and      rsi, 63
       add      rsi, r9
       sar      rsi, 6
       shl      rsi, 6
       sub      r9, rsi
       jns      SHORT G_M000_IG90
       add      r9, 64
 
G_M000_IG90:                ;; offset=0x076B
       mov      esi, 64
       test     r9d, r9d
       jne      SHORT G_M000_IG91
       mov      esi, 68
 
G_M000_IG91:                ;; offset=0x077A
       cmp      r9d, 63
       jne      SHORT G_M000_IG92
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG92:                ;; offset=0x0787
       xor      ecx, ecx
       mov      dword ptr [rbp-0x70], ecx
       cmp      r9d, 16
       jb       G_M000_IG110
       cmp      r9d, 32
       jb       G_M000_IG103
       cmp      r9d, 32
       jb       SHORT G_M000_IG93
       cmp      r9d, 48
       jb       SHORT G_M000_IG98
 
G_M000_IG93:                ;; offset=0x07AC
       cmp      r9d, 48
       jb       G_M000_IG115
       cmp      r9d, 64
       jae      G_M000_IG115
       mov      ecx, esi
       cmp      r9d, 48
       jne      SHORT G_M000_IG94
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG94:                ;; offset=0x07D0
       cmp      r9d, 63
       jne      SHORT G_M000_IG95
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG95:                ;; offset=0x07DC
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG96
       mov      r9d, -1
       jmp      SHORT G_M000_IG97
 
G_M000_IG96:                ;; offset=0x07F1
       mov      r9d, 1
 
G_M000_IG97:                ;; offset=0x07F7
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG115
 
G_M000_IG98:                ;; offset=0x0815
       mov      ecx, esi
       cmp      r9d, 16
       jne      SHORT G_M000_IG99
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG99:                ;; offset=0x0825
       cmp      r9d, 47
       jne      SHORT G_M000_IG100
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG100:                ;; offset=0x0831
       mov      rsi, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rsi]
       test     cl, 128
       je       SHORT G_M000_IG101
       mov      r9d, -1
       jmp      SHORT G_M000_IG102
 
G_M000_IG101:                ;; offset=0x0846
       mov      r9d, 1
 
G_M000_IG102:                ;; offset=0x084C
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG115
 
G_M000_IG103:                ;; offset=0x086A
       mov      ecx, esi
       test     r9d, r9d
       je       SHORT G_M000_IG104
       cmp      r9d, 16
       jne      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x0877
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG105:                ;; offset=0x087F
       cmp      r9d, 31
       je       SHORT G_M000_IG106
       cmp      r9d, 47
       jne      SHORT G_M000_IG107
 
G_M000_IG106:                ;; offset=0x088B
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG107:                ;; offset=0x0891
       add      r9d, -16
       mov      esi, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x70], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG108
       mov      esi, -1
       jmp      SHORT G_M000_IG109
 
G_M000_IG108:                ;; offset=0x08D3
       mov      esi, 1
 
G_M000_IG109:                ;; offset=0x08D8
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x70]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG115
 
G_M000_IG110:                ;; offset=0x08F0
       mov      ecx, esi
       test     r9d, r9d
       jne      SHORT G_M000_IG111
       mov      ecx, esi
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG111:                ;; offset=0x08FF
       cmp      r9d, 31
       jne      SHORT G_M000_IG112
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG112:                ;; offset=0x090B
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG113
       mov      esi, -1
       jmp      SHORT G_M000_IG114
 
G_M000_IG113:                ;; offset=0x0920
       mov      esi, 1
 
G_M000_IG114:                ;; offset=0x0925
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG115:                ;; offset=0x0936
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rdi
       mov      dword ptr [r9+0x08], r8d
       mov      byte  ptr [r9+0x0C], dl
       mov      r12d, 1
       jmp      SHORT G_M000_IG117
 
G_M000_IG116:                ;; offset=0x094D
       xor      r12d, r12d
 
G_M000_IG117:                ;; offset=0x0950
       and      r13d, r12d
       xor      edx, edx
       mov      qword ptr [rbp-0x78], rdx
 
G_M000_IG118:                ;; offset=0x0959
       mov      dword ptr [rbp-0x80], edx
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG120
 
G_M000_IG119:                ;; offset=0x0963
       mov      rdx, bword ptr [rbp-0x50]
       mov      rdi, qword ptr [rdx]
       mov      esi, dword ptr [rdx+0x08]
       movzx    r12, byte  ptr [rdx+0x0C]
       lea      rdx, [rbp-0x80]
       mov      qword ptr [rsp], rdx
       movzx    rdx, r12b
       lea      r9, [rbp-0x78]
       lea      r8, [rbp-0x48]
       mov      ecx, 1
       call     [SumTimeline:SeekCore(long,uint,byte,int,byref,byref,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG120
       mov      rax, bword ptr [rbp-0x50]
       mov      rcx, qword ptr [rbp-0x78]
       mov      edx, dword ptr [rbp-0x80]
       movzx    rdi, r12b
       mov      qword ptr [rax], rcx
       mov      dword ptr [rax+0x08], edx
       mov      byte  ptr [rax+0x0C], dil
       mov      eax, 1
       jmp      SHORT G_M000_IG121
 
G_M000_IG120:                ;; offset=0x09B5
       xor      eax, eax
 
G_M000_IG121:                ;; offset=0x09B7
       and      al, r13b
       setne    al
       movzx    rax, al
       add      r15d, eax
       dec      r14d
       jne      G_M000_IG05
 
G_M000_IG122:                ;; offset=0x09CC
       mov      rax, qword ptr [rbp-0x40]
       mov      ecx, dword ptr [rbp-0x38]
       movzx    rdx, byte  ptr [rbp-0x34]
       vmovss   xmm0, dword ptr [rbp-0x2C]
       mov      qword ptr [rbx], rax
       mov      dword ptr [rbx+0x08], ecx
       mov      byte  ptr [rbx+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [rbx+0x10], eax
       mov      dword ptr [rbx+0x14], r15d
       mov      rax, rbx
 
G_M000_IG123:                ;; offset=0x09F3
       add      rsp, 104
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2562

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[RepeatedPositiveOneFive]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 12 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x70]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0029
       xor      esi, esi
       mov      dword ptr [rbp-0x2C], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x58]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0044
       xor      ecx, ecx
       mov      qword ptr [rbp-0x40], rcx
 
G_M000_IG04:                ;; offset=0x004A
       mov      dword ptr [rbp-0x38], ecx
       mov      word  ptr [rbp-0x34], r14w
       mov      byte  ptr [rbp-0x32], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0058
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x40], xmm0
 
G_M000_IG06:                ;; offset=0x0061
       lea      rcx, bword ptr [rbp-0x40]
       mov      bword ptr [rbp-0x50], rcx
       lea      rcx, bword ptr [rbp-0x2C]
       mov      bword ptr [rbp-0x48], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x007A
       movzx    r12, word  ptr [rbx+0x0C]
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0086
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, 1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x009E
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x00A0
       mov      dword ptr [rbp-0x5C], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00AA
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, 1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00C4
       xor      ecx, ecx
 
G_M000_IG13:                ;; offset=0x00C6
       mov      eax, ecx
       and      eax, dword ptr [rbp-0x5C]
       mov      dword ptr [rbp-0x60], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x00D5
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, 1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x00EF
       xor      ecx, ecx
 
G_M000_IG16:                ;; offset=0x00F1
       mov      eax, ecx
       and      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x64], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0100
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, 1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x011A
       xor      ecx, ecx
 
G_M000_IG19:                ;; offset=0x011C
       mov      eax, ecx
       and      eax, dword ptr [rbp-0x64]
       mov      dword ptr [rbp-0x68], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x012B
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, 1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x0145
       xor      ecx, ecx
 
G_M000_IG22:                ;; offset=0x0147
       mov      eax, dword ptr [rbp-0x68]
       and      al, cl
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      G_M000_IG07
 
G_M000_IG23:                ;; offset=0x015E
       mov      rax, qword ptr [rbp-0x40]
       mov      ecx, dword ptr [rbp-0x38]
       movzx    rdx, byte  ptr [rbp-0x32]
       vmovss   xmm0, dword ptr [rbp-0x2C]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG24:                ;; offset=0x0188
       add      rsp, 72
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 407

; Assembly listing for method SignedSeekBenchmarks:RunDirect[RepeatedNegativeOneFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, 5
       jmp      G_M000_IG16
       align    [0 bytes for IG04]
 
G_M000_IG04:                ;; offset=0x0027
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:                ;; offset=0x0030
       lea      r10d, [rsi-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x005C
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG07:                ;; offset=0x0063
       cmp      esi, 48
       ja       SHORT G_M000_IG10
 
G_M000_IG08:                ;; offset=0x0068
       mov      r10, 0x1000000010001
       bt       r10, rsi
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0078
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG10:                ;; offset=0x0080
       cmp      esi, 63
       ja       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x0085
       mov      r10, 0x7FFF7FFF7FFFFFFF
       bt       r10, rsi
       jb       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0095
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG13:                ;; offset=0x009D
       test     r9b, 128
       je       G_M000_IG26
 
G_M000_IG14:                ;; offset=0x00A7
       mov      esi, -1
 
G_M000_IG15:                ;; offset=0x00AC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       dec      ecx
       mov      rsi, r8
 
G_M000_IG16:                ;; offset=0x00C2
       lea      r8, [rsi-0x01]
       test     rsi, rsi
       je       SHORT G_M000_IG27
 
G_M000_IG17:                ;; offset=0x00CB
       xor      esi, esi
       mov      dword ptr [rbp-0x08], esi
       dec      rax
       mov      rsi, rax
       mov      r9, rsi
       sar      r9, 63
       and      r9, 63
       add      r9, rsi
       sar      r9, 6
       shl      r9, 6
       sub      rsi, r9
       jns      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x00F1
       add      rsi, 64
 
G_M000_IG19:                ;; offset=0x00F5
       mov      r9d, 192
       test     esi, esi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x00FF
       mov      r9d, 196
 
G_M000_IG21:                ;; offset=0x0105
       cmp      esi, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x010A
       or       r9d, 8
       movzx    r9, r9b
 
G_M000_IG23:                ;; offset=0x0112
       cmp      esi, 16
       jb       G_M000_IG06
 
G_M000_IG24:                ;; offset=0x011B
       cmp      esi, 32
       jb       G_M000_IG05
       cmp      esi, 48
       jb       G_M000_IG04
 
G_M000_IG25:                ;; offset=0x012D
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      G_M000_IG07
 
G_M000_IG26:                ;; offset=0x0139
       mov      esi, 1
       jmp      G_M000_IG15
 
G_M000_IG27:                ;; offset=0x0143
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG28:                ;; offset=0x014B
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG29:                ;; offset=0x0166
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 364

; Assembly listing for method SignedSeekBenchmarks:RunTyped[RepeatedNegativeOneFive]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 134 single block inlinees; 29 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 104
       lea      rbp, [rsp+0x90]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x002D
       xor      r8d, r8d
       mov      dword ptr [rbp-0x2C], r8d
 
G_M000_IG03:                ;; offset=0x0034
       mov      qword ptr [rbp-0x40], r8
 
G_M000_IG04:                ;; offset=0x0038
       mov      dword ptr [rbp-0x38], r8d
       mov      byte  ptr [rbp-0x34], 1
       lea      r8, bword ptr [rbp-0x40]
       mov      bword ptr [rbp-0x50], r8
       lea      r8, bword ptr [rbp-0x2C]
       mov      bword ptr [rbp-0x48], r8
       xor      r15d, r15d
       mov      r14d, 0x1000
 
G_M000_IG05:                ;; offset=0x0059
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG33
 
G_M000_IG06:                ;; offset=0x0064
       mov      r8, bword ptr [rbp-0x50]
       mov      r9, qword ptr [r8]
       mov      edi, dword ptr [r8+0x08]
       movzx    r8, byte  ptr [r8+0x0C]
       mov      esi, r8d
       and      esi, 3
       cmp      esi, 1
       jne      G_M000_IG33
       mov      rsi, 0x8000000000000001
       cmp      r9, rsi
       jl       G_M000_IG33
       lea      rsi, [r9-0x01]
       dec      edi
       mov      rdx, r9
       sar      rdx, 63
       and      rdx, 63
       add      rdx, r9
       sar      rdx, 6
       shl      rdx, 6
       sub      r9, rdx
       jns      SHORT G_M000_IG07
       add      r9, 64
 
G_M000_IG07:                ;; offset=0x00BB
       test     r9d, r9d
       je       SHORT G_M000_IG08
       dec      r9d
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x00C5
       mov      r9d, 63
 
G_M000_IG09:                ;; offset=0x00CB
       mov      edx, 192
       test     r9d, r9d
       jne      SHORT G_M000_IG10
       mov      edx, 196
 
G_M000_IG10:                ;; offset=0x00DA
       cmp      r9d, 63
       jne      SHORT G_M000_IG11
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG11:                ;; offset=0x00E6
       xor      ecx, ecx
       mov      dword ptr [rbp-0x58], ecx
       cmp      r9d, 16
       jb       G_M000_IG28
       cmp      r9d, 32
       jb       G_M000_IG22
       cmp      r9d, 32
       jb       SHORT G_M000_IG12
       cmp      r9d, 48
       jb       SHORT G_M000_IG17
 
G_M000_IG12:                ;; offset=0x010B
       cmp      r9d, 48
       jb       G_M000_IG32
       cmp      r9d, 64
       jae      G_M000_IG32
       mov      ecx, edx
       cmp      r9d, 48
       jne      SHORT G_M000_IG13
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG13:                ;; offset=0x012F
       cmp      r9d, 63
       jne      SHORT G_M000_IG14
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG14:                ;; offset=0x013B
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG15
       mov      r9d, -1
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0150
       mov      r9d, 1
 
G_M000_IG16:                ;; offset=0x0156
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG32
 
G_M000_IG17:                ;; offset=0x0174
       mov      ecx, edx
       cmp      r9d, 16
       jne      SHORT G_M000_IG18
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG18:                ;; offset=0x0184
       cmp      r9d, 47
       jne      SHORT G_M000_IG19
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG19:                ;; offset=0x0190
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG20
       mov      r9d, -1
       jmp      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x01A5
       mov      r9d, 1
 
G_M000_IG21:                ;; offset=0x01AB
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG32
 
G_M000_IG22:                ;; offset=0x01C9
       mov      ecx, edx
       test     r9d, r9d
       je       SHORT G_M000_IG23
       cmp      r9d, 16
       jne      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x01D6
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG24:                ;; offset=0x01DE
       cmp      r9d, 31
       jne      SHORT G_M000_IG25
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG25:                ;; offset=0x01EA
       lea      edx, [r9-0x10]
       mov      r9d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r9
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x58], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG26
       mov      edx, -1
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x022C
       mov      edx, 1
 
G_M000_IG27:                ;; offset=0x0231
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x58]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG32
 
G_M000_IG28:                ;; offset=0x0249
       mov      ecx, edx
       test     r9d, r9d
       jne      SHORT G_M000_IG29
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG29:                ;; offset=0x0258
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG30
       mov      edx, -1
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x026D
       mov      edx, 1
 
G_M000_IG31:                ;; offset=0x0272
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG32:                ;; offset=0x0283
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rsi
       mov      dword ptr [r9+0x08], edi
       mov      byte  ptr [r9+0x0C], r8b
       mov      r13d, 1
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x029A
       xor      r13d, r13d
 
G_M000_IG34:                ;; offset=0x029D
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG62
 
G_M000_IG35:                ;; offset=0x02A8
       mov      r8, bword ptr [rbp-0x50]
       mov      r9, qword ptr [r8]
       mov      edi, dword ptr [r8+0x08]
       movzx    r8, byte  ptr [r8+0x0C]
       mov      esi, r8d
       and      esi, 3
       cmp      esi, 1
       jne      G_M000_IG62
       mov      rsi, 0x8000000000000001
       cmp      r9, rsi
       jl       G_M000_IG62
       lea      rsi, [r9-0x01]
       dec      edi
       mov      rdx, r9
       sar      rdx, 63
       and      rdx, 63
       add      rdx, r9
       sar      rdx, 6
       shl      rdx, 6
       sub      r9, rdx
       jns      SHORT G_M000_IG36
       add      r9, 64
 
G_M000_IG36:                ;; offset=0x02FF
       test     r9d, r9d
       je       SHORT G_M000_IG37
       dec      r9d
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x0309
       mov      r9d, 63
 
G_M000_IG38:                ;; offset=0x030F
       mov      edx, 192
       test     r9d, r9d
       jne      SHORT G_M000_IG39
       mov      edx, 196
 
G_M000_IG39:                ;; offset=0x031E
       cmp      r9d, 63
       jne      SHORT G_M000_IG40
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG40:                ;; offset=0x032A
       xor      ecx, ecx
       mov      dword ptr [rbp-0x60], ecx
       cmp      r9d, 16
       jb       G_M000_IG57
       cmp      r9d, 32
       jb       G_M000_IG51
       cmp      r9d, 32
       jb       SHORT G_M000_IG41
       cmp      r9d, 48
       jb       SHORT G_M000_IG46
 
G_M000_IG41:                ;; offset=0x034F
       cmp      r9d, 48
       jb       G_M000_IG61
       cmp      r9d, 64
       jae      G_M000_IG61
       mov      ecx, edx
       cmp      r9d, 48
       jne      SHORT G_M000_IG42
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG42:                ;; offset=0x0373
       cmp      r9d, 63
       jne      SHORT G_M000_IG43
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG43:                ;; offset=0x037F
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG44
       mov      r9d, -1
       jmp      SHORT G_M000_IG45
 
G_M000_IG44:                ;; offset=0x0394
       mov      r9d, 1
 
G_M000_IG45:                ;; offset=0x039A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG61
 
G_M000_IG46:                ;; offset=0x03B8
       mov      ecx, edx
       cmp      r9d, 16
       jne      SHORT G_M000_IG47
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG47:                ;; offset=0x03C8
       cmp      r9d, 47
       jne      SHORT G_M000_IG48
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG48:                ;; offset=0x03D4
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG49
       mov      r9d, -1
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:                ;; offset=0x03E9
       mov      r9d, 1
 
G_M000_IG50:                ;; offset=0x03EF
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG61
 
G_M000_IG51:                ;; offset=0x040D
       mov      ecx, edx
       test     r9d, r9d
       je       SHORT G_M000_IG52
       cmp      r9d, 16
       jne      SHORT G_M000_IG53
 
G_M000_IG52:                ;; offset=0x041A
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG53:                ;; offset=0x0422
       cmp      r9d, 31
       jne      SHORT G_M000_IG54
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG54:                ;; offset=0x042E
       lea      edx, [r9-0x10]
       mov      r9d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r9
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x60], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG55
       mov      edx, -1
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:                ;; offset=0x0470
       mov      edx, 1
 
G_M000_IG56:                ;; offset=0x0475
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x60]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG61
 
G_M000_IG57:                ;; offset=0x048D
       mov      ecx, edx
       test     r9d, r9d
       jne      SHORT G_M000_IG58
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG58:                ;; offset=0x049C
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG59
       mov      edx, -1
       jmp      SHORT G_M000_IG60
 
G_M000_IG59:                ;; offset=0x04B1
       mov      edx, 1
 
G_M000_IG60:                ;; offset=0x04B6
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG61:                ;; offset=0x04C7
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rsi
       mov      dword ptr [r9+0x08], edi
       mov      byte  ptr [r9+0x0C], r8b
       mov      r12d, 1
       jmp      SHORT G_M000_IG63
 
G_M000_IG62:                ;; offset=0x04DE
       xor      r12d, r12d
 
G_M000_IG63:                ;; offset=0x04E1
       and      r13d, r12d
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG93
 
G_M000_IG64:                ;; offset=0x04EF
       mov      r8, bword ptr [rbp-0x50]
       mov      r9, qword ptr [r8]
       mov      edi, dword ptr [r8+0x08]
       movzx    r8, byte  ptr [r8+0x0C]
       mov      esi, r8d
       and      esi, 3
       cmp      esi, 1
       jne      G_M000_IG93
       mov      rsi, 0x8000000000000001
       cmp      r9, rsi
       jl       G_M000_IG93
       lea      rsi, [r9-0x01]
       dec      edi
       mov      rdx, r9
       sar      rdx, 63
       and      rdx, 63
       add      rdx, r9
       sar      rdx, 6
       shl      rdx, 6
       sub      r9, rdx
       jns      SHORT G_M000_IG65
       add      r9, 64
 
G_M000_IG65:                ;; offset=0x0546
       test     r9d, r9d
       je       SHORT G_M000_IG66
       dec      r9d
       jmp      SHORT G_M000_IG67
 
G_M000_IG66:                ;; offset=0x0550
       mov      r9d, 63
 
G_M000_IG67:                ;; offset=0x0556
       mov      edx, 192
       test     r9d, r9d
       jne      SHORT G_M000_IG68
       mov      edx, 196
 
G_M000_IG68:                ;; offset=0x0565
       cmp      r9d, 63
       jne      SHORT G_M000_IG69
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG69:                ;; offset=0x0571
       xor      ecx, ecx
       mov      dword ptr [rbp-0x68], ecx
       cmp      r9d, 16
       jb       G_M000_IG87
       cmp      r9d, 32
       jb       G_M000_IG80
       cmp      r9d, 32
       jb       SHORT G_M000_IG70
       cmp      r9d, 48
       jb       SHORT G_M000_IG75
 
G_M000_IG70:                ;; offset=0x0596
       cmp      r9d, 48
       jb       G_M000_IG92
       cmp      r9d, 64
       jae      G_M000_IG92
       mov      ecx, edx
       cmp      r9d, 48
       jne      SHORT G_M000_IG71
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG71:                ;; offset=0x05BA
       cmp      r9d, 63
       jne      SHORT G_M000_IG72
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG72:                ;; offset=0x05C6
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG73
       mov      r9d, -1
       jmp      SHORT G_M000_IG74
 
G_M000_IG73:                ;; offset=0x05DB
       mov      r9d, 1
 
G_M000_IG74:                ;; offset=0x05E1
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG92
 
G_M000_IG75:                ;; offset=0x05FF
       mov      ecx, edx
       cmp      r9d, 16
       jne      SHORT G_M000_IG76
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG76:                ;; offset=0x060F
       cmp      r9d, 47
       jne      SHORT G_M000_IG77
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG77:                ;; offset=0x061B
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG78
       mov      r9d, -1
       jmp      SHORT G_M000_IG79
 
G_M000_IG78:                ;; offset=0x0630
       mov      r9d, 1
 
G_M000_IG79:                ;; offset=0x0636
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG92
 
G_M000_IG80:                ;; offset=0x0654
       mov      ecx, edx
       test     r9d, r9d
       je       SHORT G_M000_IG81
       cmp      r9d, 16
       jne      SHORT G_M000_IG82
 
G_M000_IG81:                ;; offset=0x0661
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG82:                ;; offset=0x0669
       cmp      r9d, 31
       je       SHORT G_M000_IG83
       cmp      r9d, 47
       jne      SHORT G_M000_IG84
 
G_M000_IG83:                ;; offset=0x0675
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG84:                ;; offset=0x067B
       lea      edx, [r9-0x10]
       mov      r9d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r9
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x68], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG85
       mov      edx, -1
       jmp      SHORT G_M000_IG86
 
G_M000_IG85:                ;; offset=0x06BD
       mov      edx, 1
 
G_M000_IG86:                ;; offset=0x06C2
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x68]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG92
 
G_M000_IG87:                ;; offset=0x06DA
       mov      ecx, edx
       test     r9d, r9d
       jne      SHORT G_M000_IG88
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG88:                ;; offset=0x06E9
       cmp      r9d, 31
       jne      SHORT G_M000_IG89
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG89:                ;; offset=0x06F5
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG90
       mov      edx, -1
       jmp      SHORT G_M000_IG91
 
G_M000_IG90:                ;; offset=0x070A
       mov      edx, 1
 
G_M000_IG91:                ;; offset=0x070F
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG92:                ;; offset=0x0720
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rsi
       mov      dword ptr [r9+0x08], edi
       mov      byte  ptr [r9+0x0C], r8b
       mov      r12d, 1
       jmp      SHORT G_M000_IG94
 
G_M000_IG93:                ;; offset=0x0737
       xor      r12d, r12d
 
G_M000_IG94:                ;; offset=0x073A
       and      r13d, r12d
       cmp      bword ptr [rbp-0x50], 0
       je       G_M000_IG124
 
G_M000_IG95:                ;; offset=0x0748
       mov      r8, bword ptr [rbp-0x50]
       mov      r9, qword ptr [r8]
       mov      edi, dword ptr [r8+0x08]
       movzx    r8, byte  ptr [r8+0x0C]
       mov      esi, r8d
       and      esi, 3
       cmp      esi, 1
       jne      G_M000_IG124
       mov      rsi, 0x8000000000000001
       cmp      r9, rsi
       jl       G_M000_IG124
       lea      rsi, [r9-0x01]
       dec      edi
       mov      rdx, r9
       sar      rdx, 63
       and      rdx, 63
       add      rdx, r9
       sar      rdx, 6
       shl      rdx, 6
       sub      r9, rdx
       jns      SHORT G_M000_IG96
       add      r9, 64
 
G_M000_IG96:                ;; offset=0x079F
       test     r9d, r9d
       je       SHORT G_M000_IG97
       dec      r9d
       jmp      SHORT G_M000_IG98
 
G_M000_IG97:                ;; offset=0x07A9
       mov      r9d, 63
 
G_M000_IG98:                ;; offset=0x07AF
       mov      edx, 192
       test     r9d, r9d
       jne      SHORT G_M000_IG99
       mov      edx, 196
 
G_M000_IG99:                ;; offset=0x07BE
       cmp      r9d, 63
       jne      SHORT G_M000_IG100
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG100:                ;; offset=0x07CA
       xor      ecx, ecx
       mov      dword ptr [rbp-0x70], ecx
       cmp      r9d, 16
       jb       G_M000_IG118
       cmp      r9d, 32
       jb       G_M000_IG111
       cmp      r9d, 32
       jb       SHORT G_M000_IG101
       cmp      r9d, 48
       jb       SHORT G_M000_IG106
 
G_M000_IG101:                ;; offset=0x07EF
       cmp      r9d, 48
       jb       G_M000_IG123
       cmp      r9d, 64
       jae      G_M000_IG123
       mov      ecx, edx
       cmp      r9d, 48
       jne      SHORT G_M000_IG102
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG102:                ;; offset=0x0813
       cmp      r9d, 63
       jne      SHORT G_M000_IG103
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG103:                ;; offset=0x081F
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG104
       mov      r9d, -1
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x0834
       mov      r9d, 1
 
G_M000_IG105:                ;; offset=0x083A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG123
 
G_M000_IG106:                ;; offset=0x0858
       mov      ecx, edx
       cmp      r9d, 16
       jne      SHORT G_M000_IG107
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG107:                ;; offset=0x0868
       cmp      r9d, 47
       jne      SHORT G_M000_IG108
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG108:                ;; offset=0x0874
       mov      rdx, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [rdx]
       test     cl, 128
       je       SHORT G_M000_IG109
       mov      r9d, -1
       jmp      SHORT G_M000_IG110
 
G_M000_IG109:                ;; offset=0x0889
       mov      r9d, 1
 
G_M000_IG110:                ;; offset=0x088F
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG123
 
G_M000_IG111:                ;; offset=0x08AD
       mov      ecx, edx
       test     r9d, r9d
       je       SHORT G_M000_IG112
       cmp      r9d, 16
       jne      SHORT G_M000_IG113
 
G_M000_IG112:                ;; offset=0x08BA
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG113:                ;; offset=0x08C2
       cmp      r9d, 31
       je       SHORT G_M000_IG114
       cmp      r9d, 47
       jne      SHORT G_M000_IG115
 
G_M000_IG114:                ;; offset=0x08CE
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG115:                ;; offset=0x08D4
       lea      edx, [r9-0x10]
       mov      r9d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r9
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x70], xmm0
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG116
       mov      edx, -1
       jmp      SHORT G_M000_IG117
 
G_M000_IG116:                ;; offset=0x0916
       mov      edx, 1
 
G_M000_IG117:                ;; offset=0x091B
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x70]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
       jmp      SHORT G_M000_IG123
 
G_M000_IG118:                ;; offset=0x0933
       mov      ecx, edx
       test     r9d, r9d
       jne      SHORT G_M000_IG119
       mov      ecx, edx
       or       ecx, 1
       movzx    rcx, cl
 
G_M000_IG119:                ;; offset=0x0942
       cmp      r9d, 31
       jne      SHORT G_M000_IG120
       or       ecx, 2
       movzx    rcx, cl
 
G_M000_IG120:                ;; offset=0x094E
       mov      r9, bword ptr [rbp-0x48]
       vmovss   xmm0, dword ptr [r9]
       test     cl, 128
       je       SHORT G_M000_IG121
       mov      edx, -1
       jmp      SHORT G_M000_IG122
 
G_M000_IG121:                ;; offset=0x0963
       mov      edx, 1
 
G_M000_IG122:                ;; offset=0x0968
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [r9], xmm0
 
G_M000_IG123:                ;; offset=0x0979
       mov      r9, bword ptr [rbp-0x50]
       mov      qword ptr [r9], rsi
       mov      dword ptr [r9+0x08], edi
       mov      byte  ptr [r9+0x0C], r8b
       mov      r12d, 1
       jmp      SHORT G_M000_IG125
 
G_M000_IG124:                ;; offset=0x0990
       xor      r12d, r12d
 
G_M000_IG125:                ;; offset=0x0993
       and      r13d, r12d
       xor      r8d, r8d
       mov      qword ptr [rbp-0x78], r8
 
G_M000_IG126:                ;; offset=0x099D
       mov      dword ptr [rbp-0x80], r8d
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG128
 
G_M000_IG127:                ;; offset=0x09A8
       mov      r8, bword ptr [rbp-0x50]
       mov      rdi, qword ptr [r8]
       mov      esi, dword ptr [r8+0x08]
       movzx    r12, byte  ptr [r8+0x0C]
       lea      r8, [rbp-0x80]
       mov      qword ptr [rsp], r8
       lea      r8, [rbp-0x48]
       lea      r9, [rbp-0x78]
       mov      edx, r12d
       mov      ecx, -1
       call     [SumTimeline:SeekCore(long,uint,byte,int,byref,byref,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG128
       mov      rax, bword ptr [rbp-0x50]
       mov      rcx, qword ptr [rbp-0x78]
       mov      edx, dword ptr [rbp-0x80]
       mov      qword ptr [rax], rcx
       mov      dword ptr [rax+0x08], edx
       mov      byte  ptr [rax+0x0C], r12b
       mov      eax, 1
       jmp      SHORT G_M000_IG129
 
G_M000_IG128:                ;; offset=0x09F6
       xor      eax, eax
 
G_M000_IG129:                ;; offset=0x09F8
       and      al, r13b
       setne    al
       movzx    rax, al
       add      r15d, eax
       dec      r14d
       jne      G_M000_IG05
 
G_M000_IG130:                ;; offset=0x0A0D
       mov      rax, qword ptr [rbp-0x40]
       mov      ecx, dword ptr [rbp-0x38]
       movzx    rdx, byte  ptr [rbp-0x34]
       vmovss   xmm0, dword ptr [rbp-0x2C]
       mov      qword ptr [rbx], rax
       mov      dword ptr [rbx+0x08], ecx
       mov      byte  ptr [rbx+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [rbx+0x10], eax
       mov      dword ptr [rbx+0x14], r15d
       mov      rax, rbx
 
G_M000_IG131:                ;; offset=0x0A34
       add      rsp, 104
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2627

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[RepeatedNegativeOneFive]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 12 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x70]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0029
       xor      esi, esi
       mov      dword ptr [rbp-0x2C], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x58]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0044
       xor      ecx, ecx
       mov      qword ptr [rbp-0x40], rcx
 
G_M000_IG04:                ;; offset=0x004A
       mov      dword ptr [rbp-0x38], ecx
       mov      word  ptr [rbp-0x34], r14w
       mov      byte  ptr [rbp-0x32], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0058
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x40], xmm0
 
G_M000_IG06:                ;; offset=0x0061
       lea      rcx, bword ptr [rbp-0x40]
       mov      bword ptr [rbp-0x50], rcx
       lea      rcx, bword ptr [rbp-0x2C]
       mov      bword ptr [rbp-0x48], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x007A
       movzx    r12, word  ptr [rbx+0x0C]
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0086
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, -1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x009E
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x00A0
       mov      dword ptr [rbp-0x5C], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00AA
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, -1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00C4
       xor      ecx, ecx
 
G_M000_IG13:                ;; offset=0x00C6
       mov      eax, ecx
       and      eax, dword ptr [rbp-0x5C]
       mov      dword ptr [rbp-0x60], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x00D5
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, -1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x00EF
       xor      ecx, ecx
 
G_M000_IG16:                ;; offset=0x00F1
       mov      eax, ecx
       and      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x64], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0100
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, -1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x011A
       xor      ecx, ecx
 
G_M000_IG19:                ;; offset=0x011C
       mov      eax, ecx
       and      eax, dword ptr [rbp-0x64]
       mov      dword ptr [rbp-0x68], eax
       cmp      bword ptr [rbp-0x50], 0
       je       SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x012B
       lea      rcx, [rbp-0x48]
       mov      edi, r12d
       mov      rsi, bword ptr [rbp-0x50]
       mov      edx, -1
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       mov      ecx, eax
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x0145
       xor      ecx, ecx
 
G_M000_IG22:                ;; offset=0x0147
       mov      eax, dword ptr [rbp-0x68]
       and      al, cl
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      G_M000_IG07
 
G_M000_IG23:                ;; offset=0x015E
       mov      rax, qword ptr [rbp-0x40]
       mov      ecx, dword ptr [rbp-0x38]
       movzx    rdx, byte  ptr [rbp-0x32]
       vmovss   xmm0, dword ptr [rbp-0x2C]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG24:                ;; offset=0x0188
       add      rsp, 72
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 407

; Assembly listing for method SignedSeekBenchmarks:RunDirect[LiteralPositiveSixtyFour]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, 64
       jmp      G_M000_IG16
       align    [0 bytes for IG04]
 
G_M000_IG04:                ;; offset=0x0027
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:                ;; offset=0x0030
       lea      r10d, [rsi-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r10
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x005C
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG07:                ;; offset=0x0063
       cmp      esi, 48
       ja       SHORT G_M000_IG10
 
G_M000_IG08:                ;; offset=0x0068
       mov      r10, 0x1000000010001
       bt       r10, rsi
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0078
       or       r9d, 1
       movzx    r9, r9b
 
G_M000_IG10:                ;; offset=0x0080
       cmp      esi, 63
       ja       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x0085
       mov      r10, 0x7FFF7FFF7FFFFFFF
       bt       r10, rsi
       jb       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0095
       or       r9d, 2
       movzx    r9, r9b
 
G_M000_IG13:                ;; offset=0x009D
       test     r9b, 128
       je       G_M000_IG26
 
G_M000_IG14:                ;; offset=0x00A7
       mov      esi, -1
 
G_M000_IG15:                ;; offset=0x00AC
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       inc      rax
       inc      ecx
       mov      rsi, r8
 
G_M000_IG16:                ;; offset=0x00C5
       lea      r8, [rsi-0x01]
       test     rsi, rsi
       je       SHORT G_M000_IG27
 
G_M000_IG17:                ;; offset=0x00CE
       xor      esi, esi
       mov      dword ptr [rbp-0x08], esi
       mov      rsi, rax
       sar      rsi, 63
       and      rsi, 63
       add      rsi, rax
       sar      rsi, 6
       shl      rsi, 6
       mov      r9, rax
       sub      r9, rsi
       jns      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x00F1
       add      r9, 64
 
G_M000_IG19:                ;; offset=0x00F5
       mov      esi, r9d
       mov      r9d, 64
       test     esi, esi
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x0102
       mov      r9d, 68
 
G_M000_IG21:                ;; offset=0x0108
       cmp      esi, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x010D
       or       r9d, 8
       movzx    r9, r9b
 
G_M000_IG23:                ;; offset=0x0115
       cmp      esi, 16
       jb       G_M000_IG06
 
G_M000_IG24:                ;; offset=0x011E
       cmp      esi, 32
       jb       G_M000_IG05
       cmp      esi, 48
       jb       G_M000_IG04
 
G_M000_IG25:                ;; offset=0x0130
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      G_M000_IG07
 
G_M000_IG26:                ;; offset=0x013C
       mov      esi, 1
       jmp      G_M000_IG15
 
G_M000_IG27:                ;; offset=0x0146
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG28:                ;; offset=0x014E
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG29:                ;; offset=0x0169
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 367

; Assembly listing for method SignedSeekBenchmarks:RunTyped[LiteralPositiveSixtyFour]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 39 single block inlinees; 7 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x30]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  xmmword ptr [rbp-0x28], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x18], rax
 
G_M000_IG02:                ;; offset=0x001B
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x0020
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0024
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002B
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x0030
       mov      rdx, qword ptr [rbp-0x20]
       mov      esi, dword ptr [rbp-0x18]
       movzx    r8, byte  ptr [rbp-0x14]
       mov      r9d, r8d
       and      r9d, 3
       cmp      r9d, 1
       jne      SHORT G_M000_IG07
       mov      r9, 0x7FFFFFFFFFFFFFBF
       cmp      rdx, r9
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0058
       xor      r9d, r9d
       jmp      G_M000_IG40
       align    [0 bytes for IG10]
 
G_M000_IG08:                ;; offset=0x0060
       lea      r9, [rdx+0x40]
       add      esi, 64
       mov      r10, rdx
       sar      r10, 63
       and      r10, 63
       add      r10, rdx
       sar      r10, 6
       shl      r10, 6
       mov      r11, rdx
       sub      r11, r10
       jns      SHORT G_M000_IG09
       add      r11, 64
 
G_M000_IG09:                ;; offset=0x0089
       mov      r10d, r11d
       cmp      rdx, r9
       jge      G_M000_IG39
 
G_M000_IG10:                ;; offset=0x0095
       mov      r11d, 64
       test     r10d, r10d
       jne      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00A0
       mov      r11d, 68
 
G_M000_IG12:                ;; offset=0x00A6
       cmp      r10d, 63
       jne      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00AC
       or       r11d, 8
       movzx    r11, r11b
 
G_M000_IG14:                ;; offset=0x00B4
       xor      ebx, ebx
       mov      dword ptr [rbp-0x28], ebx
       cmp      r10d, 16
       jb       G_M000_IG32
 
G_M000_IG15:                ;; offset=0x00C3
       cmp      r10d, 32
       jb       G_M000_IG26
       cmp      r10d, 32
       jb       SHORT G_M000_IG16
       cmp      r10d, 48
       jb       SHORT G_M000_IG21
 
G_M000_IG16:                ;; offset=0x00D9
       cmp      r10d, 48
       jb       G_M000_IG36
       cmp      r10d, 64
       jae      G_M000_IG36
       mov      ebx, r11d
       cmp      r10d, 48
       jne      SHORT G_M000_IG17
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG17:                ;; offset=0x00FE
       cmp      r10d, 63
       jne      SHORT G_M000_IG18
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG18:                ;; offset=0x010A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG19
       mov      r11d, -1
       jmp      SHORT G_M000_IG20
 
G_M000_IG19:                ;; offset=0x011C
       mov      r11d, 1
 
G_M000_IG20:                ;; offset=0x0122
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG36
 
G_M000_IG21:                ;; offset=0x0141
       mov      ebx, r11d
       cmp      r10d, 16
       jne      SHORT G_M000_IG22
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG22:                ;; offset=0x0152
       cmp      r10d, 47
       jne      SHORT G_M000_IG23
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x015E
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG24
       mov      r11d, -1
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0170
       mov      r11d, 1
 
G_M000_IG25:                ;; offset=0x0176
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG36
 
G_M000_IG26:                ;; offset=0x0195
       mov      ebx, r11d
       test     r10d, r10d
       je       SHORT G_M000_IG27
       cmp      r10d, 16
       jne      SHORT G_M000_IG28
 
G_M000_IG27:                ;; offset=0x01A3
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG28:                ;; offset=0x01AB
       cmp      r10d, 31
       jne      SHORT G_M000_IG29
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG29:                ;; offset=0x01B7
       lea      r11d, [r10-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG30
       mov      r11d, -1
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01F3
       mov      r11d, 1
 
G_M000_IG31:                ;; offset=0x01F9
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG36
 
G_M000_IG32:                ;; offset=0x0212
       mov      ebx, r11d
       test     r10d, r10d
       jne      SHORT G_M000_IG33
       or       r11d, 1
       movzx    rbx, r11b
 
G_M000_IG33:                ;; offset=0x0222
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG34
       mov      r11d, -1
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x0234
       mov      r11d, 1
 
G_M000_IG35:                ;; offset=0x023A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG36:                ;; offset=0x024C
       inc      rdx
       cmp      r10d, 63
       je       SHORT G_M000_IG37
       inc      r10d
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x025A
       xor      r10d, r10d
 
G_M000_IG38:                ;; offset=0x025D
       cmp      rdx, r9
       jl       G_M000_IG10
 
G_M000_IG39:                ;; offset=0x0266
       movzx    rdx, r8b
       mov      qword ptr [rbp-0x20], r9
       mov      dword ptr [rbp-0x18], esi
       mov      byte  ptr [rbp-0x14], dl
       mov      r9d, 1
 
G_M000_IG40:                ;; offset=0x027A
       test     r9d, r9d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG41:                ;; offset=0x028D
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG42:                ;; offset=0x02B5
       add      rsp, 40
       pop      rbx
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 700

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[LiteralPositiveSixtyFour]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0083
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       mov      edx, 64
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0098
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009A
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AA
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D4
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 225

; Assembly listing for method SignedSeekBenchmarks:RunDirect[RuntimePositiveSixtyFour]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x08], rax
 
G_M000_IG02:                ;; offset=0x0010
       xor      eax, eax
       xor      ecx, ecx
       vxorps   xmm0, xmm0, xmm0
       mov      edx, 0x1000
 
G_M000_IG03:                ;; offset=0x001D
       mov      esi, dword ptr [(reloc 0x7f804326b150)]
       test     esi, esi
       jle      SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0027
       mov      r8d, 1
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG10]
 
G_M000_IG05:                ;; offset=0x002F
       mov      r8d, -1
 
G_M000_IG06:                ;; offset=0x0035
       test     esi, esi
       jle      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0039
       mov      r9d, esi
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x003E
       movsxd   r9, esi
       neg      r9
 
G_M000_IG09:                ;; offset=0x0044
       jmp      G_M000_IG33
 
G_M000_IG10:                ;; offset=0x0049
       lea      r9, [rax-0x01]
 
G_M000_IG11:                ;; offset=0x004D
       mov      r10, r9
       sar      r10, 63
       and      r10, 63
       add      r10, r9
       sar      r10, 6
       shl      r10, 6
       sub      r9, r10
       jns      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0068
       add      r9, 64
 
G_M000_IG13:                ;; offset=0x006C
       mov      r10d, 64
       test     r8d, r8d
       jge      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x0077
       mov      r10d, 192
 
G_M000_IG15:                ;; offset=0x007D
       test     r9d, r9d
       jne      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x0082
       or       r10d, 4
       movzx    r10, r10b
 
G_M000_IG17:                ;; offset=0x008A
       cmp      r9d, 63
       jne      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0090
       or       r10d, 8
       movzx    r10, r10b
 
G_M000_IG19:                ;; offset=0x0098
       cmp      r9d, 16
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x009E
       cmp      r9d, 32
       jb       SHORT G_M000_IG22
       cmp      r9d, 48
       jb       SHORT G_M000_IG21
       mov      dword ptr [rbp-0x08], 0x40A00000
       jmp      SHORT G_M000_IG24
 
G_M000_IG21:                ;; offset=0x00B3
       mov      dword ptr [rbp-0x08], 0x40400000
       jmp      SHORT G_M000_IG24
 
G_M000_IG22:                ;; offset=0x00BC
       lea      r11d, [r9-0x10]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x08], xmm1
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x00E8
       mov      dword ptr [rbp-0x08], 0x3F800000
 
G_M000_IG24:                ;; offset=0x00EF
       cmp      r9d, 48
       ja       SHORT G_M000_IG27
 
G_M000_IG25:                ;; offset=0x00F5
       mov      r11, 0x1000000010001
       bt       r11, r9
       jae      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x0105
       or       r10d, 1
       movzx    r10, r10b
 
G_M000_IG27:                ;; offset=0x010D
       cmp      r9d, 63
       ja       SHORT G_M000_IG30
 
G_M000_IG28:                ;; offset=0x0113
       mov      r11, 0x7FFF7FFF7FFFFFFF
       bt       r11, r9
       jb       SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x0123
       or       r10d, 2
       movzx    r10, r10b
 
G_M000_IG30:                ;; offset=0x012B
       test     r10b, 128
       je       SHORT G_M000_IG36
 
G_M000_IG31:                ;; offset=0x0131
       mov      r9d, -1
 
G_M000_IG32:                ;; offset=0x0137
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9d
       vmulss   xmm1, xmm1, dword ptr [rbp-0x08]
       vaddss   xmm0, xmm1, xmm0
       movsxd   r9, r8d
       add      rax, r9
       add      ecx, r8d
       mov      r9, rsi
 
G_M000_IG33:                ;; offset=0x0155
       lea      rsi, [r9-0x01]
       test     r9, r9
       je       SHORT G_M000_IG37
 
G_M000_IG34:                ;; offset=0x015E
       xor      r9d, r9d
       mov      dword ptr [rbp-0x08], r9d
       test     r8d, r8d
       jle      G_M000_IG10
 
G_M000_IG35:                ;; offset=0x016E
       mov      r9, rax
       jmp      G_M000_IG11
 
G_M000_IG36:                ;; offset=0x0176
       mov      r9d, 1
       jmp      SHORT G_M000_IG32
 
G_M000_IG37:                ;; offset=0x017E
       dec      edx
       jne      G_M000_IG03
 
G_M000_IG38:                ;; offset=0x0186
       mov      qword ptr [rdi], rax
       mov      dword ptr [rdi+0x08], ecx
       mov      byte  ptr [rdi+0x0C], 1
       vmovd    eax, xmm0
       mov      dword ptr [rdi+0x10], eax
       mov      dword ptr [rdi+0x14], 0x1000
       mov      rax, rdi
 
G_M000_IG39:                ;; offset=0x01A1
       add      rsp, 16
       pop      rbp
       ret      
 
RWD00  	dd	41700000h		;        15
RWD04  	dd	40000000h		;         2
RWD08  	dd	3F800000h		;         1

; Total bytes of code 423

; Assembly listing for method SignedSeekBenchmarks:RunTyped[RuntimePositiveSixtyFour]():SumReceipt (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 126 single block inlinees; 22 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x40]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
 
G_M000_IG02:                ;; offset=0x001A
       xor      eax, eax
       mov      dword ptr [rbp-0x0C], eax
 
G_M000_IG03:                ;; offset=0x001F
       mov      qword ptr [rbp-0x20], rax
 
G_M000_IG04:                ;; offset=0x0023
       mov      dword ptr [rbp-0x18], eax
       mov      byte  ptr [rbp-0x14], 1
 
G_M000_IG05:                ;; offset=0x002A
       mov      ecx, 0x1000
 
G_M000_IG06:                ;; offset=0x002F
       mov      edx, dword ptr [(reloc 0x7f804326b150)]
       mov      rsi, qword ptr [rbp-0x20]
       mov      r8d, dword ptr [rbp-0x18]
       movzx    r9, byte  ptr [rbp-0x14]
       movsxd   r10, edx
       mov      r11d, r9d
       and      r11d, 3
       cmp      r11d, 1
       jne      SHORT G_M000_IG08
       test     r10, r10
       jle      SHORT G_M000_IG07
       mov      r11, r10
       neg      r11
       mov      rbx, 0x7FFFFFFFFFFFFFFF
       add      r11, rbx
       cmp      r11, rsi
       jl       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x006F
       test     r10, r10
       jge      SHORT G_M000_IG09
       mov      r11, r10
       neg      r11
       mov      rbx, 0x8000000000000000
       add      r11, rbx
       cmp      r11, rsi
       jle      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x008C
       xor      r11d, r11d
       jmp      G_M000_IG56
       align    [0 bytes for IG11]
 
G_M000_IG09:                ;; offset=0x0094
       add      r10, rsi
       add      r8d, edx
       test     edx, edx
       je       G_M000_IG55
       mov      r11, rsi
       sar      r11, 63
       and      r11, 63
       add      r11, rsi
       sar      r11, 6
       shl      r11, 6
       mov      rbx, rsi
       sub      rbx, r11
       jns      SHORT G_M000_IG10
       add      rbx, 64
 
G_M000_IG10:                ;; offset=0x00C4
       mov      r11d, ebx
       cmp      edx, 1
       je       G_M000_IG116
       cmp      edx, -1
       je       G_M000_IG91
       cmp      edx, 1
       jg       G_M000_IG59
       cmp      rsi, r10
       jle      G_M000_IG54
 
G_M000_IG11:                ;; offset=0x00EB
       test     r11d, r11d
       je       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00F0
       dec      r11d
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00F5
       mov      r11d, 63
 
G_M000_IG14:                ;; offset=0x00FB
       dec      rsi
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0108
       mov      edx, 196
 
G_M000_IG16:                ;; offset=0x010D
       cmp      r11d, 63
       jne      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0113
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG18:                ;; offset=0x0119
       xor      ebx, ebx
       mov      dword ptr [rbp-0x40], ebx
       cmp      r11d, 16
       jb       G_M000_IG45
       cmp      r11d, 32
       jb       G_M000_IG35
       cmp      r11d, 32
       jb       SHORT G_M000_IG19
       cmp      r11d, 48
       jb       SHORT G_M000_IG27
 
G_M000_IG19:                ;; offset=0x013E
       cmp      r11d, 48
       jb       G_M000_IG53
       cmp      r11d, 64
       jae      G_M000_IG53
       mov      ebx, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x015A
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG21:                ;; offset=0x0160
       cmp      r11d, 63
       jne      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x0166
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG23:                ;; offset=0x016C
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0176
       mov      edx, -1
       jmp      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x017D
       mov      edx, 1
 
G_M000_IG26:                ;; offset=0x0182
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG27:                ;; offset=0x01A0
       mov      ebx, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01A8
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG29:                ;; offset=0x01AE
       cmp      r11d, 47
       jne      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x01B4
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG31:                ;; offset=0x01BA
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x01C4
       mov      edx, -1
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x01CB
       mov      edx, 1
 
G_M000_IG34:                ;; offset=0x01D0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG53
 
G_M000_IG35:                ;; offset=0x01EE
       mov      ebx, edx
       test     r11d, r11d
       je       SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x01F5
       cmp      r11d, 16
       jne      SHORT G_M000_IG38
 
G_M000_IG37:                ;; offset=0x01FB
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG38:                ;; offset=0x0201
       cmp      r11d, 31
       je       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x0207
       cmp      r11d, 47
       jne      SHORT G_M000_IG41
 
G_M000_IG40:                ;; offset=0x020D
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG41:                ;; offset=0x0213
       lea      edx, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x40], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x0247
       mov      edx, -1
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:                ;; offset=0x024E
       mov      edx, 1
 
G_M000_IG44:                ;; offset=0x0253
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x40]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG45:                ;; offset=0x026B
       mov      ebx, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0272
       or       edx, 1
       movzx    rbx, dl
 
G_M000_IG47:                ;; offset=0x0278
       cmp      r11d, 31
       jne      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x027E
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG49:                ;; offset=0x0284
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG51
 
G_M000_IG50:                ;; offset=0x028E
       mov      edx, -1
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0295
       mov      edx, 1
 
G_M000_IG52:                ;; offset=0x029A
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG53:                ;; offset=0x02AB
       cmp      rsi, r10
       jg       G_M000_IG11
 
G_M000_IG54:                ;; offset=0x02B4
       movzx    rdx, r9b
       mov      qword ptr [rbp-0x20], r10
       mov      dword ptr [rbp-0x18], r8d
       mov      byte  ptr [rbp-0x14], dl
 
G_M000_IG55:                ;; offset=0x02C3
       mov      r11d, 1
 
G_M000_IG56:                ;; offset=0x02C9
       test     r11d, r11d
       setne    dl
       movzx    rdx, dl
       add      eax, edx
       dec      ecx
       jne      G_M000_IG06
 
G_M000_IG57:                ;; offset=0x02DC
       mov      rcx, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
       movzx    rsi, byte  ptr [rbp-0x14]
       vmovss   xmm0, dword ptr [rbp-0x0C]
       mov      qword ptr [rdi], rcx
       mov      dword ptr [rdi+0x08], edx
       mov      byte  ptr [rdi+0x0C], sil
       vmovd    ecx, xmm0
       mov      dword ptr [rdi+0x10], ecx
       mov      dword ptr [rdi+0x14], eax
       mov      rax, rdi
 
G_M000_IG58:                ;; offset=0x0304
       add      rsp, 56
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG59:                ;; offset=0x030B
       mov      rdx, rsi
       cmp      rdx, r10
       jl       G_M000_IG81
       jmp      SHORT G_M000_IG54
       align    [0 bytes for IG60]
 
G_M000_IG60:                ;; offset=0x0319
       mov      esi, 1
 
G_M000_IG61:                ;; offset=0x031E
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG62:                ;; offset=0x033C
       mov      ebx, esi
       cmp      r11d, 16
       jne      SHORT G_M000_IG63
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG63:                ;; offset=0x034B
       cmp      r11d, 47
       jne      SHORT G_M000_IG64
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG64:                ;; offset=0x0357
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG65
       mov      esi, -1
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x0368
       mov      esi, 1
 
G_M000_IG66:                ;; offset=0x036D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG78
 
G_M000_IG67:                ;; offset=0x038B
       mov      ebx, esi
       test     r11d, r11d
       je       SHORT G_M000_IG68
       cmp      r11d, 16
       jne      SHORT G_M000_IG69
 
G_M000_IG68:                ;; offset=0x0398
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG69:                ;; offset=0x039F
       cmp      r11d, 31
       je       SHORT G_M000_IG70
       cmp      r11d, 47
       jne      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x03AB
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG71:                ;; offset=0x03B1
       lea      esi, [r11-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG72
       mov      esi, -1
       jmp      SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x03EC
       mov      esi, 1
 
G_M000_IG73:                ;; offset=0x03F1
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vmulss   xmm1, xmm1, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG74:                ;; offset=0x0409
       mov      ebx, esi
       test     r11d, r11d
       jne      SHORT G_M000_IG75
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG75:                ;; offset=0x0417
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       SHORT G_M000_IG76
       mov      esi, -1
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:                ;; offset=0x0428
       mov      esi, 1
 
G_M000_IG77:                ;; offset=0x042D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, esi
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
 
G_M000_IG78:                ;; offset=0x043E
       inc      rdx
       cmp      r11d, 63
       je       SHORT G_M000_IG79
       inc      r11d
       jmp      SHORT G_M000_IG80
 
G_M000_IG79:                ;; offset=0x044C
       xor      r11d, r11d
 
G_M000_IG80:                ;; offset=0x044F
       cmp      rdx, r10
       jge      G_M000_IG54
 
G_M000_IG81:                ;; offset=0x0458
       mov      esi, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG83
 
G_M000_IG82:                ;; offset=0x0462
       mov      esi, 68
 
G_M000_IG83:                ;; offset=0x0467
       cmp      r11d, 63
       jne      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x046D
       or       esi, 8
       movzx    rsi, sil
 
G_M000_IG85:                ;; offset=0x0474
       xor      ebx, ebx
       mov      dword ptr [rbp-0x38], ebx
       cmp      r11d, 16
       jb       SHORT G_M000_IG74
 
G_M000_IG86:                ;; offset=0x047F
       cmp      r11d, 32
       jb       G_M000_IG67
       cmp      r11d, 32
       jb       SHORT G_M000_IG87
       cmp      r11d, 48
       jb       G_M000_IG62
 
G_M000_IG87:                ;; offset=0x0499
       cmp      r11d, 48
       jb       SHORT G_M000_IG78
       cmp      r11d, 64
       jae      SHORT G_M000_IG78
       mov      ebx, esi
       cmp      r11d, 48
       jne      SHORT G_M000_IG88
       or       esi, 1
       movzx    rbx, sil
 
G_M000_IG88:                ;; offset=0x04B4
       cmp      r11d, 63
       jne      SHORT G_M000_IG89
       or       ebx, 2
       movzx    rbx, bl
 
G_M000_IG89:                ;; offset=0x04C0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     bl, 128
       je       G_M000_IG60
 
G_M000_IG90:                ;; offset=0x04CE
       mov      esi, -1
       jmp      G_M000_IG61
 
G_M000_IG91:                ;; offset=0x04D8
       test     r11d, r11d
       je       SHORT G_M000_IG92
       dec      r11d
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x04E2
       mov      r11d, 63
 
G_M000_IG93:                ;; offset=0x04E8
       mov      edx, 192
       test     r11d, r11d
       jne      SHORT G_M000_IG94
       mov      edx, 196
 
G_M000_IG94:                ;; offset=0x04F7
       cmp      r11d, 63
       jne      SHORT G_M000_IG95
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG95:                ;; offset=0x0503
       xor      esi, esi
       mov      dword ptr [rbp-0x30], esi
       cmp      r11d, 16
       jb       G_M000_IG112
       cmp      r11d, 32
       jb       G_M000_IG106
       cmp      r11d, 32
       jb       SHORT G_M000_IG96
       cmp      r11d, 48
       jb       SHORT G_M000_IG101
 
G_M000_IG96:                ;; offset=0x0528
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG97
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG97:                ;; offset=0x054D
       cmp      r11d, 63
       jne      SHORT G_M000_IG98
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG98:                ;; offset=0x055A
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG99
       mov      edx, -1
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:                ;; offset=0x056C
       mov      edx, 1
 
G_M000_IG100:                ;; offset=0x0571
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG101:                ;; offset=0x058F
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG102
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG102:                ;; offset=0x05A0
       cmp      r11d, 47
       jne      SHORT G_M000_IG103
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG103:                ;; offset=0x05AD
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG104
       mov      edx, -1
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x05BF
       mov      edx, 1
 
G_M000_IG105:                ;; offset=0x05C4
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG106:                ;; offset=0x05E2
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG107
       cmp      r11d, 16
       jne      SHORT G_M000_IG108
 
G_M000_IG107:                ;; offset=0x05EF
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG108:                ;; offset=0x05F8
       cmp      r11d, 31
       jne      SHORT G_M000_IG109
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG109:                ;; offset=0x0605
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG110
       mov      edx, -1
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:                ;; offset=0x0644
       mov      edx, 1
 
G_M000_IG111:                ;; offset=0x0649
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x30]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG112:                ;; offset=0x0664
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG113
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG113:                ;; offset=0x0674
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG114
       mov      r11d, -1
       jmp      SHORT G_M000_IG115
 
G_M000_IG114:                ;; offset=0x0687
       mov      r11d, 1
 
G_M000_IG115:                ;; offset=0x068D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r11d
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG116:                ;; offset=0x06A4
       mov      edx, 64
       test     r11d, r11d
       jne      SHORT G_M000_IG117
       mov      edx, 68
 
G_M000_IG117:                ;; offset=0x06B3
       cmp      r11d, 63
       jne      SHORT G_M000_IG118
       or       edx, 8
       movzx    rdx, dl
 
G_M000_IG118:                ;; offset=0x06BF
       xor      esi, esi
       mov      dword ptr [rbp-0x28], esi
       cmp      r11d, 16
       jb       G_M000_IG135
       cmp      r11d, 32
       jb       G_M000_IG129
       cmp      r11d, 32
       jb       SHORT G_M000_IG119
       cmp      r11d, 48
       jb       SHORT G_M000_IG124
 
G_M000_IG119:                ;; offset=0x06E4
       cmp      r11d, 48
       jb       G_M000_IG54
       cmp      r11d, 64
       jae      G_M000_IG54
       mov      esi, edx
       cmp      r11d, 48
       jne      SHORT G_M000_IG120
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG120:                ;; offset=0x0709
       cmp      r11d, 63
       jne      SHORT G_M000_IG121
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG121:                ;; offset=0x0716
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG122
       mov      edx, -1
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:                ;; offset=0x0728
       mov      edx, 1
 
G_M000_IG123:                ;; offset=0x072D
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG124:                ;; offset=0x074B
       mov      esi, edx
       cmp      r11d, 16
       jne      SHORT G_M000_IG125
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG125:                ;; offset=0x075C
       cmp      r11d, 47
       jne      SHORT G_M000_IG126
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG126:                ;; offset=0x0769
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG127
       mov      edx, -1
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:                ;; offset=0x077B
       mov      edx, 1
 
G_M000_IG128:                ;; offset=0x0780
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG129:                ;; offset=0x079E
       mov      esi, edx
       test     r11d, r11d
       je       SHORT G_M000_IG130
       cmp      r11d, 16
       jne      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x07AB
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG131:                ;; offset=0x07B4
       cmp      r11d, 31
       jne      SHORT G_M000_IG132
       or       esi, 2
       movzx    rsi, sil
 
G_M000_IG132:                ;; offset=0x07C1
       lea      edx, [r11-0x10]
       mov      r11d, edx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x28], xmm0
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG133
       mov      edx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x0800
       mov      edx, 1
 
G_M000_IG134:                ;; offset=0x0805
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vmulss   xmm1, xmm1, dword ptr [rbp-0x28]
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
G_M000_IG135:                ;; offset=0x0820
       mov      esi, edx
       test     r11d, r11d
       jne      SHORT G_M000_IG136
       mov      esi, edx
       or       esi, 1
       movzx    rsi, sil
 
G_M000_IG136:                ;; offset=0x0830
       vmovss   xmm0, dword ptr [rbp-0x0C]
       test     sil, 128
       je       SHORT G_M000_IG137
       mov      edx, -1
       jmp      SHORT G_M000_IG138
 
G_M000_IG137:                ;; offset=0x0842
       mov      edx, 1
 
G_M000_IG138:                ;; offset=0x0847
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, edx
       vaddss   xmm0, xmm1, xmm0
       vmovss   dword ptr [rbp-0x0C], xmm0
       jmp      G_M000_IG54
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 2141

; Assembly listing for method SignedSeekBenchmarks:RunDynamic[RuntimePositiveSixtyFour]():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0027
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       movzx    r14, word  ptr [rbx+0x0C]
       lea      rsi, [rbp-0x50]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       xor      ecx, ecx
       mov      qword ptr [rbp-0x38], rcx
 
G_M000_IG04:                ;; offset=0x0048
       mov      dword ptr [rbp-0x30], ecx
       mov      word  ptr [rbp-0x2C], r14w
       mov      byte  ptr [rbp-0x2A], 1
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0056
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rbp-0x38], xmm0
 
G_M000_IG06:                ;; offset=0x005F
       lea      rcx, bword ptr [rbp-0x38]
       mov      bword ptr [rbp-0x48], rcx
       lea      rcx, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rcx
       xor      r14d, r14d
       mov      r13d, 0x1000
 
G_M000_IG07:                ;; offset=0x0078
       movzx    rdi, word  ptr [rbx+0x0C]
       mov      edx, dword ptr [(reloc 0x7f804326b150)]
       cmp      bword ptr [rbp-0x48], 0
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0089
       lea      rcx, [rbp-0x40]
       mov      rsi, bword ptr [rbp-0x48]
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0099
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009B
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r14d, eax
       dec      r13d
       jne      SHORT G_M000_IG07
 
G_M000_IG11:                ;; offset=0x00AB
       mov      rax, qword ptr [rbp-0x38]
       mov      ecx, dword ptr [rbp-0x30]
       movzx    rdx, byte  ptr [rbp-0x2A]
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      qword ptr [r15], rax
       mov      dword ptr [r15+0x08], ecx
       mov      byte  ptr [r15+0x0C], dl
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x10], eax
       mov      dword ptr [r15+0x14], r14d
       mov      rax, r15
 
G_M000_IG12:                ;; offset=0x00D5
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 226


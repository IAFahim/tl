; Assembly listing for method CombatBenchmarks:PublicScalar():CombatReceipt:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 272
       lea      rbp, [rsp+0x110]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -192
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x20], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      qword ptr [rbp-0x38], rsi
 
G_M000_IG02:                ;; offset=0x0043
       mov      dword ptr [rbp-0x108], 0x3E8
       mov      rax, gword ptr [rbp-0x30]
       mov      rax, qword ptr [rax+0x18]
       mov      qword ptr [rbp-0x40], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      eax, dword ptr [rax+0x24]
       mov      dword ptr [rbp-0x48], eax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rsi, [rbp-0x88]
       mov      rdi, gword ptr [rbp-0x30]
       call     [CombatBenchmarks:Input():CombatTimeline+Input:this]
       lea      rcx, [rbp-0x60]
       lea      rdi, [rbp-0x100]
       lea      rsi, [rbp-0x48]
       lea      rdx, [rbp-0x40]
       call     [CombatTimeline+Output:.ctor(byref,byref,byref):this]
 
G_M000_IG03:                ;; offset=0x009B
       vmovdqu  ymm0, ymmword ptr [rbp-0x100]
       vmovdqu  ymmword ptr [rbp-0xA8], ymm0
 
G_M000_IG04:                ;; offset=0x00AB
       lea      rsi, [rbp-0xB8]
       mov      rax, gword ptr [rbp-0x30]
       movzx    rdi, word  ptr [rax+0x14]
       call     [Tl.Timeline:TryStart(ushort,byref):bool]
       xor      eax, eax
       mov      dword ptr [rbp-0xBC], eax
       mov      rax, gword ptr [rbp-0x30]
       mov      rax, gword ptr [rax+0x08]
       mov      gword ptr [rbp-0xC8], rax
       xor      eax, eax
       mov      dword ptr [rbp-0xCC], eax
       jmp      G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00E4
       mov      rdi, 0x7F7424407788
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0xC8]
       mov      ecx, dword ptr [rbp-0xCC]
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG10
       mov      edx, ecx
       lea      rax, bword ptr [rax+4*rdx+0x10]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0xD0], eax
       lea      r8, [rbp-0xA8]
       mov      rax, gword ptr [rbp-0x30]
       movzx    rdi, word  ptr [rax+0x14]
       lea      rsi, [rbp-0xB8]
       lea      r9, [rbp-0xE0]
       lea      rcx, [rbp-0x88]
       mov      edx, dword ptr [rbp-0xD0]
       call     [Tl.Timeline:TryForward[CombatTimeline+Input,CombatTimeline+Output](ushort,byref,uint,byref,byref,byref):bool]
       test     eax, eax
       setne    al
       movzx    rax, al
       add      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xBC], eax
       mov      rax, qword ptr [rbp-0xE0]
       mov      qword ptr [rbp-0xB8], rax
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0xB0], eax
       mov      eax, dword ptr [rbp-0xCC]
       inc      eax
       mov      dword ptr [rbp-0xCC], eax
 
G_M000_IG06:                ;; offset=0x0184
       mov      eax, dword ptr [rbp-0x108]
       dec      eax
       mov      dword ptr [rbp-0x108], eax
       cmp      dword ptr [rbp-0x108], 0
       jg       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x019B
       lea      rdi, [rbp-0x108]
       mov      esi, 118
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG08:                ;; offset=0x01AC
       mov      rax, gword ptr [rbp-0xC8]
       mov      eax, dword ptr [rax+0x08]
       cmp      eax, dword ptr [rbp-0xCC]
       jg       G_M000_IG05
       mov      rdi, 0x7F742440778C
       call     CORINFO_HELP_COUNTPROFILE32
       lea      rcx, [rbp-0x48]
       lea      r8, [rbp-0x60]
       lea      rsi, [rbp-0xB8]
       lea      rdx, [rbp-0x40]
       mov      rdi, qword ptr [rbp-0x38]
       mov      r9d, dword ptr [rbp-0xBC]
       call     [CombatReceipt:Capture(byref,byref,byref,byref,int):CombatReceipt]
       mov      rax, qword ptr [rbp-0x38]
 
G_M000_IG09:                ;; offset=0x01F9
       vzeroupper 
       add      rsp, 272
       pop      rbp
       ret      
 
G_M000_IG10:                ;; offset=0x0205
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 523

; Assembly listing for method CombatBenchmarks:PublicScalar():CombatReceipt:this (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x76
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 132 single block inlinees; 17 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 128
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0x80]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       mov      rax, gword ptr [rbp+0xF0]
       mov      edi, dword ptr [rbp+0x64]
       mov      rdx, gword ptr [rbp+0x58]
 
G_M000_IG02:                ;; offset=0x0050
       mov      r8, bword ptr [rbp+0x98]
       mov      bword ptr [rbp-0x58], r8
       mov      r9, bword ptr [rbp+0xA0]
       mov      bword ptr [rbp-0x60], r9
       mov      r10, bword ptr [rbp+0xA8]
       mov      r11, bword ptr [rbp+0xB0]
       mov      bword ptr [rbp-0x68], r11
       movzx    rbx, byte  ptr [rbp+0xB8]
       mov      r15, bword ptr [rbp+0x78]
       mov      bword ptr [rbp-0x70], r15
       mov      r14, bword ptr [rbp+0x80]
       mov      bword ptr [rbp-0x78], r14
       mov      r13, bword ptr [rbp+0x88]
       movzx    r12, byte  ptr [rbp+0x90]
       mov      esi, dword ptr [rbp+0x68]
       movzx    r15, word  ptr [rbp+0x6C]
       movzx    r11, word  ptr [rbp+0x6E]
       movzx    r9, word  ptr [rbp+0x70]
       mov      r8d, dword ptr [rbp+0x54]
       mov      ecx, dword ptr [rdx+0x08]
       cmp      ecx, r8d
       jg       G_M000_IG29
 
G_M000_IG03:                ;; offset=0x00C3
       mov      eax, r15d
       mov      ecx, r11d
       mov      edx, r9d
       mov      r8d, dword ptr [rbp+0xE0]
       mov      r9d, dword ptr [rbp+0xE4]
       mov      r10d, dword ptr [rbp+0xD8]
       mov      r11, qword ptr [rbp+0xC0]
       mov      ebx, dword ptr [rbp+0xC8]
       mov      r15d, dword ptr [rbp+0xCC]
       mov      r14d, dword ptr [rbp+0xD0]
       mov      r13d, dword ptr [rbp+0xD4]
       mov      r12, qword ptr [rbp+0xE8]
       mov      dword ptr [r12], esi
       mov      word  ptr [r12+0x04], ax
       mov      word  ptr [r12+0x06], cx
       mov      word  ptr [r12+0x08], dx
       mov      dword ptr [r12+0x0C], r8d
       mov      dword ptr [r12+0x10], r9d
       mov      dword ptr [r12+0x14], r10d
       mov      qword ptr [r12+0x18], r11
       mov      dword ptr [r12+0x20], ebx
       mov      dword ptr [r12+0x24], r15d
       mov      dword ptr [r12+0x28], r14d
       mov      dword ptr [r12+0x2C], r13d
       mov      dword ptr [r12+0x30], edi
       mov      rax, r12
 
G_M000_IG04:                ;; offset=0x0150
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0162
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0162
       cmp      ecx, 16
       jae      G_M000_IG20
 
G_M000_IG07:                ;; offset=0x016B
       mov      r9d, 1
       movzx    r9, r9b
       vmovss   xmm0, dword ptr [r10]
       mov      r11, bword ptr [rbp-0x58]
       vmovss   xmm1, dword ptr [r11]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm2
       vaddss   xmm1, xmm1, dword ptr [r10+0x04]
       mov      r14, bword ptr [rbp-0x78]
       vmovss   dword ptr [r14], xmm0
       vmovss   dword ptr [r14+0x04], xmm1
       mov      r11d, ecx
       add      qword ptr [r13], r11
       inc      dword ptr [r13+0x08]
       cmp      r9d, 2
       ja       SHORT G_M000_IG12
 
G_M000_IG08:                ;; offset=0x01B5
       mov      r9d, r9d
       lea      r11, [reloc @RWD04]
       mov      r11d, dword ptr [r11+4*r9]
       lea      r14, G_M000_IG02
       add      r11, r14
       jmp      r11
 
G_M000_IG09:                ;; offset=0x01D0
       inc      dword ptr [r13+0x14]
       jmp      SHORT G_M000_IG12
 
G_M000_IG10:                ;; offset=0x01D6
       inc      dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x01DC
       inc      dword ptr [r13+0x0C]
 
G_M000_IG12:                ;; offset=0x01E0
       cmp      esi, 8
       jb       G_M000_IG25
 
G_M000_IG13:                ;; offset=0x01E9
       mov      esi, 1
 
G_M000_IG14:                ;; offset=0x01EE
       movzx    rsi, sil
       mov      r9, bword ptr [rbp-0x60]
       vmovss   xmm0, dword ptr [r9]
       mov      r11, bword ptr [rbp-0x68]
       vmovss   xmm1, dword ptr [r11]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vsubss   xmm0, xmm0, xmm1
       mov      r11, bword ptr [rbp-0x70]
       vmovss   dword ptr [r11], xmm0
       lea      r11d, [rcx+0x01]
       add      qword ptr [r13], r11
       inc      dword ptr [r13+0x08]
       cmp      esi, 2
       ja       G_M000_IG26
 
G_M000_IG15:                ;; offset=0x022E
       mov      esi, esi
       lea      r11, [reloc @RWD20]
       mov      r14, bword ptr [rbp-0x78]
       mov      r11d, dword ptr [r11+4*rsi]
       lea      r9, G_M000_IG02
       add      r11, r9
       jmp      r11
 
G_M000_IG16:                ;; offset=0x024C
       inc      dword ptr [r13+0x10]
 
G_M000_IG17:                ;; offset=0x0250
       mov      r9d, 1
       cmp      ecx, 63
       jb       SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x025B
       mov      r9d, 9
 
G_M000_IG19:                ;; offset=0x0261
       mov      r11d, 1
       mov      esi, 1
       jmp      G_M000_IG36
 
G_M000_IG20:                ;; offset=0x0271
       cmp      ecx, 16
       jb       G_M000_IG43
 
G_M000_IG21:                ;; offset=0x027A
       cmp      ecx, 32
       jae      G_M000_IG43
 
G_M000_IG22:                ;; offset=0x0283
       mov      r9d, 1
       movzx    r9, r9b
       mov      dword ptr [rbp-0x3C], r9d
       lea      r11d, [rcx-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r11
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmulss   xmm1, xmm0, dword ptr [reloc @RWD36]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm0
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD40]
       vmovss   dword ptr [rbp-0x48], xmm1
       vmovss   dword ptr [rbp-0x44], xmm0
       vmovss   xmm0, dword ptr [r10]
       vmovss   xmm1, dword ptr [rbp-0x48]
       mov      r11, bword ptr [rbp-0x58]
       vmulss   xmm1, xmm1, dword ptr [r11]
       vaddss   xmm0, xmm0, xmm1
       vmovss   xmm1, dword ptr [r10+0x04]
       vmovss   xmm2, dword ptr [rbp-0x44]
       vmulss   xmm2, xmm2, dword ptr [r11]
       vaddss   xmm1, xmm1, xmm2
       mov      r14, bword ptr [rbp-0x78]
       vmovss   dword ptr [r14], xmm0
       vmovss   dword ptr [r14+0x04], xmm1
       mov      r9d, ecx
       add      qword ptr [r13], r9
       inc      dword ptr [r13+0x08]
       mov      r9d, dword ptr [rbp-0x3C]
       cmp      r9d, 2
       ja       G_M000_IG85
 
G_M000_IG23:                ;; offset=0x031F
       mov      r9d, r9d
       mov      qword ptr [rbp-0x80], r9
       lea      r9, [reloc @RWD44]
       mov      r14, qword ptr [rbp-0x80]
       mov      r9d, dword ptr [r9+4*r14]
       lea      r11, G_M000_IG02
       add      r9, r11
       jmp      r9
 
G_M000_IG24:                ;; offset=0x0342
       inc      dword ptr [r13+0x14]
       jmp      G_M000_IG85
 
G_M000_IG25:                ;; offset=0x034B
       xor      esi, esi
       jmp      G_M000_IG14
 
G_M000_IG26:                ;; offset=0x0352
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG27:                ;; offset=0x035B
       xor      ecx, ecx
 
G_M000_IG28:                ;; offset=0x035D
       test     ecx, ecx
       setne    cl
       movzx    rcx, cl
       add      edi, ecx
       inc      r8d
       mov      ecx, dword ptr [rdx+0x08]
       cmp      ecx, r8d
       jle      G_M000_IG03
 
G_M000_IG29:                ;; offset=0x0376
       cmp      r8d, ecx
       jae      G_M000_IG93
       mov      ecx, dword ptr [rdx+4*r8+0x10]
       movzx    r14, word  ptr [rax+0x14]
       cmp      r14d, 1
       jne      SHORT G_M000_IG27
 
G_M000_IG30:                ;; offset=0x038F
       cmp      r11d, r14d
       jne      SHORT G_M000_IG35
 
G_M000_IG31:                ;; offset=0x0394
       test     r9b, 1
       je       SHORT G_M000_IG35
 
G_M000_IG32:                ;; offset=0x039A
       test     r9b, 2
       jne      SHORT G_M000_IG35
 
G_M000_IG33:                ;; offset=0x03A0
       test     ebx, ebx
       je       SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x03A4
       test     r12d, r12d
       jne      SHORT G_M000_IG37
 
G_M000_IG35:                ;; offset=0x03A9
       mov      ecx, esi
       xor      esi, esi
 
G_M000_IG36:                ;; offset=0x03AD
       mov      r14d, ecx
       mov      ecx, esi
       mov      esi, r14d
       jmp      SHORT G_M000_IG28
 
G_M000_IG37:                ;; offset=0x03B7
       xor      r11d, r11d
       mov      qword ptr [rbp-0x48], r11
       cmp      ecx, 8
       jae      G_M000_IG06
 
G_M000_IG38:                ;; offset=0x03C7
       mov      esi, 1
       movzx    r9, sil
       vmovss   xmm0, dword ptr [r10]
       mov      rsi, bword ptr [rbp-0x58]
       vmovss   xmm1, dword ptr [rsi]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm2
       vaddss   xmm1, xmm1, dword ptr [r10+0x04]
       mov      r14, bword ptr [rbp-0x78]
       vmovss   dword ptr [r14], xmm0
       vmovss   dword ptr [r14+0x04], xmm1
       mov      r11d, ecx
       add      qword ptr [r13], r11
       inc      dword ptr [r13+0x08]
       cmp      r9d, 2
       ja       G_M000_IG17
 
G_M000_IG39:                ;; offset=0x0413
       mov      r9d, r9d
       lea      r11, [reloc @RWD56]
       mov      r11d, dword ptr [r11+4*r9]
       lea      rsi, G_M000_IG02
       add      r11, rsi
       jmp      r11
 
G_M000_IG40:                ;; offset=0x042E
       inc      dword ptr [r13+0x14]
       jmp      G_M000_IG17
 
G_M000_IG41:                ;; offset=0x0437
       xor      esi, esi
       jmp      G_M000_IG87
 
G_M000_IG42:                ;; offset=0x043E
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG43:                ;; offset=0x0447
       cmp      ecx, 32
       jb       G_M000_IG61
 
G_M000_IG44:                ;; offset=0x0450
       cmp      ecx, 48
       jae      G_M000_IG61
 
G_M000_IG45:                ;; offset=0x0459
       cmp      ecx, 47
       je       SHORT G_M000_IG49
 
G_M000_IG46:                ;; offset=0x045E
       cmp      esi, 16
       jb       SHORT G_M000_IG48
 
G_M000_IG47:                ;; offset=0x0463
       mov      r9d, 1
       jmp      SHORT G_M000_IG50
 
G_M000_IG48:                ;; offset=0x046B
       xor      r9d, r9d
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:                ;; offset=0x0470
       mov      r9d, 2
 
G_M000_IG50:                ;; offset=0x0476
       movzx    r9, r9b
       mov      dword ptr [rbp-0x34], r9d
       vmovss   xmm0, dword ptr [r10]
       mov      r11, bword ptr [rbp-0x58]
       vmovss   xmm1, dword ptr [r11]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD68]
       vaddss   xmm0, xmm0, xmm2
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD72]
       vaddss   xmm1, xmm1, dword ptr [r10+0x04]
       mov      r14, bword ptr [rbp-0x78]
       vmovss   dword ptr [r14], xmm0
       vmovss   dword ptr [r14+0x04], xmm1
       mov      r9d, ecx
       add      qword ptr [r13], r9
       inc      dword ptr [r13+0x08]
       mov      r9d, dword ptr [rbp-0x34]
       cmp      r9d, 2
       ja       SHORT G_M000_IG55
 
G_M000_IG51:                ;; offset=0x04CA
       mov      r9d, r9d
       mov      qword ptr [rbp-0x80], r9
       lea      r9, [reloc @RWD76]
       mov      r14, qword ptr [rbp-0x80]
       mov      r9d, dword ptr [r9+4*r14]
       lea      r11, G_M000_IG02
       add      r9, r11
       jmp      r9
 
G_M000_IG52:                ;; offset=0x04ED
       inc      dword ptr [r13+0x14]
       jmp      SHORT G_M000_IG55
 
G_M000_IG53:                ;; offset=0x04F3
       inc      dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG55
 
G_M000_IG54:                ;; offset=0x04F9
       inc      dword ptr [r13+0x0C]
 
G_M000_IG55:                ;; offset=0x04FD
       cmp      esi, 8
       jb       SHORT G_M000_IG59
 
G_M000_IG56:                ;; offset=0x0502
       mov      esi, 1
 
G_M000_IG57:                ;; offset=0x0507
       movzx    rsi, sil
       mov      dword ptr [rbp-0x38], esi
       mov      r9, bword ptr [rbp-0x60]
       vmovss   xmm0, dword ptr [r9]
       mov      rsi, bword ptr [rbp-0x68]
       vmovss   xmm1, dword ptr [rsi]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vsubss   xmm0, xmm0, xmm1
       mov      rsi, bword ptr [rbp-0x70]
       vmovss   dword ptr [rsi], xmm0
       lea      esi, [rcx+0x01]
       add      qword ptr [r13], rsi
       inc      dword ptr [r13+0x08]
       mov      esi, dword ptr [rbp-0x38]
       cmp      esi, 2
       ja       SHORT G_M000_IG60
 
G_M000_IG58:                ;; offset=0x0546
       mov      esi, esi
       mov      qword ptr [rbp-0x80], rsi
       lea      rsi, [reloc @RWD88]
       mov      r14, bword ptr [rbp-0x78]
       mov      r11, qword ptr [rbp-0x80]
       mov      esi, dword ptr [rsi+4*r11]
       lea      r9, G_M000_IG02
       add      rsi, r9
       jmp      rsi
 
G_M000_IG59:                ;; offset=0x056B
       xor      esi, esi
       jmp      SHORT G_M000_IG57
 
G_M000_IG60:                ;; offset=0x056F
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG61:                ;; offset=0x0578
       cmp      ecx, 48
       jb       G_M000_IG71
 
G_M000_IG62:                ;; offset=0x0581
       cmp      ecx, 56
       jae      G_M000_IG71
 
G_M000_IG63:                ;; offset=0x058A
       cmp      ecx, 55
       je       SHORT G_M000_IG67
 
G_M000_IG64:                ;; offset=0x058F
       cmp      esi, 8
       jb       SHORT G_M000_IG66
 
G_M000_IG65:                ;; offset=0x0594
       mov      esi, 1
       jmp      SHORT G_M000_IG68
 
G_M000_IG66:                ;; offset=0x059B
       xor      esi, esi
       jmp      SHORT G_M000_IG68
 
G_M000_IG67:                ;; offset=0x059F
       mov      esi, 2
 
G_M000_IG68:                ;; offset=0x05A4
       movzx    rsi, sil
       mov      dword ptr [rbp-0x30], esi
       mov      r9, bword ptr [rbp-0x60]
       vmovss   xmm0, dword ptr [r9]
       mov      r11, bword ptr [rbp-0x68]
       vmovss   xmm1, dword ptr [r11]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vsubss   xmm0, xmm0, xmm1
       mov      rsi, bword ptr [rbp-0x70]
       vmovss   dword ptr [rsi], xmm0
       lea      esi, [rcx+0x01]
       add      qword ptr [r13], rsi
       inc      dword ptr [r13+0x08]
       mov      esi, dword ptr [rbp-0x30]
       cmp      esi, 2
       ja       SHORT G_M000_IG70
 
G_M000_IG69:                ;; offset=0x05E4
       mov      esi, esi
       mov      qword ptr [rbp-0x80], rsi
       lea      rsi, [reloc @RWD100]
       mov      r14, bword ptr [rbp-0x78]
       mov      r9, qword ptr [rbp-0x80]
       mov      esi, dword ptr [rsi+4*r9]
       lea      r11, G_M000_IG02
       add      rsi, r11
       jmp      rsi
 
G_M000_IG70:                ;; offset=0x0609
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG71:                ;; offset=0x0612
       cmp      ecx, 56
       jb       SHORT G_M000_IG73
 
G_M000_IG72:                ;; offset=0x0617
       cmp      ecx, 63
       jb       SHORT G_M000_IG78
 
G_M000_IG73:                ;; offset=0x061C
       cmp      ecx, 63
       jb       G_M000_IG92
 
G_M000_IG74:                ;; offset=0x0625
       cmp      ecx, 64
       jae      G_M000_IG91
 
G_M000_IG75:                ;; offset=0x062E
       cmp      ecx, 63
       je       SHORT G_M000_IG80
 
G_M000_IG76:                ;; offset=0x0633
       cmp      esi, 63
       jb       SHORT G_M000_IG79
 
G_M000_IG77:                ;; offset=0x0638
       mov      esi, 1
       jmp      SHORT G_M000_IG81
 
G_M000_IG78:                ;; offset=0x063F
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG79:                ;; offset=0x0648
       xor      esi, esi
       jmp      SHORT G_M000_IG81
 
G_M000_IG80:                ;; offset=0x064C
       mov      esi, 2
 
G_M000_IG81:                ;; offset=0x0651
       movzx    rsi, sil
       mov      dword ptr [rbp-0x2C], esi
       mov      r9, bword ptr [rbp-0x60]
       vmovss   xmm0, dword ptr [r9]
       vxorps   xmm1, xmm1, xmm1
       mov      r11, bword ptr [rbp-0x68]
       vmulss   xmm1, xmm1, dword ptr [r11]
       vsubss   xmm0, xmm0, xmm1
       mov      rsi, bword ptr [rbp-0x70]
       vmovss   dword ptr [rsi], xmm0
       lea      esi, [rcx+0x01]
       add      qword ptr [r13], rsi
       inc      dword ptr [r13+0x08]
       mov      esi, dword ptr [rbp-0x2C]
       cmp      esi, 2
       ja       G_M000_IG90
 
G_M000_IG82:                ;; offset=0x0691
       mov      esi, esi
       mov      qword ptr [rbp-0x80], rsi
       lea      rsi, [reloc @RWD112]
       mov      r14, bword ptr [rbp-0x78]
       mov      r11, qword ptr [rbp-0x80]
       mov      esi, dword ptr [rsi+4*r11]
       lea      r9, G_M000_IG02
       add      rsi, r9
       jmp      rsi
 
G_M000_IG83:                ;; offset=0x06B6
       inc      dword ptr [r13+0x10]
       jmp      SHORT G_M000_IG85
 
G_M000_IG84:                ;; offset=0x06BC
       inc      dword ptr [r13+0x0C]
 
G_M000_IG85:                ;; offset=0x06C0
       cmp      esi, 8
       jb       G_M000_IG41
 
G_M000_IG86:                ;; offset=0x06C9
       mov      esi, 1
 
G_M000_IG87:                ;; offset=0x06CE
       movzx    rsi, sil
       mov      dword ptr [rbp-0x4C], esi
       mov      r9, bword ptr [rbp-0x60]
       vmovss   xmm0, dword ptr [r9]
       mov      rsi, bword ptr [rbp-0x68]
       vmovss   xmm1, dword ptr [rsi]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vsubss   xmm0, xmm0, xmm1
       mov      rsi, bword ptr [rbp-0x70]
       vmovss   dword ptr [rsi], xmm0
       lea      esi, [rcx+0x01]
       add      qword ptr [r13], rsi
       inc      dword ptr [r13+0x08]
       mov      esi, dword ptr [rbp-0x4C]
       cmp      esi, 2
       ja       G_M000_IG42
 
G_M000_IG88:                ;; offset=0x0711
       mov      esi, esi
       mov      qword ptr [rbp-0x80], rsi
       lea      rsi, [reloc @RWD124]
       mov      r14, bword ptr [rbp-0x78]
       mov      r11, qword ptr [rbp-0x80]
       mov      esi, dword ptr [rsi+4*r11]
       lea      r9, G_M000_IG02
       add      rsi, r9
       jmp      rsi
 
G_M000_IG89:                ;; offset=0x0736
       inc      dword ptr [r13+0x0C]
       jmp      G_M000_IG17
 
G_M000_IG90:                ;; offset=0x073F
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG91:                ;; offset=0x0748
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG92:                ;; offset=0x0751
       mov      r14, bword ptr [rbp-0x78]
       jmp      G_M000_IG17
 
G_M000_IG93:                ;; offset=0x075A
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	40000000h		;         2
RWD04  	dd	0000018Ch ; case G_M000_IG11
       	dd	00000186h ; case G_M000_IG10
       	dd	00000180h ; case G_M000_IG09
RWD16  	dd	40A00000h		;         5
RWD20  	dd	000006E6h ; case G_M000_IG89
       	dd	000001FCh ; case G_M000_IG16
       	dd	000003DEh ; case G_M000_IG40
RWD32  	dd	41700000h		;        15
RWD36  	dd	40800000h		;         4
RWD40  	dd	3F800000h		;         1
RWD44  	dd	0000066Ch ; case G_M000_IG84
       	dd	00000666h ; case G_M000_IG83
       	dd	000002F2h ; case G_M000_IG24
RWD56  	dd	000006E6h ; case G_M000_IG89
       	dd	000001FCh ; case G_M000_IG16
       	dd	000003DEh ; case G_M000_IG40
RWD68  	dd	40C00000h		;         6
RWD72  	dd	40400000h		;         3
RWD76  	dd	000004A9h ; case G_M000_IG54
       	dd	000004A3h ; case G_M000_IG53
       	dd	0000049Dh ; case G_M000_IG52
RWD88  	dd	000006E6h ; case G_M000_IG89
       	dd	000001FCh ; case G_M000_IG16
       	dd	000003DEh ; case G_M000_IG40
RWD100 	dd	000006E6h ; case G_M000_IG89
       	dd	000001FCh ; case G_M000_IG16
       	dd	000003DEh ; case G_M000_IG40
RWD112 	dd	000006E6h ; case G_M000_IG89
       	dd	000001FCh ; case G_M000_IG16
       	dd	000003DEh ; case G_M000_IG40
RWD124 	dd	000006E6h ; case G_M000_IG89
       	dd	000001FCh ; case G_M000_IG16
       	dd	000003DEh ; case G_M000_IG40

; Total bytes of code 1888


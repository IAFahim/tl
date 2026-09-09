; Assembly listing for method SumBenchmarks:PublicScalar():SumReceipt:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 176
       lea      rbp, [rsp+0xB0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0xA0], ymm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      qword ptr [rbp-0x38], rsi
 
G_M000_IG02:                ;; offset=0x0035
       mov      dword ptr [rbp-0xA8], 0x3E8
       xor      eax, eax
       mov      dword ptr [rbp-0x3C], eax
       mov      byte  ptr [rbp-0x48], 0
       lea      rsi, [rbp-0x3C]
       lea      rdi, [rbp-0xA0]
       call     [SumTimeline+Output:.ctor(byref):this]
 
G_M000_IG03:                ;; offset=0x0059
       vmovdqu  xmm0, xmmword ptr [rbp-0xA0]
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
 
G_M000_IG04:                ;; offset=0x0066
       lea      rsi, [rbp-0x68]
       mov      rax, gword ptr [rbp-0x30]
       movzx    rdi, word  ptr [rax+0x14]
       call     [Tl.Timeline:TryStart(ushort,byref):bool]
       xor      eax, eax
       mov      dword ptr [rbp-0x6C], eax
       mov      rax, gword ptr [rbp-0x30]
       mov      rax, gword ptr [rax+0x08]
       mov      gword ptr [rbp-0x78], rax
       xor      eax, eax
       mov      dword ptr [rbp-0x7C], eax
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0090
       mov      rdi, 0x7FCFEEB89BD0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x78]
       mov      ecx, dword ptr [rbp-0x7C]
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG10
       mov      edx, ecx
       lea      rax, bword ptr [rax+4*rdx+0x10]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x80], eax
       lea      r8, [rbp-0x58]
       mov      rax, gword ptr [rbp-0x30]
       movzx    rdi, word  ptr [rax+0x14]
       lea      rsi, [rbp-0x68]
       lea      r9, [rbp-0x90]
       lea      rcx, [rbp-0x48]
       mov      edx, dword ptr [rbp-0x80]
       call     [Tl.Timeline:TryForward[SumTimeline+Input,SumTimeline+Output](ushort,byref,uint,byref,byref,byref):bool]
       test     eax, eax
       setne    al
       movzx    rax, al
       add      eax, dword ptr [rbp-0x6C]
       mov      dword ptr [rbp-0x6C], eax
       mov      rax, qword ptr [rbp-0x90]
       mov      qword ptr [rbp-0x68], rax
       mov      eax, dword ptr [rbp-0x88]
       mov      dword ptr [rbp-0x60], eax
       mov      eax, dword ptr [rbp-0x7C]
       inc      eax
       mov      dword ptr [rbp-0x7C], eax
 
G_M000_IG06:                ;; offset=0x0109
       mov      eax, dword ptr [rbp-0xA8]
       dec      eax
       mov      dword ptr [rbp-0xA8], eax
       cmp      dword ptr [rbp-0xA8], 0
       jg       SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0120
       lea      rdi, [rbp-0xA8]
       mov      esi, 97
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG08:                ;; offset=0x0131
       mov      rax, gword ptr [rbp-0x78]
       mov      eax, dword ptr [rax+0x08]
       cmp      eax, dword ptr [rbp-0x7C]
       jg       G_M000_IG05
       mov      rdi, 0x7FCFEEB89BD4
       call     CORINFO_HELP_COUNTPROFILE32
       lea      rsi, [rbp-0x68]
       mov      rdi, qword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rbp-0x3C]
       mov      edx, dword ptr [rbp-0x6C]
       call     [SumReceipt:Capture(byref,float,int):SumReceipt]
       mov      rax, qword ptr [rbp-0x38]
 
G_M000_IG09:                ;; offset=0x016A
       add      rsp, 176
       pop      rbp
       ret      
 
G_M000_IG10:                ;; offset=0x0173
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 377

; Assembly listing for method SumBenchmarks:PublicScalar():SumReceipt:this (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x61
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 35 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 48
       mov      qword ptr [rsp+0xE8], r15
       mov      qword ptr [rsp+0xE0], r14
       mov      qword ptr [rsp+0xD8], r13
       mov      qword ptr [rsp+0xD0], r12
       mov      qword ptr [rsp+0xC8], rbx
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rax, gword ptr [rbp+0x90]
       mov      rcx, qword ptr [rbp+0x88]
       mov      edi, dword ptr [rbp+0x54]
       mov      rdx, gword ptr [rbp+0x48]
       mov      esi, dword ptr [rbp+0x44]
 
G_M000_IG02:                ;; offset=0x0054
       mov      r8, bword ptr [rbp+0x68]
       movzx    r9, byte  ptr [rbp+0x70]
       mov      r10d, dword ptr [rbp+0x58]
       movzx    r11, word  ptr [rbp+0x5C]
       movzx    rbx, word  ptr [rbp+0x5E]
       movzx    r15, word  ptr [rbp+0x60]
       mov      esi, esi
       mov      r14d, dword ptr [rdx+0x08]
       cmp      r14d, esi
       jg       SHORT G_M000_IG08
 
G_M000_IG03:                ;; offset=0x007A
       vmovss   xmm0, dword ptr [rbp+0x84]
       mov      dword ptr [rcx], r10d
       mov      word  ptr [rcx+0x04], r11w
       mov      word  ptr [rcx+0x06], bx
       mov      word  ptr [rcx+0x08], r15w
       vmovd    eax, xmm0
       mov      dword ptr [rcx+0x0C], eax
       mov      dword ptr [rcx+0x10], edi
       mov      rax, rcx
 
G_M000_IG04:                ;; offset=0x00A0
       add      rsp, 200
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x00B2
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x00B2
       xor      r12d, r12d
 
G_M000_IG07:                ;; offset=0x00B5
       test     r12d, r12d
       setne    r14b
       movzx    r14, r14b
       add      edi, r14d
       inc      esi
       mov      r14d, dword ptr [rdx+0x08]
       cmp      r14d, esi
       jle      SHORT G_M000_IG03
 
G_M000_IG08:                ;; offset=0x00CE
       cmp      esi, r14d
       jae      G_M000_IG28
       mov      r14d, dword ptr [rdx+4*rsi+0x10]
       movzx    r13, word  ptr [rax+0x14]
       test     r13d, r13d
       jne      SHORT G_M000_IG06
 
G_M000_IG09:                ;; offset=0x00E6
       cmp      ebx, r13d
       jne      SHORT G_M000_IG13
 
G_M000_IG10:                ;; offset=0x00EB
       test     r15b, 1
       je       SHORT G_M000_IG13
 
G_M000_IG11:                ;; offset=0x00F1
       test     r15b, 2
       jne      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00F7
       test     r9d, r9d
       jne      SHORT G_M000_IG15
 
G_M000_IG13:                ;; offset=0x00FC
       xor      r12d, r12d
 
G_M000_IG14:                ;; offset=0x00FF
       jmp      SHORT G_M000_IG07
 
G_M000_IG15:                ;; offset=0x0101
       xor      r10d, r10d
       mov      dword ptr [rbp-0x30], r10d
       cmp      r14d, 16
       jae      SHORT G_M000_IG20
 
G_M000_IG16:                ;; offset=0x010E
       vmovss   xmm0, dword ptr [r8]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [r8], xmm0
 
G_M000_IG17:                ;; offset=0x0120
       mov      r15d, 1
       cmp      r14d, 63
       jb       SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x012C
       mov      r15d, 9
 
G_M000_IG19:                ;; offset=0x0132
       mov      r10d, r14d
       xor      ebx, ebx
       mov      r12d, 1
       jmp      SHORT G_M000_IG14
 
G_M000_IG20:                ;; offset=0x013F
       cmp      r14d, 32
       jae      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x0145
       lea      r10d, [r14-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r10
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [r8]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x30]
       vmovss   dword ptr [r8], xmm0
       jmp      SHORT G_M000_IG17
 
G_M000_IG22:                ;; offset=0x0180
       cmp      r14d, 32
       jb       SHORT G_M000_IG25
 
G_M000_IG23:                ;; offset=0x0186
       cmp      r14d, 48
       jae      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x018C
       vmovss   xmm0, dword ptr [r8]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [r8], xmm0
       jmp      SHORT G_M000_IG17
 
G_M000_IG25:                ;; offset=0x01A0
       cmp      r14d, 48
       jb       G_M000_IG17
 
G_M000_IG26:                ;; offset=0x01AA
       cmp      r14d, 64
       jae      G_M000_IG17
 
G_M000_IG27:                ;; offset=0x01B4
       vmovss   xmm0, dword ptr [r8]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [r8], xmm0
       jmp      G_M000_IG17
 
G_M000_IG28:                ;; offset=0x01CB
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41700000h		;        15
RWD08  	dd	40000000h		;         2
RWD12  	dd	40400000h		;         3
RWD16  	dd	40A00000h		;         5

; Total bytes of code 465


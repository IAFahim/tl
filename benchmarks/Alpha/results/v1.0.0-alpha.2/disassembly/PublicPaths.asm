; Assembly listing for method SumBenchmarks:PublicScalar():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 33 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x28], rax
       mov      qword ptr [rbp-0x30], rax
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       xor      esi, esi
       mov      dword ptr [rbp-0x1C], esi
       movzx    r14, word  ptr [r15+0x14]
       lea      rsi, [rbp-0x28]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG04
 
G_M000_IG03:
       xor      eax, eax
       xor      ecx, ecx
       mov      edx, 1
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       xor      eax, eax
       xor      ecx, ecx
       xor      r14d, r14d
       xor      edx, edx
 
G_M000_IG05:
       xor      edi, edi
       mov      rsi, gword ptr [r15+0x08]
       xor      r8d, r8d
       jmp      G_M000_IG16
 
G_M000_IG06:
       movzx    r11, r14w
       cmp      r11d, r10d
       jne      G_M000_IG13
       movzx    r10, dx
       and      r10d, 3
       cmp      r10d, 1
       jne      G_M000_IG13
       movzx    r10, cx
       xor      eax, eax
       mov      dword ptr [rbp-0x30], eax
       cmp      r9d, 16
       jb       G_M000_IG10
       cmp      r9d, 32
       jb       SHORT G_M000_IG09
       cmp      r9d, 32
       jb       SHORT G_M000_IG07
       cmp      r9d, 48
       jb       SHORT G_M000_IG08
 
G_M000_IG07:
       cmp      r9d, 48
       jb       G_M000_IG11
       cmp      r9d, 64
       jae      SHORT G_M000_IG11
       vmovss   xmm0, dword ptr [rbp-0x1C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rbp-0x1C], xmm0
       jmp      SHORT G_M000_IG11
 
G_M000_IG08:
       vmovss   xmm0, dword ptr [rbp-0x1C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x1C], xmm0
       jmp      SHORT G_M000_IG11
 
G_M000_IG09:
       lea      ecx, [r9-0x10]
       mov      edx, ecx
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       vmovss   xmm0, dword ptr [rbp-0x1C]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x30]
       vmovss   dword ptr [rbp-0x1C], xmm0
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       vmovss   xmm0, dword ptr [rbp-0x1C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x1C], xmm0
 
G_M000_IG11:
       mov      eax, 1
       cmp      r9d, 63
       jb       SHORT G_M000_IG12
       mov      eax, 9
 
G_M000_IG12:
       mov      ecx, r10d
       xor      edx, edx
       mov      r10d, 1
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:
       mov      r9d, eax
       movzx    rcx, cx
       movzx    r10, r14w
       movzx    rax, dx
       xor      edx, edx
       mov      r11d, edx
       mov      edx, r10d
       mov      r10d, r11d
 
G_M000_IG14:
       mov      r14d, edx
 
G_M000_IG15:
       test     r10d, r10d
       setne    dl
       movzx    rdx, dl
       add      edi, edx
       movzx    rdx, ax
       inc      r8d
       mov      eax, r9d
 
G_M000_IG16:
       mov      r9d, dword ptr [rsi+0x08]
       cmp      r9d, r8d
       jle      SHORT G_M000_IG19
 
G_M000_IG17:
       mov      r9d, dword ptr [rsi+4*r8+0x10]
       movzx    r10, word  ptr [r15+0x14]
       test     r10d, r10d
       je       G_M000_IG06
 
G_M000_IG18:
       mov      r9d, eax
       movzx    rcx, cx
       movzx    r10, r14w
       mov      r14d, r10d
       movzx    rax, dx
       xor      r10d, r10d
       jmp      SHORT G_M000_IG15
 
G_M000_IG19:
       vmovss   xmm0, dword ptr [rbp-0x1C]
       mov      dword ptr [rbx], eax
       mov      word  ptr [rbx+0x04], cx
       mov      word  ptr [rbx+0x06], r14w
       mov      word  ptr [rbx+0x08], dx
       vmovd    eax, xmm0
       mov      dword ptr [rbx+0x0C], eax
       mov      dword ptr [rbx+0x10], edi
       mov      rax, rbx
 
G_M000_IG20:
       add      rsp, 24
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 464

; Assembly listing for method SumBenchmarks:PublicBatch8():SumReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 12 single block inlinees; 3 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 80
       lea      rbp, [rsp+0x70]
       xor      eax, eax
       mov      qword ptr [rbp-0x68], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       xor      esi, esi
       mov      dword ptr [rbp-0x24], esi
       lea      rsi, bword ptr [rbp-0x24]
       mov      bword ptr [rbp-0x40], rsi
       mov      byte  ptr [rbp-0x38], 1
       movzx    r14, word  ptr [rbx+0x14]
       lea      rsi, [rbp-0x68]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG04
 
G_M000_IG03:
       xor      edx, edx
       mov      dword ptr [rbp-0x50], edx
       mov      word  ptr [rbp-0x4C], 0
       mov      word  ptr [rbp-0x4A], r14w
       mov      word  ptr [rbp-0x48], 1
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       xor      edx, edx
       mov      qword ptr [rbp-0x50], rdx
       mov      dword ptr [rbp-0x48], edx
 
G_M000_IG05:
       xor      r14d, r14d
       xor      r13d, r13d
       jmp      SHORT G_M000_IG09
 
G_M000_IG06:
       lea      rcx, [rbp-0x60]
       mov      qword ptr [rsp], rcx
       mov      ecx, 8
       lea      rsi, [rbp-0x50]
       lea      r8, [rbp-0x30]
       lea      r9, [rbp-0x40]
       call     [SumTimeline:ForwardKernel(ushort,byref,System.ReadOnlySpan`1[uint],byref,byref,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG12
 
G_M000_IG07:
       mov      eax, 8
 
G_M000_IG08:
       add      r14d, eax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x50], rax
       mov      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x48], eax
       add      r13d, 8
 
G_M000_IG09:
       mov      rdx, gword ptr [rbx+0x08]
       cmp      dword ptr [rdx+0x08], r13d
       jle      SHORT G_M000_IG13
 
G_M000_IG10:
       test     rdx, rdx
       je       SHORT G_M000_IG15
       mov      ecx, dword ptr [rdx+0x08]
       mov      esi, r13d
       lea      r8, [rsi+0x08]
       cmp      rcx, r8
       jb       SHORT G_M000_IG15
       lea      rdx, bword ptr [rdx+4*rsi+0x10]
       movzx    rdi, word  ptr [rbx+0x14]
       test     edi, edi
       je       SHORT G_M000_IG06
 
G_M000_IG11:
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rbp-0x60], rax
       mov      eax, dword ptr [rbp-0x48]
       mov      dword ptr [rbp-0x58], eax
 
G_M000_IG12:
       xor      eax, eax
       jmp      SHORT G_M000_IG08
 
G_M000_IG13:
       vmovss   xmm0, dword ptr [rbp-0x24]
       mov      eax, dword ptr [rbp-0x50]
       movzx    rcx, word  ptr [rbp-0x4C]
       movzx    rdx, word  ptr [rbp-0x4A]
       movzx    rdi, word  ptr [rbp-0x48]
       mov      dword ptr [r15], eax
       mov      word  ptr [r15+0x04], cx
       mov      word  ptr [r15+0x06], dx
       mov      word  ptr [r15+0x08], di
       vmovd    eax, xmm0
       mov      dword ptr [r15+0x0C], eax
       mov      dword ptr [r15+0x10], r14d
       mov      rax, r15
 
G_M000_IG14:
       add      rsp, 80
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG15:
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
; Total bytes of code 323

; Assembly listing for method SumTimeline:ForwardKernel(ushort,byref,System.ReadOnlySpan`1[uint],byref,byref,byref):bool (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 24 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      qword ptr [rbp-0x40], 0xD1FFAB1E
       mov      r15, rsi
       mov      rsi, rdx
       mov      r14d, ecx
       mov      rbx, r9
       mov      r13, bword ptr [rbp+0x10]
 
G_M000_IG02:
       movzx    r12, di
       test     r12d, r12d
       jne      G_M000_IG18
 
G_M000_IG03:
       movzx    rax, word  ptr [r15+0x06]
       cmp      eax, r12d
       jne      G_M000_IG18
       movzx    rax, word  ptr [r15+0x08]
       and      eax, 3
       cmp      eax, 1
       jne      G_M000_IG18
       cmp      byte  ptr [rbx+0x08], 0
       je       G_M000_IG18
       cmp      r14d, 256
       jg       G_M000_IG18
       mov      eax, r14d
       mov      edi, 4
       mul      rdx:rax, rdi
       jb       G_M000_IG21
       test     rax, rax
       je       SHORT G_M000_IG05
       add      rax, 15
       shr      rax, 4
 
G_M000_IG04:
       push     0
       push     0
       dec      rax
       jne      SHORT G_M000_IG04
       lea      rax, [rsp]
 
G_M000_IG05:
       mov      bword ptr [rbp-0x38], rax
       mov      edx, r14d
       shl      rdx, 2
       mov      rdi, rax
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       test     r14d, r14d
       je       G_M000_IG14
       mov      eax, dword ptr [r15]
       movzx    rcx, word  ptr [r15+0x04]
       mov      r15, bword ptr [rbp-0x38]
       xor      edx, edx
       cmp      edx, r14d
       jge      G_M000_IG13
       align    [0 bytes for IG06]
 
G_M000_IG06:
       mov      eax, dword ptr [r15+4*rdx]
       xor      edi, edi
       mov      dword ptr [rbp-0x30], edi
       cmp      eax, 16
       jb       G_M000_IG11
 
G_M000_IG07:
       cmp      eax, 32
       jb       SHORT G_M000_IG10
       cmp      eax, 32
       jb       SHORT G_M000_IG08
       cmp      eax, 48
       jb       SHORT G_M000_IG09
 
G_M000_IG08:
       cmp      eax, 48
       jb       G_M000_IG12
       cmp      eax, 64
       jae      SHORT G_M000_IG12
       mov      rdi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdi], xmm0
       jmp      SHORT G_M000_IG12
 
G_M000_IG09:
       mov      rdi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdi], xmm0
       jmp      SHORT G_M000_IG12
 
G_M000_IG10:
       lea      edi, [rax-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rbp-0x30], xmm0
       mov      rdi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x30]
       vmovss   dword ptr [rdi], xmm0
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:
       mov      rdi, bword ptr [rbx]
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdi], xmm0
 
G_M000_IG12:
       inc      edx
       cmp      edx, r14d
       jl       G_M000_IG06
 
G_M000_IG13:
       mov      edx, 1
       mov      edi, 9
       cmp      eax, 63
       cmovae   edx, edi
       mov      dword ptr [r13], eax
       mov      word  ptr [r13+0x04], cx
       mov      word  ptr [r13+0x06], r12w
       mov      word  ptr [r13+0x08], dx
       jmp      SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rax, qword ptr [r15]
       mov      qword ptr [r13], rax
       mov      eax, dword ptr [r15+0x08]
       mov      dword ptr [r13+0x08], eax
 
G_M000_IG15:
       mov      eax, 1
       cmp      qword ptr [rbp-0x40], 0xD1FFAB1E
       je       SHORT G_M000_IG16
       call     CORINFO_HELP_FAIL_FAST
 
G_M000_IG16:
       nop      
 
G_M000_IG17:
       lea      rsp, [rbp-0x28]
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG18:
       mov      rax, qword ptr [r15]
       mov      qword ptr [r13], rax
       mov      eax, dword ptr [r15+0x08]
       mov      dword ptr [r13+0x08], eax
       xor      eax, eax
       cmp      qword ptr [rbp-0x40], 0xD1FFAB1E
       je       SHORT G_M000_IG19
       call     CORINFO_HELP_FAIL_FAST
 
G_M000_IG19:
       nop      
 
G_M000_IG20:
       lea      rsp, [rbp-0x28]
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG21:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dd	40A00000h		;         5
RWD04  	dd	40400000h		;         3
RWD08  	dd	41700000h		;        15
RWD12  	dd	40000000h		;         2
RWD16  	dd	3F800000h		;         1

; Total bytes of code 531

; Assembly listing for method CombatBenchmarks:PublicScalar():CombatReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 125 single block inlinees; 16 inlinees without PGO data

G_M000_IG01:
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
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      qword ptr [rbp-0x30], rsi
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rsi, qword ptr [rbx+0x18]
       mov      qword ptr [rbp-0x38], rsi
       vmovss   xmm0, dword ptr [rbx+0x24]
       vmovss   dword ptr [rbp-0x40], xmm0
       lea      r14, bword ptr [rbx+0x18]
       lea      r13, bword ptr [rbx+0x20]
       lea      r12, bword ptr [rbx+0x24]
       lea      rax, bword ptr [rbx+0x28]
       test     r13, r13
       mov      r15, qword ptr [rbp-0x30]
       je       SHORT G_M000_IG04
 
G_M000_IG03:
       test     r12, r12
       je       SHORT G_M000_IG04
       test     r14, r14
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       xor      ecx, ecx
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       test     rax, rax
       setne    cl
       movzx    rcx, cl
 
G_M000_IG06:
       mov      bword ptr [rbp-0x90], rax
       mov      dword ptr [rbp-0x84], ecx
       movzx    rdx, word  ptr [rbx+0x14]
       mov      dword ptr [rbp-0x6C], edx
       lea      rsi, [rbp-0x78]
       mov      edi, edx
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       xor      r8d, r8d
       mov      dword ptr [rbp-0x68], r8d
       mov      word  ptr [rbp-0x64], 0
       mov      r8d, dword ptr [rbp-0x6C]
       mov      word  ptr [rbp-0x62], r8w
       mov      word  ptr [rbp-0x60], 1
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       xor      r8d, r8d
       mov      qword ptr [rbp-0x68], r8
       mov      dword ptr [rbp-0x60], r8d
 
G_M000_IG09:
       xor      r9d, r9d
       mov      r8, gword ptr [rbx+0x08]
       xor      esi, esi
       jmp      G_M000_IG24
 
G_M000_IG10:
       movzx    rdi, word  ptr [rbp-0x62]
       cmp      edi, ecx
       jne      G_M000_IG22
       movzx    rcx, word  ptr [rbp-0x60]
       and      ecx, 3
       cmp      ecx, 1
       jne      G_M000_IG22
       mov      ecx, dword ptr [rbp-0x84]
       test     ecx, ecx
       je       G_M000_IG22
       mov      edi, dword ptr [rbp-0x68]
       movzx    rax, word  ptr [rbp-0x64]
       xor      r10d, r10d
       mov      qword ptr [rbp-0x80], r10
       cmp      edx, 8
       jb       G_M000_IG70
       cmp      edx, 16
       jb       G_M000_IG60
       cmp      edx, 16
       jb       SHORT G_M000_IG11
       cmp      edx, 32
       jb       G_M000_IG50
 
G_M000_IG11:
       cmp      edx, 32
       jb       SHORT G_M000_IG12
       cmp      edx, 48
       jb       G_M000_IG37
 
G_M000_IG12:
       cmp      edx, 48
       jb       SHORT G_M000_IG13
       cmp      edx, 56
       jb       G_M000_IG30
 
G_M000_IG13:
       cmp      edx, 56
       jb       SHORT G_M000_IG14
       cmp      edx, 63
       jb       G_M000_IG20
 
G_M000_IG14:
       cmp      edx, 63
       jb       G_M000_IG20
       cmp      edx, 64
       jae      G_M000_IG20
       cmp      edx, 63
       jne      SHORT G_M000_IG15
       mov      r10d, 2
       jmp      SHORT G_M000_IG17
 
G_M000_IG15:
       cmp      edi, 63
       jae      SHORT G_M000_IG16
       xor      r10d, r10d
       jmp      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      r10d, 1
 
G_M000_IG17:
       movzx    rdi, r10b
       vmovss   xmm0, dword ptr [r12]
       vxorps   xmm1, xmm1, xmm1
       mov      r10, bword ptr [rbp-0x90]
       vmulss   xmm1, xmm1, dword ptr [r10]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rbp-0x40], xmm0
       lea      r11d, [rdx+0x01]
       add      r11, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], r11
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
       cmp      edi, 2
       ja       G_M000_IG29
 
G_M000_IG18:
       mov      edi, edi
       lea      r11, [reloc @RWD00]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rcx, G_M000_IG02
       add      r11, rcx
       jmp      r11
 
G_M000_IG19:
       mov      ecx, dword ptr [rbp-0x4C]
       inc      ecx
       mov      dword ptr [rbp-0x4C], ecx
 
G_M000_IG20:
       mov      edi, 1
       cmp      edx, 63
       jb       SHORT G_M000_IG21
       mov      edi, 9
 
G_M000_IG21:
       mov      r11d, 1
       mov      ecx, 1
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:
       mov      edx, dword ptr [rbp-0x68]
       movzx    rax, word  ptr [rbp-0x64]
       movzx    r11, word  ptr [rbp-0x62]
       movzx    rdi, word  ptr [rbp-0x60]
       xor      ecx, ecx
 
G_M000_IG23:
       test     ecx, ecx
       setne    cl
       movzx    rcx, cl
       add      r9d, ecx
       mov      dword ptr [rbp-0x68], edx
       mov      word  ptr [rbp-0x64], ax
       mov      word  ptr [rbp-0x62], r11w
       mov      word  ptr [rbp-0x60], di
       inc      esi
 
G_M000_IG24:
       cmp      dword ptr [r8+0x08], esi
       jle      SHORT G_M000_IG27
 
G_M000_IG25:
       mov      edx, dword ptr [r8+4*rsi+0x10]
       movzx    rcx, word  ptr [rbx+0x14]
       cmp      ecx, 1
       je       G_M000_IG10
 
G_M000_IG26:
       mov      edx, dword ptr [rbp-0x68]
       movzx    rax, word  ptr [rbp-0x64]
       movzx    r11, word  ptr [rbp-0x62]
       movzx    rdi, word  ptr [rbp-0x60]
       xor      ecx, ecx
       jmp      SHORT G_M000_IG23
 
G_M000_IG27:
       lea      r8, [rbp-0x58]
       lea      rsi, [rbp-0x68]
       lea      rdx, [rbp-0x38]
       lea      rcx, [rbp-0x40]
       mov      rdi, r15
       call     [CombatReceipt:Capture(byref,byref,byref,byref,int):CombatReceipt]
       mov      rax, r15
 
G_M000_IG28:
       add      rsp, 104
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG29:
       jmp      G_M000_IG20
 
G_M000_IG30:
       cmp      edx, 55
       jne      SHORT G_M000_IG31
       mov      r11d, 2
       jmp      SHORT G_M000_IG33
 
G_M000_IG31:
       cmp      edi, 8
       jae      SHORT G_M000_IG32
       xor      r11d, r11d
       jmp      SHORT G_M000_IG33
 
G_M000_IG32:
       mov      r11d, 1
 
G_M000_IG33:
       movzx    rdi, r11b
       vmovss   xmm0, dword ptr [r12]
       mov      r10, bword ptr [rbp-0x90]
       vmovss   xmm1, dword ptr [r10]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rbp-0x40], xmm0
       lea      r11d, [rdx+0x01]
       add      r11, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], r11
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
       cmp      edi, 2
       ja       SHORT G_M000_IG36
 
G_M000_IG34:
       mov      edi, edi
       lea      r11, [reloc @RWD16]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rcx, G_M000_IG02
       add      r11, rcx
       jmp      r11
 
G_M000_IG35:
       mov      ecx, dword ptr [rbp-0x48]
       inc      ecx
       mov      dword ptr [rbp-0x48], ecx
       jmp      G_M000_IG20
 
G_M000_IG36:
       jmp      G_M000_IG20
 
G_M000_IG37:
       cmp      edx, 47
       jne      SHORT G_M000_IG38
       mov      r11d, 2
       jmp      SHORT G_M000_IG40
 
G_M000_IG38:
       cmp      edi, 16
       jae      SHORT G_M000_IG39
       xor      r11d, r11d
       jmp      SHORT G_M000_IG40
 
G_M000_IG39:
       mov      r11d, 1
 
G_M000_IG40:
       movzx    r11, r11b
       vmovss   xmm0, dword ptr [r14]
       vmovss   xmm1, dword ptr [r13]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, xmm2
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm1, xmm1, dword ptr [r14+0x04]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   dword ptr [rbp-0x34], xmm1
       mov      ecx, edx
       add      rcx, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], rcx
       mov      ecx, dword ptr [rbp-0x50]
       inc      ecx
       mov      dword ptr [rbp-0x50], ecx
       cmp      r11d, 2
       ja       SHORT G_M000_IG45
 
G_M000_IG41:
       mov      qword ptr [rbp-0x30], r15
       mov      ecx, r11d
       lea      r11, [reloc @RWD36]
       mov      r11d, dword ptr [r11+4*rcx]
       lea      r15, G_M000_IG02
       add      r11, r15
       jmp      r11
 
G_M000_IG42:
       mov      ecx, dword ptr [rbp-0x44]
       inc      ecx
       mov      dword ptr [rbp-0x44], ecx
       mov      r15, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG45
 
G_M000_IG43:
       mov      ecx, dword ptr [rbp-0x48]
       inc      ecx
       mov      dword ptr [rbp-0x48], ecx
       mov      r15, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG45
 
G_M000_IG44:
       mov      ecx, dword ptr [rbp-0x4C]
       inc      ecx
       mov      dword ptr [rbp-0x4C], ecx
       mov      r15, qword ptr [rbp-0x30]
 
G_M000_IG45:
       cmp      edi, 8
       jae      SHORT G_M000_IG46
       xor      edi, edi
       jmp      SHORT G_M000_IG47
 
G_M000_IG46:
       mov      edi, 1
 
G_M000_IG47:
       movzx    rcx, dil
       vmovss   xmm0, dword ptr [r12]
       mov      r10, bword ptr [rbp-0x90]
       vmovss   xmm1, dword ptr [r10]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rbp-0x40], xmm0
       lea      edi, [rdx+0x01]
       add      rdi, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], rdi
       mov      edi, dword ptr [rbp-0x50]
       inc      edi
       mov      dword ptr [rbp-0x50], edi
       cmp      ecx, 2
       ja       SHORT G_M000_IG49
 
G_M000_IG48:
       mov      ecx, ecx
       lea      rdi, [reloc @RWD48]
       mov      edi, dword ptr [rdi+4*rcx]
       lea      r11, G_M000_IG02
       add      rdi, r11
       jmp      rdi
 
G_M000_IG49:
       jmp      G_M000_IG20
 
G_M000_IG50:
       mov      r11d, 1
       movzx    r11, r11b
       lea      ecx, [rdx-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD60]
       vmulss   xmm1, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD68]
       vaddss   xmm0, xmm0, xmm0
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD72]
       vmovss   dword ptr [rbp-0x80], xmm1
       vmovss   dword ptr [rbp-0x7C], xmm0
       vmovss   xmm0, dword ptr [r14]
       vmovss   xmm1, dword ptr [rbp-0x80]
       vmulss   xmm1, xmm1, dword ptr [r13]
       vaddss   xmm0, xmm0, xmm1
       vmovss   xmm1, dword ptr [r14+0x04]
       vmovss   xmm2, dword ptr [rbp-0x7C]
       vmulss   xmm2, xmm2, dword ptr [r13]
       vaddss   xmm1, xmm1, xmm2
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   dword ptr [rbp-0x34], xmm1
       mov      ecx, edx
       add      rcx, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], rcx
       mov      ecx, dword ptr [rbp-0x50]
       inc      ecx
       mov      dword ptr [rbp-0x50], ecx
       cmp      r11d, 2
       ja       SHORT G_M000_IG55
 
G_M000_IG51:
       mov      qword ptr [rbp-0x30], r15
       mov      ecx, r11d
       lea      r11, [reloc @RWD76]
       mov      r11d, dword ptr [r11+4*rcx]
       lea      r15, G_M000_IG02
       add      r11, r15
       jmp      r11
 
G_M000_IG52:
       mov      ecx, dword ptr [rbp-0x44]
       inc      ecx
       mov      dword ptr [rbp-0x44], ecx
       mov      r15, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG55
 
G_M000_IG53:
       mov      ecx, dword ptr [rbp-0x48]
       inc      ecx
       mov      dword ptr [rbp-0x48], ecx
       mov      r15, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG55
 
G_M000_IG54:
       mov      ecx, dword ptr [rbp-0x4C]
       inc      ecx
       mov      dword ptr [rbp-0x4C], ecx
       mov      r15, qword ptr [rbp-0x30]
 
G_M000_IG55:
       cmp      edi, 8
       jae      SHORT G_M000_IG56
       xor      edi, edi
       jmp      SHORT G_M000_IG57
 
G_M000_IG56:
       mov      edi, 1
 
G_M000_IG57:
       movzx    rcx, dil
       vmovss   xmm0, dword ptr [r12]
       mov      r10, bword ptr [rbp-0x90]
       vmovss   xmm1, dword ptr [r10]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rbp-0x40], xmm0
       lea      edi, [rdx+0x01]
       add      rdi, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], rdi
       mov      edi, dword ptr [rbp-0x50]
       inc      edi
       mov      dword ptr [rbp-0x50], edi
       cmp      ecx, 2
       ja       SHORT G_M000_IG59
 
G_M000_IG58:
       mov      ecx, ecx
       lea      rdi, [reloc @RWD88]
       mov      edi, dword ptr [rdi+4*rcx]
       lea      r11, G_M000_IG02
       add      rdi, r11
       jmp      rdi
 
G_M000_IG59:
       jmp      G_M000_IG20
 
G_M000_IG60:
       mov      r11d, 1
       movzx    r11, r11b
       vmovss   xmm0, dword ptr [r14]
       vmovss   xmm1, dword ptr [r13]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD68]
       vaddss   xmm0, xmm0, xmm2
       vaddss   xmm1, xmm1, dword ptr [r14+0x04]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   dword ptr [rbp-0x34], xmm1
       mov      ecx, edx
       add      rcx, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], rcx
       mov      ecx, dword ptr [rbp-0x50]
       inc      ecx
       mov      dword ptr [rbp-0x50], ecx
       cmp      r11d, 2
       ja       SHORT G_M000_IG65
 
G_M000_IG61:
       mov      qword ptr [rbp-0x30], r15
       mov      ecx, r11d
       lea      r11, [reloc @RWD100]
       mov      r11d, dword ptr [r11+4*rcx]
       lea      r15, G_M000_IG02
       add      r11, r15
       jmp      r11
 
G_M000_IG62:
       mov      ecx, dword ptr [rbp-0x44]
       inc      ecx
       mov      dword ptr [rbp-0x44], ecx
       mov      r15, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG65
 
G_M000_IG63:
       mov      ecx, dword ptr [rbp-0x48]
       inc      ecx
       mov      dword ptr [rbp-0x48], ecx
       mov      r15, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG65
 
G_M000_IG64:
       mov      ecx, dword ptr [rbp-0x4C]
       inc      ecx
       mov      dword ptr [rbp-0x4C], ecx
       mov      r15, qword ptr [rbp-0x30]
 
G_M000_IG65:
       cmp      edi, 8
       jae      SHORT G_M000_IG66
       xor      ecx, ecx
       jmp      SHORT G_M000_IG67
 
G_M000_IG66:
       mov      ecx, 1
 
G_M000_IG67:
       movzx    rcx, cl
       vmovss   xmm0, dword ptr [r12]
       mov      r10, bword ptr [rbp-0x90]
       vmovss   xmm1, dword ptr [r10]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rbp-0x40], xmm0
       lea      edi, [rdx+0x01]
       add      rdi, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], rdi
       mov      edi, dword ptr [rbp-0x50]
       inc      edi
       mov      dword ptr [rbp-0x50], edi
       cmp      ecx, 2
       ja       SHORT G_M000_IG69
 
G_M000_IG68:
       mov      ecx, ecx
       lea      rdi, [reloc @RWD112]
       mov      edi, dword ptr [rdi+4*rcx]
       lea      r11, G_M000_IG02
       add      rdi, r11
       jmp      rdi
 
G_M000_IG69:
       jmp      G_M000_IG20
 
G_M000_IG70:
       mov      edi, 1
       movzx    rdi, dil
       vmovss   xmm0, dword ptr [r14]
       vmovss   xmm1, dword ptr [r13]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD68]
       vaddss   xmm0, xmm0, xmm2
       vaddss   xmm1, xmm1, dword ptr [r14+0x04]
       vmovss   dword ptr [rbp-0x38], xmm0
       vmovss   dword ptr [rbp-0x34], xmm1
       mov      r11d, edx
       add      r11, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x58], r11
       mov      r11d, dword ptr [rbp-0x50]
       inc      r11d
       mov      dword ptr [rbp-0x50], r11d
       cmp      edi, 2
       ja       G_M000_IG20
 
G_M000_IG71:
       mov      edi, edi
       lea      r11, [reloc @RWD124]
       mov      r11d, dword ptr [r11+4*rdi]
       lea      rcx, G_M000_IG02
       add      r11, rcx
       jmp      r11
 
G_M000_IG72:
       mov      ecx, dword ptr [rbp-0x44]
       inc      ecx
       mov      dword ptr [rbp-0x44], ecx
       jmp      G_M000_IG20
 
RWD00  	dd	G_M000_IG19 - G_M000_IG02
       	dd	G_M000_IG35 - G_M000_IG02
       	dd	G_M000_IG72 - G_M000_IG02
RWD12  	dd	40A00000h		;         5
RWD16  	dd	G_M000_IG19 - G_M000_IG02
       	dd	G_M000_IG35 - G_M000_IG02
       	dd	G_M000_IG72 - G_M000_IG02
RWD28  	dd	40C00000h		;         6
RWD32  	dd	40400000h		;         3
RWD36  	dd	G_M000_IG44 - G_M000_IG02
       	dd	G_M000_IG43 - G_M000_IG02
       	dd	G_M000_IG42 - G_M000_IG02
RWD48  	dd	G_M000_IG19 - G_M000_IG02
       	dd	G_M000_IG35 - G_M000_IG02
       	dd	G_M000_IG72 - G_M000_IG02
RWD60  	dd	41700000h		;        15
RWD64  	dd	40800000h		;         4
RWD68  	dd	40000000h		;         2
RWD72  	dd	3F800000h		;         1
RWD76  	dd	G_M000_IG54 - G_M000_IG02
       	dd	G_M000_IG53 - G_M000_IG02
       	dd	G_M000_IG52 - G_M000_IG02
RWD88  	dd	G_M000_IG19 - G_M000_IG02
       	dd	G_M000_IG35 - G_M000_IG02
       	dd	G_M000_IG72 - G_M000_IG02
RWD100 	dd	G_M000_IG64 - G_M000_IG02
       	dd	G_M000_IG63 - G_M000_IG02
       	dd	G_M000_IG62 - G_M000_IG02
RWD112 	dd	G_M000_IG19 - G_M000_IG02
       	dd	G_M000_IG35 - G_M000_IG02
       	dd	G_M000_IG72 - G_M000_IG02
RWD124 	dd	G_M000_IG19 - G_M000_IG02
       	dd	G_M000_IG35 - G_M000_IG02
       	dd	G_M000_IG72 - G_M000_IG02

; Total bytes of code 1760

; Assembly listing for method CombatBenchmarks:PublicBatch8():CombatReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 11 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 192
       lea      rbp, [rsp+0xE0]
       xor      eax, eax
       mov      qword ptr [rbp-0xD8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0xD0], ymm8
       vmovdqu  ymmword ptr [rbp-0xB0], ymm8
       vmovdqu  ymmword ptr [rbp-0x90], ymm8
       vmovdqu  ymmword ptr [rbp-0x70], ymm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       lea      rsi, bword ptr [rbx+0x18]
       mov      rdi, qword ptr [rsi]
       mov      qword ptr [rbp-0x28], rdi
       vmovss   xmm0, dword ptr [rbx+0x24]
       vmovss   dword ptr [rbp-0x30], xmm0
       lea      rdi, bword ptr [rbx+0x20]
       lea      rax, bword ptr [rbx+0x24]
       lea      rcx, bword ptr [rbx+0x28]
       test     rdi, rdi
       je       SHORT G_M000_IG04
 
G_M000_IG03:
       test     rax, rax
       jne      SHORT G_M000_IG05
 
G_M000_IG04:
       xor      edx, edx
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       test     rcx, rcx
       setne    dl
       movzx    rdx, dl
 
G_M000_IG06:
       mov      bword ptr [rbp-0x70], rdi
       mov      bword ptr [rbp-0x68], rax
       mov      bword ptr [rbp-0x60], rsi
       mov      bword ptr [rbp-0x58], rcx
       mov      byte  ptr [rbp-0x50], dl
       lea      rsi, bword ptr [rbp-0x30]
       mov      bword ptr [rbp-0xD0], rsi
       lea      rsi, bword ptr [rbp-0x28]
       mov      bword ptr [rbp-0xC8], rsi
       lea      rsi, bword ptr [rbp-0x48]
       mov      bword ptr [rbp-0xC0], rsi
       mov      byte  ptr [rbp-0xB8], 1
 
G_M000_IG07:
       vmovdqu  ymm0, ymmword ptr [rbp-0xD0]
       vmovdqu  ymmword ptr [rbp-0x90], ymm0
 
G_M000_IG08:
       movzx    r14, word  ptr [rbx+0x14]
       lea      rsi, [rbp-0xD8]
       mov      edi, r14d
       call     [Tl.Timeline:TryReadSlot(ushort,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       xor      edx, edx
       mov      dword ptr [rbp-0xA0], edx
       mov      word  ptr [rbp-0x9C], 0
       mov      word  ptr [rbp-0x9A], r14w
       mov      word  ptr [rbp-0x98], 1
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       xor      edx, edx
       mov      qword ptr [rbp-0xA0], rdx
       mov      dword ptr [rbp-0x98], edx
 
G_M000_IG11:
       xor      r14d, r14d
       xor      r13d, r13d
       jmp      SHORT G_M000_IG15
 
G_M000_IG12:
       lea      rcx, [rbp-0xB0]
       mov      qword ptr [rsp], rcx
       mov      ecx, 8
       lea      rsi, [rbp-0xA0]
       lea      r8, [rbp-0x70]
       lea      r9, [rbp-0x90]
       call     [CombatTimeline:ForwardKernel(ushort,byref,System.ReadOnlySpan`1[uint],byref,byref,byref):bool]
       test     eax, eax
       je       SHORT G_M000_IG18
 
G_M000_IG13:
       mov      eax, 8
 
G_M000_IG14:
       add      r14d, eax
       mov      rax, qword ptr [rbp-0xB0]
       mov      qword ptr [rbp-0xA0], rax
       mov      eax, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0x98], eax
       add      r13d, 8
 
G_M000_IG15:
       mov      rdx, gword ptr [rbx+0x08]
       cmp      dword ptr [rdx+0x08], r13d
       jle      SHORT G_M000_IG19
 
G_M000_IG16:
       test     rdx, rdx
       je       SHORT G_M000_IG21
       mov      ecx, dword ptr [rdx+0x08]
       mov      esi, r13d
       lea      r8, [rsi+0x08]
       cmp      rcx, r8
       jb       SHORT G_M000_IG21
       lea      rdx, bword ptr [rdx+4*rsi+0x10]
       movzx    rdi, word  ptr [rbx+0x14]
       cmp      edi, 1
       je       SHORT G_M000_IG12
 
G_M000_IG17:
       mov      rax, qword ptr [rbp-0xA0]
       mov      qword ptr [rbp-0xB0], rax
       mov      eax, dword ptr [rbp-0x98]
       mov      dword ptr [rbp-0xA8], eax
 
G_M000_IG18:
       xor      eax, eax
       jmp      SHORT G_M000_IG14
 
G_M000_IG19:
       lea      r8, [rbp-0x48]
       lea      rsi, [rbp-0xA0]
       lea      rdx, [rbp-0x28]
       lea      rcx, [rbp-0x30]
       mov      rdi, r15
       mov      r9d, r14d
       call     [CombatReceipt:Capture(byref,byref,byref,byref,int):CombatReceipt]
       mov      rax, r15
 
G_M000_IG20:
       vzeroupper 
       add      rsp, 192
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG21:
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
; Total bytes of code 512

; Assembly listing for method CombatTimeline:ForwardKernel(ushort,byref,System.ReadOnlySpan`1[uint],byref,byref,byref):bool (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 117 single block inlinees; 14 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      qword ptr [rbp-0x40], 0xD1FFAB1E
       mov      r13d, edi
       mov      r14, rsi
       mov      rsi, rdx
       mov      r12d, ecx
       mov      rbx, r8
       mov      r15, r9
 
G_M000_IG02:
       movzx    rax, r13w
       cmp      eax, 1
       jne      G_M000_IG12
 
G_M000_IG03:
       movzx    rax, word  ptr [r14+0x06]
       movzx    rdx, r13w
       cmp      eax, edx
       jne      G_M000_IG12
       movzx    rax, word  ptr [r14+0x08]
       and      eax, 3
       cmp      eax, 1
       jne      G_M000_IG12
       cmp      byte  ptr [rbx+0x20], 0
       je       G_M000_IG12
       cmp      byte  ptr [r15+0x18], 0
       je       G_M000_IG12
       cmp      r12d, 256
       jg       G_M000_IG12
       mov      eax, r12d
       mov      edi, 4
       mul      rdx:rax, rdi
       jb       G_M000_IG83
       test     rax, rax
       je       SHORT G_M000_IG05
       add      rax, 15
       shr      rax, 4
 
G_M000_IG04:
       push     0
       push     0
       dec      rax
       jne      SHORT G_M000_IG04
       lea      rax, [rsp]
 
G_M000_IG05:
       mov      bword ptr [rbp-0x38], rax
       mov      edx, r12d
       shl      rdx, 2
       mov      rdi, rax
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       test     r12d, r12d
       je       SHORT G_M000_IG08
       mov      eax, dword ptr [r14]
       movzx    rcx, word  ptr [r14+0x04]
       mov      r14, bword ptr [rbp-0x38]
       xor      edx, edx
       cmp      edx, r12d
       jl       G_M000_IG76
 
G_M000_IG06:
       mov      edi, eax
 
G_M000_IG07:
       mov      eax, 1
       mov      edx, 9
       cmp      edi, 63
       cmovae   eax, edx
       movzx    rdx, r13w
       mov      rbx, bword ptr [rbp+0x10]
       mov      dword ptr [rbx], edi
       mov      word  ptr [rbx+0x04], cx
       mov      word  ptr [rbx+0x06], dx
       mov      word  ptr [rbx+0x08], ax
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rbx, bword ptr [rbp+0x10]
       mov      rax, qword ptr [r14]
       mov      qword ptr [rbx], rax
       mov      eax, dword ptr [r14+0x08]
       mov      dword ptr [rbx+0x08], eax
 
G_M000_IG09:
       mov      eax, 1
       cmp      qword ptr [rbp-0x40], 0xD1FFAB1E
       je       SHORT G_M000_IG10
       call     CORINFO_HELP_FAIL_FAST
 
G_M000_IG10:
       nop      
 
G_M000_IG11:
       lea      rsp, [rbp-0x28]
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG12:
       mov      rcx, bword ptr [rbp+0x10]
       mov      rax, qword ptr [r14]
       mov      qword ptr [rcx], rax
       mov      eax, dword ptr [r14+0x08]
       mov      dword ptr [rcx+0x08], eax
       xor      eax, eax
       cmp      qword ptr [rbp-0x40], 0xD1FFAB1E
       je       SHORT G_M000_IG13
       call     CORINFO_HELP_FAIL_FAST
 
G_M000_IG13:
       nop      
 
G_M000_IG14:
       lea      rsp, [rbp-0x28]
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG15:
       cmp      eax, 63
       jae      SHORT G_M000_IG16
       xor      esi, esi
       jmp      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      esi, 1
 
G_M000_IG17:
       movzx    rax, sil
       mov      rsi, bword ptr [rbx+0x08]
       mov      r8, bword ptr [rbx+0x18]
       mov      r9, bword ptr [r15]
       mov      r10, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [rsi]
       vxorps   xmm1, xmm1, xmm1
       vmulss   xmm1, xmm1, dword ptr [r8]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [r9], xmm0
       lea      esi, [rdi+0x01]
       add      qword ptr [r10], rsi
       inc      dword ptr [r10+0x08]
       cmp      eax, 2
       ja       G_M000_IG74
 
G_M000_IG18:
       mov      eax, eax
       lea      rsi, [reloc @RWD00]
       mov      esi, dword ptr [rsi+4*rax]
       lea      r8, G_M000_IG02
       add      rsi, r8
       jmp      rsi
 
G_M000_IG19:
       inc      dword ptr [r10+0x14]
       jmp      G_M000_IG74
 
G_M000_IG20:
       inc      dword ptr [r10+0x10]
       jmp      G_M000_IG74
 
G_M000_IG21:
       inc      dword ptr [r10+0x0C]
       jmp      G_M000_IG74
 
G_M000_IG22:
       cmp      edi, 55
       jne      SHORT G_M000_IG23
       mov      esi, 2
       jmp      SHORT G_M000_IG25
 
G_M000_IG23:
       cmp      eax, 8
       jae      SHORT G_M000_IG24
       xor      esi, esi
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:
       mov      esi, 1
 
G_M000_IG25:
       movzx    rax, sil
       mov      rsi, bword ptr [rbx+0x08]
       mov      r8, bword ptr [rbx+0x18]
       mov      r9, bword ptr [r15]
       mov      r10, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [rsi]
       vmovss   xmm1, dword ptr [r8]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [r9], xmm0
       lea      esi, [rdi+0x01]
       add      qword ptr [r10], rsi
       inc      dword ptr [r10+0x08]
       cmp      eax, 2
       ja       G_M000_IG74
 
G_M000_IG26:
       mov      eax, eax
       lea      rsi, [reloc @RWD16]
       mov      esi, dword ptr [rsi+4*rax]
       lea      r8, G_M000_IG02
       add      rsi, r8
       jmp      rsi
 
G_M000_IG27:
       inc      dword ptr [r10+0x14]
       jmp      G_M000_IG74
 
G_M000_IG28:
       inc      dword ptr [r10+0x10]
       jmp      G_M000_IG74
 
G_M000_IG29:
       inc      dword ptr [r10+0x0C]
       jmp      G_M000_IG74
 
G_M000_IG30:
       cmp      edi, 47
       jne      SHORT G_M000_IG31
       mov      esi, 2
       jmp      SHORT G_M000_IG33
 
G_M000_IG31:
       cmp      eax, 16
       jae      SHORT G_M000_IG32
       xor      esi, esi
       jmp      SHORT G_M000_IG33
 
G_M000_IG32:
       mov      esi, 1
 
G_M000_IG33:
       movzx    rsi, sil
       mov      r8, bword ptr [rbx+0x10]
       mov      r9, bword ptr [rbx]
       mov      r10, bword ptr [r15+0x08]
       mov      r11, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [r8]
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, xmm2
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm1, xmm1, dword ptr [r8+0x04]
       vmovss   dword ptr [r10], xmm0
       vmovss   dword ptr [r10+0x04], xmm1
       mov      r8d, edi
       add      qword ptr [r11], r8
       inc      dword ptr [r11+0x08]
       cmp      esi, 2
       ja       SHORT G_M000_IG38
 
G_M000_IG34:
       mov      esi, esi
       lea      r8, [reloc @RWD36]
       mov      r8d, dword ptr [r8+4*rsi]
       lea      r9, G_M000_IG02
       add      r8, r9
       jmp      r8
 
G_M000_IG35:
       inc      dword ptr [r11+0x14]
       jmp      SHORT G_M000_IG38
 
G_M000_IG36:
       inc      dword ptr [r11+0x10]
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:
       inc      dword ptr [r11+0x0C]
 
G_M000_IG38:
       cmp      eax, 8
       jae      SHORT G_M000_IG39
       xor      eax, eax
       jmp      SHORT G_M000_IG40
 
G_M000_IG39:
       mov      eax, 1
 
G_M000_IG40:
       movzx    rax, al
       mov      rsi, bword ptr [rbx+0x08]
       mov      r8, bword ptr [rbx+0x18]
       mov      r9, bword ptr [r15]
       mov      r10, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [rsi]
       vmovss   xmm1, dword ptr [r8]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [r9], xmm0
       lea      esi, [rdi+0x01]
       add      qword ptr [r10], rsi
       inc      dword ptr [r10+0x08]
       cmp      eax, 2
       ja       G_M000_IG74
 
G_M000_IG41:
       mov      eax, eax
       lea      rsi, [reloc @RWD48]
       mov      esi, dword ptr [rsi+4*rax]
       lea      r8, G_M000_IG02
       add      rsi, r8
       jmp      rsi
 
G_M000_IG42:
       inc      dword ptr [r10+0x14]
       jmp      G_M000_IG74
 
G_M000_IG43:
       inc      dword ptr [r10+0x10]
       jmp      G_M000_IG74
 
G_M000_IG44:
       inc      dword ptr [r10+0x0C]
       jmp      G_M000_IG74
 
G_M000_IG45:
       mov      esi, 1
       movzx    rsi, sil
       lea      r8d, [rdi-0x10]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, r8
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD60]
       vmulss   xmm1, xmm0, dword ptr [reloc @RWD64]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD68]
       vaddss   xmm0, xmm0, xmm0
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD72]
       vmovss   dword ptr [rbp-0x30], xmm1
       vmovss   dword ptr [rbp-0x2C], xmm0
       mov      r8, bword ptr [rbx+0x10]
       mov      r9, bword ptr [rbx]
       mov      r10, bword ptr [r15+0x08]
       mov      r11, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [r8]
       vmovss   xmm1, dword ptr [rbp-0x30]
       vmulss   xmm1, xmm1, dword ptr [r9]
       vaddss   xmm0, xmm0, xmm1
       vmovss   xmm1, dword ptr [r8+0x04]
       vmovss   xmm2, dword ptr [rbp-0x2C]
       vmulss   xmm2, xmm2, dword ptr [r9]
       vaddss   xmm1, xmm1, xmm2
       vmovss   dword ptr [r10], xmm0
       vmovss   dword ptr [r10+0x04], xmm1
       mov      r8d, edi
       add      qword ptr [r11], r8
       inc      dword ptr [r11+0x08]
       cmp      esi, 2
       ja       SHORT G_M000_IG50
 
G_M000_IG46:
       mov      esi, esi
       lea      r8, [reloc @RWD76]
       mov      r8d, dword ptr [r8+4*rsi]
       lea      r9, G_M000_IG02
       add      r8, r9
       jmp      r8
 
G_M000_IG47:
       inc      dword ptr [r11+0x14]
       jmp      SHORT G_M000_IG50
 
G_M000_IG48:
       inc      dword ptr [r11+0x10]
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:
       inc      dword ptr [r11+0x0C]
 
G_M000_IG50:
       cmp      eax, 8
       jae      SHORT G_M000_IG51
       xor      eax, eax
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:
       mov      eax, 1
 
G_M000_IG52:
       movzx    rax, al
       mov      rsi, bword ptr [rbx+0x08]
       mov      r8, bword ptr [rbx+0x18]
       mov      r9, bword ptr [r15]
       mov      r10, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [rsi]
       vmovss   xmm1, dword ptr [r8]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [r9], xmm0
       lea      esi, [rdi+0x01]
       add      qword ptr [r10], rsi
       inc      dword ptr [r10+0x08]
       cmp      eax, 2
       ja       G_M000_IG74
 
G_M000_IG53:
       mov      eax, eax
       lea      rsi, [reloc @RWD88]
       mov      esi, dword ptr [rsi+4*rax]
       lea      r8, G_M000_IG02
       add      rsi, r8
       jmp      rsi
 
G_M000_IG54:
       inc      dword ptr [r10+0x14]
       jmp      G_M000_IG74
 
G_M000_IG55:
       inc      dword ptr [r10+0x10]
       jmp      G_M000_IG74
 
G_M000_IG56:
       inc      dword ptr [r10+0x0C]
       jmp      G_M000_IG74
 
G_M000_IG57:
       mov      esi, 1
       movzx    rsi, sil
       mov      r8, bword ptr [rbx+0x10]
       mov      r9, bword ptr [rbx]
       mov      r10, bword ptr [r15+0x08]
       mov      r11, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [r8]
       vmovss   xmm1, dword ptr [r9]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD68]
       vaddss   xmm0, xmm0, xmm2
       vaddss   xmm1, xmm1, dword ptr [r8+0x04]
       vmovss   dword ptr [r10], xmm0
       vmovss   dword ptr [r10+0x04], xmm1
       mov      r8d, edi
       add      qword ptr [r11], r8
       inc      dword ptr [r11+0x08]
       cmp      esi, 2
       ja       SHORT G_M000_IG62
 
G_M000_IG58:
       mov      esi, esi
       lea      r8, [reloc @RWD100]
       mov      r8d, dword ptr [r8+4*rsi]
       lea      r9, G_M000_IG02
       add      r8, r9
       jmp      r8
 
G_M000_IG59:
       inc      dword ptr [r11+0x14]
       jmp      SHORT G_M000_IG62
 
G_M000_IG60:
       inc      dword ptr [r11+0x10]
       jmp      SHORT G_M000_IG62
 
G_M000_IG61:
       inc      dword ptr [r11+0x0C]
 
G_M000_IG62:
       cmp      eax, 8
       jae      SHORT G_M000_IG63
       xor      eax, eax
       jmp      SHORT G_M000_IG64
 
G_M000_IG63:
       mov      eax, 1
 
G_M000_IG64:
       movzx    rax, al
       mov      rsi, bword ptr [rbx+0x08]
       mov      r8, bword ptr [rbx+0x18]
       mov      r9, bword ptr [r15]
       mov      r10, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [rsi]
       vmovss   xmm1, dword ptr [r8]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [r9], xmm0
       lea      esi, [rdi+0x01]
       add      qword ptr [r10], rsi
       inc      dword ptr [r10+0x08]
       cmp      eax, 2
       ja       G_M000_IG74
 
G_M000_IG65:
       mov      eax, eax
       lea      rsi, [reloc @RWD112]
       mov      esi, dword ptr [rsi+4*rax]
       lea      r8, G_M000_IG02
       add      rsi, r8
       jmp      rsi
 
G_M000_IG66:
       inc      dword ptr [r10+0x14]
       jmp      G_M000_IG74
 
G_M000_IG67:
       inc      dword ptr [r10+0x10]
       jmp      SHORT G_M000_IG74
 
G_M000_IG68:
       inc      dword ptr [r10+0x0C]
       jmp      SHORT G_M000_IG74
 
G_M000_IG69:
       mov      eax, 1
       movzx    rax, al
       mov      rsi, bword ptr [rbx+0x10]
       mov      r8, bword ptr [rbx]
       mov      r9, bword ptr [r15+0x08]
       mov      r10, bword ptr [r15+0x10]
       vmovss   xmm0, dword ptr [rsi]
       vmovss   xmm1, dword ptr [r8]
       vmulss   xmm2, xmm1, dword ptr [reloc @RWD68]
       vaddss   xmm0, xmm0, xmm2
       vaddss   xmm1, xmm1, dword ptr [rsi+0x04]
       vmovss   dword ptr [r9], xmm0
       vmovss   dword ptr [r9+0x04], xmm1
       mov      esi, edi
       add      qword ptr [r10], rsi
       inc      dword ptr [r10+0x08]
       cmp      eax, 2
       ja       SHORT G_M000_IG74
 
G_M000_IG70:
       mov      eax, eax
       lea      rsi, [reloc @RWD124]
       mov      esi, dword ptr [rsi+4*rax]
       lea      r8, G_M000_IG02
       add      rsi, r8
       jmp      rsi
 
G_M000_IG71:
       inc      dword ptr [r10+0x14]
       jmp      SHORT G_M000_IG74
 
G_M000_IG72:
       inc      dword ptr [r10+0x10]
       jmp      SHORT G_M000_IG74
 
G_M000_IG73:
       inc      dword ptr [r10+0x0C]
 
G_M000_IG74:
       inc      edx
       cmp      edx, r12d
       jge      G_M000_IG07
 
G_M000_IG75:
       mov      eax, edi
 
G_M000_IG76:
       cmp      edx, r12d
       jae      SHORT G_M000_IG82
       mov      edi, dword ptr [r14+4*rdx]
       xor      esi, esi
       mov      qword ptr [rbp-0x30], rsi
       cmp      edi, 8
       jb       G_M000_IG69
 
G_M000_IG77:
       cmp      edi, 16
       jb       G_M000_IG57
       cmp      edi, 16
       jb       SHORT G_M000_IG78
       cmp      edi, 32
       jb       G_M000_IG45
 
G_M000_IG78:
       cmp      edi, 32
       jb       SHORT G_M000_IG79
       cmp      edi, 48
       jb       G_M000_IG30
 
G_M000_IG79:
       cmp      edi, 48
       jb       SHORT G_M000_IG80
       cmp      edi, 56
       jb       G_M000_IG22
 
G_M000_IG80:
       cmp      edi, 56
       jb       SHORT G_M000_IG81
       cmp      edi, 63
       jb       SHORT G_M000_IG74
 
G_M000_IG81:
       cmp      edi, 63
       jb       SHORT G_M000_IG74
       cmp      edi, 64
       jae      SHORT G_M000_IG74
       cmp      edi, 63
       jne      G_M000_IG15
       mov      esi, 2
       jmp      G_M000_IG17
 
G_M000_IG82:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG83:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dd	G_M000_IG21 - G_M000_IG02
       	dd	G_M000_IG20 - G_M000_IG02
       	dd	G_M000_IG19 - G_M000_IG02
RWD12  	dd	40A00000h		;         5
RWD16  	dd	G_M000_IG29 - G_M000_IG02
       	dd	G_M000_IG28 - G_M000_IG02
       	dd	G_M000_IG27 - G_M000_IG02
RWD28  	dd	40C00000h		;         6
RWD32  	dd	40400000h		;         3
RWD36  	dd	G_M000_IG37 - G_M000_IG02
       	dd	G_M000_IG36 - G_M000_IG02
       	dd	G_M000_IG35 - G_M000_IG02
RWD48  	dd	G_M000_IG44 - G_M000_IG02
       	dd	G_M000_IG43 - G_M000_IG02
       	dd	G_M000_IG42 - G_M000_IG02
RWD60  	dd	41700000h		;        15
RWD64  	dd	40800000h		;         4
RWD68  	dd	40000000h		;         2
RWD72  	dd	3F800000h		;         1
RWD76  	dd	G_M000_IG49 - G_M000_IG02
       	dd	G_M000_IG48 - G_M000_IG02
       	dd	G_M000_IG47 - G_M000_IG02
RWD88  	dd	G_M000_IG56 - G_M000_IG02
       	dd	G_M000_IG55 - G_M000_IG02
       	dd	G_M000_IG54 - G_M000_IG02
RWD100 	dd	G_M000_IG61 - G_M000_IG02
       	dd	G_M000_IG60 - G_M000_IG02
       	dd	G_M000_IG59 - G_M000_IG02
RWD112 	dd	G_M000_IG68 - G_M000_IG02
       	dd	G_M000_IG67 - G_M000_IG02
       	dd	G_M000_IG66 - G_M000_IG02
RWD124 	dd	G_M000_IG73 - G_M000_IG02
       	dd	G_M000_IG72 - G_M000_IG02
       	dd	G_M000_IG71 - G_M000_IG02

; Total bytes of code 1724


; Assembly listing for method Tl.ConsumerFusion.ConsumerBenchmarks`1[Tl.ConsumerFusion.SumConsumer]:FusedBatch8():Tl.ConsumerFusion.ConsumerReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 2
; 22 inlinees with PGO data; 52 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      rax, rsi
 
G_M000_IG02:                ;; offset=0x0016
       vxorps   xmm0, xmm0, xmm0
       mov      rcx, 0x1000000000000
       mov      qword ptr [rbp-0x30], rcx
       mov      ebx, dword ptr [rbp-0x30]
       movzx    r15, word  ptr [rbp-0x2C]
       movzx    r14, word  ptr [rbp-0x2A]
       xor      ecx, ecx
       mov      rdx, gword ptr [rdi+0x08]
       cmp      dword ptr [rdx+0x08], ecx
       jg       G_M000_IG23
 
G_M000_IG03:                ;; offset=0x0044
       vmovd    ecx, xmm0
       movzx    rdx, r15w
       movzx    rdi, r14w
       mov      dword ptr [rax], ebx
       mov      word  ptr [rax+0x04], dx
       mov      word  ptr [rax+0x06], di
       mov      dword ptr [rax+0x08], ecx
       vxorps   ymm0, ymm0, ymm0
       vmovups  ymmword ptr [rax+0x10], ymm0
 
G_M000_IG04:                ;; offset=0x0066
       vzeroupper 
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0078
       mov      edx, 5
       jmp      G_M000_IG22
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0082
       cmp      r12d, 76
       jb       SHORT G_M000_IG10
 
G_M000_IG07:                ;; offset=0x0088
       cmp      r12d, 123
       jb       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x008E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG32
 
G_M000_IG09:                ;; offset=0x009B
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       lea      r8d, [r12-0x4C]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG32
 
G_M000_IG10:                ;; offset=0x00D2
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG32
 
G_M000_IG11:                ;; offset=0x00E7
       cmp      r12d, 11
       jb       SHORT G_M000_IG16
 
G_M000_IG12:                ;; offset=0x00ED
       cmp      r12d, 18
       jb       G_M000_IG32
 
G_M000_IG13:                ;; offset=0x00F7
       cmp      r12d, 29
       jb       SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x00FD
       lea      r8d, [r12-0x1D]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG32
 
G_M000_IG15:                ;; offset=0x012C
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       jmp      G_M000_IG32
 
G_M000_IG16:                ;; offset=0x0141
       cmp      r12d, 3
       jae      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x0147
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      G_M000_IG32
 
G_M000_IG18:                ;; offset=0x0154
       cmp      r12d, 7
       jb       SHORT G_M000_IG20
 
G_M000_IG19:                ;; offset=0x015A
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       jmp      G_M000_IG32
 
G_M000_IG20:                ;; offset=0x016F
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       lea      r8d, [r12-0x03]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r8
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm1
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       jmp      G_M000_IG32
 
G_M000_IG21:                ;; offset=0x01A6
       mov      edx, 1
       cmp      r10d, 599
       je       G_M000_IG05
 
G_M000_IG22:                ;; offset=0x01B8
       mov      r8d, ebx
       mov      esi, esi
       shl      rsi, 32
       or       rsi, r8
       mov      edx, edx
       shl      rdx, 48
       or       rdx, rsi
       mov      qword ptr [rbp-0x38], rdx
       mov      ebx, dword ptr [rbp-0x38]
       movzx    r15, word  ptr [rbp-0x34]
       movzx    r14, word  ptr [rbp-0x32]
       add      ecx, 8
       mov      rdx, gword ptr [rdi+0x08]
       cmp      dword ptr [rdx+0x08], ecx
       jle      G_M000_IG03
 
G_M000_IG23:                ;; offset=0x01EE
       mov      rsi, gword ptr [rdi+0x10]
       mov      r8d, ecx
       sar      r8d, 3
       and      r8d, 3
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG40
       test     rdx, rdx
       je       G_M000_IG36
       mov      esi, dword ptr [rdx+0x08]
       mov      r8d, ecx
       add      r8, 8
       cmp      rsi, r8
       jb       G_M000_IG36
       mov      esi, ecx
       lea      rdx, bword ptr [rdx+4*rsi+0x10]
       movzx    rsi, r14w
       test     sil, 1
       je       G_M000_IG37
       movzx    rsi, r14w
       test     sil, 2
       jne      G_M000_IG38
       movzx    rsi, r15w
       mov      r8d, ebx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       imul     r9d, r8d, 600
       mov      r10d, ebx
       sub      r10d, r9d
       xor      r9d, r9d
       mov      r11d, 9
       jmp      G_M000_IG33
 
G_M000_IG24:                ;; offset=0x0273
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       jmp      SHORT G_M000_IG32
 
G_M000_IG25:                ;; offset=0x027D
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       jmp      SHORT G_M000_IG32
 
G_M000_IG26:                ;; offset=0x028F
       mov      ebx, r14d
       sub      ebx, r8d
 
G_M000_IG27:                ;; offset=0x0295
       mov      r10d, esi
       neg      r10d
       add      r10d, 0xFFFF
       movsxd   r8, r10d
       mov      r10d, ebx
       cmp      r8, r10
       jl       G_M000_IG39
       add      esi, ebx
       movzx    rsi, si
       cmp      r12d, 47
       jb       G_M000_IG11
 
G_M000_IG28:                ;; offset=0x02C0
       cmp      r12d, 200
       jb       G_M000_IG06
 
G_M000_IG29:                ;; offset=0x02CD
       cmp      r12d, 321
       jb       SHORT G_M000_IG25
 
G_M000_IG30:                ;; offset=0x02D6
       cmp      r12d, 515
       jae      SHORT G_M000_IG24
 
G_M000_IG31:                ;; offset=0x02DF
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
 
G_M000_IG32:                ;; offset=0x02F7
       mov      ebx, r15d
       mov      r10d, r12d
       mov      r8d, r14d
       add      r9, 4
 
G_M000_IG33:                ;; offset=0x0304
       dec      r11d
       je       G_M000_IG21
 
G_M000_IG34:                ;; offset=0x030D
       mov      r15d, dword ptr [rdx+r9]
       mov      r14d, r15d
       imul     r14, r14, 0x1B4E81B5
       shr      r14, 38
       imul     r13d, r14d, 600
       mov      r12d, r15d
       sub      r12d, r13d
       cmp      r15d, ebx
       jae      G_M000_IG26
 
G_M000_IG35:                ;; offset=0x0335
       cmp      r12d, r10d
       setb     bl
       movzx    rbx, bl
       jmp      G_M000_IG27
 
G_M000_IG36:                ;; offset=0x0343
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG37:                ;; offset=0x034A
       mov      rdi, 0x7FAA285AB698
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x455
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG38:                ;; offset=0x0386
       mov      rdi, 0x7FAA285AB698
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x4CD
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG39:                ;; offset=0x03C2
       mov      rdi, 0x7FAA285A0D20
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x4F7
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      edi, 0x503
       mov      rsi, 0x7FAA28151A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r14
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG40:                ;; offset=0x0419
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	40000000h		;         2
RWD04  	dd	41000000h		;         8
RWD08  	dd	42380000h		;        46
RWD12  	dd	41A80000h		;        21
RWD16  	dd	42080000h		;        34
RWD20  	dd	41500000h		;        13
RWD24  	dd	41880000h		;        17
RWD28  	dd	40A00000h		;         5
RWD32  	dd	3F800000h		;         1
RWD36  	dd	40400000h		;         3

; Total bytes of code 1055


; Assembly listing for method Tl.ConsumerFusion.ConsumerBenchmarks`1[Tl.ConsumerFusion.SumConsumer]:FusedSingle():Tl.ConsumerFusion.ConsumerReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 100
; 21 inlinees with PGO data; 48 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x30]
       mov      rax, rsi
 
G_M000_IG02:                ;; offset=0x0012
       vxorps   xmm0, xmm0, xmm0
       mov      rcx, 0x1000000000000
       mov      qword ptr [rbp-0x20], rcx
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       xor      ecx, ecx
       mov      rdx, gword ptr [rdi+0x08]
       cmp      dword ptr [rdx+0x08], ecx
       jg       G_M000_IG09
 
G_M000_IG03:                ;; offset=0x0040
       vmovd    ecx, xmm0
       movzx    rdx, r15w
       movzx    rdi, r14w
       mov      dword ptr [rax], ebx
       mov      word  ptr [rax+0x04], dx
       mov      word  ptr [rax+0x06], di
       mov      dword ptr [rax+0x08], ecx
       vxorps   ymm0, ymm0, ymm0
       vmovups  ymmword ptr [rax+0x10], ymm0
 
G_M000_IG04:                ;; offset=0x0062
       vzeroupper 
       add      rsp, 24
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0070
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0070
       cmp      r11d, r9d
       setb     r8b
       movzx    r8, r8b
       jmp      G_M000_IG11
 
G_M000_IG07:                ;; offset=0x0080
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
 
G_M000_IG08:                ;; offset=0x0098
       mov      edx, edx
       mov      esi, esi
       shl      rsi, 32
       or       rdx, rsi
       mov      esi, r8d
       shl      rsi, 48
       or       rdx, rsi
       mov      qword ptr [rbp-0x28], rdx
       mov      ebx, dword ptr [rbp-0x28]
       movzx    r15, word  ptr [rbp-0x24]
       movzx    r14, word  ptr [rbp-0x22]
       inc      ecx
       mov      rdx, gword ptr [rdi+0x08]
       cmp      dword ptr [rdx+0x08], ecx
       jle      G_M000_IG03
 
G_M000_IG09:                ;; offset=0x00CD
       mov      rsi, gword ptr [rdi+0x10]
       mov      r8d, ecx
       sar      r8d, 3
       and      r8d, 3
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG37
       cmp      ecx, dword ptr [rdx+0x08]
       jae      G_M000_IG37
       mov      edx, dword ptr [rdx+4*rcx+0x10]
       movzx    rsi, r14w
       test     sil, 1
       je       G_M000_IG34
       movzx    rsi, r14w
       test     sil, 2
       jne      G_M000_IG35
       mov      esi, ebx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     r8d, esi, 600
       mov      r9d, ebx
       sub      r9d, r8d
       mov      r8d, edx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       imul     r10d, r8d, 600
       mov      r11d, edx
       sub      r11d, r10d
       cmp      edx, ebx
       jb       G_M000_IG06
 
G_M000_IG10:                ;; offset=0x014C
       sub      r8d, esi
 
G_M000_IG11:                ;; offset=0x014F
       movzx    rsi, r15w
       neg      esi
       add      esi, 0xFFFF
       movsxd   rsi, esi
       mov      r9d, r8d
       cmp      rsi, r9
       jl       G_M000_IG36
       add      r8d, r15d
       movzx    rsi, r8w
       mov      r8d, 1
       cmp      r11d, 599
       je       G_M000_IG33
 
G_M000_IG12:                ;; offset=0x0184
       cmp      r11d, 47
       jb       G_M000_IG23
 
G_M000_IG13:                ;; offset=0x018E
       cmp      r11d, 200
       jb       SHORT G_M000_IG18
 
G_M000_IG14:                ;; offset=0x0197
       cmp      r11d, 321
       jb       SHORT G_M000_IG17
 
G_M000_IG15:                ;; offset=0x01A0
       cmp      r11d, 515
       jb       G_M000_IG07
 
G_M000_IG16:                ;; offset=0x01AD
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      G_M000_IG08
 
G_M000_IG17:                ;; offset=0x01BA
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       jmp      G_M000_IG08
 
G_M000_IG18:                ;; offset=0x01CF
       cmp      r11d, 76
       jb       SHORT G_M000_IG22
 
G_M000_IG19:                ;; offset=0x01D5
       cmp      r11d, 123
       jb       SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x01DB
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       jmp      G_M000_IG08
 
G_M000_IG21:                ;; offset=0x01E8
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       add      r11d, -76
       mov      r9d, r11d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG08
 
G_M000_IG22:                ;; offset=0x0221
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       jmp      G_M000_IG08
 
G_M000_IG23:                ;; offset=0x0236
       cmp      r11d, 11
       jb       SHORT G_M000_IG28
 
G_M000_IG24:                ;; offset=0x023C
       cmp      r11d, 18
       jb       G_M000_IG08
 
G_M000_IG25:                ;; offset=0x0246
       cmp      r11d, 29
       jb       SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x024C
       add      r11d, -29
       mov      r9d, r11d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG08
 
G_M000_IG27:                ;; offset=0x027D
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       jmp      G_M000_IG08
 
G_M000_IG28:                ;; offset=0x0292
       cmp      r11d, 3
       jb       SHORT G_M000_IG32
 
G_M000_IG29:                ;; offset=0x0298
       cmp      r11d, 7
       jb       SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x029E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       jmp      G_M000_IG08
 
G_M000_IG31:                ;; offset=0x02B3
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       lea      r9d, [r11-0x03]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, xmm1
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       jmp      G_M000_IG08
 
G_M000_IG32:                ;; offset=0x02E9
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG08
 
G_M000_IG33:                ;; offset=0x02F6
       mov      r8d, 5
       jmp      G_M000_IG12
 
G_M000_IG34:                ;; offset=0x0301
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
 
G_M000_IG35:                ;; offset=0x033D
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
 
G_M000_IG36:                ;; offset=0x0379
       mov      rdi, 0x7FE093390D20
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x4F7
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      edi, 0x503
       mov      rsi, 0x7FE092F41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r14
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x03D0
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41000000h		;         8
RWD08  	dd	40A00000h		;         5
RWD12  	dd	40400000h		;         3
RWD16  	dd	40000000h		;         2
RWD20  	dd	42380000h		;        46
RWD24  	dd	41A80000h		;        21
RWD28  	dd	42080000h		;        34
RWD32  	dd	41500000h		;        13
RWD36  	dd	41880000h		;        17

; Total bytes of code 982


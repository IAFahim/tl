; Assembly listing for method Tl.ConsumerFusion.FusedPulse:Forward[Tl.ConsumerFusion.ConsumerInput,Tl.ConsumerFusion.SumConsumer](byref,byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 433793
; 20 inlinees with PGO data; 44 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     rbx
       push     rax
       lea      rbp, [rsp+0x20]
 
G_M000_IG02:                ;; offset=0x000C
       movzx    rax, word  ptr [rdi+0x06]
       test     al, 1
       je       G_M000_IG35
       test     al, 2
       jne      G_M000_IG36
       test     r8d, r8d
       je       G_M000_IG37
 
G_M000_IG03:                ;; offset=0x0029
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
       jl       G_M000_IG11
 
G_M000_IG04:                ;; offset=0x0054
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
 
G_M000_IG05:                ;; offset=0x007B
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x0086
       align    [0 bytes for IG07]
 
G_M000_IG07:                ;; offset=0x0086
       cmp      r11d, r14d
       setb     r10b
       movzx    r10, r10b
       jmp      SHORT G_M000_IG13
 
G_M000_IG08:                ;; offset=0x0093
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdx], xmm0
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdx], xmm0
 
G_M000_IG09:                ;; offset=0x00B3
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdx], xmm0
 
G_M000_IG10:                ;; offset=0x00C3
       mov      ebx, esi
       mov      r14d, r11d
       mov      edi, r9d
       inc      eax
       cmp      eax, r8d
       jge      SHORT G_M000_IG04
 
G_M000_IG11:                ;; offset=0x00D2
       mov      esi, dword ptr [rcx+4*rax]
       mov      r9d, esi
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r10d, r9d, 600
       mov      r11d, esi
       sub      r11d, r10d
       cmp      esi, ebx
       jb       SHORT G_M000_IG07
 
G_M000_IG12:                ;; offset=0x00F4
       mov      r10d, r9d
       sub      r10d, edi
 
G_M000_IG13:                ;; offset=0x00FA
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r10d
       cmp      rdi, rbx
       jl       G_M000_IG39
       add      r10d, r15d
       movzx    r15, r10w
       cmp      r11d, 47
       jb       G_M000_IG25
 
G_M000_IG14:                ;; offset=0x0125
       cmp      r11d, 200
       jb       SHORT G_M000_IG19
 
G_M000_IG15:                ;; offset=0x012E
       cmp      r11d, 321
       jb       SHORT G_M000_IG18
 
G_M000_IG16:                ;; offset=0x0137
       cmp      r11d, 515
       jb       G_M000_IG08
 
G_M000_IG17:                ;; offset=0x0144
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG10
 
G_M000_IG18:                ;; offset=0x0159
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG09
 
G_M000_IG19:                ;; offset=0x016E
       cmp      r11d, 76
       jb       SHORT G_M000_IG24
 
G_M000_IG20:                ;; offset=0x0174
       cmp      r11d, 123
       jb       SHORT G_M000_IG23
 
G_M000_IG21:                ;; offset=0x017A
       cmp      byte  ptr [rdx], dl
 
G_M000_IG22:                ;; offset=0x017C
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG10
 
G_M000_IG23:                ;; offset=0x0191
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdx], xmm0
       lea      edi, [r11-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG10
 
G_M000_IG24:                ;; offset=0x01D3
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rdx], xmm0
       jmp      SHORT G_M000_IG22
 
G_M000_IG25:                ;; offset=0x01E5
       cmp      r11d, 11
       jb       SHORT G_M000_IG30
 
G_M000_IG26:                ;; offset=0x01EB
       cmp      r11d, 18
       jb       G_M000_IG10
 
G_M000_IG27:                ;; offset=0x01F5
       cmp      r11d, 29
       jb       SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x01FB
       lea      edi, [r11-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG10
 
G_M000_IG29:                ;; offset=0x022D
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG09
 
G_M000_IG30:                ;; offset=0x0242
       cmp      r11d, 3
       jae      SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x0248
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG10
 
G_M000_IG32:                ;; offset=0x025D
       cmp      r11d, 7
       jae      SHORT G_M000_IG34
 
G_M000_IG33:                ;; offset=0x0263
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdx], xmm0
       lea      edi, [r11-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, dword ptr [rdx]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG09
 
G_M000_IG34:                ;; offset=0x029D
       vmovss   xmm0, dword ptr [rdx]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rdx], xmm0
       jmp      G_M000_IG09
 
G_M000_IG35:                ;; offset=0x02B2
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
 
G_M000_IG36:                ;; offset=0x02EE
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
 
G_M000_IG37:                ;; offset=0x032A
       mov      rax, qword ptr [rdi]
 
G_M000_IG38:                ;; offset=0x032D
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG39:                ;; offset=0x0338
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

; Total bytes of code 911


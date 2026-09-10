; Assembly listing for method __TlGeneratedSchema0:TrySeek(ushort,byref,int,byref):bool (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     rbx
       push     rax
       lea      rbp, [rsp+0x20]
       xor      eax, eax
       mov      qword ptr [rbp-0x20], rax
       mov      rbx, rsi
       mov      r15d, edx
 
G_M000_IG02:                ;; offset=0x0018
       movzx    rsi, word  ptr [rbx+0x0C]
       movzx    rax, byte  ptr [rbx+0x0E]
       movzx    r14, di
       cmp      esi, r14d
       jne      G_M000_IG09
 
G_M000_IG03:                ;; offset=0x002D
       and      eax, 3
       cmp      eax, 1
       jne      G_M000_IG09
       mov      edi, r14d
       lea      rsi, [rbp-0x20]
       call     [Tl.Timeline:TryGetCompiledRoute(ushort,byref):bool]
       test     eax, eax
       je       G_M000_IG09
       test     byte  ptr [(reloc 0x7f0ebf3430a8)], 1
       je       G_M000_IG11
 
G_M000_IG04:                ;; offset=0x005B
       movzx    rax, byte  ptr [rbp-0x20]
       cmp      eax, 256
       jae      G_M000_IG12
       mov      rcx, 0x7F0EBC200CF0
       cmp      word  ptr [rcx+2*rax], 1
       jne      G_M000_IG09
       cmp      byte  ptr [rbp-0x1F], 0
       jne      G_M000_IG09
       mov      rax, qword ptr [rbx]
       movzx    rcx, word  ptr [rbx+0x0C]
       movzx    rdx, byte  ptr [rbx+0x0E]
       test     r14d, r14d
       jne      SHORT G_M000_IG07
       cmp      ecx, r14d
       jne      SHORT G_M000_IG07
       movsxd   rcx, r15d
       and      edx, 3
       cmp      edx, 1
       jne      SHORT G_M000_IG07
       test     rax, rax
       jl       SHORT G_M000_IG07
       test     rax, rax
       jg       SHORT G_M000_IG07
       test     rcx, rcx
       jle      SHORT G_M000_IG05
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x7FFFFFFFFFFFFFFF
       add      rdx, rdi
       cmp      rdx, rax
       jl       SHORT G_M000_IG07
 
G_M000_IG05:                ;; offset=0x00D0
       test     rcx, rcx
       jge      SHORT G_M000_IG06
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x8000000000000000
       add      rdx, rdi
       cmp      rdx, rax
       jg       SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x00ED
       add      rax, rcx
       js       SHORT G_M000_IG07
       test     rax, rax
       jg       SHORT G_M000_IG07
       test     r15d, r15d
       jne      SHORT G_M000_IG07
       mov      eax, 1
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0103
       xor      eax, eax
 
G_M000_IG08:                ;; offset=0x0105
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG09:                ;; offset=0x0110
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x0112
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG11:                ;; offset=0x011D
       mov      rdi, 0x7F0EBF343040
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG04
 
G_M000_IG12:                ;; offset=0x0131
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 311

; Assembly listing for method __TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 17 single block inlinees; 8 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 16
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x28], rax
       mov      rbx, rsi
       mov      r15d, edx
       mov      r14, rcx
 
G_M000_IG02:                ;; offset=0x0020
       movzx    rsi, word  ptr [rbx+0x0C]
       movzx    rax, byte  ptr [rbx+0x0E]
       movzx    r13, di
       cmp      esi, r13d
       jne      G_M000_IG39
 
G_M000_IG03:                ;; offset=0x0035
       and      eax, 3
       cmp      eax, 1
       jne      G_M000_IG39
       mov      edi, r13d
       lea      rsi, [rbp-0x28]
       call     [Tl.Timeline:TryGetCompiledRoute(ushort,byref):bool]
       test     eax, eax
       je       G_M000_IG39
       test     byte  ptr [(reloc 0x7f0ebf3cece0)], 1
       je       G_M000_IG41
 
G_M000_IG04:                ;; offset=0x0063
       movzx    rdi, byte  ptr [rbp-0x28]
       cmp      edi, 256
       jae      G_M000_IG42
       mov      rsi, 0x7F0EBC202958
       cmp      word  ptr [rsi+2*rdi], 1
       jne      G_M000_IG39
       movzx    rdi, byte  ptr [rbp-0x27]
       dec      edi
       cmp      edi, 15
       ja       G_M000_IG39
 
G_M000_IG05:                ;; offset=0x0099
       mov      edi, edi
       lea      rsi, [reloc @RWD00]
       mov      esi, dword ptr [rsi+4*rdi]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG06:                ;; offset=0x00B1
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen015:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG07:                ;; offset=0x00C8
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen014:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG08:                ;; offset=0x00DF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen013:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG09:                ;; offset=0x00F6
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen012:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG10:                ;; offset=0x010D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen011:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG11:                ;; offset=0x0124
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen010:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG12:                ;; offset=0x013B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen009:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG13:                ;; offset=0x0152
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen008:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG14:                ;; offset=0x0169
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen007:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG15:                ;; offset=0x0180
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen006:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG16:                ;; offset=0x0197
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen005:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG17:                ;; offset=0x01AE
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Sixteen004:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG37
 
G_M000_IG18:                ;; offset=0x01C5
       mov      rax, qword ptr [rbx]
       movzx    rcx, word  ptr [rbx+0x0C]
       movzx    rdx, byte  ptr [rbx+0x0E]
       cmp      r13d, 4
       jne      SHORT G_M000_IG21
       cmp      ecx, r13d
       jne      SHORT G_M000_IG21
       movsxd   rcx, r15d
       and      edx, 3
       cmp      edx, 1
       jne      SHORT G_M000_IG21
       test     rax, rax
       jl       SHORT G_M000_IG21
       test     rax, rax
       jg       SHORT G_M000_IG21
       test     rcx, rcx
       jle      SHORT G_M000_IG19
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x7FFFFFFFFFFFFFFF
       add      rdx, rdi
       cmp      rdx, rax
       jl       SHORT G_M000_IG21
 
G_M000_IG19:                ;; offset=0x020D
       test     rcx, rcx
       jge      SHORT G_M000_IG20
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x8000000000000000
       add      rdx, rdi
       cmp      rdx, rax
       jg       SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x022A
       add      rax, rcx
       js       SHORT G_M000_IG21
       test     rax, rax
       jg       SHORT G_M000_IG21
       test     r15d, r15d
       jne      SHORT G_M000_IG21
       mov      eax, 1
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x0240
       xor      eax, eax
 
G_M000_IG22:                ;; offset=0x0242
       jmp      G_M000_IG37
 
G_M000_IG23:                ;; offset=0x0247
       mov      rax, qword ptr [rbx]
       movzx    rcx, word  ptr [rbx+0x0C]
       movzx    rdx, byte  ptr [rbx+0x0E]
       cmp      r13d, 3
       jne      SHORT G_M000_IG26
       cmp      ecx, r13d
       jne      SHORT G_M000_IG26
       movsxd   rcx, r15d
       and      edx, 3
       cmp      edx, 1
       jne      SHORT G_M000_IG26
       test     rax, rax
       jl       SHORT G_M000_IG26
       test     rax, rax
       jg       SHORT G_M000_IG26
       test     rcx, rcx
       jle      SHORT G_M000_IG24
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x7FFFFFFFFFFFFFFF
       add      rdx, rdi
       cmp      rdx, rax
       jl       SHORT G_M000_IG26
 
G_M000_IG24:                ;; offset=0x028F
       test     rcx, rcx
       jge      SHORT G_M000_IG25
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x8000000000000000
       add      rdx, rdi
       cmp      rdx, rax
       jg       SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x02AC
       add      rax, rcx
       js       SHORT G_M000_IG26
       test     rax, rax
       jg       SHORT G_M000_IG26
       test     r15d, r15d
       jne      SHORT G_M000_IG26
       mov      eax, 1
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x02C2
       xor      eax, eax
 
G_M000_IG27:                ;; offset=0x02C4
       jmp      G_M000_IG37
 
G_M000_IG28:                ;; offset=0x02C9
       mov      rax, qword ptr [rbx]
       movzx    rcx, word  ptr [rbx+0x0C]
       movzx    rdx, byte  ptr [rbx+0x0E]
       cmp      r13d, 2
       jne      SHORT G_M000_IG31
       cmp      ecx, r13d
       jne      SHORT G_M000_IG31
       movsxd   rcx, r15d
       and      edx, 3
       cmp      edx, 1
       jne      SHORT G_M000_IG31
       test     rax, rax
       jl       SHORT G_M000_IG31
       test     rax, rax
       jg       SHORT G_M000_IG31
       test     rcx, rcx
       jle      SHORT G_M000_IG29
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x7FFFFFFFFFFFFFFF
       add      rdx, rdi
       cmp      rdx, rax
       jl       SHORT G_M000_IG31
 
G_M000_IG29:                ;; offset=0x0311
       test     rcx, rcx
       jge      SHORT G_M000_IG30
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x8000000000000000
       add      rdx, rdi
       cmp      rdx, rax
       jg       SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x032E
       add      rax, rcx
       js       SHORT G_M000_IG31
       test     rax, rax
       jg       SHORT G_M000_IG31
       test     r15d, r15d
       jne      SHORT G_M000_IG31
       mov      eax, 1
       jmp      SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x0344
       xor      eax, eax
 
G_M000_IG32:                ;; offset=0x0346
       jmp      G_M000_IG37
 
G_M000_IG33:                ;; offset=0x034B
       mov      rax, qword ptr [rbx]
       movzx    rcx, word  ptr [rbx+0x0C]
       movzx    rdx, byte  ptr [rbx+0x0E]
       cmp      r13d, 1
       jne      SHORT G_M000_IG36
       cmp      ecx, r13d
       jne      SHORT G_M000_IG36
       movsxd   rcx, r15d
       and      edx, 3
       cmp      edx, 1
       jne      SHORT G_M000_IG36
       test     rax, rax
       jl       SHORT G_M000_IG36
       test     rax, rax
       jg       SHORT G_M000_IG36
       test     rcx, rcx
       jle      SHORT G_M000_IG34
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x7FFFFFFFFFFFFFFF
       add      rdx, rdi
       cmp      rdx, rax
       jl       SHORT G_M000_IG36
 
G_M000_IG34:                ;; offset=0x0393
       test     rcx, rcx
       jge      SHORT G_M000_IG35
       mov      rdx, rcx
       neg      rdx
       mov      rdi, 0x8000000000000000
       add      rdx, rdi
       cmp      rdx, rax
       jg       SHORT G_M000_IG36
 
G_M000_IG35:                ;; offset=0x03B0
       add      rax, rcx
       js       SHORT G_M000_IG36
       test     rax, rax
       jg       SHORT G_M000_IG36
       test     r15d, r15d
       jne      SHORT G_M000_IG36
       mov      eax, 1
       jmp      SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x03C6
       xor      eax, eax
 
G_M000_IG37:                ;; offset=0x03C8
       movzx    rax, al
 
G_M000_IG38:                ;; offset=0x03CB
       add      rsp, 16
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG39:                ;; offset=0x03D8
       xor      eax, eax
 
G_M000_IG40:                ;; offset=0x03DA
       add      rsp, 16
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG41:                ;; offset=0x03E7
       mov      rdi, 0x7F0EBF3CEC78
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG04
 
G_M000_IG42:                ;; offset=0x03FB
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	0000032Bh ; case G_M000_IG33
       	dd	000002A9h ; case G_M000_IG28
       	dd	00000227h ; case G_M000_IG23
       	dd	000001A5h ; case G_M000_IG18
       	dd	0000018Eh ; case G_M000_IG17
       	dd	00000177h ; case G_M000_IG16
       	dd	00000160h ; case G_M000_IG15
       	dd	00000149h ; case G_M000_IG14
       	dd	00000132h ; case G_M000_IG13
       	dd	0000011Bh ; case G_M000_IG12
       	dd	00000104h ; case G_M000_IG11
       	dd	000000EDh ; case G_M000_IG10
       	dd	000000D6h ; case G_M000_IG09
       	dd	000000BFh ; case G_M000_IG08
       	dd	000000A8h ; case G_M000_IG07
       	dd	00000091h ; case G_M000_IG06

; Total bytes of code 1025

; Assembly listing for method __TlGeneratedSchema2:TrySeek(ushort,byref,int,byref):bool (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 15 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       push     rax
       lea      rbp, [rsp+0x30]
       xor      eax, eax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rsi
       mov      r15d, edx
       mov      r14, rcx
 
G_M000_IG02:                ;; offset=0x001F
       movzx    rsi, word  ptr [rbx+0x0C]
       movzx    rax, byte  ptr [rbx+0x0E]
       movzx    r13, di
       cmp      esi, r13d
       jne      G_M000_IG278
 
G_M000_IG03:                ;; offset=0x0034
       and      eax, 3
       cmp      eax, 1
       jne      G_M000_IG278
       mov      edi, r13d
       lea      rsi, [rbp-0x30]
       call     [Tl.Timeline:TryGetCompiledRoute(ushort,byref):bool]
       test     eax, eax
       je       G_M000_IG278
       test     byte  ptr [(reloc 0x7f0ebf3cf2d0)], 1
       je       G_M000_IG280
 
G_M000_IG04:                ;; offset=0x0062
       movzx    rdi, byte  ptr [rbp-0x30]
       cmp      edi, 256
       jae      G_M000_IG281
       mov      rsi, 0x7F0EBC202B68
       movzx    r12, word  ptr [rsi+2*rdi]
       cmp      r12d, 1
       jne      G_M000_IG257
       movzx    rdi, byte  ptr [rbp-0x2F]
       add      edi, -17
       cmp      edi, 238
       ja       G_M000_IG278
 
G_M000_IG05:                ;; offset=0x00A0
       mov      edi, edi
       lea      rsi, [reloc @RWD00]
       mov      esi, dword ptr [rsi+4*rdi]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG06:                ;; offset=0x00B8
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide238:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG07:                ;; offset=0x00CF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide237:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG08:                ;; offset=0x00E6
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide236:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG09:                ;; offset=0x00FD
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide235:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG10:                ;; offset=0x0114
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide234:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG11:                ;; offset=0x012B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide233:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG12:                ;; offset=0x0142
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide232:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG13:                ;; offset=0x0159
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide231:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG14:                ;; offset=0x0170
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide230:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG15:                ;; offset=0x0187
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide229:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG16:                ;; offset=0x019E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide228:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG17:                ;; offset=0x01B5
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide227:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG18:                ;; offset=0x01CC
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide226:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG19:                ;; offset=0x01E3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide225:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG20:                ;; offset=0x01FA
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide224:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG21:                ;; offset=0x0211
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide223:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG22:                ;; offset=0x0228
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide222:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG23:                ;; offset=0x023F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide221:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG24:                ;; offset=0x0256
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide220:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG25:                ;; offset=0x026D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide219:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG26:                ;; offset=0x0284
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide218:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG27:                ;; offset=0x029B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide217:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG28:                ;; offset=0x02B2
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide216:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG29:                ;; offset=0x02C9
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide215:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG30:                ;; offset=0x02E0
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide214:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG31:                ;; offset=0x02F7
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide213:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG32:                ;; offset=0x030E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide212:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG33:                ;; offset=0x0325
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide211:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG34:                ;; offset=0x033C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide210:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG35:                ;; offset=0x0353
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide209:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG36:                ;; offset=0x036A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide208:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG37:                ;; offset=0x0381
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide207:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG38:                ;; offset=0x0398
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide206:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG39:                ;; offset=0x03AF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide205:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG40:                ;; offset=0x03C6
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide204:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG41:                ;; offset=0x03DD
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide203:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG42:                ;; offset=0x03F4
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide202:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG43:                ;; offset=0x040B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide201:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG44:                ;; offset=0x0422
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide200:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG45:                ;; offset=0x0439
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide199:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG46:                ;; offset=0x0450
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide198:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG47:                ;; offset=0x0467
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide197:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG48:                ;; offset=0x047E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide196:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG49:                ;; offset=0x0495
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide195:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG50:                ;; offset=0x04AC
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide194:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG51:                ;; offset=0x04C3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide193:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG52:                ;; offset=0x04DA
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide192:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG53:                ;; offset=0x04F1
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide191:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG54:                ;; offset=0x0508
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide190:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG55:                ;; offset=0x051F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide189:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG56:                ;; offset=0x0536
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide188:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG57:                ;; offset=0x054D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide187:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG58:                ;; offset=0x0564
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide186:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG59:                ;; offset=0x057B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide185:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG60:                ;; offset=0x0592
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide184:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG61:                ;; offset=0x05A9
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide183:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG62:                ;; offset=0x05C0
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide182:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG63:                ;; offset=0x05D7
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide181:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG64:                ;; offset=0x05EE
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide180:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG65:                ;; offset=0x0605
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide179:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG66:                ;; offset=0x061C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide178:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG67:                ;; offset=0x0633
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide177:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG68:                ;; offset=0x064A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide176:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG69:                ;; offset=0x0661
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide175:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG70:                ;; offset=0x0678
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide174:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG71:                ;; offset=0x068F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide173:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG72:                ;; offset=0x06A6
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide172:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG73:                ;; offset=0x06BD
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide171:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG74:                ;; offset=0x06D4
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide170:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG75:                ;; offset=0x06EB
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide169:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG76:                ;; offset=0x0702
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide168:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG77:                ;; offset=0x0719
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide167:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG78:                ;; offset=0x0730
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide166:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG79:                ;; offset=0x0747
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide165:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG80:                ;; offset=0x075E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide164:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG81:                ;; offset=0x0775
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide163:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG82:                ;; offset=0x078C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide162:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG83:                ;; offset=0x07A3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide161:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG84:                ;; offset=0x07BA
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide160:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG85:                ;; offset=0x07D1
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide159:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG86:                ;; offset=0x07E8
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide158:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG87:                ;; offset=0x07FF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide157:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG88:                ;; offset=0x0816
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide156:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG89:                ;; offset=0x082D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide155:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG90:                ;; offset=0x0844
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide154:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG91:                ;; offset=0x085B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide153:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG92:                ;; offset=0x0872
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide152:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG93:                ;; offset=0x0889
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide151:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG94:                ;; offset=0x08A0
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide150:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG95:                ;; offset=0x08B7
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide149:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG96:                ;; offset=0x08CE
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide148:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG97:                ;; offset=0x08E5
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide147:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG98:                ;; offset=0x08FC
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide146:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG99:                ;; offset=0x0913
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide145:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG100:                ;; offset=0x092A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide144:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG101:                ;; offset=0x0941
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide143:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG102:                ;; offset=0x0958
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide142:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG103:                ;; offset=0x096F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide141:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG104:                ;; offset=0x0986
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide140:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG105:                ;; offset=0x099D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide139:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG106:                ;; offset=0x09B4
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide138:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG107:                ;; offset=0x09CB
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide137:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG108:                ;; offset=0x09E2
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide136:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG109:                ;; offset=0x09F9
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide135:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG110:                ;; offset=0x0A10
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide134:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG111:                ;; offset=0x0A27
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide133:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG112:                ;; offset=0x0A3E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide132:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG113:                ;; offset=0x0A55
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide131:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG114:                ;; offset=0x0A6C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide130:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG115:                ;; offset=0x0A83
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide129:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG116:                ;; offset=0x0A9A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide128:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG117:                ;; offset=0x0AB1
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide127:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG118:                ;; offset=0x0AC8
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide126:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG119:                ;; offset=0x0ADF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide125:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG120:                ;; offset=0x0AF6
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide124:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG121:                ;; offset=0x0B0D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide123:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG122:                ;; offset=0x0B24
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide122:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG123:                ;; offset=0x0B3B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide121:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG124:                ;; offset=0x0B52
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide120:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG125:                ;; offset=0x0B69
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide119:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG126:                ;; offset=0x0B80
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide118:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG127:                ;; offset=0x0B97
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide117:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG128:                ;; offset=0x0BAE
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide116:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG129:                ;; offset=0x0BC5
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide115:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG130:                ;; offset=0x0BDC
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide114:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG131:                ;; offset=0x0BF3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide113:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG132:                ;; offset=0x0C0A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide112:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG133:                ;; offset=0x0C21
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide111:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG134:                ;; offset=0x0C38
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide110:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG135:                ;; offset=0x0C4F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide109:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG136:                ;; offset=0x0C66
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide108:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG137:                ;; offset=0x0C7D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide107:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG138:                ;; offset=0x0C94
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide106:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG139:                ;; offset=0x0CAB
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide105:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG140:                ;; offset=0x0CC2
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide104:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG141:                ;; offset=0x0CD9
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide103:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG142:                ;; offset=0x0CF0
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide102:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG143:                ;; offset=0x0D07
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide101:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG144:                ;; offset=0x0D1E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide100:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG145:                ;; offset=0x0D35
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide099:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG146:                ;; offset=0x0D4C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide098:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG147:                ;; offset=0x0D63
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide097:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG148:                ;; offset=0x0D7A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide096:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG149:                ;; offset=0x0D91
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide095:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG150:                ;; offset=0x0DA8
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide094:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG151:                ;; offset=0x0DBF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide093:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG152:                ;; offset=0x0DD6
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide092:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG153:                ;; offset=0x0DED
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide091:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG154:                ;; offset=0x0E04
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide090:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG155:                ;; offset=0x0E1B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide089:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG156:                ;; offset=0x0E32
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide088:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG157:                ;; offset=0x0E49
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide087:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG158:                ;; offset=0x0E60
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide086:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG159:                ;; offset=0x0E77
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide085:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG160:                ;; offset=0x0E8E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide084:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG161:                ;; offset=0x0EA5
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide083:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG162:                ;; offset=0x0EBC
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide082:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG163:                ;; offset=0x0ED3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide081:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG164:                ;; offset=0x0EEA
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide080:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG165:                ;; offset=0x0F01
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide079:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG166:                ;; offset=0x0F18
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide078:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG167:                ;; offset=0x0F2F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide077:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG168:                ;; offset=0x0F46
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide076:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG169:                ;; offset=0x0F5D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide075:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG170:                ;; offset=0x0F74
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide074:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG171:                ;; offset=0x0F8B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide073:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG172:                ;; offset=0x0FA2
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide072:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG173:                ;; offset=0x0FB9
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide071:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG174:                ;; offset=0x0FD0
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide070:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG175:                ;; offset=0x0FE7
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide069:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG176:                ;; offset=0x0FFE
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide068:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG177:                ;; offset=0x1015
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide067:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG178:                ;; offset=0x102C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide066:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG179:                ;; offset=0x1043
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide065:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG180:                ;; offset=0x105A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide064:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG181:                ;; offset=0x1071
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide063:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG182:                ;; offset=0x1088
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide062:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG183:                ;; offset=0x109F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide061:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG184:                ;; offset=0x10B6
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide060:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG185:                ;; offset=0x10CD
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide059:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG186:                ;; offset=0x10E4
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide058:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG187:                ;; offset=0x10FB
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide057:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG188:                ;; offset=0x1112
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide056:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG189:                ;; offset=0x1129
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide055:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG190:                ;; offset=0x1140
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide054:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG191:                ;; offset=0x1157
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide053:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG192:                ;; offset=0x116E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide052:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG193:                ;; offset=0x1185
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide051:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG194:                ;; offset=0x119C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide050:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG195:                ;; offset=0x11B3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide049:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG196:                ;; offset=0x11CA
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide048:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG197:                ;; offset=0x11E1
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide047:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG198:                ;; offset=0x11F8
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide046:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG199:                ;; offset=0x120F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide045:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG200:                ;; offset=0x1226
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide044:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG201:                ;; offset=0x123D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide043:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG202:                ;; offset=0x1254
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide042:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG203:                ;; offset=0x126B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide041:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG204:                ;; offset=0x1282
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide040:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG205:                ;; offset=0x1299
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide039:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG206:                ;; offset=0x12B0
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide038:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG207:                ;; offset=0x12C7
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide037:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG208:                ;; offset=0x12DE
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide036:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG209:                ;; offset=0x12F5
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide035:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG210:                ;; offset=0x130C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide034:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG211:                ;; offset=0x1323
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide033:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG212:                ;; offset=0x133A
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide032:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG213:                ;; offset=0x1351
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide031:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG214:                ;; offset=0x1368
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide030:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG215:                ;; offset=0x137F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide029:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG216:                ;; offset=0x1396
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide028:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG217:                ;; offset=0x13AD
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide027:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG218:                ;; offset=0x13C4
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide026:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG219:                ;; offset=0x13DB
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide025:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG220:                ;; offset=0x13F2
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide024:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG221:                ;; offset=0x1409
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide023:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG222:                ;; offset=0x1420
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide022:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG223:                ;; offset=0x1437
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide021:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG224:                ;; offset=0x144E
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide020:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG225:                ;; offset=0x1465
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide019:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG226:                ;; offset=0x147C
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide018:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG227:                ;; offset=0x1493
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide017:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG228:                ;; offset=0x14AA
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide016:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG229:                ;; offset=0x14C1
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide015:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG230:                ;; offset=0x14D8
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide014:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG231:                ;; offset=0x14EF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide013:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG232:                ;; offset=0x1506
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide012:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG233:                ;; offset=0x151D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide011:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG234:                ;; offset=0x1534
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide010:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG235:                ;; offset=0x154B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide009:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG236:                ;; offset=0x1562
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide008:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG237:                ;; offset=0x1579
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide007:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG238:                ;; offset=0x1590
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide006:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG239:                ;; offset=0x15A7
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide005:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG240:                ;; offset=0x15BE
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide004:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG241:                ;; offset=0x15D5
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide003:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG242:                ;; offset=0x15EC
       mov      rax, qword ptr [rbx]
       movzx    rdi, word  ptr [rbx+0x0C]
       movzx    rsi, byte  ptr [rbx+0x0E]
       cmp      r13d, 19
       jne      SHORT G_M000_IG245
       cmp      edi, r13d
       jne      SHORT G_M000_IG245
       movsxd   rdi, r15d
       and      esi, 3
       cmp      esi, 1
       jne      SHORT G_M000_IG245
       test     rax, rax
       jl       SHORT G_M000_IG245
       test     rax, rax
       jg       SHORT G_M000_IG245
       test     rdi, rdi
       jle      SHORT G_M000_IG243
       mov      rsi, rdi
       neg      rsi
       mov      rdx, 0x7FFFFFFFFFFFFFFF
       add      rsi, rdx
       cmp      rsi, rax
       jl       SHORT G_M000_IG245
 
G_M000_IG243:                ;; offset=0x1635
       test     rdi, rdi
       jge      SHORT G_M000_IG244
       mov      rsi, rdi
       neg      rsi
       mov      rdx, 0x8000000000000000
       add      rsi, rdx
       cmp      rsi, rax
       jg       SHORT G_M000_IG245
 
G_M000_IG244:                ;; offset=0x1652
       add      rdi, rax
       js       SHORT G_M000_IG245
       test     rdi, rdi
       jg       SHORT G_M000_IG245
       test     r15d, r15d
       jne      SHORT G_M000_IG245
       mov      eax, 1
       jmp      SHORT G_M000_IG246
 
G_M000_IG245:                ;; offset=0x1668
       xor      eax, eax
 
G_M000_IG246:                ;; offset=0x166A
       jmp      G_M000_IG276
 
G_M000_IG247:                ;; offset=0x166F
       mov      rax, qword ptr [rbx]
       movzx    rdi, word  ptr [rbx+0x0C]
       movzx    rsi, byte  ptr [rbx+0x0E]
       cmp      r13d, 18
       jne      SHORT G_M000_IG250
       cmp      edi, r13d
       jne      SHORT G_M000_IG250
       movsxd   rdi, r15d
       and      esi, 3
       cmp      esi, 1
       jne      SHORT G_M000_IG250
       test     rax, rax
       jl       SHORT G_M000_IG250
       test     rax, rax
       jg       SHORT G_M000_IG250
       test     rdi, rdi
       jle      SHORT G_M000_IG248
       mov      rsi, rdi
       neg      rsi
       mov      rdx, 0x7FFFFFFFFFFFFFFF
       add      rsi, rdx
       cmp      rsi, rax
       jl       SHORT G_M000_IG250
 
G_M000_IG248:                ;; offset=0x16B8
       test     rdi, rdi
       jge      SHORT G_M000_IG249
       mov      rsi, rdi
       neg      rsi
       mov      rdx, 0x8000000000000000
       add      rsi, rdx
       cmp      rsi, rax
       jg       SHORT G_M000_IG250
 
G_M000_IG249:                ;; offset=0x16D5
       add      rdi, rax
       js       SHORT G_M000_IG250
       test     rdi, rdi
       jg       SHORT G_M000_IG250
       test     r15d, r15d
       jne      SHORT G_M000_IG250
       mov      eax, 1
       jmp      SHORT G_M000_IG251
 
G_M000_IG250:                ;; offset=0x16EB
       xor      eax, eax
 
G_M000_IG251:                ;; offset=0x16ED
       jmp      G_M000_IG276
 
G_M000_IG252:                ;; offset=0x16F2
       mov      rax, qword ptr [rbx]
       movzx    rdi, word  ptr [rbx+0x0C]
       movzx    rsi, byte  ptr [rbx+0x0E]
       cmp      r13d, 17
       jne      SHORT G_M000_IG255
       cmp      edi, r13d
       jne      SHORT G_M000_IG255
       movsxd   rdi, r15d
       and      esi, 3
       cmp      esi, 1
       jne      SHORT G_M000_IG255
       test     rax, rax
       jl       SHORT G_M000_IG255
       test     rax, rax
       jg       SHORT G_M000_IG255
       test     rdi, rdi
       jle      SHORT G_M000_IG253
       mov      rsi, rdi
       neg      rsi
       mov      rdx, 0x7FFFFFFFFFFFFFFF
       add      rsi, rdx
       cmp      rsi, rax
       jl       SHORT G_M000_IG255
 
G_M000_IG253:                ;; offset=0x173B
       test     rdi, rdi
       jge      SHORT G_M000_IG254
       mov      rsi, rdi
       neg      rsi
       mov      rdx, 0x8000000000000000
       add      rsi, rdx
       cmp      rsi, rax
       jg       SHORT G_M000_IG255
 
G_M000_IG254:                ;; offset=0x1758
       add      rdi, rax
       js       SHORT G_M000_IG255
       test     rdi, rdi
       jg       SHORT G_M000_IG255
       test     r15d, r15d
       jne      SHORT G_M000_IG255
       mov      eax, 1
       jmp      SHORT G_M000_IG256
 
G_M000_IG255:                ;; offset=0x176E
       xor      eax, eax
 
G_M000_IG256:                ;; offset=0x1770
       jmp      G_M000_IG276
 
G_M000_IG257:                ;; offset=0x1775
       cmp      r12d, 2
       jne      G_M000_IG278
       movzx    rdi, byte  ptr [rbp-0x2F]
       cmp      edi, 16
       ja       G_M000_IG278
 
G_M000_IG258:                ;; offset=0x178D
       mov      edi, edi
       lea      rsi, [reloc @RWD956]
       mov      esi, dword ptr [rsi+4*rdi]
       lea      rdx, G_M000_IG02
       add      rsi, rdx
       jmp      rsi
 
G_M000_IG259:                ;; offset=0x17A5
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide255:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG260:                ;; offset=0x17BC
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide254:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG261:                ;; offset=0x17D3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide253:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG262:                ;; offset=0x17EA
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide252:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG263:                ;; offset=0x1801
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide251:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG264:                ;; offset=0x1818
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide250:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG265:                ;; offset=0x182F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide249:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG266:                ;; offset=0x1846
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide248:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG267:                ;; offset=0x185D
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide247:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG268:                ;; offset=0x1874
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide246:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      G_M000_IG276
 
G_M000_IG269:                ;; offset=0x188B
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide245:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG276
 
G_M000_IG270:                ;; offset=0x189F
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide244:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG276
 
G_M000_IG271:                ;; offset=0x18B3
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide243:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG276
 
G_M000_IG272:                ;; offset=0x18C7
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide242:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG276
 
G_M000_IG273:                ;; offset=0x18DB
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide241:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG276
 
G_M000_IG274:                ;; offset=0x18EF
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide240:DynamicSeekKernel(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG276
 
G_M000_IG275:                ;; offset=0x1903
       mov      edi, r13d
       mov      rsi, rbx
       mov      edx, r15d
       mov      rcx, r14
       call     [Wide239:DynamicSeekKernel(ushort,byref,int,byref):bool]
 
G_M000_IG276:                ;; offset=0x1915
       movzx    rax, al
 
G_M000_IG277:                ;; offset=0x1918
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG278:                ;; offset=0x1927
       xor      eax, eax
 
G_M000_IG279:                ;; offset=0x1929
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG280:                ;; offset=0x1938
       mov      rdi, 0x7F0EBF3CF268
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG04
 
G_M000_IG281:                ;; offset=0x194C
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	000016D3h ; case G_M000_IG252
       	dd	00001650h ; case G_M000_IG247
       	dd	000015CDh ; case G_M000_IG242
       	dd	000015B6h ; case G_M000_IG241
       	dd	0000159Fh ; case G_M000_IG240
       	dd	00001588h ; case G_M000_IG239
       	dd	00001571h ; case G_M000_IG238
       	dd	0000155Ah ; case G_M000_IG237
       	dd	00001543h ; case G_M000_IG236
       	dd	0000152Ch ; case G_M000_IG235
       	dd	00001515h ; case G_M000_IG234
       	dd	000014FEh ; case G_M000_IG233
       	dd	000014E7h ; case G_M000_IG232
       	dd	000014D0h ; case G_M000_IG231
       	dd	000014B9h ; case G_M000_IG230
       	dd	000014A2h ; case G_M000_IG229
       	dd	0000148Bh ; case G_M000_IG228
       	dd	00001474h ; case G_M000_IG227
       	dd	0000145Dh ; case G_M000_IG226
       	dd	00001446h ; case G_M000_IG225
       	dd	0000142Fh ; case G_M000_IG224
       	dd	00001418h ; case G_M000_IG223
       	dd	00001401h ; case G_M000_IG222
       	dd	000013EAh ; case G_M000_IG221
       	dd	000013D3h ; case G_M000_IG220
       	dd	000013BCh ; case G_M000_IG219
       	dd	000013A5h ; case G_M000_IG218
       	dd	0000138Eh ; case G_M000_IG217
       	dd	00001377h ; case G_M000_IG216
       	dd	00001360h ; case G_M000_IG215
       	dd	00001349h ; case G_M000_IG214
       	dd	00001332h ; case G_M000_IG213
       	dd	0000131Bh ; case G_M000_IG212
       	dd	00001304h ; case G_M000_IG211
       	dd	000012EDh ; case G_M000_IG210
       	dd	000012D6h ; case G_M000_IG209
       	dd	000012BFh ; case G_M000_IG208
       	dd	000012A8h ; case G_M000_IG207
       	dd	00001291h ; case G_M000_IG206
       	dd	0000127Ah ; case G_M000_IG205
       	dd	00001263h ; case G_M000_IG204
       	dd	0000124Ch ; case G_M000_IG203
       	dd	00001235h ; case G_M000_IG202
       	dd	0000121Eh ; case G_M000_IG201
       	dd	00001207h ; case G_M000_IG200
       	dd	000011F0h ; case G_M000_IG199
       	dd	000011D9h ; case G_M000_IG198
       	dd	000011C2h ; case G_M000_IG197
       	dd	000011ABh ; case G_M000_IG196
       	dd	00001194h ; case G_M000_IG195
       	dd	0000117Dh ; case G_M000_IG194
       	dd	00001166h ; case G_M000_IG193
       	dd	0000114Fh ; case G_M000_IG192
       	dd	00001138h ; case G_M000_IG191
       	dd	00001121h ; case G_M000_IG190
       	dd	0000110Ah ; case G_M000_IG189
       	dd	000010F3h ; case G_M000_IG188
       	dd	000010DCh ; case G_M000_IG187
       	dd	000010C5h ; case G_M000_IG186
       	dd	000010AEh ; case G_M000_IG185
       	dd	00001097h ; case G_M000_IG184
       	dd	00001080h ; case G_M000_IG183
       	dd	00001069h ; case G_M000_IG182
       	dd	00001052h ; case G_M000_IG181
       	dd	0000103Bh ; case G_M000_IG180
       	dd	00001024h ; case G_M000_IG179
       	dd	0000100Dh ; case G_M000_IG178
       	dd	00000FF6h ; case G_M000_IG177
       	dd	00000FDFh ; case G_M000_IG176
       	dd	00000FC8h ; case G_M000_IG175
       	dd	00000FB1h ; case G_M000_IG174
       	dd	00000F9Ah ; case G_M000_IG173
       	dd	00000F83h ; case G_M000_IG172
       	dd	00000F6Ch ; case G_M000_IG171
       	dd	00000F55h ; case G_M000_IG170
       	dd	00000F3Eh ; case G_M000_IG169
       	dd	00000F27h ; case G_M000_IG168
       	dd	00000F10h ; case G_M000_IG167
       	dd	00000EF9h ; case G_M000_IG166
       	dd	00000EE2h ; case G_M000_IG165
       	dd	00000ECBh ; case G_M000_IG164
       	dd	00000EB4h ; case G_M000_IG163
       	dd	00000E9Dh ; case G_M000_IG162
       	dd	00000E86h ; case G_M000_IG161
       	dd	00000E6Fh ; case G_M000_IG160
       	dd	00000E58h ; case G_M000_IG159
       	dd	00000E41h ; case G_M000_IG158
       	dd	00000E2Ah ; case G_M000_IG157
       	dd	00000E13h ; case G_M000_IG156
       	dd	00000DFCh ; case G_M000_IG155
       	dd	00000DE5h ; case G_M000_IG154
       	dd	00000DCEh ; case G_M000_IG153
       	dd	00000DB7h ; case G_M000_IG152
       	dd	00000DA0h ; case G_M000_IG151
       	dd	00000D89h ; case G_M000_IG150
       	dd	00000D72h ; case G_M000_IG149
       	dd	00000D5Bh ; case G_M000_IG148
       	dd	00000D44h ; case G_M000_IG147
       	dd	00000D2Dh ; case G_M000_IG146
       	dd	00000D16h ; case G_M000_IG145
       	dd	00000CFFh ; case G_M000_IG144
       	dd	00000CE8h ; case G_M000_IG143
       	dd	00000CD1h ; case G_M000_IG142
       	dd	00000CBAh ; case G_M000_IG141
       	dd	00000CA3h ; case G_M000_IG140
       	dd	00000C8Ch ; case G_M000_IG139
       	dd	00000C75h ; case G_M000_IG138
       	dd	00000C5Eh ; case G_M000_IG137
       	dd	00000C47h ; case G_M000_IG136
       	dd	00000C30h ; case G_M000_IG135
       	dd	00000C19h ; case G_M000_IG134
       	dd	00000C02h ; case G_M000_IG133
       	dd	00000BEBh ; case G_M000_IG132
       	dd	00000BD4h ; case G_M000_IG131
       	dd	00000BBDh ; case G_M000_IG130
       	dd	00000BA6h ; case G_M000_IG129
       	dd	00000B8Fh ; case G_M000_IG128
       	dd	00000B78h ; case G_M000_IG127
       	dd	00000B61h ; case G_M000_IG126
       	dd	00000B4Ah ; case G_M000_IG125
       	dd	00000B33h ; case G_M000_IG124
       	dd	00000B1Ch ; case G_M000_IG123
       	dd	00000B05h ; case G_M000_IG122
       	dd	00000AEEh ; case G_M000_IG121
       	dd	00000AD7h ; case G_M000_IG120
       	dd	00000AC0h ; case G_M000_IG119
       	dd	00000AA9h ; case G_M000_IG118
       	dd	00000A92h ; case G_M000_IG117
       	dd	00000A7Bh ; case G_M000_IG116
       	dd	00000A64h ; case G_M000_IG115
       	dd	00000A4Dh ; case G_M000_IG114
       	dd	00000A36h ; case G_M000_IG113
       	dd	00000A1Fh ; case G_M000_IG112
       	dd	00000A08h ; case G_M000_IG111
       	dd	000009F1h ; case G_M000_IG110
       	dd	000009DAh ; case G_M000_IG109
       	dd	000009C3h ; case G_M000_IG108
       	dd	000009ACh ; case G_M000_IG107
       	dd	00000995h ; case G_M000_IG106
       	dd	0000097Eh ; case G_M000_IG105
       	dd	00000967h ; case G_M000_IG104
       	dd	00000950h ; case G_M000_IG103
       	dd	00000939h ; case G_M000_IG102
       	dd	00000922h ; case G_M000_IG101
       	dd	0000090Bh ; case G_M000_IG100
       	dd	000008F4h ; case G_M000_IG99
       	dd	000008DDh ; case G_M000_IG98
       	dd	000008C6h ; case G_M000_IG97
       	dd	000008AFh ; case G_M000_IG96
       	dd	00000898h ; case G_M000_IG95
       	dd	00000881h ; case G_M000_IG94
       	dd	0000086Ah ; case G_M000_IG93
       	dd	00000853h ; case G_M000_IG92
       	dd	0000083Ch ; case G_M000_IG91
       	dd	00000825h ; case G_M000_IG90
       	dd	0000080Eh ; case G_M000_IG89
       	dd	000007F7h ; case G_M000_IG88
       	dd	000007E0h ; case G_M000_IG87
       	dd	000007C9h ; case G_M000_IG86
       	dd	000007B2h ; case G_M000_IG85
       	dd	0000079Bh ; case G_M000_IG84
       	dd	00000784h ; case G_M000_IG83
       	dd	0000076Dh ; case G_M000_IG82
       	dd	00000756h ; case G_M000_IG81
       	dd	0000073Fh ; case G_M000_IG80
       	dd	00000728h ; case G_M000_IG79
       	dd	00000711h ; case G_M000_IG78
       	dd	000006FAh ; case G_M000_IG77
       	dd	000006E3h ; case G_M000_IG76
       	dd	000006CCh ; case G_M000_IG75
       	dd	000006B5h ; case G_M000_IG74
       	dd	0000069Eh ; case G_M000_IG73
       	dd	00000687h ; case G_M000_IG72
       	dd	00000670h ; case G_M000_IG71
       	dd	00000659h ; case G_M000_IG70
       	dd	00000642h ; case G_M000_IG69
       	dd	0000062Bh ; case G_M000_IG68
       	dd	00000614h ; case G_M000_IG67
       	dd	000005FDh ; case G_M000_IG66
       	dd	000005E6h ; case G_M000_IG65
       	dd	000005CFh ; case G_M000_IG64
       	dd	000005B8h ; case G_M000_IG63
       	dd	000005A1h ; case G_M000_IG62
       	dd	0000058Ah ; case G_M000_IG61
       	dd	00000573h ; case G_M000_IG60
       	dd	0000055Ch ; case G_M000_IG59
       	dd	00000545h ; case G_M000_IG58
       	dd	0000052Eh ; case G_M000_IG57
       	dd	00000517h ; case G_M000_IG56
       	dd	00000500h ; case G_M000_IG55
       	dd	000004E9h ; case G_M000_IG54
       	dd	000004D2h ; case G_M000_IG53
       	dd	000004BBh ; case G_M000_IG52
       	dd	000004A4h ; case G_M000_IG51
       	dd	0000048Dh ; case G_M000_IG50
       	dd	00000476h ; case G_M000_IG49
       	dd	0000045Fh ; case G_M000_IG48
       	dd	00000448h ; case G_M000_IG47
       	dd	00000431h ; case G_M000_IG46
       	dd	0000041Ah ; case G_M000_IG45
       	dd	00000403h ; case G_M000_IG44
       	dd	000003ECh ; case G_M000_IG43
       	dd	000003D5h ; case G_M000_IG42
       	dd	000003BEh ; case G_M000_IG41
       	dd	000003A7h ; case G_M000_IG40
       	dd	00000390h ; case G_M000_IG39
       	dd	00000379h ; case G_M000_IG38
       	dd	00000362h ; case G_M000_IG37
       	dd	0000034Bh ; case G_M000_IG36
       	dd	00000334h ; case G_M000_IG35
       	dd	0000031Dh ; case G_M000_IG34
       	dd	00000306h ; case G_M000_IG33
       	dd	000002EFh ; case G_M000_IG32
       	dd	000002D8h ; case G_M000_IG31
       	dd	000002C1h ; case G_M000_IG30
       	dd	000002AAh ; case G_M000_IG29
       	dd	00000293h ; case G_M000_IG28
       	dd	0000027Ch ; case G_M000_IG27
       	dd	00000265h ; case G_M000_IG26
       	dd	0000024Eh ; case G_M000_IG25
       	dd	00000237h ; case G_M000_IG24
       	dd	00000220h ; case G_M000_IG23
       	dd	00000209h ; case G_M000_IG22
       	dd	000001F2h ; case G_M000_IG21
       	dd	000001DBh ; case G_M000_IG20
       	dd	000001C4h ; case G_M000_IG19
       	dd	000001ADh ; case G_M000_IG18
       	dd	00000196h ; case G_M000_IG17
       	dd	0000017Fh ; case G_M000_IG16
       	dd	00000168h ; case G_M000_IG15
       	dd	00000151h ; case G_M000_IG14
       	dd	0000013Ah ; case G_M000_IG13
       	dd	00000123h ; case G_M000_IG12
       	dd	0000010Ch ; case G_M000_IG11
       	dd	000000F5h ; case G_M000_IG10
       	dd	000000DEh ; case G_M000_IG09
       	dd	000000C7h ; case G_M000_IG08
       	dd	000000B0h ; case G_M000_IG07
       	dd	00000099h ; case G_M000_IG06
RWD956 	dd	000018E4h ; case G_M000_IG275
       	dd	000018D0h ; case G_M000_IG274
       	dd	000018BCh ; case G_M000_IG273
       	dd	000018A8h ; case G_M000_IG272
       	dd	00001894h ; case G_M000_IG271
       	dd	00001880h ; case G_M000_IG270
       	dd	0000186Ch ; case G_M000_IG269
       	dd	00001855h ; case G_M000_IG268
       	dd	0000183Eh ; case G_M000_IG267
       	dd	00001827h ; case G_M000_IG266
       	dd	00001810h ; case G_M000_IG265
       	dd	000017F9h ; case G_M000_IG264
       	dd	000017E2h ; case G_M000_IG263
       	dd	000017CBh ; case G_M000_IG262
       	dd	000017B4h ; case G_M000_IG261
       	dd	0000179Dh ; case G_M000_IG260
       	dd	00001786h ; case G_M000_IG259

; Total bytes of code 6482



; Assembly listing for method RoutingBenchmarks:RunOne():RoutingReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 5 single block inlinees; 3 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     rbx
       push     rax
       lea      rbp, [rsp+0x10]
 
G_M000_IG02:                ;; offset=0x0008
       xor      ecx, ecx
       xor      esi, esi
       xor      r8d, r8d
       mov      r9, gword ptr [rdi+0x08]
       align    [0 bytes for IG03]
 
G_M000_IG03:                ;; offset=0x0013
       mov      eax, r8d
       cdq      
       idiv     edx:eax, dword ptr [r9+0x08]
       mov      rax, r9
       cmp      edx, dword ptr [rax+0x08]
       jae      G_M000_IG12
       mov      r10d, edx
       movzx    rax, word  ptr [rax+2*r10+0x10]
       mov      r11, gword ptr [rdi+0x10]
       cmp      edx, dword ptr [r11+0x08]
       jae      SHORT G_M000_IG12
       shl      r10, 4
       lea      rdx, bword ptr [r11+r10+0x10]
       test     rdx, rdx
       je       SHORT G_M000_IG08
 
G_M000_IG04:                ;; offset=0x0048
       mov      r10, qword ptr [rdx]
       movzx    r11, word  ptr [rdx+0x0C]
       movzx    rbx, byte  ptr [rdx+0x0E]
       test     eax, eax
       jne      SHORT G_M000_IG05
       cmp      r11d, eax
       jne      SHORT G_M000_IG05
       and      ebx, 3
       cmp      ebx, 1
       jne      SHORT G_M000_IG05
       test     r10, r10
       jl       SHORT G_M000_IG05
       test     r10, r10
       jle      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x006F
       xor      eax, eax
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x0073
       mov      eax, 1
 
G_M000_IG07:                ;; offset=0x0078
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x007A
       xor      eax, eax
 
G_M000_IG09:                ;; offset=0x007C
       test     eax, eax
       setne    al
       movzx    rax, al
       add      ecx, eax
       movzx    rax, word  ptr [rdx+0x0C]
       add      rsi, rax
       inc      r8d
       cmp      r8d, 0x10000
       jl       G_M000_IG03
 
G_M000_IG10:                ;; offset=0x009D
       mov      rax, rcx
       mov      rdx, rsi
 
G_M000_IG11:                ;; offset=0x00A3
       add      rsp, 8
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG12:                ;; offset=0x00AA
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 176

; Assembly listing for method RoutingBenchmarks:RunSixteen():RoutingReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 4 single block inlinees; 1 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x0020
       xor      r15d, r15d
       xor      r14d, r14d
       xor      r13d, r13d
 
G_M000_IG03:                ;; offset=0x0029
       mov      rcx, gword ptr [rbx+0x08]
       mov      eax, r13d
       cdq      
       idiv     edx:eax, dword ptr [rcx+0x08]
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG09
       mov      edi, edx
       movzx    rsi, word  ptr [rcx+2*rdi+0x10]
       mov      rcx, gword ptr [rbx+0x10]
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG09
       shl      rdi, 4
       lea      r12, bword ptr [rcx+rdi+0x10]
       lea      rcx, bword ptr [rbx+0x20]
       mov      bword ptr [rbp-0x38], r12
       mov      bword ptr [rbp-0x30], rcx
       cmp      bword ptr [rbp-0x38], 0
       je       SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0065
       lea      rcx, [rbp-0x30]
       mov      edi, esi
       mov      rsi, bword ptr [rbp-0x38]
       xor      edx, edx
       call     [__TlGeneratedSchema1:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0079
       xor      eax, eax
 
G_M000_IG06:                ;; offset=0x007B
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r15d, eax
       movzx    rax, word  ptr [r12+0x0C]
       add      r14, rax
       inc      r13d
       cmp      r13d, 0x10000
       jl       SHORT G_M000_IG03
 
G_M000_IG07:                ;; offset=0x009B
       mov      rax, r15
       mov      rdx, r14
 
G_M000_IG08:                ;; offset=0x00A1
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG09:                ;; offset=0x00B0
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 182

; Assembly listing for method RoutingBenchmarks:RunWide():RoutingReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 4 single block inlinees; 1 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       mov      qword ptr [rbp-0x30], rax
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x0020
       xor      r15d, r15d
       xor      r14d, r14d
       xor      r13d, r13d
 
G_M000_IG03:                ;; offset=0x0029
       mov      rcx, gword ptr [rbx+0x08]
       mov      eax, r13d
       cdq      
       idiv     edx:eax, dword ptr [rcx+0x08]
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG09
       mov      edi, edx
       movzx    rsi, word  ptr [rcx+2*rdi+0x10]
       mov      rcx, gword ptr [rbx+0x10]
       cmp      edx, dword ptr [rcx+0x08]
       jae      SHORT G_M000_IG09
       shl      rdi, 4
       lea      r12, bword ptr [rcx+rdi+0x10]
       lea      rcx, bword ptr [rbx+0x24]
       mov      bword ptr [rbp-0x38], r12
       mov      bword ptr [rbp-0x30], rcx
       cmp      bword ptr [rbp-0x38], 0
       je       SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0065
       lea      rcx, [rbp-0x30]
       mov      edi, esi
       mov      rsi, bword ptr [rbp-0x38]
       xor      edx, edx
       call     [__TlGeneratedSchema2:TrySeek(ushort,byref,int,byref):bool]
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0079
       xor      eax, eax
 
G_M000_IG06:                ;; offset=0x007B
       test     eax, eax
       setne    al
       movzx    rax, al
       add      r15d, eax
       movzx    rax, word  ptr [r12+0x0C]
       add      r14, rax
       inc      r13d
       cmp      r13d, 0x10000
       jl       SHORT G_M000_IG03
 
G_M000_IG07:                ;; offset=0x009B
       mov      rax, r15
       mov      rdx, r14
 
G_M000_IG08:                ;; offset=0x00A1
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG09:                ;; offset=0x00B0
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 182



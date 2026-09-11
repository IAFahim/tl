; Assembly listing for method ScalarCatalogQueryBenchmarks:DirectScalar():BenchmarkReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 3 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       lea      rbp, [rsp+0x20]
       mov      rax, rsi
 
G_M000_IG02:                ;; offset=0x0010
       mov      rcx, gword ptr [rdi+0x10]
       cmp      dword ptr [rcx+0x08], 0
       jbe      G_M000_IG19
       vxorps   xmm0, xmm0, xmm0
       vmovups  xmmword ptr [rcx+0x10], xmm0
       mov      rcx, gword ptr [rdi+0x18]
       cmp      dword ptr [rcx+0x08], 0
       jbe      G_M000_IG19
       vxorps   ymm0, ymm0, ymm0
       vmovdqu  ymmword ptr [rcx+0x10], ymm0
       xor      ecx, ecx
       jmp      SHORT G_M000_IG05
 
G_M000_IG03:                ;; offset=0x0042
       inc      r8d
       mov      dword ptr [rsi], r8d
 
G_M000_IG04:                ;; offset=0x0048
       inc      ecx
 
G_M000_IG05:                ;; offset=0x004A
       mov      r8, gword ptr [rdi+0x08]
       cmp      dword ptr [r8+0x08], ecx
       jle      G_M000_IG11
 
G_M000_IG06:                ;; offset=0x0058
       mov      r8, gword ptr [rdi+0x08]
       cmp      ecx, dword ptr [r8+0x08]
       jae      G_M000_IG19
       mov      r8d, dword ptr [r8+4*rcx+0x10]
       mov      rsi, gword ptr [rdi+0x10]
       cmp      dword ptr [rsi+0x08], 0
       jbe      G_M000_IG19
       add      rsi, 16
       mov      rdx, gword ptr [rdi+0x18]
       cmp      dword ptr [rdx+0x08], 0
       jbe      G_M000_IG19
       add      rdx, 16
       test     r8d, r8d
       jl       G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0098
       mov      r8d, dword ptr [rsi]
       mov      r9, qword ptr [rdx]
       mov      r10, r9
       shl      r10, 5
       sub      r10, r9
       inc      r10
       mov      qword ptr [rdx], r10
       mov      r9, qword ptr [rdx+0x08]
       lea      r9, [r9+4*r9]
       lea      r9, [2*r9+0x01]
       mov      qword ptr [rdx+0x08], r9
       mov      r11d, ecx
       mov      rbx, r11
       add      rbx, qword ptr [rdx+0x10]
       mov      qword ptr [rdx+0x10], rbx
       lea      r15, bword ptr [rdx+0x18]
       mov      r14, r15
       mov      r13d, dword ptr [r14]
       inc      r13d
       mov      dword ptr [r14], r13d
       imul     r10, r10, 37
       add      r10, 2
       mov      qword ptr [rdx], r10
       lea      r9, [r9+4*r9]
       lea      r9, [2*r9+0x02]
       mov      qword ptr [rdx+0x08], r9
       add      rbx, r11
       mov      qword ptr [rdx+0x10], rbx
       mov      r14, r15
       inc      r13d
       mov      dword ptr [r14], r13d
       mov      r14, r10
       shl      r14, 5
       sub      r14, r10
       add      r14, 3
       mov      qword ptr [rdx], r14
       lea      r9, [r9+4*r9]
       add      r9, r9
       add      r9, 3
       mov      qword ptr [rdx+0x08], r9
       add      r11, rbx
       mov      qword ptr [rdx+0x10], r11
       inc      r13d
       mov      dword ptr [r15], r13d
       cmp      r8d, 63
       jne      G_M000_IG03
       xor      edx, edx
       mov      dword ptr [rsi], edx
       inc      qword ptr [rsi+0x08]
       jmp      G_M000_IG04
 
G_M000_IG08:                ;; offset=0x014F
       lea      r8d, [rcx-0x01]
       cmp      dword ptr [rsi], 0
       je       SHORT G_M000_IG09
       mov      r9d, dword ptr [rsi]
       dec      r9d
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0160
       mov      r9d, 63
       dec      qword ptr [rsi+0x08]
 
G_M000_IG10:                ;; offset=0x016A
       mov      dword ptr [rsi], r9d
       mov      rsi, qword ptr [rdx]
       mov      r9, rsi
       shl      r9, 5
       sub      r9, rsi
       add      r9, -3
       mov      qword ptr [rdx], r9
       mov      rsi, qword ptr [rdx+0x08]
       lea      rsi, [rsi+4*rsi]
       lea      rsi, [2*rsi+0x03]
       mov      qword ptr [rdx+0x08], rsi
       mov      r8d, r8d
       mov      r10, r8
       add      r10, qword ptr [rdx+0x10]
       mov      qword ptr [rdx+0x10], r10
       lea      r15, bword ptr [rdx+0x18]
       mov      r11, r15
       mov      r13d, dword ptr [r11]
       inc      r13d
       mov      dword ptr [r11], r13d
       imul     r9, r9, 37
       add      r9, -2
       mov      qword ptr [rdx], r9
       lea      rsi, [rsi+4*rsi]
       lea      rsi, [2*rsi+0x02]
       mov      qword ptr [rdx+0x08], rsi
       add      r10, r8
       mov      qword ptr [rdx+0x10], r10
       mov      r11, r15
       inc      r13d
       mov      dword ptr [r11], r13d
       mov      r11, r9
       shl      r11, 5
       sub      r11, r9
       dec      r11
       mov      qword ptr [rdx], r11
       lea      rsi, [rsi+4*rsi]
       add      rsi, rsi
       inc      rsi
       mov      qword ptr [rdx+0x08], rsi
       add      r8, r10
       mov      qword ptr [rdx+0x10], r8
       mov      r8, r15
       lea      esi, [r13+0x01]
       mov      dword ptr [r8], esi
       jmp      G_M000_IG04
 
G_M000_IG11:                ;; offset=0x0212
       mov      rcx, gword ptr [rdi+0x10]
       test     rcx, rcx
       je       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x021B
       lea      rsi, bword ptr [rcx+0x10]
       mov      edx, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0224
       xor      rsi, rsi
       xor      edx, edx
 
G_M000_IG14:                ;; offset=0x0228
       mov      rcx, gword ptr [rdi+0x18]
       test     rcx, rcx
       je       SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0231
       lea      r8, bword ptr [rcx+0x10]
       mov      edi, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x023A
       xor      r8, r8
       xor      edi, edi
 
G_M000_IG17:                ;; offset=0x023F
       mov      rcx, r8
       mov      r8d, edi
       mov      rdi, rax
 
G_M000_IG18:                ;; offset=0x0248
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [Direct:Capture(System.ReadOnlySpan`1[ReferenceState],System.ReadOnlySpan`1[Accumulator]):BenchmarkReceipt]
 
G_M000_IG19:                ;; offset=0x0256
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 604

 130: JIT compiled ScalarCatalogQueryBenchmarks:DirectScalar() [FullOpts, IL size=121, code size=604]

; Assembly listing for method ScalarCatalogQueryBenchmarks:GeneratedQueryScalar():BenchmarkReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 8 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x60]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       vmovdqa  xmmword ptr [rbp-0x30], xmm8
       mov      qword ptr [rbp-0x20], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x002E
       mov      rsi, gword ptr [rbx+0x20]
       mov      rdx, rsi
       cmp      dword ptr [rdx+0x08], 0
       jbe      G_M000_IG25
       add      rdx, 16
       mov      dword ptr [rdx], 1
       xor      ecx, ecx
       mov      dword ptr [rdx+0x04], ecx
 
G_M000_IG03:                ;; offset=0x004E
       mov      qword ptr [rdx+0x08], rcx
 
G_M000_IG04:                ;; offset=0x0052
       mov      dword ptr [rdx+0x10], ecx
       mov      word  ptr [rdx+0x14], 0
       mov      rdx, gword ptr [rbx+0x28]
       mov      rcx, rdx
       cmp      dword ptr [rcx+0x08], 0
       jbe      G_M000_IG25
       vxorps   ymm0, ymm0, ymm0
       vmovdqu  ymmword ptr [rcx+0x10], ymm0
       test     rsi, rsi
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x007A
       lea      rcx, bword ptr [rsi+0x10]
       mov      r8d, dword ptr [rsi+0x08]
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x0084
       xor      rcx, rcx
       xor      r8d, r8d
 
G_M000_IG07:                ;; offset=0x0089
       test     rdx, rdx
       je       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x008E
       lea      rdi, bword ptr [rdx+0x10]
       mov      eax, dword ptr [rdx+0x08]
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0097
       xor      rdi, rdi
       xor      eax, eax
 
G_M000_IG10:                ;; offset=0x009B
       mov      rsi, rcx
       mov      edx, r8d
       mov      rcx, rdi
       mov      r8d, eax
       lea      rdi, [rbp-0x58]
       call     [BenchmarkCatalog+BenchmarkRowsQuery:.ctor(System.Span`1[BenchmarkCatalog+State],System.Span`1[Accumulator]):this]
 
G_M000_IG11:                ;; offset=0x00B1
       vmovdqu  ymm0, ymmword ptr [rbp-0x58]
       vmovdqu  ymmword ptr [rbp-0x38], ymm0
 
G_M000_IG12:                ;; offset=0x00BB
       cmp      dword ptr [rbx+0x30], 0
       je       SHORT G_M000_IG15
 
G_M000_IG13:                ;; offset=0x00C1
       xor      r14d, r14d
       mov      rdx, gword ptr [rbx+0x08]
       cmp      dword ptr [rdx+0x08], 0
       jle      SHORT G_M000_IG17
 
G_M000_IG14:                ;; offset=0x00CE
       mov      rdx, gword ptr [rbx+0x08]
       cmp      r14d, dword ptr [rdx+0x08]
       jae      G_M000_IG25
       mov      edx, dword ptr [rdx+4*r14+0x10]
       lea      rdi, [rbp-0x38]
       mov      esi, r14d
       call     [BenchmarkCatalog+BenchmarkRowsQuery:Tick(uint,int):this]
       inc      r14d
       mov      rdi, gword ptr [rbx+0x08]
       cmp      dword ptr [rdi+0x08], r14d
       jg       SHORT G_M000_IG14
       jmp      SHORT G_M000_IG17
 
G_M000_IG15:                ;; offset=0x00FD
       xor      r14d, r14d
       mov      rdi, gword ptr [rbx+0x08]
       cmp      dword ptr [rdi+0x08], 0
       jle      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x010A
       lea      rdi, [rbp-0x38]
       mov      esi, r14d
       call     [BenchmarkCatalog+BenchmarkRowsQuery:Tick(uint):this]
       inc      r14d
       mov      rcx, gword ptr [rbx+0x08]
       cmp      dword ptr [rcx+0x08], r14d
       jg       SHORT G_M000_IG16
 
G_M000_IG17:                ;; offset=0x0124
       mov      rcx, gword ptr [rbx+0x20]
       test     rcx, rcx
       je       SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x012D
       lea      rsi, bword ptr [rcx+0x10]
       mov      edx, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG20
 
G_M000_IG19:                ;; offset=0x0136
       xor      rsi, rsi
       xor      edx, edx
 
G_M000_IG20:                ;; offset=0x013A
       mov      rcx, gword ptr [rbx+0x28]
       test     rcx, rcx
       je       SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x0143
       lea      r8, bword ptr [rcx+0x10]
       mov      edi, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x014C
       xor      r8, r8
       xor      edi, edi
 
G_M000_IG23:                ;; offset=0x0151
       mov      rcx, r8
       mov      r8d, edi
       mov      rdi, r15
       call     [Direct:Capture(System.ReadOnlySpan`1[BenchmarkCatalog+State],System.ReadOnlySpan`1[Accumulator]):BenchmarkReceipt]
       mov      rax, r15
 
G_M000_IG24:                ;; offset=0x0163
       vzeroupper 
       add      rsp, 72
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG25:                ;; offset=0x0171
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 375

 132: JIT compiled ScalarCatalogQueryBenchmarks:GeneratedQueryScalar() [FullOpts, IL size=176, code size=375]

; Assembly listing for method BenchmarkCatalog+BenchmarkRowsQuery:Tick(uint):this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 34 single block inlinees; 6 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      ebx, esi
 
G_M000_IG02:                ;; offset=0x0015
       mov      eax, dword ptr [rdi+0x08]
       test     eax, eax
       je       SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x001C
       cmp      eax, 1
       jne      G_M000_IG11
       mov      r15, bword ptr [rdi]
       mov      r14d, dword ptr [r15]
       mov      eax, r14d
       test     eax, eax
       jne      SHORT G_M000_IG05
       mov      byte  ptr [r15+0x15], 0
 
G_M000_IG04:                ;; offset=0x0037
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0046
       cmp      eax, 1
       jne      G_M000_IG13
       mov      r13d, dword ptr [r15+0x04]
       cmp      r13d, 64
       ja       SHORT G_M000_IG04
       cmp      r13d, 64
       je       SHORT G_M000_IG04
       mov      r12d, 64
       cmp      r13d, 63
       jne      SHORT G_M000_IG06
       mov      rax, qword ptr [r15+0x08]
       inc      rax
       xor      ecx, ecx
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x0076
       lea      ecx, [r13+0x01]
       mov      rax, qword ptr [r15+0x08]
 
G_M000_IG07:                ;; offset=0x007E
       mov      dword ptr [rbp-0x30], ecx
       mov      qword ptr [rbp-0x38], rax
       mov      edx, 68
       test     r13d, r13d
       cmove    r12d, edx
       mov      edx, r12d
       or       edx, 8
       movzx    rdx, dl
       cmp      r13d, 63
       cmove    r12d, edx
       add      rdi, 16
       cmp      dword ptr [rdi+0x08], 0
       jbe      G_M000_IG15
       mov      rdx, bword ptr [rdi]
       mov      bword ptr [rbp-0x40], rdx
       cmp      r13d, 64
       jae      G_M000_IG10
       mov      edi, r12d
       mov      esi, r12d
       or       esi, 1
       movzx    rsi, sil
       test     r13d, r13d
       cmove    edi, esi
       mov      esi, edi
       or       esi, 2
       movzx    rsi, sil
       cmp      r13d, 63
       cmove    edi, esi
       mov      dword ptr [rbp-0x2C], edi
       test     byte  ptr [(reloc 0x7fc4d94f20b0)], 1
       je       G_M000_IG14
 
G_M000_IG08:                ;; offset=0x00F4
       mov      esi, -1
       mov      r8d, 1
       mov      edi, dword ptr [rbp-0x2C]
       test     dil, 128
       cmove    esi, r8d
       mov      rdi, 0x7FC4D4C00E40
       mov      edi, dword ptr [rdi]
       mov      r8, 0x7FC4D4C00E88
       mov      r8d, dword ptr [r8]
       imul     esi, r8d
       movsxd   rsi, esi
       mov      rdx, bword ptr [rbp-0x40]
       mov      r8, qword ptr [rdx]
       mov      r9, r8
       shl      r9, 5
       sub      r9, r8
       add      rsi, r9
       mov      qword ptr [rdx], rsi
       mov      rsi, qword ptr [rdx+0x08]
       lea      rsi, [rsi+4*rsi]
       add      rsi, rsi
       movsxd   rdi, edi
       add      rdi, rsi
       mov      qword ptr [rdx+0x08], rdi
       mov      edi, ebx
       add      qword ptr [rdx+0x10], rdi
       lea      rdi, bword ptr [rdx+0x18]
       mov      rsi, rdi
       inc      dword ptr [rsi]
       mov      esi, r12d
       mov      r8d, r12d
       or       r8d, 1
       movzx    r8, r8b
       test     r13d, r13d
       cmove    esi, r8d
       mov      r8d, esi
       or       r8d, 2
       movzx    r8, r8b
       cmp      r13d, 63
       cmove    esi, r8d
       mov      r8d, -1
       mov      r9d, 1
       test     sil, 128
       cmove    r8d, r9d
       mov      rsi, 0x7FC4D4C00E58
       mov      esi, dword ptr [rsi]
       mov      r9, 0x7FC4D4C00EA0
       mov      r9d, dword ptr [r9]
       imul     r10, qword ptr [rdx], 37
       imul     r8d, r9d
       movsxd   r8, r8d
       add      r8, r10
       mov      qword ptr [rdx], r8
       mov      r8, qword ptr [rdx+0x08]
       lea      r8, [r8+4*r8]
       add      r8, r8
       movsxd   rsi, esi
       add      rsi, r8
       mov      qword ptr [rdx+0x08], rsi
       mov      esi, ebx
       add      qword ptr [rdx+0x10], rsi
       mov      rsi, rdi
       inc      dword ptr [rsi]
       mov      esi, r12d
       mov      r8d, r12d
       or       r8d, 1
       movzx    r8, r8b
       test     r13d, r13d
       cmove    esi, r8d
       mov      r8d, esi
 
G_M000_IG09:                ;; offset=0x0203
       or       r8d, 2
       movzx    r8, r8b
       cmp      r13d, 63
       cmove    esi, r8d
       mov      r8d, -1
       mov      r9d, 1
       test     sil, 128
       cmove    r8d, r9d
       mov      rsi, 0x7FC4D4C00E70
       mov      esi, dword ptr [rsi]
       mov      r9, 0x7FC4D4C00EB8
       mov      r9d, dword ptr [r9]
       mov      r10, qword ptr [rdx]
       mov      r11, r10
       shl      r11, 5
       sub      r11, r10
       imul     r8d, r9d
       movsxd   r8, r8d
       add      r8, r11
       mov      qword ptr [rdx], r8
       mov      r8, qword ptr [rdx+0x08]
       lea      r8, [r8+4*r8]
       add      r8, r8
       movsxd   rsi, esi
       add      rsi, r8
       mov      qword ptr [rdx+0x08], rsi
       mov      esi, ebx
       add      qword ptr [rdx+0x10], rsi
       inc      dword ptr [rdi]
 
G_M000_IG10:                ;; offset=0x0277
       mov      dword ptr [r15], r14d
       mov      ecx, dword ptr [rbp-0x30]
       mov      dword ptr [r15+0x04], ecx
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [r15+0x08], rax
       jmp      G_M000_IG04
 
G_M000_IG11:                ;; offset=0x028E
       mov      esi, ebx
       mov      edx, 1
 
G_M000_IG12:                ;; offset=0x0295
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [BenchmarkCatalog+BenchmarkRowsQuery:__tlTickMany(uint,int):this]
 
G_M000_IG13:                ;; offset=0x02A9
       mov      rdi, 0x7FC4D91474C0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x5C2
       mov      rsi, 0x7FC4D91DA458
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.ArgumentException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG14:                ;; offset=0x02E5
       mov      rdi, 0x7FC4D94F2038
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG08
 
G_M000_IG15:                ;; offset=0x02F9
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 767

 135: JIT compiled BenchmarkCatalog+BenchmarkRowsQuery:Tick(uint) [FullOpts, IL size=100, code size=767]

; Assembly listing for method BenchmarkCatalog+BenchmarkRowsQuery:Tick(uint,int):this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 131 single block inlinees; 24 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       push     rax
       lea      rbp, [rsp+0x30]
 
G_M000_IG02:                ;; offset=0x0010
       test     edx, edx
       je       G_M000_IG63
 
G_M000_IG03:                ;; offset=0x0018
       mov      eax, dword ptr [rdi+0x08]
       test     eax, eax
       je       G_M000_IG63
       cmp      eax, 1
       je       SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x0028
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [BenchmarkCatalog+BenchmarkRowsQuery:__tlTickMany(uint,int):this]
 
G_M000_IG05:                ;; offset=0x003C
       mov      rax, bword ptr [rdi]
       mov      ecx, dword ptr [rax]
       mov      r8d, ecx
       test     r8d, r8d
       jne      SHORT G_M000_IG06
       mov      byte  ptr [rax+0x15], 0
       jmp      G_M000_IG63
       align    [0 bytes for IG07]
 
G_M000_IG06:                ;; offset=0x0052
       cmp      r8d, 1
       jne      G_M000_IG64
       cmp      edx, 1
       je       G_M000_IG57
       cmp      edx, -1
       je       G_M000_IG52
       mov      ecx, edx
       shr      ecx, 31
       movsxd   r8, edx
       neg      r8
       mov      edx, edx
       test     ecx, ecx
       cmove    r8, rdx
       jmp      G_M000_IG29
 
G_M000_IG07:                ;; offset=0x0086
       mov      r8d, dword ptr [rax]
       test     r8d, r8d
       je       G_M000_IG63
       mov      r9d, dword ptr [rax+0x04]
       cmp      r9d, 64
       ja       G_M000_IG63
       cmp      r9d, 64
       je       G_M000_IG63
       mov      r10d, 64
       cmp      r9d, 63
       jne      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x00B6
       mov      r11, qword ptr [rax+0x08]
       inc      r11
       xor      ebx, ebx
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00C1
       lea      ebx, [r9+0x01]
       mov      r11, qword ptr [rax+0x08]
 
G_M000_IG10:                ;; offset=0x00C9
       test     r9d, r9d
       jne      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00CE
       mov      r10d, 68
 
G_M000_IG12:                ;; offset=0x00D4
       cmp      r9d, 63
       jne      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x00DA
       or       r10d, 8
       movzx    r10, r10b
 
G_M000_IG14:                ;; offset=0x00E2
       lea      r15, bword ptr [rdi+0x10]
       cmp      dword ptr [r15+0x08], 0
       jbe      G_M000_IG65
       mov      r15, bword ptr [r15]
       cmp      r9d, 64
       jae      G_M000_IG28
 
G_M000_IG15:                ;; offset=0x00FE
       mov      r14d, r10d
       test     r9d, r9d
       jne      SHORT G_M000_IG16
       mov      r14d, r10d
       or       r14d, 1
       movzx    r14, r14b
 
G_M000_IG16:                ;; offset=0x0111
       cmp      r9d, 63
       jne      SHORT G_M000_IG17
       or       r14d, 2
       movzx    r14, r14b
 
G_M000_IG17:                ;; offset=0x011F
       test     r14b, 128
       je       SHORT G_M000_IG18
       mov      r14d, -1
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x012D
       mov      r14d, 1
 
G_M000_IG19:                ;; offset=0x0133
       movsxd   r14, r14d
       mov      r13, qword ptr [r15]
       mov      r12, r13
       shl      r12, 5
       sub      r12, r13
       add      r14, r12
       mov      qword ptr [r15], r14
       mov      r14, qword ptr [r15+0x08]
       lea      r14, [r14+4*r14]
       add      r14, r14
       inc      r14
       mov      qword ptr [r15+0x08], r14
       mov      r14d, esi
       add      qword ptr [r15+0x10], r14
       lea      r14, bword ptr [r15+0x18]
       mov      r13, r14
       inc      dword ptr [r13]
       mov      r13d, r10d
       test     r9d, r9d
       jne      SHORT G_M000_IG20
       mov      r13d, r10d
       or       r13d, 1
       movzx    r13, r13b
 
G_M000_IG20:                ;; offset=0x0180
       cmp      r9d, 63
       jne      SHORT G_M000_IG21
       or       r13d, 2
       movzx    r13, r13b
 
G_M000_IG21:                ;; offset=0x018E
       test     r13b, 128
       je       SHORT G_M000_IG22
       mov      r13d, -1
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x019C
       mov      r13d, 1
 
G_M000_IG23:                ;; offset=0x01A2
       imul     r12, qword ptr [r15], 37
       add      r13d, r13d
       movsxd   r13, r13d
       add      r13, r12
       mov      qword ptr [r15], r13
       mov      r13, qword ptr [r15+0x08]
       lea      r13, [r13+4*r13]
       add      r13, r13
       add      r13, 2
       mov      qword ptr [r15+0x08], r13
       mov      r13d, esi
       add      qword ptr [r15+0x10], r13
       mov      r13, r14
       inc      dword ptr [r13]
       mov      r13d, r10d
       test     r9d, r9d
       jne      SHORT G_M000_IG24
       or       r10d, 1
       movzx    r13, r10b
 
G_M000_IG24:                ;; offset=0x01E4
       cmp      r9d, 63
       jne      SHORT G_M000_IG25
       or       r13d, 2
       movzx    r13, r13b
 
G_M000_IG25:                ;; offset=0x01F2
       test     r13b, 128
       je       SHORT G_M000_IG26
       mov      r9d, -1
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x0200
       mov      r9d, 1
 
G_M000_IG27:                ;; offset=0x0206
       mov      r10, qword ptr [r15]
       mov      r13, r10
       shl      r13, 5
       sub      r13, r10
       lea      r9d, [r9+2*r9]
       movsxd   r9, r9d
       add      r9, r13
       mov      qword ptr [r15], r9
       mov      r9, qword ptr [r15+0x08]
       lea      r9, [r9+4*r9]
       add      r9, r9
       add      r9, 3
       mov      qword ptr [r15+0x08], r9
       mov      r9d, esi
       add      qword ptr [r15+0x10], r9
       inc      dword ptr [r14]
 
G_M000_IG28:                ;; offset=0x023D
       mov      dword ptr [rax], r8d
       mov      dword ptr [rax+0x04], ebx
       mov      qword ptr [rax+0x08], r11
       inc      esi
       mov      r8, rdx
 
G_M000_IG29:                ;; offset=0x024C
       lea      rdx, [r8-0x01]
       test     r8, r8
       je       G_M000_IG63
       test     ecx, ecx
       je       G_M000_IG07
 
G_M000_IG30:                ;; offset=0x0261
       dec      esi
       mov      r8d, dword ptr [rax]
       test     r8d, r8d
       je       G_M000_IG63
       mov      r9d, dword ptr [rax+0x04]
       cmp      r9d, 64
       ja       G_M000_IG63
       cmp      r9d, 64
       je       G_M000_IG63
       mov      r10d, 192
       test     r9d, r9d
       je       SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x0292
       dec      r9d
       mov      r11, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x029B
       mov      r9d, 63
       mov      r11, qword ptr [rax+0x08]
       dec      r11
 
G_M000_IG33:                ;; offset=0x02A8
       test     r9d, r9d
       jne      SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x02AD
       mov      r10d, 196
 
G_M000_IG35:                ;; offset=0x02B3
       cmp      r9d, 63
       jne      SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x02B9
       or       r10d, 8
       movzx    r10, r10b
 
G_M000_IG37:                ;; offset=0x02C1
       lea      rbx, bword ptr [rdi+0x10]
       cmp      dword ptr [rbx+0x08], 0
       jbe      G_M000_IG65
       mov      rbx, bword ptr [rbx]
       cmp      r9d, 64
       jae      G_M000_IG51
 
G_M000_IG38:                ;; offset=0x02DC
       mov      r15d, r10d
       test     r9d, r9d
       jne      SHORT G_M000_IG39
       mov      r15d, r10d
       or       r15d, 1
       movzx    r15, r15b
 
G_M000_IG39:                ;; offset=0x02EF
       cmp      r9d, 63
       jne      SHORT G_M000_IG40
       or       r15d, 2
       movzx    r15, r15b
 
G_M000_IG40:                ;; offset=0x02FD
       test     r15b, 128
       je       SHORT G_M000_IG41
       mov      r15d, -1
       jmp      SHORT G_M000_IG42
 
G_M000_IG41:                ;; offset=0x030B
       mov      r15d, 1
 
G_M000_IG42:                ;; offset=0x0311
       lea      r15d, [r15+2*r15]
       movsxd   r15, r15d
       mov      r14, qword ptr [rbx]
       mov      r13, r14
       shl      r13, 5
       sub      r13, r14
       add      r15, r13
       mov      qword ptr [rbx], r15
       mov      r15, qword ptr [rbx+0x08]
       lea      r15, [r15+4*r15]
       add      r15, r15
       add      r15, 3
       mov      qword ptr [rbx+0x08], r15
       mov      r15d, esi
       add      qword ptr [rbx+0x10], r15
       lea      r14, bword ptr [rbx+0x18]
       mov      r15, r14
       inc      dword ptr [r15]
       mov      r15d, r10d
       test     r9d, r9d
       jne      SHORT G_M000_IG43
       mov      r15d, r10d
       or       r15d, 1
       movzx    r15, r15b
 
G_M000_IG43:                ;; offset=0x0362
       cmp      r9d, 63
       jne      SHORT G_M000_IG44
       or       r15d, 2
       movzx    r15, r15b
 
G_M000_IG44:                ;; offset=0x0370
       test     r15b, 128
       je       SHORT G_M000_IG45
       mov      r15d, -1
       jmp      SHORT G_M000_IG46
 
G_M000_IG45:                ;; offset=0x037E
       mov      r15d, 1
 
G_M000_IG46:                ;; offset=0x0384
       imul     r13, qword ptr [rbx], 37
       add      r15d, r15d
       movsxd   r15, r15d
       add      r15, r13
       mov      qword ptr [rbx], r15
       mov      r15, qword ptr [rbx+0x08]
       lea      r15, [r15+4*r15]
       add      r15, r15
       add      r15, 2
       mov      qword ptr [rbx+0x08], r15
       mov      r15d, esi
       add      qword ptr [rbx+0x10], r15
       mov      r15, r14
       inc      dword ptr [r15]
       mov      r15d, r10d
       test     r9d, r9d
       jne      SHORT G_M000_IG47
       or       r10d, 1
       movzx    r15, r10b
 
G_M000_IG47:                ;; offset=0x03C4
       cmp      r9d, 63
       jne      SHORT G_M000_IG48
       or       r15d, 2
       movzx    r15, r15b
 
G_M000_IG48:                ;; offset=0x03D2
       test     r15b, 128
       je       SHORT G_M000_IG49
       mov      r10d, -1
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:                ;; offset=0x03E0
       mov      r10d, 1
 
G_M000_IG50:                ;; offset=0x03E6
       mov      r15, qword ptr [rbx]
       mov      r13, r15
       shl      r13, 5
       sub      r13, r15
       movsxd   r10, r10d
       add      r10, r13
       mov      qword ptr [rbx], r10
       mov      r10, qword ptr [rbx+0x08]
       lea      r10, [r10+4*r10]
       add      r10, r10
       inc      r10
       mov      qword ptr [rbx+0x08], r10
       mov      r10d, esi
       add      qword ptr [rbx+0x10], r10
       mov      r10, r14
       inc      dword ptr [r10]
 
G_M000_IG51:                ;; offset=0x041B
       mov      dword ptr [rax], r8d
       mov      dword ptr [rax+0x04], r9d
       mov      qword ptr [rax+0x08], r11
       mov      r8, rdx
       jmp      G_M000_IG29
 
G_M000_IG52:                ;; offset=0x042E
       mov      edx, dword ptr [rax+0x04]
       cmp      edx, 64
       ja       G_M000_IG63
       cmp      edx, 64
       je       G_M000_IG63
       mov      r8d, 192
       test     edx, edx
       je       SHORT G_M000_IG53
       dec      edx
       mov      r9, qword ptr [rax+0x08]
       jmp      SHORT G_M000_IG54
 
G_M000_IG53:                ;; offset=0x0455
       mov      edx, 63
       mov      r9, qword ptr [rax+0x08]
       dec      r9
 
G_M000_IG54:                ;; offset=0x0461
       mov      r10d, 196
       test     edx, edx
       cmove    r8d, r10d
       mov      r10d, r8d
       or       r10d, 8
       movzx    r10, r10b
       cmp      edx, 63
       cmove    r8d, r10d
       add      rdi, 16
       dec      esi
       cmp      dword ptr [rdi+0x08], 0
       jbe      G_M000_IG65
       mov      rdi, bword ptr [rdi]
       cmp      edx, 64
       jae      G_M000_IG56
       mov      r10d, r8d
       mov      r11d, r8d
       or       r11d, 1
       movzx    r11, r11b
       test     edx, edx
       cmove    r10d, r11d
       mov      r11d, r10d
       or       r11d, 2
       movzx    r11, r11b
       cmp      edx, 63
       cmove    r10d, r11d
       mov      r11d, -1
       mov      ebx, 1
       test     r10b, 128
       cmove    r11d, ebx
       lea      r10d, [r11+2*r11]
       movsxd   r10, r10d
       mov      r11, qword ptr [rdi]
       mov      rbx, r11
       shl      rbx, 5
       sub      rbx, r11
       add      r10, rbx
       mov      qword ptr [rdi], r10
       mov      r10, qword ptr [rdi+0x08]
       lea      r10, [r10+4*r10]
       add      r10, r10
       add      r10, 3
       mov      qword ptr [rdi+0x08], r10
       mov      r10d, esi
       add      qword ptr [rdi+0x10], r10
       lea      r10, bword ptr [rdi+0x18]
       mov      r11, r10
       inc      dword ptr [r11]
       mov      r11d, r8d
       mov      ebx, r8d
       or       ebx, 1
       movzx    rbx, bl
       test     edx, edx
       cmove    r11d, ebx
       mov      ebx, r11d
       or       ebx, 2
       movzx    rbx, bl
       cmp      edx, 63
       cmove    r11d, ebx
       mov      ebx, -1
       mov      r15d, 1
       test     r11b, 128
       cmove    ebx, r15d
       imul     r11, qword ptr [rdi], 37
       add      ebx, ebx
       movsxd   rbx, ebx
       add      r11, rbx
       mov      qword ptr [rdi], r11
 
G_M000_IG55:                ;; offset=0x0556
       mov      r11, qword ptr [rdi+0x08]
       lea      r11, [r11+4*r11]
       add      r11, r11
       add      r11, 2
       mov      qword ptr [rdi+0x08], r11
       mov      r11d, esi
       add      qword ptr [rdi+0x10], r11
       mov      r11, r10
       inc      dword ptr [r11]
       mov      r11d, r8d
       or       r8d, 1
       movzx    r8, r8b
       test     edx, edx
       cmove    r11d, r8d
       mov      r8d, r11d
       or       r8d, 2
       movzx    r8, r8b
       cmp      edx, 63
       cmove    r11d, r8d
       mov      r8d, -1
       test     r11b, 128
       cmove    r8d, r15d
       mov      r11, qword ptr [rdi]
       mov      rbx, r11
       shl      rbx, 5
       sub      rbx, r11
       movsxd   r8, r8d
       add      r8, rbx
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rdi+0x08]
       lea      r8, [r8+4*r8]
       add      r8, r8
       inc      r8
       mov      qword ptr [rdi+0x08], r8
       mov      esi, esi
       add      qword ptr [rdi+0x10], rsi
       inc      dword ptr [r10]
 
G_M000_IG56:                ;; offset=0x05D8
       mov      dword ptr [rax], ecx
       mov      dword ptr [rax+0x04], edx
       mov      qword ptr [rax+0x08], r9
       jmp      G_M000_IG63
 
G_M000_IG57:                ;; offset=0x05E6
       mov      edx, dword ptr [rax+0x04]
       cmp      edx, 64
       ja       G_M000_IG63
       cmp      edx, 64
       je       G_M000_IG63
       mov      r8d, 64
       cmp      edx, 63
       jne      SHORT G_M000_IG58
       mov      r9d, ecx
       mov      r10, qword ptr [rax+0x08]
       inc      r10
       xor      r11d, r11d
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:                ;; offset=0x0615
       mov      r9d, ecx
       lea      r11d, [rdx+0x01]
       mov      r10, qword ptr [rax+0x08]
 
G_M000_IG59:                ;; offset=0x0620
       mov      ecx, 68
       test     edx, edx
       cmove    r8d, ecx
       mov      ecx, r8d
       or       ecx, 8
       movzx    rcx, cl
       cmp      edx, 63
       cmove    r8d, ecx
       add      rdi, 16
       cmp      dword ptr [rdi+0x08], 0
       jbe      G_M000_IG65
       mov      rdi, bword ptr [rdi]
       cmp      edx, 64
       jae      G_M000_IG62
       mov      ecx, r8d
       mov      ebx, r8d
       or       ebx, 1
       movzx    rbx, bl
       test     edx, edx
       cmove    ecx, ebx
       mov      ebx, ecx
       or       ebx, 2
       movzx    rbx, bl
       cmp      edx, 63
       cmove    ecx, ebx
       mov      ebx, -1
       mov      r15d, 1
       test     cl, 128
       cmove    ebx, r15d
       movsxd   rcx, ebx
       mov      rbx, qword ptr [rdi]
       mov      r15, rbx
       shl      r15, 5
       sub      r15, rbx
       add      rcx, r15
       mov      qword ptr [rdi], rcx
       mov      rcx, qword ptr [rdi+0x08]
       lea      rcx, [rcx+4*rcx]
       add      rcx, rcx
       inc      rcx
       mov      qword ptr [rdi+0x08], rcx
       mov      ecx, esi
       add      qword ptr [rdi+0x10], rcx
       lea      rcx, bword ptr [rdi+0x18]
       mov      rbx, rcx
       inc      dword ptr [rbx]
       mov      ebx, r8d
       mov      r15d, r8d
       or       r15d, 1
       movzx    r15, r15b
       test     edx, edx
       cmove    ebx, r15d
       mov      r15d, ebx
       or       r15d, 2
       movzx    r15, r15b
       cmp      edx, 63
       cmove    ebx, r15d
       mov      r15d, -1
       mov      r14d, 1
       test     bl, 128
       cmove    r15d, r14d
       imul     rbx, qword ptr [rdi], 37
       add      r15d, r15d
       movsxd   r15, r15d
       add      rbx, r15
       mov      qword ptr [rdi], rbx
       mov      rbx, qword ptr [rdi+0x08]
       lea      rbx, [rbx+4*rbx]
 
G_M000_IG60:                ;; offset=0x070E
       add      rbx, rbx
       add      rbx, 2
       mov      qword ptr [rdi+0x08], rbx
       mov      ebx, esi
       add      qword ptr [rdi+0x10], rbx
       mov      rbx, rcx
       inc      dword ptr [rbx]
       mov      ebx, r8d
       or       r8d, 1
       movzx    r8, r8b
       test     edx, edx
       cmove    ebx, r8d
       cmp      edx, 63
       jne      SHORT G_M000_IG61
       mov      edx, ebx
       or       edx, 2
       movzx    rbx, dl
 
G_M000_IG61:                ;; offset=0x0742
       mov      edx, ebx
       mov      r8d, -1
       mov      ebx, 1
       test     dl, 128
       cmove    r8d, ebx
       mov      edx, esi
       mov      rsi, qword ptr [rdi]
       mov      rbx, rsi
       shl      rbx, 5
       sub      rbx, rsi
       lea      esi, [r8+2*r8]
       movsxd   rsi, esi
       add      rsi, rbx
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rdi+0x08]
       lea      rsi, [rsi+4*rsi]
       add      rsi, rsi
       add      rsi, 3
       mov      qword ptr [rdi+0x08], rsi
       add      qword ptr [rdi+0x10], rdx
       mov      rdi, rcx
       inc      dword ptr [rdi]
 
G_M000_IG62:                ;; offset=0x078E
       mov      dword ptr [rax], r9d
       mov      dword ptr [rax+0x04], r11d
       mov      qword ptr [rax+0x08], r10
 
G_M000_IG63:                ;; offset=0x0799
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG64:                ;; offset=0x07A8
       mov      rdi, 0x7FC4D91474C0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x5C2
       mov      rsi, 0x7FC4D91DA458
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.ArgumentException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG65:                ;; offset=0x07E4
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 2026

 324: JIT compiled BenchmarkCatalog+BenchmarkRowsQuery:Tick(uint,int) [FullOpts, IL size=187, code size=2026]

; Assembly listing for method BatchCatalogQueryBenchmarks:DirectBatch():BenchmarkReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 5 single block inlinees; 3 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       push     rax
       lea      rbp, [rsp+0x30]
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0016
       mov      rdi, gword ptr [rbx+0x08]
       call     [System.Array:Clear(System.Array)]
       mov      rdi, gword ptr [rbx+0x10]
       call     [System.Array:Clear(System.Array)]
       xor      ecx, ecx
 
G_M000_IG03:                ;; offset=0x002C
       xor      r8d, r8d
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG04]
 
G_M000_IG04:                ;; offset=0x0031
       inc      edi
       mov      dword ptr [rsi], edi
 
G_M000_IG05:                ;; offset=0x0035
       inc      r8d
 
G_M000_IG06:                ;; offset=0x0038
       mov      rsi, gword ptr [rbx+0x08]
       cmp      dword ptr [rsi+0x08], r8d
       jle      G_M000_IG09
 
G_M000_IG07:                ;; offset=0x0046
       mov      rsi, gword ptr [rbx+0x08]
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG18
       mov      rdx, r8
       shl      rdx, 4
       lea      rsi, bword ptr [rsi+rdx+0x10]
       mov      rdx, gword ptr [rbx+0x10]
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG18
       mov      rdi, r8
       shl      rdi, 5
       lea      rdx, bword ptr [rdx+rdi+0x10]
       mov      edi, dword ptr [rsi]
       mov      rax, qword ptr [rdx]
       mov      r9, rax
       shl      r9, 5
       sub      r9, rax
       inc      r9
       mov      qword ptr [rdx], r9
       mov      rax, qword ptr [rdx+0x08]
       lea      rax, [rax+4*rax]
       lea      rax, [2*rax+0x01]
       mov      qword ptr [rdx+0x08], rax
       mov      r10d, ecx
       mov      r11, r10
       add      r11, qword ptr [rdx+0x10]
       mov      qword ptr [rdx+0x10], r11
       lea      r14, bword ptr [rdx+0x18]
       mov      r13, r14
       mov      r12d, dword ptr [r13]
       inc      r12d
       mov      dword ptr [r13], r12d
       imul     r9, r9, 37
       add      r9, 2
       mov      qword ptr [rdx], r9
       lea      rax, [rax+4*rax]
       lea      rax, [2*rax+0x02]
       mov      qword ptr [rdx+0x08], rax
       add      r11, r10
       mov      qword ptr [rdx+0x10], r11
       mov      r13, r14
       inc      r12d
       mov      dword ptr [r13], r12d
       mov      r13, r9
       shl      r13, 5
       sub      r13, r9
       add      r13, 3
       mov      qword ptr [rdx], r13
       lea      rax, [rax+4*rax]
       add      rax, rax
       add      rax, 3
       mov      qword ptr [rdx+0x08], rax
       add      r10, r11
       mov      qword ptr [rdx+0x10], r10
       inc      r12d
       mov      dword ptr [r14], r12d
       cmp      edi, 63
       jne      G_M000_IG04
 
G_M000_IG08:                ;; offset=0x0125
       xor      edx, edx
       mov      dword ptr [rsi], edx
       inc      qword ptr [rsi+0x08]
       jmp      G_M000_IG05
 
G_M000_IG09:                ;; offset=0x0132
       inc      ecx
       cmp      ecx, 64
       jl       G_M000_IG03
 
G_M000_IG10:                ;; offset=0x013D
       mov      rcx, gword ptr [rbx+0x08]
       test     rcx, rcx
       je       SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0146
       lea      rsi, bword ptr [rcx+0x10]
       mov      edx, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x014F
       xor      rsi, rsi
       xor      edx, edx
 
G_M000_IG13:                ;; offset=0x0153
       mov      rcx, gword ptr [rbx+0x10]
       test     rcx, rcx
       je       SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x015C
       lea      r8, bword ptr [rcx+0x10]
       mov      edi, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0165
       xor      r8, r8
       xor      edi, edi
 
G_M000_IG16:                ;; offset=0x016A
       mov      rcx, r8
       mov      r8d, edi
       mov      rdi, r15
 
G_M000_IG17:                ;; offset=0x0173
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [Direct:Capture(System.ReadOnlySpan`1[ReferenceState],System.ReadOnlySpan`1[Accumulator]):BenchmarkReceipt]
 
G_M000_IG18:                ;; offset=0x0187
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 397

 326: JIT compiled BatchCatalogQueryBenchmarks:DirectBatch() [FullOpts, IL size=113, code size=397]

; Assembly listing for method BatchCatalogQueryBenchmarks:GeneratedQueryBatch():BenchmarkReceipt:this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 7 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     rbx
       sub      rsp, 112
       lea      rbp, [rsp+0x80]
       xor      eax, eax
       mov      qword ptr [rbp-0x68], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:                ;; offset=0x0030
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x48], xmm0
       vmovdqu  xmmword ptr [rbp-0x40], xmm0
       mov      rdi, gword ptr [rbx+0x18]
       mov      dword ptr [rbp-0x48], 1
       vmovdqu  xmm0, xmmword ptr [rbp-0x48]
       vmovdqu  xmmword ptr [rsp], xmm0
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rsp+0x10], rax
       call     [System.Array:Fill[BenchmarkCatalog+State](BenchmarkCatalog+State[],BenchmarkCatalog+State)]
       mov      rdi, gword ptr [rbx+0x20]
       call     [System.Array:Clear(System.Array)]
       mov      rsi, gword ptr [rbx+0x18]
       test     rsi, rsi
       je       SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0075
       lea      rdx, bword ptr [rsi+0x10]
       mov      ecx, dword ptr [rsi+0x08]
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x007E
       xor      rdx, rdx
       xor      ecx, ecx
 
G_M000_IG05:                ;; offset=0x0082
       mov      rsi, gword ptr [rbx+0x20]
       test     rsi, rsi
       je       SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x008B
       lea      r8, bword ptr [rsi+0x10]
       mov      edi, dword ptr [rsi+0x08]
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x0094
       xor      r8, r8
       xor      edi, edi
 
G_M000_IG08:                ;; offset=0x0099
       mov      rsi, rdx
       mov      edx, ecx
       mov      rcx, r8
       mov      r8d, edi
       lea      rdi, [rbp-0x68]
       call     [BenchmarkCatalog+BenchmarkRowsQuery:.ctor(System.Span`1[BenchmarkCatalog+State],System.Span`1[Accumulator]):this]
 
G_M000_IG09:                ;; offset=0x00AE
       vmovdqu  ymm0, ymmword ptr [rbp-0x68]
       vmovdqu  ymmword ptr [rbp-0x30], ymm0
 
G_M000_IG10:                ;; offset=0x00B8
       lea      rdi, [rbp-0x30]
       xor      esi, esi
       mov      edx, 64
       call     [BenchmarkCatalog+BenchmarkRowsQuery:Tick(uint,int):this]
       mov      rcx, gword ptr [rbx+0x18]
       test     rcx, rcx
       je       SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00D2
       lea      rsi, bword ptr [rcx+0x10]
       mov      edx, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00DB
       xor      rsi, rsi
       xor      edx, edx
 
G_M000_IG13:                ;; offset=0x00DF
       mov      rcx, gword ptr [rbx+0x20]
       test     rcx, rcx
       je       SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x00E8
       lea      r8, bword ptr [rcx+0x10]
       mov      edi, dword ptr [rcx+0x08]
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x00F1
       xor      r8, r8
       xor      edi, edi
 
G_M000_IG16:                ;; offset=0x00F6
       mov      rcx, r8
       mov      r8d, edi
       mov      rdi, r15
       call     [Direct:Capture(System.ReadOnlySpan`1[BenchmarkCatalog+State],System.ReadOnlySpan`1[Accumulator]):BenchmarkReceipt]
       mov      rax, r15
 
G_M000_IG17:                ;; offset=0x0108
       vzeroupper 
       add      rsp, 112
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
; Total bytes of code 276

 329: JIT compiled BatchCatalogQueryBenchmarks:GeneratedQueryBatch() [FullOpts, IL size=106, code size=276]

; Assembly listing for method BenchmarkCatalog+BenchmarkRowsQuery:__tlTickMany(uint,int):this (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 0 inlinees with PGO data; 7 single block inlinees; 7 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      rbx, rdi
       mov      r15d, esi
       mov      r14d, edx
 
G_M000_IG02:                ;; offset=0x001C
       mov      rdi, rbx
       call     [BenchmarkCatalog+BenchmarkRowsQuery:Validate():this]
       mov      r13d, r14d
       shr      r13d, 31
       movsxd   r9, r14d
       mov      r12, r9
       neg      r12
       mov      r9d, r14d
       test     r13d, r13d
       cmove    r12, r9
       jmp      G_M000_IG51
       align    [0 bytes for IG06]
 
G_M000_IG03:                ;; offset=0x0044
       xor      r9d, r9d
       test     r13d, r13d
       je       SHORT G_M000_IG05
 
G_M000_IG04:                ;; offset=0x004C
       dec      r15d
 
G_M000_IG05:                ;; offset=0x004F
       xor      edi, edi
       jmp      G_M000_IG16
 
G_M000_IG06:                ;; offset=0x0056
       cmp      edi, dword ptr [rbx+0x08]
       jae      G_M000_IG53
       mov      rsi, bword ptr [rbx]
       lea      rdx, [rdi+2*rdi]
       lea      rsi, bword ptr [rsi+8*rdx]
       mov      byte  ptr [rsi+0x15], 0
       cmp      dword ptr [rsi], 1
       jne      G_M000_IG15
 
G_M000_IG07:                ;; offset=0x0077
       lea      rdx, bword ptr [rsi+0x10]
       lea      rcx, bword ptr [rsi+0x14]
       xor      r8d, r8d
       mov      dword ptr [rdx], r8d
       mov      byte  ptr [rcx], 0
       cmp      dword ptr [rsi], 0
       je       SHORT G_M000_IG15
       mov      r8d, dword ptr [rsi+0x04]
       cmp      r8d, 64
       ja       SHORT G_M000_IG15
       cmp      r8d, 64
       je       SHORT G_M000_IG15
       test     r13d, r13d
       je       SHORT G_M000_IG08
       mov      r9d, 192
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x00AA
       mov      r9d, 64
 
G_M000_IG09:                ;; offset=0x00B0
       mov      byte  ptr [rcx], r9b
       test     r13d, r13d
       jne      SHORT G_M000_IG10
       mov      r9d, dword ptr [rsi+0x04]
       mov      dword ptr [rdx], r9d
       jmp      SHORT G_M000_IG12
 
G_M000_IG10:                ;; offset=0x00C1
       cmp      dword ptr [rsi+0x04], 0
       je       SHORT G_M000_IG11
       mov      r9d, dword ptr [rsi+0x04]
       dec      r9d
       mov      dword ptr [rdx], r9d
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x00D3
       mov      dword ptr [rdx], 63
 
G_M000_IG12:                ;; offset=0x00D9
       cmp      dword ptr [rdx], 0
       jne      SHORT G_M000_IG13
       or       byte  ptr [rcx], 4
 
G_M000_IG13:                ;; offset=0x00E1
       cmp      dword ptr [rdx], 63
       jne      SHORT G_M000_IG14
       or       byte  ptr [rcx], 8
 
G_M000_IG14:                ;; offset=0x00E9
       mov      byte  ptr [rsi+0x15], 1
       mov      r9d, 1
 
G_M000_IG15:                ;; offset=0x00F3
       inc      edi
 
G_M000_IG16:                ;; offset=0x00F5
       cmp      edi, dword ptr [rbx+0x08]
       jl       G_M000_IG06
 
G_M000_IG17:                ;; offset=0x00FE
       test     r9d, r9d
       je       G_M000_IG52
       xor      r12d, r12d
 
G_M000_IG18:                ;; offset=0x010A
       xor      eax, eax
       jmp      SHORT G_M000_IG23
 
G_M000_IG19:                ;; offset=0x010E
       mov      esi, dword ptr [r10+0x10]
       movzx    r8, byte  ptr [r10+0x14]
       test     r8b, 128
       je       SHORT G_M000_IG20
       cmp      dword ptr [r10+0x04], 0
       jne      SHORT G_M000_IG20
       mov      rcx, qword ptr [r10+0x08]
       dec      rcx
       jmp      SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x012D
       mov      rcx, qword ptr [r10+0x08]
 
G_M000_IG21:                ;; offset=0x0131
       lea      r9, bword ptr [rbx+0x10]
       cmp      eax, dword ptr [r9+0x08]
       jae      G_M000_IG53
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, rax
       shl      rdi, 5
       add      rdi, bword ptr [r9]
       mov      r9, rdi
       mov      edi, r12d
       mov      edx, r15d
       call     [MixedTimeline:ExecuteForward0(ushort,uint,uint,long,byte,byref)]
       mov      rax, qword ptr [rbp-0x30]
 
G_M000_IG22:                ;; offset=0x0160
       inc      eax
       mov      r9, rax
 
G_M000_IG23:                ;; offset=0x0165
       cmp      eax, dword ptr [rbx+0x08]
       jge      G_M000_IG28
 
G_M000_IG24:                ;; offset=0x016E
       cmp      eax, dword ptr [rbx+0x08]
       jae      G_M000_IG53
       mov      r9, bword ptr [rbx]
       lea      rdi, [rax+2*rax]
       lea      r10, bword ptr [r9+8*rdi]
       cmp      byte  ptr [r10+0x15], 0
       je       SHORT G_M000_IG22
 
G_M000_IG25:                ;; offset=0x0189
       cmp      dword ptr [r10], 1
       jne      SHORT G_M000_IG22
       test     r13d, r13d
       je       G_M000_IG19
       mov      esi, dword ptr [r10+0x10]
       movzx    r8, byte  ptr [r10+0x14]
       test     r8b, 128
       je       SHORT G_M000_IG26
       cmp      dword ptr [r10+0x04], 0
       jne      SHORT G_M000_IG26
       mov      rcx, qword ptr [r10+0x08]
       dec      rcx
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x01B7
       mov      rcx, qword ptr [r10+0x08]
 
G_M000_IG27:                ;; offset=0x01BB
       lea      r9, bword ptr [rbx+0x10]
       cmp      eax, dword ptr [r9+0x08]
       jae      G_M000_IG53
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, rax
       shl      rdi, 5
       add      rdi, bword ptr [r9]
       mov      r9, rdi
       mov      edi, r12d
       mov      edx, r15d
       call     [MixedTimeline:ExecuteReverse0(ushort,uint,uint,long,byte,byref)]
       mov      rax, qword ptr [rbp-0x30]
       jmp      G_M000_IG22
 
G_M000_IG28:                ;; offset=0x01EF
       xor      eax, eax
       jmp      SHORT G_M000_IG33
 
G_M000_IG29:                ;; offset=0x01F3
       mov      esi, dword ptr [r10+0x10]
       movzx    r8, byte  ptr [r10+0x14]
       test     r8b, 128
       je       SHORT G_M000_IG30
       cmp      dword ptr [r10+0x04], 0
       jne      SHORT G_M000_IG30
       mov      rcx, qword ptr [r10+0x08]
       dec      rcx
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x0212
       mov      rcx, qword ptr [r10+0x08]
 
G_M000_IG31:                ;; offset=0x0216
       lea      r9, bword ptr [rbx+0x10]
       cmp      eax, dword ptr [r9+0x08]
       jae      G_M000_IG53
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, rax
       shl      rdi, 5
       add      rdi, bword ptr [r9]
       mov      r9, rdi
       mov      edi, r12d
       mov      edx, r15d
       call     [MixedTimeline:ExecuteForward1(ushort,uint,uint,long,byte,byref)]
       mov      rax, qword ptr [rbp-0x38]
 
G_M000_IG32:                ;; offset=0x0245
       inc      eax
 
G_M000_IG33:                ;; offset=0x0247
       cmp      eax, dword ptr [rbx+0x08]
       jge      G_M000_IG38
 
G_M000_IG34:                ;; offset=0x0250
       cmp      eax, dword ptr [rbx+0x08]
       jae      G_M000_IG53
       mov      r9, bword ptr [rbx]
       lea      rdi, [rax+2*rax]
       lea      r10, bword ptr [r9+8*rdi]
       cmp      byte  ptr [r10+0x15], 0
       je       SHORT G_M000_IG32
 
G_M000_IG35:                ;; offset=0x026B
       cmp      dword ptr [r10], 1
       jne      SHORT G_M000_IG32
       test     r13d, r13d
       je       G_M000_IG29
       mov      esi, dword ptr [r10+0x10]
       movzx    r8, byte  ptr [r10+0x14]
       test     r8b, 128
       je       SHORT G_M000_IG36
       cmp      dword ptr [r10+0x04], 0
       jne      SHORT G_M000_IG36
       mov      rcx, qword ptr [r10+0x08]
       dec      rcx
       jmp      SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x0299
       mov      rcx, qword ptr [r10+0x08]
 
G_M000_IG37:                ;; offset=0x029D
       lea      r9, bword ptr [rbx+0x10]
       cmp      eax, dword ptr [r9+0x08]
       jae      G_M000_IG53
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, rax
       shl      rdi, 5
       add      rdi, bword ptr [r9]
       mov      r9, rdi
       mov      edi, r12d
       mov      edx, r15d
       call     [MixedTimeline:ExecuteReverse1(ushort,uint,uint,long,byte,byref)]
       mov      rax, qword ptr [rbp-0x38]
       jmp      G_M000_IG32
 
G_M000_IG38:                ;; offset=0x02D1
       inc      r12d
       movzx    r12, r12w
       cmp      r12d, 3
       jl       G_M000_IG18
 
G_M000_IG39:                ;; offset=0x02E2
       xor      eax, eax
       jmp      SHORT G_M000_IG49
       align    [0 bytes for IG40]
 
G_M000_IG40:                ;; offset=0x02E6
       cmp      eax, dword ptr [rbx+0x08]
       jae      G_M000_IG53
       mov      rcx, bword ptr [rbx]
       lea      rdx, [rax+2*rax]
       lea      rcx, bword ptr [rcx+8*rdx]
       cmp      byte  ptr [rcx+0x15], 0
       je       SHORT G_M000_IG48
 
G_M000_IG41:                ;; offset=0x0300
       mov      edx, dword ptr [rcx]
       cmp      edx, 1
       jne      SHORT G_M000_IG47
       mov      edi, dword ptr [rcx+0x10]
       movzx    rsi, byte  ptr [rcx+0x14]
       test     sil, 128
       je       SHORT G_M000_IG44
       cmp      dword ptr [rcx+0x04], 0
       jne      SHORT G_M000_IG42
       mov      r8, qword ptr [rcx+0x08]
       dec      r8
       jmp      SHORT G_M000_IG43
 
G_M000_IG42:                ;; offset=0x0324
       mov      r8, qword ptr [rcx+0x08]
 
G_M000_IG43:                ;; offset=0x0328
       jmp      SHORT G_M000_IG46
 
G_M000_IG44:                ;; offset=0x032A
       test     sil, 8
       je       SHORT G_M000_IG45
       mov      r8, qword ptr [rcx+0x08]
       inc      r8
       xor      edi, edi
       jmp      SHORT G_M000_IG46
 
G_M000_IG45:                ;; offset=0x033B
       mov      r8, qword ptr [rcx+0x08]
       inc      edi
 
G_M000_IG46:                ;; offset=0x0341
       mov      dword ptr [rcx], edx
       mov      dword ptr [rcx+0x04], edi
       mov      qword ptr [rcx+0x08], r8
 
G_M000_IG47:                ;; offset=0x034A
       mov      byte  ptr [rcx+0x15], 0
 
G_M000_IG48:                ;; offset=0x034E
       inc      eax
 
G_M000_IG49:                ;; offset=0x0350
       cmp      eax, dword ptr [rbx+0x08]
       jl       SHORT G_M000_IG40
 
G_M000_IG50:                ;; offset=0x0355
       test     r13d, r13d
       mov      r12, r14
       jne      SHORT G_M000_IG51
       inc      r15d
 
G_M000_IG51:                ;; offset=0x0360
       lea      r9, [r12-0x01]
       mov      r14, r9
       test     r12, r12
       jne      G_M000_IG03
 
G_M000_IG52:                ;; offset=0x0371
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG53:                ;; offset=0x0380
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 902

 333: JIT compiled BenchmarkCatalog+BenchmarkRowsQuery:__tlTickMany(uint,int) [FullOpts, IL size=668, code size=902]

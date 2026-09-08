; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,uint):Tl.Playback (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 64
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x30], xmm8
       mov      qword ptr [rbp-0x20], rax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x0029
       mov      rdi, bword ptr [rbp-0x08]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x003C
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x30], rax
       mov      rsi, gword ptr [rbp-0x30]
       mov      rdi, gword ptr [rbp-0x28]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x28]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x0080
       mov      rdi, bword ptr [rbp-0x08]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0093
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x38], rax
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdi, gword ptr [rbp-0x20]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x20]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x00D7
       mov      rdi, bword ptr [rbp-0x08]
       mov      rsi, bword ptr [rbp-0x10]
       mov      edx, dword ptr [rbp-0x14]
       call     [Tl.FusionExperiment.FusedPulse:ForwardOne(byref,byref,uint):Tl.Playback]
       nop      
 
G_M000_IG07:                ;; offset=0x00E9
       add      rsp, 64
       pop      rbp
       ret      
 
; Total bytes of code 239

; Assembly listing for method Tl.FusionExperiment.FusedPulse:ForwardOne(byref,byref,uint):Tl.Playback (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 112
       lea      rbp, [rsp+0x70]
       xor      eax, eax
       mov      qword ptr [rbp-0x68], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       mov      qword ptr [rbp-0x20], rax
       mov      dword ptr [rbp-0x18], eax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x0031
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x18], edx
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x1C], edx
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       cmp      eax, dword ptr [rbp-0x14]
       ja       SHORT G_M000_IG03
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       sub      eax, dword ptr [rbp-0x6C]
       mov      dword ptr [rbp-0x20], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0083
       mov      eax, dword ptr [rbp-0x1C]
       cmp      eax, dword ptr [rbp-0x18]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x20], eax
 
G_M000_IG04:                ;; offset=0x0092
       mov      eax, dword ptr [rbp-0x20]
       mov      rcx, bword ptr [rbp-0x08]
       movzx    rcx, word  ptr [rcx+0x04]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00AD
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x68], rax
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdx, gword ptr [rbp-0x68]
       mov      rdi, gword ptr [rbp-0x58]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x58]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x010E
       mov      rax, bword ptr [rbp-0x08]
       movzx    rax, word  ptr [rax+0x04]
       add      eax, dword ptr [rbp-0x20]
       movzx    rax, ax
       mov      dword ptr [rbp-0x24], eax
       mov      dword ptr [rbp-0x28], 1
       cmp      dword ptr [rbp-0x1C], 599
       jne      SHORT G_M000_IG07
       mov      eax, dword ptr [rbp-0x28]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x28], eax
 
G_M000_IG07:                ;; offset=0x013B
       cmp      dword ptr [rbp-0x1C], 47
       jae      G_M000_IG12
       cmp      dword ptr [rbp-0x1C], 11
       jae      G_M000_IG10
       cmp      dword ptr [rbp-0x1C], 3
       jae      SHORT G_M000_IG08
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG08:                ;; offset=0x0172
       cmp      dword ptr [rbp-0x1C], 7
       jae      G_M000_IG09
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x2C], xmm0
       mov      dword ptr [rbp-0x30], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x30]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x2C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x34], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x34]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG09:                ;; offset=0x0202
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG10:                ;; offset=0x0237
       cmp      dword ptr [rbp-0x1C], 18
       jb       G_M000_IG18
       cmp      dword ptr [rbp-0x1C], 29
       jae      SHORT G_M000_IG11
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG11:                ;; offset=0x027C
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0x38], xmm0
       mov      dword ptr [rbp-0x3C], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0x3C]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0x40], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x40]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG12:                ;; offset=0x02D2
       cmp      dword ptr [rbp-0x1C], 200
       jae      G_M000_IG15
       cmp      dword ptr [rbp-0x1C], 76
       jae      SHORT G_M000_IG13
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG13:                ;; offset=0x031A
       cmp      dword ptr [rbp-0x1C], 123
       jae      SHORT G_M000_IG14
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0x44], xmm0
       mov      dword ptr [rbp-0x48], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0x48]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x44]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0x4C], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x4C]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG14:                ;; offset=0x038E
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG18
 
G_M000_IG15:                ;; offset=0x03AB
       cmp      dword ptr [rbp-0x1C], 515
       jae      G_M000_IG17
       cmp      dword ptr [rbp-0x1C], 321
       jae      SHORT G_M000_IG16
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG16:                ;; offset=0x03F3
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG17:                ;; offset=0x043D
       cmp      dword ptr [rbp-0x1C], 600
       jae      SHORT G_M000_IG18
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG18:                ;; offset=0x045E
       mov      edi, dword ptr [rbp-0x14]
       mov      esi, dword ptr [rbp-0x24]
       mov      edx, dword ptr [rbp-0x28]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG19:                ;; offset=0x046E
       add      rsp, 112
       pop      rbp
       ret      
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	40400000h		;         3
RWD08  	dd	40000000h		;         2
RWD12  	dd	40A00000h		;         5
RWD16  	dd	41000000h		;         8
RWD20  	dd	41880000h		;        17
RWD24  	dd	41500000h		;        13
RWD28  	dd	42380000h		;        46
RWD32  	dd	42080000h		;        34

; Total bytes of code 1140

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       mov      dword ptr [rbp-0x04], edi
       mov      dword ptr [rbp-0x08], esi
       mov      dword ptr [rbp-0x0C], edx
 
G_M000_IG02:                ;; offset=0x0013
       mov      eax, dword ptr [rbp-0x04]
       movzx    rcx, word  ptr [rbp-0x08]
       mov      ecx, ecx
       shl      rcx, 32
       or       rax, rcx
       movzx    rcx, word  ptr [rbp-0x0C]
       mov      ecx, ecx
       shl      rcx, 48
       or       rax, rcx
       mov      qword ptr [rbp-0x18], rax
       mov      rax, qword ptr [rbp-0x18]
 
G_M000_IG03:                ;; offset=0x0038
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 62

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Backward(byref,byref,uint):Tl.Playback (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 64
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x30], xmm8
       mov      qword ptr [rbp-0x20], rax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x0029
       mov      rdi, bword ptr [rbp-0x08]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x003C
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x30], rax
       mov      rsi, gword ptr [rbp-0x30]
       mov      rdi, gword ptr [rbp-0x28]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x28]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x0080
       mov      rdi, bword ptr [rbp-0x08]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0093
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x38], rax
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdi, gword ptr [rbp-0x20]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x20]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x00D7
       mov      rdi, bword ptr [rbp-0x08]
       mov      rsi, bword ptr [rbp-0x10]
       mov      edx, dword ptr [rbp-0x14]
       call     [Tl.FusionExperiment.FusedPulse:BackwardOne(byref,byref,uint):Tl.Playback]
       nop      
 
G_M000_IG07:                ;; offset=0x00E9
       add      rsp, 64
       pop      rbp
       ret      
 
; Total bytes of code 239

; Assembly listing for method Tl.FusionExperiment.FusedPulse:BackwardOne(byref,byref,uint):Tl.Playback (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 96
       lea      rbp, [rsp+0x60]
       xor      eax, eax
       mov      qword ptr [rbp-0x4C], rax
       mov      dword ptr [rbp-0x44], eax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       mov      qword ptr [rbp-0x20], rax
       mov      dword ptr [rbp-0x18], eax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x002F
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x18], edx
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x1C], edx
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       cmp      eax, dword ptr [rbp-0x14]
       jb       SHORT G_M000_IG03
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      ecx, dword ptr [rbp-0x54]
       sub      ecx, eax
       mov      dword ptr [rbp-0x20], ecx
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0083
       mov      eax, dword ptr [rbp-0x1C]
       cmp      eax, dword ptr [rbp-0x18]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x20], eax
 
G_M000_IG04:                ;; offset=0x0092
       mov      rax, bword ptr [rbp-0x08]
       movzx    rax, word  ptr [rax+0x04]
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x08]
       movzx    rdi, word  ptr [rax+0x04]
       mov      esi, dword ptr [rbp-0x20]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x24], eax
       mov      dword ptr [rbp-0x28], 1
       cmp      dword ptr [rbp-0x1C], 599
       jne      SHORT G_M000_IG05
       mov      eax, dword ptr [rbp-0x28]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x28], eax
 
G_M000_IG05:                ;; offset=0x00D5
       cmp      dword ptr [rbp-0x1C], 47
       jae      G_M000_IG10
       cmp      dword ptr [rbp-0x1C], 11
       jae      G_M000_IG08
       cmp      dword ptr [rbp-0x1C], 3
       jae      SHORT G_M000_IG06
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG06:                ;; offset=0x010C
       cmp      dword ptr [rbp-0x1C], 7
       jae      G_M000_IG07
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x2C], xmm0
       mov      dword ptr [rbp-0x30], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x30]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x2C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x34], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0x34]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG07:                ;; offset=0x019C
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG08:                ;; offset=0x01D1
       cmp      dword ptr [rbp-0x1C], 18
       jb       G_M000_IG16
       cmp      dword ptr [rbp-0x1C], 29
       jae      SHORT G_M000_IG09
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG09:                ;; offset=0x0216
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0x38], xmm0
       mov      dword ptr [rbp-0x3C], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0x3C]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0x40], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0x40]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG10:                ;; offset=0x026C
       cmp      dword ptr [rbp-0x1C], 200
       jae      G_M000_IG13
       cmp      dword ptr [rbp-0x1C], 76
       jae      SHORT G_M000_IG11
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD24]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG11:                ;; offset=0x02B4
       cmp      dword ptr [rbp-0x1C], 123
       jae      SHORT G_M000_IG12
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0x44], xmm0
       mov      dword ptr [rbp-0x48], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0x48]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x44]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0x4C], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0x4C]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG12:                ;; offset=0x0328
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG16
 
G_M000_IG13:                ;; offset=0x0345
       cmp      dword ptr [rbp-0x1C], 515
       jae      G_M000_IG15
       cmp      dword ptr [rbp-0x1C], 321
       jae      SHORT G_M000_IG14
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      SHORT G_M000_IG16
 
G_M000_IG14:                ;; offset=0x038D
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x03D7
       cmp      dword ptr [rbp-0x1C], 600
       jae      SHORT G_M000_IG16
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG16:                ;; offset=0x03F8
       mov      edi, dword ptr [rbp-0x14]
       mov      esi, dword ptr [rbp-0x24]
       mov      edx, dword ptr [rbp-0x28]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG17:                ;; offset=0x0408
       add      rsp, 96
       pop      rbp
       ret      
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	40400000h		;         3
RWD08  	dd	40000000h		;         2
RWD12  	dd	40A00000h		;         5
RWD16  	dd	41000000h		;         8
RWD20  	dd	41880000h		;        17
RWD24  	dd	41500000h		;        13
RWD28  	dd	42380000h		;        46
RWD32  	dd	42080000h		;        34

; Total bytes of code 1038

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; fully interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 752
       lea      rbp, [rsp+0x2F0]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -672
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      bword ptr [rbp-0x30], rdi
       mov      bword ptr [rbp-0x38], rsi
       mov      bword ptr [rbp-0x48], rdx
       mov      qword ptr [rbp-0x40], rcx
 
G_M000_IG02:                ;; offset=0x004B
       mov      dword ptr [rbp-0x220], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0068
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x218], rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x228], rax
       mov      rsi, gword ptr [rbp-0x228]
       mov      rdi, gword ptr [rbp-0x218]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x218]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00BB
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00CE
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x210], rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x230], rax
       mov      rsi, gword ptr [rbp-0x230]
       mov      rdi, gword ptr [rbp-0x210]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x210]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0121
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7FC9271794B0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x0145
       add      rsp, 752
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x014E
       mov      rax, bword ptr [rbp-0x30]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x4C], eax
       mov      rax, bword ptr [rbp-0x30]
       movzx    rax, word  ptr [rax+0x04]
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x4C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x54], edx
       mov      eax, dword ptr [rbp-0x4C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x58], eax
       xor      eax, eax
       mov      dword ptr [rbp-0x5C], eax
       cmp      dword ptr [rbp-0x40], 0
       jbe      G_M000_IG155
       mov      rax, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
       jmp      G_M000_IG147
 
G_M000_IG09:                ;; offset=0x01A6
       cmp      dword ptr [rbp-0x60], 47
       jae      G_M000_IG79
       cmp      dword ptr [rbp-0x60], 11
       jae      G_M000_IG44
       cmp      dword ptr [rbp-0x60], 3
       jae      G_M000_IG21
       jmp      G_M000_IG15
 
G_M000_IG10:                ;; offset=0x01C9
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1F4], eax
       mov      eax, dword ptr [rbp-0x1F4]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x68], eax
       mov      eax, dword ptr [rbp-0x1F4]
       mov      dword ptr [rbp-0x1F8], eax
       mov      eax, dword ptr [rbp-0x1F4]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG11
       mov      eax, dword ptr [rbp-0x68]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x1F8]
       mov      dword ptr [rbp-0x1FC], eax
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0225
       mov      rdi, 0x7FC9271794B4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x1F8]
       mov      dword ptr [rbp-0x1FC], eax
 
G_M000_IG12:                ;; offset=0x024F
       mov      eax, dword ptr [rbp-0x1FC]
       mov      dword ptr [rbp-0x200], eax
       mov      eax, dword ptr [rbp-0x6C]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0271
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x208], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x238], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x240], rax
       mov      rsi, gword ptr [rbp-0x238]
       mov      rdx, gword ptr [rbp-0x240]
       mov      rdi, gword ptr [rbp-0x208]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x208]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG14:                ;; offset=0x02E7
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x6C]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x200]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG20
       mov      rdi, 0x7FC9271794B8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG15:                ;; offset=0x0365
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG19
       cmp      dword ptr [rbp-0x60], 0
       jb       SHORT G_M000_IG18
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x038A
       lea      rdi, [rbp-0x220]
       mov      esi, 286
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG17:                ;; offset=0x039B
       cmp      dword ptr [rbp-0x60], 3
       jb       G_M000_IG10
       mov      rdi, 0x7FC9271794BC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG18:                ;; offset=0x03B9
       mov      rdi, 0x7FC9271794C0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG19:                ;; offset=0x03CD
       mov      rdi, 0x7FC9271794C4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG20:                ;; offset=0x03E1
       mov      rdi, 0x7FC9271794C8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG15
 
G_M000_IG21:                ;; offset=0x03F5
       cmp      dword ptr [rbp-0x60], 7
       jae      G_M000_IG38
       jmp      G_M000_IG27
 
G_M000_IG22:                ;; offset=0x0404
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1DC], eax
       mov      eax, dword ptr [rbp-0x1DC]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x70], eax
       mov      eax, dword ptr [rbp-0x1DC]
       mov      dword ptr [rbp-0x1E0], eax
       mov      eax, dword ptr [rbp-0x1DC]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG23
       mov      eax, dword ptr [rbp-0x70]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x74], eax
       mov      eax, dword ptr [rbp-0x1E0]
       mov      dword ptr [rbp-0x1E4], eax
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x0460
       mov      rdi, 0x7FC9271794CC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x74], eax
       mov      eax, dword ptr [rbp-0x1E0]
       mov      dword ptr [rbp-0x1E4], eax
 
G_M000_IG24:                ;; offset=0x048A
       mov      eax, dword ptr [rbp-0x1E4]
       mov      dword ptr [rbp-0x1E8], eax
       mov      eax, dword ptr [rbp-0x74]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x04AC
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1F0], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x248], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x250], rax
       mov      rsi, gword ptr [rbp-0x248]
       mov      rdx, gword ptr [rbp-0x250]
       mov      rdi, gword ptr [rbp-0x1F0]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1F0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG26:                ;; offset=0x0522
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x74]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x78], xmm0
       mov      dword ptr [rbp-0x7C], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x7C]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x78]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x80], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x80]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1E8]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG32
       mov      rdi, 0x7FC9271794D0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG27:                ;; offset=0x0609
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG31
       cmp      dword ptr [rbp-0x60], 3
       jb       SHORT G_M000_IG30
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x062E
       lea      rdi, [rbp-0x220]
       mov      esi, 510
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:                ;; offset=0x063F
       cmp      dword ptr [rbp-0x60], 7
       jb       G_M000_IG22
       mov      rdi, 0x7FC9271794D4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG30:                ;; offset=0x065D
       mov      rdi, 0x7FC9271794D8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG31:                ;; offset=0x0671
       mov      rdi, 0x7FC9271794DC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG32:                ;; offset=0x0685
       mov      rdi, 0x7FC9271794E0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG33:                ;; offset=0x0699
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1C4], eax
       mov      eax, dword ptr [rbp-0x1C4]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x84], eax
       mov      eax, dword ptr [rbp-0x1C4]
       mov      dword ptr [rbp-0x1C8], eax
       mov      eax, dword ptr [rbp-0x1C4]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG34
       mov      eax, dword ptr [rbp-0x84]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x88], eax
       mov      eax, dword ptr [rbp-0x1C8]
       mov      dword ptr [rbp-0x1CC], eax
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x06FE
       mov      rdi, 0x7FC9271794E4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x88], eax
       mov      eax, dword ptr [rbp-0x1C8]
       mov      dword ptr [rbp-0x1CC], eax
 
G_M000_IG35:                ;; offset=0x072B
       mov      eax, dword ptr [rbp-0x1CC]
       mov      dword ptr [rbp-0x1D0], eax
       mov      eax, dword ptr [rbp-0x88]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x0750
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1D8], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x258], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x260], rax
       mov      rsi, gword ptr [rbp-0x258]
       mov      rdx, gword ptr [rbp-0x260]
       mov      rdi, gword ptr [rbp-0x1D8]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1D8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x07C6
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x88]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1D0]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x84]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG43
       mov      rdi, 0x7FC9271794E8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG38:                ;; offset=0x0862
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG42
       cmp      dword ptr [rbp-0x60], 7
       jb       SHORT G_M000_IG41
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x0887
       lea      rdi, [rbp-0x220]
       mov      esi, 680
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG40:                ;; offset=0x0898
       cmp      dword ptr [rbp-0x60], 11
       jb       G_M000_IG33
       mov      rdi, 0x7FC9271794EC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG41:                ;; offset=0x08B6
       mov      rdi, 0x7FC9271794F0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG42:                ;; offset=0x08CA
       mov      rdi, 0x7FC9271794F4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG43:                ;; offset=0x08DE
       mov      rdi, 0x7FC9271794F8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG38
 
G_M000_IG44:                ;; offset=0x08F2
       cmp      dword ptr [rbp-0x60], 18
       jae      G_M000_IG56
       jmp      G_M000_IG50
 
G_M000_IG45:                ;; offset=0x0901
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1AC], eax
       mov      eax, dword ptr [rbp-0x1AC]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x8C], eax
       mov      eax, dword ptr [rbp-0x1AC]
       mov      dword ptr [rbp-0x1B0], eax
       mov      eax, dword ptr [rbp-0x1AC]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG46
       mov      eax, dword ptr [rbp-0x8C]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x90], eax
       mov      eax, dword ptr [rbp-0x1B0]
       mov      dword ptr [rbp-0x1B4], eax
       jmp      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0966
       mov      rdi, 0x7FC9271794FC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      eax, dword ptr [rbp-0x1B0]
       mov      dword ptr [rbp-0x1B4], eax
 
G_M000_IG47:                ;; offset=0x0993
       mov      eax, dword ptr [rbp-0x1B4]
       mov      dword ptr [rbp-0x1B8], eax
       mov      eax, dword ptr [rbp-0x90]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x09B8
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1C0], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x268], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x270], rax
       mov      rsi, gword ptr [rbp-0x268]
       mov      rdx, gword ptr [rbp-0x270]
       mov      rdi, gword ptr [rbp-0x1C0]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG49:                ;; offset=0x0A2E
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x90]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x1B8]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x8C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG55
       mov      rdi, 0x7FC927179500
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG50:                ;; offset=0x0A9A
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG54
       cmp      dword ptr [rbp-0x60], 11
       jb       SHORT G_M000_IG53
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0ABF
       lea      rdi, [rbp-0x220]
       mov      esi, 843
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG52:                ;; offset=0x0AD0
       cmp      dword ptr [rbp-0x60], 18
       jb       G_M000_IG45
       mov      rdi, 0x7FC927179504
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG53:                ;; offset=0x0AEE
       mov      rdi, 0x7FC927179508
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG54:                ;; offset=0x0B02
       mov      rdi, 0x7FC92717950C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG55:                ;; offset=0x0B16
       mov      rdi, 0x7FC927179510
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG50
 
G_M000_IG56:                ;; offset=0x0B2A
       cmp      dword ptr [rbp-0x60], 29
       jae      G_M000_IG73
       jmp      G_M000_IG62
 
G_M000_IG57:                ;; offset=0x0B39
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x194], eax
       mov      eax, dword ptr [rbp-0x194]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x194]
       mov      dword ptr [rbp-0x198], eax
       mov      eax, dword ptr [rbp-0x194]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG58
       mov      eax, dword ptr [rbp-0x94]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x198]
       mov      dword ptr [rbp-0x19C], eax
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:                ;; offset=0x0B9E
       mov      rdi, 0x7FC927179514
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x198]
       mov      dword ptr [rbp-0x19C], eax
 
G_M000_IG59:                ;; offset=0x0BCB
       mov      eax, dword ptr [rbp-0x19C]
       mov      dword ptr [rbp-0x1A0], eax
       mov      eax, dword ptr [rbp-0x98]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG61
 
G_M000_IG60:                ;; offset=0x0BF0
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x278], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x280], rax
       mov      rsi, gword ptr [rbp-0x278]
       mov      rdx, gword ptr [rbp-0x280]
       mov      rdi, gword ptr [rbp-0x1A8]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1A8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG61:                ;; offset=0x0C66
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x98]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1A0]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x94]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG67
       mov      rdi, 0x7FC927179518
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG62:                ;; offset=0x0D02
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG66
       cmp      dword ptr [rbp-0x60], 18
       jb       SHORT G_M000_IG65
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:                ;; offset=0x0D27
       lea      rdi, [rbp-0x220]
       mov      esi, 0x405
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:                ;; offset=0x0D38
       cmp      dword ptr [rbp-0x60], 29
       jb       G_M000_IG57
       mov      rdi, 0x7FC92717951C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG65:                ;; offset=0x0D56
       mov      rdi, 0x7FC927179520
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG66:                ;; offset=0x0D6A
       mov      rdi, 0x7FC927179524
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG67:                ;; offset=0x0D7E
       mov      rdi, 0x7FC927179528
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG62
 
G_M000_IG68:                ;; offset=0x0D92
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x17C], eax
       mov      eax, dword ptr [rbp-0x17C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x17C]
       mov      dword ptr [rbp-0x180], eax
       mov      eax, dword ptr [rbp-0x17C]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG69
       mov      eax, dword ptr [rbp-0x9C]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x180]
       mov      dword ptr [rbp-0x184], eax
       jmp      SHORT G_M000_IG70
 
G_M000_IG69:                ;; offset=0x0DF7
       mov      rdi, 0x7FC92717952C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x180]
       mov      dword ptr [rbp-0x184], eax
 
G_M000_IG70:                ;; offset=0x0E24
       mov      eax, dword ptr [rbp-0x184]
       mov      dword ptr [rbp-0x188], eax
       mov      eax, dword ptr [rbp-0xA0]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG72
 
G_M000_IG71:                ;; offset=0x0E49
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x190], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x288], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x290], rax
       mov      rsi, gword ptr [rbp-0x288]
       mov      rdx, gword ptr [rbp-0x290]
       mov      rdi, gword ptr [rbp-0x190]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x190]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG72:                ;; offset=0x0EBF
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xA0]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0xA4], xmm0
       mov      dword ptr [rbp-0xA8], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0xA8]
       vmulss   xmm0, xmm0, dword ptr [rbp-0xA4]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0xAC], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0xAC]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x188]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x9C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG78
       mov      rdi, 0x7FC927179530
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG73:                ;; offset=0x0F8E
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG77
       cmp      dword ptr [rbp-0x60], 29
       jb       SHORT G_M000_IG76
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG75
 
G_M000_IG74:                ;; offset=0x0FB3
       lea      rdi, [rbp-0x220]
       mov      esi, 0x4C7
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG75:                ;; offset=0x0FC4
       cmp      dword ptr [rbp-0x60], 47
       jb       G_M000_IG68
       mov      rdi, 0x7FC927179534
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG76:                ;; offset=0x0FE2
       mov      rdi, 0x7FC927179538
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG77:                ;; offset=0x0FF6
       mov      rdi, 0x7FC92717953C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG78:                ;; offset=0x100A
       mov      rdi, 0x7FC927179540
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG73
 
G_M000_IG79:                ;; offset=0x101E
       cmp      dword ptr [rbp-0x60], 200
       jae      G_M000_IG114
       cmp      dword ptr [rbp-0x60], 76
       jae      G_M000_IG91
       jmp      G_M000_IG85
 
G_M000_IG80:                ;; offset=0x103A
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x164], eax
       mov      eax, dword ptr [rbp-0x164]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xB0], eax
       mov      eax, dword ptr [rbp-0x164]
       mov      dword ptr [rbp-0x168], eax
       mov      eax, dword ptr [rbp-0x164]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG81
       mov      eax, dword ptr [rbp-0xB0]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x16C], eax
       jmp      SHORT G_M000_IG82
 
G_M000_IG81:                ;; offset=0x109F
       mov      rdi, 0x7FC927179544
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x16C], eax
 
G_M000_IG82:                ;; offset=0x10CC
       mov      eax, dword ptr [rbp-0x16C]
       mov      dword ptr [rbp-0x170], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG84
 
G_M000_IG83:                ;; offset=0x10F1
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x178], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x298], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2A0], rax
       mov      rsi, gword ptr [rbp-0x298]
       mov      rdx, gword ptr [rbp-0x2A0]
       mov      rdi, gword ptr [rbp-0x178]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x178]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG84:                ;; offset=0x1167
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xB4]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x170]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG90
       mov      rdi, 0x7FC927179548
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG85:                ;; offset=0x1203
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG89
       cmp      dword ptr [rbp-0x60], 47
       jb       SHORT G_M000_IG88
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG87
 
G_M000_IG86:                ;; offset=0x1228
       lea      rdi, [rbp-0x220]
       mov      esi, 0x58D
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG87:                ;; offset=0x1239
       cmp      dword ptr [rbp-0x60], 76
       jb       G_M000_IG80
       mov      rdi, 0x7FC92717954C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG88:                ;; offset=0x1257
       mov      rdi, 0x7FC927179550
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG89:                ;; offset=0x126B
       mov      rdi, 0x7FC927179554
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG90:                ;; offset=0x127F
       mov      rdi, 0x7FC927179558
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG85
 
G_M000_IG91:                ;; offset=0x1293
       cmp      dword ptr [rbp-0x60], 123
       jae      G_M000_IG108
       jmp      G_M000_IG97
 
G_M000_IG92:                ;; offset=0x12A2
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x14C], eax
       mov      eax, dword ptr [rbp-0x14C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0x14C]
       mov      dword ptr [rbp-0x150], eax
       mov      eax, dword ptr [rbp-0x14C]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG93
       mov      eax, dword ptr [rbp-0xB8]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xBC], eax
       mov      eax, dword ptr [rbp-0x150]
       mov      dword ptr [rbp-0x154], eax
       jmp      SHORT G_M000_IG94
 
G_M000_IG93:                ;; offset=0x1307
       mov      rdi, 0x7FC92717955C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       mov      eax, dword ptr [rbp-0x150]
       mov      dword ptr [rbp-0x154], eax
 
G_M000_IG94:                ;; offset=0x1334
       mov      eax, dword ptr [rbp-0x154]
       mov      dword ptr [rbp-0x158], eax
       mov      eax, dword ptr [rbp-0xBC]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG96
 
G_M000_IG95:                ;; offset=0x1359
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x160], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2A8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2B0], rax
       mov      rsi, gword ptr [rbp-0x2A8]
       mov      rdx, gword ptr [rbp-0x2B0]
       mov      rdi, gword ptr [rbp-0x160]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x160]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG96:                ;; offset=0x13CF
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xBC]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0xC0], xmm0
       mov      dword ptr [rbp-0xC4], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0xC4]
       vmulss   xmm0, xmm0, dword ptr [rbp-0xC0]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0xC8], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0xC8]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x158]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG102
       mov      rdi, 0x7FC927179560
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG97:                ;; offset=0x14B6
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG101
       cmp      dword ptr [rbp-0x60], 76
       jb       SHORT G_M000_IG100
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG99
 
G_M000_IG98:                ;; offset=0x14DB
       lea      rdi, [rbp-0x220]
       mov      esi, 0x667
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG99:                ;; offset=0x14EC
       cmp      dword ptr [rbp-0x60], 123
       jb       G_M000_IG92
       mov      rdi, 0x7FC927179564
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG100:                ;; offset=0x150A
       mov      rdi, 0x7FC927179568
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG101:                ;; offset=0x151E
       mov      rdi, 0x7FC92717956C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG102:                ;; offset=0x1532
       mov      rdi, 0x7FC927179570
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG97
 
G_M000_IG103:                ;; offset=0x1546
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x134], eax
       mov      eax, dword ptr [rbp-0x134]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xCC], eax
       mov      eax, dword ptr [rbp-0x134]
       mov      dword ptr [rbp-0x138], eax
       mov      eax, dword ptr [rbp-0x134]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG104
       mov      eax, dword ptr [rbp-0xCC]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0x138]
       mov      dword ptr [rbp-0x13C], eax
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x15AB
       mov      rdi, 0x7FC927179574
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0x138]
       mov      dword ptr [rbp-0x13C], eax
 
G_M000_IG105:                ;; offset=0x15D8
       mov      eax, dword ptr [rbp-0x13C]
       mov      dword ptr [rbp-0x140], eax
       mov      eax, dword ptr [rbp-0xD0]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG107
 
G_M000_IG106:                ;; offset=0x15FD
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x148], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2B8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rsi, gword ptr [rbp-0x2B8]
       mov      rdx, gword ptr [rbp-0x2C0]
       mov      rdi, gword ptr [rbp-0x148]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x148]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG107:                ;; offset=0x1673
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xD0]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x140]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG113
       mov      rdi, 0x7FC927179578
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG108:                ;; offset=0x16F7
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG112
       cmp      dword ptr [rbp-0x60], 123
       jb       SHORT G_M000_IG111
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG110
 
G_M000_IG109:                ;; offset=0x171C
       lea      rdi, [rbp-0x220]
       mov      esi, 0x709
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG110:                ;; offset=0x172D
       cmp      dword ptr [rbp-0x60], 200
       jb       G_M000_IG103
       mov      rdi, 0x7FC92717957C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG111:                ;; offset=0x174E
       mov      rdi, 0x7FC927179580
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG112:                ;; offset=0x1762
       mov      rdi, 0x7FC927179584
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG113:                ;; offset=0x1776
       mov      rdi, 0x7FC927179588
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG108
 
G_M000_IG114:                ;; offset=0x178A
       cmp      dword ptr [rbp-0x60], 321
       jae      G_M000_IG126
       jmp      G_M000_IG120
 
G_M000_IG115:                ;; offset=0x179C
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x11C], eax
       mov      eax, dword ptr [rbp-0x11C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xD4], eax
       mov      eax, dword ptr [rbp-0x11C]
       mov      dword ptr [rbp-0x120], eax
       mov      eax, dword ptr [rbp-0x11C]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG116
       mov      eax, dword ptr [rbp-0xD4]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xD8], eax
       mov      eax, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x124], eax
       jmp      SHORT G_M000_IG117
 
G_M000_IG116:                ;; offset=0x1801
       mov      rdi, 0x7FC92717958C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xD8], eax
       mov      eax, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x124], eax
 
G_M000_IG117:                ;; offset=0x182E
       mov      eax, dword ptr [rbp-0x124]
       mov      dword ptr [rbp-0x128], eax
       mov      eax, dword ptr [rbp-0xD8]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG119
 
G_M000_IG118:                ;; offset=0x1853
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x130], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2C8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rsi, gword ptr [rbp-0x2C8]
       mov      rdx, gword ptr [rbp-0x2D0]
       mov      rdi, gword ptr [rbp-0x130]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x130]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG119:                ;; offset=0x18C9
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xD8]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG125
       mov      rdi, 0x7FC927179590
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG120:                ;; offset=0x1965
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG124
       cmp      dword ptr [rbp-0x60], 200
       jb       SHORT G_M000_IG123
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG122
 
G_M000_IG121:                ;; offset=0x198D
       lea      rdi, [rbp-0x220]
       mov      esi, 0x7CC
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG122:                ;; offset=0x199E
       cmp      dword ptr [rbp-0x60], 321
       jb       G_M000_IG115
       mov      rdi, 0x7FC927179594
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG123:                ;; offset=0x19BF
       mov      rdi, 0x7FC927179598
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG124:                ;; offset=0x19D3
       mov      rdi, 0x7FC92717959C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG125:                ;; offset=0x19E7
       mov      rdi, 0x7FC9271795A0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG120
 
G_M000_IG126:                ;; offset=0x19FB
       cmp      dword ptr [rbp-0x60], 515
       jae      G_M000_IG143
       jmp      G_M000_IG132
 
G_M000_IG127:                ;; offset=0x1A0D
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x104], eax
       mov      eax, dword ptr [rbp-0x104]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xDC], eax
       mov      eax, dword ptr [rbp-0x104]
       mov      dword ptr [rbp-0x108], eax
       mov      eax, dword ptr [rbp-0x104]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG128
       mov      eax, dword ptr [rbp-0xDC]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xE0], eax
       mov      eax, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x10C], eax
       jmp      SHORT G_M000_IG129
 
G_M000_IG128:                ;; offset=0x1A72
       mov      rdi, 0x7FC9271795A4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xE0], eax
       mov      eax, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x10C], eax
 
G_M000_IG129:                ;; offset=0x1A9F
       mov      eax, dword ptr [rbp-0x10C]
       mov      dword ptr [rbp-0x110], eax
       mov      eax, dword ptr [rbp-0xE0]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x1AC4
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x118], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2D8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2E0], rax
       mov      rsi, gword ptr [rbp-0x2D8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rdi, gword ptr [rbp-0x118]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x118]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG131:                ;; offset=0x1B3A
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xE0]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x110]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG137
       mov      rdi, 0x7FC9271795A8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG132:                ;; offset=0x1BEE
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG136
       cmp      dword ptr [rbp-0x60], 321
       jb       SHORT G_M000_IG135
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x1C16
       lea      rdi, [rbp-0x220]
       mov      esi, 0x899
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG134:                ;; offset=0x1C27
       cmp      dword ptr [rbp-0x60], 515
       jb       G_M000_IG127
       mov      rdi, 0x7FC9271795AC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG135:                ;; offset=0x1C48
       mov      rdi, 0x7FC9271795B0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG136:                ;; offset=0x1C5C
       mov      rdi, 0x7FC9271795B4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG137:                ;; offset=0x1C70
       mov      rdi, 0x7FC9271795B8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG132
 
G_M000_IG138:                ;; offset=0x1C84
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xEC], eax
       mov      eax, dword ptr [rbp-0xEC]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xE4], eax
       mov      eax, dword ptr [rbp-0xEC]
       mov      dword ptr [rbp-0xF0], eax
       mov      eax, dword ptr [rbp-0xEC]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG139
       mov      eax, dword ptr [rbp-0xE4]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xE8], eax
       mov      eax, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0xF4], eax
       jmp      SHORT G_M000_IG140
 
G_M000_IG139:                ;; offset=0x1CE9
       mov      rdi, 0x7FC9271795BC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xE8], eax
       mov      eax, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0xF4], eax
 
G_M000_IG140:                ;; offset=0x1D16
       mov      eax, dword ptr [rbp-0xF4]
       mov      dword ptr [rbp-0xF8], eax
       mov      eax, dword ptr [rbp-0xE8]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG142
 
G_M000_IG141:                ;; offset=0x1D3B
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2E8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2F0], rax
       mov      rsi, gword ptr [rbp-0x2E8]
       mov      rdx, gword ptr [rbp-0x2F0]
       mov      rdi, gword ptr [rbp-0x100]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x100]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG142:                ;; offset=0x1DB1
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xE8]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xF8]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG154
       mov      rdi, 0x7FC9271795C0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG143:                ;; offset=0x1E35
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG153
       cmp      dword ptr [rbp-0x60], 515
       jb       G_M000_IG152
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG145
 
G_M000_IG144:                ;; offset=0x1E65
       lea      rdi, [rbp-0x220]
       mov      esi, 0x93B
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG145:                ;; offset=0x1E76
       cmp      dword ptr [rbp-0x60], 600
       jb       G_M000_IG138
 
G_M000_IG146:                ;; offset=0x1E83
       mov      rdi, 0x7FC9271795C4
       call     CORINFO_HELP_COUNTPROFILE32
 
G_M000_IG147:                ;; offset=0x1E92
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG149
 
G_M000_IG148:                ;; offset=0x1EA9
       lea      rdi, [rbp-0x220]
       mov      esi, 0x947
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG149:                ;; offset=0x1EBA
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jb       G_M000_IG09
       mov      dword ptr [rbp-0x64], 1
       cmp      dword ptr [rbp-0x54], 599
       jne      SHORT G_M000_IG150
       mov      rdi, 0x7FC9271795C8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x64]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x64], eax
 
G_M000_IG150:                ;; offset=0x1EF1
       mov      rdi, 0x7FC9271795CC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x64]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG151:                ;; offset=0x1F10
       add      rsp, 752
       pop      rbp
       ret      
 
G_M000_IG152:                ;; offset=0x1F19
       mov      rdi, 0x7FC9271795D0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG153:                ;; offset=0x1F2D
       mov      rdi, 0x7FC9271795D4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG154:                ;; offset=0x1F41
       mov      rdi, 0x7FC9271795D8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG143
 
G_M000_IG155:                ;; offset=0x1F55
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	40400000h		;         3
RWD08  	dd	40000000h		;         2
RWD12  	dd	40A00000h		;         5
RWD16  	dd	41000000h		;         8
RWD20  	dd	41880000h		;        17
RWD24  	dd	41500000h		;        13
RWD28  	dd	42380000h		;        46
RWD32  	dd	42080000h		;        34

; Total bytes of code 8027

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x899
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 4
; 0 inlinees with PGO data; 4 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 16
       mov      qword ptr [rsp+0x308], r15
       mov      qword ptr [rsp+0x300], rbx
       lea      rbp, [rsp+0x10]
       mov      rdi, bword ptr [rbp+0x2C8]
       mov      esi, dword ptr [rbp+0x2B4]
       mov      edx, dword ptr [rbp+0x2B0]
       mov      r9d, dword ptr [rbp+0x2AC]
       mov      r8d, dword ptr [rbp+0x2A8]
       mov      eax, dword ptr [rbp+0x2A4]
       mov      ecx, dword ptr [rbp+0x2A0]
 
G_M000_IG02:                ;; offset=0x004B
       mov      r10, bword ptr [rbp+0x2B8]
       mov      r11d, dword ptr [rbp+0x2C0]
 
G_M000_IG03:                ;; offset=0x0059
       cmp      ecx, 515
       jae      G_M000_IG10
 
G_M000_IG04:                ;; offset=0x0065
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jb       G_M000_IG21
 
G_M000_IG05:                ;; offset=0x008A
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG06:                ;; offset=0x0090
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG111
       add      edx, esi
       movzx    rdx, dx
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      SHORT G_M000_IG08
 
G_M000_IG07:                ;; offset=0x00F0
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG08:                ;; offset=0x0114
       cmp      eax, r11d
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x0119
       cmp      ecx, 321
       jae      G_M000_IG03
 
G_M000_IG10:                ;; offset=0x0125
       cmp      eax, r11d
       jae      G_M000_IG99
 
G_M000_IG11:                ;; offset=0x012E
       cmp      ecx, 47
       jae      G_M000_IG59
 
G_M000_IG12:                ;; offset=0x0137
       cmp      ecx, 11
       jae      G_M000_IG36
 
G_M000_IG13:                ;; offset=0x0140
       cmp      ecx, 3
       jb       G_M000_IG33
 
G_M000_IG14:                ;; offset=0x0149
       cmp      ecx, 7
       jae      G_M000_IG26
       align    [0 bytes for IG15]
 
G_M000_IG15:                ;; offset=0x0152
       cmp      ecx, 3
       jb       SHORT G_M000_IG10
 
G_M000_IG16:                ;; offset=0x0157
       cmp      ecx, 7
       jae      SHORT G_M000_IG10
 
G_M000_IG17:                ;; offset=0x015C
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jb       G_M000_IG22
 
G_M000_IG18:                ;; offset=0x0181
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG19:                ;; offset=0x0187
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG102
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdi], xmm0
       lea      esi, [rcx-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, dword ptr [rdi]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG20:                ;; offset=0x01FF
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
       jmp      G_M000_IG15
 
G_M000_IG21:                ;; offset=0x021F
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG06
       align    [0 bytes for IG23]
 
G_M000_IG22:                ;; offset=0x022F
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG19
 
G_M000_IG23:                ;; offset=0x023F
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG24:                ;; offset=0x0245
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG103
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG25:                ;; offset=0x0299
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG26:                ;; offset=0x02B4
       cmp      ecx, 7
       jb       G_M000_IG10
 
G_M000_IG27:                ;; offset=0x02BD
       cmp      ecx, 11
       jae      G_M000_IG10
 
G_M000_IG28:                ;; offset=0x02C6
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jae      G_M000_IG23
 
G_M000_IG29:                ;; offset=0x02EB
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG24
       align    [0 bytes for IG30]
 
G_M000_IG30:                ;; offset=0x02FB
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG31:                ;; offset=0x0301
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG101
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG32:                ;; offset=0x0345
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG33:                ;; offset=0x0360
       cmp      ecx, 3
       jae      G_M000_IG10
 
G_M000_IG34:                ;; offset=0x0369
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jae      G_M000_IG30
 
G_M000_IG35:                ;; offset=0x038E
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG31
 
G_M000_IG36:                ;; offset=0x039E
       cmp      ecx, 18
       jb       G_M000_IG55
 
G_M000_IG37:                ;; offset=0x03A7
       cmp      ecx, 29
       jae      G_M000_IG48
       align    [0 bytes for IG38]
 
G_M000_IG38:                ;; offset=0x03B0
       cmp      ecx, 18
       jb       G_M000_IG10
 
G_M000_IG39:                ;; offset=0x03B9
       cmp      ecx, 29
       jae      G_M000_IG10
 
G_M000_IG40:                ;; offset=0x03C2
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jb       SHORT G_M000_IG44
 
G_M000_IG41:                ;; offset=0x03E3
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG42:                ;; offset=0x03E9
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG105
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG43:                ;; offset=0x043D
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
       jmp      G_M000_IG38
 
G_M000_IG44:                ;; offset=0x045D
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG42
       align    [0 bytes for IG45]
 
G_M000_IG45:                ;; offset=0x046D
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG46:                ;; offset=0x0473
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG106
       add      esi, edx
       movzx    rdx, si
       lea      esi, [rcx-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [rdi]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG47:                ;; offset=0x04D3
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG48:                ;; offset=0x04EE
       cmp      ecx, 29
       jb       G_M000_IG10
 
G_M000_IG49:                ;; offset=0x04F7
       cmp      ecx, 47
       jae      G_M000_IG10
 
G_M000_IG50:                ;; offset=0x0500
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jae      G_M000_IG45
 
G_M000_IG51:                ;; offset=0x0525
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG46
       align    [0 bytes for IG52]
 
G_M000_IG52:                ;; offset=0x0535
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG53:                ;; offset=0x053B
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG104
       add      esi, edx
       movzx    rdx, si
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG54:                ;; offset=0x056F
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG55:                ;; offset=0x058A
       cmp      ecx, 11
       jb       G_M000_IG10
 
G_M000_IG56:                ;; offset=0x0593
       cmp      ecx, 18
       jae      G_M000_IG10
 
G_M000_IG57:                ;; offset=0x059C
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jae      G_M000_IG52
 
G_M000_IG58:                ;; offset=0x05C1
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG53
 
G_M000_IG59:                ;; offset=0x05D1
       cmp      ecx, 200
       jb       G_M000_IG76
 
G_M000_IG60:                ;; offset=0x05DD
       cmp      ecx, 321
       jb       G_M000_IG72
 
G_M000_IG61:                ;; offset=0x05E9
       cmp      ecx, 515
       jb       G_M000_IG08
       align    [0 bytes for IG62]
 
G_M000_IG62:                ;; offset=0x05F5
       cmp      ecx, 515
       jb       G_M000_IG10
 
G_M000_IG63:                ;; offset=0x0601
       cmp      ecx, 600
       jae      G_M000_IG10
 
G_M000_IG64:                ;; offset=0x060D
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jb       SHORT G_M000_IG68
 
G_M000_IG65:                ;; offset=0x062E
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG66:                ;; offset=0x0634
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG112
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG67:                ;; offset=0x0678
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
       jmp      G_M000_IG62
 
G_M000_IG68:                ;; offset=0x06A1
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      SHORT G_M000_IG66
       align    [0 bytes for IG69]
 
G_M000_IG69:                ;; offset=0x06AE
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG70:                ;; offset=0x06B4
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG110
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG71:                ;; offset=0x0708
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG72:                ;; offset=0x072C
       cmp      ecx, 200
       jb       G_M000_IG10
 
G_M000_IG73:                ;; offset=0x0738
       cmp      ecx, 321
       jae      G_M000_IG10
 
G_M000_IG74:                ;; offset=0x0744
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jae      G_M000_IG69
 
G_M000_IG75:                ;; offset=0x0769
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG70
 
G_M000_IG76:                ;; offset=0x0779
       cmp      ecx, 76
       jb       G_M000_IG95
 
G_M000_IG77:                ;; offset=0x0782
       cmp      ecx, 123
       jae      G_M000_IG88
       align    [0 bytes for IG78]
 
G_M000_IG78:                ;; offset=0x078B
       cmp      ecx, 76
       jb       G_M000_IG10
 
G_M000_IG79:                ;; offset=0x0794
       cmp      ecx, 123
       jae      G_M000_IG10
 
G_M000_IG80:                ;; offset=0x079D
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jb       G_M000_IG84
 
G_M000_IG81:                ;; offset=0x07C2
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG82:                ;; offset=0x07C8
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG108
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdi], xmm0
       lea      esi, [rcx-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rsi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vaddss   xmm0, xmm0, dword ptr [rdi]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG83:                ;; offset=0x0838
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
       jmp      G_M000_IG78
 
G_M000_IG84:                ;; offset=0x0858
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG82
       align    [0 bytes for IG85]
 
G_M000_IG85:                ;; offset=0x0868
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG86:                ;; offset=0x086E
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG109
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG87:                ;; offset=0x08B2
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG88:                ;; offset=0x08D6
       cmp      ecx, 123
       jb       G_M000_IG10
 
G_M000_IG89:                ;; offset=0x08DF
       cmp      ecx, 200
       jae      G_M000_IG10
 
G_M000_IG90:                ;; offset=0x08EB
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jae      G_M000_IG85
 
G_M000_IG91:                ;; offset=0x0910
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG86
       align    [0 bytes for IG92]
 
G_M000_IG92:                ;; offset=0x0920
       mov      esi, r15d
       sub      esi, r8d
 
G_M000_IG93:                ;; offset=0x0926
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   r8, r9d
       mov      r9d, esi
       cmp      r8, r9
       jl       G_M000_IG107
       add      esi, edx
       movzx    rdx, si
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdi], xmm0
       mov      esi, ebx
       mov      r9d, ecx
       mov      r8d, r15d
       inc      eax
       cmp      eax, r11d
       jae      G_M000_IG10
 
G_M000_IG94:                ;; offset=0x097A
       mov      ecx, eax
       mov      ecx, dword ptr [r10+4*rcx]
       mov      ebx, ecx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      ecx, ebx
 
G_M000_IG95:                ;; offset=0x0995
       cmp      ecx, 47
       jb       G_M000_IG10
 
G_M000_IG96:                ;; offset=0x099E
       cmp      ecx, 76
       jae      G_M000_IG10
 
G_M000_IG97:                ;; offset=0x09A7
       cmp      eax, r11d
       jae      G_M000_IG113
       mov      ebx, eax
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jae      G_M000_IG92
 
G_M000_IG98:                ;; offset=0x09CC
       cmp      ecx, r9d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG93
 
G_M000_IG99:                ;; offset=0x09DC
       mov      eax, 1
       mov      ecx, 5
       cmp      r9d, 599
       cmove    eax, ecx
       mov      ecx, esi
       mov      edx, edx
       shl      rdx, 32
       or       rcx, rdx
       shl      rax, 48
       or       rax, rcx
 
G_M000_IG100:                ;; offset=0x0A02
       add      rsp, 768
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG101:                ;; offset=0x0A0E
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG102:                ;; offset=0x0A65
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG103:                ;; offset=0x0ABC
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG104:                ;; offset=0x0B13
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG105:                ;; offset=0x0B6A
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG106:                ;; offset=0x0BC1
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG107:                ;; offset=0x0C18
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG108:                ;; offset=0x0C6F
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG109:                ;; offset=0x0CC6
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG110:                ;; offset=0x0D1D
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG111:                ;; offset=0x0D74
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG112:                ;; offset=0x0DCB
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG113:                ;; offset=0x0E22
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41000000h		;         8
RWD08  	dd	40A00000h		;         5
RWD12  	dd	40400000h		;         3
RWD16  	dd	40000000h		;         2
RWD20  	dd	41880000h		;        17
RWD24  	dd	41500000h		;        13
RWD28  	dd	42380000h		;        46
RWD32  	dd	41A80000h		;        21
RWD36  	dd	42080000h		;        34

; Total bytes of code 3624

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Backward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; fully interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 416
       lea      rbp, [rsp+0x1A0]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -336
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      bword ptr [rbp-0x30], rdi
       mov      bword ptr [rbp-0x38], rsi
       mov      bword ptr [rbp-0x48], rdx
       mov      qword ptr [rbp-0x40], rcx
 
G_M000_IG02:                ;; offset=0x004B
       mov      dword ptr [rbp-0x190], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0068
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x188], rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x198], rax
       mov      rsi, gword ptr [rbp-0x198]
       mov      rdi, gword ptr [rbp-0x188]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x188]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00BB
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00CE
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x180], rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x1A0], rax
       mov      rsi, gword ptr [rbp-0x1A0]
       mov      rdi, gword ptr [rbp-0x180]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x180]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0121
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7FC927193748
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x0145
       add      rsp, 416
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x014E
       mov      rax, bword ptr [rbp-0x30]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x4C], eax
       mov      rax, bword ptr [rbp-0x30]
       movzx    rax, word  ptr [rax+0x04]
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x4C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x54], edx
       mov      eax, dword ptr [rbp-0x4C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x58], eax
       xor      eax, eax
       mov      dword ptr [rbp-0x5C], eax
       cmp      dword ptr [rbp-0x40], 0
       jbe      G_M000_IG132
       mov      rax, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
       jmp      G_M000_IG124
 
G_M000_IG09:                ;; offset=0x01A6
       cmp      dword ptr [rbp-0x60], 47
       jae      G_M000_IG68
       cmp      dword ptr [rbp-0x60], 11
       jae      G_M000_IG39
       cmp      dword ptr [rbp-0x60], 3
       jae      G_M000_IG19
       jmp      G_M000_IG13
 
G_M000_IG10:                ;; offset=0x01C9
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x170], eax
       mov      eax, dword ptr [rbp-0x170]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x68], eax
       mov      eax, dword ptr [rbp-0x170]
       mov      dword ptr [rbp-0x174], eax
       mov      eax, dword ptr [rbp-0x170]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG11
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x174]
       mov      dword ptr [rbp-0x178], eax
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0225
       mov      rdi, 0x7FC92719374C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x174]
       mov      dword ptr [rbp-0x178], eax
 
G_M000_IG12:                ;; offset=0x024F
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0x6C]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x178]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG18
       mov      rdi, 0x7FC927193750
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG13:                ;; offset=0x02D8
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG17
       cmp      dword ptr [rbp-0x60], 0
       jb       SHORT G_M000_IG16
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x02FD
       lea      rdi, [rbp-0x190]
       mov      esi, 263
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:                ;; offset=0x030E
       cmp      dword ptr [rbp-0x60], 3
       jb       G_M000_IG10
       mov      rdi, 0x7FC927193754
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG16:                ;; offset=0x032C
       mov      rdi, 0x7FC927193758
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG17:                ;; offset=0x0340
       mov      rdi, 0x7FC92719375C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG18:                ;; offset=0x0354
       mov      rdi, 0x7FC927193760
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG19:                ;; offset=0x0368
       cmp      dword ptr [rbp-0x60], 7
       jae      G_M000_IG33
       jmp      G_M000_IG24
 
G_M000_IG20:                ;; offset=0x0377
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x164], eax
       mov      eax, dword ptr [rbp-0x164]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x70], eax
       mov      eax, dword ptr [rbp-0x164]
       mov      dword ptr [rbp-0x168], eax
       mov      eax, dword ptr [rbp-0x164]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG21
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0x74], eax
       mov      eax, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x16C], eax
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x03D3
       mov      rdi, 0x7FC927193764
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x74], eax
       mov      eax, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x16C], eax
 
G_M000_IG22:                ;; offset=0x03FD
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0x74]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x78], xmm0
       mov      dword ptr [rbp-0x7C], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x7C]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x78]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x80], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0x80]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x16C]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG29
       mov      rdi, 0x7FC927193768
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
 
G_M000_IG23:                ;; offset=0x04EC
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG24:                ;; offset=0x04EF
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG28
       cmp      dword ptr [rbp-0x60], 3
       jb       SHORT G_M000_IG27
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x0514
       lea      rdi, [rbp-0x190]
       mov      esi, 464
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG26:                ;; offset=0x0525
       cmp      dword ptr [rbp-0x60], 7
       jb       G_M000_IG20
       mov      rdi, 0x7FC92719376C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG27:                ;; offset=0x0543
       mov      rdi, 0x7FC927193770
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG28:                ;; offset=0x0557
       mov      rdi, 0x7FC927193774
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG29:                ;; offset=0x056B
       mov      rdi, 0x7FC927193778
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG24
 
G_M000_IG30:                ;; offset=0x057F
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x158], eax
       mov      eax, dword ptr [rbp-0x158]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x84], eax
       mov      eax, dword ptr [rbp-0x158]
       mov      dword ptr [rbp-0x15C], eax
       mov      eax, dword ptr [rbp-0x158]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG31
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0x84]
       mov      dword ptr [rbp-0x88], eax
       mov      eax, dword ptr [rbp-0x15C]
       mov      dword ptr [rbp-0x160], eax
       jmp      SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x05E4
       mov      rdi, 0x7FC92719377C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x88], eax
       mov      eax, dword ptr [rbp-0x15C]
       mov      dword ptr [rbp-0x160], eax
 
G_M000_IG32:                ;; offset=0x0611
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0x88]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x84]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG38
       mov      rdi, 0x7FC927193780
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG33:                ;; offset=0x06B8
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG37
       cmp      dword ptr [rbp-0x60], 7
       jb       SHORT G_M000_IG36
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x06DD
       lea      rdi, [rbp-0x190]
       mov      esi, 611
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG35:                ;; offset=0x06EE
       cmp      dword ptr [rbp-0x60], 11
       jb       G_M000_IG30
       mov      rdi, 0x7FC927193784
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG36:                ;; offset=0x070C
       mov      rdi, 0x7FC927193788
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG37:                ;; offset=0x0720
       mov      rdi, 0x7FC92719378C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG38:                ;; offset=0x0734
       mov      rdi, 0x7FC927193790
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG33
 
G_M000_IG39:                ;; offset=0x0748
       cmp      dword ptr [rbp-0x60], 18
       jae      G_M000_IG49
       jmp      G_M000_IG43
 
G_M000_IG40:                ;; offset=0x0757
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x14C], eax
       mov      eax, dword ptr [rbp-0x14C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x8C], eax
       mov      eax, dword ptr [rbp-0x14C]
       mov      dword ptr [rbp-0x150], eax
       mov      eax, dword ptr [rbp-0x14C]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG41
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0x8C]
       mov      dword ptr [rbp-0x90], eax
       mov      eax, dword ptr [rbp-0x150]
       mov      dword ptr [rbp-0x154], eax
       jmp      SHORT G_M000_IG42
 
G_M000_IG41:                ;; offset=0x07BC
       mov      rdi, 0x7FC927193794
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      eax, dword ptr [rbp-0x150]
       mov      dword ptr [rbp-0x154], eax
 
G_M000_IG42:                ;; offset=0x07E9
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0x90]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x154]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x8C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG48
       mov      rdi, 0x7FC927193798
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG43:                ;; offset=0x0860
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG47
       cmp      dword ptr [rbp-0x60], 11
       jb       SHORT G_M000_IG46
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG45
 
G_M000_IG44:                ;; offset=0x0885
       lea      rdi, [rbp-0x190]
       mov      esi, 751
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG45:                ;; offset=0x0896
       cmp      dword ptr [rbp-0x60], 18
       jb       G_M000_IG40
       mov      rdi, 0x7FC92719379C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG46:                ;; offset=0x08B4
       mov      rdi, 0x7FC9271937A0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG47:                ;; offset=0x08C8
       mov      rdi, 0x7FC9271937A4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG48:                ;; offset=0x08DC
       mov      rdi, 0x7FC9271937A8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG43
 
G_M000_IG49:                ;; offset=0x08F0
       cmp      dword ptr [rbp-0x60], 29
       jae      G_M000_IG62
       jmp      G_M000_IG53
 
G_M000_IG50:                ;; offset=0x08FF
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x140], eax
       mov      eax, dword ptr [rbp-0x140]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x140]
       mov      dword ptr [rbp-0x144], eax
       mov      eax, dword ptr [rbp-0x140]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG51
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0x94]
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x144]
       mov      dword ptr [rbp-0x148], eax
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0964
       mov      rdi, 0x7FC9271937AC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x144]
       mov      dword ptr [rbp-0x148], eax
 
G_M000_IG52:                ;; offset=0x0991
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0x98]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x148]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x94]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG58
       mov      rdi, 0x7FC9271937B0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG53:                ;; offset=0x0A38
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG57
       cmp      dword ptr [rbp-0x60], 18
       jb       SHORT G_M000_IG56
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG55
 
G_M000_IG54:                ;; offset=0x0A5D
       lea      rdi, [rbp-0x190]
       mov      esi, 908
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG55:                ;; offset=0x0A6E
       cmp      dword ptr [rbp-0x60], 29
       jb       G_M000_IG50
       mov      rdi, 0x7FC9271937B4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG56:                ;; offset=0x0A8C
       mov      rdi, 0x7FC9271937B8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG57:                ;; offset=0x0AA0
       mov      rdi, 0x7FC9271937BC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG58:                ;; offset=0x0AB4
       mov      rdi, 0x7FC9271937C0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG53
 
G_M000_IG59:                ;; offset=0x0AC8
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x134], eax
       mov      eax, dword ptr [rbp-0x134]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x134]
       mov      dword ptr [rbp-0x138], eax
       mov      eax, dword ptr [rbp-0x134]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG60
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0x9C]
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x138]
       mov      dword ptr [rbp-0x13C], eax
       jmp      SHORT G_M000_IG61
 
G_M000_IG60:                ;; offset=0x0B2D
       mov      rdi, 0x7FC9271937C4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x138]
       mov      dword ptr [rbp-0x13C], eax
 
G_M000_IG61:                ;; offset=0x0B5A
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0xA0]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0xA4], xmm0
       mov      dword ptr [rbp-0xA8], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0xA8]
       vmulss   xmm0, xmm0, dword ptr [rbp-0xA4]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0xAC], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0xAC]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x13C]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x9C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG67
       mov      rdi, 0x7FC9271937C8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG62:                ;; offset=0x0C34
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG66
       cmp      dword ptr [rbp-0x60], 29
       jb       SHORT G_M000_IG65
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:                ;; offset=0x0C59
       lea      rdi, [rbp-0x190]
       mov      esi, 0x437
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:                ;; offset=0x0C6A
       cmp      dword ptr [rbp-0x60], 47
       jb       G_M000_IG59
       mov      rdi, 0x7FC9271937CC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG65:                ;; offset=0x0C88
       mov      rdi, 0x7FC9271937D0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG66:                ;; offset=0x0C9C
       mov      rdi, 0x7FC9271937D4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG67:                ;; offset=0x0CB0
       mov      rdi, 0x7FC9271937D8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG62
 
G_M000_IG68:                ;; offset=0x0CC4
       cmp      dword ptr [rbp-0x60], 200
       jae      G_M000_IG97
       cmp      dword ptr [rbp-0x60], 76
       jae      G_M000_IG78
       jmp      G_M000_IG72
 
G_M000_IG69:                ;; offset=0x0CE0
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x128], eax
       mov      eax, dword ptr [rbp-0x128]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xB0], eax
       mov      eax, dword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x12C], eax
       mov      eax, dword ptr [rbp-0x128]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG70
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x12C]
       mov      dword ptr [rbp-0x130], eax
       jmp      SHORT G_M000_IG71
 
G_M000_IG70:                ;; offset=0x0D45
       mov      rdi, 0x7FC9271937DC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x12C]
       mov      dword ptr [rbp-0x130], eax
 
G_M000_IG71:                ;; offset=0x0D72
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0xB4]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD24]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x130]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG77
       mov      rdi, 0x7FC9271937E0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG72:                ;; offset=0x0E19
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG76
       cmp      dword ptr [rbp-0x60], 47
       jb       SHORT G_M000_IG75
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG74
 
G_M000_IG73:                ;; offset=0x0E3E
       lea      rdi, [rbp-0x190]
       mov      esi, 0x4E3
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG74:                ;; offset=0x0E4F
       cmp      dword ptr [rbp-0x60], 76
       jb       G_M000_IG69
       mov      rdi, 0x7FC9271937E4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG75:                ;; offset=0x0E6D
       mov      rdi, 0x7FC9271937E8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG76:                ;; offset=0x0E81
       mov      rdi, 0x7FC9271937EC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG77:                ;; offset=0x0E95
       mov      rdi, 0x7FC9271937F0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG72
 
G_M000_IG78:                ;; offset=0x0EA9
       cmp      dword ptr [rbp-0x60], 123
       jae      G_M000_IG91
       jmp      G_M000_IG82
 
G_M000_IG79:                ;; offset=0x0EB8
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x11C], eax
       mov      eax, dword ptr [rbp-0x11C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0x11C]
       mov      dword ptr [rbp-0x120], eax
       mov      eax, dword ptr [rbp-0x11C]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG80
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
       mov      eax, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x124], eax
       jmp      SHORT G_M000_IG81
 
G_M000_IG80:                ;; offset=0x0F1D
       mov      rdi, 0x7FC9271937F4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       mov      eax, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x124], eax
 
G_M000_IG81:                ;; offset=0x0F4A
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0xBC]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0xC0], xmm0
       mov      dword ptr [rbp-0xC4], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0xC4]
       vmulss   xmm0, xmm0, dword ptr [rbp-0xC0]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0xC8], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0xC8]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x124]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG87
       mov      rdi, 0x7FC9271937F8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG82:                ;; offset=0x103C
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG86
       cmp      dword ptr [rbp-0x60], 76
       jb       SHORT G_M000_IG85
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG84
 
G_M000_IG83:                ;; offset=0x1061
       lea      rdi, [rbp-0x190]
       mov      esi, 0x5A6
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG84:                ;; offset=0x1072
       cmp      dword ptr [rbp-0x60], 123
       jb       G_M000_IG79
       mov      rdi, 0x7FC9271937FC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG85:                ;; offset=0x1090
       mov      rdi, 0x7FC927193800
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG86:                ;; offset=0x10A4
       mov      rdi, 0x7FC927193804
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG87:                ;; offset=0x10B8
       mov      rdi, 0x7FC927193808
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG82
 
G_M000_IG88:                ;; offset=0x10CC
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x110], eax
       mov      eax, dword ptr [rbp-0x110]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xCC], eax
       mov      eax, dword ptr [rbp-0x110]
       mov      dword ptr [rbp-0x114], eax
       mov      eax, dword ptr [rbp-0x110]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG89
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0x114]
       mov      dword ptr [rbp-0x118], eax
       jmp      SHORT G_M000_IG90
 
G_M000_IG89:                ;; offset=0x1131
       mov      rdi, 0x7FC92719380C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0x114]
       mov      dword ptr [rbp-0x118], eax
 
G_M000_IG90:                ;; offset=0x115E
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0xD0]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x118]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG96
       mov      rdi, 0x7FC927193810
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG91:                ;; offset=0x11ED
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG95
       cmp      dword ptr [rbp-0x60], 123
       jb       SHORT G_M000_IG94
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG93
 
G_M000_IG92:                ;; offset=0x1212
       lea      rdi, [rbp-0x190]
       mov      esi, 0x631
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG93:                ;; offset=0x1223
       cmp      dword ptr [rbp-0x60], 200
       jb       G_M000_IG88
       mov      rdi, 0x7FC927193814
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG94:                ;; offset=0x1244
       mov      rdi, 0x7FC927193818
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG95:                ;; offset=0x1258
       mov      rdi, 0x7FC92719381C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG96:                ;; offset=0x126C
       mov      rdi, 0x7FC927193820
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG91
 
G_M000_IG97:                ;; offset=0x1280
       cmp      dword ptr [rbp-0x60], 321
       jae      G_M000_IG107
       jmp      G_M000_IG101
 
G_M000_IG98:                ;; offset=0x1292
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x104], eax
       mov      eax, dword ptr [rbp-0x104]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xD4], eax
       mov      eax, dword ptr [rbp-0x104]
       mov      dword ptr [rbp-0x108], eax
       mov      eax, dword ptr [rbp-0x104]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG99
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0xD8], eax
       mov      eax, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x10C], eax
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:                ;; offset=0x12F7
       mov      rdi, 0x7FC927193824
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0xD8], eax
       mov      eax, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x10C], eax
 
G_M000_IG100:                ;; offset=0x1324
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0xD8]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x10C]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG106
       mov      rdi, 0x7FC927193828
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG101:                ;; offset=0x13CB
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG105
       cmp      dword ptr [rbp-0x60], 200
       jb       SHORT G_M000_IG104
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG103
 
G_M000_IG102:                ;; offset=0x13F3
       lea      rdi, [rbp-0x190]
       mov      esi, 0x6DA
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG103:                ;; offset=0x1404
       cmp      dword ptr [rbp-0x60], 321
       jb       G_M000_IG98
       mov      rdi, 0x7FC92719382C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG104:                ;; offset=0x1425
       mov      rdi, 0x7FC927193830
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG105:                ;; offset=0x1439
       mov      rdi, 0x7FC927193834
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG106:                ;; offset=0x144D
       mov      rdi, 0x7FC927193838
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG101
 
G_M000_IG107:                ;; offset=0x1461
       cmp      dword ptr [rbp-0x60], 515
       jae      G_M000_IG120
       jmp      G_M000_IG111
 
G_M000_IG108:                ;; offset=0x1473
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xF8], eax
       mov      eax, dword ptr [rbp-0xF8]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xDC], eax
       mov      eax, dword ptr [rbp-0xF8]
       mov      dword ptr [rbp-0xFC], eax
       mov      eax, dword ptr [rbp-0xF8]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG109
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0xE0], eax
       mov      eax, dword ptr [rbp-0xFC]
       mov      dword ptr [rbp-0x100], eax
       jmp      SHORT G_M000_IG110
 
G_M000_IG109:                ;; offset=0x14D8
       mov      rdi, 0x7FC92719383C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0xE0], eax
       mov      eax, dword ptr [rbp-0xFC]
       mov      dword ptr [rbp-0x100], eax
 
G_M000_IG110:                ;; offset=0x1505
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0xE0]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x100]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG116
       mov      rdi, 0x7FC927193840
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG111:                ;; offset=0x15C4
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG115
       cmp      dword ptr [rbp-0x60], 321
       jb       SHORT G_M000_IG114
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG113
 
G_M000_IG112:                ;; offset=0x15EC
       lea      rdi, [rbp-0x190]
       mov      esi, 0x78D
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG113:                ;; offset=0x15FD
       cmp      dword ptr [rbp-0x60], 515
       jb       G_M000_IG108
       mov      rdi, 0x7FC927193844
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG114:                ;; offset=0x161E
       mov      rdi, 0x7FC927193848
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG115:                ;; offset=0x1632
       mov      rdi, 0x7FC92719384C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG116:                ;; offset=0x1646
       mov      rdi, 0x7FC927193850
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG111
 
G_M000_IG117:                ;; offset=0x165A
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xEC], eax
       mov      eax, dword ptr [rbp-0xEC]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xE4], eax
       mov      eax, dword ptr [rbp-0xEC]
       mov      dword ptr [rbp-0xF0], eax
       mov      eax, dword ptr [rbp-0xEC]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG118
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0xE8], eax
       mov      eax, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0xF4], eax
       jmp      SHORT G_M000_IG119
 
G_M000_IG118:                ;; offset=0x16BF
       mov      rdi, 0x7FC927193854
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0xE8], eax
       mov      eax, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0xF4], eax
 
G_M000_IG119:                ;; offset=0x16EC
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0xE8]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xF4]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG131
       mov      rdi, 0x7FC927193858
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG132
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG120:                ;; offset=0x177B
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG130
       cmp      dword ptr [rbp-0x60], 515
       jb       G_M000_IG129
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG122
 
G_M000_IG121:                ;; offset=0x17AB
       lea      rdi, [rbp-0x190]
       mov      esi, 0x818
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG122:                ;; offset=0x17BC
       cmp      dword ptr [rbp-0x60], 600
       jb       G_M000_IG117
 
G_M000_IG123:                ;; offset=0x17C9
       mov      rdi, 0x7FC92719385C
       call     CORINFO_HELP_COUNTPROFILE32
 
G_M000_IG124:                ;; offset=0x17D8
       mov      eax, dword ptr [rbp-0x190]
       dec      eax
       mov      dword ptr [rbp-0x190], eax
       cmp      dword ptr [rbp-0x190], 0
       jg       SHORT G_M000_IG126
 
G_M000_IG125:                ;; offset=0x17EF
       lea      rdi, [rbp-0x190]
       mov      esi, 0x824
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG126:                ;; offset=0x1800
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jb       G_M000_IG09
       mov      dword ptr [rbp-0x64], 1
       cmp      dword ptr [rbp-0x54], 599
       jne      SHORT G_M000_IG127
       mov      rdi, 0x7FC927193860
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x64]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x64], eax
 
G_M000_IG127:                ;; offset=0x1837
       mov      rdi, 0x7FC927193864
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x64]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG128:                ;; offset=0x1856
       add      rsp, 416
       pop      rbp
       ret      
 
G_M000_IG129:                ;; offset=0x185F
       mov      rdi, 0x7FC927193868
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG130:                ;; offset=0x1873
       mov      rdi, 0x7FC92719386C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG124
 
G_M000_IG131:                ;; offset=0x1887
       mov      rdi, 0x7FC927193870
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG120
 
G_M000_IG132:                ;; offset=0x189B
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	40400000h		;         3
RWD08  	dd	40000000h		;         2
RWD12  	dd	40A00000h		;         5
RWD16  	dd	41000000h		;         8
RWD20  	dd	41880000h		;        17
RWD24  	dd	41500000h		;        13
RWD28  	dd	42380000h		;        46
RWD32  	dd	42080000h		;        34

; Total bytes of code 6305

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Backward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x78d
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 7
; 12 inlinees with PGO data; 4 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 16
       mov      qword ptr [rsp+0x1B8], r15
       mov      qword ptr [rsp+0x1B0], rbx
       lea      rbp, [rsp+0x10]
       mov      rax, bword ptr [rbp+0x178]
       mov      esi, dword ptr [rbp+0x164]
       mov      edi, dword ptr [rbp+0x160]
       mov      r8d, dword ptr [rbp+0x15C]
       mov      r9d, dword ptr [rbp+0x158]
       mov      ecx, dword ptr [rbp+0x154]
       mov      edx, dword ptr [rbp+0x150]
 
G_M000_IG02:                ;; offset=0x004B
       mov      r10, bword ptr [rbp+0x168]
       mov      r11d, dword ptr [rbp+0x170]
 
G_M000_IG03:                ;; offset=0x0059
       cmp      edx, 515
       jae      G_M000_IG12
 
G_M000_IG04:                ;; offset=0x0065
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG89
 
G_M000_IG05:                ;; offset=0x008A
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG06:                ;; offset=0x0095
       cmp      edi, esi
       ja       G_M000_IG90
 
G_M000_IG07:                ;; offset=0x009D
       mov      r8d, edi
 
G_M000_IG08:                ;; offset=0x00A0
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00E5
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG10:                ;; offset=0x0109
       cmp      ecx, r11d
       jae      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x010E
       cmp      edx, 321
       jae      G_M000_IG03
 
G_M000_IG12:                ;; offset=0x011A
       cmp      ecx, r11d
       jae      G_M000_IG105
 
G_M000_IG13:                ;; offset=0x0123
       cmp      edx, 47
       jb       G_M000_IG49
 
G_M000_IG14:                ;; offset=0x012C
       cmp      edx, 200
       jb       G_M000_IG29
 
G_M000_IG15:                ;; offset=0x0138
       cmp      edx, 321
       jb       G_M000_IG25
 
G_M000_IG16:                ;; offset=0x0144
       cmp      edx, 515
       jae      SHORT G_M000_IG19
       jmp      SHORT G_M000_IG10
       align    [0 bytes for IG17]
 
G_M000_IG17:                ;; offset=0x014E
       mov      r8d, edi
 
G_M000_IG18:                ;; offset=0x0151
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      SHORT G_M000_IG12
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG19:                ;; offset=0x019A
       cmp      edx, 515
       jb       G_M000_IG12
 
G_M000_IG20:                ;; offset=0x01A6
       cmp      edx, 600
       jae      G_M000_IG12
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG104
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG21:                ;; offset=0x01E2
       cmp      edi, esi
       jbe      G_M000_IG17
 
G_M000_IG22:                ;; offset=0x01EA
       mov      r8d, esi
       jmp      G_M000_IG18
       align    [0 bytes for IG23]
 
G_M000_IG23:                ;; offset=0x01F2
       mov      r8d, edi
 
G_M000_IG24:                ;; offset=0x01F5
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG25:                ;; offset=0x0252
       cmp      edx, 200
       jb       G_M000_IG12
       cmp      edx, 321
       jae      G_M000_IG12
 
G_M000_IG26:                ;; offset=0x026A
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG103
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG27:                ;; offset=0x029A
       cmp      edi, esi
       jbe      G_M000_IG23
 
G_M000_IG28:                ;; offset=0x02A2
       mov      r8d, esi
       jmp      G_M000_IG24
 
G_M000_IG29:                ;; offset=0x02AA
       cmp      edx, 76
       jb       G_M000_IG45
 
G_M000_IG30:                ;; offset=0x02B3
       cmp      edx, 123
       jae      SHORT G_M000_IG33
       jmp      G_M000_IG39
       align    [0 bytes for IG31]
 
G_M000_IG31:                ;; offset=0x02BD
       mov      r8d, edi
 
G_M000_IG32:                ;; offset=0x02C0
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG33:                ;; offset=0x030D
       cmp      edx, 123
       jb       G_M000_IG12
       cmp      edx, 200
       jae      G_M000_IG12
 
G_M000_IG34:                ;; offset=0x0322
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG102
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG35:                ;; offset=0x0352
       cmp      edi, esi
       jbe      G_M000_IG31
 
G_M000_IG36:                ;; offset=0x035A
       mov      r8d, esi
       jmp      G_M000_IG32
       align    [0 bytes for IG37]
 
G_M000_IG37:                ;; offset=0x0362
       mov      r8d, edi
 
G_M000_IG38:                ;; offset=0x0365
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       lea      r9d, [rdx-0x4C]
       mov      esi, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rsi
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG39:                ;; offset=0x03DD
       cmp      edx, 76
       jb       G_M000_IG12
       cmp      edx, 123
       jae      G_M000_IG12
 
G_M000_IG40:                ;; offset=0x03EF
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG101
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG41:                ;; offset=0x041F
       cmp      edi, esi
       jbe      G_M000_IG37
 
G_M000_IG42:                ;; offset=0x0427
       mov      r8d, esi
       jmp      G_M000_IG38
       align    [0 bytes for IG43]
 
G_M000_IG43:                ;; offset=0x042F
       mov      r8d, edi
 
G_M000_IG44:                ;; offset=0x0432
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG45:                ;; offset=0x0486
       cmp      edx, 47
       jb       G_M000_IG12
       cmp      edx, 76
       jae      G_M000_IG12
 
G_M000_IG46:                ;; offset=0x0498
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG100
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG47:                ;; offset=0x04C8
       cmp      edi, esi
       jbe      G_M000_IG43
 
G_M000_IG48:                ;; offset=0x04D0
       mov      r8d, esi
       jmp      G_M000_IG44
 
G_M000_IG49:                ;; offset=0x04D8
       cmp      edx, 11
       jae      G_M000_IG69
 
G_M000_IG50:                ;; offset=0x04E1
       cmp      edx, 3
       jb       G_M000_IG65
 
G_M000_IG51:                ;; offset=0x04EA
       cmp      edx, 7
       jae      G_M000_IG60
 
G_M000_IG52:                ;; offset=0x04F3
       cmp      edx, 3
       jb       G_M000_IG12
       cmp      edx, 7
       jae      G_M000_IG12
 
G_M000_IG53:                ;; offset=0x0505
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG93
 
G_M000_IG54:                ;; offset=0x052A
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG55:                ;; offset=0x0535
       cmp      edi, esi
       ja       G_M000_IG94
 
G_M000_IG56:                ;; offset=0x053D
       mov      r8d, edi
 
G_M000_IG57:                ;; offset=0x0540
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       lea      r9d, [rdx-0x03]
       mov      esi, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rsi
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
       jmp      G_M000_IG52
 
G_M000_IG58:                ;; offset=0x05C5
       mov      r8d, edi
 
G_M000_IG59:                ;; offset=0x05C8
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG60:                ;; offset=0x061C
       cmp      edx, 7
       jb       G_M000_IG12
       cmp      edx, 11
       jae      G_M000_IG12
 
G_M000_IG61:                ;; offset=0x062E
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG95
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG62:                ;; offset=0x065E
       cmp      edi, esi
       jbe      G_M000_IG58
       jmp      G_M000_IG96
 
G_M000_IG63:                ;; offset=0x066B
       mov      r8d, edi
 
G_M000_IG64:                ;; offset=0x066E
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG65:                ;; offset=0x06B2
       cmp      edx, 3
       jae      G_M000_IG12
 
G_M000_IG66:                ;; offset=0x06BB
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG91
 
G_M000_IG67:                ;; offset=0x06E0
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG68:                ;; offset=0x06EB
       cmp      edi, esi
       jbe      G_M000_IG63
       jmp      G_M000_IG92
 
G_M000_IG69:                ;; offset=0x06F8
       cmp      edx, 18
       jb       G_M000_IG85
 
G_M000_IG70:                ;; offset=0x0701
       cmp      edx, 29
       jb       G_M000_IG79
 
G_M000_IG71:                ;; offset=0x070A
       cmp      edx, 29
       jb       G_M000_IG12
       cmp      edx, 47
       jae      G_M000_IG12
 
G_M000_IG72:                ;; offset=0x071C
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG99
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG73:                ;; offset=0x074C
       cmp      edi, esi
       ja       SHORT G_M000_IG76
 
G_M000_IG74:                ;; offset=0x0750
       mov      r8d, edi
 
G_M000_IG75:                ;; offset=0x0753
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       lea      r9d, [rdx-0x1D]
       mov      esi, r9d
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rsi
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
       jmp      G_M000_IG71
 
G_M000_IG76:                ;; offset=0x07C0
       mov      r8d, esi
       jmp      SHORT G_M000_IG75
 
G_M000_IG77:                ;; offset=0x07C5
       mov      r8d, edi
 
G_M000_IG78:                ;; offset=0x07C8
       sub      edi, r8d
       movzx    rdi, di
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rax], xmm0
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG79:                ;; offset=0x081C
       cmp      edx, 18
       jb       G_M000_IG12
       cmp      edx, 29
       jae      G_M000_IG12
 
G_M000_IG80:                ;; offset=0x082E
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      G_M000_IG98
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG81:                ;; offset=0x085E
       cmp      edi, esi
       jbe      G_M000_IG77
 
G_M000_IG82:                ;; offset=0x0866
       mov      r8d, esi
       jmp      G_M000_IG78
 
G_M000_IG83:                ;; offset=0x086E
       mov      r8d, edi
 
G_M000_IG84:                ;; offset=0x0871
       sub      edi, r8d
       movzx    rdi, di
       mov      esi, ebx
       mov      r8d, edx
       mov      r9d, r15d
       inc      ecx
       cmp      ecx, r11d
       jae      G_M000_IG12
       mov      edx, ecx
       mov      edx, dword ptr [r10+4*rdx]
       mov      ebx, edx
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       sub      edx, ebx
 
G_M000_IG85:                ;; offset=0x08A5
       cmp      edx, 11
       jb       G_M000_IG12
       cmp      edx, 18
       jae      G_M000_IG12
 
G_M000_IG86:                ;; offset=0x08B7
       cmp      ecx, r11d
       jae      G_M000_IG107
       mov      ebx, ecx
       mov      ebx, dword ptr [r10+4*rbx]
       mov      r15d, ebx
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      ebx, esi
       jbe      SHORT G_M000_IG97
       cmp      edx, r8d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG87:                ;; offset=0x08E3
       cmp      edi, esi
       jbe      SHORT G_M000_IG83
 
G_M000_IG88:                ;; offset=0x08E7
       mov      r8d, esi
       jmp      SHORT G_M000_IG84
 
G_M000_IG89:                ;; offset=0x08EC
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG06
 
G_M000_IG90:                ;; offset=0x08F7
       mov      r8d, esi
       jmp      G_M000_IG08
 
G_M000_IG91:                ;; offset=0x08FF
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG68
 
G_M000_IG92:                ;; offset=0x090A
       mov      r8d, esi
       jmp      G_M000_IG64
 
G_M000_IG93:                ;; offset=0x0912
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG55
 
G_M000_IG94:                ;; offset=0x091D
       mov      r8d, esi
       jmp      G_M000_IG57
 
G_M000_IG95:                ;; offset=0x0925
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG62
 
G_M000_IG96:                ;; offset=0x0930
       mov      r8d, esi
       jmp      G_M000_IG59
 
G_M000_IG97:                ;; offset=0x0938
       mov      esi, r9d
       sub      esi, r15d
       jmp      SHORT G_M000_IG87
 
G_M000_IG98:                ;; offset=0x0940
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG81
 
G_M000_IG99:                ;; offset=0x094B
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG73
 
G_M000_IG100:                ;; offset=0x0956
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG47
 
G_M000_IG101:                ;; offset=0x0961
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG41
 
G_M000_IG102:                ;; offset=0x096C
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG35
 
G_M000_IG103:                ;; offset=0x0977
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG27
 
G_M000_IG104:                ;; offset=0x0982
       mov      esi, r9d
       sub      esi, r15d
       jmp      G_M000_IG21
 
G_M000_IG105:                ;; offset=0x098D
       mov      eax, 1
       mov      ecx, 5
       cmp      r8d, 599
       cmove    eax, ecx
       mov      ecx, esi
       mov      edx, edi
       shl      rdx, 32
       or       rcx, rdx
       shl      rax, 48
       or       rax, rcx
 
G_M000_IG106:                ;; offset=0x09B3
       add      rsp, 432
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG107:                ;; offset=0x09BF
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

; Total bytes of code 2501

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Stop(byref):Tl.Playback (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
       mov      qword ptr [rbp-0x18], rax
       mov      bword ptr [rbp-0x08], rdi
 
G_M000_IG02:                ;; offset=0x0018
       mov      rdi, bword ptr [rbp-0x08]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x002B
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x10], rax
       mov      edi, 743
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x18], rax
       mov      rsi, gword ptr [rbp-0x18]
       mov      rdi, gword ptr [rbp-0x10]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x10]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x006F
       mov      rax, bword ptr [rbp-0x08]
       movzx    rax, word  ptr [rax+0x06]
       mov      edx, eax
       or       edx, 2
       mov      rax, bword ptr [rbp-0x08]
       mov      edi, dword ptr [rax]
       mov      rax, bword ptr [rbp-0x08]
       movzx    rsi, word  ptr [rax+0x04]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG05:                ;; offset=0x0091
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 151

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedSingle():Tl.FusionExperiment.SumReceipt:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 112
       lea      rbp, [rsp+0x70]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:                ;; offset=0x0018
       mov      dword ptr [rbp-0x68], 0x3E8
       xor      eax, eax
       mov      dword ptr [rbp-0x34], eax
       xor      edi, edi
       call     [Tl.FusionExperiment.FusedPulse:Start(uint):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      rax, gword ptr [rax+0x08]
       mov      gword ptr [rbp-0x48], rax
       xor      eax, eax
       mov      dword ptr [rbp-0x4C], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0043
       mov      rdi, 0x7FC9271F3F30
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x48]
       mov      ecx, dword ptr [rbp-0x4C]
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG08
       mov      edx, ecx
       lea      rax, bword ptr [rax+4*rdx+0x10]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x50], eax
       lea      rsi, [rbp-0x34]
       lea      rdi, [rbp-0x40]
       mov      edx, dword ptr [rbp-0x50]
       call     [Tl.FusionExperiment.FusedPulse:Forward(byref,byref,uint):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       mov      eax, dword ptr [rbp-0x4C]
       inc      eax
       mov      dword ptr [rbp-0x4C], eax
 
G_M000_IG04:                ;; offset=0x008B
       mov      eax, dword ptr [rbp-0x68]
       dec      eax
       mov      dword ptr [rbp-0x68], eax
       cmp      dword ptr [rbp-0x68], 0
       jg       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0099
       lea      rdi, [rbp-0x68]
       mov      esi, 45
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG06:                ;; offset=0x00A7
       mov      rax, gword ptr [rbp-0x48]
       mov      eax, dword ptr [rax+0x08]
       cmp      eax, dword ptr [rbp-0x4C]
       jg       SHORT G_M000_IG03
       mov      rdi, 0x7FC9271F3F34
       call     CORINFO_HELP_COUNTPROFILE32
       lea      rdi, [rbp-0x40]
       vmovss   xmm0, dword ptr [rbp-0x34]
       call     [Tl.FusionExperiment.SumReceipt:Capture(byref,float):Tl.FusionExperiment.SumReceipt]
       mov      qword ptr [rbp-0x60], rax
       mov      dword ptr [rbp-0x58], edx
       mov      rax, qword ptr [rbp-0x60]
       mov      edx, dword ptr [rbp-0x58]
 
G_M000_IG07:                ;; offset=0x00DF
       add      rsp, 112
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x00E5
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 235

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Start(uint):Tl.Playback (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       mov      dword ptr [rbp-0x04], edi
 
G_M000_IG02:                ;; offset=0x000D
       mov      edi, dword ptr [rbp-0x04]
       xor      esi, esi
       mov      edx, 1
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG03:                ;; offset=0x001E
       add      rsp, 16
       pop      rbp
       ret      
 
; Total bytes of code 36

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedSingle():Tl.FusionExperiment.SumReceipt:this (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x2d
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 7 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0x98], r15
       mov      qword ptr [rsp+0x90], r14
       mov      qword ptr [rsp+0x88], rbx
       lea      rbp, [rsp+0x20]
       vmovss   xmm0, dword ptr [rbp+0x4C]
       mov      rdi, gword ptr [rbp+0x38]
       mov      eax, dword ptr [rbp+0x34]
 
G_M000_IG02:                ;; offset=0x0032
       mov      ebx, dword ptr [rbp+0x40]
       movzx    r15, word  ptr [rbp+0x44]
       movzx    r14, word  ptr [rbp+0x46]
       mov      ecx, dword ptr [rdi+0x08]
       mov      eax, eax
       cmp      ecx, eax
       jg       G_M000_IG25
 
G_M000_IG03:                ;; offset=0x004C
       vmovd    edx, xmm0
       mov      eax, r15d
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       mov      ecx, r14d
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0066
       add      rsp, 136
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0074
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0074
       cmp      r9d, r8d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG27
 
G_M000_IG07:                ;; offset=0x0084
       add      r9d, -29
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG24
 
G_M000_IG08:                ;; offset=0x00B2
       cmp      r9d, 200
       jae      SHORT G_M000_IG14
 
G_M000_IG09:                ;; offset=0x00BB
       cmp      r9d, 76
       jae      SHORT G_M000_IG12
 
G_M000_IG10:                ;; offset=0x00C1
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
 
G_M000_IG11:                ;; offset=0x00C9
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      G_M000_IG24
 
G_M000_IG12:                ;; offset=0x00D6
       cmp      r9d, 123
       jae      SHORT G_M000_IG11
 
G_M000_IG13:                ;; offset=0x00DC
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       add      r9d, -76
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG24
 
G_M000_IG14:                ;; offset=0x0112
       cmp      r9d, 515
       jae      G_M000_IG34
 
G_M000_IG15:                ;; offset=0x011F
       cmp      r9d, 321
       jae      G_M000_IG33
 
G_M000_IG16:                ;; offset=0x012C
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      SHORT G_M000_IG23
 
G_M000_IG17:                ;; offset=0x0136
       cmp      r9d, 7
       jae      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x013C
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       add      r9d, -3
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, xmm1
       jmp      SHORT G_M000_IG23
 
G_M000_IG19:                ;; offset=0x0167
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x0171
       cmp      r9d, 18
       jb       SHORT G_M000_IG24
 
G_M000_IG21:                ;; offset=0x0177
       cmp      r9d, 29
       jae      G_M000_IG07
 
G_M000_IG22:                ;; offset=0x0181
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
 
G_M000_IG23:                ;; offset=0x0189
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
 
G_M000_IG24:                ;; offset=0x0191
       mov      edx, edx
       mov      esi, esi
       shl      rsi, 32
       or       rdx, rsi
       mov      esi, r8d
       shl      rsi, 48
       or       rdx, rsi
       mov      qword ptr [rbp-0x20], rdx
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       inc      eax
       cmp      ecx, eax
       jle      G_M000_IG03
 
G_M000_IG25:                ;; offset=0x01C1
       cmp      eax, ecx
       jae      G_M000_IG39
       mov      edx, dword ptr [rdi+4*rax+0x10]
       test     r14b, 1
       je       G_M000_IG36
       test     r14b, 2
       jne      G_M000_IG37
       mov      esi, ebx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     esi, esi, 600
       mov      r8d, ebx
       sub      r8d, esi
       mov      esi, edx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     esi, esi, 600
       mov      r9d, edx
       sub      r9d, esi
       cmp      edx, ebx
       jb       G_M000_IG06
 
G_M000_IG26:                ;; offset=0x021B
       mov      r8d, edx
       imul     rsi, r8, 0x1B4E81B5
       shr      rsi, 38
       mov      r8d, ebx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       sub      esi, r8d
 
G_M000_IG27:                ;; offset=0x023A
       mov      r8d, r15d
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   r8, r8d
       mov      r10d, esi
       cmp      r8, r10
       jl       G_M000_IG38
       add      esi, r15d
       movzx    rsi, si
       mov      r8d, 1
       cmp      r9d, 599
       jne      SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x026B
       mov      r8d, 5
 
G_M000_IG29:                ;; offset=0x0271
       cmp      r9d, 47
       jae      G_M000_IG08
 
G_M000_IG30:                ;; offset=0x027B
       cmp      r9d, 11
       jae      G_M000_IG20
 
G_M000_IG31:                ;; offset=0x0285
       cmp      r9d, 3
       jae      G_M000_IG17
 
G_M000_IG32:                ;; offset=0x028F
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       jmp      G_M000_IG24
 
G_M000_IG33:                ;; offset=0x029C
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       jmp      G_M000_IG22
 
G_M000_IG34:                ;; offset=0x02A9
       cmp      r9d, 600
       jae      G_M000_IG24
 
G_M000_IG35:                ;; offset=0x02B6
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      G_M000_IG24
 
G_M000_IG36:                ;; offset=0x02C3
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x02FF
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG38:                ;; offset=0x033B
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG39:                ;; offset=0x0392
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	41880000h		;        17
RWD04  	dd	41000000h		;         8
RWD08  	dd	41500000h		;        13
RWD12  	dd	40000000h		;         2
RWD16  	dd	42380000h		;        46
RWD20  	dd	41A80000h		;        21
RWD24  	dd	42080000h		;        34
RWD28  	dd	3F800000h		;         1
RWD32  	dd	40400000h		;         3
RWD36  	dd	40A00000h		;         5

; Total bytes of code 920

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedBatch8():Tl.FusionExperiment.SumReceipt:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 128
       lea      rbp, [rsp+0x80]
       xor      eax, eax
       mov      qword ptr [rbp-0x68], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:                ;; offset=0x0029
       mov      dword ptr [rbp-0x80], 0x3E8
       xor      eax, eax
       mov      dword ptr [rbp-0x34], eax
       xor      edi, edi
       call     [Tl.FusionExperiment.FusedPulse:Start(uint):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0x44], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0048
       mov      rdi, 0x7FC9271F3FE8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x30]
       mov      rdi, gword ptr [rax+0x08]
       mov      esi, dword ptr [rbp-0x44]
       mov      edx, 8
       call     [System.MemoryExtensions:AsSpan[uint](uint[],int,int):System.Span`1[uint]]
       mov      bword ptr [rbp-0x58], rax
       mov      qword ptr [rbp-0x50], rdx
       mov      rdi, bword ptr [rbp-0x58]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Span`1[uint]:op_Implicit(System.Span`1[uint]):System.ReadOnlySpan`1[uint]]
       mov      bword ptr [rbp-0x68], rax
       mov      qword ptr [rbp-0x60], rdx
       mov      rdx, bword ptr [rbp-0x68]
       mov      rcx, qword ptr [rbp-0x60]
       lea      rdi, [rbp-0x40]
       lea      rsi, [rbp-0x34]
       call     [Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       mov      eax, dword ptr [rbp-0x44]
       add      eax, 8
       mov      dword ptr [rbp-0x44], eax
 
G_M000_IG04:                ;; offset=0x00AE
       mov      eax, dword ptr [rbp-0x80]
       dec      eax
       mov      dword ptr [rbp-0x80], eax
       cmp      dword ptr [rbp-0x80], 0
       jg       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00BC
       lea      rdi, [rbp-0x80]
       mov      esi, 49
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG06:                ;; offset=0x00CA
       mov      rax, gword ptr [rbp-0x30]
       mov      rax, gword ptr [rax+0x08]
       mov      eax, dword ptr [rax+0x08]
       cmp      eax, dword ptr [rbp-0x44]
       jg       G_M000_IG03
       mov      rdi, 0x7FC9271F3FEC
       call     CORINFO_HELP_COUNTPROFILE32
       lea      rdi, [rbp-0x40]
       vmovss   xmm0, dword ptr [rbp-0x34]
       call     [Tl.FusionExperiment.SumReceipt:Capture(byref,float):Tl.FusionExperiment.SumReceipt]
       mov      qword ptr [rbp-0x78], rax
       mov      dword ptr [rbp-0x70], edx
       mov      rax, qword ptr [rbp-0x78]
       mov      edx, dword ptr [rbp-0x70]
 
G_M000_IG07:                ;; offset=0x010A
       add      rsp, 128
       pop      rbp
       ret      
 
; Total bytes of code 275

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedBatch8():Tl.FusionExperiment.SumReceipt:this (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x31
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 1 inlinees with PGO data; 7 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0xA8], r15
       mov      qword ptr [rsp+0xA0], rbx
       lea      rbp, [rsp+0x20]
       mov      rbx, gword ptr [rbp+0x60]
       mov      r15d, dword ptr [rbp+0x4C]
 
G_M000_IG02:                ;; offset=0x0026
       mov      rdx, gword ptr [rbx+0x08]
       cmp      dword ptr [rdx+0x08], r15d
       jle      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0030
       mov      rdx, gword ptr [rbx+0x08]
       test     rdx, rdx
       je       SHORT G_M000_IG06
       mov      ecx, dword ptr [rdx+0x08]
       mov      esi, r15d
       lea      rdi, [rsi+0x08]
       cmp      rcx, rdi
       jb       SHORT G_M000_IG06
       lea      rdx, bword ptr [rdx+4*rsi+0x10]
       mov      ecx, 8
       lea      rsi, [rbp+0x5C]
       lea      rdi, [rbp+0x50]
       call     [Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback]
       mov      qword ptr [rbp+0x50], rax
       add      r15d, 8
       mov      rax, gword ptr [rbx+0x08]
       cmp      dword ptr [rax+0x08], r15d
       jg       SHORT G_M000_IG03
 
G_M000_IG04:                ;; offset=0x0072
       vmovss   xmm0, dword ptr [rbp+0x5C]
       mov      rax, qword ptr [rbp+0x50]
       mov      qword ptr [rbp-0x20], rax
       vmovd    eax, xmm0
       mov      dword ptr [rbp-0x18], eax
       mov      rax, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
 
G_M000_IG05:                ;; offset=0x008D
       add      rsp, 160
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x0099
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
; Total bytes of code 160

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       mov      dword ptr [rbp-0x04], edi
       mov      dword ptr [rbp-0x08], esi
       mov      dword ptr [rbp-0x0C], edx
 
G_M000_IG02:                ;; offset=0x0013
       mov      eax, dword ptr [rbp-0x04]
       movzx    rcx, word  ptr [rbp-0x08]
       mov      ecx, ecx
       shl      rcx, 32
       or       rax, rcx
       movzx    rcx, word  ptr [rbp-0x0C]
       mov      ecx, ecx
       shl      rcx, 48
       or       rax, rcx
       mov      qword ptr [rbp-0x18], rax
       mov      rax, qword ptr [rbp-0x18]
 
G_M000_IG03:                ;; offset=0x0038
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 62

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; fully interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 752
       lea      rbp, [rsp+0x2F0]
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -672
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x30], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x50], rax
       mov      bword ptr [rbp-0x30], rdi
       mov      bword ptr [rbp-0x38], rsi
       mov      bword ptr [rbp-0x48], rdx
       mov      qword ptr [rbp-0x40], rcx
 
G_M000_IG02:                ;; offset=0x004B
       mov      dword ptr [rbp-0x220], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0068
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x218], rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x228], rax
       mov      rsi, gword ptr [rbp-0x228]
       mov      rdi, gword ptr [rbp-0x218]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x218]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00BB
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00CE
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x210], rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x230], rax
       mov      rsi, gword ptr [rbp-0x230]
       mov      rdi, gword ptr [rbp-0x210]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x210]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0121
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7FC9271794B0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x0145
       add      rsp, 752
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x014E
       mov      rax, bword ptr [rbp-0x30]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x4C], eax
       mov      rax, bword ptr [rbp-0x30]
       movzx    rax, word  ptr [rax+0x04]
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x4C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x54], edx
       mov      eax, dword ptr [rbp-0x4C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x58], eax
       xor      eax, eax
       mov      dword ptr [rbp-0x5C], eax
       cmp      dword ptr [rbp-0x40], 0
       jbe      G_M000_IG155
       mov      rax, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
       jmp      G_M000_IG147
 
G_M000_IG09:                ;; offset=0x01A6
       cmp      dword ptr [rbp-0x60], 47
       jae      G_M000_IG79
       cmp      dword ptr [rbp-0x60], 11
       jae      G_M000_IG44
       cmp      dword ptr [rbp-0x60], 3
       jae      G_M000_IG21
       jmp      G_M000_IG15
 
G_M000_IG10:                ;; offset=0x01C9
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1F4], eax
       mov      eax, dword ptr [rbp-0x1F4]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x68], eax
       mov      eax, dword ptr [rbp-0x1F4]
       mov      dword ptr [rbp-0x1F8], eax
       mov      eax, dword ptr [rbp-0x1F4]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG11
       mov      eax, dword ptr [rbp-0x68]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x1F8]
       mov      dword ptr [rbp-0x1FC], eax
       jmp      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0225
       mov      rdi, 0x7FC9271794B4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x1F8]
       mov      dword ptr [rbp-0x1FC], eax
 
G_M000_IG12:                ;; offset=0x024F
       mov      eax, dword ptr [rbp-0x1FC]
       mov      dword ptr [rbp-0x200], eax
       mov      eax, dword ptr [rbp-0x6C]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0271
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x208], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x238], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x240], rax
       mov      rsi, gword ptr [rbp-0x238]
       mov      rdx, gword ptr [rbp-0x240]
       mov      rdi, gword ptr [rbp-0x208]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x208]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG14:                ;; offset=0x02E7
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x6C]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x200]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG20
       mov      rdi, 0x7FC9271794B8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG15:                ;; offset=0x0365
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG19
       cmp      dword ptr [rbp-0x60], 0
       jb       SHORT G_M000_IG18
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x038A
       lea      rdi, [rbp-0x220]
       mov      esi, 286
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG17:                ;; offset=0x039B
       cmp      dword ptr [rbp-0x60], 3
       jb       G_M000_IG10
       mov      rdi, 0x7FC9271794BC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG18:                ;; offset=0x03B9
       mov      rdi, 0x7FC9271794C0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG19:                ;; offset=0x03CD
       mov      rdi, 0x7FC9271794C4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG20:                ;; offset=0x03E1
       mov      rdi, 0x7FC9271794C8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG15
 
G_M000_IG21:                ;; offset=0x03F5
       cmp      dword ptr [rbp-0x60], 7
       jae      G_M000_IG38
       jmp      G_M000_IG27
 
G_M000_IG22:                ;; offset=0x0404
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1DC], eax
       mov      eax, dword ptr [rbp-0x1DC]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x70], eax
       mov      eax, dword ptr [rbp-0x1DC]
       mov      dword ptr [rbp-0x1E0], eax
       mov      eax, dword ptr [rbp-0x1DC]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG23
       mov      eax, dword ptr [rbp-0x70]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x74], eax
       mov      eax, dword ptr [rbp-0x1E0]
       mov      dword ptr [rbp-0x1E4], eax
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x0460
       mov      rdi, 0x7FC9271794CC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x74], eax
       mov      eax, dword ptr [rbp-0x1E0]
       mov      dword ptr [rbp-0x1E4], eax
 
G_M000_IG24:                ;; offset=0x048A
       mov      eax, dword ptr [rbp-0x1E4]
       mov      dword ptr [rbp-0x1E8], eax
       mov      eax, dword ptr [rbp-0x74]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG26
 
G_M000_IG25:                ;; offset=0x04AC
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1F0], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x248], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x250], rax
       mov      rsi, gword ptr [rbp-0x248]
       mov      rdx, gword ptr [rbp-0x250]
       mov      rdi, gword ptr [rbp-0x1F0]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1F0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG26:                ;; offset=0x0522
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x74]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x78], xmm0
       mov      dword ptr [rbp-0x7C], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x7C]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x78]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x80], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x80]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1E8]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x70]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG32
       mov      rdi, 0x7FC9271794D0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG27:                ;; offset=0x0609
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG31
       cmp      dword ptr [rbp-0x60], 3
       jb       SHORT G_M000_IG30
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG29
 
G_M000_IG28:                ;; offset=0x062E
       lea      rdi, [rbp-0x220]
       mov      esi, 510
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG29:                ;; offset=0x063F
       cmp      dword ptr [rbp-0x60], 7
       jb       G_M000_IG22
       mov      rdi, 0x7FC9271794D4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG30:                ;; offset=0x065D
       mov      rdi, 0x7FC9271794D8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG31:                ;; offset=0x0671
       mov      rdi, 0x7FC9271794DC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG32:                ;; offset=0x0685
       mov      rdi, 0x7FC9271794E0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG33:                ;; offset=0x0699
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1C4], eax
       mov      eax, dword ptr [rbp-0x1C4]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x84], eax
       mov      eax, dword ptr [rbp-0x1C4]
       mov      dword ptr [rbp-0x1C8], eax
       mov      eax, dword ptr [rbp-0x1C4]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG34
       mov      eax, dword ptr [rbp-0x84]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x88], eax
       mov      eax, dword ptr [rbp-0x1C8]
       mov      dword ptr [rbp-0x1CC], eax
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x06FE
       mov      rdi, 0x7FC9271794E4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x88], eax
       mov      eax, dword ptr [rbp-0x1C8]
       mov      dword ptr [rbp-0x1CC], eax
 
G_M000_IG35:                ;; offset=0x072B
       mov      eax, dword ptr [rbp-0x1CC]
       mov      dword ptr [rbp-0x1D0], eax
       mov      eax, dword ptr [rbp-0x88]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG37
 
G_M000_IG36:                ;; offset=0x0750
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1D8], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x258], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x260], rax
       mov      rsi, gword ptr [rbp-0x258]
       mov      rdx, gword ptr [rbp-0x260]
       mov      rdi, gword ptr [rbp-0x1D8]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1D8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x07C6
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x88]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1D0]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x84]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG43
       mov      rdi, 0x7FC9271794E8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG38:                ;; offset=0x0862
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG42
       cmp      dword ptr [rbp-0x60], 7
       jb       SHORT G_M000_IG41
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG40
 
G_M000_IG39:                ;; offset=0x0887
       lea      rdi, [rbp-0x220]
       mov      esi, 680
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG40:                ;; offset=0x0898
       cmp      dword ptr [rbp-0x60], 11
       jb       G_M000_IG33
       mov      rdi, 0x7FC9271794EC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG41:                ;; offset=0x08B6
       mov      rdi, 0x7FC9271794F0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG42:                ;; offset=0x08CA
       mov      rdi, 0x7FC9271794F4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG43:                ;; offset=0x08DE
       mov      rdi, 0x7FC9271794F8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG38
 
G_M000_IG44:                ;; offset=0x08F2
       cmp      dword ptr [rbp-0x60], 18
       jae      G_M000_IG56
       jmp      G_M000_IG50
 
G_M000_IG45:                ;; offset=0x0901
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x1AC], eax
       mov      eax, dword ptr [rbp-0x1AC]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x8C], eax
       mov      eax, dword ptr [rbp-0x1AC]
       mov      dword ptr [rbp-0x1B0], eax
       mov      eax, dword ptr [rbp-0x1AC]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG46
       mov      eax, dword ptr [rbp-0x8C]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x90], eax
       mov      eax, dword ptr [rbp-0x1B0]
       mov      dword ptr [rbp-0x1B4], eax
       jmp      SHORT G_M000_IG47
 
G_M000_IG46:                ;; offset=0x0966
       mov      rdi, 0x7FC9271794FC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      eax, dword ptr [rbp-0x1B0]
       mov      dword ptr [rbp-0x1B4], eax
 
G_M000_IG47:                ;; offset=0x0993
       mov      eax, dword ptr [rbp-0x1B4]
       mov      dword ptr [rbp-0x1B8], eax
       mov      eax, dword ptr [rbp-0x90]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG49
 
G_M000_IG48:                ;; offset=0x09B8
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1C0], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x268], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x270], rax
       mov      rsi, gword ptr [rbp-0x268]
       mov      rdx, gword ptr [rbp-0x270]
       mov      rdi, gword ptr [rbp-0x1C0]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG49:                ;; offset=0x0A2E
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x90]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x1B8]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x8C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG55
       mov      rdi, 0x7FC927179500
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG50:                ;; offset=0x0A9A
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG54
       cmp      dword ptr [rbp-0x60], 11
       jb       SHORT G_M000_IG53
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG52
 
G_M000_IG51:                ;; offset=0x0ABF
       lea      rdi, [rbp-0x220]
       mov      esi, 843
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG52:                ;; offset=0x0AD0
       cmp      dword ptr [rbp-0x60], 18
       jb       G_M000_IG45
       mov      rdi, 0x7FC927179504
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG53:                ;; offset=0x0AEE
       mov      rdi, 0x7FC927179508
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG54:                ;; offset=0x0B02
       mov      rdi, 0x7FC92717950C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG55:                ;; offset=0x0B16
       mov      rdi, 0x7FC927179510
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG50
 
G_M000_IG56:                ;; offset=0x0B2A
       cmp      dword ptr [rbp-0x60], 29
       jae      G_M000_IG73
       jmp      G_M000_IG62
 
G_M000_IG57:                ;; offset=0x0B39
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x194], eax
       mov      eax, dword ptr [rbp-0x194]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x194]
       mov      dword ptr [rbp-0x198], eax
       mov      eax, dword ptr [rbp-0x194]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG58
       mov      eax, dword ptr [rbp-0x94]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x198]
       mov      dword ptr [rbp-0x19C], eax
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:                ;; offset=0x0B9E
       mov      rdi, 0x7FC927179514
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x198]
       mov      dword ptr [rbp-0x19C], eax
 
G_M000_IG59:                ;; offset=0x0BCB
       mov      eax, dword ptr [rbp-0x19C]
       mov      dword ptr [rbp-0x1A0], eax
       mov      eax, dword ptr [rbp-0x98]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG61
 
G_M000_IG60:                ;; offset=0x0BF0
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x278], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x280], rax
       mov      rsi, gword ptr [rbp-0x278]
       mov      rdx, gword ptr [rbp-0x280]
       mov      rdi, gword ptr [rbp-0x1A8]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x1A8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG61:                ;; offset=0x0C66
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x98]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1A0]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x94]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG67
       mov      rdi, 0x7FC927179518
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG62:                ;; offset=0x0D02
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG66
       cmp      dword ptr [rbp-0x60], 18
       jb       SHORT G_M000_IG65
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG64
 
G_M000_IG63:                ;; offset=0x0D27
       lea      rdi, [rbp-0x220]
       mov      esi, 0x405
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG64:                ;; offset=0x0D38
       cmp      dword ptr [rbp-0x60], 29
       jb       G_M000_IG57
       mov      rdi, 0x7FC92717951C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG65:                ;; offset=0x0D56
       mov      rdi, 0x7FC927179520
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG66:                ;; offset=0x0D6A
       mov      rdi, 0x7FC927179524
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG67:                ;; offset=0x0D7E
       mov      rdi, 0x7FC927179528
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG62
 
G_M000_IG68:                ;; offset=0x0D92
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x17C], eax
       mov      eax, dword ptr [rbp-0x17C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x17C]
       mov      dword ptr [rbp-0x180], eax
       mov      eax, dword ptr [rbp-0x17C]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG69
       mov      eax, dword ptr [rbp-0x9C]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x180]
       mov      dword ptr [rbp-0x184], eax
       jmp      SHORT G_M000_IG70
 
G_M000_IG69:                ;; offset=0x0DF7
       mov      rdi, 0x7FC92717952C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x180]
       mov      dword ptr [rbp-0x184], eax
 
G_M000_IG70:                ;; offset=0x0E24
       mov      eax, dword ptr [rbp-0x184]
       mov      dword ptr [rbp-0x188], eax
       mov      eax, dword ptr [rbp-0xA0]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG72
 
G_M000_IG71:                ;; offset=0x0E49
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x190], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x288], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x290], rax
       mov      rsi, gword ptr [rbp-0x288]
       mov      rdx, gword ptr [rbp-0x290]
       mov      rdi, gword ptr [rbp-0x190]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x190]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG72:                ;; offset=0x0EBF
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xA0]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0xA4], xmm0
       mov      dword ptr [rbp-0xA8], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0xA8]
       vmulss   xmm0, xmm0, dword ptr [rbp-0xA4]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0xAC], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0xAC]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x188]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x9C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG78
       mov      rdi, 0x7FC927179530
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG73:                ;; offset=0x0F8E
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG77
       cmp      dword ptr [rbp-0x60], 29
       jb       SHORT G_M000_IG76
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG75
 
G_M000_IG74:                ;; offset=0x0FB3
       lea      rdi, [rbp-0x220]
       mov      esi, 0x4C7
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG75:                ;; offset=0x0FC4
       cmp      dword ptr [rbp-0x60], 47
       jb       G_M000_IG68
       mov      rdi, 0x7FC927179534
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG76:                ;; offset=0x0FE2
       mov      rdi, 0x7FC927179538
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG77:                ;; offset=0x0FF6
       mov      rdi, 0x7FC92717953C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG78:                ;; offset=0x100A
       mov      rdi, 0x7FC927179540
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG73
 
G_M000_IG79:                ;; offset=0x101E
       cmp      dword ptr [rbp-0x60], 200
       jae      G_M000_IG114
       cmp      dword ptr [rbp-0x60], 76
       jae      G_M000_IG91
       jmp      G_M000_IG85
 
G_M000_IG80:                ;; offset=0x103A
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x164], eax
       mov      eax, dword ptr [rbp-0x164]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xB0], eax
       mov      eax, dword ptr [rbp-0x164]
       mov      dword ptr [rbp-0x168], eax
       mov      eax, dword ptr [rbp-0x164]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG81
       mov      eax, dword ptr [rbp-0xB0]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x16C], eax
       jmp      SHORT G_M000_IG82
 
G_M000_IG81:                ;; offset=0x109F
       mov      rdi, 0x7FC927179544
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x16C], eax
 
G_M000_IG82:                ;; offset=0x10CC
       mov      eax, dword ptr [rbp-0x16C]
       mov      dword ptr [rbp-0x170], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG84
 
G_M000_IG83:                ;; offset=0x10F1
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x178], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x298], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2A0], rax
       mov      rsi, gword ptr [rbp-0x298]
       mov      rdx, gword ptr [rbp-0x2A0]
       mov      rdi, gword ptr [rbp-0x178]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x178]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG84:                ;; offset=0x1167
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xB4]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x170]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG90
       mov      rdi, 0x7FC927179548
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG85:                ;; offset=0x1203
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG89
       cmp      dword ptr [rbp-0x60], 47
       jb       SHORT G_M000_IG88
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG87
 
G_M000_IG86:                ;; offset=0x1228
       lea      rdi, [rbp-0x220]
       mov      esi, 0x58D
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG87:                ;; offset=0x1239
       cmp      dword ptr [rbp-0x60], 76
       jb       G_M000_IG80
       mov      rdi, 0x7FC92717954C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG88:                ;; offset=0x1257
       mov      rdi, 0x7FC927179550
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG89:                ;; offset=0x126B
       mov      rdi, 0x7FC927179554
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG90:                ;; offset=0x127F
       mov      rdi, 0x7FC927179558
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG85
 
G_M000_IG91:                ;; offset=0x1293
       cmp      dword ptr [rbp-0x60], 123
       jae      G_M000_IG108
       jmp      G_M000_IG97
 
G_M000_IG92:                ;; offset=0x12A2
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x14C], eax
       mov      eax, dword ptr [rbp-0x14C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0x14C]
       mov      dword ptr [rbp-0x150], eax
       mov      eax, dword ptr [rbp-0x14C]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG93
       mov      eax, dword ptr [rbp-0xB8]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xBC], eax
       mov      eax, dword ptr [rbp-0x150]
       mov      dword ptr [rbp-0x154], eax
       jmp      SHORT G_M000_IG94
 
G_M000_IG93:                ;; offset=0x1307
       mov      rdi, 0x7FC92717955C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xBC], eax
       mov      eax, dword ptr [rbp-0x150]
       mov      dword ptr [rbp-0x154], eax
 
G_M000_IG94:                ;; offset=0x1334
       mov      eax, dword ptr [rbp-0x154]
       mov      dword ptr [rbp-0x158], eax
       mov      eax, dword ptr [rbp-0xBC]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG96
 
G_M000_IG95:                ;; offset=0x1359
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x160], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2A8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2B0], rax
       mov      rsi, gword ptr [rbp-0x2A8]
       mov      rdx, gword ptr [rbp-0x2B0]
       mov      rdi, gword ptr [rbp-0x160]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x160]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG96:                ;; offset=0x13CF
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xBC]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x60]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0xC0], xmm0
       mov      dword ptr [rbp-0xC4], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0xC4]
       vmulss   xmm0, xmm0, dword ptr [rbp-0xC0]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0xC8], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0xC8]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x158]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG102
       mov      rdi, 0x7FC927179560
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG97:                ;; offset=0x14B6
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG101
       cmp      dword ptr [rbp-0x60], 76
       jb       SHORT G_M000_IG100
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG99
 
G_M000_IG98:                ;; offset=0x14DB
       lea      rdi, [rbp-0x220]
       mov      esi, 0x667
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG99:                ;; offset=0x14EC
       cmp      dword ptr [rbp-0x60], 123
       jb       G_M000_IG92
       mov      rdi, 0x7FC927179564
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG100:                ;; offset=0x150A
       mov      rdi, 0x7FC927179568
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG101:                ;; offset=0x151E
       mov      rdi, 0x7FC92717956C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG102:                ;; offset=0x1532
       mov      rdi, 0x7FC927179570
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG97
 
G_M000_IG103:                ;; offset=0x1546
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x134], eax
       mov      eax, dword ptr [rbp-0x134]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xCC], eax
       mov      eax, dword ptr [rbp-0x134]
       mov      dword ptr [rbp-0x138], eax
       mov      eax, dword ptr [rbp-0x134]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG104
       mov      eax, dword ptr [rbp-0xCC]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0x138]
       mov      dword ptr [rbp-0x13C], eax
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:                ;; offset=0x15AB
       mov      rdi, 0x7FC927179574
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xD0], eax
       mov      eax, dword ptr [rbp-0x138]
       mov      dword ptr [rbp-0x13C], eax
 
G_M000_IG105:                ;; offset=0x15D8
       mov      eax, dword ptr [rbp-0x13C]
       mov      dword ptr [rbp-0x140], eax
       mov      eax, dword ptr [rbp-0xD0]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG107
 
G_M000_IG106:                ;; offset=0x15FD
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x148], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2B8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2C0], rax
       mov      rsi, gword ptr [rbp-0x2B8]
       mov      rdx, gword ptr [rbp-0x2C0]
       mov      rdi, gword ptr [rbp-0x148]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x148]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG107:                ;; offset=0x1673
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xD0]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x140]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG113
       mov      rdi, 0x7FC927179578
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG108:                ;; offset=0x16F7
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG112
       cmp      dword ptr [rbp-0x60], 123
       jb       SHORT G_M000_IG111
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG110
 
G_M000_IG109:                ;; offset=0x171C
       lea      rdi, [rbp-0x220]
       mov      esi, 0x709
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG110:                ;; offset=0x172D
       cmp      dword ptr [rbp-0x60], 200
       jb       G_M000_IG103
       mov      rdi, 0x7FC92717957C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG111:                ;; offset=0x174E
       mov      rdi, 0x7FC927179580
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG112:                ;; offset=0x1762
       mov      rdi, 0x7FC927179584
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG113:                ;; offset=0x1776
       mov      rdi, 0x7FC927179588
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG108
 
G_M000_IG114:                ;; offset=0x178A
       cmp      dword ptr [rbp-0x60], 321
       jae      G_M000_IG126
       jmp      G_M000_IG120
 
G_M000_IG115:                ;; offset=0x179C
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x11C], eax
       mov      eax, dword ptr [rbp-0x11C]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xD4], eax
       mov      eax, dword ptr [rbp-0x11C]
       mov      dword ptr [rbp-0x120], eax
       mov      eax, dword ptr [rbp-0x11C]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG116
       mov      eax, dword ptr [rbp-0xD4]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xD8], eax
       mov      eax, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x124], eax
       jmp      SHORT G_M000_IG117
 
G_M000_IG116:                ;; offset=0x1801
       mov      rdi, 0x7FC92717958C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xD8], eax
       mov      eax, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x124], eax
 
G_M000_IG117:                ;; offset=0x182E
       mov      eax, dword ptr [rbp-0x124]
       mov      dword ptr [rbp-0x128], eax
       mov      eax, dword ptr [rbp-0xD8]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG119
 
G_M000_IG118:                ;; offset=0x1853
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x130], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2C8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2D0], rax
       mov      rsi, gword ptr [rbp-0x2C8]
       mov      rdx, gword ptr [rbp-0x2D0]
       mov      rdi, gword ptr [rbp-0x130]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x130]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG119:                ;; offset=0x18C9
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xD8]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG125
       mov      rdi, 0x7FC927179590
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG120:                ;; offset=0x1965
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG124
       cmp      dword ptr [rbp-0x60], 200
       jb       SHORT G_M000_IG123
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG122
 
G_M000_IG121:                ;; offset=0x198D
       lea      rdi, [rbp-0x220]
       mov      esi, 0x7CC
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG122:                ;; offset=0x199E
       cmp      dword ptr [rbp-0x60], 321
       jb       G_M000_IG115
       mov      rdi, 0x7FC927179594
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG123:                ;; offset=0x19BF
       mov      rdi, 0x7FC927179598
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG124:                ;; offset=0x19D3
       mov      rdi, 0x7FC92717959C
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG125:                ;; offset=0x19E7
       mov      rdi, 0x7FC9271795A0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG120
 
G_M000_IG126:                ;; offset=0x19FB
       cmp      dword ptr [rbp-0x60], 515
       jae      G_M000_IG143
       jmp      G_M000_IG132
 
G_M000_IG127:                ;; offset=0x1A0D
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0x104], eax
       mov      eax, dword ptr [rbp-0x104]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xDC], eax
       mov      eax, dword ptr [rbp-0x104]
       mov      dword ptr [rbp-0x108], eax
       mov      eax, dword ptr [rbp-0x104]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG128
       mov      eax, dword ptr [rbp-0xDC]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xE0], eax
       mov      eax, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x10C], eax
       jmp      SHORT G_M000_IG129
 
G_M000_IG128:                ;; offset=0x1A72
       mov      rdi, 0x7FC9271795A4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xE0], eax
       mov      eax, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x10C], eax
 
G_M000_IG129:                ;; offset=0x1A9F
       mov      eax, dword ptr [rbp-0x10C]
       mov      dword ptr [rbp-0x110], eax
       mov      eax, dword ptr [rbp-0xE0]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG131
 
G_M000_IG130:                ;; offset=0x1AC4
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x118], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2D8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2E0], rax
       mov      rsi, gword ptr [rbp-0x2D8]
       mov      rdx, gword ptr [rbp-0x2E0]
       mov      rdi, gword ptr [rbp-0x118]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x118]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG131:                ;; offset=0x1B3A
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xE0]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x110]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG137
       mov      rdi, 0x7FC9271795A8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG132:                ;; offset=0x1BEE
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG136
       cmp      dword ptr [rbp-0x60], 321
       jb       SHORT G_M000_IG135
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG134
 
G_M000_IG133:                ;; offset=0x1C16
       lea      rdi, [rbp-0x220]
       mov      esi, 0x899
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG134:                ;; offset=0x1C27
       cmp      dword ptr [rbp-0x60], 515
       jb       G_M000_IG127
       mov      rdi, 0x7FC9271795AC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG135:                ;; offset=0x1C48
       mov      rdi, 0x7FC9271795B0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG136:                ;; offset=0x1C5C
       mov      rdi, 0x7FC9271795B4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG137:                ;; offset=0x1C70
       mov      rdi, 0x7FC9271795B8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG132
 
G_M000_IG138:                ;; offset=0x1C84
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xEC], eax
       mov      eax, dword ptr [rbp-0xEC]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0xE4], eax
       mov      eax, dword ptr [rbp-0xEC]
       mov      dword ptr [rbp-0xF0], eax
       mov      eax, dword ptr [rbp-0xEC]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG139
       mov      eax, dword ptr [rbp-0xE4]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xE8], eax
       mov      eax, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0xF4], eax
       jmp      SHORT G_M000_IG140
 
G_M000_IG139:                ;; offset=0x1CE9
       mov      rdi, 0x7FC9271795BC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x60]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0xE8], eax
       mov      eax, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0xF4], eax
 
G_M000_IG140:                ;; offset=0x1D16
       mov      eax, dword ptr [rbp-0xF4]
       mov      dword ptr [rbp-0xF8], eax
       mov      eax, dword ptr [rbp-0xE8]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG142
 
G_M000_IG141:                ;; offset=0x1D3B
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2E8], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x2F0], rax
       mov      rsi, gword ptr [rbp-0x2E8]
       mov      rdx, gword ptr [rbp-0x2F0]
       mov      rdi, gword ptr [rbp-0x100]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x100]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG142:                ;; offset=0x1DB1
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0xE8]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xF8]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x5C]
       inc      eax
       mov      dword ptr [rbp-0x5C], eax
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG154
       mov      rdi, 0x7FC9271795C0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x5C], eax
       jae      G_M000_IG155
       mov      eax, dword ptr [rbp-0x5C]
       mov      rcx, bword ptr [rbp-0x48]
       mov      eax, dword ptr [rcx+4*rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x60], edx
 
G_M000_IG143:                ;; offset=0x1E35
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG153
       cmp      dword ptr [rbp-0x60], 515
       jb       G_M000_IG152
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG145
 
G_M000_IG144:                ;; offset=0x1E65
       lea      rdi, [rbp-0x220]
       mov      esi, 0x93B
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG145:                ;; offset=0x1E76
       cmp      dword ptr [rbp-0x60], 600
       jb       G_M000_IG138
 
G_M000_IG146:                ;; offset=0x1E83
       mov      rdi, 0x7FC9271795C4
       call     CORINFO_HELP_COUNTPROFILE32
 
G_M000_IG147:                ;; offset=0x1E92
       mov      eax, dword ptr [rbp-0x220]
       dec      eax
       mov      dword ptr [rbp-0x220], eax
       cmp      dword ptr [rbp-0x220], 0
       jg       SHORT G_M000_IG149
 
G_M000_IG148:                ;; offset=0x1EA9
       lea      rdi, [rbp-0x220]
       mov      esi, 0x947
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG149:                ;; offset=0x1EBA
       mov      eax, dword ptr [rbp-0x5C]
       cmp      eax, dword ptr [rbp-0x40]
       jb       G_M000_IG09
       mov      dword ptr [rbp-0x64], 1
       cmp      dword ptr [rbp-0x54], 599
       jne      SHORT G_M000_IG150
       mov      rdi, 0x7FC9271795C8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x64]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x64], eax
 
G_M000_IG150:                ;; offset=0x1EF1
       mov      rdi, 0x7FC9271795CC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x64]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG151:                ;; offset=0x1F10
       add      rsp, 752
       pop      rbp
       ret      
 
G_M000_IG152:                ;; offset=0x1F19
       mov      rdi, 0x7FC9271795D0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG153:                ;; offset=0x1F2D
       mov      rdi, 0x7FC9271795D4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG147
 
G_M000_IG154:                ;; offset=0x1F41
       mov      rdi, 0x7FC9271795D8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG143
 
G_M000_IG155:                ;; offset=0x1F55
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	40400000h		;         3
RWD08  	dd	40000000h		;         2
RWD12  	dd	40A00000h		;         5
RWD16  	dd	41000000h		;         8
RWD20  	dd	41880000h		;        17
RWD24  	dd	41500000h		;        13
RWD28  	dd	42380000h		;        46
RWD32  	dd	42080000h		;        34

; Total bytes of code 8027

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data

G_M000_IG01:                ;; offset=0x0000
 
G_M000_IG02:                ;; offset=0x0000
       mov      eax, edi
       movzx    rcx, si
       shl      rcx, 32
       or       rax, rcx
       movzx    rcx, dx
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG03:                ;; offset=0x0016
       ret      
 
; Total bytes of code 23

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 599043
; 0 inlinees with PGO data; 4 single block inlinees; 0 inlinees without PGO data

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
       je       G_M000_IG100
       test     al, 2
       jne      G_M000_IG101
       test     ecx, ecx
       je       G_M000_IG102
 
G_M000_IG03:                ;; offset=0x0028
       mov      ebx, dword ptr [rdi]
       movzx    r15, word  ptr [rdi+0x04]
       mov      edi, ebx
       imul     rdi, rdi, 0x1B4E81B5
       shr      rdi, 38
       imul     edi, edi, 600
       mov      r14d, ebx
       sub      r14d, edi
       mov      edi, ebx
       imul     rdi, rdi, 0x1B4E81B5
       shr      rdi, 38
       xor      eax, eax
       mov      r8d, dword ptr [rdx]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
       cmp      eax, ecx
       jb       G_M000_IG89
 
G_M000_IG04:                ;; offset=0x007A
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
 
G_M000_IG05:                ;; offset=0x00A1
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x00AC
       mov      r11d, r10d
       sub      r11d, edi
 
G_M000_IG07:                ;; offset=0x00B2
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG115
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG08:                ;; offset=0x00F6
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG09:                ;; offset=0x011D
       cmp      r8d, 515
       jb       G_M000_IG88
 
G_M000_IG10:                ;; offset=0x012A
       cmp      r8d, 600
       jae      G_M000_IG88
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jae      G_M000_IG06
 
G_M000_IG11:                ;; offset=0x015D
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
       jmp      G_M000_IG07
 
G_M000_IG12:                ;; offset=0x016D
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG13:                ;; offset=0x0178
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG113
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG14:                ;; offset=0x01CC
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG15:                ;; offset=0x01F3
       cmp      r8d, 200
       jb       G_M000_IG88
 
G_M000_IG16:                ;; offset=0x0200
       cmp      r8d, 321
       jae      G_M000_IG88
 
G_M000_IG17:                ;; offset=0x020D
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jb       G_M000_IG12
 
G_M000_IG18:                ;; offset=0x0233
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG13
 
G_M000_IG19:                ;; offset=0x023E
       cmp      r8d, 76
       jb       G_M000_IG38
 
G_M000_IG20:                ;; offset=0x0248
       cmp      r8d, 123
       jb       G_M000_IG31
 
G_M000_IG21:                ;; offset=0x0252
       cmp      r8d, 123
       jb       G_M000_IG88
 
G_M000_IG22:                ;; offset=0x025C
       cmp      r8d, 200
       jae      G_M000_IG88
 
G_M000_IG23:                ;; offset=0x0269
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jae      SHORT G_M000_IG27
 
G_M000_IG24:                ;; offset=0x028B
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG25:                ;; offset=0x0296
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG112
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG26:                ;; offset=0x02DA
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
       jmp      G_M000_IG21
 
G_M000_IG27:                ;; offset=0x0306
       mov      r11d, r10d
       sub      r11d, edi
       jmp      SHORT G_M000_IG25
 
G_M000_IG28:                ;; offset=0x030E
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG29:                ;; offset=0x0319
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG111
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       lea      edi, [r8-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG30:                ;; offset=0x038A
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG31:                ;; offset=0x03B1
       cmp      r8d, 76
       jb       G_M000_IG88
 
G_M000_IG32:                ;; offset=0x03BB
       cmp      r8d, 123
       jae      G_M000_IG88
 
G_M000_IG33:                ;; offset=0x03C5
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jb       G_M000_IG28
 
G_M000_IG34:                ;; offset=0x03EB
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG29
 
G_M000_IG35:                ;; offset=0x03F6
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG36:                ;; offset=0x0401
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG110
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG37:                ;; offset=0x0455
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG38:                ;; offset=0x0474
       cmp      r8d, 47
       jb       G_M000_IG88
 
G_M000_IG39:                ;; offset=0x047E
       cmp      r8d, 76
       jae      G_M000_IG88
 
G_M000_IG40:                ;; offset=0x0488
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jb       G_M000_IG35
 
G_M000_IG41:                ;; offset=0x04AE
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG36
 
G_M000_IG42:                ;; offset=0x04B9
       cmp      r8d, 11
       jb       G_M000_IG66
 
G_M000_IG43:                ;; offset=0x04C3
       cmp      r8d, 18
       jb       G_M000_IG62
 
G_M000_IG44:                ;; offset=0x04CD
       cmp      r8d, 29
       jb       G_M000_IG55
 
G_M000_IG45:                ;; offset=0x04D7
       cmp      r8d, 29
       jb       G_M000_IG88
 
G_M000_IG46:                ;; offset=0x04E1
       cmp      r8d, 47
       jae      G_M000_IG88
 
G_M000_IG47:                ;; offset=0x04EB
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jae      G_M000_IG51
 
G_M000_IG48:                ;; offset=0x0511
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG49:                ;; offset=0x051C
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG109
       add      r11d, r15d
       movzx    r15, r11w
       lea      edi, [r8-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG50:                ;; offset=0x057D
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
       jmp      G_M000_IG45
 
G_M000_IG51:                ;; offset=0x05A1
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG49
 
G_M000_IG52:                ;; offset=0x05AC
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG53:                ;; offset=0x05B7
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG108
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG54:                ;; offset=0x060B
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG55:                ;; offset=0x062A
       cmp      r8d, 18
       jb       G_M000_IG88
 
G_M000_IG56:                ;; offset=0x0634
       cmp      r8d, 29
       jae      G_M000_IG88
 
G_M000_IG57:                ;; offset=0x063E
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jb       G_M000_IG52
 
G_M000_IG58:                ;; offset=0x0664
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG53
 
G_M000_IG59:                ;; offset=0x066F
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG60:                ;; offset=0x067A
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG107
       add      r11d, r15d
       movzx    r15, r11w
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG61:                ;; offset=0x06AE
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG62:                ;; offset=0x06CD
       cmp      r8d, 11
       jb       G_M000_IG88
 
G_M000_IG63:                ;; offset=0x06D7
       cmp      r8d, 18
       jae      G_M000_IG88
 
G_M000_IG64:                ;; offset=0x06E1
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jb       G_M000_IG59
 
G_M000_IG65:                ;; offset=0x0707
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG60
 
G_M000_IG66:                ;; offset=0x0712
       cmp      r8d, 3
       jb       G_M000_IG85
 
G_M000_IG67:                ;; offset=0x071C
       cmp      r8d, 7
       jae      G_M000_IG78
 
G_M000_IG68:                ;; offset=0x0726
       cmp      r8d, 3
       jb       G_M000_IG88
 
G_M000_IG69:                ;; offset=0x0730
       cmp      r8d, 7
       jae      G_M000_IG88
 
G_M000_IG70:                ;; offset=0x073A
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jae      G_M000_IG74
 
G_M000_IG71:                ;; offset=0x0760
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG72:                ;; offset=0x076B
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG105
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmovss   dword ptr [rsi], xmm0
       lea      edi, [r8-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG73:                ;; offset=0x07E4
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
       jmp      G_M000_IG68
 
G_M000_IG74:                ;; offset=0x0808
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG72
 
G_M000_IG75:                ;; offset=0x0813
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG76:                ;; offset=0x081E
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG106
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG77:                ;; offset=0x0872
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG78:                ;; offset=0x0891
       cmp      r8d, 7
       jb       G_M000_IG88
 
G_M000_IG79:                ;; offset=0x089B
       cmp      r8d, 11
       jae      G_M000_IG88
 
G_M000_IG80:                ;; offset=0x08A5
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jb       G_M000_IG75
 
G_M000_IG81:                ;; offset=0x08CB
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG76
 
G_M000_IG82:                ;; offset=0x08D6
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
 
G_M000_IG83:                ;; offset=0x08E1
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG104
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      SHORT G_M000_IG88
 
G_M000_IG84:                ;; offset=0x0921
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG85:                ;; offset=0x0940
       cmp      r8d, 3
       jae      SHORT G_M000_IG88
 
G_M000_IG86:                ;; offset=0x0946
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jb       G_M000_IG82
 
G_M000_IG87:                ;; offset=0x096C
       mov      r11d, r10d
       sub      r11d, edi
       jmp      G_M000_IG83
 
G_M000_IG88:                ;; offset=0x0977
       cmp      eax, ecx
       jae      G_M000_IG04
 
G_M000_IG89:                ;; offset=0x097F
       cmp      r8d, 47
       jb       G_M000_IG42
 
G_M000_IG90:                ;; offset=0x0989
       cmp      r8d, 200
       jb       G_M000_IG19
 
G_M000_IG91:                ;; offset=0x0996
       cmp      r8d, 321
       jb       G_M000_IG15
 
G_M000_IG92:                ;; offset=0x09A3
       cmp      r8d, 515
       jae      G_M000_IG09
       jmp      G_M000_IG96
 
G_M000_IG93:                ;; offset=0x09B5
       mov      r11d, r10d
       sub      r11d, edi
 
G_M000_IG94:                ;; offset=0x09BB
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG114
       add      r11d, r15d
       movzx    r15, r11w
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
       mov      ebx, r9d
       mov      r14d, r8d
       mov      edi, r10d
       inc      eax
       cmp      eax, ecx
       jae      G_M000_IG88
 
G_M000_IG95:                ;; offset=0x0A1F
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r8d, eax
       mov      r8d, dword ptr [rdx+4*r8]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       sub      r8d, r9d
 
G_M000_IG96:                ;; offset=0x0A46
       cmp      r8d, 321
       jb       G_M000_IG88
 
G_M000_IG97:                ;; offset=0x0A53
       cmp      r8d, 515
       jae      G_M000_IG88
 
G_M000_IG98:                ;; offset=0x0A60
       cmp      eax, ecx
       jae      G_M000_IG116
       mov      r9d, eax
       mov      r9d, dword ptr [rdx+4*r9]
       mov      r10d, r9d
       imul     r10, r10, 0x1B4E81B5
       shr      r10, 38
       cmp      r9d, ebx
       jae      G_M000_IG93
 
G_M000_IG99:                ;; offset=0x0A86
       cmp      r8d, r14d
       setb     r11b
       movzx    r11, r11b
       jmp      G_M000_IG94
 
G_M000_IG100:                ;; offset=0x0A96
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG101:                ;; offset=0x0AD2
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG102:                ;; offset=0x0B0E
       mov      rax, qword ptr [rdi]
 
G_M000_IG103:                ;; offset=0x0B11
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG104:                ;; offset=0x0B1C
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG105:                ;; offset=0x0B73
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG106:                ;; offset=0x0BCA
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG107:                ;; offset=0x0C21
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG108:                ;; offset=0x0C78
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG109:                ;; offset=0x0CCF
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG110:                ;; offset=0x0D26
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG111:                ;; offset=0x0D7D
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG112:                ;; offset=0x0DD4
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG113:                ;; offset=0x0E2B
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG114:                ;; offset=0x0E82
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r14
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG115:                ;; offset=0x0ED9
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG116:                ;; offset=0x0F30
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	40400000h		;         3
RWD04  	dd	40000000h		;         2
RWD08  	dd	40A00000h		;         5
RWD12  	dd	41000000h		;         8
RWD16  	dd	42380000h		;        46
RWD20  	dd	41A80000h		;        21
RWD24  	dd	42080000h		;        34
RWD28  	dd	41500000h		;        13
RWD32  	dd	41880000h		;        17
RWD36  	dd	3F800000h		;         1

; Total bytes of code 3894

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedBatch8():Tl.FusionExperiment.SumReceipt:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 128
       lea      rbp, [rsp+0x80]
       xor      eax, eax
       mov      qword ptr [rbp-0x68], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:                ;; offset=0x0029
       mov      dword ptr [rbp-0x80], 0x3E8
       xor      eax, eax
       mov      dword ptr [rbp-0x34], eax
       xor      edi, edi
       call     [Tl.FusionExperiment.FusedPulse:Start(uint):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       xor      eax, eax
       mov      dword ptr [rbp-0x44], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0048
       mov      rdi, 0x7FC9271F3FE8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x30]
       mov      rdi, gword ptr [rax+0x08]
       mov      esi, dword ptr [rbp-0x44]
       mov      edx, 8
       call     [System.MemoryExtensions:AsSpan[uint](uint[],int,int):System.Span`1[uint]]
       mov      bword ptr [rbp-0x58], rax
       mov      qword ptr [rbp-0x50], rdx
       mov      rdi, bword ptr [rbp-0x58]
       mov      rsi, qword ptr [rbp-0x50]
       call     [System.Span`1[uint]:op_Implicit(System.Span`1[uint]):System.ReadOnlySpan`1[uint]]
       mov      bword ptr [rbp-0x68], rax
       mov      qword ptr [rbp-0x60], rdx
       mov      rdx, bword ptr [rbp-0x68]
       mov      rcx, qword ptr [rbp-0x60]
       lea      rdi, [rbp-0x40]
       lea      rsi, [rbp-0x34]
       call     [Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       mov      eax, dword ptr [rbp-0x44]
       add      eax, 8
       mov      dword ptr [rbp-0x44], eax
 
G_M000_IG04:                ;; offset=0x00AE
       mov      eax, dword ptr [rbp-0x80]
       dec      eax
       mov      dword ptr [rbp-0x80], eax
       cmp      dword ptr [rbp-0x80], 0
       jg       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00BC
       lea      rdi, [rbp-0x80]
       mov      esi, 49
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG06:                ;; offset=0x00CA
       mov      rax, gword ptr [rbp-0x30]
       mov      rax, gword ptr [rax+0x08]
       mov      eax, dword ptr [rax+0x08]
       cmp      eax, dword ptr [rbp-0x44]
       jg       G_M000_IG03
       mov      rdi, 0x7FC9271F3FEC
       call     CORINFO_HELP_COUNTPROFILE32
       lea      rdi, [rbp-0x40]
       vmovss   xmm0, dword ptr [rbp-0x34]
       call     [Tl.FusionExperiment.SumReceipt:Capture(byref,float):Tl.FusionExperiment.SumReceipt]
       mov      qword ptr [rbp-0x78], rax
       mov      dword ptr [rbp-0x70], edx
       mov      rax, qword ptr [rbp-0x78]
       mov      edx, dword ptr [rbp-0x70]
 
G_M000_IG07:                ;; offset=0x010A
       add      rsp, 128
       pop      rbp
       ret      
 
; Total bytes of code 275

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Start(uint):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       mov      dword ptr [rbp-0x04], edi
 
G_M000_IG02:                ;; offset=0x000D
       mov      edi, dword ptr [rbp-0x04]
       xor      esi, esi
       mov      edx, 1
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG03:                ;; offset=0x001E
       add      rsp, 16
       pop      rbp
       ret      
 
; Total bytes of code 36

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedBatch8():Tl.FusionExperiment.SumReceipt:this (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x31
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 3
; 1 inlinees with PGO data; 7 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0xA8], r15
       mov      qword ptr [rsp+0xA0], rbx
       lea      rbp, [rsp+0x20]
       mov      rbx, gword ptr [rbp+0x60]
       mov      r15d, dword ptr [rbp+0x4C]
 
G_M000_IG02:                ;; offset=0x0026
       mov      rdx, gword ptr [rbx+0x08]
       cmp      dword ptr [rdx+0x08], r15d
       jle      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0030
       mov      rdx, gword ptr [rbx+0x08]
       test     rdx, rdx
       je       SHORT G_M000_IG06
       mov      ecx, dword ptr [rdx+0x08]
       mov      esi, r15d
       lea      rdi, [rsi+0x08]
       cmp      rcx, rdi
       jb       SHORT G_M000_IG06
       lea      rdx, bword ptr [rdx+4*rsi+0x10]
       mov      ecx, 8
       lea      rsi, [rbp+0x5C]
       lea      rdi, [rbp+0x50]
       call     [Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback]
       mov      qword ptr [rbp+0x50], rax
       add      r15d, 8
       mov      rax, gword ptr [rbx+0x08]
       cmp      dword ptr [rax+0x08], r15d
       jg       SHORT G_M000_IG03
 
G_M000_IG04:                ;; offset=0x0072
       vmovss   xmm0, dword ptr [rbp+0x5C]
       mov      rax, qword ptr [rbp+0x50]
       mov      qword ptr [rbp-0x20], rax
       vmovd    eax, xmm0
       mov      dword ptr [rbp-0x18], eax
       mov      rax, qword ptr [rbp-0x20]
       mov      edx, dword ptr [rbp-0x18]
 
G_M000_IG05:                ;; offset=0x008D
       add      rsp, 160
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x0099
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
; Total bytes of code 160

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedBatch8():Tl.FusionExperiment.SumReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 1 inlinees with PGO data; 7 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     rbx
       sub      rsp, 48
       lea      rbp, [rsp+0x40]
       mov      rbx, rdi
 
G_M000_IG02:                ;; offset=0x0010
       xor      edx, edx
       mov      dword ptr [rbp-0x14], edx
       mov      rdx, 0x1000000000000
       mov      qword ptr [rbp-0x28], rdx
       mov      qword ptr [rbp-0x20], rdx
       xor      r15d, r15d
       mov      rdx, gword ptr [rbx+0x08]
       cmp      dword ptr [rdx+0x08], 0
       jle      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0034
       mov      rdx, gword ptr [rbx+0x08]
       test     rdx, rdx
       je       SHORT G_M000_IG06
       mov      ecx, dword ptr [rdx+0x08]
       mov      esi, r15d
       lea      rdi, [rsi+0x08]
       cmp      rcx, rdi
       jb       SHORT G_M000_IG06
       lea      rdx, bword ptr [rdx+4*rsi+0x10]
       mov      ecx, 8
       lea      rsi, [rbp-0x14]
       lea      rdi, [rbp-0x20]
       call     [Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback]
       mov      qword ptr [rbp-0x20], rax
       add      r15d, 8
       mov      rax, gword ptr [rbx+0x08]
       cmp      dword ptr [rax+0x08], r15d
       jg       SHORT G_M000_IG03
 
G_M000_IG04:                ;; offset=0x0076
       vmovss   xmm0, dword ptr [rbp-0x14]
       mov      rax, qword ptr [rbp-0x20]
       mov      qword ptr [rbp-0x38], rax
       vmovd    eax, xmm0
       mov      dword ptr [rbp-0x30], eax
       mov      rax, qword ptr [rbp-0x38]
       mov      edx, dword ptr [rbp-0x30]
 
G_M000_IG05:                ;; offset=0x0091
       add      rsp, 48
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x009A
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
; Total bytes of code 161

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Start(uint):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data
; 0 inlinees with PGO data; 1 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
 
G_M000_IG02:                ;; offset=0x0000
       mov      eax, edi
       mov      rcx, 0x1000000000000
       or       rax, rcx
 
G_M000_IG03:                ;; offset=0x000F
       ret      
 
; Total bytes of code 16

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,uint):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 64
       lea      rbp, [rsp+0x40]
       xor      eax, eax
       mov      qword ptr [rbp-0x38], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x30], xmm8
       mov      qword ptr [rbp-0x20], rax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x0029
       mov      rdi, bword ptr [rbp-0x08]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x003C
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x30], rax
       mov      rsi, gword ptr [rbp-0x30]
       mov      rdi, gword ptr [rbp-0x28]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x28]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x0080
       mov      rdi, bword ptr [rbp-0x08]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0093
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x38], rax
       mov      rsi, gword ptr [rbp-0x38]
       mov      rdi, gword ptr [rbp-0x20]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x20]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x00D7
       mov      rdi, bword ptr [rbp-0x08]
       mov      rsi, bword ptr [rbp-0x10]
       mov      edx, dword ptr [rbp-0x14]
       call     [Tl.FusionExperiment.FusedPulse:ForwardOne(byref,byref,uint):Tl.Playback]
       nop      
 
G_M000_IG07:                ;; offset=0x00E9
       add      rsp, 64
       pop      rbp
       ret      
 
; Total bytes of code 239

; Assembly listing for method Tl.FusionExperiment.FusedPulse:ForwardOne(byref,byref,uint):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 112
       lea      rbp, [rsp+0x70]
       xor      eax, eax
       mov      qword ptr [rbp-0x68], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       mov      qword ptr [rbp-0x20], rax
       mov      dword ptr [rbp-0x18], eax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x0031
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x18], edx
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x1C], edx
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       cmp      eax, dword ptr [rbp-0x14]
       ja       SHORT G_M000_IG03
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x6C], eax
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       sub      eax, dword ptr [rbp-0x6C]
       mov      dword ptr [rbp-0x20], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0083
       mov      rdi, 0x7FC9271FA2D8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x1C]
       cmp      eax, dword ptr [rbp-0x18]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x20], eax
 
G_M000_IG04:                ;; offset=0x00A1
       mov      eax, dword ptr [rbp-0x20]
       mov      rcx, bword ptr [rbp-0x08]
       movzx    rcx, word  ptr [rcx+0x04]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00BC
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x68], rax
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdx, gword ptr [rbp-0x68]
       mov      rdi, gword ptr [rbp-0x58]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x58]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x011D
       mov      rax, bword ptr [rbp-0x08]
       movzx    rax, word  ptr [rax+0x04]
       add      eax, dword ptr [rbp-0x20]
       movzx    rax, ax
       mov      dword ptr [rbp-0x24], eax
       mov      dword ptr [rbp-0x28], 1
       cmp      dword ptr [rbp-0x1C], 599
       jne      SHORT G_M000_IG07
       mov      rdi, 0x7FC9271FA2DC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x28]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x28], eax
 
G_M000_IG07:                ;; offset=0x0159
       cmp      dword ptr [rbp-0x1C], 47
       jae      G_M000_IG13
       cmp      dword ptr [rbp-0x1C], 11
       jae      G_M000_IG10
       cmp      dword ptr [rbp-0x1C], 3
       jae      SHORT G_M000_IG08
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG08:                ;; offset=0x0190
       cmp      dword ptr [rbp-0x1C], 7
       jae      G_M000_IG09
       mov      rdi, 0x7FC9271FA2E0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x2C], xmm0
       mov      dword ptr [rbp-0x30], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x30]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x2C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x34], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x34]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG09:                ;; offset=0x022F
       mov      rdi, 0x7FC9271FA2E4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG10:                ;; offset=0x0273
       cmp      dword ptr [rbp-0x1C], 18
       jb       SHORT G_M000_IG11
       cmp      dword ptr [rbp-0x1C], 29
       jae      SHORT G_M000_IG12
       mov      rdi, 0x7FC9271FA2E8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG11:                ;; offset=0x02C3
       mov      rdi, 0x7FC9271FA2EC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG19
 
G_M000_IG12:                ;; offset=0x02D7
       mov      rdi, 0x7FC9271FA2F0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0x38], xmm0
       mov      dword ptr [rbp-0x3C], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0x3C]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x38]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0x40], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x40]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG13:                ;; offset=0x033C
       cmp      dword ptr [rbp-0x1C], 200
       jae      G_M000_IG16
       cmp      dword ptr [rbp-0x1C], 76
       jae      SHORT G_M000_IG14
       mov      rdi, 0x7FC9271FA2F4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG14:                ;; offset=0x0393
       cmp      dword ptr [rbp-0x1C], 123
       jae      G_M000_IG15
       mov      rdi, 0x7FC9271FA2F8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x1C]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0x44], xmm0
       mov      dword ptr [rbp-0x48], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0x48]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x44]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0x4C], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x4C]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG15:                ;; offset=0x041A
       mov      rdi, 0x7FC9271FA2FC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG16:                ;; offset=0x0446
       cmp      dword ptr [rbp-0x1C], 515
       jae      G_M000_IG18
       cmp      dword ptr [rbp-0x1C], 321
       jae      SHORT G_M000_IG17
       mov      rdi, 0x7FC9271FA300
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG19
 
G_M000_IG17:                ;; offset=0x04A0
       mov      rdi, 0x7FC9271FA304
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x04F9
       cmp      dword ptr [rbp-0x1C], 600
       jae      SHORT G_M000_IG21
       mov      rdi, 0x7FC9271FA308
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG19:                ;; offset=0x0529
       mov      rdi, 0x7FC9271FA30C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x14]
       mov      esi, dword ptr [rbp-0x24]
       mov      edx, dword ptr [rbp-0x28]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG20:                ;; offset=0x0548
       add      rsp, 112
       pop      rbp
       ret      
 
G_M000_IG21:                ;; offset=0x054E
       mov      rdi, 0x7FC9271FA310
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG19
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	40400000h		;         3
RWD08  	dd	40000000h		;         2
RWD12  	dd	40A00000h		;         5
RWD16  	dd	41000000h		;         8
RWD20  	dd	41880000h		;        17
RWD24  	dd	41500000h		;        13
RWD28  	dd	42380000h		;        46
RWD32  	dd	42080000h		;        34

; Total bytes of code 1375

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedSingle():Tl.FusionExperiment.SumReceipt:this (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 112
       lea      rbp, [rsp+0x70]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:                ;; offset=0x0018
       mov      dword ptr [rbp-0x68], 0x3E8
       xor      eax, eax
       mov      dword ptr [rbp-0x34], eax
       xor      edi, edi
       call     [Tl.FusionExperiment.FusedPulse:Start(uint):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      rax, gword ptr [rax+0x08]
       mov      gword ptr [rbp-0x48], rax
       xor      eax, eax
       mov      dword ptr [rbp-0x4C], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0043
       mov      rdi, 0x7FC9271F3F30
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, gword ptr [rbp-0x48]
       mov      ecx, dword ptr [rbp-0x4C]
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG08
       mov      edx, ecx
       lea      rax, bword ptr [rax+4*rdx+0x10]
       mov      eax, dword ptr [rax]
       mov      dword ptr [rbp-0x50], eax
       lea      rsi, [rbp-0x34]
       lea      rdi, [rbp-0x40]
       mov      edx, dword ptr [rbp-0x50]
       call     [Tl.FusionExperiment.FusedPulse:Forward(byref,byref,uint):Tl.Playback]
       mov      qword ptr [rbp-0x40], rax
       mov      eax, dword ptr [rbp-0x4C]
       inc      eax
       mov      dword ptr [rbp-0x4C], eax
 
G_M000_IG04:                ;; offset=0x008B
       mov      eax, dword ptr [rbp-0x68]
       dec      eax
       mov      dword ptr [rbp-0x68], eax
       cmp      dword ptr [rbp-0x68], 0
       jg       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x0099
       lea      rdi, [rbp-0x68]
       mov      esi, 45
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG06:                ;; offset=0x00A7
       mov      rax, gword ptr [rbp-0x48]
       mov      eax, dword ptr [rax+0x08]
       cmp      eax, dword ptr [rbp-0x4C]
       jg       SHORT G_M000_IG03
       mov      rdi, 0x7FC9271F3F34
       call     CORINFO_HELP_COUNTPROFILE32
       lea      rdi, [rbp-0x40]
       vmovss   xmm0, dword ptr [rbp-0x34]
       call     [Tl.FusionExperiment.SumReceipt:Capture(byref,float):Tl.FusionExperiment.SumReceipt]
       mov      qword ptr [rbp-0x60], rax
       mov      dword ptr [rbp-0x58], edx
       mov      rax, qword ptr [rbp-0x60]
       mov      edx, dword ptr [rbp-0x58]
 
G_M000_IG07:                ;; offset=0x00DF
       add      rsp, 112
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x00E5
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 235

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedSingle():Tl.FusionExperiment.SumReceipt:this (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x2d
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 1 inlinees with PGO data; 7 single block inlinees; 1 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0x98], r15
       mov      qword ptr [rsp+0x90], r14
       mov      qword ptr [rsp+0x88], rbx
       lea      rbp, [rsp+0x20]
       vmovss   xmm0, dword ptr [rbp+0x4C]
       mov      rdi, gword ptr [rbp+0x38]
       mov      eax, dword ptr [rbp+0x34]
 
G_M000_IG02:                ;; offset=0x0032
       mov      ebx, dword ptr [rbp+0x40]
       movzx    r15, word  ptr [rbp+0x44]
       movzx    r14, word  ptr [rbp+0x46]
       mov      ecx, dword ptr [rdi+0x08]
       mov      eax, eax
       cmp      ecx, eax
       jg       G_M000_IG27
 
G_M000_IG03:                ;; offset=0x004C
       vmovd    edx, xmm0
       mov      eax, r15d
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       mov      ecx, r14d
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0066
       add      rsp, 136
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0074
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0074
       cmp      r9d, r8d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG29
 
G_M000_IG07:                ;; offset=0x0084
       mov      r8d, 5
       jmp      G_M000_IG30
 
G_M000_IG08:                ;; offset=0x008F
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG25
 
G_M000_IG09:                ;; offset=0x009C
       cmp      r9d, 600
       jae      G_M000_IG26
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      G_M000_IG26
 
G_M000_IG10:                ;; offset=0x00B6
       cmp      r9d, 76
       jb       SHORT G_M000_IG14
 
G_M000_IG11:                ;; offset=0x00BC
       cmp      r9d, 123
       jb       SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x00C2
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG26
 
G_M000_IG13:                ;; offset=0x00CF
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       add      r9d, -76
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG26
 
G_M000_IG14:                ;; offset=0x0105
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       jmp      SHORT G_M000_IG12
 
G_M000_IG15:                ;; offset=0x010F
       cmp      r9d, 11
       jb       SHORT G_M000_IG19
 
G_M000_IG16:                ;; offset=0x0115
       cmp      r9d, 18
       jb       G_M000_IG26
 
G_M000_IG17:                ;; offset=0x011F
       cmp      r9d, 29
       jb       SHORT G_M000_IG24
 
G_M000_IG18:                ;; offset=0x0125
       add      r9d, -29
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, xmm1
       jmp      SHORT G_M000_IG26
 
G_M000_IG19:                ;; offset=0x0150
       cmp      r9d, 3
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x0156
       cmp      r9d, 7
       jb       SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x015C
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      SHORT G_M000_IG25
 
G_M000_IG22:                ;; offset=0x0166
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       add      r9d, -3
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm1
       jmp      SHORT G_M000_IG25
 
G_M000_IG23:                ;; offset=0x0191
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      SHORT G_M000_IG26
 
G_M000_IG24:                ;; offset=0x019B
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
 
G_M000_IG25:                ;; offset=0x01A3
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
 
G_M000_IG26:                ;; offset=0x01AB
       mov      edx, edx
       mov      esi, esi
       shl      rsi, 32
       or       rdx, rsi
       mov      esi, r8d
       shl      rsi, 48
       or       rdx, rsi
       mov      qword ptr [rbp-0x20], rdx
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       inc      eax
       cmp      ecx, eax
       jle      G_M000_IG03
 
G_M000_IG27:                ;; offset=0x01DB
       cmp      eax, ecx
       jae      G_M000_IG38
       mov      edx, dword ptr [rdi+4*rax+0x10]
       test     r14b, 1
       je       G_M000_IG35
       test     r14b, 2
       jne      G_M000_IG36
       mov      esi, ebx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     esi, esi, 600
       mov      r8d, ebx
       sub      r8d, esi
       mov      esi, edx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     esi, esi, 600
       mov      r9d, edx
       sub      r9d, esi
       cmp      edx, ebx
       jb       G_M000_IG06
 
G_M000_IG28:                ;; offset=0x0235
       mov      r8d, edx
       imul     rsi, r8, 0x1B4E81B5
       shr      rsi, 38
       mov      r8d, ebx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       sub      esi, r8d
 
G_M000_IG29:                ;; offset=0x0254
       mov      r8d, r15d
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   r8, r8d
       mov      r10d, esi
       cmp      r8, r10
       jl       G_M000_IG37
       add      esi, r15d
       movzx    rsi, si
       mov      r8d, 1
       cmp      r9d, 599
       je       G_M000_IG07
 
G_M000_IG30:                ;; offset=0x0289
       cmp      r9d, 47
       jb       G_M000_IG15
 
G_M000_IG31:                ;; offset=0x0293
       cmp      r9d, 200
       jb       G_M000_IG10
 
G_M000_IG32:                ;; offset=0x02A0
       cmp      r9d, 515
       jae      G_M000_IG09
 
G_M000_IG33:                ;; offset=0x02AD
       cmp      r9d, 321
       jb       G_M000_IG08
 
G_M000_IG34:                ;; offset=0x02BA
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      G_M000_IG24
 
G_M000_IG35:                ;; offset=0x02C7
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG36:                ;; offset=0x0303
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x033F
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG38:                ;; offset=0x0396
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
RWD00  	dd	40000000h		;         2
RWD04  	dd	40400000h		;         3
RWD08  	dd	41000000h		;         8
RWD12  	dd	42380000h		;        46
RWD16  	dd	41A80000h		;        21
RWD20  	dd	42080000h		;        34
RWD24  	dd	41500000h		;        13
RWD28  	dd	41880000h		;        17
RWD32  	dd	3F800000h		;         1
RWD36  	dd	40A00000h		;         5

; Total bytes of code 924

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,uint):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data
; 1 inlinees with PGO data; 3 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     rbx
       lea      rbp, [rsp+0x10]
 
G_M000_IG02:                ;; offset=0x0009
       movzx    rax, word  ptr [rdi+0x06]
       test     al, 1
       je       G_M000_IG27
       test     al, 2
       jne      G_M000_IG28
       mov      eax, dword ptr [rdi]
       mov      ecx, eax
       imul     rcx, rcx, 0x1B4E81B5
       shr      rcx, 38
       imul     ecx, ecx, 600
       mov      r8d, eax
       sub      r8d, ecx
       mov      ecx, edx
       imul     rcx, rcx, 0x1B4E81B5
       shr      rcx, 38
       imul     ecx, ecx, 600
       mov      r9d, edx
       sub      r9d, ecx
       cmp      edx, eax
       jb       G_M000_IG13
 
G_M000_IG03:                ;; offset=0x0059
       mov      r8d, edx
       imul     rcx, r8, 0x1B4E81B5
       shr      rcx, 38
       mov      eax, eax
       imul     rax, rax, 0x1B4E81B5
       shr      rax, 38
       sub      ecx, eax
 
G_M000_IG04:                ;; offset=0x0076
       movzx    rax, word  ptr [rdi+0x04]
       mov      edi, eax
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      r8d, ecx
       cmp      rdi, r8
       jl       G_M000_IG29
       add      eax, ecx
       movzx    rax, ax
       mov      edi, 1
       mov      ecx, 5
       cmp      r9d, 599
       cmove    edi, ecx
       cmp      r9d, 47
       jb       G_M000_IG21
 
G_M000_IG05:                ;; offset=0x00B6
       cmp      r9d, 200
       jb       G_M000_IG16
 
G_M000_IG06:                ;; offset=0x00C3
       cmp      r9d, 515
       jae      SHORT G_M000_IG15
 
G_M000_IG07:                ;; offset=0x00CC
       cmp      r9d, 321
       jb       SHORT G_M000_IG14
 
G_M000_IG08:                ;; offset=0x00D5
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG09:                ;; offset=0x00E5
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG10:                ;; offset=0x00F5
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG11:                ;; offset=0x0105
       mov      ecx, edx
       mov      eax, eax
       shl      rax, 32
       or       rax, rcx
       mov      edi, edi
       shl      rdi, 48
       or       rax, rdi
 
G_M000_IG12:                ;; offset=0x0119
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG13:                ;; offset=0x011E
       cmp      r9d, r8d
       setb     cl
       movzx    rcx, cl
       jmp      G_M000_IG04
 
G_M000_IG14:                ;; offset=0x012C
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG10
 
G_M000_IG15:                ;; offset=0x013E
       cmp      r9d, 600
       jae      SHORT G_M000_IG11
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG11
 
G_M000_IG16:                ;; offset=0x0159
       cmp      r9d, 76
       jb       SHORT G_M000_IG20
 
G_M000_IG17:                ;; offset=0x015F
       cmp      r9d, 123
       jb       SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0165
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG11
 
G_M000_IG19:                ;; offset=0x0177
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
       add      r9d, -76
       mov      ecx, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG11
 
G_M000_IG20:                ;; offset=0x01BC
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG21:                ;; offset=0x01CE
       cmp      r9d, 11
       jb       SHORT G_M000_IG25
 
G_M000_IG22:                ;; offset=0x01D4
       cmp      r9d, 18
       jb       G_M000_IG11
 
G_M000_IG23:                ;; offset=0x01DE
       cmp      r9d, 29
       jb       G_M000_IG09
 
G_M000_IG24:                ;; offset=0x01E8
       add      r9d, -29
       mov      ecx, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG11
 
G_M000_IG25:                ;; offset=0x021D
       cmp      r9d, 3
       jb       G_M000_IG30
 
G_M000_IG26:                ;; offset=0x0227
       cmp      r9d, 7
       jae      G_M000_IG32
       jmp      G_M000_IG31
 
G_M000_IG27:                ;; offset=0x0236
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG28:                ;; offset=0x0272
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG29:                ;; offset=0x02AE
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG30:                ;; offset=0x0305
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG11
 
G_M000_IG31:                ;; offset=0x031A
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       lea      ecx, [r9-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG32:                ;; offset=0x0354
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41000000h		;         8
RWD08  	dd	40A00000h		;         5
RWD12  	dd	40000000h		;         2
RWD16  	dd	40400000h		;         3
RWD20  	dd	42380000h		;        46
RWD24  	dd	41A80000h		;        21
RWD28  	dd	42080000h		;        34
RWD32  	dd	41500000h		;        13
RWD36  	dd	41880000h		;        17

; Total bytes of code 873

; Assembly listing for method Tl.FusionExperiment.FusedPulse:ForwardOne(byref,byref,uint):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 11978
; 0 inlinees with PGO data; 1 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     rbx
       lea      rbp, [rsp+0x10]
 
G_M000_IG02:                ;; offset=0x0009
       mov      eax, dword ptr [rdi]
       mov      ecx, eax
       imul     rcx, rcx, 0x1B4E81B5
       shr      rcx, 38
       imul     ecx, ecx, 600
       mov      r8d, eax
       sub      r8d, ecx
       mov      ecx, edx
       imul     rcx, rcx, 0x1B4E81B5
       shr      rcx, 38
       imul     ecx, ecx, 600
       mov      r9d, edx
       sub      r9d, ecx
       cmp      edx, eax
       jb       G_M000_IG13
 
G_M000_IG03:                ;; offset=0x0045
       mov      r8d, edx
       imul     rcx, r8, 0x1B4E81B5
       shr      rcx, 38
       mov      eax, eax
       imul     rax, rax, 0x1B4E81B5
       shr      rax, 38
       sub      ecx, eax
 
G_M000_IG04:                ;; offset=0x0062
       movzx    rax, word  ptr [rdi+0x04]
       mov      edi, eax
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      r8d, ecx
       cmp      rdi, r8
       jl       G_M000_IG27
       add      eax, ecx
       movzx    rax, ax
       mov      edi, 1
       mov      ecx, 5
       cmp      r9d, 599
       cmove    edi, ecx
       cmp      r9d, 47
       jb       G_M000_IG21
 
G_M000_IG05:                ;; offset=0x00A2
       cmp      r9d, 200
       jb       G_M000_IG16
 
G_M000_IG06:                ;; offset=0x00AF
       cmp      r9d, 515
       jae      SHORT G_M000_IG15
 
G_M000_IG07:                ;; offset=0x00B8
       cmp      r9d, 321
       jb       SHORT G_M000_IG14
 
G_M000_IG08:                ;; offset=0x00C1
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG09:                ;; offset=0x00D1
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG10:                ;; offset=0x00E1
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG11:                ;; offset=0x00F1
       mov      ecx, edx
       mov      eax, eax
       shl      rax, 32
       or       rax, rcx
       mov      edi, edi
       shl      rdi, 48
       or       rax, rdi
 
G_M000_IG12:                ;; offset=0x0105
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG13:                ;; offset=0x010A
       cmp      r9d, r8d
       setb     cl
       movzx    rcx, cl
       jmp      G_M000_IG04
 
G_M000_IG14:                ;; offset=0x0118
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG10
 
G_M000_IG15:                ;; offset=0x012A
       cmp      r9d, 600
       jae      SHORT G_M000_IG11
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG11
 
G_M000_IG16:                ;; offset=0x0145
       cmp      r9d, 76
       jb       SHORT G_M000_IG20
 
G_M000_IG17:                ;; offset=0x014B
       cmp      r9d, 123
       jb       SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x0151
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG11
 
G_M000_IG19:                ;; offset=0x0163
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
       add      r9d, -76
       mov      ecx, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG11
 
G_M000_IG20:                ;; offset=0x01A8
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG18
 
G_M000_IG21:                ;; offset=0x01BA
       cmp      r9d, 11
       jb       SHORT G_M000_IG25
 
G_M000_IG22:                ;; offset=0x01C0
       cmp      r9d, 18
       jb       G_M000_IG11
 
G_M000_IG23:                ;; offset=0x01CA
       cmp      r9d, 29
       jb       G_M000_IG09
 
G_M000_IG24:                ;; offset=0x01D4
       add      r9d, -29
       mov      ecx, r9d
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG11
 
G_M000_IG25:                ;; offset=0x0209
       cmp      r9d, 3
       jb       SHORT G_M000_IG28
 
G_M000_IG26:                ;; offset=0x020F
       cmp      r9d, 7
       jae      G_M000_IG30
       jmp      SHORT G_M000_IG29
 
G_M000_IG27:                ;; offset=0x021B
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG28:                ;; offset=0x0272
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG11
 
G_M000_IG29:                ;; offset=0x0287
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       lea      ecx, [r9-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG30:                ;; offset=0x02C1
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41000000h		;         8
RWD08  	dd	40A00000h		;         5
RWD12  	dd	40000000h		;         2
RWD16  	dd	40400000h		;         3
RWD20  	dd	42380000h		;        46
RWD24  	dd	41A80000h		;        21
RWD28  	dd	42080000h		;        34
RWD32  	dd	41500000h		;        13
RWD36  	dd	41880000h		;        17

; Total bytes of code 726

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedSingle():Tl.FusionExperiment.SumReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 100
; 1 inlinees with PGO data; 7 single block inlinees; 1 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 16
       lea      rbp, [rsp+0x30]
 
G_M000_IG02:                ;; offset=0x0011
       vxorps   xmm0, xmm0, xmm0
       mov      rax, 0x1000000000000
       mov      qword ptr [rbp-0x28], rax
       mov      ebx, dword ptr [rbp-0x28]
       movzx    r15, word  ptr [rbp-0x24]
       movzx    r14, word  ptr [rbp-0x22]
       mov      rdi, gword ptr [rdi+0x08]
       mov      eax, dword ptr [rdi+0x08]
       mov      ecx, 16
       inc      eax
       jmp      G_M000_IG17
 
G_M000_IG03:                ;; offset=0x0043
       mov      r8d, 5
       jmp      G_M000_IG09
 
G_M000_IG04:                ;; offset=0x004E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG15
 
G_M000_IG05:                ;; offset=0x005B
       cmp      r9d, 600
       jae      G_M000_IG16
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      G_M000_IG16
 
G_M000_IG06:                ;; offset=0x0075
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       jmp      G_M000_IG22
 
G_M000_IG07:                ;; offset=0x0082
       mov      r8d, edx
       imul     rsi, r8, 0x1B4E81B5
       shr      rsi, 38
       mov      r8d, ebx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       sub      esi, r8d
 
G_M000_IG08:                ;; offset=0x00A1
       movzx    r13, r15w
       mov      r8d, r13d
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   r8, r8d
       mov      r10d, esi
       cmp      r8, r10
       jl       G_M000_IG37
       add      esi, r13d
       movzx    rsi, si
       mov      r8d, 1
       cmp      r9d, 599
       je       G_M000_IG03
 
G_M000_IG09:                ;; offset=0x00DA
       cmp      r9d, 47
       jb       G_M000_IG24
 
G_M000_IG10:                ;; offset=0x00E4
       cmp      r9d, 200
       jb       G_M000_IG20
 
G_M000_IG11:                ;; offset=0x00F1
       cmp      r9d, 515
       jae      G_M000_IG05
 
G_M000_IG12:                ;; offset=0x00FE
       cmp      r9d, 321
       jb       G_M000_IG04
 
G_M000_IG13:                ;; offset=0x010B
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
 
G_M000_IG14:                ;; offset=0x0113
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
 
G_M000_IG15:                ;; offset=0x011B
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD20]
 
G_M000_IG16:                ;; offset=0x0123
       mov      edx, edx
       mov      esi, esi
       shl      rsi, 32
       or       rdx, rsi
       mov      esi, r8d
       shl      rsi, 48
       or       rdx, rsi
       mov      qword ptr [rbp-0x30], rdx
       mov      ebx, dword ptr [rbp-0x30]
       movzx    r15, word  ptr [rbp-0x2C]
       movzx    r14, word  ptr [rbp-0x2A]
       add      rcx, 4
 
G_M000_IG17:                ;; offset=0x014D
       dec      eax
       je       G_M000_IG33
 
G_M000_IG18:                ;; offset=0x0155
       mov      edx, dword ptr [rdi+rcx]
       movzx    r13, r14w
       test     r13b, 1
       je       G_M000_IG35
       test     r13b, 2
       jne      G_M000_IG36
       mov      esi, ebx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     esi, esi, 600
       mov      r8d, ebx
       sub      r8d, esi
       mov      esi, edx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     esi, esi, 600
       mov      r9d, edx
       sub      r9d, esi
       cmp      edx, ebx
       jae      G_M000_IG07
 
G_M000_IG19:                ;; offset=0x01AA
       cmp      r9d, r8d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG08
 
G_M000_IG20:                ;; offset=0x01BA
       cmp      r9d, 76
       jb       G_M000_IG06
 
G_M000_IG21:                ;; offset=0x01C4
       cmp      r9d, 123
       jb       SHORT G_M000_IG23
 
G_M000_IG22:                ;; offset=0x01CA
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG16
 
G_M000_IG23:                ;; offset=0x01D7
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       add      r9d, -76
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG16
 
G_M000_IG24:                ;; offset=0x020D
       cmp      r9d, 11
       jb       SHORT G_M000_IG28
 
G_M000_IG25:                ;; offset=0x0213
       cmp      r9d, 18
       jb       G_M000_IG16
 
G_M000_IG26:                ;; offset=0x021D
       cmp      r9d, 29
       jb       G_M000_IG14
 
G_M000_IG27:                ;; offset=0x0227
       add      r9d, -29
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG16
 
G_M000_IG28:                ;; offset=0x0255
       cmp      r9d, 3
       jb       SHORT G_M000_IG32
 
G_M000_IG29:                ;; offset=0x025B
       cmp      r9d, 7
       jb       SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x0261
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      G_M000_IG15
 
G_M000_IG31:                ;; offset=0x026E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       add      r9d, -3
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG15
 
G_M000_IG32:                ;; offset=0x029C
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      G_M000_IG16
 
G_M000_IG33:                ;; offset=0x02A9
       movzx    r13, r15w
       mov      eax, r13d
       movzx    r13, r14w
       vmovd    edx, xmm0
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       mov      ecx, r13d
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG34:                ;; offset=0x02CB
       add      rsp, 16
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG35:                ;; offset=0x02D8
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 837
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG36:                ;; offset=0x0314
       mov      rdi, 0x7FC926CE3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 957
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x0350
       mov      rdi, 0x7FC926F2A730
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 999
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      r13, rax
       mov      edi, 0x3F3
       mov      rsi, 0x7FC926B41A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r13
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
RWD00  	dd	40000000h		;         2
RWD04  	dd	40400000h		;         3
RWD08  	dd	41500000h		;        13
RWD12  	dd	3F800000h		;         1
RWD16  	dd	41000000h		;         8
RWD20  	dd	40A00000h		;         5
RWD24  	dd	42380000h		;        46
RWD28  	dd	41A80000h		;        21
RWD32  	dd	42080000h		;        34
RWD36  	dd	41880000h		;        17

; Total bytes of code 935


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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
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
       sub      rsp, 288
       lea      rbp, [rsp+0x120]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x120], xmm8
       mov      rax, -192
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
 
G_M000_IG02:                ;; offset=0x0053
       mov      dword ptr [rbp-0x100], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0070
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x108], rax
       mov      rsi, gword ptr [rbp-0x108]
       mov      rdi, gword ptr [rbp-0xF8]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00C3
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00D6
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x110], rax
       mov      rsi, gword ptr [rbp-0x110]
       mov      rdi, gword ptr [rbp-0xF0]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0129
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7FCBEA19E2B0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x014D
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x0156
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
 
G_M000_IG09:                ;; offset=0x0188
       vmovdqu  xmm0, xmmword ptr [rbp-0x48]
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
 
G_M000_IG10:                ;; offset=0x0192
       xor      eax, eax
       mov      dword ptr [rbp-0x74], eax
       jmp      G_M000_IG28
 
G_M000_IG11:                ;; offset=0x019C
       mov      eax, dword ptr [rbp-0x68]
       cmp      dword ptr [rbp-0x74], eax
       jae      G_M000_IG34
       mov      eax, dword ptr [rbp-0x74]
       mov      rcx, bword ptr [rbp-0x70]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xA8], eax
       mov      eax, dword ptr [rbp-0xA8]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x78], edx
       mov      eax, dword ptr [rbp-0xA8]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x7C], eax
       mov      eax, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xA8]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG12
       mov      eax, dword ptr [rbp-0x7C]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xAC]
       mov      dword ptr [rbp-0xB0], eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x020A
       mov      rdi, 0x7FCBEA19E2B4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x78]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xAC]
       mov      dword ptr [rbp-0xB0], eax
 
G_M000_IG13:                ;; offset=0x0234
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x80]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x0256
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xE8], rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x118], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x120], rax
       mov      rsi, gword ptr [rbp-0x118]
       mov      rdx, gword ptr [rbp-0x120]
       mov      rdi, gword ptr [rbp-0xE8]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0xE8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG15:                ;; offset=0x02CC
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x80]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      dword ptr [rbp-0xB8], eax
       cmp      dword ptr [rbp-0x78], 47
       jae      G_M000_IG21
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0x78], 11
       jae      G_M000_IG18
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0xDC], eax
       cmp      dword ptr [rbp-0x78], 3
       jae      SHORT G_M000_IG16
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG16:                ;; offset=0x033F
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0xE0], eax
       cmp      dword ptr [rbp-0x78], 7
       jae      G_M000_IG17
       mov      rdi, 0x7FCBEA19E2B8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x84], xmm0
       mov      dword ptr [rbp-0x88], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x88]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x84]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x8C], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x8C]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG17:                ;; offset=0x0408
       mov      rdi, 0x7FCBEA19E2BC
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG18:                ;; offset=0x0458
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0xC4], eax
       cmp      dword ptr [rbp-0x78], 18
       jb       SHORT G_M000_IG19
       mov      eax, dword ptr [rbp-0xC4]
       mov      dword ptr [rbp-0xD8], eax
       cmp      dword ptr [rbp-0x78], 29
       jae      SHORT G_M000_IG20
       mov      rdi, 0x7FCBEA19E2C0
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG19:                ;; offset=0x04CC
       mov      rdi, 0x7FCBEA19E2C4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG20:                ;; offset=0x04E0
       mov      rdi, 0x7FCBEA19E2C8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0x90], xmm0
       mov      dword ptr [rbp-0x94], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0x94]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x90]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0x98], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x98]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG21:                ;; offset=0x0563
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0x78], 200
       jae      G_M000_IG24
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xCC], eax
       cmp      dword ptr [rbp-0x78], 76
       jae      SHORT G_M000_IG22
       mov      rdi, 0x7FCBEA19E2CC
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG22:                ;; offset=0x05DE
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0xD0], eax
       cmp      dword ptr [rbp-0x78], 123
       jae      G_M000_IG23
       mov      rdi, 0x7FCBEA19E2D0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0x9C], xmm0
       mov      dword ptr [rbp-0xA0], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0xA0]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x9C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0xA4], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0xA4]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG23:                ;; offset=0x068F
       mov      rdi, 0x7FCBEA19E2D4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG24:                ;; offset=0x06C7
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xC0], eax
       cmp      dword ptr [rbp-0x78], 515
       jae      G_M000_IG26
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xC8], eax
       cmp      dword ptr [rbp-0x78], 321
       jae      SHORT G_M000_IG25
       mov      rdi, 0x7FCBEA19E2D8
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG25:                ;; offset=0x0745
       mov      rdi, 0x7FCBEA19E2DC
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x07AA
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xC4], eax
       cmp      dword ptr [rbp-0x78], 600
       jae      G_M000_IG33
       mov      rdi, 0x7FCBEA19E2E0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG27:                ;; offset=0x07EA
       mov      rdi, 0x7FCBEA19E2E4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0xC4]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x74]
       inc      eax
       mov      dword ptr [rbp-0x74], eax
 
G_M000_IG28:                ;; offset=0x0816
       mov      eax, dword ptr [rbp-0x100]
       dec      eax
       mov      dword ptr [rbp-0x100], eax
       cmp      dword ptr [rbp-0x100], 0
       jg       SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x082D
       lea      rdi, [rbp-0x100]
       mov      esi, 613
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG30:                ;; offset=0x083E
       mov      eax, dword ptr [rbp-0x74]
       cmp      eax, dword ptr [rbp-0x68]
       jl       G_M000_IG11
       mov      dword ptr [rbp-0x5C], 1
       cmp      dword ptr [rbp-0x54], 599
       jne      SHORT G_M000_IG31
       mov      rdi, 0x7FCBEA19E2E8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x5C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x5C], eax
 
G_M000_IG31:                ;; offset=0x0875
       mov      rdi, 0x7FCBEA19E2EC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x5C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG32:                ;; offset=0x0894
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG33:                ;; offset=0x089D
       mov      rdi, 0x7FCBEA19E2F0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG34:                ;; offset=0x08B1
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

; Total bytes of code 2231

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x265
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
       mov      qword ptr [rsp+0x138], r15
       mov      qword ptr [rsp+0x130], rbx
       lea      rbp, [rsp+0x10]
       mov      rdi, bword ptr [rbp+0xF8]
       mov      ecx, dword ptr [rbp+0xE4]
       mov      eax, dword ptr [rbp+0xE0]
       mov      esi, dword ptr [rbp+0xDC]
       mov      edx, dword ptr [rbp+0xD8]
       mov      r8d, dword ptr [rbp+0xBC]
 
G_M000_IG02:                ;; offset=0x0044
       mov      r9, bword ptr [rbp+0xC0]
       mov      r10d, dword ptr [rbp+0xC8]
       mov      r8d, r8d
       cmp      r8d, r10d
       jl       G_M000_IG10
 
G_M000_IG03:                ;; offset=0x005E
       mov      edx, 1
       mov      edi, 5
       cmp      esi, 599
       cmove    edx, edi
       mov      ecx, ecx
       mov      eax, eax
       shl      rax, 32
       or       rax, rcx
       mov      ecx, edx
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0085
       add      rsp, 304
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0091
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0091
       cmp      r15d, esi
       setb     cl
       movzx    rcx, cl
       jmp      G_M000_IG12
 
G_M000_IG07:                ;; offset=0x009F
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdi], xmm0
 
G_M000_IG08:                ;; offset=0x00BF
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdi], xmm0
 
G_M000_IG09:                ;; offset=0x00CF
       mov      ecx, r11d
       mov      esi, r15d
       mov      edx, ebx
       inc      r8d
       cmp      r8d, r10d
       jge      G_M000_IG03
 
G_M000_IG10:                ;; offset=0x00E3
       cmp      r8d, r10d
       jae      G_M000_IG35
       mov      r11d, dword ptr [r9+4*r8]
       mov      ebx, r11d
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       mov      r15d, r11d
       sub      r15d, ebx
       mov      ebx, r11d
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       cmp      r11d, ecx
       jb       G_M000_IG06
 
G_M000_IG11:                ;; offset=0x0121
       mov      ecx, ebx
       sub      ecx, edx
 
G_M000_IG12:                ;; offset=0x0125
       mov      esi, eax
       neg      esi
       add      esi, 0xFFFF
       movsxd   rdx, esi
       mov      esi, ecx
       cmp      rdx, rsi
       jl       G_M000_IG34
       add      eax, ecx
       movzx    rax, ax
       cmp      r15d, 47
       jb       G_M000_IG24
 
G_M000_IG13:                ;; offset=0x014C
       cmp      r15d, 200
       jb       SHORT G_M000_IG19
 
G_M000_IG14:                ;; offset=0x0155
       cmp      r15d, 515
       jae      SHORT G_M000_IG17
 
G_M000_IG15:                ;; offset=0x015E
       cmp      r15d, 321
       jae      G_M000_IG07
 
G_M000_IG16:                ;; offset=0x016B
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG08
 
G_M000_IG17:                ;; offset=0x0180
       cmp      r15d, 600
       jae      G_M000_IG09
 
G_M000_IG18:                ;; offset=0x018D
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG19:                ;; offset=0x01A2
       cmp      r15d, 76
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x01A8
       cmp      r15d, 123
       jb       SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x01AE
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG22:                ;; offset=0x01C3
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdi], xmm0
       lea      ecx, [r15-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [rdi]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG23:                ;; offset=0x0205
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG24:                ;; offset=0x022A
       cmp      r15d, 11
       jb       SHORT G_M000_IG29
 
G_M000_IG25:                ;; offset=0x0230
       cmp      r15d, 18
       jb       G_M000_IG09
 
G_M000_IG26:                ;; offset=0x023A
       cmp      r15d, 29
       jb       SHORT G_M000_IG28
 
G_M000_IG27:                ;; offset=0x0240
       lea      ecx, [r15-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [rdi]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG28:                ;; offset=0x0272
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rdi], xmm0
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG29:                ;; offset=0x0297
       cmp      r15d, 3
       jb       SHORT G_M000_IG33
 
G_M000_IG30:                ;; offset=0x029D
       cmp      r15d, 7
       jae      SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x02A3
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdi], xmm0
       lea      ecx, [r15-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rcx
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [rdi]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG08
 
G_M000_IG32:                ;; offset=0x02DD
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG08
 
G_M000_IG33:                ;; offset=0x02F2
       vmovss   xmm0, dword ptr [rdi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rdi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG34:                ;; offset=0x0307
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG35:                ;; offset=0x035E
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
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

; Total bytes of code 868

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Backward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 272
       lea      rbp, [rsp+0x110]
       xor      eax, eax
       mov      qword ptr [rbp-0x108], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x100], ymm8
       vmovdqu  ymmword ptr [rbp-0xE0], ymm8
       vmovdqu  ymmword ptr [rbp-0xC0], ymm8
       vmovdqu  ymmword ptr [rbp-0xA0], ymm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       vmovdqa  xmmword ptr [rbp-0x60], xmm8
       mov      qword ptr [rbp-0x50], rax
       mov      bword ptr [rbp-0x30], rdi
       mov      bword ptr [rbp-0x38], rsi
       mov      bword ptr [rbp-0x48], rdx
       mov      qword ptr [rbp-0x40], rcx
 
G_M000_IG02:                ;; offset=0x005C
       mov      dword ptr [rbp-0xF8], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0079
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x100], rax
       mov      rsi, gword ptr [rbp-0x100]
       mov      rdi, gword ptr [rbp-0xF0]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00CC
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00DF
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xE8], rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x108], rax
       mov      rsi, gword ptr [rbp-0x108]
       mov      rdi, gword ptr [rbp-0xE8]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xE8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0132
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7FCBEA1B7B98
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x0156
       add      rsp, 272
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x015F
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
 
G_M000_IG09:                ;; offset=0x0191
       vmovdqu  xmm0, xmmword ptr [rbp-0x48]
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
 
G_M000_IG10:                ;; offset=0x019B
       xor      eax, eax
       mov      dword ptr [rbp-0x74], eax
       jmp      G_M000_IG26
 
G_M000_IG11:                ;; offset=0x01A5
       mov      eax, dword ptr [rbp-0x68]
       cmp      dword ptr [rbp-0x74], eax
       jae      G_M000_IG32
       mov      eax, dword ptr [rbp-0x74]
       mov      rcx, bword ptr [rbp-0x70]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xA8], eax
       mov      eax, dword ptr [rbp-0xA8]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x78], edx
       mov      eax, dword ptr [rbp-0xA8]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x7C], eax
       mov      eax, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xA8]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG12
       mov      eax, dword ptr [rbp-0x58]
       sub      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xAC]
       mov      dword ptr [rbp-0xB0], eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0213
       mov      rdi, 0x7FCBEA1B7B9C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x78]
       cmp      eax, dword ptr [rbp-0x54]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xAC]
       mov      dword ptr [rbp-0xB0], eax
 
G_M000_IG13:                ;; offset=0x023D
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0x80]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0xB4], eax
       cmp      dword ptr [rbp-0x78], 47
       jae      G_M000_IG19
       mov      eax, dword ptr [rbp-0xB4]
       mov      dword ptr [rbp-0xD0], eax
       cmp      dword ptr [rbp-0x78], 11
       jae      G_M000_IG16
       mov      eax, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0xD8], eax
       cmp      dword ptr [rbp-0x78], 3
       jae      SHORT G_M000_IG14
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG14:                ;; offset=0x02BB
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0xDC], eax
       cmp      dword ptr [rbp-0x78], 7
       jae      G_M000_IG15
       mov      rdi, 0x7FCBEA1B7BA0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x84], xmm0
       mov      dword ptr [rbp-0x88], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x88]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x84]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x8C], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0x8C]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG15:                ;; offset=0x0384
       mov      rdi, 0x7FCBEA1B7BA4
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG16:                ;; offset=0x03D4
       mov      eax, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0xC0], eax
       cmp      dword ptr [rbp-0x78], 18
       jb       SHORT G_M000_IG17
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0x78], 29
       jae      SHORT G_M000_IG18
       mov      rdi, 0x7FCBEA1B7BA8
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG17:                ;; offset=0x0448
       mov      rdi, 0x7FCBEA1B7BAC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG25
 
G_M000_IG18:                ;; offset=0x045C
       mov      rdi, 0x7FCBEA1B7BB0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0x90], xmm0
       mov      dword ptr [rbp-0x94], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0x94]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x90]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0x98], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0x98]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG19:                ;; offset=0x04DF
       mov      eax, dword ptr [rbp-0xB4]
       mov      dword ptr [rbp-0xB8], eax
       cmp      dword ptr [rbp-0x78], 200
       jae      G_M000_IG22
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xC8], eax
       cmp      dword ptr [rbp-0x78], 76
       jae      SHORT G_M000_IG20
       mov      rdi, 0x7FCBEA1B7BB4
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG20:                ;; offset=0x055A
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xCC], eax
       cmp      dword ptr [rbp-0x78], 123
       jae      G_M000_IG21
       mov      rdi, 0x7FCBEA1B7BB8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0x9C], xmm0
       mov      dword ptr [rbp-0xA0], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0xA0]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x9C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0xA4], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [rbp-0xA4]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG21:                ;; offset=0x060B
       mov      rdi, 0x7FCBEA1B7BBC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG22:                ;; offset=0x0643
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0x78], 515
       jae      G_M000_IG24
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xC4], eax
       cmp      dword ptr [rbp-0x78], 321
       jae      SHORT G_M000_IG23
       mov      rdi, 0x7FCBEA1B7BC0
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xC4]
       mov      dword ptr [rbp-0xC0], eax
       jmp      G_M000_IG25
 
G_M000_IG23:                ;; offset=0x06C1
       mov      rdi, 0x7FCBEA1B7BC4
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xC4]
       mov      dword ptr [rbp-0xC0], eax
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x0726
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xC0], eax
       cmp      dword ptr [rbp-0x78], 600
       jae      G_M000_IG31
       mov      rdi, 0x7FCBEA1B7BC8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG25:                ;; offset=0x0766
       mov      rdi, 0x7FCBEA1B7BCC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x74]
       inc      eax
       mov      dword ptr [rbp-0x74], eax
 
G_M000_IG26:                ;; offset=0x0792
       mov      eax, dword ptr [rbp-0xF8]
       dec      eax
       mov      dword ptr [rbp-0xF8], eax
       cmp      dword ptr [rbp-0xF8], 0
       jg       SHORT G_M000_IG28
 
G_M000_IG27:                ;; offset=0x07A9
       lea      rdi, [rbp-0xF8]
       mov      esi, 590
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG28:                ;; offset=0x07BA
       mov      eax, dword ptr [rbp-0x74]
       cmp      eax, dword ptr [rbp-0x68]
       jl       G_M000_IG11
       mov      dword ptr [rbp-0x5C], 1
       cmp      dword ptr [rbp-0x54], 599
       jne      SHORT G_M000_IG29
       mov      rdi, 0x7FCBEA1B7BD0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x5C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x5C], eax
 
G_M000_IG29:                ;; offset=0x07F1
       mov      rdi, 0x7FCBEA1B7BD4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x5C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG30:                ;; offset=0x0810
       add      rsp, 272
       pop      rbp
       ret      
 
G_M000_IG31:                ;; offset=0x0819
       mov      rdi, 0x7FCBEA1B7BD8
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG25
 
G_M000_IG32:                ;; offset=0x082D
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

; Total bytes of code 2099

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Backward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x24e
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 7
; 1 inlinees with PGO data; 4 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 16
       mov      qword ptr [rsp+0x128], r15
       mov      qword ptr [rsp+0x120], rbx
       lea      rbp, [rsp+0x10]
       mov      rax, bword ptr [rbp+0xE8]
       mov      edx, dword ptr [rbp+0xD4]
       mov      ecx, dword ptr [rbp+0xD0]
       mov      edi, dword ptr [rbp+0xCC]
       mov      esi, dword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp+0xAC]
 
G_M000_IG02:                ;; offset=0x0044
       mov      r9, bword ptr [rbp+0xB0]
       mov      r10d, dword ptr [rbp+0xB8]
       mov      r8d, r8d
       cmp      r8d, r10d
       jl       G_M000_IG11
 
G_M000_IG03:                ;; offset=0x005E
       mov      eax, 1
       mov      esi, 5
       cmp      edi, 599
       cmove    eax, esi
       mov      edx, edx
       mov      ecx, ecx
       shl      rcx, 32
       or       rcx, rdx
       shl      rax, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0083
       add      rsp, 288
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x008F
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x008F
       mov      edx, esi
       sub      edx, ebx
       jmp      G_M000_IG13
 
G_M000_IG07:                ;; offset=0x0098
       mov      edi, edx
       jmp      G_M000_IG15
 
G_M000_IG08:                ;; offset=0x009F
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG09:                ;; offset=0x00BF
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG10:                ;; offset=0x00CF
       mov      edx, r11d
       mov      edi, r15d
       mov      esi, ebx
       inc      r8d
       cmp      r8d, r10d
       jge      G_M000_IG03
 
G_M000_IG11:                ;; offset=0x00E3
       cmp      r8d, r10d
       jae      G_M000_IG37
       mov      r11d, dword ptr [r9+4*r8]
       mov      ebx, r11d
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     ebx, ebx, 600
       mov      r15d, r11d
       sub      r15d, ebx
       mov      ebx, r11d
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       cmp      r11d, edx
       jbe      G_M000_IG06
 
G_M000_IG12:                ;; offset=0x0121
       cmp      r15d, edi
       seta     dl
       movzx    rdx, dl
 
G_M000_IG13:                ;; offset=0x012A
       cmp      ecx, edx
       ja       G_M000_IG07
 
G_M000_IG14:                ;; offset=0x0132
       mov      edi, ecx
 
G_M000_IG15:                ;; offset=0x0134
       sub      ecx, edi
       movzx    rcx, cx
       cmp      r15d, 47
       jb       G_M000_IG27
 
G_M000_IG16:                ;; offset=0x0143
       cmp      r15d, 200
       jb       SHORT G_M000_IG22
 
G_M000_IG17:                ;; offset=0x014C
       cmp      r15d, 515
       jae      SHORT G_M000_IG20
 
G_M000_IG18:                ;; offset=0x0155
       cmp      r15d, 321
       jae      G_M000_IG08
 
G_M000_IG19:                ;; offset=0x0162
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG09
 
G_M000_IG20:                ;; offset=0x0177
       cmp      r15d, 600
       jae      G_M000_IG10
 
G_M000_IG21:                ;; offset=0x0184
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG10
 
G_M000_IG22:                ;; offset=0x0199
       cmp      r15d, 76
       jb       SHORT G_M000_IG26
 
G_M000_IG23:                ;; offset=0x019F
       cmp      r15d, 123
       jb       SHORT G_M000_IG25
 
G_M000_IG24:                ;; offset=0x01A5
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG10
 
G_M000_IG25:                ;; offset=0x01BA
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       lea      esi, [r15-0x4C]
       mov      edx, esi
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG10
 
G_M000_IG26:                ;; offset=0x0202
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG10
 
G_M000_IG27:                ;; offset=0x0227
       cmp      r15d, 11
       jb       SHORT G_M000_IG32
 
G_M000_IG28:                ;; offset=0x022D
       cmp      r15d, 18
       jb       G_M000_IG10
 
G_M000_IG29:                ;; offset=0x0237
       cmp      r15d, 29
       jb       SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x023D
       vmovss   xmm0, dword ptr [rax]
       lea      edx, [r15-0x1D]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG10
 
G_M000_IG31:                ;; offset=0x0273
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG10
 
G_M000_IG32:                ;; offset=0x0298
       cmp      r15d, 3
       jb       SHORT G_M000_IG36
 
G_M000_IG33:                ;; offset=0x029E
       cmp      r15d, 7
       jae      SHORT G_M000_IG35
 
G_M000_IG34:                ;; offset=0x02A4
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rax], xmm0
       vmovss   xmm0, dword ptr [rax]
       lea      edx, [r15-0x03]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vsubss   xmm0, xmm0, xmm1
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG09
 
G_M000_IG35:                ;; offset=0x02E2
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG09
 
G_M000_IG36:                ;; offset=0x02F7
       vmovss   xmm0, dword ptr [rax]
       vsubss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rax], xmm0
       jmp      G_M000_IG10
 
G_M000_IG37:                ;; offset=0x030C
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
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

; Total bytes of code 786

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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x10], rax
       mov      edi, 926
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBEA1E3F30
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
       mov      rdi, 0x7FCBEA1E3F34
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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x02FF
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG38:                ;; offset=0x033B
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBEA1E3FE8
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
       mov      rdi, 0x7FCBEA1E3FEC
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
; fully interruptible
; with Synthesized PGO: fgCalledCount is 2
; 2 inlinees with PGO data; 11 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0xA8], r15
       mov      qword ptr [rsp+0xA0], r14
       mov      qword ptr [rsp+0x98], rbx
       lea      rbp, [rsp+0x20]
       mov      rdi, gword ptr [rbp+0x60]
       vmovss   xmm0, dword ptr [rbp+0x5C]
       mov      eax, dword ptr [rbp+0x4C]
 
G_M000_IG02:                ;; offset=0x0032
       mov      ebx, dword ptr [rbp+0x50]
       movzx    r15, word  ptr [rbp+0x54]
       movzx    r14, word  ptr [rbp+0x56]
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jg       G_M000_IG35
 
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
       add      rsp, 152
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0074
       mov      ecx, 5
       jmp      G_M000_IG34
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x007E
       mov      ebx, r11d
       sub      ebx, edx
 
G_M000_IG07:                ;; offset=0x0083
       mov      esi, r15d
       neg      esi
       add      esi, 0xFFFF
       movsxd   rdx, esi
       mov      esi, ebx
       cmp      rdx, rsi
       jl       G_M000_IG40
       add      r15d, ebx
       movzx    r15, r15w
       cmp      r14d, 47
       jb       G_M000_IG23
 
G_M000_IG08:                ;; offset=0x00AD
       cmp      r14d, 200
       jb       G_M000_IG18
 
G_M000_IG09:                ;; offset=0x00BA
       cmp      r14d, 515
       jae      G_M000_IG36
 
G_M000_IG10:                ;; offset=0x00C7
       cmp      r14d, 321
       jb       SHORT G_M000_IG17
 
G_M000_IG11:                ;; offset=0x00D0
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
 
G_M000_IG12:                ;; offset=0x00E0
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
 
G_M000_IG13:                ;; offset=0x00E8
       mov      ebx, r10d
       mov      esi, r14d
       mov      edx, r11d
       add      r8, 4
 
G_M000_IG14:                ;; offset=0x00F5
       dec      r9d
       je       G_M000_IG33
 
G_M000_IG15:                ;; offset=0x00FE
       mov      r10d, dword ptr [rcx+r8]
       mov      r11d, r10d
       imul     r11, r11, 0x1B4E81B5
       shr      r11, 38
       imul     r11d, r11d, 600
       mov      r14d, r10d
       sub      r14d, r11d
       mov      r11d, r10d
       imul     r11, r11, 0x1B4E81B5
       shr      r11, 38
       cmp      r10d, ebx
       jae      G_M000_IG06
 
G_M000_IG16:                ;; offset=0x0134
       cmp      r14d, esi
       setb     bl
       movzx    rbx, bl
       jmp      G_M000_IG07
 
G_M000_IG17:                ;; offset=0x0142
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      SHORT G_M000_IG12
 
G_M000_IG18:                ;; offset=0x014C
       cmp      r14d, 76
       jb       SHORT G_M000_IG22
 
G_M000_IG19:                ;; offset=0x0152
       cmp      r14d, 123
       jb       SHORT G_M000_IG21
 
G_M000_IG20:                ;; offset=0x0158
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      SHORT G_M000_IG13
 
G_M000_IG21:                ;; offset=0x0162
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       lea      edx, [r14-0x4C]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG13
 
G_M000_IG22:                ;; offset=0x0198
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      G_M000_IG13
 
G_M000_IG23:                ;; offset=0x01AD
       cmp      r14d, 11
       jb       SHORT G_M000_IG28
 
G_M000_IG24:                ;; offset=0x01B3
       cmp      r14d, 18
       jb       G_M000_IG13
 
G_M000_IG25:                ;; offset=0x01BD
       cmp      r14d, 29
       jb       SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x01C3
       lea      edx, [r14-0x1D]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG13
 
G_M000_IG27:                ;; offset=0x01F1
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       jmp      G_M000_IG13
 
G_M000_IG28:                ;; offset=0x0206
       cmp      r14d, 3
       jb       SHORT G_M000_IG32
 
G_M000_IG29:                ;; offset=0x020C
       cmp      r14d, 7
       jae      SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x0212
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       lea      edx, [r14-0x03]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG12
 
G_M000_IG31:                ;; offset=0x0240
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       jmp      G_M000_IG12
 
G_M000_IG32:                ;; offset=0x024D
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG13
 
G_M000_IG33:                ;; offset=0x025A
       mov      ecx, 1
       cmp      esi, 599
       je       G_M000_IG05
 
G_M000_IG34:                ;; offset=0x026B
       mov      edx, ebx
       mov      esi, r15d
       shl      rsi, 32
       or       rdx, rsi
       mov      ecx, ecx
       shl      rcx, 48
       or       rcx, rdx
       mov      qword ptr [rbp-0x20], rcx
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       add      eax, 8
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG35:                ;; offset=0x02A1
       test     rcx, rcx
       je       SHORT G_M000_IG37
       mov      edx, dword ptr [rcx+0x08]
       mov      esi, eax
       add      rsi, 8
       cmp      rdx, rsi
       jb       SHORT G_M000_IG37
       mov      edx, eax
       lea      rcx, bword ptr [rcx+4*rdx+0x10]
       test     r14b, 1
       je       SHORT G_M000_IG38
       test     r14b, 2
       jne      G_M000_IG39
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       imul     edx, edx, 600
       mov      esi, ebx
       sub      esi, edx
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       xor      r8d, r8d
       mov      r9d, 9
       jmp      G_M000_IG14
 
G_M000_IG36:                ;; offset=0x02FD
       cmp      r14d, 600
       jae      G_M000_IG13
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       jmp      G_M000_IG13
 
G_M000_IG37:                ;; offset=0x0317
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG38:                ;; offset=0x031E
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG39:                ;; offset=0x035A
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG40:                ;; offset=0x0396
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
RWD00  	dd	3F800000h		;         1
RWD04  	dd	41000000h		;         8
RWD08  	dd	40A00000h		;         5
RWD12  	dd	40000000h		;         2
RWD16  	dd	42380000h		;        46
RWD20  	dd	41A80000h		;        21
RWD24  	dd	42080000h		;        34
RWD28  	dd	41500000h		;        13
RWD32  	dd	41880000h		;        17
RWD36  	dd	40400000h		;         3

; Total bytes of code 1005

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
       sub      rsp, 288
       lea      rbp, [rsp+0x120]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x120], xmm8
       mov      rax, -192
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
 
G_M000_IG02:                ;; offset=0x0053
       mov      dword ptr [rbp-0x100], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0070
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x108], rax
       mov      rsi, gword ptr [rbp-0x108]
       mov      rdi, gword ptr [rbp-0xF8]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00C3
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00D6
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x110], rax
       mov      rsi, gword ptr [rbp-0x110]
       mov      rdi, gword ptr [rbp-0xF0]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0129
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7FCBEA19E2B0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x014D
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG08:                ;; offset=0x0156
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
 
G_M000_IG09:                ;; offset=0x0188
       vmovdqu  xmm0, xmmword ptr [rbp-0x48]
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
 
G_M000_IG10:                ;; offset=0x0192
       xor      eax, eax
       mov      dword ptr [rbp-0x74], eax
       jmp      G_M000_IG28
 
G_M000_IG11:                ;; offset=0x019C
       mov      eax, dword ptr [rbp-0x68]
       cmp      dword ptr [rbp-0x74], eax
       jae      G_M000_IG34
       mov      eax, dword ptr [rbp-0x74]
       mov      rcx, bword ptr [rbp-0x70]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xA8], eax
       mov      eax, dword ptr [rbp-0xA8]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x78], edx
       mov      eax, dword ptr [rbp-0xA8]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x7C], eax
       mov      eax, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0xAC], eax
       mov      eax, dword ptr [rbp-0xA8]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG12
       mov      eax, dword ptr [rbp-0x7C]
       sub      eax, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xAC]
       mov      dword ptr [rbp-0xB0], eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x020A
       mov      rdi, 0x7FCBEA19E2B4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x78]
       cmp      eax, dword ptr [rbp-0x54]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xAC]
       mov      dword ptr [rbp-0xB0], eax
 
G_M000_IG13:                ;; offset=0x0234
       mov      eax, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0x80]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x0256
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xE8], rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x118], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x120], rax
       mov      rsi, gword ptr [rbp-0x118]
       mov      rdx, gword ptr [rbp-0x120]
       mov      rdi, gword ptr [rbp-0xE8]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0xE8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG15:                ;; offset=0x02CC
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x80]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      dword ptr [rbp-0xB8], eax
       cmp      dword ptr [rbp-0x78], 47
       jae      G_M000_IG21
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xD4], eax
       cmp      dword ptr [rbp-0x78], 11
       jae      G_M000_IG18
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0xDC], eax
       cmp      dword ptr [rbp-0x78], 3
       jae      SHORT G_M000_IG16
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG16:                ;; offset=0x033F
       mov      eax, dword ptr [rbp-0xDC]
       mov      dword ptr [rbp-0xE0], eax
       cmp      dword ptr [rbp-0x78], 7
       jae      G_M000_IG17
       mov      rdi, 0x7FCBEA19E2B8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -3
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rbp-0x84], xmm0
       mov      dword ptr [rbp-0x88], 0x3F800000
       vmovss   xmm0, dword ptr [rbp-0x88]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x84]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rbp-0x8C], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x8C]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG17:                ;; offset=0x0408
       mov      rdi, 0x7FCBEA19E2BC
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG18:                ;; offset=0x0458
       mov      eax, dword ptr [rbp-0xD4]
       mov      dword ptr [rbp-0xC4], eax
       cmp      dword ptr [rbp-0x78], 18
       jb       SHORT G_M000_IG19
       mov      eax, dword ptr [rbp-0xC4]
       mov      dword ptr [rbp-0xD8], eax
       cmp      dword ptr [rbp-0x78], 29
       jae      SHORT G_M000_IG20
       mov      rdi, 0x7FCBEA19E2C0
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG19:                ;; offset=0x04CC
       mov      rdi, 0x7FCBEA19E2C4
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG20:                ;; offset=0x04E0
       mov      rdi, 0x7FCBEA19E2C8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -29
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmovss   dword ptr [rbp-0x90], xmm0
       mov      dword ptr [rbp-0x94], 0x41000000
       vmovss   xmm0, dword ptr [rbp-0x94]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x90]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vmovss   dword ptr [rbp-0x98], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0x98]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG21:                ;; offset=0x0563
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0x78], 200
       jae      G_M000_IG24
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xCC], eax
       cmp      dword ptr [rbp-0x78], 76
       jae      SHORT G_M000_IG22
       mov      rdi, 0x7FCBEA19E2CC
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG22:                ;; offset=0x05DE
       mov      eax, dword ptr [rbp-0xCC]
       mov      dword ptr [rbp-0xD0], eax
       cmp      dword ptr [rbp-0x78], 123
       jae      G_M000_IG23
       mov      rdi, 0x7FCBEA19E2D0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0x78]
       add      eax, -76
       mov      eax, eax
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rax
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vmovss   dword ptr [rbp-0x9C], xmm0
       mov      dword ptr [rbp-0xA0], 0x41A80000
       vmovss   xmm0, dword ptr [rbp-0xA0]
       vmulss   xmm0, xmm0, dword ptr [rbp-0x9C]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rbp-0xA4], xmm0
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [rbp-0xA4]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG23:                ;; offset=0x068F
       mov      rdi, 0x7FCBEA19E2D4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xD0]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG24:                ;; offset=0x06C7
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xC0], eax
       cmp      dword ptr [rbp-0x78], 515
       jae      G_M000_IG26
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xC8], eax
       cmp      dword ptr [rbp-0x78], 321
       jae      SHORT G_M000_IG25
       mov      rdi, 0x7FCBEA19E2D8
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      G_M000_IG27
 
G_M000_IG25:                ;; offset=0x0745
       mov      rdi, 0x7FCBEA19E2DC
       call     CORINFO_HELP_COUNTPROFILE32
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
       mov      eax, dword ptr [rbp-0xC8]
       mov      dword ptr [rbp-0xC4], eax
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:                ;; offset=0x07AA
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xC4], eax
       cmp      dword ptr [rbp-0x78], 600
       jae      G_M000_IG33
       mov      rdi, 0x7FCBEA19E2E0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG27:                ;; offset=0x07EA
       mov      rdi, 0x7FCBEA19E2E4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0xC4]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x74]
       inc      eax
       mov      dword ptr [rbp-0x74], eax
 
G_M000_IG28:                ;; offset=0x0816
       mov      eax, dword ptr [rbp-0x100]
       dec      eax
       mov      dword ptr [rbp-0x100], eax
       cmp      dword ptr [rbp-0x100], 0
       jg       SHORT G_M000_IG30
 
G_M000_IG29:                ;; offset=0x082D
       lea      rdi, [rbp-0x100]
       mov      esi, 613
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG30:                ;; offset=0x083E
       mov      eax, dword ptr [rbp-0x74]
       cmp      eax, dword ptr [rbp-0x68]
       jl       G_M000_IG11
       mov      dword ptr [rbp-0x5C], 1
       cmp      dword ptr [rbp-0x54], 599
       jne      SHORT G_M000_IG31
       mov      rdi, 0x7FCBEA19E2E8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x5C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x5C], eax
 
G_M000_IG31:                ;; offset=0x0875
       mov      rdi, 0x7FCBEA19E2EC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x5C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG32:                ;; offset=0x0894
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG33:                ;; offset=0x089D
       mov      rdi, 0x7FCBEA19E2F0
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG27
 
G_M000_IG34:                ;; offset=0x08B1
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

; Total bytes of code 2231

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
; with Synthesized PGO: fgCalledCount is 84339
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
       je       G_M000_IG34
       test     al, 2
       jne      G_M000_IG35
       test     ecx, ecx
       je       G_M000_IG36
 
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
       cmp      eax, ecx
       jl       G_M000_IG11
 
G_M000_IG04:                ;; offset=0x005F
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
 
G_M000_IG05:                ;; offset=0x0086
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x0091
       align    [0 bytes for IG07]
 
G_M000_IG07:                ;; offset=0x0091
       cmp      r10d, r14d
       setb     r11b
       movzx    r11, r11b
       jmp      G_M000_IG13
 
G_M000_IG08:                ;; offset=0x00A1
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG09:                ;; offset=0x00C1
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG10:                ;; offset=0x00D1
       mov      ebx, r8d
       mov      r14d, r10d
       mov      edi, r9d
       inc      eax
       cmp      eax, ecx
       jge      G_M000_IG04
 
G_M000_IG11:                ;; offset=0x00E4
       mov      r8d, dword ptr [rdx+4*rax]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r9d, r9d, 600
       mov      r10d, r8d
       sub      r10d, r9d
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       cmp      r8d, ebx
       jb       G_M000_IG07
 
G_M000_IG12:                ;; offset=0x011A
       mov      r11d, r9d
       sub      r11d, edi
 
G_M000_IG13:                ;; offset=0x0120
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r11d
       cmp      rdi, rbx
       jl       G_M000_IG38
       add      r11d, r15d
       movzx    r15, r11w
       cmp      r10d, 47
       jb       G_M000_IG24
 
G_M000_IG14:                ;; offset=0x014B
       cmp      r10d, 200
       jb       SHORT G_M000_IG19
 
G_M000_IG15:                ;; offset=0x0154
       cmp      r10d, 515
       jae      SHORT G_M000_IG18
 
G_M000_IG16:                ;; offset=0x015D
       cmp      r10d, 321
       jae      G_M000_IG08
 
G_M000_IG17:                ;; offset=0x016A
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG18:                ;; offset=0x017F
       cmp      r10d, 600
       jae      G_M000_IG10
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG19:                ;; offset=0x01A1
       cmp      r10d, 76
       jb       SHORT G_M000_IG23
 
G_M000_IG20:                ;; offset=0x01A7
       cmp      r10d, 123
       jb       SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x01AD
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG22:                ;; offset=0x01C2
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
       lea      edi, [r10-0x4C]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG23:                ;; offset=0x0204
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG24:                ;; offset=0x0229
       cmp      r10d, 11
       jb       SHORT G_M000_IG29
 
G_M000_IG25:                ;; offset=0x022F
       cmp      r10d, 18
       jb       G_M000_IG10
 
G_M000_IG26:                ;; offset=0x0239
       cmp      r10d, 29
       jb       SHORT G_M000_IG28
 
G_M000_IG27:                ;; offset=0x023F
       lea      edi, [r10-0x1D]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vmulss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG28:                ;; offset=0x0271
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vmovss   dword ptr [rsi], xmm0
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG29:                ;; offset=0x0296
       cmp      r10d, 3
       jb       SHORT G_M000_IG33
 
G_M000_IG30:                ;; offset=0x029C
       cmp      r10d, 7
       jb       SHORT G_M000_IG32
 
G_M000_IG31:                ;; offset=0x02A2
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG32:                ;; offset=0x02B7
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       lea      edi, [r10-0x03]
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2ss xmm0, xmm0, rdi
       vdivss   xmm0, xmm0, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, dword ptr [rsi]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG09
 
G_M000_IG33:                ;; offset=0x02F1
       vmovss   xmm0, dword ptr [rsi]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       vmovss   dword ptr [rsi], xmm0
       jmp      G_M000_IG10
 
G_M000_IG34:                ;; offset=0x0306
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG35:                ;; offset=0x0342
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG36:                ;; offset=0x037E
       mov      rax, qword ptr [rdi]
 
G_M000_IG37:                ;; offset=0x0381
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG38:                ;; offset=0x038C
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
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
RWD12  	dd	40000000h		;         2
RWD16  	dd	40400000h		;         3
RWD20  	dd	42380000h		;        46
RWD24  	dd	41A80000h		;        21
RWD28  	dd	42080000h		;        34
RWD32  	dd	41500000h		;        13
RWD36  	dd	41880000h		;        17

; Total bytes of code 995

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
       mov      rdi, 0x7FCBEA1E3FE8
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
       mov      rdi, 0x7FCBEA1E3FEC
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
; fully interruptible
; with Synthesized PGO: fgCalledCount is 3
; 2 inlinees with PGO data; 11 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0xA8], r15
       mov      qword ptr [rsp+0xA0], r14
       mov      qword ptr [rsp+0x98], rbx
       lea      rbp, [rsp+0x20]
       mov      rdi, gword ptr [rbp+0x60]
       vmovss   xmm0, dword ptr [rbp+0x5C]
       mov      eax, dword ptr [rbp+0x4C]
 
G_M000_IG02:                ;; offset=0x0032
       mov      ebx, dword ptr [rbp+0x50]
       movzx    r15, word  ptr [rbp+0x54]
       movzx    r14, word  ptr [rbp+0x56]
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jg       G_M000_IG24
 
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
       add      rsp, 152
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0074
       mov      ecx, 5
       jmp      G_M000_IG23
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x007E
       cmp      r14d, 600
       jae      G_M000_IG32
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG32
 
G_M000_IG07:                ;; offset=0x0098
       cmp      r14d, 76
       jb       SHORT G_M000_IG11
 
G_M000_IG08:                ;; offset=0x009E
       cmp      r14d, 123
       jb       SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00A4
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      G_M000_IG32
 
G_M000_IG10:                ;; offset=0x00B1
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       lea      edx, [r14-0x4C]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG32
 
G_M000_IG11:                ;; offset=0x00E7
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      G_M000_IG32
 
G_M000_IG12:                ;; offset=0x00FC
       cmp      r14d, 11
       jb       SHORT G_M000_IG17
 
G_M000_IG13:                ;; offset=0x0102
       cmp      r14d, 18
       jb       G_M000_IG32
 
G_M000_IG14:                ;; offset=0x010C
       cmp      r14d, 29
       jb       SHORT G_M000_IG16
 
G_M000_IG15:                ;; offset=0x0112
       lea      edx, [r14-0x1D]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG32
 
G_M000_IG16:                ;; offset=0x0140
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      G_M000_IG32
 
G_M000_IG17:                ;; offset=0x0155
       cmp      r14d, 3
       jb       SHORT G_M000_IG21
 
G_M000_IG18:                ;; offset=0x015B
       cmp      r14d, 7
       jb       SHORT G_M000_IG20
 
G_M000_IG19:                ;; offset=0x0161
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG31
 
G_M000_IG20:                ;; offset=0x016E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       lea      edx, [r14-0x03]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rdx
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG31
 
G_M000_IG21:                ;; offset=0x019C
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       jmp      G_M000_IG32
 
G_M000_IG22:                ;; offset=0x01A9
       mov      ecx, 1
       cmp      esi, 599
       je       G_M000_IG05
 
G_M000_IG23:                ;; offset=0x01BA
       mov      edx, ebx
       mov      esi, r15d
       shl      rsi, 32
       or       rdx, rsi
       mov      ecx, ecx
       shl      rcx, 48
       or       rcx, rdx
       mov      qword ptr [rbp-0x20], rcx
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       add      eax, 8
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG24:                ;; offset=0x01F0
       test     rcx, rcx
       je       G_M000_IG37
       mov      edx, dword ptr [rcx+0x08]
       mov      esi, eax
       add      rsi, 8
       cmp      rdx, rsi
       jb       G_M000_IG37
       mov      edx, eax
       lea      rcx, bword ptr [rcx+4*rdx+0x10]
       test     r14b, 1
       je       G_M000_IG38
       test     r14b, 2
       jne      G_M000_IG39
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       imul     edx, edx, 600
       mov      esi, ebx
       sub      esi, edx
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       xor      r8d, r8d
       mov      r9d, 9
       jmp      SHORT G_M000_IG33
 
G_M000_IG25:                ;; offset=0x0255
       mov      ebx, r11d
       sub      ebx, edx
 
G_M000_IG26:                ;; offset=0x025A
       mov      esi, r15d
       neg      esi
       add      esi, 0xFFFF
       movsxd   rdx, esi
       mov      esi, ebx
       cmp      rdx, rsi
       jl       G_M000_IG40
       add      r15d, ebx
       movzx    r15, r15w
       cmp      r14d, 47
       jb       G_M000_IG12
 
G_M000_IG27:                ;; offset=0x0284
       cmp      r14d, 200
       jb       G_M000_IG07
 
G_M000_IG28:                ;; offset=0x0291
       cmp      r14d, 515
       jae      G_M000_IG06
 
G_M000_IG29:                ;; offset=0x029E
       cmp      r14d, 321
       jb       SHORT G_M000_IG36
 
G_M000_IG30:                ;; offset=0x02A7
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
 
G_M000_IG31:                ;; offset=0x02B7
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
 
G_M000_IG32:                ;; offset=0x02BF
       mov      ebx, r10d
       mov      esi, r14d
       mov      edx, r11d
       add      r8, 4
 
G_M000_IG33:                ;; offset=0x02CC
       dec      r9d
       je       G_M000_IG22
 
G_M000_IG34:                ;; offset=0x02D5
       mov      r10d, dword ptr [rcx+r8]
       mov      r11d, r10d
       imul     r11, r11, 0x1B4E81B5
       shr      r11, 38
       imul     r11d, r11d, 600
       mov      r14d, r10d
       sub      r14d, r11d
       mov      r11d, r10d
       imul     r11, r11, 0x1B4E81B5
       shr      r11, 38
       cmp      r10d, ebx
       jae      G_M000_IG25
 
G_M000_IG35:                ;; offset=0x030B
       cmp      r14d, esi
       setb     bl
       movzx    rbx, bl
       jmp      G_M000_IG26
 
G_M000_IG36:                ;; offset=0x0319
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      SHORT G_M000_IG31
 
G_M000_IG37:                ;; offset=0x0323
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG38:                ;; offset=0x032A
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG39:                ;; offset=0x0366
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG40:                ;; offset=0x03A2
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
RWD00  	dd	40400000h		;         3
RWD04  	dd	40000000h		;         2
RWD08  	dd	41000000h		;         8
RWD12  	dd	42380000h		;        46
RWD16  	dd	41A80000h		;        21
RWD20  	dd	42080000h		;        34
RWD24  	dd	41500000h		;        13
RWD28  	dd	41880000h		;        17
RWD32  	dd	40A00000h		;         5
RWD36  	dd	3F800000h		;         1

; Total bytes of code 1017

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedBatch8():Tl.FusionExperiment.SumReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 2
; 2 inlinees with PGO data; 11 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x30]
 
G_M000_IG02:                ;; offset=0x000F
       vxorps   xmm0, xmm0, xmm0
       mov      rax, 0x1000000000000
       mov      qword ptr [rbp-0x20], rax
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       xor      eax, eax
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jg       G_M000_IG23
 
G_M000_IG03:                ;; offset=0x003D
       movzx    rax, r15w
       movzx    rdx, r14w
       mov      dword ptr [rbp-0x2C], edx
       vmovd    edx, xmm0
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       movzx    rcx, word  ptr [rbp-0x2C]
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0060
       add      rsp, 24
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x006B
       mov      ecx, 5
       jmp      G_M000_IG22
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0075
       cmp      r14d, 76
       jb       SHORT G_M000_IG10
 
G_M000_IG07:                ;; offset=0x007B
       cmp      r14d, 123
       jb       SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x0081
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG31
 
G_M000_IG09:                ;; offset=0x008E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       lea      esi, [r14-0x4C]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rsi
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG31
 
G_M000_IG10:                ;; offset=0x00C4
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD20]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG31
 
G_M000_IG11:                ;; offset=0x00D9
       cmp      r14d, 11
       jb       SHORT G_M000_IG16
 
G_M000_IG12:                ;; offset=0x00DF
       cmp      r14d, 18
       jb       G_M000_IG31
 
G_M000_IG13:                ;; offset=0x00E9
       cmp      r14d, 29
       jb       SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x00EF
       lea      esi, [r14-0x1D]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rsi
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG31
 
G_M000_IG15:                ;; offset=0x011D
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
       jmp      G_M000_IG31
 
G_M000_IG16:                ;; offset=0x0132
       cmp      r14d, 3
       jb       SHORT G_M000_IG20
 
G_M000_IG17:                ;; offset=0x0138
       cmp      r14d, 7
       jb       SHORT G_M000_IG19
 
G_M000_IG18:                ;; offset=0x013E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      G_M000_IG30
 
G_M000_IG19:                ;; offset=0x014B
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       lea      esi, [r14-0x03]
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, rsi
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG30
 
G_M000_IG20:                ;; offset=0x0179
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       jmp      G_M000_IG31
 
G_M000_IG21:                ;; offset=0x0186
       mov      ecx, 1
       cmp      r8d, 599
       je       G_M000_IG05
 
G_M000_IG22:                ;; offset=0x0198
       mov      esi, ebx
       mov      edx, edx
       shl      rdx, 32
       or       rdx, rsi
       mov      ecx, ecx
       shl      rcx, 48
       or       rcx, rdx
       mov      qword ptr [rbp-0x28], rcx
       mov      ebx, dword ptr [rbp-0x28]
       movzx    r15, word  ptr [rbp-0x24]
       movzx    r14, word  ptr [rbp-0x22]
       add      eax, 8
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG23:                ;; offset=0x01CD
       test     rcx, rcx
       je       G_M000_IG37
       mov      edx, dword ptr [rcx+0x08]
       mov      esi, eax
       add      rsi, 8
       cmp      rdx, rsi
       jb       G_M000_IG37
       mov      edx, eax
       lea      rcx, bword ptr [rcx+4*rdx+0x10]
       movzx    rdx, r14w
       test     dl, 1
       je       G_M000_IG38
       movzx    rdx, r14w
       test     dl, 2
       jne      G_M000_IG39
       movzx    rdx, r15w
       mov      esi, ebx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     esi, esi, 600
       mov      r8d, ebx
       sub      r8d, esi
       mov      esi, ebx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       xor      r9d, r9d
       mov      r10d, 9
       jmp      SHORT G_M000_IG32
 
G_M000_IG24:                ;; offset=0x023E
       mov      ebx, r15d
       sub      ebx, esi
 
G_M000_IG25:                ;; offset=0x0243
       mov      r8d, edx
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   rsi, r8d
       mov      r8d, ebx
       cmp      rsi, r8
       jl       G_M000_IG40
       add      edx, ebx
       movzx    rdx, dx
       cmp      r14d, 47
       jb       G_M000_IG11
 
G_M000_IG26:                ;; offset=0x026E
       cmp      r14d, 200
       jb       G_M000_IG06
 
G_M000_IG27:                ;; offset=0x027B
       cmp      r14d, 515
       jae      G_M000_IG36
 
G_M000_IG28:                ;; offset=0x0288
       cmp      r14d, 321
       jb       SHORT G_M000_IG35
 
G_M000_IG29:                ;; offset=0x0291
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD36]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
 
G_M000_IG30:                ;; offset=0x02A1
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD28]
 
G_M000_IG31:                ;; offset=0x02A9
       mov      ebx, r11d
       mov      r8d, r14d
       mov      esi, r15d
       add      r9, 4
 
G_M000_IG32:                ;; offset=0x02B6
       dec      r10d
       je       G_M000_IG21
 
G_M000_IG33:                ;; offset=0x02BF
       mov      r11d, dword ptr [rcx+r9]
       mov      r15d, r11d
       imul     r14, r15, 0x1B4E81B5
       shr      r14, 38
       imul     r15d, r14d, 600
       mov      r14d, r11d
       sub      r14d, r15d
       mov      r15d, r11d
       imul     r15, r15, 0x1B4E81B5
       shr      r15, 38
       cmp      r11d, ebx
       jae      G_M000_IG24
 
G_M000_IG34:                ;; offset=0x02F5
       cmp      r14d, r8d
       setb     bl
       movzx    rbx, bl
       jmp      G_M000_IG25
 
G_M000_IG35:                ;; offset=0x0303
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      SHORT G_M000_IG30
 
G_M000_IG36:                ;; offset=0x030D
       cmp      r14d, 600
       jae      SHORT G_M000_IG31
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      SHORT G_M000_IG31
 
G_M000_IG37:                ;; offset=0x0320
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG38:                ;; offset=0x0327
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG39:                ;; offset=0x0363
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG40:                ;; offset=0x039F
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r14
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
RWD00  	dd	40000000h		;         2
RWD04  	dd	41000000h		;         8
RWD08  	dd	42380000h		;        46
RWD12  	dd	41A80000h		;        21
RWD16  	dd	42080000h		;        34
RWD20  	dd	41500000h		;        13
RWD24  	dd	41880000h		;        17
RWD28  	dd	40A00000h		;         5
RWD32  	dd	40400000h		;         3
RWD36  	dd	3F800000h		;         1

; Total bytes of code 1014

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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBEA1EA2D8
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
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x58], rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x60], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
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
       mov      rdi, 0x7FCBEA1EA2DC
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
       mov      rdi, 0x7FCBEA1EA2E0
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
       mov      rdi, 0x7FCBEA1EA2E4
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
       mov      rdi, 0x7FCBEA1EA2E8
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
       mov      rdi, 0x7FCBEA1EA2EC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG19
 
G_M000_IG12:                ;; offset=0x02D7
       mov      rdi, 0x7FCBEA1EA2F0
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
       mov      rdi, 0x7FCBEA1EA2F4
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
       mov      rdi, 0x7FCBEA1EA2F8
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
       mov      rdi, 0x7FCBEA1EA2FC
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
       mov      rdi, 0x7FCBEA1EA300
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
       mov      rdi, 0x7FCBEA1EA304
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
       mov      rdi, 0x7FCBEA1EA308
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG19:                ;; offset=0x0529
       mov      rdi, 0x7FCBEA1EA30C
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
       mov      rdi, 0x7FCBEA1EA310
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
       mov      rdi, 0x7FCBEA1E3F30
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
       mov      rdi, 0x7FCBEA1E3F34
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
       jg       G_M000_IG12
 
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
       jmp      G_M000_IG14
 
G_M000_IG07:                ;; offset=0x0084
       mov      r8d, 5
       jmp      G_M000_IG15
 
G_M000_IG08:                ;; offset=0x008F
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
 
G_M000_IG09:                ;; offset=0x0097
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
 
G_M000_IG10:                ;; offset=0x009F
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
 
G_M000_IG11:                ;; offset=0x00A7
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
 
G_M000_IG12:                ;; offset=0x00D7
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
 
G_M000_IG13:                ;; offset=0x0131
       mov      r8d, edx
       imul     rsi, r8, 0x1B4E81B5
       shr      rsi, 38
       mov      r8d, ebx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       sub      esi, r8d
 
G_M000_IG14:                ;; offset=0x0150
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
 
G_M000_IG15:                ;; offset=0x0185
       cmp      r9d, 47
       jb       G_M000_IG26
 
G_M000_IG16:                ;; offset=0x018F
       cmp      r9d, 200
       jb       SHORT G_M000_IG21
 
G_M000_IG17:                ;; offset=0x0198
       cmp      r9d, 515
       jae      SHORT G_M000_IG20
 
G_M000_IG18:                ;; offset=0x01A1
       cmp      r9d, 321
       jae      G_M000_IG08
 
G_M000_IG19:                ;; offset=0x01AE
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      G_M000_IG10
 
G_M000_IG20:                ;; offset=0x01BB
       cmp      r9d, 600
       jae      G_M000_IG11
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       jmp      G_M000_IG11
 
G_M000_IG21:                ;; offset=0x01D5
       cmp      r9d, 76
       jb       SHORT G_M000_IG25
 
G_M000_IG22:                ;; offset=0x01DB
       cmp      r9d, 123
       jb       SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x01E1
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      G_M000_IG11
 
G_M000_IG24:                ;; offset=0x01EE
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       add      r9d, -76
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD20]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG11
 
G_M000_IG25:                ;; offset=0x0224
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD32]
       jmp      SHORT G_M000_IG23
 
G_M000_IG26:                ;; offset=0x022E
       cmp      r9d, 11
       jb       SHORT G_M000_IG30
 
G_M000_IG27:                ;; offset=0x0234
       cmp      r9d, 18
       jb       G_M000_IG11
 
G_M000_IG28:                ;; offset=0x023E
       cmp      r9d, 29
       jb       G_M000_IG09
 
G_M000_IG29:                ;; offset=0x0248
       add      r9d, -29
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG11
 
G_M000_IG30:                ;; offset=0x0276
       cmp      r9d, 3
       jb       SHORT G_M000_IG34
 
G_M000_IG31:                ;; offset=0x027C
       cmp      r9d, 7
       jb       SHORT G_M000_IG33
 
G_M000_IG32:                ;; offset=0x0282
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       jmp      G_M000_IG10
 
G_M000_IG33:                ;; offset=0x028F
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       add      r9d, -3
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD12]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG10
 
G_M000_IG34:                ;; offset=0x02BD
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG11
 
G_M000_IG35:                ;; offset=0x02CA
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG36:                ;; offset=0x0306
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x0342
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG38:                ;; offset=0x0399
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
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

; Total bytes of code 927

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
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG28:                ;; offset=0x0272
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG29:                ;; offset=0x02AE
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
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
; with Synthesized PGO: fgCalledCount is 11968
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
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
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
       jmp      G_M000_IG20
 
G_M000_IG03:                ;; offset=0x0043
       mov      r8d, 5
       jmp      G_M000_IG12
 
G_M000_IG04:                ;; offset=0x004E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG18
 
G_M000_IG05:                ;; offset=0x005B
       cmp      r9d, 600
       jae      G_M000_IG19
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      G_M000_IG19
 
G_M000_IG06:                ;; offset=0x0075
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD08]
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:                ;; offset=0x007F
       cmp      r9d, 76
       jb       SHORT G_M000_IG06
 
G_M000_IG08:                ;; offset=0x0085
       cmp      r9d, 123
       jb       G_M000_IG23
 
G_M000_IG09:                ;; offset=0x008F
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD00]
       jmp      G_M000_IG19
 
G_M000_IG10:                ;; offset=0x009C
       mov      r8d, edx
       imul     rsi, r8, 0x1B4E81B5
       shr      rsi, 38
       mov      r8d, ebx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       sub      esi, r8d
 
G_M000_IG11:                ;; offset=0x00BB
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
 
G_M000_IG12:                ;; offset=0x00F4
       cmp      r9d, 47
       jb       G_M000_IG24
 
G_M000_IG13:                ;; offset=0x00FE
       cmp      r9d, 200
       jb       G_M000_IG07
 
G_M000_IG14:                ;; offset=0x010B
       cmp      r9d, 515
       jae      G_M000_IG05
 
G_M000_IG15:                ;; offset=0x0118
       cmp      r9d, 321
       jb       G_M000_IG04
 
G_M000_IG16:                ;; offset=0x0125
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
 
G_M000_IG17:                ;; offset=0x012D
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
 
G_M000_IG18:                ;; offset=0x0135
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD20]
 
G_M000_IG19:                ;; offset=0x013D
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
 
G_M000_IG20:                ;; offset=0x0167
       dec      eax
       je       G_M000_IG33
 
G_M000_IG21:                ;; offset=0x016F
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
       jae      G_M000_IG10
 
G_M000_IG22:                ;; offset=0x01C4
       cmp      r9d, r8d
       setb     sil
       movzx    rsi, sil
       jmp      G_M000_IG11
 
G_M000_IG23:                ;; offset=0x01D4
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD16]
       add      r9d, -76
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD24]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD28]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD32]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG19
 
G_M000_IG24:                ;; offset=0x020A
       cmp      r9d, 11
       jb       SHORT G_M000_IG28
 
G_M000_IG25:                ;; offset=0x0210
       cmp      r9d, 18
       jb       G_M000_IG19
 
G_M000_IG26:                ;; offset=0x021A
       cmp      r9d, 29
       jb       G_M000_IG17
 
G_M000_IG27:                ;; offset=0x0224
       add      r9d, -29
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD36]
       vmulss   xmm1, xmm1, dword ptr [reloc @RWD16]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD08]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG19
 
G_M000_IG28:                ;; offset=0x0252
       cmp      r9d, 3
       jb       SHORT G_M000_IG32
 
G_M000_IG29:                ;; offset=0x0258
       cmp      r9d, 7
       jb       SHORT G_M000_IG31
 
G_M000_IG30:                ;; offset=0x025E
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD04]
       jmp      G_M000_IG18
 
G_M000_IG31:                ;; offset=0x026B
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       add      r9d, -3
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2ss xmm1, xmm1, r9
       vdivss   xmm1, xmm1, dword ptr [reloc @RWD04]
       vaddss   xmm1, xmm1, dword ptr [reloc @RWD00]
       vaddss   xmm0, xmm0, xmm1
       jmp      G_M000_IG18
 
G_M000_IG32:                ;; offset=0x0299
       vaddss   xmm0, xmm0, dword ptr [reloc @RWD12]
       jmp      G_M000_IG19
 
G_M000_IG33:                ;; offset=0x02A6
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
 
G_M000_IG34:                ;; offset=0x02C8
       add      rsp, 16
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG35:                ;; offset=0x02D5
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG36:                ;; offset=0x0311
       mov      rdi, 0x7FCBE9CD3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG37:                ;; offset=0x034D
       mov      rdi, 0x7FCBE9F83EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7FCBE9B31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r13, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7FCBE9B31A30
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

; Total bytes of code 932


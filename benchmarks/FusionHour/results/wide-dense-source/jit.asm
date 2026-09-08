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
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
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
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
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
       sub      rsp, 160
       lea      rbp, [rsp+0xA0]
       xor      eax, eax
       mov      qword ptr [rbp-0x98], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x90], ymm8
       vmovdqu  ymmword ptr [rbp-0x70], ymm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       vmovdqa  xmmword ptr [rbp-0x30], xmm8
       mov      qword ptr [rbp-0x20], rax
       mov      dword ptr [rbp-0x18], eax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x0047
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x18], eax
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x1C], eax
       mov      eax, dword ptr [rbp-0x14]
       imul     ecx, dword ptr [rbp-0x1C], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x20], eax
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       cmp      eax, dword ptr [rbp-0x14]
       ja       SHORT G_M000_IG03
       mov      eax, dword ptr [rbp-0x1C]
       sub      eax, dword ptr [rbp-0x18]
       mov      dword ptr [rbp-0x24], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x008D
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       imul     ecx, dword ptr [rbp-0x18], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x20]
       cmp      eax, dword ptr [rbp-0x4C]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x24], eax
 
G_M000_IG04:                ;; offset=0x00AE
       mov      eax, dword ptr [rbp-0x24]
       mov      rcx, bword ptr [rbp-0x08]
       movzx    rcx, word  ptr [rcx+0x04]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00C9
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x88], rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x90], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x98], rax
       mov      rsi, gword ptr [rbp-0x90]
       mov      rdx, gword ptr [rbp-0x98]
       mov      rdi, gword ptr [rbp-0x88]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x88]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x013F
       mov      rax, bword ptr [rbp-0x08]
       movzx    rax, word  ptr [rax+0x04]
       add      eax, dword ptr [rbp-0x24]
       movzx    rax, ax
       mov      dword ptr [rbp-0x28], eax
       mov      dword ptr [rbp-0x2C], 1
       cmp      dword ptr [rbp-0x20], 599
       jne      SHORT G_M000_IG07
       mov      eax, dword ptr [rbp-0x2C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x2C], eax
 
G_M000_IG07:                ;; offset=0x016C
       mov      eax, dword ptr [rbp-0x20]
       mov      dword ptr [rbp-0x30], eax
       mov      eax, dword ptr [rbp-0x30]
       lea      eax, [rax+2*rax]
       mov      dword ptr [rbp-0x34], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte]]
       mov      bword ptr [rbp-0x70], rax
       mov      qword ptr [rbp-0x68], rdx
 
G_M000_IG08:                ;; offset=0x0189
       vmovdqu  xmm0, xmmword ptr [rbp-0x70]
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
 
G_M000_IG09:                ;; offset=0x0193
       mov      eax, dword ptr [rbp-0x58]
       cmp      dword ptr [rbp-0x30], eax
       jae      G_M000_IG15
       mov      eax, dword ptr [rbp-0x30]
       mov      rcx, bword ptr [rbp-0x60]
       movzx    rax, byte  ptr [rcx+rax]
       mov      dword ptr [rbp-0x38], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float]]
       mov      bword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x78], rdx
 
G_M000_IG10:                ;; offset=0x01BB
       vmovdqu  xmm0, xmmword ptr [rbp-0x80]
       vmovdqu  xmmword ptr [rbp-0x48], xmm0
 
G_M000_IG11:                ;; offset=0x01C5
       cmp      dword ptr [rbp-0x38], 0
       je       G_M000_IG13
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x34], eax
       jae      G_M000_IG15
       mov      eax, dword ptr [rbp-0x34]
       mov      rcx, bword ptr [rbp-0x48]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       cmp      dword ptr [rbp-0x38], 1
       jle      SHORT G_M000_IG12
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x34]
       inc      eax
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG15
       mov      eax, dword ptr [rbp-0x34]
       inc      eax
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0x48]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG12:                ;; offset=0x0227
       cmp      dword ptr [rbp-0x38], 2
       jle      SHORT G_M000_IG13
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x34]
       add      eax, 2
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG15
       mov      eax, dword ptr [rbp-0x34]
       add      eax, 2
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0x48]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG13:                ;; offset=0x0259
       mov      edi, dword ptr [rbp-0x14]
       mov      esi, dword ptr [rbp-0x28]
       mov      edx, dword ptr [rbp-0x2C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG14:                ;; offset=0x0269
       add      rsp, 160
       pop      rbp
       ret      
 
G_M000_IG15:                ;; offset=0x0272
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 632

; Assembly listing for method Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte] (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
 
G_M000_IG02:                ;; offset=0x0010
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x10], xmm0
       lea      rdi, [rbp-0x10]
       mov      rsi, 0x7F55F984B940
       mov      edx, 600
       call     [System.ReadOnlySpan`1[byte]:.ctor(ptr,int):this]
       mov      rax, bword ptr [rbp-0x10]
       mov      rdx, qword ptr [rbp-0x08]
 
G_M000_IG03:                ;; offset=0x003A
       add      rsp, 16
       pop      rbp
       ret      
 
; Total bytes of code 64

; Assembly listing for method Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float] (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
 
G_M000_IG02:                ;; offset=0x0010
       mov      dword ptr [rbp-0x08], 0x708
       mov      rax, 0x7F55F984BB98
       mov      bword ptr [rbp-0x10], rax
       mov      rax, bword ptr [rbp-0x10]
       mov      rdx, qword ptr [rbp-0x08]
 
G_M000_IG03:                ;; offset=0x002D
       add      rsp, 16
       pop      rbp
       ret      
 
; Total bytes of code 51

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
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
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
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
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
       sub      rsp, 144
       lea      rbp, [rsp+0x90]
       xor      eax, eax
       mov      qword ptr [rbp-0x88], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       mov      qword ptr [rbp-0x20], rax
       mov      dword ptr [rbp-0x18], eax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x003F
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x18], eax
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x1C], eax
       mov      eax, dword ptr [rbp-0x14]
       imul     ecx, dword ptr [rbp-0x1C], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x20], eax
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       cmp      eax, dword ptr [rbp-0x14]
       jb       SHORT G_M000_IG03
       mov      eax, dword ptr [rbp-0x18]
       sub      eax, dword ptr [rbp-0x1C]
       mov      dword ptr [rbp-0x24], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0085
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       imul     ecx, dword ptr [rbp-0x18], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x20]
       cmp      eax, dword ptr [rbp-0x4C]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x24], eax
 
G_M000_IG04:                ;; offset=0x00A6
       mov      rax, bword ptr [rbp-0x08]
       movzx    rax, word  ptr [rax+0x04]
       mov      dword ptr [rbp-0x64], eax
       mov      rax, bword ptr [rbp-0x08]
       movzx    rdi, word  ptr [rax+0x04]
       mov      esi, dword ptr [rbp-0x24]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x64]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x28], eax
       mov      dword ptr [rbp-0x2C], 1
       cmp      dword ptr [rbp-0x20], 599
       jne      SHORT G_M000_IG05
       mov      eax, dword ptr [rbp-0x2C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x2C], eax
 
G_M000_IG05:                ;; offset=0x00E9
       mov      eax, dword ptr [rbp-0x20]
       mov      dword ptr [rbp-0x30], eax
       mov      eax, dword ptr [rbp-0x30]
       lea      eax, [rax+2*rax]
       mov      dword ptr [rbp-0x34], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte]]
       mov      bword ptr [rbp-0x78], rax
       mov      qword ptr [rbp-0x70], rdx
 
G_M000_IG06:                ;; offset=0x0106
       vmovdqu  xmm0, xmmword ptr [rbp-0x78]
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
 
G_M000_IG07:                ;; offset=0x0110
       mov      eax, dword ptr [rbp-0x58]
       cmp      dword ptr [rbp-0x30], eax
       jae      G_M000_IG13
       mov      eax, dword ptr [rbp-0x30]
       mov      rcx, bword ptr [rbp-0x60]
       movzx    rax, byte  ptr [rcx+rax]
       mov      dword ptr [rbp-0x38], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float]]
       mov      bword ptr [rbp-0x88], rax
       mov      qword ptr [rbp-0x80], rdx
 
G_M000_IG08:                ;; offset=0x013B
       vmovdqu  xmm0, xmmword ptr [rbp-0x88]
       vmovdqu  xmmword ptr [rbp-0x48], xmm0
 
G_M000_IG09:                ;; offset=0x0148
       cmp      dword ptr [rbp-0x38], 0
       je       G_M000_IG11
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x34], eax
       jae      G_M000_IG13
       mov      eax, dword ptr [rbp-0x34]
       mov      rcx, bword ptr [rbp-0x48]
       vsubss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       cmp      dword ptr [rbp-0x38], 1
       jle      SHORT G_M000_IG10
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x34]
       inc      eax
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG13
       mov      eax, dword ptr [rbp-0x34]
       inc      eax
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0x48]
       vsubss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG10:                ;; offset=0x01AA
       cmp      dword ptr [rbp-0x38], 2
       jle      SHORT G_M000_IG11
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x34]
       add      eax, 2
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG13
       mov      eax, dword ptr [rbp-0x34]
       add      eax, 2
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0x48]
       vsubss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG11:                ;; offset=0x01DC
       mov      edi, dword ptr [rbp-0x14]
       mov      esi, dword ptr [rbp-0x28]
       mov      edx, dword ptr [rbp-0x2C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG12:                ;; offset=0x01EC
       add      rsp, 144
       pop      rbp
       ret      
 
G_M000_IG13:                ;; offset=0x01F5
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 507

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 304
       lea      rbp, [rsp+0x130]
       xor      eax, eax
       mov      qword ptr [rbp-0x128], rax
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
 
G_M000_IG02:                ;; offset=0x005C
       mov      dword ptr [rbp-0x108], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0079
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x110], rax
       mov      rsi, gword ptr [rbp-0x110]
       mov      rdi, gword ptr [rbp-0x100]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x100]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00CC
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00DF
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x118], rax
       mov      rsi, gword ptr [rbp-0x118]
       mov      rdi, gword ptr [rbp-0xF8]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0132
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7F557B59EAB0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x0156
       add      rsp, 304
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
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x4C]
       imul     ecx, dword ptr [rbp-0x54], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x58], eax
 
G_M000_IG09:                ;; offset=0x0191
       vmovdqu  xmm0, xmmword ptr [rbp-0x48]
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
 
G_M000_IG10:                ;; offset=0x019B
       xor      eax, eax
       mov      dword ptr [rbp-0x74], eax
       jmp      G_M000_IG22
 
G_M000_IG11:                ;; offset=0x01A5
       mov      eax, dword ptr [rbp-0x68]
       cmp      dword ptr [rbp-0x74], eax
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x74]
       mov      rcx, bword ptr [rbp-0x70]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x78], eax
       mov      eax, dword ptr [rbp-0xB4]
       imul     ecx, dword ptr [rbp-0x78], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x7C], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0xB4]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG12
       mov      eax, dword ptr [rbp-0x78]
       sub      eax, dword ptr [rbp-0x54]
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0213
       mov      rdi, 0x7F557B59EAB4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x7C]
       cmp      eax, dword ptr [rbp-0x58]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
 
G_M000_IG13:                ;; offset=0x023D
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xC0], eax
       mov      eax, dword ptr [rbp-0x80]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x025F
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x120], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x128], rax
       mov      rsi, gword ptr [rbp-0x120]
       mov      rdx, gword ptr [rbp-0x128]
       mov      rdi, gword ptr [rbp-0xF0]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0xF0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG15:                ;; offset=0x02D5
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x80]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x84], eax
       mov      eax, dword ptr [rbp-0x84]
       lea      eax, [rax+2*rax]
       mov      dword ptr [rbp-0x88], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte]]
       mov      bword ptr [rbp-0xD0], rax
       mov      qword ptr [rbp-0xC8], rdx
 
G_M000_IG16:                ;; offset=0x030D
       vmovdqu  xmm0, xmmword ptr [rbp-0xD0]
       vmovdqu  xmmword ptr [rbp-0xB0], xmm0
 
G_M000_IG17:                ;; offset=0x031D
       mov      eax, dword ptr [rbp-0xA8]
       cmp      dword ptr [rbp-0x84], eax
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x84]
       mov      rcx, bword ptr [rbp-0xB0]
       movzx    rax, byte  ptr [rcx+rax]
       mov      dword ptr [rbp-0x8C], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float]]
       mov      bword ptr [rbp-0xE0], rax
       mov      qword ptr [rbp-0xD8], rdx
 
G_M000_IG18:                ;; offset=0x035A
       vmovdqu  xmm0, xmmword ptr [rbp-0xE0]
       vmovdqu  xmmword ptr [rbp-0xA0], xmm0
 
G_M000_IG19:                ;; offset=0x036A
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0x8C], 0
       je       G_M000_IG21
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x98]
       cmp      dword ptr [rbp-0x88], eax
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x88]
       mov      rcx, bword ptr [rbp-0xA0]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0xE8], eax
       cmp      dword ptr [rbp-0x8C], 1
       jle      SHORT G_M000_IG20
       mov      rdi, 0x7F557B59EAB8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x88]
       inc      eax
       cmp      eax, dword ptr [rbp-0x98]
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x88]
       inc      eax
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0xA0]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG20:                ;; offset=0x0415
       mov      eax, dword ptr [rbp-0xE8]
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0x8C], 2
       jle      G_M000_IG27
       mov      rdi, 0x7F557B59EABC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x88]
       add      eax, 2
       cmp      eax, dword ptr [rbp-0x98]
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x88]
       add      eax, 2
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0xA0]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG21:                ;; offset=0x0479
       mov      rdi, 0x7F557B59EAC0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x74]
       inc      eax
       mov      dword ptr [rbp-0x74], eax
 
G_M000_IG22:                ;; offset=0x04A5
       mov      eax, dword ptr [rbp-0x108]
       dec      eax
       mov      dword ptr [rbp-0x108], eax
       cmp      dword ptr [rbp-0x108], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x04BC
       lea      rdi, [rbp-0x108]
       mov      esi, 296
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:                ;; offset=0x04CD
       mov      eax, dword ptr [rbp-0x74]
       cmp      eax, dword ptr [rbp-0x68]
       jl       G_M000_IG11
       mov      dword ptr [rbp-0x5C], 1
       cmp      dword ptr [rbp-0x58], 599
       jne      SHORT G_M000_IG25
       mov      rdi, 0x7F557B59EAC4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x5C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x5C], eax
 
G_M000_IG25:                ;; offset=0x0504
       mov      rdi, 0x7F557B59EAC8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x5C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG26:                ;; offset=0x0523
       add      rsp, 304
       pop      rbp
       ret      
 
G_M000_IG27:                ;; offset=0x052C
       mov      rdi, 0x7F557B59EACC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG21
 
G_M000_IG28:                ;; offset=0x0540
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 1350

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x128
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 4
; 1 inlinees with PGO data; 6 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0x158], r15
       mov      qword ptr [rsp+0x150], r14
       mov      qword ptr [rsp+0x148], rbx
       lea      rbp, [rsp+0x20]
       mov      rdi, bword ptr [rbp+0x108]
       mov      ecx, dword ptr [rbp+0xF4]
       mov      eax, dword ptr [rbp+0xF0]
       mov      edx, dword ptr [rbp+0xEC]
       mov      esi, dword ptr [rbp+0xE8]
       mov      r8d, dword ptr [rbp+0xCC]
 
G_M000_IG02:                ;; offset=0x004C
       mov      r9, bword ptr [rbp+0xD0]
       mov      r10d, dword ptr [rbp+0xD8]
       mov      r8d, r8d
       cmp      r8d, r10d
       jl       SHORT G_M000_IG09
 
G_M000_IG03:                ;; offset=0x0062
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
 
G_M000_IG04:                ;; offset=0x0089
       add      rsp, 328
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0097
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0097
       cmp      r14d, esi
       setb     cl
       movzx    rcx, cl
       jmp      SHORT G_M000_IG11
 
G_M000_IG07:                ;; offset=0x00A2
       vmovss   xmm0, dword ptr [rdi]
       add      ecx, 2
       cmp      ecx, 0x708
       jae      G_M000_IG16
       vaddss   xmm0, xmm0, dword ptr [r15+4*rcx]
       vmovss   dword ptr [rdi], xmm0
 
G_M000_IG08:                ;; offset=0x00BF
       mov      ecx, r11d
       mov      esi, r14d
       mov      edx, ebx
       inc      r8d
       cmp      r8d, r10d
       jge      SHORT G_M000_IG03
 
G_M000_IG09:                ;; offset=0x00CF
       cmp      r8d, r10d
       jae      G_M000_IG16
       mov      r11d, dword ptr [r9+4*r8]
       mov      ebx, r11d
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     r15d, ebx, 600
       mov      r14d, r11d
       sub      r14d, r15d
       cmp      r11d, ecx
       jb       SHORT G_M000_IG06
 
G_M000_IG10:                ;; offset=0x00FC
       mov      ecx, ebx
       sub      ecx, edx
 
G_M000_IG11:                ;; offset=0x0100
       mov      esi, eax
       neg      esi
       add      esi, 0xFFFF
       movsxd   rdx, esi
       mov      esi, ecx
       cmp      rdx, rsi
       jl       G_M000_IG15
       add      eax, ecx
       movzx    rax, ax
       lea      ecx, [r14+2*r14]
       cmp      r14d, 600
       jae      G_M000_IG16
       mov      edx, r14d
       mov      rsi, 0x7F55F984B940
       movzx    rdx, byte  ptr [rsi+rdx]
       test     edx, edx
       je       G_M000_IG08
 
G_M000_IG12:                ;; offset=0x0147
       vmovss   xmm0, dword ptr [rdi]
       cmp      ecx, 0x708
       jae      G_M000_IG16
       mov      esi, ecx
       mov      r15, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r15+4*rsi]
       vmovss   dword ptr [rdi], xmm0
       cmp      edx, 1
       jle      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0172
       vmovss   xmm0, dword ptr [rdi]
       lea      esi, [rcx+0x01]
       cmp      esi, 0x708
       jae      SHORT G_M000_IG16
       vaddss   xmm0, xmm0, dword ptr [r15+4*rsi]
       vmovss   dword ptr [rdi], xmm0
 
G_M000_IG14:                ;; offset=0x018B
       cmp      edx, 2
       jle      G_M000_IG08
       jmp      G_M000_IG07
 
G_M000_IG15:                ;; offset=0x0199
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG16:                ;; offset=0x01F0
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 502

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
       vxorps   xmm8, xmm8, xmm8
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
 
G_M000_IG02:                ;; offset=0x004B
       mov      dword ptr [rbp-0x100], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0068
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x108], rax
       mov      rsi, gword ptr [rbp-0x108]
       mov      rdi, gword ptr [rbp-0xF8]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00BB
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00CE
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x110], rax
       mov      rsi, gword ptr [rbp-0x110]
       mov      rdi, gword ptr [rbp-0xF0]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0121
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7F557B5B8358
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x0145
       add      rsp, 272
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
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x4C]
       imul     ecx, dword ptr [rbp-0x54], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x58], eax
 
G_M000_IG09:                ;; offset=0x0180
       vmovdqu  xmm0, xmmword ptr [rbp-0x48]
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
 
G_M000_IG10:                ;; offset=0x018A
       xor      eax, eax
       mov      dword ptr [rbp-0x74], eax
       jmp      G_M000_IG20
 
G_M000_IG11:                ;; offset=0x0194
       mov      eax, dword ptr [rbp-0x68]
       cmp      dword ptr [rbp-0x74], eax
       jae      G_M000_IG26
       mov      eax, dword ptr [rbp-0x74]
       mov      rcx, bword ptr [rbp-0x70]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x78], eax
       mov      eax, dword ptr [rbp-0xB4]
       imul     ecx, dword ptr [rbp-0x78], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x7C], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0xB4]
       cmp      eax, dword ptr [rbp-0x4C]
       ja       SHORT G_M000_IG12
       mov      eax, dword ptr [rbp-0x54]
       sub      eax, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0202
       mov      rdi, 0x7F557B5B835C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x7C]
       cmp      eax, dword ptr [rbp-0x58]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
 
G_M000_IG13:                ;; offset=0x022C
       mov      edi, dword ptr [rbp-0x50]
       mov      esi, dword ptr [rbp-0x80]
       call     [System.Math:Min(uint,uint):uint]
       mov      ecx, dword ptr [rbp-0x50]
       sub      ecx, eax
       movzx    rax, cx
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x84], eax
       mov      eax, dword ptr [rbp-0x84]
       lea      eax, [rax+2*rax]
       mov      dword ptr [rbp-0x88], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte]]
       mov      bword ptr [rbp-0xD0], rax
       mov      qword ptr [rbp-0xC8], rdx
 
G_M000_IG14:                ;; offset=0x026F
       vmovdqu  xmm0, xmmword ptr [rbp-0xD0]
       vmovdqu  xmmword ptr [rbp-0xB0], xmm0
 
G_M000_IG15:                ;; offset=0x027F
       mov      eax, dword ptr [rbp-0xA8]
       cmp      dword ptr [rbp-0x84], eax
       jae      G_M000_IG26
       mov      eax, dword ptr [rbp-0x84]
       mov      rcx, bword ptr [rbp-0xB0]
       movzx    rax, byte  ptr [rcx+rax]
       mov      dword ptr [rbp-0x8C], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float]]
       mov      bword ptr [rbp-0xE0], rax
       mov      qword ptr [rbp-0xD8], rdx
 
G_M000_IG16:                ;; offset=0x02BC
       vmovdqu  xmm0, xmmword ptr [rbp-0xE0]
       vmovdqu  xmmword ptr [rbp-0xA0], xmm0
 
G_M000_IG17:                ;; offset=0x02CC
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0x8C], 0
       je       G_M000_IG19
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x98]
       cmp      dword ptr [rbp-0x88], eax
       jae      G_M000_IG26
       mov      eax, dword ptr [rbp-0x88]
       mov      rcx, bword ptr [rbp-0xA0]
       vsubss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0xE8], eax
       cmp      dword ptr [rbp-0x8C], 1
       jle      SHORT G_M000_IG18
       mov      rdi, 0x7F557B5B8360
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x88]
       inc      eax
       cmp      eax, dword ptr [rbp-0x98]
       jae      G_M000_IG26
       mov      eax, dword ptr [rbp-0x88]
       inc      eax
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0xA0]
       vsubss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG18:                ;; offset=0x0377
       mov      eax, dword ptr [rbp-0xE8]
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0x8C], 2
       jle      G_M000_IG25
       mov      rdi, 0x7F557B5B8364
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x88]
       add      eax, 2
       cmp      eax, dword ptr [rbp-0x98]
       jae      G_M000_IG26
       mov      eax, dword ptr [rbp-0x88]
       add      eax, 2
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0xA0]
       vsubss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG19:                ;; offset=0x03DB
       mov      rdi, 0x7F557B5B8368
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x74]
       inc      eax
       mov      dword ptr [rbp-0x74], eax
 
G_M000_IG20:                ;; offset=0x0407
       mov      eax, dword ptr [rbp-0x100]
       dec      eax
       mov      dword ptr [rbp-0x100], eax
       cmp      dword ptr [rbp-0x100], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:                ;; offset=0x041E
       lea      rdi, [rbp-0x100]
       mov      esi, 273
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:                ;; offset=0x042F
       mov      eax, dword ptr [rbp-0x74]
       cmp      eax, dword ptr [rbp-0x68]
       jl       G_M000_IG11
       mov      dword ptr [rbp-0x5C], 1
       cmp      dword ptr [rbp-0x58], 599
       jne      SHORT G_M000_IG23
       mov      rdi, 0x7F557B5B836C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x5C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x5C], eax
 
G_M000_IG23:                ;; offset=0x0466
       mov      rdi, 0x7F557B5B8370
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x5C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG24:                ;; offset=0x0485
       add      rsp, 272
       pop      rbp
       ret      
 
G_M000_IG25:                ;; offset=0x048E
       mov      rdi, 0x7F557B5B8374
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG19
 
G_M000_IG26:                ;; offset=0x04A2
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 1192

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Backward(byref,byref,System.ReadOnlySpan`1[uint]):Tl.Playback (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x111
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 7
; 2 inlinees with PGO data; 6 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0x138], r15
       mov      qword ptr [rsp+0x130], r14
       mov      qword ptr [rsp+0x128], rbx
       lea      rbp, [rsp+0x20]
       mov      rax, bword ptr [rbp+0xE8]
       mov      edx, dword ptr [rbp+0xD4]
       mov      ecx, dword ptr [rbp+0xD0]
       mov      esi, dword ptr [rbp+0xCC]
       mov      edi, dword ptr [rbp+0xC8]
       mov      r8d, dword ptr [rbp+0xAC]
 
G_M000_IG02:                ;; offset=0x004C
       mov      r9, bword ptr [rbp+0xB0]
       mov      r10d, dword ptr [rbp+0xB8]
       mov      r8d, r8d
       cmp      r8d, r10d
       jl       SHORT G_M000_IG10
 
G_M000_IG03:                ;; offset=0x0062
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
 
G_M000_IG04:                ;; offset=0x0087
       add      rsp, 296
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0095
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0095
       mov      edx, esi
       sub      edx, ebx
       jmp      SHORT G_M000_IG12
 
G_M000_IG07:                ;; offset=0x009B
       mov      edi, edx
       jmp      SHORT G_M000_IG14
 
G_M000_IG08:                ;; offset=0x009F
       vmovss   xmm0, dword ptr [rax]
       add      esi, 2
       cmp      esi, 0x708
       jae      G_M000_IG18
       mov      edx, esi
       vsubss   xmm0, xmm0, dword ptr [r15+4*rdx]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG09:                ;; offset=0x00BE
       mov      edx, r11d
       mov      edi, r14d
       mov      esi, ebx
       inc      r8d
       cmp      r8d, r10d
       jge      SHORT G_M000_IG03
 
G_M000_IG10:                ;; offset=0x00CE
       cmp      r8d, r10d
       jae      G_M000_IG18
       mov      r11d, dword ptr [r9+4*r8]
       mov      ebx, r11d
       imul     rbx, rbx, 0x1B4E81B5
       shr      rbx, 38
       imul     r15d, ebx, 600
       mov      r14d, r11d
       sub      r14d, r15d
       cmp      r11d, edx
       jbe      SHORT G_M000_IG06
 
G_M000_IG11:                ;; offset=0x00FB
       cmp      r14d, edi
       seta     dl
       movzx    rdx, dl
 
G_M000_IG12:                ;; offset=0x0104
       cmp      ecx, edx
       ja       SHORT G_M000_IG07
 
G_M000_IG13:                ;; offset=0x0108
       mov      edi, ecx
 
G_M000_IG14:                ;; offset=0x010A
       sub      ecx, edi
       movzx    rcx, cx
       lea      esi, [r14+2*r14]
       cmp      r14d, 600
       jae      SHORT G_M000_IG18
       mov      edx, r14d
       mov      rdi, 0x7F55F984B940
       movzx    rdx, byte  ptr [rdi+rdx]
       test     edx, edx
       je       SHORT G_M000_IG09
 
G_M000_IG15:                ;; offset=0x0131
       vmovss   xmm0, dword ptr [rax]
       cmp      esi, 0x708
       jae      SHORT G_M000_IG18
       mov      edi, esi
       mov      r15, 0x7F55F984BB98
       vsubss   xmm0, xmm0, dword ptr [r15+4*rdi]
       vmovss   dword ptr [rax], xmm0
       cmp      edx, 1
       jle      SHORT G_M000_IG17
 
G_M000_IG16:                ;; offset=0x0158
       vmovss   xmm0, dword ptr [rax]
       lea      edi, [rsi+0x01]
       cmp      edi, 0x708
       jae      SHORT G_M000_IG18
       vsubss   xmm0, xmm0, dword ptr [r15+4*rdi]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG17:                ;; offset=0x0171
       cmp      edx, 2
       jle      G_M000_IG09
       jmp      G_M000_IG08
 
G_M000_IG18:                ;; offset=0x017F
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 389

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
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x10], rax
       mov      edi, 926
       mov      rsi, 0x7F557AF31A30
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
       mov      rdi, 0x7F557B5E4740
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
       mov      rdi, 0x7F557B5E4744
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
; 1 inlinees with PGO data; 9 single block inlinees; 2 inlinees without PGO data

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
       mov      eax, eax
       cmp      dword ptr [rdi+0x08], eax
       jg       G_M000_IG15
 
G_M000_IG03:                ;; offset=0x004A
       vmovd    edx, xmm0
       mov      eax, r15d
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       mov      ecx, r14d
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0064
       add      rsp, 136
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0072
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0072
       imul     esi, edx, 600
       sub      ebx, esi
       cmp      ebx, r9d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG07:                ;; offset=0x0085
       mov      edx, r15d
       neg      edx
       add      edx, 0xFFFF
       movsxd   rdx, edx
       mov      r8d, esi
       cmp      rdx, r8
       jl       G_M000_IG19
       add      esi, r15d
       movzx    rdx, si
       mov      esi, 1
       cmp      r9d, 599
       jne      SHORT G_M000_IG09
 
G_M000_IG08:                ;; offset=0x00B3
       mov      esi, 5
 
G_M000_IG09:                ;; offset=0x00B8
       lea      r8d, [r9+2*r9]
       cmp      r9d, 600
       jae      G_M000_IG20
       mov      r9d, r9d
       mov      r10, 0x7F55F984B940
       movzx    r9, byte  ptr [r10+r9]
       test     r9d, r9d
       je       SHORT G_M000_IG14
 
G_M000_IG10:                ;; offset=0x00E0
       cmp      r8d, 0x708
       jae      G_M000_IG20
       mov      r10d, r8d
       mov      r11, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r11+4*r10]
       cmp      r9d, 1
       jle      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0106
       lea      r10d, [r8+0x01]
       cmp      r10d, 0x708
       jae      G_M000_IG20
       vaddss   xmm0, xmm0, dword ptr [r11+4*r10]
 
G_M000_IG12:                ;; offset=0x011D
       cmp      r9d, 2
       jle      SHORT G_M000_IG14
 
G_M000_IG13:                ;; offset=0x0123
       add      r8d, 2
       cmp      r8d, 0x708
       jae      G_M000_IG20
       vaddss   xmm0, xmm0, dword ptr [r11+4*r8]
 
G_M000_IG14:                ;; offset=0x013A
       mov      ecx, ecx
       mov      edx, edx
       shl      rdx, 32
       or       rcx, rdx
       mov      edx, esi
       shl      rdx, 48
       or       rcx, rdx
       mov      qword ptr [rbp-0x20], rcx
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       inc      eax
       cmp      dword ptr [rdi+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG15:                ;; offset=0x016A
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG20
       mov      ecx, dword ptr [rdi+4*rax+0x10]
       test     r14b, 1
       je       SHORT G_M000_IG17
       test     r14b, 2
       jne      SHORT G_M000_IG18
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       mov      esi, ecx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     r8d, esi, 600
       mov      r9d, ecx
       sub      r9d, r8d
       cmp      ecx, ebx
       jb       G_M000_IG06
 
G_M000_IG16:                ;; offset=0x01B2
       sub      esi, edx
       jmp      G_M000_IG07
 
G_M000_IG17:                ;; offset=0x01B9
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG18:                ;; offset=0x01F5
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG19:                ;; offset=0x0231
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG20:                ;; offset=0x0288
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 654

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
       mov      rdi, 0x7F557B5E47F8
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
       mov      rdi, 0x7F557B5E47FC
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
; 3 inlinees with PGO data; 13 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 48
       mov      qword ptr [rsp+0xB8], r15
       mov      qword ptr [rsp+0xB0], r14
       mov      qword ptr [rsp+0xA8], r13
       mov      qword ptr [rsp+0xA0], rbx
       lea      rbp, [rsp+0x30]
       mov      rdi, gword ptr [rbp+0x60]
       vmovss   xmm0, dword ptr [rbp+0x5C]
       mov      eax, dword ptr [rbp+0x4C]
 
G_M000_IG02:                ;; offset=0x003A
       mov      ebx, dword ptr [rbp+0x50]
       movzx    r15, word  ptr [rbp+0x54]
       movzx    r14, word  ptr [rbp+0x56]
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jg       G_M000_IG18
 
G_M000_IG03:                ;; offset=0x0054
       vmovd    edx, xmm0
       mov      eax, r15d
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       mov      ecx, r14d
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x006E
       add      rsp, 160
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x007E
       mov      ecx, 5
       jmp      G_M000_IG17
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0088
       mov      ebx, r11d
       sub      ebx, edx
 
G_M000_IG07:                ;; offset=0x008D
       mov      r8d, r15d
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   rdx, r8d
       mov      r8d, ebx
       cmp      rdx, r8
       jl       G_M000_IG22
       add      r15d, ebx
       movzx    r15, r15w
       lea      edx, [r13+2*r13]
       cmp      r13d, 600
       jae      G_M000_IG23
       mov      r8d, r13d
       mov      rbx, 0x7F55F984B940
       movzx    r8, byte  ptr [rbx+r8]
       test     r8d, r8d
       je       SHORT G_M000_IG12
 
G_M000_IG08:                ;; offset=0x00D9
       cmp      edx, 0x708
       jae      G_M000_IG23
       mov      ebx, edx
       mov      r14, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r14+4*rbx]
       cmp      r8d, 1
       jle      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00FD
       lea      ebx, [rdx+0x01]
       cmp      ebx, 0x708
       jae      G_M000_IG23
       vaddss   xmm0, xmm0, dword ptr [r14+4*rbx]
 
G_M000_IG10:                ;; offset=0x0112
       cmp      r8d, 2
       jle      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0118
       add      edx, 2
       cmp      edx, 0x708
       jae      G_M000_IG23
       vaddss   xmm0, xmm0, dword ptr [r14+4*rdx]
 
G_M000_IG12:                ;; offset=0x012D
       mov      ebx, r10d
       mov      r8d, r13d
       mov      edx, r11d
       add      rsi, 4
 
G_M000_IG13:                ;; offset=0x013A
       dec      r9d
       je       SHORT G_M000_IG16
 
G_M000_IG14:                ;; offset=0x013F
       mov      r10d, dword ptr [rcx+rsi]
       mov      r11d, r10d
       imul     r11, r11, 0x1B4E81B5
       shr      r11, 38
       imul     r14d, r11d, 600
       mov      r13d, r10d
       sub      r13d, r14d
       cmp      r10d, ebx
       jae      G_M000_IG06
 
G_M000_IG15:                ;; offset=0x0167
       cmp      r13d, r8d
       setb     bl
       movzx    rbx, bl
       jmp      G_M000_IG07
 
G_M000_IG16:                ;; offset=0x0175
       mov      ecx, 1
       cmp      r8d, 599
       je       G_M000_IG05
 
G_M000_IG17:                ;; offset=0x0187
       mov      edx, ebx
       mov      esi, r15d
       shl      rsi, 32
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
 
G_M000_IG18:                ;; offset=0x01BD
       test     rcx, rcx
       je       SHORT G_M000_IG19
       mov      edx, dword ptr [rcx+0x08]
       mov      esi, eax
       add      rsi, 8
       cmp      rdx, rsi
       jb       SHORT G_M000_IG19
       mov      edx, eax
       lea      rcx, bword ptr [rcx+4*rdx+0x10]
       test     r14b, 1
       je       SHORT G_M000_IG20
       test     r14b, 2
       jne      SHORT G_M000_IG21
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       imul     esi, edx, 600
       mov      r8d, ebx
       sub      r8d, esi
       xor      esi, esi
       mov      r9d, 9
       jmp      G_M000_IG13
 
G_M000_IG19:                ;; offset=0x0209
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG20:                ;; offset=0x0210
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG21:                ;; offset=0x024C
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG22:                ;; offset=0x0288
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG23:                ;; offset=0x02DF
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 741

; Assembly listing for method Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte] (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
 
G_M000_IG02:                ;; offset=0x0010
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x10], xmm0
       lea      rdi, [rbp-0x10]
       mov      rsi, 0x7F55F984B940
       mov      edx, 600
       call     [System.ReadOnlySpan`1[byte]:.ctor(ptr,int):this]
       mov      rax, bword ptr [rbp-0x10]
       mov      rdx, qword ptr [rbp-0x08]
 
G_M000_IG03:                ;; offset=0x003A
       add      rsp, 16
       pop      rbp
       ret      
 
; Total bytes of code 64

; Assembly listing for method Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float] (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 16
       lea      rbp, [rsp+0x10]
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
 
G_M000_IG02:                ;; offset=0x0010
       mov      dword ptr [rbp-0x08], 0x708
       mov      rax, 0x7F55F984BB98
       mov      bword ptr [rbp-0x10], rax
       mov      rax, bword ptr [rbp-0x10]
       mov      rdx, qword ptr [rbp-0x08]
 
G_M000_IG03:                ;; offset=0x002D
       add      rsp, 16
       pop      rbp
       ret      
 
; Total bytes of code 51

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
; partially interruptible
; compiling with minopt

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       sub      rsp, 304
       lea      rbp, [rsp+0x130]
       xor      eax, eax
       mov      qword ptr [rbp-0x128], rax
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
 
G_M000_IG02:                ;; offset=0x005C
       mov      dword ptr [rbp-0x108], 0x3E8
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 1
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x0079
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x100], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x110], rax
       mov      rsi, gword ptr [rbp-0x110]
       mov      rdi, gword ptr [rbp-0x100]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0x100]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG04:                ;; offset=0x00CC
       mov      rdi, bword ptr [rbp-0x30]
       mov      esi, 2
       call     [Tl.Playback:Has(ushort):bool:this]
       test     eax, eax
       je       SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00DF
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF8], rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x118], rax
       mov      rsi, gword ptr [rbp-0x118]
       mov      rdi, gword ptr [rbp-0xF8]
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, gword ptr [rbp-0xF8]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x0132
       lea      rdi, [rbp-0x48]
       call     [System.ReadOnlySpan`1[uint]:get_IsEmpty():bool:this]
       test     eax, eax
       je       SHORT G_M000_IG08
       mov      rdi, 0x7F557B59EAB0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x30]
       mov      rax, qword ptr [rax]
 
G_M000_IG07:                ;; offset=0x0156
       add      rsp, 304
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
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x4C]
       imul     ecx, dword ptr [rbp-0x54], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x58], eax
 
G_M000_IG09:                ;; offset=0x0191
       vmovdqu  xmm0, xmmword ptr [rbp-0x48]
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
 
G_M000_IG10:                ;; offset=0x019B
       xor      eax, eax
       mov      dword ptr [rbp-0x74], eax
       jmp      G_M000_IG22
 
G_M000_IG11:                ;; offset=0x01A5
       mov      eax, dword ptr [rbp-0x68]
       cmp      dword ptr [rbp-0x74], eax
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x74]
       mov      rcx, bword ptr [rbp-0x70]
       mov      eax, dword ptr [rcx+4*rax]
       mov      dword ptr [rbp-0xB4], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x78], eax
       mov      eax, dword ptr [rbp-0xB4]
       imul     ecx, dword ptr [rbp-0x78], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x7C], eax
       mov      eax, dword ptr [rbp-0xB4]
       mov      dword ptr [rbp-0xB8], eax
       mov      eax, dword ptr [rbp-0xB4]
       cmp      eax, dword ptr [rbp-0x4C]
       jb       SHORT G_M000_IG12
       mov      eax, dword ptr [rbp-0x78]
       sub      eax, dword ptr [rbp-0x54]
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0213
       mov      rdi, 0x7F557B59EAB4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x7C]
       cmp      eax, dword ptr [rbp-0x58]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x80], eax
       mov      eax, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0xBC], eax
 
G_M000_IG13:                ;; offset=0x023D
       mov      eax, dword ptr [rbp-0xBC]
       mov      dword ptr [rbp-0xC0], eax
       mov      eax, dword ptr [rbp-0x80]
       mov      ecx, dword ptr [rbp-0x50]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x025F
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xF0], rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x120], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x128], rax
       mov      rsi, gword ptr [rbp-0x120]
       mov      rdx, gword ptr [rbp-0x128]
       mov      rdi, gword ptr [rbp-0xF0]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0xF0]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG15:                ;; offset=0x02D5
       mov      eax, dword ptr [rbp-0x50]
       add      eax, dword ptr [rbp-0x80]
       movzx    rax, ax
       mov      dword ptr [rbp-0x50], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x84], eax
       mov      eax, dword ptr [rbp-0x84]
       lea      eax, [rax+2*rax]
       mov      dword ptr [rbp-0x88], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte]]
       mov      bword ptr [rbp-0xD0], rax
       mov      qword ptr [rbp-0xC8], rdx
 
G_M000_IG16:                ;; offset=0x030D
       vmovdqu  xmm0, xmmword ptr [rbp-0xD0]
       vmovdqu  xmmword ptr [rbp-0xB0], xmm0
 
G_M000_IG17:                ;; offset=0x031D
       mov      eax, dword ptr [rbp-0xA8]
       cmp      dword ptr [rbp-0x84], eax
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x84]
       mov      rcx, bword ptr [rbp-0xB0]
       movzx    rax, byte  ptr [rcx+rax]
       mov      dword ptr [rbp-0x8C], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float]]
       mov      bword ptr [rbp-0xE0], rax
       mov      qword ptr [rbp-0xD8], rdx
 
G_M000_IG18:                ;; offset=0x035A
       vmovdqu  xmm0, xmmword ptr [rbp-0xE0]
       vmovdqu  xmmword ptr [rbp-0xA0], xmm0
 
G_M000_IG19:                ;; offset=0x036A
       mov      eax, dword ptr [rbp-0xC0]
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0x8C], 0
       je       G_M000_IG21
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x98]
       cmp      dword ptr [rbp-0x88], eax
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x88]
       mov      rcx, bword ptr [rbp-0xA0]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0xE8], eax
       cmp      dword ptr [rbp-0x8C], 1
       jle      SHORT G_M000_IG20
       mov      rdi, 0x7F557B59EAB8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x88]
       inc      eax
       cmp      eax, dword ptr [rbp-0x98]
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x88]
       inc      eax
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0xA0]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG20:                ;; offset=0x0415
       mov      eax, dword ptr [rbp-0xE8]
       mov      dword ptr [rbp-0xE4], eax
       cmp      dword ptr [rbp-0x8C], 2
       jle      G_M000_IG27
       mov      rdi, 0x7F557B59EABC
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x38]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x88]
       add      eax, 2
       cmp      eax, dword ptr [rbp-0x98]
       jae      G_M000_IG28
       mov      eax, dword ptr [rbp-0x88]
       add      eax, 2
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0xA0]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x38]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG21:                ;; offset=0x0479
       mov      rdi, 0x7F557B59EAC0
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0xE4]
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x7C]
       mov      dword ptr [rbp-0x58], eax
       mov      eax, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0x54], eax
       mov      eax, dword ptr [rbp-0x74]
       inc      eax
       mov      dword ptr [rbp-0x74], eax
 
G_M000_IG22:                ;; offset=0x04A5
       mov      eax, dword ptr [rbp-0x108]
       dec      eax
       mov      dword ptr [rbp-0x108], eax
       cmp      dword ptr [rbp-0x108], 0
       jg       SHORT G_M000_IG24
 
G_M000_IG23:                ;; offset=0x04BC
       lea      rdi, [rbp-0x108]
       mov      esi, 296
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG24:                ;; offset=0x04CD
       mov      eax, dword ptr [rbp-0x74]
       cmp      eax, dword ptr [rbp-0x68]
       jl       G_M000_IG11
       mov      dword ptr [rbp-0x5C], 1
       cmp      dword ptr [rbp-0x58], 599
       jne      SHORT G_M000_IG25
       mov      rdi, 0x7F557B59EAC4
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x5C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x5C], eax
 
G_M000_IG25:                ;; offset=0x0504
       mov      rdi, 0x7F557B59EAC8
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x4C]
       mov      esi, dword ptr [rbp-0x50]
       mov      edx, dword ptr [rbp-0x5C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG26:                ;; offset=0x0523
       add      rsp, 304
       pop      rbp
       ret      
 
G_M000_IG27:                ;; offset=0x052C
       mov      rdi, 0x7F557B59EACC
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG21
 
G_M000_IG28:                ;; offset=0x0540
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 1350

; Assembly listing for method Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte] (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data
; 1 inlinees with PGO data; 0 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
 
G_M000_IG02:                ;; offset=0x0000
       mov      rax, 0x7F55F984B940
       mov      edx, 600
 
G_M000_IG03:                ;; offset=0x000F
       ret      
 
; Total bytes of code 16

; Assembly listing for method Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float] (Tier1)
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
       mov      rax, 0x7F55F984BB98
       mov      edx, 0x708
 
G_M000_IG03:                ;; offset=0x000F
       ret      
 
; Total bytes of code 16

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
; with Synthesized PGO: fgCalledCount is 82243
; 1 inlinees with PGO data; 6 single block inlinees; 0 inlinees without PGO data

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
       je       G_M000_IG16
       test     al, 2
       jne      G_M000_IG17
       test     ecx, ecx
       je       G_M000_IG18
 
G_M000_IG03:                ;; offset=0x0028
       mov      ebx, dword ptr [rdi]
       movzx    r15, word  ptr [rdi+0x04]
       mov      edi, ebx
       imul     rdi, rdi, 0x1B4E81B5
       shr      rdi, 38
       imul     eax, edi, 600
       mov      r14d, ebx
       sub      r14d, eax
       xor      eax, eax
       cmp      eax, ecx
       jl       G_M000_IG14
 
G_M000_IG04:                ;; offset=0x0052
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
 
G_M000_IG05:                ;; offset=0x0079
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:                ;; offset=0x0084
       align    [0 bytes for IG07]
 
G_M000_IG07:                ;; offset=0x0084
       mov      r10d, r9d
       sub      r10d, edi
 
G_M000_IG08:                ;; offset=0x008A
       mov      edi, r15d
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ebx, r10d
       cmp      rdi, rbx
       jl       G_M000_IG20
       add      r10d, r15d
       movzx    r15, r10w
       lea      edi, [r11+2*r11]
       cmp      r11d, 600
       jae      G_M000_IG21
       mov      r10d, r11d
       mov      r14, 0x7F55F984B940
       movzx    r10, byte  ptr [r14+r10]
       test     r10d, r10d
       je       SHORT G_M000_IG13
 
G_M000_IG09:                ;; offset=0x00D3
       vmovss   xmm0, dword ptr [rsi]
       cmp      edi, 0x708
       jae      G_M000_IG21
       mov      ebx, edi
       mov      r14, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r14+4*rbx]
       vmovss   dword ptr [rsi], xmm0
       cmp      r10d, 1
       jle      SHORT G_M000_IG11
 
G_M000_IG10:                ;; offset=0x00FF
       vmovss   xmm0, dword ptr [rsi]
       lea      ebx, [rdi+0x01]
       cmp      ebx, 0x708
       jae      G_M000_IG21
       vaddss   xmm0, xmm0, dword ptr [r14+4*rbx]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG11:                ;; offset=0x011C
       cmp      r10d, 2
       jle      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0122
       vmovss   xmm0, dword ptr [rsi]
       add      edi, 2
       cmp      edi, 0x708
       jae      G_M000_IG21
       vaddss   xmm0, xmm0, dword ptr [r14+4*rdi]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG13:                ;; offset=0x013F
       mov      ebx, r8d
       mov      r14d, r11d
       mov      edi, r9d
       inc      eax
       cmp      eax, ecx
       jge      G_M000_IG04
 
G_M000_IG14:                ;; offset=0x0152
       mov      r8d, dword ptr [rdx+4*rax]
       mov      r9d, r8d
       imul     r9, r9, 0x1B4E81B5
       shr      r9, 38
       imul     r10d, r9d, 600
       mov      r11d, r8d
       sub      r11d, r10d
       cmp      r8d, ebx
       jae      G_M000_IG07
 
G_M000_IG15:                ;; offset=0x017A
       cmp      r11d, r14d
       setb     r10b
       movzx    r10, r10b
       jmp      G_M000_IG08
 
G_M000_IG16:                ;; offset=0x018A
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG17:                ;; offset=0x01C6
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG18:                ;; offset=0x0202
       mov      rax, qword ptr [rdi]
 
G_M000_IG19:                ;; offset=0x0205
       add      rsp, 8
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG20:                ;; offset=0x0210
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r14
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG21:                ;; offset=0x0267
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 621

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
       mov      rdi, 0x7F557B5E47F8
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
       mov      rdi, 0x7F557B5E47FC
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
; 3 inlinees with PGO data; 13 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 48
       mov      qword ptr [rsp+0xB8], r15
       mov      qword ptr [rsp+0xB0], r14
       mov      qword ptr [rsp+0xA8], r13
       mov      qword ptr [rsp+0xA0], rbx
       lea      rbp, [rsp+0x30]
       mov      rdi, gword ptr [rbp+0x60]
       vmovss   xmm0, dword ptr [rbp+0x5C]
       mov      eax, dword ptr [rbp+0x4C]
 
G_M000_IG02:                ;; offset=0x003A
       mov      ebx, dword ptr [rbp+0x50]
       movzx    r15, word  ptr [rbp+0x54]
       movzx    r14, word  ptr [rbp+0x56]
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jg       SHORT G_M000_IG08
 
G_M000_IG03:                ;; offset=0x0050
       vmovd    edx, xmm0
       mov      eax, r15d
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       mov      ecx, r14d
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x006A
       add      rsp, 160
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x007A
       mov      ecx, 5
       jmp      SHORT G_M000_IG07
       align    [0 bytes for IG09]
 
G_M000_IG06:                ;; offset=0x0081
       mov      ecx, 1
       cmp      r8d, 599
       je       SHORT G_M000_IG05
 
G_M000_IG07:                ;; offset=0x008F
       mov      edx, ebx
       mov      esi, r15d
       shl      rsi, 32
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
       jle      SHORT G_M000_IG03
 
G_M000_IG08:                ;; offset=0x00C1
       test     rcx, rcx
       je       G_M000_IG19
       mov      edx, dword ptr [rcx+0x08]
       mov      esi, eax
       add      rsi, 8
       cmp      rdx, rsi
       jb       G_M000_IG19
       mov      edx, eax
       lea      rcx, bword ptr [rcx+4*rdx+0x10]
       test     r14b, 1
       je       G_M000_IG20
       test     r14b, 2
       jne      G_M000_IG21
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       imul     esi, edx, 600
       mov      r8d, ebx
       sub      r8d, esi
       xor      esi, esi
       mov      r9d, 9
       jmp      G_M000_IG16
 
G_M000_IG09:                ;; offset=0x011D
       mov      ebx, r11d
       sub      ebx, edx
 
G_M000_IG10:                ;; offset=0x0122
       mov      r8d, r15d
       neg      r8d
       add      r8d, 0xFFFF
       movsxd   rdx, r8d
       mov      r8d, ebx
       cmp      rdx, r8
       jl       G_M000_IG22
       add      r15d, ebx
       movzx    r15, r15w
       lea      edx, [r13+2*r13]
       cmp      r13d, 600
       jae      G_M000_IG23
       mov      r8d, r13d
       mov      rbx, 0x7F55F984B940
       movzx    r8, byte  ptr [rbx+r8]
       test     r8d, r8d
       je       SHORT G_M000_IG15
 
G_M000_IG11:                ;; offset=0x016E
       cmp      edx, 0x708
       jae      G_M000_IG23
       mov      ebx, edx
       mov      r14, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r14+4*rbx]
       cmp      r8d, 1
       jle      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0192
       lea      ebx, [rdx+0x01]
       cmp      ebx, 0x708
       jae      G_M000_IG23
       vaddss   xmm0, xmm0, dword ptr [r14+4*rbx]
 
G_M000_IG13:                ;; offset=0x01A7
       cmp      r8d, 2
       jle      SHORT G_M000_IG15
 
G_M000_IG14:                ;; offset=0x01AD
       add      edx, 2
       cmp      edx, 0x708
       jae      G_M000_IG23
       vaddss   xmm0, xmm0, dword ptr [r14+4*rdx]
 
G_M000_IG15:                ;; offset=0x01C2
       mov      ebx, r10d
       mov      r8d, r13d
       mov      edx, r11d
       add      rsi, 4
 
G_M000_IG16:                ;; offset=0x01CF
       dec      r9d
       je       G_M000_IG06
 
G_M000_IG17:                ;; offset=0x01D8
       mov      r10d, dword ptr [rcx+rsi]
       mov      r11d, r10d
       imul     r11, r11, 0x1B4E81B5
       shr      r11, 38
       imul     r14d, r11d, 600
       mov      r13d, r10d
       sub      r13d, r14d
       cmp      r10d, ebx
       jae      G_M000_IG09
 
G_M000_IG18:                ;; offset=0x0200
       cmp      r13d, r8d
       setb     bl
       movzx    rbx, bl
       jmp      G_M000_IG10
 
G_M000_IG19:                ;; offset=0x020E
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG20:                ;; offset=0x0215
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG21:                ;; offset=0x0251
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG22:                ;; offset=0x028D
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG23:                ;; offset=0x02E4
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 746

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedBatch8():Tl.FusionExperiment.SumReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 2
; 3 inlinees with PGO data; 13 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 32
       lea      rbp, [rsp+0x40]
 
G_M000_IG02:                ;; offset=0x0011
       vxorps   xmm0, xmm0, xmm0
       mov      rax, 0x1000000000000
       mov      qword ptr [rbp-0x28], rax
       mov      ebx, dword ptr [rbp-0x28]
       movzx    r15, word  ptr [rbp-0x24]
       movzx    r14, word  ptr [rbp-0x22]
       xor      eax, eax
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jg       G_M000_IG18
 
G_M000_IG03:                ;; offset=0x003F
       movzx    rax, r15w
       movzx    rdx, r14w
       mov      dword ptr [rbp-0x34], edx
       vmovd    edx, xmm0
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       movzx    rcx, word  ptr [rbp-0x34]
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0062
       add      rsp, 32
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x006F
       mov      ecx, 5
       jmp      G_M000_IG17
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0079
       mov      ebx, r14d
       sub      ebx, esi
 
G_M000_IG07:                ;; offset=0x007E
       mov      r9d, edx
       neg      r9d
       add      r9d, 0xFFFF
       movsxd   rsi, r9d
       mov      r9d, ebx
       cmp      rsi, r9
       jl       G_M000_IG22
       add      edx, ebx
       movzx    rdx, dx
       lea      esi, [r13+2*r13]
       cmp      r13d, 600
       jae      G_M000_IG23
       mov      r9d, r13d
       mov      rbx, 0x7F55F984B940
       movzx    r9, byte  ptr [rbx+r9]
       test     r9d, r9d
       je       SHORT G_M000_IG12
 
G_M000_IG08:                ;; offset=0x00C8
       cmp      esi, 0x708
       jae      G_M000_IG23
       mov      ebx, esi
       mov      r15, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r15+4*rbx]
       cmp      r9d, 1
       jle      SHORT G_M000_IG10
 
G_M000_IG09:                ;; offset=0x00EC
       lea      ebx, [rsi+0x01]
       cmp      ebx, 0x708
       jae      G_M000_IG23
       vaddss   xmm0, xmm0, dword ptr [r15+4*rbx]
 
G_M000_IG10:                ;; offset=0x0101
       cmp      r9d, 2
       jle      SHORT G_M000_IG12
 
G_M000_IG11:                ;; offset=0x0107
       add      esi, 2
       cmp      esi, 0x708
       jae      G_M000_IG23
       vaddss   xmm0, xmm0, dword ptr [r15+4*rsi]
 
G_M000_IG12:                ;; offset=0x011C
       mov      ebx, r11d
       mov      r9d, r13d
       mov      esi, r14d
       add      r8, 4
 
G_M000_IG13:                ;; offset=0x0129
       dec      r10d
       je       SHORT G_M000_IG16
 
G_M000_IG14:                ;; offset=0x012E
       mov      r11d, dword ptr [rcx+r8]
       mov      r15d, r11d
       imul     r14, r15, 0x1B4E81B5
       shr      r14, 38
       imul     r15d, r14d, 600
       mov      r13d, r11d
       sub      r13d, r15d
       cmp      r11d, ebx
       jae      G_M000_IG06
 
G_M000_IG15:                ;; offset=0x0156
       cmp      r13d, r9d
       setb     bl
       movzx    rbx, bl
       jmp      G_M000_IG07
 
G_M000_IG16:                ;; offset=0x0164
       mov      ecx, 1
       cmp      r9d, 599
       je       G_M000_IG05
 
G_M000_IG17:                ;; offset=0x0176
       mov      esi, ebx
       mov      edx, edx
       shl      rdx, 32
       or       rdx, rsi
       mov      ecx, ecx
       shl      rcx, 48
       or       rcx, rdx
       mov      qword ptr [rbp-0x30], rcx
       mov      ebx, dword ptr [rbp-0x30]
       movzx    r15, word  ptr [rbp-0x2C]
       movzx    r14, word  ptr [rbp-0x2A]
       add      eax, 8
       mov      rcx, gword ptr [rdi+0x08]
       cmp      dword ptr [rcx+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG18:                ;; offset=0x01AB
       test     rcx, rcx
       je       SHORT G_M000_IG19
       mov      edx, dword ptr [rcx+0x08]
       mov      esi, eax
       add      rsi, 8
       cmp      rdx, rsi
       jb       SHORT G_M000_IG19
       mov      edx, eax
       lea      rcx, bword ptr [rcx+4*rdx+0x10]
       movzx    rdx, r14w
       test     dl, 1
       je       SHORT G_M000_IG20
       movzx    rdx, r14w
       test     dl, 2
       jne      SHORT G_M000_IG21
       movzx    rdx, r15w
       mov      esi, ebx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     r8d, esi, 600
       mov      r9d, ebx
       sub      r9d, r8d
       xor      r8d, r8d
       mov      r10d, 9
       jmp      G_M000_IG13
 
G_M000_IG19:                ;; offset=0x0203
       call     [System.ThrowHelper:ThrowArgumentOutOfRangeException()]
       int3     
 
G_M000_IG20:                ;; offset=0x020A
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG21:                ;; offset=0x0246
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG22:                ;; offset=0x0282
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r14
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG23:                ;; offset=0x02D9
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 735

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
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x28], rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
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
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x20], rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
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
       sub      rsp, 160
       lea      rbp, [rsp+0xA0]
       xor      eax, eax
       mov      qword ptr [rbp-0x98], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x90], ymm8
       vmovdqu  ymmword ptr [rbp-0x70], ymm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       vmovdqa  xmmword ptr [rbp-0x30], xmm8
       mov      qword ptr [rbp-0x20], rax
       mov      dword ptr [rbp-0x18], eax
       mov      bword ptr [rbp-0x08], rdi
       mov      bword ptr [rbp-0x10], rsi
       mov      dword ptr [rbp-0x14], edx
 
G_M000_IG02:                ;; offset=0x0047
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x18], eax
       mov      eax, dword ptr [rbp-0x14]
       mov      ecx, 600
       xor      edx, edx
       div      edx:eax, ecx
       mov      dword ptr [rbp-0x1C], eax
       mov      eax, dword ptr [rbp-0x14]
       imul     ecx, dword ptr [rbp-0x1C], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x20], eax
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       cmp      eax, dword ptr [rbp-0x14]
       ja       SHORT G_M000_IG03
       mov      eax, dword ptr [rbp-0x1C]
       sub      eax, dword ptr [rbp-0x18]
       mov      dword ptr [rbp-0x24], eax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:                ;; offset=0x008D
       mov      rdi, 0x7F557B5EAB10
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x08]
       mov      eax, dword ptr [rax]
       imul     ecx, dword ptr [rbp-0x18], 600
       sub      eax, ecx
       mov      dword ptr [rbp-0x4C], eax
       mov      eax, dword ptr [rbp-0x20]
       cmp      eax, dword ptr [rbp-0x4C]
       setb     al
       movzx    rax, al
       mov      dword ptr [rbp-0x24], eax
 
G_M000_IG04:                ;; offset=0x00BD
       mov      eax, dword ptr [rbp-0x24]
       mov      rcx, bword ptr [rbp-0x08]
       movzx    rcx, word  ptr [rcx+0x04]
       neg      ecx
       add      ecx, 0xFFFF
       movsxd   rcx, ecx
       cmp      rax, rcx
       jle      SHORT G_M000_IG06
 
G_M000_IG05:                ;; offset=0x00D8
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x88], rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x90], rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      gword ptr [rbp-0x98], rax
       mov      rsi, gword ptr [rbp-0x90]
       mov      rdx, gword ptr [rbp-0x98]
       mov      rdi, gword ptr [rbp-0x88]
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, gword ptr [rbp-0x88]
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG06:                ;; offset=0x014E
       mov      rax, bword ptr [rbp-0x08]
       movzx    rax, word  ptr [rax+0x04]
       add      eax, dword ptr [rbp-0x24]
       movzx    rax, ax
       mov      dword ptr [rbp-0x28], eax
       mov      dword ptr [rbp-0x2C], 1
       cmp      dword ptr [rbp-0x20], 599
       jne      SHORT G_M000_IG07
       mov      rdi, 0x7F557B5EAB14
       call     CORINFO_HELP_COUNTPROFILE32
       mov      eax, dword ptr [rbp-0x2C]
       or       eax, 4
       movzx    rax, ax
       mov      dword ptr [rbp-0x2C], eax
 
G_M000_IG07:                ;; offset=0x018A
       mov      eax, dword ptr [rbp-0x20]
       mov      dword ptr [rbp-0x30], eax
       mov      eax, dword ptr [rbp-0x30]
       lea      eax, [rax+2*rax]
       mov      dword ptr [rbp-0x34], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Counts():System.ReadOnlySpan`1[byte]]
       mov      bword ptr [rbp-0x70], rax
       mov      qword ptr [rbp-0x68], rdx
 
G_M000_IG08:                ;; offset=0x01A7
       vmovdqu  xmm0, xmmword ptr [rbp-0x70]
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
 
G_M000_IG09:                ;; offset=0x01B1
       mov      eax, dword ptr [rbp-0x58]
       cmp      dword ptr [rbp-0x30], eax
       jae      G_M000_IG16
       mov      eax, dword ptr [rbp-0x30]
       mov      rcx, bword ptr [rbp-0x60]
       movzx    rax, byte  ptr [rcx+rax]
       mov      dword ptr [rbp-0x38], eax
       call     [Tl.FusionExperiment.FusedPulse:get_Amounts():System.ReadOnlySpan`1[float]]
       mov      bword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x78], rdx
 
G_M000_IG10:                ;; offset=0x01D9
       vmovdqu  xmm0, xmmword ptr [rbp-0x80]
       vmovdqu  xmmword ptr [rbp-0x48], xmm0
 
G_M000_IG11:                ;; offset=0x01E3
       cmp      dword ptr [rbp-0x38], 0
       je       G_M000_IG13
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x40]
       cmp      dword ptr [rbp-0x34], eax
       jae      G_M000_IG16
       mov      eax, dword ptr [rbp-0x34]
       mov      rcx, bword ptr [rbp-0x48]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
       cmp      dword ptr [rbp-0x38], 1
       jle      SHORT G_M000_IG12
       mov      rdi, 0x7F557B5EAB18
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x34]
       inc      eax
       cmp      eax, dword ptr [rbp-0x40]
       jae      G_M000_IG16
       mov      eax, dword ptr [rbp-0x34]
       inc      eax
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0x48]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG12:                ;; offset=0x0258
       cmp      dword ptr [rbp-0x38], 2
       jle      SHORT G_M000_IG15
       mov      rdi, 0x7F557B5EAB1C
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, bword ptr [rbp-0x10]
       vmovss   xmm0, dword ptr [rax]
       mov      eax, dword ptr [rbp-0x34]
       add      eax, 2
       cmp      eax, dword ptr [rbp-0x40]
       jae      SHORT G_M000_IG16
       mov      eax, dword ptr [rbp-0x34]
       add      eax, 2
       mov      eax, eax
       mov      rcx, bword ptr [rbp-0x48]
       vaddss   xmm0, xmm0, dword ptr [rcx+4*rax]
       mov      rax, bword ptr [rbp-0x10]
       vmovss   dword ptr [rax], xmm0
 
G_M000_IG13:                ;; offset=0x0299
       mov      rdi, 0x7F557B5EAB20
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, dword ptr [rbp-0x14]
       mov      esi, dword ptr [rbp-0x28]
       mov      edx, dword ptr [rbp-0x2C]
       call     [Tl.FusionExperiment.FusedPulse:Mint(uint,ushort,ushort):Tl.Playback]
       nop      
 
G_M000_IG14:                ;; offset=0x02B8
       add      rsp, 160
       pop      rbp
       ret      
 
G_M000_IG15:                ;; offset=0x02C1
       mov      rdi, 0x7F557B5EAB24
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      SHORT G_M000_IG13
 
G_M000_IG16:                ;; offset=0x02D2
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 728

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
       mov      rdi, 0x7F557B5E4740
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
       mov      rdi, 0x7F557B5E4744
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
; 2 inlinees with PGO data; 9 single block inlinees; 1 inlinees without PGO data

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
       mov      eax, eax
       cmp      dword ptr [rdi+0x08], eax
       jg       G_M000_IG14
 
G_M000_IG03:                ;; offset=0x004A
       vmovd    edx, xmm0
       mov      eax, r15d
       shl      rax, 32
       mov      ecx, ebx
       or       rax, rcx
       mov      ecx, r14d
       shl      rcx, 48
       or       rax, rcx
 
G_M000_IG04:                ;; offset=0x0064
       add      rsp, 136
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG05:                ;; offset=0x0072
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x0072
       sub      esi, edx
 
G_M000_IG07:                ;; offset=0x0074
       mov      edx, r15d
       neg      edx
       add      edx, 0xFFFF
       movsxd   rdx, edx
       mov      r8d, esi
       cmp      rdx, r8
       jl       G_M000_IG19
       add      esi, r15d
       movzx    rdx, si
       mov      esi, 1
       cmp      r9d, 599
       je       G_M000_IG16
 
G_M000_IG08:                ;; offset=0x00A6
       lea      r8d, [r9+2*r9]
       cmp      r9d, 600
       jae      G_M000_IG20
       mov      r9d, r9d
       mov      r10, 0x7F55F984B940
       movzx    r9, byte  ptr [r10+r9]
       test     r9d, r9d
       je       SHORT G_M000_IG13
 
G_M000_IG09:                ;; offset=0x00CE
       cmp      r8d, 0x708
       jae      G_M000_IG20
       mov      r10d, r8d
       mov      r11, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r11+4*r10]
       cmp      r9d, 1
       jle      SHORT G_M000_IG11
 
G_M000_IG10:                ;; offset=0x00F4
       lea      r10d, [r8+0x01]
       cmp      r10d, 0x708
       jae      G_M000_IG20
       vaddss   xmm0, xmm0, dword ptr [r11+4*r10]
 
G_M000_IG11:                ;; offset=0x010B
       cmp      r9d, 2
       jle      SHORT G_M000_IG13
 
G_M000_IG12:                ;; offset=0x0111
       add      r8d, 2
       cmp      r8d, 0x708
       jae      G_M000_IG20
       vaddss   xmm0, xmm0, dword ptr [r11+4*r8]
 
G_M000_IG13:                ;; offset=0x0128
       mov      ecx, ecx
       mov      edx, edx
       shl      rdx, 32
       or       rcx, rdx
       mov      edx, esi
       shl      rdx, 48
       or       rcx, rdx
       mov      qword ptr [rbp-0x20], rcx
       mov      ebx, dword ptr [rbp-0x20]
       movzx    r15, word  ptr [rbp-0x1C]
       movzx    r14, word  ptr [rbp-0x1A]
       inc      eax
       cmp      dword ptr [rdi+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG14:                ;; offset=0x0158
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG20
       mov      ecx, dword ptr [rdi+4*rax+0x10]
       test     r14b, 1
       je       SHORT G_M000_IG17
       test     r14b, 2
       jne      G_M000_IG18
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       mov      esi, ecx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     r8d, esi, 600
       mov      r9d, ecx
       sub      r9d, r8d
       cmp      ecx, ebx
       jae      G_M000_IG06
 
G_M000_IG15:                ;; offset=0x01A4
       imul     esi, edx, 600
       sub      ebx, esi
       cmp      ebx, r9d
       seta     sil
       movzx    rsi, sil
       jmp      G_M000_IG07
 
G_M000_IG16:                ;; offset=0x01BC
       mov      esi, 5
       jmp      G_M000_IG08
 
G_M000_IG17:                ;; offset=0x01C6
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG18:                ;; offset=0x0202
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, r14
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, r14
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG19:                ;; offset=0x023E
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rbx, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, rbx
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG20:                ;; offset=0x0295
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 667

; Assembly listing for method Tl.FusionExperiment.FusedPulse:Forward(byref,byref,uint):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data
; 2 inlinees with PGO data; 5 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:                ;; offset=0x0000
       push     rbp
       push     r15
       push     rbx
       lea      rbp, [rsp+0x10]
 
G_M000_IG02:                ;; offset=0x0009
       movzx    rax, word  ptr [rdi+0x06]
       test     al, 1
       je       G_M000_IG12
       test     al, 2
       jne      G_M000_IG13
       mov      eax, dword ptr [rdi]
       mov      ecx, eax
       imul     rcx, rcx, 0x1B4E81B5
       shr      rcx, 38
       mov      r8d, edx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       imul     r9d, r8d, 600
       mov      r10d, edx
       sub      r10d, r9d
       cmp      edx, eax
       jb       G_M000_IG10
 
G_M000_IG03:                ;; offset=0x004F
       sub      r8d, ecx
 
G_M000_IG04:                ;; offset=0x0052
       movzx    rax, word  ptr [rdi+0x04]
       mov      edi, eax
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ecx, r8d
       cmp      rdi, rcx
       jl       G_M000_IG14
       add      eax, r8d
       movzx    rax, ax
       mov      edi, 1
       mov      ecx, 5
       cmp      r10d, 599
       cmove    edi, ecx
       lea      ecx, [r10+2*r10]
       cmp      r10d, 600
       jae      G_M000_IG15
       mov      r8d, r10d
       mov      r9, 0x7F55F984B940
       movzx    r8, byte  ptr [r9+r8]
       test     r8d, r8d
       je       SHORT G_M000_IG08
 
G_M000_IG05:                ;; offset=0x00B1
       vmovss   xmm0, dword ptr [rsi]
       cmp      ecx, 0x708
       jae      G_M000_IG15
       mov      r9d, ecx
       mov      r10, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r10+4*r9]
       vmovss   dword ptr [rsi], xmm0
       cmp      r8d, 1
       jle      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x00DE
       vmovss   xmm0, dword ptr [rsi]
       lea      r9d, [rcx+0x01]
       cmp      r9d, 0x708
       jae      G_M000_IG15
       vaddss   xmm0, xmm0, dword ptr [r10+4*r9]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG07:                ;; offset=0x00FD
       cmp      r8d, 2
       jg       SHORT G_M000_IG11
 
G_M000_IG08:                ;; offset=0x0103
       mov      ecx, edx
       mov      eax, eax
       shl      rax, 32
       or       rax, rcx
       mov      edi, edi
       shl      rdi, 48
       or       rax, rdi
 
G_M000_IG09:                ;; offset=0x0117
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG10:                ;; offset=0x011C
       imul     r8d, ecx, 600
       sub      eax, r8d
       cmp      eax, r10d
       seta     r8b
       movzx    r8, r8b
       jmp      G_M000_IG04
 
G_M000_IG11:                ;; offset=0x0136
       vmovss   xmm0, dword ptr [rsi]
       lea      r8d, [rcx+0x02]
       cmp      r8d, 0x708
       jae      G_M000_IG15
       add      ecx, 2
       vaddss   xmm0, xmm0, dword ptr [r10+4*rcx]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG08
 
G_M000_IG12:                ;; offset=0x015A
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG13:                ;; offset=0x0196
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG14:                ;; offset=0x01D2
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG15:                ;; offset=0x0229
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 559

; Assembly listing for method Tl.FusionExperiment.FusedPulse:ForwardOne(byref,byref,uint):Tl.Playback (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 10978
; 1 inlinees with PGO data; 3 single block inlinees; 0 inlinees without PGO data

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
       mov      r8d, edx
       imul     r8, r8, 0x1B4E81B5
       shr      r8, 38
       imul     r9d, r8d, 600
       mov      r10d, edx
       sub      r10d, r9d
       cmp      edx, eax
       jb       G_M000_IG10
 
G_M000_IG03:                ;; offset=0x003B
       sub      r8d, ecx
 
G_M000_IG04:                ;; offset=0x003E
       movzx    rax, word  ptr [rdi+0x04]
       mov      edi, eax
       neg      edi
       add      edi, 0xFFFF
       movsxd   rdi, edi
       mov      ecx, r8d
       cmp      rdi, rcx
       jl       G_M000_IG12
       add      eax, r8d
       movzx    rax, ax
       mov      edi, 1
       mov      ecx, 5
       cmp      r10d, 599
       cmove    edi, ecx
       lea      ecx, [r10+2*r10]
       cmp      r10d, 600
       jae      G_M000_IG13
       mov      r8d, r10d
       mov      r9, 0x7F55F984B940
       movzx    r8, byte  ptr [r9+r8]
       test     r8d, r8d
       je       SHORT G_M000_IG08
 
G_M000_IG05:                ;; offset=0x009D
       vmovss   xmm0, dword ptr [rsi]
       cmp      ecx, 0x708
       jae      G_M000_IG13
       mov      r9d, ecx
       mov      r10, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r10+4*r9]
       vmovss   dword ptr [rsi], xmm0
       cmp      r8d, 1
       jle      SHORT G_M000_IG07
 
G_M000_IG06:                ;; offset=0x00CA
       vmovss   xmm0, dword ptr [rsi]
       lea      r9d, [rcx+0x01]
       cmp      r9d, 0x708
       jae      G_M000_IG13
       vaddss   xmm0, xmm0, dword ptr [r10+4*r9]
       vmovss   dword ptr [rsi], xmm0
 
G_M000_IG07:                ;; offset=0x00E9
       cmp      r8d, 2
       jg       SHORT G_M000_IG11
 
G_M000_IG08:                ;; offset=0x00EF
       mov      ecx, edx
       mov      eax, eax
       shl      rax, 32
       or       rax, rcx
       mov      edi, edi
       shl      rdi, 48
       or       rax, rdi
 
G_M000_IG09:                ;; offset=0x0103
       pop      rbx
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG10:                ;; offset=0x0108
       imul     r8d, ecx, 600
       sub      eax, r8d
       cmp      eax, r10d
       seta     r8b
       movzx    r8, r8b
       jmp      G_M000_IG04
 
G_M000_IG11:                ;; offset=0x0122
       vmovss   xmm0, dword ptr [rsi]
       lea      r8d, [rcx+0x02]
       cmp      r8d, 0x708
       jae      SHORT G_M000_IG13
       add      ecx, 2
       vaddss   xmm0, xmm0, dword ptr [r10+4*rcx]
       vmovss   dword ptr [rsi], xmm0
       jmp      SHORT G_M000_IG08
 
G_M000_IG12:                ;; offset=0x0142
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r15, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r15
       mov      rdi, rbx
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG13:                ;; offset=0x0199
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 415

; Assembly listing for method Tl.FusionExperiment.FusionBenchmarks:FusedSingle():Tl.FusionExperiment.SumReceipt:this (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 100
; 2 inlinees with PGO data; 9 single block inlinees; 1 inlinees without PGO data

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
       mov      rdi, gword ptr [rdi+0x08]
       xor      eax, eax
       cmp      dword ptr [rdi+0x08], eax
       jg       G_M000_IG13
 
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
       align    [0 bytes for IG06]
 
G_M000_IG06:                ;; offset=0x006B
       imul     esi, edx, 600
       sub      ebx, esi
       cmp      ebx, r9d
       seta     sil
       movzx    rsi, sil
 
G_M000_IG07:                ;; offset=0x007E
       movzx    rdx, r15w
       neg      edx
       add      edx, 0xFFFF
       movsxd   rdx, edx
       mov      r8d, esi
       cmp      rdx, r8
       jl       G_M000_IG19
       add      esi, r15d
       movzx    rdx, si
       mov      esi, 1
       cmp      r9d, 599
       je       G_M000_IG15
 
G_M000_IG08:                ;; offset=0x00B1
       lea      r8d, [r9+2*r9]
       cmp      r9d, 600
       jae      G_M000_IG20
       mov      r9d, r9d
       mov      r10, 0x7F55F984B940
       movzx    r9, byte  ptr [r10+r9]
       test     r9d, r9d
       je       SHORT G_M000_IG12
 
G_M000_IG09:                ;; offset=0x00D9
       cmp      r8d, 0x708
       jae      G_M000_IG20
       mov      r10d, r8d
       mov      r11, 0x7F55F984BB98
       vaddss   xmm0, xmm0, dword ptr [r11+4*r10]
       cmp      r9d, 1
       jle      SHORT G_M000_IG11
 
G_M000_IG10:                ;; offset=0x00FF
       lea      r10d, [r8+0x01]
       cmp      r10d, 0x708
       jae      G_M000_IG20
       vaddss   xmm0, xmm0, dword ptr [r11+4*r10]
 
G_M000_IG11:                ;; offset=0x0116
       cmp      r9d, 2
       jg       G_M000_IG16
 
G_M000_IG12:                ;; offset=0x0120
       mov      ecx, ecx
       mov      edx, edx
       shl      rdx, 32
       or       rcx, rdx
       mov      edx, esi
       shl      rdx, 48
       or       rcx, rdx
       mov      qword ptr [rbp-0x28], rcx
       mov      ebx, dword ptr [rbp-0x28]
       movzx    r15, word  ptr [rbp-0x24]
       movzx    r14, word  ptr [rbp-0x22]
       inc      eax
       cmp      dword ptr [rdi+0x08], eax
       jle      G_M000_IG03
 
G_M000_IG13:                ;; offset=0x0150
       mov      ecx, dword ptr [rdi+4*rax+0x10]
       movzx    rdx, r14w
       test     dl, 1
       je       SHORT G_M000_IG17
       movzx    rdx, r14w
       test     dl, 2
       jne      G_M000_IG18
       mov      edx, ebx
       imul     rdx, rdx, 0x1B4E81B5
       shr      rdx, 38
       mov      esi, ecx
       imul     rsi, rsi, 0x1B4E81B5
       shr      rsi, 38
       imul     r8d, esi, 600
       mov      r9d, ecx
       sub      r9d, r8d
       cmp      ecx, ebx
       jb       G_M000_IG06
 
G_M000_IG14:                ;; offset=0x0199
       sub      esi, edx
       jmp      G_M000_IG07
 
G_M000_IG15:                ;; offset=0x01A0
       mov      esi, 5
       jmp      G_M000_IG08
 
G_M000_IG16:                ;; offset=0x01AA
       add      r8d, 2
       cmp      r8d, 0x708
       jae      G_M000_IG20
       vaddss   xmm0, xmm0, dword ptr [r11+4*r8]
       jmp      G_M000_IG12
 
G_M000_IG17:                ;; offset=0x01C6
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x3FC
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG18:                ;; offset=0x0202
       mov      rdi, 0x7F557B0D3948
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       mov      edi, 0x474
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rsi, rax
       mov      rdi, rbx
       call     [System.InvalidOperationException:.ctor(System.String):this]
       mov      rdi, rbx
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG19:                ;; offset=0x023E
       mov      rdi, 0x7F557B383EA0
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      edi, 0x49E
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      r14, rax
       mov      edi, 0x4AA
       mov      rsi, 0x7F557AF31A30
       call     [CORINFO_HELP_STRCNS]
       mov      rdx, rax
       mov      rsi, r14
       mov      rdi, r15
       call     [System.ArgumentOutOfRangeException:.ctor(System.String,System.String):this]
       mov      rdi, r15
       call     CORINFO_HELP_THROW
       int3     
 
G_M000_IG20:                ;; offset=0x0295
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 667


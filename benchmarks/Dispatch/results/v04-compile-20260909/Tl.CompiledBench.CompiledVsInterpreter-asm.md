## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Jit(IterationCount=12, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.CompiledBench.CompiledVsInterpreter.InterpreterSingleTick()
;         for (var i = 0; i < SingleOps; i++)
;              ^^^^^^^^^
;             _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, NextTick(interpreter: true));
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return _playback;
;         ^^^^^^^^^^^^^^^^^
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+80]
       vxorps    xmm8,xmm8,xmm8
       vmovdqu   ymmword ptr [rbp-70],ymm8
       vmovdqu   ymmword ptr [rbp-50],ymm8
       xor       eax,eax
       mov       [rbp-30],rax
       mov       rbx,rdi
       mov       r15d,400
M00_L00:
       movzx     r14d,word ptr [rbx+14]
       lea       r13,[rbx+44]
       lea       r12,[rbx+18]
       lea       rax,[rbx+20]
       mov       [rbp-78],rax
       cmp       dword ptr [rbx+10],258
       je        near ptr M00_L03
M00_L01:
       mov       edi,[rbx+10]
       lea       ecx,[rdi+1]
       mov       [rbx+10],ecx
       xor       ecx,ecx
       mov       [rbp-2C],ecx
       mov       [rbp-2C],edi
       mov       edi,r14d
       call      qword ptr [7F6F7D11F5A0]; Tl.Timeline.Live(UInt16)
       mov       [rbp-80],rax
       mov       rdi,r13
       call      qword ptr [7F6F7D1BCE58]; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       mov       rdi,7F5FD8000AC0
       mov       rdi,[rdi]
       mov       esi,[rdi+8]
       cmp       esi,r14d
       jle       short M00_L04
       mov       esi,r14d
       imul      rsi,38
       mov       rax,[rbp-80]
       cmp       [rdi+rsi+10],rax
       jne       short M00_L05
       lea       rdi,[rdi+rsi+10]
       mov       r10,[rdi+8]
M00_L02:
       lea       r8,[rbp-2C]
       mov       r9d,1
       mov       rdi,rax
       mov       rsi,r13
       mov       rdx,r12
       mov       rcx,[rbp-78]
       call      r10
       mov       [rbp-38],rax
       mov       [rbx+44],rax
       dec       r15d
       jne       near ptr M00_L00
       mov       rax,[rbx+44]
       add       rsp,58
       pop       rbx
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M00_L03:
       xor       edi,edi
       mov       [rbx+10],edi
       movzx     edi,word ptr [rbx+14]
       call      qword ptr [7F6F7D11F5A0]; Tl.Timeline.Live(UInt16)
       xor       edi,edi
       mov       [rbx+44],edi
       mov       dword ptr [rbx+48],10000
       jmp       near ptr M00_L01
M00_L04:
       mov       rax,[rbp-80]
M00_L05:
       lea       rdi,[rbp-70]
       mov       esi,r14d
       mov       rdx,rax
       call      qword ptr [7F6F7D1BCED0]
       mov       r10,[rbp-68]
       mov       rax,[rbp-80]
       jmp       short M00_L02
; Total bytes of code 297
```
```assembly
; Tl.Timeline.Live(UInt16)
;         if (index == None)
;         ^^^^^^^^^^^^^^^^^^
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         var slots = Volatile.Read(ref s_slots);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         if ((uint)index >= (uint)slots.Length)
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return Volatile.Read(ref slots[index])
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;                                                                                                           
       push      rbp
       push      r15
       push      r14
       push      rbx
       push      rax
       lea       rbp,[rsp+20]
       mov       ebx,edi
       movzx     eax,bx
       cmp       eax,0FFFF
       je        short M01_L00
       mov       rdi,7F5FD8000AA8
       mov       rdi,[rdi]
       mov       ecx,[rdi+8]
       cmp       ecx,eax
       jle       near ptr M01_L01
       mov       rax,[rdi+rax*8+10]
       test      rax,rax
       je        near ptr M01_L02
       add       rsp,8
       pop       rbx
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L00:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       r14,rax
       mov       edi,209
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F6F7D11CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L01:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F6F7D11CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L02:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F6F7D11CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 412
```
```assembly
; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
;         if (!playback.Has(PlaybackFlags.Started))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new InvalidOperationException("Playback was never started; mint one with Timeline.Start.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         if (playback.Has(PlaybackFlags.Stopped))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new InvalidOperationException("Playback is stopped.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       push      rbp
       push      rbx
       push      rax
       lea       rbp,[rsp+10]
       movzx     edi,word ptr [rdi+6]
       test      dil,1
       je        short M02_L00
       test      dil,2
       jne       short M02_L01
       add       rsp,8
       pop       rbx
       pop       rbp
       ret
M02_L00:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,76F
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F6F7CBD5890]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M02_L01:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,7E3
       mov       rsi,7F6F7CAB78E8
       call      qword ptr [7F6F7C64F348]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F6F7CBD5890]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 151
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Jit(IterationCount=12, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.CompiledBench.CompiledVsInterpreter.InterpreterBatch8()
;         for (var i = 0; i < BatchOps; i++)
;              ^^^^^^^^^
;             if (i == 0)
;             ^^^^^^^^^^^
;                 _playback = Timeline.Start(_id);
;                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, _batchTicks.AsSpan(i * BatchSize, BatchSize));
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return _playback;
;         ^^^^^^^^^^^^^^^^^
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+80]
       xor       eax,eax
       mov       [rbp-68],rax
       vxorps    xmm8,xmm8,xmm8
       vmovdqu   ymmword ptr [rbp-60],ymm8
       vmovdqa   xmmword ptr [rbp-40],xmm8
       mov       rbx,rdi
       xor       r15d,r15d
M00_L00:
       movzx     edi,word ptr [rbx+14]
       call      qword ptr [7F8521B2F5A0]; Tl.Timeline.Live(UInt16)
       xor       edi,edi
       mov       [rbx+44],edi
       mov       dword ptr [rbx+48],10000
M00_L01:
       movzx     r14d,word ptr [rbx+14]
       lea       r13,[rbx+44]
       lea       r12,[rbx+18]
       lea       rax,[rbx+20]
       mov       [rbp-70],rax
       mov       rdi,[rbx+8]
       lea       ecx,[r15*8]
       test      rdi,rdi
       je        near ptr M00_L06
       mov       edx,[rdi+8]
       mov       esi,ecx
       add       rsi,8
       cmp       rdx,rsi
       jb        near ptr M00_L06
       lea       rcx,[rdi+rcx*4+10]
       mov       [rbp-80],rcx
       mov       edi,r14d
       call      qword ptr [7F8521B2F5A0]; Tl.Timeline.Live(UInt16)
       mov       [rbp-78],rax
       mov       rdi,r13
       call      qword ptr [7F8521BCCE70]; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       mov       rdi,7F757C000AC0
       mov       rdi,[rdi]
       mov       esi,[rdi+8]
       cmp       esi,r14d
       jle       short M00_L04
       mov       esi,r14d
       imul      rsi,38
       mov       rax,[rbp-78]
       cmp       [rdi+rsi+10],rax
       jne       short M00_L05
       lea       rdi,[rdi+rsi+10]
       mov       r10,[rdi+8]
M00_L02:
       mov       r8,[rbp-80]
       mov       r9d,8
       mov       rdi,rax
       mov       rsi,r13
       mov       rdx,r12
       mov       rcx,[rbp-70]
       call      r10
       mov       [rbp-30],rax
       mov       [rbx+44],rax
       inc       r15d
       cmp       r15d,4B
       jge       short M00_L03
       test      r15d,r15d
       jne       near ptr M00_L01
       jmp       near ptr M00_L00
M00_L03:
       mov       rax,[rbx+44]
       add       rsp,58
       pop       rbx
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M00_L04:
       mov       rax,[rbp-78]
M00_L05:
       lea       rdi,[rbp-68]
       mov       esi,r14d
       mov       rdx,rax
       call      qword ptr [7F8521BCCEE8]
       mov       r10,[rbp-60]
       mov       rax,[rbp-78]
       jmp       short M00_L02
M00_L06:
       call      qword ptr [7F8521457ED0]
       int       3
; Total bytes of code 323
```
```assembly
; Tl.Timeline.Live(UInt16)
;         if (index == None)
;         ^^^^^^^^^^^^^^^^^^
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         var slots = Volatile.Read(ref s_slots);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         if ((uint)index >= (uint)slots.Length)
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return Volatile.Read(ref slots[index])
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;                                                                                                           
       push      rbp
       push      r15
       push      r14
       push      rbx
       push      rax
       lea       rbp,[rsp+20]
       mov       ebx,edi
       movzx     eax,bx
       cmp       eax,0FFFF
       je        short M01_L00
       mov       rdi,7F757C000AA8
       mov       rdi,[rdi]
       mov       ecx,[rdi+8]
       cmp       ecx,eax
       jle       near ptr M01_L01
       mov       rax,[rdi+rax*8+10]
       test      rax,rax
       je        near ptr M01_L02
       add       rsp,8
       pop       rbx
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L00:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       r14,rax
       mov       edi,209
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F8521B2CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L01:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F8521B2CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L02:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F8521B2CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 412
```
```assembly
; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
;         if (!playback.Has(PlaybackFlags.Started))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new InvalidOperationException("Playback was never started; mint one with Timeline.Start.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         if (playback.Has(PlaybackFlags.Stopped))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new InvalidOperationException("Playback is stopped.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       push      rbp
       push      rbx
       push      rax
       lea       rbp,[rsp+10]
       movzx     edi,word ptr [rdi+6]
       test      dil,1
       je        short M02_L00
       test      dil,2
       jne       short M02_L01
       add       rsp,8
       pop       rbx
       pop       rbp
       ret
M02_L00:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,76F
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F85215E5890]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M02_L01:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,7E3
       mov       rsi,7F85214C78E8
       call      qword ptr [7F852105F348]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F85215E5890]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 151
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Jit(IterationCount=12, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.CompiledBench.CompiledVsInterpreter.SumInterpreterSingleTick()
;         for (var i = 0; i < SingleOps; i++)
;              ^^^^^^^^^
;             _playback = Timeline.Forward(_id, in _playback, in _input, ref _sum, NextTick(interpreter: true));
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return _playback;
;         ^^^^^^^^^^^^^^^^^
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+80]
       vxorps    xmm8,xmm8,xmm8
       vmovdqu   ymmword ptr [rbp-70],ymm8
       vmovdqu   ymmword ptr [rbp-50],ymm8
       xor       eax,eax
       mov       [rbp-30],rax
       mov       rbx,rdi
       mov       r15d,400
M00_L00:
       movzx     r14d,word ptr [rbx+14]
       lea       r13,[rbx+44]
       lea       r12,[rbx+18]
       lea       rax,[rbx+40]
       mov       [rbp-78],rax
       cmp       dword ptr [rbx+10],258
       je        near ptr M00_L03
M00_L01:
       mov       edi,[rbx+10]
       lea       ecx,[rdi+1]
       mov       [rbx+10],ecx
       xor       ecx,ecx
       mov       [rbp-2C],ecx
       mov       [rbp-2C],edi
       mov       edi,r14d
       call      qword ptr [7F001892F5A0]; Tl.Timeline.Live(UInt16)
       mov       [rbp-80],rax
       mov       rdi,r13
       call      qword ptr [7F00189CCE58]; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       mov       rdi,7EF074000AF0
       mov       rdi,[rdi]
       mov       esi,[rdi+8]
       cmp       esi,r14d
       jle       short M00_L04
       mov       esi,r14d
       imul      rsi,38
       mov       rax,[rbp-80]
       cmp       [rdi+rsi+10],rax
       jne       short M00_L05
       lea       rdi,[rdi+rsi+10]
       mov       r10,[rdi+8]
M00_L02:
       lea       r8,[rbp-2C]
       mov       r9d,1
       mov       rdi,rax
       mov       rsi,r13
       mov       rdx,r12
       mov       rcx,[rbp-78]
       call      r10
       mov       [rbp-38],rax
       mov       [rbx+44],rax
       dec       r15d
       jne       near ptr M00_L00
       mov       rax,[rbx+44]
       add       rsp,58
       pop       rbx
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M00_L03:
       xor       edi,edi
       mov       [rbx+10],edi
       movzx     edi,word ptr [rbx+14]
       call      qword ptr [7F001892F5A0]; Tl.Timeline.Live(UInt16)
       xor       edi,edi
       mov       [rbx+44],edi
       mov       dword ptr [rbx+48],10000
       jmp       near ptr M00_L01
M00_L04:
       mov       rax,[rbp-80]
M00_L05:
       lea       rdi,[rbp-70]
       mov       esi,r14d
       mov       rdx,rax
       call      qword ptr [7F00189CCED0]
       mov       r10,[rbp-68]
       mov       rax,[rbp-80]
       jmp       short M00_L02
; Total bytes of code 297
```
```assembly
; Tl.Timeline.Live(UInt16)
;         if (index == None)
;         ^^^^^^^^^^^^^^^^^^
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         var slots = Volatile.Read(ref s_slots);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         if ((uint)index >= (uint)slots.Length)
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return Volatile.Read(ref slots[index])
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;                                                                                                           
       push      rbp
       push      r15
       push      r14
       push      rbx
       push      rax
       lea       rbp,[rsp+20]
       mov       ebx,edi
       movzx     eax,bx
       cmp       eax,0FFFF
       je        short M01_L00
       mov       rdi,7EF074000AA8
       mov       rdi,[rdi]
       mov       ecx,[rdi+8]
       cmp       ecx,eax
       jle       near ptr M01_L01
       mov       rax,[rdi+rax*8+10]
       test      rax,rax
       je        near ptr M01_L02
       add       rsp,8
       pop       rbx
       pop       r14
       pop       r15
       pop       rbp
       ret
M01_L00:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       r14,rax
       mov       edi,209
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F001892CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L01:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F001892CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L02:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F001892CD20]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 412
```
```assembly
; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
;         if (!playback.Has(PlaybackFlags.Started))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new InvalidOperationException("Playback was never started; mint one with Timeline.Start.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         if (playback.Has(PlaybackFlags.Stopped))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             throw new InvalidOperationException("Playback is stopped.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       push      rbp
       push      rbx
       push      rax
       lea       rbp,[rsp+10]
       movzx     edi,word ptr [rdi+6]
       test      dil,1
       je        short M02_L00
       test      dil,2
       jne       short M02_L01
       add       rsp,8
       pop       rbx
       pop       rbp
       ret
M02_L00:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,76F
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F00183E5890]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M02_L01:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,7E3
       mov       rsi,7F00182C78E8
       call      qword ptr [7F0017E5F348]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F00183E5890]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 151
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: NoTiering(EnvironmentVariables=DOTNET_TieredCompilation=0, IterationCount=12, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.CompiledBench.CompiledVsInterpreter.InterpreterSingleTick()
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+80]
       vxorps    xmm8,xmm8,xmm8
       vmovdqu   ymmword ptr [rbp-70],ymm8
       vmovdqu   ymmword ptr [rbp-50],ymm8
       xor       eax,eax
       mov       [rbp-30],rax
       mov       rbx,rdi
;         for (var i = 0; i < SingleOps; i++)
;              ^^^^^^^^^
       mov       r15d,400
;             _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, NextTick(interpreter: true));
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M00_L00:
       movzx     r14d,word ptr [rbx+14]
       lea       r13,[rbx+44]
       lea       r12,[rbx+18]
       lea       rax,[rbx+20]
       mov       [rbp-78],rax
       cmp       dword ptr [rbx+10],258
       jne       short M00_L01
       xor       edi,edi
       mov       [rbx+10],edi
       movzx     edi,word ptr [rbx+14]
       call      qword ptr [7F7DBF1CD2F0]; Tl.Timeline.Live(UInt16)
       xor       edi,edi
       mov       [rbx+44],edi
       mov       dword ptr [rbx+48],10000
M00_L01:
       mov       edi,[rbx+10]
       lea       ecx,[rdi+1]
       mov       [rbx+10],ecx
       xor       ecx,ecx
       mov       [rbp-2C],ecx
       mov       [rbp-2C],edi
       mov       edi,r14d
       call      qword ptr [7F7DBF1CD2F0]; Tl.Timeline.Live(UInt16)
       mov       [rbp-80],rax
       mov       rdi,r13
       call      qword ptr [7F7DBF267708]; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       mov       rdi,7F6E18000BF8
       mov       rax,[rdi]
       cmp       [rax+8],r14d
       jg        short M00_L03
       mov       r8,[rbp-80]
M00_L02:
       lea       rdi,[rbp-70]
       mov       esi,r14d
       mov       rdx,r8
       call      qword ptr [7F7DBF267720]
       jmp       short M00_L04
M00_L03:
       mov       edi,r14d
       imul      rcx,rdi,38
       mov       r8,[rbp-80]
       cmp       [rax+rcx+10],r8
       jne       short M00_L02
       vmovdqu   ymm0,ymmword ptr [rax+rcx+10]
       vmovdqu   ymmword ptr [rbp-70],ymm0
       vmovdqu   ymm0,ymmword ptr [rax+rcx+28]
       vmovdqu   ymmword ptr [rbp-58],ymm0
M00_L04:
       mov       rax,[rbp-68]
       lea       r8,[rbp-2C]
       mov       r9d,1
       mov       rdi,[rbp-80]
       mov       rsi,r13
       mov       rdx,r12
       mov       rcx,[rbp-78]
       call      rax
       mov       [rbp-38],rax
       mov       [rbx+44],rax
       dec       r15d
       jne       near ptr M00_L00
;         return _playback;
;         ^^^^^^^^^^^^^^^^^
       mov       rax,[rbx+44]
       vzeroupper
       add       rsp,58
       pop       rbx
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 298
```
```assembly
; Tl.Timeline.Live(UInt16)
       push      rbp
       push      r15
       push      r14
       push      rbx
       push      rax
       lea       rbp,[rsp+20]
       mov       ebx,edi
;         if (index == None)
;         ^^^^^^^^^^^^^^^^^^
       movzx     eax,bx
       cmp       eax,0FFFF
       je        short M01_L00
;         var slots = Volatile.Read(ref s_slots);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       mov       rdi,7F6E18000B90
       mov       rdi,[rdi]
;         if ((uint)index >= (uint)slots.Length)
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       mov       ecx,[rdi+8]
       cmp       ecx,eax
       jle       near ptr M01_L01
;         return Volatile.Read(ref slots[index])
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;                                                                                                           
       mov       rax,[rdi+rax*8+10]
       test      rax,rax
       je        near ptr M01_L02
       add       rsp,8
       pop       rbx
       pop       r14
       pop       r15
       pop       rbp
       ret
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M01_L00:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       r14,rax
       mov       edi,209
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F7DBF1CC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M01_L01:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F7DBF1CC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L02:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F7DBF1CC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 412
```
```assembly
; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       push      rbp
       push      rbx
       push      rax
       lea       rbp,[rsp+10]
;         if (!playback.Has(PlaybackFlags.Started))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       movzx     edi,word ptr [rdi+6]
       test      dil,1
       je        short M02_L00
;         if (playback.Has(PlaybackFlags.Stopped))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       test      dil,2
       jne       short M02_L01
       add       rsp,8
       pop       rbx
       pop       rbp
       ret
;             throw new InvalidOperationException("Playback was never started; mint one with Timeline.Start.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M02_L00:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,76F
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F7DBECA61F0]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
;             throw new InvalidOperationException("Playback is stopped.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M02_L01:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,7E3
       mov       rsi,7F7DBEB9C9B0
       call      qword ptr [7F7DBEAA4450]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F7DBECA61F0]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 151
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: NoTiering(EnvironmentVariables=DOTNET_TieredCompilation=0, IterationCount=12, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.CompiledBench.CompiledVsInterpreter.InterpreterBatch8()
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+80]
       xor       eax,eax
       mov       [rbp-68],rax
       vxorps    xmm8,xmm8,xmm8
       vmovdqu   ymmword ptr [rbp-60],ymm8
       vmovdqa   xmmword ptr [rbp-40],xmm8
       mov       rbx,rdi
;         for (var i = 0; i < BatchOps; i++)
;              ^^^^^^^^^
       xor       r15d,r15d
       jmp       short M00_L02
M00_L00:
       mov       edi,r14d
       imul      rcx,rdi,38
       mov       r8,[rbp-78]
       cmp       [rax+rcx+10],r8
       jne       near ptr M00_L04
       vmovdqu   ymm0,ymmword ptr [rax+rcx+10]
       vmovdqu   ymmword ptr [rbp-68],ymm0
       vmovdqu   ymm0,ymmword ptr [rax+rcx+28]
       vmovdqu   ymmword ptr [rbp-50],ymm0
M00_L01:
       mov       rax,[rbp-60]
       mov       r8,[rbp-80]
       mov       r9d,8
       mov       rdi,[rbp-78]
       mov       rsi,r13
       mov       rdx,r12
       mov       rcx,[rbp-70]
       call      rax
       mov       [rbp-30],rax
       mov       [rbx+44],rax
       inc       r15d
       cmp       r15d,4B
       jge       near ptr M00_L05
;             if (i == 0)
;             ^^^^^^^^^^^
       test      r15d,r15d
       jne       short M00_L03
;                 _playback = Timeline.Start(_id);
;                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M00_L02:
       movzx     edi,word ptr [rbx+14]
       call      qword ptr [7F3BE01ED2F0]; Tl.Timeline.Live(UInt16)
       xor       edi,edi
       mov       [rbx+44],edi
       mov       dword ptr [rbx+48],10000
;             _playback = Timeline.Forward(_id, in _playback, in _input, ref _result, _batchTicks.AsSpan(i * BatchSize, BatchSize));
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M00_L03:
       movzx     r14d,word ptr [rbx+14]
       lea       r13,[rbx+44]
       lea       r12,[rbx+18]
       lea       rax,[rbx+20]
       mov       [rbp-70],rax
       mov       rdi,[rbx+8]
       lea       ecx,[r15*8]
       test      rdi,rdi
       je        short M00_L06
       mov       edx,[rdi+8]
       mov       esi,ecx
       add       rsi,8
       cmp       rdx,rsi
       jb        short M00_L06
       lea       rcx,[rdi+rcx*4+10]
       mov       [rbp-80],rcx
       mov       edi,r14d
       call      qword ptr [7F3BE01ED2F0]; Tl.Timeline.Live(UInt16)
       mov       [rbp-78],rax
       mov       rdi,r13
       call      qword ptr [7F3BE0287738]; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       mov       rdi,7F2C3C000BF8
       mov       rax,[rdi]
       cmp       [rax+8],r14d
       jg        near ptr M00_L00
       mov       r8,[rbp-78]
M00_L04:
       lea       rdi,[rbp-68]
       mov       esi,r14d
       mov       rdx,r8
       call      qword ptr [7F3BE0287750]
       jmp       near ptr M00_L01
;         return _playback;
;         ^^^^^^^^^^^^^^^^^
M00_L05:
       mov       rax,[rbx+44]
       vzeroupper
       add       rsp,58
       pop       rbx
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
M00_L06:
       call      qword ptr [7F3BDF64D158]
       int       3
; Total bytes of code 333
```
```assembly
; Tl.Timeline.Live(UInt16)
       push      rbp
       push      r15
       push      r14
       push      rbx
       push      rax
       lea       rbp,[rsp+20]
       mov       ebx,edi
;         if (index == None)
;         ^^^^^^^^^^^^^^^^^^
       movzx     eax,bx
       cmp       eax,0FFFF
       je        short M01_L00
;         var slots = Volatile.Read(ref s_slots);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       mov       rdi,7F2C3C000B90
       mov       rdi,[rdi]
;         if ((uint)index >= (uint)slots.Length)
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       mov       ecx,[rdi+8]
       cmp       ecx,eax
       jle       near ptr M01_L01
;         return Volatile.Read(ref slots[index])
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;                                                                                                           
       mov       rax,[rdi+rax*8+10]
       test      rax,rax
       je        near ptr M01_L02
       add       rsp,8
       pop       rbx
       pop       r14
       pop       r15
       pop       rbp
       ret
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M01_L00:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       r14,rax
       mov       edi,209
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F3BE01EC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M01_L01:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F3BE01EC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L02:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7F3BE01EC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 412
```
```assembly
; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       push      rbp
       push      rbx
       push      rax
       lea       rbp,[rsp+10]
;         if (!playback.Has(PlaybackFlags.Started))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       movzx     edi,word ptr [rdi+6]
       test      dil,1
       je        short M02_L00
;         if (playback.Has(PlaybackFlags.Stopped))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       test      dil,2
       jne       short M02_L01
       add       rsp,8
       pop       rbx
       pop       rbp
       ret
;             throw new InvalidOperationException("Playback was never started; mint one with Timeline.Start.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M02_L00:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,76F
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F3BDFCC61F0]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
;             throw new InvalidOperationException("Playback is stopped.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M02_L01:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,7E3
       mov       rsi,7F3BDFBBC9B0
       call      qword ptr [7F3BDFAC4450]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7F3BDFCC61F0]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 151
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: NoTiering(EnvironmentVariables=DOTNET_TieredCompilation=0, IterationCount=12, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.CompiledBench.CompiledVsInterpreter.SumInterpreterSingleTick()
       push      rbp
       push      r15
       push      r14
       push      r13
       push      r12
       push      rbx
       sub       rsp,58
       lea       rbp,[rsp+80]
       vxorps    xmm8,xmm8,xmm8
       vmovdqu   ymmword ptr [rbp-70],ymm8
       vmovdqu   ymmword ptr [rbp-50],ymm8
       xor       eax,eax
       mov       [rbp-30],rax
       mov       rbx,rdi
;         for (var i = 0; i < SingleOps; i++)
;              ^^^^^^^^^
       mov       r15d,400
;             _playback = Timeline.Forward(_id, in _playback, in _input, ref _sum, NextTick(interpreter: true));
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M00_L00:
       movzx     r14d,word ptr [rbx+14]
       lea       r13,[rbx+44]
       lea       r12,[rbx+18]
       lea       rax,[rbx+40]
       mov       [rbp-78],rax
       cmp       dword ptr [rbx+10],258
       jne       short M00_L01
       xor       edi,edi
       mov       [rbx+10],edi
       movzx     edi,word ptr [rbx+14]
       call      qword ptr [7FF119BCD2F0]; Tl.Timeline.Live(UInt16)
       xor       edi,edi
       mov       [rbx+44],edi
       mov       dword ptr [rbx+48],10000
M00_L01:
       mov       edi,[rbx+10]
       lea       ecx,[rdi+1]
       mov       [rbx+10],ecx
       xor       ecx,ecx
       mov       [rbp-2C],ecx
       mov       [rbp-2C],edi
       mov       edi,r14d
       call      qword ptr [7FF119BCD2F0]; Tl.Timeline.Live(UInt16)
       mov       [rbp-80],rax
       mov       rdi,r13
       call      qword ptr [7FF119C676F0]; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       mov       rdi,7FE174000C28
       mov       rax,[rdi]
       cmp       [rax+8],r14d
       jg        short M00_L03
       mov       r8,[rbp-80]
M00_L02:
       lea       rdi,[rbp-70]
       mov       esi,r14d
       mov       rdx,r8
       call      qword ptr [7FF119C67708]
       jmp       short M00_L04
M00_L03:
       mov       edi,r14d
       imul      rcx,rdi,38
       mov       r8,[rbp-80]
       cmp       [rax+rcx+10],r8
       jne       short M00_L02
       vmovdqu   ymm0,ymmword ptr [rax+rcx+10]
       vmovdqu   ymmword ptr [rbp-70],ymm0
       vmovdqu   ymm0,ymmword ptr [rax+rcx+28]
       vmovdqu   ymmword ptr [rbp-58],ymm0
M00_L04:
       mov       rax,[rbp-68]
       lea       r8,[rbp-2C]
       mov       r9d,1
       mov       rdi,[rbp-80]
       mov       rsi,r13
       mov       rdx,r12
       mov       rcx,[rbp-78]
       call      rax
       mov       [rbp-38],rax
       mov       [rbx+44],rax
       dec       r15d
       jne       near ptr M00_L00
;         return _playback;
;         ^^^^^^^^^^^^^^^^^
       mov       rax,[rbx+44]
       vzeroupper
       add       rsp,58
       pop       rbx
       pop       r12
       pop       r13
       pop       r14
       pop       r15
       pop       rbp
       ret
; Total bytes of code 298
```
```assembly
; Tl.Timeline.Live(UInt16)
       push      rbp
       push      r15
       push      r14
       push      rbx
       push      rax
       lea       rbp,[rsp+20]
       mov       ebx,edi
;         if (index == None)
;         ^^^^^^^^^^^^^^^^^^
       movzx     eax,bx
       cmp       eax,0FFFF
       je        short M01_L00
;         var slots = Volatile.Read(ref s_slots);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       mov       rdi,7FE174000B90
       mov       rdi,[rdi]
;         if ((uint)index >= (uint)slots.Length)
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       mov       ecx,[rdi+8]
       cmp       ecx,eax
       jle       near ptr M01_L01
;         return Volatile.Read(ref slots[index])
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;             ?? throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;                                                                                                           
       mov       rax,[rdi+rax*8+10]
       test      rax,rax
       je        near ptr M01_L02
       add       rsp,8
       pop       rbx
       pop       r14
       pop       r15
       pop       rbp
       ret
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline.None is not a timeline index.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M01_L00:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       r14,rax
       mov       edi,209
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7FF119BCC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
;             throw new ArgumentOutOfRangeException(nameof(index), index, "Timeline index is not live.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M01_L01:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7FF119BCC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L02:
       mov       rdi,offset MT_System.UInt16
       call      CORINFO_HELP_NEWSFAST
       mov       r15,rax
       mov       [r15+8],bx
       mov       rdi,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,1FD
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       r14,rax
       mov       edi,257
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       rcx,rax
       mov       rsi,r14
       mov       rdx,r15
       mov       rdi,rbx
       call      qword ptr [7FF119BCC948]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 412
```
```assembly
; Tl.Internal.PlaybackCore.RequireRunnable(Tl.Playback ByRef)
       push      rbp
       push      rbx
       push      rax
       lea       rbp,[rsp+10]
;         if (!playback.Has(PlaybackFlags.Started))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       movzx     edi,word ptr [rdi+6]
       test      dil,1
       je        short M02_L00
;         if (playback.Has(PlaybackFlags.Stopped))
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
       test      dil,2
       jne       short M02_L01
       add       rsp,8
       pop       rbx
       pop       rbp
       ret
;             throw new InvalidOperationException("Playback was never started; mint one with Timeline.Start.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M02_L00:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,76F
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7FF1196A61F0]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
;             throw new InvalidOperationException("Playback is stopped.");
;             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
M02_L01:
       mov       rdi,offset MT_System.InvalidOperationException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       edi,7E3
       mov       rsi,7FF11959C9B0
       call      qword ptr [7FF1194A4450]
       mov       rsi,rax
       mov       rdi,rbx
       call      qword ptr [7FF1196A61F0]
       mov       rdi,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 151
```


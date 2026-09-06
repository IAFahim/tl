## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-VBSMZP(IterationCount=6, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.Hooks.Codegen.Direct()
;         _receiver.OnForward(in _frame);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return _receiver.Result;
;         ^^^^^^^^^^^^^^^^^^^^^^^^
       lea       rax,[rdi+8]
       mov       rcx,rax
       add       rdi,20
       vmovss    xmm0,dword ptr [rcx+4]
       vmovss    xmm1,dword ptr [rcx]
       vmulss    xmm1,xmm1,dword ptr [rdi+4]
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rcx+4],xmm0
       movsxd    rdx,dword ptr [rdi]
       add       [rcx+8],rdx
       inc       dword ptr [rcx+10]
       vmovss    xmm0,dword ptr [rax+4]
       mov       rcx,[rax+8]
       mov       eax,[rax+10]
       vmovss    dword ptr [rsi],xmm0
       mov       [rsi+8],rcx
       mov       [rsi+10],eax
       mov       rax,rsi
       ret
; Total bytes of code 71
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-VBSMZP(IterationCount=6, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.Hooks.Codegen.GeneratedLink()
;         ReceiverLink.Forward(ref _receiver, in _frame);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return _receiver.Result;
;         ^^^^^^^^^^^^^^^^^^^^^^^^
       lea       rax,[rdi+8]
       mov       rcx,rax
       add       rdi,20
       vmovss    xmm0,dword ptr [rcx+4]
       vmovss    xmm1,dword ptr [rcx]
       vmulss    xmm1,xmm1,dword ptr [rdi+4]
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rcx+4],xmm0
       movsxd    rdx,dword ptr [rdi]
       add       [rcx+8],rdx
       inc       dword ptr [rcx+10]
       vmovss    xmm0,dword ptr [rax+4]
       mov       rcx,[rax+8]
       mov       eax,[rax+10]
       vmovss    dword ptr [rsi],xmm0
       mov       [rsi+8],rcx
       mov       [rsi+10],eax
       mov       rax,rsi
       ret
; Total bytes of code 71
```

## .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3 (Job: Job-VBSMZP(IterationCount=6, IterationTime=250ms, WarmupCount=16))

```assembly
; Tl.Hooks.Codegen.Constrained()
;         Calls.Constrained(ref _receiver, in _frame);
;         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
;         return _receiver.Result;
;         ^^^^^^^^^^^^^^^^^^^^^^^^
       lea       rax,[rdi+8]
       mov       rcx,rax
       add       rdi,20
       vmovss    xmm0,dword ptr [rcx+4]
       vmovss    xmm1,dword ptr [rcx]
       vmulss    xmm1,xmm1,dword ptr [rdi+4]
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rcx+4],xmm0
       movsxd    rdx,dword ptr [rdi]
       add       [rcx+8],rdx
       inc       dword ptr [rcx+10]
       vmovss    xmm0,dword ptr [rax+4]
       mov       rcx,[rax+8]
       mov       eax,[rax+10]
       vmovss    dword ptr [rsi],xmm0
       mov       [rsi+8],rcx
       mov       [rsi+10],eax
       mov       rax,rsi
       ret
; Total bytes of code 71
```


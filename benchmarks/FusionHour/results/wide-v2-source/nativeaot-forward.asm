
/tmp/tl-fusion-v2-aot/FusionChecks:     file format elf64-x86-64


Disassembly of section .init:

Disassembly of section .plt:

Disassembly of section .text:

Disassembly of section __managedcode:

0000000000079e00 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0>:
   79e00:	55                   	push   rbp
   79e01:	41 57                	push   r15
   79e03:	41 56                	push   r14
   79e05:	53                   	push   rbx
   79e06:	50                   	push   rax
   79e07:	48 8d 6c 24 20       	lea    rbp,[rsp+0x20]
   79e0c:	0f b7 47 06          	movzx  eax,WORD PTR [rdi+0x6]
   79e10:	a8 01                	test   al,0x1
   79e12:	0f 84 e5 02 00 00    	je     7a0fd <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2fd>
   79e18:	a8 02                	test   al,0x2
   79e1a:	0f 85 04 03 00 00    	jne    7a124 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x324>
   79e20:	85 c9                	test   ecx,ecx
   79e22:	0f 84 c7 02 00 00    	je     7a0ef <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2ef>
   79e28:	8b 1f                	mov    ebx,DWORD PTR [rdi]
   79e2a:	44 0f b7 7f 04       	movzx  r15d,WORD PTR [rdi+0x4]
   79e2f:	8b fb                	mov    edi,ebx
   79e31:	48 69 ff b5 81 4e 1b 	imul   rdi,rdi,0x1b4e81b5
   79e38:	48 c1 ef 26          	shr    rdi,0x26
   79e3c:	69 ff 58 02 00 00    	imul   edi,edi,0x258
   79e42:	44 8b f3             	mov    r14d,ebx
   79e45:	44 2b f7             	sub    r14d,edi
   79e48:	8b fb                	mov    edi,ebx
   79e4a:	48 69 ff b5 81 4e 1b 	imul   rdi,rdi,0x1b4e81b5
   79e51:	48 c1 ef 26          	shr    rdi,0x26
   79e55:	33 c0                	xor    eax,eax
   79e57:	3b c1                	cmp    eax,ecx
   79e59:	0f 8d 5e 02 00 00    	jge    7a0bd <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2bd>
   79e5f:	44 8b 04 82          	mov    r8d,DWORD PTR [rdx+rax*4]
   79e63:	45 8b c8             	mov    r9d,r8d
   79e66:	4d 69 c9 b5 81 4e 1b 	imul   r9,r9,0x1b4e81b5
   79e6d:	49 c1 e9 26          	shr    r9,0x26
   79e71:	45 69 c9 58 02 00 00 	imul   r9d,r9d,0x258
   79e78:	45 8b d0             	mov    r10d,r8d
   79e7b:	45 2b d1             	sub    r10d,r9d
   79e7e:	45 8b c8             	mov    r9d,r8d
   79e81:	4d 69 c9 b5 81 4e 1b 	imul   r9,r9,0x1b4e81b5
   79e88:	49 c1 e9 26          	shr    r9,0x26
   79e8c:	44 3b c3             	cmp    r8d,ebx
   79e8f:	73 0d                	jae    79e9e <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x9e>
   79e91:	45 3b d6             	cmp    r10d,r14d
   79e94:	41 0f 92 c3          	setb   r11b
   79e98:	45 0f b6 db          	movzx  r11d,r11b
   79e9c:	eb 06                	jmp    79ea4 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0xa4>
   79e9e:	45 8b d9             	mov    r11d,r9d
   79ea1:	44 2b df             	sub    r11d,edi
   79ea4:	41 8b ff             	mov    edi,r15d
   79ea7:	f7 df                	neg    edi
   79ea9:	81 c7 ff ff 00 00    	add    edi,0xffff
   79eaf:	48 63 ff             	movsxd rdi,edi
   79eb2:	41 8b db             	mov    ebx,r11d
   79eb5:	48 3b fb             	cmp    rdi,rbx
   79eb8:	0f 8c 8d 02 00 00    	jl     7a14b <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x34b>
   79ebe:	45 03 df             	add    r11d,r15d
   79ec1:	45 0f b7 fb          	movzx  r15d,r11w
   79ec5:	41 83 fa 2f          	cmp    r10d,0x2f
   79ec9:	0f 82 fe 00 00 00    	jb     79fcd <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x1cd>
   79ecf:	41 81 fa c8 00 00 00 	cmp    r10d,0xc8
   79ed6:	72 6e                	jb     79f46 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x146>
   79ed8:	41 81 fa 03 02 00 00 	cmp    r10d,0x203
   79edf:	72 22                	jb     79f03 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x103>
   79ee1:	41 81 fa 58 02 00 00 	cmp    r10d,0x258
   79ee8:	0f 83 bc 01 00 00    	jae    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   79eee:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79ef2:	f3 0f 58 05 1e 50 10 	addss  xmm0,DWORD PTR [rip+0x10501e]        # 17ef18 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0>
   79ef9:	00 
   79efa:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79efe:	e9 a7 01 00 00       	jmp    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   79f03:	41 81 fa 41 01 00 00 	cmp    r10d,0x141
   79f0a:	72 25                	jb     79f31 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x131>
   79f0c:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79f10:	f3 0f 58 05 04 50 10 	addss  xmm0,DWORD PTR [rip+0x105004]        # 17ef1c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x4>
   79f17:	00 
   79f18:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79f1c:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79f20:	f3 0f 58 05 f8 4f 10 	addss  xmm0,DWORD PTR [rip+0x104ff8]        # 17ef20 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x8>
   79f27:	00 
   79f28:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79f2c:	e9 57 01 00 00       	jmp    7a088 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x288>
   79f31:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79f35:	f3 0f 58 05 e7 4f 10 	addss  xmm0,DWORD PTR [rip+0x104fe7]        # 17ef24 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0xc>
   79f3c:	00 
   79f3d:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79f41:	e9 42 01 00 00       	jmp    7a088 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x288>
   79f46:	41 83 fa 4c          	cmp    r10d,0x4c
   79f4a:	72 5c                	jb     79fa8 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x1a8>
   79f4c:	41 83 fa 7b          	cmp    r10d,0x7b
   79f50:	72 15                	jb     79f67 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x167>
   79f52:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79f56:	f3 0f 58 05 c6 4f 10 	addss  xmm0,DWORD PTR [rip+0x104fc6]        # 17ef24 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0xc>
   79f5d:	00 
   79f5e:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79f62:	e9 43 01 00 00       	jmp    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   79f67:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79f6b:	f3 0f 58 05 ad 4f 10 	addss  xmm0,DWORD PTR [rip+0x104fad]        # 17ef20 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x8>
   79f72:	00 
   79f73:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79f77:	41 8d 7a b4          	lea    edi,[r10-0x4c]
   79f7b:	0f 57 c0             	xorps  xmm0,xmm0
   79f7e:	f3 48 0f 2a c7       	cvtsi2ss xmm0,rdi
   79f83:	f3 0f 5e 05 9d 4f 10 	divss  xmm0,DWORD PTR [rip+0x104f9d]        # 17ef28 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x10>
   79f8a:	00 
   79f8b:	f3 0f 59 05 99 4f 10 	mulss  xmm0,DWORD PTR [rip+0x104f99]        # 17ef2c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x14>
   79f92:	00 
   79f93:	f3 0f 58 05 95 4f 10 	addss  xmm0,DWORD PTR [rip+0x104f95]        # 17ef30 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x18>
   79f9a:	00 
   79f9b:	f3 0f 58 06          	addss  xmm0,DWORD PTR [rsi]
   79f9f:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79fa3:	e9 02 01 00 00       	jmp    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   79fa8:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79fac:	f3 0f 58 05 80 4f 10 	addss  xmm0,DWORD PTR [rip+0x104f80]        # 17ef34 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x1c>
   79fb3:	00 
   79fb4:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79fb8:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   79fbc:	f3 0f 58 05 60 4f 10 	addss  xmm0,DWORD PTR [rip+0x104f60]        # 17ef24 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0xc>
   79fc3:	00 
   79fc4:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   79fc8:	e9 dd 00 00 00       	jmp    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   79fcd:	41 83 fa 0b          	cmp    r10d,0xb
   79fd1:	72 63                	jb     7a036 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x236>
   79fd3:	41 83 fa 12          	cmp    r10d,0x12
   79fd7:	0f 82 cd 00 00 00    	jb     7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   79fdd:	41 83 fa 1d          	cmp    r10d,0x1d
   79fe1:	72 31                	jb     7a014 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x214>
   79fe3:	41 8d 7a e3          	lea    edi,[r10-0x1d]
   79fe7:	0f 57 c0             	xorps  xmm0,xmm0
   79fea:	f3 48 0f 2a c7       	cvtsi2ss xmm0,rdi
   79fef:	f3 0f 5e 05 41 4f 10 	divss  xmm0,DWORD PTR [rip+0x104f41]        # 17ef38 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x20>
   79ff6:	00 
   79ff7:	f3 0f 59 05 21 4f 10 	mulss  xmm0,DWORD PTR [rip+0x104f21]        # 17ef20 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x8>
   79ffe:	00 
   79fff:	f3 0f 58 05 2d 4f 10 	addss  xmm0,DWORD PTR [rip+0x104f2d]        # 17ef34 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x1c>
   7a006:	00 
   7a007:	f3 0f 58 06          	addss  xmm0,DWORD PTR [rsi]
   7a00b:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a00f:	e9 96 00 00 00       	jmp    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   7a014:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a018:	f3 0f 58 05 00 4f 10 	addss  xmm0,DWORD PTR [rip+0x104f00]        # 17ef20 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x8>
   7a01f:	00 
   7a020:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a024:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a028:	f3 0f 58 05 0c 4f 10 	addss  xmm0,DWORD PTR [rip+0x104f0c]        # 17ef3c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x24>
   7a02f:	00 
   7a030:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a034:	eb 74                	jmp    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   7a036:	41 83 fa 03          	cmp    r10d,0x3
   7a03a:	72 5e                	jb     7a09a <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x29a>
   7a03c:	41 83 fa 07          	cmp    r10d,0x7
   7a040:	72 12                	jb     7a054 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x254>
   7a042:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a046:	f3 0f 58 05 ca 4e 10 	addss  xmm0,DWORD PTR [rip+0x104eca]        # 17ef18 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0>
   7a04d:	00 
   7a04e:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a052:	eb 34                	jmp    7a088 <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x288>
   7a054:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a058:	f3 0f 58 05 bc 4e 10 	addss  xmm0,DWORD PTR [rip+0x104ebc]        # 17ef1c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x4>
   7a05f:	00 
   7a060:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a064:	41 8d 7a fd          	lea    edi,[r10-0x3]
   7a068:	0f 57 c0             	xorps  xmm0,xmm0
   7a06b:	f3 48 0f 2a c7       	cvtsi2ss xmm0,rdi
   7a070:	f3 0f 5e 05 a0 4e 10 	divss  xmm0,DWORD PTR [rip+0x104ea0]        # 17ef18 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0>
   7a077:	00 
   7a078:	f3 0f 58 05 a4 4e 10 	addss  xmm0,DWORD PTR [rip+0x104ea4]        # 17ef24 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0xc>
   7a07f:	00 
   7a080:	f3 0f 58 06          	addss  xmm0,DWORD PTR [rsi]
   7a084:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a088:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a08c:	f3 0f 58 05 a8 4e 10 	addss  xmm0,DWORD PTR [rip+0x104ea8]        # 17ef3c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x24>
   7a093:	00 
   7a094:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a098:	eb 10                	jmp    7a0aa <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x2aa>
   7a09a:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a09e:	f3 0f 58 05 76 4e 10 	addss  xmm0,DWORD PTR [rip+0x104e76]        # 17ef1c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x4>
   7a0a5:	00 
   7a0a6:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a0aa:	41 8b d8             	mov    ebx,r8d
   7a0ad:	45 8b f2             	mov    r14d,r10d
   7a0b0:	41 8b f9             	mov    edi,r9d
   7a0b3:	ff c0                	inc    eax
   7a0b5:	3b c1                	cmp    eax,ecx
   7a0b7:	0f 8c a2 fd ff ff    	jl     79e5f <FusionChecks_Tl_FusionExperiment_FusedPulse__Forward_0+0x5f>
   7a0bd:	b8 01 00 00 00       	mov    eax,0x1
   7a0c2:	bf 05 00 00 00       	mov    edi,0x5
   7a0c7:	41 81 fe 57 02 00 00 	cmp    r14d,0x257
   7a0ce:	0f 44 c7             	cmove  eax,edi
   7a0d1:	8b fb                	mov    edi,ebx
   7a0d3:	41 8b cf             	mov    ecx,r15d
   7a0d6:	48 c1 e1 20          	shl    rcx,0x20
   7a0da:	48 0b f9             	or     rdi,rcx
   7a0dd:	48 c1 e0 30          	shl    rax,0x30
   7a0e1:	48 0b c7             	or     rax,rdi
   7a0e4:	48 83 c4 08          	add    rsp,0x8
   7a0e8:	5b                   	pop    rbx
   7a0e9:	41 5e                	pop    r14
   7a0eb:	41 5f                	pop    r15
   7a0ed:	5d                   	pop    rbp
   7a0ee:	c3                   	ret
   7a0ef:	48 8b 07             	mov    rax,QWORD PTR [rdi]
   7a0f2:	48 83 c4 08          	add    rsp,0x8
   7a0f6:	5b                   	pop    rbx
   7a0f7:	41 5e                	pop    r14
   7a0f9:	41 5f                	pop    r15
   7a0fb:	5d                   	pop    rbp
   7a0fc:	c3                   	ret
   7a0fd:	48 8d 3d 6c 6d 1b 00 	lea    rdi,[rip+0x1b6d6c]        # 230e70 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
   7a104:	e8 c7 18 ff ff       	call   6b9d0 <RhpNewFast>
   7a109:	48 8b d8             	mov    rbx,rax
   7a10c:	48 8b fb             	mov    rdi,rbx
   7a10f:	48 8d 35 92 6c 1a 00 	lea    rsi,[rip+0x1a6c92]        # 220da8 <__Str_Playback_was_never_started__mi_A4B7AD70A4141AA14D48EC123D832D5C325A07EB90C0D33B7FB492BC5D1E26A5>
   7a116:	e8 05 62 01 00       	call   90320 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
   7a11b:	48 8b fb             	mov    rdi,rbx
   7a11e:	e8 ad 1b ff ff       	call   6bcd0 <RhpThrowEx>
   7a123:	cc                   	int3
   7a124:	48 8d 3d 45 6d 1b 00 	lea    rdi,[rip+0x1b6d45]        # 230e70 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
   7a12b:	e8 a0 18 ff ff       	call   6b9d0 <RhpNewFast>
   7a130:	48 8b d8             	mov    rbx,rax
   7a133:	48 8b fb             	mov    rdi,rbx
   7a136:	48 8d 35 93 6b 1a 00 	lea    rsi,[rip+0x1a6b93]        # 220cd0 <__Str_Playback_is_stopped__BEFEE01D2658356891B4B1A0CFEF9C031BEA0351DB9767A723E74228769FE021>
   7a13d:	e8 de 61 01 00       	call   90320 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
   7a142:	48 8b fb             	mov    rdi,rbx
   7a145:	e8 86 1b ff ff       	call   6bcd0 <RhpThrowEx>
   7a14a:	cc                   	int3
   7a14b:	48 8d 3d fe 5f 1b 00 	lea    rdi,[rip+0x1b5ffe]        # 230150 <_ZTV46S_P_CoreLib_System_ArgumentOutOfRangeException>
   7a152:	e8 79 18 ff ff       	call   6b9d0 <RhpNewFast>
   7a157:	4c 8b f0             	mov    r14,rax
   7a15a:	49 8b fe             	mov    rdi,r14
   7a15d:	48 8d 35 6c fb 1a 00 	lea    rsi,[rip+0x1afb6c]        # 229cd0 <__Str_ticks>
   7a164:	48 8d 15 0d 6b 1a 00 	lea    rdx,[rip+0x1a6b0d]        # 220c78 <__Str_Playback_cycle_capacity_exceed_438134DB0460610D3D09165487578575BC58EF0A4041E10A1DCD99D64E2B2DA0>
   7a16b:	e8 a0 2c 01 00       	call   8ce10 <S_P_CoreLib_System_ArgumentOutOfRangeException___ctor_1>
   7a170:	49 8b fe             	mov    rdi,r14
   7a173:	e8 58 1b ff ff       	call   6bcd0 <RhpThrowEx>
   7a178:	cc                   	int3

Disassembly of section __unbox:

Disassembly of section .fini:

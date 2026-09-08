
/tmp/tl-fusion-v2-aot/FusionChecks:     file format elf64-x86-64


Disassembly of section .init:

Disassembly of section .plt:

Disassembly of section .text:

Disassembly of section __managedcode:

000000000007a180 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0>:
   7a180:	55                   	push   rbp
   7a181:	41 57                	push   r15
   7a183:	53                   	push   rbx
   7a184:	48 8d 6c 24 10       	lea    rbp,[rsp+0x10]
   7a189:	0f b7 47 06          	movzx  eax,WORD PTR [rdi+0x6]
   7a18d:	a8 01                	test   al,0x1
   7a18f:	0f 84 dc 02 00 00    	je     7a471 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2f1>
   7a195:	a8 02                	test   al,0x2
   7a197:	0f 85 fb 02 00 00    	jne    7a498 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x318>
   7a19d:	85 c9                	test   ecx,ecx
   7a19f:	0f 84 c4 02 00 00    	je     7a469 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2e9>
   7a1a5:	8b 07                	mov    eax,DWORD PTR [rdi]
   7a1a7:	0f b7 7f 04          	movzx  edi,WORD PTR [rdi+0x4]
   7a1ab:	44 8b c0             	mov    r8d,eax
   7a1ae:	4d 69 c0 b5 81 4e 1b 	imul   r8,r8,0x1b4e81b5
   7a1b5:	49 c1 e8 26          	shr    r8,0x26
   7a1b9:	45 69 c0 58 02 00 00 	imul   r8d,r8d,0x258
   7a1c0:	44 8b c8             	mov    r9d,eax
   7a1c3:	45 2b c8             	sub    r9d,r8d
   7a1c6:	44 8b c0             	mov    r8d,eax
   7a1c9:	4d 69 c0 b5 81 4e 1b 	imul   r8,r8,0x1b4e81b5
   7a1d0:	49 c1 e8 26          	shr    r8,0x26
   7a1d4:	45 33 d2             	xor    r10d,r10d
   7a1d7:	44 3b d1             	cmp    r10d,ecx
   7a1da:	0f 8d 5c 02 00 00    	jge    7a43c <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2bc>
   7a1e0:	46 8b 1c 92          	mov    r11d,DWORD PTR [rdx+r10*4]
   7a1e4:	41 8b db             	mov    ebx,r11d
   7a1e7:	48 69 db b5 81 4e 1b 	imul   rbx,rbx,0x1b4e81b5
   7a1ee:	48 c1 eb 26          	shr    rbx,0x26
   7a1f2:	69 db 58 02 00 00    	imul   ebx,ebx,0x258
   7a1f8:	45 8b fb             	mov    r15d,r11d
   7a1fb:	44 2b fb             	sub    r15d,ebx
   7a1fe:	41 8b db             	mov    ebx,r11d
   7a201:	48 69 db b5 81 4e 1b 	imul   rbx,rbx,0x1b4e81b5
   7a208:	48 c1 eb 26          	shr    rbx,0x26
   7a20c:	44 3b d8             	cmp    r11d,eax
   7a20f:	76 0b                	jbe    7a21c <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x9c>
   7a211:	45 3b f9             	cmp    r15d,r9d
   7a214:	0f 97 c0             	seta   al
   7a217:	0f b6 c0             	movzx  eax,al
   7a21a:	eb 05                	jmp    7a221 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xa1>
   7a21c:	41 8b c0             	mov    eax,r8d
   7a21f:	2b c3                	sub    eax,ebx
   7a221:	3b f8                	cmp    edi,eax
   7a223:	77 05                	ja     7a22a <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xaa>
   7a225:	44 8b cf             	mov    r9d,edi
   7a228:	eb 03                	jmp    7a22d <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xad>
   7a22a:	44 8b c8             	mov    r9d,eax
   7a22d:	41 2b f9             	sub    edi,r9d
   7a230:	0f b7 ff             	movzx  edi,di
   7a233:	41 83 ff 2f          	cmp    r15d,0x2f
   7a237:	0f 82 05 01 00 00    	jb     7a342 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x1c2>
   7a23d:	41 81 ff c8 00 00 00 	cmp    r15d,0xc8
   7a244:	72 6e                	jb     7a2b4 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x134>
   7a246:	41 81 ff 03 02 00 00 	cmp    r15d,0x203
   7a24d:	72 22                	jb     7a271 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xf1>
   7a24f:	41 81 ff 58 02 00 00 	cmp    r15d,0x258
   7a256:	0f 83 cb 01 00 00    	jae    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a25c:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a260:	f3 0f 5c 05 d8 4c 10 	subss  xmm0,DWORD PTR [rip+0x104cd8]        # 17ef40 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0>
   7a267:	00 
   7a268:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a26c:	e9 b6 01 00 00       	jmp    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a271:	41 81 ff 41 01 00 00 	cmp    r15d,0x141
   7a278:	72 25                	jb     7a29f <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x11f>
   7a27a:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a27e:	f3 0f 5c 05 be 4c 10 	subss  xmm0,DWORD PTR [rip+0x104cbe]        # 17ef44 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x4>
   7a285:	00 
   7a286:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a28a:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a28e:	f3 0f 5c 05 b2 4c 10 	subss  xmm0,DWORD PTR [rip+0x104cb2]        # 17ef48 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x8>
   7a295:	00 
   7a296:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a29a:	e9 66 01 00 00       	jmp    7a405 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x285>
   7a29f:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a2a3:	f3 0f 5c 05 a1 4c 10 	subss  xmm0,DWORD PTR [rip+0x104ca1]        # 17ef4c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xc>
   7a2aa:	00 
   7a2ab:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a2af:	e9 51 01 00 00       	jmp    7a405 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x285>
   7a2b4:	41 83 ff 4c          	cmp    r15d,0x4c
   7a2b8:	72 63                	jb     7a31d <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x19d>
   7a2ba:	41 83 ff 7b          	cmp    r15d,0x7b
   7a2be:	72 15                	jb     7a2d5 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x155>
   7a2c0:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a2c4:	f3 0f 5c 05 80 4c 10 	subss  xmm0,DWORD PTR [rip+0x104c80]        # 17ef4c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xc>
   7a2cb:	00 
   7a2cc:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a2d0:	e9 52 01 00 00       	jmp    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a2d5:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a2d9:	f3 0f 5c 05 67 4c 10 	subss  xmm0,DWORD PTR [rip+0x104c67]        # 17ef48 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x8>
   7a2e0:	00 
   7a2e1:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a2e5:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a2e9:	45 8d 47 b4          	lea    r8d,[r15-0x4c]
   7a2ed:	41 8b c0             	mov    eax,r8d
   7a2f0:	0f 57 c9             	xorps  xmm1,xmm1
   7a2f3:	f3 48 0f 2a c8       	cvtsi2ss xmm1,rax
   7a2f8:	f3 0f 5e 0d 50 4c 10 	divss  xmm1,DWORD PTR [rip+0x104c50]        # 17ef50 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x10>
   7a2ff:	00 
   7a300:	f3 0f 59 0d 4c 4c 10 	mulss  xmm1,DWORD PTR [rip+0x104c4c]        # 17ef54 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x14>
   7a307:	00 
   7a308:	f3 0f 58 0d 48 4c 10 	addss  xmm1,DWORD PTR [rip+0x104c48]        # 17ef58 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x18>
   7a30f:	00 
   7a310:	f3 0f 5c c1          	subss  xmm0,xmm1
   7a314:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a318:	e9 0a 01 00 00       	jmp    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a31d:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a321:	f3 0f 5c 05 33 4c 10 	subss  xmm0,DWORD PTR [rip+0x104c33]        # 17ef5c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x1c>
   7a328:	00 
   7a329:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a32d:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a331:	f3 0f 5c 05 13 4c 10 	subss  xmm0,DWORD PTR [rip+0x104c13]        # 17ef4c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xc>
   7a338:	00 
   7a339:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a33d:	e9 e5 00 00 00       	jmp    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a342:	41 83 ff 0b          	cmp    r15d,0xb
   7a346:	72 67                	jb     7a3af <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x22f>
   7a348:	41 83 ff 12          	cmp    r15d,0x12
   7a34c:	0f 82 d5 00 00 00    	jb     7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a352:	41 83 ff 1d          	cmp    r15d,0x1d
   7a356:	72 35                	jb     7a38d <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x20d>
   7a358:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a35c:	41 8d 47 e3          	lea    eax,[r15-0x1d]
   7a360:	0f 57 c9             	xorps  xmm1,xmm1
   7a363:	f3 48 0f 2a c8       	cvtsi2ss xmm1,rax
   7a368:	f3 0f 5e 0d f0 4b 10 	divss  xmm1,DWORD PTR [rip+0x104bf0]        # 17ef60 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x20>
   7a36f:	00 
   7a370:	f3 0f 59 0d d0 4b 10 	mulss  xmm1,DWORD PTR [rip+0x104bd0]        # 17ef48 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x8>
   7a377:	00 
   7a378:	f3 0f 58 0d dc 4b 10 	addss  xmm1,DWORD PTR [rip+0x104bdc]        # 17ef5c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x1c>
   7a37f:	00 
   7a380:	f3 0f 5c c1          	subss  xmm0,xmm1
   7a384:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a388:	e9 9a 00 00 00       	jmp    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a38d:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a391:	f3 0f 5c 05 af 4b 10 	subss  xmm0,DWORD PTR [rip+0x104baf]        # 17ef48 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x8>
   7a398:	00 
   7a399:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a39d:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a3a1:	f3 0f 5c 05 bb 4b 10 	subss  xmm0,DWORD PTR [rip+0x104bbb]        # 17ef64 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x24>
   7a3a8:	00 
   7a3a9:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a3ad:	eb 78                	jmp    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a3af:	41 83 ff 03          	cmp    r15d,0x3
   7a3b3:	72 62                	jb     7a417 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x297>
   7a3b5:	41 83 ff 07          	cmp    r15d,0x7
   7a3b9:	72 12                	jb     7a3cd <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x24d>
   7a3bb:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a3bf:	f3 0f 5c 05 79 4b 10 	subss  xmm0,DWORD PTR [rip+0x104b79]        # 17ef40 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0>
   7a3c6:	00 
   7a3c7:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a3cb:	eb 38                	jmp    7a405 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x285>
   7a3cd:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a3d1:	f3 0f 5c 05 6b 4b 10 	subss  xmm0,DWORD PTR [rip+0x104b6b]        # 17ef44 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x4>
   7a3d8:	00 
   7a3d9:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a3dd:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a3e1:	41 8d 47 fd          	lea    eax,[r15-0x3]
   7a3e5:	0f 57 c9             	xorps  xmm1,xmm1
   7a3e8:	f3 48 0f 2a c8       	cvtsi2ss xmm1,rax
   7a3ed:	f3 0f 5e 0d 4b 4b 10 	divss  xmm1,DWORD PTR [rip+0x104b4b]        # 17ef40 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0>
   7a3f4:	00 
   7a3f5:	f3 0f 58 0d 4f 4b 10 	addss  xmm1,DWORD PTR [rip+0x104b4f]        # 17ef4c <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0xc>
   7a3fc:	00 
   7a3fd:	f3 0f 5c c1          	subss  xmm0,xmm1
   7a401:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a405:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a409:	f3 0f 5c 05 53 4b 10 	subss  xmm0,DWORD PTR [rip+0x104b53]        # 17ef64 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x24>
   7a410:	00 
   7a411:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a415:	eb 10                	jmp    7a427 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x2a7>
   7a417:	f3 0f 10 06          	movss  xmm0,DWORD PTR [rsi]
   7a41b:	f3 0f 5c 05 21 4b 10 	subss  xmm0,DWORD PTR [rip+0x104b21]        # 17ef44 <__readonlydata_FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x4>
   7a422:	00 
   7a423:	f3 0f 11 06          	movss  DWORD PTR [rsi],xmm0
   7a427:	41 8b c3             	mov    eax,r11d
   7a42a:	45 8b cf             	mov    r9d,r15d
   7a42d:	44 8b c3             	mov    r8d,ebx
   7a430:	41 ff c2             	inc    r10d
   7a433:	44 3b d1             	cmp    r10d,ecx
   7a436:	0f 8c a4 fd ff ff    	jl     7a1e0 <FusionChecks_Tl_FusionExperiment_FusedPulse__Backward_0+0x60>
   7a43c:	b9 01 00 00 00       	mov    ecx,0x1
   7a441:	ba 05 00 00 00       	mov    edx,0x5
   7a446:	41 81 f9 57 02 00 00 	cmp    r9d,0x257
   7a44d:	0f 44 ca             	cmove  ecx,edx
   7a450:	8b c0                	mov    eax,eax
   7a452:	8b ff                	mov    edi,edi
   7a454:	48 c1 e7 20          	shl    rdi,0x20
   7a458:	48 0b c7             	or     rax,rdi
   7a45b:	8b f9                	mov    edi,ecx
   7a45d:	48 c1 e7 30          	shl    rdi,0x30
   7a461:	48 0b c7             	or     rax,rdi
   7a464:	5b                   	pop    rbx
   7a465:	41 5f                	pop    r15
   7a467:	5d                   	pop    rbp
   7a468:	c3                   	ret
   7a469:	48 8b 07             	mov    rax,QWORD PTR [rdi]
   7a46c:	5b                   	pop    rbx
   7a46d:	41 5f                	pop    r15
   7a46f:	5d                   	pop    rbp
   7a470:	c3                   	ret
   7a471:	48 8d 3d f8 69 1b 00 	lea    rdi,[rip+0x1b69f8]        # 230e70 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
   7a478:	e8 53 15 ff ff       	call   6b9d0 <RhpNewFast>
   7a47d:	48 8b d8             	mov    rbx,rax
   7a480:	48 8b fb             	mov    rdi,rbx
   7a483:	48 8d 35 1e 69 1a 00 	lea    rsi,[rip+0x1a691e]        # 220da8 <__Str_Playback_was_never_started__mi_A4B7AD70A4141AA14D48EC123D832D5C325A07EB90C0D33B7FB492BC5D1E26A5>
   7a48a:	e8 91 5e 01 00       	call   90320 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
   7a48f:	48 8b fb             	mov    rdi,rbx
   7a492:	e8 39 18 ff ff       	call   6bcd0 <RhpThrowEx>
   7a497:	cc                   	int3
   7a498:	48 8d 3d d1 69 1b 00 	lea    rdi,[rip+0x1b69d1]        # 230e70 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
   7a49f:	e8 2c 15 ff ff       	call   6b9d0 <RhpNewFast>
   7a4a4:	48 8b d8             	mov    rbx,rax
   7a4a7:	48 8b fb             	mov    rdi,rbx
   7a4aa:	48 8d 35 1f 68 1a 00 	lea    rsi,[rip+0x1a681f]        # 220cd0 <__Str_Playback_is_stopped__BEFEE01D2658356891B4B1A0CFEF9C031BEA0351DB9767A723E74228769FE021>
   7a4b1:	e8 6a 5e 01 00       	call   90320 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
   7a4b6:	48 8b fb             	mov    rdi,rbx
   7a4b9:	e8 12 18 ff ff       	call   6bcd0 <RhpThrowEx>
   7a4be:	cc                   	int3

Disassembly of section __unbox:

Disassembly of section .fini:


/tmp/tl-consumer-aot/ConsumerChecks:     file format elf64-x86-64


Disassembly of section __managedcode:

0000000000132060 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>>:
  132060:	55                   	push   %rbp
  132061:	41 57                	push   %r15
  132063:	41 56                	push   %r14
  132065:	41 55                	push   %r13
  132067:	41 54                	push   %r12
  132069:	53                   	push   %rbx
  13206a:	48 83 ec 28          	sub    $0x28,%rsp
  13206e:	48 8d 6c 24 50       	lea    0x50(%rsp),%rbp
  132073:	4c 8b fe             	mov    %rsi,%r15
  132076:	48 8b da             	mov    %rdx,%rbx
  132079:	0f b7 77 06          	movzwl 0x6(%rdi),%esi
  13207d:	40 f6 c6 01          	test   $0x1,%sil
  132081:	0f 84 6a 0d 00 00    	je     132df1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd91>
  132087:	40 f6 c6 02          	test   $0x2,%sil
  13208b:	0f 85 87 0d 00 00    	jne    132e18 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xdb8>
  132091:	45 85 c0             	test   %r8d,%r8d
  132094:	0f 84 45 0d 00 00    	je     132ddf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd7f>
  13209a:	44 8b 37             	mov    (%rdi),%r14d
  13209d:	44 0f b7 6f 04       	movzwl 0x4(%rdi),%r13d
  1320a2:	41 8b f6             	mov    %r14d,%esi
  1320a5:	48 69 c6 b5 81 4e 1b 	imul   $0x1b4e81b5,%rsi,%rax
  1320ac:	48 c1 e8 26          	shr    $0x26,%rax
  1320b0:	69 f0 58 02 00 00    	imul   $0x258,%eax,%esi
  1320b6:	45 8b e6             	mov    %r14d,%r12d
  1320b9:	44 2b e6             	sub    %esi,%r12d
  1320bc:	4c 8b c9             	mov    %rcx,%r9
  1320bf:	4c 89 4d b8          	mov    %r9,-0x48(%rbp)
  1320c3:	45 8b d0             	mov    %r8d,%r10d
  1320c6:	44 89 55 c8          	mov    %r10d,-0x38(%rbp)
  1320ca:	45 33 db             	xor    %r11d,%r11d
  1320cd:	45 3b da             	cmp    %r10d,%r11d
  1320d0:	0f 8d d2 0c 00 00    	jge    132da8 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd48>
  1320d6:	4c 89 5d c0          	mov    %r11,-0x40(%rbp)
  1320da:	43 8b 3c 99          	mov    (%r9,%r11,4),%edi
  1320de:	89 7d cc             	mov    %edi,-0x34(%rbp)
  1320e1:	44 8b c7             	mov    %edi,%r8d
  1320e4:	49 69 c8 b5 81 4e 1b 	imul   $0x1b4e81b5,%r8,%rcx
  1320eb:	48 c1 e9 26          	shr    $0x26,%rcx
  1320ef:	89 4d d4             	mov    %ecx,-0x2c(%rbp)
  1320f2:	44 69 c1 58 02 00 00 	imul   $0x258,%ecx,%r8d
  1320f9:	8b d7                	mov    %edi,%edx
  1320fb:	41 2b d0             	sub    %r8d,%edx
  1320fe:	89 55 d0             	mov    %edx,-0x30(%rbp)
  132101:	41 3b fe             	cmp    %r14d,%edi
  132104:	73 0d                	jae    132113 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb3>
  132106:	41 3b d4             	cmp    %r12d,%edx
  132109:	41 0f 92 c6          	setb   %r14b
  13210d:	45 0f b6 f6          	movzbl %r14b,%r14d
  132111:	eb 06                	jmp    132119 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb9>
  132113:	44 8b f1             	mov    %ecx,%r14d
  132116:	44 2b f0             	sub    %eax,%r14d
  132119:	45 8b c5             	mov    %r13d,%r8d
  13211c:	41 f7 d8             	neg    %r8d
  13211f:	41 81 c0 ff ff 00 00 	add    $0xffff,%r8d
  132126:	4d 63 c0             	movslq %r8d,%r8
  132129:	41 8b f6             	mov    %r14d,%esi
  13212c:	4c 3b c6             	cmp    %rsi,%r8
  13212f:	0f 8c 0a 0d 00 00    	jl     132e3f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xddf>
  132135:	45 03 ee             	add    %r14d,%r13d
  132138:	45 0f b7 ed          	movzwl %r13w,%r13d
  13213c:	83 fa 2f             	cmp    $0x2f,%edx
  13213f:	0f 82 c7 06 00 00    	jb     13280c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x7ac>
  132145:	81 fa c8 00 00 00    	cmp    $0xc8,%edx
  13214b:	0f 82 b5 03 00 00    	jb     132506 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4a6>
  132151:	81 fa 41 01 00 00    	cmp    $0x141,%edx
  132157:	0f 82 50 02 00 00    	jb     1323ad <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x34d>
  13215d:	81 fa 03 02 00 00    	cmp    $0x203,%edx
  132163:	0f 82 aa 00 00 00    	jb     132213 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1b3>
  132169:	81 fa 57 02 00 00    	cmp    $0x257,%edx
  13216f:	75 08                	jne    132179 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x119>
  132171:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  132177:	eb 19                	jmp    132192 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x132>
  132179:	45 85 f6             	test   %r14d,%r14d
  13217c:	75 09                	jne    132187 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x127>
  13217e:	41 81 fc 03 02 00 00 	cmp    $0x203,%r12d
  132185:	73 05                	jae    13218c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x12c>
  132187:	45 33 c0             	xor    %r8d,%r8d
  13218a:	eb 06                	jmp    132192 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x132>
  13218c:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  132192:	41 0f b6 f0          	movzbl %r8b,%esi
  132196:	38 1b                	cmp    %bl,(%rbx)
  132198:	4c 8d 73 08          	lea    0x8(%rbx),%r14
  13219c:	49 83 06 02          	addq   $0x2,(%r14)
  1321a0:	83 fe 02             	cmp    $0x2,%esi
  1321a3:	77 4e                	ja     1321f3 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x193>
  1321a5:	44 8b c6             	mov    %esi,%r8d
  1321a8:	48 8d 05 61 b5 06 00 	lea    0x6b561(%rip),%rax        # 19d710 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>>
  1321af:	42 8b 04 80          	mov    (%rax,%r8,4),%eax
  1321b3:	4c 8d 25 bf fe ff ff 	lea    -0x141(%rip),%r12        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  1321ba:	49 03 c4             	add    %r12,%rax
  1321bd:	ff e0                	jmp    *%rax
  1321bf:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  1321c3:	41 ff 00             	incl   (%r8)
  1321c6:	eb 2b                	jmp    1321f3 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x193>
  1321c8:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  1321cc:	41 ff 00             	incl   (%r8)
  1321cf:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  1321d4:	f3 0f 59 05 40 b5 06 	mulss  0x6b540(%rip),%xmm0        # 19d71c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc>
  1321db:	00 
  1321dc:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  1321e2:	f3 0f 58 03          	addss  (%rbx),%xmm0
  1321e6:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  1321ea:	eb 07                	jmp    1321f3 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x193>
  1321ec:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  1321f0:	41 ff 00             	incl   (%r8)
  1321f3:	f3 0f 10 05 21 b5 06 	movss  0x6b521(%rip),%xmm0        # 19d71c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc>
  1321fa:	00 
  1321fb:	4c 8b c3             	mov    %rbx,%r8
  1321fe:	49 8b cf             	mov    %r15,%rcx
  132201:	bf 01 00 00 00       	mov    $0x1,%edi
  132206:	e8 95 73 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  13220b:	8b 55 d0             	mov    -0x30(%rbp),%edx
  13220e:	e9 69 0b 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  132213:	81 fa 02 02 00 00    	cmp    $0x202,%edx
  132219:	75 08                	jne    132223 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1c3>
  13221b:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  132221:	eb 19                	jmp    13223c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1dc>
  132223:	45 85 f6             	test   %r14d,%r14d
  132226:	75 09                	jne    132231 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1d1>
  132228:	41 81 fc 41 01 00 00 	cmp    $0x141,%r12d
  13222f:	73 05                	jae    132236 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1d6>
  132231:	45 33 c0             	xor    %r8d,%r8d
  132234:	eb 06                	jmp    13223c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1dc>
  132236:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  13223c:	45 0f b6 f0          	movzbl %r8b,%r14d
  132240:	38 1b                	cmp    %bl,(%rbx)
  132242:	4c 8d 43 08          	lea    0x8(%rbx),%r8
  132246:	4d 8b e0             	mov    %r8,%r12
  132249:	4d 8b c4             	mov    %r12,%r8
  13224c:	49 ff 00             	incq   (%r8)
  13224f:	41 83 fe 02          	cmp    $0x2,%r14d
  132253:	77 46                	ja     13229b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x23b>
  132255:	45 8b c6             	mov    %r14d,%r8d
  132258:	48 8d 35 c1 b4 06 00 	lea    0x6b4c1(%rip),%rsi        # 19d720 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x10>
  13225f:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132263:	48 8d 05 0f fe ff ff 	lea    -0x1f1(%rip),%rax        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  13226a:	48 03 f0             	add    %rax,%rsi
  13226d:	ff e6                	jmp    *%rsi
  13226f:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132273:	41 ff 00             	incl   (%r8)
  132276:	eb 23                	jmp    13229b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x23b>
  132278:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  13227c:	41 ff 00             	incl   (%r8)
  13227f:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132284:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  13228a:	f3 0f 58 03          	addss  (%rbx),%xmm0
  13228e:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132292:	eb 07                	jmp    13229b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x23b>
  132294:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132298:	41 ff 00             	incl   (%r8)
  13229b:	f3 0f 10 05 89 b4 06 	movss  0x6b489(%rip),%xmm0        # 19d72c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1c>
  1322a2:	00 
  1322a3:	4c 8b c3             	mov    %rbx,%r8
  1322a6:	41 8b f6             	mov    %r14d,%esi
  1322a9:	49 8b cf             	mov    %r15,%rcx
  1322ac:	33 ff                	xor    %edi,%edi
  1322ae:	e8 ed 72 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  1322b3:	4d 8b c4             	mov    %r12,%r8
  1322b6:	49 83 00 03          	addq   $0x3,(%r8)
  1322ba:	41 83 fe 02          	cmp    $0x2,%r14d
  1322be:	77 4e                	ja     13230e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2ae>
  1322c0:	45 8b c6             	mov    %r14d,%r8d
  1322c3:	48 8d 35 66 b4 06 00 	lea    0x6b466(%rip),%rsi        # 19d730 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x20>
  1322ca:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  1322ce:	48 8d 15 a4 fd ff ff 	lea    -0x25c(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  1322d5:	48 03 f2             	add    %rdx,%rsi
  1322d8:	ff e6                	jmp    *%rsi
  1322da:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  1322de:	41 ff 00             	incl   (%r8)
  1322e1:	eb 2b                	jmp    13230e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2ae>
  1322e3:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  1322e7:	41 ff 00             	incl   (%r8)
  1322ea:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  1322ef:	f3 0f 59 05 45 b4 06 	mulss  0x6b445(%rip),%xmm0        # 19d73c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2c>
  1322f6:	00 
  1322f7:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  1322fd:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132301:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132305:	eb 07                	jmp    13230e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2ae>
  132307:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  13230b:	41 ff 00             	incl   (%r8)
  13230e:	f3 0f 10 05 26 b4 06 	movss  0x6b426(%rip),%xmm0        # 19d73c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2c>
  132315:	00 
  132316:	4c 8b c3             	mov    %rbx,%r8
  132319:	41 8b f6             	mov    %r14d,%esi
  13231c:	8b 55 d0             	mov    -0x30(%rbp),%edx
  13231f:	49 8b cf             	mov    %r15,%rcx
  132322:	bf 02 00 00 00       	mov    $0x2,%edi
  132327:	e8 74 72 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  13232c:	4d 8b c4             	mov    %r12,%r8
  13232f:	49 83 00 04          	addq   $0x4,(%r8)
  132333:	41 83 fe 02          	cmp    $0x2,%r14d
  132337:	77 4e                	ja     132387 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x327>
  132339:	45 8b c6             	mov    %r14d,%r8d
  13233c:	48 8d 35 fd b3 06 00 	lea    0x6b3fd(%rip),%rsi        # 19d740 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x30>
  132343:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132347:	48 8d 15 2b fd ff ff 	lea    -0x2d5(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  13234e:	48 03 f2             	add    %rdx,%rsi
  132351:	ff e6                	jmp    *%rsi
  132353:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132357:	41 ff 00             	incl   (%r8)
  13235a:	eb 2b                	jmp    132387 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x327>
  13235c:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132360:	41 ff 00             	incl   (%r8)
  132363:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132368:	f3 0f 59 05 dc b3 06 	mulss  0x6b3dc(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  13236f:	00 
  132370:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132376:	f3 0f 58 03          	addss  (%rbx),%xmm0
  13237a:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  13237e:	eb 07                	jmp    132387 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x327>
  132380:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132384:	41 ff 00             	incl   (%r8)
  132387:	f3 0f 10 05 bd b3 06 	movss  0x6b3bd(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  13238e:	00 
  13238f:	4c 8b c3             	mov    %rbx,%r8
  132392:	41 8b f6             	mov    %r14d,%esi
  132395:	8b 55 d0             	mov    -0x30(%rbp),%edx
  132398:	49 8b cf             	mov    %r15,%rcx
  13239b:	bf 03 00 00 00       	mov    $0x3,%edi
  1323a0:	e8 fb 71 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  1323a5:	8b 55 d0             	mov    -0x30(%rbp),%edx
  1323a8:	e9 cf 09 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  1323ad:	81 fa 40 01 00 00    	cmp    $0x140,%edx
  1323b3:	75 08                	jne    1323bd <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x35d>
  1323b5:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  1323bb:	eb 16                	jmp    1323d3 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x373>
  1323bd:	45 85 f6             	test   %r14d,%r14d
  1323c0:	75 06                	jne    1323c8 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x368>
  1323c2:	41 83 fc 7b          	cmp    $0x7b,%r12d
  1323c6:	73 05                	jae    1323cd <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x36d>
  1323c8:	45 33 c0             	xor    %r8d,%r8d
  1323cb:	eb 06                	jmp    1323d3 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x373>
  1323cd:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  1323d3:	41 0f b6 f0          	movzbl %r8b,%esi
  1323d7:	38 1b                	cmp    %bl,(%rbx)
  1323d9:	4c 8d 43 08          	lea    0x8(%rbx),%r8
  1323dd:	49 8b c0             	mov    %r8,%rax
  1323e0:	48 89 45 b0          	mov    %rax,-0x50(%rbp)
  1323e4:	4c 8b c0             	mov    %rax,%r8
  1323e7:	49 83 00 03          	addq   $0x3,(%r8)
  1323eb:	83 fe 02             	cmp    $0x2,%esi
  1323ee:	77 4f                	ja     13243f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3df>
  1323f0:	44 8b c6             	mov    %esi,%r8d
  1323f3:	4c 8d 0d 56 b3 06 00 	lea    0x6b356(%rip),%r9        # 19d750 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x40>
  1323fa:	47 8b 0c 81          	mov    (%r9,%r8,4),%r9d
  1323fe:	4c 8d 15 74 fc ff ff 	lea    -0x38c(%rip),%r10        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132405:	4d 03 ca             	add    %r10,%r9
  132408:	41 ff e1             	jmp    *%r9
  13240b:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  13240f:	41 ff 00             	incl   (%r8)
  132412:	eb 2b                	jmp    13243f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3df>
  132414:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132418:	41 ff 00             	incl   (%r8)
  13241b:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132420:	f3 0f 59 05 34 b3 06 	mulss  0x6b334(%rip),%xmm0        # 19d75c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c>
  132427:	00 
  132428:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  13242e:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132432:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132436:	eb 07                	jmp    13243f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3df>
  132438:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  13243c:	41 ff 00             	incl   (%r8)
  13243f:	f3 0f 10 05 15 b3 06 	movss  0x6b315(%rip),%xmm0        # 19d75c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c>
  132446:	00 
  132447:	4c 8b c3             	mov    %rbx,%r8
  13244a:	49 8b cf             	mov    %r15,%rcx
  13244d:	bf 02 00 00 00       	mov    $0x2,%edi
  132452:	e8 49 71 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132457:	8b 45 d0             	mov    -0x30(%rbp),%eax
  13245a:	3d 40 01 00 00       	cmp    $0x140,%eax
  13245f:	75 08                	jne    132469 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x409>
  132461:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  132467:	eb 19                	jmp    132482 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x422>
  132469:	45 85 f6             	test   %r14d,%r14d
  13246c:	75 09                	jne    132477 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x417>
  13246e:	41 81 fc c8 00 00 00 	cmp    $0xc8,%r12d
  132475:	73 05                	jae    13247c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x41c>
  132477:	45 33 c0             	xor    %r8d,%r8d
  13247a:	eb 06                	jmp    132482 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x422>
  13247c:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  132482:	41 0f b6 f0          	movzbl %r8b,%esi
  132486:	4c 8b 75 b0          	mov    -0x50(%rbp),%r14
  13248a:	4d 8b c6             	mov    %r14,%r8
  13248d:	49 83 00 04          	addq   $0x4,(%r8)
  132491:	83 fe 02             	cmp    $0x2,%esi
  132494:	77 4e                	ja     1324e4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x484>
  132496:	44 8b c6             	mov    %esi,%r8d
  132499:	48 8d 15 c0 b2 06 00 	lea    0x6b2c0(%rip),%rdx        # 19d760 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x50>
  1324a0:	42 8b 14 82          	mov    (%rdx,%r8,4),%edx
  1324a4:	48 8d 0d ce fb ff ff 	lea    -0x432(%rip),%rcx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  1324ab:	48 03 d1             	add    %rcx,%rdx
  1324ae:	ff e2                	jmp    *%rdx
  1324b0:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  1324b4:	41 ff 00             	incl   (%r8)
  1324b7:	eb 2b                	jmp    1324e4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x484>
  1324b9:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  1324bd:	41 ff 00             	incl   (%r8)
  1324c0:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  1324c5:	f3 0f 59 05 7f b2 06 	mulss  0x6b27f(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  1324cc:	00 
  1324cd:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  1324d3:	f3 0f 58 03          	addss  (%rbx),%xmm0
  1324d7:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  1324db:	eb 07                	jmp    1324e4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x484>
  1324dd:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  1324e1:	41 ff 00             	incl   (%r8)
  1324e4:	f3 0f 10 05 60 b2 06 	movss  0x6b260(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  1324eb:	00 
  1324ec:	4c 8b c3             	mov    %rbx,%r8
  1324ef:	8b d0                	mov    %eax,%edx
  1324f1:	49 8b cf             	mov    %r15,%rcx
  1324f4:	bf 03 00 00 00       	mov    $0x3,%edi
  1324f9:	e8 a2 70 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  1324fe:	8b 55 d0             	mov    -0x30(%rbp),%edx
  132501:	e9 76 08 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  132506:	83 fa 4c             	cmp    $0x4c,%edx
  132509:	0f 82 dd 01 00 00    	jb     1326ec <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x68c>
  13250f:	83 fa 7b             	cmp    $0x7b,%edx
  132512:	0f 82 94 00 00 00    	jb     1325ac <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x54c>
  132518:	45 85 f6             	test   %r14d,%r14d
  13251b:	75 06                	jne    132523 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c3>
  13251d:	41 83 fc 7b          	cmp    $0x7b,%r12d
  132521:	73 04                	jae    132527 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c7>
  132523:	33 f6                	xor    %esi,%esi
  132525:	eb 05                	jmp    13252c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4cc>
  132527:	be 01 00 00 00       	mov    $0x1,%esi
  13252c:	38 1b                	cmp    %bl,(%rbx)
  13252e:	4c 8d 73 08          	lea    0x8(%rbx),%r14
  132532:	4d 8b c6             	mov    %r14,%r8
  132535:	49 83 00 03          	addq   $0x3,(%r8)
  132539:	83 fe 02             	cmp    $0x2,%esi
  13253c:	77 4e                	ja     13258c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x52c>
  13253e:	44 8b c6             	mov    %esi,%r8d
  132541:	48 8d 05 24 b2 06 00 	lea    0x6b224(%rip),%rax        # 19d76c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x5c>
  132548:	42 8b 04 80          	mov    (%rax,%r8,4),%eax
  13254c:	4c 8d 25 26 fb ff ff 	lea    -0x4da(%rip),%r12        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132553:	49 03 c4             	add    %r12,%rax
  132556:	ff e0                	jmp    *%rax
  132558:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  13255c:	41 ff 00             	incl   (%r8)
  13255f:	eb 2b                	jmp    13258c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x52c>
  132561:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132565:	41 ff 00             	incl   (%r8)
  132568:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  13256d:	f3 0f 59 05 e7 b1 06 	mulss  0x6b1e7(%rip),%xmm0        # 19d75c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c>
  132574:	00 
  132575:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  13257b:	f3 0f 58 03          	addss  (%rbx),%xmm0
  13257f:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132583:	eb 07                	jmp    13258c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x52c>
  132585:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132589:	41 ff 00             	incl   (%r8)
  13258c:	f3 0f 10 05 c8 b1 06 	movss  0x6b1c8(%rip),%xmm0        # 19d75c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c>
  132593:	00 
  132594:	4c 8b c3             	mov    %rbx,%r8
  132597:	49 8b cf             	mov    %r15,%rcx
  13259a:	bf 02 00 00 00       	mov    $0x2,%edi
  13259f:	e8 fc 6f f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  1325a4:	8b 55 d0             	mov    -0x30(%rbp),%edx
  1325a7:	e9 d0 07 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  1325ac:	83 fa 7a             	cmp    $0x7a,%edx
  1325af:	75 08                	jne    1325b9 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x559>
  1325b1:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  1325b7:	eb 16                	jmp    1325cf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x56f>
  1325b9:	45 85 f6             	test   %r14d,%r14d
  1325bc:	75 06                	jne    1325c4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x564>
  1325be:	41 83 fc 4c          	cmp    $0x4c,%r12d
  1325c2:	73 05                	jae    1325c9 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x569>
  1325c4:	45 33 c0             	xor    %r8d,%r8d
  1325c7:	eb 06                	jmp    1325cf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x56f>
  1325c9:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  1325cf:	45 0f b6 f0          	movzbl %r8b,%r14d
  1325d3:	38 1b                	cmp    %bl,(%rbx)
  1325d5:	4c 8d 43 08          	lea    0x8(%rbx),%r8
  1325d9:	4d 8b e0             	mov    %r8,%r12
  1325dc:	4d 8b c4             	mov    %r12,%r8
  1325df:	49 83 00 02          	addq   $0x2,(%r8)
  1325e3:	41 83 fe 02          	cmp    $0x2,%r14d
  1325e7:	77 4e                	ja     132637 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x5d7>
  1325e9:	45 8b c6             	mov    %r14d,%r8d
  1325ec:	48 8d 35 85 b1 06 00 	lea    0x6b185(%rip),%rsi        # 19d778 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x68>
  1325f3:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  1325f7:	48 8d 05 7b fa ff ff 	lea    -0x585(%rip),%rax        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  1325fe:	48 03 f0             	add    %rax,%rsi
  132601:	ff e6                	jmp    *%rsi
  132603:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132607:	41 ff 00             	incl   (%r8)
  13260a:	eb 2b                	jmp    132637 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x5d7>
  13260c:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132610:	41 ff 00             	incl   (%r8)
  132613:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132618:	f3 0f 59 05 1c b1 06 	mulss  0x6b11c(%rip),%xmm0        # 19d73c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2c>
  13261f:	00 
  132620:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132626:	f3 0f 58 03          	addss  (%rbx),%xmm0
  13262a:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  13262e:	eb 07                	jmp    132637 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x5d7>
  132630:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132634:	41 ff 00             	incl   (%r8)
  132637:	f3 0f 10 05 fd b0 06 	movss  0x6b0fd(%rip),%xmm0        # 19d73c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2c>
  13263e:	00 
  13263f:	4c 8b c3             	mov    %rbx,%r8
  132642:	41 8b f6             	mov    %r14d,%esi
  132645:	49 8b cf             	mov    %r15,%rcx
  132648:	bf 01 00 00 00       	mov    $0x1,%edi
  13264d:	e8 4e 6f f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132652:	8b 45 d0             	mov    -0x30(%rbp),%eax
  132655:	44 8d 40 b4          	lea    -0x4c(%rax),%r8d
  132659:	0f 57 c0             	xorps  %xmm0,%xmm0
  13265c:	f3 49 0f 2a c0       	cvtsi2ss %r8,%xmm0
  132661:	f3 0f 5e 05 1b b1 06 	divss  0x6b11b(%rip),%xmm0        # 19d784 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x74>
  132668:	00 
  132669:	f3 0f 59 05 17 b1 06 	mulss  0x6b117(%rip),%xmm0        # 19d788 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x78>
  132670:	00 
  132671:	f3 0f 58 05 13 b1 06 	addss  0x6b113(%rip),%xmm0        # 19d78c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x7c>
  132678:	00 
  132679:	4d 8b c4             	mov    %r12,%r8
  13267c:	49 83 00 04          	addq   $0x4,(%r8)
  132680:	41 83 fe 02          	cmp    $0x2,%r14d
  132684:	77 49                	ja     1326cf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x66f>
  132686:	45 8b c6             	mov    %r14d,%r8d
  132689:	48 8d 35 00 b1 06 00 	lea    0x6b100(%rip),%rsi        # 19d790 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x80>
  132690:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132694:	48 8d 15 de f9 ff ff 	lea    -0x622(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  13269b:	48 03 f2             	add    %rdx,%rsi
  13269e:	ff e6                	jmp    *%rsi
  1326a0:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  1326a4:	41 ff 00             	incl   (%r8)
  1326a7:	eb 26                	jmp    1326cf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x66f>
  1326a9:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  1326ad:	41 ff 00             	incl   (%r8)
  1326b0:	0f 28 c8             	movaps %xmm0,%xmm1
  1326b3:	f3 41 0f 59 0f       	mulss  (%r15),%xmm1
  1326b8:	f3 41 0f 58 4f 04    	addss  0x4(%r15),%xmm1
  1326be:	f3 0f 58 0b          	addss  (%rbx),%xmm1
  1326c2:	f3 0f 11 0b          	movss  %xmm1,(%rbx)
  1326c6:	eb 07                	jmp    1326cf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x66f>
  1326c8:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  1326cc:	41 ff 00             	incl   (%r8)
  1326cf:	4c 8b c3             	mov    %rbx,%r8
  1326d2:	41 8b f6             	mov    %r14d,%esi
  1326d5:	8b d0                	mov    %eax,%edx
  1326d7:	49 8b cf             	mov    %r15,%rcx
  1326da:	bf 03 00 00 00       	mov    $0x3,%edi
  1326df:	e8 bc 6e f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  1326e4:	8b 55 d0             	mov    -0x30(%rbp),%edx
  1326e7:	e9 90 06 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  1326ec:	83 fa 4b             	cmp    $0x4b,%edx
  1326ef:	75 08                	jne    1326f9 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x699>
  1326f1:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  1326f7:	eb 16                	jmp    13270f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x6af>
  1326f9:	45 85 f6             	test   %r14d,%r14d
  1326fc:	75 06                	jne    132704 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x6a4>
  1326fe:	41 83 fc 2f          	cmp    $0x2f,%r12d
  132702:	73 05                	jae    132709 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x6a9>
  132704:	45 33 c0             	xor    %r8d,%r8d
  132707:	eb 06                	jmp    13270f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x6af>
  132709:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  13270f:	45 0f b6 f0          	movzbl %r8b,%r14d
  132713:	38 1b                	cmp    %bl,(%rbx)
  132715:	4c 8d 63 08          	lea    0x8(%rbx),%r12
  132719:	4d 8b c4             	mov    %r12,%r8
  13271c:	49 ff 00             	incq   (%r8)
  13271f:	41 83 fe 02          	cmp    $0x2,%r14d
  132723:	77 4e                	ja     132773 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x713>
  132725:	45 8b c6             	mov    %r14d,%r8d
  132728:	48 8d 35 6d b0 06 00 	lea    0x6b06d(%rip),%rsi        # 19d79c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x8c>
  13272f:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132733:	48 8d 05 3f f9 ff ff 	lea    -0x6c1(%rip),%rax        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  13273a:	48 03 f0             	add    %rax,%rsi
  13273d:	ff e6                	jmp    *%rsi
  13273f:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132743:	41 ff 00             	incl   (%r8)
  132746:	eb 2b                	jmp    132773 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x713>
  132748:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  13274c:	41 ff 00             	incl   (%r8)
  13274f:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132754:	f3 0f 59 05 4c b0 06 	mulss  0x6b04c(%rip),%xmm0        # 19d7a8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x98>
  13275b:	00 
  13275c:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132762:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132766:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  13276a:	eb 07                	jmp    132773 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x713>
  13276c:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132770:	41 ff 00             	incl   (%r8)
  132773:	f3 0f 10 05 2d b0 06 	movss  0x6b02d(%rip),%xmm0        # 19d7a8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x98>
  13277a:	00 
  13277b:	4c 8b c3             	mov    %rbx,%r8
  13277e:	41 8b f6             	mov    %r14d,%esi
  132781:	49 8b cf             	mov    %r15,%rcx
  132784:	33 ff                	xor    %edi,%edi
  132786:	e8 15 6e f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  13278b:	4d 8b c4             	mov    %r12,%r8
  13278e:	49 83 00 03          	addq   $0x3,(%r8)
  132792:	41 83 fe 02          	cmp    $0x2,%r14d
  132796:	77 4e                	ja     1327e6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x786>
  132798:	45 8b c6             	mov    %r14d,%r8d
  13279b:	48 8d 35 0a b0 06 00 	lea    0x6b00a(%rip),%rsi        # 19d7ac <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x9c>
  1327a2:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  1327a6:	48 8d 15 cc f8 ff ff 	lea    -0x734(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  1327ad:	48 03 f2             	add    %rdx,%rsi
  1327b0:	ff e6                	jmp    *%rsi
  1327b2:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  1327b6:	41 ff 00             	incl   (%r8)
  1327b9:	eb 2b                	jmp    1327e6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x786>
  1327bb:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  1327bf:	41 ff 00             	incl   (%r8)
  1327c2:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  1327c7:	f3 0f 59 05 8d af 06 	mulss  0x6af8d(%rip),%xmm0        # 19d75c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c>
  1327ce:	00 
  1327cf:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  1327d5:	f3 0f 58 03          	addss  (%rbx),%xmm0
  1327d9:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  1327dd:	eb 07                	jmp    1327e6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x786>
  1327df:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  1327e3:	41 ff 00             	incl   (%r8)
  1327e6:	f3 0f 10 05 6e af 06 	movss  0x6af6e(%rip),%xmm0        # 19d75c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c>
  1327ed:	00 
  1327ee:	4c 8b c3             	mov    %rbx,%r8
  1327f1:	41 8b f6             	mov    %r14d,%esi
  1327f4:	8b 55 d0             	mov    -0x30(%rbp),%edx
  1327f7:	49 8b cf             	mov    %r15,%rcx
  1327fa:	bf 02 00 00 00       	mov    $0x2,%edi
  1327ff:	e8 9c 6d f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132804:	8b 55 d0             	mov    -0x30(%rbp),%edx
  132807:	e9 70 05 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  13280c:	83 fa 0b             	cmp    $0xb,%edx
  13280f:	0f 82 f0 01 00 00    	jb     132a05 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x9a5>
  132815:	83 fa 12             	cmp    $0x12,%edx
  132818:	0f 82 5e 05 00 00    	jb     132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  13281e:	83 fa 1d             	cmp    $0x1d,%edx
  132821:	0f 82 ba 00 00 00    	jb     1328e1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x881>
  132827:	83 fa 2e             	cmp    $0x2e,%edx
  13282a:	75 08                	jne    132834 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x7d4>
  13282c:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  132832:	eb 16                	jmp    13284a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x7ea>
  132834:	45 85 f6             	test   %r14d,%r14d
  132837:	75 06                	jne    13283f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x7df>
  132839:	41 83 fc 1d          	cmp    $0x1d,%r12d
  13283d:	73 05                	jae    132844 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x7e4>
  13283f:	45 33 c0             	xor    %r8d,%r8d
  132842:	eb 06                	jmp    13284a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x7ea>
  132844:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  13284a:	41 0f b6 f0          	movzbl %r8b,%esi
  13284e:	44 8d 42 e3          	lea    -0x1d(%rdx),%r8d
  132852:	0f 57 c0             	xorps  %xmm0,%xmm0
  132855:	f3 49 0f 2a c0       	cvtsi2ss %r8,%xmm0
  13285a:	f3 0f 5e 05 56 af 06 	divss  0x6af56(%rip),%xmm0        # 19d7b8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xa8>
  132861:	00 
  132862:	f3 0f 59 05 d2 ae 06 	mulss  0x6aed2(%rip),%xmm0        # 19d73c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2c>
  132869:	00 
  13286a:	f3 0f 58 05 36 af 06 	addss  0x6af36(%rip),%xmm0        # 19d7a8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x98>
  132871:	00 
  132872:	38 1b                	cmp    %bl,(%rbx)
  132874:	4c 8d 63 08          	lea    0x8(%rbx),%r12
  132878:	4d 8b c4             	mov    %r12,%r8
  13287b:	49 ff 00             	incq   (%r8)
  13287e:	83 fe 02             	cmp    $0x2,%esi
  132881:	77 49                	ja     1328cc <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x86c>
  132883:	44 8b c6             	mov    %esi,%r8d
  132886:	48 8d 05 2f af 06 00 	lea    0x6af2f(%rip),%rax        # 19d7bc <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xac>
  13288d:	42 8b 04 80          	mov    (%rax,%r8,4),%eax
  132891:	4c 8d 35 e1 f7 ff ff 	lea    -0x81f(%rip),%r14        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132898:	49 03 c6             	add    %r14,%rax
  13289b:	ff e0                	jmp    *%rax
  13289d:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  1328a1:	41 ff 00             	incl   (%r8)
  1328a4:	eb 26                	jmp    1328cc <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x86c>
  1328a6:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  1328aa:	41 ff 00             	incl   (%r8)
  1328ad:	0f 28 c8             	movaps %xmm0,%xmm1
  1328b0:	f3 41 0f 59 0f       	mulss  (%r15),%xmm1
  1328b5:	f3 41 0f 58 4f 04    	addss  0x4(%r15),%xmm1
  1328bb:	f3 0f 58 0b          	addss  (%rbx),%xmm1
  1328bf:	f3 0f 11 0b          	movss  %xmm1,(%rbx)
  1328c3:	eb 07                	jmp    1328cc <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x86c>
  1328c5:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  1328c9:	41 ff 00             	incl   (%r8)
  1328cc:	4c 8b c3             	mov    %rbx,%r8
  1328cf:	49 8b cf             	mov    %r15,%rcx
  1328d2:	33 ff                	xor    %edi,%edi
  1328d4:	e8 c7 6c f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  1328d9:	8b 55 d0             	mov    -0x30(%rbp),%edx
  1328dc:	e9 9b 04 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  1328e1:	83 fa 1c             	cmp    $0x1c,%edx
  1328e4:	75 08                	jne    1328ee <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x88e>
  1328e6:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  1328ec:	eb 16                	jmp    132904 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x8a4>
  1328ee:	45 85 f6             	test   %r14d,%r14d
  1328f1:	75 06                	jne    1328f9 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x899>
  1328f3:	41 83 fc 12          	cmp    $0x12,%r12d
  1328f7:	73 05                	jae    1328fe <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x89e>
  1328f9:	45 33 c0             	xor    %r8d,%r8d
  1328fc:	eb 06                	jmp    132904 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x8a4>
  1328fe:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  132904:	45 0f b6 f0          	movzbl %r8b,%r14d
  132908:	38 1b                	cmp    %bl,(%rbx)
  13290a:	4c 8d 63 08          	lea    0x8(%rbx),%r12
  13290e:	4d 8b c4             	mov    %r12,%r8
  132911:	49 83 00 02          	addq   $0x2,(%r8)
  132915:	41 83 fe 02          	cmp    $0x2,%r14d
  132919:	77 4e                	ja     132969 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x909>
  13291b:	45 8b c6             	mov    %r14d,%r8d
  13291e:	48 8d 35 a3 ae 06 00 	lea    0x6aea3(%rip),%rsi        # 19d7c8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb8>
  132925:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132929:	48 8d 05 49 f7 ff ff 	lea    -0x8b7(%rip),%rax        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132930:	48 03 f0             	add    %rax,%rsi
  132933:	ff e6                	jmp    *%rsi
  132935:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132939:	41 ff 00             	incl   (%r8)
  13293c:	eb 2b                	jmp    132969 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x909>
  13293e:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132942:	41 ff 00             	incl   (%r8)
  132945:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  13294a:	f3 0f 59 05 ea ad 06 	mulss  0x6adea(%rip),%xmm0        # 19d73c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2c>
  132951:	00 
  132952:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132958:	f3 0f 58 03          	addss  (%rbx),%xmm0
  13295c:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132960:	eb 07                	jmp    132969 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x909>
  132962:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132966:	41 ff 00             	incl   (%r8)
  132969:	f3 0f 10 05 cb ad 06 	movss  0x6adcb(%rip),%xmm0        # 19d73c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x2c>
  132970:	00 
  132971:	4c 8b c3             	mov    %rbx,%r8
  132974:	41 8b f6             	mov    %r14d,%esi
  132977:	49 8b cf             	mov    %r15,%rcx
  13297a:	bf 01 00 00 00       	mov    $0x1,%edi
  13297f:	e8 1c 6c f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132984:	4d 8b c4             	mov    %r12,%r8
  132987:	49 83 00 04          	addq   $0x4,(%r8)
  13298b:	41 83 fe 02          	cmp    $0x2,%r14d
  13298f:	77 4e                	ja     1329df <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x97f>
  132991:	45 8b c6             	mov    %r14d,%r8d
  132994:	48 8d 35 39 ae 06 00 	lea    0x6ae39(%rip),%rsi        # 19d7d4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc4>
  13299b:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  13299f:	48 8d 15 d3 f6 ff ff 	lea    -0x92d(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  1329a6:	48 03 f2             	add    %rdx,%rsi
  1329a9:	ff e6                	jmp    *%rsi
  1329ab:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  1329af:	41 ff 00             	incl   (%r8)
  1329b2:	eb 2b                	jmp    1329df <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x97f>
  1329b4:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  1329b8:	41 ff 00             	incl   (%r8)
  1329bb:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  1329c0:	f3 0f 59 05 84 ad 06 	mulss  0x6ad84(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  1329c7:	00 
  1329c8:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  1329ce:	f3 0f 58 03          	addss  (%rbx),%xmm0
  1329d2:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  1329d6:	eb 07                	jmp    1329df <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x97f>
  1329d8:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  1329dc:	41 ff 00             	incl   (%r8)
  1329df:	f3 0f 10 05 65 ad 06 	movss  0x6ad65(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  1329e6:	00 
  1329e7:	4c 8b c3             	mov    %rbx,%r8
  1329ea:	41 8b f6             	mov    %r14d,%esi
  1329ed:	8b 55 d0             	mov    -0x30(%rbp),%edx
  1329f0:	49 8b cf             	mov    %r15,%rcx
  1329f3:	bf 03 00 00 00       	mov    $0x3,%edi
  1329f8:	e8 a3 6b f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  1329fd:	8b 55 d0             	mov    -0x30(%rbp),%edx
  132a00:	e9 77 03 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  132a05:	83 fa 03             	cmp    $0x3,%edx
  132a08:	0f 82 ee 02 00 00    	jb     132cfc <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc9c>
  132a0e:	83 fa 07             	cmp    $0x7,%edx
  132a11:	0f 82 24 01 00 00    	jb     132b3b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xadb>
  132a17:	83 fa 0a             	cmp    $0xa,%edx
  132a1a:	75 08                	jne    132a24 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x9c4>
  132a1c:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  132a22:	eb 16                	jmp    132a3a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x9da>
  132a24:	45 85 f6             	test   %r14d,%r14d
  132a27:	75 06                	jne    132a2f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x9cf>
  132a29:	41 83 fc 03          	cmp    $0x3,%r12d
  132a2d:	73 05                	jae    132a34 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x9d4>
  132a2f:	45 33 c0             	xor    %r8d,%r8d
  132a32:	eb 06                	jmp    132a3a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x9da>
  132a34:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  132a3a:	45 0f b6 f0          	movzbl %r8b,%r14d
  132a3e:	38 1b                	cmp    %bl,(%rbx)
  132a40:	4c 8d 63 08          	lea    0x8(%rbx),%r12
  132a44:	4d 8b c4             	mov    %r12,%r8
  132a47:	49 83 00 02          	addq   $0x2,(%r8)
  132a4b:	41 83 fe 02          	cmp    $0x2,%r14d
  132a4f:	77 4e                	ja     132a9f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xa3f>
  132a51:	45 8b c6             	mov    %r14d,%r8d
  132a54:	48 8d 35 85 ad 06 00 	lea    0x6ad85(%rip),%rsi        # 19d7e0 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd0>
  132a5b:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132a5f:	48 8d 05 13 f6 ff ff 	lea    -0x9ed(%rip),%rax        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132a66:	48 03 f0             	add    %rax,%rsi
  132a69:	ff e6                	jmp    *%rsi
  132a6b:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132a6f:	41 ff 00             	incl   (%r8)
  132a72:	eb 2b                	jmp    132a9f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xa3f>
  132a74:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132a78:	41 ff 00             	incl   (%r8)
  132a7b:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132a80:	f3 0f 59 05 94 ac 06 	mulss  0x6ac94(%rip),%xmm0        # 19d71c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc>
  132a87:	00 
  132a88:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132a8e:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132a92:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132a96:	eb 07                	jmp    132a9f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xa3f>
  132a98:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132a9c:	41 ff 00             	incl   (%r8)
  132a9f:	f3 0f 10 05 75 ac 06 	movss  0x6ac75(%rip),%xmm0        # 19d71c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc>
  132aa6:	00 
  132aa7:	4c 8b c3             	mov    %rbx,%r8
  132aaa:	41 8b f6             	mov    %r14d,%esi
  132aad:	49 8b cf             	mov    %r15,%rcx
  132ab0:	bf 01 00 00 00       	mov    $0x1,%edi
  132ab5:	e8 e6 6a f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132aba:	4d 8b c4             	mov    %r12,%r8
  132abd:	49 83 00 04          	addq   $0x4,(%r8)
  132ac1:	41 83 fe 02          	cmp    $0x2,%r14d
  132ac5:	77 4e                	ja     132b15 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xab5>
  132ac7:	45 8b c6             	mov    %r14d,%r8d
  132aca:	48 8d 35 1b ad 06 00 	lea    0x6ad1b(%rip),%rsi        # 19d7ec <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xdc>
  132ad1:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132ad5:	48 8d 15 9d f5 ff ff 	lea    -0xa63(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132adc:	48 03 f2             	add    %rdx,%rsi
  132adf:	ff e6                	jmp    *%rsi
  132ae1:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132ae5:	41 ff 00             	incl   (%r8)
  132ae8:	eb 2b                	jmp    132b15 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xab5>
  132aea:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132aee:	41 ff 00             	incl   (%r8)
  132af1:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132af6:	f3 0f 59 05 4e ac 06 	mulss  0x6ac4e(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  132afd:	00 
  132afe:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132b04:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132b08:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132b0c:	eb 07                	jmp    132b15 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xab5>
  132b0e:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132b12:	41 ff 00             	incl   (%r8)
  132b15:	f3 0f 10 05 2f ac 06 	movss  0x6ac2f(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  132b1c:	00 
  132b1d:	4c 8b c3             	mov    %rbx,%r8
  132b20:	41 8b f6             	mov    %r14d,%esi
  132b23:	8b 55 d0             	mov    -0x30(%rbp),%edx
  132b26:	49 8b cf             	mov    %r15,%rcx
  132b29:	bf 03 00 00 00       	mov    $0x3,%edi
  132b2e:	e8 6d 6a f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132b33:	8b 55 d0             	mov    -0x30(%rbp),%edx
  132b36:	e9 41 02 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  132b3b:	83 fa 06             	cmp    $0x6,%edx
  132b3e:	75 08                	jne    132b48 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xae8>
  132b40:	41 b8 02 00 00 00    	mov    $0x2,%r8d
  132b46:	eb 10                	jmp    132b58 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xaf8>
  132b48:	45 85 f6             	test   %r14d,%r14d
  132b4b:	74 05                	je     132b52 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xaf2>
  132b4d:	45 33 c0             	xor    %r8d,%r8d
  132b50:	eb 06                	jmp    132b58 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xaf8>
  132b52:	41 b8 01 00 00 00    	mov    $0x1,%r8d
  132b58:	41 0f b6 f0          	movzbl %r8b,%esi
  132b5c:	38 1b                	cmp    %bl,(%rbx)
  132b5e:	4c 8d 43 08          	lea    0x8(%rbx),%r8
  132b62:	49 8b c0             	mov    %r8,%rax
  132b65:	48 89 45 b0          	mov    %rax,-0x50(%rbp)
  132b69:	4c 8b c0             	mov    %rax,%r8
  132b6c:	49 ff 00             	incq   (%r8)
  132b6f:	83 fe 02             	cmp    $0x2,%esi
  132b72:	77 47                	ja     132bbb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb5b>
  132b74:	44 8b c6             	mov    %esi,%r8d
  132b77:	4c 8d 0d 7a ac 06 00 	lea    0x6ac7a(%rip),%r9        # 19d7f8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xe8>
  132b7e:	47 8b 0c 81          	mov    (%r9,%r8,4),%r9d
  132b82:	4c 8d 15 f0 f4 ff ff 	lea    -0xb10(%rip),%r10        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132b89:	4d 03 ca             	add    %r10,%r9
  132b8c:	41 ff e1             	jmp    *%r9
  132b8f:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132b93:	41 ff 00             	incl   (%r8)
  132b96:	eb 23                	jmp    132bbb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb5b>
  132b98:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132b9c:	41 ff 00             	incl   (%r8)
  132b9f:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132ba4:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132baa:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132bae:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132bb2:	eb 07                	jmp    132bbb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb5b>
  132bb4:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132bb8:	41 ff 00             	incl   (%r8)
  132bbb:	f3 0f 10 05 69 ab 06 	movss  0x6ab69(%rip),%xmm0        # 19d72c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1c>
  132bc2:	00 
  132bc3:	4c 8b c3             	mov    %rbx,%r8
  132bc6:	49 8b cf             	mov    %r15,%rcx
  132bc9:	33 ff                	xor    %edi,%edi
  132bcb:	e8 d0 69 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132bd0:	45 85 f6             	test   %r14d,%r14d
  132bd3:	75 06                	jne    132bdb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb7b>
  132bd5:	41 83 fc 03          	cmp    $0x3,%r12d
  132bd9:	73 05                	jae    132be0 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb80>
  132bdb:	45 33 f6             	xor    %r14d,%r14d
  132bde:	eb 06                	jmp    132be6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xb86>
  132be0:	41 be 01 00 00 00    	mov    $0x1,%r14d
  132be6:	44 8b 65 d0          	mov    -0x30(%rbp),%r12d
  132bea:	45 8d 44 24 fd       	lea    -0x3(%r12),%r8d
  132bef:	0f 57 c0             	xorps  %xmm0,%xmm0
  132bf2:	f3 49 0f 2a c0       	cvtsi2ss %r8,%xmm0
  132bf7:	f3 0f 5e 05 1d ab 06 	divss  0x6ab1d(%rip),%xmm0        # 19d71c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc>
  132bfe:	00 
  132bff:	f3 0f 58 05 55 ab 06 	addss  0x6ab55(%rip),%xmm0        # 19d75c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x4c>
  132c06:	00 
  132c07:	48 8b 45 b0          	mov    -0x50(%rbp),%rax
  132c0b:	4c 8b c0             	mov    %rax,%r8
  132c0e:	49 83 00 02          	addq   $0x2,(%r8)
  132c12:	41 83 fe 02          	cmp    $0x2,%r14d
  132c16:	77 49                	ja     132c61 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc01>
  132c18:	45 8b c6             	mov    %r14d,%r8d
  132c1b:	48 8d 35 e2 ab 06 00 	lea    0x6abe2(%rip),%rsi        # 19d804 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xf4>
  132c22:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132c26:	48 8d 15 4c f4 ff ff 	lea    -0xbb4(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132c2d:	48 03 f2             	add    %rdx,%rsi
  132c30:	ff e6                	jmp    *%rsi
  132c32:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132c36:	41 ff 00             	incl   (%r8)
  132c39:	eb 26                	jmp    132c61 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc01>
  132c3b:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132c3f:	41 ff 00             	incl   (%r8)
  132c42:	0f 28 c8             	movaps %xmm0,%xmm1
  132c45:	f3 41 0f 59 0f       	mulss  (%r15),%xmm1
  132c4a:	f3 41 0f 58 4f 04    	addss  0x4(%r15),%xmm1
  132c50:	f3 0f 58 0b          	addss  (%rbx),%xmm1
  132c54:	f3 0f 11 0b          	movss  %xmm1,(%rbx)
  132c58:	eb 07                	jmp    132c61 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc01>
  132c5a:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132c5e:	41 ff 00             	incl   (%r8)
  132c61:	4c 8b c3             	mov    %rbx,%r8
  132c64:	41 8b f6             	mov    %r14d,%esi
  132c67:	41 8b d4             	mov    %r12d,%edx
  132c6a:	49 8b cf             	mov    %r15,%rcx
  132c6d:	bf 01 00 00 00       	mov    $0x1,%edi
  132c72:	e8 29 69 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132c77:	48 8b 45 b0          	mov    -0x50(%rbp),%rax
  132c7b:	4c 8b c0             	mov    %rax,%r8
  132c7e:	49 83 00 04          	addq   $0x4,(%r8)
  132c82:	41 83 fe 02          	cmp    $0x2,%r14d
  132c86:	77 4e                	ja     132cd6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc76>
  132c88:	45 8b c6             	mov    %r14d,%r8d
  132c8b:	48 8d 35 7e ab 06 00 	lea    0x6ab7e(%rip),%rsi        # 19d810 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x100>
  132c92:	42 8b 34 86          	mov    (%rsi,%r8,4),%esi
  132c96:	48 8d 15 dc f3 ff ff 	lea    -0xc24(%rip),%rdx        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132c9d:	48 03 f2             	add    %rdx,%rsi
  132ca0:	ff e6                	jmp    *%rsi
  132ca2:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132ca6:	41 ff 00             	incl   (%r8)
  132ca9:	eb 2b                	jmp    132cd6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc76>
  132cab:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132caf:	41 ff 00             	incl   (%r8)
  132cb2:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132cb7:	f3 0f 59 05 8d aa 06 	mulss  0x6aa8d(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  132cbe:	00 
  132cbf:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132cc5:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132cc9:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132ccd:	eb 07                	jmp    132cd6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xc76>
  132ccf:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132cd3:	41 ff 00             	incl   (%r8)
  132cd6:	f3 0f 10 05 6e aa 06 	movss  0x6aa6e(%rip),%xmm0        # 19d74c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x3c>
  132cdd:	00 
  132cde:	4c 8b c3             	mov    %rbx,%r8
  132ce1:	41 8b f6             	mov    %r14d,%esi
  132ce4:	41 8b d4             	mov    %r12d,%edx
  132ce7:	49 8b cf             	mov    %r15,%rcx
  132cea:	bf 03 00 00 00       	mov    $0x3,%edi
  132cef:	e8 ac 68 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132cf4:	41 8b d4             	mov    %r12d,%edx
  132cf7:	e9 80 00 00 00       	jmp    132d7c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd1c>
  132cfc:	45 85 f6             	test   %r14d,%r14d
  132cff:	74 04                	je     132d05 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xca5>
  132d01:	33 f6                	xor    %esi,%esi
  132d03:	eb 05                	jmp    132d0a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xcaa>
  132d05:	be 01 00 00 00       	mov    $0x1,%esi
  132d0a:	38 1b                	cmp    %bl,(%rbx)
  132d0c:	48 8d 43 08          	lea    0x8(%rbx),%rax
  132d10:	4c 8b f0             	mov    %rax,%r14
  132d13:	4d 8b c6             	mov    %r14,%r8
  132d16:	49 ff 00             	incq   (%r8)
  132d19:	83 fe 02             	cmp    $0x2,%esi
  132d1c:	77 46                	ja     132d64 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd04>
  132d1e:	44 8b c6             	mov    %esi,%r8d
  132d21:	48 8d 05 f4 aa 06 00 	lea    0x6aaf4(%rip),%rax        # 19d81c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x10c>
  132d28:	42 8b 04 80          	mov    (%rax,%r8,4),%eax
  132d2c:	4c 8d 35 46 f3 ff ff 	lea    -0xcba(%rip),%r14        # 132079 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x19>
  132d33:	49 03 c6             	add    %r14,%rax
  132d36:	ff e0                	jmp    *%rax
  132d38:	4c 8d 43 18          	lea    0x18(%rbx),%r8
  132d3c:	41 ff 00             	incl   (%r8)
  132d3f:	eb 23                	jmp    132d64 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd04>
  132d41:	4c 8d 43 14          	lea    0x14(%rbx),%r8
  132d45:	41 ff 00             	incl   (%r8)
  132d48:	f3 41 0f 10 07       	movss  (%r15),%xmm0
  132d4d:	f3 41 0f 58 47 04    	addss  0x4(%r15),%xmm0
  132d53:	f3 0f 58 03          	addss  (%rbx),%xmm0
  132d57:	f3 0f 11 03          	movss  %xmm0,(%rbx)
  132d5b:	eb 07                	jmp    132d64 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0xd04>
  132d5d:	4c 8d 43 10          	lea    0x10(%rbx),%r8
  132d61:	41 ff 00             	incl   (%r8)
  132d64:	f3 0f 10 05 c0 a9 06 	movss  0x6a9c0(%rip),%xmm0        # 19d72c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x1c>
  132d6b:	00 
  132d6c:	4c 8b c3             	mov    %rbx,%r8
  132d6f:	49 8b cf             	mov    %r15,%rcx
  132d72:	33 ff                	xor    %edi,%edi
  132d74:	e8 27 68 f4 ff       	call   795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>
  132d79:	8b 55 d0             	mov    -0x30(%rbp),%edx
  132d7c:	8b 7d cc             	mov    -0x34(%rbp),%edi
  132d7f:	44 8b f7             	mov    %edi,%r14d
  132d82:	44 8b e2             	mov    %edx,%r12d
  132d85:	8b 4d d4             	mov    -0x2c(%rbp),%ecx
  132d88:	8b c1                	mov    %ecx,%eax
  132d8a:	4c 8b 5d c0          	mov    -0x40(%rbp),%r11
  132d8e:	41 ff c3             	inc    %r11d
  132d91:	49 8b fb             	mov    %r11,%rdi
  132d94:	44 8b 55 c8          	mov    -0x38(%rbp),%r10d
  132d98:	41 3b fa             	cmp    %r10d,%edi
  132d9b:	4c 8b df             	mov    %rdi,%r11
  132d9e:	4c 8b 4d b8          	mov    -0x48(%rbp),%r9
  132da2:	0f 8c 2e f3 ff ff    	jl     1320d6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_EffectConsumer>+0x76>
  132da8:	b8 01 00 00 00       	mov    $0x1,%eax
  132dad:	bf 05 00 00 00       	mov    $0x5,%edi
  132db2:	41 81 fc 57 02 00 00 	cmp    $0x257,%r12d
  132db9:	0f 44 c7             	cmove  %edi,%eax
  132dbc:	41 8b fe             	mov    %r14d,%edi
  132dbf:	41 8b cd             	mov    %r13d,%ecx
  132dc2:	48 c1 e1 20          	shl    $0x20,%rcx
  132dc6:	48 0b f9             	or     %rcx,%rdi
  132dc9:	48 c1 e0 30          	shl    $0x30,%rax
  132dcd:	48 0b c7             	or     %rdi,%rax
  132dd0:	48 83 c4 28          	add    $0x28,%rsp
  132dd4:	5b                   	pop    %rbx
  132dd5:	41 5c                	pop    %r12
  132dd7:	41 5d                	pop    %r13
  132dd9:	41 5e                	pop    %r14
  132ddb:	41 5f                	pop    %r15
  132ddd:	5d                   	pop    %rbp
  132dde:	c3                   	ret
  132ddf:	48 8b 07             	mov    (%rdi),%rax
  132de2:	48 83 c4 28          	add    $0x28,%rsp
  132de6:	5b                   	pop    %rbx
  132de7:	41 5c                	pop    %r12
  132de9:	41 5d                	pop    %r13
  132deb:	41 5e                	pop    %r14
  132ded:	41 5f                	pop    %r15
  132def:	5d                   	pop    %rbp
  132df0:	c3                   	ret
  132df1:	48 8d 3d 50 0d 12 00 	lea    0x120d50(%rip),%rdi        # 253b48 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
  132df8:	e8 23 8c f3 ff       	call   6ba20 <RhpNewFast>
  132dfd:	48 8b d8             	mov    %rax,%rbx
  132e00:	48 8b fb             	mov    %rbx,%rdi
  132e03:	48 8d 35 ee 06 11 00 	lea    0x1106ee(%rip),%rsi        # 2434f8 <__Str_Playback_was_never_started__mi_A4B7AD70A4141AA14D48EC123D832D5C325A07EB90C0D33B7FB492BC5D1E26A5>
  132e0a:	e8 31 11 f6 ff       	call   93f40 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
  132e0f:	48 8b fb             	mov    %rbx,%rdi
  132e12:	e8 09 8f f3 ff       	call   6bd20 <RhpThrowEx>
  132e17:	cc                   	int3
  132e18:	48 8d 3d 29 0d 12 00 	lea    0x120d29(%rip),%rdi        # 253b48 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
  132e1f:	e8 fc 8b f3 ff       	call   6ba20 <RhpNewFast>
  132e24:	48 8b d8             	mov    %rax,%rbx
  132e27:	48 8b fb             	mov    %rbx,%rdi
  132e2a:	48 8d 35 ef 05 11 00 	lea    0x1105ef(%rip),%rsi        # 243420 <__Str_Playback_is_stopped__BEFEE01D2658356891B4B1A0CFEF9C031BEA0351DB9767A723E74228769FE021>
  132e31:	e8 0a 11 f6 ff       	call   93f40 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
  132e36:	48 8b fb             	mov    %rbx,%rdi
  132e39:	e8 e2 8e f3 ff       	call   6bd20 <RhpThrowEx>
  132e3e:	cc                   	int3
  132e3f:	48 8d 3d e2 ff 11 00 	lea    0x11ffe2(%rip),%rdi        # 252e28 <_ZTV46S_P_CoreLib_System_ArgumentOutOfRangeException>
  132e46:	e8 d5 8b f3 ff       	call   6ba20 <RhpNewFast>
  132e4b:	4c 8b e0             	mov    %rax,%r12
  132e4e:	49 8b fc             	mov    %r12,%rdi
  132e51:	48 8d 35 f0 99 11 00 	lea    0x1199f0(%rip),%rsi        # 24c848 <__Str_ticks>
  132e58:	48 8d 15 69 05 11 00 	lea    0x110569(%rip),%rdx        # 2433c8 <__Str_Playback_cycle_capacity_exceed_438134DB0460610D3D09165487578575BC58EF0A4041E10A1DCD99D64E2B2DA0>
  132e5f:	e8 cc db f5 ff       	call   90a30 <S_P_CoreLib_System_ArgumentOutOfRangeException___ctor_1>
  132e64:	49 8b fc             	mov    %r12,%rdi
  132e67:	e8 b4 8e f3 ff       	call   6bd20 <RhpThrowEx>
  132e6c:	cc                   	int3

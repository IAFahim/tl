
/tmp/tl-consumer-aot/ConsumerChecks:     file format elf64-x86-64


Disassembly of section __managedcode:

000000000012e380 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>>:
  12e380:	55                   	push   %rbp
  12e381:	41 57                	push   %r15
  12e383:	41 56                	push   %r14
  12e385:	53                   	push   %rbx
  12e386:	50                   	push   %rax
  12e387:	48 8d 6c 24 20       	lea    0x20(%rsp),%rbp
  12e38c:	0f b7 47 06          	movzwl 0x6(%rdi),%eax
  12e390:	a8 01                	test   $0x1,%al
  12e392:	0f 84 9c 02 00 00    	je     12e634 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x2b4>
  12e398:	a8 02                	test   $0x2,%al
  12e39a:	0f 85 bb 02 00 00    	jne    12e65b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x2db>
  12e3a0:	45 85 c0             	test   %r8d,%r8d
  12e3a3:	0f 84 7d 02 00 00    	je     12e626 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x2a6>
  12e3a9:	8b 1f                	mov    (%rdi),%ebx
  12e3ab:	44 0f b7 7f 04       	movzwl 0x4(%rdi),%r15d
  12e3b0:	8b fb                	mov    %ebx,%edi
  12e3b2:	48 69 ff b5 81 4e 1b 	imul   $0x1b4e81b5,%rdi,%rdi
  12e3b9:	48 c1 ef 26          	shr    $0x26,%rdi
  12e3bd:	69 c7 58 02 00 00    	imul   $0x258,%edi,%eax
  12e3c3:	44 8b f3             	mov    %ebx,%r14d
  12e3c6:	44 2b f0             	sub    %eax,%r14d
  12e3c9:	33 c0                	xor    %eax,%eax
  12e3cb:	41 3b c0             	cmp    %r8d,%eax
  12e3ce:	0f 8d 20 02 00 00    	jge    12e5f4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x274>
  12e3d4:	8b 34 81             	mov    (%rcx,%rax,4),%esi
  12e3d7:	44 8b ce             	mov    %esi,%r9d
  12e3da:	4d 69 c9 b5 81 4e 1b 	imul   $0x1b4e81b5,%r9,%r9
  12e3e1:	49 c1 e9 26          	shr    $0x26,%r9
  12e3e5:	45 69 d1 58 02 00 00 	imul   $0x258,%r9d,%r10d
  12e3ec:	44 8b de             	mov    %esi,%r11d
  12e3ef:	45 2b da             	sub    %r10d,%r11d
  12e3f2:	3b f3                	cmp    %ebx,%esi
  12e3f4:	73 0d                	jae    12e403 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x83>
  12e3f6:	45 3b de             	cmp    %r14d,%r11d
  12e3f9:	41 0f 92 c2          	setb   %r10b
  12e3fd:	45 0f b6 d2          	movzbl %r10b,%r10d
  12e401:	eb 06                	jmp    12e409 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x89>
  12e403:	45 8b d1             	mov    %r9d,%r10d
  12e406:	44 2b d7             	sub    %edi,%r10d
  12e409:	41 8b ff             	mov    %r15d,%edi
  12e40c:	f7 df                	neg    %edi
  12e40e:	81 c7 ff ff 00 00    	add    $0xffff,%edi
  12e414:	48 63 ff             	movslq %edi,%rdi
  12e417:	41 8b da             	mov    %r10d,%ebx
  12e41a:	48 3b fb             	cmp    %rbx,%rdi
  12e41d:	0f 8c 5f 02 00 00    	jl     12e682 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x302>
  12e423:	45 03 d7             	add    %r15d,%r10d
  12e426:	45 0f b7 fa          	movzwl %r10w,%r15d
  12e42a:	41 83 fb 2f          	cmp    $0x2f,%r11d
  12e42e:	0f 82 e0 00 00 00    	jb     12e514 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x194>
  12e434:	41 81 fb c8 00 00 00 	cmp    $0xc8,%r11d
  12e43b:	72 61                	jb     12e49e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x11e>
  12e43d:	41 81 fb 41 01 00 00 	cmp    $0x141,%r11d
  12e444:	72 43                	jb     12e489 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x109>
  12e446:	41 81 fb 03 02 00 00 	cmp    $0x203,%r11d
  12e44d:	72 15                	jb     12e464 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0xe4>
  12e44f:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e453:	f3 0f 58 05 95 ee 06 	addss  0x6ee95(%rip),%xmm0        # 19d2f0 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>>
  12e45a:	00 
  12e45b:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e45f:	e9 7d 01 00 00       	jmp    12e5e1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x261>
  12e464:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e468:	f3 0f 58 05 84 ee 06 	addss  0x6ee84(%rip),%xmm0        # 19d2f4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x4>
  12e46f:	00 
  12e470:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e474:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e478:	f3 0f 58 05 78 ee 06 	addss  0x6ee78(%rip),%xmm0        # 19d2f8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x8>
  12e47f:	00 
  12e480:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e484:	e9 36 01 00 00       	jmp    12e5bf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x23f>
  12e489:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e48d:	f3 0f 58 05 67 ee 06 	addss  0x6ee67(%rip),%xmm0        # 19d2fc <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0xc>
  12e494:	00 
  12e495:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e499:	e9 21 01 00 00       	jmp    12e5bf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x23f>
  12e49e:	41 83 fb 4c          	cmp    $0x4c,%r11d
  12e4a2:	72 4b                	jb     12e4ef <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x16f>
  12e4a4:	41 83 fb 7b          	cmp    $0x7b,%r11d
  12e4a8:	72 04                	jb     12e4ae <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x12e>
  12e4aa:	38 12                	cmp    %dl,(%rdx)
  12e4ac:	eb 51                	jmp    12e4ff <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x17f>
  12e4ae:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e4b2:	f3 0f 58 05 3e ee 06 	addss  0x6ee3e(%rip),%xmm0        # 19d2f8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x8>
  12e4b9:	00 
  12e4ba:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e4be:	41 8d 7b b4          	lea    -0x4c(%r11),%edi
  12e4c2:	0f 57 c0             	xorps  %xmm0,%xmm0
  12e4c5:	f3 48 0f 2a c7       	cvtsi2ss %rdi,%xmm0
  12e4ca:	f3 0f 5e 05 2e ee 06 	divss  0x6ee2e(%rip),%xmm0        # 19d300 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x10>
  12e4d1:	00 
  12e4d2:	f3 0f 59 05 2a ee 06 	mulss  0x6ee2a(%rip),%xmm0        # 19d304 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x14>
  12e4d9:	00 
  12e4da:	f3 0f 58 05 26 ee 06 	addss  0x6ee26(%rip),%xmm0        # 19d308 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x18>
  12e4e1:	00 
  12e4e2:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12e4e6:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e4ea:	e9 f2 00 00 00       	jmp    12e5e1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x261>
  12e4ef:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e4f3:	f3 0f 58 05 11 ee 06 	addss  0x6ee11(%rip),%xmm0        # 19d30c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x1c>
  12e4fa:	00 
  12e4fb:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e4ff:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e503:	f3 0f 58 05 f1 ed 06 	addss  0x6edf1(%rip),%xmm0        # 19d2fc <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0xc>
  12e50a:	00 
  12e50b:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e50f:	e9 cd 00 00 00       	jmp    12e5e1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x261>
  12e514:	41 83 fb 0b          	cmp    $0xb,%r11d
  12e518:	72 53                	jb     12e56d <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x1ed>
  12e51a:	41 83 fb 12          	cmp    $0x12,%r11d
  12e51e:	0f 82 bd 00 00 00    	jb     12e5e1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x261>
  12e524:	41 83 fb 1d          	cmp    $0x1d,%r11d
  12e528:	72 31                	jb     12e55b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x1db>
  12e52a:	41 8d 7b e3          	lea    -0x1d(%r11),%edi
  12e52e:	0f 57 c0             	xorps  %xmm0,%xmm0
  12e531:	f3 48 0f 2a c7       	cvtsi2ss %rdi,%xmm0
  12e536:	f3 0f 5e 05 d2 ed 06 	divss  0x6edd2(%rip),%xmm0        # 19d310 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x20>
  12e53d:	00 
  12e53e:	f3 0f 59 05 b2 ed 06 	mulss  0x6edb2(%rip),%xmm0        # 19d2f8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x8>
  12e545:	00 
  12e546:	f3 0f 58 05 be ed 06 	addss  0x6edbe(%rip),%xmm0        # 19d30c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x1c>
  12e54d:	00 
  12e54e:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12e552:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e556:	e9 86 00 00 00       	jmp    12e5e1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x261>
  12e55b:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e55f:	f3 0f 58 05 91 ed 06 	addss  0x6ed91(%rip),%xmm0        # 19d2f8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x8>
  12e566:	00 
  12e567:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e56b:	eb 52                	jmp    12e5bf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x23f>
  12e56d:	41 83 fb 03          	cmp    $0x3,%r11d
  12e571:	72 5e                	jb     12e5d1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x251>
  12e573:	41 83 fb 07          	cmp    $0x7,%r11d
  12e577:	72 12                	jb     12e58b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x20b>
  12e579:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e57d:	f3 0f 58 05 6b ed 06 	addss  0x6ed6b(%rip),%xmm0        # 19d2f0 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>>
  12e584:	00 
  12e585:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e589:	eb 34                	jmp    12e5bf <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x23f>
  12e58b:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e58f:	f3 0f 58 05 5d ed 06 	addss  0x6ed5d(%rip),%xmm0        # 19d2f4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x4>
  12e596:	00 
  12e597:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e59b:	41 8d 7b fd          	lea    -0x3(%r11),%edi
  12e59f:	0f 57 c0             	xorps  %xmm0,%xmm0
  12e5a2:	f3 48 0f 2a c7       	cvtsi2ss %rdi,%xmm0
  12e5a7:	f3 0f 5e 05 41 ed 06 	divss  0x6ed41(%rip),%xmm0        # 19d2f0 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>>
  12e5ae:	00 
  12e5af:	f3 0f 58 05 45 ed 06 	addss  0x6ed45(%rip),%xmm0        # 19d2fc <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0xc>
  12e5b6:	00 
  12e5b7:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12e5bb:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e5bf:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e5c3:	f3 0f 58 05 49 ed 06 	addss  0x6ed49(%rip),%xmm0        # 19d314 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x24>
  12e5ca:	00 
  12e5cb:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e5cf:	eb 10                	jmp    12e5e1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x261>
  12e5d1:	f3 0f 10 02          	movss  (%rdx),%xmm0
  12e5d5:	f3 0f 58 05 17 ed 06 	addss  0x6ed17(%rip),%xmm0        # 19d2f4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x4>
  12e5dc:	00 
  12e5dd:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12e5e1:	8b de                	mov    %esi,%ebx
  12e5e3:	45 8b f3             	mov    %r11d,%r14d
  12e5e6:	41 8b f9             	mov    %r9d,%edi
  12e5e9:	ff c0                	inc    %eax
  12e5eb:	41 3b c0             	cmp    %r8d,%eax
  12e5ee:	0f 8c e0 fd ff ff    	jl     12e3d4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_SumConsumer>+0x54>
  12e5f4:	b8 01 00 00 00       	mov    $0x1,%eax
  12e5f9:	bf 05 00 00 00       	mov    $0x5,%edi
  12e5fe:	41 81 fe 57 02 00 00 	cmp    $0x257,%r14d
  12e605:	0f 44 c7             	cmove  %edi,%eax
  12e608:	8b fb                	mov    %ebx,%edi
  12e60a:	41 8b cf             	mov    %r15d,%ecx
  12e60d:	48 c1 e1 20          	shl    $0x20,%rcx
  12e611:	48 0b f9             	or     %rcx,%rdi
  12e614:	48 c1 e0 30          	shl    $0x30,%rax
  12e618:	48 0b c7             	or     %rdi,%rax
  12e61b:	48 83 c4 08          	add    $0x8,%rsp
  12e61f:	5b                   	pop    %rbx
  12e620:	41 5e                	pop    %r14
  12e622:	41 5f                	pop    %r15
  12e624:	5d                   	pop    %rbp
  12e625:	c3                   	ret
  12e626:	48 8b 07             	mov    (%rdi),%rax
  12e629:	48 83 c4 08          	add    $0x8,%rsp
  12e62d:	5b                   	pop    %rbx
  12e62e:	41 5e                	pop    %r14
  12e630:	41 5f                	pop    %r15
  12e632:	5d                   	pop    %rbp
  12e633:	c3                   	ret
  12e634:	48 8d 3d 0d 55 12 00 	lea    0x12550d(%rip),%rdi        # 253b48 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
  12e63b:	e8 e0 d3 f3 ff       	call   6ba20 <RhpNewFast>
  12e640:	48 8b d8             	mov    %rax,%rbx
  12e643:	48 8b fb             	mov    %rbx,%rdi
  12e646:	48 8d 35 ab 4e 11 00 	lea    0x114eab(%rip),%rsi        # 2434f8 <__Str_Playback_was_never_started__mi_A4B7AD70A4141AA14D48EC123D832D5C325A07EB90C0D33B7FB492BC5D1E26A5>
  12e64d:	e8 ee 58 f6 ff       	call   93f40 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
  12e652:	48 8b fb             	mov    %rbx,%rdi
  12e655:	e8 c6 d6 f3 ff       	call   6bd20 <RhpThrowEx>
  12e65a:	cc                   	int3
  12e65b:	48 8d 3d e6 54 12 00 	lea    0x1254e6(%rip),%rdi        # 253b48 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
  12e662:	e8 b9 d3 f3 ff       	call   6ba20 <RhpNewFast>
  12e667:	48 8b d8             	mov    %rax,%rbx
  12e66a:	48 8b fb             	mov    %rbx,%rdi
  12e66d:	48 8d 35 ac 4d 11 00 	lea    0x114dac(%rip),%rsi        # 243420 <__Str_Playback_is_stopped__BEFEE01D2658356891B4B1A0CFEF9C031BEA0351DB9767A723E74228769FE021>
  12e674:	e8 c7 58 f6 ff       	call   93f40 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
  12e679:	48 8b fb             	mov    %rbx,%rdi
  12e67c:	e8 9f d6 f3 ff       	call   6bd20 <RhpThrowEx>
  12e681:	cc                   	int3
  12e682:	48 8d 3d 9f 47 12 00 	lea    0x12479f(%rip),%rdi        # 252e28 <_ZTV46S_P_CoreLib_System_ArgumentOutOfRangeException>
  12e689:	e8 92 d3 f3 ff       	call   6ba20 <RhpNewFast>
  12e68e:	4c 8b f0             	mov    %rax,%r14
  12e691:	49 8b fe             	mov    %r14,%rdi
  12e694:	48 8d 35 ad e1 11 00 	lea    0x11e1ad(%rip),%rsi        # 24c848 <__Str_ticks>
  12e69b:	48 8d 15 26 4d 11 00 	lea    0x114d26(%rip),%rdx        # 2433c8 <__Str_Playback_cycle_capacity_exceed_438134DB0460610D3D09165487578575BC58EF0A4041E10A1DCD99D64E2B2DA0>
  12e6a2:	e8 89 23 f6 ff       	call   90a30 <S_P_CoreLib_System_ArgumentOutOfRangeException___ctor_1>
  12e6a7:	49 8b fe             	mov    %r14,%rdi
  12e6aa:	e8 71 d6 f3 ff       	call   6bd20 <RhpThrowEx>
  12e6af:	cc                   	int3

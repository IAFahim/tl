
tests/Tl.Alpha/bin/Release/net10.0/linux-x64/publish/Tl.Alpha:	file format elf64-x86-64

Disassembly of section __managedcode:

00000000000715a0 <__managedcode>:
   d5460: 55                           	pushq	%rbp
   d5461: 41 57                        	pushq	%r15
   d5463: 41 56                        	pushq	%r14
   d5465: 41 55                        	pushq	%r13
   d5467: 41 54                        	pushq	%r12
   d5469: 53                           	pushq	%rbx
   d546a: 48 83 ec 38                  	subq	$0x38, %rsp
   d546e: 48 8d 6c 24 60               	leaq	0x60(%rsp), %rbp
   d5473: 33 c0                        	xorl	%eax, %eax
   d5475: 48 89 45 a8                  	movq	%rax, -0x58(%rbp)
   d5479: 45 0f 57 c0                  	xorps	%xmm8, %xmm8
   d547d: 44 0f 29 45 b0               	movaps	%xmm8, -0x50(%rbp)
   d5482: 44 0f 29 45 c0               	movaps	%xmm8, -0x40(%rbp)
   d5487: 48 89 45 d0                  	movq	%rax, -0x30(%rbp)
   d548b: 48 8b de                     	movq	%rsi, %rbx
   d548e: 44 8b f2                     	movl	%edx, %r14d
   d5491: 4c 8b f9                     	movq	%rcx, %r15
   d5494: 0f b7 73 0c                  	movzwl	0xc(%rbx), %esi
   d5498: 0f b6 43 0e                  	movzbl	0xe(%rbx), %eax
   d549c: 44 0f b7 ef                  	movzwl	%di, %r13d
   d54a0: 41 3b f5                     	cmpl	%r13d, %esi
   d54a3: 0f 85 1b 01 00 00            	jne	0xd55c4 <__managedcode+0x64024>
   d54a9: 83 e0 03                     	andl	$0x3, %eax
   d54ac: 83 f8 01                     	cmpl	$0x1, %eax
   d54af: 0f 85 0f 01 00 00            	jne	0xd55c4 <__managedcode+0x64024>
   d54b5: 41 8b fd                     	movl	%r13d, %edi
   d54b8: 48 8d 75 d0                  	leaq	-0x30(%rbp), %rsi
   d54bc: e8 8f 08 00 00               	callq	0xd5d50 <__managedcode+0x647b0>
   d54c1: 85 c0                        	testl	%eax, %eax
   d54c3: 0f 84 fb 00 00 00            	je	0xd55c4 <__managedcode+0x64024>
   d54c9: 4c 8d 25 d0 77 0e 00         	leaq	0xe77d0(%rip), %r12     # 0x1bcca0
   d54d0: 49 83 7c 24 f8 00            	cmpq	$0x0, -0x8(%r12)
   d54d6: 0f 85 f9 00 00 00            	jne	0xd55d5 <__managedcode+0x64035>
   d54dc: 0f b6 45 d0                  	movzbl	-0x30(%rbp), %eax
   d54e0: 3d 00 01 00 00               	cmpl	$0x100, %eax            # imm = 0x100
   d54e5: 0f 83 08 01 00 00            	jae	0xd55f3 <__managedcode+0x64053>
   d54eb: 66 41 83 3c 44 01            	cmpw	$0x1, (%r12,%rax,2)
   d54f1: 0f 85 cd 00 00 00            	jne	0xd55c4 <__managedcode+0x64024>
   d54f7: 0f b6 45 d1                  	movzbl	-0x2f(%rbp), %eax
   d54fb: ff c8                        	decl	%eax
   d54fd: 83 f8 03                     	cmpl	$0x3, %eax
   d5500: 0f 87 be 00 00 00            	ja	0xd55c4 <__managedcode+0x64024>
   d5506: 8b c0                        	movl	%eax, %eax
   d5508: 48 8d 0d 09 ec 05 00         	leaq	0x5ec09(%rip), %rcx     # 0x134118
   d550f: 8b 0c 81                     	movl	(%rcx,%rax,4), %ecx
   d5512: 48 8d 15 7b ff ff ff         	leaq	-0x85(%rip), %rdx       # 0xd5494 <__managedcode+0x63ef4>
   d5519: 48 03 ca                     	addq	%rdx, %rcx
   d551c: ff e1                        	jmpq	*%rcx
   d551e: 4c 8d 3d 9b 7b 0e 00         	leaq	0xe7b9b(%rip), %r15     # 0x1bd0c0
   d5525: 49 83 7f f8 00               	cmpq	$0x0, -0x8(%r15)
   d552a: 0f 85 b9 00 00 00            	jne	0xd55e9 <__managedcode+0x64049>
   d5530: 41 0f b7 3f                  	movzwl	(%r15), %edi
   d5534: 41 3b fd                     	cmpl	%r13d, %edi
   d5537: 0f 85 87 00 00 00            	jne	0xd55c4 <__managedcode+0x64024>
   d553d: c6 45 a8 00                  	movb	$0x0, -0x58(%rbp)
   d5541: 41 8b fd                     	movl	%r13d, %edi
   d5544: 48 8d 4d a8                  	leaq	-0x58(%rbp), %rcx
   d5548: 48 8b f3                     	movq	%rbx, %rsi
   d554b: 41 8b d6                     	movl	%r14d, %edx
   d554e: e8 1d ed ff ff               	callq	0xd4270 <__managedcode+0x62cd0>
   d5553: eb 5d                        	jmp	0xd55b2 <__managedcode+0x64012>
   d5555: 4c 8d 25 74 7b 0e 00         	leaq	0xe7b74(%rip), %r12     # 0x1bd0d0
   d555c: 49 83 7c 24 f8 00            	cmpq	$0x0, -0x8(%r12)
   d5562: 75 7b                        	jne	0xd55df <__managedcode+0x6403f>
   d5564: 41 0f b7 3c 24               	movzwl	(%r12), %edi
   d5569: 41 3b fd                     	cmpl	%r13d, %edi
   d556c: 75 56                        	jne	0xd55c4 <__managedcode+0x64024>
   d556e: 49 8b 3f                     	movq	(%r15), %rdi
   d5571: 49 8b 4f 10                  	movq	0x10(%r15), %rcx
   d5575: 49 8b 77 28                  	movq	0x28(%r15), %rsi
   d5579: 49 8b 57 30                  	movq	0x30(%r15), %rdx
   d557d: 48 89 7d b0                  	movq	%rdi, -0x50(%rbp)
   d5581: 48 89 4d b8                  	movq	%rcx, -0x48(%rbp)
   d5585: 48 89 75 c0                  	movq	%rsi, -0x40(%rbp)
   d5589: 48 89 55 c8                  	movq	%rdx, -0x38(%rbp)
   d558d: 41 8b fd                     	movl	%r13d, %edi
   d5590: 48 8d 4d b0                  	leaq	-0x50(%rbp), %rcx
   d5594: 48 8b f3                     	movq	%rbx, %rsi
   d5597: 41 8b d6                     	movl	%r14d, %edx
   d559a: e8 c1 f2 ff ff               	callq	0xd4860 <__managedcode+0x632c0>
   d559f: eb 11                        	jmp	0xd55b2 <__managedcode+0x64012>
   d55a1: 41 8b fd                     	movl	%r13d, %edi
   d55a4: 48 8b f3                     	movq	%rbx, %rsi
   d55a7: 41 8b d6                     	movl	%r14d, %edx
   d55aa: 49 8b cf                     	movq	%r15, %rcx
   d55ad: e8 ee d0 ff ff               	callq	0xd26a0 <__managedcode+0x61100>
   d55b2: 0f b6 c0                     	movzbl	%al, %eax
   d55b5: 48 83 c4 38                  	addq	$0x38, %rsp
   d55b9: 5b                           	popq	%rbx
   d55ba: 41 5c                        	popq	%r12
   d55bc: 41 5d                        	popq	%r13
   d55be: 41 5e                        	popq	%r14
   d55c0: 41 5f                        	popq	%r15
   d55c2: 5d                           	popq	%rbp
   d55c3: c3                           	retq
   d55c4: 33 c0                        	xorl	%eax, %eax
   d55c6: 48 83 c4 38                  	addq	$0x38, %rsp
   d55ca: 5b                           	popq	%rbx
   d55cb: 41 5c                        	popq	%r12
   d55cd: 41 5d                        	popq	%r13
   d55cf: 41 5e                        	popq	%r14
   d55d1: 41 5f                        	popq	%r15
   d55d3: 5d                           	popq	%rbp
   d55d4: c3                           	retq
   d55d5: e8 52 1a f3 ff               	callq	0x702c <.text+0x68c>
   d55da: e9 fd fe ff ff               	jmp	0xd54dc <__managedcode+0x63f3c>
   d55df: e8 88 1a f3 ff               	callq	0x706c <.text+0x6cc>
   d55e4: e9 7b ff ff ff               	jmp	0xd5564 <__managedcode+0x63fc4>
   d55e9: e8 6e 1a f3 ff               	callq	0x705c <.text+0x6bc>
   d55ee: e9 3d ff ff ff               	jmp	0xd5530 <__managedcode+0x63f90>
   d55f3: e8 a8 39 fe ff               	callq	0xb8fa0 <__managedcode+0x47a00>
   d55f8: cc                           	int3
   d55f9: 90                           	nop
   d55fa: 90                           	nop
   d55fb: 90                           	nop
   d55fc: 90                           	nop
   d55fd: 90                           	nop
   d55fe: 90                           	nop
   d55ff: 90                           	nop
   d5600: 53                           	pushq	%rbx
   d5601: 48 81 ec 00 02 00 00         	subq	$0x200, %rsp            # imm = 0x200
   d5608: 48 8d 1d 91 76 0e 00         	leaq	0xe7691(%rip), %rbx     # 0x1bcca0
   d560f: 48 83 7b f8 00               	cmpq	$0x0, -0x8(%rbx)
   d5614: 75 24                        	jne	0xd563a <__managedcode+0x6409a>
   d5616: 48 8d 3c 24                  	leaq	(%rsp), %rdi
   d561a: e8 a1 fd ff ff               	callq	0xd53c0 <__managedcode+0x63e20>
   d561f: 48 8b fb                     	movq	%rbx, %rdi
   d5622: 48 8d 34 24                  	leaq	(%rsp), %rsi
   d5626: ba 00 02 00 00               	movl	$0x200, %edx            # imm = 0x200
   d562b: e8 b0 07 fb ff               	callq	0x85de0 <__managedcode+0x14840>
   d5630: 90                           	nop
   d5631: 48 81 c4 00 02 00 00         	addq	$0x200, %rsp            # imm = 0x200
   d5638: 5b                           	popq	%rbx
   d5639: c3                           	retq
   d563a: e8 ed 19 f3 ff               	callq	0x702c <.text+0x68c>
   d563f: eb d5                        	jmp	0xd5616 <__managedcode+0x64076>
   d5641: 90                           	nop
   d5642: 90                           	nop
   d5643: 90                           	nop
   d5644: 90                           	nop
   d5645: 90                           	nop
   d5646: 90                           	nop
   d5647: 90                           	nop
   d5648: 90                           	nop
   d5649: 90                           	nop
   d564a: 90                           	nop
   d564b: 90                           	nop
   d564c: 90                           	nop
   d564d: 90                           	nop
   d564e: 90                           	nop
   d564f: 90                           	nop
   d5650: 41 57                        	pushq	%r15
   d5652: 53                           	pushq	%rbx
   d5653: 48 81 ec 08 02 00 00         	subq	$0x208, %rsp            # imm = 0x208
   d565a: 33 c0                        	xorl	%eax, %eax
   d565c: 48 89 44 24 08               	movq	%rax, 0x8(%rsp)
   d5661: 45 0f 57 c0                  	xorps	%xmm8, %xmm8
   d5665: 44 0f 29 44 24 10            	movaps	%xmm8, 0x10(%rsp)
   d566b: 48 b8 20 fe ff ff ff ff ff ff	movabsq	$-0x1e0, %rax           # imm = 0xFE20
   d5675: 44 0f 29 84 04 00 02 00 00   	movaps	%xmm8, 0x200(%rsp,%rax)
   d567e: 44 0f 29 84 04 10 02 00 00   	movaps	%xmm8, 0x210(%rsp,%rax)
   d5687: 44 0f 29 84 04 20 02 00 00   	movaps	%xmm8, 0x220(%rsp,%rax)
   d5690: 48 83 c0 30                  	addq	$0x30, %rax
   d5694: 75 df                        	jne	0xd5675 <__managedcode+0x640d5>
   d5696: 48 89 84 24 00 02 00 00      	movq	%rax, 0x200(%rsp)
   d569e: 48 8b df                     	movq	%rdi, %rbx
   d56a1: 4c 8d 3d e8 75 0e 00         	leaq	0xe75e8(%rip), %r15     # 0x1bcc90
   d56a8: 49 83 7f f8 00               	cmpq	$0x0, -0x8(%r15)
   d56ad: 75 2f                        	jne	0xd56de <__managedcode+0x6413e>
   d56af: 41 0f b6 3f                  	movzbl	(%r15), %edi
   d56b3: 48 8d 74 24 08               	leaq	0x8(%rsp), %rsi
   d56b8: 66 c7 04 7e 01 00            	movw	$0x1, (%rsi,%rdi,2)
   d56be: 48 8b fb                     	movq	%rbx, %rdi
   d56c1: 48 8d 74 24 08               	leaq	0x8(%rsp), %rsi
   d56c6: ba 00 02 00 00               	movl	$0x200, %edx            # imm = 0x200
   d56cb: e8 10 07 fb ff               	callq	0x85de0 <__managedcode+0x14840>
   d56d0: 48 8b c3                     	movq	%rbx, %rax
   d56d3: 48 81 c4 08 02 00 00         	addq	$0x208, %rsp            # imm = 0x208
   d56da: 5b                           	popq	%rbx
   d56db: 41 5f                        	popq	%r15
   d56dd: c3                           	retq
   d56de: e8 39 19 f3 ff               	callq	0x701c <.text+0x67c>
   d56e3: eb ca                        	jmp	0xd56af <__managedcode+0x6410f>
   d56e5: 90                           	nop
   d56e6: 90                           	nop
   d56e7: 90                           	nop
   d56e8: 90                           	nop
   d56e9: 90                           	nop
   d56ea: 90                           	nop
   d56eb: 90                           	nop
   d56ec: 90                           	nop
   d56ed: 90                           	nop
   d56ee: 90                           	nop
   d56ef: 90                           	nop
   d56f0: 55                           	pushq	%rbp
   d56f1: 41 57                        	pushq	%r15
   d56f3: 41 56                        	pushq	%r14
   d56f5: 41 55                        	pushq	%r13
   d56f7: 41 54                        	pushq	%r12
   d56f9: 53                           	pushq	%rbx
   d56fa: 48 83 ec 18                  	subq	$0x18, %rsp
   d56fe: 48 8d 6c 24 40               	leaq	0x40(%rsp), %rbp
   d5703: 33 c0                        	xorl	%eax, %eax
   d5705: 48 89 45 d0                  	movq	%rax, -0x30(%rbp)
   d5709: 48 89 45 c8                  	movq	%rax, -0x38(%rbp)
   d570d: 48 8b de                     	movq	%rsi, %rbx
   d5710: 44 8b fa                     	movl	%edx, %r15d
   d5713: 4c 8b f1                     	movq	%rcx, %r14
   d5716: 0f b7 73 0c                  	movzwl	0xc(%rbx), %esi
   d571a: 0f b6 43 0e                  	movzbl	0xe(%rbx), %eax
   d571e: 44 0f b7 ef                  	movzwl	%di, %r13d
   d5722: 41 3b f5                     	cmpl	%r13d, %esi
   d5725: 0f 85 af 00 00 00            	jne	0xd57da <__managedcode+0x6423a>
   d572b: 83 e0 03                     	andl	$0x3, %eax
   d572e: 83 f8 01                     	cmpl	$0x1, %eax
   d5731: 0f 85 a3 00 00 00            	jne	0xd57da <__managedcode+0x6423a>
   d5737: 41 8b fd                     	movl	%r13d, %edi
   d573a: 48 8d 75 d0                  	leaq	-0x30(%rbp), %rsi
   d573e: e8 0d 06 00 00               	callq	0xd5d50 <__managedcode+0x647b0>
   d5743: 85 c0                        	testl	%eax, %eax
   d5745: 0f 84 8f 00 00 00            	je	0xd57da <__managedcode+0x6423a>
   d574b: 4c 8d 25 56 77 0e 00         	leaq	0xe7756(%rip), %r12     # 0x1bcea8
   d5752: 49 83 7c 24 f8 00            	cmpq	$0x0, -0x8(%r12)
   d5758: 0f 85 8d 00 00 00            	jne	0xd57eb <__managedcode+0x6424b>
   d575e: 40 0f b6 7d d0               	movzbl	-0x30(%rbp), %edi
   d5763: 81 ff 00 01 00 00            	cmpl	$0x100, %edi            # imm = 0x100
   d5769: 0f 83 8d 00 00 00            	jae	0xd57fc <__managedcode+0x6425c>
   d576f: 66 41 83 3c 7c 01            	cmpw	$0x1, (%r12,%rdi,2)
   d5775: 75 63                        	jne	0xd57da <__managedcode+0x6423a>
   d5777: 44 0f b6 65 d1               	movzbl	-0x2f(%rbp), %r12d
   d577c: 41 83 fc 02                  	cmpl	$0x2, %r12d
   d5780: 75 13                        	jne	0xd5795 <__managedcode+0x641f5>
   d5782: 41 8b fd                     	movl	%r13d, %edi
   d5785: 48 8b f3                     	movq	%rbx, %rsi
   d5788: 41 8b d7                     	movl	%r15d, %edx
   d578b: 49 8b ce                     	movq	%r14, %rcx
   d578e: e8 cd f0 ff ff               	callq	0xd4860 <__managedcode+0x632c0>
   d5793: eb 33                        	jmp	0xd57c8 <__managedcode+0x64228>
   d5795: 41 83 fc 04                  	cmpl	$0x4, %r12d
   d5799: 75 3f                        	jne	0xd57da <__managedcode+0x6423a>
   d579b: 4c 8d 35 1e 79 0e 00         	leaq	0xe791e(%rip), %r14     # 0x1bd0c0
   d57a2: 49 83 7e f8 00               	cmpq	$0x0, -0x8(%r14)
   d57a7: 75 4c                        	jne	0xd57f5 <__managedcode+0x64255>
   d57a9: 41 0f b7 3e                  	movzwl	(%r14), %edi
   d57ad: 41 3b fd                     	cmpl	%r13d, %edi
   d57b0: 75 28                        	jne	0xd57da <__managedcode+0x6423a>
   d57b2: c6 45 c8 00                  	movb	$0x0, -0x38(%rbp)
   d57b6: 41 8b fd                     	movl	%r13d, %edi
   d57b9: 48 8d 4d c8                  	leaq	-0x38(%rbp), %rcx
   d57bd: 48 8b f3                     	movq	%rbx, %rsi
   d57c0: 41 8b d7                     	movl	%r15d, %edx
   d57c3: e8 a8 ea ff ff               	callq	0xd4270 <__managedcode+0x62cd0>
   d57c8: 0f b6 c0                     	movzbl	%al, %eax
   d57cb: 48 83 c4 18                  	addq	$0x18, %rsp
   d57cf: 5b                           	popq	%rbx
   d57d0: 41 5c                        	popq	%r12
   d57d2: 41 5d                        	popq	%r13
   d57d4: 41 5e                        	popq	%r14
   d57d6: 41 5f                        	popq	%r15
   d57d8: 5d                           	popq	%rbp
   d57d9: c3                           	retq
   d57da: 33 c0                        	xorl	%eax, %eax
   d57dc: 48 83 c4 18                  	addq	$0x18, %rsp
   d57e0: 5b                           	popq	%rbx
   d57e1: 41 5c                        	popq	%r12
   d57e3: 41 5d                        	popq	%r13
   d57e5: 41 5e                        	popq	%r14
   d57e7: 41 5f                        	popq	%r15
   d57e9: 5d                           	popq	%rbp
   d57ea: c3                           	retq
   d57eb: e8 4c 18 f3 ff               	callq	0x703c <.text+0x69c>
   d57f0: e9 69 ff ff ff               	jmp	0xd575e <__managedcode+0x641be>
   d57f5: e8 62 18 f3 ff               	callq	0x705c <.text+0x6bc>
   d57fa: eb ad                        	jmp	0xd57a9 <__managedcode+0x64209>
   d57fc: e8 9f 37 fe ff               	callq	0xb8fa0 <__managedcode+0x47a00>
   d5801: cc                           	int3
   d5802: 90                           	nop
   d5803: 90                           	nop
   d5804: 90                           	nop
   d5805: 90                           	nop
   d5806: 90                           	nop
   d5807: 90                           	nop
   d5808: 90                           	nop
   d5809: 90                           	nop
   d580a: 90                           	nop
   d580b: 90                           	nop
   d580c: 90                           	nop
   d580d: 90                           	nop
   d580e: 90                           	nop
   d580f: 90                           	nop



tests/Tl.Alpha/bin/Release/net10.0/linux-x64/publish/Tl.Alpha:	file format elf64-x86-64

Disassembly of section __managedcode:

0000000000071580 <__managedcode>:
   d53a0: 55                           	pushq	%rbp
   d53a1: 41 57                        	pushq	%r15
   d53a3: 41 56                        	pushq	%r14
   d53a5: 41 55                        	pushq	%r13
   d53a7: 41 54                        	pushq	%r12
   d53a9: 53                           	pushq	%rbx
   d53aa: 48 83 ec 38                  	subq	$0x38, %rsp
   d53ae: 48 8d 6c 24 60               	leaq	0x60(%rsp), %rbp
   d53b3: 33 c0                        	xorl	%eax, %eax
   d53b5: 48 89 45 a8                  	movq	%rax, -0x58(%rbp)
   d53b9: 45 0f 57 c0                  	xorps	%xmm8, %xmm8
   d53bd: 44 0f 29 45 b0               	movaps	%xmm8, -0x50(%rbp)
   d53c2: 44 0f 29 45 c0               	movaps	%xmm8, -0x40(%rbp)
   d53c7: 48 89 45 d0                  	movq	%rax, -0x30(%rbp)
   d53cb: 48 8b de                     	movq	%rsi, %rbx
   d53ce: 44 8b f2                     	movl	%edx, %r14d
   d53d1: 4c 8b f9                     	movq	%rcx, %r15
   d53d4: 0f b7 73 0c                  	movzwl	0xc(%rbx), %esi
   d53d8: 0f b6 43 0e                  	movzbl	0xe(%rbx), %eax
   d53dc: 44 0f b7 ef                  	movzwl	%di, %r13d
   d53e0: 41 3b f5                     	cmpl	%r13d, %esi
   d53e3: 0f 85 18 01 00 00            	jne	0xd5501 <__managedcode+0x63f81>
   d53e9: 83 e0 03                     	andl	$0x3, %eax
   d53ec: 83 f8 01                     	cmpl	$0x1, %eax
   d53ef: 0f 85 0c 01 00 00            	jne	0xd5501 <__managedcode+0x63f81>
   d53f5: 41 8b fd                     	movl	%r13d, %edi
   d53f8: 48 8d 75 d0                  	leaq	-0x30(%rbp), %rsi
   d53fc: e8 2f 07 00 00               	callq	0xd5b30 <__managedcode+0x645b0>
   d5401: 85 c0                        	testl	%eax, %eax
   d5403: 0f 84 f8 00 00 00            	je	0xd5501 <__managedcode+0x63f81>
   d5409: 44 0f b6 65 d0               	movzbl	-0x30(%rbp), %r12d
   d540e: 48 8d 05 7b 78 0e 00         	leaq	0xe787b(%rip), %rax     # 0x1bcc90
   d5415: 48 89 45 a0                  	movq	%rax, -0x60(%rbp)
   d5419: 48 83 78 f8 00               	cmpq	$0x0, -0x8(%rax)
   d541e: 0f 85 ee 00 00 00            	jne	0xd5512 <__managedcode+0x63f92>
   d5424: 48 8b 45 a0                  	movq	-0x60(%rbp), %rax
   d5428: 0f b6 00                     	movzbl	(%rax), %eax
   d542b: 44 3b e0                     	cmpl	%eax, %r12d
   d542e: 0f 85 cd 00 00 00            	jne	0xd5501 <__managedcode+0x63f81>
   d5434: 0f b6 45 d1                  	movzbl	-0x2f(%rbp), %eax
   d5438: ff c8                        	decl	%eax
   d543a: 83 f8 03                     	cmpl	$0x3, %eax
   d543d: 0f 87 be 00 00 00            	ja	0xd5501 <__managedcode+0x63f81>
   d5443: 8b c0                        	movl	%eax, %eax
   d5445: 48 8d 0d 7c ec 05 00         	leaq	0x5ec7c(%rip), %rcx     # 0x1340c8
   d544c: 8b 0c 81                     	movl	(%rcx,%rax,4), %ecx
   d544f: 48 8d 15 7e ff ff ff         	leaq	-0x82(%rip), %rdx       # 0xd53d4 <__managedcode+0x63e54>
   d5456: 48 03 ca                     	addq	%rdx, %rcx
   d5459: ff e1                        	jmpq	*%rcx
   d545b: 4c 8d 3d 4e 78 0e 00         	leaq	0xe784e(%rip), %r15     # 0x1bccb0
   d5462: 49 83 7f f8 00               	cmpq	$0x0, -0x8(%r15)
   d5467: 0f 85 b9 00 00 00            	jne	0xd5526 <__managedcode+0x63fa6>
   d546d: 41 0f b7 3f                  	movzwl	(%r15), %edi
   d5471: 41 3b fd                     	cmpl	%r13d, %edi
   d5474: 0f 85 87 00 00 00            	jne	0xd5501 <__managedcode+0x63f81>
   d547a: c6 45 a8 00                  	movb	$0x0, -0x58(%rbp)
   d547e: 41 8b fd                     	movl	%r13d, %edi
   d5481: 48 8d 4d a8                  	leaq	-0x58(%rbp), %rcx
   d5485: 48 8b f3                     	movq	%rbx, %rsi
   d5488: 41 8b d6                     	movl	%r14d, %edx
   d548b: e8 c0 ed ff ff               	callq	0xd4250 <__managedcode+0x62cd0>
   d5490: eb 5d                        	jmp	0xd54ef <__managedcode+0x63f6f>
   d5492: 4c 8d 25 27 78 0e 00         	leaq	0xe7827(%rip), %r12     # 0x1bccc0
   d5499: 49 83 7c 24 f8 00            	cmpq	$0x0, -0x8(%r12)
   d549f: 75 7b                        	jne	0xd551c <__managedcode+0x63f9c>
   d54a1: 41 0f b7 3c 24               	movzwl	(%r12), %edi
   d54a6: 41 3b fd                     	cmpl	%r13d, %edi
   d54a9: 75 56                        	jne	0xd5501 <__managedcode+0x63f81>
   d54ab: 49 8b 3f                     	movq	(%r15), %rdi
   d54ae: 49 8b 4f 10                  	movq	0x10(%r15), %rcx
   d54b2: 49 8b 77 28                  	movq	0x28(%r15), %rsi
   d54b6: 49 8b 57 30                  	movq	0x30(%r15), %rdx
   d54ba: 48 89 7d b0                  	movq	%rdi, -0x50(%rbp)
   d54be: 48 89 4d b8                  	movq	%rcx, -0x48(%rbp)
   d54c2: 48 89 75 c0                  	movq	%rsi, -0x40(%rbp)
   d54c6: 48 89 55 c8                  	movq	%rdx, -0x38(%rbp)
   d54ca: 41 8b fd                     	movl	%r13d, %edi
   d54cd: 48 8d 4d b0                  	leaq	-0x50(%rbp), %rcx
   d54d1: 48 8b f3                     	movq	%rbx, %rsi
   d54d4: 41 8b d6                     	movl	%r14d, %edx
   d54d7: e8 64 f3 ff ff               	callq	0xd4840 <__managedcode+0x632c0>
   d54dc: eb 11                        	jmp	0xd54ef <__managedcode+0x63f6f>
   d54de: 41 8b fd                     	movl	%r13d, %edi
   d54e1: 48 8b f3                     	movq	%rbx, %rsi
   d54e4: 41 8b d6                     	movl	%r14d, %edx
   d54e7: 49 8b cf                     	movq	%r15, %rcx
   d54ea: e8 91 d1 ff ff               	callq	0xd2680 <__managedcode+0x61100>
   d54ef: 0f b6 c0                     	movzbl	%al, %eax
   d54f2: 48 83 c4 38                  	addq	$0x38, %rsp
   d54f6: 5b                           	popq	%rbx
   d54f7: 41 5c                        	popq	%r12
   d54f9: 41 5d                        	popq	%r13
   d54fb: 41 5e                        	popq	%r14
   d54fd: 41 5f                        	popq	%r15
   d54ff: 5d                           	popq	%rbp
   d5500: c3                           	retq
   d5501: 33 c0                        	xorl	%eax, %eax
   d5503: 48 83 c4 38                  	addq	$0x38, %rsp
   d5507: 5b                           	popq	%rbx
   d5508: 41 5c                        	popq	%r12
   d550a: 41 5d                        	popq	%r13
   d550c: 41 5e                        	popq	%r14
   d550e: 41 5f                        	popq	%r15
   d5510: 5d                           	popq	%rbp
   d5511: c3                           	retq
   d5512: e8 05 1b f3 ff               	callq	0x701c <.text+0x67c>
   d5517: e9 08 ff ff ff               	jmp	0xd5424 <__managedcode+0x63ea4>
   d551c: e8 2b 1b f3 ff               	callq	0x704c <.text+0x6ac>
   d5521: e9 7b ff ff ff               	jmp	0xd54a1 <__managedcode+0x63f21>
   d5526: e8 11 1b f3 ff               	callq	0x703c <.text+0x69c>
   d552b: e9 3d ff ff ff               	jmp	0xd546d <__managedcode+0x63eed>
   d5530: 55                           	pushq	%rbp
   d5531: 41 57                        	pushq	%r15
   d5533: 41 56                        	pushq	%r14
   d5535: 41 55                        	pushq	%r13
   d5537: 41 54                        	pushq	%r12
   d5539: 53                           	pushq	%rbx
   d553a: 48 83 ec 18                  	subq	$0x18, %rsp
   d553e: 48 8d 6c 24 40               	leaq	0x40(%rsp), %rbp
   d5543: 33 c0                        	xorl	%eax, %eax
   d5545: 48 89 45 d0                  	movq	%rax, -0x30(%rbp)
   d5549: 48 89 45 c8                  	movq	%rax, -0x38(%rbp)
   d554d: 48 8b de                     	movq	%rsi, %rbx
   d5550: 44 8b fa                     	movl	%edx, %r15d
   d5553: 4c 8b f1                     	movq	%rcx, %r14
   d5556: 0f b7 73 0c                  	movzwl	0xc(%rbx), %esi
   d555a: 0f b6 43 0e                  	movzbl	0xe(%rbx), %eax
   d555e: 44 0f b7 ef                  	movzwl	%di, %r13d
   d5562: 41 3b f5                     	cmpl	%r13d, %esi
   d5565: 0f 85 ab 00 00 00            	jne	0xd5616 <__managedcode+0x64096>
   d556b: 83 e0 03                     	andl	$0x3, %eax
   d556e: 83 f8 01                     	cmpl	$0x1, %eax
   d5571: 0f 85 9f 00 00 00            	jne	0xd5616 <__managedcode+0x64096>
   d5577: 41 8b fd                     	movl	%r13d, %edi
   d557a: 48 8d 75 d0                  	leaq	-0x30(%rbp), %rsi
   d557e: e8 ad 05 00 00               	callq	0xd5b30 <__managedcode+0x645b0>
   d5583: 85 c0                        	testl	%eax, %eax
   d5585: 0f 84 8b 00 00 00            	je	0xd5616 <__managedcode+0x64096>
   d558b: 44 0f b6 65 d0               	movzbl	-0x30(%rbp), %r12d
   d5590: 48 8d 05 f9 76 0e 00         	leaq	0xe76f9(%rip), %rax     # 0x1bcc90
   d5597: 48 89 45 c0                  	movq	%rax, -0x40(%rbp)
   d559b: 48 83 78 f8 00               	cmpq	$0x0, -0x8(%rax)
   d55a0: 0f 85 81 00 00 00            	jne	0xd5627 <__managedcode+0x640a7>
   d55a6: 48 8b 45 c0                  	movq	-0x40(%rbp), %rax
   d55aa: 40 0f b6 38                  	movzbl	(%rax), %edi
   d55ae: 44 3b e7                     	cmpl	%edi, %r12d
   d55b1: 75 63                        	jne	0xd5616 <__managedcode+0x64096>
   d55b3: 44 0f b6 65 d1               	movzbl	-0x2f(%rbp), %r12d
   d55b8: 41 83 fc 02                  	cmpl	$0x2, %r12d
   d55bc: 75 13                        	jne	0xd55d1 <__managedcode+0x64051>
   d55be: 41 8b fd                     	movl	%r13d, %edi
   d55c1: 48 8b f3                     	movq	%rbx, %rsi
   d55c4: 41 8b d7                     	movl	%r15d, %edx
   d55c7: 49 8b ce                     	movq	%r14, %rcx
   d55ca: e8 71 f2 ff ff               	callq	0xd4840 <__managedcode+0x632c0>
   d55cf: eb 33                        	jmp	0xd5604 <__managedcode+0x64084>
   d55d1: 41 83 fc 04                  	cmpl	$0x4, %r12d
   d55d5: 75 3f                        	jne	0xd5616 <__managedcode+0x64096>
   d55d7: 4c 8d 35 d2 76 0e 00         	leaq	0xe76d2(%rip), %r14     # 0x1bccb0
   d55de: 49 83 7e f8 00               	cmpq	$0x0, -0x8(%r14)
   d55e3: 75 4c                        	jne	0xd5631 <__managedcode+0x640b1>
   d55e5: 41 0f b7 3e                  	movzwl	(%r14), %edi
   d55e9: 41 3b fd                     	cmpl	%r13d, %edi
   d55ec: 75 28                        	jne	0xd5616 <__managedcode+0x64096>
   d55ee: c6 45 c8 00                  	movb	$0x0, -0x38(%rbp)
   d55f2: 41 8b fd                     	movl	%r13d, %edi
   d55f5: 48 8d 4d c8                  	leaq	-0x38(%rbp), %rcx
   d55f9: 48 8b f3                     	movq	%rbx, %rsi
   d55fc: 41 8b d7                     	movl	%r15d, %edx
   d55ff: e8 4c ec ff ff               	callq	0xd4250 <__managedcode+0x62cd0>
   d5604: 0f b6 c0                     	movzbl	%al, %eax
   d5607: 48 83 c4 18                  	addq	$0x18, %rsp
   d560b: 5b                           	popq	%rbx
   d560c: 41 5c                        	popq	%r12
   d560e: 41 5d                        	popq	%r13
   d5610: 41 5e                        	popq	%r14
   d5612: 41 5f                        	popq	%r15
   d5614: 5d                           	popq	%rbp
   d5615: c3                           	retq
   d5616: 33 c0                        	xorl	%eax, %eax
   d5618: 48 83 c4 18                  	addq	$0x18, %rsp
   d561c: 5b                           	popq	%rbx
   d561d: 41 5c                        	popq	%r12
   d561f: 41 5d                        	popq	%r13
   d5621: 41 5e                        	popq	%r14
   d5623: 41 5f                        	popq	%r15
   d5625: 5d                           	popq	%rbp
   d5626: c3                           	retq
   d5627: e8 f0 19 f3 ff               	callq	0x701c <.text+0x67c>
   d562c: e9 75 ff ff ff               	jmp	0xd55a6 <__managedcode+0x64026>
   d5631: e8 06 1a f3 ff               	callq	0x703c <.text+0x69c>
   d5636: eb ad                        	jmp	0xd55e5 <__managedcode+0x64065>
   d5638: 90                           	nop
   d5639: 90                           	nop
   d563a: 90                           	nop
   d563b: 90                           	nop
   d563c: 90                           	nop
   d563d: 90                           	nop
   d563e: 90                           	nop
   d563f: 90                           	nop


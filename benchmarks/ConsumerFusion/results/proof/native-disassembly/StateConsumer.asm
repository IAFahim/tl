
/tmp/tl-consumer-aot/ConsumerChecks:     file format elf64-x86-64


Disassembly of section __managedcode:

000000000012f980 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>>:
  12f980:	55                   	push   %rbp
  12f981:	41 57                	push   %r15
  12f983:	41 56                	push   %r14
  12f985:	41 55                	push   %r13
  12f987:	41 54                	push   %r12
  12f989:	53                   	push   %rbx
  12f98a:	50                   	push   %rax
  12f98b:	48 8d 6c 24 30       	lea    0x30(%rsp),%rbp
  12f990:	0f b7 47 06          	movzwl 0x6(%rdi),%eax
  12f994:	a8 01                	test   $0x1,%al
  12f996:	0f 84 d7 0a 00 00    	je     130473 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xaf3>
  12f99c:	a8 02                	test   $0x2,%al
  12f99e:	0f 85 f6 0a 00 00    	jne    13049a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xb1a>
  12f9a4:	45 85 c0             	test   %r8d,%r8d
  12f9a7:	0f 84 b4 0a 00 00    	je     130461 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xae1>
  12f9ad:	8b 1f                	mov    (%rdi),%ebx
  12f9af:	44 0f b7 7f 04       	movzwl 0x4(%rdi),%r15d
  12f9b4:	8b fb                	mov    %ebx,%edi
  12f9b6:	48 69 ff b5 81 4e 1b 	imul   $0x1b4e81b5,%rdi,%rdi
  12f9bd:	48 c1 ef 26          	shr    $0x26,%rdi
  12f9c1:	69 c7 58 02 00 00    	imul   $0x258,%edi,%eax
  12f9c7:	44 8b f3             	mov    %ebx,%r14d
  12f9ca:	44 2b f0             	sub    %eax,%r14d
  12f9cd:	48 89 4d d0          	mov    %rcx,-0x30(%rbp)
  12f9d1:	33 c0                	xor    %eax,%eax
  12f9d3:	41 3b c0             	cmp    %r8d,%eax
  12f9d6:	0f 8d 4f 0a 00 00    	jge    13042b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xaab>
  12f9dc:	44 8b 0c 81          	mov    (%rcx,%rax,4),%r9d
  12f9e0:	45 8b d1             	mov    %r9d,%r10d
  12f9e3:	4d 69 d2 b5 81 4e 1b 	imul   $0x1b4e81b5,%r10,%r10
  12f9ea:	49 c1 ea 26          	shr    $0x26,%r10
  12f9ee:	45 69 da 58 02 00 00 	imul   $0x258,%r10d,%r11d
  12f9f5:	45 8b e9             	mov    %r9d,%r13d
  12f9f8:	45 2b eb             	sub    %r11d,%r13d
  12f9fb:	44 3b cb             	cmp    %ebx,%r9d
  12f9fe:	73 0d                	jae    12fa0d <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x8d>
  12fa00:	45 3b ee             	cmp    %r14d,%r13d
  12fa03:	41 0f 92 c3          	setb   %r11b
  12fa07:	45 0f b6 db          	movzbl %r11b,%r11d
  12fa0b:	eb 06                	jmp    12fa13 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x93>
  12fa0d:	45 8b da             	mov    %r10d,%r11d
  12fa10:	44 2b df             	sub    %edi,%r11d
  12fa13:	41 8b ff             	mov    %r15d,%edi
  12fa16:	f7 df                	neg    %edi
  12fa18:	81 c7 ff ff 00 00    	add    $0xffff,%edi
  12fa1e:	48 63 ff             	movslq %edi,%rdi
  12fa21:	41 8b db             	mov    %r11d,%ebx
  12fa24:	48 3b fb             	cmp    %rbx,%rdi
  12fa27:	0f 8c 94 0a 00 00    	jl     1304c1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xb41>
  12fa2d:	45 03 fb             	add    %r11d,%r15d
  12fa30:	45 0f b7 ff          	movzwl %r15w,%r15d
  12fa34:	41 83 fd 2f          	cmp    $0x2f,%r13d
  12fa38:	0f 82 7e 05 00 00    	jb     12ffbc <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x63c>
  12fa3e:	41 81 fd c8 00 00 00 	cmp    $0xc8,%r13d
  12fa45:	0f 82 f6 02 00 00    	jb     12fd41 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x3c1>
  12fa4b:	41 81 fd 41 01 00 00 	cmp    $0x141,%r13d
  12fa52:	0f 82 d6 01 00 00    	jb     12fc2e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x2ae>
  12fa58:	41 81 fd 03 02 00 00 	cmp    $0x203,%r13d
  12fa5f:	0f 82 92 00 00 00    	jb     12faf7 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x177>
  12fa65:	41 81 fd 57 02 00 00 	cmp    $0x257,%r13d
  12fa6c:	75 07                	jne    12fa75 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xf5>
  12fa6e:	bf 02 00 00 00       	mov    $0x2,%edi
  12fa73:	eb 17                	jmp    12fa8c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10c>
  12fa75:	45 85 db             	test   %r11d,%r11d
  12fa78:	75 09                	jne    12fa83 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x103>
  12fa7a:	41 81 fe 03 02 00 00 	cmp    $0x203,%r14d
  12fa81:	73 04                	jae    12fa87 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x107>
  12fa83:	33 ff                	xor    %edi,%edi
  12fa85:	eb 05                	jmp    12fa8c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10c>
  12fa87:	bf 01 00 00 00       	mov    $0x1,%edi
  12fa8c:	44 0f b6 df          	movzbl %dil,%r11d
  12fa90:	48 8d 7a 08          	lea    0x8(%rdx),%rdi
  12fa94:	48 83 07 02          	addq   $0x2,(%rdi)
  12fa98:	41 83 fb 02          	cmp    $0x2,%r11d
  12fa9c:	0f 87 71 09 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12faa2:	41 8b fb             	mov    %r11d,%edi
  12faa5:	4c 8d 1d 64 d9 06 00 	lea    0x6d964(%rip),%r11        # 19d410 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>>
  12faac:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  12fab0:	4c 8d 35 d9 fe ff ff 	lea    -0x127(%rip),%r14        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fab7:	4d 03 de             	add    %r14,%r11
  12faba:	41 ff e3             	jmp    *%r11
  12fabd:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12fac1:	ff 07                	incl   (%rdi)
  12fac3:	e9 4b 09 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fac8:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12facc:	ff 07                	incl   (%rdi)
  12face:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fad2:	f3 0f 59 05 42 d9 06 	mulss  0x6d942(%rip),%xmm0        # 19d41c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xc>
  12fad9:	00 
  12fada:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fadf:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fae3:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fae7:	e9 27 09 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12faec:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12faf0:	ff 07                	incl   (%rdi)
  12faf2:	e9 1c 09 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12faf7:	41 81 fd 02 02 00 00 	cmp    $0x202,%r13d
  12fafe:	75 07                	jne    12fb07 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x187>
  12fb00:	bf 02 00 00 00       	mov    $0x2,%edi
  12fb05:	eb 17                	jmp    12fb1e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x19e>
  12fb07:	45 85 db             	test   %r11d,%r11d
  12fb0a:	75 09                	jne    12fb15 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x195>
  12fb0c:	41 81 fe 41 01 00 00 	cmp    $0x141,%r14d
  12fb13:	73 04                	jae    12fb19 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x199>
  12fb15:	33 ff                	xor    %edi,%edi
  12fb17:	eb 05                	jmp    12fb1e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x19e>
  12fb19:	bf 01 00 00 00       	mov    $0x1,%edi
  12fb1e:	44 0f b6 df          	movzbl %dil,%r11d
  12fb22:	48 8d 7a 08          	lea    0x8(%rdx),%rdi
  12fb26:	4c 8b f7             	mov    %rdi,%r14
  12fb29:	49 ff 06             	incq   (%r14)
  12fb2c:	41 83 fb 02          	cmp    $0x2,%r11d
  12fb30:	77 42                	ja     12fb74 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x1f4>
  12fb32:	41 8b db             	mov    %r11d,%ebx
  12fb35:	4c 8d 35 e4 d8 06 00 	lea    0x6d8e4(%rip),%r14        # 19d420 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fb3c:	45 8b 34 9e          	mov    (%r14,%rbx,4),%r14d
  12fb40:	4c 8d 25 49 fe ff ff 	lea    -0x1b7(%rip),%r12        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fb47:	4d 03 f4             	add    %r12,%r14
  12fb4a:	41 ff e6             	jmp    *%r14
  12fb4d:	48 8d 5a 18          	lea    0x18(%rdx),%rbx
  12fb51:	ff 03                	incl   (%rbx)
  12fb53:	eb 1f                	jmp    12fb74 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x1f4>
  12fb55:	48 8d 5a 14          	lea    0x14(%rdx),%rbx
  12fb59:	ff 03                	incl   (%rbx)
  12fb5b:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fb5f:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fb64:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fb68:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fb6c:	eb 06                	jmp    12fb74 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x1f4>
  12fb6e:	48 8d 5a 10          	lea    0x10(%rdx),%rbx
  12fb72:	ff 03                	incl   (%rbx)
  12fb74:	48 8b df             	mov    %rdi,%rbx
  12fb77:	48 83 03 03          	addq   $0x3,(%rbx)
  12fb7b:	41 83 fb 02          	cmp    $0x2,%r11d
  12fb7f:	77 4a                	ja     12fbcb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x24b>
  12fb81:	41 8b db             	mov    %r11d,%ebx
  12fb84:	4c 8d 35 a1 d8 06 00 	lea    0x6d8a1(%rip),%r14        # 19d42c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x1c>
  12fb8b:	45 8b 34 9e          	mov    (%r14,%rbx,4),%r14d
  12fb8f:	4c 8d 25 fa fd ff ff 	lea    -0x206(%rip),%r12        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fb96:	4d 03 f4             	add    %r12,%r14
  12fb99:	41 ff e6             	jmp    *%r14
  12fb9c:	48 8d 5a 18          	lea    0x18(%rdx),%rbx
  12fba0:	ff 03                	incl   (%rbx)
  12fba2:	eb 27                	jmp    12fbcb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x24b>
  12fba4:	48 8d 5a 14          	lea    0x14(%rdx),%rbx
  12fba8:	ff 03                	incl   (%rbx)
  12fbaa:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fbae:	f3 0f 59 05 82 d8 06 	mulss  0x6d882(%rip),%xmm0        # 19d438 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x28>
  12fbb5:	00 
  12fbb6:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fbbb:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fbbf:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fbc3:	eb 06                	jmp    12fbcb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x24b>
  12fbc5:	48 8d 5a 10          	lea    0x10(%rdx),%rbx
  12fbc9:	ff 03                	incl   (%rbx)
  12fbcb:	48 83 07 04          	addq   $0x4,(%rdi)
  12fbcf:	41 83 fb 02          	cmp    $0x2,%r11d
  12fbd3:	0f 87 3a 08 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fbd9:	41 8b fb             	mov    %r11d,%edi
  12fbdc:	4c 8d 1d 59 d8 06 00 	lea    0x6d859(%rip),%r11        # 19d43c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x2c>
  12fbe3:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  12fbe7:	48 8d 1d a2 fd ff ff 	lea    -0x25e(%rip),%rbx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fbee:	4c 03 db             	add    %rbx,%r11
  12fbf1:	41 ff e3             	jmp    *%r11
  12fbf4:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12fbf8:	ff 07                	incl   (%rdi)
  12fbfa:	e9 14 08 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fbff:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12fc03:	ff 07                	incl   (%rdi)
  12fc05:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fc09:	f3 0f 59 05 37 d8 06 	mulss  0x6d837(%rip),%xmm0        # 19d448 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x38>
  12fc10:	00 
  12fc11:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fc16:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fc1a:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fc1e:	e9 f0 07 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fc23:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12fc27:	ff 07                	incl   (%rdi)
  12fc29:	e9 e5 07 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fc2e:	41 81 fd 40 01 00 00 	cmp    $0x140,%r13d
  12fc35:	75 07                	jne    12fc3e <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x2be>
  12fc37:	bf 02 00 00 00       	mov    $0x2,%edi
  12fc3c:	eb 14                	jmp    12fc52 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x2d2>
  12fc3e:	45 85 db             	test   %r11d,%r11d
  12fc41:	75 06                	jne    12fc49 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x2c9>
  12fc43:	41 83 fe 7b          	cmp    $0x7b,%r14d
  12fc47:	73 04                	jae    12fc4d <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x2cd>
  12fc49:	33 ff                	xor    %edi,%edi
  12fc4b:	eb 05                	jmp    12fc52 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x2d2>
  12fc4d:	bf 01 00 00 00       	mov    $0x1,%edi
  12fc52:	40 0f b6 ff          	movzbl %dil,%edi
  12fc56:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  12fc5a:	4c 8b e3             	mov    %rbx,%r12
  12fc5d:	49 83 04 24 03       	addq   $0x3,(%r12)
  12fc62:	83 ff 02             	cmp    $0x2,%edi
  12fc65:	77 49                	ja     12fcb0 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x330>
  12fc67:	8b ff                	mov    %edi,%edi
  12fc69:	4c 8d 25 dc d7 06 00 	lea    0x6d7dc(%rip),%r12        # 19d44c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x3c>
  12fc70:	45 8b 24 bc          	mov    (%r12,%rdi,4),%r12d
  12fc74:	48 8d 0d 15 fd ff ff 	lea    -0x2eb(%rip),%rcx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fc7b:	4c 03 e1             	add    %rcx,%r12
  12fc7e:	41 ff e4             	jmp    *%r12
  12fc81:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12fc85:	ff 07                	incl   (%rdi)
  12fc87:	eb 27                	jmp    12fcb0 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x330>
  12fc89:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12fc8d:	ff 07                	incl   (%rdi)
  12fc8f:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fc93:	f3 0f 59 05 bd d7 06 	mulss  0x6d7bd(%rip),%xmm0        # 19d458 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x48>
  12fc9a:	00 
  12fc9b:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fca0:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fca4:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fca8:	eb 06                	jmp    12fcb0 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x330>
  12fcaa:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12fcae:	ff 07                	incl   (%rdi)
  12fcb0:	41 81 fd 40 01 00 00 	cmp    $0x140,%r13d
  12fcb7:	75 07                	jne    12fcc0 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x340>
  12fcb9:	bf 02 00 00 00       	mov    $0x2,%edi
  12fcbe:	eb 17                	jmp    12fcd7 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x357>
  12fcc0:	45 85 db             	test   %r11d,%r11d
  12fcc3:	75 09                	jne    12fcce <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x34e>
  12fcc5:	41 81 fe c8 00 00 00 	cmp    $0xc8,%r14d
  12fccc:	73 04                	jae    12fcd2 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x352>
  12fcce:	33 ff                	xor    %edi,%edi
  12fcd0:	eb 05                	jmp    12fcd7 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x357>
  12fcd2:	bf 01 00 00 00       	mov    $0x1,%edi
  12fcd7:	44 0f b6 df          	movzbl %dil,%r11d
  12fcdb:	48 8b fb             	mov    %rbx,%rdi
  12fcde:	48 83 07 04          	addq   $0x4,(%rdi)
  12fce2:	41 83 fb 02          	cmp    $0x2,%r11d
  12fce6:	0f 87 27 07 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fcec:	41 8b fb             	mov    %r11d,%edi
  12fcef:	4c 8d 1d 66 d7 06 00 	lea    0x6d766(%rip),%r11        # 19d45c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x4c>
  12fcf6:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  12fcfa:	4c 8d 35 8f fc ff ff 	lea    -0x371(%rip),%r14        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fd01:	4d 03 de             	add    %r14,%r11
  12fd04:	41 ff e3             	jmp    *%r11
  12fd07:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12fd0b:	ff 07                	incl   (%rdi)
  12fd0d:	e9 01 07 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fd12:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12fd16:	ff 07                	incl   (%rdi)
  12fd18:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fd1c:	f3 0f 59 05 24 d7 06 	mulss  0x6d724(%rip),%xmm0        # 19d448 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x38>
  12fd23:	00 
  12fd24:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fd29:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fd2d:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fd31:	e9 dd 06 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fd36:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12fd3a:	ff 07                	incl   (%rdi)
  12fd3c:	e9 d2 06 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fd41:	41 83 fd 4c          	cmp    $0x4c,%r13d
  12fd45:	0f 82 8c 01 00 00    	jb     12fed7 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x557>
  12fd4b:	41 83 fd 7b          	cmp    $0x7b,%r13d
  12fd4f:	0f 82 80 00 00 00    	jb     12fdd5 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x455>
  12fd55:	45 85 db             	test   %r11d,%r11d
  12fd58:	75 06                	jne    12fd60 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x3e0>
  12fd5a:	41 83 fe 7b          	cmp    $0x7b,%r14d
  12fd5e:	73 05                	jae    12fd65 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x3e5>
  12fd60:	45 33 db             	xor    %r11d,%r11d
  12fd63:	eb 06                	jmp    12fd6b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x3eb>
  12fd65:	41 bb 01 00 00 00    	mov    $0x1,%r11d
  12fd6b:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  12fd6f:	48 8b fb             	mov    %rbx,%rdi
  12fd72:	48 83 07 03          	addq   $0x3,(%rdi)
  12fd76:	41 83 fb 02          	cmp    $0x2,%r11d
  12fd7a:	0f 87 93 06 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fd80:	41 8b fb             	mov    %r11d,%edi
  12fd83:	4c 8d 1d de d6 06 00 	lea    0x6d6de(%rip),%r11        # 19d468 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x58>
  12fd8a:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  12fd8e:	4c 8d 35 fb fb ff ff 	lea    -0x405(%rip),%r14        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fd95:	4d 03 de             	add    %r14,%r11
  12fd98:	41 ff e3             	jmp    *%r11
  12fd9b:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12fd9f:	ff 07                	incl   (%rdi)
  12fda1:	e9 6d 06 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fda6:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12fdaa:	ff 07                	incl   (%rdi)
  12fdac:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fdb0:	f3 0f 59 05 a0 d6 06 	mulss  0x6d6a0(%rip),%xmm0        # 19d458 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x48>
  12fdb7:	00 
  12fdb8:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fdbd:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fdc1:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fdc5:	e9 49 06 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fdca:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12fdce:	ff 07                	incl   (%rdi)
  12fdd0:	e9 3e 06 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fdd5:	41 83 fd 7a          	cmp    $0x7a,%r13d
  12fdd9:	75 07                	jne    12fde2 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x462>
  12fddb:	bf 02 00 00 00       	mov    $0x2,%edi
  12fde0:	eb 14                	jmp    12fdf6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x476>
  12fde2:	45 85 db             	test   %r11d,%r11d
  12fde5:	75 06                	jne    12fded <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x46d>
  12fde7:	41 83 fe 4c          	cmp    $0x4c,%r14d
  12fdeb:	73 04                	jae    12fdf1 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x471>
  12fded:	33 ff                	xor    %edi,%edi
  12fdef:	eb 05                	jmp    12fdf6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x476>
  12fdf1:	bf 01 00 00 00       	mov    $0x1,%edi
  12fdf6:	44 0f b6 df          	movzbl %dil,%r11d
  12fdfa:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  12fdfe:	48 8b fb             	mov    %rbx,%rdi
  12fe01:	48 83 07 02          	addq   $0x2,(%rdi)
  12fe05:	41 83 fb 02          	cmp    $0x2,%r11d
  12fe09:	77 4a                	ja     12fe55 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x4d5>
  12fe0b:	41 8b fb             	mov    %r11d,%edi
  12fe0e:	4c 8d 35 5f d6 06 00 	lea    0x6d65f(%rip),%r14        # 19d474 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x64>
  12fe15:	45 8b 34 be          	mov    (%r14,%rdi,4),%r14d
  12fe19:	4c 8d 25 70 fb ff ff 	lea    -0x490(%rip),%r12        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fe20:	4d 03 f4             	add    %r12,%r14
  12fe23:	41 ff e6             	jmp    *%r14
  12fe26:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12fe2a:	ff 07                	incl   (%rdi)
  12fe2c:	eb 27                	jmp    12fe55 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x4d5>
  12fe2e:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12fe32:	ff 07                	incl   (%rdi)
  12fe34:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12fe38:	f3 0f 59 05 f8 d5 06 	mulss  0x6d5f8(%rip),%xmm0        # 19d438 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x28>
  12fe3f:	00 
  12fe40:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12fe45:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fe49:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fe4d:	eb 06                	jmp    12fe55 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x4d5>
  12fe4f:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12fe53:	ff 07                	incl   (%rdi)
  12fe55:	41 8d 7d b4          	lea    -0x4c(%r13),%edi
  12fe59:	0f 57 c0             	xorps  %xmm0,%xmm0
  12fe5c:	f3 48 0f 2a c7       	cvtsi2ss %rdi,%xmm0
  12fe61:	f3 0f 5e 05 17 d6 06 	divss  0x6d617(%rip),%xmm0        # 19d480 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x70>
  12fe68:	00 
  12fe69:	f3 0f 59 05 13 d6 06 	mulss  0x6d613(%rip),%xmm0        # 19d484 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x74>
  12fe70:	00 
  12fe71:	f3 0f 58 05 0f d6 06 	addss  0x6d60f(%rip),%xmm0        # 19d488 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x78>
  12fe78:	00 
  12fe79:	48 8b fb             	mov    %rbx,%rdi
  12fe7c:	48 83 07 04          	addq   $0x4,(%rdi)
  12fe80:	41 83 fb 02          	cmp    $0x2,%r11d
  12fe84:	0f 87 89 05 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fe8a:	41 8b fb             	mov    %r11d,%edi
  12fe8d:	4c 8d 1d f8 d5 06 00 	lea    0x6d5f8(%rip),%r11        # 19d48c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x7c>
  12fe94:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  12fe98:	48 8d 1d f1 fa ff ff 	lea    -0x50f(%rip),%rbx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12fe9f:	4c 03 db             	add    %rbx,%r11
  12fea2:	41 ff e3             	jmp    *%r11
  12fea5:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12fea9:	ff 07                	incl   (%rdi)
  12feab:	e9 63 05 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12feb0:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12feb4:	ff 07                	incl   (%rdi)
  12feb6:	f3 0f 59 06          	mulss  (%rsi),%xmm0
  12feba:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12febf:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12fec3:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12fec7:	e9 47 05 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fecc:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12fed0:	ff 07                	incl   (%rdi)
  12fed2:	e9 3c 05 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12fed7:	41 83 fd 4b          	cmp    $0x4b,%r13d
  12fedb:	75 07                	jne    12fee4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x564>
  12fedd:	bf 02 00 00 00       	mov    $0x2,%edi
  12fee2:	eb 14                	jmp    12fef8 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x578>
  12fee4:	45 85 db             	test   %r11d,%r11d
  12fee7:	75 06                	jne    12feef <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x56f>
  12fee9:	41 83 fe 2f          	cmp    $0x2f,%r14d
  12feed:	73 04                	jae    12fef3 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x573>
  12feef:	33 ff                	xor    %edi,%edi
  12fef1:	eb 05                	jmp    12fef8 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x578>
  12fef3:	bf 01 00 00 00       	mov    $0x1,%edi
  12fef8:	44 0f b6 df          	movzbl %dil,%r11d
  12fefc:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  12ff00:	48 8b fb             	mov    %rbx,%rdi
  12ff03:	48 ff 07             	incq   (%rdi)
  12ff06:	41 83 fb 02          	cmp    $0x2,%r11d
  12ff0a:	77 4a                	ja     12ff56 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x5d6>
  12ff0c:	41 8b fb             	mov    %r11d,%edi
  12ff0f:	4c 8d 35 82 d5 06 00 	lea    0x6d582(%rip),%r14        # 19d498 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x88>
  12ff16:	45 8b 34 be          	mov    (%r14,%rdi,4),%r14d
  12ff1a:	4c 8d 25 6f fa ff ff 	lea    -0x591(%rip),%r12        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12ff21:	4d 03 f4             	add    %r12,%r14
  12ff24:	41 ff e6             	jmp    *%r14
  12ff27:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12ff2b:	ff 07                	incl   (%rdi)
  12ff2d:	eb 27                	jmp    12ff56 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x5d6>
  12ff2f:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12ff33:	ff 07                	incl   (%rdi)
  12ff35:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12ff39:	f3 0f 59 05 63 d5 06 	mulss  0x6d563(%rip),%xmm0        # 19d4a4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x94>
  12ff40:	00 
  12ff41:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12ff46:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12ff4a:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12ff4e:	eb 06                	jmp    12ff56 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x5d6>
  12ff50:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12ff54:	ff 07                	incl   (%rdi)
  12ff56:	48 8b fb             	mov    %rbx,%rdi
  12ff59:	48 83 07 03          	addq   $0x3,(%rdi)
  12ff5d:	41 83 fb 02          	cmp    $0x2,%r11d
  12ff61:	0f 87 ac 04 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12ff67:	41 8b fb             	mov    %r11d,%edi
  12ff6a:	4c 8d 1d 37 d5 06 00 	lea    0x6d537(%rip),%r11        # 19d4a8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x98>
  12ff71:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  12ff75:	48 8d 1d 14 fa ff ff 	lea    -0x5ec(%rip),%rbx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  12ff7c:	4c 03 db             	add    %rbx,%r11
  12ff7f:	41 ff e3             	jmp    *%r11
  12ff82:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  12ff86:	ff 07                	incl   (%rdi)
  12ff88:	e9 86 04 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12ff8d:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  12ff91:	ff 07                	incl   (%rdi)
  12ff93:	f3 0f 10 06          	movss  (%rsi),%xmm0
  12ff97:	f3 0f 59 05 b9 d4 06 	mulss  0x6d4b9(%rip),%xmm0        # 19d458 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x48>
  12ff9e:	00 
  12ff9f:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  12ffa4:	f3 0f 58 02          	addss  (%rdx),%xmm0
  12ffa8:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  12ffac:	e9 62 04 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12ffb1:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  12ffb5:	ff 07                	incl   (%rdi)
  12ffb7:	e9 57 04 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12ffbc:	41 83 fd 0b          	cmp    $0xb,%r13d
  12ffc0:	0f 82 a4 01 00 00    	jb     13016a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x7ea>
  12ffc6:	41 83 fd 12          	cmp    $0x12,%r13d
  12ffca:	0f 82 43 04 00 00    	jb     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  12ffd0:	41 83 fd 1d          	cmp    $0x1d,%r13d
  12ffd4:	0f 82 aa 00 00 00    	jb     130084 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x704>
  12ffda:	41 83 fd 2e          	cmp    $0x2e,%r13d
  12ffde:	75 07                	jne    12ffe7 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x667>
  12ffe0:	bf 02 00 00 00       	mov    $0x2,%edi
  12ffe5:	eb 14                	jmp    12fffb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x67b>
  12ffe7:	45 85 db             	test   %r11d,%r11d
  12ffea:	75 06                	jne    12fff2 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x672>
  12ffec:	41 83 fe 1d          	cmp    $0x1d,%r14d
  12fff0:	73 04                	jae    12fff6 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x676>
  12fff2:	33 ff                	xor    %edi,%edi
  12fff4:	eb 05                	jmp    12fffb <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x67b>
  12fff6:	bf 01 00 00 00       	mov    $0x1,%edi
  12fffb:	44 0f b6 df          	movzbl %dil,%r11d
  12ffff:	41 8d 7d e3          	lea    -0x1d(%r13),%edi
  130003:	0f 57 c0             	xorps  %xmm0,%xmm0
  130006:	f3 48 0f 2a c7       	cvtsi2ss %rdi,%xmm0
  13000b:	f3 0f 5e 05 a1 d4 06 	divss  0x6d4a1(%rip),%xmm0        # 19d4b4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa4>
  130012:	00 
  130013:	f3 0f 59 05 1d d4 06 	mulss  0x6d41d(%rip),%xmm0        # 19d438 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x28>
  13001a:	00 
  13001b:	f3 0f 58 05 81 d4 06 	addss  0x6d481(%rip),%xmm0        # 19d4a4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x94>
  130022:	00 
  130023:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  130027:	48 8b fb             	mov    %rbx,%rdi
  13002a:	48 ff 07             	incq   (%rdi)
  13002d:	41 83 fb 02          	cmp    $0x2,%r11d
  130031:	0f 87 dc 03 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130037:	41 8b fb             	mov    %r11d,%edi
  13003a:	4c 8d 1d 77 d4 06 00 	lea    0x6d477(%rip),%r11        # 19d4b8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa8>
  130041:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  130045:	4c 8d 35 44 f9 ff ff 	lea    -0x6bc(%rip),%r14        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  13004c:	4d 03 de             	add    %r14,%r11
  13004f:	41 ff e3             	jmp    *%r11
  130052:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  130056:	ff 07                	incl   (%rdi)
  130058:	e9 b6 03 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  13005d:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  130061:	ff 07                	incl   (%rdi)
  130063:	f3 0f 59 06          	mulss  (%rsi),%xmm0
  130067:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  13006c:	f3 0f 58 02          	addss  (%rdx),%xmm0
  130070:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  130074:	e9 9a 03 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130079:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  13007d:	ff 07                	incl   (%rdi)
  13007f:	e9 8f 03 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130084:	41 83 fd 1c          	cmp    $0x1c,%r13d
  130088:	75 07                	jne    130091 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x711>
  13008a:	bf 02 00 00 00       	mov    $0x2,%edi
  13008f:	eb 14                	jmp    1300a5 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x725>
  130091:	45 85 db             	test   %r11d,%r11d
  130094:	75 06                	jne    13009c <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x71c>
  130096:	41 83 fe 12          	cmp    $0x12,%r14d
  13009a:	73 04                	jae    1300a0 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x720>
  13009c:	33 ff                	xor    %edi,%edi
  13009e:	eb 05                	jmp    1300a5 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x725>
  1300a0:	bf 01 00 00 00       	mov    $0x1,%edi
  1300a5:	44 0f b6 df          	movzbl %dil,%r11d
  1300a9:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  1300ad:	48 8b fb             	mov    %rbx,%rdi
  1300b0:	48 83 07 02          	addq   $0x2,(%rdi)
  1300b4:	41 83 fb 02          	cmp    $0x2,%r11d
  1300b8:	77 4a                	ja     130104 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x784>
  1300ba:	41 8b fb             	mov    %r11d,%edi
  1300bd:	4c 8d 35 00 d4 06 00 	lea    0x6d400(%rip),%r14        # 19d4c4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xb4>
  1300c4:	45 8b 34 be          	mov    (%r14,%rdi,4),%r14d
  1300c8:	4c 8d 25 c1 f8 ff ff 	lea    -0x73f(%rip),%r12        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  1300cf:	4d 03 f4             	add    %r12,%r14
  1300d2:	41 ff e6             	jmp    *%r14
  1300d5:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  1300d9:	ff 07                	incl   (%rdi)
  1300db:	eb 27                	jmp    130104 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x784>
  1300dd:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  1300e1:	ff 07                	incl   (%rdi)
  1300e3:	f3 0f 10 06          	movss  (%rsi),%xmm0
  1300e7:	f3 0f 59 05 49 d3 06 	mulss  0x6d349(%rip),%xmm0        # 19d438 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x28>
  1300ee:	00 
  1300ef:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  1300f4:	f3 0f 58 02          	addss  (%rdx),%xmm0
  1300f8:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  1300fc:	eb 06                	jmp    130104 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x784>
  1300fe:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  130102:	ff 07                	incl   (%rdi)
  130104:	48 8b fb             	mov    %rbx,%rdi
  130107:	48 83 07 04          	addq   $0x4,(%rdi)
  13010b:	41 83 fb 02          	cmp    $0x2,%r11d
  13010f:	0f 87 fe 02 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130115:	41 8b fb             	mov    %r11d,%edi
  130118:	4c 8d 1d b1 d3 06 00 	lea    0x6d3b1(%rip),%r11        # 19d4d0 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xc0>
  13011f:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  130123:	48 8d 1d 66 f8 ff ff 	lea    -0x79a(%rip),%rbx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  13012a:	4c 03 db             	add    %rbx,%r11
  13012d:	41 ff e3             	jmp    *%r11
  130130:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  130134:	ff 07                	incl   (%rdi)
  130136:	e9 d8 02 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  13013b:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  13013f:	ff 07                	incl   (%rdi)
  130141:	f3 0f 10 06          	movss  (%rsi),%xmm0
  130145:	f3 0f 59 05 fb d2 06 	mulss  0x6d2fb(%rip),%xmm0        # 19d448 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x38>
  13014c:	00 
  13014d:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  130152:	f3 0f 58 02          	addss  (%rdx),%xmm0
  130156:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  13015a:	e9 b4 02 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  13015f:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  130163:	ff 07                	incl   (%rdi)
  130165:	e9 a9 02 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  13016a:	41 83 fd 03          	cmp    $0x3,%r13d
  13016e:	0f 82 41 02 00 00    	jb     1303b5 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa35>
  130174:	41 83 fd 07          	cmp    $0x7,%r13d
  130178:	0f 82 e6 00 00 00    	jb     130264 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x8e4>
  13017e:	41 83 fd 0a          	cmp    $0xa,%r13d
  130182:	75 07                	jne    13018b <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x80b>
  130184:	bf 02 00 00 00       	mov    $0x2,%edi
  130189:	eb 14                	jmp    13019f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x81f>
  13018b:	45 85 db             	test   %r11d,%r11d
  13018e:	75 06                	jne    130196 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x816>
  130190:	41 83 fe 03          	cmp    $0x3,%r14d
  130194:	73 04                	jae    13019a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x81a>
  130196:	33 ff                	xor    %edi,%edi
  130198:	eb 05                	jmp    13019f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x81f>
  13019a:	bf 01 00 00 00       	mov    $0x1,%edi
  13019f:	44 0f b6 df          	movzbl %dil,%r11d
  1301a3:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  1301a7:	48 8b fb             	mov    %rbx,%rdi
  1301aa:	48 83 07 02          	addq   $0x2,(%rdi)
  1301ae:	41 83 fb 02          	cmp    $0x2,%r11d
  1301b2:	77 4a                	ja     1301fe <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x87e>
  1301b4:	41 8b fb             	mov    %r11d,%edi
  1301b7:	4c 8d 35 1e d3 06 00 	lea    0x6d31e(%rip),%r14        # 19d4dc <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xcc>
  1301be:	45 8b 34 be          	mov    (%r14,%rdi,4),%r14d
  1301c2:	4c 8d 25 c7 f7 ff ff 	lea    -0x839(%rip),%r12        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  1301c9:	4d 03 f4             	add    %r12,%r14
  1301cc:	41 ff e6             	jmp    *%r14
  1301cf:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  1301d3:	ff 07                	incl   (%rdi)
  1301d5:	eb 27                	jmp    1301fe <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x87e>
  1301d7:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  1301db:	ff 07                	incl   (%rdi)
  1301dd:	f3 0f 10 06          	movss  (%rsi),%xmm0
  1301e1:	f3 0f 59 05 33 d2 06 	mulss  0x6d233(%rip),%xmm0        # 19d41c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xc>
  1301e8:	00 
  1301e9:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  1301ee:	f3 0f 58 02          	addss  (%rdx),%xmm0
  1301f2:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  1301f6:	eb 06                	jmp    1301fe <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x87e>
  1301f8:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  1301fc:	ff 07                	incl   (%rdi)
  1301fe:	48 8b fb             	mov    %rbx,%rdi
  130201:	48 83 07 04          	addq   $0x4,(%rdi)
  130205:	41 83 fb 02          	cmp    $0x2,%r11d
  130209:	0f 87 04 02 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  13020f:	41 8b fb             	mov    %r11d,%edi
  130212:	4c 8d 1d cf d2 06 00 	lea    0x6d2cf(%rip),%r11        # 19d4e8 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xd8>
  130219:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  13021d:	48 8d 1d 6c f7 ff ff 	lea    -0x894(%rip),%rbx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  130224:	4c 03 db             	add    %rbx,%r11
  130227:	41 ff e3             	jmp    *%r11
  13022a:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  13022e:	ff 07                	incl   (%rdi)
  130230:	e9 de 01 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130235:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  130239:	ff 07                	incl   (%rdi)
  13023b:	f3 0f 10 06          	movss  (%rsi),%xmm0
  13023f:	f3 0f 59 05 01 d2 06 	mulss  0x6d201(%rip),%xmm0        # 19d448 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x38>
  130246:	00 
  130247:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  13024c:	f3 0f 58 02          	addss  (%rdx),%xmm0
  130250:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  130254:	e9 ba 01 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130259:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  13025d:	ff 07                	incl   (%rdi)
  13025f:	e9 af 01 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130264:	41 83 fd 06          	cmp    $0x6,%r13d
  130268:	75 07                	jne    130271 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x8f1>
  13026a:	bf 02 00 00 00       	mov    $0x2,%edi
  13026f:	eb 0e                	jmp    13027f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x8ff>
  130271:	45 85 db             	test   %r11d,%r11d
  130274:	74 04                	je     13027a <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x8fa>
  130276:	33 ff                	xor    %edi,%edi
  130278:	eb 05                	jmp    13027f <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x8ff>
  13027a:	bf 01 00 00 00       	mov    $0x1,%edi
  13027f:	40 0f b6 ff          	movzbl %dil,%edi
  130283:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  130287:	4c 8b e3             	mov    %rbx,%r12
  13028a:	49 ff 04 24          	incq   (%r12)
  13028e:	83 ff 02             	cmp    $0x2,%edi
  130291:	77 41                	ja     1302d4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x954>
  130293:	8b ff                	mov    %edi,%edi
  130295:	4c 8d 25 58 d2 06 00 	lea    0x6d258(%rip),%r12        # 19d4f4 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xe4>
  13029c:	45 8b 24 bc          	mov    (%r12,%rdi,4),%r12d
  1302a0:	48 8d 0d e9 f6 ff ff 	lea    -0x917(%rip),%rcx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  1302a7:	4c 03 e1             	add    %rcx,%r12
  1302aa:	41 ff e4             	jmp    *%r12
  1302ad:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  1302b1:	ff 07                	incl   (%rdi)
  1302b3:	eb 1f                	jmp    1302d4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x954>
  1302b5:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  1302b9:	ff 07                	incl   (%rdi)
  1302bb:	f3 0f 10 06          	movss  (%rsi),%xmm0
  1302bf:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  1302c4:	f3 0f 58 02          	addss  (%rdx),%xmm0
  1302c8:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  1302cc:	eb 06                	jmp    1302d4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x954>
  1302ce:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  1302d2:	ff 07                	incl   (%rdi)
  1302d4:	45 85 db             	test   %r11d,%r11d
  1302d7:	75 06                	jne    1302df <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x95f>
  1302d9:	41 83 fe 03          	cmp    $0x3,%r14d
  1302dd:	73 05                	jae    1302e4 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x964>
  1302df:	45 33 db             	xor    %r11d,%r11d
  1302e2:	eb 06                	jmp    1302ea <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x96a>
  1302e4:	41 bb 01 00 00 00    	mov    $0x1,%r11d
  1302ea:	41 8d 7d fd          	lea    -0x3(%r13),%edi
  1302ee:	0f 57 c0             	xorps  %xmm0,%xmm0
  1302f1:	f3 48 0f 2a c7       	cvtsi2ss %rdi,%xmm0
  1302f6:	f3 0f 5e 05 1e d1 06 	divss  0x6d11e(%rip),%xmm0        # 19d41c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xc>
  1302fd:	00 
  1302fe:	f3 0f 58 05 52 d1 06 	addss  0x6d152(%rip),%xmm0        # 19d458 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x48>
  130305:	00 
  130306:	48 8b fb             	mov    %rbx,%rdi
  130309:	48 83 07 02          	addq   $0x2,(%rdi)
  13030d:	41 83 fb 02          	cmp    $0x2,%r11d
  130311:	77 42                	ja     130355 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x9d5>
  130313:	41 8b fb             	mov    %r11d,%edi
  130316:	4c 8d 35 e3 d1 06 00 	lea    0x6d1e3(%rip),%r14        # 19d500 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xf0>
  13031d:	45 8b 34 be          	mov    (%r14,%rdi,4),%r14d
  130321:	4c 8d 25 68 f6 ff ff 	lea    -0x998(%rip),%r12        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  130328:	4d 03 f4             	add    %r12,%r14
  13032b:	41 ff e6             	jmp    *%r14
  13032e:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  130332:	ff 07                	incl   (%rdi)
  130334:	eb 1f                	jmp    130355 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x9d5>
  130336:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  13033a:	ff 07                	incl   (%rdi)
  13033c:	f3 0f 59 06          	mulss  (%rsi),%xmm0
  130340:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  130345:	f3 0f 58 02          	addss  (%rdx),%xmm0
  130349:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  13034d:	eb 06                	jmp    130355 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x9d5>
  13034f:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  130353:	ff 07                	incl   (%rdi)
  130355:	48 8b fb             	mov    %rbx,%rdi
  130358:	48 83 07 04          	addq   $0x4,(%rdi)
  13035c:	41 83 fb 02          	cmp    $0x2,%r11d
  130360:	0f 87 ad 00 00 00    	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  130366:	41 8b fb             	mov    %r11d,%edi
  130369:	4c 8d 1d 9c d1 06 00 	lea    0x6d19c(%rip),%r11        # 19d50c <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xfc>
  130370:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  130374:	48 8d 1d 15 f6 ff ff 	lea    -0x9eb(%rip),%rbx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  13037b:	4c 03 db             	add    %rbx,%r11
  13037e:	41 ff e3             	jmp    *%r11
  130381:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  130385:	ff 07                	incl   (%rdi)
  130387:	e9 87 00 00 00       	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  13038c:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  130390:	ff 07                	incl   (%rdi)
  130392:	f3 0f 10 06          	movss  (%rsi),%xmm0
  130396:	f3 0f 59 05 aa d0 06 	mulss  0x6d0aa(%rip),%xmm0        # 19d448 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x38>
  13039d:	00 
  13039e:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  1303a3:	f3 0f 58 02          	addss  (%rdx),%xmm0
  1303a7:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  1303ab:	eb 66                	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  1303ad:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  1303b1:	ff 07                	incl   (%rdi)
  1303b3:	eb 5e                	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  1303b5:	45 85 db             	test   %r11d,%r11d
  1303b8:	74 04                	je     1303be <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa3e>
  1303ba:	33 ff                	xor    %edi,%edi
  1303bc:	eb 05                	jmp    1303c3 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa43>
  1303be:	bf 01 00 00 00       	mov    $0x1,%edi
  1303c3:	48 8d 5a 08          	lea    0x8(%rdx),%rbx
  1303c7:	4c 8b db             	mov    %rbx,%r11
  1303ca:	49 ff 03             	incq   (%r11)
  1303cd:	83 ff 02             	cmp    $0x2,%edi
  1303d0:	77 41                	ja     130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  1303d2:	8b ff                	mov    %edi,%edi
  1303d4:	4c 8d 1d 3d d1 06 00 	lea    0x6d13d(%rip),%r11        # 19d518 <__readonlydata_ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x108>
  1303db:	45 8b 1c bb          	mov    (%r11,%rdi,4),%r11d
  1303df:	48 8d 1d aa f5 ff ff 	lea    -0xa56(%rip),%rbx        # 12f990 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x10>
  1303e6:	4c 03 db             	add    %rbx,%r11
  1303e9:	41 ff e3             	jmp    *%r11
  1303ec:	48 8d 7a 18          	lea    0x18(%rdx),%rdi
  1303f0:	ff 07                	incl   (%rdi)
  1303f2:	eb 1f                	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  1303f4:	48 8d 7a 14          	lea    0x14(%rdx),%rdi
  1303f8:	ff 07                	incl   (%rdi)
  1303fa:	f3 0f 10 06          	movss  (%rsi),%xmm0
  1303fe:	f3 0f 58 46 04       	addss  0x4(%rsi),%xmm0
  130403:	f3 0f 58 02          	addss  (%rdx),%xmm0
  130407:	f3 0f 11 02          	movss  %xmm0,(%rdx)
  13040b:	eb 06                	jmp    130413 <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0xa93>
  13040d:	48 8d 7a 10          	lea    0x10(%rdx),%rdi
  130411:	ff 07                	incl   (%rdi)
  130413:	41 8b d9             	mov    %r9d,%ebx
  130416:	45 8b f5             	mov    %r13d,%r14d
  130419:	41 8b fa             	mov    %r10d,%edi
  13041c:	ff c0                	inc    %eax
  13041e:	41 3b c0             	cmp    %r8d,%eax
  130421:	48 8b 4d d0          	mov    -0x30(%rbp),%rcx
  130425:	0f 8c b1 f5 ff ff    	jl     12f9dc <ConsumerChecks_Tl_ConsumerFusion_FusedPulse__Forward_0<ConsumerChecks_Tl_ConsumerFusion_ConsumerInput__ConsumerChecks_Tl_ConsumerFusion_StateConsumer>+0x5c>
  13042b:	b8 01 00 00 00       	mov    $0x1,%eax
  130430:	bf 05 00 00 00       	mov    $0x5,%edi
  130435:	41 81 fe 57 02 00 00 	cmp    $0x257,%r14d
  13043c:	0f 44 c7             	cmove  %edi,%eax
  13043f:	8b fb                	mov    %ebx,%edi
  130441:	41 8b cf             	mov    %r15d,%ecx
  130444:	48 c1 e1 20          	shl    $0x20,%rcx
  130448:	48 0b f9             	or     %rcx,%rdi
  13044b:	48 c1 e0 30          	shl    $0x30,%rax
  13044f:	48 0b c7             	or     %rdi,%rax
  130452:	48 83 c4 08          	add    $0x8,%rsp
  130456:	5b                   	pop    %rbx
  130457:	41 5c                	pop    %r12
  130459:	41 5d                	pop    %r13
  13045b:	41 5e                	pop    %r14
  13045d:	41 5f                	pop    %r15
  13045f:	5d                   	pop    %rbp
  130460:	c3                   	ret
  130461:	48 8b 07             	mov    (%rdi),%rax
  130464:	48 83 c4 08          	add    $0x8,%rsp
  130468:	5b                   	pop    %rbx
  130469:	41 5c                	pop    %r12
  13046b:	41 5d                	pop    %r13
  13046d:	41 5e                	pop    %r14
  13046f:	41 5f                	pop    %r15
  130471:	5d                   	pop    %rbp
  130472:	c3                   	ret
  130473:	48 8d 3d ce 36 12 00 	lea    0x1236ce(%rip),%rdi        # 253b48 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
  13047a:	e8 a1 b5 f3 ff       	call   6ba20 <RhpNewFast>
  13047f:	48 8b d8             	mov    %rax,%rbx
  130482:	48 8b fb             	mov    %rbx,%rdi
  130485:	48 8d 35 6c 30 11 00 	lea    0x11306c(%rip),%rsi        # 2434f8 <__Str_Playback_was_never_started__mi_A4B7AD70A4141AA14D48EC123D832D5C325A07EB90C0D33B7FB492BC5D1E26A5>
  13048c:	e8 af 3a f6 ff       	call   93f40 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
  130491:	48 8b fb             	mov    %rbx,%rdi
  130494:	e8 87 b8 f3 ff       	call   6bd20 <RhpThrowEx>
  130499:	cc                   	int3
  13049a:	48 8d 3d a7 36 12 00 	lea    0x1236a7(%rip),%rdi        # 253b48 <_ZTV44S_P_CoreLib_System_InvalidOperationException>
  1304a1:	e8 7a b5 f3 ff       	call   6ba20 <RhpNewFast>
  1304a6:	48 8b d8             	mov    %rax,%rbx
  1304a9:	48 8b fb             	mov    %rbx,%rdi
  1304ac:	48 8d 35 6d 2f 11 00 	lea    0x112f6d(%rip),%rsi        # 243420 <__Str_Playback_is_stopped__BEFEE01D2658356891B4B1A0CFEF9C031BEA0351DB9767A723E74228769FE021>
  1304b3:	e8 88 3a f6 ff       	call   93f40 <S_P_CoreLib_System_InvalidOperationException___ctor_0>
  1304b8:	48 8b fb             	mov    %rbx,%rdi
  1304bb:	e8 60 b8 f3 ff       	call   6bd20 <RhpThrowEx>
  1304c0:	cc                   	int3
  1304c1:	48 8d 3d 60 29 12 00 	lea    0x122960(%rip),%rdi        # 252e28 <_ZTV46S_P_CoreLib_System_ArgumentOutOfRangeException>
  1304c8:	e8 53 b5 f3 ff       	call   6ba20 <RhpNewFast>
  1304cd:	4c 8b f0             	mov    %rax,%r14
  1304d0:	49 8b fe             	mov    %r14,%rdi
  1304d3:	48 8d 35 6e c3 11 00 	lea    0x11c36e(%rip),%rsi        # 24c848 <__Str_ticks>
  1304da:	48 8d 15 e7 2e 11 00 	lea    0x112ee7(%rip),%rdx        # 2433c8 <__Str_Playback_cycle_capacity_exceed_438134DB0460610D3D09165487578575BC58EF0A4041E10A1DCD99D64E2B2DA0>
  1304e1:	e8 4a 05 f6 ff       	call   90a30 <S_P_CoreLib_System_ArgumentOutOfRangeException___ctor_1>
  1304e6:	49 8b fe             	mov    %r14,%rdi
  1304e9:	e8 32 b8 f3 ff       	call   6bd20 <RhpThrowEx>
  1304ee:	cc                   	int3

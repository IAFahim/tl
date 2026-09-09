
/tmp/tl-consumer-aot/ConsumerChecks:     file format elf64-x86-64


Disassembly of section __managedcode:

00000000000795a0 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify>:
   795a0:	55                   	push   %rbp
   795a1:	53                   	push   %rbx
   795a2:	50                   	push   %rax
   795a3:	48 8d 6c 24 10       	lea    0x10(%rsp),%rbp
   795a8:	49 8d 40 20          	lea    0x20(%r8),%rax
   795ac:	ff 00                	incl   (%rax)
   795ae:	8b c2                	mov    %edx,%eax
   795b0:	48 c1 e0 20          	shl    $0x20,%rax
   795b4:	49 33 40 28          	xor    0x28(%r8),%rax
   795b8:	44 0f b7 cf          	movzwl %di,%r9d
   795bc:	49 c1 e1 10          	shl    $0x10,%r9
   795c0:	49 33 c1             	xor    %r9,%rax
   795c3:	66 41 0f 7e c1       	movd   %xmm0,%r9d
   795c8:	49 33 c1             	xor    %r9,%rax
   795cb:	40 0f b6 f6          	movzbl %sil,%esi
   795cf:	48 33 c6             	xor    %rsi,%rax
   795d2:	48 be b3 01 00 00 00 	movabs $0x100000001b3,%rsi
   795d9:	01 00 00 
   795dc:	48 0f af c6          	imul   %rsi,%rax
   795e0:	49 89 40 28          	mov    %rax,0x28(%r8)
   795e4:	3b 51 08             	cmp    0x8(%rcx),%edx
   795e7:	75 0b                	jne    795f4 <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify+0x54>
   795e9:	0f b7 ff             	movzwl %di,%edi
   795ec:	0f b7 41 0c          	movzwl 0xc(%rcx),%eax
   795f0:	3b f8                	cmp    %eax,%edi
   795f2:	74 07                	je     795fb <ConsumerChecks_Tl_ConsumerFusion_EffectConsumer__Notify+0x5b>
   795f4:	48 83 c4 08          	add    $0x8,%rsp
   795f8:	5b                   	pop    %rbx
   795f9:	5d                   	pop    %rbp
   795fa:	c3                   	ret
   795fb:	48 8d 3d 76 86 1d 00 	lea    0x1d8676(%rip),%rdi        # 251c78 <_ZTV57ConsumerChecks_Tl_ConsumerFusion_ConsumerFailureException>
   79602:	e8 19 24 ff ff       	call   6ba20 <RhpNewFast>
   79607:	48 8b d8             	mov    %rax,%rbx
   7960a:	48 8b fb             	mov    %rbx,%rdi
   7960d:	e8 ee 4b 01 00       	call   8e200 <S_P_CoreLib_System_Exception___ctor>
   79612:	48 8b fb             	mov    %rbx,%rdi
   79615:	e8 06 27 ff ff       	call   6bd20 <RhpThrowEx>
   7961a:	cc                   	int3

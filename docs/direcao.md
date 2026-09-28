# Direção do FactoryChaos

Este arquivo é a direção compartilhada do jogo. Humanos e IA seguem ele. Não implemente o backlog inteiro de uma vez: prove uma fase antes de abrir a próxima.

## Estado atual

O protótipo single-player já tem:

- andar, olhar, pegar, soltar e arremessar
- pilha de minério, máquina (1 minério vira 1 produto em 3s), esteira física e entrega por $10
- a esteira emperra sozinha por 4 segundos, entre 18 e 32 segundos de funcionamento, e avisa o jogador
- um corte de combate: produto vira munição e canhão fixo
- onda 1: 50 segundos de preparação, um inimigo sai do túnel, três tiros do canhão derrubam, se ele chega na fábrica a rodada acaba

Ainda não existem a onda 2, pedidos, timer de entrega, superaquecimento, produto caindo, energia, upgrades de run, apostas nem multiplayer. Não apague o que já funciona para recomeçar do zero.

## Ideias

- [ ] Vários veios de minério, combinação para munições melhores, logística de esteira e fornalha
- [ ] Liberar upgrades nas máquinas, armas e melhorias
- [ ] Waves de inimigos e rodadas que aumentam de nível
- [ ] Algum sistema de aposta
- [ ] Satisfatório para jogar
- [ ] Começar minerando manualmente com picareta, depois liberar miner, depois upgraders

Unity + C#. Começar pequeno demais de propósito. Para um co-op de 2–4 jogadores com física e interação, o Netcode for GameObjects é a opção de alto nível para poucos jogadores e lógica menos complexa. Multiplayer só entra depois que o loop single-player estiver gostoso.

## Fase 1 — prove o jogo sem multiplayer

Primeiro um protótipo single-player, visualmente simples.

O mapa pode ser um galpão com:

`Entrada → Esteira → Máquina → Saída`

Cinco sistemas:

1. **Pegar e soltar objetos.** Andar, correr, pegar e jogar.
2. **Esteiras.** Objetos colocados nela se movem fisicamente.
3. **Máquinas.** Entra minério, espera, sai produto.
4. **Dinheiro.** Entregar produto dá dinheiro.
5. **Problemas.** Máquina superaquece, esteira trava, produto cai, energia acaba.

Nada de skill tree. Nada de skins. Nada de 40 máquinas. Nada de geração procedural.

A pergunta da fase é: é divertido correr pela fábrica tentando impedir tudo de desmoronar?

## Fase 2 — transforme isso em caos

Situações emergentes, não uma lista maior de máquinas.

Exemplo: a fundição precisa de 2 ferro + 1 carvão e está superaquecendo. Alguém corre buscar água. Tropeça, derruba a carga, a máquina explode e uma peça acerta o outro jogador.

Esse momento é o jogo. A fábrica é o palco.

## Loop de run

```
COMEÇO DA RUN
↓
recebe pedido (engrenagens, placas, motores)
↓
organiza a fábrica
↓
produção começa
↓
problemas aparecem
↓
correria / caos
↓
pedido entregue
↓
ganha dinheiro
↓
escolhe 1 de 3 upgrades
↓
próxima rodada mais difícil
```

O elemento seguinte é escolha de build, no espírito de Balatro. Cada upgrade melhora uma coisa e cria outro problema.

Exemplos:

- **Turbo Conveyor.** Esteiras +50% rápidas, 10% de chance do item cair.
- **Cheap Machinery.** Máquinas custam -40%, quebram 2× mais.
- **Union Break.** Produção +30%, funcionários ficam mais lentos. Exemplo de tom, não de conteúdo obrigatório.

## Arquitetura inicial

Código descartável no começo. O alvo é gameplay, não elegância. Não arquitete para 200 tipos de máquina.

```
Game
├── Player
│   ├── Movement
│   ├── Interaction
│   └── Carry
├── Factory
│   ├── Conveyor
│   ├── Machine
│   ├── Generator
│   └── DeliveryZone
├── Items
│   ├── Iron
│   ├── Coal
│   ├── Plate
│   └── Gear
├── Systems
│   ├── Economy
│   ├── Orders
│   ├── Round
│   └── Upgrades
└── UI
    ├── Order
    ├── Money
    └── Timer
```

Os scripts atuais podem permanecer na pasta plana `Assets/Scripts` até a separação ajudar de verdade.

## Multiplayer

Só depois que o loop single-player estiver minimamente gostoso.

Host + até 3 clients. Um jogador hospeda, os outros entram na lobby. Sem servidor dedicado no início. Na Steam, depois, as APIs de networking/P2P da Valve e o Steam Datagram Relay evitam expor o IP dos jogadores.

## Como a IA trabalha

Ciclo: ideia → implementação pequena → jogar → ajustar.

Exemplo de pedidos em sequência, cada um testado antes do próximo:

1. Máquina que recebe minério de ferro e, após alguns segundos, gera uma placa.
2. Superaquecimento proporcional à velocidade.
3. Fumaça acima de 80%.
4. A 100%, explode e aplica força nos rigidbodies próximos.

Não antecipe a fase seguinte dentro do mesmo pedido.

## Ordem sugerida

**Primeiro corte jogável.** Personagem anda, pega e joga. Caixa vai para a esteira, vira produto, entrega gera dinheiro. Um pedido com timer: produzir uma quantidade em poucos minutos. Se tiver charme, continue.

**Segundo corte.** Segundo jogador e só três fontes de caos: incêndio, máquinas quebrando e energia. O sinal de que funciona é cooperação espontânea (“pega o ferro”, “cadê o carvão”, “vai explodir”). “Funciona” não basta. O alvo é querer jogar de novo.

**Terceiro corte.** Upgrades, runs e progressão. A partida começa simples e termina numa linha industrial instável. O crescimento visual faz parte da satisfação.

**Arte por último.** Low-poly, personagens meio bobos, física exagerada. Referências de tom: PEAK, Human Fall Flat, Overcooked, Content Warning. Realismo custa modelagem, animação, luz, textura, VFX e consistência. Imperfeição vira personalidade.

## Primeiro milestone

Em uma partida inteira com outra pessoa:

```
2 jogadores
1 mapa
3 máquinas
4 recursos
3 problemas
5 upgrades
1 objetivo
```

Se terminarem e quiserem de novo, continue. Se a reação for só “legal, funciona”, desconfie. A versão pequena existe para descobrir se há um jogo, antes de emprego largado, orçamento alto ou um ano de aposta.

# FactoryChaos

Protótipo de fábrica em primeira pessoa, low-poly, feito em Unity. A ideia é uma linha de produção física que dá errado, e no futuro um co-op curto em que o caos vira o jogo.

A direção compartilhada está em [docs/direcao.md](docs/direcao.md). Quem for implementar segue esse arquivo e a regra em `.cursor/rules/direcao.mdc`. Prove a fase atual antes de abrir multiplayer, waves, apostas ou upgrades.

## Como abrir

1. Instale o Unity `6000.6.3f1`.
2. Abra esta pasta como projeto.
3. Abra `Assets/Scenes/SampleScene.unity` e dê Play.

O combate de teste (máquina de munição, canhão e alvo) aparece ao dar Play, à direita do galpão.

## Controles

| Ação | Tecla |
| --- | --- |
| Andar | WASD |
| Correr | Shift segurado |
| Pular | Espaço |
| Olhar | mouse |
| Pegar ou soltar | E |
| Picareta / slots | 1, depois 2 3 4 |
| Depositar na máquina | botão direito |
| Arremessar | clique esquerdo |
| Operar o canhão | E, olhando para ele e com as mãos vazias |
| Mirar e atirar | mouse e clique esquerdo |
| Sair do canhão | E ou Esc |
| Soltar o cursor | Esc |

## O que já existe

- Pegar, carregar, soltar e arremessar um objeto por vez.
- Pilha de minério. A máquina aceita minério e, em 3 segundos, solta um produto.
- Esteira física para minério e produto. Ela emperra sozinha por 4 segundos, com um aviso na tela, e volta sem botão de conserto. O intervalo entre travadas é aleatório, de 18 a 32 segundos.
- Entrega: só produto, +$10. O dinheiro aparece no canto da tela.
- Máquina de munição: 1 produto vira 1 munição em 2 segundos.
- Canhão fixo com 3 tiros. Cada tiro tira 20.
- Onda 1: 50 segundos para preparar munição. Um inimigo sai do túnel à frente. Três tiros derrubam. Se ele chega na fábrica, a rodada acaba.

Munição não entra na esteira, não vende na entrega e não entra na máquina de minério.

## Fora desta fase

Pedidos, timer, superaquecimento, produto caindo, energia, upgrades, waves, apostas e multiplayer. O [documento de direção](docs/direcao.md) descreve quando cada um entra.

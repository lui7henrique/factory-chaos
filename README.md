# FactoryChaos

Protótipo de fábrica em primeira pessoa, low-poly, feito em Unity. A ideia é uma linha de produção física que dá errado, e no futuro um co-op curto em que o caos vira o jogo.

A direção compartilhada está em [docs/direcao.md](docs/direcao.md). Quem for implementar segue esse arquivo e a regra em `.cursor/rules/direcao.mdc`. Prove a fase atual antes de abrir multiplayer, waves, apostas ou upgrades.

## Como abrir

1. Instale o Unity `6000.6.3f1`.
2. Abra esta pasta como projeto.
3. Abra `Assets/Scenes/SampleScene.unity` e dê Play.

A mina e o corte de combate (prensa, canhão e onda 1) aparecem ao dar Play. O objetivo é minerar ferro, fundir lingotes, prensar três cartuchos e defender o túnel.

## Controles

| Ação | Tecla |
| --- | --- |
| Andar | WASD |
| Correr | Shift segurado |
| Pular | Espaço |
| Olhar | mouse |
| Pegar ou soltar | E |
| Picareta / slots | 1–4 ou roda do mouse |
| Minerar | clique esquerdo; segurar repete |
| Depositar, carregar canhão ou entregar lingote | botão direito, com o item na mão e olhando para o destino |
| Arremessar | clique esquerdo |
| Operar o canhão | E, olhando para ele e com as mãos vazias |
| Mirar e atirar | mouse e clique esquerdo |
| Sair do canhão | E ou Esc |
| Abrir ou fechar o menu de pausa | Esc |
| Comparar as salas | T, com confirmação (reinicia o estado) |

## O que já existe

- Pegar, carregar, soltar e arremessar um objeto por vez.
- Pilha de minério. A máquina aceita minério e, em 3 segundos, solta um produto.
- Esteira física para minério e produto. Ela emperra sozinha por 4 segundos, com um aviso na tela, e volta sem botão de conserto. O intervalo entre travadas é aleatório, de 18 a 32 segundos.
- Entrega: só produto, +$10. O dinheiro aparece no canto da tela.
- Máquina de munição: 1 produto vira 1 munição em 2 segundos.
- Canhão fixo com 3 tiros. Cada tiro tira 20.
- Onda 1: 50 segundos para preparar munição. Um inimigo sai do túnel à frente. Três tiros derrubam. Se ele chega na fábrica, a rodada acaba.
- Menu de pausa no Esc, com continuar, controles, confirmação de reinício e configurações de volume, sensibilidade, inversão vertical e HUD compacta. Preferências ficam salvas neste computador.
- HUD inspirada no Paper, ligada à onda, dinheiro, inventário físico, produção, esteira e canhão. A [auditoria da integração](docs/hud-integration.md) explica os elementos do mockup que foram adaptados ou descartados e os testes realizados.
- Feedback de coleta, entrega, produção e combate, incluindo sons procedurais, confirmação de acerto, recuo e clarão do canhão. Veja o [registro do refinamento](docs/refinement.md) e sua verificação.

Munição não entra na esteira, não vende na entrega e não entra na máquina de minério.

## Fora desta fase

Pedidos, timer, superaquecimento, produto caindo, energia, upgrades, waves, apostas e multiplayer. O [documento de direção](docs/direcao.md) descreve quando cada um entra.

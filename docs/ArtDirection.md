# Direção de arte do FactoryChaos

Fonte da arte. Humanos e IA seguem este arquivo. A regra em `.cursor/rules/art-direction.mdc` aponta para cá.

Princípios de referência: [How To Make Low Poly Look Good](https://sundaysundae.co/how-to-make-low-poly-look-good/) (Sunday Sundae, Pontypants, 2018). O artigo não é um estilo a copiar. As decisões abaixo são as que valem neste projeto.

## O que o artigo nos deu

Low-poly moderno mistura forma simples com iluminação cuidada. A silhueta precisa ler de qualquer ângulo. Polígonos seguem o tamanho e a importância do objeto: um tufo pequeno não leva a mesma malha de uma fornalha. Faces planas, vértices únicos por face, sem smooth compartilhado. Cor chapada; complexidade vem da luz, não de textura. Saturação alta demais, sobretudo no verde, compete com os itens. Specular baixo tira o neon dos highlights. Luz direcional, preenchimento do céu/ambiente e bounce (mesmo aproximado) importam mais que mais geometria. Depth haze leve ajuda a ler profundidade. Depth of field e motion blur ficam desligados durante o jogo: o jogador precisa ver o que carrega.

## Mundo

Uma pequena oficina industrial dentro de uma mina. Estética estilizada, sólida e convidativa, com acabamento intermediário: formas simples bem trabalhadas e detalhes funcionais.

Tudo precisa funcionar na câmera em primeira pessoa, inclusive perto dos objetos e com um item na mão.

A amostra viva é a **fornalha** e o trecho de cenário `WorkshopBay` ao redor dela. Prensa, canhão, entrega e veio só herdam esta linguagem depois da revisão visual.

## Paleta

Valores de partida. Ajuste só depois de olhar a cena iluminada, não por gosto isolado do material.

| Uso | Hex | Notas |
| --- | --- | --- |
| Casca pintada | `#35696B` | Fornalha e futuras carcaças. |
| Bases e mecanismos | `#343B43` | Pés, chaminé, molduras. |
| Paredes | `#858078` | Concreto quente da oficina. |
| Ferro exposto | `#A5ADB5` | Bandejas, lingote. |
| Marcações | `#D5A63A` | Bordas de entrada e saída, capuz da lâmpada. |
| Cristais do minério | `#D87670` | Só no veio e no minério. |
| Lâmpada livre | `#59EF61` | Já existente. |
| Lâmpada ocupada | `#FFBF36` | Já existente. |

Minério, lingote e munição não se diferenciam só por cor: minério é pedra facetada com cristal coral, lingote é barra de ferro, munição é cartucho amarelo com ponta de cobre.

Gramado do pátio fica um pouco menos saturado que o item, para o chão não gritar.

## Formas

- Cada equipamento lê pelo contorno: fornalha = corpo + boca de fogo + chaminé; prensa = vertical + compressão; canhão = tubo + suporte + base.
- Espessuras, pés largos e bordas amarelas de interação são a linguagem comum.
- Chanfro largo onde o jogador chega perto. Sem parafusos, rebites, ferrugem ou tubos finos.
- Densidade de polígonos proporcional ao tamanho. Tufos e pedras de chão são mais pobres que a fornalha.
- Flat shading. Não compartilhe vértices entre faces. Não deixe normais invertidas.

## Materiais

URP/Lit, sem mapas de albedo. Metallic e Smoothness baixos. Ferro exposto pode chegar a metallic 0.32 e smoothness 0.22. Casca pintada fica quase fosca. Concreto e pedra, metallic 0.

Arquivos reutilizáveis em `Assets/Art/Materials`, criados por **GameObject > Factory Chaos > Apply Workshop Art Sample**. No Play, `ArtMaterials.Runtime()` usa a mesma receita se o asset ainda não existir.

Não gere um material por peça. Emissão só no fogo, no miolo da fornalha e nas lâmpadas de estado, em valores moderados, sem depender de bloom alto.

## Iluminação

URP 17.6.0 neste projeto. Compatível e usado:

- Luz direcional principal com sombras soft.
- Até 4 additional lights por objeto (`PC_RPAsset`). Uma point light quente na baía da fornalha cabe.
- Trilight ambiente em runtime, porque GI baked exigiria bake no Editor e ainda não temos lightmaps.
- Fog exponencial bem leve, no lugar de um haze de pós pesado.
- Bloom já existe no `SampleSceneProfile` (threshold 1, intensity 0.25). Não subir.
- Motion Blur no profile já está `active: 0`. Depth of Field não está no profile; `WorkshopLook` desliga os dois se aparecerem.

Não usar neste passo: Probe Volumes, SSAO extra, baked GI, reflexos espelhados em faces grandes, DOF, motion blur de gameplay.

Direcional: cor quente `#FFEB C6` aproximada (`ArtPalette.Sun`), intensity 1.15, temperatura 4250 K. Fill da oficina: point light pêssego, intensity 3.2, range 8.5. A boca da fornalha brilha; o corpo e as bandejas continuam legíveis.

## Código que recria visual

| Script | Quando | O que deve fazer |
| --- | --- | --- |
| `PlaySceneVisuals` | Play, se o visual ainda não está na cena | Chão, fornalha e chama `WorkshopLook`. Não apaga um `Visual` já existente. |
| `WorkshopLook` | Play | Luz, fog, baía `WorkshopBay`. |
| `FurnaceVisual` | Menu ou Play | Casca da fornalha. Usa `ArtMaterials`. |
| `FurnaceVisualMenu` | Edit | Atualiza a máquina selecionada, Undo, não salva a cena. |
| `WorkshopArtMenu` | Edit | Cria materiais em `Assets/Art/Materials`, baía, luz e fornalha. |

Se a fornalha já tem um filho `Visual`, o Play não reconstrói. Rode o menu de arte só em edit mode e salve a cena se quiser persistir a baía.

## Fábrica interna

A cena `IndoorFactory` é a oficina jogável dentro da mina. `FactorySite` com `Indoor` impede que `PlaySceneVisuals`, `MiningBootstrap` e `CombatTestSpawner` plantem grama, veio ou máquinas do pátio. A casca usa os materiais de `Assets/Art/Materials`: parede `#858078`, grafite `#343B43`, concreto, rocha e marcação `#D5A63A`. Sem parafusos e sem tubos finos. Fornalha, prensa e canhão usam a mesma paleta, sem malha nova. Luz de preenchimento pêssego, fog exponencial leve, sem sol atravessando o teto. O comando é GameObject > Factory Chaos > Build Indoor Factory.

## Fora desta amostra

Não refazer prensa, canhão, alvo, entrega nem HUD neste passo. Não abrir skill tree, skins nem geração procedural.

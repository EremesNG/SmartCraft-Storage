# SmartCraft-Storage

Mod pessoal para Valheim 1.0 que junta automação de armazenamento e de
estações num único pacote: guardar itens em massa nos baús próximos,
craftar/construir puxando material direto dos baús sem precisar abrir eles,
manter fogueira, forja, carvoaria e cozinha abastecidas e coletando
sozinhas, e alimentar automaticamente animais domesticáveis a partir de um
baú próximo.

Requer [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
e [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).

## Atalhos

Todos os atalhos usam a tecla **E** como base, combinada com um modificador.
Funcionam com o cursor livre (sem precisar estar com um baú aberto).

| Atalho | Ação |
|---|---|
| `Shift + E` | **Quick-stack**: guarda os itens do seu inventário nos baús próximos que já têm aquele item. Itens travados (🔒) e equipados nunca são movidos. |
| `Ctrl + E` | **Restock**: repõe do(s) baú(s) próximo(s) os itens marcados pra restock, até completar uma pilha cheia no seu inventário — mesmo que você não tenha nenhuma unidade daquele item ainda. |
| `Alt + clique esquerdo` num item do inventário | Alterna o **lock** (🔒) daquele item — item travado nunca é movido pelo quick-stack. |
| `Alt + Ctrl + clique esquerdo` num item do inventário | Alterna a marcação de **restock** (🔵) daquele item — define a lista que o `Ctrl+E` usa. |

Os dois cliques com modificador substituem o clique normal (não abrem/movem
o item) só enquanto o modificador estiver segurado.

## Baús próximos: como são escolhidos

Toda automação deste mod (quick-stack, restock, crafting-de-baús-próximos,
as 4 estações, e a alimentação automática de animais) usa a mesma regra pra
decidir quais baús contam como "próximos":

- Dentro do raio configurado (ver seção de configuração)
- Não é um caixão de jogador morto (`TombStone`)
- Não está sendo usado por ninguém no momento (aberto por outro jogador)
- Você tem permissão de acesso no baú (respeita público/privado/grupo)
- O baú está dentro de uma área com Ward (proteção) que te dá acesso — baú
  fora do seu ward, ou dentro do ward de outra pessoa sem permissão, é
  ignorado

Funciona em multiplayer: quando o mod precisa escrever num baú que
pertence a outro jogador (ZDO owner diferente), ele reivindica a posse
antes de mexer, do mesmo jeito que o próprio jogo faz quando você abre um
baú manualmente.

## Guardar em massa (Quick-Stack)

`Shift + E` — pra cada item do seu inventário (exceto equipados e
travados), procura um baú próximo que já tenha aquele item (mesmo nome e
qualidade) e move pra lá. Primeiro completa as pilhas existentes no baú;
se sobrar quantidade e o baú tiver espaço livre, cria uma pilha nova nele
também. Só move pra um baú que **já contém** o item — não é um "guarda
tudo", é achar onde aquele item já mora.

## Lock de item

`Alt + clique esquerdo` num item marca ele com uma borda laranja. Item
travado nunca é movido pelo quick-stack, mesmo que exista um baú com aquele
item por perto. Útil pra manter munição, comida ou material de construção
sempre no seu inventário.

## Restock

Marque quais itens você quer manter sempre reabastecidos com
`Alt + Ctrl + clique esquerdo` (marca com um ponto azul no canto do slot).
Depois, `Ctrl + E` puxa desses baús próximos o suficiente de cada item
marcado pra completar uma pilha cheia no seu inventário — mesmo que você
esteja com zero daquele item. Exemplo: marca flecha e carne assada; toda
vez que aperta `Ctrl+E`, enche seu inventário com essas duas coisas a
partir do que tiver nos baús por perto.

## Craft e construção usando baús próximos

Ao craftar num banco de trabalho/forja/etc. ou ao construir (martelo em
mãos), o jogo passa a enxergar os itens dos baús próximos como se
estivessem no seu inventário — sem precisar abrir nenhum baú. A prioridade
é sempre consumir primeiro o que você já carrega; só busca no baú o que
faltar. A contagem mostrada na UI de craft também já soma os baús próximos.

## Estações automáticas

As 4 categorias de estação puxam material de baús próximos sozinhas e
guardam o resultado em vez de derrubar no chão. Cada uma tem seu próprio
raio e pode ser desligada individualmente (veja a tabela de configuração).
O EXP de perícia (Cooking) da coleta automática de comida vai sempre para
quem é dono da estação (normalmente quem construiu ou foi o primeiro a
interagir com ela) — em multiplayer, não necessariamente quem está por
perto ou estocou o ingrediente.

### Fogueira, tocha e lareira

Reabastece combustível (lenha, resina, etc.) puxando do baú mais próximo
até encher, uma unidade por vez. Não tem buffer/fila — é só um tanque de
combustível, então ele enche até o máximo sempre que há espaço.

### Fundição (forja de minério)

Puxa minério e combustível dos baús próximos automaticamente e guarda a
barra produzida no baú mais próximo com espaço. Se não sobrar espaço em
nenhum baú, o restante cai no chão normalmente (comportamento padrão do
jogo, sem duplicar nem perder o que já foi guardado).

### Carvoaria

Mesmo raio da fundição (configurável junto). Diferente da fogueira, tem
um **buffer** configurável (padrão: 3) — mantém só esse tanto de madeira na
fila interna dela em vez de encher tudo de uma vez, deixando a produção em
andamento terminar antes de pegar mais. Também tem um teto configurável de
carvão acumulado nos baús próximos: passando desse teto, ela para de puxar
madeira nova (sem interromper o que já está processando). O carvão
produzido primeiro tenta alimentar fundições próximas que estejam com pouco
combustível (estratégia configurável: priorizar a que tem menos combustível
ou a mais próxima); só o que sobra vai pro baú.

### Cozinha (espeto de fogueira, panela, etc.)

Puxa comida crua (e combustível próprio, se a estação usar) dos baús
próximos e cozinha sozinha. Quando o item termina de cozinhar, coleta
automaticamente e guarda no baú mais próximo — sem precisar interagir com a
estação pra tirar a comida pronta. A coleta automática passa pelo mesmo
caminho de código de uma interação manual, então o EXP de perícia e o
bônus de rendimento continuam funcionando normalmente (ver observação
sobre quem recebe o EXP acima).

## Alimentação automática de animais

Animais domesticáveis (javali, lobo, lox, e qualquer outro com o
componente de domesticação do próprio jogo) que estiverem com fome puxam
comida compatível de um baú próximo automaticamente — tanto durante o
processo de domesticação de um bicho selvagem quanto depois, pra manter a
reprodução ativa num curral. O mod cria o item de comida de verdade perto
do bicho (não faz o bicho "ficar alimentado" magicamente): o animal anda
até lá e come normalmente, com a mesma animação de sempre — só a origem da
comida (baú em vez de você jogando no chão manualmente) é automática. Cada
espécie só recebe o item que ela mesma aceita (a mesma lista que o jogo já
usa pra decidir o que aquele bicho come).

Atenção: essa automação também alimenta bichos selvagens ainda não
domados, ou qualquer criatura domesticável que não esteja em modo de
alerta/combate, desde que tenha um baú com comida compatível dentro do
raio — não distingue "esse eu quero alimentar" de "esse tá só passando
perto do baú".

## Configuração

Todas as opções ficam no [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/)
do BepInEx, divididas em quatro seções.

**Raios** (guardar/restock/craft-de-baú):

| Opção | Padrão | Descrição |
|---|---|---|
| `QuickStackRadius` | 20m | Raio em que quick-stack e restock procuram baús |
| `CraftingChestRadius` | 20m | Raio em que craft/construção considera itens de baús próximos |

**Estações** (raios e liga/desliga por comportamento):

| Opção | Padrão | Descrição |
|---|---|---|
| `FireplaceRadius` | 10m | Raio de fogueiras/tochas/lareiras |
| `SmelterKilnRadius` | 10m | Raio compartilhado entre fundição e carvoaria |
| `CookingStationRadius` | 10m | Raio das estações de cozinha |
| `FireplaceAutoRefuel` | ligado | Fogueiras puxam combustível automaticamente |
| `SmelterAutoRefuel` | ligado | Fundições puxam minério/combustível automaticamente |
| `SmelterAutoCollect` | ligado | Fundições guardam a produção no baú |
| `KilnAutoRefuel` | ligado | Carvoarias puxam madeira automaticamente |
| `KilnAutoCollect` | ligado | Carvoarias guardam/redirecionam o carvão produzido |
| `CookingStationAutoRefuel` | ligado | Estações de cozinha puxam comida crua/combustível automaticamente |
| `CookingStationAutoCollect` | ligado | Estações de cozinha coletam e guardam sozinhas |

**Carvoaria** (ajuste fino específico dela):

| Opção | Padrão | Descrição |
|---|---|---|
| `KilnWoodBuffer` | 3 | Nível de madeira mantido na fila interna (não é a capacidade máxima) |
| `KilnMaxCoalInChest` | 50 | Teto de carvão nos baús próximos antes de pausar reposição de madeira |
| `KilnFeedStrategy` | `LeastFuelFirst` | Como escolher qual fundição próxima alimentar primeiro: `LeastFuelFirst` (menos combustível) ou `Nearest` (mais próxima) |

**Animais** (alimentação automática):

| Opção | Padrão | Descrição |
|---|---|---|
| `AnimalFeederRadius` | 20m | Raio em que animais domesticáveis famintos procuram comida em baús próximos |
| `AnimalAutoFeed` | ligado | Animais domesticáveis puxam comida compatível de baús próximos automaticamente |

Cada opção também tem uma descrição própria dentro do Configuration
Manager.

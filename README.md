# SenacRun

Jogo 3D feito na Unity 6 (URP): um aluno sai da Estação Jurubatuba e precisa atravessar a avenida, a Marginal, passar pelo mercado e chegar ao Senac sem ser atropelado.

## Controles
- **WASD / setas**: andar
- **R**: reiniciar
- **Enter / Espaço**: jogar de novo na tela de fim

## Como montar o projeto
1. Crie um projeto Unity 6 (template 3D URP) ou abra um existente.
2. Copie a pasta `Assets/` deste repositório para dentro do projeto.
3. Em *Edit > Project Settings > Player > Active Input Handling*, selecione **Both**.
4. Menu **Tools > Senac Game > Gerar tudo (prefabs + fase 1)**.
5. Salve a cena (Ctrl+S) e dê Play.

## Estrutura
- `Assets/Scripts/` — scripts do jogo (aluno, veículos, spawner, câmera, GameManager com HUD e telas de fim)
- `Assets/Editor/SenacLevelBuilder.cs` — gera materiais, prefabs (aluno, carros, táxi, ônibus, caminhões, ciclista) e a fase inteira. O layout das faixas fica em `Fase1()`.

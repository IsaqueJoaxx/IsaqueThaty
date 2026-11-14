# NOCTURNE — IFM

Protótipo local para experimentar em partidas personalizadas. A Riot restringe ferramentas que tiram conclusões durante partidas; partida personalizada não constitui aprovação da Riot. Referência: https://support.riotgames.com/en-us/riot/events/third-party-applications

## Iniciar

Abra INICIAR.cmd. Usa o Python disponível neste computador. Em outro computador, instale Python com Tkinter e execute `py -3 -m pip install -r requirements.txt`, depois `py -3 nocturne_ifm.py`.

## Calibrar

1. Use o jogo em janela sem bordas no monitor principal. Mantenha o painel em um segundo monitor ou fora da área capturada; o painel precisa continuar visível para marcar aparições.
2. Clique em Selecionar área de combate e arraste sobre a região central do jogo. Exclua minimapa, HUD, chat e painel deste programa.
3. Com um campeão inimigo visível, clique em Calibrar barra e selecione a largura TOTAL da barra de vida, incluindo a parte vazia e sem incluir o retrato. Pode ajustar o número de pixels manualmente.
4. Selecione o minimapa inteiro. Clique na cópia do minimapa no painel onde você acabou de ver o jungler. Cada clique registra uma nova aparição. O aplicativo NÃO reconhece automaticamente o jungler.
5. Escolha o limite de vida (30% inicialmente) e clique em Iniciar. Teste também o som do Windows. Para interromper, clique novamente ou feche o painel.

## O que os resultados significam

O detector procura segmentos vermelhos horizontais com uma extensão escura à direita. Exige duas capturas consecutivas próximas e limita o som a um aviso a cada oito segundos. A porcentagem depende da largura calibrada. Não é reconhecimento de campeões: tropas, monstros, textos e outras cores podem causar falsos positivos; barras segmentadas, movimento, escudos e daltonismo podem causar omissões. Vida extremamente baixa com menos de seis pixels vermelhos não é detectada. Apenas inimigos presentes na câmera podem ser avaliados. Não decide se você deve gankar ou usar R, nem calcula alcance, dano ou segurança.

O círculo laranja mostra uma possibilidade geométrica em torno da posição marcada manualmente, com crescimento configurável em fração da largura do minimapa por segundo. Não é um modelo de rota e não usa velocidade do campeão, terreno, recall ou teleporte; portanto não garante conter a posição real. Após 45 segundos a informação expira. O coeficiente inicial é ilustrativo.

Não lê memória, tráfego nem arquivos do jogo; não executa comandos no jogo, usa atalhos globais ou envia capturas. As imagens são processadas em memória. Salva somente largura da barra e limite em config.json. As regiões são recalibradas a cada abertura para evitar coordenadas antigas.

## Validação

O detector possui teste sintético em test_detector.py. Não foi validado numa sessão de League of Legends. Antes de confiar nos avisos, compare visualmente os resultados com campeões e tropas em uma partida de teste. Recalibre após mudar resolução ou escala da interface.

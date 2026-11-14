"""
Script para destacar campos variáveis no memorial descritivo base.
Adiciona fundo amarelo nos textos que devem mudar a cada novo projeto.
"""
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

INPUT_XML = r'D:\Claude ai\memorial_unpacked\word\document.xml'
OUTPUT_XML = r'D:\Claude ai\memorial_unpacked\word\document.xml'

HIGHLIGHT_TAG = '<w:highlight w:val="yellow"/>'

# -----------------------------------------------------------------------
# Padrões variáveis (ordem: mais específicos primeiro)
# -----------------------------------------------------------------------
VARIABLE_PATTERNS = [
    # Código do projeto
    r'PRJ-\d{6}-EXE-MMD-\d{4}-REV\d{2}',
    r'PRJ-244229[A-Z0-9\-]*',
    # Datas
    r'JANEIRO/2026',
    r'JANEIRO / 20\s*2\s*6',
    r'01\s*/202\s*6',
    r'JUNHO / 2019',
    r'Janeiro/2026',
    r'Janeiro / 2026',
    # Nome do projeto
    r'VIADUTO SEDE',
    r'Viaduto Sede',
    # Localização
    r'BRUMADINHO/MG',
    r'Brumadinho/MG',
    r'Brumadinho',
    r'BRUMADINHO',
    # Empresa contratada e cliente
    r'Compasso Engenharia',
    r'COMPASSO ENGENHARIA',
    r'Prefeitura Municipal de Brumadinho',
    r'AV\. BAR[ÃA]O HOMEM DE MELO[^<]+',
    r'contato@grupoprojetaengenharia\.com\.br',
    r'\(31\) 3347-4405[^<]*',
    # Equipe técnica
    r'Juliana Gon[çc]alves Oliveira',
    r'Fabiane Ferreira Ganda',
    r'Jefferson Fridel Martins',
    r'M[áa]rcio Luiz Provetti Silva',
    r'Gislaine Machado Tadim de Castro',
    r'CREA 239\.787/D',
    r'239\.787/D',
    # Responsáveis técnicos (cargos e siglas de revisão)
    r'\bGMT\b',
    r'\bFFG\b',
    r'\bJFM\b',
    r'\bJGO\b',
    # Dimensões específicas do projeto
    r'274,51 metros',
    r'274,51',
    # População e dados locais
    r'38\.915',
    r'uma popula[çc][ãa]o de estimada de [0-9\.,]+ pessoas',
    # Nomes dos alinhamentos/vias do projeto
    r'Rua Irineu Lamounier',
    r'Rua Presidente Kennedy',
    r'Novo Viaduto',
    r'MG-040',
    # Legislação municipal específica (BH)
    r'Decreto Municipal n[º°]\s*13\.842/10[^<]*',
    r'Decreto Municipal n[º°]\s*14\.060/10[^<]*',
    r'Lei Municipal n[º°]\s*8\.616/03[^<]*',
    r'Lei Municipal n[º°]\s*9\.068/05[^<]*',
    r'Lei Municipal n[º°]\s*9\.725/09[^<]*',
    r'Lei Municipal n[º°]\s*10\.522/12[^<]*',
    r'Lei Municipal n[º°]\s*10\.534/12[^<]*',
    r'Lei Municipal n[º°]\s*9\.505/08[^<]*',
    r'Portaria SMMA/SMPU[^<]+',
    r'Portaria SMPU/SMMA[^<]+',
    r'Belo Horizonte:',
    # Código dos desenhos
    r'PRJ-244229-EXE-GEO-\d{4}-REV\d{2}',
    r'PRJ-244229-EXE-TER-\d{4}-REV\d{2}',
    r'PRJ-244229-EXE-DRE-\d{4}-REV\d{2}',
    r'PRJ-244229-EXE-PAV-\d{4}-REV\d{2}',
    r'PRJ-244229-EXE-SIN-\d{4}-REV\d{2}',
    r'PRJ-244229-EXE-\w+-\d{4}-REV\d{2}',
]

# Combinar em um único regex com grupos nomeados
combined_pattern = '(' + '|'.join(VARIABLE_PATTERNS) + ')'
combined_re = re.compile(combined_pattern)


def get_rpr(run_xml):
    """Extrai o bloco <w:rPr>...</w:rPr> de um run."""
    m = re.search(r'<w:rPr>.*?</w:rPr>', run_xml, re.DOTALL)
    return m.group(0) if m else ''


def add_highlight(rpr):
    """Adiciona destaque amarelo ao rPr."""
    if rpr:
        if '<w:highlight' in rpr:
            return re.sub(r'<w:highlight[^/]*/>', HIGHLIGHT_TAG, rpr)
        return rpr.replace('<w:rPr>', '<w:rPr>' + HIGHLIGHT_TAG, 1)
    return '<w:rPr>' + HIGHLIGHT_TAG + '</w:rPr>'


def make_run(rpr, text, original_run_attrs=''):
    """Cria um novo bloco <w:r> com rPr e texto."""
    needs_preserve = text != text.strip() or '  ' in text
    if needs_preserve:
        t_tag = f'<w:t xml:space="preserve">{text}</w:t>'
    else:
        t_tag = f'<w:t>{text}</w:t>'
    return f'<w:r>{rpr}{t_tag}</w:r>'


def process_run(run_xml):
    """Processa um run: divide e adiciona highlight nos trechos variáveis."""
    # Extrair texto do run
    t_match = re.search(r'<w:t[^>]*>(.*?)</w:t>', run_xml, re.DOTALL)
    if not t_match:
        return run_xml

    text = t_match.group(1)

    # Verificar se há algum padrão variável
    if not combined_re.search(text):
        return run_xml

    rpr = get_rpr(run_xml)
    highlighted_rpr = add_highlight(rpr)

    result = ''
    last_end = 0
    for m in combined_re.finditer(text):
        before = text[last_end:m.start()]
        match_text = m.group(0)
        after_start = m.end()

        if before:
            result += make_run(rpr, before)
        result += make_run(highlighted_rpr, match_text)
        last_end = after_start

    remaining = text[last_end:]
    if remaining:
        result += make_run(rpr, remaining)

    return result


def process_document(content):
    """Processa o documento inteiro, substituindo runs com campos variáveis."""
    # Encontrar todos os runs e processar cada um
    def replace_run(m):
        return process_run(m.group(0))

    # Processar apenas runs normais (não dentro de rPr, pPr etc.)
    # Um run começa com <w:r> ou <w:r  (com atributos) e termina com </w:r>
    result = re.sub(
        r'<w:r(?:\s[^>]*)?>(?:(?!<w:r[>\s]).)*?</w:r>',
        replace_run,
        content,
        flags=re.DOTALL
    )
    return result


# -----------------------------------------------------------------------
# Processar também caixas de texto (txbxContent) - capa do documento
# -----------------------------------------------------------------------
def process_textbox_runs(content):
    """Destaca runs dentro de caixas de texto (capa)."""
    def replace_in_txbx(m):
        inner = re.sub(
            r'<w:r(?:\s[^>]*)?>(?:(?!<w:r[>\s]).)*?</w:r>',
            lambda r: process_run(r.group(0)),
            m.group(0),
            flags=re.DOTALL
        )
        return inner

    return re.sub(
        r'<w:txbxContent>.*?</w:txbxContent>',
        replace_in_txbx,
        content,
        flags=re.DOTALL
    )


# -----------------------------------------------------------------------
# Execução
# -----------------------------------------------------------------------
print("Lendo documento...", flush=True)
with open(INPUT_XML, 'r', encoding='utf-8') as f:
    content = f.read()

print("Processando runs do corpo do documento...", flush=True)
content = process_document(content)

print("Processando caixas de texto (capa)...", flush=True)
content = process_textbox_runs(content)

print("Salvando resultado...", flush=True)
with open(OUTPUT_XML, 'w', encoding='utf-8') as f:
    f.write(content)

print("Concluído com sucesso!", flush=True)

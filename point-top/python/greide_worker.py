"""Ponte do gerador de greide com o painel: protocolo JSON por linha, como worker.py."""
import contextlib
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent / "greide"))
SAIDA = sys.stdout


def emit(kind, **data):
    print(json.dumps(dict(kind=kind, **data), ensure_ascii=False, allow_nan=False), file=SAIDA, flush=True)


def main():
    import demo
    import gerador

    pedido = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    if pedido.get("demo"):
        pedido["eixo"], pedido["levantamento"], pedido["perfilIri"] = demo.gerar(pedido["demoFolder"])
    params = pedido.get("params") or {}
    pasta, resultado, arquivos = gerador.executar(
        pedido["eixo"], pedido["levantamento"], pedido["output"],
        parametros=params.get("projeto"), limites=params.get("limites"),
        perfil_iri=pedido.get("perfilIri"), opcoes_iri=params.get("iri"),
        sintetico=bool(pedido.get("demo")),
        progresso=lambda v: emit("progress", value=round(float(v), 2)),
    )
    emit("done", folder=str(pasta), result=resultado, files=arquivos)


if __name__ == "__main__":
    try:
        # Qualquer print perdido vai para o erro padrão e não corrompe o protocolo.
        with contextlib.redirect_stdout(sys.stderr):
            main()
    except Exception as exc:
        emit("error", message=str(exc))
        sys.exit(1)

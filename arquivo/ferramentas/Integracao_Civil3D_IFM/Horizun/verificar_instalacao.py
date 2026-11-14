import hashlib
import json
import os
from pathlib import Path
import subprocess
import tomllib

root = Path(__file__).resolve().parent
config = Path.home() / '.codex/config.toml'
before = tomllib.loads((root / 'codex-config-antes-horizun.toml').read_text(encoding='utf-8-sig'))
after = tomllib.loads(config.read_text(encoding='utf-8-sig'))
entry = after.get('mcp_servers', {}).get('horizun-civil3d')
assert entry and entry.get('enabled', True), 'Servidor não registrado ou desabilitado'
preserved = dict(after)
preserved['mcp_servers'] = dict(after['mcp_servers'])
preserved['mcp_servers'].pop('horizun-civil3d')
if not preserved['mcp_servers'] and 'mcp_servers' not in before:
    preserved.pop('mcp_servers')
def normalize(data):
    data = json.loads(json.dumps(data))
    for server in data.get('mcp_servers', {}).values():
        if server.get('args') == []:
            server.pop('args')
    return data

assert normalize(preserved) == normalize(before), 'Outra configuração do Codex foi alterada'

product = Path(os.environ['LOCALAPPDATA']) / 'Programs/Horizun/Civil3D-MCP'
manifest = json.loads((product / 'manifest.json').read_text(encoding='utf-8-sig'))
count = 0
for field, path_field in [('bundle_sha256', 'bundle_dir'), ('server_sha256', 'server_dir')]:
    for relative, expected in manifest[field].items():
        path = Path(manifest[path_field]) / relative
        actual = hashlib.sha256(path.read_bytes()).hexdigest()
        assert actual.lower() == expected.lower(), f'Hash divergente: {relative}'
        count += 1

messages = [
    {'jsonrpc': '2.0', 'id': 0, 'method': 'initialize', 'params': {'protocolVersion': '2025-06-18', 'capabilities': {}, 'clientInfo': {'name': 'verificacao-ifm', 'version': '1'}}},
    {'jsonrpc': '2.0', 'method': 'notifications/initialized'},
    {'jsonrpc': '2.0', 'id': 1, 'method': 'tools/list', 'params': {}},
    {'jsonrpc': '2.0', 'id': 2, 'method': 'tools/call', 'params': {'name': 'horizun_c3d_health', 'arguments': {}}},
]
result = subprocess.run([entry['command']], input='\n'.join(json.dumps(m) for m in messages) + '\n', text=True, encoding='utf-8', capture_output=True, timeout=45)
responses = {m['id']: m for line in result.stdout.splitlines() if 'id' in (m := json.loads(line))}
assert 0 in responses and 'result' in responses[0], 'Inicialização MCP falhou'
assert 1 in responses and 'result' in responses[1], 'Catálogo MCP não respondeu'
tools = responses[1]['result']['tools']
assert any(t['name'] == 'horizun_c3d_health' for t in tools)
report = {'version': manifest['version'], 'years': manifest['civil3d_years'], 'files_verified': count,
          'other_codex_settings_preserved': True, 'mcp_initialize': 'ok', 'tool_count': len(tools),
          'health': responses.get(2, {}).get('result', {})}
(root / 'verificacao-instalada.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(report, ensure_ascii=False, indent=2))

# (907) c78lg：台帳の錨（file:line:名）を木で引く共通の 1 本（build907.py と patch_overview907.py が呼ぶ）。
#  'C:名'＝Coefficients.cs の public の宣言／'D:名'＝Diplomacy.cs・'W:'＝World.cs・'L:'＝Landmass.cs・'M:'＝MultiNationSim.cs のメソッドか欄の定義／
#  'ファイル:行:字'＝その行に字が有るかを確かめて返す。
import re, subprocess
_repo = _tree = None; _cache = {}; _tracked = []
PFX = {'C': 'Coefficients.cs', 'D': 'Diplomacy.cs', 'W': 'World.cs', 'L': 'Landmass.cs', 'M': 'MultiNationSim.cs'}
def init(repo, tree):
    global _repo, _tree, _tracked
    _repo, _tree = repo, tree
    _tracked = subprocess.run(['git', '-C', repo, 'ls-tree', '-r', '--name-only', tree], capture_output=True, text=True).stdout.split('\n')
def path_of(fn):
    c = [p for p in _tracked if p.split('/')[-1] == fn]
    s = [p for p in c if p.startswith('PriceOfBread.SimCore/')]
    return (s or c)[0]
def lines(path):
    if path not in _cache:
        _cache[path] = subprocess.run(['git', '-C', _repo, 'show', f'{_tree}:{path}'], capture_output=True).stdout.decode('utf-8-sig', 'replace').split('\n')
    return _cache[path]
def resolve(a):
    m = re.match(r'^([A-Za-z]+\.cs):(\d+):(.+)$', a)
    if m:
        fn, ln, tok = m.group(1), int(m.group(2)), m.group(3)
        L = lines(path_of(fn))
        assert tok in L[ln - 1], ('行に字が無い', a, L[ln - 1][:80])
        return f'{fn}:{ln}:{tok}'
    k, nm = a.split(':', 1)
    fn = PFX[k]; L = lines(path_of(fn))
    if k == 'C':
        pat = re.compile(r'public[^=(]*\b' + re.escape(nm) + r'\b\s*(=|\{)')
    else:
        pat = re.compile(r'(private|public|internal|static|readonly)[^=;]*\b' + re.escape(nm) + r'\b\s*(\(|=|;|\{|,)')
    hit = [i for i, l in enumerate(L, 1) if pat.search(l.split('//')[0])]
    assert hit, ('宣言が無い', a)
    return f'{fn}:{hit[0]}:{nm}'

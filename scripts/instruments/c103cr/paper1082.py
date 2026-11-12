# (1082) 紙の台本：world.json（今の木）に data1082 の値を記憶の上で当て、前と後を数える（エンジンを呼ばない・world.json を自分で読む）。
#   使い方: python -X utf8 paper1082.py <world.json>
#   ①空の州 ②国ごとの多数の文化と宗教（州の share の重み＝構築の人口の比の近似）③中核の割合が 0 になる州（多数と違う札・空は 1）
#   ④CultureRealms の表の文化の州の数と過半 ⑤宗教の完全一致の保護の割合（国の多数の宗教が X の国から見た、相手の国の X の州の share の和）
import json, sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from data1082 import DATA

W = json.load(open(sys.argv[1], encoding='utf-8'))
NAT = W['nations']
REALMS = ["イタリア", "ドイツ", "アラブ", "マラーター", "漢"]   # CultureRealms.Table の文化（CultureRealms.cs:28〜:40 を写した）

def apply(nats):
    out = json.loads(json.dumps(nats, ensure_ascii=False))
    hit = 0
    for (nn, city, cu, re_, _) in DATA:
        for n in out:
            if n['name'] != nn: continue
            for p in n['provinces']:
                if p['city'] == city:
                    assert p.get('culture', '') == '' and p.get('religion', '') == '', (nn, city)
                    p['culture'] = cu; p['religion'] = re_; hit += 1
    assert hit == len(DATA), hit
    return out

def majority(n, key):
    acc = collections.defaultdict(float)
    for p in n['provinces']:
        v = p.get(key, '')
        if v: acc[v] += p['share']
    if not acc: return ''
    return sorted(acc.items(), key=lambda kv: (-kv[1], kv[0]))[0][0]

def report(tag, nats):
    print(f"== {tag}")
    empty = [(n['name'], p['city'], p.get('culture', ''), p.get('religion', '')) for n in nats for p in n['provinces'] if not p.get('culture') or not p.get('religion')]
    print(f"①空の州（文化か宗教が空）{len(empty)}・文化が空 {sum(1 for e in empty if not e[2])}・宗教が空 {sum(1 for e in empty if not e[3])}")
    zero = []
    for n in nats:
        m = majority(n, 'culture')
        for p in n['provinces']:
            c = p.get('culture', '')
            if c and m and c != m: zero.append((n['name'], p['city'], c, m))
    print(f"③中核の割合が 0 の州（全ての国）{len(zero)}")
    cnt = collections.Counter(p.get('culture', '') for n in nats for p in n['provinces'])
    print("④CultureRealms：" + "・".join(f"{r} {cnt[r]} 州（過半 {cnt[r] // 2 + 1}）" for r in REALMS))
    return zero

before = NAT; after = apply(NAT)
zb = report("前（今の world.json）", before); za = report("後（data1082 を当てた）", after)
nb = {(a, b) for (a, b, _, _) in zb}; na = {(a, b) for (a, b, _, _) in za}
print("③の差（後にだけある州）：" + "・".join(f"{a}/{b}（{c}≠多数 {m}）" for (a, b, c, m) in za if (a, b) not in nb))
print("③の差（前にだけある州）：" + "・".join(f"{a}/{b}" for (a, b) in nb - na))
print("②多数の文化・宗教の前→後が変わった国：" + ("・".join(f"{b['name']} {majority(b,'culture')}/{majority(b,'religion')}→{majority(a,'culture')}/{majority(a,'religion')}" for b, a in zip(before, after) if (majority(b,'culture'), majority(b,'religion')) != (majority(a,'culture'), majority(a,'religion'))) or "なし"))
touched = sorted({nn for (nn, *_r) in DATA})
print("②触った国の多数（後）：" + "・".join(f"{n['name']} {majority(n,'culture')}/{majority(n,'religion')}" for n in after if n['name'] in touched))
# ⑤保護の割合（Diplomacy.ProtectorGrievance の形＝相手の州のうち保護者の宗教の州の人口の割合・ここは share で近似）
for rel in ["土着信仰", "イスラム", "正教", "プロテスタント"]:
    prot = [n['name'] for n in after if majority(n, 'religion') == rel]
    rows = []
    for n_b, n_a in zip(before, after):
        if majority(n_a, 'religion') == rel: continue
        sb = sum(p['share'] for p in n_b['provinces'] if p.get('religion') == rel) / max(1e-12, sum(p['share'] for p in n_b['provinces']))
        sa = sum(p['share'] for p in n_a['provinces'] if p.get('religion') == rel) / max(1e-12, sum(p['share'] for p in n_a['provinces']))
        if abs(sa - sb) > 1e-12: rows.append(f"{n_a['name']} {sb:.4f}→{sa:.4f}")
    print(f"⑤宗教 {rel}：多数がこの宗教の国 {len(prot)}（{'・'.join(prot) or 'なし'}）／保護の割合が変わる相手 " + ("・".join(rows) or "なし"))

# (1111) IL の門の比較：ilgate.sh が書いた before.txt・after.txt ほかを読み、各門の合否を出す（赤があれば終了コード 1）。
import re, sys, difflib, os
d = sys.argv[1]
def rd(n): return open(os.path.join(d, n), encoding='utf-8').read().split('\n')
def types(lines):
    cur, out = None, {}
    for l in lines:
        if l.startswith('T '): cur = l[2:]; out[cur] = []
        elif cur is not None and not l.startswith('#'): out[cur].append(l)
    return out
def heads(lines): return [l for l in lines if l.startswith('# ') and not l.startswith('# types')]
b, a, s, dp = rd('before.txt'), rd('after.txt'), rd('after_simcore_only.txt'), rd('after_display_only.txt')
tb, ta, ts, td = types(b), types(a), types(s), types(dp)
bad = 0
def gate(name, ok, extra=''):
    global bad
    print(('緑 ' if ok else '赤 ') + name + (' ' + extra if extra else ''))
    if not ok: bad += 1
# 型の集合
gate('G1a 型の集合が前＝後（SimCore ∪ Display）', set(tb) == set(ta), f'前 {len(tb)} 後 {len(ta)} 差 {sorted(set(tb) ^ set(ta))[:8]}')
# 型ごとの写し
diff = [t for t in tb if t in ta and tb[t] != ta[t]]
gate('G1b 型ごとの IL・署名・定数・属性の写しが一致', not diff, f'違う型 {len(diff)} 例 {diff[:5]}')
for t in diff[:3]:
    for l in list(difflib.unified_diff(tb[t], ta[t], lineterm='', n=0))[:12]: print('   ', l[:200])
# 後の SimCore.dll に Display の型 0
disp_names = {t for t in td if t.startswith('PriceOfBread.SimCore.Display')}
leak = [t for t in ts if t.startswith('PriceOfBread.SimCore.Display')]
gate('G1c 後の SimCore.dll に Display の型が 0', not leak and len(disp_names) > 0, f'SimCore 側の Display 型 {len(leak)}・Display.dll 側 {len(disp_names)}')
# Display の型の名の列
nb = sorted(t for t in tb if t.startswith('PriceOfBread.SimCore.Display'))
na = sorted(t for t in td if t.startswith('PriceOfBread.SimCore.Display'))
gate('G2 Display の型の名の列が前後で同じ', nb == na and len(nb) > 0, f'前 {len(nb)} 後 {len(na)}')
# アセンブリの属性：差が足した InternalsVisibleTo だけ
hb, ha = heads(b), heads(a)
ab = [l for l in hb if l.startswith('# asmattr')]
aa = [l for l in ha if l.startswith('# asmattr')]
# after.txt の asmattr は SimCore と Display の和＝SimCore だけの版で比べる
asm_s = [l for l in heads(s) if l.startswith('# asmattr')]
added = sorted(set(asm_s) - set(ab)); removed = sorted(set(ab) - set(asm_s))
ivt = lambda l: 'InternalsVisibleToAttribute' in l
gate('G1d アセンブリの属性の差が足した InternalsVisibleTo だけ（SimCore.dll 単体）', not removed and all(ivt(l) for l in added), f'足 {len(added)}（{sum(ivt(l) for l in added)} が IVT）・消 {len(removed)}')
# 埋め込みの資源
rb = [l for l in hb if l.startswith('# resource')]; rs = [l for l in heads(s) if l.startswith('# resource')]
gate('G1e 埋め込みの資源が同じ（SimCore.dll 単体）', rb == rs, f'前 {len(rb)} 後 {len(rs)}')
# 合成の型の名簿（アセンブリごとに持つ）
eb = [l for l in hb if l.startswith('# embedded')]; es = [l for l in heads(s) if l.startswith('# embedded')]
gate('G1f SimCore.dll の合成の型の名簿が同じ', eb == es, f'前 {len(eb)} 後 {len(es)}')
print('# Display.dll の合成の型の名簿', [l for l in heads(dp) if l.startswith('# embedded')])
sys.exit(1 if bad else 0)

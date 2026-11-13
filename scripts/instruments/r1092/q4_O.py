# (1092) 問い 4：開幕の O（外への力）＝max(0, L−人口K×q)・q＝ΣL÷Σ人口（四半期 0・InstrumentsRulePower.cs の写し＝(1083) report の式）。
# 腕 A＝四半期 0 の実際（rulepower の oNationIndex と照合）・腕 B＝L0×(装備 q19÷装備 warm)〔stock の全ての国・落ちの後の装備の水準を開幕に当てた紙〕・腕 C＝L を四半期 20 の実際に置く（人口は四半期 0）。B・C は q を置いた L から取り直す。どれも紙の近似。
# 追記：腕 D＝L0×(装備 q39÷warm)・腕 E＝L0×(兵 q39÷warm)・腕 F＝stock の q39 の L（清の落ちが四半期 19 から始まる＝q19・q20 は過渡の途中という指摘を受けて足した）。
# 使い方: python -X utf8 -I q4_O.py <表のフォルダ> > q4_out.txt
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]
res = collections.defaultdict(list); top = collections.defaultdict(collections.Counter); chk = []; onat = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1, 11):
    pop = collections.defaultdict(float); L0 = {}; O0 = {}; L20 = {}
    with open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q > 20: break
            n = c[ix['nation']]
            if q == 0:
                pop[n] += float(c[ix['popK']]); L0.setdefault(n, float(c[ix['lNationIndex']])); O0.setdefault(n, float(c[ix['oNationIndex']]))
            elif q == 20: L20.setdefault(n, float(c[ix['lNationIndex']]))
    ew = {}; e19 = {}; e39 = {}; sw = {}; s39 = {}; L39 = {}
    with open(os.path.join(d, f'stock_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t')
            n = c[ix['nation']]
            if c[0] == 'warm': ew[n] = float(c[ix['capitalEquipFillShare']]); sw[n] = float(c[ix['soldierK']])
            elif c[0] == 'q19': e19[n] = float(c[ix['capitalEquipFillShare']])
            elif c[0] == 'q39': e39[n] = float(c[ix['capitalEquipFillShare']]); s39[n] = float(c[ix['soldierK']]); L39[n] = float(c[ix['landPowerIndex']])
            elif c[0] == 'q40': break
    P = sum(pop.values())
    def run(name, L):
        T = sum(L.get(n, 0.0) for n in pop); q = T / P
        O = {n: max(0.0, L.get(n, 0.0) - pop[n] * q) for n in pop}
        SO = sum(O.values())
        res[name + '_O>0の国'].append(sum(1 for v in O.values() if v > 0)); res[name + '_ΣO÷ΣL'].append(SO / T if T > 0 else float('nan'))
        for n, v in sorted(O.items(), key=lambda kv: -kv[1])[:5]:
            if v > 0: top[name][n] += 1
        for n in ('フランス', 'オーストリア', '清', 'マラータ同盟', '日本', 'ロシア', 'イギリス', 'スペイン', 'プロイセン'):
            onat[name][n].append(O[n] / SO if SO > 0 else 0.0)
        return O
    OA = run('A', L0)
    chk.append(max(abs(OA[n] - O0[n]) / max(1e-9, abs(O0[n]) + 1e-9) for n in pop if O0[n] > 1e-6))
    run('B', {n: L0[n] * (e19[n] / ew[n]) if n in e19 and n in ew and ew[n] > 1e-12 else L0[n] for n in pop})
    run('C', {n: L20.get(n, 0.0) for n in pop})
    run('D装備q39', {n: L0[n] * (e39[n] / ew[n]) if n in e39 and n in ew and ew[n] > 1e-12 else L0[n] for n in pop})
    run('E兵q39', {n: L0[n] * (s39[n] / sw[n]) if n in s39 and n in sw and sw[n] > 1e-12 else L0[n] for n in pop})
    run('F実際q39', {n: L39.get(n, 0.0) for n in pop})
print(f'腕 A の照合：計器の oNationIndex との最大の相対差（O＞0 の国）＝種ごとの最大の中央 {st.median(chk):.3g}')
for k in sorted(res):
    a = res[k]; print(f'{k}\t中央 {st.median(a):.3g}\t幅 {min(a):.3g}〜{max(a):.3g}')
for name in ('A', 'B', 'C', 'D装備q39', 'E兵q39', 'F実際q39'):
    print(f'腕 {name} の O の上位 5 に入った種の数：' + '・'.join(f'{n} {c}' for n, c in top[name].most_common(10)))
    print(f'腕 {name} の ΣO の取り分（10 種の中央）：' + '・'.join(f'{n} {st.median(v):.2f}' for n, v in onat[name].items()))

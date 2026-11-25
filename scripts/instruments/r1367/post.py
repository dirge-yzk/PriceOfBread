# (1367) 後処理：T8 の型の細分（規則で決める・目で読んだ例外は書かない）と、核の出力の〈判断の読み手〉の数
import os, re, sys, collections, json
OUT = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/r1367_work/out'
T = chr(9); NL = chr(10)
rows = [l.rstrip(NL).split(T) for l in open(os.path.join(OUT, 'T8_hand_blend_decay.tsv'), encoding='utf-8') if l.strip() and not l.startswith('#')]
hdr, rows = rows[0], rows[1:]
def subtype(r):
    node, site, mem, form, op, tag, rhs = r[:7]
    if mem == 'Blend' or re.search(r'/\s*(t1|rest)\b|debtBefore|debt0|/\s*System\.Math\.Max\(1e-9', rhs):
        return '加重平均（人や債務が移る時の構成の保存）'
    if re.search(r'^1\.0 \+|\(1\.0 \+ g', rhs) or '1.0 + c.' in rhs:
        return '増加の倍（減衰でない）'
    if 'approach' in form:
        return '時間の寄り（Toward と同じ形を手で書いた）'
    if re.search(r'Keep|Decay|ReplaceRate|ReplacementRate|Smoothing|rollover|Ema|adoptRate|catchup|TechGrowthRate|0\.75 \*|PortDecay|TransportDecay|SeizureChill|rate\)', rhs):
        if 'x+=k*(t-x)' in form:
            return '時間の寄り（Toward と同じ形を手で書いた）'
        if '+' not in rhs and 'TechLevel' not in node:
            return '時間の減衰（毎期の keep か率）'
        if 'blend' in form or 'leaky' in form or 'approach' in form or re.search(r'\(1\.0 - a\)|0\.75 \*|rollover|adoptRate|TechGrowthRate', rhs):
            return '時間の寄り（Toward と同じ形を手で書いた）' if 'leaky' not in form or '0.75' in rhs else '漏れのある積み（keep×x＋入り）'
        return '時間の減衰（毎期の keep か率）'
    if 'leaky' in form:
        return '漏れのある積み（keep×x＋入り）'
    return '一度きりの倍（事件・支払いの比・按分）'
cnt = collections.Counter()
with open(os.path.join(OUT, 'T8b_hand_blend_decay_typed.tsv'), 'w', encoding='utf-8', newline=NL) as fo:
    fo.write(T.join(['型（規則で決めた）'] + hdr) + NL)
    for r in rows:
        st = subtype(r); cnt[(st, r[5])] += 1
        fo.write(T.join([st] + r) + NL)
    fo.write(NL + '# 型ごとの数（核・候補・その他）' + NL)
    for (st, tag), n in sorted(cnt.items()): fo.write(T.join(['#', st, tag or 'その他', str(n)]) + NL)

# 核の出力のうち〈判断の読み手〉（写し・検査・表示の材料・構築を除く）
EXCL = {'Snapshot': '表示の材料〈推〉', 'Check': '不変条件の検査', 'Capture': '盤面の写し', 'RestoreTo': '盤面の写し', 'World': '構築', 'SpawnNation': '建国の写し', 'Blend': '人と一緒に運ぶ（書き手の一部）'}
rd = [l.rstrip(NL).split(T) for l in open(os.path.join(OUT, 'T4_readers_core.tsv'), encoding='utf-8')][1:]
c_all = collections.Counter(); c_judge = collections.Counter(); c_ex = collections.defaultdict(collections.Counter)
for r in rd:
    if len(r) < 3: continue
    nd, mem = r[0], r[2]
    c_all[nd] += 1
    if mem in EXCL: c_ex[nd][EXCL[mem]] += 1
    else: c_judge[nd] += 1
with open(os.path.join(OUT, 'T1b_outputs_judgement.tsv'), 'w', encoding='utf-8', newline=NL) as fo:
    fo.write(T.join(['節点', '出力の全数(T4)', '判断の読み手', '除いた物']) + NL)
    for nd in c_all:
        fo.write(T.join([nd, str(c_all[nd]), str(c_judge[nd]), ' '.join(f'{k}{v}' for k, v in c_ex[nd].items())]) + NL)
print(open(os.path.join(OUT, 'T1b_outputs_judgement.tsv'), encoding='utf-8').read())
for (st, tag), n in sorted(cnt.items()): print(st, tag, n)

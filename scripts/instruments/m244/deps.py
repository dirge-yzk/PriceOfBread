"""(244) fullS の注入の連鎖（subprocess で呼ぶ別の注入）を洗い出し、scripts/instruments に無いものを挙げる。
python deps.py <旧の一時フォルダ> [copy]   copy を付けると旧の一時フォルダから足りないものを写す。
同じフォルダの renscripts.py（名前の一意な改名＝`.旧名` だけ）・renamb.py（曖昧な名前を受け手の型で分けて改名）・sub.py（目印の文字列の置き換え）・ren.py（C# の名前の置き換え）は (244) で線U の改名に注入を合わせた道具。"""
import re, os, sys, shutil
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OLD = sys.argv[1]
copy = len(sys.argv) > 2
# (248) (245) で消した注入 gate・wpn・t186 は外した（旧の一時フォルダから書き戻さない）。
start = ("s3b/inj_s3a.py m149/inj_disc.py m190/inj_navy_n.py m160/inj_need.py army/inj_fc3.py "
         "m194/inj_mil8x.py m190/fix_mil4.py cas156/inj_cas.py m168/inj_d165.py m190/inj_war171n.py m177/inj_col177.py "
         "m181/inj_war181.py m207/inj_fin181y.py m183/inj_tier.py m195/inj_need183x.py m188/inj_sec188.py m184/inj_serf184.py m186/inj_g186.py "
         "m201/inj_c201.py m205/inj_fx205.py m190/inj_nvy190n.py need/inj_mig.py m199/inj_cnl199.py m194/inj_mob194.py "
         "m195/inj_nvq195.py m178/inj_pow178.py m185/inj_cap185.py m206/inj_c206.py m203/inj_t203.py m204/inj_d204.py m204/inj_rv204.py m208/inj_s208.py").split()
seen = set()
todo = list(start)
missing = []
while todo:
    f = todo.pop()
    if f in seen:
        continue
    seen.add(f)
    p = os.path.join(REPO, f)
    if not os.path.exists(p):
        missing.append(f)
        if copy and os.path.exists(os.path.join(OLD, f)):
            os.makedirs(os.path.dirname(p), exist_ok=True)
            shutil.copyfile(os.path.join(OLD, f), p)
        if not os.path.exists(p):
            continue
    s = open(p, encoding='utf-8').read()
    for m in re.finditer(r"os\.path\.join\(\s*sp\s*,\s*'([^']+)'\s*,\s*'([^']+\.py)'\)", s):
        todo.append(m.group(1) + '/' + m.group(2))
    for m in re.finditer(r"['\"]([a-z0-9_]+/[A-Za-z0-9_]+\.py)['\"]", s):
        todo.append(m.group(1))
print('closure', len(seen))
print('missing', missing)
print('still missing', [f for f in missing if not os.path.exists(os.path.join(REPO, f))])

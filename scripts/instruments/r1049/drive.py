# (1049) E1 の計時の台本：ABC を 3 周・1 本ずつ・45 分で打ち切り・終わり（例外でも）に e1.mark。
#   呼び方：python -X utf8 -I drive.py   （計り台 PobBench1049 は D:/PoB_work/r1049_work/bench を art/ へ隔離ビルドした物）
import os, subprocess, time
W = 'D:/PoB_work/r1049_work'
EXE = W + '/art/bin/Bench/release/PobBench1049.exe'
OUT = W + '/measurements/m1049_20261111/runs.txt'
CAP = 45 * 60
PS = ("Get-Process | ForEach-Object { try { '' + $_.Id + '`t' + $_.ProcessName + '`t' + $_.TotalProcessorTime.TotalSeconds } catch {} }")

def snap():
    r = subprocess.run(['powershell', '-NoProfile', '-Command', PS], capture_output=True, text=True, encoding='utf-8', errors='replace')
    d = {}
    for ln in r.stdout.splitlines():
        p = ln.split('\t')
        if len(p) == 3:
            try: d[(int(p[0]), p[1])] = float(p[2])
            except ValueError: pass
    return d

def cpu_total():
    r = subprocess.run(['powershell', '-NoProfile', '-Command',
        "(Get-Counter '\\Processor(_Total)\\% Processor Time' -SampleInterval 2 -MaxSamples 1).CounterSamples[0].CookedValue"],
        capture_output=True, text=True, encoding='utf-8', errors='replace')
    return r.stdout.strip()

def other_delta(a, b, skip_names):
    tot = 0.0; top = []
    for k, v in b.items():
        if k[1] in skip_names: continue
        dv = v - a.get(k, 0.0)
        if dv > 0:
            tot += dv; top.append((dv, k[1], k[0]))
    top.sort(reverse=True)
    return tot, top[:5]

def log(s):
    with open(OUT, 'ab') as f: f.write((s + '\n').encode('utf-8'))
    print(s, flush=True)

t0 = time.time()
done = 0
err = ''
try:
    log(f'# start {time.strftime("%F %T")} cpu%_total={cpu_total()}')
    plan = [(r, c) for r in range(3) for c in 'ABC']
    for rnd, cfg in plan:
        el = time.time() - t0
        if el > CAP:
            log(f'# CUT at {time.strftime("%F %T")} elapsed={el:.0f}s before round={rnd} cfg={cfg}')
            break
        env = dict(os.environ)
        env.pop('POB_COEF', None)
        if cfg == 'C': env['POB_COEF'] = 'LaborWeightsByPower=0'
        a = snap()
        p = subprocess.run([EXE, cfg, '1', '668'], capture_output=True, text=True, env=env, encoding='utf-8', errors='replace')
        b = snap()
        oth, top = other_delta(a, b, {'PobBench1049', 'powershell', 'Idle', 'System'})
        lines = p.stdout.strip().splitlines()
        head = lines[0] if lines else f'(no output rc={p.returncode}) {p.stderr[-300:]}'
        log(f'round={rnd} {head} other_cpu_s={oth:.1f} top={";".join(f"{n}/{i}:{v:.0f}" for v, n, i in top)}')
        if len(lines) > 1: log(f'round={rnd} cfg={cfg} {lines[1]}')
        done += 1
    log(f'# end {time.strftime("%F %T")} elapsed={time.time()-t0:.0f}s runs={done} cpu%_total={cpu_total()}')
except Exception as e:
    err = repr(e)[:200].replace('\n', ' ')
    try: log(f'# ERROR {err}')
    except Exception: pass
finally:
    with open(W + '/e1.mark', 'wb') as f:
        f.write(f'rc=0 end={time.strftime("%F %T")} runs={done}{" error=" + err if err else ""}\n'.encode('utf-8'))

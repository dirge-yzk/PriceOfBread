# -*- coding: utf-8 -*-
"""(332) batch.sh prep の〈編集の python〉：木の根（引数1）の world.json を腕 B1（イギリスの比だけ）にする。
  bash scripts/instruments/batch.sh prep <腕> commit:<sha> plain scripts/instruments/e7/edit_B1.py"""
import sys, os
root = sys.argv[1]; wj = os.path.join(root, 'PriceOfBread.SimCore', 'world.json')
here = os.path.dirname(os.path.abspath(__file__))
sys.argv = [os.path.join(here, 'make_world_b.py'), wj, wj, '--only-uk']
exec(compile(open(os.path.join(here, 'make_world_b.py'), 'rb').read().decode('utf-8'), os.path.join(here, 'make_world_b.py'), 'exec'))

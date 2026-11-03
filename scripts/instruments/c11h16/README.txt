(799) c11h16 資本逃避の不信の述語 Fiscal.CurrencyMemoryBroken の使い捨ての台本
- ProbeH16.cs.txt ＋ 述語の中の並行評価（probe.diff.txt）＝木 probe_tree（この枝の木に足して素の 10 種を回す・成績表は m797 と全行一致）
- mut.py ＝ 突然変異 M1〜M6 を使い捨ての木 mut_tree に当てて CurrencyMemoryBrokenTests を回す（出力 mutation_out.txt）
- 注：probe は述語が `/ Calendar.MonthsPerYear` の形だった木（e97afc63）で回した。最終の形（6672fabc）は `Calendar.PerYearToPerMonth(閾値)`＝中身は同じ `perYear / MonthsPerYear` の 1 回の除算で式の値は変わらない。恒等の腕（ident）は最終の木で回し直した。

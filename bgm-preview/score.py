"""The Bread Ledger / パンの帳簿 — an original chamber cue in 3/4."""

from dataclasses import dataclass
from random import Random
import re

BPM = 88
BEATS_PER_BAR = 3
TITLE = "The Bread Ledger"

# Ease off as each lead instrument moves into its upper register.
UPPER_REGISTER = {"violin": 74, "flute": 76, "clarinet": 77}


@dataclass(frozen=True)
class Note:
    part: str
    start: float
    duration: float
    pitch: int
    velocity: int


def pitch(name):
    letter, accidental, octave = re.fullmatch(r"([A-G])([#b]?)(-?\d)", name).groups()
    return (int(octave) + 1) * 12 + "C D EF G A B".index(letter) + {"": 0, "#": 1, "b": -1}[accidental]


# Bass, inner voice, upper voice, and the harp's three upper chord tones.
VOICINGS = {
    "Dm": "D3 F4 A4 D4 A4 F5",
    "Dm/F": "F3 D4 A4 F4 A4 D5",
    "Dm/A": "A2 F4 D5 D4 A4 F5",
    "Bb": "Bb2 F4 D5 F4 Bb4 D5",
    "Bb/D": "D3 F4 Bb4 D4 Bb4 F5",
    "Gm": "G2 Bb3 D4 D4 G4 Bb4",
    "Gm/Bb": "Bb2 G4 D5 D4 G4 Bb4",
    "A7": "A2 G4 C#5 E4 G4 C#5",
    "A7/E": "E3 C#4 G4 E4 A4 C#5",
    "Asus": "A2 E4 D5 E4 A4 D5",
    "F": "F3 A4 C5 F4 C5 A5",
    "F/C": "C3 A4 F5 F4 A4 C5",
    "C": "C3 E4 G4 E4 G4 C5",
    "C/E": "E3 G4 C5 E4 G4 C5",
    "C7": "C3 Bb3 E4 E4 G4 Bb4",
    "Am": "A2 E4 C5 E4 A4 C5",
    "E7/G#": "G#2 D4 B4 E4 G#4 D5",
    "Edim": "E3 G4 Bb4 E4 G4 Bb4",
    "Eb": "Eb3 G4 Bb4 Eb4 G4 Bb4",
    "Gm/D": "D3 G4 Bb4 D4 G4 Bb4",
    "D": "D3 F#4 A4 D4 A4 F#5",
}


@dataclass(frozen=True)
class Section:
    name: str
    chords: str
    lead: str
    phrases: tuple[str, ...]
    dynamic: int


# Each phrase fills one bar. Durations are eighth notes.
SECTIONS = (
    Section("I. Before the market opens", "Dm Dm/F Bb Asus", "clarinet", (
        "-/6", "-/5 A4/1", "D5/3 C5/1 Bb4/2", "A4/3 E4/2 -/1",
    ), 0),
    Section("II. The market", "Dm Dm/F Bb Gm A7/E Dm Bb A7", "clarinet", (
        "A4/1 D5/3 C5/1 A4/1", "F4/3 G4/1 A4/2", "Bb4/2 D5/2 F5/2", "E5/1 D5/1 Bb4/3 -/1",
        "A4/2 G4/1 E4/1 C#4/2", "D4/3 F4/1 A4/2", "Bb4/2 A4/1 F4/1 D4/2", "E4/2 C#4/3 -/1",
    ), 7),
    Section("III. The weight of a sack", "Gm Gm/Bb Dm/A A7 Dm/F C/E Bb C7", "violin", (
        "D5/3 Bb4/1 G4/2", "Bb4/2 D5/1 E5/1 F5/2", "A5/3 G5/1 F5/2", "E5/2 C#5/2 A4/1 -/1",
        "F5/2 E5/1 D5/1 A4/2", "G4/2 C5/3 -/1", "D5/2 C5/1 Bb4/1 F4/2", "G4/2 E4/2 C5/1 -/1",
    ), 10),
    Section("IV. Ships in the roadstead", "F C/E Dm Bb/D Gm C7 F/C F", "flute", (
        "A4/1 C5/1 F5/3 E5/1", "G5/2 E5/2 C5/1 -/1", "D5/2 F5/1 A5/3", "F5/2 D5/1 Bb4/2 -/1",
        "G5/2 D5/1 Bb4/1 D5/2", "E5/1 F5/1 G5/2 E5/2", "F5/3 E5/1 C5/2", "A4/3 C5/1 -/2",
    ), 12),
    Section("V. Credit comes due", "Am E7/G# Dm/F Edim Gm Eb Asus A7", "clarinet", (
        "E5/3 C5/1 A4/2", "B4/2 D5/1 E5/1 G#4/2", "A4/3 F4/1 D4/2", "E4/2 G4/2 Bb4/1 -/1",
        "D5/3 C5/1 Bb4/2", "G4/2 Bb4/1 Eb5/3", "D5/2 A4/2 E5/2", "G5/2 E5/1 C#5/2 -/1",
    ), 10),
    Section("VI. The ledger", "Dm Dm/F Bb Gm A7/E Dm/F Gm/Bb A7", "violin", (
        "A4/1 D5/3 F5/1 A5/1", "G5/2 F5/1 E5/1 D5/2", "Bb4/1 D5/1 F5/2 D5/2", "G5/2 F5/1 D5/2 -/1",
        "E5/2 G5/1 F5/1 C#5/2", "D5/2 F5/3 G5/1", "G5/2 F5/1 D5/1 Bb4/2", "E5/1 D5/1 C#5/3 -/1",
    ), 14),
    Section("VII. Lamps in the windows", "Dm Bb/D Gm/D Dm/A Bb A7 D Asus", "clarinet", (
        "D5/3 A4/1 F4/2", "Bb4/2 D5/3 -/1", "Bb4/2 G4/1 D4/3", "-/6",
        "F4/3 D4/1 Bb3/2", "C#4/2 E4/2 A4/1 -/1", "F#4/3 A4/1 D5/2", "E5/2 D5/2 -/2",
    ), -2),
)

# Answering phrases leave room for the lead, and enter at different points in the bar.
ANSWERS = {
    5: ("viola", "-/4 D4/2"),
    7: ("violin", "-/3 G4/2 D5/1"),
    9: ("viola", "A3/3 D4/2 -/1"),
    11: ("violin", "-/2 E5/2 D5/1 C#5/1"),
    13: ("clarinet", "-/2 G4/2 Bb4/2"),
    15: ("clarinet", "-/1 C#4/2 E4/2 -/1"),
    17: ("flute", "-/4 E5/2"),
    19: ("clarinet", "Bb4/2 G4/2 E4/2"),
    20: ("clarinet", "F4/2 A4/2 C5/2"),
    22: ("clarinet", "-/2 A4/2 F4/2"),
    24: ("violin", "Bb4/3 A4/1 G4/2"),
    27: ("clarinet", "F4/3 G4/1 A4/2"),
    28: ("viola", "C4/3 E4/1 A4/2"),
    31: ("violin", "-/2 Bb4/2 G4/2"),
    33: ("violin", "Bb4/3 G4/3"),
    35: ("viola", "-/1 C#4/2 E4/2 -/1"),
    36: ("clarinet", "F4/3 A4/1 D5/2"),
    38: ("flute", "-/2 F5/2 D5/2"),
    40: ("clarinet", "A4/2 G4/2 C#5/2"),
    42: ("clarinet", "G4/2 Bb4/2 D5/2"),
    43: ("flute", "-/2 E5/2 G5/2"),
    45: ("violin", "F5/3 D5/2 -/1"),
    47: ("violin", "A4/1 D5/2 C5/1 A4/2"),
    50: ("violin", "A4/3 F#4/2 -/1"),
}


def make_score():
    rng = Random(1783)
    notes = []
    markers = []
    bar = 0

    def note(part, beat, length, name, velocity):
        offset = rng.uniform(-.013, .018)
        key = pitch(name)
        upper = UPPER_REGISTER.get(part, 127)
        velocity -= round(.95 * max(0, key - upper))
        notes.append(Note(part, max(0, bar * 3 + beat + offset),
                          length, key, max(1, min(127, round(velocity + rng.uniform(-2, 2))))))

    def phrase(part, text, velocity):
        beat = 0
        for token in text.split():
            name, eighths = token.split("/")
            duration = int(eighths) / 2
            if name != "-":
                accent = 3 if beat == 0 else (-3 if duration == .5 else 0)
                note(part, beat, duration - (.04 if duration > .5 else .06),
                     name, velocity + accent + min(3, int(duration)))
            beat += duration
        if beat != BEATS_PER_BAR:
            raise ValueError(f"{part}, bar {bar + 1}: phrase has {beat} beats")

    for section in SECTIONS:
        markers.append((bar * 3, section.name))
        for index, (chord, melody) in enumerate(zip(section.chords.split(), section.phrases, strict=True)):
            bass, inner, upper, *harp = VOICINGS[chord].split()
            level = section.dynamic + (0, 1, 4, -2)[index % 4]
            phrase(section.lead, melody, 55 + level)
            if bar in ANSWERS:
                part, answer = ANSWERS[bar]
                phrase(part, answer, 42 + level)

            if bar in (0, 2, 28, 29, 34, 35, 44, 46, 50, 51):
                note("cello", 0, 2.78 if bar != 51 else 2.2, bass, 43 + level)
            else:
                note("pizzicato", 0, .72, bass, 52 + level)
                if index % 4 == 2 or 36 <= bar <= 43:
                    note("pizzicato", 2, .52, bass, 38 + level)
                if index % 4 == 1:
                    note("cello", .51, 2.31, bass, 38 + level)

            answer_part = ANSWERS.get(bar, ("", ""))[0]
            if answer_part != "viola" and bar not in (0, 1, 49):
                entrance = .52 if bar % 4 == 0 else .09
                note("viola", entrance, 2.66 - entrance, inner, 38 + level)
            if section.lead != "violin" and answer_part != "violin" and bar in (3, 8, 10, 21, 23, 25, 26, 28, 32, 34, 35, 46):
                note("violin", .99, 1.81, upper, 34 + level)
            if 28 <= bar <= 35:
                arp = ((0.50, 0), (1.50, 1), (2.00, 2))
            elif 20 <= bar <= 27 or 36 <= bar <= 43:
                arp = ((0.00, 0), (0.50, 1), (1.00, 2), (2.00, 1), (2.50, 2))
            elif bar in (0, 1, 44, 46, 50):
                arp = ((0.00, 0), (0.50, 1), (1.50, 2), (2.50, 1))
            elif bar == 51:
                arp = ((0.50, 0), (1.00, 1), (1.50, 2))
            elif index % 4 == 3:
                arp = ((0.0, 0), (0.5, 1), (1.0, 2))
            else:
                arp = ((0.0, 0), (1.0, 1), (1.5, 2), (2.0, 1))
            for beat, tone in arp:
                note("harp", beat, min(1.22, 3 - beat), harp[tone],
                     47 + level + (2 if beat == 0 else 0) - tone * 2)
            bar += 1

    return notes, markers, bar * BEATS_PER_BAR

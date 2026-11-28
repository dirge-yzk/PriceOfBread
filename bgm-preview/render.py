"""Render the score against VSCO 2 CE and export its editable MIDI performance."""

from collections import defaultdict
from dataclasses import dataclass
from math import cos, pi, sin
from pathlib import Path
import re
import subprocess

from imageio_ffmpeg import get_ffmpeg_exe
import mido
import numpy as np
from scipy import signal
import soundfile as sf

from score import BEATS_PER_BAR, BPM, TITLE, UPPER_REGISTER, make_score, pitch

ROOT = Path(__file__).resolve().parent
RATE = 48_000
SECONDS_PER_BEAT = 60 / BPM
STEM = "the-bread-ledger-1783"
TICKS = 960
PRESENCE_FILTER = signal.butter(2, 3100, "lowpass", fs=RATE, output="sos")


@dataclass(frozen=True)
class Part:
    program: int
    pan: float
    gain: float
    attack: float
    release: float
    sample_octave_shift: int
    plucked: bool = False


PARTS = {
    "harp": Part(46, -.36, .44, .002, .72, 0, True),
    "pizzicato": Part(45, .29, .56, .003, .28, 12, True),
    "cello": Part(42, .34, .59, .042, .25, 12),
    "viola": Part(41, .16, .43, .052, .21, 12),
    "violin": Part(40, -.27, .88, .032, .17, 0),
    "clarinet": Part(71, -.06, .75, .026, .15, 12),
    "flute": Part(73, .28, .72, .029, .15, 12),
}


@dataclass(frozen=True)
class Sample:
    root: int
    detune: float
    rate: int
    data: np.ndarray
    layer: int


def load_bank(part_name, part):
    folder = ROOT / ".samples" / part_name
    bank = []
    for path in sorted(folder.glob("*.wav")):
        audio, rate = sf.read(path, dtype="float32", always_2d=True)
        audio = signal.sosfilt(signal.butter(2, 38, "highpass", fs=rate, output="sos"), audio, axis=0)
        root = pitch(re.search(r"_([A-G]#?\d)_", path.name)[1]) + part.sample_octave_shift
        # Measure the partials near the labeled pitch and compensate for tuning drift.
        body = audio[int(.10 * rate):int(.68 * rate)].mean(axis=1)
        spectrum = abs(np.fft.rfft(body * np.hanning(len(body)), n=65536))
        frequencies = np.arange(len(spectrum)) * rate / 65536
        offsets = np.linspace(-.55, .55, 111)
        base = 440 * 2 ** ((root - 69 + offsets) / 12)
        partials = np.array([np.interp(base * harmonic, frequencies, spectrum) for harmonic in range(1, 7)])
        strength = (partials / np.sqrt(np.arange(1, 7))[:, None]).sum(axis=0)
        best = int(strength.argmax())
        if strength[best] < .22 * spectrum.max() or best in (0, len(offsets) - 1):
            continue

        amplitude = np.max(np.abs(audio), axis=1)
        start = np.argmax(amplitude > amplitude.max() * (.015 if part.plucked else .045))
        start = max(0, start - int(.004 * rate))
        audio = audio[start:]
        body = audio[:int(rate * (.32 if part.plucked else .85))]
        loudness = np.sqrt(np.mean(body * body))
        if loudness < 1e-5:
            continue
        layer = 2 if "_v2_" in path.name else 1
        bank.append(Sample(root, offsets[best], rate, (audio * (.10 / loudness)).astype(np.float32), layer))
    if not bank:
        raise ValueError(f"No usable recordings for {part_name}. Run fetch_samples.py first.")
    print(f"{part_name}: {len(bank)} samples, MIDI roots {sorted({sample.root for sample in bank})}")
    return bank


def render_note(note, part, bank, rng):
    preferred_layer = 2 if note.velocity >= 65 else 1
    rank = lambda sample: (abs(sample.root - note.pitch), sample.layer != preferred_layer)
    best = min(map(rank, bank))
    candidates = [sample for sample in bank if rank(sample) == best]
    sample = candidates[rng.integers(len(candidates))]
    duration = note.duration * SECONDS_PER_BEAT
    frequency_ratio = 2 ** ((note.pitch - sample.root - sample.detune + rng.normal(0, .018)) / 12)
    source_step = frequency_ratio * sample.rate / RATE
    frames = min(round((duration + part.release) * RATE), int((len(sample.data) - 1) / source_step))
    time = np.arange(frames, dtype=np.float64) / RATE
    position = time * RATE * source_step
    source_positions = np.arange(len(sample.data))
    audio = np.column_stack([
        np.interp(position, source_positions, channel) for channel in sample.data.T
    ])
    # Retain the fundamental while easing the upper harmonics and bow/breath attack.
    high_register = min(1, max(0, (note.pitch - UPPER_REGISTER.get(note.part, 127) + 2) / 7))
    if high_register:
        rounded = signal.sosfiltfilt(PRESENCE_FILTER, audio, axis=0)
        audio += (.48 * high_register) * (rounded - audio)
    attack = min(part.attack + .014 * high_register, (.12 + .05 * high_register) * duration)
    release_start = min(duration, time[-1] - min(part.release, .07))
    fade_in = np.sin(np.clip(time / attack, 0, 1) * pi / 2) ** 2
    fade_out = np.cos(np.clip((time - release_start) / (time[-1] - release_start), 0, 1) * pi / 2) ** 2
    if part.plucked:
        shape = 1.0
    else:
        phase = np.clip(time / duration, 0, 1)
        shape = .77 + .25 * np.sin(pi * phase ** .8) + .09 * phase

    # Preserve a little of the stereo recording while seating the players in the room.
    center = audio.mean(axis=1)
    width = (audio[:, 0] - center) * .22
    angle = (part.pan + 1) * pi / 4
    stereo = np.column_stack((center * cos(angle) + width, center * sin(angle) - width))
    strength = (note.velocity / 69) ** 1.7 * part.gain
    return (stereo * (fade_in * fade_out * shape * strength)[:, None]).astype(np.float32)


def add_wrapped(buffer, start, audio):
    first = min(len(audio), len(buffer) - start)
    buffer[start:start + first] += audio[:first]
    buffer[:len(audio) - first] += audio[first:]


def chamber_room(dry, rng):
    send = dry.mean(axis=1)
    wet = np.zeros_like(dry)
    time = np.arange(round(1.8 * RATE)) / RATE
    for channel, reflections in enumerate((
        ((.023, .35), (.039, .19), (.062, -.16), (.079, .17)),
        ((.028, .33), (.048, .22), (.071, -.14), (.091, .14)),
    )):
        diffuse = rng.normal(0, 1, len(time))
        diffuse = signal.sosfilt(signal.butter(2, (210, 5200), "bandpass", fs=RATE, output="sos"), diffuse)
        diffuse *= np.clip((time - .042) / .095, 0, 1) * np.exp(-5.2 * time)
        diffuse *= .54 / np.linalg.norm(diffuse)
        for delay, gain in reflections:
            diffuse[round(delay * RATE)] += gain
        # Fold the tail back into the beginning so room decay also loops.
        response = signal.oaconvolve(send, diffuse).astype(np.float32)
        wet[:, channel] = response[:len(send)]
        wet[:len(diffuse) - 1, channel] += response[len(send):]
    return dry + .25 * wet


def export_midi(notes, markers, beats):
    midi = mido.MidiFile(ticks_per_beat=TICKS)
    meta = midi.add_track(TITLE)
    meta.append(mido.MetaMessage("set_tempo", tempo=mido.bpm2tempo(BPM)))
    meta.append(mido.MetaMessage("time_signature", numerator=BEATS_PER_BAR, denominator=4))
    meta.append(mido.MetaMessage("key_signature", key="Dm"))
    previous = 0
    for beat, text in [*markers, (beats, "Loop end")]:
        tick = round(beat * TICKS)
        meta.append(mido.MetaMessage("marker", text=text, time=tick - previous))
        previous = tick

    by_part = defaultdict(list)
    for note in notes:
        by_part[note.part].append(note)
    for channel, (name, part) in enumerate(PARTS.items()):
        track = midi.add_track(name.capitalize())
        track.append(mido.Message("program_change", channel=channel, program=part.program))
        track.append(mido.Message("control_change", channel=channel, control=10, value=round(63.5 * (part.pan + 1))))
        by_pitch = defaultdict(list)
        for note in by_part[name]:
            by_pitch[note.pitch].append(note)
        events = []
        for key, repeated_notes in by_pitch.items():
            repeated_notes.sort(key=lambda note: note.start)
            for index, note in enumerate(repeated_notes):
                start = round(note.start * TICKS)
                end = round((note.start + note.duration) * TICKS)
                if index + 1 < len(repeated_notes):
                    next_start = round(repeated_notes[index + 1].start * TICKS)
                    end = min(end, next_start)
                events.extend(((start, note.velocity, key), (end, 0, key)))
        previous = 0
        for tick, velocity, key in sorted(events):
            track.append(mido.Message("note_on" if velocity else "note_off", note=key, velocity=velocity,
                                      channel=channel, time=tick - previous))
            previous = tick
    midi.save(ROOT / f"{STEM}.mid")


def export_audio(mix):
    wav = ROOT / f"{STEM}-loop.wav"
    ogg = ROOT / f"{STEM}-loop.ogg"
    sf.write(wav, mix, RATE, subtype="PCM_24")
    subprocess.run(
        [get_ffmpeg_exe(), "-v", "error", "-nostdin", "-y", "-i", str(wav),
         "-c:a", "libvorbis", "-q:a", "5", str(ogg)],
        check=True,
    )


def main():
    notes, markers, beats = make_score()
    banks = {name: load_bank(name, part) for name, part in PARTS.items()}
    rng = np.random.default_rng(1783)
    dry = np.zeros((round(beats * SECONDS_PER_BEAT * RATE), 2), dtype=np.float32)
    for note in notes:
        audio = render_note(note, PARTS[note.part], banks[note.part], rng)
        add_wrapped(dry, round(note.start * SECONDS_PER_BEAT * RATE), audio)
    mix = chamber_room(dry, rng)
    del dry
    # Use the periodic filter response at the seam too.
    highpass = signal.butter(2, 42, "highpass", fs=RATE, output="sos")
    _, state = signal.sosfilt(highpass, mix[-RATE:], axis=0, zi=np.zeros((len(highpass), 2, 2)))
    mix, _ = signal.sosfilt(highpass, mix, axis=0, zi=state)
    rms = np.sqrt(np.mean(mix * mix))
    # Set the playback level without flattening the dynamics.
    mix *= min(10 ** (-22 / 20) / rms, 10 ** (-3 / 20) / np.max(np.abs(mix)))
    export_audio(mix)
    export_midi(notes, markers, beats)
    print(f"{len(notes)} notes, {beats / BEATS_PER_BAR:.0f} bars, {len(mix) / RATE:.3f}s, {RATE} Hz")


if __name__ == "__main__":
    main()

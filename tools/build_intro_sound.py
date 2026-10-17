"""The X20CTL opening sound, synthesised from sine waves (owner request 17 Oct 2026: "a good sound effect").

Every sound is made here from oscillators: no samples, recordings or third-party kits, and nothing copied from another
product. It follows what the owner approved and refused in earlier passes:
  kept     soft D5/F#5/A5 sine notes for the three LED eyes, a short soft pad under the outline, volume-only swells
           under 900 Hz, the "dug dug" heartbeat of two low thumps with a muffled knock as the X locks
  refused  noise layers, pitch glides and sirens, detuned drones, bright bell partials, a chord at the end

Cues follow IntroView and IntroBladeFormation: LEDs .18/.36/.54 s, outline drawn .75-2.0 s, blades slide in 3.45-4.03 s,
lock 4.03 s (impact pulse 4.10 s), second beat 4.23 s, slogan 5.10 s, the X lands in the header at 6.85 s.

    python tools/build_intro_sound.py [output.wav]
"""
import sys
import wave
from pathlib import Path

import numpy as np
from scipy.signal import butter, sosfilt

RATE = 48000
TOTAL = 7.1
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parents[1] / "desktop-dotnet/X20Ctl.Product/Brand/intro.wav"

t = np.arange(int(RATE * TOTAL)) / RATE
left = np.zeros_like(t)
right = np.zeros_like(t)


def note(hz):
    return 440.0 * 2 ** ((hz - 69) / 12)


def lowpass(x, cutoff, order=2):
    return sosfilt(butter(order, cutoff, fs=RATE, output="sos"), x)


def place(signal, at, gain=1.0, pan=0.0):
    """Mix a mono signal in at a time, equal-power panned (-1 left, +1 right)."""
    start = int(at * RATE)
    end = min(len(t), start + len(signal))
    angle = (pan + 1) * np.pi / 4
    left[start:end] += signal[: end - start] * gain * np.cos(angle)
    right[start:end] += signal[: end - start] * gain * np.sin(angle)


def span(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def felt(hz, length=1.4, decay=.38):
    """A felt-mallet sine: a soft 6 ms attack, a quick settle into a long gentle decay, a whisper of second harmonic."""
    s = span(length)
    tone = np.sin(2 * np.pi * hz * s) + .06 * np.sin(2 * np.pi * 2 * hz * s)
    envelope = np.minimum(s / .006, 1) * (.55 * np.exp(-s / .05) + .45) * np.exp(-s / decay)
    return lowpass(tone * envelope, 1800)


def swell(hzs, length, rise, fall, cutoff=900):
    """Near-pure sines that only change in volume: a slow rise, a hold and a fall, never in pitch."""
    s = span(length)
    tone = sum(np.sin(2 * np.pi * hz * s) * w for hz, w in hzs)
    envelope = np.clip(s / rise, 0, 1) ** 2 * np.clip((length - s) / fall, 0, 1) ** 1.5
    return lowpass(tone * envelope, cutoff)


def thump(strength=1.0):
    """One beat of the heart: a round 70 Hz body (it settles from just above, inside 25 ms, the weight of a kick, not a
    glide), an octave above so laptop speakers carry it, and a muffled knock of two low sines on top."""
    s = span(.6)
    settle = 70 + 22 * np.exp(-s / .008)
    phase = 2 * np.pi * np.cumsum(settle) / RATE
    body = (np.sin(phase) + .38 * np.sin(2 * phase)) * np.minimum(s / .003, 1) * np.exp(-s / .13)
    knock = (np.sin(2 * np.pi * 180 * s) + .6 * np.sin(2 * np.pi * 265 * s)) * np.minimum(s / .002, 1) * np.exp(-s / .022)
    return lowpass(body + .45 * knock, 1200) * strength


# 1. the LED eyes, left to right: D5, F#5, A5
for at, midi, pan in ((.18, 74, -.18), (.36, 78, 0), (.54, 81, .18)):
    place(felt(note(midi)), at, .2, pan)

# 2. the outline being drawn: a short soft pad, D3 and A3 with a breath of D4, gone before the controller leans in
place(swell([(note(50), 1), (note(57), .7), (note(62), .25)], 1.85, .55, .8), .72, .09)

# 3. the blades sliding in: the same low notes swelling, only in volume, cut off by the lock
pull = swell([(note(45), .8), (note(50), 1), (note(57), .5)], .62, .55, .03)
place(pull, 3.42, .1)

# 4. the lock, then the heart: dug ... dug
place(thump(1.0), 4.03, .42)
place(thump(.72), 4.23, .42)

# 5. the slogan: one soft note, the eyes' D an octave lower, a rounded answer and no chord
place(felt(note(62), 1.6, .6), 5.12, .11)

# 6. the X lands in the header: the faintest knock
place(thump(.35), 6.85, .22)

# gentle edges and a peak that leaves headroom
mix = np.stack([left, right], axis=1)
mix *= .5 / np.max(np.abs(mix))
fade = np.minimum(1, (len(t) - np.arange(len(t))) / (.05 * RATE))
mix *= fade[:, None]

OUT.parent.mkdir(parents=True, exist_ok=True)
with wave.open(str(OUT), "wb") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(RATE)
    w.writeframes((np.clip(mix, -1, 1) * 32767).astype("<i2").tobytes())


def rms(a, b):
    part = mix[int(a * RATE):int(b * RATE)]
    return float(np.sqrt(np.mean(part ** 2)))


print(f"{OUT} {TOTAL}s peak {np.max(np.abs(mix)):.2f}")
for name, a, b in (("eyes", .15, .75), ("outline", .75, 2.6), ("pull", 3.42, 4.03), ("dug dug", 4.03, 4.6), ("slogan", 5.1, 6.0), ("land", 6.85, 7.05)):
    print(f"  {name:8} rms {rms(a, b):.3f}")

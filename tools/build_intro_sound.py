"""Generate original X20CTL sound design from oscillators and seeded noise only.
No recordings, samples, reference audio, or external assets are read.
Run: python tools/build_intro_sound.py
"""
from pathlib import Path
import wave, math
import numpy as np
from scipy.signal import butter, sosfilt

RATE = 48000
DURATION = 7.1
rng = np.random.default_rng(20261016)
audio = np.zeros((round(RATE * DURATION), 2), dtype=np.float64)

def add(start, signal, pan=0):
    offset = round(start * RATE)
    count = min(len(signal), len(audio) - offset)
    gains = np.array([math.sqrt((1-pan)/2), math.sqrt((1+pan)/2)])
    audio[offset:offset+count] += signal[:count, None] * gains

def sweep(start, duration, low, high, level, pan=0):
    t = np.arange(round(duration*RATE))/RATE
    u = t/duration
    phase = 2*np.pi*(low*t+(high-low)*t*t/(2*duration))
    envelope = np.sin(np.pi*u)**2
    noise = sosfilt(butter(2, [400, 3800], btype='bandpass', fs=RATE, output='sos'), rng.standard_normal(len(t)))
    add(start, (np.sin(phase)*.35+noise*.45)*envelope*level, pan)

# Air moves across the controller, then a separate mechanical low impact seats the X.
sweep(.35, 1.0, 180, 430, .12, -.45)
sweep(1.2, 1.25, 460, 210, .09, .45)
sweep(2.6, 1.45, 95, 620, .18)
t = np.arange(round(1.5*RATE))/RATE
impact = np.sin(2*np.pi*(52*t+38*.035*(1-np.exp(-t/.035))))
impact *= (1-np.exp(-t/.006))*np.exp(-t/.22)
add(4.02, impact*.32)
# Short, inharmonic metallic resolution: no melody and no sustained music bed.
for frequency, amplitude, decay, pan in [(731,.085,.24,-.25),(1139,.045,.18,.25),(1877,.018,.12,0)]:
    tone = np.sin(2*np.pi*frequency*t)*(1-np.exp(-t/.004))*np.exp(-t/decay)
    add(4.07, tone*amplitude, pan)
# Quiet original stereo reflections.
base=audio.copy()
for delay,gain in [(.083,.16),(.147,.09),(.229,.045)]:
    n=round(delay*RATE);audio[n:]+=base[:-n,::-1]*gain
peak=np.max(np.abs(audio))
audio *= .24/max(peak,1e-12)
audio[-round(.3*RATE):]=0
pcm=np.round(audio*32767).astype('<i2')
out=Path(__file__).resolve().parents[1]/'desktop-dotnet/X20Ctl.Product/Brand/original-intro.wav'
with wave.open(str(out),'wb') as target:
    target.setnchannels(2);target.setsampwidth(2);target.setframerate(RATE);target.writeframes(pcm.tobytes())
print(f'Generated original oscillator/noise audio: {DURATION}s; peak {20*math.log10(.24):.2f} dBFS')

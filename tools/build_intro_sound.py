"""Original stereo intro score; no reference audio is sampled.
Timed to IntroView: trace .35s, white reveal .75-1.95s, morph 2.65-3.65s,
X settlement 3.65-4.6s, header handoff 5.55-6.4s. Deterministic at 48 kHz.
"""
from pathlib import Path
import wave
import numpy as np
from scipy.signal import butter, sosfilt, fftconvolve

SR=48000
DUR=6.65
t=np.arange(round(SR*DUR))/SR
rng=np.random.default_rng(20261015)
stereo=np.zeros((len(t),2))

def envelope(start,attack,hold,release):
    x=t-start
    a=np.clip(x/attack,0,1)
    e=np.sin(a*np.pi/2)**2
    e*=np.exp(-np.maximum(x-attack-hold,0)/max(release/5,.001))
    return np.where(x>=0,e,0)

def filtered(x,cutoff,order=2):
    return sosfilt(butter(order,cutoff,fs=SR,output='sos'),x)

def add(signal,gain=1,pan=0):
    stereo[:,0]+=signal*gain*np.sqrt((1-pan)/2)
    stereo[:,1]+=signal*gain*np.sqrt((1+pan)/2)

def pad(start,attack,hold,release,gain,frequencies):
    e=envelope(start,attack,hold,release)
    for j,f in enumerate(frequencies):
        phase=rng.uniform(0,2*np.pi)
        # Slow detuning adds width without a piercing pitch riser.
        for detune,pan in [(-.22,-.55),(.22,.55)]:
            ph=2*np.pi*(f+detune)*t+phase
            tone=np.sin(ph)+.17*np.sin(2*ph)+.045*np.sin(4*ph)
            add(filtered(tone,2200)*e,gain/len(frequencies),pan)

# Quiet travelling texture and warm tonal bed; no separate LED pings.
noise=filtered(rng.normal(0,1,len(t)),1400)
noise-=filtered(noise,150)
noise/=max(np.sqrt(np.mean(noise**2)),1e-9)
add(noise*envelope(.3,.65,.15,1.2),.018,-.25)
pad(.32,.75,.15,1.8,.22,[110,146.832,220,293.665])
# First reveal opens the upper harmonics, then leaves room before the morph.
pad(.8,.6,.1,1.1,.12,[146.832,220,349.228,440])
# The main transformation is one full low swell with a soft onset, not a heartbeat.
pad(2.6,.56,.2,2.3,.34,[73.416,146.832,220,293.665,349.228])
add(noise*envelope(2.65,.38,.06,1.0),.029,.2)
# A restrained upper resolution makes the completed X audible on laptop speakers.
pad(3.5,.18,.05,2.5,.105,[293.665,440,587.33])

# Short stereo room; delayed energy follows the gesture, with no circular wraparound.
for channel in range(2):
    n=int(SR*1.6)
    ir=filtered(rng.normal(0,1,n),2800)*np.exp(-np.arange(n)/SR/.24)
    ir[:int(.025*SR)]=0
    ir/=max(np.sqrt(np.sum(ir*ir)),1e-9)
    stereo[:,channel]+=.18*fftconvolve(stereo[:,channel],ir)[:len(t)]
# Remove DC and guarantee a silent tail before the intro completes.
stereo-=stereo.mean(axis=0)
fade=np.clip(t/.02,0,1)*np.clip((6.4-t)/.65,0,1)
stereo*=fade[:,None]
peak=np.max(np.abs(stereo))
stereo*=.26/max(peak,1e-9)
pcm=np.round(stereo*32767).astype('<i2')
out=Path(__file__).resolve().parents[1]/'desktop-dotnet/X20Ctl.Product/Brand/intro.wav'
with wave.open(str(out),'wb') as f:
    f.setnchannels(2);f.setsampwidth(2);f.setframerate(SR);f.writeframes(pcm.tobytes())
print(f'Wrote {out}: {DUR}s stereo, peak {20*np.log10(np.max(np.abs(stereo))):.2f} dBFS')

"""Prepare the user's supplied reference effect for the local intro preview.
No music is synthesized. Requires a decoded 48 kHz stereo 16-bit reference WAV.
Usage: python tools/build_intro_sound.py --reference path/to/reference.wav
The app uses the prepared WAV directly and needs no Python audio dependency.
"""
import argparse,array,sys,wave,math
from pathlib import Path
parser=argparse.ArgumentParser();parser.add_argument('--reference',type=Path,required=True);args=parser.parse_args()
sr=48000;duration=7.1
with wave.open(str(args.reference),'rb') as source:
    if (source.getframerate(),source.getnchannels(),source.getsampwidth())!=(sr,2,2):raise ValueError('Expected 48 kHz stereo 16-bit WAV')
    source.setpos(round(2.25*sr));data=array.array('h',source.readframes(round(6.65*sr)))
if sys.byteorder!='little':data.byteswap()
needed=round(duration*sr)*2
data.extend([0]*max(0,needed-len(data)));data=data[:needed]
for i in range(len(data)//2):
    t=i/sr;gain=min(1,t/.012)*max(0,min(1,(6.65-t)/.2))
    data[2*i]=round(data[2*i]*gain);data[2*i+1]=round(data[2*i+1]*gain)
peak=max(abs(v) for v in data)/32768
out=Path(__file__).resolve().parents[1]/'desktop-dotnet/X20Ctl.Product/Brand/intro.wav'
if sys.byteorder!='little':data.byteswap()
with wave.open(str(out),'wb') as target:
    target.setnchannels(2);target.setsampwidth(2);target.setframerate(sr);target.writeframes(data.tobytes())
print(f'Prepared reference effect: {duration:.2f}s, peak {20*math.log10(peak):.2f}dBFS')
